#nullable enable

using System;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Profiles;
using ClassicUO.Game.ScreenDecorations.Triggers;

namespace ClassicUO.Game.ScreenDecorations.Rules;

/// <summary>
/// What asked for an overlay. Each source is capped on its own, so a script holding four effects
/// cannot push the player's own state off the screen and the reverse cannot happen either.
/// </summary>
public enum DemandSource
{
    /// <summary>A rule in the rulebase, firing.</summary>
    Rule,

    /// <summary>The options previewing a look. Singular by construction, so it needs no cap.</summary>
    Preview,

    /// <summary>A Python script, via the scripting API.</summary>
    Script
}

/// <summary>
/// What the manager resolves a firing rule into, and what the compositor is handed. The one place
/// trigger-supplied values and profile-authored ones meet: everything above this is either wiring or
/// authoring, and everything below it is drawing.
/// </summary>
/// <param name="RuleId">The rule that is firing, and the compositor slot it occupies.</param>
/// <param name="Profile">The look it raises.</param>
/// <param name="Priority">Higher composites on top and survives the concurrency cap.</param>
/// <param name="Signal">What the trigger reported for this occurrence.</param>
/// <param name="Source">Which partition of the concurrency cap it is bounded by.</param>
public readonly record struct RuleDemand(
    Guid RuleId,
    EffectProfile Profile,
    int Priority,
    TriggerSignal Signal,
    DemandSource Source = DemandSource.Rule
);
