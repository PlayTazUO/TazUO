#nullable enable

using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

/// <summary>
/// A row pinning its trailing widget to the right edge whatever the leading one's width. Stretches to
/// its parent for the width to push against, so it needs a parent that gives it one.
/// </summary>
public class SpaceBetweenRow : HorizontalStackPanel
{
    #region Ctor

    /// <summary>Pairs the leading and trailing widgets into one row.</summary>
    /// <param name="leading">The widget that takes up the remaining space.</param>
    /// <param name="trailing">The widget pinned to the right edge.</param>
    /// <param name="spacing">Gap kept between them when the leading widget doesn't fill it all.</param>
    public SpaceBetweenRow(Widget leading, Widget trailing, int spacing = 4)
    {
        Spacing = spacing;
        HorizontalAlignment = HorizontalAlignment.Stretch;

        SetProportionType(leading, ProportionType.Fill);

        Widgets.Add(leading);
        Widgets.Add(trailing);
    }

    #endregion
}
