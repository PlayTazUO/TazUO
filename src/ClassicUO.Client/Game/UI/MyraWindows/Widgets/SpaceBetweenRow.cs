#nullable enable

using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

/// <summary>
/// A horizontal row that pins its trailing widget to the right edge, whatever the leading one's own
/// width - a label beside a remove glyph, a value beside a clear button. Needs a real width to push
/// against: stretches itself to fill its parent, so it does nothing useful inside an unstretched
/// container (a plain <see cref="VerticalStackPanel" /> child, say) unless that parent gives it one.
/// </summary>
public class SpaceBetweenRow : HorizontalStackPanel
{
    #region Ctor

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
