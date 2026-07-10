using System.Collections.Generic;

namespace SichuanMahjong.Core.Utils
{
    public static class Utils
    {
        /// <summary>
        /// Port of the Java combination generator, including its use of set
        /// semantics for the working "current" collection: adding an element
        /// equal to one already present is a no-op, but the later removal
        /// still deletes the pre-existing equal element. HuFitter's behavior
        /// depends on those semantics, so they are preserved verbatim.
        /// </summary>
        public static HashSet<HashSet<T>> GetCombinations<T>(List<T> list, int n)
        {
            HashSet<HashSet<T>> result = new HashSet<HashSet<T>>(HashSet<T>.CreateSetComparer());
            GetCombinationsHelper(list, n, 0, new HashSet<T>(), result);
            return result;
        }

        private static void GetCombinationsHelper<T>(List<T> list, int n, int index, HashSet<T> current,
                                                     HashSet<HashSet<T>> result)
        {
            if (current.Count == n)
            {
                result.Add(new HashSet<T>(current));
                return;
            }

            if (index == list.Count)
            {
                return;
            }

            T element = list[index];

            current.Add(element);
            GetCombinationsHelper(list, n, index + 1, current, result);

            current.Remove(element);
            GetCombinationsHelper(list, n, index + 1, current, result);
        }
    }
}
