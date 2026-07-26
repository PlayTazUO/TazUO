using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ClassicUO.Utility.Logging;
using Dapper;
using Microsoft.Data.Sqlite;

namespace ClassicUO.Game.Managers
{
    /// <summary>
    /// Base class that removes the boilerplate of working with a SQLite database: resolving the data
    /// directory, building the connection string, and serializing access behind a lock. Subclass it,
    /// pass a database file name to the constructor, then use <see cref="WithConnectionAsync{T}"/> /
    /// <see cref="WithConnectionAsync"/> together with Dapper's connection extension methods
    /// (<c>ExecuteAsync</c>, <c>QueryAsync</c>, <c>ExecuteScalarAsync</c>, ...) to run SQL.
    /// <para>
    /// Each call opens and disposes a short-lived connection while holding a <see cref="SemaphoreSlim"/>,
    /// matching the conventions used by the other SQLite managers in the project.
    /// </para>
    /// <example>
    /// <code>
    /// public class MyThingDb : SqliteDatabase
    /// {
    ///     public MyThingDb() : base("mything.db")
    ///     {
    ///         WithConnectionAsync(c => c.ExecuteAsync(
    ///             "CREATE TABLE IF NOT EXISTS things (id INTEGER PRIMARY KEY, name TEXT NOT NULL)"
    ///         )).GetAwaiter().GetResult();
    ///     }
    ///
    ///     public Task SaveAsync(int id, string name) => WithConnectionAsync(c => c.ExecuteAsync(
    ///         "INSERT OR REPLACE INTO things (id, name) VALUES (@Id, @Name)", new { Id = id, Name = name }));
    /// }
    /// </code>
    /// </example>
    /// </summary>
    public abstract class SqliteDatabase : IDisposable
    {
        private readonly SemaphoreSlim _dbLock = new(1, 1);
        private bool _disposed;

        /// <summary>The directory that contains the database file.</summary>
        protected string DataDirectory { get; }

        /// <summary>The full path to the database file on disk.</summary>
        protected string DatabasePath { get; }

        /// <summary>The connection string used to open connections to this database.</summary>
        protected string ConnectionString { get; }

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

            ClearReadOnlyAttribute(DatabasePath);

            ConnectionString = new SqliteConnectionStringBuilder
            {
                DataSource = DatabasePath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Shared
            }.ToString();
        }

        /// <summary>
        /// Runs an operation against a freshly opened connection while holding the database lock, and
        /// returns its result. The connection is opened and disposed for you. Use this with Dapper's
        /// connection extension methods for reads (<c>QueryAsync</c>, <c>ExecuteScalarAsync</c>, ...).
        /// </summary>
        protected async Task<T> WithConnectionAsync<T>(Func<SqliteConnection, Task<T>> operation)
        {
            ThrowIfDisposed();

            await _dbLock.WaitAsync().ConfigureAwait(false);
            try
            {
                try
                {
                    return await OpenAndRunAsync(operation).ConfigureAwait(false);
                }
                catch (SqliteException ex) when (IsCorruptionError(ex) && QuarantineCorruptDatabase(ex))
                {
                    // The file was corrupt and has been quarantined; a fresh database will be created
                    // on this retry. Only one retry is attempted - if it fails again the error propagates.
                    return await OpenAndRunAsync(operation).ConfigureAwait(false);
                }
            }
            finally
            {
                _dbLock.Release();
            }
        }

        /// <summary>
        /// Runs an operation against a freshly opened connection while holding the database lock. The
        /// connection is opened and disposed for you. Use this with Dapper's <c>ExecuteAsync</c> for
        /// writes/DDL.
        /// </summary>
        protected Task WithConnectionAsync(Func<SqliteConnection, Task> operation) =>
            WithConnectionAsync(async connection =>
            {
                await operation(connection).ConfigureAwait(false);
                return true;
            });

        /// <summary>Opens a fresh connection, runs the operation, and disposes the connection.</summary>
        private async Task<T> OpenAndRunAsync<T>(Func<SqliteConnection, Task<T>> operation)
        {
            await using SqliteConnection connection = new(ConnectionString);
            await connection.OpenAsync().ConfigureAwait(false);
            return await operation(connection).ConfigureAwait(false);
        }

