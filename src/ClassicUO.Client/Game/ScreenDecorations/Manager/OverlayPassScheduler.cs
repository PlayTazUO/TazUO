#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using ClassicUO.Game.Managers;
using ClassicUO.Utility.Logging;
using Lock = System.Threading.Lock;

namespace ClassicUO.Game.ScreenDecorations.Manager;

/// <summary>
/// Decides when the overlay manager gets to run a reconcile pass, and marshals every one of them
/// onto the main thread.
/// <para>
/// Three things ask for a pass and nothing else: something calling <see cref="Queue" /> directly (an
/// event trigger firing, a settings change), a one-shot wake-up for the instant the next occurrence
/// lapses, and - only while some rule needs polling - a timer. A client whose enabled rules are all
/// event-driven therefore wakes the main thread not at all until one of them fires.
/// </para>
/// <para>
/// This class is the whole threading surface of the overlay system: everything it owns is under
/// <see cref="_sync" />, and the manager's own state is main-thread only because passes are the only
/// way in. The lock is never held across a pass - that runs on the main thread, and blocking a timer
/// thread on it would let passes queue up behind a stalled frame.
/// </para>
/// </summary>
internal sealed class OverlayPassScheduler
{
    #region Private members

    /// <summary>
    /// Gap between polling passes. The floor on how long an overlay can lag the state that justifies
    /// it, and half the average lag.
    /// </summary>
    private static readonly TimeSpan _pollInterval = TimeSpan.FromMilliseconds(350);

    private readonly Action _runPass;

    private readonly Lock _sync = new();

    /// <summary>Whether passes may be queued at all.</summary>
    private bool _running;

    /// <summary>Set while a pass is queued or running, so a slow frame cannot leave several passes
    /// stacked up waiting on the main thread.</summary>
    private bool _passPending;

    /// <summary>Cancels the polling loop. Null exactly when no rule needs polling.</summary>
    private CancellationTokenSource? _pollCancellation;

    /// <summary>Cancels the pending wake-up for the next occurrence to lapse.</summary>
    private CancellationTokenSource? _expiryCancellation;

    /// <summary>What that wake-up is set for, so an unchanged deadline is not rescheduled on every
    /// pass.</summary>
    private DateTime? _expiryDeadline;

    #endregion

    #region Ctor

    /// <param name="runPass">The pass to run. Always invoked on the main thread, never re-entrantly.</param>
    public OverlayPassScheduler(Action runPass) => _runPass = runPass;

    #endregion

    #region Public methods

    /// <summary>
    /// Starts or stops scheduling. Stopping cancels both timers and drops any pending request, so a
    /// pass queued moments earlier cannot re-show what a teardown just took down.
    /// </summary>
    /// <param name="running">Whether anything could need a pass.</param>
    public void SetRunning(bool running)
    {
        if (running)
        {
            lock (_sync)
                _running = true;

            return;
        }

        CancellationTokenSource? poll;
        CancellationTokenSource? expiry;

        lock (_sync)
        {
            _running = false;
            _passPending = false;
            poll = _pollCancellation;
            expiry = _expiryCancellation;
            _pollCancellation = null;
            _expiryCancellation = null;
            _expiryDeadline = null;
        }

        Stop(poll);
        Stop(expiry);
    }

    /// <summary>
    /// Runs the polling loop only while some rule needs polling. A client whose rules are all
    /// event-driven should not be waking the main thread twice a second to re-read nothing.
    /// </summary>
    /// <param name="needed">Whether any enabled rule is a polling one.</param>
    public void SetPollingNeeded(bool needed)
    {
        CancellationTokenSource? started = null;
        CancellationTokenSource? stopped = null;

        lock (_sync)
        {
            if (!_running)
                needed = false;

            if (needed == (_pollCancellation != null))
                return;

            if (needed)
            {
                started = _pollCancellation = new CancellationTokenSource();
            }
            else
            {
                stopped = _pollCancellation;
                _pollCancellation = null;
            }
        }

        if (started != null)
        {
            CancellationToken token = started.Token;
            _ = Task.Run(() => PollLoop(token), token);

            return;
        }

        Stop(stopped);
    }

    /// <summary>
    /// Wakes the manager at the instant the next occurrence lapses, so a declared duration is
    /// honoured exactly instead of being rounded up to the next polling pass.
    /// </summary>
    /// <param name="deadline">When to wake, or null if nothing is pending.</param>
    public void ScheduleExpiry(DateTime? deadline)
    {
        CancellationTokenSource? stopped;
        CancellationTokenSource? started = null;

        lock (_sync)
        {
            if (deadline == _expiryDeadline)
                return;

            stopped = _expiryCancellation;
            _expiryCancellation = null;
            _expiryDeadline = null;

            if (deadline != null && _running)
            {
                started = _expiryCancellation = new CancellationTokenSource();
                _expiryDeadline = deadline;
            }
        }

        Stop(stopped);

        if (started == null)
            return;

        TimeSpan delay = deadline!.Value - DateTime.UtcNow;

        _ = ExpireAfter(delay < TimeSpan.Zero ? TimeSpan.Zero : delay, started.Token);
    }

    /// <summary>
    /// Asks for a pass on the main thread. Drops the request while nothing is running, so callers
    /// that fire on demand cost nothing with the system off. Safe from any thread.
    /// </summary>
    public void Queue()
    {
        lock (_sync)
        {
            if (_passPending || !_running)
                return;

            _passPending = true;
        }

        MainThreadQueue.EnqueueAction(RunQueuedPass);
    }

    #endregion

    #region Private methods

    private void RunQueuedPass()
    {
        try
        {
            bool running;

            lock (_sync)
                running = _running;

            if (running)
                _runPass();
        }
        finally
        {
            lock (_sync)
                _passPending = false;
        }
    }

    private async Task PollLoop(CancellationToken token)
    {
        try
        {
            using var timer = new PeriodicTimer(_pollInterval);

            while (await timer.WaitForNextTickAsync(token))
                Queue();
        }
        catch (OperationCanceledException)
        {
            // Torn down during the wait. Nothing to unwind: whoever cancelled has already cleared
            // the overlays or will on the next pass.
        }
        catch (Exception e)
        {
            // This loop is the only thing driving polling rules; dying silently would leave them
            // frozen on whatever was last shown, with no clue as to why.
            Log.Error($"Screen overlay polling loop stopped: {e}");
        }
    }

    private async Task ExpireAfter(TimeSpan delay, CancellationToken token)
    {
        try
        {
            await Task.Delay(delay, token);
            Queue();
        }
        catch (OperationCanceledException)
        {
            // A later occurrence moved the deadline, or the system was torn down.
        }
    }

    private static void Stop(CancellationTokenSource? cancellation)
    {
        cancellation?.Cancel();
        cancellation?.Dispose();
    }

    #endregion
}
