using System.Collections.Generic;
using SichuanMahjong.Core.Model;

namespace SichuanMahjong.Core.Validation
{
    public class PlayerStatusChecker
    {
        private readonly Player player;
        private readonly List<Tile> tiles;
        private readonly Dictionary<string, List<Tile>> categorizedTiles;
        private readonly HashSet<Group> pair;
        private readonly HashSet<Group> sequence;
        private readonly HashSet<Group> triple;
        private readonly HashSet<Group> pung;
        private readonly HashSet<Group> kong;
        private readonly PungKongChecker pungKongChecker;

        public PlayerStatusChecker(Player player, Tile newTile)
        {
            pair = new HashSet<Group>();
            sequence = new HashSet<Group>();
            triple = new HashSet<Group>();
            pung = new HashSet<Group>();
            kong = new HashSet<Group>();
            categorizedTiles = new Dictionary<string, List<Tile>>();
            this.player = player;
            pungKongChecker = new PungKongChecker(player, newTile);

            List<Tile> handTiles = player.GetHand().ToList();
            handTiles.Add(newTile);
            foreach (Group group in player.GetHand().GetPung())
            {
                pung.Add(group);
            }
            foreach (Group group in player.GetHand().GetKong())
            {
                kong.Add(group);
            }
            tiles = new List<Tile>(handTiles);
            SetTiles();
        }

        private void SetTiles()
        {
            tiles.Sort((a, b) =>
            {
                int byType = a.GetTileType().CompareTo(b.GetTileType());
                return byType != 0 ? byType : a.GetNumber().CompareTo(b.GetNumber());
            });
            foreach (Tile tile in tiles)
            {
                string key = tile.GetTileType().GetEnglish();
                if (!categorizedTiles.TryGetValue(key, out List<Tile> list))
                {
                    list = new List<Tile>();
                    categorizedTiles[key] = list;
                }
                list.Add(tile);
            }
            foreach (List<Tile> tileByCategory in categorizedTiles.Values)
            {
                GenerateSets(tileByCategory);
            }
            UpdateStatus();
        }

        public void UpdateStatus()
        {
            player.ClearStatus();
            if (CheckHu())
            {
                player.SetHuStatus();
                CoreEnv.Println("setted hu status to " + player.GetName());
            }
            if (pungKongChecker.CanPung())
            {
                player.SetPungStatus();
                CoreEnv.Println("setted pung status to " + player.GetName());
            }
            if (pungKongChecker.CanNormalKong())
            {
                player.SetNormalKongStatus();
                CoreEnv.Println("setted Normal Kong status to " + player.GetName());
            }
            if (pungKongChecker.CanHiddenKong())
            {
                player.SetHiddenKongStatus();
                CoreEnv.Println("setted Hidden Kong status to " + player.GetName());
            }
            if (pungKongChecker.CanAddKong())
            {
                player.SetAddKongStatus();
                CoreEnv.Println("setted Add Kong status to " + player.GetName());
            }
        }

        private bool CheckHu()
        {
            if (!IsMissingOneSuit())
            {
                return false;
            }
            HuFitter huFitter = new HuFitter(pair, sequence, triple, pung, kong);
            HashSet<List<Group>> candidates = huFitter.FitAllHu();
            foreach (List<Group> groups in candidates)
            {
                if (CanHuFromHand(tiles, groups))
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsMissingOneSuit()
        {
            HashSet<TileTypeEnum> suits = new HashSet<TileTypeEnum>();
            foreach (Tile tile in tiles)
            {
                suits.Add(tile.GetTileType());
            }
            foreach (Group group in pung)
            {
                AddGroupSuits(suits, group);
            }
            foreach (Group group in kong)
            {
                AddGroupSuits(suits, group);
            }
            return suits.Count <= 2;
        }

        private static void AddGroupSuits(HashSet<TileTypeEnum> suits, Group group)
        {
            foreach (Tile tile in group.ToList())
            {
                suits.Add(tile.GetTileType());
            }
        }

        private void GenerateSets(List<Tile> suitTiles)
        {
            if (suitTiles.Count < 2)
            {
                return;
            }
            if (suitTiles.Count == 2)
            {
                SetGroups(suitTiles[0], suitTiles[1]);
            }
            for (int i = 0; i < suitTiles.Count - 2; i++)
            {
                for (int j = i + 1; j < suitTiles.Count - 1; j++)
                {
                    for (int k = j + 1; k < suitTiles.Count; k++)
                    {
                        SetGroups(suitTiles[i], suitTiles[j], suitTiles[k]);
                    }
                }
            }
        }

        private void SetGroups(params Tile[] tileArgs)
        {
            List<Tile> tilesList = new List<Tile>(tileArgs);
            for (int i = 0; i < tilesList.Count; i++)
            {
                for (int j = i + 1; j < tilesList.Count; j++)
                {
                    if (tilesList[i].GetNumber() == tilesList[j].GetNumber())
                    {
                        pair.Add(new Group(new List<Tile> { tilesList[i], tilesList[j] }, GroupEnum.PAIR, 1));
                    }
                }
            }
            for (int i = 0; i < tilesList.Count - 2; i++)
            {
                if (tilesList[i].GetNumber() == tilesList[i + 1].GetNumber()
                    && tilesList[i + 1].GetNumber() == tilesList[i + 2].GetNumber())
                {
                    triple.Add(new Group(new List<Tile> { tilesList[i], tilesList[i + 1], tilesList[i + 2] },
                        GroupEnum.TRIPLE, 1));
                }
                else if (tilesList[i + 1].GetNumber() == tilesList[i].GetNumber() + 1
                         && tilesList[i + 2].GetNumber() == tilesList[i + 1].GetNumber() + 1)
                {
                    sequence.Add(new Group(new List<Tile> { tilesList[i], tilesList[i + 1], tilesList[i + 2] },
                        GroupEnum.SEQUENCE, 1));
                }
            }
        }

        private static bool CanHuFromHand(List<Tile> hand, IEnumerable<Group> groups)
        {
            List<Tile> handCopy = new List<Tile>(hand);
            foreach (Group group in groups)
            {
                foreach (Tile tile in group.ToList())
                {
                    if (!RemoveOneByValue(handCopy, tile))
                    {
                        return false;
                    }
                }
            }
            return handCopy.Count == 0;
        }

        private static bool RemoveOneByValue(List<Tile> list, Tile tile)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Equals(tile))
                {
                    list.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }
    }
}
