using System.Collections.Generic;

namespace Display
{
    public static class HashSetExtensions
    {
        public static void AddRange<T>(this HashSet<T> hashSet, IEnumerable<T> itemsToAdd)
        {
            foreach (var item in itemsToAdd)
                hashSet.Add(item);
        }
    }
}
