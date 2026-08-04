using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Utility.Logging;

namespace ClassicUO.LegionScripting
{
    public static class PersistentVars
    {
        private const string DB_FILE = "legionvars.db";
        private const string GlobalScopeKey = "GLOBAL";

        private static string _charScopeKey = "";
        private static string _accountScopeKey = "";
        private static string _serverScopeKey = "";

        private static PersistentVarsDb Db
        {
            get
            {
                field ??= new PersistentVarsDb();
                return field;
            }
        }

        public static void Load()
        {
            _charScopeKey = ProfileManager.CurrentProfile.ServerName + ProfileManager.CurrentProfile.Username + ProfileManager.CurrentProfile.CharacterName;
            _accountScopeKey = ProfileManager.CurrentProfile.ServerName + ProfileManager.CurrentProfile.Username;
            _serverScopeKey = ProfileManager.CurrentProfile.ServerName;

            _ = Db; // Ensure the database and table exist before any reads/writes happen.
        }

        public static void Unload()
        {
            // SQLite handles flushing automatically - nothing to do here.
        }

        private static (string scope, string scopeKey) GetScopeKeyPair(LegionAPI.PersistentVar scope)
        {
            switch (scope)
            {
                case LegionAPI.PersistentVar.Char:
                    return (scope.ToString(), _charScopeKey);
                case LegionAPI.PersistentVar.Account:
                    return (scope.ToString(), _accountScopeKey);
                case LegionAPI.PersistentVar.Server:
                    return (scope.ToString(), _serverScopeKey);
                case LegionAPI.PersistentVar.Global:
                    return (scope.ToString(), GlobalScopeKey);
                default:
                    throw new ArgumentOutOfRangeException(nameof(scope), scope, null);
            }
        }

        public static string GetVar(LegionAPI.PersistentVar scope, string key, string defaultValue = "") => GetVarAsync(scope, key, defaultValue).Result;

        public static async Task<string> GetVarAsync(LegionAPI.PersistentVar scope, string key, string defaultValue = "")
        {
            (string scopeStr, string scopeKey) = GetScopeKeyPair(scope);

            try
            {
                string value = await Db.GetValueAsync(scopeStr, scopeKey, key).ConfigureAwait(false);
                return value ?? defaultValue;
            }
            catch (Exception ex)
            {
                Log.Error($"Error getting var '{key}': {ex.Message}");
                return defaultValue;
            }
        }

        public static void SaveVar(LegionAPI.PersistentVar scope, string key, string value) => SaveVarAsync(scope, key, value, null).ConfigureAwait(false);

        public static void SaveVar(LegionAPI.PersistentVar scope, string key, string value, Action onComplete) => SaveVarAsync(scope, key, value, onComplete).ConfigureAwait(false);

        public static async Task SaveVarAsync(LegionAPI.PersistentVar scope, string key, string value, Action onComplete = null)
        {
            (string scopeStr, string scopeKey) = GetScopeKeyPair(scope);

            try
            {
                await Db.SaveValueAsync(scopeStr, scopeKey, key, value).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Error($"Error saving var '{key}': {ex.Message}");
            }

            onComplete?.Invoke();
        }

        public static void DeleteVar(LegionAPI.PersistentVar scope, string key) => DeleteVarAsync(scope, key, null).ConfigureAwait(false);

        public static void DeleteVar(LegionAPI.PersistentVar scope, string key, Action onComplete) => DeleteVarAsync(scope, key, onComplete).ConfigureAwait(false);

        public static async Task DeleteVarAsync(LegionAPI.PersistentVar scope, string key, Action onComplete = null)
        {
            (string scopeStr, string scopeKey) = GetScopeKeyPair(scope);

            try
            {
                await Db.DeleteValueAsync(scopeStr, scopeKey, key).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Error($"Error deleting var '{key}': {ex.Message}");
            }

            onComplete?.Invoke();
        }

        public static Dictionary<string, string> GetAllVars(LegionAPI.PersistentVar scope) => GetAllVarsAsync(scope).Result;

