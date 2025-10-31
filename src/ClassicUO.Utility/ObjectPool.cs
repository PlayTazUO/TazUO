namespace ClassicUO.Utility;

using System;
using System.Collections.Generic;

public class ObjectPool<T> where T : class
{
    private readonly Stack<T> _pool;
    private readonly Func<T> _factory;
    private readonly Action<T> _onReturn;
    public int MaxCapacity { get; set; } = 3000;

    public ObjectPool(Func<T> factory, Action<T> onReturn = null, int initialCapacity = 0)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _onReturn = onReturn;
        _pool = new Stack<T>(initialCapacity);

        for (int i = 0; i < initialCapacity; i++)
            _pool.Push(_factory());
    }

    public T Get() => _pool.Count > 0 ? _pool.Pop() : _factory();

    public void Return(T obj)
    {
        _onReturn?.Invoke(obj);
        if (_pool.Count < MaxCapacity)
            _pool.Push(obj);
    }

    public void Clear() => _pool.Clear();

    public int Count => _pool.Count;
}