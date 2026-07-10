namespace SichuanMahjong.Core.Model
{
    /// <summary>
    /// Declaration order matters: sorting compares the enum value, matching
    /// Java's TileTypeEnum.compareTo (B &lt; C &lt; D).
    /// </summary>
    public enum TileTypeEnum
    {
        B, // Bamboo 条
        C, // Character 万
        D  // Dot 筒
    }

    public static class TileTypeEnumExtensions
    {
        public static string GetEnglish(this TileTypeEnum type)
        {
            switch (type)
            {
                case TileTypeEnum.B: return "Bamboo";
                case TileTypeEnum.C: return "Character";
                default: return "Dot";
            }
        }

        public static string GetChinese(this TileTypeEnum type)
        {
            switch (type)
            {
                case TileTypeEnum.B: return "条";
                case TileTypeEnum.C: return "万";
                default: return "筒";
            }
        }
    }
}
