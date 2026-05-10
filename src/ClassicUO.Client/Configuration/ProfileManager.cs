// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.ComponentModel;
using System.IO;
using ClassicUO.Game.Managers;
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

        static ProfileManager()
        {
            // Subscribe to player creation event to load Char-scoped settings
            EventSink.OnPlayerCreated += OnPlayerCreated;
        }

        private static void OnPlayerCreated(object sender, System.EventArgs e) =>
            // Load Char-scoped settings after player is created (when serial is available)
            CurrentProfile?.LoadCharScopedSettings();

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

        public static void Load(string servername, string username, string charactername)
        {
            string path = FileSystemHelper.CreateFolderIfNotExists(RootPath, username.Trim(), servername.Trim(), charactername.Trim());
            string fileToLoad = Path.Combine(path, "profile.json");

            ProfilePath = path;
            CurrentProfile = ConfigurationResolver.Load<Profile>(fileToLoad, ProfileJsonContext.DefaultToUse.Profile) ?? NewFromDefault();

            CurrentProfile.Username = username;
            CurrentProfile.ServerName = servername;
            CurrentProfile.CharacterName = charactername;

            if (CurrentProfile.GridHighlightSetup.Count == 0)
            {
                GridHighLightProfile.MigrateGridHighlightToSetup(CurrentProfile);
                ConfigurationResolver.Save(CurrentProfile, Path.Combine(ProfilePath, "profile.json"), ProfileJsonContext.DefaultToUse.Profile);
            }

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

        public static void UnLoadProfile() => CurrentProfile = null;

        private static void OnCurrentProfilePropertyChanged(object sender, PropertyChangedEventArgs e) => CurrentProfilePropertyChanged?.Invoke(sender, e);
    }
}
