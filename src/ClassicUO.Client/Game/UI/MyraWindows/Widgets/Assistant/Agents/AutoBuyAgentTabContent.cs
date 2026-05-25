#nullable enable
using System.Collections.Generic;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Utility;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Assistant.Agents;

public static class AutoBuyAgentTabContent
{
    public static Widget Build()
    {
        Profile? profile = ProfileManager.CurrentProfile;
        if (profile == null)
            return new MyraLabel("未加载配置文件", MyraLabel.TextStyle.P);

        var root = new VerticalStackPanel { Spacing = 6 };

        root.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.BuyAgentEnabled, b => profile.BuyAgentEnabled = b, "启用自动购买"));

        root.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.BuyAgentSubContainers, b => profile.BuyAgentSubContainers = b, "包含子容器？",
            "这也会计算背包内容器中的物品（尚未打开的容器可能没有准确的内容物计数）。"));

        root.Widgets.Add(new MyraLabel("选项:", MyraLabel.TextStyle.H3));
        root.Widgets.Add(MyraHSlider.SliderWithLabel(
            "最大总物品数",
            out _,
            v => profile.BuyAgentMaxItems = (int)v,
            0, 1000,
            profile.BuyAgentMaxItems));
        root.Widgets.Add(MyraHSlider.SliderWithLabel(
            "最大独特物品数",
            out _,
            v => profile.BuyAgentMaxUniques = (int)v,
            0, 100,
            profile.BuyAgentMaxUniques));

        root.Widgets.Add(new MyraLabel("条目:", MyraLabel.TextStyle.H3));

        var entriesPanel = new VerticalStackPanel { Spacing = 4 };

        void BuildEntriesList()
        {
            entriesPanel.Widgets.Clear();
            List<BuySellItemConfig> entries = BuySellAgent.Instance?.BuyConfigs ?? new List<BuySellItemConfig>();

            if (entries.Count == 0)
            {
                entriesPanel.Widgets.Add(new MyraLabel("没有配置条目。", MyraLabel.TextStyle.H3));
                return;
            }

            var grid = new MyraGrid();
            grid.SetupWithHeaders(
                GridColumnInfo.Auto("图形"),
                GridColumnInfo.Fill("图形ID"),
                GridColumnInfo.Fill("色调"),
                GridColumnInfo.Fill("最大数量"),
                GridColumnInfo.Fill("补货至"),
                GridColumnInfo.Fill("最高价格"),
                GridColumnInfo.Auto("启用"),
                GridColumnInfo.Auto("操作")
            );

            int dataRow = 1;
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                BuySellItemConfig entry = entries[i];

                if (entry.Graphic > 0)
                    grid.AddWidget(new MyraArtTexture((uint)entry.Graphic), dataRow, 0);

                var graphicBox = new MyraInputBox { Text = entry.Graphic.ToString() };
                graphicBox.TextChangedByUser += (_, _) =>
                {
                    if (StringHelper.TryParseInt(graphicBox.Text, out int g) && g is > 0 and <= ushort.MaxValue)
                        entry.Graphic = (ushort)g;
                };
                grid.AddWidget(graphicBox, dataRow, 1);

                var hueBox = MyraInputBox.Hue(entry.Hue);
                hueBox.Width = null;
                hueBox.TextChangedByUser += (_, _) =>
                {
                    if (MyraInputBox.TryParseHue(hueBox.Text, out ushort hue))
                        entry.Hue = hue;
                };
                grid.AddWidget(hueBox, dataRow, 2);

                var maxAmountBox = new MyraInputBox
                {
                    Text = entry.MaxAmount == ushort.MaxValue ? "0" : entry.MaxAmount.ToString(),
                    Tooltip = "设为0表示无限制。",
                };
                maxAmountBox.TextChangedByUser += (_, _) =>
                {
                    if (ushort.TryParse(maxAmountBox.Text, out ushort ma))
                        entry.MaxAmount = ma == 0 ? ushort.MaxValue : ma;
                };
                grid.AddWidget(maxAmountBox, dataRow, 3);

                var restockBox = new MyraInputBox
                {
                    Text = entry.RestockUpTo.ToString(),
                    Tooltip = "购买时补货至的数量（0 = 禁用）。",
                };
                restockBox.TextChangedByUser += (_, _) =>
                {
                    if (ushort.TryParse(restockBox.Text, out ushort r)) entry.RestockUpTo = r;
                };
                grid.AddWidget(restockBox, dataRow, 4);

                var maxPriceBox = new MyraInputBox
                {
                    Text = entry.MaxPrice.ToString(),
                    Tooltip = "每个物品的最高价格（0 = 无限制）。",
                };
                maxPriceBox.TextChangedByUser += (_, _) =>
                {
                    if (uint.TryParse(maxPriceBox.Text, out uint mp)) entry.MaxPrice = mp;
                };
                grid.AddWidget(maxPriceBox, dataRow, 5);

                var cb = MyraCheckButton.CreateWithCallback(entry.Enabled, b => entry.Enabled = b);
                cb.HorizontalAlignment = HorizontalAlignment.Center;
                grid.AddWidget(cb, dataRow, 6);

                grid.AddWidget(MyraStyle.ApplyButtonDangerStyle(new MyraButton("删除", () =>
                {
                    BuySellAgent.Instance?.DeleteConfig(entry);
                    BuildEntriesList();
                })), dataRow, 7);

                dataRow++;
            }

            entriesPanel.Widgets.Add(grid);
        }

        BuildEntriesList();

        // Inline add entry panel
        var addEntryPanel = new VerticalStackPanel { Visible = false, Spacing = 4 };
        var newGraphicBox = new MyraInputBox { HintText = "图形ID", Width = 80 };
        var newHueBox = MyraInputBox.Hue(ushort.MaxValue, 80, "色调 (-1=任意)");
        var newMaxAmountBox = new MyraInputBox { HintText = "最大数量 (0=无限制)", Width = 130 };
        var newRestockBox = new MyraInputBox { HintText = "补货至", Width = 100 };
        var newMaxPriceBox = new MyraInputBox { HintText = "最高价格 (0=无限制)", Width = 110 };

        var addFieldsRow1 = new HorizontalStackPanel { Spacing = 4 };
        addFieldsRow1.Widgets.Add(new MyraLabel("图形:", MyraLabel.TextStyle.P));
        addFieldsRow1.Widgets.Add(newGraphicBox);
        addFieldsRow1.Widgets.Add(new MyraLabel("色调:", MyraLabel.TextStyle.P));
        addFieldsRow1.Widgets.Add(newHueBox);

        var addFieldsRow2 = new HorizontalStackPanel { Spacing = 4 };
        addFieldsRow2.Widgets.Add(new MyraLabel("最大数量:", MyraLabel.TextStyle.P));
        addFieldsRow2.Widgets.Add(newMaxAmountBox);
        addFieldsRow2.Widgets.Add(new MyraLabel("补货至:", MyraLabel.TextStyle.P));
        addFieldsRow2.Widgets.Add(newRestockBox);
        addFieldsRow2.Widgets.Add(new MyraLabel("最高价格:", MyraLabel.TextStyle.P));
        addFieldsRow2.Widgets.Add(newMaxPriceBox);

        void ClearAddFields()
        {
            newGraphicBox.Text = "";
            newHueBox.Text = "";
            newMaxAmountBox.Text = "";
            newRestockBox.Text = "";
            newMaxPriceBox.Text = "";
        }

        var addConfirmRow = new HorizontalStackPanel { Spacing = 4 };
        addConfirmRow.Widgets.Add(new MyraButton("添加", () =>
        {
            if (StringHelper.TryParseInt(newGraphicBox.Text, out int graphic))
            {
                BuySellItemConfig newConfig = BuySellAgent.Instance.NewBuyConfig();
                newConfig.Graphic = (ushort)graphic;

                if (MyraInputBox.TryParseHue(newHueBox.Text, out ushort hue))
                    newConfig.Hue = hue;
                else
                    newConfig.Hue = ushort.MaxValue;

                if (!string.IsNullOrEmpty(newMaxAmountBox.Text) && ushort.TryParse(newMaxAmountBox.Text, out ushort maxAmount))
                    newConfig.MaxAmount = maxAmount == 0 ? ushort.MaxValue : maxAmount;

                if (!string.IsNullOrEmpty(newRestockBox.Text) && ushort.TryParse(newRestockBox.Text, out ushort restock))
                    newConfig.RestockUpTo = restock;

                if (!string.IsNullOrEmpty(newMaxPriceBox.Text) && uint.TryParse(newMaxPriceBox.Text, out uint maxPrice))
                    newConfig.MaxPrice = maxPrice;

                ClearAddFields();
                addEntryPanel.Visible = false;
                BuildEntriesList();
            }
        }));
        addConfirmRow.Widgets.Add(new MyraButton("取消", () =>
        {
            addEntryPanel.Visible = false;
            ClearAddFields();
        }));

        addEntryPanel.Widgets.Add(new MyraLabel("添加新条目:", MyraLabel.TextStyle.H3));
        addEntryPanel.Widgets.Add(addFieldsRow1);
        addEntryPanel.Widgets.Add(addFieldsRow2);
        addEntryPanel.Widgets.Add(addConfirmRow);

        // Action buttons
        var actionRow = new HorizontalStackPanel { Spacing = 6 };
        actionRow.Widgets.Add(new MyraButton("手动添加条目", () => addEntryPanel.Visible = !addEntryPanel.Visible));
        actionRow.Widgets.Add(new MyraButton("从目标添加", () =>
        {
            GameActions.Print(Client.Game.UO.World, "目标物品以添加");
            World.Instance.TargetManager.SetTargeting(targeted =>
            {
                if (targeted is Entity entity && SerialHelper.IsItem(entity))
                {
                    BuySellItemConfig newConfig = BuySellAgent.Instance.NewBuyConfig();
                    newConfig.Graphic = entity.Graphic;
                    newConfig.Hue = entity.Hue;
                    BuildEntriesList();
                }
            });
        }) { Tooltip = "目标一个物品以将其添加到购买列表。" });
        actionRow.Widgets.Add(new MyraButton("导入", () =>
        {
            string? json = Clipboard.GetClipboardText();
            if (json.NotNullNotEmpty() && BuySellAgent.ImportFromJson(json, AgentType.Buy))
            {
                GameActions.Print("已导入购买列表!", Constants.HUE_SUCCESS);
                BuildEntriesList();
                return;
            }
            GameActions.Print("您的剪贴板中没有有效的导出数据。", Constants.HUE_ERROR);
        }) { Tooltip = "从剪贴板导入（必须有有效的导出数据）。" });
        actionRow.Widgets.Add(new MyraButton("导出", () =>
        {
            BuySellAgent.GetJsonExport(AgentType.Buy)?.CopyToClipboard();
            GameActions.Print("已将购买列表导出到剪贴板!", Constants.HUE_SUCCESS);
        }) { Tooltip = "将列表导出到剪贴板。" });

        root.Widgets.Add(actionRow);
        root.Widgets.Add(addEntryPanel);
        root.Widgets.Add(new ScrollViewer { MaxHeight = 300, Content = entriesPanel });

        return root;
    }
}
