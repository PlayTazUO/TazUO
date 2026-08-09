using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using ClassicUO.Assets;
using ClassicUO.Common;
using ClassicUO.Common.Enums;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Managers.Hotkeys;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Game.UI.MyraWindows.Options.Tabs;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.LegionScripting;
using ClassicUO.Utility;
using ClassicUO.Utility.Debounce;
using ClassicUO.Utility.Logging;
using FontStashSharp.RichText;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.WrapPanel;

namespace ClassicUO.Game.UI.MyraWindows;

public class ScriptManagerWindow : MyraControl
{
    private const string SCRIPT_HEADER =
        "# See examples at" +
        "\n#   https://github.com/PlayTazUO/PublicLegionScripts/" +
        "\n# Or documentation at" +
        "\n#   https://tazuo.org/legion/legionapi/";

    private static string NoGroupText => TazLang.Get("scriptmanager_nogroup", "No group");

    public static ScriptManagerWindow Instance { get; private set; }

    private readonly HashSet<string> _collapsedGroups = [];
    private bool _pendingReload = true;
    private string _searchFilter = "";
    private readonly VerticalStackPanel _scriptListPanel = new() { Spacing = 2, Padding = new Thickness(2, 0, 2, 4) };

    // Tracks which group/subgroup the last context menu was invoked on
    private string _contextMenuGroup = "";
    private string _contextMenuSubGroup = "";

    private MyraGrid _mainGrid;

    // Resizing fires this on every mouse-move tick; debounce so we're not hitting the settings DB
    // on every pixel, only once the drag settles.
    private readonly Debounce<Point?> _windowSizeDebounce = new(
        size => MainThreadQueue.EnqueueAction(() =>
        {
            if (size.HasValue)
            {
                if (size.Value.X <= 0)
                    size = new Point(100, size.Value.Y);
                
                if (size.Value.Y <= 0)
                    size = new Point(size.Value.X, 100);
                ProfileManager.CurrentProfile?.ScriptManagerWindowSize = size.Value;
            }
        }),
        350
    );

    public ScriptManagerWindow() : base(TazLang.Get("scriptmanager_title", "Script Manager"))
    {
        Instance = this;
        CanBeSaved = true;
        Build();
        RestoreWindowState();
        LegionScripting.LegionScripting.ScriptStarted += OnScriptChanged;
        LegionScripting.LegionScripting.ScriptStopped += OnScriptChanged;
    }

