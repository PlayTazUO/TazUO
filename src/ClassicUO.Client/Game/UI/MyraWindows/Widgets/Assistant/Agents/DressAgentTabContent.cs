#nullable enable
using System.Collections.Generic;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Configuration;
using Myra.Graphics2D.UI;
using ClassicUO.Configuration;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Assistant.Agents;

public static class DressAgentTabContent
{
    public static Widget Build()
    {
        if (DressAgentManager.Instance == null)
            return new MyraLabel(TazLang.Get("dressagent_notloaded", "Dress Agent not loaded"), MyraLabel.TextStyle.P);

        DressConfig? selectedConfig = null;
        var leftPanel = new VerticalStackPanel { Spacing = 4 };
        var rightPanel = new VerticalStackPanel { Spacing = 4 };

        void BuildItemsGrid(VerticalStackPanel itemsPanel)
        {
            itemsPanel.Widgets.Clear();
            if (selectedConfig == null || selectedConfig.Items.Count == 0)
            {
                itemsPanel.Widgets.Add(new MyraLabel(TazLang.Get("dressagent_noitems", "No items configured."), MyraLabel.TextStyle.P));
                return;
            }

            var grid = new MyraGrid();
            grid.SetupWithHeaders(
                GridColumnInfo.Auto(TazLang.Get("dressagent_col_serial", "Serial")),
                GridColumnInfo.Fill(TazLang.Get("dressagent_col_name", "Name")),
                GridColumnInfo.Auto(TazLang.Get("dressagent_col_layer", "Layer")),
                GridColumnInfo.Auto(TazLang.Get("dressagent_col_actions", "Actions"))
            );

            int dataRow = 1;
            for (int i = selectedConfig.Items.Count - 1; i >= 0; i--)
            {
                DressItem item = selectedConfig.Items[i];
                grid.AddWidget(new MyraLabel($"{item.Serial:X}", MyraLabel.TextStyle.P, MyraLabel.AlignMode.Right), dataRow, 0);
                grid.AddWidget(new MyraLabel(item.Name, MyraLabel.TextStyle.P), dataRow, 1);
                grid.AddWidget(new MyraLabel(((Layer)item.Layer).ToString(), MyraLabel.TextStyle.P), dataRow, 2);
                DressItem captured = item;
                grid.AddWidget(MyraStyle.ApplyButtonDangerStyle(new MyraButton(TazLang.Get("agent_delete", "Delete"), () =>
                {
                    DressAgentManager.Instance.RemoveItemFromConfig(selectedConfig, captured.Serial);
                    BuildItemsGrid(itemsPanel);
                }) { Tooltip = TazLang.Get("dressagent_delete_tooltip", "Remove this item") }), dataRow, 3);
                dataRow++;
            }

            itemsPanel.Widgets.Add(grid);
        }

        void BuildConfigList()
        {
            leftPanel.Widgets.Clear();
            leftPanel.Widgets.Add(new MyraLabel(TazLang.Get("dressagent_configurations", "Dress Configurations"), MyraLabel.TextStyle.H3));
            leftPanel.Widgets.Add(new MyraButton(TazLang.Get("dressagent_addconfig", "Add Configuration"), () =>
            {
                DressConfig newConfig = DressAgentManager.Instance.CreateNewConfig(
                    string.Format(TazLang.Get("dressagent_newconfigname_fmt", "Config {0}"), DressAgentManager.Instance.CurrentPlayerConfigs.Count + 1));
                selectedConfig = newConfig;
                BuildConfigList();
                BuildConfigDetails();
            }));

            foreach (DressConfig config in DressAgentManager.Instance.CurrentPlayerConfigs)
            {
                DressConfig captured = config;
                var btn = new MyraButton(string.Format(TazLang.Get("dressagent_configbutton_fmt", "{0} ({1} items)"), config.Name, config.Items.Count.ToString()), () =>
                {
                    selectedConfig = captured;
                    BuildConfigDetails();
                });
                if (!string.IsNullOrEmpty(config.CharacterName))
                    btn.Tooltip = string.Format(TazLang.Get("dressagent_config_char_tooltip_fmt", "Character: {0}"), config.CharacterName);
                leftPanel.Widgets.Add(btn);
            }
        }

        void BuildConfigDetails()
        {
            rightPanel.Widgets.Clear();
            if (selectedConfig == null)
            {
                rightPanel.Widgets.Add(new MyraLabel(TazLang.Get("dressagent_selectconfig_details", "Select a configuration to view details"), MyraLabel.TextStyle.P));
                return;
            }

            // Name
            var nameBox = new MyraInputBox { Text = selectedConfig.Name, Width = 200 };
            nameBox.TextChangedByUser += (_, _) =>
            {
                if (!string.IsNullOrWhiteSpace(nameBox.Text))
                {
                    selectedConfig.Name = nameBox.Text.Trim();
                    DressAgentManager.Instance.Save();
                }
            };
            var nameRow = new HorizontalStackPanel { Spacing = 4 };
            nameRow.Widgets.Add(new MyraLabel(TazLang.Get("dressagent_name_label", "Name:"), MyraLabel.TextStyle.P));
            nameRow.Widgets.Add(nameBox);
            rightPanel.Widgets.Add(nameRow);

            // Action buttons
            var actionRow = new HorizontalStackPanel { Spacing = 4 };
            actionRow.Widgets.Add(new MyraButton(TazLang.Get("dressagent_dress", "Dress"), () =>
            {
                DressAgentManager.Instance.DressFromConfig(selectedConfig);
                GameActions.Print(string.Format(TazLang.Get("dressagent_dressing_msg_fmt", "Dressing from config: {0}"), selectedConfig.Name));
            }));
            actionRow.Widgets.Add(new MyraButton(TazLang.Get("dressagent_undress", "Undress"), () =>
            {
                DressAgentManager.Instance.UndressFromConfig(selectedConfig);
                GameActions.Print(string.Format(TazLang.Get("dressagent_undressing_msg_fmt", "Undressing from config: {0}"), selectedConfig.Name));
            }));
            actionRow.Widgets.Add(new MyraButton(TazLang.Get("dressagent_createdressmacro", "Create Dress Macro"), () =>
            {
                DressAgentManager.Instance.CreateDressMacro(selectedConfig.Name);
                GameActions.Print(string.Format(TazLang.Get("dressagent_createdressmacro_msg_fmt", "Created Dress Macro: {0}"), selectedConfig.Name));
            }));
            actionRow.Widgets.Add(new MyraButton(TazLang.Get("dressagent_createundressmacro", "Create Undress Macro"), () =>
            {
                DressAgentManager.Instance.CreateUndressMacro(selectedConfig.Name);
                GameActions.Print(string.Format(TazLang.Get("dressagent_createundressmacro_msg_fmt", "Created Undress Macro: {0}"), selectedConfig.Name));
            }));
            actionRow.Widgets.Add(MyraStyle.ApplyButtonDangerStyle(new MyraButton(TazLang.Get("agent_delete", "Delete"), () =>
            {
                DressAgentManager.Instance.DeleteConfig(selectedConfig);
                List<DressConfig> configs = DressAgentManager.Instance.CurrentPlayerConfigs;
                selectedConfig = configs.Count > 0 ? configs[0] : null;
                BuildConfigList();
                BuildConfigDetails();
            })));
            rightPanel.Widgets.Add(actionRow);

            // KR Equip Packet
            rightPanel.Widgets.Add(new MyraSpacer(15, 1));
            rightPanel.Widgets.Add(MyraCheckButton.CreateWithCallback(
                selectedConfig.UseKREquipPacket,
                b => { selectedConfig.UseKREquipPacket = b; DressAgentManager.Instance.Save(); },
                TazLang.Get("dressagent_krpacket_label", "Use KR Equip Packet (faster)"),
                TazLang.Get("dressagent_krpacket_tooltip_operation", "Uses KR equip/unequip packets for faster operation")));

            // Undress bag
            rightPanel.Widgets.Add(new MyraSpacer(15, 1));
            rightPanel.Widgets.Add(new MyraLabel(TazLang.Get("dressagent_undressbagsettings", "Undress Bag Settings"), MyraLabel.TextStyle.H3));
            var undressBagRow = new HorizontalStackPanel { Spacing = 4 };
            undressBagRow.Widgets.Add(new MyraButton(TazLang.Get("dressagent_setundressbag", "Set Undress Bag"), () =>
            {
                GameActions.Print(TazLang.Get("dressagent_target_undressbag_prompt", "Select container for undressed items"), 82);
                World.Instance.TargetManager.SetTargeting(target =>
                {
                    if (target is Entity entity && SerialHelper.IsItem(entity))
                    {
                        if (selectedConfig == null) return;
                        DressAgentManager.Instance.SetUndressBag(selectedConfig, entity.Serial);
                        GameActions.Print(string.Format(TazLang.Get("dressagent_undressbag_set_fmt", "Undress bag set to {0}"), entity.Serial.ToString("X")), Constants.HUE_SUCCESS);
                        BuildConfigDetails();
                    }
                    else
                        GameActions.Print(TazLang.Get("agent_msg_onlyitems", "Only items can be selected!"));
                });
            }));
            if (selectedConfig.UndressBagSerial != 0)
            {
                undressBagRow.Widgets.Add(new MyraLabel(string.Format(TazLang.Get("dressagent_undressbag_current_fmt", "Current: ({0})"), selectedConfig.UndressBagSerial.ToString("X")), MyraLabel.TextStyle.P));
                undressBagRow.Widgets.Add(MyraStyle.ApplyButtonDangerStyle(new MyraButton(TazLang.Get("dressagent_clear", "Clear"), () =>
                {
                    DressAgentManager.Instance.SetUndressBag(selectedConfig, 0);
                    BuildConfigDetails();
                })));
            }
            else
                undressBagRow.Widgets.Add(new MyraLabel(TazLang.Get("dressagent_undressbag_default", "Default: Your backpack"), MyraLabel.TextStyle.P));
            rightPanel.Widgets.Add(undressBagRow);

            // Items section
            rightPanel.Widgets.Add(new MyraSpacer(15, 1));
            rightPanel.Widgets.Add(new MyraLabel(TazLang.Get("dressagent_itemsheading", "Items to Dress/Undress"), MyraLabel.TextStyle.H3));
            var itemsPanel = new VerticalStackPanel { Spacing = 2 };
            var itemActionRow = new HorizontalStackPanel { Spacing = 4 };
            itemActionRow.Widgets.Add(new MyraButton(TazLang.Get("dressagent_addcurrentlyequipped", "Add Currently Equipped"), () =>
            {
                DressAgentManager.Instance.AddCurrentlyEquippedItems(selectedConfig);
                GameActions.Print(TazLang.Get("dressagent_added_equipped_msg", "Added currently equipped items to config"));
                BuildItemsGrid(itemsPanel);
            }));
            itemActionRow.Widgets.Add(new MyraButton(TazLang.Get("dressagent_targetitemtoadd", "Target Item to Add"), () =>
            {
                GameActions.Print(TazLang.Get("dressagent_target_item_prompt", "Target an item to add to this config"), 82);
                World.Instance.TargetManager.SetTargeting(obj =>
                {
                    if (obj is Entity entity && SerialHelper.IsItem(entity))
                    {
                        if (selectedConfig == null) return;
                        DressAgentManager.Instance.AddItemToConfig(selectedConfig, entity.Serial, entity.Name);
                        GameActions.Print(string.Format(TazLang.Get("dressagent_addeditem_fmt", "Added item: {0}"), entity.Name));
                        BuildItemsGrid(itemsPanel);
                    }
                    else
                        GameActions.Print(TazLang.Get("agent_msg_onlyitemsadded", "Only items can be added!"));
                });
            }));
            itemActionRow.Widgets.Add(MyraStyle.ApplyButtonDangerStyle(new MyraButton(TazLang.Get("dressagent_clearitems", "Clear All Items"), () =>
            {
                DressAgentManager.Instance.ClearConfig(selectedConfig);
                GameActions.Print(TazLang.Get("dressagent_cleareditems_msg", "Cleared all items from config"));
                BuildItemsGrid(itemsPanel);
            })));
            rightPanel.Widgets.Add(itemActionRow);
            BuildItemsGrid(itemsPanel);
            rightPanel.Widgets.Add(new ScrollViewer { MaxHeight = 250, Content = itemsPanel });
        }

        BuildConfigList();
        BuildConfigDetails();

        var root = new HorizontalStackPanel { Spacing = 8 };
        root.Widgets.Add(new ScrollViewer { Width = 200, Content = leftPanel });
        root.Widgets.Add(rightPanel);
        return root;
    }
}
