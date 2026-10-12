#nullable enable
using System;
using System.Collections.Generic;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

public class MyraTabControl : TabControl
{
    private readonly List<Func<Widget>> _builders = new();
    private readonly List<bool> _rebuildOnSelect = new();

    public MyraTabControl()
    {
        SelectedIndexChanged += OnTabSelected;
    }

    /// <param name="rebuildOnSelect">
    ///     Rebuild the page every time the tab is selected instead of caching it. Use for pages that
    ///     mirror state changed elsewhere (e.g. agent lists the game can edit), so revisiting the tab
    ///     shows what is current. Avoid where the page holds unsaved input state.
    /// </param>
    public void AddTab(string name, Func<Widget> builder, string? tooltip = null, bool rebuildOnSelect = false)
    {
        int index = _builders.Count;
        _builders.Add(builder);
        _rebuildOnSelect.Add(rebuildOnSelect);

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

    /// <summary>Rebuilds the selected page while preserving its parent tabs and their selections.</summary>
    internal void RefreshSelectedContent()
    {
        if (SelectedItem?.Content is MyraTabControl nestedTabs)
        {
            nestedTabs.RefreshSelectedContent();
        }
        else if (SelectedItem?.Tag is int index && index >= 0 && index < _builders.Count)
        {
            SelectedItem.Content = _builders[index]();
        }
    }

    private void OnTabSelected(object? sender, EventArgs e)
    {
        if (SelectedItem == null) return;

        if (SelectedItem.Tag is int idx && idx < _builders.Count)
        {
            if (SelectedItem.Content != null && !_rebuildOnSelect[idx]) return;

            SelectedItem.Content = _builders[idx]();
        }
    }
}