    public static void Show()
    {
        foreach (IGui g in UIManager.Gumps)
        {
            if (g is ScriptManagerWindow w)
            {
                // Keep the window where the user left it; SetInScreen guards against it being
                // fully off-screen without clobbering the remembered position.
                w.SetInScreen();
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
        _rootWindow.LocationChanged -= OnWindowLocationChanged;
        _windowSizeDebounce.Flush();
        _windowSizeDebounce.Dispose();
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

    // Restores the window's size and position from the profile (persisted in SqlProfile). Falls
    // back to auto-sizing / centering when a value has not been saved yet.
    private void RestoreWindowState()
    {
        // Persist user-set size. Note we have to debounce here since the resize even can be raised on every game tick.
        _rootWindow.Props.InitialSizeStore = new Accessor<Point?>(
            () => ProfileManager.CurrentProfile?.ScriptManagerWindowSize == Point.Zero ? null : ProfileManager.CurrentProfile?.ScriptManagerWindowSize,
            size => _windowSizeDebounce.Invoke(size)
        );

        // Restore position or center when it has never been saved. Persist future moves.
        Point? position = ProfileManager.CurrentProfile?.ScriptManagerWindowPosition;
        if (position.HasValue && position.Value != Point.Zero)
        {
            SetPosition(position.Value.X, position.Value.Y);
            SetInScreen();
        }
        else
            CenterInViewPort();

        _rootWindow.LocationChanged += OnWindowLocationChanged;
    }

    private void OnWindowLocationChanged(object sender, EventArgs e) => ProfileManager.CurrentProfile?.ScriptManagerWindowPosition = new Point(_rootWindow.Left, _rootWindow.Top);

    private void Build()
    {
        _mainGrid = new MyraGrid();
        _mainGrid.AddRow();                                           // Row 0: menu bar (Auto)
        _mainGrid.AddRow(new Proportion(ProportionType.Fill));        // Row 1: script list (Fill)
        _mainGrid.AddColumn(new Proportion(ProportionType.Fill));     // single Fill column

        _mainGrid.AddWidget(BuildMenuBar(), 0, 0);

        _mainGrid.AddWidget(_scriptListPanel, 1, 0);

        SetRootContent(_mainGrid);
    }

    private WrapPanel BuildMenuBar()
    {
        var bar = new WrapPanel
        {
            UniformSizing = false,
            Orientation = Orientation.Horizontal,
            HorizontalSpacing = 4,
            VerticalSpacing = 4,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 4)
        };

        bar.Widgets.Add(new MyraButton(TazLang.Get("scriptmanager_menu", "Menu"), ShowMainMenu));
        bar.Widgets.Add(new MyraButton(TazLang.Get("scriptmanager_addbutton", "Add +"), ShowAddMenu));

        var searchBox = new MyraInputBox { HintText = TazLang.Get("scriptmanager_search_hint", "Search..."), Width = 180 };
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
            (TazLang.Get("scriptmanager_refresh", "Refresh"),                    () => _pendingReload = true),
            (TazLang.Get("scriptmanager_publicbrowser", "Public Script Browser"),      ScriptBrowser.Show),
            (TazLang.Get("scriptmanager_scriptrecording", "Script Recording"),           () => UIManager.Add(new ScriptRecordingGump())),
            (TazLang.Get("scriptmanager_scriptinginfo", "Scripting Info"),             ScriptingInfoGump.Show),
            (TazLang.Get("scriptmanager_persistentvars", "Persistent Variables"),       PersistentVarsWindow.Show),
            (TazLang.Get("scriptmanager_runningscripts", "Running Scripts"),           RunningScriptsWindow.Show),
            (ContextMenuLabelToggle(cacheDisabled, TazLang.Get("scriptmanager_disablemodulecache", "Disable module cache")), () =>
                LegionScripting.LegionScripting.LScriptSettings.DisableModuleCache = !cacheDisabled)
        );
    }

    private void ShowAddMenu()
    {
        _contextMenuGroup = "";
        _contextMenuSubGroup = NoGroupText;
        ShowGroupContextMenu("", NoGroupText);
    }

    // ── Script list ───────────────────────────────────────────────────────

    private void RebuildScriptList()
    {
        _scriptListPanel.Widgets.Clear();

        bool hasFilter = !string.IsNullOrWhiteSpace(_searchFilter);

        var groupsMap = new Dictionary<string, Dictionary<string, List<ScriptFile>>>
        {
            { "", new Dictionary<string, List<ScriptFile>> { { "", [] } } }
        };

        foreach (ScriptFile sf in LegionScripting.LegionScripting.LoadedScripts)
        {
            if (hasFilter && sf.FileName.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            if (!groupsMap.ContainsKey(sf.Group))
                groupsMap[sf.Group] = new Dictionary<string, List<ScriptFile>>();

            if (!groupsMap[sf.Group].ContainsKey(sf.SubGroup))
                groupsMap[sf.Group][sf.SubGroup] = [];

            groupsMap[sf.Group][sf.SubGroup].Add(sf);
        }

        foreach (KeyValuePair<string, Dictionary<string, List<ScriptFile>>> group in groupsMap)
        {
            string groupName = string.IsNullOrEmpty(group.Key) ? NoGroupText : group.Key;
            BuildGroupWidgets(groupName, group.Value, "");
        }
    }

    private void BuildGroupWidgets(string groupName, Dictionary<string, List<ScriptFile>> subGroups, string parentGroup)
    {
        string fullGroupPath = string.IsNullOrEmpty(parentGroup) ? groupName : Path.Combine(parentGroup, groupName);
        string normalizedGroupName = groupName == NoGroupText ? "" : groupName;
        string normalizedParentGroup = parentGroup == NoGroupText ? "" : parentGroup;
        string indent = string.IsNullOrEmpty(parentGroup) ? "" : "   ";

        bool isCollapsedInSettings = string.IsNullOrEmpty(normalizedParentGroup)
            ? LegionScripting.LegionScripting.IsGroupCollapsed(normalizedGroupName)
            : LegionScripting.LegionScripting.IsGroupCollapsed(normalizedParentGroup, normalizedGroupName);

        if (isCollapsedInSettings)
            _collapsedGroups.Add(fullGroupPath);

        bool isCollapsed = _collapsedGroups.Contains(fullGroupPath);

        var groupRow = new HorizontalStackPanel
        {
            Spacing = 4,
            VerticalAlignment = VerticalAlignment.Center
        };

        if (!string.IsNullOrEmpty(indent))
            groupRow.Widgets.Add(new MyraLabel(indent, MyraLabel.TextStyle.P));

        groupRow.Widgets.Add(CreateCollapseExpandButton(isCollapsed, fullGroupPath, normalizedParentGroup, normalizedGroupName));

        var groupLabel = new MyraLabel(groupName, MyraLabel.TextStyle.P);
        groupLabel.TouchDown += (s, e) =>
        {
            ToggleGroupState(isCollapsed, fullGroupPath, normalizedParentGroup, normalizedGroupName);
            RebuildScriptList();
        };
        groupRow.Widgets.Add(groupLabel);

        groupRow.Widgets.Add(new MyraButton("...", () => ShowGroupContextMenu(parentGroup, groupName)));

        // Add the group to the actual parent script panel
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

    /// <summary>
    /// Create a Collapse/Expand button for a script group
    /// </summary>
    /// <param name="isCollapsed">Whether the group is currently collapsed</param>
    /// <param name="fullGroupPath">The group's full FS path</param>
    /// <param name="normalizedParentGroup">The normalized parent group name</param>
    /// <param name="normalizedGroupName">The normalized group name</param>
    /// <returns>A read-to-use button for the given group/state</returns>
    private BasicButton CreateCollapseExpandButton(
        bool isCollapsed,
        string fullGroupPath,
        string normalizedParentGroup,
        string normalizedGroupName
    )
    {
        return new IconButton(
            isCollapsed ? "⮞" : "⮟",
            () =>
            {
                ToggleGroupState(isCollapsed, fullGroupPath, normalizedParentGroup, normalizedGroupName);
                RebuildScriptList();
            },
            glyphSize: StyleConstantsDefaults.RESET_ICON_FONT_SIZE
        );
    }

    private void BuildScriptWidget(ScriptFile script, string indent)
    {
        var row = new HorizontalStackPanel { Spacing = 4, VerticalAlignment = VerticalAlignment.Center };

        if (!string.IsNullOrEmpty(indent))
            row.Widgets.Add(new MyraLabel(indent, MyraLabel.TextStyle.P));

        row.Widgets.Add(new MyraButton("...", () => ShowScriptContextMenu(script)));

        bool isPlaying = script.IsPlaying;
        string playBtnText = isPlaying
            ? TazLang.Get("scriptmanager_stopbutton", "Stop")
            : TazLang.Get("scriptmanager_playbutton", "Play");

        var playStopBtn = new MyraButton(playBtnText, () =>
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
            string autostartTooltip = hasGlobal
                ? TazLang.Get("scriptmanager_autostart_global_tooltip", "Autostart: All characters")
                : TazLang.Get("scriptmanager_autostart_char_tooltip", "Autostart: This character");

            row.Widgets.Add(new MyraLabel(hasGlobal ? "[G]" : "[C]", MyraLabel.TextStyle.P)
            {
                TextColor = hasGlobal ? Color.Gold : new Color(0, 204, 255, 255),
                Tooltip = autostartTooltip
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
        bool isZip      = script is ZipScriptFile;

        var items = new List<(string, Action)>
        {
            (TazLang.Get("scriptmanager_editconstants", "Edit Constants"), () => new ScriptConstantsEditorWindow(script)),
            (TazLang.Get("scriptmanager_edit", "Edit"),           () => new ScriptEditorWindow(script))
        };

        if (!isZip)
        {
            items.Add((TazLang.Get("scriptmanager_rename", "Rename"),          () => ShowRenameScriptDialog(script)));
            items.Add((TazLang.Get("scriptmanager_editexternally", "Edit Externally"), () => FileSystemHelper.OpenFileWithDefaultApp(script.FullPath)));
            items.Add((TazLang.Get("scripting_openlocation"), () =>
            {
                if (!FileSystemHelper.OpenLocation(script.FullPath))
                    GameActions.PrintUserWarn(World.Instance, TazLang.Get("scripting_openlocationfailed", [script.FullPath]));
            }));
        }
        else
        {
            items.Add((TazLang.Get("scripting_openlocation"), () =>
            {
                var zipScript = (ZipScriptFile)script;
                if (!FileSystemHelper.OpenLocation(zipScript.ZipPath))
                    GameActions.PrintUserWarn(World.Instance, TazLang.Get("scripting_openlocationfailed", [zipScript.ZipPath]));
            }));
        }

        items.Add((ContextMenuLabelToggle(globalAuto, TazLang.Get("scriptmanager_autostartall", "Autostart on all chars")), () =>
        {
            LegionScripting.LegionScripting.SetAutoPlay(script, true, !globalAuto);
            RebuildScriptList();
        }));
        items.Add((ContextMenuLabelToggle(charAuto, TazLang.Get("scriptmanager_autostartchar", "Autostart for this char")), () =>
        {
            LegionScripting.LegionScripting.SetAutoPlay(script, false, !charAuto);
            RebuildScriptList();
        }));

        HotkeyBinding scriptHotkey = ScriptHotkeysManager.GetBinding(script);
        string hotkeyLabel = scriptHotkey.IsEmpty
            ? TazLang.Get("scriptmanager_sethotkey", "Set Hotkey")
            : TazLang.Get("scriptmanager_sethotkey_bound", [scriptHotkey.Describe()]);
        items.Add((hotkeyLabel, () => new ScriptHotkeyWindow(script)));
        items.Add((TazLang.Get("scriptmanager_createmacrobutton", "Create Macro Button"), () =>
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
        }));
        items.Add((TazLang.Get("scriptmanager_delete", "Delete"), () =>
        {
            if (script is ZipScriptFile zs)
                ShowZipDeleteConfirm(zs);
            else
                ShowDeleteConfirm(
                    TazLang.Get("scriptmanager_deletescript_title", "Delete Script"),
                    TazLang.Get("scriptmanager_deletescript_msg", [script.FileName]),
                    () => PerformDeleteScript(script));
        }));

        ShowContextMenu(items.ToArray());
    }

    private void ShowGroupContextMenu(string parentGroup, string groupName)
    {
        bool isRealGroup = groupName != NoGroupText && !string.IsNullOrEmpty(groupName);
        _contextMenuGroup    = parentGroup;
        _contextMenuSubGroup = groupName;

        var items = new List<(string, Action)>();

        if (isRealGroup)
            items.Add((TazLang.Get("scriptmanager_renamegroup", "Rename Group"), () => ShowRenameGroupDialog(groupName, parentGroup)));

        items.Add((TazLang.Get("scriptmanager_newscript", "New Script"), () => ShowNewScriptDialog(_contextMenuGroup, _contextMenuSubGroup)));

        if (string.IsNullOrEmpty(parentGroup))
            items.Add((TazLang.Get("scriptmanager_newgroup", "New Group"), ShowNewGroupDialog));

        if (isRealGroup)
            items.Add((
                TazLang.Get("scriptmanager_deletegroup", "Delete Group"),
                () => ShowDeleteConfirm(
                    TazLang.Get("scriptmanager_deletegroup", "Delete Group"),
                    TazLang.Get("scriptmanager_deletegroup_msg", [groupName]),
                    () => PerformDeleteGroup(groupName, parentGroup)
                )
            ));

        ShowContextMenu(items.ToArray());
    }

    // ── Dialogs ───────────────────────────────────────────────────────────

    private void ShowNewScriptDialog(string contextGroup, string contextSubGroup)
    {
        var nameBox = new MyraInputBox { HintText = TazLang.Get("scriptmanager_scriptname_hint", "script_name"), Width = 220 };
        var content = new VerticalStackPanel { Spacing = 4 };
        content.Widgets.Add(new MyraLabel(TazLang.Get("scriptmanager_entername_script", "Enter a name for this script:"), MyraLabel.TextStyle.P));
        content.Widgets.Add(nameBox);

        new MyraDialog(TazLang.Get("scriptmanager_newscript", "New Script"), content, ok =>
        {
            if (!ok) return;
            string name = nameBox.Text?.Trim() ?? "";
            if (!name.EndsWith(".py") && !name.EndsWith(".cs")) name += ".py";
            CreateScript(name, contextGroup, contextSubGroup);
        });
    }

    private void ShowNewGroupDialog()
    {
        var nameBox = new MyraInputBox { HintText = TazLang.Get("scriptmanager_groupname_hint", "group_name"), Width = 220 };
        var content = new VerticalStackPanel { Spacing = 4 };
        content.Widgets.Add(new MyraLabel(TazLang.Get("scriptmanager_entername_group", "Enter a name for this group:"), MyraLabel.TextStyle.P));
        content.Widgets.Add(nameBox);

        new MyraDialog(TazLang.Get("scriptmanager_newgroup", "New Group"), content, ok =>
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
        content.Widgets.Add(new MyraLabel(TazLang.Get("scriptmanager_newname_script", [displayName]), MyraLabel.TextStyle.P));
        content.Widgets.Add(nameBox);

        new MyraDialog(TazLang.Get("scriptmanager_renamescript_title", "Rename Script"), content, ok =>
        {
            if (ok) PerformRenameScript(script, nameBox.Text?.Trim() ?? "");
        });
    }

    private void ShowRenameGroupDialog(string groupName, string parentGroup)
    {
        var nameBox = new MyraInputBox { Text = groupName, Width = 220 };
        var content = new VerticalStackPanel { Spacing = 4 };
        content.Widgets.Add(new MyraLabel(TazLang.Get("scriptmanager_newname_group", [groupName]), MyraLabel.TextStyle.P));
        content.Widgets.Add(nameBox);

        new MyraDialog(TazLang.Get("scriptmanager_renamegroup", "Rename Group"), content, ok =>
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
            GameActions.Print(World.Instance, TazLang.Get("scriptmanager_invalidscriptname", "Invalid script name."), 32);
            return;
        }

        try
        {
            string normalizedGroup    = contextGroup    == NoGroupText ? "" : contextGroup;
            string normalizedSubGroup = contextSubGroup == NoGroupText ? "" : contextSubGroup;
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
                GameActions.Print(World.Instance, TazLang.Get("scriptmanager_invalidtargetdir", "Invalid target directory."), 32);
                return;
            }

            if (!Directory.Exists(targetDirFull)) Directory.CreateDirectory(targetDirFull);

            if (!File.Exists(targetFileFull))
            {
                File.WriteAllText(targetFileFull, SCRIPT_HEADER);
                _pendingReload = true;
                GameActions.Print(World.Instance, TazLang.Get("scriptmanager_createdscript", [sanitizedName]), 66);
            }
            else
            {
                GameActions.Print(World.Instance, TazLang.Get("scriptmanager_scriptexists", [sanitizedName]), 32);
            }
        }
        catch (UnauthorizedAccessException) { GameActions.Print(World.Instance, TazLang.Get("scriptmanager_accessdenied", "Access denied."), 32); }
        catch (IOException ioEx) { GameActions.Print(World.Instance, TazLang.Get("scriptmanager_fileopfailed", [ioEx.Message]), 32); }
        catch (Exception e) { GameActions.Print(World.Instance, TazLang.Get("scriptmanager_errorcreatingscript", [e.Message]), 32); Log.Error(e.ToString()); }
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
            GameActions.Print(World.Instance, TazLang.Get("scriptmanager_invalidgroupname", "Invalid group name."), 32);
            return;
        }

        try
        {
            string normalizedGroup    = contextGroup    == NoGroupText ? "" : contextGroup;
            string normalizedSubGroup = contextSubGroup == NoGroupText ? "" : contextSubGroup;
            if (!string.IsNullOrEmpty(normalizedGroup))    normalizedGroup    = Path.GetFileName(normalizedGroup);
            if (!string.IsNullOrEmpty(normalizedSubGroup)) normalizedSubGroup = Path.GetFileName(normalizedSubGroup);

            string path = Path.Combine(LegionScripting.LegionScripting.ScriptPath,
                normalizedGroup ?? "", normalizedSubGroup ?? "", sanitizedName);

            string scriptsRoot = Path.GetFullPath(LegionScripting.LegionScripting.ScriptPath);
            string targetPath  = Path.GetFullPath(path);

            if (!targetPath.StartsWith(scriptsRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) &&
                !targetPath.Equals(scriptsRoot, StringComparison.OrdinalIgnoreCase))
            {
                GameActions.Print(World.Instance, TazLang.Get("scriptmanager_invalidgrouplocation", "Invalid group location."), 32);
                return;
            }

            if (!Directory.Exists(targetPath)) Directory.CreateDirectory(targetPath);
            File.WriteAllText(Path.Combine(targetPath, "Example.py"), "import API");
            _pendingReload = true;
            GameActions.Print(World.Instance, TazLang.Get("scriptmanager_creategroup", [sanitizedName]), 66);
        }
        catch (UnauthorizedAccessException) { GameActions.Print(World.Instance, TazLang.Get("scriptmanager_accessdenied", "Access denied."), 32); }
        catch (IOException ioEx) { GameActions.Print(World.Instance, TazLang.Get("scriptmanager_diropfailed", [ioEx.Message]), 32); }
        catch (Exception e) { GameActions.Print(World.Instance, TazLang.Get("scriptmanager_errorcreatinggroup", [e.Message]), 32); Log.Error(e.ToString()); }
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
                GameActions.Print(World.Instance, TazLang.Get("scriptmanager_fileexists", [newName]), 32);
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
        catch (Exception ex) { GameActions.Print(World.Instance, TazLang.Get("scriptmanager_errorrenamingscript", [ex.Message]), 32); }
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
                GameActions.Print(World.Instance, TazLang.Get("scriptmanager_groupexists", [newName]), 32);
                return;
            }
            if (!Directory.Exists(currentPath))
            {
                GameActions.Print(World.Instance, TazLang.Get("scriptmanager_sourcegroupnotfound", [groupName]), 32);
                return;
            }
            if (!string.Equals(currentPath, newPath, StringComparison.OrdinalIgnoreCase))
            {
                Directory.Move(currentPath, newPath);
                _pendingReload = true;
                GameActions.Print(World.Instance, TazLang.Get("scriptmanager_renamedgroup", [groupName, newName]), 66);
            }
        }
        catch (UnauthorizedAccessException) { GameActions.Print(World.Instance, TazLang.Get("scriptmanager_accessdenied", "Access denied."), 32); }
        catch (DirectoryNotFoundException)  { GameActions.Print(World.Instance, TazLang.Get("scriptmanager_dirnotfound", "Directory not found."), 32); }
        catch (IOException ioEx) { GameActions.Print(World.Instance, TazLang.Get("scriptmanager_diropfailed", [ioEx.Message]), 32); }
        catch (Exception ex) { GameActions.Print(World.Instance, TazLang.Get("scriptmanager_errorrenaminggroup", [ex.Message]), 32); Log.Error(ex.ToString()); }
    }

    private void ShowZipDeleteConfirm(ZipScriptFile script) =>
        new ZipDeleteDialog(
            script.FileName,
            Path.GetFileName(script.ZipPath),
            onDeleteScript: () => PerformDeleteZipScript(script),
            onDeleteZip:    () => PerformDeleteEntireZip(script));

    private void PerformDeleteZipScript(ZipScriptFile script)
    {
        try
        {
            using ZipArchive archive = ZipFile.Open(script.ZipPath, ZipArchiveMode.Update);
            archive.GetEntry(script.EntryPath)?.Delete();
            LegionScripting.LegionScripting.LoadedScripts.Remove(script);
            _pendingReload = true;
            GameActions.Print(World.Instance, TazLang.Get("scriptmanager_deletedfromzip", [script.FileName]), 66);
        }
        catch (Exception ex) { GameActions.Print(World.Instance, TazLang.Get("scriptmanager_errordeletingzipentry", [ex.Message]), 32); Log.Error(ex.ToString()); }
    }

    private void PerformDeleteEntireZip(ZipScriptFile script)
    {
        try
        {
            string zipPath = script.ZipPath;
            File.Delete(zipPath);
            LegionScripting.LegionScripting.LoadedScripts.RemoveAll(s => s is ZipScriptFile z && z.ZipPath == zipPath);
            _pendingReload = true;
            GameActions.Print(World.Instance, TazLang.Get("scriptmanager_deletedzip", [Path.GetFileName(zipPath)]), 66);
        }
        catch (Exception ex) { GameActions.Print(World.Instance, TazLang.Get("scriptmanager_errordeletingzip", [ex.Message]), 32); Log.Error(ex.ToString()); }
    }

    private void PerformDeleteScript(ScriptFile script)
    {
        try
        {
            File.Delete(script.FullPath);
            LegionScripting.LegionScripting.LoadedScripts.Remove(script);
            _pendingReload = true;
            GameActions.Print(World.Instance, TazLang.Get("scriptmanager_deletedscript", [script.FileName]), 66);
        }
        catch (Exception ex) { GameActions.Print(World.Instance, TazLang.Get("scriptmanager_errordeletingscript", [ex.Message]), 32); Log.Error(ex.ToString()); }
    }

    private void PerformDeleteGroup(string groupName, string parentGroup)
    {
        try
        {
            string gPath = string.IsNullOrEmpty(parentGroup) ? groupName : Path.Combine(parentGroup, groupName);
            gPath = Path.Combine(LegionScripting.LegionScripting.ScriptPath, gPath);

            if (!Directory.Exists(gPath))
            {
                GameActions.Print(World.Instance, TazLang.Get("scriptmanager_groupnotfound", [groupName]), 32);
                return;
            }

            Directory.Delete(gPath, true);
            _pendingReload = true;
            GameActions.Print(World.Instance, TazLang.Get("scriptmanager_deletedgroup", [groupName]), 66);
        }
        catch (UnauthorizedAccessException) { GameActions.Print(World.Instance, TazLang.Get("scriptmanager_accessdenied", "Access denied."), 32); }
        catch (IOException ioEx) { GameActions.Print(World.Instance, TazLang.Get("scriptmanager_deleteopfailed", [ioEx.Message]), 32); }
        catch (Exception ex) { GameActions.Print(World.Instance, TazLang.Get("scriptmanager_errordeletinggroup", [ex.Message]), 32); Log.Error(ex.ToString()); }
    }

    private sealed class ZipDeleteDialog : MyraControl
    {
        public ZipDeleteDialog(string scriptName, string zipName, Action onDeleteScript, Action onDeleteZip)
            : base(TazLang.Get("scriptmanager_deletezipscript_title", "Delete Zip Script"))
        {
            var layout = new VerticalStackPanel { Spacing = 8, Padding = new Thickness(8) };

            layout.Widgets.Add(new MyraLabel(
                TazLang.Get("scriptmanager_deletezipscript_msg", [scriptName, zipName]),
                MyraLabel.TextStyle.P) { TextColor = Color.OrangeRed });

            var btnRow = new HorizontalStackPanel
            {
                Spacing = 8,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            btnRow.Widgets.Add(new MyraButton(TazLang.Get("scriptmanager_deletescriptonly", "Delete Script Only"), () =>
            {
                _disposeRequested = true;
                onDeleteScript();
            }));
            btnRow.Widgets.Add(new MyraButton(TazLang.Get("scriptmanager_deleteentirezip", "Delete Entire Zip"), () =>
            {
                _disposeRequested = true;
                onDeleteZip();
            }));
            btnRow.Widgets.Add(new MyraButton(TazLang.Get("scriptmanager_cancel", "Cancel"), () => _disposeRequested = true));

            layout.Widgets.Add(btnRow);
            SetRootContent(layout);
            CenterInViewPort();
            UIManager.Add(this);
            BringOnTop();
        }
    }
}
