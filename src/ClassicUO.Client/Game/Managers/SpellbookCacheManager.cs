using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Managers
{
    internal sealed class SpellbookCacheManager
    {
        private static SpellbookCacheManager _instance;
        public static SpellbookCacheManager Instance => _instance ??= new SpellbookCacheManager();

        private readonly Dictionary<byte, SpellbookCacheEntry> _cache = new();
        private readonly Dictionary<int, SpellBookType> _spellIdToBookType = new();
        private readonly Dictionary<SpellBookType, int> _spellBookBaseId = new();
        private readonly SemaphoreSlim _saveLock = new(1, 1);
        private string _cacheFilePath;
        private const uint DEFAULT_TTL = 14400;

        private SpellbookCacheManager() { }

        public void Initialize()
        {
            if (_cacheFilePath != null)
                return;

            _cacheFilePath = string.IsNullOrEmpty(ProfileManager.ProfilePath)
                ? Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Cache", "spellbooks.json")
                : Path.Combine(ProfileManager.ProfilePath, "spellbook_cache.json");

            LoadFromDisk();
        }

        public SpellbookCacheEntry GetCachedSpellbook(byte spellbookType)
        {
            if (_cache.TryGetValue(spellbookType, out var cached) && !cached.IsExpired())
                return cached;
            return null;
        }

        public SpellBookType? GetSpellBookType(int spellId)
        {
            return _spellIdToBookType.TryGetValue(spellId, out var bookType) ? bookType : null;
        }

        public int GetSpellBookBaseId(SpellBookType bookType)
        {
            return _spellBookBaseId.TryGetValue(bookType, out var baseId) ? baseId : 0;
        }

        public void OnCacheValid(byte spellbookType, uint version, ulong spellBitmask, uint ttl = DEFAULT_TTL)
        {
            if (!_cache.TryGetValue(spellbookType, out var cached))
                return;

            if (cached.Version == version)
            {
                cached.RefreshTTL(ttl);
                cached.SpellBitmask = spellBitmask;
            }
            else
            {
                InvalidateCache(spellbookType);
            }
        }

        public void UpdateCache(SpellbookCacheEntry entry, uint ttl = DEFAULT_TTL)
        {
            if (entry.CachedAt == default)
                entry.CachedAt = DateTime.UtcNow;
            entry.RefreshTTL(ttl);

            _cache[entry.SpellbookType] = entry;

            var bookType = (SpellBookType)entry.SpellbookType;
            DynamicSpellbookRegistry.RegisterDynamic(bookType);
            SyncDynamicSpellsToRegistry(bookType, entry.Spells);

            QueueSave();
        }

        public void SyncCachedSpellsToRegistry(byte spellbookType)
        {
            if (!_cache.TryGetValue(spellbookType, out var cached))
                return;

            var bookType = (SpellBookType)spellbookType;
            DynamicSpellbookRegistry.RegisterDynamic(bookType);
            SyncDynamicSpellsToRegistry(bookType, cached.Spells);
        }

        private void SyncDynamicSpellsToRegistry(SpellBookType bookType, List<DynamicSpellDefinition> sortedSpells)
        {
            ClearMappingsForBookType(bookType);

            var spellDict = DynamicSpellbookRegistry.GetSpellDictionary(bookType);
            spellDict.Clear();

            if (sortedSpells.Count > 0)
                _spellBookBaseId[bookType] = sortedSpells[0].SpellID;

            for (int i = 0; i < sortedSpells.Count; i++)
            {
                var dynSpell = sortedSpells[i];
                int fullSpellID = dynSpell.SpellID + 1;

                _spellIdToBookType[fullSpellID] = bookType;

                spellDict[i + 1] = new SpellDefinition(
                    name: string.IsNullOrEmpty(dynSpell.Name) ? $"Spell {dynSpell.SpellID}" : dynSpell.Name,
                    index: fullSpellID,
                    gumpIconID: dynSpell.IconGraphic,
                    gumpSmallIconID: dynSpell.IconGraphic,
                    powerwords: dynSpell.PowerWords ?? "",
                    manacost: dynSpell.ManaCost,
                    minskill: dynSpell.MinSkill,
                    tithingcost: 0,
                    target: (TargetType)dynSpell.TargetType,
                    regs: Array.Empty<Reagents>()
                );
            }
        }

        private void ClearMappingsForBookType(SpellBookType bookType)
        {
            _spellBookBaseId.Remove(bookType);

            List<int> toRemove = null;
            foreach (var pair in _spellIdToBookType)
            {
                if (pair.Value == bookType)
                {
                    toRemove ??= new List<int>();
                    toRemove.Add(pair.Key);
                }
            }
            if (toRemove != null)
            {
                foreach (var key in toRemove)
                    _spellIdToBookType.Remove(key);
            }
        }

        public void InvalidateCache(byte spellbookType)
        {
            _cache.Remove(spellbookType);
            QueueSave();
        }

        public void InvalidateAll()
        {
            _cache.Clear();
            QueueSave();
        }

        private void QueueSave()
        {
            _ = SaveToDiskAsync();
        }

        private void LoadFromDisk()
        {
            try
            {
                if (string.IsNullOrEmpty(_cacheFilePath) || !File.Exists(_cacheFilePath))
                    return;

                string json = File.ReadAllText(_cacheFilePath);
                var entries = JsonSerializer.Deserialize(json, SpellbookCacheJsonContext.Default.ListSpellbookCacheEntry);

                if (entries == null)
                    return;

                foreach (var entry in entries)
                {
                    entry.RefreshTTL(DEFAULT_TTL);
                    _cache[entry.SpellbookType] = entry;

                    var bookType = (SpellBookType)entry.SpellbookType;
                    DynamicSpellbookRegistry.RegisterDynamic(bookType);
                    SyncDynamicSpellsToRegistry(bookType, entry.Spells);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to load spellbook cache: {ex.Message}");
            }
        }

        private async Task SaveToDiskAsync()
        {
            await _saveLock.WaitAsync();
            try
            {
                if (string.IsNullOrEmpty(_cacheFilePath))
                    return;

                var directory = Path.GetDirectoryName(_cacheFilePath);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                var snapshot = new List<SpellbookCacheEntry>(_cache.Values);
                string json = JsonSerializer.Serialize(snapshot, SpellbookCacheJsonContext.Default.ListSpellbookCacheEntry);

                await File.WriteAllTextAsync(_cacheFilePath, json);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to save spellbook cache: {ex.Message}");
            }
            finally
            {
                _saveLock.Release();
            }
        }
    }
}
