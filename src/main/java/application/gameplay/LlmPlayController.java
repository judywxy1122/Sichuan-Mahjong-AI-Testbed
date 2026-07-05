package application.gameplay;

import application.game.Game;
import model.basic.Tile;
import model.players.Player;
import model.tiles.Group;

import java.io.BufferedReader;
import java.io.BufferedWriter;
import java.io.File;
import java.io.IOException;
import java.io.InputStreamReader;
import java.io.OutputStreamWriter;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.nio.file.StandardOpenOption;
import java.time.LocalDateTime;
import java.util.List;
import java.util.concurrent.TimeUnit;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

public class LlmPlayController implements PlayController {
    private static final int TIMEOUT_SECONDS = 32;
    private static final Path LLM_LOG_PATH = Paths.get("llm_play_log.txt");
    private static final Pattern ACTION_PATTERN = Pattern.compile("\"action\"\\s*:\\s*\"([^\"]*)\"");
    private static final Pattern TILE_PATTERN = Pattern.compile("\"tile\"\\s*:\\s*(?:\"([^\"]*)\"|null)");
    private static final Pattern REASON_PATTERN = Pattern.compile("\"reason\"\\s*:\\s*\"([^\"]*)\"");

    private final AutoPlayController fallbackController;

    public LlmPlayController(AutoPlayController fallbackController) {
        this.fallbackController = fallbackController;
    }

    @Override
    public String getName() {
        return "LLM Play";
    }

    public void markNewGame(Game game) {
        if (game == null) {
            appendLlmLog("NEW GAME", "A new Mahjong hand started.");
            return;
        }
        appendLlmLog("NEW GAME", "A new Mahjong hand started."
                + "\nlast_action=" + game.getLastActionText()
                + "\nstatus=" + game.getStatusText()
                + "\nturn_player=" + (game.getTurnPlayer() == null ? "" : game.getTurnPlayer().getName()));
    }

    @Override
    public PlayDecision choose(Game game, Player player) {
        PlayerActionContext context = new PlayerActionContext(game, player);
        if (!context.isActionNeeded()) {
            return PlayDecision.none("No player action needed.", getName());
        }

        String apiKey = System.getenv("GPT_API_SG_KEY");
        if (apiKey == null || apiKey.trim().isEmpty()) {
            return fallback(game, player, "GPT_API_SG_KEY is not set.", null, null, null);
        }

        String requestJson = buildRequestJson(game, player, context);
        appendLlmLog("REQUEST", "legal_actions=" + context.legalActionLabels()
                + "\nrequest_json=" + requestJson);
        try {
            long startedAt = System.currentTimeMillis();
            String response = callLlmBridge(requestJson);
            long elapsedMs = System.currentTimeMillis() - startedAt;
            appendLlmLog("RESPONSE", "elapsed_ms=" + elapsedMs + "\nraw_response=" + response.trim());
            PlayDecision decision = parseDecision(response, context);
            if (context.isLegal(decision)) {
                appendLlmLog("ACCEPTED", describeDecision(decision));
                return decision;
            }
            return fallback(game, player, "LLM returned an illegal action: "
                    + describeDecision(decision)
                    + "; legal actions: " + context.legalActionLabels() + ".",
                    requestJson, response, decision);
        } catch (IOException | InterruptedException | RuntimeException e) {
            if (e instanceof InterruptedException) {
                Thread.currentThread().interrupt();
            }
            return fallback(game, player, "LLM unavailable: " + e.getMessage(), requestJson, null, null);
        }
    }

    private PlayDecision fallback(Game game, Player player, String reason,
                                  String requestJson, String response, PlayDecision rejectedDecision) {
        PlayDecision fallback = fallbackController.choose(game, player);
        appendLlmLog("FALLBACK", "reason=" + reason
                + "\nfallback_decision=" + describeDecision(fallback)
                + (rejectedDecision == null ? "" : "\nrejected_decision=" + describeDecision(rejectedDecision))
                + (response == null ? "" : "\nraw_response=" + response.trim())
                + (requestJson == null ? "" : "\nrequest_json=" + requestJson));
        return new PlayDecision(fallback.getAction(), fallback.getTile(),
                reason + " Fallback: " + fallback.getReason(), getName() + " -> " + fallback.getSource());
    }

