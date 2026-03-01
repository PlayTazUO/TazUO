#nullable enable
using System.Collections.Generic;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.Utility;
using Myra.Graphics2D.UI;
using TextBox = Myra.Graphics2D.UI.TextBox;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Assistant.Agents;

public static class AutoSellAgentTabContent
{
    public static Widget Build()
    {
        Profile? profile = ProfileManager.CurrentProfile;
        if (profile == null)
            return new MyraLabel("Profile not loaded", MyraLabel.Style.P);

        var root = new VerticalStackPanel { Spacing = 6 };

        root.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.SellAgentEnabled, b => profile.SellAgentEnabled = b, "Enable Auto Sell"));

        root.Widgets.Add(new MyraLabel("Options:", MyraLabel.Style.H3));
        root.Widgets.Add(MyraHSlider.SliderWithLabel(
            "Max total items",
            out _,
            v => profile.SellAgentMaxItems = (int)v,
            0, 1000,
            profile.SellAgentMaxItems));
        root.Widgets.Add(MyraHSlider.SliderWithLabel(
            "Max unique items",
            out _,
            v => profile.SellAgentMaxUniques = (int)v,
            0, 100,
            profile.SellAgentMaxUniques));

        root.Widgets.Add(new MyraLabel("Entries:", MyraLabel.Style.H3));

        var entriesPanel = new VerticalStackPanel { Spacing = 4 };

        void BuildEntriesList()
        {
            entriesPanel.Widgets.Clear();
            List<BuySellItemConfig> entries = BuySellAgent.Instance?.SellConfigs ?? new List<BuySellItemConfig>();

            if (entries.Count == 0)
            {
                entriesPanel.Widgets.Add(new MyraLabel("No entries configured.", MyraLabel.Style.P));
                return;
            }

            var grid = new MyraGrid();
            grid.AddColumn(null, 7);
            MyraStyle.ApplyStandardGridStyling(grid);

            grid.AddWidget(new MyraLabel("Art", MyraLabel.Style.H3), 0, 0);
            grid.AddWidget(new MyraLabel("Graphic", MyraLabel.Style.H3), 0, 1);
            grid.AddWidget(new MyraLabel("Hue", MyraLabel.Style.H3), 0, 2);
            grid.AddWidget(new MyraLabel("Max Amount", MyraLabel.Style.H3), 0, 3);
            grid.AddWidget(new MyraLabel("Min on Hand", MyraLabel.Style.H3), 0, 4);
            grid.AddWidget(new MyraLabel("Enabled", MyraLabel.Style.H3), 0, 5);
            grid.AddWidget(new MyraLabel("Actions", MyraLabel.Style.H3), 0, 6);

            int dataRow = 1;
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                BuySellItemConfig entry = entries[i];

                if (entry.Graphic > 0)
                    grid.AddWidget(new MyraArtTexture((uint)entry.Graphic), dataRow, 0);

                var graphicBox = new TextBox { Text = entry.Graphic.ToString(), Width = 60 };
                graphicBox.TextChangedByUser += (_, _) =>
                {
                    if (StringHelper.TryParseInt(graphicBox.Text, out int g) && g is > 0 and <= ushort.MaxValue)
                        entry.Graphic = (ushort)g;
                };
                grid.AddWidget(graphicBox, dataRow, 1);

                var hueBox = new TextBox
                {
                    Text = entry.Hue == ushort.MaxValue ? "-1" : entry.Hue.ToString(),
                    Width = 50,
                    Tooltip = "Set to -1 to match any hue."
                };
                hueBox.TextChangedByUser += (_, _) =>
                {
                    if (hueBox.Text == "-1") entry.Hue = ushort.MaxValue;
                    else if (ushort.TryParse(hueBox.Text, out ushort h)) entry.Hue = h;
                };
                grid.AddWidget(hueBox, dataRow, 2);

                var maxAmountBox = new TextBox
                {
                    Text = entry.MaxAmount == ushort.MaxValue ? "0" : entry.MaxAmount.ToString(),
                    Width = 60,
                    Tooltip = "Set to 0 for unlimited."
                };
                maxAmountBox.TextChangedByUser += (_, _) =>
                {
                    if (ushort.TryParse(maxAmountBox.Text, out ushort ma))
                        entry.MaxAmount = ma == 0 ? ushort.MaxValue : ma;
                };
                grid.AddWidget(maxAmountBox, dataRow, 3);

                var restockBox = new TextBox
                {
                    Text = entry.RestockUpTo.ToString(),
                    Width = 60,
                    Tooltip = "Minimum amount to keep on hand (0 = disabled)."
                };
                restockBox.TextChangedByUser += (_, _) =>
                {
                    if (ushort.TryParse(restockBox.Text, out ushort r)) entry.RestockUpTo = r;
                };
                grid.AddWidget(restockBox, dataRow, 4);

                grid.AddWidget(MyraCheckButton.CreateWithCallback(entry.Enabled, b => entry.Enabled = b), dataRow, 5);

                grid.AddWidget(MyraStyle.ApplyButtonDangerStyle(new MyraButton("Delete", () =>
                {
                    BuySellAgent.Instance?.DeleteConfig(entry);
                    BuildEntriesList();
                })), dataRow, 6);

                dataRow++;
            }

            entriesPanel.Widgets.Add(grid);
        }

        BuildEntriesList();

        // Inline add entry panel
        var addEntryPanel = new VerticalStackPanel { Visible = false, Spacing = 4 };
        var newGraphicBox = new TextBox { HintText = "Graphic ID", Width = 80 };
        var newHueBox = new TextBox { HintText = "Hue (-1=any)", Width = 80 };
        var newMaxAmountBox = new TextBox { HintText = "Max Amount (0=unlimited)", Width = 130 };
        var newRestockBox = new TextBox { HintText = "Min on Hand (0=disabled)", Width = 130 };

        var addFieldsRow1 = new HorizontalStackPanel { Spacing = 4 };
        addFieldsRow1.Widgets.Add(new MyraLabel("Graphic:", MyraLabel.Style.P));
        addFieldsRow1.Widgets.Add(newGraphicBox);
        addFieldsRow1.Widgets.Add(new MyraLabel("Hue:", MyraLabel.Style.P));
        addFieldsRow1.Widgets.Add(newHueBox);

        var addFieldsRow2 = new HorizontalStackPanel { Spacing = 4 };
        addFieldsRow2.Widgets.Add(new MyraLabel("Max Amount:", MyraLabel.Style.P));
        addFieldsRow2.Widgets.Add(newMaxAmountBox);
        addFieldsRow2.Widgets.Add(new MyraLabel("Min on Hand:", MyraLabel.Style.P));
        addFieldsRow2.Widgets.Add(newRestockBox);

        void ClearAddFields()
        {
            newGraphicBox.Text = "";
            newHueBox.Text = "";
            newMaxAmountBox.Text = "";
            newRestockBox.Text = "";
        }

        var addConfirmRow = new HorizontalStackPanel { Spacing = 4 };
        addConfirmRow.Widgets.Add(new MyraButton("Add", () =>
        {
            if (StringHelper.TryParseInt(newGraphicBox.Text, out int graphic))
            {
                BuySellItemConfig newConfig = BuySellAgent.Instance.NewSellConfig();
                newConfig.Graphic = (ushort)graphic;

                if (!string.IsNullOrEmpty(newHueBox.Text) && newHueBox.Text != "-1")
                {
                    if (ushort.TryParse(newHueBox.Text, out ushort hue)) newConfig.Hue = hue;
                }
                else
                    newConfig.Hue = ushort.MaxValue;

                if (!string.IsNullOrEmpty(newMaxAmountBox.Text) && ushort.TryParse(newMaxAmountBox.Text, out ushort maxAmount))
                    newConfig.MaxAmount = maxAmount == 0 ? ushort.MaxValue : maxAmount;

                if (!string.IsNullOrEmpty(newRestockBox.Text) && ushort.TryParse(newRestockBox.Text, out ushort restock))
                    newConfig.RestockUpTo = restock;

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

        addEntryPanel.Widgets.Add(new MyraLabel("Add New Entry:", MyraLabel.Style.H3));
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
                    BuySellItemConfig newConfig = BuySellAgent.Instance.NewSellConfig();
                    newConfig.Graphic = entity.Graphic;
                    newConfig.Hue = entity.Hue;
                    BuildEntriesList();
                }
            });
        }) { Tooltip = "Target an item to add it to the sell list." });
        actionRow.Widgets.Add(new MyraButton("Import", () =>
        {
            string? json = Clipboard.GetClipboardText();
            if (json.NotNullNotEmpty() && BuySellAgent.ImportFromJson(json, AgentType.Sell))
            {
                GameActions.Print("Imported sell list!", Constants.HUE_SUCCESS);
                BuildEntriesList();
                return;
            }
            GameActions.Print("Your clipboard does not have a valid export copied.", Constants.HUE_ERROR);
        }) { Tooltip = "Import from clipboard (must have a valid export copied)." });
        actionRow.Widgets.Add(new MyraButton("Export", () =>
        {
            BuySellAgent.GetJsonExport(AgentType.Sell)?.CopyToClipboard();
            GameActions.Print("Exported sell list to your clipboard!", Constants.HUE_SUCCESS);
        }) { Tooltip = "Export your list to clipboard." });

        root.Widgets.Add(actionRow);
        root.Widgets.Add(addEntryPanel);
        root.Widgets.Add(new ScrollViewer { MaxHeight = 300, Content = entriesPanel });

        return root;
    }
}