        /// <summary>
        /// Ensures a table matches the given <see cref="SqliteTableSchema"/>: creates it
        /// (<c>CREATE TABLE IF NOT EXISTS</c>) if it does not exist, otherwise reconciles its columns
        /// against the schema - adding any that are missing (<c>ALTER TABLE ... ADD COLUMN</c>) and
        /// dropping any that are no longer declared (<c>ALTER TABLE ... DROP COLUMN</c>, requires
        /// SQLite 3.35+). Safe to call on every startup for schema migrations.
        /// <para>
        /// This only reconciles column presence. It does not detect or migrate a column's type,
        /// nullability, default, or PRIMARY KEY status changing - SQLite cannot alter those in place,
        /// so changing them requires a manual table rebuild.
        /// </para>
        /// </summary>
        /// <param name="schema">The desired table name and columns.</param>
        protected Task EnsureTableAsync(SqliteTableSchema schema) => WithConnectionAsync(async connection =>
        {
            List<string> primaryKeys = new();
            foreach (SqliteColumn c in schema.Columns)
            {
                if (c.PrimaryKey)
                    primaryKeys.Add(c.Name);
            }

            // Inline "PRIMARY KEY" only works for a single-column key; otherwise use a table constraint.
            bool compositeKey = primaryKeys.Count > 1;

            StringBuilder createSql = new();
            createSql.Append("CREATE TABLE IF NOT EXISTS ");
            createSql.Append(QuoteIdentifier(schema.Name));
            createSql.Append(" (");

            for (int i = 0; i < schema.Columns.Count; i++)
            {
                if (i > 0)
                    createSql.Append(", ");

                createSql.Append(schema.Columns[i].ToDefinition(includePrimaryKey: !compositeKey));
            }

            if (compositeKey)
            {
                createSql.Append(", PRIMARY KEY (");
                for (int i = 0; i < primaryKeys.Count; i++)
                {
                    if (i > 0)
                        createSql.Append(", ");

                    createSql.Append(QuoteIdentifier(primaryKeys[i]));
                }
                createSql.Append(')');
            }

            createSql.Append(')');

            await connection.ExecuteAsync(createSql.ToString()).ConfigureAwait(false);

            // Reconcile columns against the schema using the pragma_table_info table-valued function,
            // so the existing-column read goes through Dapper rather than a hand-rolled data reader loop.
            List<string> existingColumns = (await connection.QueryAsync<string>(
                $"SELECT name FROM pragma_table_info({QuoteLiteral(schema.Name)})").ConfigureAwait(false)).ToList();

            HashSet<string> existingSet = new(existingColumns, StringComparer.OrdinalIgnoreCase);
            HashSet<string> desiredSet = new(StringComparer.OrdinalIgnoreCase);
            foreach (SqliteColumn column in schema.Columns)
                desiredSet.Add(column.Name);

            foreach (SqliteColumn column in schema.Columns)
            {
                if (existingSet.Contains(column.Name))
                    continue;

                // A primary key cannot be added via ALTER TABLE, so never inline it here.
                await connection.ExecuteAsync(
                    $"ALTER TABLE {QuoteIdentifier(schema.Name)} ADD COLUMN {column.ToDefinition(includePrimaryKey: false)}"
                ).ConfigureAwait(false);
            }

            foreach (string existingColumn in existingColumns)
            {
                if (desiredSet.Contains(existingColumn))
                    continue;

                await connection.ExecuteAsync(
                    $"ALTER TABLE {QuoteIdentifier(schema.Name)} DROP COLUMN {QuoteIdentifier(existingColumn)}"
                ).ConfigureAwait(false);
            }
        });

        /// <summary>
        /// Quotes a SQLite identifier (table/column name) so it cannot break out of the surrounding
        /// SQL. Identifiers cannot be passed as bound parameters, so callers that build SQL from
        /// identifiers must quote them; embedded double quotes are doubled per the SQLite grammar.
        /// </summary>
        protected static string QuoteIdentifier(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
                throw new ArgumentException("Identifier cannot be null or empty.", nameof(identifier));

            return "\"" + identifier.Replace("\"", "\"\"") + "\"";
        }

