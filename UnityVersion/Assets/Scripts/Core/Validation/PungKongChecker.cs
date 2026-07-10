using System.Collections.Generic;
using System.Linq;
using SichuanMahjong.Core.Model;

namespace SichuanMahjong.Core.Validation
{
    public class PungKongChecker
    {
        private readonly Player player;
        private readonly List<Tile> playerHand;
        private readonly Tile newTile;

        public PungKongChecker(Player player, Tile newTile)
        {
            this.player = player;
            playerHand = player.GetHand().ToList();
            this.newTile = newTile;
        }

        public bool CanPung()
        {
            if (player.IsPlaying())
            {
                return false;
            }
            int tileCount = playerHand.Count(t => t.Equals(newTile));
            return tileCount >= 2;
        }

        public bool CanNormalKong()
        {
            if (player.IsPlaying())
            {
                return false;
            }
            int tileCount = playerHand.Count(t => t.Equals(newTile));
            return tileCount >= 3;
        }

        public bool CanAddKong()
        {
            if (player.IsWaiting())
            {
                return false;
            }
            List<Group> pungs = player.GetHand().GetPung();
            foreach (Group pung in pungs)
            {
                if (newTile.Equals(pung.ToList()[1]))
                {
                    return true;
                }
            }
            return false;
        }

        public bool CanHiddenKong()
        {
            if (player.IsWaiting())
            {
                return false;
            }
            foreach (Tile tile in playerHand)
            {
                if (playerHand.Count(t => t.Equals(tile)) == 4)
                {
                    return true;
                }
            }
            int tileCount = playerHand.Count(t => t.Equals(newTile));
            return tileCount >= 3;
        }
    }
}
