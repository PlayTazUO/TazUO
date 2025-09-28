using ImGuiNET;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Utility;
using System;
using System.IO;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;

namespace ClassicUO.Game.UI.ImGuiControls
{
    public class AutoLootWindow : SingletonImGuiWindow<AutoLootWindow>
    {
        private Profile profile;
        private bool enableAutoLoot;
        private bool enableScavenger;
        private bool enableProgressBar;
        private bool autoLootHumanCorpses;

        private string newGraphicInput = "";
        private string newHueInput = "";
        private string newRegexInput = "";
        private int actionDelay = 1000;

        private List<AutoLootManager.AutoLootConfigEntry> lootEntries;
        private bool showAddEntry = false;
        private Dictionary<string, string> entryGraphicInputs = new Dictionary<string, string>();
        private Dictionary<string, string> entryHueInputs = new Dictionary<string, string>();
        private Dictionary<string, string> entryRegexInputs = new Dictionary<string, string>();
        private bool showCharacterImportPopup = false;

        private AutoLootWindow() : base("Auto Loot")
        {
            WindowFlags = ImGuiWindowFlags.AlwaysAutoResize;
            profile = ProfileManager.CurrentProfile;

            enableAutoLoot = profile.EnableAutoLoot;
            enableScavenger = profile.EnableScavenger;
            enableProgressBar = profile.EnableAutoLootProgressBar;
            autoLootHumanCorpses = profile.AutoLootHumanCorpses;
            actionDelay = profile.MoveMultiObjectDelay;

            lootEntries = AutoLootManager.Instance.AutoLootList;
        }

