using System;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations;
using ClassicUO.Game.ScreenDecorations.Overlays;

namespace ClassicUO.Game.ScreenDecorations.Manager.Triggers;

/// <summary>
/// Carries what an occurrence asks for when it fires.
/// </summary>
/// <param name="modulation">How this instance departs from the effect's authored profile.</param>
public sealed class EffectActivationArgs(OverlayModulation modulation) : EventArgs
{
    /// <summary>How this instance departs from the effect's authored profile.</summary>
    public OverlayModulation Modulation { get; } = modulation;
}

/// <summary>
/// An effect driven by something happening rather than by a state that can be sampled. The counter
/// part to <see cref="EffectPollingTrigger" />: the manager cannot ask these whether they hold, so
/// they announce it.
/// <para>
/// Two shapes, distinguished by <see cref="HeadlessDuration" />. A stateful trigger raises
/// <see cref="Activated" /> when its condition begins and <see cref="Deactivated" /> when it ends. A
/// headless one has no end to report - an earthquake is over when it is over - and instead declares
/// how long one occurrence lasts, after which the manager retires it.
/// </para>
/// </summary>
public interface IEffectEventTrigger
{
    /// <summary>Raised when the occurrence begins. Re-raising while already active restates the
    /// modulation and, for a headless trigger, restarts its clock.</summary>
    event EventHandler<EffectActivationArgs> Activated;

    /// <summary>Raised when the occurrence ends. Never raised by a headless trigger.</summary>
    event EventHandler Deactivated;

    /// <summary>The effect this asks for.</summary>
    OverlayEffectSlot EffectSlot { get; }

    /// <summary>Higher composites on top and survives the concurrency cap.</summary>
    int Priority { get; }

    /// <summary>How long one activation lasts with no second event to end it, or null for a
    /// trigger that reports its own <see cref="Deactivated" />.</summary>
    TimeSpan? HeadlessDuration { get; }

    /// <summary>
    /// Attaches to whatever this watches. Called by the manager on entering the world, so a trigger
    /// costs nothing at the login screen; never called twice without an intervening
    /// <see cref="UnRegister" />.
    /// </summary>
    void Register();

    /// <summary>Detaches from whatever <see cref="Register" /> attached to.</summary>
    void UnRegister();
}
