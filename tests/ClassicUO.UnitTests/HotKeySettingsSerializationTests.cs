using System.Collections.Generic;
using System.Text.Json;
using ClassicUO.Game.Data;
using FluentAssertions;
using SDL3;
using Xunit;

namespace ClassicUO.UnitTests;

public class HotKeySettingsSerializationTests
{
    [Fact]
    public void Roundtrips_all_trigger_kinds_and_action_fields()
    {
        var settings = new HotKeySettings
        {
            Entries = new List<HotKeyEntry>
            {
                new() { Trigger = new HotKeyTrigger { Kind = HotKeyTriggerKind.Keyboard, Key = (int)SDL.SDL_Keycode.SDLK_F1, Mod = (int)SDL.SDL_Keymod.SDL_KMOD_CTRL },
                        Action = new HotKeyAction { Type = HotKeyActionType.Spell, SpellId = 1 } },
                new() { Trigger = new HotKeyTrigger { Kind = HotKeyTriggerKind.MouseButton, Button = 4 },
                        Action = new HotKeyAction { Type = HotKeyActionType.Script, ScriptPath = "group/farm.py" } },
                new() { Trigger = new HotKeyTrigger { Kind = HotKeyTriggerKind.MouseWheel, WheelUp = true },
                        Action = new HotKeyAction { Type = HotKeyActionType.Consumable, ConsumableGraphic = 0x0F0C, ConsumableHue = -1, ConsumableLabel = "Heal" } },
            },
            CustomConsumables = new List<CustomConsumable>
            {
                new() { Label = "My Apple", Graphic = 0x2FD8, Hue = 0 },
            },
        };

        string json = JsonSerializer.Serialize(settings, HotKeySettingsContext.Default.HotKeySettings);
        var back = JsonSerializer.Deserialize(json, HotKeySettingsContext.Default.HotKeySettings);

        back.Entries.Should().HaveCount(3);
        back.Entries[0].Trigger.Key.Should().Be((int)SDL.SDL_Keycode.SDLK_F1);
        back.Entries[1].Trigger.Button.Should().Be(4);
        back.Entries[2].Action.ConsumableLabel.Should().Be("Heal");
        back.CustomConsumables.Should().ContainSingle(c => c.Label == "My Apple");
    }
}
