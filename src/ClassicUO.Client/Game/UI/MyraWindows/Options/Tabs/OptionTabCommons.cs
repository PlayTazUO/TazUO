using System;
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Assets;
using ClassicUO.Common;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using FontStashSharp;
using FontStashSharp.RichText;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.WrapPanel;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

public static class OptionTabCommons
{
    internal static WrapPanel StyledVerticalWrapPanel(params Widget[] children) => StyledWrapPanel(Orientation.Vertical, children);

    internal static WrapPanel StyledHorizontalWrapPanel(params Widget[] children) => StyledWrapPanel(Orientation.Horizontal, children);

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

    internal static OptionItem StyledFontSelector(
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

        return OptionsFactory.CreateComboBox(label, backingProp.Get(), TrueTypeLoader.Instance.GetSortedFontNames().Names, callback);
    }

    internal static Widget StyledHorizontalSeparator() =>
        new HorizontalSeparator { Thickness = 2, Color = new Color(0, 0, 0, 75), BorderThickness = StyleConstantsDefaults.BorderThickness };

    internal static Widget StyledVerticalSeparator() =>
        new VerticalSeparator() { Thickness = 2, Color = new Color(0, 0, 0, 75), BorderThickness = StyleConstantsDefaults.BorderThickness };


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

    internal static MyraButton StyledButton(string label, Action onClick) => new(label, onClick);

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
