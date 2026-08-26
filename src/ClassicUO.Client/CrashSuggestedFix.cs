// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.IO;
using System.Text;
using ClassicUO.Utility.Logging;
using ClassicUO.Utility.Platforms;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO;

/// <summary>
///     Inspects an unhandled exception and, when the cause is recognized, returns a
///     human-friendly explanation and suggested fix that is shown in the crash log.
///     Keeping this out of <see cref="Bootstrap" /> lets the list of known crashes grow
///     without cluttering Main.cs.
/// </summary>
internal static class CrashSuggestedFix
{
    /// <summary>
    ///     Win32 4551 - "An application control policy has blocked this file". Raised by Windows
    ///     S mode (which permits Store apps only, so no reinstall can help) and by Smart App
    ///     Control refusing an unsigned file. Both are all-or-nothing and accept no per-file
    ///     exception. The message text is localized, so the HRESULT is the only reliable match.
    /// </summary>
    private const int H_RESULT_BLOCKED_BY_APPLICATION_POLICY = unchecked((int)0x800711C7);

    /// <summary>
    ///     COR_E_NOTSUPPORTED as raised by the assembly loader when a file still carries the
    ///     "downloaded from the internet" zone mark, or lives on a network share.
    /// </summary>
    private const int H_RESULT_UNTRUSTED_ZONE = unchecked((int)0x80131515);

    /// <summary>
    ///     Returns a suggested fix for the given exception, or <c>null</c> when the crash
    ///     is not one we have specific advice for.
    /// </summary>
    public static string Get(object e)
    {
        try
        {
            if (e is not Exception exception)
                return null;

            if (TryGetMissingGraphicsAdapterCrashFix(exception, out string adapterFix))
                return adapterFix;

            if (TryGetDisplayAdapterCrashFix(exception, out string displayFix))
                return displayFix;

            if (Client.IsShaderCompileFailure(exception))
                return Client.GraphicsShaderHelpMessage;

            if (TryGetNoFna3DDriverCrashFix(exception, out string driverFix))
                return driverFix;

            if (TryGetNoSuitableGraphicsDeviceCrashFix(exception, out string graphicsDeviceFix))
                return graphicsDeviceFix;

            if (TryGetFontRenderingCrashFix(exception, out string fontFix))
                return fontFix;

            if (TryGetPluginCrashFix(exception, out string pluginFix))
                return pluginFix;

            if (TryGetPluginPacketCrashFix(exception, out string pluginPacketFix))
                return pluginPacketFix;

            if (TryGetMapLoaderCrashFix(exception, out string mapFix))
                return mapFix;

            if (TryGetZlibVersionMismatchCrashFix(exception, out string zlibFix))
                return zlibFix;

            if (TryGetBadUopFileCrashFix(exception, out string uopFix))
                return uopFix;

            if (TryGetMissingAssemblyCrashFix(exception, out string assemblyFix))
                return assemblyFix;

            if (TryGetPolicyBlockedFileCrashFix(exception, out string policyBlockedFix))
                return policyBlockedFix;

            if (TryGetFileAccessDeniedCrashFix(exception, out string fileAccessFix))
                return fileAccessFix;
        }
        catch
        {
            Log.Error("Failed to obtain a suggested fix for error");
        }

        return null;
    }

    /// <summary>
    ///     Recognizes an <see cref="ArgumentOutOfRangeException" /> thrown from
    ///     <c>GraphicsAdapter.get_DefaultAdapter()</c> - typically the OS powered down the
    ///     graphics adapter to save power and no default adapter remains.
    /// </summary>
    /// <param name="e">Exception under inspection.</param>
    /// <param name="fix">Set to the suggested fix text when recognized.</param>
    /// <returns>True if the crash was recognized.</returns>
    private static bool TryGetMissingGraphicsAdapterCrashFix(Exception e, out string fix)
    {
        fix = null;

        if (e is not ArgumentOutOfRangeException argumentOutOfRangeException ||
            argumentOutOfRangeException.StackTrace?.Contains("Microsoft.Xna.Framework.Graphics.GraphicsAdapter.get_DefaultAdapter()") != true)
            return false;

        fix = "It appears TazUO was unable to find a suitable graphics adapter to use. " +
              "This can sometimes occur if your operating system shuts down your graphics adapter to preserve power.";
        return true;
    }

