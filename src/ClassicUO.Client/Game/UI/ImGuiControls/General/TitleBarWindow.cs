using ImGuiNET;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using System.Numerics;

namespace ClassicUO.Game.UI.ImGuiControls
{
    public class TitleBarWindow : SingletonImGuiWindow<TitleBarWindow>
    {
        private Profile profile;
        private bool enableTitleBarStats;
        private int updateInterval;
        private TitleBarStatsMode statsMode;

        private TitleBarWindow() : base("Title Bar Settings")
        {
            WindowFlags = ImGuiWindowFlags.AlwaysAutoResize;
            profile = ProfileManager.CurrentProfile;

            if (profile != null)
            {
                enableTitleBarStats = profile.EnableTitleBarStats;
                updateInterval = profile.TitleBarUpdateInterval;
                statsMode = profile.TitleBarStatsMode;
            }
        }

        public override void DrawContent()
        {
            if (profile == null)
            {
                ImGui.Text("Profile not loaded");
                return;
            }

            ImGui.Spacing();

            // Main description
            ImGui.TextWrapped("Configure window title bar to show HP, Mana, and Stamina information.");
            ImGui.Spacing();

            // Wiki link (placeholder - would need proper link handling)
            if (ImGui.Button("Title Bar Status Wiki"))
            {
                // Would open wiki link here
                GameActions.Print(Client.Game.UO.World, "Wiki: https://github.com/PlayTazUO/TazUO/wiki/Title-Bar-Status");
            }
            ImGui.Spacing();

            // Enable title bar stats
            if (ImGui.Checkbox("Enable title bar stats", ref enableTitleBarStats))
            {
                profile.EnableTitleBarStats = enableTitleBarStats;
                if (enableTitleBarStats)
                {
                    TitleBarStatsManager.ForceUpdate();
                }
                else
                {
                    Client.Game.SetWindowTitle(string.IsNullOrEmpty(World.Instance.Player?.Name) ? string.Empty : World.Instance.Player.Name);
                }
            }
            ImGui.Spacing();

            // Update interval slider
            ImGui.Text("Update interval (ms):");
            ImGui.SetNextItemWidth(300);
            if (ImGui.SliderInt("##UpdateInterval", ref updateInterval, 500, 5000))
            {
                profile.TitleBarUpdateInterval = updateInterval;
                TitleBarStatsManager.ForceUpdate();
            }
            ImGuiComponents.Tooltip("How often to update the title bar (500ms - 5000ms)");
            ImGui.Spacing();

            // Display mode section
            ImGui.SeparatorText("Display Mode:");
            ImGui.Spacing();

            // Radio buttons for display mode
            if (ImGui.RadioButton("Text (HP 85/100, MP 42/50, SP 95/100)", statsMode == TitleBarStatsMode.Text))
            {
                statsMode = TitleBarStatsMode.Text;
                profile.TitleBarStatsMode = statsMode;
                TitleBarStatsManager.ForceUpdate();
            }

            if (ImGui.RadioButton("Percent (HP 85%, MP 84%, SP 95%)", statsMode == TitleBarStatsMode.Percent))
            {
                statsMode = TitleBarStatsMode.Percent;
                profile.TitleBarStatsMode = statsMode;
                TitleBarStatsManager.ForceUpdate();
            }

            if (ImGui.RadioButton("Progress Bar (HP [||||||    ] MP [||||||    ] SP [||||||    ])", statsMode == TitleBarStatsMode.ProgressBar))
            {
                statsMode = TitleBarStatsMode.ProgressBar;
                profile.TitleBarStatsMode = statsMode;
                TitleBarStatsManager.ForceUpdate();
            }

            ImGui.Spacing();

            // Preview section
            ImGui.SeparatorText("Preview:");
            ImGui.TextWrapped(TitleBarStatsManager.GetPreviewText());
            ImGui.Spacing();

            // Note about Unicode characters
            ImGui.TextWrapped("Note: Progress bars use Unicode block characters (█▓▒░) and may not display correctly on all systems.");
        }
    }
}