        /// <summary>
        /// Quotes a string as a SQLite text literal (e.g. for use as a table-valued function argument,
        /// where a bound parameter or double-quoted identifier cannot be used).
        /// </summary>
        private static string QuoteLiteral(string value) => "'" + value.Replace("'", "''") + "'";

        /// <summary>
        /// Returns true if the exception indicates the database file itself is corrupt/unreadable
        /// (a malformed schema or a file that is not a database) rather than a transient or usage error.
        /// These are the only conditions that quarantining and recreating the file can recover from.
        /// </summary>
        private static bool IsCorruptionError(SqliteException ex) =>
            ex.SqliteErrorCode == SQLITE_CORRUPT || ex.SqliteErrorCode == SQLITE_NOTADB;

        // SQLite primary result codes (see https://www.sqlite.org/rescode.html). A malformed database
        // schema, "database disk image is malformed" all report SQLITE_CORRUPT (11); a file whose header
        // is not recognizable as a SQLite database reports SQLITE_NOTADB (26).
        private const int SQLITE_CORRUPT = 11;
        private const int SQLITE_NOTADB = 26;

        /// <summary>
        /// Moves a corrupt database file (and its WAL/SHM/journal sidecars) aside so a fresh, empty
        /// database can be created in its place, letting the client keep running instead of crashing on
        /// startup. The corrupt copy is preserved with a <c>.corrupt</c> suffix for later inspection.
        /// Returns true only if the primary file was successfully moved out of the way, meaning the
        /// caller can safely retry the operation against a clean database.
        /// </summary>
        private bool QuarantineCorruptDatabase(SqliteException ex)
        {
            try
            {
                // Pooled connections keep the file handle open; without clearing the pool the move/delete
                // below fails on Windows because the file is still in use.
                SqliteConnection.ClearAllPools();

                if (!File.Exists(DatabasePath))
                    return false;

                string quarantinePath = DatabasePath + ".corrupt";

                MoveAside(DatabasePath, quarantinePath);
                // The sidecar files belong to the corrupt database; discard them so they cannot be
                // paired with the new file. Best-effort - a fresh database recreates them as needed.
                TryDelete(DatabasePath + "-wal");
                TryDelete(DatabasePath + "-shm");
                TryDelete(DatabasePath + "-journal");

                Log.Warn($"Corrupt SQLite database '{DatabasePath}' detected ({ex.Message.Trim()}). " +
                         $"Moved it to '{quarantinePath}' and recreating an empty database.");

                return !File.Exists(DatabasePath);
            }
            catch (Exception cleanupEx)
            {
                Log.Error($"Failed to quarantine corrupt SQLite database '{DatabasePath}': {cleanupEx.Message}");
                return false;
            }
        }

        /// <summary>Moves <paramref name="source"/> to <paramref name="destination"/>, overwriting any existing quarantine copy.</summary>
        private static void MoveAside(string source, string destination)
        {
            TryDelete(destination);
            File.Move(source, destination);
        }

        /// <summary>Best-effort deletion of a file that may or may not exist.</summary>
        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
                // Best-effort - a leftover sidecar is harmless once the primary file is gone.
            }
        }

        /// <summary>
        /// Best-effort clearing of the read-only file attribute on an existing database file. A read-only
        /// file (set by cloud-sync, antivirus, or a backup restore) opens fine but fails every write with
        /// "SQLite Error 8: attempt to write a readonly database". Failures here are swallowed.
        /// </summary>
        private static void ClearReadOnlyAttribute(string path)
        {
            try
            {
                if (!File.Exists(path))
                    return;

                FileAttributes attributes = File.GetAttributes(path);
                if ((attributes & FileAttributes.ReadOnly) != 0)
                    File.SetAttributes(path, attributes & ~FileAttributes.ReadOnly);
            }
            catch
            {
                // Fall through and let the normal connection path surface any resulting error.
            }
        }

        /// <summary>Throws <see cref="ObjectDisposedException"/> if this database has already been disposed.</summary>
        protected void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);
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
