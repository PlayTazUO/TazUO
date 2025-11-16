// SPDX-License-Identifier: BSD-2-Clause

using System.IO;
using ClassicUO.Game.UI.Gumps.GridHighLight;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;

namespace ClassicUO.Configuration
{
    internal static class ProfileManager
    {
        public static Profile CurrentProfile { get; private set; }
        public static string ProfilePath { get; private set; }

        private static string _rootPath;
        private static string RootPath
        {
            get
            {
                if (string.IsNullOrEmpty(_rootPath))
                {
                    if (string.IsNullOrWhiteSpace(Settings.GlobalSettings.ProfilesPath))
                    {
                        _rootPath = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Profiles");
                    }
                    else
                    {
                        _rootPath = Settings.GlobalSettings.ProfilesPath;
                    }
                }

                return _rootPath;
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
            
            // 自动应用DPI缩放（如果尚未手动配置）
            ApplyAutoDPIScaling(CurrentProfile);

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

        /// <summary>
        /// 根据launcher_scale_factor或DPI自动应用全局缩放
        /// 只在用户未手动配置时自动设置
        /// </summary>
        private static void ApplyAutoDPIScaling(Profile profile)
        {
            if (profile == null)
                return;

            // 优先使用launcher_scale_factor（首次运行时）
            // 这主要是为了解决Windows高DPI下UI过小的问题
            // macOS不需要，因为SDL已经自动处理了HiDPI
            bool isMacOS = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX);
            
            if (CUOEnviroment.UseLauncherScaleForProfile && 
                Settings.GlobalSettings?.LauncherScaleFactor > 1.01f &&
                !isMacOS)
            {
                // 检查是否是默认值（用户未手动调整过）
                if (profile.GlobalScale == 1.5f || profile.GlobalScale == 1.0f || !profile.GlobalScaling)
                {
                    profile.GlobalScaling = true;
                    profile.GlobalScale = Settings.GlobalSettings.LauncherScaleFactor;
                    Utility.Logging.Log.Trace($"Applied launcher_scale_factor to GlobalScale: {profile.GlobalScale:F2}x (Windows high-DPI fix)");
                    return;
                }
            }

            // 备用方案：使用DPI缩放
            float dpiScale = CUOEnviroment.DPIScaleFactor;
            if (dpiScale > 1.0f && !profile.GlobalScaling)
            {
                if (profile.GlobalScale == 1.5f || profile.GlobalScale == 1.0f)
                {
                    profile.GlobalScaling = true;
                    profile.GlobalScale = dpiScale;
                    Utility.Logging.Log.Trace($"Auto-enabled global scaling: {dpiScale:F2}x for high-DPI display");
                }
            }
        }
    }
}
