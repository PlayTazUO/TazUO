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
            return new MyraLabel("Profile not loaded", MyraLabel.TextStyle.P);

        var root = new VerticalStackPanel { Spacing = 6 };

        root.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.BuyAgentEnabled, b => profile.BuyAgentEnabled = b, "Enable Auto Buy"));

        root.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.BuyAgentSubContainers, b => profile.BuyAgentSubContainers = b, "Include sub containers?",
            "This will also count items inside containers in your backpack (Containers that have not been opened yet may not have an accurate count of contents)."));

        root.Widgets.Add(new MyraLabel("Options:", MyraLabel.TextStyle.H3));
        root.Widgets.Add(MyraHSlider.SliderWithLabel(
            "Max total items",
            out _,
            v => profile.BuyAgentMaxItems = (int)v,
            0, 1000,
            profile.BuyAgentMaxItems));
        root.Widgets.Add(MyraHSlider.SliderWithLabel(
            "Max unique items",
            out _,
            v => profile.BuyAgentMaxUniques = (int)v,
            0, 100,
            profile.BuyAgentMaxUniques));

        root.Widgets.Add(new MyraLabel("Entries:", MyraLabel.TextStyle.H3));

        var entriesPanel = new VerticalStackPanel { Spacing = 4 };

        void BuildEntriesList()
        {
            entriesPanel.Widgets.Clear();
            List<BuySellItemConfig> entries = BuySellAgent.Instance?.BuyConfigs ?? new List<BuySellItemConfig>();

            if (entries.Count == 0)
            {
                entriesPanel.Widgets.Add(new MyraLabel("No entries configured.", MyraLabel.TextStyle.H3));
                return;
            }

            var grid = new MyraGrid();
            grid.SetupWithHeaders(
                GridColumnInfo.Auto("Art"),
                GridColumnInfo.Fill("Graphic"),
                GridColumnInfo.Fill("Hue"),
                GridColumnInfo.Fill("Max Amount"),
                GridColumnInfo.Fill("Restock Up To"),
                GridColumnInfo.Fill("Max Price"),
                GridColumnInfo.Auto("Enabled"),
                GridColumnInfo.Auto("Actions")
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
                    Tooltip = "Set to 0 for unlimited.",
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
                    Tooltip = "Amount to restock up to when buying (0 = disabled).",
                };
                restockBox.TextChangedByUser += (_, _) =>
                {
                    if (ushort.TryParse(restockBox.Text, out ushort r)) entry.RestockUpTo = r;
                };
                grid.AddWidget(restockBox, dataRow, 4);

                var maxPriceBox = new MyraInputBox
                {
                    Text = entry.MaxPrice.ToString(),
                    Tooltip = "Maximum price per item (0 = no limit).",
                };
                maxPriceBox.TextChangedByUser += (_, _) =>
                {
                    if (uint.TryParse(maxPriceBox.Text, out uint mp)) entry.MaxPrice = mp;
                };
                grid.AddWidget(maxPriceBox, dataRow, 5);

                var cb = MyraCheckButton.CreateWithCallback(entry.Enabled, b => entry.Enabled = b);
                cb.HorizontalAlignment = HorizontalAlignment.Center;
                grid.AddWidget(cb, dataRow, 6);

                grid.AddWidget(MyraStyle.ApplyButtonDangerStyle(new MyraButton("Delete", () =>
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
        var newGraphicBox = new MyraInputBox { HintText = "Graphic ID", Width = 80 };
        var newHueBox = MyraInputBox.Hue(ushort.MaxValue, 80, "Hue (-1=any)");
        var newMaxAmountBox = new MyraInputBox { HintText = "Max Amount (0=unlimited)", Width = 130 };
        var newRestockBox = new MyraInputBox { HintText = "Restock Up To", Width = 100 };
        var newMaxPriceBox = new MyraInputBox { HintText = "Max Price (0=no limit)", Width = 110 };

        var addFieldsRow1 = new HorizontalStackPanel { Spacing = 4 };
        addFieldsRow1.Widgets.Add(new MyraLabel("Graphic:", MyraLabel.TextStyle.P));
        addFieldsRow1.Widgets.Add(newGraphicBox);
        addFieldsRow1.Widgets.Add(new MyraLabel("Hue:", MyraLabel.TextStyle.P));
        addFieldsRow1.Widgets.Add(newHueBox);

        var addFieldsRow2 = new HorizontalStackPanel { Spacing = 4 };
        addFieldsRow2.Widgets.Add(new MyraLabel("Max Amount:", MyraLabel.TextStyle.P));
        addFieldsRow2.Widgets.Add(newMaxAmountBox);
        addFieldsRow2.Widgets.Add(new MyraLabel("Restock Up To:", MyraLabel.TextStyle.P));
        addFieldsRow2.Widgets.Add(newRestockBox);
        addFieldsRow2.Widgets.Add(new MyraLabel("Max Price:", MyraLabel.TextStyle.P));
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
        addConfirmRow.Widgets.Add(new MyraButton("Add", () =>
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
        addConfirmRow.Widgets.Add(new MyraButton("Cancel", () =>
        {
            addEntryPanel.Visible = false;
            ClearAddFields();
        }));

        addEntryPanel.Widgets.Add(new MyraLabel("Add New Entry:", MyraLabel.TextStyle.H3));
        addEntryPanel.Widgets.Add(addFieldsRow1);
        addEntryPanel.Widgets.Add(addFieldsRow2);
        addEntryPanel.Widgets.Add(addConfirmRow);

        // Action buttons
        var actionRow = new HorizontalStackPanel { Spacing = 6 };
        actionRow.Widgets.Add(new MyraButton("Add Manual Entry", () => addEntryPanel.Visible = !addEntryPanel.Visible));
        actionRow.Widgets.Add(new MyraButton("Add from Target", () =>
        {
            GameActions.Print(Client.Game.UO.World, "Target item to add");
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
        }) { Tooltip = "Target an item to add it to the buy list." });
        actionRow.Widgets.Add(new MyraButton("Import", () =>
        {
            string? json = Clipboard.GetClipboardText();
            if (json.NotNullNotEmpty() && BuySellAgent.ImportFromJson(json, AgentType.Buy))
            {
                GameActions.Print("Imported buy list!", Constants.HUE_SUCCESS);
                BuildEntriesList();
                return;
            }
            GameActions.Print("Your clipboard does not have a valid export copied.", Constants.HUE_ERROR);
        }) { Tooltip = "Import from clipboard (must have a valid export copied)." });
        actionRow.Widgets.Add(new MyraButton("Export", () =>
        {
            BuySellAgent.GetJsonExport(AgentType.Buy)?.CopyToClipboard();
            GameActions.Print("Exported buy list to your clipboard!", Constants.HUE_SUCCESS);
        }) { Tooltip = "Export your list to clipboard." });

        root.Widgets.Add(actionRow);
        root.Widgets.Add(addEntryPanel);
        root.Widgets.Add(new ScrollViewer { MaxHeight = 300, Content = entriesPanel });

        return root;
    }
}
