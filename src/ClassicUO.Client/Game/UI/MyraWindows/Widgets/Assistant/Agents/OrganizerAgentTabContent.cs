#nullable enable
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Utility;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Assistant.Agents;

public static class OrganizerAgentTabContent
{
    public static Widget Build()
    {
        OrganizerConfig? selectedConfig = null;
        var leftPanel = new VerticalStackPanel { Spacing = 4 };
        var rightPanel = new VerticalStackPanel { Spacing = 4 };

        void BuildItemsGrid(VerticalStackPanel itemsPanel)
        {
            itemsPanel.Widgets.Clear();
            if (selectedConfig == null || selectedConfig.ItemConfigs.Count == 0)
            {
                itemsPanel.Widgets.Add(new MyraLabel("没有配置物品。", MyraLabel.TextStyle.H3));
                return;
            }

            var grid = new MyraGrid();
            grid.SetupWithHeaders(
                GridColumnInfo.Auto("图形"),
                GridColumnInfo.Auto("色调"),
                GridColumnInfo.Auto("数量"),
                GridColumnInfo.Fill("目标"),
                GridColumnInfo.Auto("启用"),
                GridColumnInfo.Auto("操作")
            );

            int dataRow = 1;
            for (int i = selectedConfig.ItemConfigs.Count - 1; i >= 0; i--)
            {
                OrganizerItemConfig item = selectedConfig.ItemConfigs[i];

                // Art / Graphic
                Widget artWidget =
                    item.Graphic > 0
                        ? new MyraArtTexture((uint)item.Graphic)
                        {
                            Tooltip = $"Graphic: {item.Graphic:X4}",
                            Margin = new Thickness(2, 0),
                        }
                        : new MyraLabel($"{item.Graphic:X4}", MyraLabel.TextStyle.P);
                grid.AddWidget(artWidget, dataRow, 0);

                // Hue
                var hueBox = MyraInputBox.Hue(item.Hue);
                hueBox.TextChangedByUser += (_, _) =>
                {
                    if (MyraInputBox.TryParseHue(hueBox.Text, out ushort hue))
                        item.Hue = hue;
                };
                grid.AddWidget(hueBox, dataRow, 1);

                // Amount
                var amountBox = new MyraInputBox
                {
                    Text = item.Amount.ToString(),
                    Tooltip = "要移动的数量。会考虑已在目标中的物品。\n(0 = 全部移动)",
                    Width = 80,
                };
                amountBox.TextChangedByUser += (_, _) =>
                {
                    if (ushort.TryParse(amountBox.Text, out ushort amount))
                        item.Amount = amount;
                };
                grid.AddWidget(amountBox, dataRow, 2);

                // Destination (rebuild the cell in-place via a container panel)
                var destCell = new HorizontalStackPanel { Spacing = 4 };
                OrganizerItemConfig captured = item;

                void BuildDestCell()
                {
                    destCell.Widgets.Clear();
                    if (captured.DestContSerial != 0)
                    {
                        var label = new MyraLabel($"{captured.DestContSerial:X}", MyraLabel.TextStyle.P) { Tooltip = "每个物品的目标" };
                        StackPanel.SetProportionType(label, ProportionType.Fill);
                        destCell.Widgets.Add(label);
                        destCell.Widgets.Add(MyraStyle.ApplyButtonDangerStyle(new MyraButton("X", () =>
                        {
                            captured.DestContSerial = 0;
                            BuildDestCell();
                        }) { Tooltip = "清除并使用配置目标" }));
                    }
                    else
                    {
                        var label = new MyraLabel("配置", MyraLabel.TextStyle.P) { Tooltip = "使用配置的目标" };
                        StackPanel.SetProportionType(label, ProportionType.Fill);
                        destCell.Widgets.Add(label);
                        destCell.Widgets.Add(new MyraButton("设置", () =>
                        {
                            GameActions.Print("选择此物品的 [目标] 容器", 82);
                            World.Instance.TargetManager.SetTargeting(destination =>
                            {
                                if (destination is Entity destEntity && SerialHelper.IsItem(destEntity))
                                {
                                    captured.DestContSerial = destEntity.Serial;
                                    GameActions.Print($"每个物品的目标已设置为 {destEntity.Serial:X}", Constants.HUE_SUCCESS);
                                    BuildDestCell();
                                }
                                else
                                    GameActions.Print("只能选择物品!");
                            });
                        }) { Tooltip = "设置每个物品的目标" });
                    }
                }

                BuildDestCell();
                grid.AddWidget(destCell, dataRow, 3);

                // Enabled
                var cb = MyraCheckButton.CreateWithCallback(item.Enabled, b => item.Enabled = b);
                cb.HorizontalAlignment = HorizontalAlignment.Center;
                grid.AddWidget(cb, dataRow, 4);

                // Delete
                grid.AddWidget(MyraStyle.ApplyButtonDangerStyle(new MyraButton("删除", () =>
                {
                    selectedConfig.DeleteItemConfig(captured);
                    BuildItemsGrid(itemsPanel);
                }) { Tooltip = "删除此物品" }), dataRow, 5);

                dataRow++;
            }

            itemsPanel.Widgets.Add(grid);
        }

        void BuildConfigList()
        {
            leftPanel.Widgets.Clear();
            leftPanel.Widgets.Add(new MyraButton("添加整理", () =>
            {
                OrganizerConfig newConfig = OrganizerAgent.Instance.NewOrganizerConfig();
                selectedConfig = newConfig;
                BuildConfigList();
                BuildConfigDetails();
            }));
            leftPanel.Widgets.Add(new MyraLabel("列表", MyraLabel.TextStyle.H3));

            foreach (OrganizerConfig config in OrganizerAgent.Instance.OrganizerConfigs)
            {
                OrganizerConfig capturedConfig = config;
                int enabledItems = config.ItemConfigs.Count(ic => ic.Enabled);
                var btn = new MyraButton(config.Name, () =>
                {
                    selectedConfig = capturedConfig;
                    BuildConfigDetails();
                }) { Tooltip = $"{enabledItems} enabled items" };
                leftPanel.Widgets.Add(btn);
            }
        }

        void BuildConfigDetails()
        {
            rightPanel.Widgets.Clear();
            if (selectedConfig == null)
            {
                rightPanel.Widgets.Add(new MyraLabel("选择一个整理器以查看详情", MyraLabel.TextStyle.P));
                return;
            }

            // Enabled + Name
            var topRow = new HorizontalStackPanel { Spacing = 8 };
            topRow.Widgets.Add(MyraCheckButton.CreateWithCallback(
                selectedConfig.Enabled, b => selectedConfig.Enabled = b, "启用"));
            var nameBox = new MyraInputBox { Text = selectedConfig.Name, Width = 150 };
            nameBox.TextChangedByUser += (_, _) =>
            {
                if (!string.IsNullOrWhiteSpace(nameBox.Text))
                    selectedConfig.Name = nameBox.Text;
            };
            topRow.Widgets.Add(new MyraLabel("名称:", MyraLabel.TextStyle.P));
            topRow.Widgets.Add(nameBox);
            rightPanel.Widgets.Add(topRow);

            // Action buttons
            var actionRow = new HorizontalStackPanel { Spacing = 4 };
            actionRow.Widgets.Add(new MyraButton("运行整理", () =>
                OrganizerAgent.Instance.RunOrganizer(selectedConfig.Name)));
            actionRow.Widgets.Add(new MyraButton("复制", () =>
            {
                OrganizerConfig? duped = OrganizerAgent.Instance.DupeConfig(selectedConfig);
                if (duped != null)
                {
                    selectedConfig = duped;
                    BuildConfigList();
                    BuildConfigDetails();
                }
            }));
            actionRow.Widgets.Add(new MyraButton("创建宏", () =>
            {
                OrganizerAgent.Instance.CreateOrganizerMacroButton(selectedConfig.Name);
                GameActions.Print($"已创建整理宏: {selectedConfig.Name}");
            }));
            actionRow.Widgets.Add(new MyraButton("导入", () =>
            {
                string? json = Clipboard.GetClipboardText();
                if (json.NotNullNotEmpty() && OrganizerAgent.Instance.ImportFromJson(json))
                {
                    BuildConfigList();
                    return;
                }
                GameActions.Print("您的剪贴板中没有有效的导出数据。", Constants.HUE_ERROR);
            }) { Tooltip = "从剪贴板导入（必须有有效的导出数据）。" });
            actionRow.Widgets.Add(new MyraButton("导出", () =>
            {
                OrganizerAgent.Instance.GetJsonExport(selectedConfig)?.CopyToClipboard();
                GameActions.Print("已将整理器导出到剪贴板!", Constants.HUE_SUCCESS);
            }) { Tooltip = "将此整理器导出到剪贴板。" });
            actionRow.Widgets.Add(MyraStyle.ApplyButtonDangerStyle(new MyraButton("删除", () =>
            {
                OrganizerAgent.Instance.DeleteConfig(selectedConfig);
                List<OrganizerConfig> configs = OrganizerAgent.Instance.OrganizerConfigs;
                selectedConfig = configs.Count > 0 ? configs[0] : null;
                BuildConfigList();
                BuildConfigDetails();
            })));
            rightPanel.Widgets.Add(actionRow);

            // Container settings
            rightPanel.Widgets.Add(new MyraSpacer(5, 1));
            rightPanel.Widgets.Add(new MyraLabel("容器设置:", MyraLabel.TextStyle.H2));
            var contRow = new HorizontalStackPanel { Spacing = 4 };
            contRow.Widgets.Add(new MyraButton("设置来源容器", () =>
            {
                GameActions.Print("选择 [来源] 容器", 82);
                World.Instance.TargetManager.SetTargeting(source =>
                {
                    if (source is Entity sourceEntity && SerialHelper.IsItem(sourceEntity))
                    {
                        if (selectedConfig == null) return;
                        selectedConfig.SourceContSerial = sourceEntity.Serial;
                        GameActions.Print($"来源容器已设置为 0x{sourceEntity.Serial:X4} ({sourceEntity.Name})", Constants.HUE_SUCCESS);
                        BuildConfigDetails();
                    }
                    else
                        GameActions.Print("只能选择物品!");
                });
            }));
            contRow.Widgets.Add(new MyraButton("设置目标容器", () =>
            {
                GameActions.Print("选择 [目标] 容器", 82);
                World.Instance.TargetManager.SetTargeting(destination =>
                {
                    if (destination is Entity destEntity && SerialHelper.IsItem(destEntity))
                    {
                        if (selectedConfig == null) return;
                        selectedConfig.DestContSerial = destEntity.Serial;
                        GameActions.Print($"目标容器已设置为 0x{destEntity.Serial:X4} ({destEntity.Name})", Constants.HUE_SUCCESS);
                        BuildConfigDetails();
                    }
                    else
                        GameActions.Print("只能选择物品!");
                });
            }));
            rightPanel.Widgets.Add(contRow);

            var contInfoRow = new HorizontalStackPanel { Spacing = 12 };
            string sourceText = selectedConfig.SourceContSerial != 0
                ? $"来源: (0x{selectedConfig.SourceContSerial:X4})"
                : "来源: 您的背包";
            contInfoRow.Widgets.Add(new MyraLabel(sourceText, MyraLabel.TextStyle.P));
            string destText = selectedConfig.DestContSerial != 0
                ? $"目标: (0x{selectedConfig.DestContSerial:X4})"
                : "目标: 未设置";
            contInfoRow.Widgets.Add(new MyraLabel(destText, MyraLabel.TextStyle.P));
            rightPanel.Widgets.Add(contInfoRow);

            // Items section
            rightPanel.Widgets.Add(new MyraSpacer(5, 1));
            rightPanel.Widgets.Add(new MyraLabel("要整理的物品:", MyraLabel.TextStyle.H2));

            var itemsPanel = new VerticalStackPanel { Spacing = 2 };

            // Add item buttons
            var addEntryPanel = new VerticalStackPanel { Visible = false, Spacing = 4 };
            var newGraphicBox = new MyraInputBox { HintText = "图形（十六进制，例如 0EED）", Width = 150 };
            var newHueBox = MyraInputBox.Hue(ushort.MaxValue, 80, "色调 (-1 = 任意)");

            var addItemRow = new HorizontalStackPanel { Spacing = 4 };
            addItemRow.Widgets.Add(new MyraButton("目标物品添加", () =>
            {
                World.Instance.TargetManager.SetTargeting(obj =>
                {
                    if (obj is Entity objEntity && SerialHelper.IsItem(objEntity))
                    {
                        if (selectedConfig == null) return;
                        OrganizerItemConfig newItemConfig = selectedConfig.NewItemConfig();
                        newItemConfig.Graphic = objEntity.Graphic;
                        newItemConfig.Hue = objEntity.Hue;
                        GameActions.Print($"已添加物品: 图形 {objEntity.Graphic:X}, 色调 {objEntity.Hue:X}");
                        BuildItemsGrid(itemsPanel);
                    }
                    else
                        GameActions.Print("只能添加物品!");
                });
            }));
            addItemRow.Widgets.Add(new MyraButton("手动添加物品", () => addEntryPanel.Visible = !addEntryPanel.Visible));
            rightPanel.Widgets.Add(addItemRow);

            // Manual add form
            var addFieldsRow = new HorizontalStackPanel { Spacing = 4 };
            addFieldsRow.Widgets.Add(new MyraLabel("图形:", MyraLabel.TextStyle.P) { Tooltip = "十六进制值，例如 0EED。" });
            addFieldsRow.Widgets.Add(newGraphicBox);
            addFieldsRow.Widgets.Add(new MyraLabel("色调:", MyraLabel.TextStyle.P) { Tooltip = "设为 -1 以匹配任意色调。" });
            addFieldsRow.Widgets.Add(newHueBox);

            var addConfirmRow = new HorizontalStackPanel { Spacing = 4 };
            addConfirmRow.Widgets.Add(new MyraButton("添加", () =>
            {
                if (ushort.TryParse(newGraphicBox.Text, NumberStyles.HexNumber, null, out ushort graphic))
                {
                    OrganizerItemConfig newItemConfig = selectedConfig.NewItemConfig();
                    newItemConfig.Graphic = graphic;

                    if (MyraInputBox.TryParseHue(newHueBox.Text, out ushort hue))
                        newItemConfig.Hue = hue;

                    newGraphicBox.Text = "";
                    newHueBox.Text = "";
                    addEntryPanel.Visible = false;
                    BuildItemsGrid(itemsPanel);
                }
            }));
            addConfirmRow.Widgets.Add(new MyraButton("取消", () =>
            {
                addEntryPanel.Visible = false;
                newGraphicBox.Text = "";
                newHueBox.Text = "";
            }));

            addEntryPanel.Widgets.Add(new MyraLabel("手动输入:", MyraLabel.TextStyle.H3));
            addEntryPanel.Widgets.Add(addFieldsRow);
            addEntryPanel.Widgets.Add(addConfirmRow);
            rightPanel.Widgets.Add(addEntryPanel);

            BuildItemsGrid(itemsPanel);
            rightPanel.Widgets.Add(new ScrollViewer { MaxHeight = 250, Content = itemsPanel });
        }

        BuildConfigList();
        BuildConfigDetails();

        var root = new HorizontalStackPanel { Spacing = MyraStyle.STANDARD_SPACING };
        root.Widgets.Add(new ScrollViewer { Width = 160, Content = leftPanel });
        root.Widgets.Add(rightPanel);
        return root;
    }
}
