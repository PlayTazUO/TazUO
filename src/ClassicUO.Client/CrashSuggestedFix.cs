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

                if (TryGetPluginCrashFix(exception, out string pluginFix))
                {
                    return pluginFix;
                }
            }
            catch
            {
                Log.Error("Failed to obtain a suggested fix for error");
            }

            return null;
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
    }
}
