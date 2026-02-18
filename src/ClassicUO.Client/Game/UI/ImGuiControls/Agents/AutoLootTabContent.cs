using System;
using ImGuiNET;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.GameObjects;
using ClassicUO.Utility;
using System.Numerics;
using System.Collections.Generic;

namespace ClassicUO.Game.UI.ImGuiControls
{
    public class AutoLootTabContent : TabContent
    {
        private Profile profile;
        private bool enableAutoLoot, enableScavenger, enableProgressBar, autoLootHumanCorpses, hueAfterProcess;

        private static readonly string[] _priorityNames = { "High", "Normal", "Low" };
        private static readonly AutoLootManager.AutoLootPriority[] _priorityValues = { AutoLootManager.AutoLootPriority.High, AutoLootManager.AutoLootPriority.Normal, AutoLootManager.AutoLootPriority.Low };

        private string newGraphicInput = "";
        private string newHueInput = "";
        private string newRegexInput = "";

        private AutoLootManager.AutoLootProfile _selectedProfile;
        private AutoLootManager.AutoLootProfile _contextMenuProfile;
        private int _selectedProfileIndex = -1;
        private int _draggedProfileIndex = -1;
        private bool showAddEntry = false;
        private Dictionary<string, string> entryGraphicInputs = new Dictionary<string, string>();
        private Dictionary<string, string> entryHueInputs = new Dictionary<string, string>();
        private Dictionary<string, string> entryRegexInputs = new Dictionary<string, string>();
        private Dictionary<string, string> entryDestinationInputs = new Dictionary<string, string>();
        private bool _showRenamePopup = false;
        private bool _showDeletePopup = false;
        private bool _showImportCharPopup = false;
        private bool _renamePopupOpen = false;
        private bool _renamePopupFocusInput = false;
        private string _renameInput = "";
        private Dictionary<string, List<AutoLootManager.AutoLootConfigEntry>> _otherCharConfigs;
        private static readonly string[] PriorityLabels = { "Low", "Normal", "High" };
        private string _profileDestInput = "";

        // Exclusion list UI state
        private bool _exclusionSelected = false;
        private Dictionary<string, string> _exclEntryGraphicInputs = new();
        private Dictionary<string, string> _exclEntryHueInputs = new();
        private Dictionary<string, string> _exclEntryRegexInputs = new();

