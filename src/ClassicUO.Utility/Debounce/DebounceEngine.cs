using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ClassicUO.Utility.Debounce;

/// <summary>
///     Shared timing engine backing <see cref="Debounce" /> and <see cref="Debounce{T}" />. Internal
///     implementation detail: callers should use one of those two types instead of this class directly.
/// </summary>
/// <remarks>
///     All public members lock on <c>this</c> via <see cref="MethodImplOptions.Synchronized" />. That is
///     safe here only because this class is <see langword="internal" /> and never exposed to callers, so
///     nothing outside this assembly can take a competing lock on the same instance.
/// </remarks>
internal sealed class DebounceEngine : IDisposable
{
    private readonly Action _invoke;
    private readonly int _waitMs;
    private readonly int? _maxWaitMs;
    private readonly bool _leading;
    private readonly bool _trailing;
    private readonly Timer _timer;

    private DateTime _windowStart;
    private bool _windowOpen;
    private bool _callPending;
    private bool _disposed;

    /// <summary>
    ///     Creates a new debounce engine.
    /// </summary>
    /// <param name="invoke">The delegate to invoke once debouncing settles.</param>
    /// <param name="waitMs">Milliseconds of silence required before <paramref name="invoke" /> fires.</param>
    /// <param name="leading">Whether to invoke on the leading edge of a burst of calls.</param>
    /// <param name="trailing">Whether to invoke on the trailing edge of a burst of calls.</param>
    /// <param name="maxWaitMs">
    ///     The maximum time <paramref name="invoke" /> may be delayed while calls keep arriving.
    ///     <see langword="null" /> means no cap.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="invoke" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="waitMs" /> is negative.</exception>
    public DebounceEngine(Action invoke, int waitMs, bool leading, bool trailing, int? maxWaitMs)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(waitMs);

        _invoke = invoke ?? throw new ArgumentNullException(nameof(invoke));
        _waitMs = waitMs;
        _maxWaitMs = maxWaitMs;
        _leading = leading;
        // At least one edge must fire, otherwise the debounced action would never run.
        _trailing = trailing || !leading;

        _timer = new Timer(_ => Elapsed(), null, Timeout.Infinite, Timeout.Infinite);
    }

    /// <summary>
    ///     Registers a call. If no window is currently open this starts one (invoking immediately when
    ///     <see cref="_leading" /> is set); otherwise it extends the current window and marks a trailing
    ///     invocation as pending.
    /// </summary>
    [MethodImpl(MethodImplOptions.Synchronized)]
    public void Call()
    {
        if (_disposed)
            return;

        DateTime now = DateTime.UtcNow;
        bool isNewWindow = !_windowOpen;

        if (isNewWindow)
        {
            _windowOpen = true;
            _windowStart = now;

            if (_leading)
            {
                _callPending = false;
                _invoke();
            }
            else
                _callPending = true;
        }
        else
            _callPending = true;

        int delay = _waitMs;

        if (_maxWaitMs.HasValue)
        {
            int elapsed = (int)(now - _windowStart).TotalMilliseconds;
            delay = Math.Min(delay, Math.Max(0, _maxWaitMs.Value - elapsed));
        }

        _timer.Change(delay, Timeout.Infinite);
    }

    /// <summary>Timer callback: closes the current window and fires the trailing invocation if one is due.</summary>
    [MethodImpl(MethodImplOptions.Synchronized)]
    private void Elapsed()
    {
        if (_disposed)
            return;

        _windowOpen = false;

        if (_trailing && _callPending)
            _invoke();

        _callPending = false;
    }

    /// <summary>Closes the current window without invoking, discarding any pending trailing call.</summary>
    [MethodImpl(MethodImplOptions.Synchronized)]
    public void Cancel()
    {
        if (_disposed)
            return;

        _windowOpen = false;
        _callPending = false;
        _timer.Change(Timeout.Infinite, Timeout.Infinite);
    }

    /// <summary>If a window is open, closes it immediately, invoking now if a call is pending.</summary>
    [MethodImpl(MethodImplOptions.Synchronized)]
    public void Flush()
    {
        if (_disposed)
            return;

        if (!_windowOpen)
            return;

        _windowOpen = false;
        _timer.Change(Timeout.Infinite, Timeout.Infinite);

        if (_callPending)
            _invoke();

        _callPending = false;
    }

    /// <summary>Stops the engine permanently; subsequent calls to <see cref="Call" /> become no-ops.</summary>
    [MethodImpl(MethodImplOptions.Synchronized)]
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _windowOpen = false;
        _timer.Dispose();
    }
}
