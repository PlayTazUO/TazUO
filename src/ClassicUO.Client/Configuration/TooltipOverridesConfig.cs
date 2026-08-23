using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using ClassicUO.Game.Managers;

namespace ClassicUO.Configuration
{
    /// <summary>Uniform read/write access to a scoped tooltip-override store.</summary>
    public interface ITooltipOverridesScopedSave
    {
        List<ToolTipOverrideData> Overrides { get; }

        void Upsert(ToolTipOverrideData data);

        void RemoveAt(int index);

        void Move(int index, int delta);

        void Clear();
    }

    /// <summary>
    /// Base for the scoped tooltip-override saves. Each scope is a separate <see cref="JsonSave{T}"/>
    /// persisting <c>tooltip_overrides.json</c> into its own folder (see <see cref="JsonSaveLocationHelper"/>),
    /// so rules can be shared machine-wide, per server, per account or kept per character. The aggregate
    /// <see cref="TooltipOverridesConfig"/> owns one of each and merges them for tooltip processing.
    /// </summary>
    /// <typeparam name="T">The concrete scoped save type.</typeparam>
    public abstract class TooltipOverridesScopedSave<T> : JsonSave<T>, INotifyPropertyChanged, ITooltipOverridesScopedSave
        where T : TooltipOverridesScopedSave<T>, INotifyPropertyChanged, new()
    {
        public List<ToolTipOverrideData> Overrides { get; set; } = new();

        /// <summary>
        /// Stores <paramref name="data"/> at its <see cref="ToolTipOverrideData.Index"/>. When the index
        /// is out of range the entry is appended (and its index updated to its new position). Persists on
        /// any change.
        /// </summary>
        public void Upsert(ToolTipOverrideData data)
        {
            if (data == null)
                return;

            if (data.Index >= 0 && data.Index < Overrides.Count)
            {
                Overrides[data.Index] = data;
            }
            else
            {
                data.Index = Overrides.Count;
                Overrides.Add(data);
            }

            Save();
        }

        /// <summary>Removes the entry at <paramref name="index"/>, if present, reindexes and persists.</summary>
        public void RemoveAt(int index)
        {
            if (index < 0 || index >= Overrides.Count)
                return;

            Overrides.RemoveAt(index);
            Reindex();
            Save();
        }

        /// <summary>
        /// Swaps the entry at <paramref name="index"/> with its neighbour <paramref name="index"/> + <paramref name="delta"/>
        /// (i.e. -1 moves it one position up, +1 one position down), reindexes and persists. A no-op when the move
        /// would fall outside the list.
        /// </summary>
        public void Move(int index, int delta)
        {
            int target = index + delta;
            if (index < 0 || target < 0 || target >= Overrides.Count)
                return;

            (Overrides[index], Overrides[target]) = (Overrides[target], Overrides[index]);
            Reindex();
            Save();
        }

        /// <summary>Removes every override and persists.</summary>
        public void Clear()
        {
            Overrides.Clear();
            Save();
        }

        /// <summary>Keeps each entry's <see cref="ToolTipOverrideData.Index"/> in sync with its list position.</summary>
        internal void Reindex()
        {
            for (int i = 0; i < Overrides.Count; i++)
            {
                if (Overrides[i] != null)
                    Overrides[i].Index = i;
            }
        }
    }

    /// <summary>Tooltip overrides scoped to the current profile folder (per character).</summary>
    public sealed class TooltipOverridesCharScope : TooltipOverridesScopedSave<TooltipOverridesCharScope>
    {
        protected override SettingsScope Scope => SettingsScope.Char;

        protected override string FileName => TooltipOverridesConfig.TooltipOverridesFileName;

        protected override JsonTypeInfo<TooltipOverridesCharScope> TypeInfo => TooltipOverridesJsonContext.DefaultToUse.TooltipOverridesCharScope;
    }

    /// <summary>Tooltip overrides scoped to <c>Data/&lt;Server&gt;/&lt;Account&gt;</c> (per account on a server).</summary>
    public sealed class TooltipOverridesAccountScope : TooltipOverridesScopedSave<TooltipOverridesAccountScope>
    {
        protected override SettingsScope Scope => SettingsScope.Account;

        protected override string FileName => TooltipOverridesConfig.TooltipOverridesFileName;

        protected override JsonTypeInfo<TooltipOverridesAccountScope> TypeInfo => TooltipOverridesJsonContext.DefaultToUse.TooltipOverridesAccountScope;
    }

    /// <summary>Tooltip overrides scoped to <c>Data/&lt;Server&gt;</c> (per server).</summary>
    public sealed class TooltipOverridesServerScope : TooltipOverridesScopedSave<TooltipOverridesServerScope>
    {
        protected override SettingsScope Scope => SettingsScope.Server;

        protected override string FileName => TooltipOverridesConfig.TooltipOverridesFileName;

        protected override JsonTypeInfo<TooltipOverridesServerScope> TypeInfo => TooltipOverridesJsonContext.DefaultToUse.TooltipOverridesServerScope;
    }

