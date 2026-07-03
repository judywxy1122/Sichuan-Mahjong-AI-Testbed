package application.gameplay;

import application.game.Game;
import model.basic.Tile;
import model.players.Player;

import java.util.ArrayList;
import java.util.LinkedHashSet;
import java.util.List;
import java.util.Set;

public class PlayerActionContext {
    private final Game game;
    private final Player player;

    public PlayerActionContext(Game game, Player player) {
        this.game = game;
        this.player = player;
    }

    public boolean isActionNeeded() {
        if (player.containsHu()) {
            return true;
        }
        if (player.isPlaying()) {
            return true;
        }
        return player.containsResponseAction();
    }

    public List<String> legalActionLabels() {
        List<String> actions = new ArrayList<>();
        if (player.containsHu()) {
            actions.add("hu");
        }
        if (!player.isPlaying() && player.containsResponseAction()) {
            addResponseActions(actions);
            return actions;
        }
        if (player.isPlaying() && player.containsKong()) {
            actions.add("kong");
            actions.add("skip");
            return actions;
        }
        if (player.isPlaying()) {
            for (Tile tile : uniqueDiscardTiles()) {
                actions.add("discard:" + TileCodec.code(tile));
            }
        }
        return actions;
    }

    private void addResponseActions(List<String> actions) {
        if (player.containsChow()) {
            actions.add("chow");
        }
        if (player.containsPung()) {
            actions.add("pung");
        }
        if (player.containsKong()) {
            actions.add("kong");
        }
        actions.add("skip");
    }

    public boolean isLegal(PlayDecision decision) {
        if (decision == null) {
            return false;
        }
        switch (decision.getAction()) {
            case HU:
                return player.containsHu();
            case CHOW:
                return !player.isPlaying() && player.containsChow();
            case PUNG:
                return !player.isPlaying() && player.containsPung();
            case KONG:
                return player.containsKong();
            case SKIP:
                return (!player.isPlaying() && player.containsResponseAction())
                        || (player.isPlaying() && player.containsKong());
            case DISCARD:
                return player.isPlaying() && !player.containsChouPungKong()
                        && resolveDiscardTile(decision.getTile()) != null;
            default:
                return false;
        }
    }

    public Tile resolveDiscardTile(Tile requestedTile) {
        if (requestedTile == null) {
            return null;
        }
        Tile newTile = player.getHand().getNewTile();
        if (newTile != null && newTile.equals(requestedTile)) {
            return newTile;
        }
        for (Tile tile : player.getHand().toList()) {
            if (tile.equals(requestedTile)) {
                return tile;
            }
        }
        return null;
    }

    public List<Tile> uniqueDiscardTiles() {
        Set<String> seen = new LinkedHashSet<>();
        List<Tile> tiles = new ArrayList<>();
        Tile newTile = player.getHand().getNewTile();
        if (newTile != null && seen.add(TileCodec.code(newTile))) {
            tiles.add(newTile);
        }
        for (Tile tile : player.getHand().toList()) {
            if (seen.add(TileCodec.code(tile))) {
                tiles.add(tile);
            }
        }
        return tiles;
    }

    public Tile getLastPlayedTile() {
        return game.getLastPlayedTile();
    }
}
