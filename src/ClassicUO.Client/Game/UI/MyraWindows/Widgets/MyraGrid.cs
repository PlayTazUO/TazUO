#nullable enable
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

    internal void SetupWithHeaders(params GridColumnInfo[] columns)
    {
        MyraStyle.ApplyStandardGridStyling(this);

        foreach (GridColumnInfo col in columns)
        {
            AddColumn(col.Type == ColumnType.Fill
                ? new Proportion(ProportionType.Part, col.Weight)
                : new Proportion(ProportionType.Auto));
        }

        for (int i = 0; i < columns.Length; i++)
        {
            GridColumnInfo col = columns[i];
            AddWidget(new MyraLabel(col.Label, MyraLabel.TextStyle.TableHeader,
                col.AlignRight ? MyraLabel.AlignMode.Right : MyraLabel.AlignMode.Left), 0, i);
        }
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

