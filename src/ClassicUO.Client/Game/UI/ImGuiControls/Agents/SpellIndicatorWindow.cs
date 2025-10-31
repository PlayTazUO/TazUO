using ImGuiNET;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Data;
using ClassicUO.Utility;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;
using ClassicUO.Game.Managers.SpellVisualRange;

namespace ClassicUO.Game.UI.ImGuiControls
{
    public class SpellIndicatorWindow : SingletonImGuiWindow<SpellIndicatorWindow>
    {
        private Profile profile;
        private bool enableSpellIndicators;
        private string spellSearchInput = "";
        private Dictionary<int, string> spellNameInputs = new Dictionary<int, string>();
        private Dictionary<int, string> spellPowerWordsInputs = new Dictionary<int, string>();
        private Dictionary<int, string> spellCursorSizeInputs = new Dictionary<int, string>();
        private Dictionary<int, string> spellCastRangeInputs = new Dictionary<int, string>();
        private Dictionary<int, string> spellCastTimeInputs = new Dictionary<int, string>();
        private Dictionary<int, string> spellMaxDurationInputs = new Dictionary<int, string>();
        private SpellRangeInfo selectedSpell = null;

        private SpellIndicatorWindow() : base("Spell Indicators")
        {
            WindowFlags = ImGuiWindowFlags.AlwaysAutoResize;
            profile = ProfileManager.CurrentProfile;
            enableSpellIndicators = profile?.EnableSpellIndicators ?? false;
        }

        public override void DrawContent()
        {
            if (profile == null)
            {
                ImGui.Text("Profile not loaded");
                return;
            }

            ImGui.Spacing();

            if (ImGui.Checkbox("Enable Spell Indicators", ref enableSpellIndicators))
            {
                profile.EnableSpellIndicators = enableSpellIndicators;
            }
            ImGuiComponents.Tooltip("Enable visual spell range indicators that show casting range and area of effect for spells.");

            ImGui.SeparatorText("Spell Search:");

            ImGui.Text("Spell search:");
            ImGui.SetNextItemWidth(250);
            if (ImGui.InputText("##SpellSearch", ref spellSearchInput, 100))
            {
                if (string.IsNullOrWhiteSpace(spellSearchInput))
                {
                    selectedSpell = null;
                }
                else if (SpellDefinition.TryGetSpellFromName(spellSearchInput, out SpellDefinition spell))
                {
                    if (SpellVisualRangeManager.Instance.SpellRangeCache.TryGetValue(spell.ID, out SpellRangeInfo info))
                    {
                        selectedSpell = info;
                        InitializeInputs(info);
                    }
                }
                else
                {
                    selectedSpell = null;
                }
            }
            ImGui.SameLine();
            if (ImGui.Button("Clear"))
            {
                spellSearchInput = "";
                selectedSpell = null;
            }
            ImGuiComponents.Tooltip("Type a spell name to search and edit its spell indicator settings.");

            if (selectedSpell != null)
            {
                ImGui.SeparatorText("Spell Configuration:");
                DrawSpellEditor(selectedSpell);
            }
            else if (string.IsNullOrWhiteSpace(spellSearchInput))
            {
                ImGui.SeparatorText("All Spell Indicators:");
                DrawSpellTable();
            }
        }

        private void InitializeInputs(SpellRangeInfo spell)
        {
            if (!spellNameInputs.ContainsKey(spell.ID))
                spellNameInputs[spell.ID] = spell.Name;
            if (!spellPowerWordsInputs.ContainsKey(spell.ID))
                spellPowerWordsInputs[spell.ID] = spell.PowerWords ?? "";
            if (!spellCursorSizeInputs.ContainsKey(spell.ID))
                spellCursorSizeInputs[spell.ID] = spell.CursorSize.ToString();
            if (!spellCastRangeInputs.ContainsKey(spell.ID))
                spellCastRangeInputs[spell.ID] = spell.CastRange.ToString();
            if (!spellCastTimeInputs.ContainsKey(spell.ID))
                spellCastTimeInputs[spell.ID] = spell.CastTime.ToString();
            if (!spellMaxDurationInputs.ContainsKey(spell.ID))
                spellMaxDurationInputs[spell.ID] = spell.MaxDuration.ToString();
        }

