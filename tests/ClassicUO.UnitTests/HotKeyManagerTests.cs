using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Input;
using FluentAssertions;
using SDL3;
using Xunit;

namespace ClassicUO.UnitTests;

[Collection("HotKeyManager")] // serial: static state
public class HotKeyManagerTests
{
    private static HotKeyEntry Kb(SDL.SDL_Keycode k, SDL.SDL_Keymod m = SDL.SDL_Keymod.SDL_KMOD_NONE)
        => new() { Trigger = new HotKeyTrigger { Kind = HotKeyTriggerKind.Keyboard, Key = (int)k, Mod = (int)m },
                   Action = new HotKeyAction { Type = HotKeyActionType.Ability, AbilityPrimary = true } };

    [Fact]
    public void TestDispatch_matches_keyboard_entry()
    {
        HotKeyManager.Entries.Clear();
        HotKeyManager.Entries.Add(Kb(SDL.SDL_Keycode.SDLK_F1, SDL.SDL_Keymod.SDL_KMOD_CTRL));

        HotKeyManager.TestDispatch(
            new HotKeyTrigger { Kind = HotKeyTriggerKind.Keyboard, Key = (int)SDL.SDL_Keycode.SDLK_F1 },
            SDL.SDL_Keymod.SDL_KMOD_LCTRL, out var matched).Should().BeTrue();
        matched.Should().NotBeNull();
    }

    [Fact]
    public void TestDispatch_ignores_disabled_entry()
    {
        HotKeyManager.Entries.Clear();
        var e = Kb(SDL.SDL_Keycode.SDLK_F1);
        e.Enabled = false;
        HotKeyManager.Entries.Add(e);

        HotKeyManager.TestDispatch(
            new HotKeyTrigger { Kind = HotKeyTriggerKind.Keyboard, Key = (int)SDL.SDL_Keycode.SDLK_F1 },
            SDL.SDL_Keymod.SDL_KMOD_NONE, out _).Should().BeFalse();
    }

    [Fact]
    public void AddCustomConsumable_caps_at_four()
    {
        HotKeyManager.CustomConsumables.Clear();
        for (int i = 0; i < 4; i++)
            HotKeyManager.AddCustomConsumable(new CustomConsumable { Label = $"c{i}", Graphic = i }).Should().BeTrue();
        HotKeyManager.AddCustomConsumable(new CustomConsumable { Label = "c5", Graphic = 5 }).Should().BeFalse();
        HotKeyManager.CustomConsumables.Should().HaveCount(4);
    }

    [Fact]
    public void TestDispatch_matches_toggle_hotkeys_entry()
    {
        HotKeyManager.Entries.Clear();
        HotKeyManager.Entries.Add(new HotKeyEntry
        {
            Trigger = new HotKeyTrigger { Kind = HotKeyTriggerKind.Keyboard, Key = (int)SDL.SDL_Keycode.SDLK_PAUSE },
            Action = new HotKeyAction { Type = HotKeyActionType.ToggleHotkeys },
        });
        HotKeyManager.TestDispatch(
            new HotKeyTrigger { Kind = HotKeyTriggerKind.Keyboard, Key = (int)SDL.SDL_Keycode.SDLK_PAUSE },
            SDL.SDL_Keymod.SDL_KMOD_NONE, out var matched).Should().BeTrue();
        matched.Action.Type.Should().Be(HotKeyActionType.ToggleHotkeys);
    }

    [Fact]
    public void TriggerFromMacro_and_back_roundtrip_keyboard()
    {
        var m = new Macro("t") { Key = SDL.SDL_Keycode.SDLK_F3, Ctrl = true };
        var t = HotKeyManager.TriggerFromMacro(m);
        t.Kind.Should().Be(HotKeyTriggerKind.Keyboard);
        t.Key.Should().Be((int)SDL.SDL_Keycode.SDLK_F3);
        t.Mod.Should().Be((int)SDL.SDL_Keymod.SDL_KMOD_CTRL);

        var m2 = new Macro("t2");
        HotKeyManager.ApplyTriggerToMacro(m2, t);
        m2.Key.Should().Be(SDL.SDL_Keycode.SDLK_F3);
        m2.Ctrl.Should().BeTrue();
        m2.MouseButton.Should().Be(MouseButtonType.None);
        m2.WheelScroll.Should().BeFalse();
    }

    [Fact]
    public void TriggerFromMacro_handles_mouse_and_wheel()
    {
        var mb = new Macro("mb") { MouseButton = MouseButtonType.XButton1 };
        HotKeyManager.TriggerFromMacro(mb).Kind.Should().Be(HotKeyTriggerKind.MouseButton);
        HotKeyManager.TriggerFromMacro(mb).Button.Should().Be((int)MouseButtonType.XButton1);

        var w = new Macro("w") { WheelScroll = true, WheelUp = true };
        var wt = HotKeyManager.TriggerFromMacro(w);
        wt.Kind.Should().Be(HotKeyTriggerKind.MouseWheel);
        wt.WheelUp.Should().BeTrue();
    }

    [Fact]
    public void TriggerFromMacro_unbound_is_none()
    {
        HotKeyManager.TriggerFromMacro(new Macro("u")).Kind.Should().Be(HotKeyTriggerKind.None);
    }

    [Fact]
    public void OnMacroKeyChanged_updates_referencing_entries()
    {
        HotKeyManager.Entries.Clear();
        HotKeyManager.Entries.Add(new HotKeyEntry
        {
            Trigger = new HotKeyTrigger { Kind = HotKeyTriggerKind.Keyboard, Key = (int)SDL.SDL_Keycode.SDLK_A },
            Action = new HotKeyAction { Type = HotKeyActionType.Macro, MacroName = "MyMacro" },
        });
        HotKeyManager.Entries.Add(new HotKeyEntry
        {
            Trigger = new HotKeyTrigger { Kind = HotKeyTriggerKind.Keyboard, Key = (int)SDL.SDL_Keycode.SDLK_Z },
            Action = new HotKeyAction { Type = HotKeyActionType.Macro, MacroName = "OtherMacro" },
        });
        var m = new Macro("MyMacro") { Key = SDL.SDL_Keycode.SDLK_B, Shift = true };
        HotKeyManager.OnMacroKeyChanged(m);
        HotKeyManager.Entries[0].Trigger.Key.Should().Be((int)SDL.SDL_Keycode.SDLK_B);
        HotKeyManager.Entries[0].Trigger.Mod.Should().Be((int)SDL.SDL_Keymod.SDL_KMOD_SHIFT);
        HotKeyManager.Entries[1].Trigger.Key.Should().Be((int)SDL.SDL_Keycode.SDLK_Z);
    }
}