        public static async Task<Dictionary<string, string>> GetAllVarsAsync(LegionAPI.PersistentVar scope)
        {
            (string scopeStr, string scopeKey) = GetScopeKeyPair(scope);

            try
            {
                return await Db.GetAllAsync(scopeStr, scopeKey).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Error($"Error getting all vars: {ex.Message}");
                return new Dictionary<string, string>();
            }
        }

        private sealed class PersistentVarsDb : SqliteDatabase
        {
            // Column names must stay exactly as-is (including "scope_key") - this table already
            // exists on disk for every existing install, and the base schema reconciliation only
            // adds/drops columns, it never renames or migrates data between them.
            private static readonly SqliteTableSchema Schema = new("persistent_vars",
                SqliteColumn.Str("id", primaryKey: true, notNull: true, def: "''"),
                SqliteColumn.Str("scope", notNull: true),
                SqliteColumn.Str("scope_key", notNull: true),
                SqliteColumn.Str("key", notNull: true),
                SqliteColumn.Str("value", notNull: true));

            // The schema constructor ensures the table; only the index and one-time id backfill remain.
            public PersistentVarsDb() : base(Schema, DB_FILE)
            {
                InitializeAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            }

            private async Task InitializeAsync()
            {
                // Index creation and the one-time id backfill cannot be expressed through the generic
                // row helpers, so they run as raw statements through the base class.
                await ExecuteAsync("""
                                   CREATE INDEX IF NOT EXISTS idx_scope_scopekey
                                   ON persistent_vars(scope, scope_key)
                                   """).ConfigureAwait(false);

                // Rows written before the "id" column existed (every install prior to this change)
                // need it backfilled once, otherwise they become orphaned and unreadable through the
                // id-based lookups below.
                await ExecuteAsync("""
                                   UPDATE persistent_vars SET id = scope || char(31) || scope_key || char(31) || key
                                   WHERE id IS NULL OR id = ''
                                   """).ConfigureAwait(false);

                // Migrated tables gained "id" via ALTER TABLE, which cannot declare it a primary key,
                // so it has no unique constraint there. The upsert's ON CONFLICT("id") requires one;
                // this index supplies it (fresh tables already declare id PRIMARY KEY). Created after
                // the backfill so every id is populated and unique before the constraint is enforced.
                await ExecuteAsync("""
                                   CREATE UNIQUE INDEX IF NOT EXISTS idx_persistent_vars_id
                                   ON persistent_vars(id)
                                   """).ConfigureAwait(false);
            }

            // The embedded character between segments is U+001F (unit separator, matches
            // char(31) in the backfill above) so scope/scope_key/key stay unambiguous when
            // concatenated into a single explicit key.
            private static string MakeId(string scope, string scopeKey, string key) => $"{scope}{scopeKey}{key}";

            public async Task<string> GetValueAsync(string scope, string scopeKey, string key)
            {
                SqliteRow? row = await GetFirstAsync(new SqliteRow { ["id"] = MakeId(scope, scopeKey, key) }).ConfigureAwait(false);
                return row?.Get<string>("value");
            }

            public Task<int> SaveValueAsync(string scope, string scopeKey, string key, string value) => AddOrUpdateAsync(new SqliteRow
            {
                ["id"] = MakeId(scope, scopeKey, key),
                ["scope"] = scope,
                ["scope_key"] = scopeKey,
                ["key"] = key,
                ["value"] = value ?? ""
            });

            public Task<int> DeleteValueAsync(string scope, string scopeKey, string key) => DeleteAsync(
                new SqliteRow { ["id"] = MakeId(scope, scopeKey, key) });

            public async Task<Dictionary<string, string>> GetAllAsync(string scope, string scopeKey)
            {
                IReadOnlyList<SqliteRow> rows = await GetAsync(new SqliteRow { ["scope"] = scope, ["scope_key"] = scopeKey }).ConfigureAwait(false);
                return rows.ToDictionary(r => r.Get<string>("key"), r => r.Get<string>("value"));
            }
        }
    }
}
