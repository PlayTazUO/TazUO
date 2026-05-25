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

    private const string NOGROUPTEXT = "无分组";

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

    public ScriptManagerWindow() : base("脚本管理器")
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
        bar.Widgets.Add(new MyraButton("菜单", ShowMainMenu));
        bar.Widgets.Add(new MyraButton("添加 +", ShowAddMenu));

        var searchBox = new MyraInputBox { HintText = "搜索...", Width = 180 };
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
            ("刷新",                    () => _pendingReload = true),
            ("公共脚本浏览器",      ScriptBrowser.Show),
            ("脚本录制",           () => UIManager.Add(new ScriptRecordingGump())),
            ("脚本信息",             ScriptingInfoGump.Show),
            ("持久变量",       PersistentVarsWindow.Show),
            ("运行中的脚本",            RunningScriptsWindow.Show),
            (ContextMenuLabelToggle(cacheDisabled, "禁用模块缓存"), () =>
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
        var playStopBtn = new MyraButton(isPlaying ? "停止" : "播放", () =>
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
                    Tooltip = hasGlobal ? "自动启动: 所有角色" : "自动启动: 本角色"
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
            ("编辑常量",       () => new ScriptConstantsEditorWindow(script)),
            ("重命名",               () => ShowRenameScriptDialog(script)),
            ("编辑",                 () => new ScriptEditorWindow(script)),
            ("外部编辑",      () => FileSystemHelper.OpenFileWithDefaultApp(script.FullPath)),
            (Language.Instance.Scripting.OpenLocation, () =>
            {
                if (!FileSystemHelper.OpenLocation(script.FullPath))
                    GameActions.PrintUserWarn(World.Instance, string.Format(Language.Instance.Scripting.OpenLocationFailed, script.FullPath));
            }),
            (ContextMenuLabelToggle(globalAuto, "所有角色自动启动"), () =>
            {
                LegionScripting.LegionScripting.SetAutoPlay(script, true, !globalAuto);
                RebuildScriptList();
            }),
            (ContextMenuLabelToggle(charAuto, "本角色自动启动"), () =>
            {
                LegionScripting.LegionScripting.SetAutoPlay(script, false, !charAuto);
                RebuildScriptList();
            }),
            ("创建宏按钮", () =>
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
            ("删除", () => ShowDeleteConfirm(
                "删除脚本",
                $"确定要删除 '{script.FileName}' 吗？\n此操作无法撤销。",
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
            items.Add(("重命名分组", () => ShowRenameGroupDialog(groupName, parentGroup)));

        items.Add(("新建脚本", () => ShowNewScriptDialog(_contextMenuGroup, _contextMenuSubGroup)));

        if (string.IsNullOrEmpty(parentGroup))
            items.Add(("新建分组", ShowNewGroupDialog));

        if (isRealGroup)
            items.Add(("删除分组", () => ShowDeleteConfirm(
                "删除分组",
                $"删除分组 '{groupName}' ？\n这将永久删除该文件夹及其中的所有脚本。",
                () => PerformDeleteGroup(groupName, parentGroup))));

        ShowContextMenu(items.ToArray());
    }

    // ── Dialogs ───────────────────────────────────────────────────────────

    private void ShowNewScriptDialog(string contextGroup, string contextSubGroup)
    {
        var nameBox = new MyraInputBox { HintText = "脚本名称", Width = 220 };
        var content = new VerticalStackPanel { Spacing = 4 };
        content.Widgets.Add(new MyraLabel("请输入脚本名称：", MyraLabel.TextStyle.P));
        content.Widgets.Add(nameBox);

        new MyraDialog("新建脚本", content, ok =>
        {
            if (!ok) return;
            string name = nameBox.Text?.Trim() ?? "";
            if (!name.EndsWith(".py") && !name.EndsWith(".cs")) name += ".py";
            CreateScript(name, contextGroup, contextSubGroup);
        });
    }

    private void ShowNewGroupDialog()
    {
        var nameBox = new MyraInputBox { HintText = "分组名称", Width = 220 };
        var content = new VerticalStackPanel { Spacing = 4 };
        content.Widgets.Add(new MyraLabel("请输入分组名称：", MyraLabel.TextStyle.P));
        content.Widgets.Add(nameBox);

        new MyraDialog("新建分组", content, ok =>
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
        content.Widgets.Add(new MyraLabel($"'{displayName}' 的新名称：", MyraLabel.TextStyle.P));
        content.Widgets.Add(nameBox);

        new MyraDialog("重命名脚本", content, ok =>
        {
            if (ok) PerformRenameScript(script, nameBox.Text?.Trim() ?? "");
        });
    }

    private void ShowRenameGroupDialog(string groupName, string parentGroup)
    {
        var nameBox = new MyraInputBox { Text = groupName, Width = 220 };
        var content = new VerticalStackPanel { Spacing = 4 };
        content.Widgets.Add(new MyraLabel($"分组 '{groupName}' 的新名称：", MyraLabel.TextStyle.P));
        content.Widgets.Add(nameBox);

        new MyraDialog("重命名分组", content, ok =>
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
            GameActions.Print(World.Instance, "无效的脚本名称。", 32);
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
                GameActions.Print(World.Instance, "无效的目标目录。", 32);
                return;
            }

            if (!Directory.Exists(targetDirFull)) Directory.CreateDirectory(targetDirFull);

            if (!File.Exists(targetFileFull))
            {
                File.WriteAllText(targetFileFull, SCRIPT_HEADER);
                _pendingReload = true;
                GameActions.Print(World.Instance, $"已创建脚本 '{sanitizedName}'", 66);
            }
            else
            {
                GameActions.Print(World.Instance, $"名为 '{sanitizedName}' 的脚本已存在。", 32);
            }
        }
        catch (UnauthorizedAccessException) { GameActions.Print(World.Instance, "访问被拒绝。", 32); }
        catch (IOException ioEx) { GameActions.Print(World.Instance, $"文件操作失败: {ioEx.Message}", 32); }
        catch (Exception e) { GameActions.Print(World.Instance, $"创建脚本出错: {e.Message}", 32); Log.Error(e.ToString()); }
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
            GameActions.Print(World.Instance, "无效的分组名称。", 32);
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
                GameActions.Print(World.Instance, "无效的分组位置。", 32);
                return;
            }

            if (!Directory.Exists(targetPath)) Directory.CreateDirectory(targetPath);
            File.WriteAllText(Path.Combine(targetPath, "Example.py"), "import API");
            _pendingReload = true;
            GameActions.Print(World.Instance, $"已创建分组 '{sanitizedName}'", 66);
        }
        catch (UnauthorizedAccessException) { GameActions.Print(World.Instance, "访问被拒绝。", 32); }
        catch (IOException ioEx) { GameActions.Print(World.Instance, $"目录操作失败: {ioEx.Message}", 32); }
        catch (Exception e) { GameActions.Print(World.Instance, $"创建分组出错: {e.Message}", 32); Log.Error(e.ToString()); }
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
                GameActions.Print(World.Instance, $"名为 '{newName}' 的文件已存在。", 32);
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
        catch (Exception ex) { GameActions.Print(World.Instance, $"重命名脚本出错: {ex.Message}", 32); }
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
                GameActions.Print(World.Instance, $"名为 '{newName}' 的分组已存在。", 32);
                return;
            }
            if (!Directory.Exists(currentPath))
            {
                GameActions.Print(World.Instance, $"未找到源分组 '{groupName}'。", 32);
                return;
            }
            if (!string.Equals(currentPath, newPath, StringComparison.OrdinalIgnoreCase))
            {
                Directory.Move(currentPath, newPath);
                _pendingReload = true;
                GameActions.Print(World.Instance, $"已将分组 '{groupName}' 重命名为 '{newName}'", 66);
            }
        }
        catch (UnauthorizedAccessException) { GameActions.Print(World.Instance, "访问被拒绝。", 32); }
        catch (DirectoryNotFoundException)  { GameActions.Print(World.Instance, "目录未找到。", 32); }
        catch (IOException ioEx) { GameActions.Print(World.Instance, $"目录操作失败: {ioEx.Message}", 32); }
        catch (Exception ex) { GameActions.Print(World.Instance, $"重命名分组出错: {ex.Message}", 32); Log.Error(ex.ToString()); }
    }

    private void PerformDeleteScript(ScriptFile script)
    {
        try
        {
            File.Delete(script.FullPath);
            LegionScripting.LegionScripting.LoadedScripts.Remove(script);
            _pendingReload = true;
            GameActions.Print(World.Instance, $"已删除脚本 '{script.FileName}'", 66);
        }
        catch (Exception ex) { GameActions.Print(World.Instance, $"删除脚本出错: {ex.Message}", 32); Log.Error(ex.ToString()); }
    }

    private void PerformDeleteGroup(string groupName, string parentGroup)
    {
        try
        {
            string gPath = string.IsNullOrEmpty(parentGroup) ? groupName : Path.Combine(parentGroup, groupName);
            gPath = Path.Combine(LegionScripting.LegionScripting.ScriptPath, gPath);

            if (!Directory.Exists(gPath))
            {
                GameActions.Print(World.Instance, $"未找到分组 '{groupName}'", 32);
                return;
            }

            Directory.Delete(gPath, true);
            _pendingReload = true;
            GameActions.Print(World.Instance, $"已删除分组 '{groupName}' 及其所有内容", 66);
        }
        catch (UnauthorizedAccessException) { GameActions.Print(World.Instance, "访问被拒绝。", 32); }
        catch (IOException ioEx) { GameActions.Print(World.Instance, $"删除操作失败: {ioEx.Message}", 32); }
        catch (Exception ex) { GameActions.Print(World.Instance, $"删除分组出错: {ex.Message}", 32); Log.Error(ex.ToString()); }
    }
}
