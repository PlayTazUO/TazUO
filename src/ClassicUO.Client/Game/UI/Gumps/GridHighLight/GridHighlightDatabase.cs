using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Utility.Logging;
using Microsoft.Data.Sqlite;

namespace ClassicUO.Game.UI.Gumps.GridHighLight
{
    /// <summary>
    /// SQLite-backed store for a single profile's grid highlight rules. Each profile keeps its own
    /// <c>gridhighlights.db</c> alongside its <c>profile.json</c>, so no profile key is needed; the rows
    /// in one database belong to exactly one profile.
    /// <para>
    /// Every setting is stored in its own column. The scalar settings (and the equipment-slot flags)
    /// live on the <c>grid_highlights</c> table, one row per rule; the variable-length lists
    /// (item names, properties, excluded negatives, required rarities) live in child tables keyed back to
    /// the rule by its id. This replaces the legacy <see cref="Profile.GridHighlightSetup"/> storage that
    /// lived inside <c>profile.json</c>; existing profiles are migrated on first load
    /// (see <see cref="LoadForProfile"/>).
    /// </para>
    /// </summary>
    public sealed class GridHighlightDatabase : SqliteDatabase
    {
        private const string DB_FILE = "gridhighlights.db";

        private const string TABLE = "grid_highlights";
        private const string TABLE_ITEM_NAMES = "grid_highlight_item_names";
        private const string TABLE_PROPERTIES = "grid_highlight_properties";
        private const string TABLE_EXCLUDE_NEGATIVES = "grid_highlight_exclude_negatives";
        private const string TABLE_REQUIRED_RARITIES = "grid_highlight_required_rarities";

        // The equipment-slot flags, mapped to their columns and to the GridHighlightSlot members. Kept in
        // one place so the schema, the writes and the reads can never drift apart. All default to enabled
        // except "Other" (matches GridHighlightSlot's defaults).
        private static readonly (string Column, Func<GridHighlightSlot, bool> Get, Action<GridHighlightSlot, bool> Set)[] SlotColumns =
        {
            ("slot_talisman", s => s.Talisman, (s, v) => s.Talisman = v),
            ("slot_right_hand", s => s.RightHand, (s, v) => s.RightHand = v),
            ("slot_left_hand", s => s.LeftHand, (s, v) => s.LeftHand = v),
            ("slot_head", s => s.Head, (s, v) => s.Head = v),
            ("slot_earring", s => s.Earring, (s, v) => s.Earring = v),
            ("slot_neck", s => s.Neck, (s, v) => s.Neck = v),
            ("slot_chest", s => s.Chest, (s, v) => s.Chest = v),
            ("slot_shirt", s => s.Shirt, (s, v) => s.Shirt = v),
            ("slot_back", s => s.Back, (s, v) => s.Back = v),
            ("slot_robe", s => s.Robe, (s, v) => s.Robe = v),
            ("slot_arms", s => s.Arms, (s, v) => s.Arms = v),
            ("slot_hands", s => s.Hands, (s, v) => s.Hands = v),
            ("slot_bracelet", s => s.Bracelet, (s, v) => s.Bracelet = v),
            ("slot_ring", s => s.Ring, (s, v) => s.Ring = v),
            ("slot_belt", s => s.Belt, (s, v) => s.Belt = v),
            ("slot_skirt", s => s.Skirt, (s, v) => s.Skirt = v),
            ("slot_legs", s => s.Legs, (s, v) => s.Legs = v),
            ("slot_footwear", s => s.Footwear, (s, v) => s.Footwear = v),
            ("slot_other", s => s.Other, (s, v) => s.Other = v),
        };

        private static GridHighlightDatabase _current;
        private static string _currentDirectory;

        /// <summary>
        /// The database for the currently loaded profile, or <see langword="null"/> if no profile
        /// location is known yet.
        /// </summary>
        public static GridHighlightDatabase Current => GetForProfilePath(ProfileManager.ProfilePath);

        /// <summary>
        /// Returns the database that lives in <paramref name="profileDirectory"/>, creating (and caching)
        /// it as needed. Switching to a new directory disposes the previously cached instance.
        /// </summary>
        public static GridHighlightDatabase GetForProfilePath(string profileDirectory)
        {
            if (string.IsNullOrEmpty(profileDirectory))
                return null;

            if (_current != null && string.Equals(_currentDirectory, profileDirectory, StringComparison.Ordinal))
                return _current;

            _current?.Dispose();
            _current = new GridHighlightDatabase(profileDirectory);
            _currentDirectory = profileDirectory;
            return _current;
        }

