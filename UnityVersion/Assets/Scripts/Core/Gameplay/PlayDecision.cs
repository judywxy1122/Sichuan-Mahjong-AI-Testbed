using SichuanMahjong.Core.Model;

namespace SichuanMahjong.Core.Gameplay
{
    public class PlayDecision
    {
        public enum ActionType
        {
            HU,
            CHOW,
            PUNG,
            KONG,
            SKIP,
            DISCARD,
            NONE
        }

        private readonly ActionType action;
        private readonly Tile tile;
        private readonly string reason;
        private readonly string source;

        public PlayDecision(ActionType action, Tile tile, string reason, string source)
        {
            this.action = action;
            this.tile = tile;
            this.reason = reason ?? "";
            this.source = source ?? "";
        }

        public static PlayDecision Of(ActionType action, string reason, string source)
        {
            return new PlayDecision(action, null, reason, source);
        }

        public static PlayDecision Discard(Tile tile, string reason, string source)
        {
            return new PlayDecision(ActionType.DISCARD, tile, reason, source);
        }

        public static PlayDecision None(string reason, string source)
        {
            return new PlayDecision(ActionType.NONE, null, reason, source);
        }

        public ActionType GetAction()
        {
            return action;
        }

        public Tile GetTile()
        {
            return tile;
        }

        public string GetReason()
        {
            return reason;
        }

        public string GetSource()
        {
            return source;
        }
    }
}
