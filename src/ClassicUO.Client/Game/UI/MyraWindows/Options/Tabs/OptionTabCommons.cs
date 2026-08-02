using System;
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Assets;
using ClassicUO.Common;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.Game.UI.MyraWindows.Widgets.Search;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using FontStashSharp;
using FontStashSharp.RichText;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.WrapPanel;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

/// <summary>
/// Shared UI-building helpers used across option tab implementations: styled layout panels,
/// combo boxes, separators, sliders, and icon buttons
/// </summary>
public static class OptionTabCommons
{
    /// <summary>Creates a vertically-oriented styled <see cref="WrapPanel"/> containing <paramref name="children"/></summary>
    /// <param name="children">The widgets to add to the panel</param>
    /// <returns>A configured vertical <see cref="WrapPanel"/></returns>
    internal static WrapPanel StyledVerticalWrapPanel(params Widget[] children) => StyledWrapPanel(Orientation.Vertical, children);

    /// <summary>Creates a horizontally-oriented styled <see cref="WrapPanel"/> containing <paramref name="children"/></summary>
    /// <param name="children">The widgets to add to the panel</param>
    /// <returns>A configured horizontal <see cref="WrapPanel"/></returns>
    internal static WrapPanel StyledHorizontalWrapPanel(params Widget[] children) => StyledWrapPanel(Orientation.Horizontal, children);

    /// <summary>Creates a styled <see cref="WrapPanel"/> with standard spacing and margins</summary>
    /// <param name="orientation">The panel's layout orientation</param>
    /// <param name="children">The widgets to add to the panel; <see langword="null"/> entries are skipped</param>
    /// <returns>A configured <see cref="WrapPanel"/></returns>
    internal static WrapPanel StyledWrapPanel(Orientation orientation, params Widget[] children)
    {
        var panel = new WrapPanel
        {
            Orientation = orientation,
            UniformSizing = false,
            Aligned = false,
            VerticalSpacing = MyraStyle.STANDARD_SPACING,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(MyraStyle.STANDARD_SPACING, 10, MyraStyle.STANDARD_SPACING, 10)
        };

        if (children?.Length > 0)
            panel.AddRange(children.Where(c => c != null).ToArray());

        return panel;
    }

    /// <summary>Creates a styled <see cref="StackPanel"/> with standard spacing</summary>
    /// <param name="orientation">The panel's layout orientation</param>
    /// <param name="children">The widgets to add; <see langword="null"/> entries are skipped</param>
    /// <returns>A configured <see cref="StackPanel"/></returns>
    internal static StackPanel StyledStackPanel(Orientation orientation, params Widget[] children)
    {
        StackPanel panel;
        if (orientation == Orientation.Horizontal)
            panel = new HorizontalStackPanel();
        else
            panel = new VerticalStackPanel();

        panel.Spacing = MyraStyle.STANDARD_SPACING;
        panel.VerticalAlignment = VerticalAlignment.Center;
        children?.ForEach(child =>
        {
            if (child != null)
                panel.Widgets.Add(child);
        });
        return panel;
    }

    /// <summary>
    /// Creates an <see cref="OptionItem"/> containing a font-selector combo box bound to a
    /// <see cref="string"/> font-name property. Populates the combo with names from
    /// <see cref="TrueTypeLoader"/>
    /// </summary>
    /// <param name="label">The label displayed beside the combo box</param>
    /// <param name="backingProp">Accessor for the underlying font name value</param>
    /// <param name="onAfterUpdate">Optional action invoked after the new font name is persisted</param>
    /// <returns>An <see cref="OptionItem"/> wrapping the font-selector widget</returns>
    internal static Widget StyledFontSelector(
        string label,
        Accessor<string> backingProp,
        Action<string> onAfterUpdate = null
    )
    {
        Action<string> callback;
        if (onAfterUpdate != null)
            callback = newValue =>
            {
                backingProp.Set(newValue);
                onAfterUpdate(newValue);
            };
        else
            callback = backingProp.Set;

        var combo = new ContainsLevenshteinComboBox(backingProp.Get(), TrueTypeLoader.Instance.GetSortedFontNames().Names, callback);

        if (string.IsNullOrWhiteSpace(label))
            return combo;

        return new MyraLabel(label, MyraLabel.TextStyle.P).PlaceBefore(combo);
    }

    /// <summary>Creates a thin horizontal separator widget styled for use between option sections</summary>
    /// <returns>A styled <see cref="HorizontalSeparator"/></returns>
    internal static Widget StyledHorizontalSeparator() =>
        new HorizontalSeparator { Thickness = 2, Color = new Color(0, 0, 0, 75), BorderThickness = StyleConstantsDefaults.BorderThickness };

    /// <summary>Creates a thin vertical separator widget styled for use between side-by-side option groups</summary>
    /// <returns>A styled <see cref="VerticalSeparator"/></returns>
    internal static Widget StyledVerticalSeparator() =>
        new VerticalSeparator() { Thickness = 2, Color = new Color(0, 0, 0, 75), BorderThickness = StyleConstantsDefaults.BorderThickness };

