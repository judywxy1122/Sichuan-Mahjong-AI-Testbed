using System.Collections.Generic;
using System.Linq;

namespace SichuanMahjong.Core.Model
{
    public class Tiles
    {
        protected readonly List<Tile> tiles;

        public Tiles()
        {
            tiles = new List<Tile>();
            Sort();
        }

        public Tiles(List<Tile> tiles)
        {
            this.tiles = tiles;
            Sort();
        }

        public virtual void Add(Tile tile)
        {
            tiles.Add(tile);
            UpdatePosition();
        }

        public virtual void Remove(Tile tile)
        {
            for (int i = 0; i < tiles.Count; i++)
            {
                if (tiles[i].Equals(tile))
                {
                    tiles.RemoveAt(i);
                    break;
                }
            }
            Sort();
        }

        public void Sort()
        {
            tiles.Sort((tile1, tile2) =>
            {
                if (tile1.GetTileType() == tile2.GetTileType())
                {
                    return tile1.GetNumber() - tile2.GetNumber();
                }
                return tile1.GetTileType().CompareTo(tile2.GetTileType());
            });
            UpdatePosition();
        }

        public virtual void UpdatePosition()
        {
        }

        public Tile GetLast()
        {
            return tiles[tiles.Count - 1];
        }

        public void RemoveLast()
        {
            tiles.RemoveAt(tiles.Count - 1);
        }

        public List<Tile> ToList()
        {
            if (tiles == null)
            {
                return new List<Tile>();
            }
            return new List<Tile>(tiles);
        }

        /// <summary>
        /// Content hash over the tile list, mirroring Java's List.hashCode()
        /// contract (order-dependent). GamePanel's play key relies on this.
        /// </summary>
        public override int GetHashCode()
        {
            int hash = 1;
            foreach (Tile tile in tiles)
            {
                hash = hash * 31 + (tile == null ? 0 : tile.GetHashCode());
            }
            return hash;
        }

        public override bool Equals(object o)
        {
            if (ReferenceEquals(this, o)) return true;
            if (o == null || GetType() != o.GetType()) return false;
            Tiles other = (Tiles)o;
            if (tiles == null || other.tiles == null) return tiles == other.tiles;
            return tiles.SequenceEqual(other.tiles);
        }

        public override string ToString()
        {
            return "Tiles{tiles=[" + string.Join(", ", tiles) + "]}";
        }
    }
}
