using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;
using ClassicUO.Game.GameObjects;
using ClassicUO.Utility;
using IronPython.Runtime;

namespace ClassicUO.Game.Managers
{
    internal class FriendsListManager
    {
        private static FriendsListManager _instance;
        private List<FriendEntry> _friends;
        private string _savePath;
        private bool _loaded;

        public static FriendsListManager Instance => _instance ??= new FriendsListManager();

        private FriendsListManager()
        {
            _friends = new List<FriendEntry>();
            _loaded = false;
        }

        public void OnSceneLoad() => Load();

        public void OnSceneUnload() => Save();

        public void Load()
        {
            if (_loaded)
                return;

            string serverName = FileSystemHelper.RemoveInvalidChars(World.Instance.ServerName);
            string serverFolder = Path.Combine(CUOEnviroment.ExecutablePath, "Data", serverName);

            // Create directory if it doesn't exist
            if (!Directory.Exists(serverFolder))
                Directory.CreateDirectory(serverFolder);

            _savePath = Path.Combine(serverFolder, "friends.json");

            if (JsonHelper.Load(_savePath, FriendsListJsonContext.Default.ListFriendEntry, out List<FriendEntry> friends))
            {
                _friends = friends ?? new List<FriendEntry>();
            }
            else
            {
                _friends = new List<FriendEntry>();
            }

            _loaded = true;
        }

        public void Save()
        {
            if (!_loaded || _savePath == null)
                return;

            JsonHelper.SaveAndBackup(_friends, _savePath, FriendsListJsonContext.Default.ListFriendEntry);
        }

        public List<FriendEntry> GetFriends()
        {
            if (!_loaded)
                Load();

            return new List<FriendEntry>(_friends);
        }

        public bool AddFriend(uint serial, string name)
        {
            if (!_loaded)
                Load();

            if (string.IsNullOrWhiteSpace(name))
                return false;

            // Check if friend already exists
            if (_friends.Exists(f => f.Serial == serial))
                return false;

            var friend = new FriendEntry
            {
                Serial = serial,
                Name = name.Trim(),
                DateAdded = DateTime.UtcNow
            };

            _friends.Add(friend);
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

            FriendEntry friend = _friends.Find(f => f.Serial == serial);
            if (friend == null)
                return false;

            _friends.Remove(friend);
            Save();
            return true;
        }

        public bool RemoveFriend(string name)
        {
            if (!_loaded)
                Load();

            if (string.IsNullOrWhiteSpace(name))
                return false;

            FriendEntry friend = _friends.Find(f => string.Equals(f.Name, name.Trim(), StringComparison.OrdinalIgnoreCase));
            if (friend == null)
                return false;

            _friends.Remove(friend);
            Save();
            return true;
        }

        public bool IsFriend(uint serial)
        {
            if (!_loaded)
                Load();

            return _friends.Exists(f => f.Serial == serial);
        }

        public bool IsFriend(string name)
        {
            if (!_loaded)
                Load();

            if (string.IsNullOrWhiteSpace(name))
                return false;

            return _friends.Exists(f => string.Equals(f.Name, name.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        public FriendEntry GetFriend(uint serial)
        {
            if (!_loaded)
                Load();

            return _friends.Find(f => f.Serial == serial);
        }

        public FriendEntry GetFriend(string name)
        {
            if (!_loaded)
                Load();

            if (string.IsNullOrWhiteSpace(name))
                return null;

            return _friends.Find(f => string.Equals(f.Name, name.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        public PythonList GetAllFriends()
        {
            if (!_loaded)
                Load();

            PythonList friendList = new();

            foreach (FriendEntry friend in _friends)
                friendList.Add(friend.Serial);

            return friendList;
        }
    }

    public class FriendEntry
    {
        public uint Serial { get; set; }
        public string Name { get; set; }
        public DateTime DateAdded { get; set; }
    }

    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(List<FriendEntry>))]
    [JsonSerializable(typeof(FriendEntry))]
    internal sealed partial class FriendsListJsonContext : JsonSerializerContext
    {
    }
}
