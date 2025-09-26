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
        private GeneralWindow() : base("General Tab")
        {
            WindowFlags = ImGuiWindowFlags.AlwaysAutoResize;
            _objectMoveDelay = _profile.MoveMultiObjectDelay;
            _highlightObjects = _profile.HighlightGameObjects;
            _showNames = _profile.NameOverheadToggled;
            _turnDelay = _profile.TurnDelay;
        }

        public override void DrawContent()
        {
            if (_profile == null)
            {
                ImGui.Text("Profile not loaded");
                return;
            }

            ImGui.Spacing();

            if (ImGui.BeginTabBar("##GeneralTabs", ImGuiTabBarFlags.None))
            {
                if (ImGui.BeginTabItem("Options"))
                {
                    DrawOptionsTab();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Info"))
                {
                    DrawInfoTab();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("HUD"))
                {
                    ImGui.Text("HUD Settings will go here.");
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Journal Filter"))
                {
                    ImGui.Text("Journal Filter Settings will go here.");
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Graphics"))
                {
                    GraphicReplacementWindow.GetInstance()?.DrawContent();
                    ImGui.EndTabItem();
                }
                ImGui.EndTabBar();
            }
        }


        private void DrawOptionsTab()
        {
            // Group: Visual Config
            ImGui.BeginGroup();
            ImGui.Spacing();
            ImGui.TextColored(ImGuiTheme.Colors.BaseContent, "Visual Config");

            if (ImGui.Checkbox("Highlight game objects", ref _highlightObjects))
            {
                _profile.HighlightGameObjects = _highlightObjects;
            }

            if (ImGui.Checkbox("Show Names", ref _showNames))
            {
                _profile.NameOverheadToggled = _showNames;
            }

            ImGui.SameLine();
            ImGui.TextDisabled("(?)");
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Toggle the display of names above characters and NPCs in the game world.");
            ImGui.EndGroup();

            ImGui.SameLine();

            // Group: Delay Config
            ImGui.BeginGroup();
            ImGui.TextColored(ImGuiTheme.Colors.BaseContent, "Delay Config");

            int tempTurnDelay = _turnDelay;

            ImGui.SetNextItemWidth(150);
            if (ImGui.SliderInt("Turn Delay", ref tempTurnDelay, 0, 150, " %d ms"))
            {
                if (tempTurnDelay < 0) tempTurnDelay = 0;
                if (tempTurnDelay > ushort.MaxValue) tempTurnDelay = 100;

                _turnDelay = (ushort)tempTurnDelay;
                _profile.TurnDelay = _turnDelay;
            }
            ImGui.SetNextItemWidth(150);
            if (ImGui.InputInt("Object Delay", ref _objectMoveDelay, 50, 100))
            {
                if (_objectMoveDelay < 0 || _objectMoveDelay > 1000)
                    _objectMoveDelay = 1000;

                _profile.MoveMultiObjectDelay = _objectMoveDelay;
            }
            ImGui.EndGroup();
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
        }

    }
}
