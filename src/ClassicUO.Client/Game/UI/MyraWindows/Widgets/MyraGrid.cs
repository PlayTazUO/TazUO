#nullable enable
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

    internal void AddWidget(Widget widget, int row, int col)
    {
        Widgets.Add(widget);
        SetRow(widget, row);
        SetColumn(widget, col);
    }
}