        public AutoLootTabContent()
        {
            profile = ProfileManager.CurrentProfile;

            enableAutoLoot = profile.EnableAutoLoot;
            enableScavenger = profile.EnableScavenger;
            enableProgressBar = profile.EnableAutoLootProgressBar;
            autoLootHumanCorpses = profile.AutoLootHumanCorpses;
            hueAfterProcess = profile.HueCorpseAfterAutoloot;
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
                profile.EnableAutoLoot = enableAutoLoot;
            ImGuiComponents.Tooltip("Auto Loot allows you to automatically pick up items from corpses based on configured criteria.");

            ImGui.SameLine();

            if (ImGui.Button("Set Grab Bag"))
            {
                GameActions.Print(Client.Game.UO.World, "Target container to grab items into");
                Client.Game.UO.World.TargetManager.SetTargeting(CursorTarget.SetGrabBag, 0, TargetType.Neutral);
            }
            ImGuiComponents.Tooltip("Choose a container to grab items into");

            ImGui.SeparatorText("Options:");
            if (ImGui.Checkbox("Enable Scavenger", ref enableScavenger))
                profile.EnableScavenger = enableScavenger;
            ImGuiComponents.Tooltip("Scavenger option allows to pick objects from ground.");

            ImGui.SameLine();


            if (ImGui.Checkbox("Enable progress bar", ref enableProgressBar))
                profile.EnableAutoLootProgressBar = enableProgressBar;
            ImGuiComponents.Tooltip("Shows a progress bar gump.");


            if (ImGui.Checkbox("Auto loot human corpses", ref autoLootHumanCorpses))
                profile.AutoLootHumanCorpses = autoLootHumanCorpses;
            ImGuiComponents.Tooltip("Auto loots human corpses.");

            ImGui.SameLine();
            if (ImGui.Checkbox("Hue corpse after processing", ref hueAfterProcess))
                profile.HueCorpseAfterAutoloot = hueAfterProcess;
            ImGuiComponents.Tooltip("Hue corpses after processing to make it easier to see if autoloot has processed them.");

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            // Loading state check
            if (!AutoLootManager.Instance.Loaded && AutoLootManager.Instance.Profiles.Count == 0)
            {
                ImGui.Text("Loading...");
                return;
            }

            // Lazy-init: auto-select first profile when profiles become available
            if (_selectedProfile == null)
            {
                var profiles = AutoLootManager.Instance.Profiles;
                if (profiles.Count > 0)
                {
                    _selectedProfile = profiles[0];
                    _selectedProfileIndex = 0;
                    AutoLootManager.Instance.SelectedProfile = _selectedProfile;
                }
            }

            // 2-column layout: left = profile sidebar, right = entry table
            if (ImGui.BeginTable("AutoLootProfileTable", 2, ImGuiTableFlags.Resizable))
            {
                ImGui.TableSetupColumn("Profiles", ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableSetupColumn("Details", ImGuiTableColumnFlags.WidthStretch);

                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                DrawProfileSidebar();

                ImGui.TableSetColumnIndex(1);
                if (_exclusionSelected)
                    DrawExclusionEntryTable();
                else
                    DrawEntryTable();

                ImGui.EndTable();
            }

            // Rename profile popup
            if (_showRenamePopup)
            {
                ImGui.OpenPopup("Rename Profile");
                _showRenamePopup = false;
                _renamePopupOpen = true;
                _renamePopupFocusInput = true;
            }

            if (ImGui.BeginPopupModal("Rename Profile", ref _renamePopupOpen, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.Text("Enter new profile name:");
                ImGui.SetNextItemWidth(250);

                if (_renamePopupFocusInput)
                {
                    ImGui.SetKeyboardFocusHere();
                    _renamePopupFocusInput = false;
                }

                bool enterPressed = ImGui.InputText("##RenameInput", ref _renameInput, 128, ImGuiInputTextFlags.EnterReturnsTrue);

                if (ImGui.Button("OK") || enterPressed)
                {
                    if (_contextMenuProfile != null && !string.IsNullOrWhiteSpace(_renameInput))
                    {
                        AutoLootManager.Instance.RenameProfile(_contextMenuProfile, _renameInput);
                    }
                    ImGui.CloseCurrentPopup();
                }

                ImGui.SameLine();

                if (ImGui.Button("Cancel"))
                {
                    ImGui.CloseCurrentPopup();
                }

                ImGui.EndPopup();
            }

            // Delete profile confirmation popup
            if (_showDeletePopup)
            {
                ImGui.OpenPopup("Delete Profile");
                _showDeletePopup = false;
            }

            if (ImGui.BeginPopupModal("Delete Profile"))
            {
                ImGui.Text($"Delete profile '{_contextMenuProfile?.Name}'?");

                if (ImGui.Button("Confirm"))
                {
                    if (_contextMenuProfile != null)
                    {
                        bool wasSelected = _contextMenuProfile == _selectedProfile;
                        AutoLootManager.Instance.DeleteProfile(_contextMenuProfile);

                        if (wasSelected)
                        {
                            var profiles = AutoLootManager.Instance.Profiles;
                            if (profiles.Count > 0)
                            {
                                _selectedProfile = profiles[0];
                                _selectedProfileIndex = 0;
                                AutoLootManager.Instance.SelectedProfile = _selectedProfile;
                            }
                            else
                            {
                                _selectedProfile = null;
                                _selectedProfileIndex = -1;
                                AutoLootManager.Instance.SelectedProfile = null;
                            }

                            entryGraphicInputs.Clear();
                            entryHueInputs.Clear();
                            entryRegexInputs.Clear();
                            entryDestinationInputs.Clear();
                        }
                    }

                    ImGui.CloseCurrentPopup();
                }

                ImGui.SameLine();

                if (ImGui.Button("Cancel"))
                {
                    ImGui.CloseCurrentPopup();
                }

                ImGui.EndPopup();
            }

            // Import from Character popup
            if (_showImportCharPopup)
            {
                ImGui.OpenPopup("Import from Character");
                _showImportCharPopup = false;
            }

            if (ImGui.BeginPopupModal("Import from Character"))
            {
                ImGui.Text("This will create a new inactive profile with the imported entries.");
                ImGui.Spacing();

                if (_otherCharConfigs == null || _otherCharConfigs.Count == 0)
                {
                    ImGui.Text("No other characters with autoloot entries found.");
                }
                else
                {
                    ImGui.Text("Select a character to import from:");
                    ImGui.Separator();

                    foreach (var kvp in _otherCharConfigs)
                    {
                        if (ImGui.Selectable($"{kvp.Key} ({kvp.Value.Count} entries)"))
                        {
                            AutoLootManager.AutoLootProfile imported = AutoLootManager.Instance.ImportFromOtherCharacter(kvp.Key, kvp.Value);
                            if (imported != null)
                            {
                                int idx = AutoLootManager.Instance.Profiles.IndexOf(imported);
                                if (idx >= 0)
                                    SelectProfile(imported, idx);
                            }
                            ImGui.CloseCurrentPopup();
                            break;
                        }
                    }
                }

                ImGui.Spacing();
                if (ImGui.Button("Cancel"))
                {
                    ImGui.CloseCurrentPopup();
                }

                ImGui.EndPopup();
            }
        }

        private void DrawExclusionEntryTable()
        {
            ImGui.PushStyleColor(ImGuiCol.Text, ImGuiTheme.Current.Error);
            ImGui.SeparatorText("Exclusions");
            ImGui.PopStyleColor();
            ImGui.TextWrapped("Items matching these entries will never be looted or scavenged.");

            ImGui.Spacing();

            var exclusions = AutoLootManager.Instance.ExclusionList;

            if (ImGui.Button("Add Manual Entry##Excl"))
                showAddEntry = !showAddEntry;

            ImGui.SameLine();
            if (ImGui.Button("Add from Target##Excl"))
            {
                World.Instance.TargetManager.SetTargeting((targeted) =>
                {
                    if (targeted is Entity entity && SerialHelper.IsItem(entity))
                    {
                        AutoLootManager.Instance.AddExclusionEntry(entity.Graphic, entity.Hue, entity.Name);
                        GameActions.Print($"Added item to exclusion list.");
                    }
                });
            }

            if (showAddEntry)
            {
                ImGui.SeparatorText("Add New Exclusion:");
                ImGui.Spacing();

                ImGui.BeginGroup();
                ImGui.AlignTextToFramePadding();
                ImGui.Text("Graphic:");
                ImGui.SameLine();
                ImGuiComponents.Tooltip("Item Graphic");
                ImGui.SetNextItemWidth(70);
                ImGui.InputText("##ExclNewGraphic", ref newGraphicInput, 10);
                ImGui.EndGroup();

                ImGui.SameLine();

                ImGui.BeginGroup();
                ImGui.AlignTextToFramePadding();
                ImGui.Text("Hue:");
                ImGui.SameLine();
                ImGuiComponents.Tooltip("Set -1 to match any Hue");
                ImGui.SetNextItemWidth(70);
                ImGui.InputText("##ExclNewHue", ref newHueInput, 10);
                ImGui.EndGroup();

                ImGui.Text("Regex:");
                ImGui.InputText("##ExclNewRegex", ref newRegexInput, 500);

                ImGui.Spacing();
                if (ImGui.Button("Add##ExclAdd"))
                {
                    if (StringHelper.TryParseInt(newGraphicInput, out int graphic))
                    {
                        ushort hue = ushort.MaxValue;
                        if (!string.IsNullOrEmpty(newHueInput) && newHueInput != "-1")
                            ushort.TryParse(newHueInput, out hue);

                        var entry = AutoLootManager.Instance.AddExclusionEntry((ushort)graphic, hue);
                        if (entry != null && !string.IsNullOrEmpty(newRegexInput))
                        {
                            entry.RegexSearch = newRegexInput;
                            AutoLootManager.Instance.ClearMatchCache();
                        }

                        newGraphicInput = "";
                        newHueInput = "";
                        newRegexInput = "";
                        showAddEntry = false;
                    }
                }
                ImGui.SameLine();
                if (ImGui.Button("Cancel##ExclAdd"))
                {
                    showAddEntry = false;
                    newGraphicInput = "";
                    newHueInput = "";
                    newRegexInput = "";
                }
            }

            ImGui.SeparatorText("Current Exclusion Entries:");

            if (exclusions.Count == 0)
            {
                ImGui.Text("No exclusions configured.");
                return;
            }

            if (ImGui.BeginTable("ExclusionTable", 5, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY, new Vector2(0, ImGuiTheme.Dimensions.STANDARD_TABLE_SCROLL_HEIGHT)))
            {
                ImGui.TableSetupColumn(string.Empty, ImGuiTableColumnFlags.WidthFixed, 52);
                ImGui.TableSetupColumn("Graphic", ImGuiTableColumnFlags.WidthFixed, ImGuiTheme.Dimensions.STANDARD_INPUT_WIDTH);
                ImGui.TableSetupColumn("Hue", ImGuiTableColumnFlags.WidthFixed, ImGuiTheme.Dimensions.STANDARD_INPUT_WIDTH);
                ImGui.TableSetupColumn("Regex", ImGuiTableColumnFlags.WidthFixed, 60);
                ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthFixed, 60);
                ImGui.TableHeadersRow();

                for (int i = exclusions.Count - 1; i >= 0; i--)
                {
                    AutoLootManager.AutoLootConfigEntry entry = exclusions[i];
                    ImGui.TableNextRow();

                    ImGui.TableNextColumn();
                    if (!DrawArt((ushort)entry.Graphic, new Vector2(50, 50)))
                        ImGui.Text($"{entry.Graphic:X4}");
                    SetTooltip(entry.Name);

                    ImGui.TableNextColumn();
                    if (!_exclEntryGraphicInputs.ContainsKey(entry.Uid))
                        _exclEntryGraphicInputs[entry.Uid] = entry.Graphic.ToString();
                    string graphicStr = _exclEntryGraphicInputs[entry.Uid];
                    if (ImGui.InputText($"##ExclGraphic{i}", ref graphicStr, 10))
                    {
                        _exclEntryGraphicInputs[entry.Uid] = graphicStr;
                        if (StringHelper.TryParseInt(graphicStr, out int newGraphic))
                        {
                            entry.Graphic = newGraphic;
                            AutoLootManager.Instance.RebuildExclusionIndex();
                            AutoLootManager.Instance.ClearMatchCache();
                        }
                    }
                    SetTooltip("Set to -1 to match any graphic.");

                    ImGui.TableNextColumn();
                    if (!_exclEntryHueInputs.ContainsKey(entry.Uid))
                        _exclEntryHueInputs[entry.Uid] = entry.Hue == ushort.MaxValue ? "-1" : entry.Hue.ToString();
                    string hueStr = _exclEntryHueInputs[entry.Uid];
                    if (ImGui.InputText($"##ExclHue{i}", ref hueStr, 10))
                    {
                        _exclEntryHueInputs[entry.Uid] = hueStr;
                        if (hueStr == "-1")
                        {
                            entry.Hue = ushort.MaxValue;
                            AutoLootManager.Instance.ClearMatchCache();
                        }
                        else if (ushort.TryParse(hueStr, out ushort newHue))
                        {
                            entry.Hue = newHue;
                            AutoLootManager.Instance.ClearMatchCache();
                        }
                    }
                    SetTooltip("Set to -1 to match any hue.");

                    ImGui.TableNextColumn();
                    if (!_exclEntryRegexInputs.ContainsKey(entry.Uid))
                        _exclEntryRegexInputs[entry.Uid] = entry.RegexSearch ?? "";
                    string regexStr = _exclEntryRegexInputs[entry.Uid];

                    if (ImGui.Button($"Edit##Excl{i}"))
                        ImGui.OpenPopup($"ExclRegexEditor##{i}");

                    if (ImGui.BeginPopup($"ExclRegexEditor##{i}"))
                    {
                        ImGui.TextColored(ImGuiTheme.Current.Primary, "Regex Editor:");
                        if (ImGui.InputTextMultiline($"##ExclRegex{i}", ref regexStr, 500, new Vector2(300, 100)))
                        {
                            _exclEntryRegexInputs[entry.Uid] = regexStr;
                            entry.RegexSearch = regexStr;
                            AutoLootManager.Instance.ClearMatchCache();
                        }
                        if (ImGui.Button("Close"))
                            ImGui.CloseCurrentPopup();
                        ImGui.EndPopup();
                    }

                    ImGui.TableNextColumn();
                    if (ImGui.Button($"Delete##ExclDel{i}"))
                    {
                        AutoLootManager.Instance.RemoveExclusionEntry(entry.Uid);
                        _exclEntryGraphicInputs.Remove(entry.Uid);
                        _exclEntryHueInputs.Remove(entry.Uid);
                        _exclEntryRegexInputs.Remove(entry.Uid);
                    }
                }

                ImGui.EndTable();
            }
        }

        private void DrawProfileSidebar()
        {
            var profiles = AutoLootManager.Instance.Profiles;

            // Exclusion list — permanent entry, always at top
            ImGui.PushStyleColor(ImGuiCol.Text, ImGuiTheme.Current.Error);
            if (ImGui.Selectable("Exclusions", _exclusionSelected))
            {
                _exclusionSelected = true;
                _selectedProfile = null;
                _selectedProfileIndex = -1;
                entryGraphicInputs.Clear();
                entryHueInputs.Clear();
                entryRegexInputs.Clear();
                entryDestinationInputs.Clear();
            }
            ImGui.PopStyleColor();
            ImGuiComponents.Tooltip("Items matching these entries will never be looted or scavenged. This list cannot be renamed or deleted.");

            ImGui.Separator();

            // Profile list in scrollable region
            float availableHeight = ImGui.GetContentRegionAvail().Y - 60; // Reserve space for buttons below
            if (availableHeight < 50) availableHeight = 50;
            ImGui.BeginChild("ProfileList", new Vector2(0, availableHeight), ImGuiChildFlags.None);

            int dropTargetIndex = -1;

            for (int i = 0; i < profiles.Count; i++)
            {
                AutoLootManager.AutoLootProfile profile = profiles[i];
                bool isActive = profile.IsActive;

                if (ImGui.Checkbox($"##Active{i}", ref isActive))
                {
                    profile.IsActive = isActive;
                    AutoLootManager.Instance.SaveProfile(profile);
                    AutoLootManager.Instance.RebuildMergedList();
                }

                ImGui.SameLine();

                bool isSelected = _selectedProfile == profile;
                if (ImGui.Selectable(profile.Name + $"##Profile{i}", isSelected))
                {
                    SelectProfile(profile, i);
                }

                // Drag source
                if (ImGui.BeginDragDropSource(ImGuiDragDropFlags.None))
                {
                    _draggedProfileIndex = i;
                    unsafe
                    {
                        fixed (int* ptr = &_draggedProfileIndex)
                        {
                            ImGui.SetDragDropPayload("PROFILE", new IntPtr(ptr), sizeof(int));
                        }
                    }
                    ImGui.Text(profile.Name);
                    ImGui.EndDragDropSource();
                }

                // Drop target
                if (ImGui.BeginDragDropTarget())
                {
                    dropTargetIndex = i;
                    unsafe
                    {
                        ImGuiPayloadPtr payload = ImGui.AcceptDragDropPayload("PROFILE");
                        if (payload.NativePtr != null)
                        {
                            int fromIndex = *(int*)payload.Data;
                            if (fromIndex != i)
                            {
                                AutoLootManager.Instance.ReorderProfile(fromIndex, i);

                                if (_selectedProfile != null)
                                {
                                    _selectedProfileIndex = AutoLootManager.Instance.Profiles.IndexOf(_selectedProfile);
                                }
                            }
                        }
                    }
                    ImGui.EndDragDropTarget();
                }

                // Right-click context menu for profile
                if (ImGui.BeginPopupContextItem($"ProfileContext##{i}"))
                {
                    if (ImGui.MenuItem("Send to Organizer"))
                    {
                        SendProfileToOrganizer(profile);
                    }
                    ImGui.EndPopup();
                }

                // Visual feedback: draw a colored line at the drop target
                if (dropTargetIndex == i)
                {
                    ImDrawListPtr drawList = ImGui.GetWindowDrawList();
                    Vector2 itemMin = ImGui.GetItemRectMin();
                    Vector2 itemMax = ImGui.GetItemRectMax();
                    uint lineColor = ImGui.ColorConvertFloat4ToU32(ImGuiTheme.Current.Primary);
                    drawList.AddLine(
                        new Vector2(itemMin.X, itemMax.Y),
                        new Vector2(itemMax.X, itemMax.Y),
                        lineColor, 2.0f
                    );
                }
            }

            ImGui.EndChild();

            // Action buttons
            if (ImGui.Button("+"))
            {
                AutoLootManager.AutoLootProfile newProfile = AutoLootManager.Instance.CreateProfile("New Profile");
                SelectProfile(newProfile, profiles.Count - 1);
            }
            ImGuiComponents.Tooltip("New Profile");

            if (_selectedProfile != null && !_exclusionSelected)
            {
                ImGui.SameLine();
                if (ImGui.Button("Rename"))
                {
                    _contextMenuProfile = _selectedProfile;
                    _showRenamePopup = true;
                    _renameInput = _selectedProfile.Name;
                }

                ImGui.SameLine();
                bool canDelete = profiles.Count > 1;
                if (!canDelete) ImGui.BeginDisabled();
                if (ImGui.Button("X"))
                {
                    _contextMenuProfile = _selectedProfile;
                    _showDeletePopup = true;
                }
                ImGuiComponents.Tooltip("Delete Profile");
                if (!canDelete) ImGui.EndDisabled();
            }

            if (ImGui.Button("Import Character"))
            {
                _otherCharConfigs = AutoLootManager.Instance.GetOtherCharacterConfigs();
                _showImportCharPopup = true;
            }

            if (_selectedProfile != null && !_exclusionSelected)
            {
                ImGui.SameLine();
                if (ImGui.Button("Export"))
                {
                    string json = AutoLootManager.Instance.GetProfileJsonExport(_selectedProfile);
                    if (json != null)
                    {
                        json.CopyToClipboard();
                        GameActions.Print($"Exported profile '{_selectedProfile.Name}' to clipboard.", Constants.HUE_SUCCESS);
                    }
                }
                ImGuiComponents.Tooltip("Export to Clipboard");

                ImGui.SameLine();
                if (ImGui.Button("Paste"))
                {
                    string json = Clipboard.GetClipboardText();
                    AutoLootManager.AutoLootProfile imported = AutoLootManager.Instance.ImportProfileFromClipboard(json);
                    if (imported != null)
                    {
                        int idx = AutoLootManager.Instance.Profiles.IndexOf(imported);
                        SelectProfile(imported, idx);
                        GameActions.Print($"Imported profile '{imported.Name}' from clipboard.", Constants.HUE_SUCCESS);
                    }
                    else
                    {
                        GameActions.Print("Clipboard does not contain a valid autoloot profile or entry list.", Constants.HUE_ERROR);
                    }
                }
                ImGuiComponents.Tooltip("Import from Clipboard");
            }
        }

        private void DrawEntryContextMenu(AutoLootManager.AutoLootConfigEntry entry, int rowIndex)
        {
            if (ImGui.BeginPopupContextItem($"EntryContext##{rowIndex}"))
            {
                var profiles = AutoLootManager.Instance.Profiles;

                if (ImGui.BeginMenu("Move to"))
                {
                    foreach (var p in profiles)
                    {
                        if (p == _selectedProfile) continue;
                        if (ImGui.MenuItem(p.Name))
                        {
                            AutoLootManager.Instance.MoveEntryToProfile(entry, _selectedProfile, p);
                            entryGraphicInputs.Remove(entry.Uid);
                            entryHueInputs.Remove(entry.Uid);
                            entryRegexInputs.Remove(entry.Uid);
                            entryDestinationInputs.Remove(entry.Uid);
                        }
                    }
                    ImGui.EndMenu();
                }

                if (ImGui.BeginMenu("Copy to"))
                {
                    foreach (var p in profiles)
                    {
                        if (p == _selectedProfile) continue;
                        if (ImGui.MenuItem(p.Name))
                            AutoLootManager.Instance.CopyEntryToProfile(entry, p);
                    }
                    ImGui.EndMenu();
                }

                ImGui.EndPopup();
            }
        }

        private void SelectProfile(AutoLootManager.AutoLootProfile profile, int index)
        {
            _exclusionSelected = false;
            _selectedProfile = profile;
            _selectedProfileIndex = index;
            AutoLootManager.Instance.SelectedProfile = profile;

            entryGraphicInputs.Clear();
            entryHueInputs.Clear();
            entryRegexInputs.Clear();
            entryDestinationInputs.Clear();
        }

        private static void SendProfileToOrganizer(AutoLootManager.AutoLootProfile profile)
        {
            if (profile == null || profile.Entries.Count == 0)
            {
                GameActions.Print("Profile has no entries to send.", Constants.HUE_ERROR);
                return;
            }

            OrganizerConfig config = OrganizerAgent.Instance.NewOrganizerConfig();
            config.Name = $"From: {profile.Name}";
            config.DestContSerial = profile.DestinationContainer;

            foreach (AutoLootManager.AutoLootConfigEntry entry in profile.Entries)
            {
                // Skip wildcard entries (graphic -1) — organizer requires exact graphics
                if (entry.Graphic < 0) continue;

                OrganizerItemConfig itemConfig = config.NewItemConfig();
                itemConfig.Graphic = (ushort)entry.Graphic;
                itemConfig.Hue = entry.Hue;
                itemConfig.DestContSerial = entry.DestinationContainer;
            }

            OrganizerAgent.Instance.Save();
            GameActions.Print($"Created organizer '{config.Name}' with {config.ItemConfigs.Count} items from loot profile.", Constants.HUE_SUCCESS);
        }

        private void DrawEntryTable()
        {
            if (_selectedProfile == null)
            {
                ImGui.Text("Select a profile to view entries");
                return;
            }

            ImGui.SeparatorText($"Editing: {_selectedProfile.Name}");

            // Profile default destination bag
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Profile Bag:");
            ImGui.SameLine();
            ImGuiComponents.Tooltip("Default destination for all entries in this profile (overridden by per-entry destinations)");
            _profileDestInput = _selectedProfile.DestinationContainer == 0 ? "" : $"0x{_selectedProfile.DestinationContainer:X}";
            ImGui.SetNextItemWidth(90);
            if (ImGui.InputText("##ProfileDest", ref _profileDestInput, 20))
            {
                if (string.IsNullOrWhiteSpace(_profileDestInput))
                {
                    _selectedProfile.DestinationContainer = 0;
                    AutoLootManager.Instance.RebuildMergedList();
                    AutoLootManager.Instance.SaveProfile(_selectedProfile);
                }
                else if (uint.TryParse(_profileDestInput.Replace("0x", "").Replace("0X", ""), System.Globalization.NumberStyles.HexNumber, null, out uint destSerial))
                {
                    _selectedProfile.DestinationContainer = destSerial;
                    AutoLootManager.Instance.RebuildMergedList();
                    AutoLootManager.Instance.SaveProfile(_selectedProfile);
                }
            }
            ImGui.SameLine();
            if (ImGui.Button("Target##ProfileDest"))
            {
                AutoLootManager.AutoLootProfile profile = _selectedProfile;
                World.Instance.TargetManager.SetTargeting((targetedContainer) =>
                {
                    if (targetedContainer != null && targetedContainer is Entity targetedEntity && SerialHelper.IsItem(targetedEntity))
                    {
                        profile.DestinationContainer = targetedEntity.Serial;
                        AutoLootManager.Instance.RebuildMergedList();
                        AutoLootManager.Instance.SaveProfile(profile);
                    }
                });
            }
            ImGui.SameLine();
            if (ImGui.Button("Clear##ProfileDest"))
            {
                _selectedProfile.DestinationContainer = 0;
                AutoLootManager.Instance.RebuildMergedList();
                AutoLootManager.Instance.SaveProfile(_selectedProfile);
            }

            ImGui.Spacing();

            if (ImGui.Button("Add Manual Entry"))
            {
                showAddEntry = !showAddEntry;
            }
            ImGui.SameLine();
            if (ImGui.Button("Add from Target"))
            {
                AutoLootManager.AutoLootProfile targetProfile = _selectedProfile;
                World.Instance.TargetManager.SetTargeting((targetedItem) =>
                {
                    if (targetedItem != null && targetedItem is Entity targetedEntity)
                    {
                        if (SerialHelper.IsItem(targetedEntity))
                        {
                            var newEntry = new AutoLootManager.AutoLootConfigEntry
                            {
                                Graphic = targetedEntity.Graphic,
                                Hue = targetedEntity.Hue,
                                Name = targetedEntity.Name
                            };

                            foreach (AutoLootManager.AutoLootConfigEntry existing in targetProfile.Entries)
                                if (existing.Equals(newEntry))
                                    return;

                            targetProfile.Entries.Add(newEntry);
                            AutoLootManager.Instance.RebuildMergedList();
                            AutoLootManager.Instance.SaveProfile(targetProfile);
                        }
                    }
                });
            }
            ImGui.SameLine();
            if (ImGui.Button("Set All Destinations"))
            {
                AutoLootManager.AutoLootProfile destProfile = _selectedProfile;
                GameActions.Print(Client.Game.UO.World, "Target container to set as destination for all entries");
                World.Instance.TargetManager.SetTargeting((targetedContainer) =>
                {
                    if (targetedContainer != null && targetedContainer is Entity targetedEntity && SerialHelper.IsItem(targetedEntity))
                    {
                        foreach (var entry in destProfile.Entries)
                        {
                            entry.DestinationContainer = targetedEntity.Serial;
                            entryDestinationInputs[entry.Uid] = $"0x{targetedEntity.Serial:X}";
                        }
                        AutoLootManager.Instance.SaveProfile(destProfile);
                    }
                });
            }
            ImGuiComponents.Tooltip("Target a container to set as the destination for all entries in this profile");
            ImGui.SameLine();
            if (ImGui.Button("Clear All Destinations"))
            {
                foreach (var entry in _selectedProfile.Entries)
                {
                    entry.DestinationContainer = 0;
                    entryDestinationInputs[entry.Uid] = "";
                }
                AutoLootManager.Instance.SaveProfile(_selectedProfile);
            }
            ImGuiComponents.Tooltip("Clear the destination container for all entries in this profile (items will go to grab bag or backpack)");

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
                    if (StringHelper.TryParseInt(newGraphicInput, out int graphic))
                    {
                        ushort hue = ushort.MaxValue;
                        if (!string.IsNullOrEmpty(newHueInput) && newHueInput != "-1")
                        {
                            ushort.TryParse(newHueInput, out hue);
                        }

                        var newEntry = new AutoLootManager.AutoLootConfigEntry
                        {
                            Graphic = graphic,
                            Hue = hue,
                            RegexSearch = newRegexInput
                        };

                        bool isDuplicate = false;
                        foreach (AutoLootManager.AutoLootConfigEntry existing in _selectedProfile.Entries)
                            if (existing.Equals(newEntry))
                            {
                                isDuplicate = true;
                                break;
                            }

                        if (!isDuplicate)
                        {
                            _selectedProfile.Entries.Add(newEntry);
                            AutoLootManager.Instance.RebuildMergedList();
                            AutoLootManager.Instance.SaveProfile(_selectedProfile);
                        }

                        newGraphicInput = "";
                        newHueInput = "";
                        newRegexInput = "";
                        showAddEntry = false;
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

            if (_selectedProfile.Entries.Count == 0)
            {
                ImGui.Text("No entries configured");
            }
            else
            // Table headers
            if (ImGui.BeginTable("AutoLootTable", 8, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY, new Vector2(0, ImGuiTheme.Dimensions.STANDARD_TABLE_SCROLL_HEIGHT)))
            {
                ImGui.TableSetupColumn(string.Empty, ImGuiTableColumnFlags.WidthFixed, 52);
                ImGui.TableSetupColumn("Graphic", ImGuiTableColumnFlags.WidthFixed, ImGuiTheme.Dimensions.STANDARD_INPUT_WIDTH);
                ImGui.TableSetupColumn("Hue", ImGuiTableColumnFlags.WidthFixed, ImGuiTheme.Dimensions.STANDARD_INPUT_WIDTH);
                ImGui.TableSetupColumn("Priority", ImGuiTableColumnFlags.WidthFixed, 80);
                ImGui.TableSetupColumn("Scav", ImGuiTableColumnFlags.WidthFixed, 35);
                ImGui.TableSetupColumn("Regex", ImGuiTableColumnFlags.WidthFixed, 60);
                ImGui.TableSetupColumn("Destination", ImGuiTableColumnFlags.WidthFixed, 150);
                ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthFixed, 60);
                ImGui.TableHeadersRow();

                for (int i = _selectedProfile.Entries.Count - 1; i >= 0; i--)
                {
                    AutoLootManager.AutoLootConfigEntry entry = _selectedProfile.Entries[i];
                    ImGui.TableNextRow();

                    // Invisible selectable for row-level right-click context menu
                    ImGui.TableNextColumn();
                    ImGui.Selectable($"##RowCtx{i}", false, ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowOverlap);
                    DrawEntryContextMenu(entry, i);
                    ImGui.SameLine();

                    if (!DrawArt((ushort)entry.Graphic, new Vector2(50, 50)))
                        ImGui.Text($"{entry.Graphic:X4}");
                    SetTooltip(entry.Name);

                    ImGui.TableNextColumn();
                    // Initialize input string if not exists
                    if (!entryGraphicInputs.ContainsKey(entry.Uid))
                    {
                        entryGraphicInputs[entry.Uid] = entry.Graphic.ToString();
                    }
                    string graphicStr = entryGraphicInputs[entry.Uid];
                    if (ImGui.InputText($"##Graphic{i}", ref graphicStr, 10))
                    {
                        entryGraphicInputs[entry.Uid] = graphicStr;
                        if (StringHelper.TryParseInt(graphicStr, out int newGraphic))
                        {
                            entry.Graphic = newGraphic;
                            AutoLootManager.Instance.NotifyEntryChanged();
                        }
                    }
                    SetTooltip("Set to -1 to match any graphic.");

                    ImGui.TableNextColumn();
                    // Initialize input string if not exists
                    if (!entryHueInputs.ContainsKey(entry.Uid))
                    {
                        entryHueInputs[entry.Uid] = entry.Hue == ushort.MaxValue ? "-1" : entry.Hue.ToString();
                    }
                    string hueStr = entryHueInputs[entry.Uid];
                    if (ImGui.InputText($"##Hue{i}", ref hueStr, 10))
                    {
                        entryHueInputs[entry.Uid] = hueStr;
                        if (hueStr == "-1")
                        {
                            entry.Hue = ushort.MaxValue;
                            AutoLootManager.Instance.NotifyMatchCriteriaChanged();
                        }
                        else if (ushort.TryParse(hueStr, out ushort newHue))
                        {
                            entry.Hue = newHue;
                            AutoLootManager.Instance.NotifyMatchCriteriaChanged();
                        }
                    }
                    SetTooltip("Set to -1 to match any hue.");

                    ImGui.TableNextColumn();
                    string[] priorityNames = _priorityNames;
                    AutoLootManager.AutoLootPriority[] priorityValues = _priorityValues;
                    int currentPriorityIndex = System.Array.IndexOf(priorityValues, entry.Priority);
                    if (currentPriorityIndex < 0) currentPriorityIndex = 1; // Default to Normal
                    ImGui.SetNextItemWidth(75);
                    if (ImGui.Combo($"##Priority{i}", ref currentPriorityIndex, priorityNames, priorityNames.Length))
                    {
                        entry.Priority = priorityValues[currentPriorityIndex];
                        AutoLootManager.Instance.NotifyMatchCriteriaChanged();
                    }

                    ImGui.TableNextColumn();
                    bool scavenge = entry.Scavenge;
                    if (ImGui.Checkbox($"##Scav{i}", ref scavenge))
                    {
                        entry.Scavenge = scavenge;
                        AutoLootManager.Instance.NotifyMatchCriteriaChanged();
                        AutoLootManager.Instance.SaveProfile(_selectedProfile);
                    }
                    ImGuiComponents.Tooltip("When unchecked, this entry only applies to corpse looting and won't be used by the scavenger for ground items.");

                    ImGui.TableNextColumn();
                    // Initialize input string if not exists
                    if (!entryRegexInputs.ContainsKey(entry.Uid))
                    {
                        entryRegexInputs[entry.Uid] = entry.RegexSearch ?? "";
                    }
                    string regexStr = entryRegexInputs[entry.Uid];


                    if (ImGui.Button($"Edit##{i}"))
                    {
                        ImGui.OpenPopup($"RegexEditor##{i}");
                    }

                    if (ImGui.BeginPopup($"RegexEditor##{i}"))
                    {
                        ImGui.TextColored(ImGuiTheme.Current.Primary, "Regex Editor:");

                        if (ImGui.InputTextMultiline($"##Regex{i}", ref regexStr, 500, new Vector2(300, 100)))
                        {
                            entryRegexInputs[entry.Uid] = regexStr;
                            entry.RegexSearch = regexStr;
                            AutoLootManager.Instance.NotifyMatchCriteriaChanged();
                        }

                        if (ImGui.Button("Close"))
                            ImGui.CloseCurrentPopup();

                        ImGui.EndPopup();
                    }

                    ImGui.TableNextColumn();
                    // Initialize input string if not exists
                    if (!entryDestinationInputs.ContainsKey(entry.Uid))
                    {
                        entryDestinationInputs[entry.Uid] = entry.DestinationContainer == 0 ? "" : $"0x{entry.DestinationContainer:X}";
                    }
                    string destStr = entryDestinationInputs[entry.Uid];
                    ImGui.SetNextItemWidth(80);
                    if (ImGui.InputText($"##Dest{i}", ref destStr, 20))
                    {
                        entryDestinationInputs[entry.Uid] = destStr;
                        if (string.IsNullOrWhiteSpace(destStr))
                        {
                            entry.DestinationContainer = 0;
                        }
                        else if (uint.TryParse(destStr.Replace("0x", "").Replace("0X", ""), System.Globalization.NumberStyles.HexNumber, null, out uint destSerial))
                        {
                            entry.DestinationContainer = destSerial;
                        }
                    }
                    ImGui.SameLine();
                    if (ImGui.Button($"Target##Dest{i}"))
                    {
                        World.Instance.TargetManager.SetTargeting((targetedContainer) =>
                        {
                            if (targetedContainer != null && targetedContainer is Entity targetedEntity)
                            {
                                if (SerialHelper.IsItem(targetedEntity))
                                {
                                    entry.DestinationContainer = targetedEntity.Serial;
                                    entryDestinationInputs[entry.Uid] = $"0x{targetedEntity.Serial:X}";
                                }
                            }
                        });
                    }

                    ImGui.TableNextColumn();
                    if (ImGui.Button($"Delete##Delete{i}"))
                    {
                        _selectedProfile.Entries.Remove(entry);
                        AutoLootManager.Instance.RebuildMergedList();
                        AutoLootManager.Instance.SaveProfile(_selectedProfile);
                        // Clean up input dictionaries
                        entryGraphicInputs.Remove(entry.Uid);
                        entryHueInputs.Remove(entry.Uid);
                        entryRegexInputs.Remove(entry.Uid);
                        entryDestinationInputs.Remove(entry.Uid);
                    }

                }

                ImGui.EndTable();
            }
        }
    }
}
