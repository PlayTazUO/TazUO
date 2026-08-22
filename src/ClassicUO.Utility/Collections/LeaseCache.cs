#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ClassicUO.Utility.Collections;

/// <summary>
///     Keyed cache whose entries live exactly as long as someone is using them.
/// </summary>
/// <remarks>
///     Pair every <see cref="Lease" /> with a <see cref="Release" />. The entry is dropped and disposed with the
///     last one; a leaked release pins it for good. <b>Not thread safe</b> — binds to its constructing thread.
/// </remarks>
/// <typeparam name="TKey">Cache key. Should be a value type or otherwise cheap to hash</typeparam>
/// <typeparam name="TValue">Cached value. <see cref="IDisposable" /> ones are disposed with the last lease unless opted out</typeparam>
public sealed class LeaseCache<TKey, TValue> where TKey : notnull
{
    #region Private members

    private record struct CacheItem(TValue Value, int LeaseCount);

    private readonly Dictionary<TKey, CacheItem> _entries = new();
    private readonly bool _disposesValues;
    private readonly int _ownerThreadId;

    #endregion

    #region Ctor

    /// <summary>
    ///     Creates a cache bound to the calling thread.
    /// </summary>
    /// <param name="disposeValues">False when the values are owned elsewhere and must not be disposed here.</param>
    public LeaseCache(bool disposeValues = true)
    {
        _disposesValues = disposeValues;
        _ownerThreadId = Environment.CurrentManagedThreadId;
    }

    #endregion

    #region Public accessors

    /// <summary>Number of distinct keys currently leased.</summary>
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
    ///     Takes a lease on the value for <paramref name="key" />, producing it on a miss.
    /// </summary>
    /// <param name="key">Cache key.</param>
    /// <param name="factory">
    ///     Produces the value on a miss. Prefer a <c>static</c> lambda and carry state on the key so the call
    ///     allocates no closure.
    /// </param>
    /// <returns>The value, valid until the caller releases this lease.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="factory" /> is null.</exception>
    public TValue Lease(TKey key, Func<TKey, TValue> factory)
    {
        AssertOwnerThread();
        ArgumentNullException.ThrowIfNull(factory);

        ref CacheItem existing = ref CollectionsMarshal.GetValueRefOrNullRef(_entries, key);

        if (!Unsafe.IsNullRef(ref existing))
        {
            existing.LeaseCount++;
            return existing.Value;
        }

        TValue created = factory(key);
        _entries[key] = new CacheItem(created, 1);

        return created;
    }

    /// <summary>
    ///     Gives up one lease on <paramref name="key" />, dropping the entry once none remain.
    /// </summary>
    /// <remarks>An over-release is silently ignored, but it still corrupts the count for whoever does hold the key.</remarks>
    /// <param name="key">Cache key previously passed to <see cref="Lease" />.</param>
    public void Release(TKey key)
    {
        AssertOwnerThread();

        ref CacheItem entry = ref CollectionsMarshal.GetValueRefOrNullRef(_entries, key);

        if (Unsafe.IsNullRef(ref entry))
            return;

        entry.LeaseCount--;

        if (entry.LeaseCount > 0)
            return;

        // Copy out before removing: the ref is invalidated by the removal.
        TValue value = entry.Value;
        _entries.Remove(key);

        if (_disposesValues)
            (value as IDisposable)?.Dispose();
    }

    #endregion

    #region Private methods

    [Conditional("DEBUG")]
    private void AssertOwnerThread() =>
        Debug.Assert(
            Environment.CurrentManagedThreadId == _ownerThreadId,
            $"{nameof(LeaseCache<,>)} accessed from thread {Environment.CurrentManagedThreadId} "
            + $"but is bound to thread {_ownerThreadId}. Marshal the call onto the owning thread."
        );

    #endregion
}
