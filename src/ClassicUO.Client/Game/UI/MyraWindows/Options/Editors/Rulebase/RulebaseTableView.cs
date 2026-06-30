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

/// <summary>
///     Renders a list of <typeparamref name="TRule" /> rows as a styled grid: header, striped/selected
///     row backgrounds, and per-column cells produced by <see cref="RulebaseColumn{TRule}.CellFactory" />.
///     Cell widgets are cached per rule/column so re-rendering on data changes is cheap.
/// </summary>
/// <typeparam name="TRule">The rule type the table displays.</typeparam>
public sealed class RulebaseTableView<TRule> : Panel where TRule : IRule
{
    private readonly MyraGrid _grid = new() { RowSpacing = 0, HorizontalAlignment = HorizontalAlignment.Stretch };
    private readonly List<TRule> _rules = [];
    private readonly List<Panel> _rowBackgrounds = [];
    private readonly Dictionary<TRule, Dictionary<RulebaseColumn<TRule>, Widget>> _cellCache = [];
    private readonly Dictionary<Widget, Panel> _cellWrappers = [];
    private RulebaseColumn<TRule>[] _lastVisibleColumns = [];
    private Panel? _headerBackground;
    private readonly List<MyraLabel> _headerLabels = [];
    private bool _lastShowHeader;

    /// <summary>Raised when <see cref="SelectedIndex" /> changes via <see cref="SetSelectedIndex" /></summary>
    public event EventHandler? SelectedIndexChanged;

    /// <summary>The columns to render. Mutating this list does not auto-refresh; call <see cref="Refresh" /></summary>
    public IList<RulebaseColumn<TRule>> Columns { get; }

    /// <summary>Visual styling applied to the table; changes auto-trigger a refresh</summary>
    public RulebaseStyleOptions StyleOptions { get; }

