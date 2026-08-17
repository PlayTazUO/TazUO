#nullable enable
using System;
using System.Collections.Generic;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows;

/// <summary>
/// Tooltip override configuration window. The override rules are picked from a combo list on the
/// left (the combo's ListView is embedded directly in a scroll area, not dropped down) and all
/// their settings are edited in the detail panel on the right, mirroring the Dress Agent layout.
/// </summary>
public sealed class TooltipOverrideConfigWindow : MyraControl
{
    private static readonly Array LayerValues = Enum.GetValues(typeof(TooltipLayers));
    private static readonly string[] LayerNames = Enum.GetNames(typeof(TooltipLayers));

    private readonly World _world;
    private readonly VerticalStackPanel _detailPanel = new() { Spacing = MyraStyle.STANDARD_SPACING };

    // The toggle button of the ComboView stays unused: its ListView is shown inline in the panel,
    // which carries its own ScrollViewer and so doubles as the scroll area.
    private readonly ComboView _overrideCombo = new();

    private MyraButton _deleteButton = null!;
    private MyraButton _moveUpButton = null!;
    private MyraButton _moveDownButton = null!;
    private MyraLabel _statusLabel = null!;
    private DateTime _statusUntil = DateTime.MinValue;
    private bool _suppressComboEvent;
    private int _selectedIndex = -1;

    public TooltipOverrideConfigWindow(World world) : base(TazLang.Get("tooltipconfig_title", "Tooltip Override Configuration"))
    {
        _world = world;
        Build();
        CenterInViewPort();
    }

    /// <summary>Opens the window, focusing an existing instance instead of stacking duplicates.</summary>
    public static void Show(World world)
    {
        foreach (IGui gump in UIManager.Gumps)
        {
            if (gump is TooltipOverrideConfigWindow w && !w.IsDisposed)
            {
                w.BringOnTop();
                return;
            }
        }

        UIManager.Add(new TooltipOverrideConfigWindow(world));
    }

    public override void Update()
    {
        base.Update();

        if (_statusLabel.Visible && DateTime.Now > _statusUntil)
            _statusLabel.Visible = false;
    }

    private void Build()
    {
        var root = new VerticalStackPanel { Spacing = MyraStyle.STANDARD_SPACING };

        root.Widgets.Add(new LinkLabel(
            TazLang.Get("tooltipconfig_wiki", "Tooltip Overrides Wiki"),
            "https://tazuo.org?q=tooltip+override",
            MyraLabel.TextStyle.P));

        root.Widgets.Add(BuildToolbar());

        var main = new HorizontalStackPanel { Spacing = 8 };
        main.Widgets.Add(BuildListPanel());
        main.Widgets.Add(new ScrollViewer { MaxHeight = 450, Content = _detailPanel });
        root.Widgets.Add(main);

        SetRootContent(root);
        RefreshCombo(_selectedIndex);
    }

    private Widget BuildToolbar()
    {
        var toolbar = new HorizontalStackPanel { Spacing = 4, VerticalAlignment = VerticalAlignment.Center };

        toolbar.Widgets.Add(new MyraButton(TazLang.Get("tooltipconfig_export", "Export"),
            () => ToolTipOverrideData.ExportOverrideSettings(_world)));
        toolbar.Widgets.Add(new MyraButton(TazLang.Get("tooltipconfig_import", "Import"), () =>
        {
            ToolTipOverrideData.ImportOverrideSettings();
            RefreshCombo(_selectedIndex);
        }));
        toolbar.Widgets.Add(new MyraButton(TazLang.Get("tooltipconfig_refresh", "Refresh"), () => RefreshCombo(_selectedIndex)));

        MyraButton deleteAll = new(TazLang.Get("tooltipconfig_deleteall", "Delete All"), ConfirmDeleteAll)
        {
            Tooltip = TazLang.Get("tooltipconfig_deleteall_tooltip",
                "/c[red]This will remove ALL tooltip override settings.\nThis is not reversible.")
        };
        MyraStyle.ApplyButtonDangerStyle(deleteAll);
        toolbar.Widgets.Add(deleteAll);

        _statusLabel = new MyraLabel("", MyraLabel.TextStyle.P) { Visible = false };
        toolbar.Widgets.Add(_statusLabel);

        return toolbar;
    }

