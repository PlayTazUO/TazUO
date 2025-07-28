using System;
using ClassicUO.Game.UI.Controls;

namespace ClassicUO.Game.UI;

public class Positioner
{
    public int TopPadding;
    public int LeftPadding;
    public int BlankLineHeight;
    public int IndentWidth;

    public int X, Y, LastY, LastHeight;

    // Table positioning properties
    private bool _tableMode = false;
    private int _tableColumns = 0;
    private int _currentColumn = 0;
    private int _tableStartX;
    private int _tableStartY;
    private int _columnWidth;
    private int _columnPadding;
    private int _maxRowHeight = 0;

    public Positioner(int leftPadding = 2, int topPadding = 5, int blankLineHeight = 20, int indentation = 40)
    {
        LeftPadding = leftPadding;
        TopPadding = topPadding;
        BlankLineHeight = blankLineHeight;
        IndentWidth = indentation;

        Y = LastY = TopPadding;
        X = leftPadding;
    }

    public void BlankLine()
    {
        if (_tableMode)
        {
            EndTable();
        }

        LastY = Y;
        Y += BlankLineHeight;
    }

    public void Indent()
    {
        X += IndentWidth;
    }

    public void RemoveIndent()
    {
        X -= IndentWidth;
    }

    /// <summary>
    /// Start table positioning mode
    /// </summary>
    /// <param name="columns">Number of columns in the table</param>
    /// <param name="columnWidth">Width of each column (0 for auto-width)</param>
    /// <param name="columnPadding">Padding between columns</param>
    public void StartTable(int columns, int columnWidth = 0, int columnPadding = 10)
    {
        if (_tableMode)
        {
            EndTable(); // End any existing table first
        }

        _tableMode = true;
        _tableColumns = columns;
        _currentColumn = 0;
        _tableStartX = X;
        _tableStartY = Y;
        _columnWidth = columnWidth;
        _columnPadding = columnPadding;
        _maxRowHeight = 0;
    }

    /// <summary>
    /// End table positioning mode and return to normal positioning
    /// </summary>
    public void EndTable()
    {
        if (!_tableMode) return;

        _tableMode = false;

        // Move Y to below the last row
        if (_maxRowHeight > 0)
        {
            Y = _tableStartY + _maxRowHeight + TopPadding;
        }

        LastY = Y;
        X = _tableStartX;

        // Reset table state
        _tableColumns = 0;
        _currentColumn = 0;
        _maxRowHeight = 0;
    }

    /// <summary>
    /// Force move to next row in table mode
    /// </summary>
    public void NextTableRow()
    {
        if (!_tableMode) return;

        // Move to next row
        _tableStartY += _maxRowHeight + TopPadding;
        Y = _tableStartY;
        _currentColumn = 0;
        _maxRowHeight = 0;
    }

    public Control Position(Control c)
    {
        if (_tableMode)
        {
            return PositionInTable(c);
        }

        c.X = X;
        c.Y = Y;

        LastY = Y;
        Y += c.Height + TopPadding;
        LastHeight = c.Height;

        return c;
    }

    private Control PositionInTable(Control c)
    {
        // Calculate column position
        int columnX = _tableStartX + (_currentColumn * (_columnWidth + _columnPadding));

        // If using auto-width, we can't pre-calculate positions perfectly
        // This is a simple implementation that assumes uniform spacing
        c.X = columnX;
        c.Y = _tableStartY;

        // Track the maximum height in this row
        _maxRowHeight = Math.Max(_maxRowHeight, c.Height);

        // Move to next column
        _currentColumn++;

        // Check if we need to move to next row
        if (_currentColumn >= _tableColumns)
        {
            NextTableRow();
        }

        return c;
    }

    /// <summary>
    /// Position control in a specific table cell
    /// </summary>
    /// <param name="c">Control to position</param>
    /// <param name="column">Column index (0-based)</param>
    /// <param name="row">Row index (0-based)</param>
    /// <returns></returns>
    public Control PositionInTableCell(Control c, int column, int row)
    {
        if (!_tableMode)
        {
            throw new Exception("Must be in table mode to use PositionInTableCell");
        }

        if (column >= _tableColumns)
        {
            throw new ArgumentException($"Column {column} exceeds table columns {_tableColumns}");
        }

        int cellX = _tableStartX + (column * (_columnWidth + _columnPadding));
        int cellY = _tableStartY + (row * (_maxRowHeight + TopPadding));

        c.X = cellX;
        c.Y = cellY;

        return c;
    }

    public Control PositionRightOf(Control c, Control other, int padding = 5)
    {
        c.Y = other.Y;
        c.X = other.X + other.Width + padding;

        return c;
    }

    public Control PositionExact(Control c, int x, int y)
    {
        c.X = x;
        c.Y = y;

        return c;
    }

    public void Reset()
    {
        if (_tableMode)
        {
            EndTable();
        }

        X = LeftPadding;
        Y = LastY = TopPadding;
    }

    /// <summary>
    /// Check if currently in table positioning mode
    /// </summary>
    public bool IsInTableMode => _tableMode;

    /// <summary>
    /// Get current table info (columns, current column, current row)
    /// </summary>
    public (int columns, int currentColumn, int currentRow) GetTableInfo()
    {
        if (!_tableMode) return (0, 0, 0);

        int currentRow = _tableStartY == Y ? 0 : (_tableStartY - Y) / (_maxRowHeight + TopPadding);
        return (_tableColumns, _currentColumn, currentRow);
    }
}
