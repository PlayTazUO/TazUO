using ClassicUO.Game.Managers.Hotkeys;
using FluentAssertions;
using SDL3;
using Xunit;

namespace ClassicUO.UnitTests.Game.Managers.Hotkeys
{
    public class HotkeyUtilTest
    {
        [Fact]
        public void NormalizeMods_CollapsesLeftRightToGeneric()
        {
            HotkeyUtil.NormalizeMods(SDL.SDL_Keymod.SDL_KMOD_LCTRL).Should().Be(SDL.SDL_Keymod.SDL_KMOD_CTRL);
            HotkeyUtil.NormalizeMods(SDL.SDL_Keymod.SDL_KMOD_RSHIFT).Should().Be(SDL.SDL_Keymod.SDL_KMOD_SHIFT);
            HotkeyUtil.NormalizeMods(SDL.SDL_Keymod.SDL_KMOD_RALT).Should().Be(SDL.SDL_Keymod.SDL_KMOD_ALT);
        }

        [Fact]
        public void NormalizeMods_StripsLockKeys()
        {
            SDL.SDL_Keymod input = SDL.SDL_Keymod.SDL_KMOD_NUM | SDL.SDL_Keymod.SDL_KMOD_CAPS | SDL.SDL_Keymod.SDL_KMOD_SCROLL;
            HotkeyUtil.NormalizeMods(input).Should().Be(SDL.SDL_Keymod.SDL_KMOD_NONE);
        }

        [Fact]
        public void ModsFromFlags_BuildsGenericMask()
        {
            HotkeyUtil.ModsFromFlags(ctrl: true, shift: true, alt: false)
                .Should().Be(SDL.SDL_Keymod.SDL_KMOD_CTRL | SDL.SDL_Keymod.SDL_KMOD_SHIFT);
        }

        [Fact]
        public void ParseHotKeyString_ParsesKeyAndModifiers()
        {
            (SDL.SDL_Keycode key, SDL.SDL_Keymod mod) = HotkeyUtil.ParseHotKeyString("CTRL+SHIFT+SDLK_F1");

            key.Should().Be(SDL.SDL_Keycode.SDLK_F1);
            mod.Should().Be(SDL.SDL_Keymod.SDL_KMOD_CTRL | SDL.SDL_Keymod.SDL_KMOD_SHIFT);
        }

        [Fact]
        public void ParseHotKeyString_EmptyInput_ReturnsUnknown()
        {
            (SDL.SDL_Keycode key, SDL.SDL_Keymod mod) = HotkeyUtil.ParseHotKeyString("");

            key.Should().Be(SDL.SDL_Keycode.SDLK_UNKNOWN);
            mod.Should().Be(SDL.SDL_Keymod.SDL_KMOD_NONE);
        }

        [Fact]
        public void TryParseKeycode_ExtractsSdlkToken()
        {
            HotkeyUtil.TryParseKeycode("CTRL+SDLK_F5", out SDL.SDL_Keycode key).Should().BeTrue();
            key.Should().Be(SDL.SDL_Keycode.SDLK_F5);
        }

        [Fact]
        public void TryParseKeycode_NoKey_ReturnsFalse()
        {
            HotkeyUtil.TryParseKeycode("CTRL+SHIFT", out _).Should().BeFalse();
        }
    }
}
