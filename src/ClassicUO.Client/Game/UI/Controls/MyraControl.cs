using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Myra;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.Controls;

public class MyraControl : Control
{
    private Desktop _desktop = new();

    public MyraControl()
    {
        var grid = new GridLayout
        {
            ColumnSpacing = 8,
            RowSpacing = 8,
        };
        grid.ColumnsProportions.Add(new Proportion());
        grid.ColumnsProportions.Add(new Proportion());
        grid.RowsProportions.Add(new Proportion());
        grid.RowsProportions.Add(new Proportion());



        var widget = new Myra.Graphics2D.UI.Widget();

        var helloWorld = new Myra.Graphics2D.UI.Label
        {
            Id = "label",
            Text = "Hello, World!"
        };
        widget.

        var button = new Myra.Graphics2D.UI.Button
        {
            Content = new Myra.Graphics2D.UI.Label
            {
                Text = "Show"
            }
        };

        button.Click += (s, a) =>
        {
            var messageBox = Dialog.CreateMessageBox("Message", "Some message!");
            messageBox.ShowModal(_desktop);
        };

        _desktop.Root = grid;
    }

    public override bool Draw(UltimaBatcher2D batcher, int x, int y)
    {
        return base.Draw(batcher, x, y);
    }
}
