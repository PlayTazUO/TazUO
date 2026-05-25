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
            return new MyraLabel("换装代理未加载", MyraLabel.TextStyle.P);

        DressConfig? selectedConfig = null;
        var leftPanel = new VerticalStackPanel { Spacing = 4 };
        var rightPanel = new VerticalStackPanel { Spacing = 4 };

        void BuildItemsGrid(VerticalStackPanel itemsPanel)
        {
            itemsPanel.Widgets.Clear();
            if (selectedConfig == null || selectedConfig.Items.Count == 0)
            {
                itemsPanel.Widgets.Add(new MyraLabel("没有配置物品。", MyraLabel.TextStyle.P));
                return;
            }

            var grid = new MyraGrid();
            grid.SetupWithHeaders(
                GridColumnInfo.Auto("序列号"),
                GridColumnInfo.Fill("名称"),
                GridColumnInfo.Auto("层"),
                GridColumnInfo.Auto("操作")
            );

            int dataRow = 1;
            for (int i = selectedConfig.Items.Count - 1; i >= 0; i--)
            {
                DressItem item = selectedConfig.Items[i];
                grid.AddWidget(new MyraLabel($"{item.Serial:X}", MyraLabel.TextStyle.P, MyraLabel.AlignMode.Right), dataRow, 0);
                grid.AddWidget(new MyraLabel(item.Name, MyraLabel.TextStyle.P), dataRow, 1);
                grid.AddWidget(new MyraLabel(((Layer)item.Layer).ToString(), MyraLabel.TextStyle.P), dataRow, 2);
                DressItem captured = item;
                grid.AddWidget(MyraStyle.ApplyButtonDangerStyle(new MyraButton("删除", () =>
                {
                    DressAgentManager.Instance.RemoveItemFromConfig(selectedConfig, captured.Serial);
                    BuildItemsGrid(itemsPanel);
                }) { Tooltip = "移除此物品" }), dataRow, 3);
                dataRow++;
            }

            itemsPanel.Widgets.Add(grid);
        }

        void BuildConfigList()
        {
            leftPanel.Widgets.Clear();
            leftPanel.Widgets.Add(new MyraLabel("换装配置", MyraLabel.TextStyle.H3));
            leftPanel.Widgets.Add(new MyraButton("添加配置", () =>
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
                rightPanel.Widgets.Add(new MyraLabel("选择一个配置以查看详情", MyraLabel.TextStyle.P));
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
            nameRow.Widgets.Add(new MyraLabel("名称:", MyraLabel.TextStyle.P));
            nameRow.Widgets.Add(nameBox);
            rightPanel.Widgets.Add(nameRow);

            // Action buttons
            var actionRow = new HorizontalStackPanel { Spacing = 4 };
            actionRow.Widgets.Add(new MyraButton("换装", () =>
            {
                DressAgentManager.Instance.DressFromConfig(selectedConfig);
                GameActions.Print($"正在从配置换装: {selectedConfig.Name}");
            }));
            actionRow.Widgets.Add(new MyraButton("卸装", () =>
            {
                DressAgentManager.Instance.UndressFromConfig(selectedConfig);
                GameActions.Print($"正在从配置卸装: {selectedConfig.Name}");
            }));
            actionRow.Widgets.Add(new MyraButton("创建换装宏", () =>
            {
                DressAgentManager.Instance.CreateDressMacro(selectedConfig.Name);
                GameActions.Print($"已创建换装宏: {selectedConfig.Name}");
            }));
            actionRow.Widgets.Add(new MyraButton("创建卸装宏", () =>
            {
                DressAgentManager.Instance.CreateUndressMacro(selectedConfig.Name);
                GameActions.Print($"已创建卸装宏: {selectedConfig.Name}");
            }));
            actionRow.Widgets.Add(MyraStyle.ApplyButtonDangerStyle(new MyraButton("删除", () =>
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
                "使用KR装备数据包（更快）",
                "使用KR装备/卸装数据包以加快操作速度"));

            // Undress bag
            rightPanel.Widgets.Add(new MyraSpacer(15, 1));
            rightPanel.Widgets.Add(new MyraLabel("卸装包设置", MyraLabel.TextStyle.H3));
            var undressBagRow = new HorizontalStackPanel { Spacing = 4 };
            undressBagRow.Widgets.Add(new MyraButton("设置卸装包", () =>
            {
                GameActions.Print("Select container for undressed items", 82);
                World.Instance.TargetManager.SetTargeting(target =>
                {
                    if (target is Entity entity && SerialHelper.IsItem(entity))
                    {
                        if (selectedConfig == null) return;
                DressAgentManager.Instance.SetUndressBag(selectedConfig, entity.Serial);
                GameActions.Print($"卸装包已设置为 {entity.Serial:X}", Constants.HUE_SUCCESS);
                        BuildConfigDetails();
                    }
                    else
                        GameActions.Print("只能选择物品!");
                });
            }));
            if (selectedConfig.UndressBagSerial != 0)
            {
                undressBagRow.Widgets.Add(new MyraLabel($"Current: ({selectedConfig.UndressBagSerial:X})", MyraLabel.TextStyle.P));
                undressBagRow.Widgets.Add(MyraStyle.ApplyButtonDangerStyle(new MyraButton("清除", () =>
                {
                    DressAgentManager.Instance.SetUndressBag(selectedConfig, 0);
                    BuildConfigDetails();
                })));
            }
            else
                undressBagRow.Widgets.Add(new MyraLabel("默认: 您的背包", MyraLabel.TextStyle.P));
            rightPanel.Widgets.Add(undressBagRow);

            // Items section
            rightPanel.Widgets.Add(new MyraSpacer(15, 1));
            rightPanel.Widgets.Add(new MyraLabel("换装/卸装物品", MyraLabel.TextStyle.H3));
            var itemsPanel = new VerticalStackPanel { Spacing = 2 };
            var itemActionRow = new HorizontalStackPanel { Spacing = 4 };
            itemActionRow.Widgets.Add(new MyraButton("添加当前装备", () =>
            {
                DressAgentManager.Instance.AddCurrentlyEquippedItems(selectedConfig);
                GameActions.Print("已将当前装备物品添加到配置");
                BuildItemsGrid(itemsPanel);
            }));
            itemActionRow.Widgets.Add(new MyraButton("目标物品添加", () =>
            {
                GameActions.Print("Target an item to add to this config", 82);
                World.Instance.TargetManager.SetTargeting(obj =>
                {
                    if (obj is Entity entity && SerialHelper.IsItem(entity))
                    {
                        if (selectedConfig == null) return;
                        DressAgentManager.Instance.AddItemToConfig(selectedConfig, entity.Serial, entity.Name);
                        GameActions.Print($"已添加物品: {entity.Name}");
                        BuildItemsGrid(itemsPanel);
                    }
                    else
                        GameActions.Print("只能添加物品!");
                });
            }));
            itemActionRow.Widgets.Add(MyraStyle.ApplyButtonDangerStyle(new MyraButton("清空全部物品", () =>
            {
                DressAgentManager.Instance.ClearConfig(selectedConfig);
                GameActions.Print("已从配置中清空所有物品");
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
