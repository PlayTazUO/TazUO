using ClassicUO.Game.Managers;
using ClassicUO.Game.Managers.Hotkeys;
using FluentAssertions;
using SDL3;
using Xunit;

namespace ClassicUO.UnitTests.Game.Managers
{
    public class CounterBarHotkeysManagerTest
    {
        // High indices unlikely to collide with anything the client registers; always cleaned up.
        private const int Idx = 100000;

        [Fact]
        public void SetBinding_UsableKey_RoundTripsThroughGet()
        {
            try
            {
                CounterBarHotkeysManager.SetBinding(Idx, new HotkeyBinding(SDL.SDL_Keycode.SDLK_F1, SDL.SDL_Keymod.SDL_KMOD_CTRL));

                HotkeyBinding got = CounterBarHotkeysManager.GetBinding(Idx);
                got.HasKey.Should().BeTrue();
                got.Key.Should().Be(SDL.SDL_Keycode.SDLK_F1);
                got.Ctrl.Should().BeTrue();
            }
            finally
            {
                CounterBarHotkeysManager.ClearBinding(Idx);
            }
        }

        [Fact]
        public void SetBinding_EmptyBinding_ClearsRegistration()
        {
            CounterBarHotkeysManager.SetBinding(Idx, new HotkeyBinding(SDL.SDL_Keycode.SDLK_F2, SDL.SDL_Keymod.SDL_KMOD_NONE));
            CounterBarHotkeysManager.SetBinding(Idx, new HotkeyBinding());

            CounterBarHotkeysManager.GetBinding(Idx).IsEmpty.Should().BeTrue();
        }

        [Fact]
        public void PruneFrom_RemovesCellsAtOrAboveThreshold()
        {
            try
            {
                CounterBarHotkeysManager.SetBinding(Idx, new HotkeyBinding(SDL.SDL_Keycode.SDLK_F3, SDL.SDL_Keymod.SDL_KMOD_NONE));
                CounterBarHotkeysManager.SetBinding(Idx + 1, new HotkeyBinding(SDL.SDL_Keycode.SDLK_F4, SDL.SDL_Keymod.SDL_KMOD_NONE));

                CounterBarHotkeysManager.PruneFrom(Idx + 1);

                CounterBarHotkeysManager.GetBinding(Idx).IsEmpty.Should().BeFalse();
                CounterBarHotkeysManager.GetBinding(Idx + 1).IsEmpty.Should().BeTrue();
            }
            finally
            {
                CounterBarHotkeysManager.ClearBinding(Idx);
                CounterBarHotkeysManager.ClearBinding(Idx + 1);
            }
        }
    }
}
