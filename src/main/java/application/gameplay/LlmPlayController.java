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
import java.util.List;
import java.util.concurrent.TimeUnit;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

public class LlmPlayController implements PlayController {
    private static final int TIMEOUT_SECONDS = 8;
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

    @Override
    public PlayDecision choose(Game game, Player player) {
        PlayerActionContext context = new PlayerActionContext(game, player);
        if (!context.isActionNeeded()) {
            return PlayDecision.none("No player action needed.", getName());
        }

        String apiKey = System.getenv("GPT_API_SG_KEY");
        if (apiKey == null || apiKey.trim().isEmpty()) {
            return fallback(game, player, "GPT_API_SG_KEY is not set.");
        }

        try {
            String response = callLlmBridge(buildRequestJson(game, player, context));
            PlayDecision decision = parseDecision(response, context);
            if (context.isLegal(decision)) {
                return decision;
            }
            return fallback(game, player, "LLM returned an illegal action.");
        } catch (IOException | InterruptedException | RuntimeException e) {
            if (e instanceof InterruptedException) {
                Thread.currentThread().interrupt();
            }
            return fallback(game, player, "LLM unavailable: " + e.getMessage());
        }
    }

    private PlayDecision fallback(Game game, Player player, String reason) {
        PlayDecision fallback = fallbackController.choose(game, player);
        return new PlayDecision(fallback.getAction(), fallback.getTile(),
                reason + " Fallback: " + fallback.getReason(), getName() + " -> " + fallback.getSource());
    }

    private String callLlmBridge(String requestJson) throws IOException, InterruptedException {
        ProcessBuilder builder = new ProcessBuilder("python3", "scripts/llm_play.py");
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
            throw new IOException(output.toString().trim());
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
        action = action.toLowerCase();
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
            Tile tile = context.resolveDiscardTile(TileCodec.fromCode(tileCode));
            return PlayDecision.discard(tile, reason, getName());
        }
        return PlayDecision.none("Unknown LLM action: " + action, getName());
    }

    private String extract(Pattern pattern, String text) {
        Matcher matcher = pattern.matcher(text == null ? "" : text);
        return matcher.find() ? matcher.group(1) : null;
    }
}
