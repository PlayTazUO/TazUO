// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Utility.Logging;
using System;
using System.IO;
using System.Text;

namespace ClassicUO
{
    /// <summary>
    /// Inspects an unhandled exception and, when the cause is recognized, returns a
    /// human-friendly explanation and suggested fix that is shown in the crash log.
    /// Keeping this out of <see cref="Bootstrap"/> lets the list of known crashes grow
    /// without cluttering Main.cs.
    /// </summary>
    internal static class CrashSuggestedFix
    {
        /// <summary>
        /// Returns a suggested fix for the given exception, or <c>null</c> when the crash
        /// is not one we have specific advice for.
        /// </summary>
        public static string Get(object e)
        {
            try
            {
                if (e is not Exception exception)
                {
                    return null;
                }

                if (exception is ArgumentOutOfRangeException argumentOutOfRangeException &&
                    argumentOutOfRangeException.StackTrace?.Contains("Microsoft.Xna.Framework.Graphics.GraphicsAdapter.get_DefaultAdapter()") == true)
                {
                    return "It appears TazUO was unable to find a suitable graphics adapter to use. " +
                           "This can sometimes occur if your operating system shuts down your graphics adapter to preserve power.";
                }

                if (exception is ArgumentOutOfRangeException fetchDisplayAdapterException &&
                    fetchDisplayAdapterException.StackTrace?.Contains("SDL3_FNAPlatform.FetchDisplayAdapter") == true)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("TazUO crashed while trying to identify the display it is running on.");
                    sb.AppendLine("This usually happens when the connected monitors change while the game is running - for example a monitor is unplugged, turned off, put to sleep, or switched to a different input.");
                    sb.AppendLine("It can also occur when using a docking station, a KVM switch, or a laptop lid that was closed/opened.");
                    sb.AppendLine();
                    sb.AppendLine("This is a low-level issue in how the operating system reports displays and is not something TazUO can prevent.");
                    sb.AppendLine();
                    sb.AppendLine("Suggested fixes:");
                    sb.AppendLine("1. Make sure your monitor(s) stay powered on and connected while TazUO is running.");
                    sb.AppendLine("2. Avoid unplugging monitors, closing your laptop lid, or switching monitor inputs while the game is open.");
                    sb.AppendLine("3. If you use a docking station or KVM switch, try connecting your monitor directly to test.");
                    sb.AppendLine("4. Update your graphics card drivers to the latest version.");
                    sb.AppendLine("5. Simply restart TazUO - it should start normally once your displays are stable.");
                    return sb.ToString();
                }

                if (Client.IsShaderCompileFailure(exception))
                {
                    return Client.GraphicsShaderHelpMessage;
                }

                if (exception is InvalidOperationException noFna3DDriverException &&
                    noFna3DDriverException.Message.Contains("No supported FNA3D driver found!"))
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("TazUO could not start because FNA3D was unable to find a supported graphics driver.");
                    sb.AppendLine("This means none of the available rendering backends (Direct3D 11, Vulkan or OpenGL) could be initialized on your system.");
                    sb.AppendLine("It usually means your graphics drivers are missing or out of date, or you are running in an environment without proper GPU access (for example a remote desktop, virtual machine, or a system that fell back to a software renderer).");
                    sb.AppendLine();
                    sb.AppendLine("Suggested fixes:");
                    sb.AppendLine("1. Update your graphics card drivers to the latest version, then restart your computer.");
                    sb.AppendLine("2. Make sure you are running TazUO on the machine's real display and not through a remote desktop session that blocks GPU access.");
                    sb.AppendLine("3. Try forcing a specific graphics driver by adding one of the following command-line arguments:");
                    sb.AppendLine("     -force_driver 1   (OpenGL)");
                    sb.AppendLine("     -force_driver 2   (Vulkan)");
                    sb.AppendLine("     -force_driver 3   (SDL/FNA auto-select)");
                    sb.AppendLine("   Try each one in turn until the client starts successfully.");
                    sb.AppendLine("4. If you are running inside a virtual machine, enable 3D/GPU acceleration for it or run TazUO on physical hardware.");
                    return sb.ToString();
                }

