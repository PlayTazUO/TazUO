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
    private readonly List<Panel> _rowBackgrounds = [];
    private readonly Dictionary<TRule, Dictionary<RulebaseColumn<TRule>, Widget>> _cellCache = [];
    private RulebaseColumn<TRule>[] _lastVisibleColumns = [];
    private Panel? _headerBackground;
    private readonly List<MyraLabel> _headerLabels = [];
    private bool _lastShowHeader;

    public event EventHandler? SelectedIndexChanged;

    public IList<RulebaseColumn<TRule>> Columns { get; }
    public RulebaseStyleOptions StyleOptions { get; }
    public int? SelectedIndex { get; private set; }

    public RulebaseTableView(IList<RulebaseColumn<TRule>> columns, RulebaseStyleOptions styleOptions)
    {
        Columns = columns;
        StyleOptions = styleOptions;
        StyleOptions.PropertyChanged += (_, _) => Refresh();
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

        var currentRules = new HashSet<TRule>(_rules);
        var rulesToRemove = _cellCache.Keys.Where(r => !currentRules.Contains(r)).ToList();

        foreach (TRule r in rulesToRemove)
            _cellCache.Remove(r);

        Refresh();
    }

    public void Refresh(bool force = false)
    {
        _grid.Border = StyleOptions.OuterBorder.Brush;
        _grid.BorderThickness = StyleOptions.OuterBorder.Thickness;

        RulebaseColumn<TRule>[] visibleColumns = GetVisibleColumns().ToArray();

        // Short circuit the logic with 'force' to avoid comparing the sequences in case force re-render is issued
        bool columnsChanged = force || !visibleColumns.SequenceEqual(_lastVisibleColumns);
        bool headerVisibilityChanged = force || _lastShowHeader != StyleOptions.ShowHeader;

        if (columnsChanged || headerVisibilityChanged)
        {
            _grid.Widgets.Clear();
            _grid.ColumnsProportions.Clear();
            _grid.RowsProportions.Clear();
            _rowBackgrounds.Clear();
            _headerLabels.Clear();
            _headerBackground = null;

            _lastVisibleColumns = visibleColumns;
            _lastShowHeader = StyleOptions.ShowHeader;

            foreach (RulebaseColumn<TRule> column in visibleColumns)
                _grid.ColumnsProportions.Add(column.Proportion);
        }

        if (visibleColumns.Length == 0)
            return;

        HashSet<Widget> activeWidgets = [];
        int currentRow = 0;

        if (StyleOptions.ShowHeader)
        {
            if (_grid.RowsProportions.Count == 0)
                _grid.RowsProportions.Add(new Proportion(ProportionType.Auto));

            _headerBackground ??= new Panel
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            _headerBackground.Background = new SolidBrush(StyleOptions.HeaderBackground);

            if (!_grid.Widgets.Contains(_headerBackground))
                _grid.AddWidget(_headerBackground, currentRow, 0, colspan: visibleColumns.Length);
            else
            {
                Grid.SetRow(_headerBackground, currentRow);
                Grid.SetColumnSpan(_headerBackground, visibleColumns.Length);
            }

            activeWidgets.Add(_headerBackground);

            for (int i = 0; i < visibleColumns.Length; i++)
            {
                if (i >= _headerLabels.Count)
                {
                    _headerLabels.Add(
                        CreateHeaderCell(visibleColumns[i].Header, i < visibleColumns.Length - 1)
                    );
                }

                MyraLabel headerLabel = _headerLabels[i];
                headerLabel.Text = visibleColumns[i].Header;
                headerLabel.Border = i < visibleColumns.Length - 1 ? StyleOptions.HeaderVerticalBorder : null;
                headerLabel.BorderThickness = i < visibleColumns.Length - 1 ? new Thickness(0, 0, 1, 0) : new Thickness(0);

                if (!_grid.Widgets.Contains(headerLabel))
                    _grid.AddWidget(headerLabel, currentRow, i);
                else
                {
                    Grid.SetRow(headerLabel, currentRow);
                    Grid.SetColumn(headerLabel, i);
                }

                activeWidgets.Add(headerLabel);
            }

            currentRow++;
        }

        while (_grid.RowsProportions.Count < currentRow + _rules.Count)
            _grid.RowsProportions.Add(new Proportion(ProportionType.Auto));

        while (_grid.RowsProportions.Count > currentRow + _rules.Count)
            _grid.RowsProportions.RemoveAt(_grid.RowsProportions.Count - 1);

        for (int i = 0; i < _rules.Count; i++)
        {
            int gridRow = currentRow + i;
            TRule rule = _rules[i];

            if (i >= _rowBackgrounds.Count)
            {
                _rowBackgrounds.Add(
                    new Panel
                    {
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        VerticalAlignment = VerticalAlignment.Stretch
                    }
                );
            }

            Panel bg = _rowBackgrounds[i];
            bg.Background = new SolidBrush(GetRowColor(i));
            bg.Border = i + 1 < _rules.Count ? StyleOptions.RowBorders?.Brush : null;
            bg.BorderThickness = new Thickness(0, 0, 0, StyleOptions.RowBorders?.Thickness ?? 0);

            if (!_grid.Widgets.Contains(bg))
                _grid.AddWidget(bg, gridRow, 0, colspan: visibleColumns.Length);
            else
            {
                Grid.SetRow(bg, gridRow);
                Grid.SetColumnSpan(bg, visibleColumns.Length);
            }

            activeWidgets.Add(bg);

            if (!_cellCache.TryGetValue(rule, out Dictionary<RulebaseColumn<TRule>, Widget>? ruleCells))
            {
                ruleCells = [];
                _cellCache[rule] = ruleCells;
            }

            for (int j = 0; j < visibleColumns.Length; j++)
            {
                RulebaseColumn<TRule> col = visibleColumns[j];

                if (!ruleCells.TryGetValue(col, out Widget? cell))
                {
                    cell = col.CellFactory(rule);
                    ruleCells[col] = cell;
                }

                UpdateCell(cell, j < visibleColumns.Length - 1);

                if (!_grid.Widgets.Contains(cell))
                    _grid.AddWidget(cell, gridRow, j);
                else
                {
                    Grid.SetRow(cell, gridRow);
                    Grid.SetColumn(cell, j);
                }

                activeWidgets.Add(cell);
            }
        }

        List<Widget> toRemove = _grid.Widgets.Where(w => !activeWidgets.Contains(w)).ToList();

        foreach (Widget w in toRemove)
            _grid.Widgets.Remove(w);
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

    private void UpdateCell(Widget content, bool withRightBorder)
    {
        content.HorizontalAlignment = HorizontalAlignment.Stretch;
        content.VerticalAlignment = VerticalAlignment.Center;
        content.Padding = new Thickness(6, 3);
        content.Border = StyleOptions.ColumnBorders?.Brush;

        var thickness = new Thickness(0, 0, StyleOptions.ColumnBorders?.Thickness ?? 0, 0);

        if (!withRightBorder)
            thickness.Right = 0;

        content.BorderThickness = thickness;
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
