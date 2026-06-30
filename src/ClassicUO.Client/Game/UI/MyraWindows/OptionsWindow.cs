#nullable enable
using System;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.WrapPanel;

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

    /// <summary>The window title, also used to match existing instances in <see cref="GetExisting"/>.</summary>
    public string Title { get; }

    /// <param name="title">The window title shown in the title bar.</param>
    public OptionsWindow(string title) : base(title)
    {
        Title = title;
        _stack = new VerticalStackPanel { Spacing = MyraStyle.STANDARD_SPACING, Padding = new Thickness(8) };

        SetRootContent(_stack);
        CenterInScreen();
        UIManager.Add(this);
        BringOnTop();
    }

    /// <summary>
    /// Returns an existing, non-disposed <see cref="OptionsWindow"/> with the given <paramref name="title"/>,
    /// or null if none is open. Useful to focus an existing window instead of opening a duplicate.
    /// </summary>
    public static OptionsWindow? GetExisting(string title)
    {
        foreach (IGui g in UIManager.Gumps)
        {
            if (g is OptionsWindow ow && !ow.IsDisposed && ow.Title == title)
                return ow;
        }

        return null;
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
        WrapPanel row = MyraInputBox.LabeledHorizontalStackPanel(label, out MyraInputBox input, width, value, tooltip: tooltip);

        if (inputFilter != null)
            input.InputFilter = inputFilter;

        input.TextChangedByUser += (_, _) => onChange(input.Text ?? string.Empty);

        _stack.Widgets.Add(row);
        Refresh();
        return this;
    }

    /// <summary>Appends a labeled dropdown that invokes <paramref name="onChange"/> when the selection changes.</summary>
    public OptionsWindow AddDropdown(string label, string[] items, int selectedIndex, Action<int> onChange, string? tooltip = null)
    {
        if (items == null || items.Length == 0)
            throw new ArgumentException("Items array cannot be null or empty.", nameof(items));

        var row = new HorizontalStackPanel { Spacing = MyraStyle.STANDARD_SPACING, VerticalAlignment = VerticalAlignment.Center };
        row.Widgets.Add(new MyraLabel(label, MyraLabel.TextStyle.P));

#pragma warning disable CS0612, CS0618
        var combo = new ComboBox { VerticalAlignment = VerticalAlignment.Center };

        if (tooltip != null)
            combo.Tooltip = tooltip;

        foreach (string item in items)
            combo.Items.Add(new ListItem(item));

        if (selectedIndex >= 0 && selectedIndex < items.Length)
            combo.SelectedIndex = selectedIndex;

        combo.SelectedIndexChanged += (_, _) =>
        {
            if (combo.SelectedIndex.HasValue)
                onChange(combo.SelectedIndex.Value);
        };
#pragma warning restore CS0612, CS0618

        row.Widgets.Add(combo);
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