        /// <summary>
        /// Creates the database in <paramref name="profileDirectory"/> and ensures its schema.
        /// </summary>
        public GridHighlightDatabase(string profileDirectory) : base(DB_FILE, profileDirectory)
        {
            CreateBackups();
            EnsureSchemaAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        private async Task EnsureSchemaAsync()
        {
            List<SqliteColumn> columns = new()
            {
                SqliteColumn.Int("id", primaryKey: true, notNull: true),
                SqliteColumn.Int("enabled", notNull: true, def: "1"),
                SqliteColumn.Str("name", def: "''"),
                SqliteColumn.Int("hue", notNull: true, def: "0"),
                SqliteColumn.Str("highlight_color", notNull: true, def: "'#FF0000'"),
                SqliteColumn.Int("accept_extra_properties", notNull: true, def: "1"),
                SqliteColumn.Int("overweight", notNull: true, def: "0"),
                SqliteColumn.Int("minimum_weight", notNull: true, def: "0"),
                SqliteColumn.Int("maximum_weight", notNull: true, def: "0"),
                SqliteColumn.Int("minimum_property", notNull: true, def: "0"),
                SqliteColumn.Int("maximum_property", notNull: true, def: "0"),
                SqliteColumn.Int("minimum_matching_property", notNull: true, def: "0"),
                SqliteColumn.Int("maximum_matching_property", notNull: true, def: "0"),
                SqliteColumn.Int("loot_on_match", notNull: true, def: "0"),
                SqliteColumn.Int("destination_container", notNull: true, def: "0"),
                SqliteColumn.Int("is_highlight_properties", notNull: true, def: "1"),
            };

            foreach ((string column, _, _) in SlotColumns)
                columns.Add(SqliteColumn.Int(column, notNull: true, def: column == "slot_other" ? "0" : "1"));

            await EnsureTableAsync(TABLE, columns.ToArray()).ConfigureAwait(false);

            await EnsureTableAsync(TABLE_ITEM_NAMES,
                SqliteColumn.Int("rule_id", primaryKey: true, notNull: true),
                SqliteColumn.Int("ord", primaryKey: true, notNull: true),
                SqliteColumn.Str("value", notNull: true, def: "''")).ConfigureAwait(false);

            await EnsureTableAsync(TABLE_EXCLUDE_NEGATIVES,
                SqliteColumn.Int("rule_id", primaryKey: true, notNull: true),
                SqliteColumn.Int("ord", primaryKey: true, notNull: true),
                SqliteColumn.Str("value", notNull: true, def: "''")).ConfigureAwait(false);

            await EnsureTableAsync(TABLE_REQUIRED_RARITIES,
                SqliteColumn.Int("rule_id", primaryKey: true, notNull: true),
                SqliteColumn.Int("ord", primaryKey: true, notNull: true),
                SqliteColumn.Str("value", notNull: true, def: "''")).ConfigureAwait(false);

            await EnsureTableAsync(TABLE_PROPERTIES,
                SqliteColumn.Int("rule_id", primaryKey: true, notNull: true),
                SqliteColumn.Int("ord", primaryKey: true, notNull: true),
                SqliteColumn.Str("name", def: "''"),
                SqliteColumn.Int("min_value", notNull: true, def: "-1"),
                SqliteColumn.Int("is_optional", notNull: true, def: "0")).ConfigureAwait(false);
        }

        /// <summary>
        /// Loads this profile's rules from the database into <see cref="Profile.GridHighlightSetup"/>,
        /// migrating any legacy <c>profile.json</c> storage on the first run. Returns
        /// <see langword="true"/> when a migration was performed (and the profile should therefore be
        /// re-saved to drop its legacy copy).
        /// </summary>
        public bool LoadForProfile(Profile profile)
        {
            if (profile == null)
                return false;

            List<GridHighlightSetupEntry> stored = Load();

            if (stored.Count > 0)
            {
                profile.GridHighlightSetup = stored;
                ClearLegacy(profile);
                return false;
            }

            // Nothing in the database yet: migrate whatever legacy storage exists.
            List<GridHighlightSetupEntry> migrated = MigrateLegacy(profile);
            profile.GridHighlightSetup = migrated;

            if (migrated.Count > 0)
            {
                Save(migrated);
                ClearLegacy(profile);
                return true;
            }

            return false;
        }

        /// <summary>Persists the given profile's rules to the database.</summary>
        public void SaveForProfile(Profile profile)
        {
            if (profile == null)
                return;

            Save(profile.GridHighlightSetup ?? new List<GridHighlightSetupEntry>());
        }

        /// <summary>Reads all rules, ordered as they should appear.</summary>
        public List<GridHighlightSetupEntry> Load()
            => LoadAsync().ConfigureAwait(false).GetAwaiter().GetResult();

        /// <inheritdoc cref="Load()"/>
        public Task<List<GridHighlightSetupEntry>> LoadAsync()
        {
            return ExecuteAsync(async connection =>
            {
                List<GridHighlightSetupEntry> ordered = new();
                Dictionary<long, GridHighlightSetupEntry> byId = new();

                // Main rule rows. Read fully before opening any other reader (the SQLite provider allows
                // only one open reader per connection).
                await using (SqliteCommand cmd = connection.CreateCommand())
                {
                    cmd.CommandText = BuildMainSelect();
                    await using SqliteDataReader reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
                    while (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        long id = reader.GetInt64(0);
                        GridHighlightSetupEntry entry = ReadMainRow(reader);
                        ordered.Add(entry);
                        byId[id] = entry;
                    }
                }

                await LoadStringChildAsync(connection, TABLE_ITEM_NAMES, byId, (e, v) => e.ItemNames.Add(v)).ConfigureAwait(false);
                await LoadStringChildAsync(connection, TABLE_EXCLUDE_NEGATIVES, byId, (e, v) => e.ExcludeNegatives.Add(v)).ConfigureAwait(false);
                await LoadStringChildAsync(connection, TABLE_REQUIRED_RARITIES, byId, (e, v) => e.RequiredRarities.Add(v)).ConfigureAwait(false);
                await LoadPropertiesAsync(connection, byId).ConfigureAwait(false);

                return ordered;
            });
        }

        /// <summary>
        /// Replaces every rule stored in this database with <paramref name="entries"/>. The rewrite runs
        /// inside a single transaction so a reader never observes a partially written set.
        /// </summary>
        public void Save(IReadOnlyList<GridHighlightSetupEntry> entries)
            => SaveAsync(entries).ConfigureAwait(false).GetAwaiter().GetResult();

        /// <inheritdoc cref="Save(IReadOnlyList{GridHighlightSetupEntry})"/>
        public Task SaveAsync(IReadOnlyList<GridHighlightSetupEntry> entries)
        {
            return ExecuteAsync<object>(async connection =>
            {
                await using SqliteTransaction transaction = (SqliteTransaction)await connection.BeginTransactionAsync().ConfigureAwait(false);

                foreach (string table in new[] { TABLE, TABLE_ITEM_NAMES, TABLE_EXCLUDE_NEGATIVES, TABLE_REQUIRED_RARITIES, TABLE_PROPERTIES })
                {
                    await using SqliteCommand delete = connection.CreateCommand();
                    delete.Transaction = transaction;
                    delete.CommandText = $"DELETE FROM {QuoteIdentifier(table)}";
                    await delete.ExecuteNonQueryAsync().ConfigureAwait(false);
                }

                for (int i = 0; entries != null && i < entries.Count; i++)
                {
                    GridHighlightSetupEntry entry = entries[i];

                    await InsertMainRowAsync(connection, transaction, i, entry).ConfigureAwait(false);
                    await InsertStringChildAsync(connection, transaction, TABLE_ITEM_NAMES, i, entry.ItemNames).ConfigureAwait(false);
                    await InsertStringChildAsync(connection, transaction, TABLE_EXCLUDE_NEGATIVES, i, entry.ExcludeNegatives).ConfigureAwait(false);
                    await InsertStringChildAsync(connection, transaction, TABLE_REQUIRED_RARITIES, i, entry.RequiredRarities).ConfigureAwait(false);
                    await InsertPropertiesAsync(connection, transaction, i, entry.Properties).ConfigureAwait(false);
                }

                await transaction.CommitAsync().ConfigureAwait(false);
                return null;
            });
        }

        private static string BuildMainSelect()
        {
            System.Text.StringBuilder sb = new();
            sb.Append("SELECT id, enabled, name, hue, highlight_color, accept_extra_properties, overweight, ");
            sb.Append("minimum_weight, maximum_weight, minimum_property, maximum_property, ");
            sb.Append("minimum_matching_property, maximum_matching_property, loot_on_match, ");
            sb.Append("destination_container, is_highlight_properties");
            foreach ((string column, _, _) in SlotColumns)
            {
                sb.Append(", ");
                sb.Append(QuoteIdentifier(column));
            }
            sb.Append(" FROM ");
            sb.Append(QuoteIdentifier(TABLE));
            sb.Append(" ORDER BY id");
            return sb.ToString();
        }

        private static GridHighlightSetupEntry ReadMainRow(SqliteDataReader reader)
        {
            GridHighlightSetupEntry entry = new()
            {
                Enabled = reader.GetInt64(1) != 0,
                Name = reader.IsDBNull(2) ? null : reader.GetString(2),
                Hue = (ushort)reader.GetInt64(3),
                HighlightColor = reader.IsDBNull(4) ? "#FF0000" : reader.GetString(4),
                AcceptExtraProperties = reader.GetInt64(5) != 0,
                Overweight = reader.GetInt64(6) != 0,
                MinimumWeight = (int)reader.GetInt64(7),
                MaximumWeight = (int)reader.GetInt64(8),
                MinimumProperty = (int)reader.GetInt64(9),
                MaximumProperty = (int)reader.GetInt64(10),
                MinimumMatchingProperty = (int)reader.GetInt64(11),
                MaximumMatchingProperty = (int)reader.GetInt64(12),
                LootOnMatch = reader.GetInt64(13) != 0,
                DestinationContainer = (uint)reader.GetInt64(14),
                IsHighlightProperties = reader.GetInt64(15) != 0,
                GridHighlightSlot = new GridHighlightSlot(),
                ItemNames = new List<string>(),
                Properties = new List<GridHighlightProperty>(),
                ExcludeNegatives = new List<string>(),
                RequiredRarities = new List<string>(),
            };

            const int slotOffset = 16;
            for (int i = 0; i < SlotColumns.Length; i++)
                SlotColumns[i].Set(entry.GridHighlightSlot, reader.GetInt64(slotOffset + i) != 0);

            return entry;
        }

        private async Task InsertMainRowAsync(SqliteConnection connection, SqliteTransaction transaction, int id, GridHighlightSetupEntry entry)
        {
            GridHighlightSlot slot = entry.GridHighlightSlot ?? new GridHighlightSlot();

            System.Text.StringBuilder columns = new(
                "id, enabled, name, hue, highlight_color, accept_extra_properties, overweight, " +
                "minimum_weight, maximum_weight, minimum_property, maximum_property, " +
                "minimum_matching_property, maximum_matching_property, loot_on_match, " +
                "destination_container, is_highlight_properties");
            System.Text.StringBuilder values = new(
                "$id, $enabled, $name, $hue, $highlight_color, $accept_extra_properties, $overweight, " +
                "$minimum_weight, $maximum_weight, $minimum_property, $maximum_property, " +
                "$minimum_matching_property, $maximum_matching_property, $loot_on_match, " +
                "$destination_container, $is_highlight_properties");

            foreach ((string column, _, _) in SlotColumns)
            {
                columns.Append(", ");
                columns.Append(QuoteIdentifier(column));
                values.Append(", $");
                values.Append(column);
            }

            await using SqliteCommand cmd = connection.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = $"INSERT INTO {QuoteIdentifier(TABLE)} ({columns}) VALUES ({values})";

            cmd.Parameters.AddWithValue("$id", id);
            cmd.Parameters.AddWithValue("$enabled", entry.Enabled ? 1 : 0);
            cmd.Parameters.AddWithValue("$name", (object)entry.Name ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$hue", entry.Hue);
            cmd.Parameters.AddWithValue("$highlight_color", entry.HighlightColor ?? "#FF0000");
            cmd.Parameters.AddWithValue("$accept_extra_properties", entry.AcceptExtraProperties ? 1 : 0);
            cmd.Parameters.AddWithValue("$overweight", entry.Overweight ? 1 : 0);
            cmd.Parameters.AddWithValue("$minimum_weight", entry.MinimumWeight);
            cmd.Parameters.AddWithValue("$maximum_weight", entry.MaximumWeight);
            cmd.Parameters.AddWithValue("$minimum_property", entry.MinimumProperty);
            cmd.Parameters.AddWithValue("$maximum_property", entry.MaximumProperty);
            cmd.Parameters.AddWithValue("$minimum_matching_property", entry.MinimumMatchingProperty);
            cmd.Parameters.AddWithValue("$maximum_matching_property", entry.MaximumMatchingProperty);
            cmd.Parameters.AddWithValue("$loot_on_match", entry.LootOnMatch ? 1 : 0);
            cmd.Parameters.AddWithValue("$destination_container", entry.DestinationContainer);
            cmd.Parameters.AddWithValue("$is_highlight_properties", entry.IsHighlightProperties ? 1 : 0);

            foreach ((string column, Func<GridHighlightSlot, bool> get, _) in SlotColumns)
                cmd.Parameters.AddWithValue($"${column}", get(slot) ? 1 : 0);

            await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
        }

        private async Task LoadStringChildAsync(SqliteConnection connection, string table, Dictionary<long, GridHighlightSetupEntry> byId, Action<GridHighlightSetupEntry, string> add)
        {
            await using SqliteCommand cmd = connection.CreateCommand();
            cmd.CommandText = $"SELECT rule_id, value FROM {QuoteIdentifier(table)} ORDER BY rule_id, ord";
            await using SqliteDataReader reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                if (byId.TryGetValue(reader.GetInt64(0), out GridHighlightSetupEntry entry))
                    add(entry, reader.IsDBNull(1) ? string.Empty : reader.GetString(1));
            }
        }

        private async Task LoadPropertiesAsync(SqliteConnection connection, Dictionary<long, GridHighlightSetupEntry> byId)
        {
            await using SqliteCommand cmd = connection.CreateCommand();
            cmd.CommandText = $"SELECT rule_id, name, min_value, is_optional FROM {QuoteIdentifier(TABLE_PROPERTIES)} ORDER BY rule_id, ord";
            await using SqliteDataReader reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                if (!byId.TryGetValue(reader.GetInt64(0), out GridHighlightSetupEntry entry))
                    continue;

                entry.Properties.Add(new GridHighlightProperty
                {
                    Name = reader.IsDBNull(1) ? null : reader.GetString(1),
                    MinValue = (int)reader.GetInt64(2),
                    IsOptional = reader.GetInt64(3) != 0
                });
            }
        }