                if (exception is Microsoft.Xna.Framework.Graphics.NoSuitableGraphicsDeviceException noSuitableGraphicsDeviceException)
                {
                    if (noSuitableGraphicsDeviceException.Message.Contains("OpenGL 2.1 support is required!"))
                    {
                        var sb = new StringBuilder();
                        sb.AppendLine("TazUO was unable to find a graphics device with the required OpenGL 2.1 support.");
                        sb.AppendLine("This usually means your graphics drivers are missing, out of date, or the client fell back to a software renderer (GDI Generic).");
                        sb.AppendLine();
                        sb.AppendLine("Suggested fixes:");
                        sb.AppendLine("1. Update your graphics card drivers to the latest version.");
                        sb.AppendLine("2. Try launching TazUO with a different graphics driver by adding one of the following command-line arguments:");
                        sb.AppendLine("     -force_driver 1   (OpenGL)");
                        sb.AppendLine("     -force_driver 2   (Vulkan)");
                        sb.AppendLine("     -force_driver 3   (SDL/FNA auto-select)");
                        sb.AppendLine("   Try each one in turn until the client starts successfully.");
                        return sb.ToString();
                    }

                    if (noSuitableGraphicsDeviceException.Message.Contains("Could not create swapchain!"))
                    {
                        string dataPath = Path.Join(CUOEnviroment.ExecutablePath, "Data");
                        string scriptsPath = Path.Join(CUOEnviroment.ExecutablePath, "LegionScripts");
                        var sb = new StringBuilder();
                        sb.AppendLine("Issue analysis indicates a potential conflict with your TazUO installation.");
                        sb.AppendLine("The client does not support side-by-side installation of both legacy and modern builds.");
                        sb.AppendLine($"Please backup your data ('{dataPath}') and script ('{scriptsPath}') folders and delete everything else.");
                        sb.AppendLine("Re-download *only* your selected channel (Legacy or Modern) from the launcher.");
                        sb.AppendLine("Copy your backed up Data and LegionScripts folders back to where they were.");
                        return sb.ToString();
                    }
                }

                if (TryGetFontRenderingCrashFix(exception, out string fontFix))
                {
                    return fontFix;
                }

                if (TryGetPluginCrashFix(exception, out string pluginFix))
                {
                    return pluginFix;
                }

                if (TryGetPluginPacketCrashFix(exception, out string pluginPacketFix))
                {
                    return pluginPacketFix;
                }

                if (TryGetMapLoaderCrashFix(exception, out string mapFix))
                {
                    return mapFix;
                }

                if (TryGetZlibVersionMismatchCrashFix(exception, out string zlibFix))
                {
                    return zlibFix;
                }

                if (TryGetBadUopFileCrashFix(exception, out string uopFix))
                {
                    return uopFix;
                }

                if (TryGetMissingAssemblyCrashFix(exception, out string assemblyFix))
                {
                    return assemblyFix;
                }
            }
            catch
            {
                Log.Error("Failed to obtain a suggested fix for error");
            }

