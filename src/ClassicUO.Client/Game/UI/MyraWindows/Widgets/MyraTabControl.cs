#nullable enable
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

public class MyraTabControl : TabControl
{
    private readonly List<Func<Widget>> _builders = new();

    public MyraTabControl()
    {
        SelectedIndexChanged += OnTabSelected;
    }

    public void AddTab(string name, Func<Widget> builder)
    {
        int index = _builders.Count;
        _builders.Add(builder);
        Items.Add(new TabItem(name) { Tag = index });
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
