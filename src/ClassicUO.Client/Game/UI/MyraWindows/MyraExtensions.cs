using System.Collections.Generic;
using FontStashSharp;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.WrapPanel;

namespace ClassicUO.Game.UI.MyraWindows;

public static class MyraExtensions
{
    public static HorizontalStackPanel PlaceBefore(this Widget widget, Widget rightSide)
    {
        var panel = new HorizontalStackPanel() { Spacing = MyraStyle.STANDARD_SPACING, VerticalAlignment = VerticalAlignment.Center};
        panel.Widgets.Add(widget);
        panel.Widgets.Add(rightSide);
        return panel;
    }

    public static HorizontalStackPanel PlaceBefore(this Widget widget, Widget[] rightSide)
    {
        var panel = new HorizontalStackPanel() { Spacing = MyraStyle.STANDARD_SPACING, VerticalAlignment = VerticalAlignment.Center };
        panel.Widgets.Add(widget);
        foreach (Widget w in rightSide)
            panel.Widgets.Add(w);
        return panel;
    }

    public static ScrollViewer WrapInScroll(this Widget widget, int? maxHeight = null)
    {
        var scroll = new ScrollViewer();
        scroll.Content = widget;

        if(maxHeight != null)
            scroll.MaxHeight = maxHeight.Value;

        return scroll;
    }

    public static WrapPanel AddRange(this WrapPanel panel, params Widget[] children)
    {
        foreach (Widget child in children)
            panel.Widgets.Add(child);
        return panel;
    }

    /// <summary>
    /// Walks the widget tree beneath <paramref name="root"/>, breadth first.
    /// <para>
    /// Myra's composites do not share one base class, so each has to be handled: <see cref="Container"/>
    /// holds children, <see cref="ContentControl"/> (a <see cref="ScrollViewer"/>, an option item) holds
    /// a single <c>Content</c>, and <see cref="TabControl"/> keeps per-tab content on its items. Both
    /// of the latter derive from <see cref="Widget"/> rather than <see cref="Container"/>, so missing
    /// either silently truncates the walk at the first scroller or tab.
    /// </para>
    /// </summary>
    /// <param name="root">The widget to walk beneath; not itself yielded</param>
    /// <returns>Every reachable descendant</returns>
    public static IEnumerable<Widget> Descendants(this Widget root)
    {
        Queue<Widget> pending = new();
        pending.Enqueue(root);

        while (pending.Count > 0)
        {
            Widget current = pending.Dequeue();

            if (!ReferenceEquals(current, root))
                yield return current;

            foreach (Widget child in current.ChildWidgets())
                pending.Enqueue(child);
        }
    }

    /// <summary>
    /// Finds the nearest descendant of type <typeparamref name="T"/>, or <paramref name="root"/>
    /// itself when it already is one.
    /// </summary>
    /// <typeparam name="T">The widget type to look for</typeparam>
    /// <param name="root">The widget to search from</param>
    /// <returns>The match, or <see langword="null"/> when there is none</returns>
    public static T FindSelfOrDescendant<T>(this Widget root) where T : Widget
    {
        if (root is T self)
            return self;

        foreach (Widget descendant in root.Descendants())
        {
            if (descendant is T match)
                return match;
        }

        return null;
    }

    /// <summary>
    /// The widgets directly beneath <paramref name="widget"/>.
    /// <para>
    /// Myra's composites share no common base, so each shape has to be handled: a
    /// <see cref="Container"/> holds children, a <see cref="ContentControl"/> (a
    /// <see cref="ScrollViewer"/>, an option item) holds a single <c>Content</c>, and a
    /// <see cref="TabControl"/> keeps per-tab content on its items. The latter two derive from
    /// <see cref="Widget"/> rather than <see cref="Container"/>, so missing either silently
    /// truncates any walk at the first scroller or tab.
    /// </para>
    /// </summary>
    /// <param name="widget">The widget whose children are wanted</param>
    /// <returns>Its immediate children; empty for a leaf</returns>
    public static IEnumerable<Widget> ChildWidgets(this Widget widget)
    {
        switch (widget)
        {
            case Container container:
                foreach (Widget child in container.Widgets)
                    yield return child;

                break;

            case ContentControl content when content.Content != null:
                yield return content.Content;

                break;
        }

        if (widget is not TabControl tabs)
            yield break;

        // An unbuilt tab has no content yet and simply contributes nothing.
        foreach (TabItem item in tabs.Items)
        {
            if (item.Content != null)
                yield return item.Content;
        }
    }

    /// <summary>
    /// Re-sizes the body text inside an already-built widget tree, for a context that wants the
    /// same controls drawn smaller - a dense results list, say.
    /// <para>
    /// Only labels currently drawn in the UI font are touched, identified by font instance - a
    /// glyph label uses the symbol font, and a heading drawn at its own size uses that size, so
    /// retargeting either would blank the glyph or flatten a deliberate hierarchy.
    /// </para>
    /// <para>
    /// A heading that happens to be body-sized is indistinguishable from body text by this test -
    /// the font cache hands out one instance per name and size - and is re-sized along with it.
    /// Intentional: such a heading reads as body text to the user too, and leaving it at full size
    /// inside a shrunk tree is what would look wrong.
    /// </para>
    /// </summary>
    /// <param name="root">The widget whose labels should be re-sized; itself included</param>
    /// <param name="offset">Size offset from the user's configured UI font size</param>
    /// <returns><paramref name="root"/>, for chaining</returns>
    public static Widget ApplyUiFontOffset(this Widget root, int offset)
    {
        if (offset == 0)
            return root;

        SpriteFontBase body = MyraStyle.UiFont;
        SpriteFontBase scaled = MyraStyle.GetUiFont(offset);

        if (ReferenceEquals(body, scaled))
            return root;

        if (root is Label self && ReferenceEquals(self.Font, body))
            self.Font = scaled;

        foreach (Widget descendant in root.Descendants())
        {
            if (descendant is Label label && ReferenceEquals(label.Font, body))
                label.Font = scaled;
        }

        return root;
    }
}
