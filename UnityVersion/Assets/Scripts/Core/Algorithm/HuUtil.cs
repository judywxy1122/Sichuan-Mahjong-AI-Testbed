using System.Collections.Generic;

namespace SichuanMahjong.Core.Algorithm
{
    /// <summary>
    /// C# port of com.github.esrrhs.majiang_algorithm.HuUtil (runtime paths).
    /// </summary>
    public static class HuUtil
    {
        public static bool IsHu(List<int> input, int guiCard)
        {
            List<int> cards = NewCounts();
            foreach (int c in input)
            {
                cards[c - 1]++;
            }
            int guiNum = cards[guiCard - 1];
            cards[guiCard - 1] = 0;

            return IsHuCard(cards, guiNum);
        }

        public static bool IsHuExtra(List<int> input, List<int> guiCard, int extra)
        {
            List<int> cards = NewCounts();
            foreach (int c in input)
            {
                cards[c - 1]++;
            }

            int guiNum = 0;
            foreach (int gui in guiCard)
            {
                guiNum += cards[gui - 1];
                cards[gui - 1] = 0;
            }

            if (extra != 0)
            {
                cards[extra - 1]++;
            }

            return IsHuCard(cards, guiNum);
        }

        public static bool IsHuCard(List<int> cards, int guiNum)
        {
            long wan_key = 0, tong_key = 0, tiao_key = 0, feng_key = 0, jian_key = 0;
            BuildKeys(cards, ref wan_key, ref tong_key, ref tiao_key, ref feng_key, ref jian_key);

            List<List<HuTableInfo>> tmp = new List<List<HuTableInfo>>();
            if (wan_key != 0)
            {
                tmp.Add(Get(HuTable.table, wan_key));
            }
            if (tong_key != 0)
            {
                tmp.Add(Get(HuTable.table, tong_key));
            }
            if (tiao_key != 0)
            {
                tmp.Add(Get(HuTable.table, tiao_key));
            }
            if (feng_key != 0)
            {
                tmp.Add(Get(HuTableFeng.table, feng_key));
            }
            if (jian_key != 0)
            {
                tmp.Add(Get(HuTableJian.table, jian_key));
            }

            List<List<HuTableInfo>> tmp1 = new List<List<HuTableInfo>>();
            foreach (List<HuTableInfo> huTableInfos in tmp)
            {
                if (huTableInfos == null)
                {
                    return false;
                }
                List<HuTableInfo> tmp2 = new List<HuTableInfo>();
                foreach (HuTableInfo huTableInfo in huTableInfos)
                {
                    if (huTableInfo.hupai == null && huTableInfo.needGui <= guiNum)
                    {
                        tmp2.Add(huTableInfo);
                    }
                }
                if (tmp2.Count == 0)
                {
                    return false;
                }
                tmp1.Add(tmp2);
            }

            return IsHuTableInfo(tmp1, 0, guiNum, false);
        }

