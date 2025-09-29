using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.LegionScripting;
using ClassicUO.Utility.Logging;
using ImGuiNET;
using Microsoft.Xna.Framework;
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
        private ScriptFile _contextMenuScript = null;
        private Vector2 _contextMenuPosition;
        private bool _showMainMenu = false;
        private bool _pendingReload = false;
        private ScriptFile _renamingScript = null;
        private string _renameBuffer = "";
        private double _lastClickTime = 0.0;
        private ScriptFile _lastClickedScript = null;

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

        private ScriptManagerWindow() : base("Script Manager")
        {
            WindowFlags = ImGuiWindowFlags.None;
            _pendingReload = true;
        }

        public override void DrawContent()
        {
            // Load scripts if needed
            if (_pendingReload)
            {
                LegionScripting.LegionScripting.LoadScriptsFromFile();
                _pendingReload = false;
            }

            // Cancel rename if user clicks outside
            if (_renamingScript != null && ImGui.IsMouseClicked(ImGuiMouseButton.Left) && !ImGui.IsAnyItemActive())
            {
                _renamingScript = null;
                _renameBuffer = "";
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
        }

        private void DrawMenuBar()
        {
            if (ImGui.Button("Menu"))
            {
                _showMainMenu = !_showMainMenu;
            }

            if (_showMainMenu)
            {
                ImGui.SetNextWindowPos(ImGui.GetCursorScreenPos());
                if (ImGui.Begin("ScriptManagerMenu", ref _showMainMenu,
                    ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysAutoResize))
                {
                    if (ImGui.MenuItem("Refresh"))
                    {
                        _pendingReload = true;
                        _showMainMenu = false;
                    }

                    if (ImGui.MenuItem("Public Script Browser"))
                    {
                        UIManager.Add(new ScriptBrowser(World.Instance));
                        _showMainMenu = false;
                    }

                    if (ImGui.MenuItem("Script Recording"))
                    {
                        UIManager.Add(new ScriptRecordingGump());
                        _showMainMenu = false;
                    }

                    if (ImGui.MenuItem("Scripting Info"))
                    {
                        ScriptingInfoGump.Show();
                        _showMainMenu = false;
                    }

                    bool disableCache = LegionScripting.LegionScripting.LScriptSettings.DisableModuleCache;
                    if (ImGui.Checkbox("Disable Module Cache", ref disableCache))
                    {
                        LegionScripting.LegionScripting.LScriptSettings.DisableModuleCache = disableCache;
                    }
                }
                ImGui.End();
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

            // Set the default open state based on collapsed state
            ImGui.SetNextItemOpen(!isCollapsed, ImGuiCond.Once);

            // Call TreeNode and store the open state
            bool nodeOpen = ImGui.TreeNode(groupName);

            // Only toggle collapsed state when left-clicked
            if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
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

            // Right-click context menu for group
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                _showContextMenu = true;
                _contextMenuGroup = parentGroup;
                _contextMenuSubGroup = groupName;
                _contextMenuScript = null;
                _contextMenuPosition = ImGui.GetMousePos();
            }

            // If node is open, render children and call TreePop
            if (nodeOpen)
            {
                ImGui.Indent();

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

                ImGui.Unindent();
                ImGui.TreePop();
            }

            ImGui.PopID();
        }

        private void DrawScript(ScriptFile script)
        {
            ImGui.PushID(script.FullPath);

            // Get script display name (without extension)
            string displayName = script.FileName;
            int lastDotIndex = displayName.LastIndexOf('.');
            if (lastDotIndex != -1)
                displayName = displayName.Substring(0, lastDotIndex);

            // Check if script is playing
            bool isPlaying = script.IsPlaying || (script.GetScript != null && script.GetScript.IsPlaying);

            // Script status color
            //Vector4 scriptColor = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);

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
            //ImGui.PushStyleColor(ImGuiCol.Text, scriptColor);

            if (_renamingScript == script)
            {
                // Show rename input and save button
                ImGui.SetKeyboardFocusHere();
                if (ImGui.InputText("##rename", ref _renameBuffer, 256, ImGuiInputTextFlags.EnterReturnsTrue))
                {
                    PerformRename(script);
                }

                ImGui.SameLine();
                if (ImGui.Button("Save##rename"))
                {
                    PerformRename(script);
                }

                ImGui.SameLine();
                if (ImGui.Button("Cancel##rename"))
                {
                    _renamingScript = null;
                    _renameBuffer = "";
                }
            }
            else
            {
                // Normal script display with double-click detection
                bool isSelected = false;
                if (ImGui.Selectable($"  {displayName}", isSelected))
                {
                    // Handle double-click for rename
                    double currentTime = ImGui.GetTime();
                    if (_lastClickedScript == script && (currentTime - _lastClickTime) < 0.5) // 500ms for double-click
                    {
                        // Start renaming
                        _renamingScript = script;
                        _renameBuffer = displayName;
                    }
                    _lastClickedScript = script;
                    _lastClickTime = currentTime;
                }
            }

            //ImGui.PopStyleColor();

            // Tooltip with full filename (only when not renaming)
            if (_renamingScript != script && ImGui.IsItemHovered())
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

        private void PerformRename(ScriptFile script)
        {
            if (string.IsNullOrWhiteSpace(_renameBuffer))
            {
                _renamingScript = null;
                _renameBuffer = "";
                return;
            }

            try
            {
                // Get the original extension
                string originalExtension = Path.GetExtension(script.FileName);

                // Ensure the new name has the correct extension
                string newName = _renameBuffer;
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
                _renamingScript = null;
                _renameBuffer = "";
            }
        }

        private void DrawContextMenus()
        {
            if (_showContextMenu)
            {
                ImGui.SetNextWindowPos(_contextMenuPosition);
                ImGui.SetNextWindowSize(new Vector2(200, 0));

                if (ImGui.Begin("ContextMenu", ref _showContextMenu,
                    ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.AlwaysAutoResize))
                {
                    // Check for outside click or Escape key to dismiss menu
                    if ((ImGui.IsMouseClicked(ImGuiMouseButton.Left) && !ImGui.IsWindowHovered(ImGuiHoveredFlags.RootAndChildWindows)) ||
                        ImGui.IsKeyPressed(ImGuiKey.Escape))
                    {
                        _showContextMenu = false;
                    }

                    if (_contextMenuScript != null)
                    {
                        DrawScriptContextMenu(_contextMenuScript);
                    }
                    else
                    {
                        DrawGroupContextMenu(_contextMenuGroup, _contextMenuSubGroup);
                    }
                }
                ImGui.End();
            }
        }

        private void DrawScriptContextMenu(ScriptFile script)
        {
            ImGui.Text(script.FileName);
            ImGui.Separator();

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
                QuestionGump g = new QuestionGump(World.Instance, "Are you sure?", (r) =>
                {
                    if (r)
                    {
                        try
                        {
                            File.Delete(script.FullPath);
                            LegionScripting.LegionScripting.LoadedScripts.Remove(script);
                        }
                        catch (Exception) { }
                    }
                });
                UIManager.Add(g);
                _showContextMenu = false;
            }
        }

        private void DrawGroupContextMenu(string parentGroup, string groupName)
        {
            if (ImGui.MenuItem("New script"))
            {
                _showNewScriptDialog = true;
                _showContextMenu = false;
            }

            if (string.IsNullOrEmpty(parentGroup))
            {
                if (ImGui.MenuItem("New group"))
                {
                    _showNewGroupDialog = true;
                    _showContextMenu = false;
                }
            }

            if (groupName != NOGROUPTEXT && !string.IsNullOrEmpty(groupName))
            {
                if (ImGui.MenuItem("Delete group"))
                {
                    QuestionGump g = new QuestionGump(World.Instance, "Delete group?", (r) =>
                    {
                        if (r)
                        {
                            try
                            {
                                string gPath = string.IsNullOrEmpty(parentGroup) ? groupName : Path.Combine(parentGroup, groupName);
                                gPath = Path.Combine(LegionScripting.LegionScripting.ScriptPath, gPath);
                                Directory.Delete(gPath, true);
                                _pendingReload = true;
                            }
                            catch (Exception) { }
                        }
                    });
                    UIManager.Add(g);
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

        public override void Dispose()
        {
            _showMainMenu = false;
            _showContextMenu = false;
            _showNewScriptDialog = false;
            _showNewGroupDialog = false;
            _renamingScript = null;
            _renameBuffer = "";
            _lastClickedScript = null;
            base.Dispose();
        }
    }
}
