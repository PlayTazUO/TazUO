using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ClassicUO.Utility.Logging;
using Microsoft.Data.Sqlite;

namespace ClassicUO.Game.Managers
{
    /// <summary>
    /// Base class that removes the boilerplate of working with a SQLite database: resolving the data
    /// directory, building the connection string, and serializing access behind a lock. Subclass it,
    /// pass a database file name to the constructor, then use <see cref="WithConnectionAsync{T}"/> /
    /// <see cref="WithConnectionAsync"/> to run SQL against a freshly opened connection, or the
    /// <see cref="ExecuteAsync(string)"/> helper for parameter-free statements.
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
    ///         ExecuteAsync("CREATE TABLE IF NOT EXISTS things (id INTEGER PRIMARY KEY, name TEXT NOT NULL)")
    ///             .GetAwaiter().GetResult();
    ///     }
    ///
    ///     public Task SaveAsync(int id, string name) => ExecuteAsync(
    ///         "INSERT OR REPLACE INTO things (id, name) VALUES ($id, $name)",
    ///         new[] { new SqliteParameter("$id", id), new SqliteParameter("$name", name) });
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
                // Private cache (the default). Shared cache is an in-process cache-sharing feature: it
                // provides no benefit across separate clients/processes and changes locking semantics so
                // that table-level contention surfaces as SQLITE_LOCKED, which the busy handler does not
                // retry. WAL journal mode (enabled per connection below) is what actually makes
                // concurrent multi-client access safe and non-blocking for readers.
                Cache = SqliteCacheMode.Private
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
        /// returns its result. The connection is opened and disposed for you. Use this to run reads
        /// and writes directly against the connection with <see cref="SqliteCommand"/>.
        /// </summary>
        protected async Task<T> WithConnectionAsync<T>(Func<SqliteConnection, Task<T>> operation)
        {
            ThrowIfDisposed();

            await _dbLock.WaitAsync().ConfigureAwait(false);
            try
            {
                // Retry loop for cross-process lock contention. busy_timeout (set per connection) already
                // makes SQLite wait inside the driver for a lock another client holds; this outer loop is
                // a bounded backstop for the rare case a lock outlives that timeout under heavy multi-client
                // write load, so an occasional SQLITE_BUSY is retried rather than thrown at the caller.
                for (int attempt = 0; ; attempt++)
                {
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
                    catch (SqliteException ex) when (IsBusyError(ex) && attempt < MAX_BUSY_RETRIES)
                    {
                        // Another client held the database longer than busy_timeout. Back off briefly (the
                        // delay grows with each attempt) and try again; after MAX_BUSY_RETRIES it propagates.
                        await Task.Delay(BUSY_RETRY_BASE_DELAY_MS * (attempt + 1)).ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                _dbLock.Release();
            }
        }

        /// <summary>
        /// Runs an operation against a freshly opened connection while holding the database lock. The
        /// connection is opened and disposed for you. Use this to run writes and DDL directly against
        /// the connection with <see cref="SqliteCommand"/>.
        /// </summary>
        protected Task WithConnectionAsync(Func<SqliteConnection, Task> operation) =>
            WithConnectionAsync(async connection =>
            {
                await operation(connection).ConfigureAwait(false);
                return true;
            });

        /// <summary>
        /// Executes a raw SQL statement against a fresh connection while holding the database lock,
        /// returning the number of rows affected. For statements the generic row helpers cannot
        /// express, such as one-off data migrations and index creation.
        /// </summary>
        protected Task<int> ExecuteAsync(string sql) => ExecuteAsync(sql, null);

        /// <summary>
        /// Executes a raw SQL statement with optional named parameters against a fresh connection while
        /// holding the database lock, returning the number of rows affected.
        /// </summary>
        /// <param name="sql">The SQL statement to execute.</param>
        /// <param name="parameters">
        /// The <see cref="SqliteParameter"/>s to bind, or <c>null</c> for a parameter-free statement.
        /// </param>
        protected Task<int> ExecuteAsync(string sql, IEnumerable<SqliteParameter> parameters) =>
            WithConnectionAsync(connection => ExecuteNonQueryAsync(connection, sql, parameters));

        /// <summary>Opens a fresh connection, configures it for multi-client use, runs the operation, and disposes it.</summary>
        private async Task<T> OpenAndRunAsync<T>(Func<SqliteConnection, Task<T>> operation)
        {
            await using SqliteConnection connection = new(ConnectionString);
            await connection.OpenAsync().ConfigureAwait(false);
            await ConfigureConnectionAsync(connection).ConfigureAwait(false);
            return await operation(connection).ConfigureAwait(false);
        }

        /// <summary>Executes a non-query SQL statement (optionally with named parameters) against a connection.</summary>
        private static async Task<int> ExecuteNonQueryAsync(SqliteConnection connection, string sql, IEnumerable<SqliteParameter> parameters = null)
        {
            await using SqliteCommand command = connection.CreateCommand();
            command.CommandText = sql;
            if (parameters != null)
                command.Parameters.AddRange(parameters);

            return await command.ExecuteNonQueryAsync().ConfigureAwait(false);
        }

        /// <summary>Reads the first column of every row returned by a query as strings (e.g. pragma results).</summary>
        private static async Task<List<string>> QueryStringsAsync(SqliteConnection connection, string sql)
        {
            List<string> results = new();

            await using SqliteCommand command = connection.CreateCommand();
            command.CommandText = sql;

            await using SqliteDataReader reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
            while (await reader.ReadAsync().ConfigureAwait(false))
                results.Add(reader.GetString(0));

            return results;
        }

        /// <summary>
        /// Applies the per-connection pragmas that make the database safe and non-blocking under
        /// concurrent multi-client access:
        /// <list type="bullet">
        /// <item><c>busy_timeout</c> - wait this long for a lock another client holds instead of failing
        /// immediately with SQLITE_BUSY. Per connection, so it is set on every open.</item>
        /// <item><c>journal_mode=WAL</c> - lets multiple clients read while one writes (the default
        /// rollback journal takes an exclusive database lock for the whole of every write). WAL is
        /// persisted in the database header, so re-asserting it here is cheap once it is set.</item>
        /// <item><c>synchronous=NORMAL</c> - the standard companion to WAL: durable against application
        /// crashes, trading only the last committed transaction on an OS/power loss for much less fsync
        /// overhead.</item>
        /// </list>
        /// </summary>
        private static Task ConfigureConnectionAsync(SqliteConnection connection) => ExecuteNonQueryAsync(connection,
            $"PRAGMA busy_timeout={BUSY_TIMEOUT_MS}; PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL;");

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

            await ExecuteNonQueryAsync(connection, createSql.ToString()).ConfigureAwait(false);

            // Reconcile columns against the schema using the pragma_table_info table-valued function,
            // so the existing-column read uses a simple reader loop.
            List<string> existingColumns = await QueryStringsAsync(connection,
                $"SELECT name FROM pragma_table_info({QuoteLiteral(schema.Name)})").ConfigureAwait(false);

            HashSet<string> existingSet = new(existingColumns, StringComparer.OrdinalIgnoreCase);
            HashSet<string> desiredSet = new(StringComparer.OrdinalIgnoreCase);
            foreach (SqliteColumn column in schema.Columns)
                desiredSet.Add(column.Name);

            foreach (SqliteColumn column in schema.Columns)
            {
                if (existingSet.Contains(column.Name))
                    continue;

                try
                {
                    // A primary key cannot be added via ALTER TABLE, so never inline it here.
                    await ExecuteNonQueryAsync(connection,
                        $"ALTER TABLE {QuoteIdentifier(schema.Name)} ADD COLUMN {column.ToDefinition(includePrimaryKey: false)}"
                    ).ConfigureAwait(false);
                }
                catch (SqliteException ex) when (IsDuplicateColumnError(ex))
                {
                    // Another client added this column between our read of the existing columns and this
                    // ALTER (simultaneous first launch / upgrade). The end state is what we wanted, so the
                    // race is benign - carry on.
                }
            }

            foreach (string existingColumn in existingColumns)
            {
                if (desiredSet.Contains(existingColumn))
                    continue;

                try
                {
                    await ExecuteNonQueryAsync(connection,
                        $"ALTER TABLE {QuoteIdentifier(schema.Name)} DROP COLUMN {QuoteIdentifier(existingColumn)}"
                    ).ConfigureAwait(false);
                }
                catch (SqliteException ex) when (IsMissingColumnError(ex))
                {
                    // Another client already dropped this column concurrently. Benign - the column is gone,
                    // which is the desired outcome.
                }
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

            SqliteParams parameters = new();
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

            return WithConnectionAsync(connection => ExecuteNonQueryAsync(connection, sql.ToString(), parameters));
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

            (string where, SqliteParams parameters) = BuildWhere(filter);
            string sql = $"DELETE FROM {QuoteIdentifier(schema.Name)} WHERE {where}";

            return WithConnectionAsync(connection => ExecuteNonQueryAsync(connection, sql, parameters));
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

            SqliteParams parameters = null;
            if (filter.Count > 0)
            {
                (string where, SqliteParams whereParams) = BuildWhere(filter);
                sql.Append(" WHERE ").Append(where);
                parameters = whereParams;
            }

            List<SqliteRow> rows = await WithConnectionAsync(async connection =>
            {
                await using SqliteCommand command = connection.CreateCommand();
                command.CommandText = sql.ToString();
                if (parameters != null)
                    command.Parameters.AddRange(parameters);

                await using SqliteDataReader reader = await command.ExecuteReaderAsync().ConfigureAwait(false);

                List<SqliteRow> results = new();
                while (await reader.ReadAsync().ConfigureAwait(false))
                {
                    IDictionary<string, object> values = new Dictionary<string, object>(reader.FieldCount, StringComparer.OrdinalIgnoreCase);
                    for (int i = 0; i < reader.FieldCount; i++)
                        values[reader.GetName(i)] = reader.GetValue(i);

                    results.Add(SqliteRow.FromValues(values));
                }

                return results;
            }).ConfigureAwait(false);

            return rows;
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
        private static (string sql, SqliteParams parameters) BuildWhere(SqliteRow filter)
        {
            StringBuilder sql = new();
            SqliteParams parameters = new();

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

        /// <summary>
        /// Returns true if the exception is transient lock contention from another client holding the
        /// database - the kind of failure a short backoff-and-retry can clear.
        /// </summary>
        private static bool IsBusyError(SqliteException ex) =>
            ex.SqliteErrorCode == SQLITE_BUSY || ex.SqliteErrorCode == SQLITE_LOCKED;

        /// <summary>
        /// Returns true if an <c>ALTER TABLE ... ADD COLUMN</c> failed because the column already exists -
        /// i.e. another client added it concurrently. Matched on message text because SQLite reports it
        /// with the generic SQLITE_ERROR code and no distinct extended code.
        /// </summary>
        private static bool IsDuplicateColumnError(SqliteException ex) =>
            ex.SqliteErrorCode == SQLITE_ERROR &&
            ex.Message.Contains("duplicate column", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Returns true if an <c>ALTER TABLE ... DROP COLUMN</c> failed because the column is already gone -
        /// i.e. another client dropped it concurrently. Matched on message text for the same reason as
        /// <see cref="IsDuplicateColumnError"/>.
        /// </summary>
        private static bool IsMissingColumnError(SqliteException ex) =>
            ex.SqliteErrorCode == SQLITE_ERROR &&
            ex.Message.Contains("no such column", StringComparison.OrdinalIgnoreCase);

        // SQLite primary result codes (see https://www.sqlite.org/rescode.html). A malformed database
        // schema, "database disk image is malformed" all report SQLITE_CORRUPT (11); a file whose header
        // is not recognizable as a SQLite database reports SQLITE_NOTADB (26). SQLITE_BUSY (5) and
        // SQLITE_LOCKED (6) are lock contention; a duplicate/missing column on ALTER reports the generic
        // SQLITE_ERROR (1).
        private const int SQLITE_ERROR = 1;
        private const int SQLITE_BUSY = 5;
        private const int SQLITE_LOCKED = 6;
        private const int SQLITE_CORRUPT = 11;
        private const int SQLITE_NOTADB = 26;

        // How long a connection waits for a lock another client holds before giving up (SQLITE_BUSY), and
        // the bounded application-level retry that backs it up if a lock outlives that wait.
        private const int BUSY_TIMEOUT_MS = 30_000;
        private const int MAX_BUSY_RETRIES = 3;
        private const int BUSY_RETRY_BASE_DELAY_MS = 50;

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

        /// <summary>
        /// An ordered collection of named parameters bound to a command. Names are unprefixed;
        /// the leading <c>@</c> is added here so callers stay consistent with the parameter tokens
        /// generated in the SQL they build.
        /// </summary>
        private sealed class SqliteParams : List<SqliteParameter>
        {
            public void Add(string name, object value) =>
                Add(new SqliteParameter("@" + name, value ?? DBNull.Value));
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