        private static bool IsHuTableInfo(List<List<HuTableInfo>> tmp, int index, int guiNum, bool jiang)
        {
            if (index >= tmp.Count)
            {
                return (guiNum % 3 == 0 && jiang) || (guiNum % 3 == 2 && !jiang);
            }
            foreach (HuTableInfo huTableInfo in tmp[index])
            {
                if (jiang)
                {
                    if (huTableInfo.hupai == null && huTableInfo.needGui <= guiNum && !huTableInfo.jiang)
                    {
                        if (IsHuTableInfo(tmp, index + 1, guiNum - huTableInfo.needGui, true))
                        {
                            return true;
                        }
                    }
                }
                else
                {
                    if (huTableInfo.hupai == null && huTableInfo.needGui <= guiNum)
                    {
                        if (IsHuTableInfo(tmp, index + 1, guiNum - huTableInfo.needGui, huTableInfo.jiang))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public static List<int> IsTing(List<int> input, int guiCard)
        {
            List<int> cards = NewCounts();
            foreach (int c in input)
            {
                cards[c - 1]++;
            }
            int guiNum = cards[guiCard - 1];
            cards[guiCard - 1] = 0;

            return IsTingCard(cards, guiNum);
        }

        public static List<int> IsTingExtra(List<int> input, List<int> guiCard)
        {
            List<int> cards = NewCounts();
            foreach (int c in input)
            {
                cards[c - 1]++;
            }

            int guiNum = 0;
            foreach (int gui in guiCard)
            {
                guiNum += cards[gui - 1];
                cards[gui - 1] = 0;
            }

            return IsTingCard(cards, guiNum);
        }

        public static List<int> IsTingCard(List<int> cards, int guiNum)
        {
            long wan_key = 0, tong_key = 0, tiao_key = 0, feng_key = 0, jian_key = 0;
            BuildKeys(cards, ref wan_key, ref tong_key, ref tiao_key, ref feng_key, ref jian_key);

            List<int> tmpType = new List<int>();
            List<List<HuTableInfo>> tmpTing = new List<List<HuTableInfo>>();
            List<List<HuTableInfo>> tmp = new List<List<HuTableInfo>>();

            List<HuTableInfo> wanHuTableInfo = Get(HuTable.table, wan_key);
            if (wanHuTableInfo == null)
            {
                return new List<int>();
            }
            tmpTing.Add(wanHuTableInfo);
            if (wan_key != 0)
            {
                tmpType.Add(MaJiangDef.TYPE_WAN);
                tmp.Add(wanHuTableInfo);
            }
            List<HuTableInfo> tongHuTableInfo = Get(HuTable.table, tong_key);
            if (tongHuTableInfo == null)
            {
                return new List<int>();
            }
            tmpTing.Add(tongHuTableInfo);
            if (tong_key != 0)
            {
                tmpType.Add(MaJiangDef.TYPE_TONG);
                tmp.Add(tongHuTableInfo);
            }
            List<HuTableInfo> tiaoHuTableInfo = Get(HuTable.table, tiao_key);
            if (tiaoHuTableInfo == null)
            {
                return new List<int>();
            }
            tmpTing.Add(tiaoHuTableInfo);
            if (tiao_key != 0)
            {
                tmpType.Add(MaJiangDef.TYPE_TIAO);
                tmp.Add(tiaoHuTableInfo);
            }
            List<HuTableInfo> fengHuTableInfo = Get(HuTableFeng.table, feng_key);
            if (fengHuTableInfo == null)
            {
                return new List<int>();
            }
            tmpTing.Add(fengHuTableInfo);
            if (feng_key != 0)
            {
                tmpType.Add(MaJiangDef.TYPE_FENG);
                tmp.Add(fengHuTableInfo);
            }
            List<HuTableInfo> jianHuTableInfo = Get(HuTableJian.table, jian_key);
            if (jianHuTableInfo == null)
            {
                return new List<int>();
            }
            tmpTing.Add(jianHuTableInfo);
            if (jian_key != 0)
            {
                tmpType.Add(MaJiangDef.TYPE_JIAN);
                tmp.Add(jianHuTableInfo);
            }

            List<int> ret = new List<int>();
            for (int type = MaJiangDef.TYPE_WAN; type <= MaJiangDef.TYPE_JIAN; type++)
            {
                List<HuTableInfo> huTableInfos = tmpTing[type - 1];
                int[] cache = new int[9];
                foreach (HuTableInfo huTableInfo in huTableInfos)
                {
                    if (huTableInfo.hupai != null && huTableInfo.needGui <= guiNum)
                    {
                        bool cached = true;
                        for (int j = 0; j < huTableInfo.hupai.Length; j++)
                        {
                            if (huTableInfo.hupai[j] > 0 && cache[j] == 0)
                            {
                                cached = false;
                                break;
                            }
                        }

                        if (!cached && IsTingHuTableInfo(tmpType, tmp, 0, guiNum - huTableInfo.needGui,
                                huTableInfo.jiang, type))
                        {
                            for (int j = 0; j < huTableInfo.hupai.Length; j++)
                            {
                                if (huTableInfo.hupai[j] > 0)
                                {
                                    if (cache[j] == 0)
                                    {
                                        ret.Add(MaJiangDef.ToCard(type, j));
                                    }
                                    cache[j]++;
                                }
                            }
                        }
                    }
                }
            }
            return ret;
        }

        private static bool IsTingHuTableInfo(List<int> tmpType, List<List<HuTableInfo>> tmp, int index, int guiNum,
                                              bool jiang, int tingType)
        {
            if (index >= tmp.Count)
            {
                return guiNum == 0 && jiang;
            }
            if (tmpType[index] == tingType)
            {
                return IsTingHuTableInfo(tmpType, tmp, index + 1, guiNum, jiang, tingType);
            }
            foreach (HuTableInfo huTableInfo in tmp[index])
            {
                if (huTableInfo.hupai == null && huTableInfo.needGui <= guiNum)
                {
                    if (jiang)
                    {
                        if (!huTableInfo.jiang)
                        {
                            if (IsTingHuTableInfo(tmpType, tmp, index + 1, guiNum - huTableInfo.needGui, true, tingType))
                            {
                                return true;
                            }
                        }
                    }
                    else
                    {
                        if (IsTingHuTableInfo(tmpType, tmp, index + 1, guiNum - huTableInfo.needGui,
                                huTableInfo.jiang, tingType))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private static List<int> NewCounts()
        {
            List<int> cards = new List<int>(MaJiangDef.MAX_NUM);
            for (int i = 0; i < MaJiangDef.MAX_NUM; i++)
            {
                cards.Add(0);
            }
            return cards;
        }

        internal static void BuildKeys(List<int> cards, ref long wan_key, ref long tong_key, ref long tiao_key,
                                       ref long feng_key, ref long jian_key)
        {
            for (int i = MaJiangDef.WAN1; i <= MaJiangDef.WAN9; i++)
            {
                wan_key = wan_key * 10 + cards[i - 1];
            }
            for (int i = MaJiangDef.TONG1; i <= MaJiangDef.TONG9; i++)
            {
                tong_key = tong_key * 10 + cards[i - 1];
            }
            for (int i = MaJiangDef.TIAO1; i <= MaJiangDef.TIAO9; i++)
            {
                tiao_key = tiao_key * 10 + cards[i - 1];
            }
            for (int i = MaJiangDef.FENG_DONG; i <= MaJiangDef.FENG_BEI; i++)
            {
                feng_key = feng_key * 10 + cards[i - 1];
            }
            for (int i = MaJiangDef.JIAN_ZHONG; i <= MaJiangDef.JIAN_BAI; i++)
            {
                jian_key = jian_key * 10 + cards[i - 1];
            }
        }

        internal static TValue Get<TValue>(Dictionary<long, TValue> table, long key) where TValue : class
        {
            return table.TryGetValue(key, out TValue value) ? value : null;
        }
    }
}