        private async Task InsertStringChildAsync(SqliteConnection connection, SqliteTransaction transaction, string table, int ruleId, List<string> values)
        {
            if (values == null || values.Count == 0)
                return;

            for (int ord = 0; ord < values.Count; ord++)
            {
                await using SqliteCommand cmd = connection.CreateCommand();
                cmd.Transaction = transaction;
                cmd.CommandText = $"INSERT INTO {QuoteIdentifier(table)} (rule_id, ord, value) VALUES ($rule_id, $ord, $value)";
                cmd.Parameters.AddWithValue("$rule_id", ruleId);
                cmd.Parameters.AddWithValue("$ord", ord);
                cmd.Parameters.AddWithValue("$value", values[ord] ?? string.Empty);
                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
        }

        private async Task InsertPropertiesAsync(SqliteConnection connection, SqliteTransaction transaction, int ruleId, List<GridHighlightProperty> properties)
        {
            if (properties == null || properties.Count == 0)
                return;

            for (int ord = 0; ord < properties.Count; ord++)
            {
                GridHighlightProperty property = properties[ord];

                await using SqliteCommand cmd = connection.CreateCommand();
                cmd.Transaction = transaction;
                cmd.CommandText = $"INSERT INTO {QuoteIdentifier(TABLE_PROPERTIES)} (rule_id, ord, name, min_value, is_optional) VALUES ($rule_id, $ord, $name, $min_value, $is_optional)";
                cmd.Parameters.AddWithValue("$rule_id", ruleId);
                cmd.Parameters.AddWithValue("$ord", ord);
                cmd.Parameters.AddWithValue("$name", (object)property.Name ?? DBNull.Value);
                cmd.Parameters.AddWithValue("$min_value", property.MinValue);
                cmd.Parameters.AddWithValue("$is_optional", property.IsOptional ? 1 : 0);
                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
        }

#pragma warning disable CS0618 // Reading/clearing the obsolete legacy storage is the whole point of migration.
        private static List<GridHighlightSetupEntry> MigrateLegacy(Profile profile)
        {
            // Preferred legacy source: the GridHighlightSetup list that used to live in profile.json.
            if (profile.LegacyGridHighlightSetup is { Count: > 0 } legacy)
                return new List<GridHighlightSetupEntry>(legacy);

            // Older still: the parallel GridHighlight_* lists. This populates profile.GridHighlightSetup
            // and clears those lists.
            GridHighLightProfile.MigrateGridHighlightToSetup(profile);
            return profile.GridHighlightSetup ?? new List<GridHighlightSetupEntry>();
        }

        private static void ClearLegacy(Profile profile) => profile.LegacyGridHighlightSetup = new List<GridHighlightSetupEntry>();
#pragma warning restore CS0618
    }
}
