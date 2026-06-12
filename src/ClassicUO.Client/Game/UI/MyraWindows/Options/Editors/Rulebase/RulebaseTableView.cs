#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Options.Editors.Rulebase;

public sealed class RulebaseTableView<TRule> : VerticalStackPanel where TRule : IRule
{
    private readonly Panel _headerPanel = new();
    private readonly VerticalStackPanel _rowsPanel = new();
    private readonly List<TRule> _rules = [];

    public event EventHandler? SelectedIndexChanged;

    public IList<RulebaseColumn<TRule>> Columns { get; }
    public RulebaseStyleOptions StyleOptions { get; }
    public int? SelectedIndex { get; private set; }

    public RulebaseTableView(IList<RulebaseColumn<TRule>> columns, RulebaseStyleOptions styleOptions)
    {
        Columns = columns;
        StyleOptions = styleOptions;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Top;
        Spacing = 0;

        Widgets.Add(_headerPanel);
        Widgets.Add(_rowsPanel);
    }

    public void SetSelectedIndex(int? index)
    {
        if (SelectedIndex == index)
            return;

        SelectedIndex = index;
        RefreshRows();
        SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SetRules(IEnumerable<TRule> rules)
    {
        _rules.Clear();
        _rules.AddRange(rules);
        Refresh();
    }

    public void Refresh()
    {
        RefreshHeader();
        RefreshRows();
    }

    private void RefreshHeader()
    {
        _headerPanel.Widgets.Clear();
        _headerPanel.Visible = StyleOptions.ShowHeader;

        if (!StyleOptions.ShowHeader)
            return;

        _headerPanel.Widgets.Add(CreateHeaderGrid());
    }

    private Grid CreateHeaderGrid()
    {
        MyraGrid grid = CreateGrid();
        grid.Background = new SolidBrush(StyleOptions.HeaderBackground);

        int columnIndex = 0;
        RulebaseColumn<TRule>[] visibleColumns = GetVisibleColumns().ToArray();

        for (int i = 0; i < visibleColumns.Length; i++)
            grid.AddWidget(
                CreateHeaderCell(
                    visibleColumns[i].Header,
                    i < visibleColumns.Length - 1
                ),
                0,
                columnIndex++
            );

        return grid;
    }

    private MyraLabel CreateHeaderCell(string text, bool withRightBorder) =>
        new(text, MyraLabel.TextStyle.H5)
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center,
            Padding = new Thickness(6, 4),
            Border = withRightBorder ? StyleOptions.HeaderVerticalBorder : null,
            BorderThickness = withRightBorder ? new Thickness(0, 0, 1, 0) : new Thickness(0)
        };

    private void RefreshRows()
    {
        _rowsPanel.Widgets.Clear();

        for (int index = 0; index < _rules.Count; index++)
            _rowsPanel.Widgets.Add(CreateRow(_rules[index], index));
    }

    private Widget CreateRow(TRule rule, int rowIndex)
    {
        Panel rowPanel = CreateRowPanel(rowIndex);
        rowPanel.Widgets.Add(CreateRowGrid(rule));
        rowPanel.TouchDown += (_, _) => SetSelectedIndex(rowIndex);
        return rowPanel;
    }

    private Panel CreateRowPanel(int rowIndex)
    {
        var panel = new Panel
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Padding = new Thickness(0),
            Background = new SolidBrush(GetRowColor(rowIndex)),
            Border = StyleOptions.RowBorders.Brush,
            BorderThickness = StyleOptions.RowBorders.Thickness
        };

        return panel;
    }

    private Color GetRowColor(int rowIndex)
    {
        if (StyleOptions.HighlightSelectedRow && SelectedIndex == rowIndex)
            return StyleOptions.SelectedRowBackground;

        if (!StyleOptions.UseStripedRows)
            return Color.Transparent;

        return rowIndex % 2 == 0
            ? StyleOptions.EvenRowBackground
            : StyleOptions.OddRowBackground;
    }

    private Grid CreateRowGrid(TRule rule)
    {
        MyraGrid grid = CreateGrid();

        int columnIndex = 0;
        foreach (RulebaseColumn<TRule> column in GetVisibleColumns())
            grid.AddWidget(CreateCell(column.CellFactory(rule)), 0, columnIndex++);

        return grid;
    }

    private Widget CreateCell(Widget content)
    {
        content.HorizontalAlignment = HorizontalAlignment.Stretch;
        content.VerticalAlignment = VerticalAlignment.Center;
        content.Padding = new Thickness(6, 3);
        content.Border = StyleOptions.ColumnBorders.Brush;
        content.BorderThickness = StyleOptions.ColumnBorders.Thickness;
        return content;
    }

    private MyraGrid CreateGrid()
    {
        var grid = new MyraGrid { HorizontalAlignment = HorizontalAlignment.Stretch };
        foreach (RulebaseColumn<TRule> column in GetVisibleColumns())
            grid.ColumnsProportions.Add(column.Proportion);

        return grid;
    }

    private IEnumerable<RulebaseColumn<TRule>> GetVisibleColumns() =>
        Columns.Where(column => column.Visible);
}
