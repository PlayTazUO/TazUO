#nullable enable

using System;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Profiles;
using ClassicUO.Game.ScreenDecorations.Triggers;

namespace ClassicUO.Game.ScreenDecorations.Rules;

/// <summary>
/// What the manager resolves a firing rule into, and what the compositor is handed. The one place
/// trigger-supplied values and profile-authored ones meet: everything above this is either wiring or
/// authoring, and everything below it is drawing.
/// </summary>
/// <param name="RuleId">The rule that is firing, and the compositor slot it occupies.</param>
/// <param name="Profile">The look it raises.</param>
/// <param name="Priority">Higher composites on top and survives the concurrency cap.</param>
/// <param name="Signal">What the trigger reported for this occurrence.</param>
public readonly record struct RuleDemand(
    Guid RuleId,
    EffectProfile Profile,
    int Priority,
    TriggerSignal Signal
);
