using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Managers
{
    public class FriendliesSQLManager : SqliteDatabase
    {
        public static FriendliesSQLManager Instance
        {
            get
            {
                if (field == null)
                    field = new();
                return field;
            }
            private set => field = value;
        }

        private const string DB_FILE = "friendlies.db";

        private static readonly SqliteTableSchema FriendliesSchema = new("friendlies",
            SqliteColumn.Int("serial", primaryKey: true),
            SqliteColumn.Str("name", notNull: true));

        // The schema constructor ensures the table; only the index creation remains.
        public FriendliesSQLManager() : base(FriendliesSchema, DB_FILE)
        {
            InitializeAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        // Index creation cannot be expressed through the generic row helpers, so it runs as a raw
        // statement through the base class.
        private async Task InitializeAsync()
        {
            try
            {
                await ExecuteAsync("""
                                   CREATE INDEX IF NOT EXISTS idx_name
                                   ON friendlies(name)
                                   """).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Error($@"Error initializing FriendliesSQLManager: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Asynchronously adds a friendly to the database, inserting or replacing as needed.
        /// </summary>
        /// <param name="serial">The serial of the entity</param>
        /// <param name="name">The name of the entity</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the manager has been disposed</exception>
        public async Task AddAsync(uint serial, string name)
        {
            try
            {
                await AddOrUpdateAsync(new SqliteRow { ["serial"] = serial, ["name"] = name ?? string.Empty }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Error($@"Error adding friendly {serial} ('{name}'): {ex.Message}");
            }
        }

        /// <summary>
        /// Asynchronously removes a friendly from the database by serial.
        /// </summary>
        /// <param name="serial">The serial of the entity to remove</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the manager has been disposed</exception>
        public async Task RemoveAsync(uint serial)
        {
            try
            {
                await DeleteAsync(new SqliteRow { ["serial"] = serial }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Error($@"Error removing friendly {serial}: {ex.Message}");
            }
        }

        /// <summary>
        /// Asynchronously checks if a serial is in the friendlies database.
        /// </summary>
        /// <param name="serial">The serial to check</param>
        /// <returns>A task that represents the asynchronous operation, containing true if the serial exists, false otherwise</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the manager has been disposed</exception>
        public async Task<bool> ContainsAsync(uint serial)
        {
            try
            {
                return (await GetFirstAsync(new SqliteRow { ["serial"] = serial }).ConfigureAwait(false)).HasValue;
            }
            catch (Exception ex)
            {
                Log.Error($@"Error checking friendly {serial}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Asynchronously retrieves a friendly's name by serial.
        /// </summary>
        /// <param name="serial">The serial to look up</param>
        /// <returns>A task that represents the asynchronous operation, containing the name or null if not found</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the manager has been disposed</exception>
        public async Task<string> GetNameAsync(uint serial)
        {
            try
            {
                SqliteRow? row = await GetFirstAsync(new SqliteRow { ["serial"] = serial }).ConfigureAwait(false);
                return row?.Get<string>("name");
            }
            catch (Exception ex)
            {
                Log.Error($@"Error getting name for friendly {serial}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Asynchronously retrieves all friendlies from the database.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation, containing a dictionary mapping serials to names</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the manager has been disposed</exception>
        public async Task<Dictionary<uint, string>> GetAllAsync()
        {
            try
            {
                IReadOnlyList<SqliteRow> rows = await GetAsync().ConfigureAwait(false);
                return rows.ToDictionary(r => (uint)r.Get<long>("serial"), r => r.Get<string>("name"));
            }
            catch (Exception ex)
            {
                Log.Error($@"Error getting all friendlies: {ex.Message}");
                return new Dictionary<uint, string>();
            }
        }

        /// <summary>
        /// Asynchronously clears all friendlies from the database.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the manager has been disposed</exception>
        public async Task ClearAsync()
        {
            try
            {
                // A full-table delete has no filter for DeleteAsync, so it runs as a raw statement.
                await ExecuteAsync("DELETE FROM friendlies").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Error($@"Error clearing friendlies: {ex.Message}");
            }
        }

        /// <summary>
        /// Releases resources used by the FriendliesSQLManager.
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();

            if (ReferenceEquals(Instance, this))
                Instance = null;
        }
    }
}