        private void DrawSpellEditor(SpellRangeInfo spell)
        {
            InitializeInputs(spell);

            if (ImGui.BeginTable("SpellEditorTable", 2, ImGuiTableFlags.SizingFixedFit))
            {
                ImGui.TableSetupColumn("Property", ImGuiTableColumnFlags.WidthFixed, 120);
                ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

                // Name
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("Name:");
                ImGui.TableNextColumn();
                string nameStr = spellNameInputs[spell.ID];
                ImGui.SetNextItemWidth(200);
                if (ImGui.InputText($"##Name{spell.ID}", ref nameStr, 100))
                {
                    spellNameInputs[spell.ID] = nameStr;
                    spell.Name = nameStr;
                    SaveSpell();
                }

                // Power Words
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("Power Words:");
                ImGui.TableNextColumn();
                string powerWordsStr = spellPowerWordsInputs[spell.ID];
                ImGui.SetNextItemWidth(200);
                if (ImGui.InputText($"##PowerWords{spell.ID}", ref powerWordsStr, 100))
                {
                    spellPowerWordsInputs[spell.ID] = powerWordsStr;
                    spell.PowerWords = powerWordsStr;
                    SaveSpell();
                }
                ImGuiComponents.Tooltip("Power words must be exact, this is the best way we can detect spells.");

                // Cursor Size
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("Cursor Size:");
                ImGui.TableNextColumn();
                string cursorSizeStr = spellCursorSizeInputs[spell.ID];
                ImGui.SetNextItemWidth(100);
                if (ImGui.InputText($"##CursorSize{spell.ID}", ref cursorSizeStr, 10))
                {
                    spellCursorSizeInputs[spell.ID] = cursorSizeStr;
                    if (int.TryParse(cursorSizeStr, out int cursorSize))
                    {
                        spell.CursorSize = cursorSize;
                        SaveSpell();
                    }
                }
                ImGuiComponents.Tooltip("This is the area to show around the cursor, intended for area spells that affect the area near your target.");

                // Cast Range
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("Cast Range:");
                ImGui.TableNextColumn();
                string castRangeStr = spellCastRangeInputs[spell.ID];
                ImGui.SetNextItemWidth(100);
                if (ImGui.InputText($"##CastRange{spell.ID}", ref castRangeStr, 10))
                {
                    spellCastRangeInputs[spell.ID] = castRangeStr;
                    if (int.TryParse(castRangeStr, out int castRange))
                    {
                        spell.CastRange = castRange;
                        SaveSpell();
                    }
                }

                // Cast Time
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("Cast Time:");
                ImGui.TableNextColumn();
                string castTimeStr = spellCastTimeInputs[spell.ID];
                ImGui.SetNextItemWidth(100);
                if (ImGui.InputText($"##CastTime{spell.ID}", ref castTimeStr, 10))
                {
                    spellCastTimeInputs[spell.ID] = castTimeStr;
                    if (double.TryParse(castTimeStr, out double castTime))
                    {
                        spell.CastTime = castTime;
                        SaveSpell();
                    }
                }

                // Max Duration
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("Max Duration:");
                ImGui.TableNextColumn();
                string maxDurationStr = spellMaxDurationInputs[spell.ID];
                ImGui.SetNextItemWidth(100);
                if (ImGui.InputText($"##MaxDuration{spell.ID}", ref maxDurationStr, 10))
                {
                    spellMaxDurationInputs[spell.ID] = maxDurationStr;
                    if (int.TryParse(maxDurationStr, out int maxDuration))
                    {
                        spell.MaxDuration = maxDuration;
                        SaveSpell();
                    }
                }
                ImGuiComponents.Tooltip("This is a fallback in-case the spell detection fails.");

                // Cursor Hue
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("Cursor Hue:");
                ImGui.TableNextColumn();
                int cursorHueInt = spell.CursorHue;
                ImGui.SetNextItemWidth(100);
                if (ImGui.InputInt($"##CursorHue{spell.ID}", ref cursorHueInt, 1, 10))
                {
                    if (cursorHueInt >= 0 && cursorHueInt <= ushort.MaxValue)
                    {
                        spell.CursorHue = (ushort)cursorHueInt;
                        SaveSpell();
                    }
                }

                // Range Hue
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("Range Hue:");
                ImGui.TableNextColumn();
                int rangeHueInt = spell.Hue;
                ImGui.SetNextItemWidth(100);
                if (ImGui.InputInt($"##RangeHue{spell.ID}", ref rangeHueInt, 1, 10))
                {
                    if (rangeHueInt >= 0 && rangeHueInt <= ushort.MaxValue)
                    {
                        spell.Hue = (ushort)rangeHueInt;
                        SaveSpell();
                    }
                }

                // Checkboxes
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("Is Linear:");
                ImGui.TableNextColumn();
                bool isLinear = spell.IsLinear;
                if (ImGui.Checkbox($"##IsLinear{spell.ID}", ref isLinear))
                {
                    spell.IsLinear = isLinear;
                    SaveSpell();
                }
                ImGuiComponents.Tooltip("Used for spells like wall of stone that create a line.");

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("Show Range During Cast:");
                ImGui.TableNextColumn();
                bool showRange = spell.ShowCastRangeDuringCasting;
                if (ImGui.Checkbox($"##ShowRange{spell.ID}", ref showRange))
                {
                    spell.ShowCastRangeDuringCasting = showRange;
                    SaveSpell();
                }

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("Freeze While Casting:");
                ImGui.TableNextColumn();
                bool freezeWhileCasting = spell.FreezeCharacterWhileCasting;
                if (ImGui.Checkbox($"##FreezeWhileCasting{spell.ID}", ref freezeWhileCasting))
                {
                    spell.FreezeCharacterWhileCasting = freezeWhileCasting;
                    SaveSpell();
                }
                ImGuiComponents.Tooltip("Prevent yourself from moving and disrupting your spell.");

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("Expect Target Cursor:");
                ImGui.TableNextColumn();
                bool expectTargetCursor = spell.ExpectTargetCursor;
                if (ImGui.Checkbox($"##ExpectTargetCursor{spell.ID}", ref expectTargetCursor))
                {
                    spell.ExpectTargetCursor = expectTargetCursor;
                    SaveSpell();
                }

                ImGui.EndTable();
            }
        }

