using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations;

namespace ClassicUO.Game.ScreenDecorations.Manager.Triggers;

/// <summary>
/// One reason for an overlay effect to be showing, and how it should look when that reason holds.
/// </summary>
/// <param name="Condition">Whether the reason holds right now. Polled on the reconcile pass, so it
/// must be cheap and safe to call with no player in the world.</param>
/// <param name="EffectSlot">The effect this asks for.</param>
/// <param name="Priority">Higher composites on top and survives the concurrency cap.</param>
/// <param name="OnsetTrauma">Screen shake to fire when it starts; zero for none.</param>
public readonly record struct EffectPollingTrigger(
    Func<bool> Condition,
    OverlayEffectSlot EffectSlot,
    int Priority,
    float OnsetTrauma
);

/// <summary>
/// Every trigger the client knows about, grouped by the effect it asks for. Fixed at startup: these
/// are code-defined, so the table is built once and handed out as read-only rather than rebuilt per
/// lookup - the reconcile pass asks for all six effects twice a second.
/// </summary>
public static class EffectTriggerRegistry
{
    private static readonly EffectPollingTrigger _playerPoisoned = new(
        () => World.Instance?.Player?.IsPoisoned ?? false,
        OverlayEffectSlot.Poison,
        1,
        0.3f
    );

    /// <summary>
    /// The table. An effect absent from it simply has no trigger yet and stays dormant unless
    /// previewed; add an entry here rather than anywhere in the manager.
    /// </summary>
    private static readonly FrozenDictionary<OverlayEffectSlot, ImmutableArray<EffectPollingTrigger>> _byEffect =
        new Dictionary<OverlayEffectSlot, ImmutableArray<EffectPollingTrigger>>
        {
            [OverlayEffectSlot.Poison] = [_playerPoisoned]
        }.ToFrozenDictionary();

    /// <summary>
    /// The triggers that can call for <paramref name="effectSlot" />.
    /// </summary>
    /// <param name="effectSlot">The effect to look up.</param>
    /// <returns>Its triggers, empty for an effect nothing triggers yet.</returns>
    public static ImmutableArray<EffectPollingTrigger> GetTriggersForEffect(OverlayEffectSlot effectSlot) =>
        _byEffect.TryGetValue(effectSlot, out ImmutableArray<EffectPollingTrigger> triggers) ? triggers : [];

    /// <summary>
    /// Builds the event-driven triggers shipped with the client. Fresh instances rather than shared
    /// ones: each hooks and unhooks its own event source over the manager's lifetime, which is state
    /// that should not outlive a registration. Add built-in event triggers here.
    /// </summary>
    /// <returns>The triggers, for the manager to register.</returns>
    public static ImmutableArray<IEffectEventTrigger> CreateEventTriggers() => [new EarthquakeTrigger()];
}
