using ImGuiNET;
using ClassicUO.Configuration;
using System.Numerics;

namespace ClassicUO.Game.UI.ImGuiControls
{
    public class GeneralWindow : SingletonImGuiWindow<GeneralWindow>
    {
        private readonly Profile _profile = ProfileManager.CurrentProfile;
        private int _objectMoveDelay;
        private bool _highlightObjects;
        private bool _showNames;
        private ushort _turnDelay;
        private GeneralWindow() : base("General Settings")
        {
            WindowFlags = ImGuiWindowFlags.AlwaysAutoResize;
            _objectMoveDelay = _profile.MoveMultiObjectDelay;
            _highlightObjects = _profile.HighlightGameObjects;
            _showNames = _profile.NameOverheadToggled;
            _turnDelay = _profile.TurnDelay;
        }

        private int _activeTab = 0;

        public override void DrawContent()
        {
            if (_profile == null)
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
            switch (_activeTab)
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

            if (_activeTab == tabIndex)
            {
                // Active tab
                buttonColor = ImGuiTheme.Colors.Primary;
                textColor = ImGuiTheme.Colors.BaseContent;
            }
            else
            {
                // Inactive tab
                buttonColor = ImGuiTheme.Colors.Base200;
                textColor = ImGuiTheme.Colors.BaseContent;
            }

            ImGui.PushStyleColor(ImGuiCol.Button, buttonColor);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(buttonColor.X + 0.1f, buttonColor.Y + 0.1f, buttonColor.Z + 0.1f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.Text, textColor);

            if (ImGui.Button(label))
            {
                _activeTab = tabIndex;
            }

            ImGui.PopStyleColor(3);
        }

        private void DrawOptionsTab()
        {
            // Section title with spacing
            ImGui.TextColored(ImGuiTheme.Colors.BaseContent, "Visual Config");
            ImGui.Separator();
            ImGui.Spacing();

            if (ImGui.Checkbox("Highlight game objects", ref _highlightObjects))
            {
                _profile.HighlightGameObjects = _highlightObjects;
            }

            if (ImGui.Checkbox("Show Names", ref _showNames))
            {
                _profile.NameOverheadToggled = _showNames;
            }

            // Large separation between sections
            ImGui.Dummy(new Vector2(0.0f, 20.0f));

            // Second section
            ImGui.TextColored(ImGuiTheme.Colors.BaseContent, "Delay Config");
            ImGui.Separator();
            ImGui.Spacing();

            int tempTurnDelay = _turnDelay;

            if (ImGui.SliderInt("Turn Delay", ref tempTurnDelay, 0, 150, " %d ms"))
            {
                if (tempTurnDelay < 0) tempTurnDelay = 0;
                if (tempTurnDelay > ushort.MaxValue) tempTurnDelay = 100;

                _turnDelay = (ushort)tempTurnDelay;
                _profile.TurnDelay = _turnDelay;
            }

            ImGui.Spacing(); // Moderate space between controls

            if (ImGui.InputInt("Object Delay", ref _objectMoveDelay, 50, 100))
            {
                if (_objectMoveDelay < 0 || _objectMoveDelay > 1000)
                    _objectMoveDelay = 1000;
                _profile.MoveMultiObjectDelay = _objectMoveDelay;
            }
        }

        private readonly string _version = "TazUO Version: " + CUOEnviroment.Version; //Pre-cache to prevent reading var and string concatenation every frame
        private void DrawInfoTab()
        {
            ImGui.Text("Ping:");
            ImGui.Spacing();
            ImGui.Text("FPS:");
            ImGui.Spacing();
            ImGui.Text("Last Object:");
            ImGui.Spacing();
            ImGui.Text(_version);


            if (ImGui.Button("More Details"))
            {
                // Logic to show more information
            }
        }
    }
}
