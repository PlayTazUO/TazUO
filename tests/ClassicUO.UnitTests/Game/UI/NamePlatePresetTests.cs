using System;
using System.Linq;
using System.Text.Json;
using ClassicUO.Configuration;
using ClassicUO.Game.UI.Gumps;
using Xunit;

namespace ClassicUO.UnitTests.Game.UI;

public class NamePlatePresetTests
{
    [Fact]
    public void ExistingProfilesKeepTheirAppearanceUntilLegacyIsSelected()
    {
        Profile profile = JsonSerializer.Deserialize(
            "{\"name_plate_preset\":2,\"name_plate_opacity\":37,\"name_plate_health_bar_opacity\":61}",
            ProfileJsonContext.DefaultToUse.Profile);

        Assert.Equal(NamePlatePreset.WorldOfWarcraftBlockyBars, profile.NamePlatePreset);
        Assert.False(profile.NamePlateUseNotorietyText);
        Assert.True(profile.NamePlateShowMissingHealth);
        Assert.Equal(37, profile.NamePlateOpacity);
        Assert.Equal(61, profile.NamePlateHealthBarOpacity);
    }

    [Fact]
    public void LegacyRestoresAppearanceWithoutChangingVisibilityPreferences()
    {
        var profile = new Profile();
        NamePlatePresets.Apply(profile, NamePlatePreset.WorldOfWarcraftBlockyBars);
        profile.NamePlateHideAtFullHealth = true;
        profile.NamePlateHideAtFullHealthInWarmode = true;
        profile.NamePlateAvoidOverlap = true;

        NamePlatePresets.Apply(profile, NamePlatePreset.Legacy);

        Assert.Equal(NamePlatePreset.Legacy, profile.NamePlatePreset);
        Assert.True(profile.NamePlateUseNotorietyText);
        Assert.False(profile.NamePlateShowMissingHealth);
        Assert.Equal(NamePlateBackgroundMode.EntityNotorietyColor, profile.NamePlateBackgroundMode);
        Assert.Equal(NamePlateHealthBarMode.StatusColor, profile.NamePlateHealthBarMode);
        Assert.False(profile.NamePlateUseFixedWidth);
        Assert.False(profile.NamePlateUseFixedHealthBarWidth);
        Assert.False(profile.NamePlateSplitHealthBar);
        Assert.Equal(0, profile.NamePlateHeight);
        Assert.Equal(0, profile.NamePlateCornerRadius);
        Assert.Equal("avadonian", profile.NamePlateFont);
        Assert.Equal(20, profile.NamePlateFontSize);
        Assert.Equal(75, profile.NamePlateOpacity);
        Assert.Equal(50, profile.NamePlateHealthBarOpacity);
        Assert.Equal(50, profile.NamePlateBorderOpacity);
        Assert.True(profile.NamePlateHideAtFullHealth);
        Assert.True(profile.NamePlateHideAtFullHealthInWarmode);
        Assert.True(profile.NamePlateAvoidOverlap);
    }

    [Fact]
    public void SwitchingAwayFromLegacyRestoresEveryModernPreset()
    {
        foreach (NamePlatePreset preset in Enum.GetValues<NamePlatePreset>()
                     .Where(p => p is not NamePlatePreset.Custom and not NamePlatePreset.Legacy))
        {
            var direct = new Profile();
            NamePlatePresets.Apply(direct, preset);
            var fromLegacy = new Profile();
            NamePlatePresets.Apply(fromLegacy, NamePlatePreset.Legacy);
            NamePlatePresets.Apply(fromLegacy, preset);

            Assert.Equal(Serialize(direct), Serialize(fromLegacy));
        }
    }

    [Fact]
    public void CustomKeepsTweakedLegacySettingsAndSurvivesReload()
    {
        var profile = new Profile();
        NamePlatePresets.Apply(profile, NamePlatePreset.Legacy);
        profile.NamePlateHealthBarOpacity = 23;
        profile.NamePlateCornerRadius = 7;
        NamePlatePresets.SetCustom(profile);
        string before = Serialize(profile);

        NamePlatePresets.Apply(profile, NamePlatePreset.Custom);
        Assert.Equal(before, Serialize(profile));

        Profile restored = JsonSerializer.Deserialize(before, ProfileJsonContext.DefaultToUse.Profile);
        Assert.Equal(NamePlatePreset.Custom, restored.NamePlatePreset);
        Assert.True(restored.NamePlateUseNotorietyText);
        Assert.False(restored.NamePlateShowMissingHealth);
        Assert.Equal(NamePlateBackgroundMode.EntityNotorietyColor, restored.NamePlateBackgroundMode);
        Assert.Equal(23, restored.NamePlateHealthBarOpacity);
        Assert.Equal(7, restored.NamePlateCornerRadius);
    }

    private static string Serialize(Profile profile)
    {
        // Exercise the real profile serializer, but reload only nameplate settings:
        // unrelated options-font setters require an initialized game UI.
        using JsonDocument document = JsonDocument.Parse(JsonSerializer.Serialize(profile, ProfileJsonContext.DefaultToUse.Profile));
        return JsonSerializer.Serialize(document.RootElement.EnumerateObject()
            .Where(p => p.Name.StartsWith("name_plate_", StringComparison.Ordinal))
            .ToDictionary(p => p.Name, p => p.Value));
    }
}
