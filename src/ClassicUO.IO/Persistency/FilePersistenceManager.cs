using System;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using ClassicUO.Utility.Logging;

namespace ClassicUO.IO.Persistency;

/// <summary>
///     Describes a cache entry without tying it to a concrete value type.
/// </summary>
/// <remarks>
///     A typed definition allows the cache manager to load, validate, and persist values
///     while still keeping the cache key strongly associated with the intended value type.
/// </remarks>
public abstract class PersistentItemDefinition<TEntryType> where TEntryType : struct, Enum
{
    /// <summary>
    ///     Gets the cache bucket used to store this item.
    /// </summary>
    public abstract TEntryType Key { get; }

    /// <summary>
    ///     Gets the runtime type expected for values stored under this definition.
    /// </summary>
    public abstract Type ValueType { get; }
}

/// <summary>
///     Strongly typed cache definition used for cache entries with a known value type.
/// </summary>
/// <typeparam name="TEntryType">The enum describing possible file 'buckets'</typeparam>
/// <typeparam name="TValue">The type stored in the cache entry.</typeparam>
/// <remarks>
///     The type constraint ensures the cached value can be instantiated when a persisted
///     value does not exist or cannot be loaded.
/// </remarks>
public abstract class PersistentItemDefinition<TEntryType, TValue> : PersistentItemDefinition<TEntryType> where TEntryType : struct, Enum where TValue : class, new()
{
    /// <summary>
    ///     Gets the runtime value type associated with this cache definition.
    /// </summary>
    public sealed override Type ValueType => typeof(TValue);
}

/// <summary>
///     Thread-safe cache manager responsible for loading, storing, and persisting small
///     application caches to disk.
/// </summary>
/// <remarks>
///     <para>
///         The manager stores cache values in a concurrent dictionary keyed by <see cref="TEntryType" />.
///         Each dictionary entry holds the actual typed value directly.
///     </para>
///     <para>
///         Thread safety is achieved using a <see cref="Lock" /> when loading or creating values
///         to ensure that file loading and object creation happens only once per cache key, even
///         under concurrent access.
///     </para>
///     <para>
///         The cache currently stores one logical value per <see cref="TEntryType" />.
///     </para>
/// </remarks>
public class FilePersistenceManager<TEntryType> where TEntryType : struct, Enum
{
    /// <summary>
    ///     In-memory cache entries storing the actual typed values.
    /// </summary>
    private readonly ConcurrentDictionary<TEntryType, object> _cache = new();

    /// <summary>
    ///     Precomputed file paths for each cache bucket.
    /// </summary>
    private readonly FrozenDictionary<TEntryType, string> _filePaths;

    /// <summary>
    ///     Directory used to store cache files on disk.
    /// </summary>
    private readonly string _cacheDirectory;

    /// <summary>
    ///     JSON serializer settings used for cache persistence.
    /// </summary>
    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly Lock _cacheLock = new();

    /// <summary>
    ///     Creates a new cache manager and initializes the cache directory and file map.
    /// </summary>
    /// <param name="persistencyDirName">The name of the directory in which to store persistent files</param>
    public FilePersistenceManager(string persistencyDirName)
    {
        AssertValidDirName(persistencyDirName);
        _cacheDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, persistencyDirName);

        if (!Directory.Exists(_cacheDirectory))
            Directory.CreateDirectory(_cacheDirectory);

        var filePaths = new Dictionary<TEntryType, string>();

        foreach (TEntryType cacheType in Enum.GetValues<TEntryType>())
            filePaths[cacheType] = GetCacheFilePath(cacheType);

