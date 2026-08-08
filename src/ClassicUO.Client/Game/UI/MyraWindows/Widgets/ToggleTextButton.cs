#nullable enable
using System;
using Myra.Events;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

public class ToggleTextButton : ToggleButton
{
    /// <summary>
    /// Determines whether the button should remain pressed when clicked.
    /// If true, once pressed, the button will remain pressed until manually released via the <see cref="ToggleButton.IsToggled"/>  property.
    /// </summary>
    public bool SpringLoaded { get; set; }

    private readonly Action<ToggleTextButton>? _onClick;

    public ToggleTextButton(string text, Action<ToggleTextButton>? onClick = null)
    {
        _onClick = onClick;
        Margin = new Thickness(2);
        VerticalAlignment = VerticalAlignment.Center;
        Content = new MyraLabel(text, MyraLabel.TextStyle.P);
    }

    public override void OnTouchDown(TouchEventArgs args)
    {
        if (!Enabled)
            return;

        if (SpringLoaded && IsToggled)
            return;

        base.OnTouchDown(args);
        _onClick?.Invoke(this);
    }
}
