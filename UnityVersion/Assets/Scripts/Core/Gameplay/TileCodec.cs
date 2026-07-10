using SichuanMahjong.Core.Model;

namespace SichuanMahjong.Core.Gameplay
{
    public static class TileCodec
    {
        public static string Code(Tile tile)
        {
            return tile == null ? "" : tile.ToString();
        }

        public static Tile FromCode(string code)
        {
            if (code == null || code.Length < 2)
            {
                return null;
            }
            TileTypeEnum type;
            switch (code.Substring(0, 1))
            {
                case "B": type = TileTypeEnum.B; break;
                case "C": type = TileTypeEnum.C; break;
                case "D": type = TileTypeEnum.D; break;
                default: return null;
            }
            if (!int.TryParse(code.Substring(1), out int number))
            {
                return null;
            }
            if (number < 1 || number > 9)
            {
                return null;
            }
            return new Tile(type, number);
        }

        public static string Display(Tile tile)
        {
            if (tile == null)
            {
                return "";
            }
            string[] chineseNumbers = { "", "一", "二", "三", "四", "五", "六", "七", "八", "九" };
            if (tile.GetTileType() == TileTypeEnum.B && tile.GetNumber() == 1)
            {
                return "幺鸡";
            }
            return chineseNumbers[tile.GetNumber()] + tile.GetTileType().GetChinese();
        }
    }
}
