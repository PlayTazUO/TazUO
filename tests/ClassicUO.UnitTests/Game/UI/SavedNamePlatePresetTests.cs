using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using ClassicUO.Configuration;
using ClassicUO.Game.UI.Gumps;
using Xunit;

namespace ClassicUO.UnitTests.Game.UI;

public class SavedNamePlatePresetTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "nameplate-presets-" + Guid.NewGuid().ToString("N"));
    private NamePlatePresetStore Store => new(_directory);

    [Fact]
    public void SnapshotCoversAllNameplateSettingsAndRestoresThemAfterReload()
    {
        var profile = new Profile();
        PropertyInfo[] settings = typeof(Profile).GetProperties().Where(p =>
            (p.Name.StartsWith("NamePlate", StringComparison.Ordinal) &&
             p.Name is not nameof(Profile.NamePlatePreset) and not nameof(Profile.NamePlateSavedPresetName)) ||
            p.Name is nameof(Profile.ShowNewMobileNameIncoming) or nameof(Profile.ShowNewCorpseNameIncoming)).ToArray();

        foreach (PropertyInfo property in settings)
        {
            Assert.NotNull(typeof(SavedNamePlatePreset).GetProperty(property.Name));
            object value = property.PropertyType == typeof(bool) ? !(bool)property.GetValue(profile)
                : property.PropertyType == typeof(byte) ? (byte)43
                : property.PropertyType == typeof(int) ? 29
                : property.PropertyType == typeof(string) ? "test font"
                : Enum.GetValues(property.PropertyType).GetValue(1);
            property.SetValue(profile, value);
        }

        SavedNamePlatePreset snapshot = SavedNamePlatePreset.Capture(profile);
        Assert.Equal(SaveNamePlatePresetResult.Saved, Store.Save(snapshot, "  My preset  ", [], out var saved));
        Assert.Equal("My preset", saved.Name);
        SavedNamePlatePreset loaded = Assert.Single(new NamePlatePresetStore(_directory).Load());
        Assert.Equal(saved, loaded);

        var otherCharacter = new Profile { SpeechHue = 123 };
        loaded.ApplyTo(otherCharacter);
        foreach (PropertyInfo property in settings)
            Assert.Equal(property.GetValue(profile), property.GetValue(otherCharacter));
        Assert.Equal(123, otherCharacter.SpeechHue);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("bad\nname")]
    public void InvalidNamesDoNotCreatePresets(string name)
    {
        Assert.Equal(SaveNamePlatePresetResult.InvalidName, Store.Save(SavedNamePlatePreset.Capture(new Profile()), name, [], out var saved));
        Assert.Null(saved);
        Assert.Empty(Store.Load());
        Assert.False(Directory.Exists(_directory));
    }

    [Fact]
    public void TooLongNamesAreRejected()
    {
        Assert.Equal(SaveNamePlatePresetResult.InvalidName,
            Store.Save(SavedNamePlatePreset.Capture(new Profile()), new string('a', 61), [], out _));
        Assert.Empty(Store.Load());
    }

    [Fact]
    public void DuplicateAndBuiltInNamesDoNotOverwriteExistingPresets()
    {
        var profile = new Profile { NamePlateOpacity = 23 };
        Assert.Equal(SaveNamePlatePresetResult.Saved, Store.Save(SavedNamePlatePreset.Capture(profile), "PvP", [], out _));
        profile.NamePlateOpacity = 91;
        Assert.Equal(SaveNamePlatePresetResult.DuplicateName, Store.Save(SavedNamePlatePreset.Capture(profile), "  pvp  ", [], out _));
        Assert.Equal(SaveNamePlatePresetResult.DuplicateName, Store.Save(SavedNamePlatePreset.Capture(profile), " legacy ", ["Legacy"], out _));
        Assert.Equal(23, Assert.Single(Store.Load()).NamePlateOpacity);
        Assert.Single(Directory.GetFiles(_directory));
    }

    [Fact]
    public void EditingCurrentSettingsDoesNotMutateTheSavedSnapshot()
    {
        var profile = new Profile { NamePlateOpacity = 23 };
        var snapshot = SavedNamePlatePreset.Capture(profile);
        profile.NamePlateOpacity = 91;
        Assert.Equal(SaveNamePlatePresetResult.Saved, Store.Save(snapshot, "PvP", [], out var saved));
        NamePlatePresets.SelectSaved(profile, saved);
        NamePlatePresets.SetCustom(profile);
        Assert.Empty(profile.NamePlateSavedPresetName);
        Assert.Equal(23, Assert.Single(Store.Load()).NamePlateOpacity);
    }

    [Fact]
    public void SavedSelectionSurvivesReloadAndSwitchesBackToBuiltIns()
    {
        var profile = new Profile { NamePlateOpacity = 23 };
        Store.Save(SavedNamePlatePreset.Capture(profile), "PvP", [], out var saved);
        var entries = NamePlatePresets.GetEntries(Store.Load());
        int savedIndex = entries.Count - 1;
        profile.NamePlateOpacity = 91;
        NamePlatePresets.Apply(profile, entries[savedIndex]);
        Assert.Equal(23, profile.NamePlateOpacity);
        Assert.Equal(savedIndex, NamePlatePresets.GetSelectedIndex(profile, entries));

        Profile restored = JsonSerializer.Deserialize("{\"name_plate_preset\":0,\"name_plate_saved_preset_name\":\"PvP\"}",
            ProfileJsonContext.DefaultToUse.Profile);
        Assert.Equal(savedIndex, NamePlatePresets.GetSelectedIndex(restored, entries));

        NamePlatePresets.Apply(profile, NamePlatePreset.Legacy);
        Assert.Empty(profile.NamePlateSavedPresetName);
        Assert.Equal((int)NamePlatePreset.Legacy, NamePlatePresets.GetSelectedIndex(profile, entries));
        Assert.Equal(saved, Assert.Single(Store.Load()));
    }

    [Fact]
    public void MissingSavedPresetFallsBackToCustomWithoutChangingSettings()
    {
        var profile = new Profile { NamePlateSavedPresetName = "Missing", NamePlateOpacity = 23 };
        Assert.Equal(0, NamePlatePresets.GetSelectedIndex(profile, NamePlatePresets.GetEntries([])));
        Assert.Equal(23, profile.NamePlateOpacity);
    }

    [Fact]
    public void EachSavedPresetRestoresItsOwnSettingsAfterSwitching()
    {
        var profile = new Profile { NamePlateOpacity = 23 };
        Store.Save(SavedNamePlatePreset.Capture(profile), "PvP", [], out _);
        profile.NamePlateOpacity = 91;
        Store.Save(SavedNamePlatePreset.Capture(profile), "PvM", [], out _);
        var entries = NamePlatePresets.GetEntries(Store.Load());
        NamePlatePresets.Entry pvp = Assert.Single(entries.Where(p => p.Name == "PvP"));
        NamePlatePresets.Entry pvm = Assert.Single(entries.Where(p => p.Name == "PvM"));

        NamePlatePresets.Apply(profile, pvp);
        Assert.Equal(23, profile.NamePlateOpacity);
        Assert.Equal("PvP", profile.NamePlateSavedPresetName);
        NamePlatePresets.Apply(profile, pvm);
        Assert.Equal(91, profile.NamePlateOpacity);
        Assert.Equal("PvM", profile.NamePlateSavedPresetName);
        NamePlatePresets.SetCustom(profile);
        Assert.Empty(profile.NamePlateSavedPresetName);
        Assert.Equal(2, Store.Load().Count);
    }

    [Fact]
    public void NamesAreDataAndCannotEscapeThePresetFolder()
    {
        Assert.Equal(SaveNamePlatePresetResult.Saved, Store.Save(SavedNamePlatePreset.Capture(new Profile()), "../my preset", [], out _));
        Assert.Equal("../my preset", Assert.Single(Store.Load()).Name);
        Assert.Single(Directory.GetFiles(_directory, "*.json"));
        Assert.Empty(Directory.GetDirectories(_directory));
    }

    [Fact]
    public void UnreadablePresetDoesNotHideValidPresets()
    {
        Store.Save(SavedNamePlatePreset.Capture(new Profile()), "Valid", [], out _);
        File.WriteAllText(Path.Combine(_directory, "broken.json"), "{ invalid");
        Assert.Equal("Valid", Assert.Single(Store.Load()).Name);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }
}
