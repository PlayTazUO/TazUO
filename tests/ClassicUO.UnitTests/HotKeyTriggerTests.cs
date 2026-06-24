using ClassicUO.Game.Data;
using ClassicUO.Input;
using FluentAssertions;
using SDL3;
using Xunit;

namespace ClassicUO.UnitTests;

public class HotKeyTriggerTests
{
    [Fact]
    public void NormalizeMods_strips_lock_keys_and_consolidates_left_right()
    {
        var raw = SDL.SDL_Keymod.SDL_KMOD_LCTRL | SDL.SDL_Keymod.SDL_KMOD_NUM | SDL.SDL_Keymod.SDL_KMOD_RSHIFT;
        var norm = HotKeyTrigger.NormalizeMods(raw);
        norm.Should().Be(SDL.SDL_Keymod.SDL_KMOD_CTRL | SDL.SDL_Keymod.SDL_KMOD_SHIFT);
    }

    [Fact]
    public void MatchesKeyboard_matches_same_key_and_mod_after_normalization()
    {
        var t = new HotKeyTrigger
        {
            Kind = HotKeyTriggerKind.Keyboard,
            Key = (int)SDL.SDL_Keycode.SDLK_F1,
            Mod = (int)SDL.SDL_Keymod.SDL_KMOD_CTRL,
        };
        t.MatchesKeyboard(SDL.SDL_Keycode.SDLK_F1, SDL.SDL_Keymod.SDL_KMOD_LCTRL | SDL.SDL_Keymod.SDL_KMOD_CAPS).Should().BeTrue();
        t.MatchesKeyboard(SDL.SDL_Keycode.SDLK_F2, SDL.SDL_Keymod.SDL_KMOD_LCTRL).Should().BeFalse();
        t.MatchesKeyboard(SDL.SDL_Keycode.SDLK_F1, SDL.SDL_Keymod.SDL_KMOD_NONE).Should().BeFalse();
    }

    [Fact]
    public void Keyboard_trigger_never_matches_mouse_and_vice_versa()
    {
        var kb = new HotKeyTrigger { Kind = HotKeyTriggerKind.Keyboard, Key = (int)SDL.SDL_Keycode.SDLK_F1 };
        kb.MatchesMouseButton(MouseButtonType.XButton1, SDL.SDL_Keymod.SDL_KMOD_NONE).Should().BeFalse();

        var mb = new HotKeyTrigger { Kind = HotKeyTriggerKind.MouseButton, Button = (int)MouseButtonType.XButton1 };
        mb.MatchesKeyboard(SDL.SDL_Keycode.SDLK_F1, SDL.SDL_Keymod.SDL_KMOD_NONE).Should().BeFalse();
        mb.MatchesMouseButton(MouseButtonType.XButton1, SDL.SDL_Keymod.SDL_KMOD_NONE).Should().BeTrue();
    }

    [Fact]
    public void MatchesWheel_respects_direction_and_mod()
    {
        var t = new HotKeyTrigger { Kind = HotKeyTriggerKind.MouseWheel, WheelUp = true, Mod = (int)SDL.SDL_Keymod.SDL_KMOD_SHIFT };
        t.MatchesWheel(true, SDL.SDL_Keymod.SDL_KMOD_RSHIFT).Should().BeTrue();
        t.MatchesWheel(false, SDL.SDL_Keymod.SDL_KMOD_RSHIFT).Should().BeFalse();
    }

    [Fact]
    public void Describe_renders_each_kind()
    {
        new HotKeyTrigger { Kind = HotKeyTriggerKind.Keyboard, Key = (int)SDL.SDL_Keycode.SDLK_F1, Mod = (int)SDL.SDL_Keymod.SDL_KMOD_CTRL }
            .Describe().Should().NotBeNullOrEmpty();
        new HotKeyTrigger { Kind = HotKeyTriggerKind.MouseButton, Button = (int)MouseButtonType.XButton1 }.Describe().Should().Be("Mouse4");
        new HotKeyTrigger { Kind = HotKeyTriggerKind.MouseWheel, WheelUp = true }.Describe().Should().Be("Wheel Up");
        new HotKeyTrigger { Kind = HotKeyTriggerKind.None }.Describe().Should().Be("");
    }
}