        public override void DrawContent()
        {
            if (profile == null)
            {
                ImGui.Text("Profile not loaded");
                return;
            }
            // Main settings
            ImGui.Spacing();
            if (ImGui.Checkbox("Enable Auto Loot", ref enableAutoLoot))
            {
                profile.EnableAutoLoot = enableAutoLoot;
            }
            ImGuiComponents.Tooltip("Auto Loot allows you to automatically pick up items from corpses based on configured criteria.");

            ImGui.SameLine();

            if (ImGui.Button("Set Grab Bag"))
            {
                GameActions.Print(Client.Game.UO.World, "Target container to grab items into");
                Client.Game.UO.World.TargetManager.SetTargeting(CursorTarget.SetGrabBag, 0, TargetType.Neutral);
            }
            ImGui.SameLine();

            ImGuiComponents.Tooltip("Choose a container to grab items into");

            ImGui.SeparatorText("Options:");

            if (ImGui.Checkbox("Enable Scavenger", ref enableScavenger))
            {
                profile.EnableScavenger = enableScavenger;
            }
            ImGui.SameLine();

            ImGuiComponents.Tooltip("Scavenger option allows to pick objects from ground.");

            if (ImGui.Checkbox("Enable progress bar", ref enableProgressBar))
            {
                profile.EnableAutoLootProgressBar = enableProgressBar;
            }
            ImGui.SameLine();

            ImGuiComponents.Tooltip("Shows a progress bar gump.");


            if (ImGui.Checkbox("Auto loot human corpses", ref autoLootHumanCorpses))
            {
                profile.AutoLootHumanCorpses = autoLootHumanCorpses;
            }
            ImGui.SameLine();

            ImGuiComponents.Tooltip("Auto loots human corpses.");

            // Buttons for grab bag and import/export
            ImGui.SeparatorText("Import & Export:");
            if (ImGui.Button("Export JSON"))
            {
                FileSelector.ShowFileBrowser(Client.Game.UO.World, FileSelectorType.Directory, null, null, (selectedPath) =>
                {
                    if (string.IsNullOrWhiteSpace(selectedPath)) return;
                    string fileName = $"AutoLoot_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                    string fullPath = Path.Combine(selectedPath, fileName);
                    AutoLootManager.Instance.ExportToFile(fullPath);
                }, "Export Autoloot Configuration");
            }

            ImGui.SameLine();
            if (ImGui.Button("Import JSON"))
            {
                FileSelector.ShowFileBrowser(Client.Game.UO.World, FileSelectorType.File, null, new[] { "json" }, (selectedFile) =>
                {
                    if (string.IsNullOrWhiteSpace(selectedFile)) return;
                    AutoLootManager.Instance.ImportFromFile(selectedFile);
                    // Clear input dictionaries to refresh with new data
                    entryGraphicInputs.Clear();
                    entryHueInputs.Clear();
                    entryRegexInputs.Clear();
                    lootEntries = AutoLootManager.Instance.AutoLootList;
                }, "Import Autoloot Configuration");
            }

            ImGui.SameLine();
            if (ImGui.Button("Import from Character"))
            {
                showCharacterImportPopup = true;
            }

            // Add entry section
            ImGui.SeparatorText("Entries:");

            if (ImGui.Button("Add Manual Entry"))
            {
                showAddEntry = !showAddEntry;
            }
            ImGui.SameLine();
            if (ImGui.Button("Add from Target"))
            {
                World.Instance.TargetManager.SetTargeting((targetedItem) =>
                {
                    if (targetedItem != null && targetedItem is Entity targetedEntity)
                    {
                        if (SerialHelper.IsItem(targetedEntity))
                        {
                            AutoLootManager.Instance.AddAutoLootEntry(targetedEntity.Graphic, targetedEntity.Hue, targetedEntity.Name);
                            lootEntries = AutoLootManager.Instance.AutoLootList;
                        }
                    }
                });
            }

            if (showAddEntry)
            {
                ImGui.SeparatorText("Add New Entry:");
                ImGui.Spacing();

                ImGui.BeginGroup();
                ImGui.AlignTextToFramePadding();
                ImGui.Text("Graphic:");
                ImGui.SameLine();
                ImGuiComponents.Tooltip("Item Graphic");
                ImGui.SetNextItemWidth(70);
                ImGui.InputText("##NewGraphic", ref newGraphicInput, 10);
                ImGui.EndGroup();

                ImGui.SameLine();

                ImGui.BeginGroup();
                ImGui.AlignTextToFramePadding();
                ImGui.Text("Hue:");
                ImGui.SameLine();

                ImGuiComponents.Tooltip("Set -1 to match any Hue");
                ImGui.SetNextItemWidth(70);
                ImGui.InputText("##NewHue", ref newHueInput, 10);
                ImGui.EndGroup();

                ImGui.Text("Regex:");
                ImGui.InputText("##NewRegex", ref newRegexInput, 500);

                ImGui.Spacing();

                if (ImGui.Button("Add##AddEntry"))
                {
                    if (StringHelper.TryParseGraphic(newGraphicInput, out int graphic))
                    {
                        ushort hue = ushort.MaxValue;
                        if (!string.IsNullOrEmpty(newHueInput) && newHueInput != "-1")
                        {
                            ushort.TryParse(newHueInput, out hue);
                        }

                        var entry = AutoLootManager.Instance.AddAutoLootEntry((ushort)graphic, hue, "");
                        entry.RegexSearch = newRegexInput;

                        newGraphicInput = "";
                        newHueInput = "";
                        newRegexInput = "";
                        showAddEntry = false;
                        lootEntries = AutoLootManager.Instance.AutoLootList;
                    }
                }
                ImGui.SameLine();
                if (ImGui.Button("Cancel##AddEntry"))
                {
                    showAddEntry = false;
                    newGraphicInput = "";
                    newHueInput = "";
                    newRegexInput = "";
                }
            }

            ImGui.SeparatorText("Current Auto Loot Entries:");
            // List of current entries

            if (lootEntries.Count == 0)
            {
                ImGui.Text("No entries configured");
            }
            else
            // Table headers
            if (ImGui.BeginTable("AutoLootTable", 5, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY, new Vector2(0, ImGuiTheme.Dimensions.STANDARD_TABLE_SCROLL_HEIGHT)))
            {
                ImGui.TableSetupColumn(string.Empty, ImGuiTableColumnFlags.WidthFixed, 52);
                ImGui.TableSetupColumn("Graphic", ImGuiTableColumnFlags.WidthFixed, ImGuiTheme.Dimensions.STANDARD_INPUT_WIDTH);
                ImGui.TableSetupColumn("Hue", ImGuiTableColumnFlags.WidthFixed, ImGuiTheme.Dimensions.STANDARD_INPUT_WIDTH);
                ImGui.TableSetupColumn("Regex", ImGuiTableColumnFlags.WidthFixed, 100);
                ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthFixed, ImGuiTheme.Dimensions.STANDARD_INPUT_WIDTH);
                ImGui.TableHeadersRow();

                for (int i = lootEntries.Count - 1; i >= 0; i--)
                {
                    var entry = lootEntries[i];
                    ImGui.TableNextRow();

                    ImGui.TableNextColumn();
                    if (!DrawArt((ushort)entry.Graphic, new Vector2(50, 50)))
                        ImGui.Text($"{entry.Graphic:X4}");

                    ImGui.TableNextColumn();
                    // Initialize input string if not exists
                    if (!entryGraphicInputs.ContainsKey(entry.UID))
                    {
                        entryGraphicInputs[entry.UID] = entry.Graphic.ToString();
                    }
                    string graphicStr = entryGraphicInputs[entry.UID];
                    if (ImGui.InputText($"##Graphic{i}", ref graphicStr, 10))
                    {
                        entryGraphicInputs[entry.UID] = graphicStr;
                        if (StringHelper.TryParseGraphic(graphicStr, out int newGraphic))
                        {
                            entry.Graphic = newGraphic;
                        }
                    }

                    ImGui.TableNextColumn();
                    // Initialize input string if not exists
                    if (!entryHueInputs.ContainsKey(entry.UID))
                    {
                        entryHueInputs[entry.UID] = entry.Hue == ushort.MaxValue ? "-1" : entry.Hue.ToString();
                    }
                    string hueStr = entryHueInputs[entry.UID];
                    if (ImGui.InputText($"##Hue{i}", ref hueStr, 10))
                    {
                        entryHueInputs[entry.UID] = hueStr;
                        if (hueStr == "-1")
                        {
                            entry.Hue = ushort.MaxValue;
                        }
                        else if (ushort.TryParse(hueStr, out ushort newHue))
                        {
                            entry.Hue = newHue;
                        }
                    }

                    ImGui.TableNextColumn();
                    // Initialize input string if not exists
                    if (!entryRegexInputs.ContainsKey(entry.UID))
                    {
                        entryRegexInputs[entry.UID] = entry.RegexSearch ?? "";
                    }
                    string regexStr = entryRegexInputs[entry.UID];


                    if (ImGui.Button($"Edit##{i}"))
                    {
                        ImGui.OpenPopup($"RegexEditor##{i}");
                    }

                    if (ImGui.BeginPopup($"RegexEditor##{i}"))
                    {
                        ImGui.TextColored(ImGuiTheme.Colors.Primary, "Regex Editor:");

                        if (ImGui.InputTextMultiline($"##Regex{i}", ref regexStr, 500, new Vector2(300, 100)))
                        {
                            entryRegexInputs[entry.UID] = regexStr;
                            entry.RegexSearch = regexStr;
                        }

                        if (ImGui.Button("Close"))
                            ImGui.CloseCurrentPopup();

                        ImGui.EndPopup();
                    }



                    ImGui.TableNextColumn();
                    if (ImGui.Button($"Delete##Delete{i}"))
                    {
                        AutoLootManager.Instance.TryRemoveAutoLootEntry(entry.UID);
                        // Clean up input dictionaries
                        entryGraphicInputs.Remove(entry.UID);
                        entryHueInputs.Remove(entry.UID);
                        entryRegexInputs.Remove(entry.UID);
                        lootEntries = AutoLootManager.Instance.AutoLootList;
                    }
                }

                ImGui.EndTable();
            }