    /// <summary>
    ///     Recognizes an <see cref="ArgumentOutOfRangeException" /> thrown from
    ///     <c>SDL3_FNAPlatform.FetchDisplayAdapter</c> - the connected displays changed while
    ///     the client was running (unplugged, slept, docked/undocked).
    /// </summary>
    /// <param name="e">Exception under inspection.</param>
    /// <param name="fix">Set to the suggested fix text when recognized.</param>
    /// <returns>True if the crash was recognized.</returns>
    private static bool TryGetDisplayAdapterCrashFix(Exception e, out string fix)
    {
        fix = null;

        if (e is not ArgumentOutOfRangeException fetchDisplayAdapterException ||
            fetchDisplayAdapterException.StackTrace?.Contains("SDL3_FNAPlatform.FetchDisplayAdapter") != true)
            return false;

        var sb = new StringBuilder();
        sb.AppendLine("TazUO crashed while trying to identify the display it is running on.");
        sb.AppendLine(
            "This usually happens when the connected monitors change while the game is running - for example a monitor is unplugged, turned off, put to sleep, or switched to a different input.");
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
        fix = sb.ToString();
        return true;
    }

    /// <summary>
    ///     Recognizes FNA3D failing to initialize any rendering backend (Direct3D 11, Vulkan or
    ///     OpenGL) - usually missing/outdated graphics drivers or no real GPU access (remote
    ///     desktop, VM, software renderer fallback).
    /// </summary>
    /// <param name="e">Exception under inspection.</param>
    /// <param name="fix">Set to the suggested fix text when recognized.</param>
    /// <returns>True if the crash was recognized.</returns>
    private static bool TryGetNoFna3DDriverCrashFix(Exception e, out string fix)
    {
        fix = null;

        if (e is not InvalidOperationException noFna3DDriverException ||
            !noFna3DDriverException.Message.Contains("No supported FNA3D driver found!"))
            return false;

        var sb = new StringBuilder();
        sb.AppendLine("TazUO could not start because FNA3D was unable to find a supported graphics driver.");
        sb.AppendLine("This means none of the available rendering backends (Direct3D 11, Vulkan or OpenGL) could be initialized on your system.");
        sb.AppendLine(
            "It usually means your graphics drivers are missing or out of date, or you are running in an environment without proper GPU access (for example a remote desktop, virtual machine, or a system that fell back to a software renderer).");
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
        fix = sb.ToString();
        return true;
    }

    /// <summary>
    ///     Recognizes <see cref="Microsoft.Xna.Framework.Graphics.NoSuitableGraphicsDeviceException" />
    ///     and picks between its two known causes: missing OpenGL 2.1 support, or a swapchain
    ///     failure that indicates a legacy and modern TazUO build installed side by side.
    /// </summary>
    /// <param name="e">Exception under inspection.</param>
    /// <param name="fix">Set to the suggested fix text when recognized.</param>
    /// <returns>True if the crash was recognized.</returns>
    private static bool TryGetNoSuitableGraphicsDeviceCrashFix(Exception e, out string fix)
    {
        fix = null;

        if (e is not NoSuitableGraphicsDeviceException noSuitableGraphicsDeviceException)
            return false;

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
            fix = sb.ToString();
            return true;
        }

