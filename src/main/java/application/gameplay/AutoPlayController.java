package application.gameplay;

import aimodel.ProbabilityAI;
import application.game.Game;
import model.basic.Tile;
import model.players.Player;

import java.util.Collections;
import java.util.List;

public class AutoPlayController implements PlayController {
    private final ProbabilityAI probabilityAI = new ProbabilityAI();

    @Override
    public String getName() {
        return "Auto Play";
    }

    @Override
    public PlayDecision choose(Game game, Player player) {
        PlayerActionContext context = new PlayerActionContext(game, player);
        if (!context.isActionNeeded()) {
            return PlayDecision.none("No player action needed.", getName());
        }

        if (player.containsHu()) {
            return PlayDecision.of(PlayDecision.Action.HU, "Take the available win.", getName());
        }

        if (!player.isPlaying() && player.containsResponseAction()) {
            return chooseResponse(context, player);
        }

        if (player.isPlaying() && player.containsKong()) {
            Tile kongTile = findKongTile(player);
            probabilityAI.setHand(player.getHand());
            if (kongTile != null && probabilityAI.shouldKong(kongTile)) {
                return PlayDecision.of(PlayDecision.Action.KONG, "Probability AI accepts the kong.", getName());
            }
            return PlayDecision.of(PlayDecision.Action.SKIP, "Probability AI skips the kong.", getName());
        }

        if (player.isPlaying()) {
            Tile tile = chooseDiscardTile(context, player);
            if (tile != null) {
                return PlayDecision.discard(tile, "Probability AI selected a discard.", getName());
            }
        }

        return PlayDecision.none("No legal action selected.", getName());
    }

    private PlayDecision chooseResponse(PlayerActionContext context, Player player) {
        Tile tile = context.getLastPlayedTile();
        if (tile == null) {
            return PlayDecision.of(PlayDecision.Action.SKIP, "No claim tile is available.", getName());
        }

        probabilityAI.setHand(player.getHand());
        if (player.containsKong() && probabilityAI.shouldKong(tile)) {
            return PlayDecision.of(PlayDecision.Action.KONG, "Probability AI accepts the kong.", getName());
        }
        if (player.containsPung() && probabilityAI.shouldPung(tile)) {
            return PlayDecision.of(PlayDecision.Action.PUNG, "Probability AI accepts the pung.", getName());
        }
        if (player.containsChow() && probabilityAI.shouldChow(tile)) {
            return PlayDecision.of(PlayDecision.Action.CHOW, "Probability AI accepts the chow.", getName());
        }
        return PlayDecision.of(PlayDecision.Action.SKIP, "Probability AI skips the claim.", getName());
    }

    private Tile chooseDiscardTile(PlayerActionContext context, Player player) {
        probabilityAI.setHand(player.getHand());
        Tile proposed = probabilityAI.getTileToPlay();
        Tile resolved = context.resolveDiscardTile(proposed);
        if (resolved != null) {
            return resolved;
        }
        Tile newTile = player.getHand().getNewTile();
        if (newTile != null) {
            return newTile;
        }
        List<Tile> tiles = player.getHand().toList();
        return tiles.isEmpty() ? null : tiles.get(0);
    }

    private Tile findKongTile(Player player) {
        Tile newTile = player.getHand().getNewTile();
        if (newTile != null) {
            return newTile;
        }
        List<Tile> tiles = player.getHand().toList();
        for (Tile tile : tiles) {
            if (Collections.frequency(tiles, tile) >= 4) {
                return tile;
            }
        }
        return null;
    }
}
