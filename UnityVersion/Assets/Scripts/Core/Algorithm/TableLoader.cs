using System.Collections.Generic;
using System.Globalization;

namespace SichuanMahjong.Core.Algorithm
{
    /// <summary>
    /// Runtime loading shared by the AI and Hu lookup tables. This replaces
    /// the load(...) halves of the Java AICommon/HuCommon classes; the
    /// offline table *generation* tooling (gen(), SQL export) was not ported
    /// because the game only ever consumes the pre-generated table files.
    /// </summary>
    public static class TableLoader
    {
        /// <summary>Parses "key jiang p ..." lines into an AI probability table.</summary>
        public static void LoadAiTable(Dictionary<long, List<AITableInfo>> table, IEnumerable<string> lines)
        {
            foreach (string str in lines)
            {
                if (string.IsNullOrEmpty(str))
                {
                    continue;
                }
                string[] strs = str.Split(' ');
                long key = long.Parse(strs[0], CultureInfo.InvariantCulture);
                int jiang = int.Parse(strs[1], CultureInfo.InvariantCulture);
                double p = double.Parse(strs[2], CultureInfo.InvariantCulture);

                if (!table.TryGetValue(key, out List<AITableInfo> aiTableInfos))
                {
                    aiTableInfos = new List<AITableInfo>();
                    table[key] = aiTableInfos;
                }

                aiTableInfos.Add(new AITableInfo { jiang = jiang != 0, p = p });
            }
        }

        /// <summary>Parses "key gui jiang hu ..." lines into a hu-check table.</summary>
        public static void LoadHuTable(Dictionary<long, List<HuTableInfo>> table, int n, IEnumerable<string> lines)
        {
            foreach (string str in lines)
            {
                if (string.IsNullOrEmpty(str))
                {
                    continue;
                }
                string[] strs = str.Split(' ');
                long key = long.Parse(strs[0], CultureInfo.InvariantCulture);
                int gui = int.Parse(strs[1], CultureInfo.InvariantCulture);
                int jiang = int.Parse(strs[2], CultureInfo.InvariantCulture);
                int hu = int.Parse(strs[3], CultureInfo.InvariantCulture);

                if (!table.TryGetValue(key, out List<HuTableInfo> huTableInfos))
                {
                    huTableInfos = new List<HuTableInfo>();
                    table[key] = huTableInfos;
                }

                byte[] num = new byte[n];
                long tmp = hu;
                for (int i = 0; i < n; i++)
                {
                    num[n - 1 - i] = (byte)(tmp % 10);
                    tmp /= 10;
                }
                huTableInfos.Add(new HuTableInfo
                {
                    needGui = (byte)gui,
                    jiang = jiang != 0,
                    hupai = hu == -1 ? null : num
                });
            }
        }
    }
}
