using System;
using ImGuiNET;
using ClassicUO.Configuration;
using ClassicUO.Network;

namespace ClassicUO.Game.UI.ImGuiControls
{
    public class GeneralWindow : SingletonImGuiWindow<GeneralWindow>
    {
        private readonly Profile _profile = ProfileManager.CurrentProfile;
        private int _objectMoveDelay;
        private bool _highlightObjects;
        private bool _showNames;
        private bool _autoOpenOwnCorpse;
        private ushort _turnDelay;
        private float _imguiWindowAlpha, _lastImguiWindowAlpha;
        private GeneralWindow() : base("General Tab")
        {
            WindowFlags = ImGuiWindowFlags.AlwaysAutoResize;
            _objectMoveDelay = _profile.MoveMultiObjectDelay;
            _highlightObjects = _profile.HighlightGameObjects;
            _showNames = _profile.NameOverheadToggled;
            _autoOpenOwnCorpse = _profile.AutoOpenOwnCorpse;
            _turnDelay = _profile.TurnDelay;
            _imguiWindowAlpha = _lastImguiWindowAlpha = Client.Settings.Get(SettingsScope.Global, Constants.SqlSettings.IMGUI_ALPHA, 1.0f);
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
                    HudWindow.GetInstance()?.DrawContent();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Spell Bar"))
                {
                    SpellBarWindow.GetInstance()?.DrawContent();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Title Bar"))
                {
                    TitleBarWindow.GetInstance()?.DrawContent();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Spell Indicators"))
                {
                    SpellIndicatorWindow.GetInstance()?.DrawContent();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Friends List"))
                {
                    FriendsListWindow.GetInstance()?.DrawContent();
                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }
        }


        private void DrawOptionsTab()
        {
            // Group: Visual Config
            ImGui.BeginGroup();
            ImGui.AlignTextToFramePadding();
            ImGui.TextColored(ImGuiTheme.Colors.BaseContent, "Visual Config");

            ImGui.SetNextItemWidth(125);
            if (ImGui.SliderFloat("Assistant Alpha", ref _imguiWindowAlpha, 0.2f, 1.0f, "%.2f"))
            {
                if(Math.Abs(_imguiWindowAlpha - _lastImguiWindowAlpha) > 0.05)
                {
                    _imguiWindowAlpha = Math.Clamp(_imguiWindowAlpha, 0.2f, 1.0f);
                    _ = Client.Settings.SetAsync(SettingsScope.Global, Constants.SqlSettings.IMGUI_ALPHA, _imguiWindowAlpha);
                    ImGuiManager.UpdateTheme(_imguiWindowAlpha);
                    _lastImguiWindowAlpha = _imguiWindowAlpha;
                }
            }
            ImGuiComponents.Tooltip("Adjust the background transparency of all ImGui windows.");

            if (ImGui.Checkbox("Highlight game objects", ref _highlightObjects))
            {
                _profile.HighlightGameObjects = _highlightObjects;
            }

            if (ImGui.Checkbox("Show Names", ref _showNames))
            {
                _profile.NameOverheadToggled = _showNames;
            }
            ImGuiComponents.Tooltip("Toggle the display of names above characters and NPCs in the game world.");

            if (ImGui.Checkbox("Auto open own corpse", ref _autoOpenOwnCorpse))
            {
                _profile.AutoOpenOwnCorpse = _autoOpenOwnCorpse;
            }
            ImGuiComponents.Tooltip("Automatically open your own corpse when you die, even if auto open corpses is disabled.");

            ImGui.EndGroup();

            ImGui.SameLine();

            // Group: Delay Config
            ImGui.BeginGroup();
            ImGui.AlignTextToFramePadding();
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
                _objectMoveDelay = Math.Clamp(_objectMoveDelay, 0, 3000);

                _profile.MoveMultiObjectDelay = _objectMoveDelay;
            }
            ImGui.EndGroup();
        }

        private readonly string _version = "TazUO Version: " + CUOEnviroment.Version; //Pre-cache to prevent reading var and string concatenation every frame
        private uint _lastObject = 0;
        private string _lastObjectString = "Last Object: 0x00000000";
        private void DrawInfoTab()
        {
            if (World.Instance != null)
            {
                if (_lastObject != World.Instance.LastObject)
                {
                    _lastObject = World.Instance.LastObject;
                    _lastObjectString = $"Last Object: 0x{_lastObject:X8}";
                }
            }

            ImGui.Text("Ping: " + AsyncNetClient.Socket.Statistics.Ping + "ms");
            ImGui.Spacing();
            ImGui.Text("FPS: " + CUOEnviroment.CurrentRefreshRate);
            ImGui.Spacing();
            ImGui.Text(_lastObjectString);
            if(ImGui.IsItemClicked())
            {
                SDL3.SDL.SDL_SetClipboardText($"0x{_lastObject:X8}");
                GameActions.Print("Copied last object to clipboard.", 62);
            }
            ImGui.Spacing();
            ImGui.Text(_version);
        }

    }
}