    /// <summary>Tooltip overrides scoped to the shared <c>Data</c> folder (machine-wide).</summary>
    public sealed class TooltipOverridesGlobalScope : TooltipOverridesScopedSave<TooltipOverridesGlobalScope>
    {
        protected override SettingsScope Scope => SettingsScope.Global;

        protected override string FileName => TooltipOverridesConfig.TooltipOverridesFileName;

        protected override JsonTypeInfo<TooltipOverridesGlobalScope> TypeInfo => TooltipOverridesJsonContext.DefaultToUse.TooltipOverridesGlobalScope;
    }

    /// <summary>
    /// Aggregate tooltip-override store. Owns one scoped save per <see cref="SettingsScope"/> and merges
    /// their rules for tooltip processing, more-specific scopes first, so a character rule beats an
    /// account, server or global rule.
    /// </summary>
    public sealed class TooltipOverridesConfig
    {
        public const string TooltipOverridesFileName = "tooltip_overrides.json";

        /// <summary>Per-character rules, persisted in the current profile folder.</summary>
        public TooltipOverridesCharScope Char { get; private set; } = new();

        /// <summary>Per-account rules, persisted under the account folder.</summary>
        public TooltipOverridesAccountScope Account { get; private set; } = new();

        /// <summary>Per-server rules, persisted under the server folder.</summary>
        public TooltipOverridesServerScope Server { get; private set; } = new();

        /// <summary>Machine-wide rules, persisted in the shared Data folder.</summary>
        public TooltipOverridesGlobalScope Global { get; private set; } = new();

        private static TooltipOverridesConfig _current;

        /// <summary>The tooltip-override config for the currently loaded profile.</summary>
        public static TooltipOverridesConfig Current => _current ??= Load();

        /// <summary>
        /// Loads every scoped save and sets it as <see cref="Current"/>. Called on each profile load so the
        /// caches track the active server/account/character (each scope's folder is resolved from those).
        /// </summary>
        public static TooltipOverridesConfig Load()
        {
            var config = new TooltipOverridesConfig
            {
                Char = TooltipOverridesCharScope.Load(),
                Account = TooltipOverridesAccountScope.Load(),
                Server = TooltipOverridesServerScope.Load(),
                Global = TooltipOverridesGlobalScope.Load()
            };

            config.Reindex();

            return _current = config;
        }

        /// <summary>Persists every scoped save and drops the cache so the next profile reloads fresh.</summary>
        public static void Unload()
        {
            if (_current == null)
                return;

            _current.Char.Save();
            _current.Account.Save();
            _current.Server.Save();
            _current.Global.Save();
            _current = null;
        }

        /// <summary>Returns the scoped save backing <paramref name="scope"/>.</summary>
        public ITooltipOverridesScopedSave GetScope(SettingsScope scope) => scope switch
        {
            SettingsScope.Char => Char,
            SettingsScope.Account => Account,
            SettingsScope.Server => Server,
            SettingsScope.Global => Global,
            _ => throw new ArgumentOutOfRangeException(nameof(scope), scope, null)
        };

        /// <summary>
        /// Every override from all scopes, most-specific scope first (char, account, server, global).
        /// Tooltip processing walks this in order and the first matching rule wins.
        /// </summary>
        public ToolTipOverrideData[] GetAllOverrides()
        {
            var combined = new List<ToolTipOverrideData>(Char.Overrides.Count + Account.Overrides.Count + Server.Overrides.Count + Global.Overrides.Count);
            combined.AddRange(Char.Overrides);
            combined.AddRange(Account.Overrides);
            combined.AddRange(Server.Overrides);
            combined.AddRange(Global.Overrides);
            return combined.ToArray();
        }

        /// <summary>Keeps each scope's <see cref="ToolTipOverrideData.Index"/> in sync with its list position.</summary>
        private void Reindex()
        {
            Char.Reindex();
            Account.Reindex();
            Server.Reindex();
            Global.Reindex();
        }
    }

    [JsonSerializable(typeof(TooltipOverridesCharScope), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(TooltipOverridesAccountScope), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(TooltipOverridesServerScope), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(TooltipOverridesGlobalScope), GenerationMode = JsonSourceGenerationMode.Metadata)]
    sealed partial class TooltipOverridesJsonContext : JsonSerializerContext
    {
        sealed class SnakeCaseNamingPolicy : JsonNamingPolicy
        {
            public static SnakeCaseNamingPolicy Instance { get; } = new SnakeCaseNamingPolicy();

            public override string ConvertName(string name) =>
                string.Concat(name.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x.ToString() : x.ToString())).ToLower();
        }

        private static Lazy<JsonSerializerOptions> _jsonOptions { get; } = new Lazy<JsonSerializerOptions>(() =>
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = SnakeCaseNamingPolicy.Instance
            };
            return options;
        });

        public static TooltipOverridesJsonContext DefaultToUse { get; } = new TooltipOverridesJsonContext(_jsonOptions.Value);
    }
}
