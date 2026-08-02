using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Configuration
{
    /// <summary>
    /// The master switch and the two system switches, which every consumer gates on.
    /// </summary>
    public class ScreenDecorationsGatingTests
    {
        [Fact]
        public void BothSystemsRunByDefault()
        {
            var settings = new ScreenDecorations();

            settings.OverlaysActive.Should().BeTrue();
            settings.ShakeActive.Should().BeTrue();
        }

        [Fact]
        public void MasterSwitchStopsBothSystems()
        {
            var settings = new ScreenDecorations { Enabled = false };

            settings.OverlaysActive.Should().BeFalse();
            settings.ShakeActive.Should().BeFalse();
        }

        [Fact]
        public void SystemSwitchesAreIndependent()
        {
            var settings = new ScreenDecorations();
            settings.Shake.Enabled = false;

            settings.OverlaysActive.Should().BeTrue();
            settings.ShakeActive.Should().BeFalse();

            settings.Shake.Enabled = true;
            settings.Overlays.Enabled = false;

            settings.OverlaysActive.Should().BeFalse();
            settings.ShakeActive.Should().BeTrue();
        }
    }
}
