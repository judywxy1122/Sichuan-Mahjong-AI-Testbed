using System.Collections.Generic;
using SichuanMahjong.Core.Model;

namespace SichuanMahjong.Core.Validation
{
    public class HuFitter
    {
        private readonly List<Group> allGroups;
        private readonly List<Group> allGroupsOf3;
        private readonly HashSet<Group> pair;
        private readonly HashSet<Group> sequence;
        private readonly HashSet<Group> sequenceDup;
        private readonly HashSet<Group> triple;
        private readonly HashSet<Group> pung;
        private readonly HashSet<Group> kong;

        public HuFitter(HashSet<Group> pair, HashSet<Group> sequence, HashSet<Group> triple,
                        HashSet<Group> pung, HashSet<Group> kong)
        {
            this.pair = pair;
            this.sequence = sequence;
            this.triple = triple;
            this.pung = pung;
            this.kong = kong;

            sequenceDup = new HashSet<Group>();
            List<Group> sequenceCopy = new List<Group>(this.sequence);
            foreach (Group group in sequenceCopy)
            {
                sequenceDup.Add(group.GetDup());
            }
            allGroups = new List<Group>();
            allGroups.AddRange(this.sequence);
            allGroups.AddRange(this.triple);
            allGroups.AddRange(this.pair);
            allGroups.AddRange(this.kong);
            allGroupsOf3 = new List<Group>();
            allGroupsOf3.AddRange(this.sequence);
            allGroupsOf3.AddRange(sequenceDup);
            allGroupsOf3.AddRange(this.triple);
        }

        public HashSet<List<Group>> FitAllHu()
        {
            HashSet<List<Group>> result = new HashSet<List<Group>>(new GroupListComparer());
            foreach (List<Group> candidate in FitStandardHu())
            {
                result.Add(candidate);
            }
            foreach (List<Group> candidate in FitSevenPairsHu())
            {
                result.Add(candidate);
            }
            return result;
        }

        public HashSet<List<Group>> FitStandardHu()
        {
            int groupsNeeded = 4 - pung.Count - kong.Count;
            HashSet<List<Group>> result = new HashSet<List<Group>>(new GroupListComparer());
            foreach (Group pairGroup in pair)
            {
                HashSet<HashSet<Group>> combinations = Utils.Utils.GetCombinations(allGroupsOf3, groupsNeeded);
                foreach (HashSet<Group> groupOf3 in combinations)
                {
                    List<Group> temp = new List<Group> { pairGroup };
                    temp.AddRange(groupOf3);
                    result.Add(temp);
                }
            }
            return result;
        }

        public HashSet<List<Group>> FitSevenPairsHu()
        {
            if (pair.Count != 7)
            {
                return new HashSet<List<Group>>(new GroupListComparer());
            }
            HashSet<List<Group>> result = new HashSet<List<Group>>(new GroupListComparer());
            if (kong.Count >= 3)
            {
                // Dragon 7 pairs
                List<Group> temp = new List<Group>();
                temp.AddRange(kong);
                if (kong.Count == 3)
                {
                    temp.AddRange(pair);
                }
                result.Add(temp);
                return result;
            }
            if (IsPure(new List<Group>(pair)))
            {
                // Pure 7 pairs
                result.Add(new List<Group>(pair));
                return result;
            }
            result.Add(new List<Group>(pair));
            return result;
        }

        private static bool IsPure(List<Group> groups)
        {
            TileTypeEnum category = groups[0].ToList()[0].GetTileType();
            foreach (Group group in groups)
            {
                foreach (Tile tile in group.ToList())
                {
                    if (tile.GetTileType() != category)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Element-wise list equality, standing in for Java's List.equals so
        /// that Set&lt;List&lt;Group&gt;&gt; deduplicates the same way.
        /// </summary>
        private sealed class GroupListComparer : IEqualityComparer<List<Group>>
        {
            public bool Equals(List<Group> a, List<Group> b)
            {
                if (ReferenceEquals(a, b)) return true;
                if (a == null || b == null || a.Count != b.Count) return false;
                for (int i = 0; i < a.Count; i++)
                {
                    if (!a[i].Equals(b[i])) return false;
                }
                return true;
            }

            public int GetHashCode(List<Group> list)
            {
                int hash = 1;
                foreach (Group group in list)
                {
                    hash = hash * 31 + (group == null ? 0 : group.GetHashCode());
                }
                return hash;
            }
        }
    }
}
