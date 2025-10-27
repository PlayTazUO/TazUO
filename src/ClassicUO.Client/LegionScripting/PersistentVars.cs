using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ClassicUO.Configuration;
using Microsoft.Data.Sqlite;

namespace ClassicUO.LegionScripting
{
    public static class PersistentVars
    {
        private const string DB_FILE = "legionvars.db";
        private const string OLD_DATA_FILE = "legionvars.dat";
        private const string GlobalScopeKey = "GLOBAL";
        private const char SEPARATOR = '\t';

        private static string _charScopeKey = "";
        private static string _accountScopeKey = "";
        private static string _serverScopeKey = "";

        private static readonly SemaphoreSlim _dbLock = new SemaphoreSlim(1, 1);
        private static string DataPath => Path.Combine(CUOEnviroment.ExecutablePath, "Data", DB_FILE);
        private static string OldDataPath => Path.Combine(CUOEnviroment.ExecutablePath, "Data", OLD_DATA_FILE);

        private static string ConnectionString => new SqliteConnectionStringBuilder
        {
            DataSource = DataPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        }.ToString();

        public static void Load()
        {
            _charScopeKey = ProfileManager.CurrentProfile.ServerName + ProfileManager.CurrentProfile.Username + ProfileManager.CurrentProfile.CharacterName;
            _accountScopeKey = ProfileManager.CurrentProfile.ServerName + ProfileManager.CurrentProfile.Username;
            _serverScopeKey = ProfileManager.CurrentProfile.ServerName;

            InitializeDatabaseAsync().Wait();
        }

        private static async Task InitializeDatabaseAsync()
        {
            await _dbLock.WaitAsync();
            try
            {
                // Ensure the Data directory exists
                string dataDir = Path.GetDirectoryName(DataPath);
                if (!Directory.Exists(dataDir))
                {
                    Directory.CreateDirectory(dataDir);
                }

                using (var connection = new SqliteConnection(ConnectionString))
                {
                    await connection.OpenAsync();

                    SqliteCommand createTableCmd = connection.CreateCommand();
                    createTableCmd.CommandText = @"
                        CREATE TABLE IF NOT EXISTS persistent_vars (
                            scope TEXT NOT NULL,
                            scope_key TEXT NOT NULL,
                            key TEXT NOT NULL,
                            value TEXT NOT NULL,
                            PRIMARY KEY (scope, scope_key, key)
                        )";
                    await createTableCmd.ExecuteNonQueryAsync();

                    // Create index for faster lookups
                    SqliteCommand createIndexCmd = connection.CreateCommand();
                    createIndexCmd.CommandText = @"
                        CREATE INDEX IF NOT EXISTS idx_scope_scopekey
                        ON persistent_vars(scope, scope_key)";
                    await createIndexCmd.ExecuteNonQueryAsync();
                }

                // Migrate old data if exists
                if (File.Exists(OldDataPath))
                {
                    await MigrateOldDataAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to initialize persistent vars database: {ex.Message}");
            }
            finally
            {
                _dbLock.Release();
            }
        }

        private static async Task MigrateOldDataAsync()
        {
            try
            {
                string[] lines = await File.ReadAllLinesAsync(OldDataPath);
                int migratedCount = 0;

                using (var connection = new SqliteConnection(ConnectionString))
                {
                    await connection.OpenAsync();

                    using (SqliteTransaction transaction = connection.BeginTransaction())
                    {
                        SqliteCommand insertCmd = connection.CreateCommand();
                        insertCmd.Transaction = transaction;
                        insertCmd.CommandText = @"
                            INSERT OR REPLACE INTO persistent_vars (scope, scope_key, key, value)
                            VALUES ($scope, $scope_key, $key, $value)";

                        SqliteParameter scopeParam = insertCmd.Parameters.Add("$scope", SqliteType.Text);
                        SqliteParameter scopeKeyParam = insertCmd.Parameters.Add("$scope_key", SqliteType.Text);
                        SqliteParameter keyParam = insertCmd.Parameters.Add("$key", SqliteType.Text);
                        SqliteParameter valueParam = insertCmd.Parameters.Add("$value", SqliteType.Text);

                        foreach (string line in lines)
                        {
                            if (string.IsNullOrEmpty(line)) continue;

                            string[] parts = line.Split(SEPARATOR);
                            if (parts.Length >= 4)
                            {
                                scopeParam.Value = parts[0];
                                scopeKeyParam.Value = parts[1];
                                keyParam.Value = parts[2];
                                string value = parts.Length > 4 ? string.Join(SEPARATOR.ToString(), parts, 3, parts.Length - 3) : parts[3];
                                valueParam.Value = UnescapeValue(value);

                                await insertCmd.ExecuteNonQueryAsync();
                                migratedCount++;
                            }
                        }

                        transaction.Commit();
                    }
                }

                // Backup old file and delete
                string backupPath = OldDataPath + ".bak";
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }
                File.Move(OldDataPath, backupPath);

                Console.WriteLine($"Migrated {migratedCount} persistent vars from old format to SQLite");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to migrate old persistent vars data: {ex.Message}");
            }
        }

        private static string UnescapeValue(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;

            return value.Replace("\\r", "\r")
                       .Replace("\\n", "\n")
                       .Replace("\\t", "\t")
                       .Replace("\\\\", "\\");
        }

