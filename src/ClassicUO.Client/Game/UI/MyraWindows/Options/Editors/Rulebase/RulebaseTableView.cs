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

public sealed class RulebaseTableView<TRule> : Panel where TRule : IRule
{
    private readonly MyraGrid _grid = new() { RowSpacing = 0, HorizontalAlignment = HorizontalAlignment.Stretch };
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

        Widgets.Add(_grid);
    }

    public void SetSelectedIndex(int? index)
    {
        if (SelectedIndex == index)
            return;

        SelectedIndex = index;
        Refresh();
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
        _grid.Widgets.Clear();
        _grid.ColumnsProportions.Clear();
        _grid.RowsProportions.Clear();

        RulebaseColumn<TRule>[] visibleColumns = GetVisibleColumns().ToArray();

        if (visibleColumns.Length == 0)
            return;

        foreach (RulebaseColumn<TRule> column in visibleColumns)
            _grid.ColumnsProportions.Add(column.Proportion);

        int currentRow = 0;

        if (StyleOptions.ShowHeader)
        {
            _grid.RowsProportions.Add(new Proportion(ProportionType.Auto));

            var headerBackground = new Panel
            {
                Background = new SolidBrush(StyleOptions.HeaderBackground),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            _grid.AddWidget(headerBackground, currentRow, 0, colspan: visibleColumns.Length);

            for (int i = 0; i < visibleColumns.Length; i++)
                _grid.AddWidget(
                    CreateHeaderCell(
                        visibleColumns[i].Header,
                        i < visibleColumns.Length - 1
                    ),
                    currentRow,
                    i
                );

            currentRow++;
        }

        for (int i = 0; i < _rules.Count; i++)
        {
            _grid.RowsProportions.Add(new Proportion(ProportionType.Auto));
            int rowIndex = i;
            int gridRow = currentRow + i;

            var rowBackground = new Panel
            {
                Background = new SolidBrush(GetRowColor(i)),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Border = StyleOptions.RowBorders.Brush,
                BorderThickness = StyleOptions.RowBorders.Thickness
            };

            _grid.AddWidget(rowBackground, gridRow, 0, colspan: visibleColumns.Length);

            for (int j = 0; j < visibleColumns.Length; j++)
            {
                Widget cell = CreateCell(
                    visibleColumns[j].CellFactory(_rules[i]),
                    j < visibleColumns.Length - 1
                );

                _grid.AddWidget(cell, gridRow, j);
            }
        }
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

    private Widget CreateCell(Widget content, bool withRightBorder)
    {
        content.HorizontalAlignment = HorizontalAlignment.Stretch;
        content.VerticalAlignment = VerticalAlignment.Center;
        content.Padding = new Thickness(6, 3);
        content.Border = StyleOptions.ColumnBorders.Brush;

        Thickness thickness = StyleOptions.ColumnBorders.Thickness;

        if (!withRightBorder)
            thickness.Right = 0;

        content.BorderThickness = thickness;

        return content;
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

    private IEnumerable<RulebaseColumn<TRule>> GetVisibleColumns() =>
        Columns.Where(column => column.Visible);

    public int? GetRowIndexAt(Point globalPos)
    {
        Widget? hit = _grid.HitTest(globalPos);
        if (hit == null)
            return null;

        Widget? current = hit;
        while (current != null && current.Parent != _grid)
            current = current.Parent;

        if (current == null)
            return null;

        int gridRow = Grid.GetRow(current);
        int ruleIndex = gridRow - (StyleOptions.ShowHeader ? 1 : 0);

        if (ruleIndex >= 0 && ruleIndex < _rules.Count)
            return ruleIndex;

        return null;
    }
}
