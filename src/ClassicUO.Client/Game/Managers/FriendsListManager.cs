using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Managers
{
    internal class FriendsListManager
    {
        private static FriendsListManager _instance;
        private FriendsListSave _save;
        private bool _loaded;

        public static FriendsListManager Instance => _instance ??= new FriendsListManager();

        private List<FriendEntry> Friends => _save.Friends;

        private FriendsListManager()
        {
            _save = new FriendsListSave();
            _loaded = false;
        }

        public void OnSceneLoad() => Load();

        public void OnSceneUnload() => Save();

        public void Load()
        {
            if (_loaded)
                return;

            _save = FriendsListSave.LoadWithMigration();
            _loaded = true;
        }

        public void Save()
        {
            if (!_loaded)
                return;

            _save.Save();
        }

        public List<FriendEntry> GetFriends()
        {
            if (!_loaded)
                Load();

            return new List<FriendEntry>(Friends);
        }

        public bool AddFriend(uint serial, string name)
        {
            if (!_loaded)
                Load();

            if (string.IsNullOrWhiteSpace(name))
                return false;

            // Check if friend already exists
            if (Friends.Exists(f => f.Serial == serial))
                return false;

            var friend = new FriendEntry
            {
                Serial = serial,
                Name = name.Trim(),
                DateAdded = DateTime.UtcNow
            };

            Friends.Add(friend);
            Save();
            return true;
        }

        public bool AddFriend(Mobile mobile)
        {
            if (mobile == null)
                return false;

            return AddFriend(mobile.Serial, mobile.Name);
        }

        public bool RemoveFriend(uint serial)
        {
            if (!_loaded)
                Load();

            FriendEntry friend = Friends.Find(f => f.Serial == serial);
            if (friend == null)
                return false;

            Friends.Remove(friend);
            Save();
            return true;
        }

        public bool RemoveFriend(string name)
        {
            if (!_loaded)
                Load();

            if (string.IsNullOrWhiteSpace(name))
                return false;

            FriendEntry friend = Friends.Find(f => string.Equals(f.Name, name.Trim(), StringComparison.OrdinalIgnoreCase));
            if (friend == null)
                return false;

            Friends.Remove(friend);
            Save();
            return true;
        }

        public bool IsFriend(uint serial)
        {
            if (!_loaded)
                Load();

            return Friends.Exists(f => f.Serial == serial);
        }

        public bool IsFriend(string name)
        {
            if (!_loaded)
                Load();

            if (string.IsNullOrWhiteSpace(name))
                return false;

            return Friends.Exists(f => string.Equals(f.Name, name.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        public FriendEntry GetFriend(uint serial)
        {
            if (!_loaded)
                Load();

            return Friends.Find(f => f.Serial == serial);
        }

        public FriendEntry GetFriend(string name)
        {
            if (!_loaded)
                Load();

            if (string.IsNullOrWhiteSpace(name))
                return null;

            return Friends.Find(f => string.Equals(f.Name, name.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        public IList<uint> GetAllFriends()
        {
            if (!_loaded)
                Load();

            return new List<uint>(Friends.Select(friend => friend.Serial));
        }
    }

    public class FriendEntry
    {
        public uint Serial { get; set; }
        public string Name { get; set; }
        public DateTime DateAdded { get; set; }
    }

    /// <summary>
    /// JSON-backed store for the friends list. Persisted to <c>friends.json</c> under the
    /// <see cref="SettingsScope.Server"/> folder. Saving/loading (with rotating backups) is handled by
    /// <see cref="JsonSave{T}"/>.
    /// </summary>
    public sealed class FriendsListSave : JsonSave<FriendsListSave>
    {
        public const string FriendsFileName = "friends.json";

        public List<FriendEntry> Friends { get; set; } = new();

        protected override SettingsScope Scope => SettingsScope.Server;

        protected override string FileName => FriendsFileName;

        protected override JsonTypeInfo<FriendsListSave> TypeInfo => FriendsListJsonContext.Default.FriendsListSave;

        /// <summary>Migrates any legacy bare-array file to the wrapped format, then loads the server's save.</summary>
        public static FriendsListSave LoadWithMigration()
        {
            MigrateLegacyFormatIfNeeded();
            return Load();
        }

        // Older versions stored a bare JSON array; rewrite it once into the wrapped format JsonSave expects.
        private static void MigrateLegacyFormatIfNeeded()
        {
            string path = Path.Combine(JsonSaveLocationHelper.GetScopeDirectory(SettingsScope.Server), FriendsFileName);

            if (!File.Exists(path))
                return;

            try
            {
                string json = File.ReadAllText(path);

                if (!json.TrimStart().StartsWith('['))
                    return;

                List<FriendEntry> legacy = JsonSerializer.Deserialize(json, FriendsListJsonContext.Default.ListFriendEntry) ?? new List<FriendEntry>();
                new FriendsListSave { Friends = legacy }.Save();
            }
            catch (Exception ex)
            {
                Log.Error($"Error migrating legacy friends list: {ex.Message}");
            }
        }
    }

    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(FriendsListSave))]
    [JsonSerializable(typeof(List<FriendEntry>))]
    [JsonSerializable(typeof(FriendEntry))]
    internal sealed partial class FriendsListJsonContext : JsonSerializerContext
    {
    }
}
