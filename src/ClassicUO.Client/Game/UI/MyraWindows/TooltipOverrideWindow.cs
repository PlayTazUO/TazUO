#nullable enable
using System;
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
/// Myra-based replacement for the legacy <c>TooltipConfigGump</c>. Lists the current profile's
/// tooltip override rules with per-row search/format text, min/max value ranges and a layer
/// filter, plus toolbar actions to add, import, export, refresh and clear all overrides.
///
/// Each edit is committed straight to the profile's override lists through the shared
/// <see cref="ToolTipOverrideData"/> model, and structural changes (add/delete/clear) rebuild
/// the lightweight widget list rather than tearing down per-label textures like the old gump.
/// </summary>
public sealed class TooltipOverrideWindow : MyraControl
{
    private static readonly Array LayerValues = Enum.GetValues(typeof(TooltipLayers));
    private static readonly string[] LayerNames = Enum.GetNames(typeof(TooltipLayers));

    private readonly World _world;
    private readonly VerticalStackPanel _rowsPanel = new() { Spacing = MyraStyle.STANDARD_SPACING };

    private MyraLabel _statusLabel = null!;
    private DateTime _statusUntil = DateTime.MinValue;

    public TooltipOverrideWindow(World world) : base(TazLang.Get("tooltipconfig_title", "Tooltip Override Configuration"))
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
            if (gump is TooltipOverrideWindow w && !w.IsDisposed)
            {
                w.BringOnTop();
                return;
            }
        }

        UIManager.Add(new TooltipOverrideWindow(world));
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

        BuildRows();
        root.Widgets.Add(new ScrollViewer { MaxHeight = 450, MinWidth = 620, Content = _rowsPanel });

        SetRootContent(root);
    }

    private Widget BuildToolbar()
    {
        var toolbar = new HorizontalStackPanel { Spacing = 4, VerticalAlignment = VerticalAlignment.Center };

        toolbar.Widgets.Add(new MyraButton(TazLang.Get("tooltipconfig_add", "Add +"), AddNewOverride));
        toolbar.Widgets.Add(new MyraButton(TazLang.Get("tooltipconfig_export", "Export"),
            () => ToolTipOverrideData.ExportOverrideSettings(_world)));
        toolbar.Widgets.Add(new MyraButton(TazLang.Get("tooltipconfig_import", "Import"),
            ToolTipOverrideData.ImportOverrideSettings));
        toolbar.Widgets.Add(new MyraButton(TazLang.Get("tooltipconfig_refresh", "Refresh"), BuildRows));

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

    private void BuildRows()
    {
        _rowsPanel.Widgets.Clear();

        int count = ProfileManager.CurrentProfile == null ? 0 : TooltipOverridesConfig.Current.Overrides.Count;

        if (count == 0)
        {
            _rowsPanel.Widgets.Add(new MyraLabel(
                TazLang.Get("tooltipconfig_empty", "No tooltip overrides yet. Use \"Add +\" to create one."),
                MyraLabel.TextStyle.P));
            return;
        }

        for (int i = 0; i < count; i++)
            _rowsPanel.Widgets.Add(BuildRow(ToolTipOverrideData.Get(i)));
    }

    private Widget BuildRow(ToolTipOverrideData data)
    {
        var body = new VerticalStackPanel
        {
            Spacing = 4,
            Border = new SolidBrush(MyraStyle.GridBorderColor),
            BorderThickness = new Thickness(1),
            Background = new SolidBrush(new Color(0, 0, 0, 25)),
            Padding = new Thickness(4)
        };

        // Row 1: delete button, search text and format text.
        var row1 = new HorizontalStackPanel { Spacing = 4, VerticalAlignment = VerticalAlignment.Center };

        MyraButton deleteButton = new("X", () =>
        {
            data.Delete();
            BuildRows();
        })
        {
            Tooltip = TazLang.Get("tooltipconfig_deleterow", "Delete this override")
        };
        MyraStyle.ApplyButtonDangerStyle(deleteButton);
        row1.Widgets.Add(deleteButton);

        var searchBox = new MyraInputBox
        {
            Text = data.SearchText,
            Width = 220,
            Tooltip = TazLang.Get("tooltipconfig_searchtext_tooltip",
                "This is the search text for matching tooltip lines.")
        };
        searchBox.TextChangedByUser += (_, _) =>
        {
            // The override is unusable without search text, so only commit non-empty values (matches the legacy gump).
            if (string.IsNullOrEmpty(searchBox.Text))
                return;

            data.SearchText = searchBox.Text;
            data.Save();
            ShowSaved();
        };
        row1.Widgets.Add(searchBox);

        var formatBox = new MyraInputBox
        {
            Text = data.FormattedText,
            Width = 300,
            Tooltip = TazLang.Get("tooltipconfig_formattext_tooltip",
                "This is what the matching tooltip line will be replaced with. See the wiki for more details!")
        };
        formatBox.TextChangedByUser += (_, _) =>
        {
            data.FormattedText = formatBox.Text ?? "";
            data.Save();
            ShowSaved();
        };
        row1.Widgets.Add(formatBox);

        body.Widgets.Add(row1);

        // Row 2: two min/max value ranges and the layer filter.
        var row2 = new HorizontalStackPanel { Spacing = 4, VerticalAlignment = VerticalAlignment.Center };

        row2.Widgets.Add(new MyraLabel(TazLang.Get("tooltipconfig_minmax", "Min/Max"), MyraLabel.TextStyle.P));
        row2.Widgets.Add(NumericBox(data.Min1, v => { data.Min1 = v; data.Save(); ShowSaved(); }));
        row2.Widgets.Add(NumericBox(data.Max1, v => { data.Max1 = v; data.Save(); ShowSaved(); }));

        row2.Widgets.Add(new MyraLabel(TazLang.Get("tooltipconfig_minmax", "Min/Max"), MyraLabel.TextStyle.P));
        row2.Widgets.Add(NumericBox(data.Min2, v => { data.Min2 = v; data.Save(); ShowSaved(); }));
        row2.Widgets.Add(NumericBox(data.Max2, v => { data.Max2 = v; data.Save(); ShowSaved(); }));

        row2.Widgets.Add(new MyraLabel(TazLang.Get("tooltipconfig_layer", "Layer"), MyraLabel.TextStyle.P));
        row2.Widgets.Add(BuildLayerCombo(data));

        body.Widgets.Add(row2);

        // Row 3: optional custom tooltip border color applied when this rule matches.
        body.Widgets.Add(BuildBorderColor(data));

        return body;
    }

    private Widget BuildBorderColor(ToolTipOverrideData data)
    {
        var row = new HorizontalStackPanel { Spacing = 4, VerticalAlignment = VerticalAlignment.Center };

        row.Widgets.Add(new MyraLabel(TazLang.Get("tooltipconfig_bordercolor", "Match Border Color:"), MyraLabel.TextStyle.P)
        {
            Tooltip = TazLang.Get("tooltipconfig_bordercolor_tooltip",
                "Optional. When this rule matches, the tooltip is drawn with a thick border in this hue.")
        });

        ushort swatchHue = data.HasBorderHue ? (ushort)data.BorderHue : (ushort)0;
        var swatch = new MyraArtTexture(0x0FAB, swatchHue, 20) { Tooltip = BorderSwatchTooltip(data) };

        swatch.TouchUp += (_, _) =>
        {
            if (!swatch.Enabled)
                return;

            UIManager.GetGump<ClassicUO.Game.UI.Gumps.ModernColorPicker>()?.Dispose();
            UIManager.Add(new ClassicUO.Game.UI.Gumps.ModernColorPicker(World.Instance, newHue =>
            {
                data.BorderHue = newHue;
                data.Save();
                swatch.SetColorByHue(newHue);
                swatch.Tooltip = BorderSwatchTooltip(data);
                ShowSaved();
            }, isClickable: true));
        };
        row.Widgets.Add(swatch);

        MyraButton clear = new(TazLang.Get("tooltipconfig_bordercolor_clear", "Clear"), () =>
        {
            data.BorderHue = -1;
            data.Save();
            swatch.SetColorByHue(0);
            swatch.Tooltip = BorderSwatchTooltip(data);
            ShowSaved();
        })
        {
            Tooltip = TazLang.Get("tooltipconfig_bordercolor_clear_tooltip", "Remove the custom border color for this rule")
        };
        MyraStyle.ApplyButtonDangerStyle(clear);
        row.Widgets.Add(clear);

        return row;
    }

    private static string BorderSwatchTooltip(ToolTipOverrideData data) =>
        data.HasBorderHue
            ? string.Format(TazLang.Get("tooltipconfig_bordercolor_set_tooltip", "Custom border hue: {0}. Click to change."), data.BorderHue)
            : TazLang.Get("tooltipconfig_bordercolor_none_tooltip", "No custom border. Click to choose a color.");

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
            combo.ListView.Widgets.Add(new Myra.Graphics2D.UI.Label { Text = LayerNames[i], Tag = i });

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
        // Requesting the next (out-of-range) index makes ToolTipOverrideData create and persist a
        // new default entry, so we just rebuild the list afterwards to show it.
        if (ProfileManager.CurrentProfile == null)
            return;

        ToolTipOverrideData.Get(TooltipOverridesConfig.Current.Overrides.Count);
        BuildRows();
    }

    private void ConfirmDeleteAll()
    {
        new MyraDialog(
            TazLang.Get("tooltipconfig_deleteall", "Delete All"),
            new MyraLabel(
                TazLang.Get("tooltipconfig_deleteall_confirm",
                    "This will remove ALL tooltip override settings.\nThis is not reversible."),
                MyraLabel.TextStyle.P),
            ok =>
            {
                if (!ok)
                    return;

                ClearAll();
                BuildRows();
            });
    }

    private static void ClearAll()
    {
        if (ProfileManager.CurrentProfile == null)
            return;

        TooltipOverridesConfig.Current.Clear();
    }

    private void ShowSaved()
    {
        _statusLabel.Text = TazLang.Get("tooltipconfig_saved", "Saved");
        _statusLabel.Visible = true;
        _statusUntil = DateTime.Now.AddSeconds(1);
    }
}
