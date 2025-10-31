using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ClassicUO.Configuration;
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
                await createTableCmd.ExecuteNonQueryAsync().ConfigureAwait(false);

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

        private T ParseValue<T>(string value, T defaultValue)
        {
            if (string.IsNullOrEmpty(value))
                return defaultValue;

            try
            {
                Type type = typeof(T);

                if (type == typeof(string))
                    return (T)(object)value;

                if (type == typeof(bool))
                    return (T)(object)bool.Parse(value);

                if (type == typeof(int))
                    return (T)(object)int.Parse(value);

                if (type == typeof(uint))
                    return (T)(object)uint.Parse(value);

                if (type == typeof(short))
                    return (T)(object)short.Parse(value);

                if (type == typeof(ushort))
                    return (T)(object)ushort.Parse(value);

                if (type == typeof(float))
                    return (T)(object)float.Parse(value);

                if (type == typeof(double))
                    return (T)(object)double.Parse(value);

                return defaultValue;
            }
            catch
            {
                return defaultValue;
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

        /// <summary>
        /// Synchronously retrieves a setting value from the database.
        /// </summary>
        /// <param name="scope">The settings scope (Char, Account, Server, or Global)</param>
        /// <param name="name">The name of the setting</param>
        /// <param name="defaultValue">The default value to return if the setting doesn't exist</param>
        /// <returns>The setting value or the default value if not found</returns>
        public string Get(SettingsScope scope, string name, string defaultValue = "") => GetAsync(scope, name, defaultValue).ConfigureAwait(false).GetAwaiter().GetResult();

        /// <summary>
        /// Synchronously retrieves a strongly-typed setting value from the database.
        /// </summary>
        /// <typeparam name="T">The type to parse the setting value to (string, int, uint, short, ushort, bool)</typeparam>
        /// <param name="scope">The settings scope (Char, Account, Server, or Global)</param>
        /// <param name="name">The name of the setting</param>
        /// <param name="defaultValue">The default value to return if the setting doesn't exist or parsing fails</param>
        /// <returns>The parsed setting value or the default value if not found or parsing fails</returns>
        public T Get<T>(SettingsScope scope, string name, T defaultValue = default) => GetAsync(scope, name, defaultValue).ConfigureAwait(false).GetAwaiter().GetResult();

        /// <summary>
        /// Asynchronously retrieves a setting value from the database.
        /// </summary>
        /// <param name="scope">The settings scope (Char, Account, Server, or Global)</param>
        /// <param name="name">The name of the setting</param>
        /// <param name="defaultValue">The default value to return if the setting doesn't exist</param>
        /// <returns>A task that represents the asynchronous operation, containing the setting value or the default value if not found</returns>
        public async Task<string> GetAsync(SettingsScope scope, string name, string defaultValue = "") => await GetAsync(scope, name, defaultValue, null);

        /// <summary>
        /// Asynchronously retrieves a strongly-typed setting value from the database.
        /// </summary>
        /// <typeparam name="T">The type to parse the setting value to (string, int, uint, short, ushort, bool)</typeparam>
        /// <param name="scope">The settings scope (Char, Account, Server, or Global)</param>
        /// <param name="name">The name of the setting</param>
        /// <param name="defaultValue">The default value to return if the setting doesn't exist or parsing fails</param>
        /// <returns>A task that represents the asynchronous operation, containing the parsed setting value or the default value if not found or parsing fails</returns>
        public async Task<T> GetAsync<T>(SettingsScope scope, string name, T defaultValue = default)
        {
            string stringValue = await GetAsync(scope, name, defaultValue?.ToString() ?? string.Empty);
            return ParseValue(stringValue, defaultValue);
        }

        /// <summary>
        /// Asynchronously retrieves a strongly-typed setting value and invokes a callback on the main thread.
        /// </summary>
        /// <typeparam name="T">The type to parse the setting value to (string, int, uint, short, ushort, bool)</typeparam>
        /// <param name="scope">The settings scope (Char, Account, Server, or Global)</param>
        /// <param name="name">The name of the setting</param>
        /// <param name="defaultValue">The default value to return if the setting doesn't exist or parsing fails</param>
        /// <param name="onComplete">Callback invoked with the parsed result on the main thread</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public async Task GetAsyncOnMainThread<T>(SettingsScope scope, string name, T defaultValue, Action<T> onComplete)
        {
            T result = await GetAsync(scope, name, defaultValue);
            if (onComplete != null)
            {
                MainThreadQueue.EnqueueAction(() => onComplete(result));
            }
        }

        /// <summary>
        /// Asynchronously retrieves a setting value without pausing the thread to wait for the results.
        /// </summary>
        /// <param name="scope">The settings scope (Char, Account, Server, or Global)</param>
        /// <param name="name">The name of the setting</param>
        /// <param name="defaultValue">The default value to return if the setting doesn't exist</param>
        /// <param name="onComplete">Callback invoked with the result. WARNING: Does not run on the main thread! Use MainThreadQueue if needed.</param>
        /// <returns>A task that represents the asynchronous operation, containing the setting value or the default value if not found</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the manager has been disposed</exception>
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

        /// <summary>
        /// Synchronously sets a setting value in the database.
        /// </summary>
        /// <param name="scope">The settings scope (Char, Account, Server, or Global)</param>
        /// <param name="name">The name of the setting</param>
        /// <param name="value">The value to store</param>
        public void Set(SettingsScope scope, string name, string value) => SetAsync(scope, name, value).ConfigureAwait(false).GetAwaiter().GetResult();

        /// <summary>
        /// Synchronously sets a strongly-typed setting value in the database.
        /// </summary>
        /// <typeparam name="T">The type of the value (string, int, uint, short, ushort, bool)</typeparam>
        /// <param name="scope">The settings scope (Char, Account, Server, or Global)</param>
        /// <param name="name">The name of the setting</param>
        /// <param name="value">The value to store</param>
        public void Set<T>(SettingsScope scope, string name, T value) => SetAsync(scope, name, value).ConfigureAwait(false).GetAwaiter().GetResult();

        /// <summary>
        /// Asynchronously sets a setting value in the database, inserting or replacing as needed.
        /// </summary>
        /// <param name="scope">The settings scope (Char, Account, Server, or Global)</param>
        /// <param name="name">The name of the setting</param>
        /// <param name="value">The value to store</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the manager has been disposed</exception>
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

                await using SqliteCommand cmd = connection.CreateCommand();
                cmd.CommandText = """
                                  INSERT OR REPLACE INTO settings (scope, name, value)
                                  VALUES ($scope, $name, $value)
                                  """;
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

        /// <summary>
        /// Asynchronously sets a strongly-typed setting value in the database, inserting or replacing as needed.
        /// </summary>
        /// <typeparam name="T">The type of the value (string, int, uint, short, ushort, bool)</typeparam>
        /// <param name="scope">The settings scope (Char, Account, Server, or Global)</param>
        /// <param name="name">The name of the setting</param>
        /// <param name="value">The value to store</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the manager has been disposed</exception>
        public async Task SetAsync<T>(SettingsScope scope, string name, T value)
        {
            string stringValue = value?.ToString() ?? string.Empty;
            await SetAsync(scope, name, stringValue);
        }

        /// <summary>
        /// Asynchronously retrieves all settings for a given scope.
        /// </summary>
        /// <param name="scope">The settings scope (Char, Account, Server, or Global)</param>
        /// <returns>A task that represents the asynchronous operation, containing a dictionary of all setting name-value pairs for the scope</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the manager has been disposed</exception>
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

                await using SqliteCommand cmd = connection.CreateCommand();
                cmd.CommandText = """
                                  SELECT name, value FROM settings
                                  WHERE scope = $scope
                                  """;
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

        /// <summary>
        /// Synchronously retrieves all settings for a given scope.
        /// </summary>
        /// <param name="scope">The settings scope (Char, Account, Server, or Global)</param>
        /// <returns>A dictionary of all setting name-value pairs for the scope</returns>
        public Dictionary<string, string> GetAll(SettingsScope scope) => GetAllAsync(scope).ConfigureAwait(false).GetAwaiter().GetResult();

        /// <summary>
        /// Releases resources used by the SQLSettingsManager.
        /// </summary>
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
