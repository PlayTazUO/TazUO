#nullable enable

using System;

namespace ClassicUO.Game.ScreenDecorations.Triggers;

/// <summary>
/// What a trigger reports when it is firing.
/// <para>
/// This is the entire crosstalk surface between a trigger and the manager: a poll returns one of
/// these, an event carries the identical struct. The manager therefore reconciles one shape and
/// never branches on the kind of trigger that produced it.
/// </para>
/// <para>
/// Use <see cref="Default" /> rather than <c>default</c>. Only the parameterless constructor sets
/// <see cref="Intensity" /> to 1; <c>default(TriggerSignal)</c> bypasses it and leaves the
/// occurrence at zero strength - invisible rather than unscaled.
/// </para>
/// </summary>
public readonly record struct TriggerSignal
{
    /// <summary>
    /// Relative strength of this occurrence, 0-1. Scales the profile; it does not replace any
    /// authored value. Known only at the trigger - an earthquake's distance, a hit's damage.
    /// </summary>
    public float Intensity { get; init; } = 1f;

    /// <summary>
    /// How long this occurrence runs, or null if the trigger will report its own end. Comes from the
    /// trigger either because it knows inherently (a quake sound's length) or because it was
    /// parameterized with it (a chat match has no natural duration).
    /// </summary>
    public TimeSpan? Duration { get; init; }

    public TriggerSignal()
    {
    }

    /// <summary>The profile untouched: full strength, ended by the trigger itself.</summary>
    public static TriggerSignal Default => new();
}

/// <summary>How the manager has to watch a trigger.</summary>
public enum TriggerKind
{
    /// <summary>Sampled on the reconcile pass. Cheap and safe to call with no world loaded.</summary>
    Poll,

    /// <summary>Announces itself. Costs nothing until it fires.</summary>
    Event
}

/// <summary>Carries an event trigger's signal to the manager.</summary>
public sealed class TriggerFiredArgs : EventArgs
{
    /// <summary>What the occurrence is asking for.</summary>
    public TriggerSignal Signal { get; init; } = TriggerSignal.Default;
}
