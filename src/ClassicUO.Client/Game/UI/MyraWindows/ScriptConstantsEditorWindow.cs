#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.LegionScripting;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows;

public class ScriptConstantsEditorWindow : MyraControl
{
    private readonly ScriptFile _script;
    private Dictionary<string, ConstantEntry> _constants = new();
    private string _filterText = "";
    private bool _hasUnsavedChanges;
    private DateTime _statusUntil = DateTime.MinValue;

    private readonly VerticalStackPanel _constantsPanel = new() { Spacing = 2 };
    private MyraLabel _statusLabel = null!;
    private MyraLabel _countLabel = null!;

    public ScriptConstantsEditorWindow(ScriptFile script) : base(script.FileName + " Constants")
    {
        _script = script;
        ParseConstants();
        Build();
        CenterInViewPort();
        UIManager.Add(this);
        BringOnTop();
    }

    public override void Update()
    {
        base.Update();
        if (_statusLabel.Visible && _statusUntil != DateTime.MaxValue && DateTime.Now > _statusUntil)
            _statusLabel.Visible = false;
    }

    private void Build()
    {
        var root = new VerticalStackPanel { Spacing = MyraStyle.STANDARD_SPACING };
        root.Widgets.Add(BuildToolbar());
        BuildConstantsGrid();
        root.Widgets.Add(new ScrollViewer { MaxHeight = 450, MinWidth = 500, Content = _constantsPanel });
        SetRootContent(root);
    }

    private Widget BuildToolbar()
    {
        var toolbar = new HorizontalStackPanel { Spacing = 4 };

        var filterBox = new MyraInputBox { HintText = "过滤常量...", Width = 175, Text = _filterText };
        filterBox.TextChangedByUser += (_, _) =>
        {
            _filterText = filterBox.Text ?? "";
            BuildConstantsGrid();
        };
        toolbar.Widgets.Add(filterBox);

        toolbar.Widgets.Add(new MyraButton("刷新", RefreshConstants));
        toolbar.Widgets.Add(new MyraButton("保存", SaveConstants));

        _statusLabel = new MyraLabel("", MyraLabel.TextStyle.P) { Visible = false };
        toolbar.Widgets.Add(_statusLabel);

        _countLabel = new MyraLabel("", MyraLabel.TextStyle.P);
        toolbar.Widgets.Add(_countLabel);
        UpdateCountLabel();

        return toolbar;
    }

    private void UpdateCountLabel()
    {
        int n = _constants.Count;
        _countLabel.Text = $"({n} constant{(n != 1 ? "s" : "")})";
    }

    private void ShowStatus(string text, float seconds)
    {
        _statusLabel.Text = text;
        _statusLabel.Visible = true;
        _statusUntil = seconds <= 0 ? DateTime.MaxValue : DateTime.Now.AddSeconds(seconds);
    }

    private void BuildConstantsGrid()
    {
        _constantsPanel.Widgets.Clear();

        IEnumerable<ConstantEntry> filtered = _constants.Values;
        if (!string.IsNullOrWhiteSpace(_filterText))
            filtered = filtered.Where(c =>
                c.Name.Contains(_filterText, StringComparison.OrdinalIgnoreCase) ||
                c.EditValue.Contains(_filterText, StringComparison.OrdinalIgnoreCase));

        var list = filtered.OrderBy(c => c.LineNumber).ToList();

        if (list.Count == 0)
        {
            if (string.IsNullOrWhiteSpace(_filterText))
            {
                _constantsPanel.Widgets.Add(new MyraLabel("脚本中未找到常量。\n常量必须是大写名称的顶层赋值。\n例如:  MAX_DISTANCE = 10", MyraLabel.TextStyle.P));
            }
            else
            {
                _constantsPanel.Widgets.Add(new MyraLabel("没有匹配过滤条件的常量。", MyraLabel.TextStyle.P));
            }
            return;
        }

        var grid = new MyraGrid();
        grid.SetupWithHeaders(
            GridColumnInfo.Auto("常量"),
            GridColumnInfo.Fill("值"),
            GridColumnInfo.Auto("行")
        );

        int row = 1;
        foreach (ConstantEntry c in list)
        {
            ConstantEntry captured = c;
            grid.AddWidget(new MyraLabel(c.Name, MyraLabel.TextStyle.P), row, 0);

            if (IsBooleanValue(c.EditValue))
                grid.AddWidget(BuildBooleanEditor(captured), row, 1);
            else if (TryParseArray(c.EditValue, out _))
                grid.AddWidget(BuildArrayRow(captured), row, 1);
            else
                grid.AddWidget(BuildTextEditor(captured), row, 1);

            grid.AddWidget(new MyraLabel($"{c.LineNumber + 1}", MyraLabel.TextStyle.P), row, 2);
            row++;
        }

        _constantsPanel.Widgets.Add(grid);
    }

