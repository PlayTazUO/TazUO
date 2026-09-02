#nullable enable

using ClassicUO.Configuration;
using ClassicUO.Game.UI.MyraWindows.Theme;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

/// <summary>The bordered, fixed-width box a multi-value picker shows its picked rows in, with a grey
/// placeholder while there are none.</summary>
public sealed class PickedItemsBox : VerticalStackPanel
{
    #region Private members

    private const int SPACING = 4;

    private readonly MyraLabel _placeholder;

    #endregion

    #region Ctor

    /// <param name="width">Fixed width, matching whatever picker row sits above this box.</param>
    public PickedItemsBox(int width)
    {
        Spacing = SPACING;
        Width = width;
        Border = new SolidBrush(MyraStyle.GridBorderColor);
        BorderThickness = new Thickness(1);
        Padding = new Thickness(4);

        _placeholder = new MyraLabel(TazLang.Get("pickeditemsbox_empty", "No items"), MyraLabel.TextStyle.P)
        {
            TextColor = MyraTheme.Current.DisabledText
        };

        Widgets.Add(_placeholder);
    }

    #endregion

    #region Public methods

    /// <summary>Adds a picked-item row, hiding the empty-state placeholder.</summary>
    /// <param name="row">The row to add.</param>
    public void AddRow(Widget row)
    {
        Widgets.Add(row);
        RefreshPlaceholder();
    }

    /// <summary>Removes a picked-item row, showing the placeholder again once none are left.</summary>
    /// <param name="row">The row to remove.</param>
    public void RemoveRow(Widget row)
    {
        Widgets.Remove(row);
        RefreshPlaceholder();
    }

    #endregion

    #region Private methods

    /// <summary>The placeholder is hidden, never removed, so it stays counted in <c>Widgets</c>.</summary>
    private void RefreshPlaceholder() => _placeholder.Visible = Widgets.Count == 1;

    #endregion
}