    /// <summary>
    /// Creates a labeled combo box widget for any equatable value type, mapping items by value
    /// identity rather than by index. Duplicate option values are silently ignored.
    /// </summary>
    /// <typeparam name="TValue">The option value type; must implement <see cref="IEquatable{T}"/></typeparam>
    /// <param name="label">Optional label text; when non-empty it is placed to the left of the combo box</param>
    /// <param name="value">The initially selected value</param>
    /// <param name="options">The available options, in display order</param>
    /// <param name="onChange">Callback invoked with the newly selected value</param>
    /// <param name="tooltip">Optional tooltip text on the combo box</param>
    /// <returns>A widget containing the combo box and optional label</returns>
    internal static Widget CreateOptionsComboBox<TValue>(
        string label,
        TValue value,
        IEnumerable<TValue> options,
        Action<TValue> onChange,
        string tooltip = null
    ) where TValue : IEquatable<TValue>
    {
        var comboView = new ComboView
        {
            MinWidth = 200,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        if (tooltip != null)
            comboView.Tooltip = tooltip;

        Dictionary<int, TValue> indexToValue = new();
        Dictionary<TValue, int> valueToIndex = new();

        TValue[] optionsArray = options.ToArray();

        int i = 0;
        foreach (TValue option in optionsArray)
        {
            if (!valueToIndex.TryAdd(option, i))
            {
                // Duplicate, ignore
                Log.WarnDebug($"A duplicate option {option.ToString()} encountered. Ignoring");
                continue;
            }

            indexToValue.Add(i, option);
            comboView.ListView.Widgets.Add(new Label { Text = option.ToString(), Tag = i });
            i++;
        }

        int selectedIndex = valueToIndex.GetValueOrDefault(value, -1);

        comboView.ListView.SelectedIndex = selectedIndex;
        comboView.ListView.SelectedIndexChanged += (_, _) =>
        {
            if (comboView.ListView.SelectedIndex.HasValue)
                onChange(indexToValue[comboView.ListView.SelectedIndex.Value]);
        };

        if (string.IsNullOrWhiteSpace(label))
            return comboView;

        return new MyraLabel(label, MyraLabel.TextStyle.P).PlaceBefore(comboView);
    }

    /// <summary>
    /// Creates a <see cref="Grid"/> that places <paramref name="left"/> widgets on the left,
    /// a fill spacer in the middle, and <paramref name="right"/> widgets on the right —
    /// the standard "space-between" toolbar layout
    /// </summary>
    /// <param name="left">Widgets anchored to the left edge</param>
    /// <param name="right">Widgets anchored to the right edge</param>
    /// <returns>A horizontally-stretched grid with left, fill, and right columns</returns>
    internal static Grid StyledHorizontalSpaceBetween(Widget[] left, Widget[] right)
    {
        var grid = new MyraGrid { HorizontalAlignment = HorizontalAlignment.Stretch };

        if (left?.Length > 0)
        {
            grid.AddColumn(Proportion.Auto, (uint)left.Length);
            for (int i = 0; i < left.Length; i++)
                grid.AddWidget(left[i], 0, i);
        }

        grid.AddColumn(Proportion.Fill);

        if (right?.Length > 0)
        {
            grid.AddColumn(Proportion.Auto, (uint)right.Length);
            for (int i = 0; i < right.Length; i++)
                grid.AddWidget(right[i], 0, i + (left?.Length ?? 0) + 1);
        }

        return grid;
    }

    /// <summary>Creates a standard styled <see cref="MyraButton"/></summary>
    /// <param name="label">The button label text</param>
    /// <param name="onClick">Action invoked when the button is clicked</param>
    /// <returns>A configured <see cref="MyraButton"/></returns>
    internal static MyraButton StyledButton(string label, Action onClick) => new(label, onClick);

    /// <summary>
    /// Creates a square <see cref="BasicButton"/> whose label is a Unicode symbol rendered with
    /// a specific font. Pixel offsets allow fine-tuning symbol alignment within the button bounds,
    /// which is necessary because different Unicode glyphs have inconsistent baseline positions.
    /// </summary>
    /// <param name="text">The Unicode symbol to display</param>
    /// <param name="font">The font used to render <paramref name="text"/></param>
    /// <param name="onClick">Action invoked when the button is clicked</param>
    /// <param name="tooltip">Optional tooltip text</param>
    /// <param name="width">Button width in pixels</param>
    /// <param name="height">Button height in pixels</param>
    /// <param name="topOffset">Optional vertical pixel nudge for the label within the button</param>
    /// <param name="leftOffset">Optional horizontal pixel nudge for the label within the button</param>
    /// <returns>A configured <see cref="BasicButton"/></returns>
    internal static BasicButton StyledTextIconButton(
        string text,
        SpriteFontBase font,
        Action onClick,
        string tooltip = null,
        int width = StyleConstantsDefaults.TOOLBAR_BUTTON_SIZE,
        int height = StyleConstantsDefaults.TOOLBAR_BUTTON_SIZE,
        int? topOffset = null,
        int? leftOffset = null
    )
    {
        var label = new Label
        {
            Text = text,
            Font = font,
            Wrap = false,
            SingleLine = true,
            TextAlign = TextHorizontalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Width = width,
            Height = height,
            Padding = new Thickness(0),
            Margin = new Thickness(0),
            Top = topOffset ?? 0,
            Left = leftOffset ?? 0
        };

        var button = new BasicButton(onClick)
        {
            Width = width,
            Height = height,
            Tooltip = tooltip,
            Content = label,
            VerticalAlignment = VerticalAlignment.Center,
            Padding = new Thickness(0),
            Margin = new Thickness(0)
        };

        return button;
    }
}