    /// <summary>Index of the currently selected row, or null if none is selected</summary>
    public int? SelectedIndex { get; private set; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="RulebaseTableView{TRule}" /> class.
    /// </summary>
    /// <param name="columns">The columns to render.</param>
    /// <param name="styleOptions">Visual styling for the table.</param>
    public RulebaseTableView(IList<RulebaseColumn<TRule>> columns, RulebaseStyleOptions styleOptions)
    {
        Columns = columns;
        StyleOptions = styleOptions;
        StyleOptions.PropertyChanged += (_, _) => Refresh();
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Top;
        Widgets.Add(_grid);
    }

    /// <summary>
    ///     Sets the selected row index and re-renders to reflect the new selection highlight.
    /// </summary>
    /// <param name="index">The row index to select, or null to clear selection.</param>
    public void SetSelectedIndex(int? index)
    {
        if (SelectedIndex == index)
            return;

        SelectedIndex = index;
        Refresh();
        SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    ///     Replaces the displayed rules and drops cached cells for any rules no longer present.
    /// </summary>
    /// <param name="rules">The new set of rules to display.</param>
    public void SetRules(IEnumerable<TRule> rules)
    {
        _rules.Clear();
        _rules.AddRange(rules);

        var currentRules = new HashSet<TRule>(_rules);
        var rulesToRemove = _cellCache.Keys.Where(r => !currentRules.Contains(r)).ToList();

        foreach (TRule r in rulesToRemove)
        {
            if (_cellCache.TryGetValue(r, out Dictionary<RulebaseColumn<TRule>, Widget>? cells))
                foreach (Widget cell in cells.Values)
                    _cellWrappers.Remove(cell);
            _cellCache.Remove(r);
        }

        Refresh();
    }

    /// <summary>
    ///     Rebuilds the underlying grid's structure and cell contents to match the current rules,
    ///     columns, and style options. Reuses cached cell widgets where possible.
    /// </summary>
    /// <param name="force">When true, skips change-detection and rebuilds the grid structure unconditionally.</param>
    public void Refresh(bool force = false)
    {
        _grid.Border = StyleOptions.OuterBorder.Brush;
        _grid.BorderThickness = StyleOptions.OuterBorder.Thickness;

        RulebaseColumn<TRule>[] visibleColumns = GetVisibleColumns().ToArray();

        RebuildGridStructureIfNeeded(visibleColumns, force);

        if (visibleColumns.Length == 0)
            return;

        HashSet<Widget> activeWidgets = [];
        int currentRow = RenderHeader(visibleColumns, activeWidgets);
        RenderRows(visibleColumns, currentRow, activeWidgets);
        RemoveInactiveWidgets(activeWidgets);
    }

    /// <summary>Clears and re-seeds the grid's column/row proportions when columns or header visibility change</summary>
    /// <param name="visibleColumns">The columns currently visible</param>
    /// <param name="force">When true, skips change-detection and rebuilds unconditionally</param>
    private void RebuildGridStructureIfNeeded(RulebaseColumn<TRule>[] visibleColumns, bool force)
    {
        // Short circuit the logic with 'force' to avoid comparing the sequences in case force re-render is issued
        bool columnsChanged = force || !visibleColumns.SequenceEqual(_lastVisibleColumns);
        bool headerVisibilityChanged = force || _lastShowHeader != StyleOptions.ShowHeader;

        if (!columnsChanged && !headerVisibilityChanged)
            return;

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

    /// <summary>Renders the header row, if enabled, reusing cached label widgets</summary>
    /// <param name="visibleColumns">The columns currently visible</param>
    /// <param name="activeWidgets">Set to record widgets still in use this refresh</param>
    /// <returns>The grid row index at which rule rows should start</returns>
    private int RenderHeader(RulebaseColumn<TRule>[] visibleColumns, HashSet<Widget> activeWidgets)
    {
        const int currentRow = 0;

        if (!StyleOptions.ShowHeader)
            return currentRow;

        if (_grid.RowsProportions.Count == 0)
            _grid.RowsProportions.Add(new Proportion(ProportionType.Auto));

        _headerBackground ??= new Panel { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
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
                _headerLabels.Add(
                    CreateHeaderCell(visibleColumns[i].Header, i < visibleColumns.Length - 1)
                );

            MyraLabel headerLabel = _headerLabels[i];
            headerLabel.Text = visibleColumns[i].Header;
            headerLabel.Tooltip = visibleColumns[i].HeaderTooltip;
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

        return currentRow + 1;
    }

    /// <summary>Syncs row proportions to the current rule count and renders each rule's background and cells</summary>
    /// <param name="visibleColumns">The columns currently visible</param>
    /// <param name="startRow">The grid row index of the first rule row</param>
    /// <param name="activeWidgets">Set to record widgets still in use this refresh</param>
    private void RenderRows(RulebaseColumn<TRule>[] visibleColumns, int startRow, HashSet<Widget> activeWidgets)
    {
        while (_grid.RowsProportions.Count < startRow + _rules.Count)
            _grid.RowsProportions.Add(new Proportion(ProportionType.Auto));

        while (_grid.RowsProportions.Count > startRow + _rules.Count)
            _grid.RowsProportions.RemoveAt(_grid.RowsProportions.Count - 1);

        for (int i = 0; i < _rules.Count; i++)
        {
            int gridRow = startRow + i;
            TRule rule = _rules[i];

            RenderRowBackground(i, gridRow, visibleColumns.Length, activeWidgets);
            RenderRowCells(rule, gridRow, visibleColumns, activeWidgets);
        }
    }

    /// <summary>Creates or reuses a row's background panel and positions it in the grid</summary>
    /// <param name="rowIndex">The rule index of the row</param>
    /// <param name="gridRow">The grid row index to place the background at</param>
    /// <param name="columnCount">The number of visible columns, used for the colspan</param>
    /// <param name="activeWidgets">Set to record widgets still in use this refresh</param>
    private void RenderRowBackground(int rowIndex, int gridRow, int columnCount, HashSet<Widget> activeWidgets)
    {
        if (rowIndex >= _rowBackgrounds.Count)
            _rowBackgrounds.Add(
                new Panel { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch }
            );

        Panel bg = _rowBackgrounds[rowIndex];
        bg.Background = new SolidBrush(GetRowColor(rowIndex));
        bg.Border = rowIndex + 1 < _rules.Count ? StyleOptions.RowBorders?.Brush : null;
        bg.BorderThickness = new Thickness(0, 0, 0, StyleOptions.RowBorders?.Thickness ?? 0);

        if (!_grid.Widgets.Contains(bg))
            _grid.AddWidget(bg, gridRow, 0, colspan: columnCount);
        else
        {
            Grid.SetRow(bg, gridRow);
            Grid.SetColumnSpan(bg, columnCount);
        }

        activeWidgets.Add(bg);
    }

    /// <summary>Creates or reuses a rule's cached cell widgets for each visible column and positions them in the grid</summary>
    /// <param name="rule">The rule whose cells are being rendered</param>
    /// <param name="gridRow">The grid row index to place the cells at</param>
    /// <param name="visibleColumns">The columns currently visible</param>
    /// <param name="activeWidgets">Set to record widgets still in use this refresh</param>
    private void RenderRowCells(TRule rule, int gridRow, RulebaseColumn<TRule>[] visibleColumns, HashSet<Widget> activeWidgets)
    {
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

            Widget gridWidget = UpdateCell(cell, col.CellContentAlignment, j < visibleColumns.Length - 1);

            if (!_grid.Widgets.Contains(gridWidget))
                _grid.AddWidget(gridWidget, gridRow, j);
            else
            {
                Grid.SetRow(gridWidget, gridRow);
                Grid.SetColumn(gridWidget, j);
            }

            activeWidgets.Add(gridWidget);
        }
    }

    /// <summary>Removes any grid widgets not touched during this refresh (e.g. dropped rows/columns)</summary>
    /// <param name="activeWidgets">Widgets that were rendered this refresh and should be kept</param>
    private void RemoveInactiveWidgets(HashSet<Widget> activeWidgets)
    {
        List<Widget> toRemove = _grid.Widgets.Where(w => !activeWidgets.Contains(w)).ToList();

        foreach (Widget w in toRemove)
            _grid.Widgets.Remove(w);
    }

    /// <summary>Builds a styled header label widget</summary>
    /// <param name="text">The header text to display</param>
    /// <param name="withRightBorder">Whether to draw a vertical separator on the cell's right edge</param>
    private MyraLabel CreateHeaderCell(string text, bool withRightBorder) =>
        new(text, MyraLabel.TextStyle.H5)
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center,
            Padding = new Thickness(6, 4),
            Border = withRightBorder ? StyleOptions.HeaderVerticalBorder : null,
            BorderThickness = withRightBorder ? new Thickness(0, 0, 1, 0) : new Thickness(0)
        };

    /// <summary>
    ///     Applies cell padding/alignment and the right-hand column border, if any.
    ///     Returns a wrapper Panel for non-stretch alignments so the border spans the full cell width.
    /// </summary>
    /// <param name="content">The cell widget to style</param>
    /// <param name="hAlign">The horizontal alignment of the cell's content</param>
    /// <param name="withRightBorder">Whether to draw a vertical separator on the cell's right edge</param>
    private Widget UpdateCell(Widget content, HorizontalAlignment hAlign, bool withRightBorder)
    {
        content.HorizontalAlignment = hAlign;
        content.VerticalAlignment = VerticalAlignment.Center;
        content.Padding = new Thickness(6, 3);

        Widget cellWidget;

        if (hAlign != HorizontalAlignment.Stretch)
        {
            if (!_cellWrappers.TryGetValue(content, out Panel? wrapper))
            {
                wrapper = new Panel { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
                wrapper.Widgets.Add(content);
                _cellWrappers[content] = wrapper;
            }

            cellWidget = wrapper;
        }
        else
            cellWidget = content;

        var thickness = new Thickness(0, 0, StyleOptions.ColumnBorders?.Thickness ?? 0, 0);

        if (!withRightBorder)
            thickness.Right = 0;

        cellWidget.Border = StyleOptions.ColumnBorders?.Brush;
        cellWidget.BorderThickness = thickness;

        return cellWidget;
    }

    /// <summary>Determines a row's background color from selection state and striping settings</summary>
    /// <param name="rowIndex">The rule index of the row</param>
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

    /// <summary>Filters <see cref="Columns" /> down to those currently visible</summary>
    private IEnumerable<RulebaseColumn<TRule>> GetVisibleColumns() =>
        Columns.Where(column => column.Visible);

    /// <summary>
    ///     Resolves the rule index of the row located at the given screen position, accounting for the header row.
    /// </summary>
    /// <param name="globalPos">The position to test, in screen coordinates.</param>
    /// <returns>The rule index hit, or null if the position is outside any row.</returns>
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
