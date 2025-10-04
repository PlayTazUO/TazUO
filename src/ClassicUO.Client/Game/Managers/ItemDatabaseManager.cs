using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using Microsoft.Data.Sqlite;

namespace ClassicUO.Game.Managers
{
    public sealed class ItemDatabaseManager
    {
        private static readonly Lazy<ItemDatabaseManager> _instance =
            new Lazy<ItemDatabaseManager>(() => new ItemDatabaseManager());
        private readonly object _dbLock = new(), _timerLock = new();
        private string _databasePath;
        private bool _initialized;
        private ConcurrentQueue<ItemInfo> _pendingItems = new();
        private Timer _pendingItemsTimer;

        public static ItemDatabaseManager Instance
        {
            get
            {
                return _instance.Value;
            }
        }

        private ItemDatabaseManager()
        {
            _databasePath = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "items.db");
        }

        public void Initialize()
        {
            if (_initialized)
                return;

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_databasePath));
                CreateDatabaseIfNotExists();
                _initialized = true;
                Log.Trace($"ItemDatabaseManager initialized with database at: {_databasePath}");
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to initialize ItemDatabaseManager: {ex}");
            }
        }

        private void CreateDatabaseIfNotExists()
        {
            lock (_dbLock)
            {
                using var connection = new SqliteConnection($"Data Source={_databasePath}");
                connection.Open();

                string createTableQuery = @"
                    CREATE TABLE IF NOT EXISTS Items (
                        Serial INTEGER PRIMARY KEY,
                        Graphic INTEGER NOT NULL,
                        Hue INTEGER NOT NULL,
                        Name TEXT NOT NULL DEFAULT '',
                        Properties TEXT NOT NULL DEFAULT '',
                        Container INTEGER NOT NULL,
                        Layer INTEGER NOT NULL DEFAULT 0,
                        UpdatedTime TEXT NOT NULL,
                        Character INTEGER NOT NULL,
                        CharacterName TEXT NOT NULL DEFAULT '',
                        ServerName TEXT NOT NULL DEFAULT '',
                        X INTEGER NOT NULL,
                        Y INTEGER NOT NULL,
                        OnGround INTEGER NOT NULL
                    )";

                using var command = new SqliteCommand(createTableQuery, connection);
                command.ExecuteNonQuery();

                // Add Layer column if it doesn't exist (migration for existing databases)
                try
                {
                    string addLayerColumnQuery = @"ALTER TABLE Items ADD COLUMN Layer INTEGER NOT NULL DEFAULT 0";
                    using var addColumnCommand = new SqliteCommand(addLayerColumnQuery, connection);
                    addColumnCommand.ExecuteNonQuery();
                }
                catch (SqliteException)
                {
                    // Column already exists, ignore
                }

                // Add ServerName column if it doesn't exist (migration for existing databases)
                try
                {
                    string addServerNameColumnQuery = @"ALTER TABLE Items ADD COLUMN ServerName TEXT NOT NULL DEFAULT ''";
                    using var addServerNameColumnCommand = new SqliteCommand(addServerNameColumnQuery, connection);
                    addServerNameColumnCommand.ExecuteNonQuery();
                }
                catch (SqliteException)
                {
                    // Column already exists, ignore
                }

                // Create index for faster lookups
                string createIndexQuery = @"
                    CREATE INDEX IF NOT EXISTS idx_items_serial ON Items(Serial);
                    CREATE INDEX IF NOT EXISTS idx_items_character ON Items(Character);
                    CREATE INDEX IF NOT EXISTS idx_items_updated_time ON Items(UpdatedTime);
                    CREATE INDEX IF NOT EXISTS idx_items_container ON Items(Container);
                    CREATE INDEX IF NOT EXISTS idx_items_graphic ON Items(Graphic);
                    CREATE INDEX IF NOT EXISTS idx_items_graphic_hue ON Items(Graphic, Hue);
                    CREATE INDEX IF NOT EXISTS idx_items_on_ground ON Items(OnGround);
                    CREATE INDEX IF NOT EXISTS idx_items_character_updated ON Items(Character, UpdatedTime);
                    CREATE INDEX IF NOT EXISTS idx_items_server_character ON Items(ServerName, Character);";

                using var indexCommand = new SqliteCommand(createIndexQuery, connection);
                indexCommand.ExecuteNonQuery();
            }
        }

        public async Task AddOrUpdateItemAsync(ItemInfo itemInfo)
        {
            var profile = ProfileManager.CurrentProfile;
            if (!_initialized || profile == null || !profile.ItemDatabaseEnabled)
                return;

            await Task.Run(() =>
            {
                try
                {
                    lock (_dbLock)
                    {
                        using var connection = new SqliteConnection($"Data Source={_databasePath}");
                        connection.Open();

                        string upsertQuery = @"
                            INSERT INTO Items
                            (Serial, Graphic, Hue, Name, Properties, Container, Layer, UpdatedTime, Character, CharacterName, ServerName, X, Y, OnGround)
                            VALUES
                            (@Serial, @Graphic, @Hue, @Name, @Properties, @Container, @Layer, @UpdatedTime, @Character, @CharacterName, @ServerName, @X, @Y, @OnGround)
                            ON CONFLICT(Serial) DO UPDATE SET
                                Graphic = excluded.Graphic,
                                Hue = excluded.Hue,
                                Name = CASE WHEN excluded.Name = '' THEN Items.Name ELSE excluded.Name END,
                                Properties = CASE WHEN excluded.Properties = '' THEN Items.Properties ELSE excluded.Properties END,
                                Container = excluded.Container,
                                Layer = excluded.Layer,
                                UpdatedTime = excluded.UpdatedTime,
                                Character = excluded.Character,
                                CharacterName = CASE WHEN excluded.CharacterName = '' THEN Items.CharacterName ELSE excluded.CharacterName END,
                                ServerName = CASE WHEN excluded.ServerName = '' THEN Items.ServerName ELSE excluded.ServerName END,
                                X = excluded.X,
                                Y = excluded.Y,
                                OnGround = excluded.OnGround";

                        using var command = new SqliteCommand(upsertQuery, connection);
                        command.Parameters.AddWithValue("@Serial", itemInfo.Serial);
                        command.Parameters.AddWithValue("@Graphic", itemInfo.Graphic);
                        command.Parameters.AddWithValue("@Hue", itemInfo.Hue);
                        command.Parameters.AddWithValue("@Name", itemInfo.Name ?? string.Empty);
                        command.Parameters.AddWithValue("@Properties", itemInfo.Properties ?? string.Empty);
                        command.Parameters.AddWithValue("@Container", itemInfo.Container);
                        command.Parameters.AddWithValue("@Layer", (int)itemInfo.Layer);
                        command.Parameters.AddWithValue("@UpdatedTime", itemInfo.UpdatedTime.ToString("yyyy-MM-dd HH:mm:ss"));
                        command.Parameters.AddWithValue("@Character", itemInfo.Character);
                        command.Parameters.AddWithValue("@CharacterName", itemInfo.CharacterName ?? string.Empty);
                        command.Parameters.AddWithValue("@ServerName", itemInfo.ServerName ?? string.Empty);
                        command.Parameters.AddWithValue("@X", itemInfo.X);
                        command.Parameters.AddWithValue("@Y", itemInfo.Y);
                        command.Parameters.AddWithValue("@OnGround", itemInfo.OnGround ? 1 : 0);

                        command.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Failed to add/update item {itemInfo.Serial} in database: {ex}");
                }
            });
        }

        public async Task AddOrUpdateItemsAsync(IEnumerable<ItemInfo> items)
        {
            var profile = ProfileManager.CurrentProfile;
            if (!_initialized || profile == null || !profile.ItemDatabaseEnabled)
                return;

            await Task.Run(() =>
            {
                try
                {
                    lock (_dbLock)
                    {
                        using var connection = new SqliteConnection($"Data Source={_databasePath}");
                        connection.Open();

                        using var transaction = connection.BeginTransaction();

                        string upsertQuery = @"
                            INSERT INTO Items
                            (Serial, Graphic, Hue, Name, Properties, Container, Layer, UpdatedTime, Character, CharacterName, ServerName, X, Y, OnGround)
                            VALUES
                            (@Serial, @Graphic, @Hue, @Name, @Properties, @Container, @Layer, @UpdatedTime, @Character, @CharacterName, @ServerName, @X, @Y, @OnGround)
                            ON CONFLICT(Serial) DO UPDATE SET
                                Graphic = excluded.Graphic,
                                Hue = excluded.Hue,
                                Name = CASE WHEN excluded.Name = '' THEN Items.Name ELSE excluded.Name END,
                                Properties = CASE WHEN excluded.Properties = '' THEN Items.Properties ELSE excluded.Properties END,
                                Container = excluded.Container,
                                Layer = excluded.Layer,
                                UpdatedTime = excluded.UpdatedTime,
                                Character = excluded.Character,
                                CharacterName = CASE WHEN excluded.CharacterName = '' THEN Items.CharacterName ELSE excluded.CharacterName END,
                                ServerName = CASE WHEN excluded.ServerName = '' THEN Items.ServerName ELSE excluded.ServerName END,
                                X = excluded.X,
                                Y = excluded.Y,
                                OnGround = excluded.OnGround";

                        using var command = new SqliteCommand(upsertQuery, connection, transaction);

                        foreach (var itemInfo in items)
                        {
                            command.Parameters.Clear();
                            command.Parameters.AddWithValue("@Serial", itemInfo.Serial);
                            command.Parameters.AddWithValue("@Graphic", itemInfo.Graphic);
                            command.Parameters.AddWithValue("@Hue", itemInfo.Hue);
                            command.Parameters.AddWithValue("@Name", itemInfo.Name ?? string.Empty);
                            command.Parameters.AddWithValue("@Properties", itemInfo.Properties ?? string.Empty);
                            command.Parameters.AddWithValue("@Container", itemInfo.Container);
                            command.Parameters.AddWithValue("@Layer", (int)itemInfo.Layer);
                            command.Parameters.AddWithValue("@UpdatedTime", itemInfo.UpdatedTime.ToString("yyyy-MM-dd HH:mm:ss"));
                            command.Parameters.AddWithValue("@Character", itemInfo.Character);
                            command.Parameters.AddWithValue("@CharacterName", itemInfo.CharacterName ?? string.Empty);
                            command.Parameters.AddWithValue("@ServerName", itemInfo.ServerName ?? string.Empty);
                            command.Parameters.AddWithValue("@X", itemInfo.X);
                            command.Parameters.AddWithValue("@Y", itemInfo.Y);
                            command.Parameters.AddWithValue("@OnGround", itemInfo.OnGround ? 1 : 0);

                            command.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Failed to add/update items in database: {ex}");
                }
            });
        }

        public void GetItemInfo(uint serial, Action<ItemInfo> onFound)
        {
            var profile = ProfileManager.CurrentProfile;
            if (!_initialized || profile == null || !profile.ItemDatabaseEnabled)
            {
                Task.Run(() => onFound?.Invoke(null));
                return;
            }

            Task.Run(() =>
            {
                ItemInfo resultItem = null;
                bool shouldInvokeCallback = false;

                try
                {
                    lock (_dbLock)
                    {
                        using var connection = new SqliteConnection($"Data Source={_databasePath}");
                        connection.Open();

                        string selectQuery = @"
                            SELECT Serial, Graphic, Hue, Name, Properties, Container, Layer, UpdatedTime, Character, CharacterName, ServerName, X, Y, OnGround
                            FROM Items
                            WHERE Serial = @Serial";

                        using var command = new SqliteCommand(selectQuery, connection);
                        command.Parameters.AddWithValue("@Serial", serial);

                        using var reader = command.ExecuteReader();
                        if (reader.Read())
                        {
                            resultItem = CreateItemInfoFromReader(reader);
                        }
                        shouldInvokeCallback = true;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Failed to get item {serial} from database: {ex}");
                    resultItem = null;
                    shouldInvokeCallback = true;
                }

                if (shouldInvokeCallback)
                {
                    onFound?.Invoke(resultItem);
                }
            });
        }

        public void SearchItems(Action<List<ItemInfo>> onResults,
            uint? serial = null,
            ushort? graphic = null,
            ushort? hue = null,
            string name = null,
            string properties = null,
            uint? container = null,
            Layer? layer = null,
            DateTime? updatedAfter = null,
            DateTime? updatedBefore = null,
            uint? character = null,
            string characterName = null,
            string serverName = null,
            bool? onGround = null,
            int limit = 1000)
        {
            var profile = ProfileManager.CurrentProfile;
            if (!_initialized || profile == null || !profile.ItemDatabaseEnabled)
            {
                Task.Run(() => onResults?.Invoke(new List<ItemInfo>()));
                return;
            }

            Task.Run(() =>
            {
                List<ItemInfo> results = new List<ItemInfo>();
                bool shouldInvokeCallback = false;

                try
                {
                    lock (_dbLock)
                    {
                        using var connection = new SqliteConnection($"Data Source={_databasePath}");
                        connection.Open();

                        var whereConditions = new List<string>();
                        var parameters = new List<(string name, object value)>();

                        if (serial.HasValue)
                        {
                            whereConditions.Add("Serial = @Serial");
                            parameters.Add(("@Serial", serial.Value));
                        }

                        if (graphic.HasValue)
                        {
                            whereConditions.Add("Graphic = @Graphic");
                            parameters.Add(("@Graphic", graphic.Value));
                        }

                        if (hue.HasValue)
                        {
                            whereConditions.Add("Hue = @Hue");
                            parameters.Add(("@Hue", hue.Value));
                        }

                        if (!string.IsNullOrEmpty(name))
                        {
                            whereConditions.Add("Name LIKE @Name ESCAPE '\\' COLLATE NOCASE");
                            parameters.Add(("@Name", $"%{EscapeLikePattern(name)}%"));
                        }

                        if (!string.IsNullOrEmpty(properties))
                        {
                            whereConditions.Add("Properties LIKE @Properties ESCAPE '\\' COLLATE NOCASE");
                            parameters.Add(("@Properties", $"%{EscapeLikePattern(properties)}%"));
                        }

                        if (container.HasValue)
                        {
                            whereConditions.Add("Container = @Container");
                            parameters.Add(("@Container", container.Value));
                        }

                        if (layer.HasValue)
                        {
                            whereConditions.Add("Layer = @Layer");
                            parameters.Add(("@Layer", (int)layer.Value));
                        }

                        if (updatedAfter.HasValue)
                        {
                            whereConditions.Add("UpdatedTime >= @UpdatedAfter");
                            parameters.Add(("@UpdatedAfter", updatedAfter.Value.ToString("yyyy-MM-dd HH:mm:ss")));
                        }

                        if (updatedBefore.HasValue)
                        {
                            whereConditions.Add("UpdatedTime <= @UpdatedBefore");
                            parameters.Add(("@UpdatedBefore", updatedBefore.Value.ToString("yyyy-MM-dd HH:mm:ss")));
                        }

                        if (character.HasValue)
                        {
                            whereConditions.Add("Character = @Character");
                            parameters.Add(("@Character", character.Value));
                        }

                        if (!string.IsNullOrEmpty(characterName))
                        {
                            whereConditions.Add("CharacterName LIKE @CharacterName ESCAPE '\\' COLLATE NOCASE");
                            parameters.Add(("@CharacterName", $"%{EscapeLikePattern(characterName)}%"));
                        }

                        if (!string.IsNullOrEmpty(serverName))
                        {
                            whereConditions.Add("ServerName LIKE @ServerName ESCAPE '\\' COLLATE NOCASE");
                            parameters.Add(("@ServerName", $"%{EscapeLikePattern(serverName)}%"));
                        }

                        if (onGround.HasValue)
                        {
                            whereConditions.Add("OnGround = @OnGround");
                            parameters.Add(("@OnGround", onGround.Value ? 1 : 0));
                        }

                        string selectQuery = @"
                            SELECT Serial, Graphic, Hue, Name, Properties, Container, Layer, UpdatedTime, Character, CharacterName, ServerName, X, Y, OnGround
                            FROM Items";

                        if (whereConditions.Count > 0)
                        {
                            selectQuery += " WHERE " + string.Join(" AND ", whereConditions);
                        }

                        selectQuery += " ORDER BY UpdatedTime DESC";

                        if (limit > 0)
                        {
                            selectQuery += $" LIMIT {limit}";
                        }

                        using var command = new SqliteCommand(selectQuery, connection);

                        foreach (var (paramName, paramValue) in parameters)
                        {
                            command.Parameters.AddWithValue(paramName, paramValue);
                        }

                        using var reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            results.Add(CreateItemInfoFromReader(reader));
                        }
                        shouldInvokeCallback = true;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Failed to search items in database: {ex}");
                    results = new List<ItemInfo>();
                    shouldInvokeCallback = true;
                }

                if (shouldInvokeCallback)
                {
                    onResults?.Invoke(results);
                }
            });
        }

        private static string EscapeLikePattern(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            return input.Replace("\\", "\\\\")
                       .Replace("%", "\\%")
                       .Replace("_", "\\_");
        }

        private ItemInfo CreateItemInfoFromReader(SqliteDataReader reader)
        {
            return new ItemInfo
            {
                Serial = Convert.ToUInt32(reader["Serial"]),
                Graphic = Convert.ToUInt16(reader["Graphic"]),
                Hue = Convert.ToUInt16(reader["Hue"]),
                Name = reader["Name"].ToString() ?? string.Empty,
                Properties = reader["Properties"].ToString() ?? string.Empty,
                Container = Convert.ToUInt32(reader["Container"]),
                Layer = (Layer)Convert.ToInt32(reader["Layer"]),
                UpdatedTime = DateTime.Parse(reader["UpdatedTime"].ToString()),
                Character = Convert.ToUInt32(reader["Character"]),
                CharacterName = reader["CharacterName"].ToString() ?? string.Empty,
                ServerName = reader["ServerName"].ToString() ?? string.Empty,
                X = Convert.ToInt32(reader["X"]),
                Y = Convert.ToInt32(reader["Y"]),
                OnGround = Convert.ToInt32(reader["OnGround"]) == 1
            };
        }

        public async Task ClearOldDataAsync(TimeSpan maxAge)
        {
            var profile = ProfileManager.CurrentProfile;
            if (!_initialized || profile == null || !profile.ItemDatabaseEnabled)
                return;

            await Task.Run(() =>
            {
                try
                {
                    lock (_dbLock)
                    {
                        using var connection = new SqliteConnection($"Data Source={_databasePath}");
                        connection.Open();

                        DateTime cutoffTime = DateTime.Now - maxAge;
                        string deleteQuery = @"DELETE FROM Items WHERE UpdatedTime < @CutoffTime";

                        using var command = new SqliteCommand(deleteQuery, connection);
                        command.Parameters.AddWithValue("@CutoffTime", cutoffTime.ToString("yyyy-MM-dd HH:mm:ss"));

                        int deletedRows = command.ExecuteNonQuery();
                        Log.Trace($"Cleared {deletedRows} old items from database (older than {cutoffTime})");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Failed to clear old data from database: {ex}");
                }
            });
        }

        public void AddOrUpdateItem(Item item, World world)
        {
            if (!_initialized || item == null || world?.Player == null || ProfileManager.CurrentProfile?.ItemDatabaseEnabled == false)
                return;

            if (item.ItemData.IsDoor || item.ItemData.IsLight || item.ItemData.IsInternal || item.ItemData.IsRoof || item.ItemData.IsWall  || item.IsMulti || item.IsCorpse || StaticFilters.IsRock(item.Graphic) || StaticFilters.IsTree(item.Graphic, out _))
                return;
            
            // Check if ItemData is accessible (TileData might not be loaded yet)
            Layer layer = Layer.Invalid;
            try
            {
                layer = (Layer)item.ItemData.Layer;
            }
            catch { }

            var itemInfo = new ItemInfo
            {
                Serial = item.Serial,
                Graphic = item.Graphic,
                Hue = item.Hue,
                Name = item.Name ?? string.Empty,
                Properties = string.Empty, // Will be filled by tooltip if available
                Container = item.Container,
                Layer = layer,
                UpdatedTime = DateTime.Now,
                Character = world.Player.Serial,
                CharacterName = world.Player.Name ?? string.Empty,
                ServerName = ProfileManager.CurrentProfile?.ServerName ?? "unknown",
                X = item.X,
                Y = item.Y,
                OnGround = item.OnGround
            };

            // Try to get properties from OPL
            if (world.OPL.TryGetNameAndData(item.Serial, out string oplName, out string oplData))
            {
                if (!string.IsNullOrEmpty(oplName))
                {
                    itemInfo.Name = oplName;
                }
                if (!string.IsNullOrEmpty(oplData))
                {
                    itemInfo.Properties = oplData;
                }
            }

            _pendingItems.Enqueue(itemInfo);

            lock (_timerLock)
            {
                if (_pendingItemsTimer != null)
                    return;

                _pendingItemsTimer = new Timer(3000);
                if (_pendingItemsTimer != null)
                {
                    _pendingItemsTimer.Elapsed += PendingItemsTimerOnElapsed;
                    _pendingItemsTimer.Start();
                }
            }
        }

        private void PendingItemsTimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            _ = BulkPending();
        }

        private async Task BulkPending()
        {
            await Task.Run(() =>
            {
                Log.Debug("Bulking items");
                List<ItemInfo> items = new();
                while (_pendingItems.TryDequeue(out ItemInfo itemInfo))
                {
                    items.Add(itemInfo);
                }

                lock (_timerLock)
                {
                    _pendingItemsTimer?.Dispose();
                    _pendingItemsTimer = null;
                }

                _ = AddOrUpdateItemsAsync(items);
            });
        }
    }
}
