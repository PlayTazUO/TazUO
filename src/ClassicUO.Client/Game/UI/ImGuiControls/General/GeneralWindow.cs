using System;
using System.Collections.Generic;
using ImGuiNET;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Network;

namespace ClassicUO.Game.UI.ImGuiControls
{
    public class GeneralWindow : SingletonImGuiWindow<GeneralWindow>
    {
        private readonly Profile _profile = ProfileManager.CurrentProfile;
        private int _objectMoveDelay;
        private bool _highlightObjects, _petScaling;
        private bool _showNames;
        private bool _autoOpenOwnCorpse;
        private bool _useLongDistancePathing;
        private ushort _turnDelay;
        private float _imguiWindowAlpha, _lastImguiWindowAlpha;
        private int _currentThemeIndex;
        private string[] _themeNames;
        private int _pathfindingGenerationTimeMs;
        private GeneralWindow() : base("General Tab")
        {
            WindowFlags = ImGuiWindowFlags.AlwaysAutoResize;
            _objectMoveDelay = _profile.MoveMultiObjectDelay;
            _highlightObjects = _profile.HighlightGameObjects;
            _showNames = _profile.NameOverheadToggled;
            _autoOpenOwnCorpse = _profile.AutoOpenOwnCorpse;
            _turnDelay = _profile.TurnDelay;
            _imguiWindowAlpha = _lastImguiWindowAlpha = Client.Settings.Get(SettingsScope.Global, Constants.SqlSettings.IMGUI_ALPHA, 1.0f);
            _useLongDistancePathing = World.Instance?.Player?.Pathfinder.UseLongDistancePathfinding ?? false;
            _pathfindingGenerationTimeMs = Client.Settings.Get(SettingsScope.Global, Constants.SqlSettings.LONG_DISTANCE_PATHING_SPEED, 2);
            _petScaling = _profile.EnablePetScaling;

            // Initialize theme selector
            _themeNames = ImGuiTheme.GetThemes();
            string currentTheme = Client.Settings.Get(SettingsScope.Global, Constants.SqlSettings.IMGUI_THEME, "Default");
            _currentThemeIndex = Array.IndexOf(_themeNames, currentTheme);
            if (_currentThemeIndex < 0) _currentThemeIndex = 0;
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

                if (ImGui.BeginTabItem("Pathfinding"))
                {
                    DrawPathfindingTab();
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
            ImGui.TextColored(ImGuiTheme.Current.BaseContent, "Visual Config");

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

            ImGui.SetNextItemWidth(125);
            if (ImGui.Combo("Theme", ref _currentThemeIndex, _themeNames, _themeNames.Length))
            {
                string selectedTheme = _themeNames[_currentThemeIndex];
                if (ImGuiTheme.SetTheme(selectedTheme))
                {
                    _ = Client.Settings.SetAsync(SettingsScope.Global, Constants.SqlSettings.IMGUI_THEME, selectedTheme);
                    ImGuiManager.UpdateTheme(_imguiWindowAlpha);
                }
            }
            ImGuiComponents.Tooltip("Select the color theme for ImGui windows.");

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

            if(ImGui.Button("Open Theme Editor"))
                ImGuiThemeEditorWindow.Show();

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

            if (ImGui.Checkbox("Enable pet scaling", ref _petScaling))
            {
                _profile.EnablePetScaling = _petScaling;

                Dictionary<uint, Mobile>.ValueCollection mobs = World.Instance.Mobiles.Values;
                foreach (Mobile mob in mobs)
                {
                    if (mob != null && mob.IsRenamable)
                        mob.Scale = _petScaling ? 0.6f : 1f;
                }
            }

            ImGui.EndGroup();

            ImGui.SameLine();

            // Group: Delay Config
            ImGui.BeginGroup();
            ImGui.AlignTextToFramePadding();
            ImGui.TextColored(ImGuiTheme.Current.BaseContent, "Delay Config");

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

        private void DrawPathfindingTab()
        {
            ImGui.BeginGroup();

            ImGui.SetNextItemWidth(150);
            if (ImGui.Checkbox("Long-Distance Pathfinding", ref _useLongDistancePathing))
            {
                World.Instance.Player.Pathfinder.UseLongDistancePathfinding = _useLongDistancePathing;
                Client.Settings.SetAsync(SettingsScope.Global, Constants.SqlSettings.USE_LONG_DISTANCE_PATHING,  _useLongDistancePathing);
            }
            ImGuiComponents.Tooltip("This is currently in beta.");


            ImGui.SetNextItemWidth(150);
            if (ImGui.SliderInt("Pathfinding Gen Time", ref _pathfindingGenerationTimeMs, 1, 50, "%d ms"))
            {
                _pathfindingGenerationTimeMs = Math.Clamp(_pathfindingGenerationTimeMs, 1, 50);
                Client.Settings?.SetAsync(SettingsScope.Global, Constants.SqlSettings.LONG_DISTANCE_PATHING_SPEED,  _pathfindingGenerationTimeMs);
                if (Managers.WalkableManager.Instance != null) Managers.WalkableManager.Instance.TARGET_GENERATION_TIME_MS = _pathfindingGenerationTimeMs;
            }
            ImGuiComponents.Tooltip("Target time in milliseconds for pathfinding cache generation per cycle. Higher values generate cache faster but may cause performance issues.");

            ImGui.SetNextItemWidth(150);
            if (ImGui.Button("Reset current map cache"))
            {
                WalkableManager.Instance?.StartFreshGeneration(World.Instance.MapIndex);
            }
            ImGuiComponents.Tooltip("This will start regeneration of the current map cache.");


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
            ClipboardOnClick(_lastObjectString);
            ImGui.Spacing();
            ImGui.Text(_version);
        }

    }
}
