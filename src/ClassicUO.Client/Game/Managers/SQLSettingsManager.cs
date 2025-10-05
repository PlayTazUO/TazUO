using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ClassicUO.Configuration;
using ClassicUO.LegionScripting;
using Microsoft.Data.Sqlite;

namespace ClassicUO.Game.Managers
{
    public class SQLSettingsManager : IDisposable
    {
        private const string DB_FILE = "settings.db";
        private const int MAX_BACKUPS = 3;

        private readonly SemaphoreSlim _dbLock = new SemaphoreSlim(1, 1);
        private readonly string _dataPath;
        private readonly string _connectionString;
        private bool _disposed = false;

        public SQLSettingsManager()
        {
            var dataDir = Path.Combine(CUOEnviroment.ExecutablePath, "Data");
            _dataPath = Path.Combine(dataDir, DB_FILE);

            _connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = _dataPath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Shared
            }.ToString();

            InitializeAsync().Wait();
        }

        private async Task InitializeAsync()
        {
            await _dbLock.WaitAsync();
            try
            {
                // Ensure the Data directory exists
                var dataDir = Path.GetDirectoryName(_dataPath);
                if (!Directory.Exists(dataDir))
                {
                    Directory.CreateDirectory(dataDir);
                }

                // Create backups if the database exists
                if (File.Exists(_dataPath))
                {
                    CreateBackups();
                }

                // Create/open database and initialize table
                using (var connection = new SqliteConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var createTableCmd = connection.CreateCommand();
                    createTableCmd.CommandText = @"
                        CREATE TABLE IF NOT EXISTS settings (
                            scope TEXT NOT NULL,
                            name TEXT NOT NULL,
                            value TEXT NOT NULL,
                            PRIMARY KEY (scope, name)
                        )";
                    await createTableCmd.ExecuteNonQueryAsync();

                    // Create index for faster lookups
                    var createIndexCmd = connection.CreateCommand();
                    createIndexCmd.CommandText = @"
                        CREATE INDEX IF NOT EXISTS idx_scope
                        ON settings(scope)";
                    await createIndexCmd.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing SQLSettingsManager: {ex.Message}");
                throw;
            }
            finally
            {
                _dbLock.Release();
            }
        }

        private void CreateBackups()
        {
            try
            {
                // Rotate existing backups: .3 -> delete, .2 -> .3, .1 -> .2
                for (int i = MAX_BACKUPS; i > 0; i--)
                {
                    var backupPath = $"{_dataPath}.{i}";

                    if (i == MAX_BACKUPS)
                    {
                        // Delete oldest backup
                        if (File.Exists(backupPath))
                        {
                            File.Delete(backupPath);
                        }
                    }
                    else
                    {
                        // Rotate backup to next number
                        var nextBackupPath = $"{_dataPath}.{i + 1}";
                        if (File.Exists(backupPath))
                        {
                            if (File.Exists(nextBackupPath))
                            {
                                File.Delete(nextBackupPath);
                            }
                            File.Move(backupPath, nextBackupPath);
                        }
                    }
                }

                // Create new .1 backup from current database
                var firstBackupPath = $"{_dataPath}.1";
                if (File.Exists(firstBackupPath))
                {
                    File.Delete(firstBackupPath);
                }
                File.Copy(_dataPath, firstBackupPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to create settings database backups: {ex.Message}");
            }
        }

        private string GetScopeKey(SettingsScope scope)
        {
            switch (scope)
            {
                case SettingsScope.Char:
                    return ProfileManager.CurrentProfile != null
                        ? $"{ProfileManager.CurrentProfile.ServerName}_{ProfileManager.CurrentProfile.Username}_{ProfileManager.CurrentProfile.CharacterName}"
                        : "CHAR";
                case SettingsScope.Account:
                    return ProfileManager.CurrentProfile != null
                        ? $"{ProfileManager.CurrentProfile.ServerName}_{ProfileManager.CurrentProfile.Username}"
                        : "ACCOUNT";
                case SettingsScope.Server:
                    return ProfileManager.CurrentProfile?.ServerName ?? "SERVER";
                case SettingsScope.Global:
                    return "GLOBAL";
                default:
                    throw new ArgumentOutOfRangeException(nameof(scope), scope, null);
            }
        }

        public string Get(SettingsScope scope, string name, string defaultValue = "")
        {
            return GetAsync(scope, name, defaultValue).Result;
        }

        public string Get(SettingsScope scope, string name, string defaultValue, Action<string> onComplete)
        {
            return GetAsync(scope, name, defaultValue, onComplete).Result;
        }

        public async Task<string> GetAsync(SettingsScope scope, string name, string defaultValue = "")
        {
            return await GetAsync(scope, name, defaultValue, null);
        }

        public async Task<string> GetAsync(SettingsScope scope, string name, string defaultValue, Action<string> onComplete)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SQLSettingsManager));

            var scopeKey = GetScopeKey(scope);

            await _dbLock.WaitAsync();
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var cmd = connection.CreateCommand();
                    cmd.CommandText = @"
                        SELECT value FROM settings
                        WHERE scope = $scope AND name = $name";
                    cmd.Parameters.AddWithValue("$scope", scopeKey);
                    cmd.Parameters.AddWithValue("$name", name);

                    var result = await cmd.ExecuteScalarAsync();
                    var value = result?.ToString() ?? defaultValue;

                    onComplete?.Invoke(value);

                    return value;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting setting '{name}' from scope '{scopeKey}': {ex.Message}");
                onComplete?.Invoke(defaultValue);
                return defaultValue;
            }
            finally
            {
                _dbLock.Release();
            }
        }

        public void Set(SettingsScope scope, string name, string value)
        {
            SetAsync(scope, name, value).ConfigureAwait(false);
        }

        public async Task SetAsync(SettingsScope scope, string name, string value)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SQLSettingsManager));

            var scopeKey = GetScopeKey(scope);

            await _dbLock.WaitAsync();
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var cmd = connection.CreateCommand();
                    cmd.CommandText = @"
                        INSERT OR REPLACE INTO settings (scope, name, value)
                        VALUES ($scope, $name, $value)";
                    cmd.Parameters.AddWithValue("$scope", scopeKey);
                    cmd.Parameters.AddWithValue("$name", name);
                    cmd.Parameters.AddWithValue("$value", value ?? string.Empty);

                    await cmd.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting '{name}' in scope '{scopeKey}': {ex.Message}");
            }
            finally
            {
                _dbLock.Release();
            }
        }

        public async Task<Dictionary<string, string>> GetAllAsync(SettingsScope scope)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SQLSettingsManager));

            var scopeKey = GetScopeKey(scope);
            var result = new Dictionary<string, string>();

            await _dbLock.WaitAsync();
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var cmd = connection.CreateCommand();
                    cmd.CommandText = @"
                        SELECT name, value FROM settings
                        WHERE scope = $scope";
                    cmd.Parameters.AddWithValue("$scope", scopeKey);

                    using (var reader = await cmd.ExecuteReaderAsync())
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
                Console.WriteLine($"Error getting all settings from scope '{scopeKey}': {ex.Message}");
                return result;
            }
            finally
            {
                _dbLock.Release();
            }
        }

        public Dictionary<string, string> GetAll(SettingsScope scope)
        {
            return GetAllAsync(scope).Result;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _dbLock.Wait();
            try
            {
                _disposed = true;
            }
            finally
            {
                _dbLock.Release();
                _dbLock.Dispose();
            }
        }
    }
}

public enum SettingsScope
{
    Char,
    Account,
    Server,
    Global
}
