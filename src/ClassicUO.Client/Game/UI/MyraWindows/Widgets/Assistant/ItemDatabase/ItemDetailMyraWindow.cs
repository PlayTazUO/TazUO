#nullable enable
using System;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Managers.Structs;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Assistant.ItemDatabase;

public class ItemDetailMyraWindow : MyraControl
{
    private readonly ItemInfo _item;

    public ItemDetailMyraWindow(ItemInfo item) : base($"Item Details — {item.Name}")
    {
        _item = item;

        var layout = new VerticalStackPanel { Spacing = 8 };
        layout.Widgets.Add(BuildGraphicSection());
        layout.Widgets.Add(BuildBasicInfoSection());
        layout.Widgets.Add(BuildLocationSection());
        layout.Widgets.Add(BuildPropertiesSection());
        layout.Widgets.Add(BuildActionsSection());

        SetRootContent(new ScrollViewer { MaxHeight = 600, Content = layout });
        CenterInViewPort();
        UIManager.Add(this);
        BringOnTop();
    }

    private Widget BuildGraphicSection()
    {
        var row = new HorizontalStackPanel { Spacing = 8 };

        if (_item.Graphic > 0)
            row.Widgets.Add(new MyraArtTexture(_item.Graphic, 64)
                { Tooltip = $"Graphic: {_item.Graphic} (0x{_item.Graphic:X4})" });

        var infoCol = new VerticalStackPanel { Spacing = 2 };
        infoCol.Widgets.Add(new MyraLabel($"Graphic ID: {_item.Graphic} (0x{_item.Graphic:X4})", MyraLabel.TextStyle.P));
        infoCol.Widgets.Add(_item.Hue > 0
            ? new MyraLabel($"Hue: {_item.Hue} (0x{_item.Hue:X4})", MyraLabel.TextStyle.P)
            : new MyraLabel("Hue: Default", MyraLabel.TextStyle.P));
        row.Widgets.Add(infoCol);
        return row;
    }

    private Widget BuildBasicInfoSection()
    {
        var panel = new VerticalStackPanel { Spacing = 2 };
        panel.Widgets.Add(new MyraLabel("Basic Information", MyraLabel.TextStyle.H3));

        if (_item.CustomName.NotNullNotEmpty())
            panel.Widgets.Add(new MyraLabel($"Custom Name: {_item.CustomName}", MyraLabel.TextStyle.P));

        panel.Widgets.Add(new MyraLabel($"Name: {_item.Name} (0x{_item.Serial:X8})", MyraLabel.TextStyle.P));
        panel.Widgets.Add(new MyraLabel($"Layer: {_item.Layer} ({(int)_item.Layer})", MyraLabel.TextStyle.P));

        TimeSpan timeAgo = DateTime.Now - _item.UpdatedTime;
        string timeText = timeAgo.TotalDays >= 1    ? $"{timeAgo.Days}d ago"
            : timeAgo.TotalHours >= 1               ? $"{timeAgo.Hours}h ago"
            : timeAgo.TotalMinutes >= 1             ? $"{(int)timeAgo.TotalMinutes}m ago"
            : "Just now";
        panel.Widgets.Add(new MyraLabel($"Last seen: {timeText}", MyraLabel.TextStyle.P));

        string charServer = _item.CharacterName;
        if (!string.IsNullOrEmpty(_item.ServerName))
            charServer += $" (Server: {_item.ServerName})";
        panel.Widgets.Add(new MyraLabel($"Character: {charServer}", MyraLabel.TextStyle.P));

        return panel;
    }

    private Widget BuildLocationSection()
    {
        var panel = new VerticalStackPanel { Spacing = 2 };
        panel.Widgets.Add(new MyraLabel("Location", MyraLabel.TextStyle.H3));

        if (_item.OnGround)
        {
            panel.Widgets.Add(new MyraLabel($"On ground at {_item.X}, {_item.Y}", MyraLabel.TextStyle.P));
        }
        else
        {
            panel.Widgets.Add(new MyraLabel("In container", MyraLabel.TextStyle.P));
            if (_item.Container != 0)
            {
                panel.Widgets.Add(new MyraLabel($"Container: 0x{_item.Container:X8}", MyraLabel.TextStyle.P));

                Item? containerItem = Client.Game.UO?.World?.Items?.Get(_item.Container);
                if (containerItem != null &&
                    containerItem.RootContainer != 0 &&
                    containerItem.RootContainer != _item.Container)
                    panel.Widgets.Add(new MyraLabel($"Root Container: 0x{containerItem.RootContainer:X8}", MyraLabel.TextStyle.P));
            }
        }

        return panel;
    }

    private Widget BuildPropertiesSection()
    {
        var panel = new VerticalStackPanel { Spacing = 2 };
        panel.Widgets.Add(new MyraLabel("Properties", MyraLabel.TextStyle.H3));

        if (!string.IsNullOrEmpty(_item.Properties))
        {
            foreach (string prop in _item.Properties.Split('|'))
                if (!string.IsNullOrWhiteSpace(prop))
                    panel.Widgets.Add(new MyraLabel($"• {prop.Trim()}", MyraLabel.TextStyle.P));
        }
        else
        {
            panel.Widgets.Add(new MyraLabel("No properties available", MyraLabel.TextStyle.P));
        }

        return panel;
    }

