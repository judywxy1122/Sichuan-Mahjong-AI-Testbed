using System.Collections.Generic;
using SichuanMahjong.Core.Model;

namespace SichuanMahjong.Core.Gameplay
{
    public class PlayerActionContext
    {
        private readonly Game game;
        private readonly Player player;

        public PlayerActionContext(Game game, Player player)
        {
            this.game = game;
            this.player = player;
        }

        public bool IsActionNeeded()
        {
            if (player.ContainsHu())
            {
                return true;
            }
            if (player.IsPlaying())
            {
                return true;
            }
            return player.ContainsResponseAction();
        }

        public List<string> LegalActionLabels()
        {
            List<string> actions = new List<string>();
            if (player.ContainsHu())
            {
                actions.Add("hu");
            }
            if (!player.IsPlaying() && player.ContainsResponseAction())
            {
                AddResponseActions(actions);
                return actions;
            }
            if (player.IsPlaying() && player.ContainsKong())
            {
                actions.Add("kong");
                actions.Add("skip");
                return actions;
            }
            if (player.IsPlaying())
            {
                foreach (Tile tile in UniqueDiscardTiles())
                {
                    actions.Add("discard:" + TileCodec.Code(tile));
                }
            }
            return actions;
        }

        private void AddResponseActions(List<string> actions)
        {
            if (player.ContainsChow())
            {
                actions.Add("chow");
            }
            if (player.ContainsPung())
            {
                actions.Add("pung");
            }
            if (player.ContainsKong())
            {
                actions.Add("kong");
            }
            actions.Add("skip");
        }

        public bool IsLegal(PlayDecision decision)
        {
            if (decision == null)
            {
                return false;
            }
            switch (decision.GetAction())
            {
                case PlayDecision.ActionType.HU:
                    return player.ContainsHu();
                case PlayDecision.ActionType.CHOW:
                    return !player.IsPlaying() && player.ContainsChow();
                case PlayDecision.ActionType.PUNG:
                    return !player.IsPlaying() && player.ContainsPung();
                case PlayDecision.ActionType.KONG:
                    return player.ContainsKong();
                case PlayDecision.ActionType.SKIP:
                    return (!player.IsPlaying() && player.ContainsResponseAction())
                           || (player.IsPlaying() && player.ContainsKong());
                case PlayDecision.ActionType.DISCARD:
                    return player.IsPlaying() && !player.ContainsChouPungKong()
                           && ResolveDiscardTile(decision.GetTile()) != null;
                default:
                    return false;
            }
        }

        public Tile ResolveDiscardTile(Tile requestedTile)
        {
            if (requestedTile == null)
            {
                return null;
            }
            Tile newTile = player.GetHand().GetNewTile();
            if (newTile != null && newTile.Equals(requestedTile))
            {
                return newTile;
            }
            foreach (Tile tile in player.GetHand().ToList())
            {
                if (tile.Equals(requestedTile))
                {
                    return tile;
                }
            }
            return null;
        }

        public List<Tile> UniqueDiscardTiles()
        {
            HashSet<string> seen = new HashSet<string>();
            List<Tile> tiles = new List<Tile>();
            Tile newTile = player.GetHand().GetNewTile();
            if (newTile != null && seen.Add(TileCodec.Code(newTile)))
            {
                tiles.Add(newTile);
            }
            foreach (Tile tile in player.GetHand().ToList())
            {
                if (seen.Add(TileCodec.Code(tile)))
                {
                    tiles.Add(tile);
                }
            }
            return tiles;
        }

        public Tile GetLastPlayedTile()
        {
            return game.GetLastPlayedTile();
        }
    }
}
