using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows;

public class TestWindow : MyraControl
{
    public TestWindow() : base("Test Window")
    {
        _rootWindow.Left = 300;
        _rootWindow.Top = 300;

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
        _rootWindow.Content = grid;
    }
}
