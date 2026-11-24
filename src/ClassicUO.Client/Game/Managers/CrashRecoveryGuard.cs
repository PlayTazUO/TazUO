#nullable enable

using System;

namespace ClassicUO.Game.Managers;

/// <summary>
///     Decides whether a crashed client may rebuild itself in place, so a fault that reproduces on every
///     attempt cannot keep cycling the client through the login screen instead of letting it die.
/// </summary>
/// <remarks>
///     Evaluated only when a crash happens - nothing runs while the client is healthy. The history is in
///     memory: recovery keeps the process alive, so a crash chain never crosses a process boundary, and a
///     file shared by every client in <c>Data/</c> would refuse to recover a second box just because another
///     client on the machine crashed.
/// </remarks>
internal static class CrashRecoveryGuard
{
    /// <summary>Recoveries allowed inside <see cref="WindowMs"/> before the client stops trying.</summary>
    public const int MaxRecoveries = 3;

    /// <summary>How long an allowance lasts, in milliseconds.</summary>
    public const long WindowMs = 10 * 60 * 1000;

    private static long _windowStartMs;
    private static int _recoveries;

    /// <summary>
    ///     Records a recovery attempt and reports whether it may go ahead.
    /// </summary>
    /// <remarks>
    ///     Denies once <see cref="MaxRecoveries"/> crashes have happened inside <see cref="WindowMs"/>; the
    ///     caller is expected to close the client then. The allowance starts over once the window has passed,
    ///     so crashes hours apart never accumulate against each other.
    /// </remarks>
    /// <param name="nowMs">Current time, usually <see cref="Environment.TickCount64"/>.</param>
    /// <returns><see langword="true"/> if the client may rebuild itself.</returns>
    public static bool TryBeginRecovery(long nowMs)
    {
        if (_recoveries == 0 || nowMs - _windowStartMs >= WindowMs)
        {
            _windowStartMs = nowMs;
            _recoveries = 1;

            return true;
        }

        return ++_recoveries <= MaxRecoveries;
    }

    /// <summary>Drops the recorded crashes.</summary>
    /// <remarks>Isolation seam for tests.</remarks>
    public static void Reset() => _recoveries = 0;
}
