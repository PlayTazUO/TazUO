using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.Controls;

public class MyraControl : Control
{
    private Desktop _desktop = new();

    private bool isFocused
    {
        get;
        set
        {
            field = value;
            if (value)
            {
                BringOnTop();
            }
        }
    }

    public MyraControl()
    {
        CanMove = true;
        X = 300;
        Y = 300;

        var grid = new Grid
        {
            RowSpacing = 8,
            ColumnSpacing = 8
        };

        grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        grid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        grid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        grid.Background = new SolidBrush(new Color(0, 0, 0, 0.75f));

        var helloWorld = new Myra.Graphics2D.UI.Label
        {
            Id = "label",
            Text = "Hello, World!"
        };
        grid.Widgets.Add(helloWorld);

// ComboBox
        var combo = new ComboView();
        Grid.SetColumn(combo, 1);
        Grid.SetRow(combo, 0);

        combo.Widgets.Add(new Myra.Graphics2D.UI.Label{Text = "Red", TextColor = Color.Red});
        combo.Widgets.Add(new Myra.Graphics2D.UI.Label{Text = "Green", TextColor = Color.Green});
        combo.Widgets.Add(new Myra.Graphics2D.UI.Label{Text = "Blue", TextColor = Color.Blue});

        grid.Widgets.Add(combo);

// Button
        var button = new Myra.Graphics2D.UI.Button
        {
            Content = new Myra.Graphics2D.UI.Label
            {
                Text = "Show"
            }
        };
        Grid.SetColumn(button, 0);
        Grid.SetRow(button, 1);

        button.Click += (s, a) =>
        {
            var messageBox = Dialog.CreateMessageBox("Message", "Some message!");
            messageBox.ShowModal(_desktop);
        };

        grid.Widgets.Add(button);

// Spin button
        var spinButton = new SpinButton
        {
            Width = 100,
            Nullable = true
        };
        grid.Widgets.Add(spinButton);
        Grid.SetColumn(spinButton, 1);
        Grid.SetRow(spinButton, 1);

        grid.Width = 300;
        grid.Height = 100;

        var w = new Window { Title = "Myra Control", Content = grid };
        w.Left = ScreenCoordinateX;
        w.Top = ScreenCoordinateY;

        w.Closed += (s, a) => Dispose();
        w.TouchDown += (s, a) => isFocused = true;
        _desktop.Root = w;
    }

    public override bool Draw(UltimaBatcher2D batcher, int x, int y)
    {
        if (!base.Draw(batcher, x, y)) return false;

        _desktop.Render();
        return true;
    }
}