        if (noSuitableGraphicsDeviceException.Message.Contains("Could not create swapchain!"))
        {
            var sb = new StringBuilder();
            sb.AppendLine("Issue analysis indicates a potential conflict with your TazUO installation.");
            sb.AppendLine("The client does not support side-by-side installation of both legacy and modern builds.");
            sb.AppendLine();
            AppendCleanReinstallSteps(sb, redownloadInstruction: "Re-download *only* your selected channel (Legacy or Modern) from the launcher.");
            fix = sb.ToString();
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Recognizes a crash inside FontStashSharp's glyph/kerning cache. That cache is not
    ///     thread-safe, so this fires when text is built from a background thread - almost
    ///     always a Legion script touching the UI off the main thread.
    /// </summary>
    /// <param name="e">Exception under inspection.</param>
    /// <param name="fix">Set to the suggested fix text when recognized.</param>
    /// <returns>True if the crash was recognized.</returns>
    private static bool TryGetFontRenderingCrashFix(Exception e, out string fix)
    {
        fix = null;

        string details = e.ToString();

        if (string.IsNullOrEmpty(details))
            return false;

        bool crashedInFontCache = details.Contains("FontStashSharp.Rasterizers.StbTrueTypeSharp.Int32Map") ||
                                  details.Contains("GetGlyphKernAdvance") ||
                                  (details.Contains("FontStashSharp") && details.Contains("GetKerning"));

        if (!crashedInFontCache)
            return false;

        var sb = new StringBuilder();
        sb.AppendLine("TazUO crashed inside the font rendering code (FontStashSharp) while measuring or drawing text.");
        sb.AppendLine(
            "The font glyph/kerning caches are not thread-safe, so this happens when text is created from a background thread at the same time the game is drawing.");
        sb.AppendLine("This is almost always triggered by a Legion script that shows messages or builds UI directly from its own thread.");
        sb.AppendLine();
        sb.AppendLine("Suggested fixes:");
        sb.AppendLine("1. If you were running a script when this happened, note which one and update it to the latest version.");
        sb.AppendLine(
            "2. In custom scripts, avoid printing messages or creating gumps/controls directly - use the provided API (for example API.SysMsg) which safely marshals the work onto the main thread.");
        sb.AppendLine("3. Try reproducing without any scripts running to confirm a script is the cause.");
        sb.AppendLine("4. If it still crashes with no scripts, please report it with this crash log so it can be investigated.");
        fix = sb.ToString();
        return true;
    }

    /// <summary>
    ///     Recognizes a crash inside <c>MapLoader.LoadMap</c> - a missing, truncated, or
    ///     version-mismatched map data file (map/staidx/statics for the entered facet).
    /// </summary>
    /// <param name="e">Exception under inspection.</param>
    /// <param name="fix">Set to the suggested fix text when recognized.</param>
    /// <returns>True if the crash was recognized.</returns>
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
        sb.AppendLine(
            "The affected files are named like map0.mul (or map0.uop), staidx0.mul and statics0.mul, with the number matching the facet you were entering.");
        sb.AppendLine();
        sb.AppendLine("Suggested fixes:");
        sb.AppendLine("1. Make sure TazUO is pointed at a complete Ultima Online data directory that contains all of the map, staidx and statics files.");
        sb.AppendLine(
            "2. Verify/repair your UO installation (for example through the official installer or your shard's patcher) to restore any missing or corrupt files.");
        sb.AppendLine("3. If you copied the data files manually, re-copy them and confirm none were skipped or truncated.");
        sb.AppendLine("4. Confirm the client version configured in TazUO matches the version of your UO data files.");
        fix = sb.ToString();
        return true;
    }

