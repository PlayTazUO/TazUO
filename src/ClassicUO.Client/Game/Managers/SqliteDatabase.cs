using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ClassicUO.Utility.Logging;
using Microsoft.Data.Sqlite;

namespace ClassicUO.Game.Managers
{
    /// <summary>
    /// Base class that removes the boilerplate of working with a SQLite database. Subclass it, pass a
    /// database file name to the constructor, then use the declarative schema helpers
    /// (<see cref="EnsureTableAsync"/>, <see cref="EnsureColumnsAsync"/>, <see cref="EnsureIndexAsync"/>)
    /// and the row write helpers (<see cref="AddRowAsync"/>, <see cref="AddOrUpdateAsync"/>).
    /// <para>
    /// All operations are serialized behind a <see cref="SemaphoreSlim"/> and open a short-lived
    /// connection per call, matching the conventions used by the other SQLite managers in the project.
    /// Values are always bound as SQL parameters, never interpolated.
    /// </para>
    /// <example>
    /// <code>
    /// public class MyThingDb : SqliteDatabase
    /// {
    ///     public MyThingDb() : base("mything.db")
    ///     {
    ///         EnsureTableAsync("things",
    ///             SqliteColumn.Int("id", primaryKey: true),
    ///             SqliteColumn.Str("name", notNull: true, def: "''")
    ///         ).GetAwaiter().GetResult();
    ///     }
    ///
    ///     public Task SaveAsync(int id, string name) =>
    ///         AddOrUpdateAsync("things", new SqliteRow { { "id", id }, { "name", name } });
    /// }
    /// </code>
    /// </example>
    /// </summary>
    public abstract class SqliteDatabase : IDisposable
    {
        private const int MAX_BACKUPS = 3;

        private readonly SemaphoreSlim _dbLock = new(1, 1);

        /// <summary>The directory that contains the database file.</summary>
        protected string DataDirectory { get; }

        /// <summary>The full path to the database file on disk.</summary>
        protected string DatabasePath { get; }

        /// <summary>The connection string used to open connections to this database.</summary>
        protected string ConnectionString { get; }

        private bool _disposed;

        /// <summary>
        /// Creates the base database. The containing directory is created if it does not exist.
        /// </summary>
        /// <param name="dbFileName">The database file name, e.g. <c>"mything.db"</c>.</param>
        /// <param name="dataDirectory">
        /// The directory to place the database in. Defaults to the shared
        /// <c>{ExecutablePath}/Data</c> directory used by the other managers. Provide an explicit
        /// directory (e.g. a temp path) to make a subclass unit-testable.
        /// </param>
        protected SqliteDatabase(string dbFileName, string dataDirectory = null)
        {
            DataDirectory = dataDirectory ?? Path.Combine(CUOEnviroment.ExecutablePath, "Data");
            DatabasePath = Path.Combine(DataDirectory, dbFileName);

            if (!Directory.Exists(DataDirectory))
                Directory.CreateDirectory(DataDirectory);

            ConnectionString = new SqliteConnectionStringBuilder
            {
                DataSource = DatabasePath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Shared
            }.ToString();
        }

        private async Task<SqliteConnection> OpenConnectionAsync()
        {
            SqliteConnection connection = new(ConnectionString);
            await connection.OpenAsync().ConfigureAwait(false);
            return connection;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);
        }

        /// <summary>
        /// Ensures a table exists, creating it (<c>CREATE TABLE IF NOT EXISTS</c>) with the given
        /// columns if necessary. If more than one column is flagged as a primary key, a composite
        /// table-level PRIMARY KEY is emitted.
        /// </summary>
        /// <param name="tableName">The table name.</param>
        /// <param name="columns">The columns that define the table.</param>
        public async Task EnsureTableAsync(string tableName, params SqliteColumn[] columns)
        {
            ThrowIfDisposed();

            if (columns == null || columns.Length == 0)
                throw new ArgumentException("At least one column is required.", nameof(columns));

            List<string> primaryKeys = new();
            foreach (SqliteColumn c in columns)
            {
                if (c.PrimaryKey)
                    primaryKeys.Add(c.Name);
            }

            // Inline "PRIMARY KEY" only works for a single-column key; otherwise use a table constraint.
            bool compositeKey = primaryKeys.Count > 1;

            StringBuilder sb = new();
            sb.Append("CREATE TABLE IF NOT EXISTS ");
            sb.Append(tableName);
            sb.Append(" (");

            for (int i = 0; i < columns.Length; i++)
            {
                if (i > 0)
                    sb.Append(", ");

                sb.Append(columns[i].ToDefinition(includePrimaryKey: !compositeKey));
            }

            if (compositeKey)
            {
                sb.Append(", PRIMARY KEY (");
                sb.Append(string.Join(", ", primaryKeys));
                sb.Append(')');
            }

            sb.Append(')');

            await ExecuteNonQueryAsync(sb.ToString()).ConfigureAwait(false);
        }