    private String callLlmBridge(String requestJson) throws IOException, InterruptedException {
        String python = System.getenv("MAHJONG_LLM_PYTHON");
        if (python == null || python.trim().isEmpty()) {
            python = "python3";
        }
        ProcessBuilder builder = new ProcessBuilder(python, "scripts/llm_play.py");
        builder.directory(new File("."));
        builder.redirectErrorStream(true);
        Process process = builder.start();

        try (BufferedWriter writer = new BufferedWriter(
                new OutputStreamWriter(process.getOutputStream(), StandardCharsets.UTF_8))) {
            writer.write(requestJson);
        }

        if (!process.waitFor(TIMEOUT_SECONDS, TimeUnit.SECONDS)) {
            process.destroyForcibly();
            throw new IOException("timed out after " + TIMEOUT_SECONDS + "s");
        }

        StringBuilder output = new StringBuilder();
        try (BufferedReader reader = new BufferedReader(
                new InputStreamReader(process.getInputStream(), StandardCharsets.UTF_8))) {
            String line;
            while ((line = reader.readLine()) != null) {
                output.append(line).append('\n');
            }
        }

        if (process.exitValue() != 0) {
            String reason = extract(REASON_PATTERN, output.toString());
            throw new IOException(reason == null ? output.toString().trim() : reason);
        }
        return output.toString();
    }

    private String buildRequestJson(Game game, Player player, PlayerActionContext context) {
        StringBuilder json = new StringBuilder();
        json.append("{");
        field(json, "player", player.getName()).append(",");
        field(json, "last_action", game.getLastActionText()).append(",");
        field(json, "status", game.getStatusText()).append(",");
        field(json, "last_played_tile", TileCodec.code(game.getLastPlayedTile())).append(",");
        arrayField(json, "legal_actions", context.legalActionLabels()).append(",");
        tileArrayField(json, "hand", player.getHand().toList()).append(",");
        field(json, "new_tile", TileCodec.code(player.getHand().getNewTile())).append(",");
        tileArrayField(json, "your_discards", player.getTable().toList()).append(",");
        groupArrayField(json, "melds", player.getHand().getPungKong());
        json.append("}");
        return json.toString();
    }

    private StringBuilder field(StringBuilder json, String name, String value) {
        return json.append("\"").append(name).append("\":\"").append(escape(value)).append("\"");
    }

    private StringBuilder arrayField(StringBuilder json, String name, List<String> values) {
        json.append("\"").append(name).append("\":[");
        for (int i = 0; i < values.size(); i++) {
            if (i > 0) {
                json.append(",");
            }
            json.append("\"").append(escape(values.get(i))).append("\"");
        }
        return json.append("]");
    }

    private StringBuilder tileArrayField(StringBuilder json, String name, List<Tile> tiles) {
        json.append("\"").append(name).append("\":[");
        for (int i = 0; i < tiles.size(); i++) {
            if (i > 0) {
                json.append(",");
            }
            json.append("\"").append(TileCodec.code(tiles.get(i))).append("\"");
        }
        return json.append("]");
    }

    private StringBuilder groupArrayField(StringBuilder json, String name, List<Group> groups) {
        json.append("\"").append(name).append("\":[");
        for (int i = 0; i < groups.size(); i++) {
            if (i > 0) {
                json.append(",");
            }
            json.append("{");
            field(json, "type", groups.get(i).getCategory().toString()).append(",");
            tileArrayField(json, "tiles", groups.get(i).toList());
            json.append("}");
        }
        return json.append("]");
    }

