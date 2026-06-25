using ClassicUO.Game.Managers.Hotkeys;
using ClassicUO.Input;
using FluentAssertions;
using SDL3;
using Xunit;

namespace ClassicUO.UnitTests.Game.Managers.Hotkeys
{
    public class HotkeyBindingTest
    {
        [Fact]
        public void Empty_Binding_IsEmpty()
        {
            new HotkeyBinding().IsEmpty.Should().BeTrue();
        }

        [Fact]
        public void Constructor_NormalizesLeftRightModifiers()
        {
            var b = new HotkeyBinding(SDL.SDL_Keycode.SDLK_F1, SDL.SDL_Keymod.SDL_KMOD_LCTRL);

            b.HasKey.Should().BeTrue();
            b.Ctrl.Should().BeTrue();
            b.Shift.Should().BeFalse();
            b.Alt.Should().BeFalse();
            b.Mod.Should().Be(SDL.SDL_Keymod.SDL_KMOD_CTRL);
        }

        [Fact]
        public void Constructor_StripsLockKeys()
        {
            var b = new HotkeyBinding(SDL.SDL_Keycode.SDLK_F1,
                SDL.SDL_Keymod.SDL_KMOD_SHIFT | SDL.SDL_Keymod.SDL_KMOD_NUM | SDL.SDL_Keymod.SDL_KMOD_CAPS);

            b.Shift.Should().BeTrue();
            b.Ctrl.Should().BeFalse();
            b.Alt.Should().BeFalse();
        }

        [Fact]
        public void Clear_MakesBindingEmpty()
        {
            var b = new HotkeyBinding(SDL.SDL_Keycode.SDLK_F1, SDL.SDL_Keymod.SDL_KMOD_CTRL);
            b.Clear();
            b.IsEmpty.Should().BeTrue();
        }

        [Fact]
        public void IsPressed_EmptyBinding_IsFalse()
        {
            // An unbound hotkey must never report pressed, regardless of the modifier-match mode,
            // so nothing triggers when no hotkey is set.
            new HotkeyBinding().IsPressed().Should().BeFalse();
            new HotkeyBinding().IsPressed(allowAdditionalModifiers: false).Should().BeFalse();
        }

        [Fact]
        public void IsPressed_ClearedBinding_IsFalse()
        {
            var b = new HotkeyBinding(SDL.SDL_Keycode.SDLK_F1, SDL.SDL_Keymod.SDL_KMOD_CTRL);
            b.Clear();
            b.IsPressed().Should().BeFalse();
        }

        [Fact]
        public void Clone_ProducesIndependentCopy()
        {
            var original = new HotkeyBinding(SDL.SDL_Keycode.SDLK_F1, SDL.SDL_Keymod.SDL_KMOD_CTRL)
            {
                ControllerButtons = new[] { SDL.SDL_GamepadButton.SDL_GAMEPAD_BUTTON_SOUTH }
            };

            HotkeyBinding clone = original.Clone();
            clone.Clear();

            original.HasKey.Should().BeTrue();
            original.ControllerButtons.Should().NotBeNull();
        }

        [Fact]
        public void Matches_SameKeyAndMods_True()
        {
            var a = new HotkeyBinding(SDL.SDL_Keycode.SDLK_F1, SDL.SDL_Keymod.SDL_KMOD_CTRL);
            var b = new HotkeyBinding(SDL.SDL_Keycode.SDLK_F1, SDL.SDL_Keymod.SDL_KMOD_LCTRL);

            a.Matches(b).Should().BeTrue();
        }

        [Fact]
        public void Matches_DifferentMods_False()
        {
            var a = new HotkeyBinding(SDL.SDL_Keycode.SDLK_F1, SDL.SDL_Keymod.SDL_KMOD_CTRL);
            var b = new HotkeyBinding(SDL.SDL_Keycode.SDLK_F1, SDL.SDL_Keymod.SDL_KMOD_SHIFT);

            a.Matches(b).Should().BeFalse();
        }

        [Fact]
        public void Matches_KeyboardVsMouse_False()
        {
            var key = new HotkeyBinding(SDL.SDL_Keycode.SDLK_F1, SDL.SDL_Keymod.SDL_KMOD_NONE);
            var mouse = new HotkeyBinding { MouseButton = MouseButtonType.Middle };

            key.Matches(mouse).Should().BeFalse();
        }

        [Fact]
        public void Matches_SameMouseButtonAndMods_True()
        {
            var a = new HotkeyBinding { MouseButton = MouseButtonType.Middle, Ctrl = true };
            var b = new HotkeyBinding { MouseButton = MouseButtonType.Middle, Ctrl = true };

            a.Matches(b).Should().BeTrue();
        }

        [Fact]
        public void Matches_ControllerButtons_OrderInsensitive()
        {
            var a = new HotkeyBinding
            {
                ControllerButtons = new[]
                {
                    SDL.SDL_GamepadButton.SDL_GAMEPAD_BUTTON_SOUTH,
                    SDL.SDL_GamepadButton.SDL_GAMEPAD_BUTTON_EAST
                }
            };
            var b = new HotkeyBinding
            {
                ControllerButtons = new[]
                {
                    SDL.SDL_GamepadButton.SDL_GAMEPAD_BUTTON_EAST,
                    SDL.SDL_GamepadButton.SDL_GAMEPAD_BUTTON_SOUTH
                }
            };

            a.Matches(b).Should().BeTrue();
        }

        [Fact]
        public void Matches_EmptyBindings_False()
        {
            new HotkeyBinding().Matches(new HotkeyBinding()).Should().BeFalse();
        }
    }
}
