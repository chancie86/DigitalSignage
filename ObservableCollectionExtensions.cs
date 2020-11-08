using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Display
{
    public static class ObservableCollectionExtensions
    {
        public static void AddRange<T>(this ObservableCollection<T> collection, IEnumerable<T> itemsToAdd)
        {
            foreach (var item in itemsToAdd)
            {
                collection.Add(item);
            }
        }
    }
}
