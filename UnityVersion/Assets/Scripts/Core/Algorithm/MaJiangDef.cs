using System.Collections.Generic;

namespace SichuanMahjong.Core.Algorithm
{
    /// <summary>
    /// C# port of com.github.esrrhs.majiang_algorithm.MaJiangDef
    /// (runtime subset of https://github.com/esrrhs/majiang_algorithm).
    /// </summary>
    public static class MaJiangDef
    {
        public const int WAN1 = 1;
        public const int WAN9 = 9;

        public const int TONG1 = 10;
        public const int TONG9 = 18;

        public const int TIAO1 = 19;
        public const int TIAO9 = 27;

        public const int FENG_DONG = 28;
        public const int FENG_BEI = 31;

        public const int JIAN_ZHONG = 32;
        public const int JIAN_BAI = 34;

        public const int HUA_CHUN = 35;
        public const int HUA_JU = 42;

        public const int MAX_NUM = 42;

        public const int TYPE_WAN = 1;
        public const int TYPE_TONG = 2;
        public const int TYPE_TIAO = 3;
        public const int TYPE_FENG = 4;
        public const int TYPE_JIAN = 5;
        public const int TYPE_HUA = 6;

        private static readonly string[] HonorNames =
        {
            "东", "南", "西", "北", "中", "发", "白", "春", "夏", "秋", "冬", "梅", "兰", "竹", "菊"
        };

        public static int ToCard(int type, int index)
        {
            switch (type)
            {
                case TYPE_WAN: return WAN1 + index;
                case TYPE_TONG: return TONG1 + index;
                case TYPE_TIAO: return TIAO1 + index;
                case TYPE_FENG: return FENG_DONG + index;
                case TYPE_JIAN: return JIAN_ZHONG + index;
                case TYPE_HUA: return HUA_CHUN + index;
            }
            return 0;
        }

        public static string CardsToString(IEnumerable<int> cards)
        {
            string ret = "";
            foreach (int c in cards)
            {
                ret += CardToString(c) + ",";
            }
            return ret;
        }

        public static string CardToString(int card)
        {
            if (card >= WAN1 && card <= WAN9)
            {
                return (card - WAN1 + 1) + "万";
            }
            if (card >= TONG1 && card <= TONG9)
            {
                return (card - TONG1 + 1) + "筒";
            }
            if (card >= TIAO1 && card <= TIAO9)
            {
                return (card - TIAO1 + 1) + "条";
            }
            if (card >= FENG_DONG && card <= MAX_NUM)
            {
                return HonorNames[card - FENG_DONG];
            }
            return "错误" + card;
        }

        public static List<int> StringToCards(string str)
        {
            List<int> ret = new List<int>();
            foreach (string s in str.Split(','))
            {
                if (!string.IsNullOrEmpty(s))
                {
                    ret.Add(StringToCard(s));
                }
            }
            return ret;
        }

        public static int StringToCard(string str)
        {
            if (str.Contains("万"))
            {
                return WAN1 - 1 + int.Parse(str.Substring(0, 1));
            }
            if (str.Contains("筒"))
            {
                return TONG1 - 1 + int.Parse(str.Substring(0, 1));
            }
            if (str.Contains("条"))
            {
                return TIAO1 - 1 + int.Parse(str.Substring(0, 1));
            }

            int c = FENG_DONG;
            foreach (string s in HonorNames)
            {
                if (str.Contains(s))
                {
                    return c;
                }
                c++;
            }
            return 0;
        }

        public static int Type(int card)
        {
            if (card >= WAN1 && card <= WAN9) return TYPE_WAN;
            if (card >= TONG1 && card <= TONG9) return TYPE_TONG;
            if (card >= TIAO1 && card <= TIAO9) return TYPE_TIAO;
            if (card >= FENG_DONG && card <= FENG_BEI) return TYPE_FENG;
            if (card >= JIAN_ZHONG && card <= JIAN_BAI) return TYPE_JIAN;
            if (card >= HUA_CHUN && card <= HUA_JU) return TYPE_HUA;
            return 0;
        }
    }
}
