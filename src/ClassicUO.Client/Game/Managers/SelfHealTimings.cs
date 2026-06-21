using System;

namespace ClassicUO.Game.Managers
{
    /// <summary>
    /// Computes self-heal loop timings from the player's Faster Casting (FC) and Faster Cast
    /// Recovery (FCR) for the active spell school. Pure and unit-tested.
    ///
    /// Magery: recovery base 6, FC cap 2.   Chivalry: recovery base 7, FC cap 4.
    /// recovery = max(0, base - FCR) x 0.25s.   cast = max(0.25s, spellBase - FC x 0.25s).
    /// Base cast times: Heal 1.0s, Cure 1.25s, Close Wounds 1.5s, Cleanse by Fire 1.0s.
    /// </summary>
    public static class SelfHealTimings
    {
        private const double MageryHealBaseSec = 1.0;    // Heal
        private const double MageryCureBaseSec = 1.25;   // Cure
        private const double ChivalryHealBaseSec = 1.5;  // Close Wounds
        private const double ChivalryCureBaseSec = 1.0;  // Cleanse by Fire
        private const double CastFloorSec = 0.25;        // server cast-time floor
        private const double FcStepSec = 0.25;           // each FC point shaves 0.25s
        private const int RecoveryStepMs = 250;          // each FCR point = 0.25s recovery
        private const int GraceMarginMs = 400;           // latency/jitter pad over the longest cast

        public const int MageryFcCap = 2;
        public const int ChivalryFcCap = 4;
        public const int MageryFcrCap = 6;
        public const int ChivalryFcrCap = 7;

        public static int FcCap(bool chivalry) => chivalry ? ChivalryFcCap : MageryFcCap;
        public static int FcrCap(bool chivalry) => chivalry ? ChivalryFcrCap : MageryFcrCap;

        /// <summary>
        /// Returns the loop timings for the given school + FC/FCR.
        /// recastDelayMs = the cast recovery (pad after a successful heal);
        /// castStartGraceMs = the longest of the heal/cure cast times plus a latency margin.
        /// FC/FCR are clamped to the school caps.
        /// </summary>
        public static (int recastDelayMs, int castStartGraceMs) Compute(bool chivalry, int fc, int fcr)
        {
            int recoveryBase = chivalry ? 7 : 6;
            fc = Math.Clamp(fc, 0, FcCap(chivalry));
            fcr = Math.Clamp(fcr, 0, FcrCap(chivalry));

            int recoveryMs = Math.Max(0, recoveryBase - fcr) * RecoveryStepMs;

            double healBase = chivalry ? ChivalryHealBaseSec : MageryHealBaseSec;
            double cureBase = chivalry ? ChivalryCureBaseSec : MageryCureBaseSec;
            double healCastSec = Math.Max(CastFloorSec, healBase - fc * FcStepSec);
            double cureCastSec = Math.Max(CastFloorSec, cureBase - fc * FcStepSec);
            int longestCastMs = (int)Math.Round(Math.Max(healCastSec, cureCastSec) * 1000.0);

            return (recoveryMs, longestCastMs + GraceMarginMs);
        }

        private const int PingPadMultiplier = 2;   // pad the verify window by ~one round-trip of slack
        private const int MaxPingMs = 1000;         // clamp absurd/garbage ping readings

        /// <summary>
        /// Cure-verify window: how long to wait for the poison-cleared status before recasting Cure.
        /// The clear arrives via a server health-bar packet ~one round-trip after we self-target, so a
        /// fixed window (e.g. 600ms) is too short on a 150-200ms connection — the guard times out and
        /// fires a redundant second cure. We pad the configured base by 2x the current ping so the
        /// window scales with latency. ping is clamped so a bad reading can't blow the window up.
        /// </summary>
        public static int CureVerifyWindow(int configuredMs, int pingMs)
        {
            int pad = Math.Clamp(pingMs, 0, MaxPingMs) * PingPadMultiplier;
            return Math.Max(0, configuredMs) + pad;
        }
    }
}
