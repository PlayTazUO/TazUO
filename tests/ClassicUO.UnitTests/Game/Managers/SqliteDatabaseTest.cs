using System;
using System.IO;
using System.Threading.Tasks;
using ClassicUO.Game.Managers;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.Managers
{
    public class SqliteDatabaseTest : IDisposable
    {
        // Concrete subclass exposing the protected scalar helper so tests can read values back.
        private sealed class TestDatabase : SqliteDatabase
        {
            public TestDatabase(string directory) : base("test.db", directory) { }

            public Task<object> ScalarAsync(string sql, string paramName = null, object paramValue = null)
            {
                if (paramName == null)
                    return ExecuteScalarAsync(sql);

                return ExecuteScalarAsync(sql, new[]
                {
                    new System.Collections.Generic.KeyValuePair<string, object>(paramName, paramValue)
                });
            }
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
        public async Task EnsureTable_And_EnsureColumns_AreIdempotent()
        {
            Func<Task> act = async () =>
            {
                await _db.EnsureTableAsync("things",
                    SqliteColumn.Int("id", primaryKey: true),
                    SqliteColumn.Str("name", notNull: true, def: "''"));

                // Calling again must not throw.
                await _db.EnsureTableAsync("things",
                    SqliteColumn.Int("id", primaryKey: true),
                    SqliteColumn.Str("name", notNull: true, def: "''"));

                // Adding a new column, twice, must not throw (migration path).
                await _db.EnsureColumnsAsync("things", SqliteColumn.Int("count", def: "0"));
                await _db.EnsureColumnsAsync("things", SqliteColumn.Int("count", def: "0"));
            };

            await act.Should().NotThrowAsync();
            File.Exists(Path.Combine(_tempDir, "test.db")).Should().BeTrue();
        }

        [Fact]
        public async Task AddRow_Then_AddOrUpdate_UpsertsByPrimaryKey()
        {
            await _db.EnsureTableAsync("things",
                SqliteColumn.Int("id", primaryKey: true),
                SqliteColumn.Str("name", notNull: true, def: "''"));

            await _db.AddRowAsync("things", new SqliteRow { { "id", 1 }, { "name", "original" } });

            object before = await _db.ScalarAsync("SELECT name FROM things WHERE id = $id", "$id", 1);
            before.Should().Be("original");

            // Same primary key with a changed value should update, not duplicate.
            await _db.AddOrUpdateAsync("things", new SqliteRow { { "id", 1 }, { "name", "updated" } });

            object after = await _db.ScalarAsync("SELECT name FROM things WHERE id = $id", "$id", 1);
            after.Should().Be("updated");

            object count = await _db.ScalarAsync("SELECT COUNT(*) FROM things");
            Convert.ToInt64(count).Should().Be(1);
        }

        [Fact]
        public async Task AddOrUpdate_WithCompositeKey_Works()
        {
            await _db.EnsureTableAsync("kv",
                SqliteColumn.Str("scope", primaryKey: true, notNull: true),
                SqliteColumn.Str("key", primaryKey: true, notNull: true),
                SqliteColumn.Str("value", notNull: true, def: "''"));

            await _db.AddOrUpdateAsync("kv", new SqliteRow { { "scope", "a" }, { "key", "x" }, { "value", "1" } });
            await _db.AddOrUpdateAsync("kv", new SqliteRow { { "scope", "a" }, { "key", "x" }, { "value", "2" } });

            object value = await _db.ScalarAsync("SELECT value FROM kv WHERE scope = 'a' AND key = 'x'");
            value.Should().Be("2");

            object count = await _db.ScalarAsync("SELECT COUNT(*) FROM kv");
            Convert.ToInt64(count).Should().Be(1);
        }

        [Fact]
        public async Task Operations_AfterDispose_Throw()
        {
            var db = new TestDatabase(_tempDir);
            db.Dispose();

            Func<Task> act = () => db.EnsureTableAsync("t", SqliteColumn.Int("id", primaryKey: true));
            await act.Should().ThrowAsync<ObjectDisposedException>();
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