    private Widget BuildListPanel()
    {
        var panel = new VerticalStackPanel { Spacing = 4, MinWidth = 200 };

        panel.Widgets.Add(new MyraLabel(TazLang.Get("tooltipconfig2_list", "Overrides"), MyraLabel.TextStyle.H3));

        ListView listView = _overrideCombo.ListView;
        listView.HorizontalAlignment = HorizontalAlignment.Stretch;
        listView.MinWidth = 200;
        listView.MinHeight = 80;
        listView.MaxHeight = 300;
        listView.SelectedIndexChanged += (_, _) =>
        {
            if (_suppressComboEvent)
                return;

            _selectedIndex = listView.SelectedIndex ?? -1;
            BuildDetails();
        };
        panel.Widgets.Add(listView);

        var addDeleteRow = new HorizontalStackPanel { Spacing = 4 };
        addDeleteRow.Widgets.Add(new MyraButton(TazLang.Get("tooltipconfig_add", "Add +"), AddNewOverride));

        MyraButton delete = new(TazLang.Get("tooltipconfig2_delete", "Delete Selected"), DeleteSelected)
        {
            Tooltip = TazLang.Get("tooltipconfig2_delete_tooltip", "Delete the selected override")
        };
        MyraStyle.ApplyButtonDangerStyle(delete);
        _deleteButton = delete;
        addDeleteRow.Widgets.Add(delete);
        panel.Widgets.Add(addDeleteRow);

        var moveRow = new HorizontalStackPanel { Spacing = 4 };
        MyraButton moveUp = new("", () => MoveSelected(-1))
        {
            Tooltip = TazLang.Get("tooltipconfig2_moveup", "Move this override up")
        };
        MyraStyle.ApplySkillButtonStyle(moveUp, Lock.Up);
        _moveUpButton = moveUp;
        moveRow.Widgets.Add(moveUp);

        MyraButton moveDown = new("", () => MoveSelected(1))
        {
            Tooltip = TazLang.Get("tooltipconfig2_movedown", "Move this override down")
        };
        MyraStyle.ApplySkillButtonStyle(moveDown, Lock.Down);
        _moveDownButton = moveDown;
        moveRow.Widgets.Add(moveDown);
        panel.Widgets.Add(moveRow);

        return panel;
    }

    /// <summary>
    /// Repopulates the override combo, restoring the selection to <paramref name="selectIndex"/>
    /// (clamped to the list, defaulting to the first entry when it is out of range) and rebuilding
    /// the detail panel for it.
    /// </summary>
    private void RefreshCombo(int selectIndex)
    {
        _suppressComboEvent = true;
        _overrideCombo.ListView.Widgets.Clear();

        int count = ProfileManager.CurrentProfile == null ? 0 : TooltipOverridesConfig.Current.Overrides.Count;

        if (count == 0)
        {
            _selectedIndex = -1;
            _suppressComboEvent = false;
            BuildDetails();
            return;
        }

        for (int i = 0; i < count; i++)
        {
            string label = ToolTipOverrideData.Get(i).SearchText;
            if (string.IsNullOrWhiteSpace(label))
                label = $"Override {i + 1}";
            _overrideCombo.ListView.Widgets.Add(new Myra.Graphics2D.UI.Label { Text = label });
        }

        if (selectIndex < 0 || selectIndex >= count)
            selectIndex = 0;

        _selectedIndex = selectIndex;
        _overrideCombo.ListView.SelectedIndex = selectIndex;

        _suppressComboEvent = false;
        BuildDetails();
    }

