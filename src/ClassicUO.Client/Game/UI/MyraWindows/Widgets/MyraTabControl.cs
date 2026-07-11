#nullable enable
using System;
using System.Collections.Generic;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

public class MyraTabControl : TabControl
{
    private readonly List<Func<Widget>> _builders = new();

    public MyraTabControl()
    {
        SelectedIndexChanged += OnTabSelected;
    }

    public void AddTab(string name, Func<Widget> builder, string? tooltip = null)
    {
        int index = _builders.Count;
        _builders.Add(builder);

        var item = new TabItem(name) { Tag = index };
        Items.Add(item);

        if (!string.IsNullOrEmpty(tooltip))
            SetHeaderTooltip(item, tooltip!);
    }

    /// <summary>
    /// Applies a hover tooltip to a tab's header button. Myra's <see cref="TabItem"/> does not
    /// expose its header button, so this reaches it through the control's own visual tree: the
    /// header buttons live in the first <see cref="Grid"/> under <c>InternalChild</c>, each tagged
    /// with its owning <see cref="TabItem"/>. If Myra's internals ever change, this degrades to
    /// simply not showing a tooltip rather than throwing.
    /// </summary>
    private void SetHeaderTooltip(TabItem item, string tooltip)
    {
        Grid? buttonsGrid = null;
        foreach (Widget w in InternalChild.Widgets)
        {
            if (w is Grid grid)
            {
                buttonsGrid = grid;
                break;
            }
        }

        if (buttonsGrid == null)
            return;

        foreach (Widget button in buttonsGrid.Widgets)
        {
            if (ReferenceEquals(button.Tag, item))
            {
                button.Tooltip = tooltip;
                return;
            }
        }
    }

    public void SelectFirst()
    {
        if (Items.Count > 0)
            SelectedIndex = 0;
    }

    private void OnTabSelected(object? sender, EventArgs e)
    {
        if (SelectedItem == null || SelectedItem.Content != null) return;

        if (SelectedItem.Tag is int idx && idx < _builders.Count)
        {
            SelectedItem.Content = _builders[idx]();
        }
    }
}
