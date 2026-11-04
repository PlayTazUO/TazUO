#nullable enable

using ClassicUO.Input;
using FluentAssertions;
using SDL3;
using Xunit;

namespace ClassicUO.UnitTests.Input
{
    /// <summary>
    /// <see cref="Keyboard"/> holds process-wide static state, so every test here resets it first
    /// and drives it only through the real event entry points.
    /// </summary>
    public class KeyboardIsKeyPressedTests
    {
        public KeyboardIsKeyPressedTests()
        {
            Keyboard.ClearHeldKeys();
            Keyboard.ClearModifiers();
        }

        [Fact]
        public void Empty_Input_IsFalse()
        {
            Keyboard.IsKeyPressed(string.Empty).Should().BeFalse();
            Keyboard.IsKeyPressed(null).Should().BeFalse();
        }

        [Fact]
        public void PlainKey_MatchesOnlyWhileHeld()
        {
            Keyboard.IsKeyPressed("F1").Should().BeFalse();

            PressKey(SDL.SDL_Keycode.SDLK_F1);
            Keyboard.IsKeyPressed("F1").Should().BeTrue();

            ReleaseKey(SDL.SDL_Keycode.SDLK_F1);
            Keyboard.IsKeyPressed("F1").Should().BeFalse();
        }

        [Fact]
        public void ModifiedKey_RequiresBothKeyAndModifiers()
        {
            PressKey(SDL.SDL_Keycode.SDLK_F1);
            Keyboard.IsKeyPressed("CTRL+F1").Should().BeFalse();

            PressKey(SDL.SDL_Keycode.SDLK_F1, SDL.SDL_Keymod.SDL_KMOD_LCTRL);
            Keyboard.IsKeyPressed("CTRL+F1").Should().BeTrue();
            Keyboard.IsKeyPressed("CTRL+SHIFT+F1").Should().BeFalse();
        }

        [Fact]
        public void ExtraHeldModifiers_DoNotPreventMatch()
        {
            PressKey(SDL.SDL_Keycode.SDLK_F1, SDL.SDL_Keymod.SDL_KMOD_LCTRL | SDL.SDL_Keymod.SDL_KMOD_LSHIFT);

            Keyboard.IsKeyPressed("F1").Should().BeTrue();
            Keyboard.IsKeyPressed("CTRL+F1").Should().BeTrue();
            Keyboard.IsKeyPressed("CTRL+SHIFT+F1").Should().BeTrue();
        }

        [Fact]
        public void ModifierOnly_MatchesWhileThoseModifiersHeld()
        {
            Keyboard.IsKeyPressed("CTRL").Should().BeFalse();

            PressBareModifier(SDL.SDL_Keycode.SDLK_LCTRL, SDL.SDL_Keymod.SDL_KMOD_LCTRL);
            Keyboard.IsKeyPressed("CTRL").Should().BeTrue();
            Keyboard.IsKeyPressed("SHIFT").Should().BeFalse();
            Keyboard.IsKeyPressed("CTRL+SHIFT").Should().BeFalse();

            PressBareModifier(
                SDL.SDL_Keycode.SDLK_LSHIFT,
                SDL.SDL_Keymod.SDL_KMOD_LCTRL | SDL.SDL_Keymod.SDL_KMOD_LSHIFT
            );
            Keyboard.IsKeyPressed("CTRL+SHIFT").Should().BeTrue();

            ReleaseBareModifier(SDL.SDL_Keycode.SDLK_LCTRL, SDL.SDL_Keymod.SDL_KMOD_LSHIFT);
            Keyboard.IsKeyPressed("CTRL").Should().BeFalse();
            Keyboard.IsKeyPressed("SHIFT").Should().BeTrue();
        }

        /// <summary>
        /// Guards the case an unknown key name must not degrade into the modifier-only path, which
        /// would report a permanently held key.
        /// </summary>
        [Theory]
        [InlineData("ENTER")]
        [InlineData("ESC")]
        [InlineData("NOT_A_KEY")]
        [InlineData("CTRL+ENTER")]
        [InlineData("CTRL+SHIFT+NOT_A_KEY")]
        public void UnrecognizedKeyName_IsFalseEvenWithModifiersHeld(string input)
        {
            PressKey(
                SDL.SDL_Keycode.SDLK_F1,
                SDL.SDL_Keymod.SDL_KMOD_LCTRL | SDL.SDL_Keymod.SDL_KMOD_LSHIFT | SDL.SDL_Keymod.SDL_KMOD_LALT
            );

            Keyboard.IsKeyPressed(input).Should().BeFalse();
        }

        [Theory]
        [InlineData("ctrl+f1")]
        [InlineData("CTRL + F1")]
        [InlineData("F1+CTRL")]
        [InlineData("CTRL+SDLK_F1")]
        public void InputFormatting_IsNormalizedBeforeMatching(string input)
        {
            PressKey(SDL.SDL_Keycode.SDLK_F1, SDL.SDL_Keymod.SDL_KMOD_LCTRL);

            Keyboard.IsKeyPressed(input).Should().BeTrue();
        }

        [Fact]
        public void ClearHeldKeys_DropsKeyButKeepsModifiers()
        {
            PressKey(SDL.SDL_Keycode.SDLK_F1, SDL.SDL_Keymod.SDL_KMOD_LCTRL);

            Keyboard.ClearHeldKeys();

            Keyboard.IsKeyPressed("CTRL+F1").Should().BeFalse();
            Keyboard.IsKeyPressed("CTRL").Should().BeTrue();
        }

        private static void PressKey(SDL.SDL_Keycode key, SDL.SDL_Keymod mod = SDL.SDL_Keymod.SDL_KMOD_NONE)
        {
            Keyboard.OnKeyDown(MakeEvent(SDL.SDL_EventType.SDL_EVENT_KEY_DOWN, key, mod));
        }

        private static void ReleaseKey(SDL.SDL_Keycode key, SDL.SDL_Keymod mod = SDL.SDL_Keymod.SDL_KMOD_NONE)
        {
            Keyboard.OnKeyUp(MakeEvent(SDL.SDL_EventType.SDL_EVENT_KEY_UP, key, mod));
        }

        /// <summary>
        /// Presses a lone modifier key. <paramref name="mod"/> is the mask SDL reports with the
        /// event, i.e. the state after the press.
        /// </summary>
        private static void PressBareModifier(SDL.SDL_Keycode key, SDL.SDL_Keymod mod) => PressKey(key, mod);

        /// <summary>
        /// Releases a lone modifier key. <paramref name="mod"/> is the mask remaining afterwards.
        /// </summary>
        private static void ReleaseBareModifier(SDL.SDL_Keycode key, SDL.SDL_Keymod mod) => ReleaseKey(key, mod);

        private static SDL.SDL_KeyboardEvent MakeEvent(
            SDL.SDL_EventType type,
            SDL.SDL_Keycode key,
            SDL.SDL_Keymod mod
        )
        {
            return new SDL.SDL_KeyboardEvent
            {
                type = type,
                key = (uint)key,
                mod = mod
            };
        }
    }
}
