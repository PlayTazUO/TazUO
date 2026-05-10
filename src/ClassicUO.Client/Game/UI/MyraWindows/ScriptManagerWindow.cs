using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using ClassicUO.Common.Enums;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.LegionScripting;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows;

public class ScriptManagerWindow : MyraControl
{
    private const string SCRIPT_HEADER =
        "# See examples at" +
        "\n#   https://github.com/PlayTazUO/PublicLegionScripts/" +
        "\n# Or documentation at" +
        "\n#   https://tazuo.org/legion/legionapi/";

    private const string NOGROUPTEXT = "No group";

    public static ScriptManagerWindow Instance { get; private set; }

    private const int MIN_WIDTH  = 200;
    private const int MIN_HEIGHT = 200;

    private readonly HashSet<string> _collapsedGroups = [];
    private bool _pendingReload = true;
    private string _searchFilter = "";
    private readonly VerticalStackPanel _scriptListPanel = new() { Spacing = 2 };

    // Tracks which group/subgroup the last context menu was invoked on
    private string _contextMenuGroup = "";
    private string _contextMenuSubGroup = "";

    private MyraGrid _mainGrid;

    public ScriptManagerWindow() : base("Script Manager")
    {
        Instance = this;
        CanBeSaved = true;
        Build();
        CenterInViewPort();
        LegionScripting.LegionScripting.ScriptStarted += OnScriptChanged;
        LegionScripting.LegionScripting.ScriptStopped += OnScriptChanged;
    }

    public static void Show()
    {
        foreach (IGui g in UIManager.Gumps)
        {
            if (g is ScriptManagerWindow w)
            {
                w.BringOnTop();
                return;
            }
        }
        UIManager.Add(new ScriptManagerWindow());
    }

    public override void Dispose()
    {
        LegionScripting.LegionScripting.ScriptStarted -= OnScriptChanged;
        LegionScripting.LegionScripting.ScriptStopped -= OnScriptChanged;
        if (Instance == this)
            Instance = null;
        base.Dispose();
    }

    private void OnScriptChanged(object sender, ScriptFile script) => RebuildScriptList();

    public void Refresh() => _pendingReload = true;

    public override void PreDraw()
    {
        base.PreDraw();

        if (_pendingReload)
        {
            _pendingReload = false;
            LegionScripting.LegionScripting.LoadScriptsFromFile();
            RebuildScriptList();
        }
    }

    public override void Save(XmlTextWriter xml)
    {
        base.Save(xml);
        xml.WriteAttributeString("width",  (_rootWindow.Width).ToString());
        xml.WriteAttributeString("height", (_rootWindow.Height).ToString());
    }

    public override void Load(XmlElement xml)
    {
        base.Load(xml);
        if (int.TryParse(xml.GetAttribute("width"),  out int w) && w >= MIN_WIDTH)  _rootWindow.Width  = w;
        if (int.TryParse(xml.GetAttribute("height"), out int h) && h >= MIN_HEIGHT) _rootWindow.Height = h;
    }

    private void Build()
    {
        _mainGrid = new MyraGrid();
        _rootWindow.Height = Math.Clamp(_rootWindow.Height ?? _rootWindow.Bounds.Height, StyleConstantsDefaults.WINDOW_MIN_HEIGHT, 600);
        _mainGrid.AddRow();                                           // Row 0: menu bar (Auto)
        _mainGrid.AddRow(new Proportion(ProportionType.Fill));        // Row 1: script list (Fill)
        _mainGrid.AddColumn(new Proportion(ProportionType.Fill));     // single Fill column

        _mainGrid.AddWidget(BuildMenuBar(), 0, 0);

        _mainGrid.AddWidget(_scriptListPanel, 1, 0);

        SetRootContent(_mainGrid);
    }