    private Widget BuildTextEditor(ConstantEntry constant)
    {
        string original = constant.OriginalValue;
        var box = new MyraInputBox { Text = constant.EditValue };
        box.TextChangedByUser += (_, _) =>
        {
            constant.EditValue = box.Text ?? "";
                    box.Tooltip = constant.OriginalValue != constant.EditValue ? $"原始值: {original}" : null;
                };
                if (constant.OriginalValue != constant.EditValue)
                    box.Tooltip = $"原始值: {original}";
        return box;
    }

    private Widget BuildBooleanEditor(ConstantEntry constant)
    {
        string original = constant.OriginalValue;
#pragma warning disable CS0612, CS0618
        var combo = new ComboBox();
        combo.Items.Add(new ListItem("True"));
        combo.Items.Add(new ListItem("False"));
        combo.SelectedIndex = constant.EditValue.Trim().Equals("True", StringComparison.OrdinalIgnoreCase) ? 0 : 1;
        combo.SelectedIndexChanged += (_, _) =>
        {
            if (combo.SelectedIndex == null) return;
            constant.EditValue = combo.SelectedIndex == 0 ? "True" : "False";
                combo.Tooltip = constant.OriginalValue != constant.EditValue ? $"原始值: {original}" : null;
            CheckForChanges();
        };
#pragma warning restore CS0612, CS0618
        if (constant.OriginalValue != constant.EditValue)
            combo.Tooltip = $"原始值: {original}";
        return combo;
    }

    private Widget BuildArrayRow(ConstantEntry constant)
    {
        string original = constant.OriginalValue;
        var row = new HorizontalStackPanel { Spacing = 4 };
        var readonlyBox = new MyraInputBox { Text = constant.EditValue, Enabled = false };
        if (constant.OriginalValue != constant.EditValue)
            readonlyBox.Tooltip = $"原始值: {original}";
        row.Widgets.Add(readonlyBox);
        row.Widgets.Add(new MyraButton("Edit", () => ShowArrayEditor(constant)));
        return row;
    }

    private void ShowArrayEditor(ConstantEntry constant)
    {
        TryParseArray(constant.EditValue, out List<string>? elements);
        var elementsCopy = new List<string>(elements ?? []);

        var elementsPanel = new VerticalStackPanel { Spacing = 2 };

        void BuildElements()
        {
            elementsPanel.Widgets.Clear();
            for (int i = 0; i < elementsCopy.Count; i++)
            {
                int idx = i;
                var eRow = new HorizontalStackPanel { Spacing = 4 };
                eRow.Widgets.Add(new MyraLabel($"[{idx}]", MyraLabel.TextStyle.P));
                var eBox = new MyraInputBox { Text = elementsCopy[idx], MinWidth = 180 };
                eBox.TextChangedByUser += (_, _) => elementsCopy[idx] = eBox.Text ?? "";
                eRow.Widgets.Add(eBox);
                eRow.Widgets.Add(MyraStyle.ApplyButtonDangerStyle(new MyraButton("X", () =>
                {
                    elementsCopy.RemoveAt(idx);
                    BuildElements();
                })                     { Tooltip = "移除此元素" }));
                elementsPanel.Widgets.Add(eRow);
            }
            elementsPanel.Widgets.Add(new MyraButton("添加元素", () =>
            {
                elementsCopy.Add("0");
                BuildElements();
            }));
        }

        BuildElements();

        var content = new VerticalStackPanel { Spacing = 4 };
        content.Widgets.Add(new MyraLabel($"编辑: {constant.Name}", MyraLabel.TextStyle.H3));
        content.Widgets.Add(new ScrollViewer { MaxHeight = 300, Content = elementsPanel });

        new MyraDialog($"数组编辑器: {constant.Name}", content, ok =>
        {
            if (!ok) return;
            constant.EditValue = "[" + string.Join(", ", elementsCopy) + "]";
            CheckForChanges();
            BuildConstantsGrid();
        });
    }

