using ClassicUO.Configuration;
using ClassicUO.Game.UI.Gumps;
using System;

namespace ClassicUO.Game.Managers
{
    public class CoolDownBarManager
    {
        private const int MAX_COOLDOWN_BARS = 15;
        private static CoolDownBar[] coolDownBars = new CoolDownBar[MAX_COOLDOWN_BARS];
        private World World;

        public CoolDownBarManager(World world)
        {
            this.World = world;
            EventSink.MessageReceived += MessageManager_MessageReceived;
        }

        private void MessageManager_MessageReceived(object sender, MessageEventArgs e)
        {
            if (ProfileManager.CurrentProfile == null) return;

            foreach (CooldownBarConfigEntry bar in CooldownBarsConfig.Current.Bars)
            {
                switch (bar.MessageType)
                {
                    default:
                    case 0:
                        break;
                    case 1: //self
                        if (e.Parent != null && e.Parent.Serial != World.Player.Serial)
                            return;
                        break;
                    case 2:
                        if (e.Parent != null && e.Parent.Serial == World.Player.Serial)
                            return;
                        break;

                }
                if (e.Text.Contains(bar.Trigger))
                {
                    AddCoolDownBar(
                        World,
                        TimeSpan.FromSeconds(bar.Cooldown),
                        bar.Label,
                        bar.Hue,
                        bar.ReplaceIfExists,
                        bar.SkipIfExists
                        );
                }
            }
        }

        public static void AddCoolDownBar(World world, TimeSpan _duration, string _name, ushort _hue, bool replace, bool skipIfExists = false)
        {
            if (replace || skipIfExists)
                for (int i = 0; i < coolDownBars.Length; i++)
                {
                    if (coolDownBars[i] != null && !coolDownBars[i].IsDisposed && coolDownBars[i].Name == _name)
                    {
                        //An instance is already on-screen. Preserve the running countdown and do not add a new one.
                        if (skipIfExists)
                            return;

                        coolDownBars[i].Dispose();
                        coolDownBars[i] = new CoolDownBar(world, _duration, _name, _hue, CoolDownBar.DEFAULT_X, CoolDownBar.DEFAULT_Y + (i * (CoolDownBar.COOL_DOWN_HEIGHT + 5)));
                        UIManager.Add(coolDownBars[i]);
                        return;
                    }
                }
            for (int i = 0; i < coolDownBars.Length; i++)
            {
                if (coolDownBars[i] == null || coolDownBars[i].IsDisposed)
                {
                    coolDownBars[i] = new CoolDownBar(world, _duration, _name, _hue, CoolDownBar.DEFAULT_X, CoolDownBar.DEFAULT_Y + (i * (CoolDownBar.COOL_DOWN_HEIGHT + 5)));
                    UIManager.Add(coolDownBars[i]);
                    return;
                }
            }
        }

        public static CoolDownBar FindCoolDownBar(string name)
        {
            for (int i = 0; i < coolDownBars.Length; i++)
                if (coolDownBars[i] != null && !coolDownBars[i].IsDisposed && coolDownBars[i].Name == name)
                    return coolDownBars[i];
            return null;
        }

        public static void UpdateCoolDownBar(string name, TimeSpan? maxValue = null, TimeSpan? currentValue = null)
        {
            CoolDownBar bar = FindCoolDownBar(name);
            bar?.Update(maxValue, currentValue);
        }

        public static void RestartCoolDownBar(string name)
        {
            CoolDownBar bar = FindCoolDownBar(name);
            bar?.Restart();
        }

        public static void DeleteCoolDownBar(string name)
        {
            CoolDownBar bar = FindCoolDownBar(name);
            if (bar == null)
                return;

            bar.Dispose();
            for (int i = 0; i < coolDownBars.Length; i++)
                if (coolDownBars[i] == bar)
                {
                    coolDownBars[i] = null;
                    return;
                }
        }

        public static bool CoolDownBarExists(string name) => FindCoolDownBar(name) != null;
    }
}
