using System;
using System.IO;
using ClassicUO.Game;
using ClassicUO.Network;
using ClassicUO.Utility;

namespace ClassicUO.Configuration
{
    /// <summary>
    /// Resolves the on-disk directory used for a given <see cref="SettingsScope"/>.
    ///
    /// The scopes intentionally mirror the ones used by the SQL settings system
    /// (<see cref="SettingsScope"/>) so both storage backends refer to the same conceptual
    /// buckets, but here each scope maps to a folder rather than a database scope key:
    ///
    /// <list type="bullet">
    /// <item><description><see cref="SettingsScope.Global"/> - the shared <c>Data</c> folder. Files here can be
    /// touched by multiple client instances at once, so callers must guard access (see
    /// <c>JsonSave</c>, which takes a cross-process lock for this scope).</description></item>
    /// <item><description><see cref="SettingsScope.Server"/> - <c>Data/&lt;ServerName&gt;</c>. Matches the existing
    /// per-server convention already used for friends lists, map markers, etc.</description></item>
    /// <item><description><see cref="SettingsScope.Account"/> - <c>Data/&lt;ServerName&gt;/&lt;Account&gt;</c>, nested under
    /// the server folder.</description></item>
    /// <item><description><see cref="SettingsScope.Char"/> - the current profile's save folder
    /// (<see cref="ProfileManager.ProfilePath"/>, i.e. <c>Data/Profiles/&lt;Account&gt;/&lt;Server&gt;/&lt;Char&gt;</c>).</description></item>
    /// </list>
    ///
    /// This is factored out into a helper because these locations are needed in multiple places.
    /// </summary>
    public static class JsonSaveLocationHelper
    {
        /// <summary>The root <c>Data</c> folder (also the <see cref="SettingsScope.Global"/> location).</summary>
        public static string DataDirectory => Path.Combine(CUOEnviroment.ExecutablePath, "Data");

        /// <summary>
        /// Returns the directory that files for the given <paramref name="scope"/> should be saved in.
        /// The directory is not guaranteed to exist yet; callers should create it as needed.
        /// </summary>
        public static string GetScopeDirectory(SettingsScope scope)
        {
            switch (scope)
            {
                case SettingsScope.Global:
                    return DataDirectory;

                case SettingsScope.Server:
                    return Path.Combine(DataDirectory, GetServerFolderName());

                case SettingsScope.Account:
                    return Path.Combine(DataDirectory, GetServerFolderName(), GetAccountFolderName());

                case SettingsScope.Char:
                    return GetCharDirectory();

                default:
                    throw new ArgumentOutOfRangeException(nameof(scope), scope, null);
            }
        }

        /// <summary>
        /// The folder name used for the current server, sanitized for use as a path segment.
        /// Falls back to the world's server name, then to a literal <c>"Server"</c> when nothing is loaded.
        /// </summary>
        public static string GetServerFolderName()
        {
            string server = ProfileManager.CurrentProfile?.ServerName;

            if (string.IsNullOrEmpty(server))
                server = World.Instance?.ServerName;

            if (string.IsNullOrEmpty(server))
                server = "Server";

            return FileSystemHelper.RemoveInvalidChars(server);
        }

        /// <summary>
        /// The folder name used for the current account, sanitized for use as a path segment.
        /// Falls back to the account currently being logged in, then to a literal <c>"Account"</c>.
        /// </summary>
        public static string GetAccountFolderName()
        {
            string account = ProfileManager.CurrentProfile?.Username;

            if (string.IsNullOrEmpty(account))
                account = LoginHandshake.Account;

            if (string.IsNullOrEmpty(account))
                account = "Account";

            return FileSystemHelper.RemoveInvalidChars(account);
        }

        private static string GetCharDirectory()
        {
            // The profile save location is the natural Char scope; it already encodes account/server/char.
            if (!string.IsNullOrEmpty(ProfileManager.ProfilePath))
                return ProfileManager.ProfilePath;

            // No profile is loaded yet - fall back to the account folder so we never write to a bogus path.
            return GetScopeDirectory(SettingsScope.Account);
        }
    }
}
