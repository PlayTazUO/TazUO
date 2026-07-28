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

        // The primary table this database manages, when constructed with a schema. Null when the
        // schema-less constructor is used (subclasses that still hand-roll their own SQL). The generic
        // row helpers (AddOrUpdateAsync/DeleteAsync/GetAsync) require this to be set.
        private readonly SqliteTableSchema? _schema;

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
        /// Creates the base database and immediately ensures its primary table matches
        /// <paramref name="schema"/> - creating it if absent and reconciling columns (adding missing,
        /// dropping removed) otherwise. This is the recommended constructor: a subclass declares its
        /// columns once, passes them here, and then works entirely through the generic row helpers
        /// (<see cref="AddOrUpdateAsync"/>, <see cref="DeleteAsync"/>, <see cref="GetAsync"/>) without
        /// writing any SQL of its own.
        /// </summary>
        /// <param name="schema">The table name and columns to ensure. See <see cref="EnsureTableAsync"/>.</param>
        /// <param name="dbFileName">The database file name, e.g. <c>"mything.db"</c>.</param>
        /// <param name="dataDirectory">
        /// The directory to place the database in. Defaults to the shared <c>{ExecutablePath}/Data</c>
        /// directory. Provide an explicit directory (e.g. a temp path) to make a subclass unit-testable.
        /// </param>
        protected SqliteDatabase(SqliteTableSchema schema, string dbFileName, string dataDirectory = null)
            : this(dbFileName, dataDirectory)
        {
            _schema = schema;

            // Ensure the schema up-front so the row helpers can be used immediately after construction.
            // Blocking here mirrors the established pattern for these managers (the table must exist
            // before any query runs) and is safe because nothing else can hold the lock yet.
            EnsureTableAsync(schema).GetAwaiter().GetResult();
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
        /// Inserts a row, or updates the existing row when it collides with the table's PRIMARY KEY
        /// (an <c>INSERT ... ON CONFLICT(pk) DO UPDATE</c> upsert). Only the columns present in
        /// <paramref name="row"/> are written; non-key columns are updated to the incoming values while
        /// the key columns identify the row. All SQL is built here from the schema passed to the
        /// constructor - the caller only supplies the data.
        /// </summary>
        /// <param name="row">The row data. Must include every PRIMARY KEY column.</param>
        /// <returns>The number of rows affected.</returns>
        protected Task<int> AddOrUpdateAsync(SqliteRow row)
        {
            SqliteTableSchema schema = RequireSchema();

            // Keep only columns that are both declared in the schema and supplied on the row, in schema
            // order so the generated SQL is stable and predictable.
            List<SqliteColumn> columns = new();
            foreach (SqliteColumn column in schema.Columns)
            {
                if (row.Contains(column.Name))
                    columns.Add(column);
            }

            if (columns.Count == 0)
                throw new ArgumentException("The row has no columns matching the table schema.", nameof(row));

            List<string> primaryKeys = PrimaryKeyColumns(schema);

            DynamicParameters parameters = new();
            StringBuilder sql = new();
            sql.Append("INSERT INTO ").Append(QuoteIdentifier(schema.Name)).Append(" (");

            for (int i = 0; i < columns.Count; i++)
            {
                if (i > 0)
                    sql.Append(", ");

                sql.Append(QuoteIdentifier(columns[i].Name));
            }

            sql.Append(") VALUES (");

            for (int i = 0; i < columns.Count; i++)
            {
                if (i > 0)
                    sql.Append(", ");

                string name = "p" + i;
                sql.Append('@').Append(name);
                parameters.Add(name, row[columns[i].Name]);
            }

            sql.Append(')');

            if (primaryKeys.Count > 0)
            {
                sql.Append(" ON CONFLICT(");
                for (int i = 0; i < primaryKeys.Count; i++)
                {
                    if (i > 0)
                        sql.Append(", ");

                    sql.Append(QuoteIdentifier(primaryKeys[i]));
                }
                sql.Append(") DO ");

                // Update every supplied non-key column; if the only supplied columns are the key itself
                // there is nothing to change, so the conflict is a no-op.
                List<SqliteColumn> updatable = new();
                foreach (SqliteColumn column in columns)
                {
                    if (!primaryKeys.Contains(column.Name, StringComparer.OrdinalIgnoreCase))
                        updatable.Add(column);
                }

                if (updatable.Count == 0)
                {
                    sql.Append("NOTHING");
                }
                else
                {
                    sql.Append("UPDATE SET ");
                    for (int i = 0; i < updatable.Count; i++)
                    {
                        if (i > 0)
                            sql.Append(", ");

                        string quoted = QuoteIdentifier(updatable[i].Name);
                        sql.Append(quoted).Append(" = excluded.").Append(quoted);
                    }
                }
            }

            return WithConnectionAsync(connection => connection.ExecuteAsync(sql.ToString(), parameters));
        }

        /// <summary>
        /// Deletes every row that matches the equality filter in <paramref name="filter"/> (all supplied
        /// columns must match, combined with AND). Typically the filter is the row's PRIMARY KEY.
        /// </summary>
        /// <param name="filter">The columns to match. Must contain at least one column.</param>
        /// <returns>The number of rows deleted.</returns>
        protected Task<int> DeleteAsync(SqliteRow filter)
        {
            SqliteTableSchema schema = RequireSchema();

            if (filter.Count == 0)
                throw new ArgumentException(
                    "A delete requires at least one filter column; pass the row's key to identify what to delete.",
                    nameof(filter));

            (string where, DynamicParameters parameters) = BuildWhere(filter);
            string sql = $"DELETE FROM {QuoteIdentifier(schema.Name)} WHERE {where}";

            return WithConnectionAsync(connection => connection.ExecuteAsync(sql, parameters));
        }

        /// <summary>
        /// Reads rows from the table. With no filter it returns every row; otherwise it returns the rows
        /// whose columns all equal the supplied values (combined with AND). Each result is a
        /// <see cref="SqliteRow"/> the subclass maps back to its domain type.
        /// </summary>
        /// <param name="filter">Optional equality filter. Omit (or pass <c>default</c>) to select all rows.</param>
        protected async Task<IReadOnlyList<SqliteRow>> GetAsync(SqliteRow filter = default)
        {
            SqliteTableSchema schema = RequireSchema();

            StringBuilder sql = new();
            sql.Append("SELECT * FROM ").Append(QuoteIdentifier(schema.Name));

            DynamicParameters parameters = null;
            if (filter.Count > 0)
            {
                (string where, DynamicParameters whereParams) = BuildWhere(filter);
                sql.Append(" WHERE ").Append(where);
                parameters = whereParams;
            }

            IEnumerable<dynamic> rows = await WithConnectionAsync(connection =>
                connection.QueryAsync(sql.ToString(), parameters)).ConfigureAwait(false);

            List<SqliteRow> results = new();
            foreach (IDictionary<string, object> row in rows)
                results.Add(SqliteRow.FromValues(row));

            return results;
        }

        /// <summary>
        /// Reads the first row matching the filter, or <c>null</c> if none match. A convenience over
        /// <see cref="GetAsync"/> for lookups by a unique key.
        /// </summary>
        protected async Task<SqliteRow?> GetFirstAsync(SqliteRow filter = default)
        {
            IReadOnlyList<SqliteRow> rows = await GetAsync(filter).ConfigureAwait(false);
            return rows.Count > 0 ? rows[0] : null;
        }

        /// <summary>Returns the schema set by the constructor, or throws if the schema-less constructor was used.</summary>
        private SqliteTableSchema RequireSchema()
        {
            if (_schema == null)
                throw new InvalidOperationException(
                    "The generic row helpers require a schema. Use the SqliteDatabase(SqliteTableSchema, ...) constructor.");

            return _schema.Value;
        }

        /// <summary>Returns the PRIMARY KEY column names of a schema, in declaration order.</summary>
        private static List<string> PrimaryKeyColumns(SqliteTableSchema schema)
        {
            List<string> keys = new();
            foreach (SqliteColumn column in schema.Columns)
            {
                if (column.PrimaryKey)
                    keys.Add(column.Name);
            }

            return keys;
        }

        /// <summary>
        /// Builds a parameterized WHERE fragment matching each column in <paramref name="filter"/> for
        /// equality (a NULL value becomes <c>IS NULL</c>), combined with AND.
        /// </summary>
        private static (string sql, DynamicParameters parameters) BuildWhere(SqliteRow filter)
        {
            StringBuilder sql = new();
            DynamicParameters parameters = new();

            int i = 0;
            foreach (string column in filter.Columns)
            {
                if (i > 0)
                    sql.Append(" AND ");

                object value = filter[column];
                if (value is null)
                {
                    sql.Append(QuoteIdentifier(column)).Append(" IS NULL");
                }
                else
                {
                    string name = "w" + i;
                    sql.Append(QuoteIdentifier(column)).Append(" = @").Append(name);
                    parameters.Add(name, value);
                }

                i++;
            }

            return (sql.ToString(), parameters);
        }

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
