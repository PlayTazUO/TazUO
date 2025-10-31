using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Game.UI.ImGuiControls.Legion;
using ClassicUO.Input;
using ClassicUO.LegionScripting;
using ClassicUO.Utility.Logging;
using ImGuiNET;
using Vector2 = System.Numerics.Vector2;
using Vector4 = System.Numerics.Vector4;

namespace ClassicUO.Game.UI.ImGuiControls
{
    public class ScriptManagerWindow : SingletonImGuiWindow<ScriptManagerWindow>
    {
        private readonly HashSet<string> _collapsedGroups = new HashSet<string>();
        private bool _showContextMenu = false;
        private string _contextMenuGroup = "";
        private string _contextMenuSubGroup = "";
        private ScriptFile _contextMenuScript = null;
        private Vector2 _contextMenuPosition;
        private bool _pendingReload = false;
        private bool _shouldCancelRename = false;

        private const string SCRIPT_HEADER =
            "# See examples at" +
            "\n#   https://github.com/PlayTazUO/PublicLegionScripts/" +
            "\n# Or documentation at" +
            "\n#   https://tazuo.org/legion/api/";

        private const string EXAMPLE_LSCRIPT =
            SCRIPT_HEADER +
            @"
player = API.Player
delay = 8
diffhits = 10

while True:
    if player.HitsMax - player.Hits > diffhits or player.IsPoisoned:
        if API.BandageSelf():
            API.CreateCooldownBar(delay, 'Bandaging...', 21)
            API.Pause(delay)
        else:
            API.SysMsg('WARNING: No bandages!', 32)
            break
    API.Pause(0.5)";

        private const string NOGROUPTEXT = "No group";

        // Helper classes for cleaner state management

        private class RenameState
        {
            public bool IsRenaming => Script != null || !string.IsNullOrEmpty(GroupName);
            public ScriptFile Script { get; set; }
            public string GroupName { get; set; }
            public string GroupParent { get; set; }
            public string Buffer { get; set; } = "";

            public void StartScriptRename(ScriptFile script, string initialName)
            {
                Clear();
                Script = script;
                Buffer = initialName;
            }

            public void StartGroupRename(string groupName, string parentGroup)
            {
                Clear();
                GroupName = groupName;
                GroupParent = parentGroup;
                Buffer = groupName;
            }

            public void Clear()
            {
                Script = null;
                GroupName = "";
                GroupParent = "";
                Buffer = "";
            }
        }

        private class DialogState
        {
            public bool ShowNewScript { get; set; }
            public bool ShowNewGroup { get; set; }
            public bool ShowRenameGroup { get; set; }
            public bool ShowDeleteConfirm { get; set; }

            public string NewScriptName { get; set; } = "";
            public string NewGroupName { get; set; } = "";

            public string DeleteTitle { get; set; } = "";
            public string DeleteMessage { get; set; } = "";
            public ScriptFile ScriptToDelete { get; set; }
            public string GroupToDelete { get; set; } = "";
            public string GroupToDeleteParent { get; set; } = "";

            public void ClearAll()
            {
                ShowNewScript = false;
                ShowNewGroup = false;
                ShowRenameGroup = false;
                ShowDeleteConfirm = false;
                NewScriptName = "";
                NewGroupName = "";
                DeleteTitle = "";
                DeleteMessage = "";
                ScriptToDelete = null;
                GroupToDelete = "";
                GroupToDeleteParent = "";
            }

            public void ShowScriptDeleteDialog(ScriptFile script)
            {
                ScriptToDelete = script;
                GroupToDelete = "";
                GroupToDeleteParent = "";
                DeleteTitle = "Delete Script";
                DeleteMessage = $"Are you sure you want to delete '{script.FileName}'?\n\nThis action cannot be undone.";
                ShowDeleteConfirm = true;
            }

            public void ShowGroupDeleteDialog(string groupName, string parentGroup)
            {
                ScriptToDelete = null;
                GroupToDelete = groupName;
                GroupToDeleteParent = parentGroup;
                DeleteTitle = "Delete Group";
                DeleteMessage = $"Are you sure you want to delete the group '{groupName}'?\n\nThis will permanently delete the folder and ALL scripts inside it.\nThis action cannot be undone.";
                ShowDeleteConfirm = true;
            }
        }

        private readonly RenameState _renameState = new RenameState();
        private readonly DialogState _dialogState = new DialogState();

        private ScriptManagerWindow() : base("Script Manager")
        {
            WindowFlags = ImGuiWindowFlags.None;
            _pendingReload = true;
        }