        private void DrawSpellTable()
        {
            var spells = SpellVisualRangeManager.Instance.SpellRangeCache.Values.OrderBy(s => s.Name).ToList();

            if (spells.Count == 0)
            {
                ImGui.Text("No spell indicators configured");
                return;
            }

            if (ImGui.BeginTable("SpellIndicatorTable", 6, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY, new Vector2(0, ImGuiTheme.Dimensions.STANDARD_TABLE_SCROLL_HEIGHT)))
            {
                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthFixed, 150);
                ImGui.TableSetupColumn("Power Words", ImGuiTableColumnFlags.WidthFixed, 120);
                ImGui.TableSetupColumn("Cast Range", ImGuiTableColumnFlags.WidthFixed, 80);
                ImGui.TableSetupColumn("Cursor Size", ImGuiTableColumnFlags.WidthFixed, 80);
                ImGui.TableSetupColumn("Cast Time", ImGuiTableColumnFlags.WidthFixed, 80);
                ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthFixed, 80);
                ImGui.TableHeadersRow();

                foreach (SpellRangeInfo spell in spells)
                {
                    ImGui.TableNextRow();

                    ImGui.TableNextColumn();
                    ImGui.Text(spell.Name);

                    ImGui.TableNextColumn();
                    ImGui.Text(spell.PowerWords ?? "");

                    ImGui.TableNextColumn();
                    ImGui.Text(spell.CastRange.ToString());

                    ImGui.TableNextColumn();
                    ImGui.Text(spell.CursorSize.ToString());

                    ImGui.TableNextColumn();
                    ImGui.Text(spell.CastTime.ToString("F1"));

                    ImGui.TableNextColumn();
                    if (ImGui.Button($"Edit##{spell.ID}"))
                    {
                        selectedSpell = spell;
                        spellSearchInput = spell.Name;
                        InitializeInputs(spell);
                    }
                }

                ImGui.EndTable();
            }
        }

        private void SaveSpell()
        {
            SpellVisualRangeManager.Instance.DelayedSave();
            if (selectedSpell != null)
                SpellVisualRangeManager.Instance.ReindexSpellPowerWords(selectedSpell);
        }
    }
}