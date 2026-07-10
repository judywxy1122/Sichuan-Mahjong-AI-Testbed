using System.Collections.Generic;

namespace SichuanMahjong.Core.Algorithm
{
    /// <summary>Probability table for the 1万..9万 shaped suits (majiang_ai_normal.txt).</summary>
    public static class AITable
    {
        public static readonly Dictionary<long, List<AITableInfo>> table = new Dictionary<long, List<AITableInfo>>();

        public static void Load(IEnumerable<string> lines)
        {
            table.Clear();
            TableLoader.LoadAiTable(table, lines);
        }
    }

    /// <summary>Probability table for the four winds (majiang_ai_feng.txt).</summary>
    public static class AITableFeng
    {
        public static readonly Dictionary<long, List<AITableInfo>> table = new Dictionary<long, List<AITableInfo>>();

        public static void Load(IEnumerable<string> lines)
        {
            table.Clear();
            TableLoader.LoadAiTable(table, lines);
        }
    }

    /// <summary>Probability table for 中/发/白 (majiang_ai_jian.txt).</summary>
    public static class AITableJian
    {
        public static readonly Dictionary<long, List<AITableInfo>> table = new Dictionary<long, List<AITableInfo>>();

        public static void Load(IEnumerable<string> lines)
        {
            table.Clear();
            TableLoader.LoadAiTable(table, lines);
        }
    }
}
