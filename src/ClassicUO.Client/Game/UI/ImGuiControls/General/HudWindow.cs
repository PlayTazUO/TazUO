using ImGuiNET;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ClassicUO.Game.UI.ImGuiControls
{
    public class HudWindow : SingletonImGuiWindow<HudWindow>
    {
        private Profile profile;
        private Dictionary<HideHudFlags, bool> hudFlagStates;

        private HudWindow() : base("HUD Settings")
        {
            WindowFlags = ImGuiWindowFlags.AlwaysAutoResize;
            profile = ProfileManager.CurrentProfile;

            // Initialize HUD flag states
            hudFlagStates = new Dictionary<HideHudFlags, bool>();
            InitializeHudStates();
        }

        private void InitializeHudStates()
        {
            if (profile == null) return;

            foreach (HideHudFlags flag in Enum.GetValues(typeof(HideHudFlags)))
            {
                if (flag == HideHudFlags.None) continue;
                hudFlagStates[flag] = ByteFlagHelper.HasFlag(profile.HideHudGumpFlags, (ulong)flag);
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

            // Header text
            ImGui.TextWrapped("Check the types of gumps you would like to toggle visibility when using the Toggle Hud Visible macro.");
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            // Quick actions
            if (ImGui.Button("Select All"))
            {
                SetAllFlags(true);
            }
            ImGui.SameLine();
            if (ImGui.Button("Deselect All"))
            {
                SetAllFlags(false);
            }
            ImGui.SameLine();
            if (ImGui.Button("Toggle HUD Now"))
            {
                HideHudManager.ToggleHidden(profile.HideHudGumpFlags);
            }
            ImGuiComponents.Tooltip("Immediately toggle the visibility of selected HUD elements");

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            // Draw HUD options in a table format for better organization
            if (ImGui.BeginTable("HudOptionsTable", 2, ImGuiTableFlags.SizingStretchSame))
            {
                ImGui.TableSetupColumn("Column1", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Column2", ImGuiTableColumnFlags.WidthStretch);

                bool isFirstColumn = true;
                foreach (KeyValuePair<HideHudFlags, bool> kvp in hudFlagStates.ToList())
                {
                    HideHudFlags flag = kvp.Key;
                    if (flag == HideHudFlags.None) continue;

                    if (isFirstColumn)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                    }
                    else
                    {
                        ImGui.TableNextColumn();
                    }

                    bool currentState = kvp.Value;
                    string flagName = HideHudManager.GetFlagName(flag);

                    if (ImGui.Checkbox(flagName, ref currentState))
                    {
                        hudFlagStates[flag] = currentState;
                        UpdateProfileFlags(flag, currentState);

                        // Handle special case for "All" flag
                        if (flag == HideHudFlags.All)
                        {
                            if (currentState)
                            {
                                SetAllFlags(true);
                            }
                            else
                            {
                                // When "All" is unchecked, uncheck everything
                                SetAllFlags(false);
                            }
                        }
                    }

                    // Add tooltip for some flags
                    AddTooltipForFlag(flag);

                    isFirstColumn = !isFirstColumn;
                }

                ImGui.EndTable();
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            // Additional information
            if (ImGui.CollapsingHeader("Advanced"))
            {
                ImGui.Text($"Raw flag value: {profile.HideHudGumpFlags}");
                if (ImGui.Button("Reset to Default"))
                {
                    profile.HideHudGumpFlags = 0;
                    InitializeHudStates();
                }
                ImGuiComponents.Tooltip("Reset all HUD visibility settings to default (everything visible)");
            }
        }

        private void SetAllFlags(bool state)
        {
            foreach (HideHudFlags flag in hudFlagStates.Keys.ToList())
            {
                if (flag == HideHudFlags.None) continue;
                hudFlagStates[flag] = state;
                UpdateProfileFlags(flag, state);
            }
        }

        private void UpdateProfileFlags(HideHudFlags flag, bool enabled)
        {
            if (enabled)
            {
                profile.HideHudGumpFlags = ByteFlagHelper.AddFlag(profile.HideHudGumpFlags, (ulong)flag);
            }
            else
            {
                profile.HideHudGumpFlags = flag == HideHudFlags.All ? (ulong)0 : ByteFlagHelper.RemoveFlag(profile.HideHudGumpFlags, (ulong)flag);
            }
        }

        private void AddTooltipForFlag(HideHudFlags flag)
        {
            string tooltip = flag switch
            {
                HideHudFlags.Paperdoll => "Character paperdoll windows",
                HideHudFlags.WorldMap => "World map window",
                HideHudFlags.GridContainers => "Grid-style container windows",
                HideHudFlags.Containers => "Traditional container windows",
                HideHudFlags.Healthbars => "Health bar windows",
                HideHudFlags.StatusBar => "Character status windows",
                HideHudFlags.SpellBar => "Spell bar windows",
                HideHudFlags.Journal => "Journal/chat windows",
                HideHudFlags.XMLGumps => "Server-sent XML gump windows",
                HideHudFlags.NearbyCorpseLoot => "Nearby corpse loot windows",
                HideHudFlags.MacroButtons => "Macro button windows",
                HideHudFlags.SkillButtons => "Skill button windows",
                HideHudFlags.SkillsMenus => "Skills menu windows",
                HideHudFlags.TopMenuBar => "Top menu bar",
                HideHudFlags.DurabilityTracker => "Item durability tracker",
                HideHudFlags.BuffBar => "Buff/debuff status bars",
                HideHudFlags.CounterBar => "Item counter bars",
                HideHudFlags.InfoBar => "Information bars",
                HideHudFlags.SpellIcons => "Spell icon buttons",
                HideHudFlags.NameOverheadGump => "Name overhead displays",
                HideHudFlags.ScriptManagerGump => "Script manager window",
                HideHudFlags.PlayerChar => "Player character (your avatar in the game world)",
                HideHudFlags.Mouse => "Mouse cursor",
                HideHudFlags.All => "Select/deselect all HUD elements at once",
                _ => null
            };

            if (!string.IsNullOrEmpty(tooltip))
            {
                ImGuiComponents.Tooltip(tooltip);
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            hudFlagStates?.Clear();
        }
    }
}
