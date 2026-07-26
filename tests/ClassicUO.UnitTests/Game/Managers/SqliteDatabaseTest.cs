using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClassicUO.Game.Managers;
using Dapper;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Xunit;

namespace ClassicUO.UnitTests.Game.Managers
{
    public class SqliteDatabaseTest : IDisposable
    {
        // Concrete subclass exposing the protected helpers so tests can exercise them.
        private sealed class TestDatabase : SqliteDatabase
        {
            public TestDatabase(string directory) : base("test.db", directory) { }

            public Task<T> RunAsync<T>(Func<SqliteConnection, Task<T>> operation) => WithConnectionAsync(operation);

            public Task RunAsync(Func<SqliteConnection, Task> operation) => WithConnectionAsync(operation);

            public Task EnsureSchemaAsync(SqliteTableSchema schema) => EnsureTableAsync(schema);

            public Task<List<string>> GetColumnsAsync(string table) => WithConnectionAsync(async c =>
                (await c.QueryAsync<string>($"SELECT name FROM pragma_table_info('{table}')")).ToList());
        }

        private readonly string _tempDir;
        private readonly TestDatabase _db;

        public SqliteDatabaseTest()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "tazuo_sqlite_test_" + Guid.NewGuid().ToString("N"));
            _db = new TestDatabase(_tempDir);
        }

        [Fact]
        public void Constructor_CreatesDataDirectory()
        {
            Directory.Exists(_tempDir).Should().BeTrue();
        }

        [Fact]
        public async Task WithConnectionAsync_CreatesDatabaseFile_AndRunsSql()
        {
            await _db.RunAsync(c => c.ExecuteAsync(
                "CREATE TABLE IF NOT EXISTS things (id INTEGER PRIMARY KEY, name TEXT NOT NULL)"));

            File.Exists(Path.Combine(_tempDir, "test.db")).Should().BeTrue();

            await _db.RunAsync(c => c.ExecuteAsync(
                "INSERT OR REPLACE INTO things (id, name) VALUES (@Id, @Name)", new { Id = 1, Name = "original" }));

            string name = await _db.RunAsync(c => c.ExecuteScalarAsync<string>(
                "SELECT name FROM things WHERE id = @Id", new { Id = 1 }));
            name.Should().Be("original");

            // Upsert by primary key should update, not duplicate.
            await _db.RunAsync(c => c.ExecuteAsync(
                "INSERT OR REPLACE INTO things (id, name) VALUES (@Id, @Name)", new { Id = 1, Name = "updated" }));

            name = await _db.RunAsync(c => c.ExecuteScalarAsync<string>(
                "SELECT name FROM things WHERE id = @Id", new { Id = 1 }));
            name.Should().Be("updated");

            long count = await _db.RunAsync(c => c.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM things"));
            count.Should().Be(1);
        }

        [Fact]
        public async Task EnsureTableAsync_CreatesTable_WithDeclaredColumns()
        {
            var schema = new SqliteTableSchema("things",
                SqliteColumn.Int("id", primaryKey: true),
                SqliteColumn.Str("name", notNull: true, def: "''"));

            await _db.EnsureSchemaAsync(schema);

            File.Exists(Path.Combine(_tempDir, "test.db")).Should().BeTrue();

            List<string> columns = await _db.GetColumnsAsync("things");
            columns.Should().BeEquivalentTo(new[] { "id", "name" });

            // Calling again with the same schema must not throw and must not alter the columns.
            await _db.EnsureSchemaAsync(schema);
            columns = await _db.GetColumnsAsync("things");
            columns.Should().BeEquivalentTo(new[] { "id", "name" });
        }

        [Fact]
        public async Task EnsureTableAsync_AddsMissingColumn_AndPreservesExistingData()
        {
            await _db.EnsureSchemaAsync(new SqliteTableSchema("things",
                SqliteColumn.Int("id", primaryKey: true),
                SqliteColumn.Str("name", notNull: true, def: "''")));

            await _db.RunAsync(c => c.ExecuteAsync(
                "INSERT INTO things (id, name) VALUES (@Id, @Name)", new { Id = 1, Name = "original" }));

            // Re-declare the schema with an extra column - the migration should add it without
            // touching existing rows.
            await _db.EnsureSchemaAsync(new SqliteTableSchema("things",
                SqliteColumn.Int("id", primaryKey: true),
                SqliteColumn.Str("name", notNull: true, def: "''"),
                SqliteColumn.Int("count", def: "0")));

            List<string> columns = await _db.GetColumnsAsync("things");
            columns.Should().BeEquivalentTo(new[] { "id", "name", "count" });

            string name = await _db.RunAsync(c => c.ExecuteScalarAsync<string>(
                "SELECT name FROM things WHERE id = @Id", new { Id = 1 }));
            name.Should().Be("original");

            long count = await _db.RunAsync(c => c.ExecuteScalarAsync<long>(
                "SELECT count FROM things WHERE id = @Id", new { Id = 1 }));
            count.Should().Be(0);
        }

        [Fact]
        public async Task EnsureTableAsync_DropsRemovedColumn_AndPreservesRemainingData()
        {
            await _db.EnsureSchemaAsync(new SqliteTableSchema("things",
                SqliteColumn.Int("id", primaryKey: true),
                SqliteColumn.Str("name", notNull: true, def: "''"),
                SqliteColumn.Int("count", def: "0")));

            await _db.RunAsync(c => c.ExecuteAsync(
                "INSERT INTO things (id, name, count) VALUES (@Id, @Name, @Count)",
                new { Id = 1, Name = "original", Count = 5 }));

            // Re-declare the schema without "count" - the migration should drop that column.
            await _db.EnsureSchemaAsync(new SqliteTableSchema("things",
                SqliteColumn.Int("id", primaryKey: true),
                SqliteColumn.Str("name", notNull: true, def: "''")));

            List<string> columns = await _db.GetColumnsAsync("things");
            columns.Should().BeEquivalentTo(new[] { "id", "name" });

            string name = await _db.RunAsync(c => c.ExecuteScalarAsync<string>(
                "SELECT name FROM things WHERE id = @Id", new { Id = 1 }));
            name.Should().Be("original");
        }

        [Fact]
        public async Task WithConnectionAsync_RecoversFromCorruptDatabaseFile()
        {
            // Release the fixture instance so it doesn't hold a pooled handle to the file we're about to
            // clobber, then write bytes that SQLite cannot recognize as a database at all.
            _db.Dispose();

            string dbPath = Path.Combine(_tempDir, "test.db");
            await File.WriteAllTextAsync(dbPath, "this is not a valid sqlite database");

            using var db = new TestDatabase(_tempDir);

            // The first operation should detect the corruption, move the bad file aside, and succeed
            // against a freshly created database rather than throwing.
            Func<Task> act = () => db.RunAsync(c => c.ExecuteAsync(
                "CREATE TABLE IF NOT EXISTS things (id INTEGER PRIMARY KEY, name TEXT NOT NULL)"));
            await act.Should().NotThrowAsync();

            // The corrupt copy is preserved for inspection and the new database is usable.
            File.Exists(dbPath + ".corrupt").Should().BeTrue();

            long count = await db.RunAsync(c => c.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM things"));
            count.Should().Be(0);
        }

        [Fact]
        public async Task Constructor_ClearsReadOnlyAttribute_AndAllowsWrites()
        {
            await _db.RunAsync(c => c.ExecuteAsync(
                "CREATE TABLE IF NOT EXISTS things (id INTEGER PRIMARY KEY, name TEXT NOT NULL)"));
            _db.Dispose();

            // Flag the file read-only; writes would otherwise fail with "attempt to write a readonly database".
            string dbPath = Path.Combine(_tempDir, "test.db");
            File.SetAttributes(dbPath, File.GetAttributes(dbPath) | FileAttributes.ReadOnly);
            (File.GetAttributes(dbPath) & FileAttributes.ReadOnly).Should().NotBe(0);

            // A new instance must clear the attribute so writes succeed.
            using var db = new TestDatabase(_tempDir);
            (File.GetAttributes(dbPath) & FileAttributes.ReadOnly).Should().Be(0);

            Func<Task> act = () => db.RunAsync(c => c.ExecuteAsync(
                "INSERT OR REPLACE INTO things (id, name) VALUES (@Id, @Name)", new { Id = 1, Name = "written" }));
            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task Operations_AfterDispose_Throw()
        {
            var db = new TestDatabase(_tempDir);
            db.Dispose();

            Func<Task> act = () => db.RunAsync(c => c.ExecuteAsync("SELECT 1"));
            await act.Should().ThrowAsync<ObjectDisposedException>();
        }

        [Fact]
        public void Dispose_IsIdempotent()
        {
            var db = new TestDatabase(_tempDir);

            Action act = () =>
            {
                db.Dispose();
                db.Dispose();
            };

            act.Should().NotThrow();
        }

        public void Dispose()
        {
            _db.Dispose();

            try
            {
                if (Directory.Exists(_tempDir))
                    Directory.Delete(_tempDir, true);
            }
            catch
            {
                // Best-effort cleanup.
            }
        }
    }
}
