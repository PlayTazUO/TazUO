using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClassicUO.Utility.Logging;
using Dapper;
using Dapper.Contrib.Extensions;

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

        [Table("friendlies")]
        private sealed class FriendlyRecord
        {
            [ExplicitKey]
            public long Serial { get; set; }
            public string Name { get; set; }
        }

        private const string DB_FILE = "friendlies.db";

        private static readonly SqliteTableSchema FriendliesSchema = new("friendlies",
            SqliteColumn.Int("serial", primaryKey: true),
            SqliteColumn.Str("name", notNull: true));

        public FriendliesSQLManager() : base(DB_FILE)
        {
            InitializeAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        // Table creation/migration goes through the base class's schema reconciliation. The index
        // is unrelated to table structure, so it's still created directly. Row-level CRUD below goes
        // through Dapper.Contrib's typed helpers (Get/GetAll/Insert/Update/Delete/DeleteAll) instead
        // of hand-written SQL.
        private async Task InitializeAsync()
        {
            try
            {
                await EnsureTableAsync(FriendliesSchema).ConfigureAwait(false);

                await WithConnectionAsync(connection => connection.ExecuteAsync("""
                                                                                 CREATE INDEX IF NOT EXISTS idx_name
                                                                                 ON friendlies(name)
                                                                                 """)).ConfigureAwait(false);
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
                await WithConnectionAsync(async connection =>
                {
                    FriendlyRecord record = new() { Serial = serial, Name = name ?? string.Empty };
                    FriendlyRecord existing = await connection.GetAsync<FriendlyRecord>((long)serial).ConfigureAwait(false);

                    if (existing == null)
                        await connection.InsertAsync(record).ConfigureAwait(false);
                    else
                        await connection.UpdateAsync(record).ConfigureAwait(false);
                }).ConfigureAwait(false);
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
                await WithConnectionAsync(connection =>
                    connection.DeleteAsync(new FriendlyRecord { Serial = serial })).ConfigureAwait(false);
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
                return await WithConnectionAsync(async connection =>
                    await connection.GetAsync<FriendlyRecord>((long)serial).ConfigureAwait(false) != null)
                    .ConfigureAwait(false);
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
                return await WithConnectionAsync(async connection =>
                {
                    FriendlyRecord existing = await connection.GetAsync<FriendlyRecord>((long)serial).ConfigureAwait(false);
                    return existing?.Name;
                }).ConfigureAwait(false);
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
                return await WithConnectionAsync(async connection =>
                {
                    IEnumerable<FriendlyRecord> rows = await connection.GetAllAsync<FriendlyRecord>().ConfigureAwait(false);
                    return rows.ToDictionary(r => (uint)r.Serial, r => r.Name);
                }).ConfigureAwait(false);
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
                await WithConnectionAsync(connection => connection.DeleteAllAsync<FriendlyRecord>()).ConfigureAwait(false);
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