        public override void DrawContent()
        {
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 0);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(6, 6));
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(4, 4));
            ImGui.PushStyleVar(ImGuiStyleVar.PopupRounding, 0);
            ImGui.PushStyleVar(ImGuiStyleVar.PopupBorderSize, 1.0f);
            ImGui.PushStyleColor(ImGuiCol.HeaderHovered, ImGuiTheme.Colors.Primary * 0.8f);
            ImGui.PushStyleColor(ImGuiCol.HeaderActive, ImGuiTheme.Colors.Primary);
            ImGui.PushStyleColor(ImGuiCol.Header, ImGuiTheme.Colors.Primary);

            // Load scripts if needed
            if (_pendingReload)
            {
                LegionScripting.LegionScripting.LoadScriptsFromFile();
                _pendingReload = false;
            }

            // Cancel rename if user clicks outside (but give buttons priority)
            if (_renameState.IsRenaming && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                // Check if we clicked outside the rename input area
                // We need to set a flag and check it after the input is drawn
                _shouldCancelRename = true;
            }

            // Top menu bar - fixed at the top, not affected by scrolling
            DrawMenuBar();
            ImGui.SeparatorText("Scripts");
            ImGui.Spacing();
            // Create a scrollable child region for the script groups
            Vector2 contentRegionAvail = ImGui.GetContentRegionAvail();

            if (ImGui.BeginChild("ScriptGroupsScrollable", new Vector2(contentRegionAvail.X, contentRegionAvail.Y), ImGuiChildFlags.None, ImGuiWindowFlags.None))
            {
                // Organize scripts by groups
                Dictionary<string, Dictionary<string, List<ScriptFile>>> groupsMap = OrganizeScripts();

                // Draw script groups within the scrollable area
                DrawScriptGroups(groupsMap);
            }
            ImGui.EndChild();

            // Handle context menus and dialogs
            DrawContextMenus();
            DrawDialogs();

            // Reset cancel rename flag if it wasn't used
            _shouldCancelRename = false;

            ImGui.PopStyleColor(3);
            ImGui.PopStyleVar(5);
        }

        private void DrawMenuBar()
        {
            if (ImGui.Button("Menu"))
            {
                ImGui.OpenPopup("ScriptManagerMenu");

            }
            ImGui.SameLine();
            if (ImGui.Button("Add +"))
            {
                _showContextMenu = true;
                _contextMenuGroup = ""; // Root level
                _contextMenuSubGroup = NOGROUPTEXT; // This will show both "New Script" and "New Group" options
                _contextMenuScript = null;
                _contextMenuPosition = ImGui.GetMousePos();
            }

            if (ImGui.BeginPopup("ScriptManagerMenu"))
            {
                if (ImGui.MenuItem("Refresh"))
                {
                    _pendingReload = true;
                }

                if (ImGui.MenuItem("Public Script Browser"))
                {
                    ScriptBrowser.Show();
                }

                if (ImGui.MenuItem("Script Recording"))
                {
                    UIManager.Add(new ScriptRecordingGump());
                }

                if (ImGui.MenuItem("Scripting Info"))
                {
                    ScriptingInfoGump.Show();
                }

                if (ImGui.MenuItem("Persistent Variables"))
                {
                    PersistentVarsWindow.Show();
                }

                if (ImGui.MenuItem("Running Scripts"))
                {
                    RunningScriptsWindow.Show();
                }

                bool disableCache = LegionScripting.LegionScripting.LScriptSettings.DisableModuleCache;
                if (ImGui.Checkbox("Disable Module Cache", ref disableCache))
                {
                    LegionScripting.LegionScripting.LScriptSettings.DisableModuleCache = disableCache;
                }
                ImGui.EndPopup();
            }
        }

        private Dictionary<string, Dictionary<string, List<ScriptFile>>> OrganizeScripts()
        {
            var groupsMap = new Dictionary<string, Dictionary<string, List<ScriptFile>>>
            {
                { "", new Dictionary<string, List<ScriptFile>> { { "", new List<ScriptFile>() } } }
            };

            foreach (ScriptFile sf in LegionScripting.LegionScripting.LoadedScripts)
            {
                if (!groupsMap.ContainsKey(sf.Group))
                    groupsMap[sf.Group] = new Dictionary<string, List<ScriptFile>>();

                if (!groupsMap[sf.Group].ContainsKey(sf.SubGroup))
                    groupsMap[sf.Group][sf.SubGroup] = new List<ScriptFile>();

                groupsMap[sf.Group][sf.SubGroup].Add(sf);
            }

            return groupsMap;
        }

        private void DrawScriptGroups(Dictionary<string, Dictionary<string, List<ScriptFile>>> groupsMap)
        {
            foreach (KeyValuePair<string, Dictionary<string, List<ScriptFile>>> group in groupsMap)
            {
                string groupName = string.IsNullOrEmpty(group.Key) ? NOGROUPTEXT : group.Key;
                DrawGroup(groupName, group.Value, "");
            }
        }

        private void DrawGroup(string groupName, Dictionary<string, List<ScriptFile>> subGroups, string parentGroup)
        {
            string fullGroupPath = string.IsNullOrEmpty(parentGroup) ? groupName : Path.Combine(parentGroup, groupName);

            // Initialize collapsed state from settings if not already in our set
            string normalizedGroupName = groupName == NOGROUPTEXT ? "" : groupName;
            string normalizedParentGroup = parentGroup == NOGROUPTEXT ? "" : parentGroup;
            string parentSpacer = string.IsNullOrEmpty(parentGroup) ? string.Empty : "   ";

            bool isCollapsedInSettings = string.IsNullOrEmpty(normalizedParentGroup)
                ? LegionScripting.LegionScripting.IsGroupCollapsed(normalizedGroupName)
                : LegionScripting.LegionScripting.IsGroupCollapsed(normalizedParentGroup, normalizedGroupName);

            if (isCollapsedInSettings && !_collapsedGroups.Contains(fullGroupPath))
                _collapsedGroups.Add(fullGroupPath);

            bool isCollapsed = _collapsedGroups.Contains(fullGroupPath);
            // Group header with expand/collapse button and context menu
            ImGui.PushID(fullGroupPath);
            // Create custom expand/collapse button with custom symbols
            string expandSymbol = isCollapsed ? "+" : "-"; // Plus for collapsed, minus for expanded
            // Use a square button with larger size for better visibility
            ImGui.Text($"{parentSpacer}[ {expandSymbol} ] ");

            ImGui.SameLine(0, 2); // Small spacing between button and text

            bool nodeOpen = !isCollapsed;
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(5, 10));
            // Use Selectable instead of Text to get hover highlighting
            bool groupSelected = false;
            if (ImGui.Selectable(groupName, groupSelected, ImGuiSelectableFlags.SpanAllColumns))
            {
                // Single click on group name - toggle expand/collapse
                if (!ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                {
                    ToggleGroupState(isCollapsed, fullGroupPath, normalizedParentGroup, normalizedGroupName);
                }
            }

            // Right-click context menu for group
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                _showContextMenu = true;
                _contextMenuGroup = parentGroup;
                _contextMenuSubGroup = groupName;
                _contextMenuScript = null;
                _contextMenuPosition = ImGui.GetMousePos();
            }

            // Accept drag and drop for moving scripts to this group
            if (ImGui.BeginDragDropTarget())
            {
                // Highlight the drop target area with primary theme color
                ImDrawListPtr drawList = ImGui.GetWindowDrawList();
                Vector2 itemMin = ImGui.GetItemRectMin();
                Vector2 itemMax = ImGui.GetItemRectMax();
                uint highlightColor = ImGui.ColorConvertFloat4ToU32(ImGuiTheme.Colors.Primary * 0.5f); // Semi-transparent primary color
                drawList.AddRectFilled(itemMin, itemMax, highlightColor);

                unsafe
                {
                    ImGuiPayloadPtr payload = ImGui.AcceptDragDropPayload("SCRIPT_FILE");
                    if (payload.NativePtr != null)
                    {
                        // Extract the script file path from payload
                        byte[] payloadData = new byte[payload.DataSize];
                        System.Runtime.InteropServices.Marshal.Copy(payload.Data, payloadData, 0, (int)payload.DataSize);
                        string scriptPath = System.Text.Encoding.UTF8.GetString(payloadData);

                        // Find the script and move it to this group
                        ScriptFile script = LegionScripting.LegionScripting.LoadedScripts.FirstOrDefault(s => s.FullPath == scriptPath);
                        if (script != null)
                        {
                            // Determine the correct target group hierarchy based on current level
                            string targetGroup, targetSubGroup;

                            if (string.IsNullOrEmpty(parentGroup) || parentGroup == NOGROUPTEXT)
                            {
                                // Dropping into a top-level group
                                targetGroup = normalizedGroupName;
                                targetSubGroup = "";
                            }
                            else
                            {
                                // Dropping into a subgroup
                                targetGroup = normalizedParentGroup;
                                targetSubGroup = normalizedGroupName;
                            }

                            MoveScriptToGroup(script, targetGroup, targetSubGroup);
                        }
                    }
                }
                ImGui.EndDragDropTarget();
            }

            // If node is open, render children without extra indentation
            if (nodeOpen)
            {
                // Draw subgroups and scripts
                foreach (KeyValuePair<string, List<ScriptFile>> subGroup in subGroups)
                {
                    if (!string.IsNullOrEmpty(subGroup.Key))
                    {
                        // This is a subgroup
                        var subGroupData = new Dictionary<string, List<ScriptFile>> { { "", subGroup.Value } };
                        DrawGroup(subGroup.Key, subGroupData, groupName);
                    }
                    else
                    {
                        // These are scripts directly in this group
                        foreach (ScriptFile script in subGroup.Value)
                        {
                            DrawScript(script, parentSpacer);
                        }
                    }
                }
            }
            ImGui.PopStyleVar(1);
            ImGui.PopID();
        }

        private void DrawScript(ScriptFile script, string spacer)
        {
            ImGui.PushID(script.FullPath);

            ImGui.Text(spacer);
            ImGui.SameLine();

            // Add menu button next to play button
            if (ImGui.Button("..."))
            {
                _showContextMenu = true;
                _contextMenuScript = script;
                _contextMenuGroup = "";
                _contextMenuSubGroup = "";
                _contextMenuPosition = ImGui.GetMousePos();
            }
            ImGui.SameLine();
            // Get script display name (without extension)
            string displayName = script.FileName;
            int lastDotIndex = displayName.LastIndexOf('.');
            if (lastDotIndex != -1)
                displayName = displayName.Substring(0, lastDotIndex);

            // Check if script is playing
            bool isPlaying = script.IsPlaying || (script.GetScript != null && script.GetScript.IsPlaying);

            // Draw play/stop button
            string buttonText = isPlaying ? "Stop" : "Play";
            Vector4 buttonColor = isPlaying
                ? new Vector4(0.2f, 0.6f, 0.2f, 1.0f) // Green for play
                : ImGuiTheme.Colors.Primary;


            ImGui.PushStyleColor(ImGuiCol.Button, buttonColor);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, buttonColor * 1.2f);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, buttonColor * 0.8f);

            if (ImGui.Button(buttonText, new Vector2(50, 0)))
            {
                if (isPlaying)
                    LegionScripting.LegionScripting.StopScript(script);
                else
                    LegionScripting.LegionScripting.PlayScript(script);
            }

            ImGui.PopStyleColor(3);

            // Autostart indicator
            ImGui.SameLine();
            bool hasGlobalAutostart = LegionScripting.LegionScripting.AutoLoadEnabled(script, true);
            bool hasCharacterAutostart = LegionScripting.LegionScripting.AutoLoadEnabled(script, false);

            if (hasGlobalAutostart || hasCharacterAutostart)
            {
                Vector4 autostartColor = hasGlobalAutostart
                    ? new Vector4(1.0f, 0.8f, 0.0f, 1.0f)  // Gold for global autostart
                    : new Vector4(0.0f, 0.8f, 1.0f, 1.0f); // Cyan for character autostart

                ImGui.PushStyleColor(ImGuiCol.Text, autostartColor);
                string indicator = hasGlobalAutostart ? "[G]" : "[C]";
                ImGui.Text(indicator);
                ImGui.PopStyleColor();

                if (ImGui.IsItemHovered())
                {
                    string tooltip = hasGlobalAutostart ? "Autostart: All characters" : "Autostart: This character";
                    ImGui.SetTooltip(tooltip);
                }
                ImGui.SameLine();
            }

            // Draw script name or rename input
            if (_renameState.Script == script)
            {
                // Show rename input - Enter to save, Escape or click outside to cancel
                ImGui.SetKeyboardFocusHere();
                ImGui.SetNextItemWidth(150);
                string buffer = _renameState.Buffer;
                if (ImGui.InputText($"##rename{script.FullPath}", ref buffer, 256, ImGuiInputTextFlags.EnterReturnsTrue))
                {
                    _renameState.Buffer = buffer;
                    PerformRename(script);
                }
                else
                {
                    _renameState.Buffer = buffer;
                }

                // Check for Escape key to cancel rename
                if (ImGui.IsKeyPressed(ImGuiKey.Escape))
                {
                    _renameState.Clear();
                }

                // Check if we should cancel rename due to clicking outside
                if (_shouldCancelRename)
                {
                    // If the input text was clicked/hovered, don't cancel
                    if (!ImGui.IsItemHovered() && !ImGui.IsItemActive())
                    {
                        _renameState.Clear();
                    }
                    _shouldCancelRename = false; // Reset the flag
                }
            }
            else
            {
                // Normal script display with native double-click detection
                bool isSelected = false;
                ImGui.Selectable($"{displayName}", isSelected);

                // Use native ImGUI double-click detection
                if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                {
                    // Start renaming
                    _renameState.StartScriptRename(script, displayName);
                }


                // Begin drag source for script
                if (ImGui.BeginDragDropSource(ImGuiDragDropFlags.None))
                {
                    // Set payload to script file path
                    unsafe
                    {
                        byte[] scriptPathBytes = System.Text.Encoding.UTF8.GetBytes(script.FullPath);
                        fixed (byte* ptr = scriptPathBytes)
                        {
                            ImGui.SetDragDropPayload("SCRIPT_FILE", new IntPtr(ptr), (uint)scriptPathBytes.Length);
                        }
                    }

                    // Tooltip showing what's being dragged
                    ImGui.Text($"Moving: {displayName}");
                    ImGui.EndDragDropSource();
                }
            }

            // Tooltip with full filename (only when not renaming)
            if (_renameState.Script != script && ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(script.FileName);
            }

            // Right-click context menu for script (works on both button and name)
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                _showContextMenu = true;
                _contextMenuScript = script;
                _contextMenuGroup = "";
                _contextMenuSubGroup = "";
                _contextMenuPosition = ImGui.GetMousePos();
            }
            ImGui.PopID();
        }

        private void ToggleGroupState(bool isCollapsed, string fullGroupPath, string normalizedParentGroup, string normalizedGroupName)
        {
            if (isCollapsed)
            {
                _collapsedGroups.Remove(fullGroupPath);
                if (string.IsNullOrEmpty(normalizedParentGroup))
                    LegionScripting.LegionScripting.SetGroupCollapsed(normalizedGroupName, "", false);
                else
                    LegionScripting.LegionScripting.SetGroupCollapsed(normalizedParentGroup, normalizedGroupName, false);
            }
            else
            {
                _collapsedGroups.Add(fullGroupPath);
                if (string.IsNullOrEmpty(normalizedParentGroup))
                    LegionScripting.LegionScripting.SetGroupCollapsed(normalizedGroupName, "", true);
                else
                    LegionScripting.LegionScripting.SetGroupCollapsed(normalizedParentGroup, normalizedGroupName, true);
            }
        }

        private void PerformRename(ScriptFile script)
        {
            if (string.IsNullOrWhiteSpace(_renameState.Buffer))
            {
                _renameState.Clear();
                return;
            }

            try
            {
                // Get the original extension
                string originalExtension = Path.GetExtension(script.FileName);

                // Ensure the new name has the correct extension
                string newName = _renameState.Buffer;
                if (!newName.EndsWith(originalExtension, StringComparison.OrdinalIgnoreCase))
                {
                    newName += originalExtension;
                }

                // Build new file path
                string directory = Path.GetDirectoryName(script.FullPath);
                string newPath = Path.Combine(directory, newName);

                // Check if the new file name already exists
                if (File.Exists(newPath) && !string.Equals(script.FullPath, newPath))
                {
                    GameActions.Print(World.Instance, $"A file with the name '{newName}' already exists.", 32);
                    return;
                }

                // Perform the rename
                if (!string.Equals(script.FullPath, newPath))
                {
                    File.Move(script.FullPath, newPath);

                    // Update the script object
                    script.FullPath = newPath;
                    script.FileName = newName;

                    _pendingReload = true;
                }
            }
            catch (Exception ex)
            {
                GameActions.Print(World.Instance, $"Error renaming script: {ex.Message}", 32);
            }
            finally
            {
                _renameState.Clear();
            }
        }

        private void PerformGroupRename()
        {
            if (string.IsNullOrWhiteSpace(_renameState.Buffer))
            {
                _renameState.Clear();
                return;
            }

            try
            {
                // Build current group path
                string currentPath = LegionScripting.LegionScripting.ScriptPath;
                if (!string.IsNullOrEmpty(_renameState.GroupParent))
                    currentPath = Path.Combine(currentPath, _renameState.GroupParent);
                currentPath = Path.Combine(currentPath, _renameState.GroupName);

                // Build new group path
                string newPath = LegionScripting.LegionScripting.ScriptPath;
                if (!string.IsNullOrEmpty(_renameState.GroupParent))
                    newPath = Path.Combine(newPath, _renameState.GroupParent);
                newPath = Path.Combine(newPath, _renameState.Buffer);

                // Check if the new group name already exists
                if (Directory.Exists(newPath) && !string.Equals(currentPath, newPath, StringComparison.OrdinalIgnoreCase))
                {
                    GameActions.Print(World.Instance, $"A group with the name '{_renameState.Buffer}' already exists.", 32);
                    return;
                }

                // Check if current directory exists
                if (!Directory.Exists(currentPath))
                {
                    GameActions.Print(World.Instance, $"Source group '{_renameState.GroupName}' not found.", 32);
                    return;
                }

                // Perform the rename
                if (!string.Equals(currentPath, newPath, StringComparison.OrdinalIgnoreCase))
                {
                    Directory.Move(currentPath, newPath);
                    _pendingReload = true;
                    GameActions.Print(World.Instance, $"Renamed group '{_renameState.GroupName}' to '{_renameState.Buffer}'", 66);
                }
            }
            catch (UnauthorizedAccessException)
            {
                GameActions.Print(World.Instance, "Access denied. Check directory permissions.", 32);
            }
            catch (DirectoryNotFoundException)
            {
                GameActions.Print(World.Instance, "Directory not found.", 32);
            }
            catch (IOException ioEx)
            {
                GameActions.Print(World.Instance, $"Directory operation failed: {ioEx.Message}", 32);
            }
            catch (Exception ex)
            {
                GameActions.Print(World.Instance, $"Error renaming group: {ex.Message}", 32);
                Log.Error($"Error renaming group {_renameState.GroupName}: {ex}");
            }
            finally
            {
                _renameState.Clear();
            }
        }

        private void PerformDelete()
        {
            try
            {
                if (_dialogState.ScriptToDelete != null)
                {
                    // Delete script file
                    File.Delete(_dialogState.ScriptToDelete.FullPath);
                    LegionScripting.LegionScripting.LoadedScripts.Remove(_dialogState.ScriptToDelete);
                    GameActions.Print(World.Instance, $"Deleted script '{_dialogState.ScriptToDelete.FileName}'", 66);
                    _pendingReload = true;
                }
                else if (!string.IsNullOrEmpty(_dialogState.GroupToDelete))
                {
                    // Delete group folder
                    string gPath = string.IsNullOrEmpty(_dialogState.GroupToDeleteParent) ? _dialogState.GroupToDelete : Path.Combine(_dialogState.GroupToDeleteParent, _dialogState.GroupToDelete);
                    gPath = Path.Combine(LegionScripting.LegionScripting.ScriptPath, gPath);

                    if (Directory.Exists(gPath))
                    {
                        Directory.Delete(gPath, true);
                        GameActions.Print(World.Instance, $"Deleted group '{_dialogState.GroupToDelete}' and all its contents", 66);
                        _pendingReload = true;
                    }
                    else
                    {
                        GameActions.Print(World.Instance, $"Group '{_dialogState.GroupToDelete}' not found", 32);
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                GameActions.Print(World.Instance, "Access denied. Check file/directory permissions.", 32);
            }
            catch (DirectoryNotFoundException)
            {
                GameActions.Print(World.Instance, "Directory not found.", 32);
            }
            catch (FileNotFoundException)
            {
                GameActions.Print(World.Instance, "File not found.", 32);
            }
            catch (IOException ioEx)
            {
                GameActions.Print(World.Instance, $"Delete operation failed: {ioEx.Message}", 32);
            }
            catch (Exception ex)
            {
                string itemType = _dialogState.ScriptToDelete != null ? "script" : "group";
                string itemName = _dialogState.ScriptToDelete != null ? _dialogState.ScriptToDelete.FileName : _dialogState.GroupToDelete;
                GameActions.Print(World.Instance, $"Error deleting {itemType}: {ex.Message}", 32);
                Log.Error($"Error deleting {itemType} {itemName}: {ex}");
            }
            finally
            {
                // Reset delete state using DialogState
                _dialogState.ClearAll();
            }
        }

        private void DrawContextMenus()
        {
            if (_showContextMenu)
            {
                ImGui.SetNextWindowPos(_contextMenuPosition, ImGuiCond.Appearing);
                ImGui.OpenPopup("ContextMenu");
                _showContextMenu = false;
            }

            if (ImGui.BeginPopup("ContextMenu"))
            {
                if (_contextMenuScript != null)
                {
                    DrawScriptContextMenu(_contextMenuScript);
                }
                else
                {
                    DrawGroupContextMenu(_contextMenuGroup, _contextMenuSubGroup);
                }
                ImGui.EndPopup();
            }
        }

        private void DrawScriptContextMenu(ScriptFile script)
        {
            ImGui.Text(script.FileName);
            ImGui.SeparatorText("Options:");

            if(ImGui.MenuItem("Edit Constants"))
                ImGuiManager.AddWindow(new ScriptConstantsEditorWindow(script));

            if (ImGui.MenuItem("Rename"))
            {
                // Start renaming the script
                string displayName = script.FileName;
                int lastDotIndex = displayName.LastIndexOf('.');
                if (lastDotIndex != -1)
                    displayName = displayName.Substring(0, lastDotIndex);

                _renameState.StartScriptRename(script, displayName);
                _showContextMenu = false;
            }

            if (ImGui.MenuItem("Edit"))
            {
                UIManager.Add(new ScriptEditor(World.Instance, script));
                _showContextMenu = false;
            }

            if (ImGui.MenuItem("Edit Externally"))
            {
                OpenFileWithDefaultApp(script.FullPath);
                _showContextMenu = false;
            }

            if (ImGui.BeginMenu("Autostart"))
            {
                bool globalAutostart = LegionScripting.LegionScripting.AutoLoadEnabled(script, true);
                bool characterAutostart = LegionScripting.LegionScripting.AutoLoadEnabled(script, false);

                if (ImGui.Checkbox("All characters", ref globalAutostart))
                {
                    LegionScripting.LegionScripting.SetAutoPlay(script, true, globalAutostart);
                }

                if (ImGui.Checkbox("This character", ref characterAutostart))
                {
                    LegionScripting.LegionScripting.SetAutoPlay(script, false, characterAutostart);
                }

                ImGui.EndMenu();
            }

            if (ImGui.MenuItem("Create macro button"))
            {
                var mm = MacroManager.TryGetMacroManager(World.Instance);
                if (mm != null)
                {
                    var mac = new Macro(script.FileName);
                    mac.Items = new MacroObjectString(MacroType.ClientCommand, MacroSubType.MSC_NONE, "togglelscript " + script.FileName);
                    mm.PushToBack(mac);

                    var bg = new MacroButtonGump(World.Instance, mac, Mouse.Position.X, Mouse.Position.Y);
                    UIManager.Add(bg);
                }
                _showContextMenu = false;
            }

            if (ImGui.MenuItem("Delete"))
            {
                _dialogState.ShowScriptDeleteDialog(script);
                _showContextMenu = false;
            }
        }

        private void DrawGroupContextMenu(string parentGroup, string groupName)
        {
            if (groupName != NOGROUPTEXT && !string.IsNullOrEmpty(groupName))
            {
                ImGui.Text(groupName);
                ImGui.SeparatorText("Options:");
                if (ImGui.MenuItem("Rename"))
                {
                    _renameState.StartGroupRename(groupName, parentGroup);
                    _dialogState.ShowRenameGroup = true;
                    _showContextMenu = false;
                }

                if (ImGui.MenuItem("New Script"))
                {
                    _dialogState.ShowNewScript = true;
                    _showContextMenu = false;
                }

                if (string.IsNullOrEmpty(parentGroup))
                {
                    if (ImGui.MenuItem("New Group"))
                    {
                        _dialogState.ShowNewGroup = true;
                        _showContextMenu = false;
                    }
                }

                if (ImGui.MenuItem("Delete Group"))
                {
                    _dialogState.ShowGroupDeleteDialog(groupName, parentGroup);
                    _showContextMenu = false;
                }
            }
            else
            {
                if (ImGui.MenuItem("New Script"))
                {
                    _dialogState.ShowNewScript = true;
                    _showContextMenu = false;
                }

                if (string.IsNullOrEmpty(parentGroup))
                {
                    if (ImGui.MenuItem("New Group"))
                    {
                        _dialogState.ShowNewGroup = true;
                        _showContextMenu = false;
                    }
                }
            }
        }

        private void DrawDialogs()
        {
            // Open popups when dialog state changes - ImGUI will handle positioning automatically
            if (_dialogState.ShowNewScript && !ImGui.IsPopupOpen("New Script"))
            {
                ImGui.OpenPopup("New Script");
                // Center the popup on the main viewport
                ImGui.SetNextWindowPos(ImGui.GetMainViewport().GetCenter(), ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
            }
            if (_dialogState.ShowNewGroup && !ImGui.IsPopupOpen("New Group"))
            {
                ImGui.OpenPopup("New Group");
                ImGui.SetNextWindowPos(ImGui.GetMainViewport().GetCenter(), ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
            }
            if (_dialogState.ShowRenameGroup && !ImGui.IsPopupOpen("Rename Group"))
            {
                ImGui.OpenPopup("Rename Group");
                ImGui.SetNextWindowPos(ImGui.GetMainViewport().GetCenter(), ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
            }
            if (_dialogState.ShowDeleteConfirm && !ImGui.IsPopupOpen(_dialogState.DeleteTitle))
            {
                ImGui.OpenPopup(_dialogState.DeleteTitle);
                ImGui.SetNextWindowPos(ImGui.GetMainViewport().GetCenter(), ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
            }

            // New Script Dialog
            bool showNewScript = _dialogState.ShowNewScript;
            if (ImGui.BeginPopupModal("New Script", ref showNewScript, ImGuiWindowFlags.AlwaysAutoResize))
            {
                _dialogState.ShowNewScript = showNewScript;
                ImGui.Text("Enter a name for this script.");
                ImGui.Text("Use .lscript or .py extension");

                string scriptName = _dialogState.NewScriptName;
                ImGui.InputText("##ScriptName", ref scriptName, 100);
                _dialogState.NewScriptName = scriptName;

                ImGui.Separator();

                if (ImGui.Button("Create"))
                {
                    if (!string.IsNullOrEmpty(_dialogState.NewScriptName))
                    {
                        if (_dialogState.NewScriptName.EndsWith(".lscript") || _dialogState.NewScriptName.EndsWith(".py"))
                        {
                            // Validate and sanitize the script name to ensure it is a plain filename
                            string sanitizedName = Path.GetFileName(_dialogState.NewScriptName.Trim());

                            // Reject names that contain path separators or relative navigation
                            if (string.IsNullOrWhiteSpace(sanitizedName) ||
                                sanitizedName != _dialogState.NewScriptName.Trim() ||
                                sanitizedName.Contains("\\") ||
                                sanitizedName.Contains("/") ||
                                sanitizedName.Contains("..") ||
                                sanitizedName == "." ||
                                sanitizedName == "..")
                            {
                                GameActions.Print(World.Instance, "Invalid script name. Names cannot contain path separators or relative navigation.", 32);
                            }
                            else
                            {
                                try
                                {
                                    // Normalize sentinels by replacing NOGROUPTEXT with empty string
                                    string normalizedGroup = _contextMenuGroup == NOGROUPTEXT ? "" : _contextMenuGroup;
                                    string normalizedSubGroup = _contextMenuSubGroup == NOGROUPTEXT ? "" : _contextMenuSubGroup;

                                    // Sanitize group path segments as well
                                    if (!string.IsNullOrEmpty(normalizedGroup))
                                        normalizedGroup = Path.GetFileName(normalizedGroup);
                                    if (!string.IsNullOrEmpty(normalizedSubGroup))
                                        normalizedSubGroup = Path.GetFileName(normalizedSubGroup);

                                    string gPath = string.IsNullOrEmpty(normalizedGroup) ? normalizedSubGroup :
                                        string.IsNullOrEmpty(normalizedSubGroup) ? normalizedGroup :
                                        Path.Combine(normalizedGroup, normalizedSubGroup);

                                    // Build target paths
                                    string targetDirectory = Path.Combine(LegionScripting.LegionScripting.ScriptPath, gPath ?? "");
                                    string filePath = Path.Combine(targetDirectory, sanitizedName);

                                    // Get full paths and verify they stay within the scripts root
                                    string scriptsRootFullPath = Path.GetFullPath(LegionScripting.LegionScripting.ScriptPath);
                                    string targetDirectoryFullPath = Path.GetFullPath(targetDirectory);
                                    string targetFileFullPath = Path.GetFullPath(filePath);

                                    // Verify both directory and file paths are within the scripts root
                                    if (!targetDirectoryFullPath.StartsWith(scriptsRootFullPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) &&
                                        !targetDirectoryFullPath.Equals(scriptsRootFullPath, StringComparison.OrdinalIgnoreCase))
                                    {
                                        GameActions.Print(World.Instance, "Invalid target directory. Path must be within the scripts directory.", 32);
                                    }
                                    else if (!targetFileFullPath.StartsWith(scriptsRootFullPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) &&
                                        !targetFileFullPath.Equals(scriptsRootFullPath, StringComparison.OrdinalIgnoreCase))
                                    {
                                        GameActions.Print(World.Instance, "Invalid script path. Path must be within the scripts directory.", 32);
                                    }
                                    else
                                    {
                                        // Create directory if it doesn't exist (now validated)
                                        if (!Directory.Exists(targetDirectoryFullPath))
                                            Directory.CreateDirectory(targetDirectoryFullPath);

                                        // Create script file if it doesn't exist
                                        if (!File.Exists(targetFileFullPath))
                                        {
                                            File.WriteAllText(targetFileFullPath, SCRIPT_HEADER);
                                            _pendingReload = true;
                                            GameActions.Print(World.Instance, $"Created script '{sanitizedName}'", 66);
                                        }
                                        else
                                        {
                                            GameActions.Print(World.Instance, $"A script named '{sanitizedName}' already exists.", 32);
                                        }
                                    }
                                }
                                catch (UnauthorizedAccessException)
                                {
                                    GameActions.Print(World.Instance, "Access denied. Check directory permissions.", 32);
                                }
                                catch (DirectoryNotFoundException)
                                {
                                    GameActions.Print(World.Instance, "Directory not found.", 32);
                                }
                                catch (IOException ioEx)
                                {
                                    GameActions.Print(World.Instance, $"File operation failed: {ioEx.Message}", 32);
                                }
                                catch (Exception e)
                                {
                                    GameActions.Print(World.Instance, $"Error creating script: {e.Message}", 32);
                                    Log.Error($"Error creating script {sanitizedName}: {e}");
                                }
                            }
                        }
                        else
                        {
                            GameActions.Print(World.Instance, "Script files must end with .lscript or .py", 32);
                        }
                    }

                    _dialogState.NewScriptName = "";
                    _dialogState.ShowNewScript = false;
                    ImGui.CloseCurrentPopup();
                }

                ImGui.SameLine();

                if (ImGui.Button("Cancel"))
                {
                    _dialogState.NewScriptName = "";
                    _dialogState.ShowNewScript = false;
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndPopup();
            }

            // New Group Dialog
            bool showNewGroup = _dialogState.ShowNewGroup;
            if (ImGui.BeginPopupModal("New Group", ref showNewGroup, ImGuiWindowFlags.AlwaysAutoResize))
            {
                _dialogState.ShowNewGroup = showNewGroup;
                ImGui.Text("Enter a name for this group.");

                string groupName = _dialogState.NewGroupName;
                ImGui.InputText("##GroupName", ref groupName, 100);
                _dialogState.NewGroupName = groupName;

                ImGui.Separator();

                if (ImGui.Button("Create"))
                {
                    if (!string.IsNullOrEmpty(_dialogState.NewGroupName))
                    {
                        // Sanitize the group name to prevent path traversal
                        string sanitizedGroupName = Path.GetFileName(_dialogState.NewGroupName.Trim());

                        // Remove extension if present
                        int p = sanitizedGroupName.IndexOf('.');
                        if (p != -1)
                            sanitizedGroupName = sanitizedGroupName.Substring(0, p);

                        // Explicitly reject names that contain directory separators or equal ".."
                        if (string.IsNullOrEmpty(sanitizedGroupName) ||
                            sanitizedGroupName != _dialogState.NewGroupName.Trim() ||
                            sanitizedGroupName.Contains("\\") ||
                            sanitizedGroupName.Contains("/") ||
                            sanitizedGroupName == ".." ||
                            sanitizedGroupName == ".")
                        {
                            GameActions.Print(World.Instance, "Invalid group name. Names cannot contain path separators or relative navigation.", 32);
                        }
                        else
                        {
                            try
                            {
                                // Build full path including parent group with sanitized segments
                                string normalizedGroup = _contextMenuGroup == NOGROUPTEXT ? "" : _contextMenuGroup;
                                string normalizedSubGroup = _contextMenuSubGroup == NOGROUPTEXT ? "" : _contextMenuSubGroup;

                                // Sanitize parent group segments as well
                                if (!string.IsNullOrEmpty(normalizedGroup))
                                    normalizedGroup = Path.GetFileName(normalizedGroup);
                                if (!string.IsNullOrEmpty(normalizedSubGroup))
                                    normalizedSubGroup = Path.GetFileName(normalizedSubGroup);

                                string path = Path.Combine(LegionScripting.LegionScripting.ScriptPath,
                                    normalizedGroup ?? "",
                                    normalizedSubGroup ?? "",
                                    sanitizedGroupName);

                                // Resolve both paths to absolute canonical paths
                                string scriptsRootPath = Path.GetFullPath(LegionScripting.LegionScripting.ScriptPath);
                                string targetPath = Path.GetFullPath(path);

                                // Verify the target path starts with the scripts root path
                                if (!targetPath.StartsWith(scriptsRootPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) &&
                                    !targetPath.Equals(scriptsRootPath, StringComparison.OrdinalIgnoreCase))
                                {
                                    GameActions.Print(World.Instance, "Invalid group location. Path must be within the scripts directory.", 32);
                                }
                                else
                                {
                                    if (!Directory.Exists(targetPath))
                                    {
                                        Directory.CreateDirectory(targetPath);
                                    }
                                    File.WriteAllText(Path.Combine(targetPath, "Example.py"), EXAMPLE_LSCRIPT);
                                    _pendingReload = true;
                                    GameActions.Print(World.Instance, $"Created group '{sanitizedGroupName}'", 66);
                                }
                            }
                            catch (UnauthorizedAccessException)
                            {
                                GameActions.Print(World.Instance, "Access denied. Check directory permissions.", 32);
                            }
                            catch (DirectoryNotFoundException)
                            {
                                GameActions.Print(World.Instance, "Directory not found.", 32);
                            }
                            catch (IOException ioEx)
                            {
                                GameActions.Print(World.Instance, $"Directory operation failed: {ioEx.Message}", 32);
                            }
                            catch (Exception e)
                            {
                                GameActions.Print(World.Instance, $"Error creating group: {e.Message}", 32);
                                Log.Error($"Error creating group {sanitizedGroupName}: {e}");
                            }
                        }
                    }

                    _dialogState.NewGroupName = "";
                    _dialogState.ShowNewGroup = false;
                    ImGui.CloseCurrentPopup();
                }

                ImGui.SameLine();

                if (ImGui.Button("Cancel"))
                {
                    _dialogState.NewGroupName = "";
                    _dialogState.ShowNewGroup = false;
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndPopup();
            }

            // Rename Group Dialog
            bool showRenameGroup = _dialogState.ShowRenameGroup;
            if (ImGui.BeginPopupModal("Rename Group", ref showRenameGroup, ImGuiWindowFlags.AlwaysAutoResize))
            {
                _dialogState.ShowRenameGroup = showRenameGroup;
                ImGui.Text($"Enter a new name for the group '{_renameState.GroupName}'.");

                string renameBuffer = _renameState.Buffer;
                ImGui.InputText("##Group Name", ref renameBuffer, 100);
                _renameState.Buffer = renameBuffer;

                ImGui.Separator();

                if (ImGui.Button("Save"))
                {
                    if (!string.IsNullOrEmpty(_renameState.Buffer))
                    {
                        int p = _renameState.Buffer.IndexOf('.');
                        if (p != -1)
                            _renameState.Buffer = _renameState.Buffer.Substring(0, p);

                        PerformGroupRename();
                    }

                    _dialogState.ShowRenameGroup = false;
                    ImGui.CloseCurrentPopup();
                }

                ImGui.SameLine();

                if (ImGui.Button("Cancel"))
                {
                    _renameState.Clear();
                    _dialogState.ShowRenameGroup = false;
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndPopup();
            }

            // Delete Confirmation Dialog
            bool showDeleteConfirm = _dialogState.ShowDeleteConfirm;
            if (ImGui.BeginPopupModal(_dialogState.DeleteTitle, ref showDeleteConfirm, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse))
            {
                _dialogState.ShowDeleteConfirm = showDeleteConfirm;
                // Add warning icon color
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.8f, 0.0f, 1.0f)); // Orange/yellow warning color
                ImGui.Text("⚠");
                ImGui.PopStyleColor();
                ImGui.SameLine();

                ImGui.Text(_dialogState.DeleteMessage);

                ImGui.Separator();

                // Buttons with different colors
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.8f, 0.2f, 0.2f, 1.0f)); // Red for delete
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.9f, 0.3f, 0.3f, 1.0f));
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.7f, 0.1f, 0.1f, 1.0f));

                if (ImGui.Button("Delete"))
                {
                    PerformDelete();
                    ImGui.CloseCurrentPopup();
                }

                ImGui.PopStyleColor(3);
                ImGui.SameLine();

                if (ImGui.Button("Cancel"))
                {
                    _dialogState.ClearAll();
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndPopup();
            }
        }

        private static void OpenFileWithDefaultApp(string filePath)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    ProcessStartInfo p = new() { FileName = "xdg-open", ArgumentList = { filePath }};
                    Process.Start(p);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    ProcessStartInfo p = new() { FileName = "open", ArgumentList = { filePath }};
                    Process.Start(p);
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error opening file: " + ex.Message);
            }
        }

        private void MoveScriptToGroup(ScriptFile script, string targetGroup, string targetSubGroup)
        {
            try
            {
                // Validate input parameters
                if (script == null)
                {
                    GameActions.Print(World.Instance, "Invalid script file.", 32);
                    return;
                }

                // Normalize empty strings to prevent issues
                targetGroup = targetGroup ?? "";
                targetSubGroup = targetSubGroup ?? "";

                // Prevent moving to the same location
                if (script.Group == targetGroup && script.SubGroup == targetSubGroup)
                {
                    GameActions.Print(World.Instance, "Script is already in this location.", 66);
                    return;
                }

                // Check if source file exists
                if (!File.Exists(script.FullPath))
                {
                    GameActions.Print(World.Instance, $"Source file '{script.FileName}' not found.", 32);
                    return;
                }

                // Build the target directory path
                string targetPath = LegionScripting.LegionScripting.ScriptPath;
                if (!string.IsNullOrEmpty(targetGroup))
                    targetPath = Path.Combine(targetPath, targetGroup);
                if (!string.IsNullOrEmpty(targetSubGroup))
                    targetPath = Path.Combine(targetPath, targetSubGroup);

                // Create target directory if it doesn't exist
                if (!Directory.Exists(targetPath))
                {
                    try
                    {
                        Directory.CreateDirectory(targetPath);
                    }
                    catch (Exception ex)
                    {
                        GameActions.Print(World.Instance, $"Failed to create target directory: {ex.Message}", 32);
                        return;
                    }
                }

                // Build new file path
                string newFilePath = Path.Combine(targetPath, script.FileName);

                // Check if file already exists at target location
                if (File.Exists(newFilePath))
                {
                    GameActions.Print(World.Instance, $"A file named '{script.FileName}' already exists in the target group.", 32);
                    return;
                }

                // Validate that the target path is within the scripts directory (security check)
                string normalizedTargetPath = Path.GetFullPath(targetPath);
                string normalizedScriptPath = Path.GetFullPath(LegionScripting.LegionScripting.ScriptPath);
                if (!normalizedTargetPath.StartsWith(normalizedScriptPath))
                {
                    GameActions.Print(World.Instance, "Invalid target location.", 32);
                    return;
                }

                // Check if the script is currently running and warn the user
                if (script.IsPlaying)
                {
                    GameActions.Print(World.Instance, $"Warning: Moving running script '{script.FileName}'. The script will continue running.", 34);
                }

                // Move the file
                File.Move(script.FullPath, newFilePath);

                // Remove the script from the loaded scripts collection so it gets rediscovered in its new location
                LegionScripting.LegionScripting.LoadedScripts.Remove(script);

                // Refresh the script list - this will reload scripts from files and rediscover the moved script
                _pendingReload = true;

                // Build display message for target location
                string targetDisplayName = "root";
                if (!string.IsNullOrEmpty(targetGroup))
                {
                    targetDisplayName = targetGroup;
                    if (!string.IsNullOrEmpty(targetSubGroup))
                        targetDisplayName += "/" + targetSubGroup;
                }

                GameActions.Print(World.Instance, $"Moved '{script.FileName}' to {targetDisplayName}", 66);
            }
            catch (UnauthorizedAccessException)
            {
                GameActions.Print(World.Instance, "Access denied. Check file permissions.", 32);
            }
            catch (DirectoryNotFoundException)
            {
                GameActions.Print(World.Instance, "Directory not found.", 32);
            }
            catch (IOException ioEx)
            {
                GameActions.Print(World.Instance, $"File operation failed: {ioEx.Message}", 32);
            }
            catch (Exception ex)
            {
                GameActions.Print(World.Instance, $"Error moving script: {ex.Message}", 32);
                Log.Error($"Error moving script {script.FileName}: {ex}");
            }
        }

        public override void Dispose()
        {
            _showContextMenu = false;
            _dialogState.ClearAll();
            _renameState.Clear();
            _shouldCancelRename = false;
            base.Dispose();
        }
    }
}
