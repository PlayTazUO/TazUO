using System;
using System.Collections.Generic;
using System.IO;
using ClassicUO.Configuration;
using ClassicUO.Utility;

namespace ClassicUO.Game.Managers
{
    internal static class SimpleAccountManager
    {
        private static readonly string appDataRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TazUO"
        );

        private static string accountPath = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Profiles");

        public static string[] GetAccounts()
        {
            var accounts = new List<string>();

            if (Directory.Exists(accountPath))
            {
                string[] dirs = Directory.GetDirectories(accountPath);

                foreach (string dir in dirs)
                {
                    accounts.Add(Path.GetFileName(dir));
                }
            }
            return accounts.ToArray();
        }

        /// <summary>
        /// Returns the stored (encrypted) password for an account, or null when
        /// none has been saved. Optional convenience; the client still works with
        /// a single global password when no per-account password exists.
        /// Passwords are stored outside the game folder under
        /// %APPDATA%/TazUO/&lt;server&gt;/&lt;account&gt;/account.password.
        /// </summary>
        public static string GetAccountPassword(string account)
        {
            if (string.IsNullOrEmpty(account))
                return null;

            foreach (string file in FindAccountPasswordFiles(account))
            {
                try
                {
                    return File.ReadAllText(file);
                }
                catch
                {
                    // Try the next server folder if this one is unreadable.
                }
            }
            return null;
        }

        /// <summary>
        /// Persists the (encrypted) password for an account outside the game folder,
        /// keyed by the last server used: %APPDATA%/TazUO/&lt;server&gt;/&lt;account&gt;/account.password.
        /// Passing null or empty removes any stored password for that account.
        /// </summary>
        public static void SetAccountPassword(string account, string encryptedPassword)
        {
            if (string.IsNullOrEmpty(account))
                return;

            string server = string.IsNullOrEmpty(Settings.GlobalSettings.LastServerName)
                ? "Unknown"
                : Settings.GlobalSettings.LastServerName;

            string file = Path.Combine(appDataRoot, server, account, "account.password");
            string dir = Path.GetDirectoryName(file);

            if (string.IsNullOrWhiteSpace(encryptedPassword))
            {
                if (File.Exists(file))
                {
                    try { File.Delete(file); } catch { }
                }
                return;
            }

            try
            {
                Directory.CreateDirectory(dir);
                File.WriteAllText(file, encryptedPassword);
            }
            catch
            {
                // Best effort: a writable profile folder is normally guaranteed.
            }
        }

        private static IEnumerable<string> FindAccountPasswordFiles(string account)
        {
            if (!Directory.Exists(appDataRoot))
                yield break;

            foreach (string serverDir in Directory.GetDirectories(appDataRoot))
            {
                string file = Path.Combine(serverDir, account, "account.password");
                if (File.Exists(file))
                    yield return file;
            }
        }
    }
}
