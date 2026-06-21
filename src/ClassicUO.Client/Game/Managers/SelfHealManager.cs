using System;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers.SpellVisualRange;
using ClassicUO.Input;
using ClassicUO.Network;
using SDL3;

namespace ClassicUO.Game.Managers
{
    /// <summary>
    /// Native hold-to-heal hotkey. While the bound key is held, spams Heal on self
    /// (Cure when poisoned). Release stops after the in-flight cast.
    /// </summary>
    public static class SelfHealManager
    {
        private static readonly SelfHealStateMachine _machine = new();
        private static readonly LiveEnv _env = new();
        private static bool _held;
        private static bool _loaded;

        public static void Load()
        {
            if (_loaded) return;
            Keyboard.KeyUpEvent += OnKeyUp;
            _loaded = true;
        }

        public static void Unload()
        {
            if (!_loaded) return;
            Keyboard.KeyUpEvent -= OnKeyUp;
            _held = false;
            _loaded = false;
        }

        public static void Update() => _machine.Tick(_env, _held);

        /// <summary>Called from GameSceneInputHandler.OnKeyDown (already focus-gated).</summary>
        public static void HandleKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod, bool repeat)
        {
            if (repeat) return;

            Profile p = ProfileManager.CurrentProfile;
            if (p == null || !p.SelfHeal_Enabled || p.SelfHeal_Key == 0 || p.DisableHotkeys)
                return;

            if ((int)key != p.SelfHeal_Key)
                return;

            if (NormalizeMods(mod) != (SDL.SDL_Keymod)p.SelfHeal_Mod)
                return;

            _held = true;
        }

        // Key-up via the raw keyboard event so release is caught even if focus changed.
        private static void OnKeyUp(string hotkey)
        {
            Profile p = ProfileManager.CurrentProfile;
            if (p == null || p.SelfHeal_Key == 0)
                return;

            if (TryParseKeycode(hotkey, out SDL.SDL_Keycode key) && (int)key == p.SelfHeal_Key)
                _held = false;
        }

        // Strip lock keys and normalize left/right modifiers, matching SpellBarManager.
        private static SDL.SDL_Keymod NormalizeMods(SDL.SDL_Keymod mod)
        {
            mod &= ~SDL.SDL_Keymod.SDL_KMOD_NUM;
            mod &= ~SDL.SDL_Keymod.SDL_KMOD_CAPS;
            mod &= ~SDL.SDL_Keymod.SDL_KMOD_SCROLL;
            mod &= ~SDL.SDL_Keymod.SDL_KMOD_MODE;

            if ((mod & (SDL.SDL_Keymod.SDL_KMOD_LCTRL | SDL.SDL_Keymod.SDL_KMOD_RCTRL)) != 0)
            {
                mod &= ~(SDL.SDL_Keymod.SDL_KMOD_LCTRL | SDL.SDL_Keymod.SDL_KMOD_RCTRL);
                mod |= SDL.SDL_Keymod.SDL_KMOD_CTRL;
            }
            if ((mod & (SDL.SDL_Keymod.SDL_KMOD_LSHIFT | SDL.SDL_Keymod.SDL_KMOD_RSHIFT)) != 0)
            {
                mod &= ~(SDL.SDL_Keymod.SDL_KMOD_LSHIFT | SDL.SDL_Keymod.SDL_KMOD_RSHIFT);
                mod |= SDL.SDL_Keymod.SDL_KMOD_SHIFT;
            }
            if ((mod & (SDL.SDL_Keymod.SDL_KMOD_LALT | SDL.SDL_Keymod.SDL_KMOD_RALT)) != 0)
            {
                mod &= ~(SDL.SDL_Keymod.SDL_KMOD_LALT | SDL.SDL_Keymod.SDL_KMOD_RALT);
                mod |= SDL.SDL_Keymod.SDL_KMOD_ALT;
            }
            return mod;
        }

