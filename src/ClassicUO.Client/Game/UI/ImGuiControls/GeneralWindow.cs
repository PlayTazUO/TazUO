using ImGuiNET;
using ClassicUO.Configuration;
using System.Numerics;

namespace ClassicUO.Game.UI.ImGuiControls
{
    public class GeneralWindow : SingletonImGuiWindow<GeneralWindow>
    {
        private Profile profile;
        private int ObjectMoveDelay;
        private bool HighlightObjects;
        private bool ShowNames;
        private ushort TurnDelay;
        private GeneralWindow() : base("General Settings")
        {
            WindowFlags = ImGuiWindowFlags.AlwaysAutoResize;
            profile = ProfileManager.CurrentProfile;
            ObjectMoveDelay = profile.MoveMultiObjectDelay;
            HighlightObjects = profile.HighlightGameObjects;
            ShowNames = profile.NameOverheadToggled;
            TurnDelay = profile.TurnDelay;
        }

        private int activeTab = 0;

        public override void DrawContent()
        {
            if (profile == null)
            {
                ImGui.Text("Profile not loaded");
                return;
            }

            ImGui.Separator();

            CreateTabButton("Options", 0);
            ImGui.SameLine(0, 5);
            CreateTabButton("Info", 1);
            ImGui.SameLine(0, 5);
            CreateTabButton("HUD", 2);
            ImGui.SameLine(0, 5);
            CreateTabButton("Journal Filter", 3);

            ImGui.Separator();
            ImGui.Spacing();

            // Show active tab content
            switch (activeTab)
            {
                case 0:
                    DrawOptionsTab();
                    break;
                case 1:
                    DrawInfoTab();
                    break;
                case 2:
                    ImGui.Text("HUD Settings will go here.");
                    break;
                case 3:
                    ImGui.Text("Journal Filter Settings will go here.");
                    break;
            }
        }

        private void CreateTabButton(string label, int tabIndex)
        {
            Vector4 buttonColor, textColor;

            if (activeTab == tabIndex)
            {
                // Active tab
                buttonColor = ThemeUtils.Colors.Primary;
                textColor = ThemeUtils.Colors.BaseContent;
            }
            else
            {
                // Inactive tab
                buttonColor = ThemeUtils.Colors.Base200;
                textColor = ThemeUtils.Colors.BaseContent;
            }

            ImGui.PushStyleColor(ImGuiCol.Button, buttonColor);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(buttonColor.X + 0.1f, buttonColor.Y + 0.1f, buttonColor.Z + 0.1f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.Text, textColor);

            if (ImGui.Button(label))
            {
                activeTab = tabIndex;
            }

            ImGui.PopStyleColor(3);
        }

        private void DrawOptionsTab()
        {
            // Section title with spacing
            ImGui.TextColored(ThemeUtils.Colors.BaseContent, "Visual Config");
            ImGui.Separator();
            ImGui.Spacing();

            if (ImGui.Checkbox("Highlight game objects", ref HighlightObjects))
            {
                profile.HighlightGameObjects = HighlightObjects;
            }

            if (ImGui.Checkbox("Show Names", ref ShowNames))
            {
                profile.NameOverheadToggled = ShowNames;
            }

            // Large separation between sections
            ImGui.Dummy(new Vector2(0.0f, 20.0f));

            // Second section
            ImGui.TextColored(ThemeUtils.Colors.BaseContent, "Delay Config");
            ImGui.Separator();
            ImGui.Spacing();

            int tempTurnDelay = TurnDelay;

            if (ImGui.SliderInt("Turn Delay", ref tempTurnDelay, 0, 150, " %d ms"))
            {
                if (tempTurnDelay < 0) tempTurnDelay = 0;
                if (tempTurnDelay > ushort.MaxValue) tempTurnDelay = 100;

                TurnDelay = (ushort)tempTurnDelay;
                profile.TurnDelay = TurnDelay;
            }

            ImGui.Spacing(); // Moderate space between controls

            if (ImGui.InputInt("Object Delay", ref ObjectMoveDelay, 50, 100))
            {
                if (ObjectMoveDelay < 0 || ObjectMoveDelay > 1000)
                    ObjectMoveDelay = 1000;
                profile.MoveMultiObjectDelay = ObjectMoveDelay;
            }
        }

        private void DrawInfoTab()
        {
            ImGui.Text("Ping:");
            ImGui.Spacing();
            ImGui.Text("FPS:");
            ImGui.Spacing();
            ImGui.Text("Last Object:");
            ImGui.Spacing();
            ImGui.Text("TazUO version: 1.0.0");


            if (ImGui.Button("More Details"))
            {
                // Logic to show more information
            }
        }
    }
}
