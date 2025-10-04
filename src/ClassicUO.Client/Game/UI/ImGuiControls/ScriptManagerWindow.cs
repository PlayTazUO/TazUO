using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
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
        private string _newScriptName = "";
        private string _newGroupName = "";
        private bool _showContextMenu = false;
        private string _contextMenuGroup = "";
        private string _contextMenuSubGroup = "";
        private bool _showNewScriptDialog = false;
        private bool _showNewGroupDialog = false;
        private bool _showRenameGroupDialog = false;
        private bool _showDeleteConfirmDialog = false;
        private string _deleteConfirmMessage = "";
        private string _deleteConfirmTitle = "";
        private ScriptFile _scriptToDelete = null;
        private string _groupToDelete = "";
        private string _groupToDeleteParent = "";
        private ScriptFile _contextMenuScript = null;
        private Vector2 _contextMenuPosition;
        private bool _showMainMenu = false;
        private bool _pendingReload = false;
        private bool _shouldCancelRename = false;
        private string _renamingGroup = null;
        private string _renamingParentGroup = null;
        private string _groupRenameBuffer = "";

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
        private class DoubleClickDetector
        {
            private string _lastClickedItem = null;
            private double _lastClickTime = 0.0;
            private const double DOUBLE_CLICK_TIME = 0.4; // 400ms

            public bool CheckDoubleClick(string itemKey)
            {
                double currentTime = ImGui.GetTime();
                bool isDoubleClick = _lastClickedItem == itemKey && (currentTime - _lastClickTime) < DOUBLE_CLICK_TIME;

                _lastClickedItem = itemKey;
                _lastClickTime = currentTime;

                return isDoubleClick;
            }

            public void Reset()
            {
                _lastClickedItem = null;
                _lastClickTime = 0.0;
            }
        }

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

        private readonly DoubleClickDetector _scriptDoubleClick = new DoubleClickDetector();
        private readonly DoubleClickDetector _groupDoubleClick = new DoubleClickDetector();
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

            // Top menu bar
            DrawMenuBar();

            ImGui.Separator();

            // Organize scripts by groups
            var groupsMap = OrganizeScripts();

            // Draw script groups
            DrawScriptGroups(groupsMap);

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
                    UIManager.Add(new ScriptBrowser(World.Instance));
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
            foreach (var group in groupsMap)
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
            Vector4 buttonColor = new Vector4(0, 0, 0, 0); // Transparent background
            ImGui.PushStyleColor(ImGuiCol.Button, buttonColor);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, buttonColor);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, buttonColor);

            // Use a square button with larger size for better visibility
            bool clicked = ImGui.Button($" {expandSymbol}  ##{fullGroupPath}");
            ImGui.PopStyleColor(3);

            ImGui.SameLine(0, 2); // Small spacing between button and text

            // Use Selectable instead of Text to get hover highlighting
            bool groupSelected = false;
            ImGui.Selectable(groupName, groupSelected, ImGuiSelectableFlags.SpanAllColumns);

            // Check if the group name was clicked for double-click rename
            bool nodeOpen = !isCollapsed;

            // Handle expand/collapse button click
            if (clicked)
            {
                ToggleGroupState(isCollapsed, fullGroupPath, normalizedParentGroup, normalizedGroupName);
            }

            // Handle single click on group name for expand/collapse and double-click for rename
            if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
            {
                string groupKey = $"{parentGroup}|{groupName}";

                if (groupName != NOGROUPTEXT && _groupDoubleClick.CheckDoubleClick(groupKey))
                {
                    // Double-click: Start renaming group
                    _renamingGroup = groupName;
                    _renamingParentGroup = parentGroup;
                    _groupRenameBuffer = groupName;
                    _showRenameGroupDialog = true;
                }
                else
                {
                    // Single-click: Toggle expand/collapse
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
                var drawList = ImGui.GetWindowDrawList();
                var itemMin = ImGui.GetItemRectMin();
                var itemMax = ImGui.GetItemRectMax();
                var highlightColor = ImGui.ColorConvertFloat4ToU32(ImGuiTheme.Colors.Primary * 0.5f); // Semi-transparent primary color
                drawList.AddRectFilled(itemMin, itemMax, highlightColor);

                unsafe
                {
                    var payload = ImGui.AcceptDragDropPayload("SCRIPT_FILE");
                    if (payload.NativePtr != null)
                    {
                        // Extract the script file path from payload
                        byte[] payloadData = new byte[payload.DataSize];
                        System.Runtime.InteropServices.Marshal.Copy(payload.Data, payloadData, 0, (int)payload.DataSize);
                        string scriptPath = System.Text.Encoding.UTF8.GetString(payloadData);

                        // Find the script and move it to this group
                        var script = LegionScripting.LegionScripting.LoadedScripts.FirstOrDefault(s => s.FullPath == scriptPath);
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
                foreach (var subGroup in subGroups)
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
                        foreach (var script in subGroup.Value)
                        {
                            DrawScript(script);
                        }
                    }
                }
            }

            ImGui.PopID();
        }

        private void DrawScript(ScriptFile script)
        {
            ImGui.PushID(script.FullPath);

            // Add menu button next to play button
            if (ImGui.Button("...", new Vector2(20, 0)))
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
                // Normal script display with double-click detection
                bool isSelected = false;
                if (ImGui.Selectable($"{displayName}", isSelected))
                {
                    // Handle double-click for rename
                    string scriptKey = script.FullPath;
                    if (_scriptDoubleClick.CheckDoubleClick(scriptKey))
                    {
                        // Start renaming
                        _renameState.StartScriptRename(script, displayName);
                    }
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
            if (string.IsNullOrWhiteSpace(_groupRenameBuffer))
            {
                _renamingGroup = null;
                _renamingParentGroup = null;
                _groupRenameBuffer = "";
                return;
            }

            try
            {
                // Build current group path
                string currentPath = LegionScripting.LegionScripting.ScriptPath;
                if (!string.IsNullOrEmpty(_renamingParentGroup))
                    currentPath = Path.Combine(currentPath, _renamingParentGroup);
                currentPath = Path.Combine(currentPath, _renamingGroup);

                // Build new group path
                string newPath = LegionScripting.LegionScripting.ScriptPath;
                if (!string.IsNullOrEmpty(_renamingParentGroup))
                    newPath = Path.Combine(newPath, _renamingParentGroup);
                newPath = Path.Combine(newPath, _groupRenameBuffer);

                // Check if the new group name already exists
                if (Directory.Exists(newPath) && !string.Equals(currentPath, newPath, StringComparison.OrdinalIgnoreCase))
                {
                    GameActions.Print(World.Instance, $"A group with the name '{_groupRenameBuffer}' already exists.", 32);
                    return;
                }

                // Check if current directory exists
                if (!Directory.Exists(currentPath))
                {
                    GameActions.Print(World.Instance, $"Source group '{_renamingGroup}' not found.", 32);
                    return;
                }

                // Perform the rename
                if (!string.Equals(currentPath, newPath, StringComparison.OrdinalIgnoreCase))
                {
                    Directory.Move(currentPath, newPath);
                    _pendingReload = true;
                    GameActions.Print(World.Instance, $"Renamed group '{_renamingGroup}' to '{_groupRenameBuffer}'", 66);
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
                Log.Error($"Error renaming group {_renamingGroup}: {ex}");
            }
            finally
            {
                _renamingGroup = null;
                _renamingParentGroup = null;
                _groupRenameBuffer = "";
            }
        }

        private void PerformDelete()
        {
            try
            {
                if (_scriptToDelete != null)
                {
                    // Delete script file
                    File.Delete(_scriptToDelete.FullPath);
                    LegionScripting.LegionScripting.LoadedScripts.Remove(_scriptToDelete);
                    GameActions.Print(World.Instance, $"Deleted script '{_scriptToDelete.FileName}'", 66);
                    _pendingReload = true;
                }
                else if (!string.IsNullOrEmpty(_groupToDelete))
                {
                    // Delete group folder
                    string gPath = string.IsNullOrEmpty(_groupToDeleteParent) ? _groupToDelete : Path.Combine(_groupToDeleteParent, _groupToDelete);
                    gPath = Path.Combine(LegionScripting.LegionScripting.ScriptPath, gPath);

                    if (Directory.Exists(gPath))
                    {
                        Directory.Delete(gPath, true);
                        GameActions.Print(World.Instance, $"Deleted group '{_groupToDelete}' and all its contents", 66);
                        _pendingReload = true;
                    }
                    else
                    {
                        GameActions.Print(World.Instance, $"Group '{_groupToDelete}' not found", 32);
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
                string itemType = _scriptToDelete != null ? "script" : "group";
                string itemName = _scriptToDelete != null ? _scriptToDelete.FileName : _groupToDelete;
                GameActions.Print(World.Instance, $"Error deleting {itemType}: {ex.Message}", 32);
                Log.Error($"Error deleting {itemType} {itemName}: {ex}");
            }
            finally
            {
                // Reset delete state
                _scriptToDelete = null;
                _groupToDelete = "";
                _groupToDeleteParent = "";
                _deleteConfirmMessage = "";
                _deleteConfirmTitle = "";
            }
        }

        private void DrawContextMenus()
        {
            if (_showContextMenu)
            {
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
            ImGui.Separator();

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
                    Macro mac = new Macro(script.FileName);
                    mac.Items = new MacroObjectString(MacroType.ClientCommand, MacroSubType.MSC_NONE, "togglelscript " + script.FileName);
                    mm.PushToBack(mac);

                    MacroButtonGump bg = new MacroButtonGump(World.Instance, mac, Mouse.Position.X, Mouse.Position.Y);
                    UIManager.Add(bg);
                }
                _showContextMenu = false;
            }

            if (ImGui.MenuItem("Delete"))
            {
                // Set up delete confirmation dialog for script
                _scriptToDelete = script;
                _groupToDelete = "";
                _groupToDeleteParent = "";
                _deleteConfirmTitle = "Delete Script";
                _deleteConfirmMessage = $"Are you sure you want to delete '{script.FileName}'?\n\nThis action cannot be undone.";
                _showDeleteConfirmDialog = true;
                _showContextMenu = false;
            }
        }

        private void DrawGroupContextMenu(string parentGroup, string groupName)
        {

            if (ImGui.MenuItem("New Script"))
            {
                _showNewScriptDialog = true;
                _showContextMenu = false;
            }

            if (string.IsNullOrEmpty(parentGroup))
            {
                if (ImGui.MenuItem("New Group"))
                {
                    _showNewGroupDialog = true;
                    _showContextMenu = false;
                }
            }

            if (groupName != NOGROUPTEXT && !string.IsNullOrEmpty(groupName))
            {
                if (ImGui.MenuItem("Rename Group"))
                {
                    _renamingGroup = groupName;
                    _renamingParentGroup = parentGroup;
                    _groupRenameBuffer = groupName;
                    _showRenameGroupDialog = true;
                    _showContextMenu = false;
                }

                if (ImGui.MenuItem("Delete Group"))
                {
                    // Set up delete confirmation dialog for group
                    _scriptToDelete = null;
                    _groupToDelete = groupName;
                    _groupToDeleteParent = parentGroup;
                    _deleteConfirmTitle = "Delete Group";
                    _deleteConfirmMessage = $"Are you sure you want to delete the group '{groupName}'?\n\nThis will permanently delete the folder and ALL scripts inside it.\nThis action cannot be undone.";
                    _showDeleteConfirmDialog = true;
                    _showContextMenu = false;
                }
            }
        }

        private void DrawDialogs()
        {
            // New Script Dialog
            if (_showNewScriptDialog)
            {
                ImGui.SetNextWindowPos(ImGui.GetMainViewport().GetCenter(), ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
                if (ImGui.Begin("New Script", ref _showNewScriptDialog, ImGuiWindowFlags.AlwaysAutoResize))
                {
                    ImGui.Text("Enter a name for this script.");
                    ImGui.Text("Use .lscript or .py extension");

                    ImGui.InputText("Script Name", ref _newScriptName, 100);

                    ImGui.Separator();

                    if (ImGui.Button("Create"))
                    {
                        if (!string.IsNullOrEmpty(_newScriptName))
                        {
                            if (_newScriptName.EndsWith(".lscript") || _newScriptName.EndsWith(".py"))
                            {
                                try
                                {
                                    // Normalize sentinels by replacing NOGROUPTEXT with empty string
                                    string normalizedGroup = _contextMenuGroup == NOGROUPTEXT ? "" : _contextMenuGroup;
                                    string normalizedSubGroup = _contextMenuSubGroup == NOGROUPTEXT ? "" : _contextMenuSubGroup;

                                    string gPath = string.IsNullOrEmpty(normalizedGroup) ? normalizedSubGroup :
                                        string.IsNullOrEmpty(normalizedSubGroup) ? normalizedGroup :
                                        Path.Combine(normalizedGroup, normalizedSubGroup);

                                    string filePath = Path.Combine(LegionScripting.LegionScripting.ScriptPath, gPath, _newScriptName);

                                    if (!Directory.Exists(Path.Combine(LegionScripting.LegionScripting.ScriptPath, gPath)))
                                        Directory.CreateDirectory(Path.Combine(LegionScripting.LegionScripting.ScriptPath, gPath));

                                    if (!File.Exists(filePath))
                                    {
                                        File.WriteAllText(filePath, SCRIPT_HEADER);
                                        _pendingReload = true;
                                    }
                                }
                                catch (Exception e)
                                {
                                    GameActions.Print(World.Instance, e.ToString(), 32);
                                }
                            }
                            else
                            {
                                GameActions.Print(World.Instance, "Script files must end with .lscript or .py", 32);
                            }
                        }

                        _newScriptName = "";
                        _showNewScriptDialog = false;
                    }

                    ImGui.SameLine();

                    if (ImGui.Button("Cancel"))
                    {
                        _newScriptName = "";
                        _showNewScriptDialog = false;
                    }
                }
                ImGui.End();
            }

            // New Group Dialog
            if (_showNewGroupDialog)
            {
                ImGui.SetNextWindowPos(ImGui.GetMainViewport().GetCenter(), ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
                if (ImGui.Begin("New Group", ref _showNewGroupDialog, ImGuiWindowFlags.AlwaysAutoResize))
                {
                    ImGui.Text("Enter a name for this group.");

                    ImGui.InputText("Group Name", ref _newGroupName, 100);

                    ImGui.Separator();

                    if (ImGui.Button("Create"))
                    {
                        if (!string.IsNullOrEmpty(_newGroupName))
                        {
                            int p = _newGroupName.IndexOf('.');
                            if (p != -1)
                                _newGroupName = _newGroupName.Substring(0, p);

                            try
                            {
                                // Build full path including parent group
                                string normalizedGroup = _contextMenuGroup == NOGROUPTEXT ? "" : _contextMenuGroup;
                                string normalizedSubGroup = _contextMenuSubGroup == NOGROUPTEXT ? "" : _contextMenuSubGroup;

                                string path = Path.Combine(LegionScripting.LegionScripting.ScriptPath,
                                    normalizedGroup ?? "",
                                    normalizedSubGroup ?? "",
                                    _newGroupName);

                                if (!Directory.Exists(path))
                                {
                                    Directory.CreateDirectory(path);
                                }
                                File.WriteAllText(Path.Combine(path, "Example.py"), EXAMPLE_LSCRIPT);
                                _pendingReload = true;
                            }
                            catch (Exception e)
                            {
                                Log.Error(e.ToString());
                            }
                        }

                        _newGroupName = "";
                        _showNewGroupDialog = false;
                    }

                    ImGui.SameLine();

                    if (ImGui.Button("Cancel"))
                    {
                        _newGroupName = "";
                        _showNewGroupDialog = false;
                    }
                }
                ImGui.End();
            }

            // Rename Group Dialog
            if (_showRenameGroupDialog)
            {
                ImGui.SetNextWindowPos(ImGui.GetMainViewport().GetCenter(), ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
                if (ImGui.Begin("Rename Group", ref _showRenameGroupDialog, ImGuiWindowFlags.AlwaysAutoResize))
                {
                    ImGui.Text($"Enter a new name for the group '{_renamingGroup}'.");

                    ImGui.InputText("##Group Name", ref _groupRenameBuffer, 100);

                    ImGui.Separator();

                    if (ImGui.Button("Save"))
                    {
                        if (!string.IsNullOrEmpty(_groupRenameBuffer))
                        {
                            int p = _groupRenameBuffer.IndexOf('.');
                            if (p != -1)
                                _groupRenameBuffer = _groupRenameBuffer.Substring(0, p);

                            PerformGroupRename();
                        }

                        _showRenameGroupDialog = false;
                    }

                    ImGui.SameLine();

                    if (ImGui.Button("Cancel"))
                    {
                        _renamingGroup = null;
                        _renamingParentGroup = null;
                        _groupRenameBuffer = "";
                        _showRenameGroupDialog = false;
                    }
                }
                ImGui.End();
            }

            // Delete Confirmation Dialog
            if (_showDeleteConfirmDialog)
            {
                ImGui.SetNextWindowPos(ImGui.GetMainViewport().GetCenter(), ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
                if (ImGui.Begin(_deleteConfirmTitle, ref _showDeleteConfirmDialog, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse))
                {
                    // Add warning icon color
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.8f, 0.0f, 1.0f)); // Orange/yellow warning color
                    ImGui.Text("⚠");
                    ImGui.PopStyleColor();
                    ImGui.SameLine();

                    ImGui.Text(_deleteConfirmMessage);

                    ImGui.Separator();

                    // Buttons with different colors
                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.8f, 0.2f, 0.2f, 1.0f)); // Red for delete
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.9f, 0.3f, 0.3f, 1.0f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.7f, 0.1f, 0.1f, 1.0f));

                    if (ImGui.Button("Delete"))
                    {
                        PerformDelete();
                        _showDeleteConfirmDialog = false;
                    }

                    ImGui.PopStyleColor(3);
                    ImGui.SameLine();

                    if (ImGui.Button("Cancel"))
                    {
                        // Reset delete state
                        _scriptToDelete = null;
                        _groupToDelete = "";
                        _groupToDeleteParent = "";
                        _deleteConfirmMessage = "";
                        _deleteConfirmTitle = "";
                        _showDeleteConfirmDialog = false;
                    }
                }
                ImGui.End();
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
                    Process.Start("xdg-open", filePath);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", filePath);
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
            _showMainMenu = false;
            _showContextMenu = false;
            _showNewScriptDialog = false;
            _showNewGroupDialog = false;
            _showRenameGroupDialog = false;
            _showDeleteConfirmDialog = false;
            _scriptToDelete = null;
            _groupToDelete = "";
            _groupToDeleteParent = "";
            _deleteConfirmMessage = "";
            _deleteConfirmTitle = "";
            _renameState.Clear();
            _scriptDoubleClick.Reset();
            _groupDoubleClick.Reset();
            _shouldCancelRename = false;
            _renamingGroup = null;
            _renamingParentGroup = null;
            _groupRenameBuffer = "";
            base.Dispose();
        }
    }
}
