using System.Collections.Generic;
using System.Linq;

namespace SichuanMahjong.Core.Algorithm
{
    /// <summary>
    /// C# port of com.github.esrrhs.majiang_algorithm.AIUtil (runtime paths).
    /// </summary>
    public static class AIUtil
    {
        public static double Calc(List<int> input, List<int> guiCard)
        {
            List<int> cards = new List<int>(MaJiangDef.MAX_NUM);
            for (int i = 0; i < MaJiangDef.MAX_NUM; i++)
            {
                cards.Add(0);
            }
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

            List<int> ting = HuUtil.IsTingCard(cards, guiNum);
            if (ting.Count > 0)
            {
                return ting.Count * 10;
            }

            long wan_key = 0, tong_key = 0, tiao_key = 0, feng_key = 0, jian_key = 0;
            HuUtil.BuildKeys(cards, ref wan_key, ref tong_key, ref tiao_key, ref feng_key, ref jian_key);

            List<List<AITableInfo>> tmp = new List<List<AITableInfo>>
            {
                HuUtil.Get(AITable.table, wan_key),
                HuUtil.Get(AITable.table, tong_key),
                HuUtil.Get(AITable.table, tiao_key),
                HuUtil.Get(AITableFeng.table, feng_key),
                HuUtil.Get(AITableJian.table, jian_key)
            };

            List<double> ret = new List<double>();
            CalcAITableInfo(ret, tmp, 0, false, 0.0);

            return ret.Max();
        }

        private static void CalcAITableInfo(List<double> ret, List<List<AITableInfo>> tmp, int index, bool jiang,
                                            double cur)
        {
            if (index >= tmp.Count)
            {
                if (jiang)
                {
                    ret.Add(cur);
                }
                return;
            }
            foreach (AITableInfo aiTableInfo in tmp[index])
            {
                if (jiang)
                {
                    if (!aiTableInfo.jiang)
                    {
                        CalcAITableInfo(ret, tmp, index + 1, true, cur + aiTableInfo.p);
                    }
                }
                else
                {
                    CalcAITableInfo(ret, tmp, index + 1, aiTableInfo.jiang, cur + aiTableInfo.p);
                }
            }
        }

        public static int OutAI(List<int> input, List<int> guiCard)
        {
            int ret = 0;
            // Java uses Double.MIN_VALUE (smallest positive double), which is
            // C#'s double.Epsilon — NOT double.MinValue.
            double max = double.Epsilon;
            int[] cache = new int[MaJiangDef.MAX_NUM + 1];
            foreach (int c in input)
            {
                if (cache[c] == 0)
                {
                    if (!guiCard.Contains(c))
                    {
                        List<int> tmp = new List<int>(input);
                        tmp.Remove(c);
                        double score = Calc(tmp, guiCard);
                        if (score > max)
                        {
                            max = score;
                            ret = c;
                        }
                    }
                }
                cache[c] = 1;
            }
            return ret;
        }

        public static bool ChiAI(List<int> input, List<int> guiCard, int card, int card1, int card2)
        {
            if (guiCard.Contains(card) || guiCard.Contains(card1) || guiCard.Contains(card2))
            {
                return false;
            }

            if (input.Count(c => c == card1) < 1 || input.Count(c => c == card2) < 1)
            {
                return false;
            }

            double score = Calc(input, guiCard);

            List<int> tmp = new List<int>(input);
            tmp.Remove(card1);
            tmp.Remove(card2);
            double scoreNew = Calc(tmp, guiCard);

            return scoreNew >= score;
        }

        public static List<int> ChiAI(List<int> input, List<int> guiCard, int card)
        {
            List<int> ret = new List<int>();
            if (guiCard.Contains(card))
            {
                return ret;
            }

            double score = Calc(input, guiCard);
            double scoreNewMax = 0;

            int card1 = 0;
            int card2 = 0;

            if (input.Count(c => c == card - 2) > 0 && input.Count(c => c == card - 1) > 0
                && MaJiangDef.Type(card) == MaJiangDef.Type(card - 2)
                && MaJiangDef.Type(card) == MaJiangDef.Type(card - 1))
            {
                List<int> tmp = new List<int>(input);
                tmp.Remove(card - 2);
                tmp.Remove(card - 1);
                double scoreNew = Calc(tmp, guiCard);
                if (scoreNew > scoreNewMax)
                {
                    scoreNewMax = scoreNew;
                    card1 = card - 2;
                    card2 = card - 1;
                }
            }

            if (input.Count(c => c == card - 1) > 0 && input.Count(c => c == card + 1) > 0
                && MaJiangDef.Type(card) == MaJiangDef.Type(card - 1)
                && MaJiangDef.Type(card) == MaJiangDef.Type(card + 1))
            {
                List<int> tmp = new List<int>(input);
                tmp.Remove(card - 1);
                tmp.Remove(card + 1);
                double scoreNew = Calc(tmp, guiCard);
                if (scoreNew > scoreNewMax)
                {
                    scoreNewMax = scoreNew;
                    card1 = card - 1;
                    card2 = card + 1;
                }
            }

            if (input.Count(c => c == card + 1) > 0 && input.Count(c => c == card + 2) > 0
                && MaJiangDef.Type(card) == MaJiangDef.Type(card + 1)
                && MaJiangDef.Type(card) == MaJiangDef.Type(card + 2))
            {
                List<int> tmp = new List<int>(input);
                tmp.Remove(card + 1);
                tmp.Remove(card + 2);
                double scoreNew = Calc(tmp, guiCard);
                if (scoreNew > scoreNewMax)
                {
                    scoreNewMax = scoreNew;
                    card1 = card + 1;
                    card2 = card + 2;
                }
            }

            if (scoreNewMax > score)
            {
                ret.Add(card1);
                ret.Add(card2);
            }

            return ret;
        }

        public static bool PengAI(List<int> input, List<int> guiCard, int card, double award)
        {
            if (guiCard.Contains(card))
            {
                return false;
            }

            if (input.Count(c => c == card) < 2)
            {
                return false;
            }

            double score = Calc(input, guiCard);

            List<int> tmp = new List<int>(input);
            tmp.Remove(card);
            tmp.Remove(card);
            double scoreNew = Calc(tmp, guiCard);

            return scoreNew + award >= score;
        }

        public static bool GangAI(List<int> input, List<int> guiCard, int card, double award)
        {
            if (guiCard.Contains(card))
            {
                return false;
            }

            if (input.Count(c => c == card) < 3)
            {
                return false;
            }

            double score = Calc(input, guiCard);

            List<int> tmp = new List<int>(input);
            tmp.Remove(card);
            tmp.Remove(card);
            tmp.Remove(card);
            tmp.Remove(card);
            double scoreNew = Calc(tmp, guiCard);

            return scoreNew + award >= score;
        }
    }
}
