using System;
using System.Collections.Generic;
using System.Reflection;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.Managers
{
    /// <summary>
    /// Tests for <see cref="ToolTipOverrideData"/> tooltip override resolution.
    ///
    /// These cover the two branches of <see cref="ToolTipOverrideData.ResolveTooltipText"/>:
    /// the "item" branch (a serial that <see cref="SerialHelper.IsItem"/> recognizes) and the
    /// "not item" branch (raw OPL text, e.g. vendor search results). Items shown in server-sent
    /// gumps are referenced by serial but often aren't real entries in world.Items, so they must
    /// still receive overrides via their raw OPL text - a case that was accidentally broken when
    /// a failed serial lookup started returning the raw, un-overridden text instead of trying the
    /// text based override.
    /// </summary>
    public class ToolTipOverrideTest : IDisposable
    {
        // Any serial in [0x40000000, 0x80000000) is treated as an item by SerialHelper.IsItem.
        private const uint ITEM_SERIAL = 0x40000001;

        // A distinctive token so we can prove the override was applied rather than the raw text shown.
        private const string OVERRIDE_FORMAT = "FIRERES={1}";
        private const string RAW_TOOLTIP = "Some Sword\nFire Resist 15";

        public ToolTipOverrideTest()
        {
            SetCurrentProfile(CreateProfileWithNoOverrides());
            SetTooltipOverrides(new ToolTipOverrideData(0, "Fire Resist", OVERRIDE_FORMAT, -1, 100, -1, 100, (byte)TooltipLayers.Any));
        }

        public void Dispose()
        {
            SetCurrentProfile(null);
            SetTooltipOverrides();
        }

        #region "Not item" branch (raw OPL text / vendor search)

        [Fact]
        public void ProcessTooltipText_StringOverload_AppliesOverride()
        {
            string result = ToolTipOverrideData.ProcessTooltipText(RAW_TOOLTIP);

            result.Should().Contain("FIRERES=15", "the configured override should rewrite the property line");
            result.Should().NotContain("Fire Resist 15", "the original property line should have been replaced");
        }

        [Fact]
        public void ResolveTooltipText_NonItemSerial_AppliesOverrideFromRawText()
        {
            var world = new World();

            // serial 0 is not an item, so resolution must come from the raw OPL text.
            string result = ToolTipOverrideData.ResolveTooltipText(world, 0, RAW_TOOLTIP);

            result.Should().Contain("FIRERES=15");

            world.Clear();
        }

        #endregion

        #region "Item" branch (item shown in a server gump, not a real world item)

        [Fact]
        public void ResolveTooltipText_ItemSerialNotInWorld_AppliesOverrideFromRawText()
        {
            var world = new World();

            // ITEM_SERIAL is a valid item serial (as sent in a server gump) but is NOT in world.Items,
            // exactly like an item shown in a server-sent gump. The override must still be applied.
            string result = ToolTipOverrideData.ResolveTooltipText(world, ITEM_SERIAL, RAW_TOOLTIP);

            result.Should().Contain("FIRERES=15");

            world.Clear();
        }

        [Fact]
        public void ResolveTooltipText_ItemSerialNotInWorld_DoesNotReturnRawUnprocessedText()
        {
            var world = new World();

            // Regression guard: when the serial lookup fails, resolution must fall through to the
            // text based override instead of returning the raw, un-overridden tooltip.
            string result = ToolTipOverrideData.ResolveTooltipText(world, ITEM_SERIAL, RAW_TOOLTIP);

            result.Should().NotBe(RAW_TOOLTIP);
            result.Should().NotContain("Fire Resist 15");

            world.Clear();
        }

        [Fact]
        public void ProcessTooltipText_ItemNotInWorld_ReturnsNull()
        {
            var world = new World();

            // Documents why the raw-text fallback in ResolveTooltipText is required: the serial based
            // lookup produces nothing for an item that isn't in world.Items.
            string result = ToolTipOverrideData.ProcessTooltipText(world, ITEM_SERIAL);

            result.Should().BeNull();

            world.Clear();
        }

        #endregion

        #region Fallback / no-override behaviour

        [Fact]
        public void ResolveTooltipText_NoOverrideConfigured_KeepsPropertyText()
        {
            SetCurrentProfile(CreateProfileWithNoOverrides());
            SetTooltipOverrides();
            var world = new World();

            string result = ToolTipOverrideData.ResolveTooltipText(world, ITEM_SERIAL, RAW_TOOLTIP);

            // With no overrides, the property line survives unchanged (no override token added).
            result.Should().Contain("Fire Resist 15");
            result.Should().NotContain("FIRERES=");

            world.Clear();
        }

        [Fact]
        public void ResolveTooltipText_EmptyRawTextAndNoWorldItem_ReturnsRawText()
        {
            var world = new World();

            string result = ToolTipOverrideData.ResolveTooltipText(world, ITEM_SERIAL, string.Empty);

            result.Should().BeEmpty();

            world.Clear();
        }

        #endregion

        #region Helpers

        private static Profile CreateProfileWithNoOverrides()
        {
            return new Profile
            {
                // Skip grid-highlight matching so the test doesn't depend on the highlight config/db.
                GridHighlightProperties = false,
                GridHighlightShowRuleName = false,
            };
        }

        private static void SetCurrentProfile(Profile profile)
        {
            PropertyInfo prop = typeof(ProfileManager).GetProperty(
                nameof(ProfileManager.CurrentProfile),
                BindingFlags.Public | BindingFlags.Static);

            prop.SetValue(null, profile);
        }

        /// <summary>
        /// Replaces the cached <see cref="TooltipOverridesConfig"/> with one holding exactly the given
        /// overrides. Tooltip overrides now live in tooltip_overrides.json (via TooltipOverridesConfig)
        /// rather than the profile, so tests seed the config directly instead of the profile's lists.
        /// </summary>
        private static void SetTooltipOverrides(params ToolTipOverrideData[] overrides)
        {
            var config = new TooltipOverridesConfig { Overrides = new List<ToolTipOverrideData>(overrides) };

            FieldInfo field = typeof(TooltipOverridesConfig).GetField(
                "_current",
                BindingFlags.NonPublic | BindingFlags.Static);

            field.SetValue(null, config);
        }

        #endregion
    }
}
