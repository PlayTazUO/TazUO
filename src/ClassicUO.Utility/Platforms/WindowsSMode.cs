using System;
using System.Runtime.InteropServices;

namespace ClassicUO.Utility.Platforms
{
    /// <summary>
    /// Result of querying Windows S mode. <see cref="Unknown"/> means the policy could not be
    /// read on a Windows machine (for example, an unsupported Windows version) and must not be
    /// treated as "not in S mode". Non-Windows systems report <see cref="Disabled"/> - S mode is
    /// a Windows-only concept, so its absence there is certain, not unknown.
    /// </summary>
    public enum SModeState
    {
        Unknown,
        Enabled,
        Disabled
    }

    /// <summary>
    /// Reports whether Windows is running in S mode, which allows Microsoft Store apps only and
    /// therefore blocks TazUO outright. Callers use this to tell the difference between a broken
    /// installation - which a reinstalling fixes - and a policy block, which it cannot.
    /// </summary>
    public static partial class WindowsSMode
    {
        private const string CODE_INTEGRITY_POLICY_KEY = @"SYSTEM\CurrentControlSet\Control\CI\Policy";

        /// <summary>Present since Windows 10 1803; 1 means the machine is in S mode.</summary>
        private const string SKU_POLICY_REQUIRED_VALUE = "SkuPolicyRequired";

        private const uint RESTRICT_TYPE_REG_DWORD = 0x00000010;
        private const int ERROR_SUCCESS = 0;
        private const int ERROR_FILE_NOT_FOUND = 2;
        private const int ERROR_PATH_NOT_FOUND = 3;

        private static readonly IntPtr _hkeyLocalMachine = new(unchecked((int)0x80000002));

        /// <summary>
        /// S mode cannot change without a reboot, so the first successful read is kept for the
        /// lifetime of the process.
        /// </summary>
        private static SModeState? _cachedState;

        /// <summary>
        /// Queries whether the current machine runs Windows in S mode.
        /// </summary>
        /// <returns>
        /// <see cref="SModeState.Disabled"/> on non-Windows systems, when the policy value is
        /// simply absent (the normal case outside S mode), or when it was read and is 0;
        /// <see cref="SModeState.Enabled"/> when it was read and is 1; otherwise
        /// <see cref="SModeState.Unknown"/> for an unexpected read failure. Never throws.
        /// </returns>
        public static SModeState GetState()
        {
            _cachedState ??= ReadState();
            return _cachedState.Value;
        }

        private static SModeState ReadState()
        {
            if (!PlatformHelper.IsWindows)
                return SModeState.Disabled;

            try
            {
                uint dataSize = sizeof(uint);
                int result = RegGetValueW(
                    _hkeyLocalMachine,
                    CODE_INTEGRITY_POLICY_KEY,
                    SKU_POLICY_REQUIRED_VALUE,
                    RESTRICT_TYPE_REG_DWORD,
                    IntPtr.Zero,
                    out uint skuPolicyRequired,
                    ref dataSize
                );

                // A missing key/value means the policy was never set, which is the normal state
                // on a non-S-mode machine - not an unknown result.
                if (result is ERROR_FILE_NOT_FOUND or ERROR_PATH_NOT_FOUND)
                    return SModeState.Disabled;

                if (result != ERROR_SUCCESS)
                    return SModeState.Unknown;

                return skuPolicyRequired == 1 ? SModeState.Enabled : SModeState.Disabled;
            }
            catch (Exception)
            {
                // Reached when advapi32 cannot be resolved at all. The caller only ever uses this
                // to pick wording, so a failed probe must stay silent rather than mask the crash
                // that prompted it.
                return SModeState.Unknown;
            }
        }

        /// <summary>
        /// Win32 <c>RegGetValueW</c>. Reads a single registry value with its type restricted by
        /// <paramref name="flags"/>, so a value of the wrong type fails the call instead of
        /// silently reinterpreting bytes.
        /// </summary>
        /// <param name="hkey">Predefined root key handle, for example <see cref="_hkeyLocalMachine"/>.</param>
        /// <param name="subKey">Key path relative to <paramref name="hkey"/>.</param>
        /// <param name="valueName">Name of the value to read.</param>
        /// <param name="flags">RRF_RT_* restriction, for example <see cref="RESTRICT_TYPE_REG_DWORD"/>.</param>
        /// <param name="type">Receives the registry type code; <see cref="IntPtr.Zero"/> if not needed.</param>
        /// <param name="data">Receives the value's data.</param>
        /// <param name="dataSize">Size of <paramref name="data"/> in bytes, in and out.</param>
        /// <returns>A Win32 error code; <see cref="ERROR_SUCCESS"/> on success.</returns>
        [LibraryImport("advapi32.dll", EntryPoint = "RegGetValueW", StringMarshalling = StringMarshalling.Utf16)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static partial int RegGetValueW(
            IntPtr hkey,
            string subKey,
            string valueName,
            uint flags,
            IntPtr type,
            out uint data,
            ref uint dataSize
        );
    }
}
