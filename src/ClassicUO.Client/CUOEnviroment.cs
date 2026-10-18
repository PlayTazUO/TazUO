// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Reflection;
using System.Threading;

namespace ClassicUO
{
    internal static class CUOEnviroment
    {
        public static Thread GameThread;
        public static float DPIScaleFactor = 1.0f;
        public static bool NoSound;
        public static string[] Args;
        public static string[] Plugins;
        public static bool Debug;
        public static bool IsHighDPI;
        public static uint CurrentRefreshRate;
        public static bool SkipLoginScreen;
        public static bool NoServerPing;
        public static Assembly Assembly => Assembly.GetEntryAssembly();

        public static readonly bool IsUnix = Environment.OSVersion.Platform != PlatformID.Win32NT && Environment.OSVersion.Platform != PlatformID.Win32Windows && Environment.OSVersion.Platform != PlatformID.Win32S && Environment.OSVersion.Platform != PlatformID.WinCE;

        public static readonly string Version = Assembly.GetExecutingAssembly()?.GetName()?.Version?.ToString() ?? "0.0.0.0";

        /// <summary>
        ///     The directory the client is installed in. Anchored to the executing app host rather than the
        ///     process working directory so data and log paths do not depend on how the client was launched
        ///     (a launcher, shortcut, or native host can change the cwd). Matches <see cref="AppContext.BaseDirectory"/>,
        ///     which the native library loader already relies on.
        /// </summary>
        public static readonly string ExecutablePath = AppContext.BaseDirectory;
    }
}