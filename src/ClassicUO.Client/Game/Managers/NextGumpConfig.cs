using ClassicUO.Game.UI.Gumps;
using ClassicUO.Network;

namespace ClassicUO.Game.Managers
{
    public static class NextGumpConfig
    {
        public static bool Enabled { get; set; }
        public static uint Serial { get; set; }
        public static int? X { get; set; }
        public static int? Y { get; set; }
        public static bool? IsVisible { get; set; }
        public static bool AutoClose { get; set; }
        public static bool AutoRespond { get; set; }
        public static int AutoRespondButton { get; set; }

        public static void Reset()
        {
            Enabled = false;
            Serial = 0;
            X = null;
            Y = null;
            IsVisible = null;
            AutoClose = false;
            AutoRespond = false;
            AutoRespondButton = 0;
        }

        public static bool Apply(Gump gump)
        {
            if (!Enabled || gump == null)
            {
                return false;
            }

            // If Serial is 0, match any gump, otherwise check if ServerSerial or LocalSerial matches
            if (Serial != 0 && gump.ServerSerial != Serial && gump.LocalSerial != Serial)
            {
                return false;
            }

            // Apply the configuration conditionally
            if (X.HasValue)
            {
                gump.X = X.Value;
            }

            if (Y.HasValue)
            {
                gump.Y = Y.Value;
            }

            if (IsVisible.HasValue)
            {
                gump.IsVisible = IsVisible.Value;
            }

            if (AutoRespond)
            {
                GameActions.ReplyGump(World.Instance, gump.LocalSerial, gump.ServerSerial, AutoRespondButton);
            }

            if (AutoClose)
            {
                gump.Dispose();
            }

            Reset();

            return true;
        }
    }
}
