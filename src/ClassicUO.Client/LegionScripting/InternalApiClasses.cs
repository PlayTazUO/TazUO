using System.Timers;

namespace ClassicUO.LegionScripting;

/// <summary>
/// Represents a user-requested timed callback
/// </summary>
/// <param name="timer">The timer serving the callback</param>
/// <param name="timesToRepeat">The number of times to repeat after initial callback invocation</param>
internal class TimedCallback(Timer timer, int timesToRepeat)
{
    public Timer Timer { get; } = timer;
    public int TimesToRepeat { get; } = timesToRepeat;
    public ulong TimesInvoked { get; set; }
}
