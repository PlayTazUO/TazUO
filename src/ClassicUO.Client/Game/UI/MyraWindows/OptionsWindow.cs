#nullable enable
using System;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows;

/// <summary>
/// A reusable, lightweight options window for quick settings that don't warrant a dedicated window.
/// Build it up fluently with <see cref="AddLabel"/>, <see cref="AddCheckbox"/> and <see cref="AddInput"/>,
/// each of which appends the appropriate control (wired to a callback) to a vertical stack.
/// The window is shown automatically once constructed.
/// </summary>
public class OptionsWindow : MyraControl
{
    private readonly VerticalStackPanel _stack;

    /// <param name="title">The window title shown in the title bar.</param>
    public OptionsWindow(string title) : base(title)
    {
        _stack = new VerticalStackPanel { Spacing = MyraStyle.STANDARD_SPACING, Padding = new Thickness(8) };

        SetRootContent(_stack);
        CenterInViewPort();
        UIManager.Add(this);
        BringOnTop();
    }

    /// <summary>Appends a plain text label.</summary>
    public OptionsWindow AddLabel(string text, MyraLabel.TextStyle style = MyraLabel.TextStyle.P)
    {
        _stack.Widgets.Add(new MyraLabel(text, style));
        Refresh();
        return this;
    }

    /// <summary>Appends a labeled checkbox that invokes <paramref name="onChange"/> when toggled.</summary>
    public OptionsWindow AddCheckbox(string label, bool isChecked, Action<bool> onChange, string? tooltip = null)
    {
        _stack.Widgets.Add(MyraCheckButton.CreateWithCallback(isChecked, onChange, label, tooltip));
        Refresh();
        return this;
    }

    /// <summary>Appends a labeled text input that invokes <paramref name="onChange"/> as the user types.</summary>
    public OptionsWindow AddInput(
        string label,
        string value,
        Action<string> onChange,
        int width = 200,
        string? tooltip = null,
        Func<char, bool>? inputFilter = null
    )
    {
        var row = MyraInputBox.WithLabel(label, out MyraInputBox input, width, value, tooltip: tooltip);

        if (inputFilter != null)
            input.InputFilter = inputFilter;

        input.TextChangedByUser += (_, _) => onChange(input.Text ?? string.Empty);

        _stack.Widgets.Add(row);
        Refresh();
        return this;
    }

    private void Refresh()
    {
        ForceSizeUpdate();
        CenterInViewPort();
    }
}
