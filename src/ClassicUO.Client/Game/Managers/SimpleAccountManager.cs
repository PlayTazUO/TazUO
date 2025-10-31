using System.Collections.Generic;
using System.IO;

namespace ClassicUO.Game.Managers
{
    internal static class SimpleAccountManager
    {
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
    }
}