    private Widget BuildMenuBar()
    {
        var bar = new HorizontalStackPanel { Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
        bar.Widgets.Add(new MyraButton("Menu", ShowMainMenu));
        bar.Widgets.Add(new MyraButton("Add +", ShowAddMenu));

        var searchBox = new MyraInputBox { HintText = "Search...", Width = 180 };
        searchBox.TextChangedByUser += (_, _) =>
        {
            _searchFilter = searchBox.Text ?? "";
            RebuildScriptList();
        };
        bar.Widgets.Add(searchBox);
        return bar;
    }

    private void ShowMainMenu()
    {
        bool cacheDisabled = LegionScripting.LegionScripting.LScriptSettings.DisableModuleCache;
        ShowContextMenu(
            ("Refresh",                    () => _pendingReload = true),
            ("Public Script Browser",      ScriptBrowser.Show),
            ("Script Recording",           () => UIManager.Add(new ScriptRecordingGump())),
            ("Scripting Info",             ScriptingInfoGump.Show),
            ("Persistent Variables",       PersistentVarsWindow.Show),
            ("Running Scripts",            RunningScriptsWindow.Show),
            (ContextMenuLabelToggle(cacheDisabled, "Disable module cache"), () =>
                LegionScripting.LegionScripting.LScriptSettings.DisableModuleCache = !cacheDisabled)
        );
    }

    private void ShowAddMenu()
    {
        _contextMenuGroup = "";
        _contextMenuSubGroup = NOGROUPTEXT;
        ShowGroupContextMenu("", NOGROUPTEXT);
    }

    // ── Script list ───────────────────────────────────────────────────────

    private void RebuildScriptList()
    {
        _scriptListPanel.Widgets.Clear();

        bool hasFilter = !string.IsNullOrWhiteSpace(_searchFilter);

        var groupsMap = new Dictionary<string, Dictionary<string, List<ScriptFile>>>
        {
            { "", new Dictionary<string, List<ScriptFile>> { { "", new List<ScriptFile>() } } }
        };

        foreach (ScriptFile sf in LegionScripting.LegionScripting.LoadedScripts)
        {
            if (hasFilter && sf.FileName.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            if (!groupsMap.ContainsKey(sf.Group))
                groupsMap[sf.Group] = new Dictionary<string, List<ScriptFile>>();

            if (!groupsMap[sf.Group].ContainsKey(sf.SubGroup))
                groupsMap[sf.Group][sf.SubGroup] = new List<ScriptFile>();

            groupsMap[sf.Group][sf.SubGroup].Add(sf);
        }

        foreach (KeyValuePair<string, Dictionary<string, List<ScriptFile>>> group in groupsMap)
        {
            string groupName = string.IsNullOrEmpty(group.Key) ? NOGROUPTEXT : group.Key;
            BuildGroupWidgets(groupName, group.Value, "");
        }
    }

    private void BuildGroupWidgets(string groupName, Dictionary<string, List<ScriptFile>> subGroups, string parentGroup)
    {
        string fullGroupPath = string.IsNullOrEmpty(parentGroup) ? groupName : Path.Combine(parentGroup, groupName);
        string normalizedGroupName = groupName == NOGROUPTEXT ? "" : groupName;
        string normalizedParentGroup = parentGroup == NOGROUPTEXT ? "" : parentGroup;
        string indent = string.IsNullOrEmpty(parentGroup) ? "" : "   ";

        bool isCollapsedInSettings = string.IsNullOrEmpty(normalizedParentGroup)
            ? LegionScripting.LegionScripting.IsGroupCollapsed(normalizedGroupName)
            : LegionScripting.LegionScripting.IsGroupCollapsed(normalizedParentGroup, normalizedGroupName);

        if (isCollapsedInSettings && !_collapsedGroups.Contains(fullGroupPath))
            _collapsedGroups.Add(fullGroupPath);

        bool isCollapsed = _collapsedGroups.Contains(fullGroupPath);

        var groupRow = new HorizontalStackPanel { Spacing = 4, VerticalAlignment = VerticalAlignment.Center };

        if (!string.IsNullOrEmpty(indent))
            groupRow.Widgets.Add(new MyraLabel(indent, MyraLabel.TextStyle.P));

        groupRow.Widgets.Add(new MyraButton(isCollapsed ? "[+]" : "[-]", () =>
        {
            ToggleGroupState(isCollapsed, fullGroupPath, normalizedParentGroup, normalizedGroupName);
            RebuildScriptList();
        }));

        var groupLabel = new MyraLabel(groupName, MyraLabel.TextStyle.P);
        groupLabel.TouchDown += (s, e) =>
        {
            ToggleGroupState(isCollapsed, fullGroupPath, normalizedParentGroup, normalizedGroupName);
            RebuildScriptList();
        };
        groupRow.Widgets.Add(groupLabel);

        groupRow.Widgets.Add(new MyraButton("...", () => ShowGroupContextMenu(parentGroup, groupName)));

        _scriptListPanel.Widgets.Add(groupRow);

        if (isCollapsed) return;

        foreach (KeyValuePair<string, List<ScriptFile>> subGroup in subGroups)
        {
            if (!string.IsNullOrEmpty(subGroup.Key))
            {
                var subGroupData = new Dictionary<string, List<ScriptFile>> { { "", subGroup.Value } };
                BuildGroupWidgets(subGroup.Key, subGroupData, groupName);
            }
            else
            {
                foreach (ScriptFile script in subGroup.Value)
                    BuildScriptWidget(script, indent + "   ");
            }
        }
    }

    private void BuildScriptWidget(ScriptFile script, string indent)
    {
        var row = new HorizontalStackPanel { Spacing = 4, VerticalAlignment = VerticalAlignment.Center };

        if (!string.IsNullOrEmpty(indent))
            row.Widgets.Add(new MyraLabel(indent, MyraLabel.TextStyle.P));

        row.Widgets.Add(new MyraButton("...", () => ShowScriptContextMenu(script)));

        bool isPlaying = script.IsPlaying;
        var playStopBtn = new MyraButton(isPlaying ? "Stop" : "Play", () =>
        {
            if (script.IsPlaying)
                LegionScripting.LegionScripting.StopScript(script);
            else
                LegionScripting.LegionScripting.PlayScript(script);
            RebuildScriptList();
        });

        row.Widgets.Add(playStopBtn);

        bool hasGlobal = LegionScripting.LegionScripting.AutoLoadEnabled(script, true);
        bool hasChar = LegionScripting.LegionScripting.AutoLoadEnabled(script, false);

        if (hasGlobal || hasChar)
        {
            row.Widgets.Add(new MyraLabel(hasGlobal ? "[G]" : "[C]", MyraLabel.TextStyle.P)
            {
                TextColor = hasGlobal ? Color.Gold : new Color(0, 204, 255, 255),
                Tooltip = hasGlobal ? "Autostart: All characters" : "Autostart: This character"
            });
        }

        string displayName = script.FileName;
        int dot = displayName.LastIndexOf('.');
        if (dot != -1) displayName = displayName.Substring(0, dot);

        MyraLabel displayLabel;
        row.Widgets.Add(displayLabel = new MyraLabel(displayName, MyraLabel.TextStyle.P) { Tooltip = script.FileName });

        if (isPlaying)
        {
            displayLabel.Background = new SolidBrush(new Color(51, 153, 51, 255));
            displayLabel.Padding = new Thickness(2);
        }

        _scriptListPanel.Widgets.Add(row);
    }

    // ── Context menus ─────────────────────────────────────────────────────

    private void ShowScriptContextMenu(ScriptFile script)
    {
        bool globalAuto = LegionScripting.LegionScripting.AutoLoadEnabled(script, true);
        bool charAuto   = LegionScripting.LegionScripting.AutoLoadEnabled(script, false);

        ShowContextMenu(
            ("Edit Constants",       () => new ScriptConstantsEditorWindow(script)),
            ("Rename",               () => ShowRenameScriptDialog(script)),
            ("Edit",                 () => new ScriptEditorWindow(script)),
            ("Edit Externally",      () => FileSystemHelper.OpenFileWithDefaultApp(script.FullPath)),
            (Language.Instance.Scripting.OpenLocation, () =>
            {
                if (!FileSystemHelper.OpenLocation(script.FullPath))
                    GameActions.PrintUserWarn(World.Instance, string.Format(Language.Instance.Scripting.OpenLocationFailed, script.FullPath));
            }),
            (ContextMenuLabelToggle(globalAuto, "Autostart on all chars"), () =>
            {
                LegionScripting.LegionScripting.SetAutoPlay(script, true, !globalAuto);
                RebuildScriptList();
            }),
            (ContextMenuLabelToggle(charAuto, "Autostart for this char"), () =>
            {
                LegionScripting.LegionScripting.SetAutoPlay(script, false, !charAuto);
                RebuildScriptList();
            }),
            ("Create Macro Button", () =>
            {
                var mm = MacroManager.TryGetMacroManager(World.Instance);
                if (mm == null) return;
                var mac = new Macro(script.FileName);
                mac.Items = new MacroObjectString(MacroType.ClientCommand, MacroSubType.MSC_NONE, "togglelscript " + script.FileName);
                mm.PushToBack(mac);
                var bg = new MacroButtonGump(World.Instance, mac, 0, 0);
                bg.CenterXInViewPort();
                bg.CenterYInViewPort();
                UIManager.Add(bg);
            }),
            ("Delete", () => ShowDeleteConfirm(
                "Delete Script",
                $"Are you sure you want to delete '{script.FileName}'?\nThis action cannot be undone.",
                () => PerformDeleteScript(script)))
        );
    }

    private void ShowGroupContextMenu(string parentGroup, string groupName)
    {
        bool isRealGroup = groupName != NOGROUPTEXT && !string.IsNullOrEmpty(groupName);
        _contextMenuGroup    = parentGroup;
        _contextMenuSubGroup = groupName;

        var items = new List<(string, Action)>();

        if (isRealGroup)
            items.Add(("Rename Group", () => ShowRenameGroupDialog(groupName, parentGroup)));

        items.Add(("New Script", () => ShowNewScriptDialog(_contextMenuGroup, _contextMenuSubGroup)));

        if (string.IsNullOrEmpty(parentGroup))
            items.Add(("New Group", ShowNewGroupDialog));

        if (isRealGroup)
            items.Add(("Delete Group", () => ShowDeleteConfirm(
                "Delete Group",
                $"Delete group '{groupName}'?\nThis will permanently delete the folder and ALL scripts inside it.",
                () => PerformDeleteGroup(groupName, parentGroup))));

        ShowContextMenu(items.ToArray());
    }

    // ── Dialogs ───────────────────────────────────────────────────────────

    private void ShowNewScriptDialog(string contextGroup, string contextSubGroup)
    {
        var nameBox = new MyraInputBox { HintText = "script_name", Width = 220 };
        var content = new VerticalStackPanel { Spacing = 4 };
        content.Widgets.Add(new MyraLabel("Enter a name for this script:", MyraLabel.TextStyle.P));
        content.Widgets.Add(nameBox);

        new MyraDialog("New Script", content, ok =>
        {
            if (!ok) return;
            string name = nameBox.Text?.Trim() ?? "";
            if (!name.EndsWith(".py") && !name.EndsWith(".cs")) name += ".py";
            CreateScript(name, contextGroup, contextSubGroup);
        });
    }

    private void ShowNewGroupDialog()
    {
        var nameBox = new MyraInputBox { HintText = "group_name", Width = 220 };
        var content = new VerticalStackPanel { Spacing = 4 };
        content.Widgets.Add(new MyraLabel("Enter a name for this group:", MyraLabel.TextStyle.P));
        content.Widgets.Add(nameBox);

        new MyraDialog("New Group", content, ok =>
        {
            if (!ok) return;
            CreateGroup(nameBox.Text?.Trim() ?? "", _contextMenuGroup, _contextMenuSubGroup);
        });
    }

    private void ShowRenameScriptDialog(ScriptFile script)
    {
        string displayName = script.FileName;
        int dot = displayName.LastIndexOf('.');
        if (dot != -1) displayName = displayName.Substring(0, dot);

        var nameBox = new MyraInputBox { Text = displayName, Width = 220 };
        var content = new VerticalStackPanel { Spacing = 4 };
        content.Widgets.Add(new MyraLabel($"New name for '{displayName}':", MyraLabel.TextStyle.P));
        content.Widgets.Add(nameBox);

        new MyraDialog("Rename Script", content, ok =>
        {
            if (ok) PerformRenameScript(script, nameBox.Text?.Trim() ?? "");
        });
    }

    private void ShowRenameGroupDialog(string groupName, string parentGroup)
    {
        var nameBox = new MyraInputBox { Text = groupName, Width = 220 };
        var content = new VerticalStackPanel { Spacing = 4 };
        content.Widgets.Add(new MyraLabel($"New name for group '{groupName}':", MyraLabel.TextStyle.P));
        content.Widgets.Add(nameBox);

        new MyraDialog("Rename Group", content, ok =>
        {
            if (ok) PerformRenameGroup(groupName, parentGroup, nameBox.Text?.Trim() ?? "");
        });
    }

    private void ShowDeleteConfirm(string title, string message, Action onConfirm)
    {
        var label = new MyraLabel(message, MyraLabel.TextStyle.P) { TextColor = Color.OrangeRed };
        new MyraDialog(title, label, ok => { if (ok) onConfirm(); });
    }

    // ── Group state ───────────────────────────────────────────────────────

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

    // ── File operations ───────────────────────────────────────────────────

    private void CreateScript(string name, string contextGroup, string contextSubGroup)
    {
        if (string.IsNullOrEmpty(name)) return;

        string sanitizedName = Path.GetFileName(name.Trim());
        if (string.IsNullOrWhiteSpace(sanitizedName) || sanitizedName != name.Trim() ||
            sanitizedName.Contains('\\') || sanitizedName.Contains('/') ||
            sanitizedName.Contains("..") || sanitizedName is "." or "..")
        {
            GameActions.Print(World.Instance, "Invalid script name.", 32);
            return;
        }

        try
        {
            string normalizedGroup    = contextGroup    == NOGROUPTEXT ? "" : contextGroup;
            string normalizedSubGroup = contextSubGroup == NOGROUPTEXT ? "" : contextSubGroup;
            if (!string.IsNullOrEmpty(normalizedGroup))    normalizedGroup    = Path.GetFileName(normalizedGroup);
            if (!string.IsNullOrEmpty(normalizedSubGroup)) normalizedSubGroup = Path.GetFileName(normalizedSubGroup);

            string gPath = string.IsNullOrEmpty(normalizedGroup)    ? normalizedSubGroup :
                           string.IsNullOrEmpty(normalizedSubGroup) ? normalizedGroup :
                           Path.Combine(normalizedGroup, normalizedSubGroup);

            string targetDirectory  = Path.Combine(LegionScripting.LegionScripting.ScriptPath, gPath ?? "");
            string scriptsRoot      = Path.GetFullPath(LegionScripting.LegionScripting.ScriptPath);
            string targetDirFull    = Path.GetFullPath(targetDirectory);
            string targetFileFull   = Path.GetFullPath(Path.Combine(targetDirectory, sanitizedName));

            if (!targetDirFull.StartsWith(scriptsRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) &&
                !targetDirFull.Equals(scriptsRoot, StringComparison.OrdinalIgnoreCase))
            {
                GameActions.Print(World.Instance, "Invalid target directory.", 32);
                return;
            }

            if (!Directory.Exists(targetDirFull)) Directory.CreateDirectory(targetDirFull);

            if (!File.Exists(targetFileFull))
            {
                File.WriteAllText(targetFileFull, SCRIPT_HEADER);
                _pendingReload = true;
                GameActions.Print(World.Instance, $"Created script '{sanitizedName}'", 66);
            }
            else
            {
                GameActions.Print(World.Instance, $"A script named '{sanitizedName}' already exists.", 32);
            }
        }
        catch (UnauthorizedAccessException) { GameActions.Print(World.Instance, "Access denied.", 32); }
        catch (IOException ioEx) { GameActions.Print(World.Instance, $"File operation failed: {ioEx.Message}", 32); }
        catch (Exception e) { GameActions.Print(World.Instance, $"Error creating script: {e.Message}", 32); Log.Error(e.ToString()); }
    }

    private void CreateGroup(string name, string contextGroup, string contextSubGroup)
    {
        if (string.IsNullOrEmpty(name)) return;

        string sanitizedName = Path.GetFileName(name.Trim());
        int p = sanitizedName.IndexOf('.');
        if (p != -1) sanitizedName = sanitizedName.Substring(0, p);

        if (string.IsNullOrEmpty(sanitizedName) || sanitizedName != name.Trim() ||
            sanitizedName.Contains('\\') || sanitizedName.Contains('/') ||
            sanitizedName is ".." or ".")
        {
            GameActions.Print(World.Instance, "Invalid group name.", 32);
            return;
        }

        try
        {
            string normalizedGroup    = contextGroup    == NOGROUPTEXT ? "" : contextGroup;
            string normalizedSubGroup = contextSubGroup == NOGROUPTEXT ? "" : contextSubGroup;
            if (!string.IsNullOrEmpty(normalizedGroup))    normalizedGroup    = Path.GetFileName(normalizedGroup);
            if (!string.IsNullOrEmpty(normalizedSubGroup)) normalizedSubGroup = Path.GetFileName(normalizedSubGroup);

            string path = Path.Combine(LegionScripting.LegionScripting.ScriptPath,
                normalizedGroup ?? "", normalizedSubGroup ?? "", sanitizedName);

            string scriptsRoot = Path.GetFullPath(LegionScripting.LegionScripting.ScriptPath);
            string targetPath  = Path.GetFullPath(path);

            if (!targetPath.StartsWith(scriptsRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) &&
                !targetPath.Equals(scriptsRoot, StringComparison.OrdinalIgnoreCase))
            {
                GameActions.Print(World.Instance, "Invalid group location.", 32);
                return;
            }

            if (!Directory.Exists(targetPath)) Directory.CreateDirectory(targetPath);
            File.WriteAllText(Path.Combine(targetPath, "Example.py"), "import API");
            _pendingReload = true;
            GameActions.Print(World.Instance, $"Created group '{sanitizedName}'", 66);
        }
        catch (UnauthorizedAccessException) { GameActions.Print(World.Instance, "Access denied.", 32); }
        catch (IOException ioEx) { GameActions.Print(World.Instance, $"Directory operation failed: {ioEx.Message}", 32); }
        catch (Exception e) { GameActions.Print(World.Instance, $"Error creating group: {e.Message}", 32); Log.Error(e.ToString()); }
    }

    private void PerformRenameScript(ScriptFile script, string newDisplayName)
    {
        if (string.IsNullOrWhiteSpace(newDisplayName)) return;

        try
        {
            string originalExtension = Path.GetExtension(script.FileName);
            string newName = newDisplayName.EndsWith(originalExtension, StringComparison.OrdinalIgnoreCase)
                ? newDisplayName : newDisplayName + originalExtension;

            string directory = Path.GetDirectoryName(script.FullPath)!;
            string newPath   = Path.Combine(directory, newName);

            if (File.Exists(newPath) && !string.Equals(script.FullPath, newPath))
            {
                GameActions.Print(World.Instance, $"A file named '{newName}' already exists.", 32);
                return;
            }

            if (!string.Equals(script.FullPath, newPath))
            {
                File.Move(script.FullPath, newPath);
                script.FullPath  = newPath;
                script.FileName  = newName;
                _pendingReload   = true;
            }
        }
        catch (Exception ex) { GameActions.Print(World.Instance, $"Error renaming script: {ex.Message}", 32); }
    }

    private void PerformRenameGroup(string groupName, string parentGroup, string newName)
    {
        if (string.IsNullOrWhiteSpace(newName)) return;

        int p = newName.IndexOf('.');
        if (p != -1) newName = newName.Substring(0, p);

        try
        {
            string currentPath = LegionScripting.LegionScripting.ScriptPath;
            if (!string.IsNullOrEmpty(parentGroup)) currentPath = Path.Combine(currentPath, parentGroup);
            currentPath = Path.Combine(currentPath, groupName);

            string newPath = LegionScripting.LegionScripting.ScriptPath;
            if (!string.IsNullOrEmpty(parentGroup)) newPath = Path.Combine(newPath, parentGroup);
            newPath = Path.Combine(newPath, newName);

            if (Directory.Exists(newPath) && !string.Equals(currentPath, newPath, StringComparison.OrdinalIgnoreCase))
            {
                GameActions.Print(World.Instance, $"A group named '{newName}' already exists.", 32);
                return;
            }
            if (!Directory.Exists(currentPath))
            {
                GameActions.Print(World.Instance, $"Source group '{groupName}' not found.", 32);
                return;
            }
            if (!string.Equals(currentPath, newPath, StringComparison.OrdinalIgnoreCase))
            {
                Directory.Move(currentPath, newPath);
                _pendingReload = true;
                GameActions.Print(World.Instance, $"Renamed group '{groupName}' to '{newName}'", 66);
            }
        }
        catch (UnauthorizedAccessException) { GameActions.Print(World.Instance, "Access denied.", 32); }
        catch (DirectoryNotFoundException)  { GameActions.Print(World.Instance, "Directory not found.", 32); }
        catch (IOException ioEx) { GameActions.Print(World.Instance, $"Directory operation failed: {ioEx.Message}", 32); }
        catch (Exception ex) { GameActions.Print(World.Instance, $"Error renaming group: {ex.Message}", 32); Log.Error(ex.ToString()); }
    }

    private void PerformDeleteScript(ScriptFile script)
    {
        try
        {
            File.Delete(script.FullPath);
            LegionScripting.LegionScripting.LoadedScripts.Remove(script);
            _pendingReload = true;
            GameActions.Print(World.Instance, $"Deleted script '{script.FileName}'", 66);
        }
        catch (Exception ex) { GameActions.Print(World.Instance, $"Error deleting script: {ex.Message}", 32); Log.Error(ex.ToString()); }
    }

    private void PerformDeleteGroup(string groupName, string parentGroup)
    {
        try
        {
            string gPath = string.IsNullOrEmpty(parentGroup) ? groupName : Path.Combine(parentGroup, groupName);
            gPath = Path.Combine(LegionScripting.LegionScripting.ScriptPath, gPath);

            if (!Directory.Exists(gPath))
            {
                GameActions.Print(World.Instance, $"Group '{groupName}' not found", 32);
                return;
            }

            Directory.Delete(gPath, true);
            _pendingReload = true;
            GameActions.Print(World.Instance, $"Deleted group '{groupName}' and all its contents", 66);
        }
        catch (UnauthorizedAccessException) { GameActions.Print(World.Instance, "Access denied.", 32); }
        catch (IOException ioEx) { GameActions.Print(World.Instance, $"Delete operation failed: {ioEx.Message}", 32); }
        catch (Exception ex) { GameActions.Print(World.Instance, $"Error deleting group: {ex.Message}", 32); Log.Error(ex.ToString()); }
    }
}
