#nullable enable
using System;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

public class MyraGrid : Grid
{
    /// <summary>
    /// Standard Myra grid with defaults fit for TazUO
    /// </summary>
    public MyraGrid()
    {
        RowSpacing = 2;
        ColumnSpacing = 2;
    }

    public MyraGrid(Proportion[] columns, Proportion[] rows)
    {
        foreach (Proportion proportion in columns ?? [])
            AddColumn(proportion);

        foreach (Proportion proportion in rows ?? [])
            AddRow(proportion);
    }

    /// <param name="proportion">Leave proportion empty to default to auto proportions</param>
    internal void AddColumn(Proportion? proportion = null, uint count = 1)
    {
        for (uint i = 0; i < count; i++)
            ColumnsProportions.Add(proportion ?? new Proportion(ProportionType.Auto));
    }

    /// <param name="proportion">Leave proportion empty to default to auto proportions</param>
    internal void AddRow(Proportion? proportion = null, uint count = 1)
    {
        for (uint i = 0; i < count; i++)
            RowsProportions.Add(proportion ?? new Proportion(ProportionType.Auto));
    }

    internal void AddWidget(
        Widget widget,
        int row, int col,
        int? rowspan = null,
        int? colspan = null
    )
    {
        Widgets.Add(widget);
        SetRow(widget, row);
        SetColumn(widget, col);

        if (rowspan != null)
            SetRowSpan(widget, rowspan.Value);

        if (colspan != null)
            SetColumnSpan(widget, colspan.Value);
    }

    internal Widget[] SetupWithHeaders(params GridColumnInfo[] columns) => SetupWithHeaders(null, columns);

    /// <summary>
    /// Sets up the grid's columns and header row, optionally making each header a button that
    /// reports the column it belongs to. A sortable grid passes the callback and re-sorts itself.
    /// </summary>
    /// <param name="onHeaderClick">
    /// Invoked with the clicked column's index. Null leaves headers as plain labels.
    /// </param>
    /// <param name="columns">Column definitions, left to right.</param>
    /// <returns>
    /// The header widgets in column order, so a caller that keeps the grid around can update
    /// their labels in place instead of rebuilding the header row.
    /// </returns>
    internal Widget[] SetupWithHeaders(Action<int>? onHeaderClick, params GridColumnInfo[] columns)
    {
        MyraStyle.ApplyStandardGridStyling(this);

        foreach (GridColumnInfo col in columns)
        {
            AddColumn(col.Type == ColumnType.Fill
                ? new Proportion(ProportionType.Part, col.Weight)
                : new Proportion(ProportionType.Auto));
        }

        var headers = new Widget[columns.Length];
        for (int i = 0; i < columns.Length; i++)
        {
            GridColumnInfo col = columns[i];

            Widget header;
            if (onHeaderClick != null)
            {
                int index = i;
                header = new MyraButton(col.Label, () => onHeaderClick(index), MyraLabel.TextStyle.TableHeader)
                {
                    HorizontalAlignment = col.AlignRight ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                };
            }
            else
            {
                header = new MyraLabel(col.Label, MyraLabel.TextStyle.TableHeader,
                    col.AlignRight ? MyraLabel.AlignMode.Right : MyraLabel.AlignMode.Left);
            }

            headers[i] = header;
            AddWidget(header, 0, i);
        }

        return headers;
    }
}

internal readonly struct GridColumnInfo
{
    public readonly string Label;
    public readonly ColumnType Type;
    public readonly uint Weight;
    public readonly bool AlignRight;

    public GridColumnInfo(string label, ColumnType type = ColumnType.Auto, uint weight = 1, bool alignRight = false)
    {
        Label = label;
        Type = type;
        Weight = weight;
        AlignRight = alignRight;
    }

    public static GridColumnInfo Auto(string label, bool alignRight = false)
        => new(label, ColumnType.Auto, 1, alignRight);

    public static GridColumnInfo Fill(string label, uint weight = 1)
        => new(label, ColumnType.Fill, weight, false);

    public static GridColumnInfo Numeric(string label)
        => new(label, ColumnType.Auto, 1, true);


}

internal enum ColumnType
{
    Auto,
    Fill,
}

