using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
                await using SqliteConnection connection = new(ConnectionString);
                await connection.OpenAsync().ConfigureAwait(false);
                return await operation(connection).ConfigureAwait(false);
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
        protected async Task WithConnectionAsync(Func<SqliteConnection, Task> operation)
        {
            ThrowIfDisposed();

            await _dbLock.WaitAsync().ConfigureAwait(false);
            try
            {
                await using SqliteConnection connection = new(ConnectionString);
                await connection.OpenAsync().ConfigureAwait(false);
                await operation(connection).ConfigureAwait(false);
            }
            finally
            {
                _dbLock.Release();
            }
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
        /// Best-effort clearing of the read-only file attribute on an existing database file. A file
        /// flagged read-only (commonly by cloud-sync clients like OneDrive, antivirus, or restoring the
        /// Data folder from a backup/zip) can still be opened with <see cref="SqliteOpenMode.ReadWriteCreate"/>,
        /// but any write fails with <c>SQLite Error 8: 'attempt to write a readonly database'</c>. Clearing
        /// the attribute up front lets writes succeed; failures here are swallowed so a locked-down file
        /// never turns database construction into a crash.
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
                // Best-effort: if the attribute can't be read or cleared, fall through and let the
                // normal connection path surface any resulting error.
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
