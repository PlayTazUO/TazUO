using System;

namespace ClassicUO.Utility.Debounce;

/// <summary>
///     Lodash-style debounce for a parameterless action. Delays invoking the wrapped action until
///     <see cref="Invoke" /> hasn't been called for the configured wait time. See
///     https://lodash.info/doc/debounce
/// </summary>
public sealed class Debounce : IDisposable
{
    private readonly DebounceEngine _engine;

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
    public Debounce(Action action, int waitMs, bool leading = false, bool trailing = true, int? maxWaitMs = null)
    {
        _engine = new DebounceEngine(action, waitMs, leading, trailing, maxWaitMs);
    }

    /// <summary>Schedules (or reschedules) an invocation.</summary>
    public void Invoke() => _engine.Call();

    /// <summary>Cancels any pending invocation without firing it.</summary>
    public void Cancel() => _engine.Cancel();

    /// <summary>Immediately invokes a pending call, if any, and closes the debounce window.</summary>
    public void Flush() => _engine.Flush();

    /// <summary>Stops the debounce permanently; further calls to <see cref="Invoke" /> become no-ops.</summary>
    public void Dispose() => _engine.Dispose();
}
