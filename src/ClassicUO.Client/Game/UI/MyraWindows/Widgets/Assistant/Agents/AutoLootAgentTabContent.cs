#nullable enable
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Utility;
using Myra.Graphics2D.UI;
using Myra.Graphics2D;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Assistant.Agents;

public static class AutoLootAgentTabContent
{
    private static readonly string[] PriorityLabels = { "低", "正常", "高" };

    public static Widget Build()
    {
        Profile? profile = ProfileManager.CurrentProfile;

        var root = new VerticalStackPanel { Spacing = 6 };

        // Enable Auto Loot + Set Grab Bag
        var topRow = new HorizontalStackPanel { Spacing = 8 };
        topRow.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.EnableAutoLoot,
            b => profile.EnableAutoLoot = b,
            "启用自动拾取",
            "自动拾取允许您根据配置的条件自动从尸体上拾取物品。"));
        topRow.Widgets.Add(new MyraButton("设置拾取包", () =>
        {
            GameActions.Print(Client.Game.UO.World, "目标容器以将物品拾取到其中");
            Client.Game.UO.World.TargetManager.SetTargeting(CursorTarget.SetGrabBag, 0, TargetType.Neutral);
        }) { Tooltip = "选择一个容器以将物品拾取到其中" });
        root.Widgets.Add(topRow);

        // Options
        root.Widgets.Add(new MyraSpacer(15, 5));
        root.Widgets.Add(new MyraLabel("选项:", MyraLabel.TextStyle.H2));

        var optRow1 = new HorizontalStackPanel { Spacing = 8 };
        optRow1.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.EnableScavenger,
            b => profile.EnableScavenger = b,
            "启用自动搜刮",
            "自动搜刮选项允许从地面拾取物品。"));
        optRow1.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.EnableAutoLootProgressBar,
            b => profile.EnableAutoLootProgressBar = b,
            "启用进度条",
            "显示进度条窗口。"));
        root.Widgets.Add(optRow1);

        var optRow2 = new HorizontalStackPanel { Spacing = 8 };
        optRow2.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.AutoLootHumanCorpses,
            b => profile.AutoLootHumanCorpses = b,
            "自动拾取人类尸体",
            "自动拾取人类尸体。"));
        optRow2.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.HueCorpseAfterAutoloot,
            b => profile.HueCorpseAfterAutoloot = b,
            "处理后着色尸体",
            "处理后着色尸体以便更容易看到自动拾取是否已处理它们。"));
        root.Widgets.Add(optRow2);

        // Entries section
        root.Widgets.Add(new MyraSpacer(15, 5));
        root.Widgets.Add(new MyraLabel("条目:", MyraLabel.TextStyle.H2));

        var entriesPanel = new VerticalStackPanel { Spacing = 4 };

        void BuildEntriesList()
        {
            entriesPanel.Widgets.Clear();
            List<AutoLootManager.AutoLootConfigEntry>? entries = AutoLootManager.Instance.AutoLootList;

            if (entries.Count == 0)
            {
                entriesPanel.Widgets.Add(new MyraLabel("没有配置条目。", MyraLabel.TextStyle.P));
                return;
            }

            var grid = new MyraGrid();
            grid.SetupWithHeaders(
                GridColumnInfo.Auto("图形"),
                GridColumnInfo.Auto("图形ID"),
                GridColumnInfo.Auto("色调"),
                GridColumnInfo.Auto("正则"),
                GridColumnInfo.Auto("优先级"),
                GridColumnInfo.Fill("目标"),
                GridColumnInfo.Auto("顺序"),
                GridColumnInfo.Auto("操作")
            );

            int dataRow = 1;
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                AutoLootManager.AutoLootConfigEntry entry = entries[i];

                // Art image (col 0)
                if (entry.Graphic is > 0 and < ushort.MaxValue)
                    grid.AddWidget(new MyraArtTexture((uint)entry.Graphic) { Tooltip = entry.Name, Margin = new Thickness(2, 0) }, dataRow, 0);
                else
                {
                    var nameBox = new MyraInputBox
                    {
                        Text = entry.Name,
                        HintText = "名称",
                        Tooltip = "此条目的显示名称。",
                        MinWidth = 80,
                    };
                    nameBox.TextChangedByUser += (_, _) => entry.Name = nameBox.Text;
                    grid.AddWidget(nameBox, dataRow, 0);
                }

                // Graphic
                var graphicBox = new MyraInputBox
                {
                    Text = entry.Graphic == ushort.MaxValue ? "-1" : entry.Graphic.ToString(),
                    Tooltip = "物品图形ID。设为 -1 以匹配任何图形。",
                };
                graphicBox.TextChangedByUser += (_, _) =>
                {
                    if (StringHelper.TryParseInt(graphicBox.Text, out int g))
                        entry.Graphic = g == -1 ? ushort.MaxValue : g;
                };
                grid.AddWidget(graphicBox, dataRow, 1);

                // Hue
                var hueBox = MyraInputBox.Hue(entry.Hue);
                hueBox.TextChangedByUser += (_, _) =>
                {
                    if (MyraInputBox.TryParseHue(hueBox.Text, out ushort hue))
                        entry.Hue = hue;
                };
                grid.AddWidget(hueBox, dataRow, 2);

                // Regex edit — opens a MyraDialog (own Desktop, registered with UIManager)
                grid.AddWidget(new MyraButton("编辑正则", () =>
                {
                    var regexInput = new MyraInputBox
                    {
                        Text = entry.RegexSearch ?? "",
                        Multiline = true,
                        Width = 300,
                        Height = 80,
                        Tooltip = "匹配物品名称和属性的正则表达式。"
                    };
                    new MyraDialog("编辑正则", regexInput, ok =>
                    {
                        if (ok) entry.RegexSearch = regexInput.Text;
                    });
                }), dataRow, 3);

                // Priority cycle: < label >
                var priorityLabel = new MyraLabel(PriorityLabels[(int)entry.Priority], MyraLabel.TextStyle.P);
                var priorityRow = new HorizontalStackPanel { Spacing = 2 };
                priorityRow.Widgets.Add(new MyraButton("<", () =>
                {
                    int p = ((int)entry.Priority - 1 + PriorityLabels.Length) % PriorityLabels.Length;
                    entry.Priority = (AutoLootManager.AutoLootPriority)p;
                    priorityLabel.Text = PriorityLabels[p];
                }));
                priorityRow.Widgets.Add(priorityLabel);
                priorityRow.Widgets.Add(new MyraButton(">", () =>
                {
                    int p = ((int)entry.Priority + 1) % PriorityLabels.Length;
                    entry.Priority = (AutoLootManager.AutoLootPriority)p;
                    priorityLabel.Text = PriorityLabels[p];
                }));
                grid.AddWidget(priorityRow, dataRow, 4);

                // Destination box + Target button
                var destCell = new HorizontalStackPanel { Spacing = 4 };
                var destBox = new MyraInputBox
                {
                    Text = entry.DestinationContainer == 0 ? "" : $"0x{entry.DestinationContainer:X}",
                    HintText = "序列号（十六进制）",
                    Tooltip = "目标容器序列号（十六进制）。留空则使用拾取包。",
                    MinWidth = 100,
                };
                destBox.TextChangedByUser += (_, _) =>
                {
                    if (string.IsNullOrWhiteSpace(destBox.Text))
                        entry.DestinationContainer = 0;
                    else if (uint.TryParse(destBox.Text.Replace("0x", "").Replace("0X", ""), NumberStyles.HexNumber, null, out uint serial))
                        entry.DestinationContainer = serial;
                };
                StackPanel.SetProportionType(destBox, ProportionType.Fill);
                destCell.Widgets.Add(destBox);
                destCell.Widgets.Add(new MyraButton("目标", () =>
                {
                    World.Instance.TargetManager.SetTargeting(targeted =>
                    {
                        if (targeted is Entity e && SerialHelper.IsItem(e))
                        {
                            entry.DestinationContainer = e.Serial;
                            destBox.Text = $"0x{e.Serial:X}";
                        }
                    });
                }) { Tooltip = "目标一个容器作为此条目的目标。" });
                grid.AddWidget(destCell, dataRow, 5);

                // Up / Down reorder buttons (col 6)
                // Display is reversed: i = entries.Count-1 is top row, i=0 is bottom row.
                // "Up" in display = swap with i+1 in list; "Down" = swap with i-1.
                var orderRow = new HorizontalStackPanel { Spacing = 2 };
                var upBtn = new MyraButton("<", () =>
                {
                    int idx = entries.IndexOf(entry);
                    if (idx < entries.Count - 1)
                    {
                        (entries[idx], entries[idx + 1]) = (entries[idx + 1], entries[idx]);
                        BuildEntriesList();
                    }
                }) { Tooltip = "上移" };
                var downBtn = new MyraButton(">", () =>
                {
                    int idx = entries.IndexOf(entry);
                    if (idx > 0)
                    {
                        (entries[idx], entries[idx - 1]) = (entries[idx - 1], entries[idx]);
                        BuildEntriesList();
                    }
                }) { Tooltip = "下移" };
                if (i == entries.Count - 1) upBtn.Enabled = false;
                if (i == 0) downBtn.Enabled = false;
                orderRow.Widgets.Add(upBtn);
                orderRow.Widgets.Add(downBtn);
                grid.AddWidget(orderRow, dataRow, 6);

                var delBtn = new MyraButton("删除", () =>
                {
                    AutoLootManager.Instance.TryRemoveAutoLootEntry(entry.Uid);
                    BuildEntriesList();
                });
                delBtn.VerticalAlignment = VerticalAlignment.Center;
                grid.AddWidget(MyraStyle.ApplyButtonDangerStyle(delBtn), dataRow, 7);

                dataRow += 1;
            }

            entriesPanel.Widgets.Add(grid);
        }

        BuildEntriesList();

        // Add entry inline panel
        var addEntryPanel = new VerticalStackPanel { Visible = false, Spacing = 4 };
        var newNameBox = new MyraInputBox { HintText = "名称", Width = 100 };
        var newGraphicBox = new MyraInputBox { HintText = "图形ID", Width = 100, Tooltip = "图形 (-1 = 任意)" };
        var newHueBox = MyraInputBox.Hue(ushort.MaxValue, 100, "色调 (-1 = 任意)");
        var newRegexBox = new MyraInputBox { HintText = "正则（可选）", Width = 200 };

        var addFieldsRow = new HorizontalStackPanel { Spacing = 4 };
        addFieldsRow.Widgets.Add(new MyraLabel("名称:", MyraLabel.TextStyle.P));
        addFieldsRow.Widgets.Add(newNameBox);
        addFieldsRow.Widgets.Add(new MyraLabel("图形:", MyraLabel.TextStyle.P));
        addFieldsRow.Widgets.Add(newGraphicBox);
        addFieldsRow.Widgets.Add(new MyraLabel("色调:", MyraLabel.TextStyle.P));
        addFieldsRow.Widgets.Add(newHueBox);
        addFieldsRow.Widgets.Add(new MyraLabel("正则:", MyraLabel.TextStyle.P));
        addFieldsRow.Widgets.Add(newRegexBox);

        var addConfirmRow = new HorizontalStackPanel { Spacing = 4 };
        addConfirmRow.Widgets.Add(new MyraButton("添加", () =>
        {
            if (StringHelper.TryParseInt(newGraphicBox.Text, out int graphic))
            {
                if (graphic > ushort.MaxValue)
                    return;

                if(graphic == -1)
                    graphic = ushort.MaxValue;

                if (!MyraInputBox.TryParseHue(newHueBox.Text, out ushort hue))
                    hue = ushort.MaxValue;

                AutoLootManager.AutoLootConfigEntry? entry = AutoLootManager.Instance.AddAutoLootEntry((ushort)graphic, hue, newNameBox.Text);
                entry.RegexSearch = newRegexBox.Text;

                newNameBox.Text = "";
                newGraphicBox.Text = "";
                newHueBox.Text = "";
                newRegexBox.Text = "";
                addEntryPanel.Visible = false;
                BuildEntriesList();
            }
        }));
        addConfirmRow.Widgets.Add(new MyraButton("取消", () =>
        {
            addEntryPanel.Visible = false;
            newGraphicBox.Text = "";
            newHueBox.Text = "";
            newRegexBox.Text = "";
        }));

        addEntryPanel.Widgets.Add(new MyraLabel("添加新条目:", MyraLabel.TextStyle.H3));
        addEntryPanel.Widgets.Add(addFieldsRow);
        addEntryPanel.Widgets.Add(addConfirmRow);

        // Import from character inline panel
        var importCharPanel = new VerticalStackPanel { Visible = false, Spacing = 4 };

        void BuildImportCharPanel()
        {
            importCharPanel.Widgets.Clear();
            Dictionary<string, List<AutoLootManager.AutoLootConfigEntry>>? otherConfigs = AutoLootManager.Instance.GetOtherCharacterConfigs();

            if (otherConfigs.Count == 0)
            {
                importCharPanel.Widgets.Add(new MyraLabel("未找到其他角色配置。", MyraLabel.TextStyle.P));
            }
            else
            {
                importCharPanel.Widgets.Add(new MyraLabel("选择要导入的角色:", MyraLabel.TextStyle.H3));
                foreach (KeyValuePair<string, List<AutoLootManager.AutoLootConfigEntry>> kv in otherConfigs.OrderBy(c => c.Key))
                {
                    string charName = kv.Key;
                    List<AutoLootManager.AutoLootConfigEntry> configs = kv.Value;
                    importCharPanel.Widgets.Add(new MyraButton($"{charName} ({configs.Count} items)", () =>
                    {
                        AutoLootManager.Instance.ImportFromOtherCharacter(charName, configs);
                        BuildEntriesList();
                        importCharPanel.Visible = false;
                    }));
                }
            }

            importCharPanel.Widgets.Add(new MyraButton("取消", () => importCharPanel.Visible = false));
        }

        // Action buttons
        var actionRow = new HorizontalStackPanel { Spacing = 6 };
        actionRow.Widgets.Add(new MyraButton("导入", () =>
        {
            string? json = Clipboard.GetClipboardText();
            if (json.NotNullNotEmpty() && AutoLootManager.Instance.ImportFromJson(json))
            {
                GameActions.Print("已导入拾取列表!", Constants.HUE_SUCCESS);
                BuildEntriesList();
                return;
            }
            GameActions.Print("您的剪贴板中没有有效的导出数据。", Constants.HUE_ERROR);
        }) { Tooltip = "从剪贴板导入（必须有有效的导出数据）。" });

        actionRow.Widgets.Add(new MyraButton("导出", () =>
        {
            AutoLootManager.Instance.GetJsonExport()?.CopyToClipboard();
            GameActions.Print("已将拾取列表导出到剪贴板!", Constants.HUE_SUCCESS);
        }) { Tooltip = "将列表导出到剪贴板。" });

        actionRow.Widgets.Add(new MyraButton("从角色导入", () =>
        {
            BuildImportCharPanel();
            importCharPanel.Visible = !importCharPanel.Visible;
        }) { Tooltip = "从另一个角色导入自动拾取配置。" });

        var addRow = new HorizontalStackPanel { Spacing = 6 };
        addRow.Widgets.Add(new MyraButton("手动添加条目", () => addEntryPanel.Visible = !addEntryPanel.Visible));
        addRow.Widgets.Add(new MyraButton("从目标添加", () =>
        {
            World.Instance.TargetManager.SetTargeting(targeted =>
            {
                if (targeted is Entity entity && SerialHelper.IsItem(entity))
                {
                    AutoLootManager.Instance.AddAutoLootEntry(entity.Graphic, entity.Hue, entity.Name);
                    BuildEntriesList();
                }
            });
        }) { Tooltip = "目标一个物品以将其添加到拾取列表。" });

        root.Widgets.Add(actionRow);
        root.Widgets.Add(addRow);
        root.Widgets.Add(addEntryPanel);
        root.Widgets.Add(importCharPanel);
        root.Widgets.Add(new ScrollViewer { MaxHeight = 300, Content = entriesPanel });

        return root;
    }
}
