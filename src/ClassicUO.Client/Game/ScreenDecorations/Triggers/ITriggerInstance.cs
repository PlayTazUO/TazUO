#nullable enable

using System;

namespace ClassicUO.Game.ScreenDecorations.Triggers;

/// <summary>
/// A built trigger, watching on one rule's behalf. One instance per rule, since each carries its own
/// parameters.
/// </summary>
public interface ITriggerInstance : IDisposable
{
    /// <summary>
    /// Begins watching. Called on entering the world, so a trigger costs nothing at the login
    /// screen; never called twice without an intervening <see cref="Detach" />.
    /// </summary>
    void Attach();

    /// <summary>Stops watching, releasing whatever <see cref="Attach" /> hooked.</summary>
    void Detach();
}

/// <summary>Sampled. Returns its signal when it holds, null when it does not.</summary>
public interface IPollingTrigger : ITriggerInstance
{
    /// <summary>
    /// Reads the state this trigger watches. Called on the reconcile pass, on the main thread, so it
    /// must be cheap and safe to call with no player in the world.
    /// </summary>
    /// <returns>The occurrence's signal, or null if the trigger is not firing.</returns>
    TriggerSignal? Sample();
}

/// <summary>
/// Announces itself. <see cref="Fired" /> carries the same payload a poll would return, so the
/// manager reconciles both kinds through one path.
/// </summary>
public interface IEventTrigger : ITriggerInstance
{
    /// <summary>Raised when an occurrence begins. Re-raising while one is already up restates its
    /// signal and, for a trigger that supplies a duration, restarts the clock.</summary>
    event EventHandler<TriggerFiredArgs> Fired;

    /// <summary>
    /// Raised only by a trigger that knows its own end. One that does not leaves
    /// <see cref="TriggerSignal.Duration" /> set instead, and the manager retires it.
    /// </summary>
    event EventHandler Ended;
}
