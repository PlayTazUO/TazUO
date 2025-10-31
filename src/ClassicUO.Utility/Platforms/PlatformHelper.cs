// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Utility.Platforms
{
    public static class PlatformHelper
    {
        public static readonly bool IsMonoRuntime = Type.GetType("Mono.Runtime") != null;

        public static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        public static readonly bool IsLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        public static readonly bool IsOSX = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        public static void LaunchBrowser(string url, bool skipValidation = false, bool retry = false)
        {
            try
            {
                if (!skipValidation)
                {
                    if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri) || (uri.Scheme != "http" && uri.Scheme != "https" && uri.Scheme != "file"))
                    {
                        Log.Error($"Invalid URL format: {url}, trying with https://..");

                        if (!retry)
                        {
                            LaunchBrowser("https://" + url, skipValidation: false, retry: true);
                        }

                        return;
                    }
                }

                if (IsWindows)
                {
                    // Use shell execute - secure and prevents command injection
                    var psi = new ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    };
                    using var process = Process.Start(psi);
                    Log.Trace($"Launched browser (Windows) for: {url}");
                }
                else if (IsOSX)
                {
                    using var process = Process.Start("open", url);
                    Log.Trace($"Launched browser (macOS) for: {url}");
                }
                else
                {
                    using var process = Process.Start("xdg-open", url);
                    Log.Trace($"Launched browser (Linux) for: {url}");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to launch browser for '{url}': {ex}");
            }
        }
    }
}