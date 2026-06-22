#nullable enable
using System;
using ClassicUO.Configuration;
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

    public ItemDetailMyraWindow(ItemInfo item) : base(TazLang.Get("item_detail_title_fmt", new[] { item.Name }))
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
                { Tooltip = TazLang.Get("item_detail_tooltip_graphic_fmt", new[] { _item.Graphic.ToString(), _item.Graphic.ToString("X4") }) });

        var infoCol = new VerticalStackPanel { Spacing = 2 };
        infoCol.Widgets.Add(new MyraLabel(TazLang.Get("item_detail_graphic_id_fmt", new[] { _item.Graphic.ToString(), _item.Graphic.ToString("X4") }), MyraLabel.TextStyle.P));
        infoCol.Widgets.Add(_item.Hue > 0
            ? new MyraLabel(TazLang.Get("item_detail_hue_fmt", new[] { _item.Hue.ToString(), _item.Hue.ToString("X4") }), MyraLabel.TextStyle.P)
            : new MyraLabel(TazLang.Get("item_detail_hue_default", "Hue: Default"), MyraLabel.TextStyle.P));
        row.Widgets.Add(infoCol);
        return row;
    }

    private Widget BuildBasicInfoSection()
    {
        var panel = new VerticalStackPanel { Spacing = 2 };
        panel.Widgets.Add(new MyraLabel(TazLang.Get("item_detail_basic_info", "Basic Information"), MyraLabel.TextStyle.H3));

        if (_item.CustomName.NotNullNotEmpty())
            panel.Widgets.Add(new MyraLabel(TazLang.Get("item_detail_custom_name_fmt", new[] { _item.CustomName }), MyraLabel.TextStyle.P));

        panel.Widgets.Add(new MyraLabel(TazLang.Get("item_detail_name_fmt", new[] { _item.Name, _item.Serial.ToString("X8") }), MyraLabel.TextStyle.P));
        panel.Widgets.Add(new MyraLabel(TazLang.Get("item_detail_layer_fmt", new[] { _item.Layer.ToString(), ((int)_item.Layer).ToString() }), MyraLabel.TextStyle.P));

        TimeSpan timeAgo = DateTime.Now - _item.UpdatedTime;
        string timeText = timeAgo.TotalDays >= 1
            ? TazLang.Get("item_detail_time_days_fmt", new[] { timeAgo.Days.ToString() })
            : timeAgo.TotalHours >= 1
                ? TazLang.Get("item_detail_time_hours_fmt", new[] { timeAgo.Hours.ToString() })
                : timeAgo.TotalMinutes >= 1
                    ? TazLang.Get("item_detail_time_minutes_fmt", new[] { ((int)timeAgo.TotalMinutes).ToString() })
                    : TazLang.Get("item_detail_time_just_now", "Just now");
        panel.Widgets.Add(new MyraLabel(TazLang.Get("item_detail_last_seen_fmt", new[] { timeText }), MyraLabel.TextStyle.P));

        string charServer = _item.CharacterName;
        if (!string.IsNullOrEmpty(_item.ServerName))
            charServer = TazLang.Get("item_detail_character_server_fmt", new[] { _item.CharacterName, _item.ServerName });
        panel.Widgets.Add(new MyraLabel(TazLang.Get("item_detail_character_fmt", new[] { charServer }), MyraLabel.TextStyle.P));

        return panel;
    }

    private Widget BuildLocationSection()
    {
        var panel = new VerticalStackPanel { Spacing = 2 };
        panel.Widgets.Add(new MyraLabel(TazLang.Get("item_detail_location", "Location"), MyraLabel.TextStyle.H3));

        if (_item.OnGround)
        {
            panel.Widgets.Add(new MyraLabel(TazLang.Get("item_detail_on_ground_fmt", new[] { _item.X.ToString(), _item.Y.ToString() }), MyraLabel.TextStyle.P));
        }
        else
        {
            panel.Widgets.Add(new MyraLabel(TazLang.Get("item_detail_in_container", "In container"), MyraLabel.TextStyle.P));
            if (_item.Container != 0)
            {
                panel.Widgets.Add(new MyraLabel(TazLang.Get("item_detail_container_fmt", new[] { _item.Container.ToString("X8") }), MyraLabel.TextStyle.P));

                Item? containerItem = Client.Game.UO?.World?.Items?.Get(_item.Container);
                if (containerItem != null &&
                    containerItem.RootContainer != 0 &&
                    containerItem.RootContainer != _item.Container)
                    panel.Widgets.Add(new MyraLabel(TazLang.Get("item_detail_root_container_fmt", new[] { containerItem.RootContainer.ToString("X8") }), MyraLabel.TextStyle.P));
            }
        }

        return panel;
    }

    private Widget BuildPropertiesSection()
    {
        var panel = new VerticalStackPanel { Spacing = 2 };
        panel.Widgets.Add(new MyraLabel(TazLang.Get("item_detail_properties", "Properties"), MyraLabel.TextStyle.H3));

        if (!string.IsNullOrEmpty(_item.Properties))
        {
            foreach (string prop in _item.Properties.Split('|'))
                if (!string.IsNullOrWhiteSpace(prop))
                    panel.Widgets.Add(new MyraLabel($"• {prop.Trim()}", MyraLabel.TextStyle.P));
        }
        else
        {
            panel.Widgets.Add(new MyraLabel(TazLang.Get("item_detail_no_properties", "No properties available"), MyraLabel.TextStyle.P));
        }

        return panel;
    }

    private Widget BuildActionsSection()
    {
        var panel = new VerticalStackPanel { Spacing = 4 };
        panel.Widgets.Add(new MyraLabel(TazLang.Get("item_detail_actions", "Actions"), MyraLabel.TextStyle.H3));

        var row1 = new HorizontalStackPanel { Spacing = 4 };

        // Use Item — only if item exists in world
        Item? worldItem = World.Instance?.Items?.Get(_item.Serial);
        if (worldItem != null && !worldItem.IsDestroyed)
        {
            row1.Widgets.Add(new MyraButton(TazLang.Get("item_detail_btn_use_item", "Use Item"), () =>
                GameActions.DoubleClick(World.Instance, _item.Serial))
            { Tooltip = TazLang.Get("item_detail_tooltip_use_item", "Double-click the item to use it") });
        }

        // Take Item — only if not already in backpack
        uint backpackSerial = Client.Game.UO?.World?.Player?.Backpack?.Serial ?? 0;
        if (_item.Container != backpackSerial)
        {
            row1.Widgets.Add(new MyraButton(TazLang.Get("item_detail_btn_take_item", "Take Item"), MoveToBackpack)
                { Tooltip = TazLang.Get("item_detail_tooltip_take_item", "Move the item to your backpack") });
        }

        row1.Widgets.Add(new MyraButton(TazLang.Get("item_detail_btn_locate", "Try to Locate"), TryToLocate)
            { Tooltip = TazLang.Get("item_detail_tooltip_locate", "Create a quest arrow pointing to the item's last known location") });

        row1.Widgets.Add(new MyraButton(TazLang.Get("item_detail_btn_set_custom_name", "Set Custom Name"), () =>
        {
            var nameBox = new MyraInputBox { Text = _item.CustomName, Width = 220 };
            new MyraDialog(TazLang.Get("item_detail_dialog_set_custom_name", "Set Custom Name"), nameBox, ok =>
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
            row2.Widgets.Add(new MyraButton(TazLang.Get("item_detail_btn_view_container", "View Container"), () =>
                OpenContainerDetail(_item.Container))
            { Tooltip = TazLang.Get("item_detail_tooltip_view_container", "View the container's database entry") });

            Item? cont = Client.Game.UO?.World?.Items?.Get(_item.Container);
            if (cont != null &&
                cont.RootContainer != 0 &&
                cont.RootContainer != _item.Container)
            {
                row2.Widgets.Add(new MyraButton(TazLang.Get("item_detail_btn_view_root_container", "View Root Container"), () =>
                    OpenContainerDetail(cont.RootContainer))
                { Tooltip = TazLang.Get("item_detail_tooltip_view_root_container", "View the root container's database entry") });
            }
        }

        row2.Widgets.Add(new MyraButton(TazLang.Get("item_detail_btn_close", "Close"), () => _disposeRequested = true));
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
