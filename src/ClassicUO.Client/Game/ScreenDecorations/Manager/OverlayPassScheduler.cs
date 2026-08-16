#nullable enable

using System;

namespace ClassicUO.Game.ScreenDecorations.Manager;

/// <summary>
/// Decides when the overlay manager gets to run a reconcile pass. Driven by the frame loop:
/// <see cref="Tick" /> is called once per frame from the scene update, and everything else here only
/// sets a flag that it reads.
/// <para>
/// Three things ask for a pass and nothing else: <see cref="RequestPass" /> (an event trigger firing,
/// a settings change), the polling interval elapsing - only while some rule needs polling - and the
/// next occurrence lapsing.
/// </para>
/// <para>
/// Main thread only, deliberately. Callers arriving from elsewhere marshal before they reach the
/// manager, which leaves this class with no locks, timers or background tasks: a request is a bool
/// write, and a pass runs inline on the frame that notices it.
/// </para>
/// </summary>
internal sealed class OverlayPassScheduler
{
    #region Private members

    /// <summary>
    /// Gap between polling passes. The floor on how long an overlay can lag the state that justifies
    /// it, and half the average lag.
    /// </summary>
    private const uint POLL_INTERVAL_MS = 350;

    /// <summary>
    /// Ceiling on how far ahead a deadline may be set. Keeps every deadline difference inside the
    /// signed range the wrap-safe comparison in <see cref="Tick" /> relies on.
    /// </summary>
    private const uint MAX_DEADLINE_MS = uint.MaxValue / 4;

    private readonly Action _runPass;

    /// <summary>Whether passes may run at all.</summary>
    private bool _enabled;

    /// <summary>Whether any enabled rule has to be sampled rather than waited on.</summary>
    private bool _polling;

    /// <summary>A pass is wanted on the next frame.</summary>
    private bool _requested;

    /// <summary>Frame clock reading the next polling pass is due at.</summary>
    private uint _nextPoll;

    /// <summary>Frame clock reading the soonest live occurrence lapses at.</summary>
    private uint _expiryAt;

    /// <summary>Whether <see cref="_expiryAt" /> means anything.</summary>
    private bool _hasExpiry;

    #endregion

    #region Ctor

    /// <param name="runPass">The pass to run. Always invoked on the main thread, never re-entrantly.</param>
    public OverlayPassScheduler(Action runPass)
    {
        _runPass = runPass;
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Runs a pass if one is due. Called once per frame; costs a field read and a branch when the
    /// system is off, which is the whole reason the timers this replaced are gone.
    /// </summary>
    public void Tick()
    {
        if (!_enabled)
            return;

        uint now = Time.Ticks;

        // Subtraction rather than `now >= deadline`: Time.Ticks is a uint that wraps about every 49
        // days, and only the difference stays meaningful across that wrap. Deadlines are capped at
        // MAX_DEADLINE_MS so the difference always fits the signed cast.
        if (_polling && (int)(now - _nextPoll) >= 0)
        {
            _nextPoll = now + POLL_INTERVAL_MS;
            _requested = true;
        }

        if (_hasExpiry && (int)(now - _expiryAt) >= 0)
        {
            _hasExpiry = false;
            _requested = true;
        }

        if (!_requested)
            return;

        _requested = false;
        _runPass();
    }

    /// <summary>
    /// Starts or stops scheduling. Stopping drops any pending request and the expiry, so a pass
    /// asked for moments earlier cannot re-show what a teardown just took down.
    /// </summary>
    /// <param name="enabled">Whether anything could need a pass.</param>
    public void SetEnabled(bool enabled)
    {
        _enabled = enabled;

        if (enabled)
            return;

        _requested = false;
        _hasExpiry = false;
    }

    /// <summary>
    /// Polls only while some rule needs it. A client whose rules are all event-driven should not be
    /// re-reading nothing twice a second.
    /// </summary>
    /// <param name="needed">Whether any enabled rule is a polling one.</param>
    public void SetPollingNeeded(bool needed)
    {
        if (needed == _polling)
            return;

        _polling = needed;

        // Due at once, so a rule that the player already qualifies for does not wait out an interval
        // before it can be noticed.
        if (needed)
            _nextPoll = Time.Ticks;
    }

    /// <summary>
    /// Wakes the manager on the frame the next occurrence lapses, so a declared duration is honoured
    /// to within a frame instead of being rounded up to the next polling pass.
    /// </summary>
    /// <param name="deadline">Frame clock reading to wake at, or null if nothing is pending.</param>
    public void ScheduleExpiry(uint? deadline)
    {
        _hasExpiry = deadline.HasValue;
        _expiryAt = deadline ?? 0;
    }

    /// <summary>
    /// Converts an absolute instant into the frame-clock deadline <see cref="ScheduleExpiry" /> wants.
    /// Called once per pass rather than per frame: reading DateTime.UtcNow costs around 50x what
    /// reading Time.Ticks does, so the conversion happens where passes are, not where frames are.
    /// </summary>
    /// <param name="at">When the occurrence lapses.</param>
    /// <param name="now">The instant to measure from.</param>
    /// <returns>The frame clock reading to wake at.</returns>
    public static uint ToDeadline(DateTime at, DateTime now)
    {
        double ms = (at - now).TotalMilliseconds;

        if (ms <= 0)
            return Time.Ticks;

        return Time.Ticks + (uint)Math.Min(ms, MAX_DEADLINE_MS);
    }

    /// <summary>
    /// Asks for a pass on the next frame. Drops the request while nothing is running, so callers that
    /// fire on demand cost nothing with the system off.
    /// </summary>
    public void RequestPass()
    {
        if (_enabled)
            _requested = true;
    }

    #endregion
}
