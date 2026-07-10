using System.Collections.Generic;

namespace SichuanMahjong.Core.Algorithm
{
    /// <summary>
    /// Hu lookup tables (majiang_clien_*.txt). NOTE: exactly like the Java
    /// original, this game never loads these tables — AIUtil.Calc probes them,
    /// finds nothing, and falls through to the AI probability tables. They are
    /// ported so the behavior stays identical if table files are supplied.
    /// </summary>
    public static class HuTable
    {
        public static readonly Dictionary<long, List<HuTableInfo>> table = new Dictionary<long, List<HuTableInfo>>();

        public static void Load(IEnumerable<string> lines)
        {
            table.Clear();
            TableLoader.LoadHuTable(table, 9, lines);
        }
    }

    public static class HuTableFeng
    {
        public static readonly Dictionary<long, List<HuTableInfo>> table = new Dictionary<long, List<HuTableInfo>>();

        public static void Load(IEnumerable<string> lines)
        {
            table.Clear();
            TableLoader.LoadHuTable(table, 4, lines);
        }
    }

    public static class HuTableJian
    {
        public static readonly Dictionary<long, List<HuTableInfo>> table = new Dictionary<long, List<HuTableInfo>>();

        public static void Load(IEnumerable<string> lines)
        {
            table.Clear();
            TableLoader.LoadHuTable(table, 3, lines);
        }
    }
}
