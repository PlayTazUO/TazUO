#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ClassicUO.Utility.Collections;

/// <summary>
///     Fixed-capacity keyed cache with FIFO eviction.
/// </summary>
/// <remarks>
///     <para>
///         <b>Not thread safe.</b> The instance binds to its constructing thread and asserts on cross-thread
///         access. Intended for values that already carry a thread affinity of their own, which a lock could
///         not lift anyway.
///     </para>
///     <para>
///         Owns what it stores: <see cref="IDisposable" /> values are disposed on eviction and on clear.
///     </para>
/// </remarks>
/// <typeparam name="TKey">Cache key. Should be a value type or otherwise cheap to hash</typeparam>
/// <typeparam name="TValue">Cached value. Disposed on eviction when it implements <see cref="IDisposable" /></typeparam>
public sealed class BoundedCache<TKey, TValue> where TKey : notnull
{
    #region Private members

    private readonly Dictionary<TKey, TValue> _entries;
    private readonly Queue<TKey> _insertionOrder;
    private readonly int _capacity;
    private readonly int _ownerThreadId;

    #endregion

    #region Ctor

    /// <summary>
    ///     Creates a cache bound to the calling thread.
    /// </summary>
    /// <param name="capacity">Maximum live entries. Adding beyond this evicts the oldest.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="capacity" /> is not positive.</exception>
    public BoundedCache(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);

        _capacity = capacity;
        _entries = new Dictionary<TKey, TValue>(capacity);
        _insertionOrder = new Queue<TKey>(capacity);
        _ownerThreadId = Environment.CurrentManagedThreadId;
    }

    #endregion

    #region Public accessors

    /// <summary>Live entry count.</summary>
    public int Count
    {
        get
        {
            AssertOwnerThread();
            return _entries.Count;
        }
    }

    #endregion

    #region Public methods

    /// <summary>
    ///     Returns the cached value for <paramref name="key" />, producing and storing it on a miss.
    /// </summary>
    /// <param name="key">Cache key.</param>
    /// <param name="factory">
    ///     Produces the value on a miss. Prefer a <c>static</c> lambda and carry state on the key so the
    ///     call allocates no closure.
    /// </param>
    /// <returns>The cached or newly created value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="factory" /> is null.</exception>
    public TValue GetOrAdd(TKey key, Func<TKey, TValue> factory)
    {
        AssertOwnerThread();
        ArgumentNullException.ThrowIfNull(factory);

        if (_entries.TryGetValue(key, out TValue? existing))
            return existing;

        // Evict before inserting so the cache never transiently exceeds its capacity.
        while (_entries.Count >= _capacity && _insertionOrder.Count > 0)
            EvictOldest();

        TValue created = factory(key);

        _entries[key] = created;
        _insertionOrder.Enqueue(key);

        return created;
    }

    /// <summary>Looks up a key without creating a value on a miss.</summary>
    /// <param name="key">Cache key.</param>
    /// <param name="value">Receives the cached value, or default on a miss.</param>
    /// <returns>True when the key was present.</returns>
    public bool TryGetValue(TKey key, out TValue? value)
    {
        AssertOwnerThread();
        return _entries.TryGetValue(key, out value);
    }

    /// <summary>Drops every entry, disposing any that are <see cref="IDisposable" />.</summary>
    public void Clear()
    {
        AssertOwnerThread();

        foreach (TValue value in _entries.Values)
            (value as IDisposable)?.Dispose();

        _entries.Clear();
        _insertionOrder.Clear();
    }

    #endregion

    #region Private methods

    private void EvictOldest()
    {
        TKey oldest = _insertionOrder.Dequeue();

        if (_entries.Remove(oldest, out TValue? value))
            (value as IDisposable)?.Dispose();
    }

    [Conditional("DEBUG")]
    private void AssertOwnerThread() =>
        Debug.Assert(
            Environment.CurrentManagedThreadId == _ownerThreadId,
            $"{nameof(BoundedCache<TKey, TValue>)} accessed from thread {Environment.CurrentManagedThreadId} "
            + $"but is bound to thread {_ownerThreadId}. Marshal the call onto the owning thread."
        );

    #endregion
}
