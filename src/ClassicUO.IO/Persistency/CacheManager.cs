using System;
using System.Threading;

namespace ClassicUO.IO.Persistency;

/// <summary>
///     Identifies the supported cache buckets managed by <see cref="CacheManager" />.
///     Each cache type maps to one persisted JSON file on disk and one in-memory entry.
/// </summary>
public enum CacheType
{
    Font
}

/// <summary>
/// A singleton manager for operating file-based caches
/// </summary>
public static class CacheManager
{
    /// <summary>
    ///     Lazily creates the singleton cache manager in a thread-safe manner.
    /// </summary>
    private static readonly Lazy<FilePersistenceManager<CacheType>> _instance =
        new(() => new FilePersistenceManager<CacheType>("Cache"), LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>
    ///     Gets the singleton cache manager instance.
    /// </summary>
    public static FilePersistenceManager<CacheType> Instance => _instance.Value;
}
