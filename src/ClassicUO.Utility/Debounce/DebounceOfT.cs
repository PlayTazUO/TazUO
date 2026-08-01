using System;

namespace ClassicUO.Utility.Debounce;

/// <summary>
///     Lodash-style debounce for a single-argument action. The most recent argument passed to
///     <see cref="Invoke" /> is the one used when the action finally fires. See
///     https://lodash.info/doc/debounce
/// </summary>
/// <typeparam name="T">The type of argument passed through to the wrapped action.</typeparam>
public sealed class Debounce<T> : IDisposable
{
    private readonly DebounceEngine _engine;
    private T _arg;

    /// <summary>
    ///     Creates a debounced wrapper around <paramref name="action" />.
    /// </summary>
    /// <param name="action">The action to invoke once debouncing settles.</param>
    /// <param name="waitMs">Milliseconds of silence required before <paramref name="action" /> fires.</param>
    /// <param name="leading">
    ///     If <see langword="true" />, <paramref name="action" /> also fires immediately on the first
    ///     <see cref="Invoke" /> of a burst.
    /// </param>
    /// <param name="trailing">
    ///     If <see langword="true" />, <paramref name="action" /> fires after the burst goes quiet for
    ///     <paramref name="waitMs" />. At least one of <paramref name="leading" />/<paramref name="trailing" />
    ///     always applies; if both are <see langword="false" /> this behaves as if <paramref name="trailing" />
    ///     were <see langword="true" />.
    /// </param>
    /// <param name="maxWaitMs">
    ///     The maximum time <paramref name="action" /> may be delayed while <see cref="Invoke" /> keeps being
    ///     called. <see langword="null" /> (the default) means the action can be delayed indefinitely as long
    ///     as calls keep arriving.
    /// </param>
    public Debounce(Action<T> action, int waitMs, bool leading = false, bool trailing = true, int? maxWaitMs = null)
    {
        ArgumentNullException.ThrowIfNull(action);

        _engine = new DebounceEngine(() => action(_arg), waitMs, leading, trailing, maxWaitMs);
    }

    /// <summary>Records <paramref name="arg" /> as the latest argument and schedules (or reschedules) an invocation.</summary>
    public void Invoke(T arg)
    {
        _arg = arg;
        _engine.Call();
    }

    /// <summary>Cancels any pending invocation without firing it.</summary>
    public void Cancel() => _engine.Cancel();

    /// <summary>Immediately invokes a pending call, if any, and closes the debounce window.</summary>
    public void Flush() => _engine.Flush();

    /// <summary>Stops the debounce permanently; further calls to <see cref="Invoke" /> become no-ops.</summary>
    public void Dispose() => _engine.Dispose();
}
