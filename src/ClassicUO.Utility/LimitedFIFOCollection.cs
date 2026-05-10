#nullable enable
using System.Collections.Generic;
using System.Linq;

namespace ClassicUO.Utility;

public class LimitedFIFOCollection<T>
{
    private readonly List<T> _list;
    public int Limit { get; }

    public LimitedFIFOCollection(int limit)
    {
        Limit = limit;
        _list = new List<T>(limit + 1);
    }

    public void Add(T item)
    {
        _list.Add(item);
        while (_list.Count > Limit) _list.RemoveAt(0);
    }

    public T? First()
    {
        if (_list.Count == 0) return default;

        return _list[0];
    }

    public void Clear() => _list.Clear();

    public IEnumerable<T> GetItems() => _list.AsEnumerable();
}
