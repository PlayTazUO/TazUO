using ClassicUO.Configuration;
using ClassicUO.Utility;
using System;

namespace ClassicUO.Game.Managers
{
    public static class TitleBarStatsManager
    {
        public static void UpdateTitleBar()
        {
            if (ProfileManager.CurrentProfile == null)
            {
                return;
            }

            if (!ProfileManager.CurrentProfile.EnableTitleBarStats || World.Player == null)
            {
                return;
            }

            string statsText = GenerateStatsText();
            string title = string.IsNullOrEmpty(World.Player.Name) ?
                statsText :
                $"{World.Player.Name} - {statsText}";

            Client.Game.SetWindowTitle(title);
        }

        private static string GenerateStatsText()
        {
            if (ProfileManager.CurrentProfile == null)
            {
                return string.Empty;
            }

            switch (ProfileManager.CurrentProfile.TitleBarStatsMode)
            {
                case TitleBarStatsMode.Text:
                    return $"HP {World.Player.Hits}/{World.Player.HitsMax}, MP {World.Player.Mana}/{World.Player.ManaMax}, SP {World.Player.Stamina}/{World.Player.StaminaMax}";

                case TitleBarStatsMode.Percent:
                    int hpPercent = World.Player.HitsMax > 0 ? (World.Player.Hits * 100) / World.Player.HitsMax : 100;
                    int mpPercent = World.Player.ManaMax > 0 ? (World.Player.Mana * 100) / World.Player.ManaMax : 100;
                    int spPercent = World.Player.StaminaMax > 0 ? (World.Player.Stamina * 100) / World.Player.StaminaMax : 100;
                    return $"HP {hpPercent}%, MP {mpPercent}%, SP {spPercent}%";

                case TitleBarStatsMode.ProgressBar:
                    string hpBar = GenerateProgressBar(World.Player.Hits, World.Player.HitsMax);
                    string mpBar = GenerateProgressBar(World.Player.Mana, World.Player.ManaMax);
                    string spBar = GenerateProgressBar(World.Player.Stamina, World.Player.StaminaMax);
                    return $"HP {hpBar} MP {mpBar} SP {spBar}";

                default:
                    return $"HP {World.Player.Hits}/{World.Player.HitsMax}, MP {World.Player.Mana}/{World.Player.ManaMax}, SP {World.Player.Stamina}/{World.Player.StaminaMax}"; // Fallback to text mode
            }
        }

        private static string GenerateProgressBar(ushort current, ushort max)
        {
            const int barLength = 8;
            const char fullBlock = '█';
            const char partialBlock = '▓';
            const char emptyBlock = '░';

            if (max == 0)
                return new string(emptyBlock, barLength);

            float percentage = (float)current / max;
            int filledBlocks = (int)Math.Floor(percentage * barLength);
            bool hasPartial = (percentage * barLength) - filledBlocks > 0.5f;

            string result = "";

            // Add full blocks
            for (int i = 0; i < filledBlocks; i++)
            {
                result += fullBlock;
            }

            // Add partial block if needed
            if (hasPartial && filledBlocks < barLength)
            {
                result += partialBlock;
                filledBlocks++;
            }

            // Fill remaining with empty blocks
            while (result.Length < barLength)
            {
                result += emptyBlock;
            }

            return result;
        }

        public static void ForceUpdate()
        {
            UpdateTitleBar();
        }

        public static string GetPreviewText()
        {
            if (ProfileManager.CurrentProfile == null)
            {
                return string.Empty;
            }

            if (World.Player == null)
            {
                // Use sample values for preview
                switch (ProfileManager.CurrentProfile.TitleBarStatsMode)
                {
                    case TitleBarStatsMode.Text:
                        return "PlayerName - HP 85/100, MP 42/50, SP 95/100";
                    case TitleBarStatsMode.Percent:
                        return "PlayerName - HP 85%, MP 84%, SP 95%";
                    case TitleBarStatsMode.ProgressBar:
                        return "PlayerName - HP ██████▓░ MP ██████▓░ SP ███████▓";
                    default:
                        return "PlayerName - HP 85/100, MP 42/50, SP 95/100";
                }
            }

            string statsText = GenerateStatsText();
            return string.IsNullOrEmpty(World.Player.Name) ?
                statsText :
                $"{World.Player.Name} - {statsText}";
        }
    }

    public enum TitleBarStatsMode
    {
        Text = 0,
        Percent = 1,
        ProgressBar = 2
    }
}
