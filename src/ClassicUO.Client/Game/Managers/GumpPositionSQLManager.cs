using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Managers
{
    /// <summary>
    /// A single permanently-saved gump position: the gump's cache key (server serial / item serial),
    /// a human friendly name for display, and the on-screen location it should reopen at.
    /// </summary>
    public readonly struct SavedGumpPosition
    {
        public SavedGumpPosition(uint serial, string name, int x, int y)
        {
            Serial = serial;
            Name = name;
            X = x;
            Y = y;
        }

        public uint Serial { get; }
        public string Name { get; }
        public int X { get; }
        public int Y { get; }
    }

    /// <summary>
    /// Backing SQLite store for the permanent gump-position feature. Mirrors the in-memory
    /// <see cref="UIManager"/> gump position cache for the subset of gumps the user has chosen to pin,
    /// so those gumps reopen at their pinned location across restarts. The database lives alongside the
    /// other managers in the shared <c>{ExecutablePath}/Data</c> directory.
    /// </summary>
    public class GumpPositionSQLManager : SqliteDatabase
    {
        public static GumpPositionSQLManager Instance
        {
            get
            {
                if (field == null)
                    field = new();
                return field;
            }
            private set => field = value;
        }

        private const string DB_FILE = "gump_positions.db";

        // Pinned positions untouched for this long are purged on startup.
        private const long RETENTION_SECONDS = 120L * 24 * 60 * 60;

        private static readonly SqliteTableSchema PositionsSchema = new("gump_positions",
            SqliteColumn.Int("serial", primaryKey: true),
            SqliteColumn.Str("name"),
            SqliteColumn.Int("x", notNull: true, def: "0"),
            SqliteColumn.Int("y", notNull: true, def: "0"),
            SqliteColumn.Int("last_seen", notNull: true, def: "0"));

        // The schema constructor ensures the table; only the stale purge remains.
        public GumpPositionSQLManager() : base(PositionsSchema, DB_FILE)
        {
            InitializeAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        private static long NowUnix() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        private async Task InitializeAsync()
        {
            try
            {
                await PurgeStaleAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Error($@"Error initializing GumpPositionSQLManager: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Startup housekeeping: deletes any pinned position that has not been seen within the retention
        /// window (120 days).
        /// </summary>
        private async Task PurgeStaleAsync()
        {
            long cutoff = NowUnix() - RETENTION_SECONDS;

            foreach (SqliteRow row in await GetAsync().ConfigureAwait(false))
            {
                if (row.Get<long>("last_seen") < cutoff)
                    await DeleteAsync(new SqliteRow { ["serial"] = row.Get<long>("serial") }).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Inserts or updates a pinned gump position. Existing rows keep their key and are overwritten
        /// with the supplied name and coordinates.
        /// </summary>
        public async Task SaveAsync(uint serial, string name, int x, int y)
        {
            try
            {
                await AddOrUpdateAsync(new SqliteRow
                {
                    ["serial"] = serial,
                    ["name"] = name ?? string.Empty,
                    ["x"] = x,
                    ["y"] = y,
                    ["last_seen"] = NowUnix()
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Error($@"Error saving gump position {serial}: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates only the coordinates of an already-pinned gump (used when the user drags/moves a
        /// gump whose position is being tracked). Does nothing if the serial is not already stored.
        /// </summary>
        public async Task UpdatePositionAsync(uint serial, int x, int y)
        {
            try
            {
                SqliteRow? existing = await GetFirstAsync(new SqliteRow { ["serial"] = serial }).ConfigureAwait(false);
                if (existing == null)
                    return;

                // Only the coordinates and timestamp are written; the name column is left untouched.
                await AddOrUpdateAsync(new SqliteRow
                {
                    ["serial"] = serial,
                    ["x"] = x,
                    ["y"] = y,
                    ["last_seen"] = NowUnix()
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Error($@"Error updating gump position {serial}: {ex.Message}");
            }
        }

        /// <summary>Removes a pinned gump position by serial.</summary>
        public async Task RemoveAsync(uint serial)
        {
            try
            {
                await DeleteAsync(new SqliteRow { ["serial"] = serial }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Error($@"Error removing gump position {serial}: {ex.Message}");
            }
        }

        /// <summary>Retrieves every pinned gump position.</summary>
        public async Task<List<SavedGumpPosition>> GetAllAsync()
        {
            try
            {
                IReadOnlyList<SqliteRow> rows = await GetAsync().ConfigureAwait(false);
                return rows.Select(r =>
                    new SavedGumpPosition((uint)r.Get<long>("serial"), r.Get<string>("name"), r.Get<int>("x"), r.Get<int>("y"))).ToList();
            }
            catch (Exception ex)
            {
                Log.Error($@"Error getting all gump positions: {ex.Message}");
                return new List<SavedGumpPosition>();
            }
        }

        /// <summary>
        /// Synchronous convenience wrapper around <see cref="GetAllAsync"/> for the one-time startup
        /// seed of the in-memory cache, mirroring the blocking pattern used by the other SQLite managers.
        /// </summary>
        public List<SavedGumpPosition> GetAll() =>
            GetAllAsync().ConfigureAwait(false).GetAwaiter().GetResult();

        public override void Dispose()
        {
            base.Dispose();

            if (ReferenceEquals(Instance, this))
                Instance = null;
        }
    }
}
