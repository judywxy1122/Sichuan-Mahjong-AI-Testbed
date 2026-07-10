namespace SichuanMahjong.Core.Model
{
    public class Tile : Entity
    {
        private int index;
        private readonly TileTypeEnum type;
        private readonly int number;

        public Tile(TileTypeEnum type, int number) : base(0, 0, 0, 0)
        {
            this.type = type;
            this.number = number;
        }

        public TileTypeEnum GetTileType()
        {
            return type;
        }

        public int GetNumber()
        {
            return number;
        }

        public int GetIndex()
        {
            return index;
        }

        public void SetIndex(int index)
        {
            this.index = index;
        }

        public override string ToString()
        {
            return type + number.ToString();
        }

        public override bool Equals(object o)
        {
            if (ReferenceEquals(this, o)) return true;
            if (o == null || GetType() != o.GetType()) return false;
            Tile tile = (Tile)o;
            return number == tile.number && type == tile.type;
        }

        public override int GetHashCode()
        {
            return (int)type * 31 + number;
        }
    }
}