            return null;
        }

        private static bool TryGetFontRenderingCrashFix(Exception e, out string fix)
        {
            fix = null;

            string details = e.ToString();

            if (string.IsNullOrEmpty(details))
                return false;

            bool crashedInFontCache =
                details.Contains("FontStashSharp.Rasterizers.StbTrueTypeSharp.Int32Map") ||
                details.Contains("GetGlyphKernAdvance") ||
                (details.Contains("FontStashSharp") && details.Contains("GetKerning"));

            if (!crashedInFontCache)
                return false;

            var sb = new StringBuilder();
            sb.AppendLine("TazUO crashed inside the font rendering code (FontStashSharp) while measuring or drawing text.");
            sb.AppendLine("The font glyph/kerning caches are not thread-safe, so this happens when text is created from a background thread at the same time the game is drawing.");
            sb.AppendLine("This is almost always triggered by a Legion script that shows messages or builds UI directly from its own thread.");
            sb.AppendLine();
            sb.AppendLine("Suggested fixes:");
            sb.AppendLine("1. If you were running a script when this happened, note which one and update it to the latest version.");
            sb.AppendLine("2. In custom scripts, avoid printing messages or creating gumps/controls directly - use the provided API (for example API.SysMsg) which safely marshals the work onto the main thread.");
            sb.AppendLine("3. Try reproducing without any scripts running to confirm a script is the cause.");
            sb.AppendLine("4. If it still crashes with no scripts, please report it with this crash log so it can be investigated.");
            fix = sb.ToString();
            return true;
        }

        private static bool TryGetMapLoaderCrashFix(Exception e, out string fix)
        {
            fix = null;

            // ToString() on the top-level exception includes the stack traces of any inner
            // (and aggregated) exceptions, so we can inspect the whole chain in one string.
            string details = e.ToString();

            if (string.IsNullOrEmpty(details))
                return false;

            // A crash inside MapLoader.LoadMap almost always means one of the map data files
            // (mapN.mul/.uop, staidxN.mul or staticsN.mul) is missing, truncated, or from a
            // mismatched client version - the loader ends up dereferencing a reader that was
            // never opened for that map.
            if (!details.Contains("ClassicUO.Assets.MapLoader.LoadMap"))
                return false;

            var sb = new StringBuilder();
            sb.AppendLine("TazUO crashed while loading the map data files.");
            sb.AppendLine("This usually means one of the map files is missing, incomplete, or does not match your client version.");
            sb.AppendLine("The affected files are named like map0.mul (or map0.uop), staidx0.mul and statics0.mul, with the number matching the facet you were entering.");
            sb.AppendLine();
            sb.AppendLine("Suggested fixes:");
            sb.AppendLine("1. Make sure TazUO is pointed at a complete Ultima Online data directory that contains all of the map, staidx and statics files.");
            sb.AppendLine("2. Verify/repair your UO installation (for example through the official installer or your shard's patcher) to restore any missing or corrupt files.");
            sb.AppendLine("3. If you copied the data files manually, re-copy them and confirm none were skipped or truncated.");
            sb.AppendLine("4. Confirm the client version configured in TazUO matches the version of your UO data files.");
            fix = sb.ToString();
            return true;
        }

        private static bool TryGetZlibVersionMismatchCrashFix(Exception e, out string fix)
        {
            fix = null;

            // A MissingMethodException naming a ZLib entry point means the loaded
            // ClassicUO.Utility.dll is older than the client executable expects - the two
            // were not updated together (a partial update or files copied over an old
            // install). This most commonly surfaces from the -zlib launch argument, which
            // calls into the newer ZLib API.
            if (e is not MissingMethodException)
                return false;

            string details = e.ToString();

            if (string.IsNullOrEmpty(details) ||
                !details.Contains("ClassicUO.Utility.ZLib"))
                return false;

            var sb = new StringBuilder();
            sb.AppendLine("TazUO could not start because its files do not all match.");
            sb.AppendLine("The bundled zlib helper library (ClassicUO.Utility.dll) is older than this build of TazUO expects, which usually happens after a partial update or when new files were copied over an old installation.");
            sb.AppendLine("This crash is triggered by the '-zlib' launch argument, which uses a newer zlib feature that the out-of-date file does not have.");
            sb.AppendLine();
            sb.AppendLine("Suggested fixes:");
            sb.AppendLine("1. Remove the '-zlib' launch argument. Instead, enable managed zlib from the Options menu on the login screen (Misc tab) - it does the same thing and is saved for you.");
            sb.AppendLine("2. Reinstall or re-download TazUO so every file is updated together, then try again.");
            sb.AppendLine("3. If you copied files manually, make sure ClassicUO.Utility.dll was replaced along with the rest of the client.");
            fix = sb.ToString();
            return true;
        }

        private static bool TryGetBadUopFileCrashFix(Exception e, out string fix)
        {
            fix = null;

            // ToString() on the top-level exception includes the stack traces of any inner
            // (and aggregated) exceptions, so we can inspect the whole chain in one string.
            string details = e.ToString();

            if (string.IsNullOrEmpty(details))
                return false;

            // "Bad uop file" is thrown by UOFileUop.FillEntries when a .uop file does not begin
            // with the expected magic number. That means the file is truncated, corrupt, still
            // being downloaded/patched, or not actually a UOP file (for example a 0-byte
            // placeholder or a mismatched client version).
            if (!(e is ArgumentException) ||
                !details.Contains("Bad uop file") ||
                !details.Contains("UOFileUop.FillEntries"))
                return false;

            var sb = new StringBuilder();
            sb.AppendLine("TazUO crashed while loading one of the UO data files (.uop).");
            sb.AppendLine("The file did not start with the expected header, which means it is corrupt, truncated, still being downloaded/patched, or is not a real UOP file (for example an empty placeholder).");
            sb.AppendLine("The name just above 'UOFileUop.FillEntries' in the error (for example TileArtLoader, ArtLoader, etc.) tells you which set of files is affected.");
            sb.AppendLine();
            sb.AppendLine("Suggested fixes:");
            sb.AppendLine("1. Make sure TazUO is pointed at a complete Ultima Online data directory and that no files are 0 bytes.");
            sb.AppendLine("2. Verify/repair your UO installation (through the official installer or your shard's patcher) to restore any corrupt or truncated files.");
            sb.AppendLine("3. If a patcher or download was still running, let it finish completely and then start TazUO again.");
            sb.AppendLine("4. If you copied the data files manually, re-copy them and confirm none were skipped or truncated.");
            sb.AppendLine("5. Confirm the client version configured in TazUO matches the version of your UO data files.");
            fix = sb.ToString();
            return true;
        }

        private static bool TryGetMissingAssemblyCrashFix(Exception e, out string fix)
        {
            fix = null;

            // A FileNotFoundException for one of TazUO's own assemblies (for example
            // ClassicUO.Assets.dll) means a required file is missing from the installation.
            // The most common cause is a failed/partial install or files that were not all
            // copied together, so one dll is absent while the executable still expects it.
            if (e is not FileNotFoundException fileNotFound)
                return false;

            string details = e.ToString();

            if (string.IsNullOrEmpty(details))
                return false;

            // Match on the assembly name from either the FileName property or the message
            // text (the message is localized, but the assembly name is not translated).
            string fileName = fileNotFound.FileName ?? string.Empty;

            bool missingTazuoAssembly =
                fileName.StartsWith("ClassicUO.", StringComparison.OrdinalIgnoreCase) ||
                details.Contains("Could not load file or assembly 'ClassicUO.");

            if (!missingTazuoAssembly)
                return false;

            var sb = new StringBuilder();
            sb.AppendLine("TazUO could not start because one of its own program files is missing.");
            sb.AppendLine("A required assembly (for example ClassicUO.Assets.dll) could not be found next to the executable.");
            sb.AppendLine("This almost always means the install did not finish, or the client files were not all copied together, so one or more files are missing.");
            sb.AppendLine();
            sb.AppendLine("Suggested fixes:");
            sb.AppendLine("1. Reinstall or re-download TazUO so every file is placed together, then try again.");
            sb.AppendLine("2. If you copied TazUO manually, re-copy the entire folder and make sure no files were skipped.");
            sb.AppendLine("3. Check that your antivirus did not quarantine or delete any of TazUO's files.");
            sb.AppendLine("4. Do not mix files from different TazUO versions - use a clean install of a single build.");
            fix = sb.ToString();
            return true;
        }

        private static bool TryGetPluginCrashFix(Exception e, out string fix)
        {
            fix = null;

            // ToString() on the top-level exception includes the stack traces of any inner
            // (and aggregated) exceptions, so we can inspect the whole chain in one string.
            string details = e.ToString();

            if (string.IsNullOrEmpty(details))
                return false;

            // These frames only appear when the crash happened while TazUO was loading a
            // third-party plugin (assistant/copilot, e.g. Razor or a UO "copilot"). Such
            // plugins run their own initialization code - and often bundle their own copy of
            // the UO file loaders - so a crash originating here comes from the plugin, not
            // from TazUO itself.
            bool crashedInPluginLoad =
                details.Contains("ClassicUO.Network.Plugin.Load") ||
                details.Contains("ClassicUO.Network.Plugin.Create") ||
                details.Contains("GameController.LoadPlugins");

            if (!crashedInPluginLoad)
                return false;

            var sb = new StringBuilder();
            sb.AppendLine("A plugin failed to start and crashed TazUO.");
            sb.AppendLine("The error above was thrown by a third-party plugin's own code while it was being loaded, not by TazUO itself.");
            sb.AppendLine();
            sb.AppendLine("Suggested fixes:");
            sb.AppendLine("1. Update the plugin to its latest version, or temporarily remove/disable it to confirm it is the cause.");
            sb.AppendLine("2. Make sure the plugin is pointed at the same valid UO data directory that TazUO uses.");
            sb.AppendLine("   Many assistants load the UO art/map files themselves and will crash if that path is wrong or the files are missing.");
            sb.AppendLine("3. Verify the plugin supports your client version and this build of TazUO.");
            sb.AppendLine("4. If it keeps crashing, send this crash log to the plugin's author.");

            fix = sb.ToString();
            return true;
        }

        private static bool TryGetPluginPacketCrashFix(Exception e, out string fix)
        {
            fix = null;

            // ToString() on the top-level exception includes the stack traces of any inner
            // (and aggregated) exceptions, so we can inspect the whole chain in one string.
            string details = e.ToString();

            if (string.IsNullOrEmpty(details))
                return false;

            // A crash inside OnPluginRecv/OnPluginSend means a third-party plugin called the
            // client's packet-injection API (for example an assistant's SendToClient) at
            // runtime and handed it a buffer that was too small - typically because the packet
            // grew larger than the buffer the plugin allocated for it. The destination-too-short
            // ArgumentException is the classic symptom of this.
            bool crashedInPluginPacketApi =
                details.Contains("ClassicUO.Network.Plugin.OnPluginRecv") ||
                details.Contains("ClassicUO.Network.Plugin.OnPluginSend");

            if (!crashedInPluginPacketApi)
                return false;

            var sb = new StringBuilder();
            sb.AppendLine("A plugin crashed TazUO while injecting a network packet into the client.");
            sb.AppendLine("The error above was thrown when a third-party plugin (for example an assistant such as Razor) used the client's packet-injection API and handed over a packet that did not fit its allocated buffer.");
            sb.AppendLine();
            sb.AppendLine("Suggested fixes:");
            sb.AppendLine("1. Update the plugin (and any of its scripting add-ons) to the latest version - this crash is usually a plugin bug, not a TazUO bug.");
            sb.AppendLine("2. Temporarily disable the plugin to confirm it is the cause.");
            sb.AppendLine("3. Make sure the plugin is pointed at the same UO data directory TazUO uses and that its files are not corrupt or out of date.");
            sb.AppendLine("4. If it keeps happening, report the crash log to the plugin's author, noting which script or feature was running when it occurred.");

            fix = sb.ToString();
            return true;
        }
    }
}
