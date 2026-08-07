// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.ComponentModel;
using System.IO;
using ClassicUO.Game.UI.Gumps.GridHighLight;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;

namespace ClassicUO.Configuration
{
    internal static class ProfileManager
    {
        /// <summary>
        /// Occurs when the current <see cref="Profile"/> has changed.
        /// Currently, this happens only during world creation/destruction, i.e., once per login.
        /// </summary>
        public static event EventHandler CurrentProfileChanged;

        /// <summary>
        /// Occurs when a property of the current <see cref="Profile"/> has changed.
        /// </summary>
        public static event PropertyChangedEventHandler CurrentProfilePropertyChanged;

        public static Profile CurrentProfile
        {
            get;
            private set
            {
                if (field == value)
                    return;

                // If we had a profile, unregister the event first
                if (field != null)
                    field.PropertyChanged -= OnCurrentProfilePropertyChanged;

                field = value;

                // Register the event on the new value
                if (field != null)
                    field.PropertyChanged += OnCurrentProfilePropertyChanged;

                // Notify that the profile itself has changed (as opposed to a profile 'setting'
                CurrentProfileChanged?.Invoke(null, EventArgs.Empty);
            }
        }

        public static string ProfilePath { get; private set; }

        /// <summary>Machine-wide settings loaded once at startup.</summary>
        public static GlobalSettingsSave GlobalSettings { get; private set; }

        /// <summary>Settings for the currently selected server. Loaded once the server is known.</summary>
        public static ServerSettingsSave ServerSettings { get; private set; }

        /// <summary>Settings for the currently logged-in account. Loaded once the server and account are known.</summary>
        public static AccountSettingsSave AccountSettings { get; private set; }

        public static void LoadGlobalSettings() => GlobalSettings = GlobalSettingsSave.Load();

        /// <summary>
        /// Loads the settings for the currently selected server. The server folder is derived from
        /// <see cref="World.ServerName"/>, so call this after the server has been selected.
        /// </summary>
        public static void LoadServerSettings() => ServerSettings = ServerSettingsSave.Load();

        /// <summary>
        /// Loads the settings for the currently logged-in account. The account folder is nested under the
        /// server folder, so call this once both the server and account are known.
        /// </summary>
        public static void LoadAccountSettings() => AccountSettings = AccountSettingsSave.Load();

        public static void SaveGlobalSettings()
        {
            GlobalSettings?.Save();
            GlobalSettings = null;
        }

        public static void SaveServerSettings()
        {
            ServerSettings?.Save();
            ServerSettings = null;
        }

        public static void SaveAccountSettings()
        {
            AccountSettings?.Save();
            AccountSettings = null;
        }

        public static string RootPath
        {
            get
            {
                if (string.IsNullOrEmpty(field))
                {
                    if (string.IsNullOrWhiteSpace(Settings.GlobalSettings.ProfilesPath))
                    {
                        field = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Profiles");
                    }
                    else
                    {
                        field = Settings.GlobalSettings.ProfilesPath;
                    }
                }

                return field;
            }
        }

        public static void Load(string servername, string username, string charactername, uint serial)
        {
            string path = FileSystemHelper.CreateFolderIfNotExists(RootPath, username.Trim(), servername.Trim(), charactername.Trim());
            string fileToLoad = Path.Combine(path, "profile.json");

            ProfilePath = path;

            // Load through JsonSave (which recovers from the rotating backups), falling back to the
            // default.json template when the character has no saved profile yet.
            CurrentProfile = File.Exists(fileToLoad) ? Profile.Load() : NewFromDefault();

            CurrentProfile.Username = username;
            CurrentProfile.ServerName = servername;
            CurrentProfile.CharacterName = charactername;
            CurrentProfile.Serial = serial;

            // Load (or migrate from the in-profile GridHighlightSetup / legacy per-list storage) the grid highlights.
            if (GridHighlightsConfig.LoadForProfile(ProfilePath, CurrentProfile))
            {
                CurrentProfile.Save();
            }

            // Load (or migrate from the legacy per-list profile storage) the cooldown-bar rules.
            if (CooldownBarsConfig.LoadForProfile(ProfilePath, CurrentProfile))
            {
                CurrentProfile.Save();
            }

            // Load the grid-container band layout rules for this profile.
            GridContainerBandsConfig.LoadForProfile(ProfilePath);

            // Load the tooltip overrides for this profile (migration from the legacy profile lists is
            // handled in Profile.HandleMigration).
            TooltipOverridesConfig.Load(ProfilePath);

            ValidateFields(CurrentProfile);

            CurrentProfile.AfterLoad();

            Client.Game?.SetVSync(CurrentProfile.EnableVSync);
        }

        public static void SetProfileAsDefault(Profile profile) => profile.SaveAs(RootPath, "default.json");

        public static Profile NewFromDefault() => ConfigurationResolver.Load<Profile>(Path.Combine(RootPath, "default.json"), ProfileJsonContext.DefaultToUse.Profile) ?? new Profile();

        private static void ValidateFields(Profile profile)
        {
            if (profile == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(profile.ServerName))
            {
                throw new InvalidDataException();
            }

            if (string.IsNullOrEmpty(profile.Username))
            {
                throw new InvalidDataException();
            }

            if (string.IsNullOrEmpty(profile.CharacterName))
            {
                throw new InvalidDataException();
            }

            if (profile.WindowClientBounds.X < 600)
            {
                profile.WindowClientBounds = new Point(600, profile.WindowClientBounds.Y);
            }

            if (profile.WindowClientBounds.Y < 480)
            {
                profile.WindowClientBounds = new Point(profile.WindowClientBounds.X, 480);
            }
        }

        public static void UnLoadProfile()
        {
            CurrentProfile = null;
            // Drop profile-scoped caches so edits can't be saved against the previous profile's path.
            GridContainerBandsConfig.Reset();
            // Leaving the world means leaving the server/account too, so persist their scoped settings.
            SaveServerSettings();
            SaveAccountSettings();
        }

        private static void OnCurrentProfilePropertyChanged(object sender, PropertyChangedEventArgs e) => CurrentProfilePropertyChanged?.Invoke(sender, e);
    }
}
