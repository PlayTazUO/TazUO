using System.Linq;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Configuration;

/// <summary>
/// The master switch and the two system switches, which every consumer gates on.
/// </summary>
public class ScreenDecorationsGatingTests
{
    /// <summary>
    /// Every switch is opt-in. These effects obscure and displace the world, so an unconfigured
    /// profile must not start distorting anyone's screen on its own.
    /// </summary>
    [Fact]
    public void NothingRunsByDefault()
    {
        var settings = new ScreenDecorations();

        settings.Enabled.Should().BeFalse();
        settings.OverlaysActive.Should().BeFalse();
        settings.ShakeActive.Should().BeFalse();

        // Including the shipped rules: a clean profile must not start distorting anyone's screen
        // just because the client knows how to.
        settings.Overlays.ResolveRules().Should().OnlyContain(rule => !rule.Enabled);
    }

    /// <summary>Both systems stay inside the game world unless asked otherwise, so neither can
    /// obscure or displace the UI by default.</summary>
    [Fact]
    public void NeitherSystemCoversTheWindowByDefault()
    {
        var settings = new ScreenDecorations();

        // One switch per look governs both halves of its scope: which pass draws it, and which
        // rectangle its shake displaces.
        settings.Overlays.AllProfiles().Should().OnlyContain(profile => !profile.FullScreen);
    }

    [Fact]
    public void MasterSwitchStopsBothSystems()
    {
        var settings = AllOn();
        settings.Enabled = false;

        settings.OverlaysActive.Should().BeFalse();
        settings.ShakeActive.Should().BeFalse();
    }

    [Fact]
    public void SystemSwitchesAreIndependent()
    {
        ScreenDecorations settings = AllOn();
        settings.Shake.Enabled = false;

        settings.OverlaysActive.Should().BeTrue();
        settings.ShakeActive.Should().BeFalse();

        settings.Shake.Enabled = true;
        settings.Overlays.Enabled = false;

        settings.OverlaysActive.Should().BeFalse();
        settings.ShakeActive.Should().BeTrue();
    }

    private static ScreenDecorations AllOn()
    {
        var settings = new ScreenDecorations { Enabled = true };

        settings.Overlays.Enabled = true;
        settings.Shake.Enabled = true;

        return settings;
    }
}