    private void BuildDetails()
    {
        _detailPanel.Widgets.Clear();

        if (_selectedIndex < 0 || ProfileManager.CurrentProfile == null ||
            _selectedIndex >= TooltipOverridesConfig.Current.Overrides.Count)
        {
            _deleteButton.Enabled = false;
            _moveUpButton.Enabled = false;
            _moveDownButton.Enabled = false;
            _detailPanel.Widgets.Add(new MyraLabel(
                TazLang.Get("tooltipconfig2_selectprompt",
                    "Select an override from the list to edit its settings, or use \"Add +\" to create one."),
                MyraLabel.TextStyle.P));
            return;
        }

        _deleteButton.Enabled = true;
        _moveUpButton.Enabled = _selectedIndex > 0;
        _moveDownButton.Enabled = _selectedIndex < TooltipOverridesConfig.Current.Overrides.Count - 1;
        var data = ToolTipOverrideData.Get(_selectedIndex);

        var searchBox = new MyraInputBox
        {
            Text = data.SearchText,
            Width = 220,
            Tooltip = TazLang.Get("tooltipconfig_searchtext_tooltip")
        };
        searchBox.TextChangedByUser += (_, _) =>
        {
            // The override is unusable without search text, so only commit non-empty values (matches the legacy gump).
            if (string.IsNullOrEmpty(searchBox.Text))
                return;

            data.SearchText = searchBox.Text;
            data.Save();

            if (_overrideCombo.ListView.SelectedItem is Myra.Graphics2D.UI.Label item)
                item.Text = data.SearchText;
            ShowSaved();
        };

        var searchRow = new HorizontalStackPanel { Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
        searchRow.Widgets.Add(new MyraLabel(
            TazLang.Get("tooltipconfig2_matchingattribute"),
            MyraLabel.TextStyle.P));
        searchRow.Widgets.Add(searchBox);
        _detailPanel.Widgets.Add(searchRow);

        var formatBox = new MyraInputBox
        {
            Text = data.FormattedText,
            Width = 300,
            Tooltip = TazLang.Get("tooltipconfig_formattext_tooltip")
        };
        formatBox.TextChangedByUser += (_, _) =>
        {
            data.FormattedText = formatBox.Text ?? "";
            data.Save();
            ShowSaved();
        };
        var formatRow = new HorizontalStackPanel { Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
        formatRow.Widgets.Add(new MyraLabel(
            TazLang.Get("tooltipconfig2_replacement"),
            MyraLabel.TextStyle.P));
        formatRow.Widgets.Add(formatBox);
        _detailPanel.Widgets.Add(formatRow);

        var ranges = new VerticalStackPanel { Spacing = 4 };
        ranges.Widgets.Add(new MyraLabel(
            TazLang.Get("tooltipconfig2_attributevalues"),
            MyraLabel.TextStyle.P));

        var firstValueRow = new HorizontalStackPanel { Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
        firstValueRow.Widgets.Add(new MyraSpacer(20, 0));
        firstValueRow.Widgets.Add(new MyraLabel(
            TazLang.Get("tooltipconfig2_firstvalue"),
            MyraLabel.TextStyle.P));
        firstValueRow.Widgets.Add(NumericBox(data.Min1, v => { data.Min1 = v; data.Save(); ShowSaved(); }));
        firstValueRow.Widgets.Add(NumericBox(data.Max1, v => { data.Max1 = v; data.Save(); ShowSaved(); }));
        ranges.Widgets.Add(firstValueRow);

        var secondValueRow = new HorizontalStackPanel { Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
        secondValueRow.Widgets.Add(new MyraSpacer(20, 0));
        secondValueRow.Widgets.Add(new MyraLabel(
            TazLang.Get("tooltipconfig2_secondvalue"),
            MyraLabel.TextStyle.P));
        secondValueRow.Widgets.Add(NumericBox(data.Min2, v => { data.Min2 = v; data.Save(); ShowSaved(); }));
        secondValueRow.Widgets.Add(NumericBox(data.Max2, v => { data.Max2 = v; data.Save(); ShowSaved(); }));
        ranges.Widgets.Add(secondValueRow);
        _detailPanel.Widgets.Add(ranges);

        var layerRow = new HorizontalStackPanel { Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
        layerRow.Widgets.Add(new MyraLabel(
            TazLang.Get("tooltipconfig2_matchinglayer"),
            MyraLabel.TextStyle.P));
        layerRow.Widgets.Add(BuildLayerCombo(data));
        _detailPanel.Widgets.Add(layerRow);

        _detailPanel.Widgets.Add(BuildBorderColor(data));
    }

    private Widget BuildBorderColor(ToolTipOverrideData data)
    {
        var row = new HorizontalStackPanel { Spacing = 4, VerticalAlignment = VerticalAlignment.Center };

        row.Widgets.Add(new MyraLabel(TazLang.Get("tooltipconfig_bordercolor"), MyraLabel.TextStyle.P)
        {
            Tooltip = TazLang.Get("tooltipconfig_bordercolor_tooltip")
        });

        ushort swatchHue = data.HasBorderHue ? (ushort)data.BorderHue : (ushort)0;
        var swatch = new MyraArtTexture(0x0FAB, swatchHue, 20) { Tooltip = BorderSwatchTooltip(data) };

        swatch.TouchUp += (_, _) =>
        {
            if (!swatch.Enabled)
                return;

            UIManager.GetGump<Gumps.ModernColorPicker>()?.Dispose();
            UIManager.Add(new Gumps.ModernColorPicker(World.Instance, newHue =>
            {
                data.BorderHue = newHue;
                data.Save();
                swatch.SetColorByHue(newHue);
                swatch.Tooltip = BorderSwatchTooltip(data);
                ShowSaved();
            }, isClickable: true));
        };
        row.Widgets.Add(swatch);

        MyraButton clear = new(TazLang.Get("tooltipconfig_bordercolor_clear"), () =>
        {
            data.BorderHue = -1;
            data.Save();
            swatch.SetColorByHue(0);
            swatch.Tooltip = BorderSwatchTooltip(data);
            ShowSaved();
        })
        {
            Tooltip = TazLang.Get("tooltipconfig_bordercolor_clear_tooltip")
        };
        MyraStyle.ApplyButtonDangerStyle(clear);
        row.Widgets.Add(clear);

        return row;
    }

    private static string BorderSwatchTooltip(ToolTipOverrideData data) =>
        data.HasBorderHue
            ? string.Format(TazLang.Get("tooltipconfig_bordercolor_set_tooltip"), data.BorderHue)
            : TazLang.Get("tooltipconfig_bordercolor_none_tooltip");

    private static Widget NumericBox(int value, Action<int> onChanged)
    {
        var box = new MyraInputBox
        {
            Text = value.ToString(),
            Width = 55,
            InputFilter = c => char.IsDigit(c) || c == '-'
        };
        box.TextChangedByUser += (_, _) =>
        {
            if (int.TryParse(box.Text, out int v))
                onChanged(v);
        };
        return box;
    }

    private Widget BuildLayerCombo(ToolTipOverrideData data)
    {
        var combo = new ComboView { MinWidth = 130, VerticalAlignment = VerticalAlignment.Center };

        for (int i = 0; i < LayerNames.Length; i++)
            combo.ListView.Widgets.Add(new Myra.Graphics2D.UI.Label { Text = LayerNames[i] });

        combo.ListView.SelectedIndex = Array.IndexOf(LayerValues, data.ItemLayer);
        combo.ListView.SelectedIndexChanged += (_, _) =>
        {
            if (combo.ListView.SelectedIndex is not int idx)
                return;

            data.ItemLayer = (TooltipLayers)LayerValues.GetValue(idx)!;
            data.Save();
            ShowSaved();
        };

        return combo;
    }

    private void AddNewOverride()
    {
        if (ProfileManager.CurrentProfile == null)
            return;

        // Requesting the next (out-of-range) index makes ToolTipOverrideData create and persist a
        // new default entry, which the refresh then selects for editing.
        int next = TooltipOverridesConfig.Current.Overrides.Count;
        ToolTipOverrideData.Get(next);
        RefreshCombo(next);
    }

    private void DeleteSelected()
    {
        if (_selectedIndex < 0 || ProfileManager.CurrentProfile == null)
            return;

        List<ToolTipOverrideData> overrides = TooltipOverridesConfig.Current.Overrides;
        if (_selectedIndex >= overrides.Count)
            return;

        overrides[_selectedIndex].Delete();
        RefreshCombo(_selectedIndex);
    }

    private void MoveSelected(int delta)
    {
        if (_selectedIndex < 0 || ProfileManager.CurrentProfile == null)
            return;

        List<ToolTipOverrideData> overrides = TooltipOverridesConfig.Current.Overrides;
        if (_selectedIndex >= overrides.Count)
            return;

        TooltipOverridesConfig.Current.Move(_selectedIndex, delta);
        RefreshCombo(_selectedIndex + delta);
    }

    private void ConfirmDeleteAll() => new MyraDialog(
            TazLang.Get("tooltipconfig_deleteall"),
            new MyraLabel(
                TazLang.Get("tooltipconfig_deleteall_confirm"),
                MyraLabel.TextStyle.P),
            ok =>
            {
                if (!ok)
                    return;

                ClearAll();
                RefreshCombo(-1);
            });

    private static void ClearAll()
    {
        if (ProfileManager.CurrentProfile == null)
            return;

        TooltipOverridesConfig.Current.Clear();
    }

    private void ShowSaved()
    {
        _statusLabel.Text = TazLang.Get("tooltipconfig_saved");
        _statusLabel.Visible = true;
        _statusUntil = DateTime.Now.AddSeconds(1);
    }
}
