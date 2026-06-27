using System.Collections.Generic;

namespace ClassicUO.Utility.Collections;

public static class CollectionExtensions
{
    public static void AddRange<T>(this IList<T> list, IEnumerable<T> values)
    {
        foreach (T value in values)
            list.Add(value);
    }
}
