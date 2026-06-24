using ClassicUO.Game.Data;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests;

public class HotKeyActionTests
{
    [Fact]
    public void DisplayName_for_consumable_uses_label()
    {
        var a = new HotKeyAction { Type = HotKeyActionType.Consumable, ConsumableLabel = "Heal Potion" };
        a.DisplayName(null).Should().Be("Heal Potion");
    }

    [Fact]
    public void DisplayName_for_ability_reports_primary_or_secondary()
    {
        new HotKeyAction { Type = HotKeyActionType.Ability, AbilityPrimary = true }.DisplayName(null).Should().Be("Primary Ability");
        new HotKeyAction { Type = HotKeyActionType.Ability, AbilityPrimary = false }.DisplayName(null).Should().Be("Secondary Ability");
    }

    [Fact]
    public void DisplayName_for_toggle_hotkeys()
    {
        new HotKeyAction { Type = HotKeyActionType.ToggleHotkeys }.DisplayName(null).Should().Be("Toggle Hotkeys");
    }
}