        // Keyboard event strings look like "CTRL+SHIFT+SDLK_F1". Extract the SDLK_ token.
        private static bool TryParseKeycode(string hotkey, out SDL.SDL_Keycode key)
        {
            key = SDL.SDL_Keycode.SDLK_UNKNOWN;
            if (string.IsNullOrEmpty(hotkey))
                return false;

            foreach (string part in hotkey.Split('+'))
            {
                if (part.StartsWith("SDLK_", StringComparison.OrdinalIgnoreCase) &&
                    Enum.TryParse(part, true, out SDL.SDL_Keycode parsed))
                {
                    key = parsed;
                    return true;
                }
            }
            return false;
        }

        private sealed class LiveEnv : ISelfHealEnv
        {
            public long Now => ClassicUO.Time.Ticks;

            public bool CanAct
            {
                get
                {
                    Profile p = ProfileManager.CurrentProfile;
                    if (p == null || !p.SelfHeal_Enabled || p.DisableHotkeys)
                        return false;

                    var player = Client.Game?.UO?.World?.Player;
                    return player != null && !player.IsDead;
                }
            }

            public bool IsPoisoned => Client.Game?.UO?.World?.Player?.IsPoisoned ?? false;

            // Recast (recovery) and cast-start grace are physical consequences of the spell school
            // + FC/FCR, not free-form preferences, so we derive them from SelfHealTimings at runtime
            // rather than a stored default. A stale stored grace (e.g. 800ms) can be shorter than the
            // actual Cure cast (~0.75s + latency), which let the loop time out mid-cast and recast on
            // top of the in-flight Cure (auto-fizzle). The formula always yields cast + a latency margin.
            public long RecastDelayMs => Timings.recastDelayMs;

            public long CastStartGraceMs => Timings.castStartGraceMs;

            private static (int recastDelayMs, int castStartGraceMs) Timings
            {
                get
                {
                    Profile p = ProfileManager.CurrentProfile;
                    if (p == null)
                        return ((int)SelfHealStateMachine.DefaultRecastDelayMs, (int)SelfHealStateMachine.DefaultCastStartGraceMs);
                    return SelfHealTimings.Compute(p.SelfHeal_UseChivalry, p.SelfHeal_FC, p.SelfHeal_FCR);
                }
            }

            public long CureVerifyMs
            {
                get
                {
                    int configured = (int)(ProfileManager.CurrentProfile?.SelfHeal_CureVerifyMs ?? SelfHealStateMachine.DefaultCureVerifyMs);
                    int ping = (int)(AsyncNetClient.Socket?.Statistics?.Ping ?? 0);
                    return SelfHealTimings.CureVerifyWindow(configured, ping);
                }
            }

            public long InterruptRetryMs =>
                ProfileManager.CurrentProfile?.SelfHeal_InterruptRetryMs ?? SelfHealStateMachine.DefaultInterruptRetryMs;

            public bool IsCasting => Client.Game?.UO?.World?.Player?.IsCasting ?? false;

            public long LastCastFailedAt => SpellVisualRangeManager.Instance?.LastCastFailedTick ?? 0;

            private static bool UseChivalry => ProfileManager.CurrentProfile?.SelfHeal_UseChivalry ?? false;

            public int HealSpellId =>
                UseChivalry ? SelfHealStateMachine.ChivalryHealSpellId : SelfHealStateMachine.MageryHealSpellId;

            public int CureSpellId =>
                UseChivalry ? SelfHealStateMachine.ChivalryCureSpellId : SelfHealStateMachine.MageryCureSpellId;

            public bool IsTargetingAfterCast
            {
                get
                {
                    // Prefer the spell-visual detector (it respects cast timing), but fall back to
                    // the raw target-cursor flag so self-heal also works when Spell Indicators are
                    // disabled. Only consulted right after our own cast, so the raw flag is safe.
                    if (SpellVisualRangeManager.Instance?.IsTargetingAfterCasting() ?? false)
                        return true;

                    return Client.Game?.UO?.World?.TargetManager?.IsTargeting ?? false;
                }
            }

            public void Cast(int spellId) => GameActions.CastSpell(spellId);

            public void TargetSelf()
            {
                var world = Client.Game?.UO?.World;
                if (world?.Player != null)
                    world.TargetManager.Target(world.Player.Serial);
            }
        }
    }
}