        /// <summary>
        /// Ensures each given column exists on a table, adding any that are missing via
        /// <c>ALTER TABLE ... ADD COLUMN</c>. Columns that already exist are skipped silently, so this
        /// is safe to call on every startup for schema migrations.
        /// </summary>
        /// <param name="tableName">The table to add columns to.</param>
        /// <param name="columns">The columns to ensure exist.</param>
        public async Task EnsureColumnsAsync(string tableName, params SqliteColumn[] columns)
        {
            ThrowIfDisposed();

            if (columns == null || columns.Length == 0)
                return;

            await _dbLock.WaitAsync().ConfigureAwait(false);
            try
            {
                await using SqliteConnection connection = await OpenConnectionAsync().ConfigureAwait(false);

                foreach (SqliteColumn column in columns)
                {
                    try
                    {
                        await using SqliteCommand cmd = connection.CreateCommand();
                        // A primary key cannot be added via ALTER TABLE, so never inline it here.
                        cmd.CommandText = $"ALTER TABLE {tableName} ADD COLUMN {column.ToDefinition(includePrimaryKey: false)}";
                        await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                    }
                    catch (SqliteException)
                    {
                        // Column already exists - ignore, this is the expected migration path.
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($@"Error ensuring columns on table '{tableName}': {ex.Message}");
            }
            finally
            {
                _dbLock.Release();
            }
        }

        /// <summary>
        /// Ensures an index exists (<c>CREATE INDEX IF NOT EXISTS</c>) over the given columns.
        /// </summary>
        /// <param name="indexName">The index name.</param>
        /// <param name="tableName">The table the index is on.</param>
        /// <param name="columns">The columns to index.</param>
        public async Task EnsureIndexAsync(string indexName, string tableName, params string[] columns)
        {
            ThrowIfDisposed();

            if (columns == null || columns.Length == 0)
                throw new ArgumentException("At least one column is required.", nameof(columns));

            string sql = $"CREATE INDEX IF NOT EXISTS {indexName} ON {tableName} ({string.Join(", ", columns)})";
            await ExecuteNonQueryAsync(sql).ConfigureAwait(false);
        }

        /// <summary>
        /// Inserts a new row (<c>INSERT INTO</c>) built from the given <see cref="SqliteRow"/>.
        /// </summary>
        /// <param name="tableName">The table to insert into.</param>
        /// <param name="row">The column/value pairs to insert.</param>
        public Task AddRowAsync(string tableName, SqliteRow row) => WriteRowAsync(tableName, row, orReplace: false);

        /// <summary>
        /// Inserts or replaces a row (<c>INSERT OR REPLACE INTO</c>) built from the given
        /// <see cref="SqliteRow"/>. Requires the table to have a PRIMARY KEY or UNIQUE constraint for
        /// the replace to take effect.
        /// </summary>
        /// <param name="tableName">The table to write to.</param>
        /// <param name="row">The column/value pairs to write.</param>
        public Task AddOrUpdateAsync(string tableName, SqliteRow row) => WriteRowAsync(tableName, row, orReplace: true);

        private async Task WriteRowAsync(string tableName, SqliteRow row, bool orReplace)
        {
            ThrowIfDisposed();

            if (row.Count == 0)
                throw new ArgumentException("Row has no columns.", nameof(row));

            StringBuilder columnList = new();
            StringBuilder valueList = new();
            List<KeyValuePair<string, object>> parameters = new(row.Count);

            int i = 0;
            foreach (KeyValuePair<string, object> pair in row.Columns)
            {
                if (i > 0)
                {
                    columnList.Append(", ");
                    valueList.Append(", ");
                }

                string paramName = $"$p{i}";
                columnList.Append(pair.Key);
                valueList.Append(paramName);
                parameters.Add(new KeyValuePair<string, object>(paramName, pair.Value ?? DBNull.Value));
                i++;
            }

            string verb = orReplace ? "INSERT OR REPLACE INTO" : "INSERT INTO";
            string sql = $"{verb} {tableName} ({columnList}) VALUES ({valueList})";

            await ExecuteNonQueryAsync(sql, parameters).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes a non-query statement (INSERT/UPDATE/DELETE/DDL) and returns the number of rows
        /// affected. Available to subclasses for statements the helpers do not cover.
        /// </summary>
        protected async Task<int> ExecuteNonQueryAsync(string sql, IEnumerable<KeyValuePair<string, object>> parameters = null)
        {
            ThrowIfDisposed();

            await _dbLock.WaitAsync().ConfigureAwait(false);
            try
            {
                await using SqliteConnection connection = await OpenConnectionAsync().ConfigureAwait(false);
                await using SqliteCommand cmd = connection.CreateCommand();
                cmd.CommandText = sql;
                AddParameters(cmd, parameters);
                return await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Error($@"Error executing SQL '{sql}': {ex.Message}");
                return 0;
            }
            finally
            {
                _dbLock.Release();
            }
        }

        /// <summary>
        /// Executes a query and returns the first column of the first row, or null. Available to
        /// subclasses for simple scalar reads.
        /// </summary>
        protected async Task<object> ExecuteScalarAsync(string sql, IEnumerable<KeyValuePair<string, object>> parameters = null)
        {
            ThrowIfDisposed();

            await _dbLock.WaitAsync().ConfigureAwait(false);
            try
            {
                await using SqliteConnection connection = await OpenConnectionAsync().ConfigureAwait(false);
                await using SqliteCommand cmd = connection.CreateCommand();
                cmd.CommandText = sql;
                AddParameters(cmd, parameters);
                object result = await cmd.ExecuteScalarAsync().ConfigureAwait(false);
                return result == DBNull.Value ? null : result;
            }
            catch (Exception ex)
            {
                Log.Error($@"Error executing scalar SQL '{sql}': {ex.Message}");
                return null;
            }
            finally
            {
                _dbLock.Release();
            }
        }

        private static void AddParameters(SqliteCommand cmd, IEnumerable<KeyValuePair<string, object>> parameters)
        {
            if (parameters == null)
                return;

            foreach (KeyValuePair<string, object> p in parameters)
                cmd.Parameters.AddWithValue(p.Key, p.Value ?? DBNull.Value);
        }

        /// <summary>
        /// Rotates up to <see cref="MAX_BACKUPS"/> copies of the database file (<c>.db.1</c>,
        /// <c>.db.2</c>, ...). Opt-in: a subclass may call this from its constructor before creating
        /// its schema if it wants startup backups.
        /// </summary>
        protected void CreateBackups()
        {
            try
            {
                if (!File.Exists(DatabasePath))
                    return;

                // Rotate existing backups: .3 -> delete, .2 -> .3, .1 -> .2
                for (int i = MAX_BACKUPS; i > 0; i--)
                {
                    string backupPath = $"{DatabasePath}.{i}";

                    if (i == MAX_BACKUPS)
                    {
                        if (File.Exists(backupPath))
                            File.Delete(backupPath);
                    }
                    else
                    {
                        string nextBackupPath = $"{DatabasePath}.{i + 1}";
                        if (File.Exists(backupPath))
                        {
                            if (File.Exists(nextBackupPath))
                                File.Delete(nextBackupPath);
                            File.Move(backupPath, nextBackupPath);
                        }
                    }
                }

                string firstBackupPath = $"{DatabasePath}.1";
                if (File.Exists(firstBackupPath))
                    File.Delete(firstBackupPath);

                File.Copy(DatabasePath, firstBackupPath);
            }
            catch (Exception ex)
            {
                Log.Error($@"Warning: Failed to create database backups for '{DatabasePath}': {ex.Message}");
            }
        }

        /// <summary>
        /// Runs an arbitrary operation against an open connection while holding the database lock. Use
        /// this from a subclass for reads/queries that the built-in helpers do not cover (e.g. running
        /// a <see cref="SqliteDataReader"/>). The connection is opened and disposed for you.
        /// </summary>
        protected async Task<T> ExecuteAsync<T>(Func<SqliteConnection, Task<T>> operation)
        {
            ThrowIfDisposed();

            await _dbLock.WaitAsync().ConfigureAwait(false);
            try
            {
                await using SqliteConnection connection = await OpenConnectionAsync().ConfigureAwait(false);
                return await operation(connection).ConfigureAwait(false);
            }
            finally
            {
                _dbLock.Release();
            }
        }

        /// <summary>Releases resources used by the database.</summary>
        public virtual void Dispose()
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

            GC.SuppressFinalize(this);
        }
    }
}
