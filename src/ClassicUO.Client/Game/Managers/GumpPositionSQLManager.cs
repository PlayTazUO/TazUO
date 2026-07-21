using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClassicUO.Utility.Logging;
using Dapper.Contrib.Extensions;

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

        [Table("gump_positions")]
        private sealed class GumpPositionRecord
        {
            [ExplicitKey]
            public long Serial { get; set; }
            public string Name { get; set; }
            public int X { get; set; }
            public int Y { get; set; }

            // Dapper.Contrib maps a property straight onto a column of the same name (there is no
            // column-rename attribute in 2.0.78), so this property is named to match the "last_seen"
            // column exactly. When this position was last saved/seen, as Unix time in seconds.
            public long last_seen { get; set; }
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

        public GumpPositionSQLManager() : base(DB_FILE)
        {
            InitializeAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        private static long NowUnix() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // Table creation/migration goes through the base class's schema reconciliation; row-level CRUD
        // below goes through Dapper.Contrib's typed helpers (Get/GetAll/Insert/Update/Delete) instead of
        // hand-written SQL, matching the FriendliesSQLManager conventions.
        private async Task InitializeAsync()
        {
            try
            {
                await EnsureTableAsync(PositionsSchema).ConfigureAwait(false);
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
        private Task PurgeStaleAsync()
        {
            long cutoff = NowUnix() - RETENTION_SECONDS;

            return WithConnectionAsync(async connection =>
            {
                List<GumpPositionRecord> rows = (await connection.GetAllAsync<GumpPositionRecord>().ConfigureAwait(false)).ToList();

                foreach (GumpPositionRecord row in rows)
                {
                    if (row.last_seen < cutoff)
                        await connection.DeleteAsync(row).ConfigureAwait(false);
                }
            });
        }

        /// <summary>
        /// Inserts or updates a pinned gump position. Existing rows keep their key and are overwritten
        /// with the supplied name and coordinates.
        /// </summary>
        public async Task SaveAsync(uint serial, string name, int x, int y)
        {
            try
            {
                await WithConnectionAsync(async connection =>
                {
                    GumpPositionRecord record = new() { Serial = serial, Name = name ?? string.Empty, X = x, Y = y, last_seen = NowUnix() };
                    GumpPositionRecord existing = await connection.GetAsync<GumpPositionRecord>((long)serial).ConfigureAwait(false);

                    if (existing == null)
                        await connection.InsertAsync(record).ConfigureAwait(false);
                    else
                        await connection.UpdateAsync(record).ConfigureAwait(false);
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
                await WithConnectionAsync(async connection =>
                {
                    GumpPositionRecord existing = await connection.GetAsync<GumpPositionRecord>((long)serial).ConfigureAwait(false);

                    if (existing == null)
                        return;

                    existing.X = x;
                    existing.Y = y;
                    existing.last_seen = NowUnix();
                    await connection.UpdateAsync(existing).ConfigureAwait(false);
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
                await WithConnectionAsync(connection =>
                    connection.DeleteAsync(new GumpPositionRecord { Serial = serial })).ConfigureAwait(false);
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
                return await WithConnectionAsync(async connection =>
                {
                    IEnumerable<GumpPositionRecord> rows = await connection.GetAllAsync<GumpPositionRecord>().ConfigureAwait(false);
                    return rows.Select(r => new SavedGumpPosition((uint)r.Serial, r.Name, r.X, r.Y)).ToList();
                }).ConfigureAwait(false);
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
