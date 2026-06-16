using System;
using Timer = System.Timers.Timer;

namespace ClassicUO.LegionScripting;

/// <summary>
///     Represents a user-requested timed callback
/// </summary>
/// <param name="callback">The callback to invoke</param>
/// <param name="timer">The timer serving the callback</param>
/// <param name="timesToRepeat">The number of times to repeat after the initial callback invocation</param>
internal class TimedCallback(Action callback, Timer timer, int timesToRepeat)
{
    public Action Callback { get; } = callback;
    public Timer Timer { get; } = timer;
    public int TimesToRepeat { get; } = timesToRepeat;
    public ulong TimesInvoked { get; set; }
    public bool IsCancellationRequested { get; set; }
}