    private void CheckForChanges()
    {
        _hasUnsavedChanges = _constants.Values.Any(c => c.OriginalValue != c.EditValue);
        if (_hasUnsavedChanges)
            ShowStatus("• 未保存的更改", 0);
        else if (_statusLabel.Text == "• Unsaved changes")
            _statusLabel.Visible = false;
    }

    private void RefreshConstants()
    {
        if (!File.Exists(_script.FullPath)) return;
        _script.FileContents = File.ReadAllLines(_script.FullPath);
        _script.FileContentsJoined = string.Join("\n", _script.FileContents);
        ParseConstants();
        _hasUnsavedChanges = false;
        UpdateCountLabel();
        BuildConstantsGrid();
        ShowStatus("已从文件刷新", 3);
    }

    private void SaveConstants()
    {
        try
        {
            if (!_hasUnsavedChanges)
            {
                ShowStatus("没有需要保存的更改", 2);
                return;
            }

            string[] updatedLines = new string[_script.FileContents.Length];
            Array.Copy(_script.FileContents, updatedLines, _script.FileContents.Length);

            foreach (ConstantEntry c in _constants.Values.Where(c => c.OriginalValue != c.EditValue))
                updatedLines[c.LineNumber] = $"{c.Name} = {c.EditValue}";

            File.WriteAllLines(_script.FullPath, updatedLines);

            _script.FileContents = updatedLines;
            _script.FileContentsJoined = string.Join("\n", updatedLines);

            foreach (ConstantEntry c in _constants.Values)
            {
                c.OriginalValue = c.EditValue;
                c.FullLine = updatedLines[c.LineNumber];
            }

            _hasUnsavedChanges = false;
            ShowStatus("保存成功!", 3);
            BuildConstantsGrid();
        }
        catch (Exception ex)
        {
            ShowStatus($"错误: {ex.Message}", 5);
        }
    }

    private void ParseConstants()
    {
        _constants.Clear();
        if (_script.FileContents is not { Length: > 0 }) return;

        var pattern = new Regex(@"^([A-Z][A-Z0-9_]*)\s*=\s*(.+?)(?:\s*#.*)?$", RegexOptions.Compiled);

        for (int i = 0; i < _script.FileContents.Length; i++)
        {
            string line = _script.FileContents[i].TrimEnd();

            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                continue;
            if (line.Length > 0 && char.IsWhiteSpace(line[0]))
                continue;

            Match m = pattern.Match(line);
            if (!m.Success) continue;

            string name  = m.Groups[1].Value;
            string value = m.Groups[2].Value.Trim();

            _constants[name] = new ConstantEntry
            {
                Name = name, OriginalValue = value, EditValue = value,
                LineNumber = i, FullLine = line
            };
        }
    }

    private static bool IsBooleanValue(string value)
    {
        string t = value.Trim();
        return t.Equals("True",  StringComparison.OrdinalIgnoreCase) ||
               t.Equals("False", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryParseArray(string value, out List<string>? elements)
    {
        elements = null;
        if (string.IsNullOrWhiteSpace(value)) return false;
        string t = value.Trim();
        if (!t.StartsWith("[") || !t.EndsWith("]")) return false;
        string inner = t.Substring(1, t.Length - 2);
        elements = inner.Split(',').Select(s => s.Trim()).ToList();
        return elements.Count > 0;
    }

    private class ConstantEntry
    {
        public string Name { get; set; } = "";
        public string OriginalValue { get; set; } = "";
        public string EditValue = "";
        public int LineNumber { get; set; }
        public string FullLine { get; set; } = "";
    }
}
