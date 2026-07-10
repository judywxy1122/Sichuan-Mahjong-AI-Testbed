using System.Collections.Generic;
using System.Linq;
using SichuanMahjong.Core.AiModel;
using SichuanMahjong.Core.Model;

namespace SichuanMahjong.Core.Gameplay
{
    public class AutoPlayController : IPlayController
    {
        private readonly ProbabilityAI probabilityAI = new ProbabilityAI();

        public string GetName()
        {
            return "Auto Play";
        }

        public PlayDecision Choose(Game game, Player player)
        {
            PlayerActionContext context = new PlayerActionContext(game, player);
            if (!context.IsActionNeeded())
            {
                return PlayDecision.None("No player action needed.", GetName());
            }

            if (player.ContainsHu())
            {
                return PlayDecision.Of(PlayDecision.ActionType.HU, "Take the available win.", GetName());
            }

            if (!player.IsPlaying() && player.ContainsResponseAction())
            {
                return ChooseResponse(context, player);
            }

            if (player.IsPlaying() && player.ContainsKong())
            {
                Tile kongTile = FindKongTile(player);
                probabilityAI.SetHand(player.GetHand());
                if (kongTile != null && probabilityAI.ShouldKong(kongTile))
                {
                    return PlayDecision.Of(PlayDecision.ActionType.KONG, "Probability AI accepts the kong.", GetName());
                }
                return PlayDecision.Of(PlayDecision.ActionType.SKIP, "Probability AI skips the kong.", GetName());
            }

            if (player.IsPlaying())
            {
                Tile tile = ChooseDiscardTile(context, player);
                if (tile != null)
                {
                    return PlayDecision.Discard(tile, "Probability AI selected a discard.", GetName());
                }
            }

            return PlayDecision.None("No legal action selected.", GetName());
        }

        private PlayDecision ChooseResponse(PlayerActionContext context, Player player)
        {
            Tile tile = context.GetLastPlayedTile();
            if (tile == null)
            {
                return PlayDecision.Of(PlayDecision.ActionType.SKIP, "No claim tile is available.", GetName());
            }

            probabilityAI.SetHand(player.GetHand());
            if (player.ContainsKong() && probabilityAI.ShouldKong(tile))
            {
                return PlayDecision.Of(PlayDecision.ActionType.KONG, "Probability AI accepts the kong.", GetName());
            }
            if (player.ContainsPung() && probabilityAI.ShouldPung(tile))
            {
                return PlayDecision.Of(PlayDecision.ActionType.PUNG, "Probability AI accepts the pung.", GetName());
            }
            if (player.ContainsChow() && probabilityAI.ShouldChow(tile))
            {
                return PlayDecision.Of(PlayDecision.ActionType.CHOW, "Probability AI accepts the chow.", GetName());
            }
            return PlayDecision.Of(PlayDecision.ActionType.SKIP, "Probability AI skips the claim.", GetName());
        }

        private Tile ChooseDiscardTile(PlayerActionContext context, Player player)
        {
            probabilityAI.SetHand(player.GetHand());
            Tile proposed = probabilityAI.GetTileToPlay();
            Tile resolved = context.ResolveDiscardTile(proposed);
            if (resolved != null)
            {
                return resolved;
            }
            Tile newTile = player.GetHand().GetNewTile();
            if (newTile != null)
            {
                return newTile;
            }
            List<Tile> tiles = player.GetHand().ToList();
            return tiles.Count == 0 ? null : tiles[0];
        }

        private static Tile FindKongTile(Player player)
        {
            Tile newTile = player.GetHand().GetNewTile();
            if (newTile != null)
            {
                return newTile;
            }
            List<Tile> tiles = player.GetHand().ToList();
            foreach (Tile tile in tiles)
            {
                if (tiles.Count(t => t.Equals(tile)) >= 4)
                {
                    return tile;
                }
            }
            return null;
        }
    }
}
