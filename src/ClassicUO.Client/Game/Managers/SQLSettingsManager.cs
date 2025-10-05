using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ClassicUO.Configuration;
using ClassicUO.LegionScripting;
using ClassicUO.Utility.Logging;
using Microsoft.Data.Sqlite;

namespace ClassicUO.Game.Managers
{
    public class SQLSettingsManager : IDisposable
    {
        private const string DB_FILE = "settings.db";
        private const int MAX_BACKUPS = 3;

        private readonly SemaphoreSlim _dbLock = new(1, 1);
        private readonly string _dataDir;
        private readonly string _dataPath;
        private readonly string _connectionString;
        private bool _disposed;

        public SQLSettingsManager()
        {
            _dataDir = Path.Combine(CUOEnviroment.ExecutablePath, "Data");
            _dataPath = Path.Combine(_dataDir, DB_FILE);

            _connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = _dataPath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Shared
            }.ToString();

            InitializeAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        private async Task InitializeAsync()
        {
            await _dbLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (!Directory.Exists(_dataDir))
                {
                    Directory.CreateDirectory(_dataDir);
                }

                // Create backups if the database exists
                if (File.Exists(_dataPath))
                {
                    CreateBackups();
                }

                // Create/open database and initialize table
                await using SqliteConnection connection = new(_connectionString);
                await connection.OpenAsync().ConfigureAwait(false);

                await using SqliteCommand createTableCmd = connection.CreateCommand();
                createTableCmd.CommandText = """
                                             CREATE TABLE IF NOT EXISTS settings (
                                                 scope TEXT NOT NULL,
                                                 name TEXT NOT NULL,
                                                 value TEXT NOT NULL,
                                                 PRIMARY KEY (scope, name)
                                             )
                                             """;
                await createTableCmd.ExecuteNonQueryAsync();

                // Create index for faster lookups
                await using SqliteCommand createIndexCmd = connection.CreateCommand();
                createIndexCmd.CommandText = """
                                             CREATE INDEX IF NOT EXISTS idx_scope
                                             ON settings(scope)
                                             """;
                await createIndexCmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Error($@"Error initializing SQLSettingsManager: {ex.Message}");
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
                    string backupPath = $"{_dataPath}.{i}";

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
                        string nextBackupPath = $"{_dataPath}.{i + 1}";
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
                string firstBackupPath = $"{_dataPath}.1";
                if (File.Exists(firstBackupPath))
                {
                    File.Delete(firstBackupPath);
                }
                File.Copy(_dataPath, firstBackupPath);
            }
            catch (Exception ex)
            {
                Log.Error($@"Warning: Failed to create settings database backups: {ex.Message}");
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
            return GetAsync(scope, name, defaultValue).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public string Get(SettingsScope scope, string name, string defaultValue, Action<string> onComplete)
        {
            return GetAsync(scope, name, defaultValue, onComplete).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public async Task<string> GetAsync(SettingsScope scope, string name, string defaultValue = "")
        {
            return await GetAsync(scope, name, defaultValue, null);
        }

        public async Task<string> GetAsync(SettingsScope scope, string name, string defaultValue, Action<string> onComplete)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SQLSettingsManager));

            string scopeKey = GetScopeKey(scope);

            await _dbLock.WaitAsync().ConfigureAwait(false);
            try
            {
                await using SqliteConnection connection = new(_connectionString);
                await connection.OpenAsync().ConfigureAwait(false);

                await using SqliteCommand cmd = connection.CreateCommand();
                cmd.CommandText = """
                                  SELECT value FROM settings
                                  WHERE scope = $scope AND name = $name
                                  """;
                cmd.Parameters.AddWithValue("$scope", scopeKey);
                cmd.Parameters.AddWithValue("$name", name);

                object result = await cmd.ExecuteScalarAsync().ConfigureAwait(false);
                string value = result?.ToString() ?? defaultValue;

                onComplete?.Invoke(value);

                return value;
            }
            catch (Exception ex)
            {
                Log.Error($@"Error getting setting '{name}' from scope '{scopeKey}': {ex.Message}");
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

            string scopeKey = GetScopeKey(scope);

            await _dbLock.WaitAsync().ConfigureAwait(false);
            try
            {
                await using SqliteConnection connection = new(_connectionString);
                await connection.OpenAsync().ConfigureAwait(false);

                SqliteCommand cmd = connection.CreateCommand();
                cmd.CommandText = @"
                        INSERT OR REPLACE INTO settings (scope, name, value)
                        VALUES ($scope, $name, $value)";
                cmd.Parameters.AddWithValue("$scope", scopeKey);
                cmd.Parameters.AddWithValue("$name", name);
                cmd.Parameters.AddWithValue("$value", value ?? string.Empty);

                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Error($@"Error setting '{name}' in scope '{scopeKey}': {ex.Message}");
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

            string scopeKey = GetScopeKey(scope);
            Dictionary<string, string> result = new();

            await _dbLock.WaitAsync();
            try
            {
                await using SqliteConnection connection = new(_connectionString);
                await connection.OpenAsync();

                SqliteCommand cmd = connection.CreateCommand();
                cmd.CommandText = @"
                        SELECT name, value FROM settings
                        WHERE scope = $scope";
                cmd.Parameters.AddWithValue("$scope", scopeKey);

                await using SqliteDataReader reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    result[reader.GetString(0)] = reader.GetString(1);
                }

                return result;
            }
            catch (Exception ex)
            {
                Log.Error($@"Error getting all settings from scope '{scopeKey}': {ex.Message}");
                return result;
            }
            finally
            {
                _dbLock.Release();
            }
        }

        public Dictionary<string, string> GetAll(SettingsScope scope)
        {
            return GetAllAsync(scope).ConfigureAwait(false).GetAwaiter().GetResult();
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
