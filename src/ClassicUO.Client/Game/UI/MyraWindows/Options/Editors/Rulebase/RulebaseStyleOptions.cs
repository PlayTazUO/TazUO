#nullable enable

using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;

namespace ClassicUO.Game.UI.MyraWindows.Options.Editors.Rulebase;

public record struct BorderStyle(IBrush Brush, Thickness Thickness);

public sealed class RulebaseStyleOptions
{
    public bool ShowHeader { get; set; } = true;
    public bool UseStripedRows { get; set; } = true;
    public BorderStyle OuterBorder { get; set; } = new(new SolidBrush(MyraStyle.GridBorderColor), new Thickness(1));
    public BorderStyle ColumnBorders { get; set; } = new(new SolidBrush(MyraStyle.GridBorderColor), new Thickness(0, 0, 1, 0));
    public BorderStyle RowBorders { get; set; } = new(new SolidBrush(MyraStyle.GridBorderColor), new Thickness(0, 0, 0, 1));
    public bool HighlightSelectedRow { get; set; } = true;

    public Color HeaderBackground { get; set; } = new(0, 0, 0, 55);
    public Color OddRowBackground { get; set; } = new(20, 20, 45, 70);
    public Color EvenRowBackground { get; set; } = new(0, 0, 0, 20);
    public Color SelectedRowBackground { get; set; } = new(80, 120, 180, 75);
}