        private static (API.PersistentVar scope, string scopeKey) GetScopeKeyPair(API.PersistentVar scope)
        {
            switch (scope)
            {
                case API.PersistentVar.Char:
                    return (scope, _charScopeKey);
                case API.PersistentVar.Account:
                    return (scope, _accountScopeKey);
                case API.PersistentVar.Server:
                    return (scope, _serverScopeKey);
                case API.PersistentVar.Global:
                    return (scope, GlobalScopeKey);
                default:
                    throw new ArgumentOutOfRangeException(nameof(scope), scope, null);
            }
        }

        public static string GetVar(API.PersistentVar scope, string key, string defaultValue = "") => GetVarAsync(scope, key, defaultValue).Result;

        public static async Task<string> GetVarAsync(API.PersistentVar scope, string key, string defaultValue = "")
        {
            (API.PersistentVar s, string scopeKey) = GetScopeKeyPair(scope);
            string scopeStr = s.ToString();

            await _dbLock.WaitAsync();
            try
            {
                using (var connection = new SqliteConnection(ConnectionString))
                {
                    await connection.OpenAsync();

                    SqliteCommand cmd = connection.CreateCommand();
                    cmd.CommandText = @"
                        SELECT value FROM persistent_vars
                        WHERE scope = $scope AND scope_key = $scope_key AND key = $key";
                    cmd.Parameters.AddWithValue("$scope", scopeStr);
                    cmd.Parameters.AddWithValue("$scope_key", scopeKey);
                    cmd.Parameters.AddWithValue("$key", key);

                    object result = await cmd.ExecuteScalarAsync();
                    return result?.ToString() ?? defaultValue;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting var '{key}': {ex.Message}");
                return defaultValue;
            }
            finally
            {
                _dbLock.Release();
            }
        }

        public static void SaveVar(API.PersistentVar scope, string key, string value) => SaveVarAsync(scope, key, value, null).ConfigureAwait(false);

        public static void SaveVar(API.PersistentVar scope, string key, string value, Action onComplete) => SaveVarAsync(scope, key, value, onComplete).ConfigureAwait(false);

        public static async Task SaveVarAsync(API.PersistentVar scope, string key, string value, Action onComplete = null)
        {
            (API.PersistentVar s, string scopeKey) = GetScopeKeyPair(scope);
            string scopeStr = s.ToString();

            await _dbLock.WaitAsync();
            try
            {
                using (var connection = new SqliteConnection(ConnectionString))
                {
                    await connection.OpenAsync();

                    SqliteCommand cmd = connection.CreateCommand();
                    cmd.CommandText = @"
                        INSERT OR REPLACE INTO persistent_vars (scope, scope_key, key, value)
                        VALUES ($scope, $scope_key, $key, $value)";
                    cmd.Parameters.AddWithValue("$scope", scopeStr);
                    cmd.Parameters.AddWithValue("$scope_key", scopeKey);
                    cmd.Parameters.AddWithValue("$key", key);
                    cmd.Parameters.AddWithValue("$value", value);

                    await cmd.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving var '{key}': {ex.Message}");
            }
            finally
            {
                _dbLock.Release();
                onComplete?.Invoke();
            }
        }

        public static void DeleteVar(API.PersistentVar scope, string key) => DeleteVarAsync(scope, key, null).ConfigureAwait(false);

        public static void DeleteVar(API.PersistentVar scope, string key, Action onComplete) => DeleteVarAsync(scope, key, onComplete).ConfigureAwait(false);

        public static async Task DeleteVarAsync(API.PersistentVar scope, string key, Action onComplete = null)
        {
            (API.PersistentVar s, string scopeKey) = GetScopeKeyPair(scope);
            string scopeStr = s.ToString();

            await _dbLock.WaitAsync();
            try
            {
                using (var connection = new SqliteConnection(ConnectionString))
                {
                    await connection.OpenAsync();

                    SqliteCommand cmd = connection.CreateCommand();
                    cmd.CommandText = @"
                        DELETE FROM persistent_vars
                        WHERE scope = $scope AND scope_key = $scope_key AND key = $key";
                    cmd.Parameters.AddWithValue("$scope", scopeStr);
                    cmd.Parameters.AddWithValue("$scope_key", scopeKey);
                    cmd.Parameters.AddWithValue("$key", key);

                    await cmd.ExecuteNonQueryAsync();
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting var '{key}': {ex.Message}");
            }
            finally
            {
                _dbLock.Release();
                onComplete?.Invoke();
            }
        }

        public static Dictionary<string, string> GetAllVars(API.PersistentVar scope) => GetAllVarsAsync(scope).Result;

        public static void Unload()
        {
            // SQLite handles this automatically - no need to flush
            // Just ensure any pending operations complete
            _dbLock.Wait();
            _dbLock.Release();
        }

        public static async Task<Dictionary<string, string>> GetAllVarsAsync(API.PersistentVar scope)
        {
            (API.PersistentVar s, string scopeKey) = GetScopeKeyPair(scope);
            string scopeStr = s.ToString();
            var result = new Dictionary<string, string>();

            await _dbLock.WaitAsync();
            try
            {
                using (var connection = new SqliteConnection(ConnectionString))
                {
                    await connection.OpenAsync();

                    SqliteCommand cmd = connection.CreateCommand();
                    cmd.CommandText = @"
                        SELECT key, value FROM persistent_vars
                        WHERE scope = $scope AND scope_key = $scope_key";
                    cmd.Parameters.AddWithValue("$scope", scopeStr);
                    cmd.Parameters.AddWithValue("$scope_key", scopeKey);

                    using (SqliteDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            result[reader.GetString(0)] = reader.GetString(1);
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting all vars: {ex.Message}");
                return result;
            }
            finally
            {
                _dbLock.Release();
            }
        }
    }
}
