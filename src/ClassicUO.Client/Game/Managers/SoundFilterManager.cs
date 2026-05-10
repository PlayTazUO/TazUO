using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using ClassicUO.Configuration;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Managers
{
    public class SoundFilterManager
    {
        private HashSet<int> _filteredSounds;
        private HashSet<int> _filteredMusic;
        private bool _isLoaded;
        private bool _isMusicLoaded;

        public static SoundFilterManager Instance
        {
            get
            {
                field ??= new SoundFilterManager();
                return field;
            }
        }

        public HashSet<int> FilteredSounds
        {
            get
            {
                EnsureLoaded();
                return _filteredSounds;
            }
        }

        public HashSet<int> FilteredMusic
        {
            get
            {
                EnsureMusicLoaded();
                return _filteredMusic;
            }
        }

        private SoundFilterManager()
        {
            _filteredSounds = new HashSet<int>();
            _filteredMusic = new HashSet<int>();
            _isLoaded = false;
            _isMusicLoaded = false;
        }

        private void EnsureLoaded(bool isMusic = false)
        {
            if (isMusic)
                EnsureMusicLoaded();
            else
            {
                if (!_isLoaded)
                    Load();
            }
        }

        private void EnsureMusicLoaded()
        {
            if (!_isMusicLoaded)
                LoadMusic();
        }

        private void Load()
        {
            if (Client.Settings == null)
            {
                Log.Warn("SQLSettings not available for SoundFilterManager");
                _filteredSounds = new HashSet<int>();
                _isLoaded = true;
                return;
            }

            try
            {
                string json = Client.Settings.Get(SettingsScope.Account, Constants.SqlSettings.SOUND_FILTER_IDS, "[]");

                if (!string.IsNullOrWhiteSpace(json))
                    _filteredSounds = JsonSerializer.Deserialize(json, HashSetIntContext.Default.HashSetInt32)
                                      ?? new HashSet<int>();
                else
                    _filteredSounds = new HashSet<int>();
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to load sound filters: {ex.Message}");
                _filteredSounds = new HashSet<int>();
            }

            _isLoaded = true;
        }

        private void LoadMusic()
        {
            if (Client.Settings == null)
            {
                Log.Warn("SQLSettings not available for SoundFilterManager (music)");
                _filteredMusic = new HashSet<int>();
                _isMusicLoaded = true;
                return;
            }

            try
            {
                string json = Client.Settings.Get(SettingsScope.Account, Constants.SqlSettings.MUSIC_FILTER_IDS, "[]");

                if (!string.IsNullOrWhiteSpace(json))
                    _filteredMusic = JsonSerializer.Deserialize(json, HashSetIntContext.Default.HashSetInt32)
                                     ?? new HashSet<int>();
                else
                    _filteredMusic = new HashSet<int>();
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to load music filters: {ex.Message}");
                _filteredMusic = new HashSet<int>();
            }

            _isMusicLoaded = true;
        }

        public void Save(bool isMusic = false)
        {
            if (Client.Settings == null)
            {
                Log.Warn("SQLSettings not available for SoundFilterManager save");
                return;
            }

            try
            {
                if (isMusic)
                {
                    string json = JsonSerializer.Serialize(_filteredMusic, HashSetIntContext.Default.HashSetInt32);
                    _ = Client.Settings.SetAsync(SettingsScope.Account, Constants.SqlSettings.MUSIC_FILTER_IDS, json);
                }
                else
                {
                    string json = JsonSerializer.Serialize(_filteredSounds, HashSetIntContext.Default.HashSetInt32);
                    _ = Client.Settings.SetAsync(SettingsScope.Account, Constants.SqlSettings.SOUND_FILTER_IDS, json);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to save {(isMusic ? "music" : "sound")} filters: {ex.Message}");
            }
        }

        public void AddFilter(int soundId, bool isMusic = false)
        {
            EnsureLoaded(isMusic);
            if (isMusic)
            {
                if (_filteredMusic.Add(soundId))
                    Save(isMusic: true);
            }
            else
            {
                if (_filteredSounds.Add(soundId))
                    Save();
            }
        }

        public void RemoveFilter(int soundId, bool isMusic = false)
        {
            EnsureLoaded(isMusic);
            if (isMusic)
            {
                if (_filteredMusic.Remove(soundId))
                    Save(isMusic: true);
            }
            else
            {
                if (_filteredSounds.Remove(soundId))
                    Save();
            }
        }

        public bool IsSoundFiltered(int soundId, bool isMusic = false)
        {
            EnsureLoaded(isMusic);
            return isMusic ? _filteredMusic.Contains(soundId) : _filteredSounds.Contains(soundId);
        }

        public void Clear(bool isMusic = false)
        {
            EnsureLoaded(isMusic);
            if (isMusic)
            {
                _filteredMusic.Clear();
                Save(isMusic: true);
            }
            else
            {
                _filteredSounds.Clear();
                Save();
            }
        }

        public void Reset(bool isMusic = false)
        {
            if (isMusic)
            {
                _isMusicLoaded = false;
                _filteredMusic.Clear();
            }
            else
            {
                _isLoaded = false;
                _filteredSounds.Clear();
            }
        }
    }

    [JsonSerializable(typeof(HashSet<int>))]
    [JsonSourceGenerationOptions(
        WriteIndented = false,
        PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        IgnoreReadOnlyProperties = false,
        IncludeFields = false)]
    public partial class HashSetIntContext : JsonSerializerContext
    {
    }
}
