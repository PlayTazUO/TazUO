#nullable enable
using System.Collections.Generic;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Assistant.Agents;

public static class DressAgentTabContent
{
    public static Widget Build()
    {
        if (DressAgentManager.Instance == null)
            return new MyraLabel("Dress Agent not loaded", MyraLabel.TextStyle.P);

        DressConfig? selectedConfig = null;
        var leftPanel = new VerticalStackPanel { Spacing = 4 };
        var rightPanel = new VerticalStackPanel { Spacing = 4 };

        void BuildItemsGrid(VerticalStackPanel itemsPanel)
        {
            itemsPanel.Widgets.Clear();
            if (selectedConfig == null || selectedConfig.Items.Count == 0)
            {
                itemsPanel.Widgets.Add(new MyraLabel("No items configured.", MyraLabel.TextStyle.P));
                return;
            }

            var grid = new MyraGrid();
            grid.SetupWithHeaders(
                GridColumnInfo.Auto("Serial"),
                GridColumnInfo.Fill("Name"),
                GridColumnInfo.Auto("Layer"),
                GridColumnInfo.Auto("Actions")
            );

            int dataRow = 1;
            for (int i = selectedConfig.Items.Count - 1; i >= 0; i--)
            {
                DressItem item = selectedConfig.Items[i];
                grid.AddWidget(new MyraLabel($"{item.Serial:X}", MyraLabel.TextStyle.P, MyraLabel.AlignMode.Right), dataRow, 0);
                grid.AddWidget(new MyraLabel(item.Name, MyraLabel.TextStyle.P), dataRow, 1);
                grid.AddWidget(new MyraLabel(((Layer)item.Layer).ToString(), MyraLabel.TextStyle.P), dataRow, 2);
                DressItem captured = item;
                grid.AddWidget(MyraStyle.ApplyButtonDangerStyle(new MyraButton("Delete", () =>
                {
                    DressAgentManager.Instance.RemoveItemFromConfig(selectedConfig, captured.Serial);
                    BuildItemsGrid(itemsPanel);
                }) { Tooltip = "Remove this item" }), dataRow, 3);
                dataRow++;
            }

            itemsPanel.Widgets.Add(grid);
        }

        void BuildConfigList()
        {
            leftPanel.Widgets.Clear();
            leftPanel.Widgets.Add(new MyraLabel("Dress Configurations", MyraLabel.TextStyle.H3));
            leftPanel.Widgets.Add(new MyraButton("Add Configuration", () =>
            {
                DressConfig newConfig = DressAgentManager.Instance.CreateNewConfig(
                    $"Config {DressAgentManager.Instance.CurrentPlayerConfigs.Count + 1}");
                selectedConfig = newConfig;
                BuildConfigList();
                BuildConfigDetails();
            }));

            foreach (DressConfig config in DressAgentManager.Instance.CurrentPlayerConfigs)
            {
                DressConfig captured = config;
                var btn = new MyraButton($"{config.Name} ({config.Items.Count} items)", () =>
                {
                    selectedConfig = captured;
                    BuildConfigDetails();
                });
                if (!string.IsNullOrEmpty(config.CharacterName))
                    btn.Tooltip = $"Character: {config.CharacterName}";
                leftPanel.Widgets.Add(btn);
            }
        }

        void BuildConfigDetails()
        {
            rightPanel.Widgets.Clear();
            if (selectedConfig == null)
            {
                rightPanel.Widgets.Add(new MyraLabel("Select a configuration to view details", MyraLabel.TextStyle.P));
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
            nameRow.Widgets.Add(new MyraLabel("Name:", MyraLabel.TextStyle.P));
            nameRow.Widgets.Add(nameBox);
            rightPanel.Widgets.Add(nameRow);

            // Action buttons
            var actionRow = new HorizontalStackPanel { Spacing = 4 };
            actionRow.Widgets.Add(new MyraButton("Dress", () =>
            {
                DressAgentManager.Instance.DressFromConfig(selectedConfig);
                GameActions.Print($"Dressing from config: {selectedConfig.Name}");
            }));
            actionRow.Widgets.Add(new MyraButton("Undress", () =>
            {
                DressAgentManager.Instance.UndressFromConfig(selectedConfig);
                GameActions.Print($"Undressing from config: {selectedConfig.Name}");
            }));
            actionRow.Widgets.Add(new MyraButton("Create Dress Macro", () =>
            {
                DressAgentManager.Instance.CreateDressMacro(selectedConfig.Name);
                GameActions.Print($"Created Dress Macro: {selectedConfig.Name}");
            }));
            actionRow.Widgets.Add(new MyraButton("Create Undress Macro", () =>
            {
                DressAgentManager.Instance.CreateUndressMacro(selectedConfig.Name);
                GameActions.Print($"Created Undress Macro: {selectedConfig.Name}");
            }));
            actionRow.Widgets.Add(MyraStyle.ApplyButtonDangerStyle(new MyraButton("Delete", () =>
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
                "Use KR Equip Packet (faster)",
                "Uses KR equip/unequip packets for faster operation"));

            // Undress bag
            rightPanel.Widgets.Add(new MyraSpacer(15, 1));
            rightPanel.Widgets.Add(new MyraLabel("Undress Bag Settings", MyraLabel.TextStyle.H3));
            var undressBagRow = new HorizontalStackPanel { Spacing = 4 };
            undressBagRow.Widgets.Add(new MyraButton("Set Undress Bag", () =>
            {
                GameActions.Print("Select container for undressed items", 82);
                World.Instance.TargetManager.SetTargeting(target =>
                {
                    if (target is Entity entity && SerialHelper.IsItem(entity))
                    {
                        if (selectedConfig == null) return;
                        DressAgentManager.Instance.SetUndressBag(selectedConfig, entity.Serial);
                        GameActions.Print($"Undress bag set to {entity.Serial:X}", Constants.HUE_SUCCESS);
                        BuildConfigDetails();
                    }
                    else
                        GameActions.Print("Only items can be selected!");
                });
            }));
            if (selectedConfig.UndressBagSerial != 0)
            {
                undressBagRow.Widgets.Add(new MyraLabel($"Current: ({selectedConfig.UndressBagSerial:X})", MyraLabel.TextStyle.P));
                undressBagRow.Widgets.Add(MyraStyle.ApplyButtonDangerStyle(new MyraButton("Clear", () =>
                {
                    DressAgentManager.Instance.SetUndressBag(selectedConfig, 0);
                    BuildConfigDetails();
                })));
            }
            else
                undressBagRow.Widgets.Add(new MyraLabel("Default: Your backpack", MyraLabel.TextStyle.P));
            rightPanel.Widgets.Add(undressBagRow);

            // Items section
            rightPanel.Widgets.Add(new MyraSpacer(15, 1));
            rightPanel.Widgets.Add(new MyraLabel("Items to Dress/Undress", MyraLabel.TextStyle.H3));
            var itemsPanel = new VerticalStackPanel { Spacing = 2 };
            var itemActionRow = new HorizontalStackPanel { Spacing = 4 };
            itemActionRow.Widgets.Add(new MyraButton("Add Currently Equipped", () =>
            {
                DressAgentManager.Instance.AddCurrentlyEquippedItems(selectedConfig);
                GameActions.Print("Added currently equipped items to config");
                BuildItemsGrid(itemsPanel);
            }));
            itemActionRow.Widgets.Add(new MyraButton("Target Item to Add", () =>
            {
                GameActions.Print("Target an item to add to this config", 82);
                World.Instance.TargetManager.SetTargeting(obj =>
                {
                    if (obj is Entity entity && SerialHelper.IsItem(entity))
                    {
                        if (selectedConfig == null) return;
                        DressAgentManager.Instance.AddItemToConfig(selectedConfig, entity.Serial, entity.Name);
                        GameActions.Print($"Added item: {entity.Name}");
                        BuildItemsGrid(itemsPanel);
                    }
                    else
                        GameActions.Print("Only items can be added!");
                });
            }));
            itemActionRow.Widgets.Add(MyraStyle.ApplyButtonDangerStyle(new MyraButton("Clear All Items", () =>
            {
                DressAgentManager.Instance.ClearConfig(selectedConfig);
                GameActions.Print("Cleared all items from config");
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
