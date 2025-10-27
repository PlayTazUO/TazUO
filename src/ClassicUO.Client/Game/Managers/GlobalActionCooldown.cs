using ClassicUO.Configuration;

namespace ClassicUO.Game.Managers
{
    public static class GlobalActionCooldown
    {
        private static long nextActionTime = 0;
        private static long cooldownDuration => ProfileManager.CurrentProfile.MoveMultiObjectDelay;
        public static long CooldownDuration => cooldownDuration;

        public static bool IsOnCooldown => Time.Ticks < nextActionTime;

        public static void BeginCooldown() => nextActionTime = Time.Ticks + cooldownDuration;
    }
}
