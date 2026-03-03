using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Managers
{
    internal sealed class SpellbookCacheManager
    {
        private static SpellbookCacheManager _instance;
        public static SpellbookCacheManager Instance => _instance ??= new SpellbookCacheManager();

        private readonly Dictionary<byte, SpellbookCacheEntry> _cache;
        private readonly Dictionary<byte, ulong> _spellBitmasks;
        private readonly Dictionary<int, int> _clientToServerSpellId; // Maps client spell ID to server spell ID
        private readonly Dictionary<int, SpellBookType> _spellIdToBookType; // Maps spell ID to spellbook type
        private readonly Dictionary<SpellBookType, int> _spellBookBaseId; // Maps spellbook type to its base spell ID
        private string _cacheFilePath;
        private const uint DEFAULT_TTL = 14400; // 4 hours
        private bool _isInitialized;

        private SpellbookCacheManager()
        {
            _cache = new Dictionary<byte, SpellbookCacheEntry>();
            _spellBitmasks = new Dictionary<byte, ulong>();
            _clientToServerSpellId = new Dictionary<int, int>();
            _spellIdToBookType = new Dictionary<int, SpellBookType>();
            _spellBookBaseId = new Dictionary<SpellBookType, int>();
            _isInitialized = false;
        }

        public void Initialize()
        {
            if (_isInitialized)
            {
                Log.Trace("SpellbookCacheManager already initialized");
                return;
            }

            if (string.IsNullOrEmpty(ProfileManager.ProfilePath))
            {
                Log.Warn("SpellbookCacheManager.Initialize called before ProfileManager.Load - using fallback path");
                _cacheFilePath = Path.Combine(
                    CUOEnviroment.ExecutablePath,
                    "Data",
                    "Cache",
                    "spellbooks.json"
                );
            }
            else
            {
                _cacheFilePath = Path.Combine(ProfileManager.ProfilePath, "spellbook_cache.json");
                Log.Trace($"SpellbookCacheManager initialized with profile path: {_cacheFilePath}");
            }

            LoadFromDisk();
            _isInitialized = true;
        }

        public bool ShouldRequestSpellbook(byte spellbookType, out uint cachedVersion)
        {
            Log.Trace($"[CLIENT CACHE] ====== CHECKING CACHE ======");
            Log.Trace($"[CLIENT CACHE] Spellbook Type: {spellbookType}");

            if (!_cache.TryGetValue(spellbookType, out var cached))
            {
                // No cache at all - request with version 0
                Log.Trace($"[CLIENT CACHE] No cache found - requesting from server with version 0");
                cachedVersion = 0;
                return true;
            }

            Log.Trace($"[CLIENT CACHE] Cache found - Version: {cached.Version}");
            Log.Trace($"[CLIENT CACHE] Cache Expiry: {cached.ExpiresAt}");
            Log.Trace($"[CLIENT CACHE] Current Time: {DateTimeOffset.UtcNow.ToUnixTimeSeconds()}");
            Log.Trace($"[CLIENT CACHE] Is Expired: {cached.IsExpired()}");
            Log.Trace($"[CLIENT CACHE] Sending verification request to server with cached version {cached.Version}");
            cachedVersion = cached.Version;
            return true;
        }

        public SpellbookCacheEntry GetCachedSpellbook(byte spellbookType)
        {
            if (!_isInitialized)
            {
                Log.Warn("SpellbookCacheManager.GetCachedSpellbook called before Initialize()");
                return null;
            }

            if (_cache.TryGetValue(spellbookType, out var cached))
            {
                if (cached.IsFresh())
                    return cached;
            }
            return null;
        }

        public int GetServerSpellId(int clientSpellId)
        {
            if (_clientToServerSpellId.TryGetValue(clientSpellId, out int serverSpellId))
            {
                Log.Trace($"[SPELLBOOK CACHE] Mapped client spell ID {clientSpellId} to server spell ID {serverSpellId}");
                return serverSpellId;
            }
            // No mapping, return client ID (for standard spells)
            return clientSpellId;
        }
        public SpellBookType? GetSpellBookType(int spellId)
        {
            if (_spellIdToBookType.TryGetValue(spellId, out SpellBookType bookType))
            {
                return bookType;
            }
            return null;
        }
        public int GetSpellBookBaseId(SpellBookType bookType)
        {
            if (_spellBookBaseId.TryGetValue(bookType, out int baseId))
            {
                return baseId;
            }
            return 0;
        }
        public void OnCacheValid(byte spellbookType, uint version, ulong spellBitmask, uint ttl = DEFAULT_TTL)
        {
            if (_cache.TryGetValue(spellbookType, out var cached))
            {
                if (cached.Version == version)
                {
                    cached.RefreshTTL(ttl);
                    cached.SpellBitmask = spellBitmask;
                    _spellBitmasks[spellbookType] = spellBitmask;

                    Log.Trace($"Spellbook cache valid for type {spellbookType}, version {version}");
                }
                else
                {
                    Log.Warn($"Version mismatch for spellbook {spellbookType}: cached={cached.Version}, server={version}");
                    InvalidateCache(spellbookType);
                }
            }
        }

        public void UpdateCache(
            byte spellbookType,
            uint version,
            ulong spellBitmask,
            ushort bookGraphic,
            ushort minimizedGraphic,
            List<DynamicSpellDefinition> spells,
            uint ttl = DEFAULT_TTL,
            byte spellsPerPageSide = 8,
            byte maxDictionaryPages = 2,
            string[] pageNames = null,
            bool displayManaCost = false,
            bool displayMinSkill = false,
            string customPropertyTitle = null,
            string customPropertyLabel = null,
            string customPropertyName = null)
        {
            var entry = new SpellbookCacheEntry
            {
                SpellbookType = spellbookType,
                Version = version,
                CachedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddSeconds(ttl),
                SpellBitmask = spellBitmask,
                BookGraphic = bookGraphic,
                MinimizedGraphic = minimizedGraphic,
                SpellsPerPageSide = spellsPerPageSide,
                MaxDictionaryPages = maxDictionaryPages,
                PageNames = pageNames ?? Array.Empty<string>(),
                DisplayManaCost = displayManaCost,
                DisplayMinSkill = displayMinSkill,
                CustomPropertyTitle = customPropertyTitle,
                CustomPropertyLabel = customPropertyLabel,
                CustomPropertyName = customPropertyName
            };

            foreach (var spell in spells)
            {
                entry.Spells[spell.SpellID] = spell;
            }

            _cache[spellbookType] = entry;
            _spellBitmasks[spellbookType] = spellBitmask;

            Log.Trace($"Updated spellbook cache for type {spellbookType}, version {version}, {spells.Count} spells");

            var bookType = (SpellBookType)spellbookType;
            DynamicSpellbookRegistry.RegisterDynamic(bookType);
            SyncDynamicSpellsToRegistry(bookType, spells);

            SaveToDiskAsync();
        }
        public void SyncCachedSpellsToRegistry(byte spellbookType)
        {
            if (!_cache.TryGetValue(spellbookType, out var cached))
            {
                Log.Warn($"[SPELLBOOK CACHE] SyncCachedSpellsToRegistry: No cache for type {spellbookType}");
                return;
            }

            var bookType = (SpellBookType)spellbookType;
            var spellList = new List<DynamicSpellDefinition>(cached.Spells.Values);
            DynamicSpellbookRegistry.RegisterDynamic(bookType);
            SyncDynamicSpellsToRegistry(bookType, spellList);
        }
        private void SyncDynamicSpellsToRegistry(SpellBookType bookType, List<DynamicSpellDefinition> spells)
        {
            Log.Trace($"[SPELLBOOK CACHE] SyncDynamicSpellsToRegistry called with {spells.Count} spells for {bookType}");

            var spellDict = DynamicSpellbookRegistry.GetSpellDictionary(bookType);
            spellDict.Clear();
            _clientToServerSpellId.Clear();
            Log.Trace($"[SPELLBOOK CACHE] Cleared spell dictionary for {bookType} and client-to-server mapping");

            var sortedSpells = new List<DynamicSpellDefinition>(spells);
            sortedSpells.Sort((a, b) => a.SpellID.CompareTo(b.SpellID));

            if (sortedSpells.Count > 0)
            {
                int baseSpellId = sortedSpells[0].SpellID;
                _spellBookBaseId[bookType] = baseSpellId;
                Log.Trace($"[SPELLBOOK CACHE] Base spell ID for {bookType}: {baseSpellId}");
            }

            for (int i = 0; i < sortedSpells.Count; i++)
            {
                var dynSpell = sortedSpells[i];

                int fullSpellID = dynSpell.SpellID;

                int dictionaryIndex = i + 1;

                _clientToServerSpellId[fullSpellID] = dynSpell.SpellID;

                _spellIdToBookType[fullSpellID] = bookType;

                Log.Trace($"[SPELLBOOK CACHE] Syncing spell {i + 1}: ServerSpellID={dynSpell.SpellID}, ClientID={fullSpellID}, StorageIndex={dictionaryIndex}, BookType={bookType}, Name='{dynSpell.Name}', Icon=0x{dynSpell.IconGraphic:X4}");

                var spellDef = new SpellDefinition(
                    name: string.IsNullOrEmpty(dynSpell.Name) ? $"Spell {dynSpell.SpellID}" : dynSpell.Name,
                    index: fullSpellID,
                    gumpIconID: dynSpell.IconGraphic,
                    gumpSmallIconID: dynSpell.IconGraphic,
                    powerwords: dynSpell.PowerWords ?? "",
                    manacost: 0,
                    minskill: 0,
                    tithingcost: 0,
                    target: (TargetType)dynSpell.TargetType,
                    regs: new Reagents[0]
                );

                spellDict[dictionaryIndex] = spellDef;
                Log.Trace($"[SPELLBOOK CACHE] SetSpell called for index {dictionaryIndex} with MappedID={fullSpellID}");
            }

            Log.Trace($"[SPELLBOOK CACHE] Synced {sortedSpells.Count} dynamic spells to {bookType} registry");
        }

        public void InvalidateCache(byte spellbookType)
        {
            if (spellbookType == 0xFF)
            {
                _cache.Clear();
                _spellBitmasks.Clear();
                Log.Trace("Invalidated all spellbook caches");
            }
            else
            {
                _cache.Remove(spellbookType);
                _spellBitmasks.Remove(spellbookType);
                Log.Trace($"Invalidated spellbook cache for type {spellbookType}");
            }

            SaveToDiskAsync();
        }

        public ulong GetSpellBitmask(byte spellbookType)
        {
            return _spellBitmasks.TryGetValue(spellbookType, out var mask) ? mask : 0;
        }
        public bool HasSpell(byte spellbookType, ushort spellID)
        {
            if (!_cache.TryGetValue(spellbookType, out var cached))
                return false;

            var sortedSpells = new List<DynamicSpellDefinition>(cached.Spells.Values);
            sortedSpells.Sort((a, b) => a.SpellID.CompareTo(b.SpellID));

            int index = sortedSpells.FindIndex(s => s.SpellID == spellID);
            if (index < 0 || index >= 64)
                return false;

            ulong mask = GetSpellBitmask(spellbookType);
            return (mask & (1UL << index)) != 0;
        }
        private void LoadFromDisk()
        {
            try
            {
                if (string.IsNullOrEmpty(_cacheFilePath))
                {
                    Log.Warn("Cache file path not set - call Initialize() first");
                    return;
                }

                if (!File.Exists(_cacheFilePath))
                {
                    Log.Trace("No spellbook cache file found");
                    return;
                }

                string json = File.ReadAllText(_cacheFilePath);
                var persistent = JsonSerializer.Deserialize(
                    json,
                    SpellbookCacheJsonContext.Default.ListPersistentCacheEntry
                );

                if (persistent != null)
                {
                    Log.Info($"[SPELLBOOK CACHE] LoadFromDisk: Found {persistent.Count} cached spellbook(s)");

                    foreach (var entry in persistent)
                    {
                        _cache[entry.SpellbookType] = entry.ToRuntimeCache(DEFAULT_TTL);

                        var bookType = (SpellBookType)entry.SpellbookType;
                        Log.Info($"[SPELLBOOK CACHE] Registering dynamic spellbook type: {bookType} (byte value: {entry.SpellbookType})");
                        DynamicSpellbookRegistry.RegisterDynamic(bookType);
                        Log.Info($"[SPELLBOOK CACHE] Syncing {entry.Spells.Count} spells from disk cache for {bookType}");
                        SyncDynamicSpellsToRegistry(bookType, entry.Spells);

                        ushort itemGraphic = entry.ItemGraphic;
                        if (itemGraphic > 0)
                        {
                            SpellbookTypeRegistry.Register(itemGraphic, bookType);
                            Log.Trace($"[SPELLBOOK CACHE] Registered ItemGraphic 0x{itemGraphic:X4} -> {bookType} from cache");
                        }
                    }

                    Log.Trace($"Loaded {persistent.Count} spellbook caches from disk");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to load spellbook cache: {ex.Message}");
            }
        }
        private async void SaveToDiskAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_cacheFilePath))
                {
                    Log.Warn("Cache file path not set - cannot save to disk");
                    return;
                }

                var directory = Path.GetDirectoryName(_cacheFilePath);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                var persistent = new List<PersistentCacheEntry>();
                foreach (var entry in _cache.Values)
                {
                    persistent.Add(PersistentCacheEntry.FromRuntimeCache(entry));
                }

                string json = JsonSerializer.Serialize(
                    persistent,
                    SpellbookCacheJsonContext.Default.ListPersistentCacheEntry
                );

                await File.WriteAllTextAsync(_cacheFilePath, json);

                Log.Trace($"Saved {persistent.Count} spellbook caches to disk");
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to save spellbook cache: {ex.Message}");
                // Non-fatal, continue
            }
        }
        public void Clear()
        {
            _cache.Clear();
            _spellBitmasks.Clear();
        }
    }
}