    /// <summary>
    ///     Recognizes a <see cref="MissingMethodException" /> naming a ZLib entry point - the
    ///     loaded ClassicUO.Utility.dll is older than this executable expects, most commonly
    ///     surfaced by the <c>-zlib</c> launch argument calling a newer ZLib API.
    /// </summary>
    /// <param name="e">Exception under inspection.</param>
    /// <param name="fix">Set to the suggested fix text when recognized.</param>
    /// <returns>True if the crash was recognized.</returns>
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
        sb.AppendLine(
            "The bundled zlib helper library (ClassicUO.Utility.dll) is older than this build of TazUO expects, which usually happens after a partial update or when new files were copied over an old installation.");
        sb.AppendLine("This crash is triggered by the '-zlib' launch argument, which uses a newer zlib feature that the out-of-date file does not have.");
        sb.AppendLine();
        sb.AppendLine("Suggested fixes:");
        sb.AppendLine(
            "1. Remove the '-zlib' launch argument. Instead, enable managed zlib from the Options menu on the login screen (Misc tab) - it does the same thing and is saved for you.");
        sb.AppendLine("2. Reinstall or re-download TazUO so every file is updated together, then try again.");
        sb.AppendLine("3. If you copied files manually, make sure ClassicUO.Utility.dll was replaced along with the rest of the client.");
        fix = sb.ToString();
        return true;
    }

    /// <summary>
    ///     Recognizes "Bad uop file" from <c>UOFileUop.FillEntries</c> - the .uop file did not
    ///     start with the expected header, so it is corrupt, truncated, still downloading, or a
    ///     placeholder rather than a real UOP file.
    /// </summary>
    /// <param name="e">Exception under inspection.</param>
    /// <param name="fix">Set to the suggested fix text when recognized.</param>
    /// <returns>True if the crash was recognized.</returns>
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
        sb.AppendLine(
            "The file did not start with the expected header, which means it is corrupt, truncated, still being downloaded/patched, or is not a real UOP file (for example an empty placeholder).");
        sb.AppendLine(
            "The name just above 'UOFileUop.FillEntries' in the error (for example TileArtLoader, ArtLoader, etc.) tells you which set of files is affected.");
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

    /// <summary>
    ///     Recognizes failures to load one of TazUO's own assemblies (for example
    ///     ClassicUO.Assets.dll) and picks the advice that matches the reason: the file is
    ///     absent, a Windows application control policy blocked it, its zone mark makes it
    ///     untrusted, or it is present but unloadable.
    /// </summary>
    /// <param name="e">Exception under inspection.</param>
    /// <param name="fix">Set to the suggested fix text when recognized.</param>
    /// <returns>True if the crash was recognized.</returns>
    private static bool TryGetMissingAssemblyCrashFix(Exception e, out string fix)
    {
        fix = null;

        // FileNotFoundException = the dll is absent.
        // FileLoadException = it was found, but the loader refused it, which on Windows is usually a security policy rather than
        // a broken installation, so the two need different advice.
        if (e is not FileNotFoundException and not FileLoadException)
            return false;

        // FileName carries either a bare assembly name or a full path depending on where
        // the loader failed; the message is partly localized but always embeds the name.
        string fileName = e switch
        {
            FileNotFoundException notFound => notFound.FileName,
            FileLoadException loadFailed => loadFailed.FileName,
            _ => null
        } ?? string.Empty;

        string assemblyName = Path.GetFileName(fileName);

        // Checked against Message (and InnerException.Message), never the full ToString() dump -
        // the stack trace alone contains "ClassicUO." in nearly every frame of this codebase and
        // would match almost any exception, not just a genuine assembly load failure.
        bool isOwnAssembly = assemblyName.StartsWith("ClassicUO.", StringComparison.OrdinalIgnoreCase) ||
                             e.Message.Contains("ClassicUO.", StringComparison.OrdinalIgnoreCase) ||
                             (e.InnerException?.Message.Contains("ClassicUO.", StringComparison.OrdinalIgnoreCase) ?? false);

        if (!isOwnAssembly)
            return false;

        fix = e switch
        {
            FileNotFoundException => BuildAssemblyMissingFix(assemblyName),
            FileLoadException when e.HResult == H_RESULT_BLOCKED_BY_APPLICATION_POLICY => BuildAssemblyBlockedByPolicyFix(assemblyName),
            FileLoadException when e.HResult == H_RESULT_UNTRUSTED_ZONE => BuildAssemblyUntrustedZoneFix(assemblyName),
            _ => BuildAssemblyUnloadableFix(assemblyName)
        };

        return true;
    }

    /// <summary>
    ///     Recognizes a <see cref="FileLoadException" /> raised by Windows' application control
    ///     policy (0x800711C7) for a file that is not one of TazUO's own assemblies.
    ///     <c>TryGetMissingAssemblyCrashFix</c> already covers <c>ClassicUO.*.dll</c> at startup;
    ///     this catches runtime dependencies such as MP3Sharp.dll that surface the same block
    ///     while the game is running.
    /// </summary>
    /// <param name="e">Exception under inspection.</param>
    /// <param name="fix">Set to the suggested fix text when recognized.</param>
    /// <returns>True if the crash was recognized.</returns>
    private static bool TryGetPolicyBlockedFileCrashFix(Exception e, out string fix)
    {
        fix = null;

        if (e is not FileLoadException fileLoadException ||
            fileLoadException.HResult != H_RESULT_BLOCKED_BY_APPLICATION_POLICY)
            return false;

        string fileName = Path.GetFileName(fileLoadException.FileName ?? string.Empty);

        fix = BuildPolicyBlockedFileFix(fileName, "Windows blocked a file TazUO needed to load while the game was running.");
        return true;
    }

    /// <summary>
    ///     Sentence subject naming the file that failed, or a neutral stand-in when the loader
    ///     reported no name. Never substitutes a likely-looking name - a wrong file name in a
    ///     crash log sends the user chasing a file that was never involved.
    /// </summary>
    /// <param name="assemblyName">File name taken from the exception; may be empty.</param>
    /// <returns>Either <c>File "name.dll"</c> or a generic stand-in, both usable as a subject.</returns>
    private static string DescribeAffectedFile(string assemblyName) => string.IsNullOrEmpty(assemblyName) ? "A required TazUO file" : $"File \"{assemblyName}\"";

    /// <summary>
    ///     Appends the clean reinstall procedure. Installing over an existing folder does not
    ///     resolve these crashes, so the backup/delete steps are the point.
    /// </summary>
    /// <param name="sb">Builder the steps are appended to.</param>
    /// <param name="includeHeader">
    ///     False when the caller already introduced the steps with its own conditional lead-in.
    /// </param>
    /// <param name="redownloadInstruction">
    ///     Replaces the default step 4 wording for callers with a more specific redownload
    ///     instruction (for example: redownload one particular channel).
    /// </param>
    private static void AppendCleanReinstallSteps(StringBuilder sb, bool includeHeader = true, string redownloadInstruction = null)
    {
        string dataPath = Path.Join(CUOEnviroment.ExecutablePath, "Data");
        string scriptsPath = Path.Join(CUOEnviroment.ExecutablePath, "LegionScripts");

        if (includeHeader)
            sb.AppendLine("A clean reinstall of TazUO fixes this:");

        sb.AppendLine("1. Close TazUO and the launcher.");
        sb.AppendLine($"2. Back up your data folder ('{dataPath}') and your script folder ('{scriptsPath}').");
        sb.AppendLine("3. Delete everything else in the TazUO folder. Do not install a new build on top of an old one.");
        sb.AppendLine($"4. {redownloadInstruction ?? "Re-download TazUO from the launcher and let it finish."}");
        sb.AppendLine("5. Copy your backed up folders back.");
    }

    /// <summary>Builds advice for the case where the assembly file itself is absent.</summary>
    /// <param name="assemblyName">File name from the exception, or empty if unreported.</param>
    private static string BuildAssemblyMissingFix(string assemblyName)
    {
        var sb = new StringBuilder();
        sb.AppendLine("TazUO could not start because one of its files is missing.");
        sb.AppendLine($"{DescribeAffectedFile(assemblyName)} was not found. This usually means the install did not finish, or some files were not copied over.");
        sb.AppendLine();
        AppendCleanReinstallSteps(sb);
        sb.AppendLine();
        sb.AppendLine("If it happens again, your antivirus may be deleting TazUO's files. Add the TazUO folder to its exclusions.");
        return sb.ToString();
    }

    /// <summary>
    ///     Builds advice for <see cref="H_RESULT_BLOCKED_BY_APPLICATION_POLICY" />, branching on
    ///     detected S mode state since a reinstall cannot fix S mode.
    /// </summary>
    /// <param name="assemblyName">File name from the exception, or empty if unreported.</param>
    private static string BuildAssemblyBlockedByPolicyFix(string assemblyName) =>
        BuildPolicyBlockedFileFix(assemblyName, "Windows prevented TazUO from starting.");

    /// <summary>
    ///     Shared advice for <see cref="H_RESULT_BLOCKED_BY_APPLICATION_POLICY" /> - a Windows
    ///     application control policy refused a file. The same block can hit a TazUO assembly at
    ///     startup or a runtime dependency (for example MP3Sharp.dll), so the build differs only
    ///     in its lead-in sentence.
    /// </summary>
    /// <param name="fileName">File name from the exception, or empty if unreported.</param>
    /// <param name="header">Lead-in sentence; startup and runtime failures use different wording.</param>
    private static string BuildPolicyBlockedFileFix(string fileName, string header)
    {
        SModeState sMode = WindowsSMode.GetState();

        var sb = new StringBuilder();
        sb.AppendLine(header);
        sb.AppendLine($"{DescribeAffectedFile(fileName)} is present but Windows security prevented its load. This may sometimes occur after an update.");

        // Windows S mode allows only store apps - the fix here is specific - disable it.
        if (sMode == SModeState.Enabled)
        {
            AppendSModeIsCauseSection(sb);
            return sb.ToString();
        }

        sb.AppendLine("The block comes from Windows, not from a fault in TazUO.");
        sb.AppendLine();

        // We may still be in S mode but failed to detect it for some reason. Provide a manual check procedure for the user
        if (sMode == SModeState.Unknown)
            AppendSModeCheckSection(sb);
        else
        {
            // A block like this is usually all-or-nothing (see H_RESULT_BLOCKED_BY_APPLICATION_POLICY),
            // so reinstalling the same file will not lift it - only worth ruling out a corrupted download.
            sb.AppendLine("First, rule out a corrupted download with a clean reinstall:");
            AppendCleanReinstallSteps(sb, false);
        }

        sb.AppendLine();
        AppendAssemblyBlockedRemainingSteps(sb);
        return sb.ToString();
    }

    /// <summary>
    ///     S mode is confirmed active - no other advice applies, since reinstalling or touching
    ///     antivirus/Smart App Control settings cannot make a Store-only OS run a desktop app.
    /// </summary>
    /// <param name="sb">Builder the section is appended to.</param>
    private static void AppendSModeIsCauseSection(StringBuilder sb)
    {
        sb.AppendLine(
            "Your computer runs Windows in S mode, which only allows apps from the Microsoft Store, so TazUO cannot run until you leave it. Reinstalling will not help.");
        sb.AppendLine();
        sb.AppendLine("Warning: leaving S mode is permanent. You cannot go back to it afterwards.");
        sb.AppendLine(
            "To disable S mode, open Settings -> System -> Activation and use the 'Switch to Windows Home' or 'Switch to Windows Pro' link, then start TazUO again.");
    }

    /// <summary>
    ///     S mode could not be detected (for example an older Windows build) - ask the user to
    ///     check manually before pointing them at a reinstall that S mode would make pointless.
    /// </summary>
    /// <param name="sb">Builder the section is appended to.</param>
    private static void AppendSModeCheckSection(StringBuilder sb)
    {
        sb.AppendLine("First, check whether your computer runs Windows in S mode, under Settings -> System -> Activation.");
        sb.AppendLine("S mode only allows apps from the Microsoft Store, so TazUO cannot run until you leave it. Reinstalling will not help.");
        sb.AppendLine("Warning: leaving S mode is permanent. You cannot go back to it afterwards.");
        sb.AppendLine("To disable S mode, click the 'Switch to Windows Home' or 'Switch to Windows Pro' link on that page, then start TazUO again.");
        sb.AppendLine();
        sb.AppendLine("If you are not in S mode, a clean reinstall of TazUO may fix this:");
        AppendCleanReinstallSteps(sb, false);
    }

    /// <summary>
    ///     Appends the fallback steps for a policy block once S mode has been ruled out or
    ///     already addressed: antivirus, install location, admin-managed machines, Smart App
    ///     Control as a last resort.
    /// </summary>
    /// <param name="sb">Builder the steps are appended to.</param>
    private static void AppendAssemblyBlockedRemainingSteps(StringBuilder sb)
    {
        sb.AppendLine("If TazUO is still blocked:");
        sb.AppendLine("- Add the TazUO folder to your antivirus exclusions. In Windows Security: 'Virus & threat protection' -> 'Manage settings' -> 'Exclusions'.");
        sb.AppendLine(@"- Install TazUO in your Documents folder rather than directly on C:\ or D:\.");
        sb.AppendLine("- On a work or school computer you may not be able to change this yourself. Ask whoever manages the computer to allow the TazUO folder.");
        sb.AppendLine("- Last resort: Windows Security -> 'App & browser control' -> 'Smart App Control' blocks files it does not recognise.");
        sb.AppendLine("  Warning: switching it off is permanent. Windows will not let you switch it back on without reinstalling Windows.");
    }

    /// <summary>
    ///     Builds advice for <see cref="H_RESULT_UNTRUSTED_ZONE" />. Unlike the other assembly
    ///     failures, the cause here is directly deducible from the HRESULT alone (a zone mark or
    ///     a network path), so that specific fix leads and a reinstall is only the fallback.
    /// </summary>
    /// <param name="assemblyName">File name from the exception, or empty if unreported.</param>
    private static string BuildAssemblyUntrustedZoneFix(string assemblyName)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Windows prevented TazUO from starting.");
        sb.AppendLine(
            $"{DescribeAffectedFile(assemblyName)} is present but Windows would not load it, because the file is still marked as downloaded from the internet, or TazUO is running from a network drive.");
        sb.AppendLine();
        sb.AppendLine(
            "- If TazUO is on a network or shared drive, move the whole folder to your own computer's drive. Running it from a network drive causes this every time.");
        sb.AppendLine("- Otherwise, right-click the file, open Properties, and tick 'Unblock' near the bottom if it is there, then click OK.");
        sb.AppendLine();
        sb.AppendLine("If that does not resolve it, a clean reinstall of TazUO fixes this:");
        AppendCleanReinstallSteps(sb, false);
        return sb.ToString();
    }

    /// <summary>
    ///     Builds advice for the fallback case: the file loaded neither cleanly nor with a
    ///     recognized HRESULT, so it is likely damaged, locked, or a leftover mismatched file.
    /// </summary>
    /// <param name="assemblyName">File name from the exception, or empty if unreported.</param>
    private static string BuildAssemblyUnloadableFix(string assemblyName)
    {
        var sb = new StringBuilder();
        sb.AppendLine("TazUO could not start.");
        sb.AppendLine(
            $"{DescribeAffectedFile(assemblyName)} is present but could not be opened. It is most likely damaged, still being written, in use by another program, or left over from an older version of TazUO.");
        sb.AppendLine();
        AppendCleanReinstallSteps(sb);
        sb.AppendLine();
        sb.AppendLine("Make sure TazUO and the launcher are fully closed and any update has finished before starting again.");
        sb.AppendLine("If a clean reinstall does not help, your antivirus may be holding the file. Add the TazUO folder to its exclusions.");
        return sb.ToString();
    }

    /// <summary>
    ///     Recognizes a crash inside plugin loading (<c>Plugin.Load</c>, <c>Plugin.Create</c>,
    ///     <c>GameController.LoadPlugins</c>) - the fault is in the third-party plugin's own
    ///     init code, not in TazUO.
    /// </summary>
    /// <param name="e">Exception under inspection.</param>
    /// <param name="fix">Set to the suggested fix text when recognized.</param>
    /// <returns>True if the crash was recognized.</returns>
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
        bool crashedInPluginLoad = details.Contains("ClassicUO.Network.Plugin.Load") ||
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

    /// <summary>
    ///     Recognizes a crash inside <c>Plugin.OnPluginRecv</c>/<c>OnPluginSend</c> - a
    ///     third-party plugin injected a packet larger than the buffer it allocated for it.
    /// </summary>
    /// <param name="e">Exception under inspection.</param>
    /// <param name="fix">Set to the suggested fix text when recognized.</param>
    /// <returns>True if the crash was recognized.</returns>
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
        bool crashedInPluginPacketApi = details.Contains("ClassicUO.Network.Plugin.OnPluginRecv") ||
                                        details.Contains("ClassicUO.Network.Plugin.OnPluginSend");

        if (!crashedInPluginPacketApi)
            return false;

        var sb = new StringBuilder();
        sb.AppendLine("A plugin crashed TazUO while injecting a network packet into the client.");
        sb.AppendLine(
            "The error above was thrown when a third-party plugin (for example an assistant such as Razor) used the client's packet-injection API and handed over a packet that did not fit its allocated buffer.");
        sb.AppendLine();
        sb.AppendLine("Suggested fixes:");
        sb.AppendLine("1. Update the plugin (and any of its scripting add-ons) to the latest version - this crash is usually a plugin bug, not a TazUO bug.");
        sb.AppendLine("2. Temporarily disable the plugin to confirm it is the cause.");
        sb.AppendLine("3. Make sure the plugin is pointed at the same UO data directory TazUO uses and that its files are not corrupt or out of date.");
        sb.AppendLine("4. If it keeps happening, report the crash log to the plugin's author, noting which script or feature was running when it occurred.");

        fix = sb.ToString();
        return true;
    }

    /// <summary>
    ///     Recognizes any <see cref="UnauthorizedAccessException" /> - Windows refused
    ///     read/write access to one of TazUO's own files (profile data, logs, cache, etc).
    ///     Not tied to a single call site, since this can be thrown from anywhere TazUO touches
    ///     disk.
    /// </summary>
    /// <param name="e">Exception under inspection.</param>
    /// <param name="fix">Set to the suggested fix text when recognized.</param>
    /// <returns>True if the crash was recognized.</returns>
    private static bool TryGetFileAccessDeniedCrashFix(Exception e, out string fix)
    {
        fix = null;

        if (e is not UnauthorizedAccessException)
            return false;

        var sb = new StringBuilder();
        sb.AppendLine("TazUO could not read or write one of its files because Windows denied access to it.");
        sb.AppendLine("This is a Windows file permission issue, not a bug in TazUO or a corrupted file.");
        sb.AppendLine();
        sb.AppendLine("Suggested fixes:");
        sb.AppendLine("1. Make sure TazUO is not installed directly on the C: drive root or inside a system folder like Program Files - move it to your Documents folder instead.");
        sb.AppendLine("2. Right-click the TazUO folder -> Properties -> Security, and make sure your Windows user account has Full control.");
        sb.AppendLine("3. Add the TazUO folder to your antivirus exclusions - some antivirus software locks files it is scanning.");
        sb.AppendLine("4. Make sure no other program (a backup tool, cloud sync, another copy of TazUO) has the file open at the same time.");
        sb.AppendLine("5. Try running TazUO as Administrator once to rule out a permissions problem.");
        fix = sb.ToString();
        return true;
    }
}