    private String escape(String value) {
        if (value == null) {
            return "";
        }
        return value.replace("\\", "\\\\").replace("\"", "\\\"");
    }

    private PlayDecision parseDecision(String response, PlayerActionContext context) {
        String action = extract(ACTION_PATTERN, response);
        String tileCode = extract(TILE_PATTERN, response);
        String reason = extract(REASON_PATTERN, response);
        if (action == null) {
            return PlayDecision.none("LLM response had no action.", getName());
        }
        action = action.trim().toLowerCase();
        if (action.startsWith("discard:") && (tileCode == null || tileCode.trim().isEmpty())) {
            tileCode = action.substring("discard:".length());
            action = "discard";
        }
        action = normalizeAction(action);
        if ("hu".equals(action)) {
            return PlayDecision.of(PlayDecision.Action.HU, reason, getName());
        }
        if ("chow".equals(action)) {
            return PlayDecision.of(PlayDecision.Action.CHOW, reason, getName());
        }
        if ("pung".equals(action)) {
            return PlayDecision.of(PlayDecision.Action.PUNG, reason, getName());
        }
        if ("kong".equals(action)) {
            return PlayDecision.of(PlayDecision.Action.KONG, reason, getName());
        }
        if ("skip".equals(action)) {
            return PlayDecision.of(PlayDecision.Action.SKIP, reason, getName());
        }
        if ("discard".equals(action)) {
            String normalizedTileCode = normalizeTileCode(tileCode);
            Tile tile = context.resolveDiscardTile(TileCodec.fromCode(normalizedTileCode));
            String discardReason = reason;
            if (tile == null) {
                discardReason = appendReason(reason, "raw tile=" + normalizedTileCode);
            }
            return PlayDecision.discard(tile, discardReason, getName());
        }
        return PlayDecision.none("Unknown LLM action: " + action, getName());
    }

    private String normalizeAction(String action) {
        if ("win".equals(action) || "hu牌".equals(action) || "胡".equals(action)) {
            return "hu";
        }
        if ("chi".equals(action) || "chou".equals(action) || "eat".equals(action) || "吃".equals(action)) {
            return "chow";
        }
        if ("pong".equals(action) || "peng".equals(action) || "碰".equals(action)) {
            return "pung";
        }
        if ("gang".equals(action) || "杠".equals(action)) {
            return "kong";
        }
        if ("pass".equals(action) || "fold".equals(action) || "过".equals(action)) {
            return "skip";
        }
        return action;
    }

    private String normalizeTileCode(String tileCode) {
        if (tileCode == null) {
            return null;
        }
        return tileCode.trim().toUpperCase();
    }

    private String describeDecision(PlayDecision decision) {
        if (decision == null) {
            return "null";
        }
        StringBuilder builder = new StringBuilder(decision.getAction().name().toLowerCase());
        if (decision.getTile() != null) {
            builder.append(" ").append(TileCodec.code(decision.getTile()));
        }
        if (decision.getReason() != null && !decision.getReason().isEmpty()) {
            builder.append(" (").append(decision.getReason()).append(")");
        }
        return builder.toString();
    }

    private String appendReason(String reason, String detail) {
        if (reason == null || reason.isEmpty()) {
            return detail;
        }
        return reason + "; " + detail;
    }

    private synchronized void appendLlmLog(String section, String body) {
        String text = "\n=== " + LocalDateTime.now() + " " + section + " ===\n"
                + (body == null ? "" : body)
                + "\n";
        try {
            Files.write(LLM_LOG_PATH, text.getBytes(StandardCharsets.UTF_8),
                    StandardOpenOption.CREATE, StandardOpenOption.APPEND);
        } catch (IOException e) {
            System.err.println("Could not write LLM play log: " + e.getMessage());
        }
    }

    private String extract(Pattern pattern, String text) {
        Matcher matcher = pattern.matcher(text == null ? "" : text);
        return matcher.find() ? matcher.group(1) : null;
    }
}
