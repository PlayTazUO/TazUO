using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClassicUO.Utility.Logging;
using Dapper;

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

        private static readonly SqliteTableSchema PositionsSchema = new("gump_positions",
            SqliteColumn.Int("serial", primaryKey: true),
            SqliteColumn.Str("name"),
            SqliteColumn.Int("x", notNull: true, def: "0"),
            SqliteColumn.Int("y", notNull: true, def: "0"));

        public GumpPositionSQLManager() : base(DB_FILE)
        {
            InitializeAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        private async Task InitializeAsync()
        {
            try
            {
                await EnsureTableAsync(PositionsSchema).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Error($@"Error initializing GumpPositionSQLManager: {ex.Message}");
                throw;
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
                await WithConnectionAsync(connection => connection.ExecuteAsync(
                    """
                    INSERT INTO gump_positions (serial, name, x, y)
                    VALUES (@Serial, @Name, @X, @Y)
                    ON CONFLICT(serial) DO UPDATE SET name = excluded.name, x = excluded.x, y = excluded.y
                    """,
                    new { Serial = (long)serial, Name = name ?? string.Empty, X = x, Y = y })).ConfigureAwait(false);
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
                await WithConnectionAsync(connection => connection.ExecuteAsync(
                    "UPDATE gump_positions SET x = @X, y = @Y WHERE serial = @Serial",
                    new { Serial = (long)serial, X = x, Y = y })).ConfigureAwait(false);
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
                await WithConnectionAsync(connection => connection.ExecuteAsync(
                    "DELETE FROM gump_positions WHERE serial = @Serial",
                    new { Serial = (long)serial })).ConfigureAwait(false);
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
                    IEnumerable<PositionRow> rows = await connection.QueryAsync<PositionRow>(
                        "SELECT serial AS Serial, name AS Name, x AS X, y AS Y FROM gump_positions").ConfigureAwait(false);

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

        // Dapper materialization target; the public API exposes the immutable SavedGumpPosition instead.
        private sealed class PositionRow
        {
            public long Serial { get; set; }
            public string Name { get; set; }
            public int X { get; set; }
            public int Y { get; set; }
        }
    }
}
