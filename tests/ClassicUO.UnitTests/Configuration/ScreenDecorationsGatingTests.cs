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

            foreach (OverlayEffectSlot effect in OverlaySystemSettings.AllEffects)
                settings.Overlays.GetSettings(effect).Enabled.Should().BeFalse();
        }

        /// <summary>Both systems stay inside the game world unless asked otherwise, so neither can
        /// obscure or displace the UI by default.</summary>
        [Fact]
        public void NeitherSystemCoversTheWindowByDefault()
        {
            var settings = new ScreenDecorations();

            settings.Shake.FullScreen.Should().BeFalse();

            foreach (OverlayEffectSlot effect in OverlaySystemSettings.AllEffects)
                settings.Overlays.GetSettings(effect).FullScreen.Should().BeFalse();
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
}