    private Widget BuildActionsSection()
    {
        var panel = new VerticalStackPanel { Spacing = 4 };
        panel.Widgets.Add(new MyraLabel("Actions", MyraLabel.TextStyle.H3));

        var row1 = new HorizontalStackPanel { Spacing = 4 };

        // Use Item — only if item exists in world
        Item? worldItem = World.Instance?.Items?.Get(_item.Serial);
        if (worldItem != null && !worldItem.IsDestroyed)
        {
            row1.Widgets.Add(new MyraButton("Use Item", () =>
                GameActions.DoubleClick(World.Instance, _item.Serial))
            { Tooltip = "Double-click the item to use it" });
        }

        // Take Item — only if not already in backpack
        uint backpackSerial = Client.Game.UO?.World?.Player?.Backpack?.Serial ?? 0;
        if (_item.Container != backpackSerial)
        {
            row1.Widgets.Add(new MyraButton("Take Item", MoveToBackpack)
                { Tooltip = "Move the item to your backpack" });
        }

        row1.Widgets.Add(new MyraButton("Try to Locate", TryToLocate)
            { Tooltip = "Create a quest arrow pointing to the item's last known location" });

        row1.Widgets.Add(new MyraButton("Set Custom Name", () =>
        {
            var nameBox = new MyraInputBox { Text = _item.CustomName, Width = 220 };
            new MyraDialog("Set Custom Name", nameBox, ok =>
            {
                if (!ok) return;
                _item.CustomName = nameBox.Text ?? "";
                Item? wi = World.Instance?.Items?.Get(_item.Serial);
                if (wi != null)
                {
                    wi.CustomName = _item.CustomName;
                    ItemDatabaseManager.Instance.AddOrUpdateItem(wi, World.Instance);
                }
            });
        }));

        panel.Widgets.Add(row1);

        var row2 = new HorizontalStackPanel { Spacing = 4 };

        if (!_item.OnGround && _item.Container != 0)
        {
            row2.Widgets.Add(new MyraButton("View Container", () =>
                OpenContainerDetail(_item.Container))
            { Tooltip = "View the container's database entry" });

            Item? cont = Client.Game.UO?.World?.Items?.Get(_item.Container);
            if (cont != null &&
                cont.RootContainer != 0 &&
                cont.RootContainer != _item.Container)
            {
                row2.Widgets.Add(new MyraButton("View Root Container", () =>
                    OpenContainerDetail(cont.RootContainer))
                { Tooltip = "View the root container's database entry" });
            }
        }

        row2.Widgets.Add(new MyraButton("Close", () => _disposeRequested = true));
        panel.Widgets.Add(row2);

        return panel;
    }

    private void MoveToBackpack()
    {
        try
        {
            World? world = Client.Game.UO?.World;
            PlayerMobile? player = world?.Player;
            if (player == null) return;

            Item? item = world?.Items?.Get(_item.Serial);
            if (item == null) { Log.Warn("Cannot move item: not found in world"); return; }

            Item? backpack = world?.Items?.Get(player.Backpack?.Serial ?? 0);
            if (backpack == null) { Log.Warn("Cannot move item: backpack not found"); return; }

            if (backpack.Serial == item.Container) { Log.Info("Item is already in backpack"); return; }

            ObjectActionQueue.Instance.Enqueue(
                new MoveRequest(item.Serial, backpack.Serial).ToObjectActionQueueItem(),
                ActionPriority.MoveItem);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to move item to backpack: {ex.Message}");
        }
    }

    private void TryToLocate()
    {
        try
        {
            World? world = Client.Game.UO?.World;
            if (world?.Player == null) return;

            if (_item.OnGround)
            {
                CreateQuestArrow(_item.X, _item.Y);
                return;
            }

            if (_item.Container == 0) return;

            Item? containerItem = world.Items?.Get(_item.Container);
            if (containerItem != null)
            {
                if (containerItem.RootContainer == world.Player.Serial)
                {
                    CreateQuestArrow(world.Player.X, world.Player.Y);
                }
                else
                {
                    Item? root = world.Items?.Get(containerItem.RootContainer);
                    if (root != null && root.OnGround)
                        CreateQuestArrow(root.X, root.Y);
                    else
                    {
                        Mobile? mob = world.Mobiles?.Get(containerItem.RootContainer);
                        if (mob != null)
                            CreateQuestArrow(mob.X, mob.Y);
                        else
                            SearchDatabaseForLocation(containerItem.RootContainer);
                    }
                }
            }
            else
            {
                SearchDatabaseForLocation(_item.Container);
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to locate item: {ex.Message}");
        }
    }

    private void SearchDatabaseForLocation(uint containerSerial) =>
        ItemDatabaseManager.Instance.SearchItems(
            results =>
            {
                MainThreadQueue.InvokeOnMainThread(() =>
                {
                    if (results is { Count: > 0 })
                    {
                        ItemInfo ci = results[0];
                        if (ci.OnGround)
                            CreateQuestArrow(ci.X, ci.Y);
                        else
                        {
                            World? world = Client.Game.UO?.World;
                            if (world?.Player != null && ci.Container == world.Player.Serial)
                                CreateQuestArrow(world.Player.X, world.Player.Y);
                        }
                    }
                });
            },
            serial: containerSerial,
            limit: 1);

    private void CreateQuestArrow(int x, int y)
    {
        try
        {
            World? world = Client.Game.UO?.World;
            if (world == null) return;

            QuestArrowGump? existing = UIManager.GetGump<QuestArrowGump>(_item.Serial);
            existing?.Dispose();

            var arrow = new QuestArrowGump(world, _item.Serial, x, y)
                { CanCloseWithRightClick = true };
            UIManager.Add(arrow);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to create quest arrow: {ex.Message}");
        }
    }

    private void OpenContainerDetail(uint containerSerial) =>
        ItemDatabaseManager.Instance.SearchItems(
            results =>
            {
                MainThreadQueue.InvokeOnMainThread(() =>
                {
                    if (results is { Count: > 0 })
                        new ItemDetailMyraWindow(results[0]);
                    else
                        Log.Warn($"Container 0x{containerSerial:X8} not found in item database");
                });
            },
            serial: containerSerial,
            limit: 1);
}
