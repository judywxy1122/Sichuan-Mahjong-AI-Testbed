using System.Collections.Generic;
using SichuanMahjong.Core.Model;

namespace SichuanMahjong.Core.Gameplay
{
    /// <summary>
    /// Arranges a winner's concealed tiles into display groups (pairs /
    /// triplets / sequences). Extracted from the Java GamePanel so the logic
    /// is UI-independent; the Unity winning-hand dialog consumes the result.
    /// </summary>
    public static class WinningHandArranger
    {
        public static List<List<Tile>> ArrangeConcealedWinningGroups(Player winner, Tile winningTile)
        {
            List<Tile> tiles = winner.GetHand().ToList();
            if (winner.GetHand().GetNewTile() != null)
            {
                tiles.Add(winner.GetHand().GetNewTile());
            }
            else if (winningTile != null)
            {
                tiles.Add(winningTile);
            }
            SortTiles(tiles);

            List<List<Tile>> sevenPairs = TryArrangeSevenPairs(tiles);
            if (sevenPairs.Count > 0)
            {
                return sevenPairs;
            }

            int meldCount = winner.GetHand().GetPungKong().Count;
            int groupsNeeded = 4 - meldCount;
            return TryArrangeStandardHu(tiles, groupsNeeded);
        }

        private static void SortTiles(List<Tile> tiles)
        {
            tiles.Sort((a, b) =>
            {
                if (a.GetTileType() == b.GetTileType())
                {
                    return a.GetNumber() - b.GetNumber();
                }
                return a.GetTileType().CompareTo(b.GetTileType());
            });
        }

        private static List<List<Tile>> TryArrangeSevenPairs(List<Tile> tiles)
        {
            if (tiles.Count != 14)
            {
                return new List<List<Tile>>();
            }
            List<Tile> remaining = new List<Tile>(tiles);
            List<List<Tile>> result = new List<List<Tile>>();
            while (remaining.Count > 0)
            {
                Tile first = remaining[0];
                remaining.RemoveAt(0);
                int matchIndex = FindMatchingTileIndex(remaining, first);
                if (matchIndex < 0)
                {
                    return new List<List<Tile>>();
                }
                List<Tile> pair = new List<Tile> { first, remaining[matchIndex] };
                remaining.RemoveAt(matchIndex);
                result.Add(pair);
            }
            return result;
        }

        private static List<List<Tile>> TryArrangeStandardHu(List<Tile> tiles, int groupsNeeded)
        {
            for (int i = 0; i < tiles.Count; i++)
            {
                Tile pairTile = tiles[i];
                int pairIndex = FindMatchingTileIndex(tiles, pairTile, i + 1);
                if (pairIndex < 0)
                {
                    continue;
                }

                List<Tile> remaining = new List<Tile>(tiles);
                Tile secondPairTile = remaining[pairIndex];
                remaining.RemoveAt(pairIndex);
                Tile firstPairTile = remaining[i];
                remaining.RemoveAt(i);
                List<List<Tile>> groups = new List<List<Tile>>();
                if (ArrangeGroups(remaining, groupsNeeded, groups))
                {
                    groups.Add(new List<Tile> { firstPairTile, secondPairTile });
                    return groups;
                }
            }
            return new List<List<Tile>>();
        }

        private static bool ArrangeGroups(List<Tile> remaining, int groupsNeeded, List<List<Tile>> groups)
        {
            if (groupsNeeded == 0)
            {
                return remaining.Count == 0;
            }
            if (remaining.Count < 3)
            {
                return false;
            }

            SortTiles(remaining);
            Tile first = remaining[0];

            int secondSame = FindMatchingTileIndex(remaining, first, 1);
            if (secondSame >= 0)
            {
                int thirdSame = FindMatchingTileIndex(remaining, first, secondSame + 1);
                if (thirdSame >= 0)
                {
                    List<Tile> nextRemaining = new List<Tile>(remaining);
                    Tile third = nextRemaining[thirdSame];
                    nextRemaining.RemoveAt(thirdSame);
                    Tile second = nextRemaining[secondSame];
                    nextRemaining.RemoveAt(secondSame);
                    Tile firstTile = nextRemaining[0];
                    nextRemaining.RemoveAt(0);
                    groups.Add(new List<Tile> { firstTile, second, third });
                    if (ArrangeGroups(nextRemaining, groupsNeeded - 1, groups))
                    {
                        return true;
                    }
                    groups.RemoveAt(groups.Count - 1);
                }
            }

            Tile secondInSequence = new Tile(first.GetTileType(), first.GetNumber() + 1);
            Tile thirdInSequence = new Tile(first.GetTileType(), first.GetNumber() + 2);
            int secondIndex = FindMatchingTileIndex(remaining, secondInSequence, 1);
            int thirdIndex = FindMatchingTileIndex(remaining, thirdInSequence, 1);
            if (secondIndex >= 0 && thirdIndex >= 0)
            {
                List<Tile> nextRemaining = new List<Tile>(remaining);
                int high = secondIndex > thirdIndex ? secondIndex : thirdIndex;
                int low = secondIndex < thirdIndex ? secondIndex : thirdIndex;
                Tile third = nextRemaining[high];
                nextRemaining.RemoveAt(high);
                Tile second = nextRemaining[low];
                nextRemaining.RemoveAt(low);
                Tile firstTile = nextRemaining[0];
                nextRemaining.RemoveAt(0);
                groups.Add(new List<Tile> { firstTile, second, third });
                if (ArrangeGroups(nextRemaining, groupsNeeded - 1, groups))
                {
                    return true;
                }
                groups.RemoveAt(groups.Count - 1);
            }

            return false;
        }

        private static int FindMatchingTileIndex(List<Tile> tiles, Tile tile, int startIndex = 0)
        {
            for (int i = startIndex; i < tiles.Count; i++)
            {
                if (tiles[i].Equals(tile))
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