            // Character import popup
            if (showCharacterImportPopup)
            {
                ImGui.OpenPopup("Import from Character");
                showCharacterImportPopup = false;
            }

            if (ImGui.BeginPopupModal("Import from Character"))
            {
                var otherConfigs = AutoLootManager.Instance.GetOtherCharacterConfigs();

                if (otherConfigs.Count == 0)
                {
                    ImGui.Text("No other character autoloot configurations found.");
                    if (ImGui.Button("OK"))
                    {
                        ImGui.CloseCurrentPopup();
                    }
                }
                else
                {
                    ImGui.Text("Select a character to import autoloot configuration from:");
                    ImGui.Separator();

                    foreach (var characterConfig in otherConfigs.OrderBy(c => c.Key))
                    {
                        string characterName = characterConfig.Key;
                        var configs = characterConfig.Value;

                        if (ImGui.Button($"{characterName} ({configs.Count} items)"))
                        {
                            AutoLootManager.Instance.ImportFromOtherCharacter(characterName, configs);
                            // Clear input dictionaries to refresh with new data
                            entryGraphicInputs.Clear();
                            entryHueInputs.Clear();
                            entryRegexInputs.Clear();
                            lootEntries = AutoLootManager.Instance.AutoLootList;
                            ImGui.CloseCurrentPopup();
                        }
                    }

                    ImGui.Separator();
                    if (ImGui.Button("Cancel"))
                    {
                        ImGui.CloseCurrentPopup();
                    }
                }

                ImGui.EndPopup();
            }
        }


    }
}