        _filePaths = filePaths.ToFrozenDictionary();
    }

    private static void AssertValidDirName(string dirName)
    {
        if (string.IsNullOrWhiteSpace(dirName))
            throw new ArgumentException("Directory name cannot be null or whitespace.", nameof(dirName));

        char[] invalidChars = Path.GetInvalidFileNameChars();
        if (dirName.IndexOfAny(invalidChars) >= 0)
            throw new ArgumentException($"Directory name contains invalid characters: {dirName}", nameof(dirName));
    }

    /// <summary>
    ///     Gets a typed cache value for the specified definition.
    /// </summary>
    /// <typeparam name="TValue">The expected cached value type</typeparam>
    /// <param name="definition">The cache definition describing the key and expected type</param>
    /// <returns>
    ///     The cached value, if one exists or a new, default instance of <typeparamref name="TValue" />
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         The method first attempts to retrieve a cached value without locking. If the value
    ///         is found and has the correct type, it is returned immediately.
    ///     </para>
    ///     <para>
    ///         If no value exists or the type doesn't match, the method acquires a lock to ensure
    ///         thread-safe loading. A double-check pattern is used to avoid redundant loading if
    ///         another thread has already loaded the value.
    ///     </para>
    ///     <para>
    ///         Values are loaded from disk or created as new instances using <see cref="LoadOrCreate{TValue}"/>.
    ///         Once loaded, the value is stored directly in the cache dictionary for future access.
    ///     </para>
    /// </remarks>
    public TValue Get<TValue>(PersistentItemDefinition<TEntryType, TValue> definition) where TValue : class, new()
    {
        if (_cache.TryGetValue(definition.Key, out object cachedValue))
        {
            if (cachedValue is TValue castCachedValue)
                return castCachedValue;
        }

        lock (_cacheLock)
        {
            if (_cache.TryGetValue(definition.Key, out cachedValue))
            {
                if (cachedValue is TValue castCachedValue)
                    return castCachedValue;
            }


            TValue freshValue = LoadOrCreate(definition);
            _cache[definition.Key] = freshValue;
            return freshValue;
        }
    }

    /// <summary>
    ///     Stores a cache value and persists it to disk.
    /// </summary>
    /// <typeparam name="TValue">The value type being stored.</typeparam>
    /// <param name="definition">The cache definition describing where the value belongs.</param>
    /// <param name="data">The value to cache and save.</param>
    /// <returns><c>true</c> if the value was stored and written successfully; otherwise <c>false</c>.</returns>
    /// <remarks>
    ///     The in-memory cache is updated first so subsequent reads can reuse the same value.
    ///     The file write is then attempted to keep the persisted cache in sync.
    /// </remarks>
    public bool Set<TValue>(PersistentItemDefinition<TEntryType, TValue> definition, TValue data) where TValue : class, new()
    {
        if (data == null)
        {
            Log.Warn($"Attempted to set null data for cache type: {definition.Key}");
            return false;
        }

        _cache[definition.Key] = data;
        return SaveToFile(definition, data);
    }

    /// <summary>
    ///     Builds the on-disk file path for a cache bucket.
    /// </summary>
    /// <param name="cacheType">The cache bucket identifier.</param>
    /// <returns>The full file path for the cache bucket.</returns>
    private string GetCacheFilePath(TEntryType cacheType) => Path.Combine(_cacheDirectory, $"{cacheType.ToString().ToLowerInvariant()}.json");

    /// <summary>
    ///     Loads the cache value from disk or creates a new instance when the backing file does not exist or cannot be read
    /// </summary>
    /// <typeparam name="TValue">The type to load or create.</typeparam>
    /// <param name="definition">The cache definition describing the cache entry.</param>
    /// <returns>The loaded value, or a fresh default instance if loading fails.</returns>
    private TValue LoadOrCreate<TValue>(PersistentItemDefinition<TEntryType, TValue> definition) where TValue : class, new() => LoadFromFile(definition) ?? new TValue();


    /// <summary>
    ///     Attempts to load a cache bucket's value from disk.
    /// </summary>
    /// <typeparam name="TValue">The expected value type</typeparam>
    /// <param name="definition">The cache definition describing which file to read</param>
    /// <returns>
    ///     The deserialized value if the file exists and can be read; otherwise <c>null</c>
    /// </returns>
    /// <remarks>
    ///     Failures are logged and treated as cache misses.
    /// </remarks>
    private TValue LoadFromFile<TValue>(PersistentItemDefinition<TEntryType, TValue> definition) where TValue : class, new()
    {
        string filePath = _filePaths[definition.Key];

        if (!File.Exists(filePath))
            return null;

        try
        {
            string json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<TValue>(json, _jsonSerializerOptions);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to load cache file for {definition.Key}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    ///     Serializes the given contents into the give cache bucket backing file
    /// </summary>
    /// <typeparam name="TValue">The type being persisted</typeparam>
    /// <param name="definition">The cache definition describing where to save the value</param>
    /// <param name="data">The value to serialize.</param>
    /// <returns><c>true</c> if the write succeeds, <c>false</c> otherwise</returns>
    /// <remarks>
    ///     Failures are logged and reported back to the caller
    /// </remarks>
    private bool SaveToFile<TValue>(PersistentItemDefinition<TEntryType, TValue> definition, TValue data) where TValue : class, new()
    {
        string filePath = _filePaths[definition.Key];
        try
        {
            string json = JsonSerializer.Serialize(data, _jsonSerializerOptions);
            File.WriteAllText(filePath, json);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to save cache file for {definition.Key}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    ///     Deletes the backing file for a cache bucket
    /// </summary>
    /// <param name="cacheType">The cache type to delete</param>
    private void DeleteCacheFile(TEntryType cacheType)
    {
        string filePath = _filePaths[cacheType];
        try
        {
            File.Delete(filePath);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to delete cache file for {cacheType}: {e.Message}");
        }
    }

    /// <summary>
    ///     Removes a single cache bucket, from both memory and file
    /// </summary>
    /// <param name="cacheType">The cache bucket to remove.</param>
    public void Clear(TEntryType cacheType)
    {
        _cache.TryRemove(cacheType, out _);
        DeleteCacheFile(cacheType);
    }

    /// <summary>
    ///     Clears all cache entries, both from memory and on file
    /// </summary>
    public void ClearAll()
    {
        _cache.Clear();
        foreach (TEntryType cacheType in _filePaths.Keys)
            DeleteCacheFile(cacheType);
    }
}
