package application.gameplay;

import model.basic.Tile;

public class PlayDecision {
    public enum Action {
        HU,
        CHOW,
        PUNG,
        KONG,
        SKIP,
        DISCARD,
        NONE
    }

    private final Action action;
    private final Tile tile;
    private final String reason;
    private final String source;

    public PlayDecision(Action action, Tile tile, String reason, String source) {
        this.action = action;
        this.tile = tile;
        this.reason = reason == null ? "" : reason;
        this.source = source == null ? "" : source;
    }

    public static PlayDecision of(Action action, String reason, String source) {
        return new PlayDecision(action, null, reason, source);
    }

    public static PlayDecision discard(Tile tile, String reason, String source) {
        return new PlayDecision(Action.DISCARD, tile, reason, source);
    }

    public static PlayDecision none(String reason, String source) {
        return new PlayDecision(Action.NONE, null, reason, source);
    }

    public Action getAction() {
        return action;
    }

    public Tile getTile() {
        return tile;
    }

    public String getReason() {
        return reason;
    }

    public String getSource() {
        return source;
    }
}
