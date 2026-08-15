#nullable enable

using System;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Profiles;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Rules;
using ClassicUO.Game.ScreenDecorations.Triggers;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.ScreenDecorations.Manager;

/// <summary>
/// One rule's live wiring, and the latest thing its trigger said.
/// <para>
/// Mutable and long-lived - a signal outlives the events that change it - so a class rather than a
/// struct. It also owns the subscription lifetime, because hooking an event source and remembering
/// the handler well enough to unhook it again are the same responsibility, and getting the second
/// half wrong leaks for the rest of the session.
/// </para>
/// <para>
/// Main thread only, save for the raw trigger events, which the manager marshals before they reach
/// <see cref="Raise" />.
/// </para>
/// </summary>
internal sealed class WatchedRule : IDisposable
{
    #region Public accessors

    /// <summary>The rule as it stood when the wiring was built.</summary>
    public OverlayRule Rule { get; }

    /// <summary>The look it raises.</summary>
    public EffectProfile Profile { get; }

    /// <summary>Whether the manager has to sample this or wait to be told.</summary>
    public TriggerKind Kind { get; }

    /// <summary>When the current occurrence lapses, or null if there is none or the trigger ends
    /// itself.</summary>
    public DateTime? ExpiresAt { get; private set; }

    #endregion

    #region Private members

    private readonly ITriggerInstance _trigger;

    /// <summary>Held so the subscription can be removed again; an anonymous handler cannot be.</summary>
    private EventHandler<TriggerFiredArgs>? _fired;

    /// <summary>Held for the same reason as <see cref="_fired" />.</summary>
    private EventHandler? _finished;

    private bool _attached;

    /// <summary>Whether an event-driven occurrence is currently being asserted.</summary>
    private bool _active;

    /// <summary>What the most recent occurrence asked for.</summary>
    private TriggerSignal _signal = TriggerSignal.Default;

    #endregion

    #region Ctor

    /// <summary>
    /// Wires one rule to a freshly built trigger. The trigger watches nothing until
    /// <see cref="Attach" />.
    /// </summary>
    /// <param name="rule">The rule this serves.</param>
    /// <param name="profile">The look it raises.</param>
    /// <param name="trigger">The instance built for it, owned from here on.</param>
    /// <param name="kind">How the trigger reports.</param>
    public WatchedRule(OverlayRule rule, EffectProfile profile, ITriggerInstance trigger, TriggerKind kind)
    {
        Rule = rule;
        Profile = profile;
        Kind = kind;
        _trigger = trigger;
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Subscribes and lets the trigger hook its own source. Wrapped, because that is arbitrary code
    /// and one trigger failing must not stop the rest - or the world - from coming up.
    /// </summary>
    /// <param name="onFired">Called when an occurrence begins or is restated.</param>
    /// <param name="onEnded">Called when a stateful trigger reports its own end.</param>
    public void Attach(Action<WatchedRule, TriggerSignal> onFired, Action<WatchedRule> onEnded)
    {
        if (_attached)
            return;

        // Set before anything is hooked, not after: Detach() is a no-op while this is false, so a
        // throw part-way through would leave the subscriptions below in place with nothing able to
        // remove them again.
        _attached = true;

        try
        {
            if (_trigger is IEventTrigger events)
            {
                _fired = (_, args) => onFired(this, args.Signal);
                _finished = (_, _) => onEnded(this);

                events.Fired += _fired;
                events.Ended += _finished;
            }

            _trigger.Attach();
        }
        catch (Exception e)
        {
            Log.Error($"Overlay trigger for rule '{Rule.Name}' failed to attach: {e}");
        }
    }

    /// <summary>Unhooks everything <see cref="Attach" /> hooked. Wrapped for the same reason, and
    /// more so: failing here leaks a subscription.</summary>
    public void Detach()
    {
        if (!_attached)
            return;

        try
        {
            if (_trigger is IEventTrigger events)
            {
                if (_fired != null)
                    events.Fired -= _fired;

                if (_finished != null)
                    events.Ended -= _finished;
            }

            _trigger.Detach();
        }
        catch (Exception e)
        {
            Log.Error($"Overlay trigger for rule '{Rule.Name}' failed to detach: {e}");
        }
        finally
        {
            _attached = false;
            _fired = null;
            _finished = null;
        }
    }

    /// <summary>
    /// What this rule is asserting right now. A polling rule is asked; an event-driven one has
    /// already said, so the manager reads both through one call and never branches on kind.
    /// </summary>
    /// <returns>Its signal, or null if it is not firing.</returns>
    public TriggerSignal? Sample()
    {
        if (_trigger is not IPollingTrigger polling)
            return _active ? _signal : null;

        try
        {
            return polling.Sample();
        }
        catch (Exception e)
        {
            // Conditions reach into live game state; one throwing must not take down the pass and
            // leave every other rule unreconciled.
            Log.Warn($"Overlay rule '{Rule.Name}' failed to sample its trigger: {e}");
            return null;
        }
    }

    /// <summary>
    /// Raises the signal. Re-raising one already up restates it and, where the trigger supplies a
    /// duration, restarts the clock - a second quake landing inside the first extends it rather than
    /// being swallowed.
    /// </summary>
    /// <param name="signal">What the trigger reported.</param>
    public void Raise(TriggerSignal signal)
    {
        _active = true;
        _signal = signal;
        ExpiresAt = signal.Duration is { } duration ? DateTime.UtcNow + duration : null;
    }

    /// <summary>Drops the signal, whichever shape it was.</summary>
    public void ClearSignal()
    {
        _active = false;
        ExpiresAt = null;
    }

    /// <summary>
    /// Retires an occurrence whose declared span has run out. It has no second event to end it.
    /// </summary>
    /// <param name="now">The instant to judge against.</param>
    public void ExpireIfLapsed(DateTime now)
    {
        // Null never compares true, which is what leaves stateful signals alone.
        if (_active && ExpiresAt <= now)
            ClearSignal();
    }

    /// <summary>Releases the trigger. <see cref="Detach" /> first - this does not unhook.</summary>
    public void Dispose()
    {
        ClearSignal();

        try
        {
            _trigger.Dispose();
        }
        catch (Exception e)
        {
            Log.Error($"Overlay trigger for rule '{Rule.Name}' failed to dispose: {e}");
        }
    }

    #endregion
}
