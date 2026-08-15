using System;
using ClassicUO.Game.ScreenDecorations.Overlays;
using ClassicUO.Game.ScreenDecorations.Overlays.Presets;
using Xunit;

namespace ClassicUO.UnitTests.Game.ScreenDecorations;

/// <summary>
/// Names a shipped preset without carrying an instance. Theory rows have to be serializable for
/// xUnit to enumerate them individually, and the preset types are not - so the rows carry this and
/// resolve the instance inside the test.
/// </summary>
public enum ShippedPreset
{
    Poison,
    Bleed,
    Fog,
    Drunk,
    Concussion,
    TunnelVision,
    Death
}

/// <summary>The one definition of "every preset the client ships", which the built-in profiles are
/// seeded from.</summary>
public static class ShippedPresetCatalog
{
    /// <summary>A fresh instance per call - presets are mutable, and a shared one would carry
    /// whatever the previous theory row tuned into the next.</summary>
    public static ScreenOverlayPreset Create(ShippedPreset preset)
    {
        return preset switch
        {
            ShippedPreset.Poison => new PoisonOverlay(),
            ShippedPreset.Bleed => new BleedOverlay(),
            ShippedPreset.Fog => new FogOverlay(),
            ShippedPreset.Drunk => new DrunkOverlay(),
            ShippedPreset.Concussion => new ConcussionOverlay(),
            ShippedPreset.TunnelVision => new TunnelVisionOverlay(),
            ShippedPreset.Death => new DeathOverlay(),
            _ => throw new ArgumentOutOfRangeException(nameof(preset))
        };
    }

    /// <summary>The built-in profile seeded from <paramref name="preset"/>.</summary>
    public static Guid ProfileId(ShippedPreset preset)
    {
        return preset switch
        {
            ShippedPreset.Poison => BuiltInProfiles.Ids.Poison,
            ShippedPreset.Bleed => BuiltInProfiles.Ids.Bleed,
            ShippedPreset.Fog => BuiltInProfiles.Ids.Fog,
            ShippedPreset.Drunk => BuiltInProfiles.Ids.Drunk,
            ShippedPreset.Concussion => BuiltInProfiles.Ids.Concussion,
            ShippedPreset.TunnelVision => BuiltInProfiles.Ids.TunnelVision,
            ShippedPreset.Death => BuiltInProfiles.Ids.Death,
            _ => throw new ArgumentOutOfRangeException(nameof(preset))
        };
    }

    /// <summary>Every shipped preset, as theory rows.</summary>
    public static TheoryData<ShippedPreset> All()
    {
        var presets = new TheoryData<ShippedPreset>();
        presets.AddRange(Enum.GetValues<ShippedPreset>());

        return presets;
    }

    /// <summary>The named subset, as theory rows.</summary>
    public static TheoryData<ShippedPreset> Only(params ShippedPreset[] presets)
    {
        var rows = new TheoryData<ShippedPreset>();
        rows.AddRange(presets);

        return rows;
    }
}
