using System.Collections.Generic;
using System.Linq;

namespace SichuanMahjong.Core.Model
{
    public class HandTiles : Tiles
    {
        private Tile newTile;
        private readonly List<Group> kong;
        private readonly List<Group> pung;

        public HandTiles(List<Tile> tiles) : base(tiles)
        {
            kong = new List<Group>();
            pung = new List<Group>();
        }

        public override void Add(Tile tile)
        {
            if (newTile != null)
            {
                base.Add(newTile);
            }
            newTile = tile;
            Sort();
        }

        public override void Remove(Tile tile)
        {
            RemoveForDiscard(tile);
        }

        public Tile RemoveForDiscard(Tile tile)
        {
            if (tile == null)
            {
                return null;
            }
            if (ReferenceEquals(tile, newTile))
            {
                Tile removed = newTile;
                newTile = null;
                Sort();
                return removed;
            }
            for (int i = 0; i < tiles.Count; i++)
            {
                Tile current = tiles[i];
                if (current.Equals(tile))
                {
                    tiles.RemoveAt(i);
                    MergeNewWithHand();
                    Sort();
                    return current;
                }
            }
            if (newTile != null && newTile.Equals(tile))
            {
                Tile removed = newTile;
                newTile = null;
                Sort();
                return removed;
            }
            return null;
        }

        private void MergeNewWithHand()
        {
            if (newTile != null)
            {
                base.Add(newTile);
                newTile = null;
            }
        }

        public void AddPung(Tile tile)
        {
            List<Tile> pungTiles = new List<Tile>
            {
                new Tile(tile.GetTileType(), tile.GetNumber()),
                new Tile(tile.GetTileType(), tile.GetNumber()),
                new Tile(tile.GetTileType(), tile.GetNumber())
            };
            pung.Add(new Group(pungTiles, GroupEnum.PUNG, 0));
            RemoveOneByValue(tile);
            RemoveOneByValue(tile);
            Sort();
        }

        public void AddNormalKong(Tile tile)
        {
            List<Tile> kongTiles = GenerateNewKong(tile);
            kong.Add(new Group(kongTiles, GroupEnum.NORMAL_KONG, 0));
            tiles.RemoveAll(t => t.Equals(tile));
            Sort();
        }

        public void AddAddKong()
        {
            for (int i = 0; i < pung.Count; i++)
            {
                if (newTile.Equals(pung[i].ToList()[0]))
                {
                    pung.RemoveAt(i);
                    break;
                }
            }
            List<Tile> kongTiles = GenerateNewKong(newTile);
            kong.Add(new Group(kongTiles, GroupEnum.ADD_KONG, 0));
            newTile = null;
            Sort();
        }

        public void AddHiddenKong()
        {
            foreach (Tile tile in tiles)
            {
                int frequency = tiles.Count(t => t.Equals(tile));
                if (frequency == 4)
                {
                    List<Tile> kongTiles = GenerateNewKong(tile);
                    kong.Add(new Group(kongTiles, GroupEnum.HIDDEN_KONG, 0));
                    tiles.RemoveAll(t => t.Equals(tile));
                    Sort();
                    break;
                }
                if (frequency == 3 && tile.Equals(newTile))
                {
                    List<Tile> kongTiles = GenerateNewKong(tile);
                    kong.Add(new Group(kongTiles, GroupEnum.HIDDEN_KONG, 0));
                    tiles.RemoveAll(t => t.Equals(tile));
                    newTile = null;
                    Sort();
                    break;
                }
            }
        }

        private static List<Tile> GenerateNewKong(Tile tile)
        {
            return new List<Tile>
            {
                new Tile(tile.GetTileType(), tile.GetNumber()),
                new Tile(tile.GetTileType(), tile.GetNumber()),
                new Tile(tile.GetTileType(), tile.GetNumber()),
                new Tile(tile.GetTileType(), tile.GetNumber())
            };
        }

        /// <summary>
        /// Java's List.remove(Object) removes the first element equal to the
        /// argument; List&lt;T&gt;.Remove uses reference semantics only when
        /// Equals is not overridden, so spell out the by-value removal.
        /// </summary>
        private void RemoveOneByValue(Tile tile)
        {
            for (int i = 0; i < tiles.Count; i++)
            {
                if (tiles[i].Equals(tile))
                {
                    tiles.RemoveAt(i);
                    return;
                }
            }
        }

        public List<Group> GetKong()
        {
            return kong;
        }

        public List<Group> GetPung()
        {
            return pung;
        }

        public List<Group> GetPungKong()
        {
            List<Group> pungKong = new List<Group>();
            pungKong.AddRange(pung);
            pungKong.AddRange(kong);
            return pungKong;
        }

        public Tile GetNewTile()
        {
            return newTile;
        }

        public override string ToString()
        {
            return "HandTiles{tiles=[" + string.Join(", ", tiles) + "], newTile=" + newTile
                   + ", pung=[" + string.Join(", ", pung) + "], kong=[" + string.Join(", ", kong) + "]}";
        }
    }
}
