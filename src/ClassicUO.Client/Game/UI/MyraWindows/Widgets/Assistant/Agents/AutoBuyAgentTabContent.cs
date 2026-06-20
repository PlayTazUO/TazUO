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
            return new MyraLabel(TazLang.Get("autobuy_profilenotloaded"), MyraLabel.TextStyle.P);

        var root = new VerticalStackPanel { Spacing = 6 };

        root.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.BuyAgentEnabled, b => profile.BuyAgentEnabled = b, TazLang.Get("autobuy_enable")));

        root.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.BuyAgentSubContainers, b => profile.BuyAgentSubContainers = b,
            TazLang.Get("autobuy_subcontainers"),
            TazLang.Get("autobuy_subcontainers_tooltip")));

        root.Widgets.Add(new MyraLabel(TazLang.Get("autobuy_options"), MyraLabel.TextStyle.H3));
        root.Widgets.Add(LabeledHorizontalSlider.SliderWithLabel(
            TazLang.Get("autobuy_maxitems"),
            out _,
            v => profile.BuyAgentMaxItems = (int)v,
            0, 1000,
            profile.BuyAgentMaxItems));
        root.Widgets.Add(LabeledHorizontalSlider.SliderWithLabel(
            TazLang.Get("autobuy_maxuniques"),
            out _,
            v => profile.BuyAgentMaxUniques = (int)v,
            0, 100,
            profile.BuyAgentMaxUniques));

        root.Widgets.Add(new MyraLabel(TazLang.Get("autobuy_entries"), MyraLabel.TextStyle.H3));

        var entriesPanel = new VerticalStackPanel { Spacing = 4 };

        void BuildEntriesList()
        {
            entriesPanel.Widgets.Clear();
            List<BuySellItemConfig> entries = BuySellAgent.Instance?.BuyConfigs ?? new List<BuySellItemConfig>();

            if (entries.Count == 0)
            {
                entriesPanel.Widgets.Add(new MyraLabel(TazLang.Get("autobuy_noentries"), MyraLabel.TextStyle.H3));
                return;
            }

            var grid = new MyraGrid();
            grid.SetupWithHeaders(
                GridColumnInfo.Auto(TazLang.Get("agent_col_art")),
                GridColumnInfo.Fill(TazLang.Get("agent_col_graphic")),
                GridColumnInfo.Fill(TazLang.Get("agent_col_hue")),
                GridColumnInfo.Fill(TazLang.Get("agent_col_maxamount")),
                GridColumnInfo.Fill(TazLang.Get("autobuy_col_restockupto")),
                GridColumnInfo.Fill(TazLang.Get("autobuy_col_maxprice")),
                GridColumnInfo.Auto(TazLang.Get("agent_col_enabled")),
                GridColumnInfo.Auto(TazLang.Get("agent_col_actions"))
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
                    Tooltip = TazLang.Get("agent_maxamount_tooltip"),
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
                    Tooltip = TazLang.Get("autobuy_restockupto_tooltip"),
                };
                restockBox.TextChangedByUser += (_, _) =>
                {
                    if (ushort.TryParse(restockBox.Text, out ushort r)) entry.RestockUpTo = r;
                };
                grid.AddWidget(restockBox, dataRow, 4);

                var maxPriceBox = new MyraInputBox
                {
                    Text = entry.MaxPrice.ToString(),
                    Tooltip = TazLang.Get("autobuy_maxprice_tooltip"),
                };
                maxPriceBox.TextChangedByUser += (_, _) =>
                {
                    if (uint.TryParse(maxPriceBox.Text, out uint mp)) entry.MaxPrice = mp;
                };
                grid.AddWidget(maxPriceBox, dataRow, 5);

                var cb = MyraCheckButton.CreateWithCallback(entry.Enabled, b => entry.Enabled = b);
                cb.HorizontalAlignment = HorizontalAlignment.Center;
                grid.AddWidget(cb, dataRow, 6);

                grid.AddWidget(MyraStyle.ApplyButtonDangerStyle(new MyraButton(TazLang.Get("agent_delete"), () =>
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
        addFieldsRow1.Widgets.Add(new MyraLabel(TazLang.Get("agent_graphic_label"), MyraLabel.TextStyle.P));
        addFieldsRow1.Widgets.Add(newGraphicBox);
        addFieldsRow1.Widgets.Add(new MyraLabel(TazLang.Get("agent_hue_label"), MyraLabel.TextStyle.P));
        addFieldsRow1.Widgets.Add(newHueBox);

        var addFieldsRow2 = new HorizontalStackPanel { Spacing = 4 };
        addFieldsRow2.Widgets.Add(new MyraLabel(TazLang.Get("agent_maxamount_label"), MyraLabel.TextStyle.P));
        addFieldsRow2.Widgets.Add(newMaxAmountBox);
        addFieldsRow2.Widgets.Add(new MyraLabel(TazLang.Get("autobuy_restockupto_label"), MyraLabel.TextStyle.P));
        addFieldsRow2.Widgets.Add(newRestockBox);
        addFieldsRow2.Widgets.Add(new MyraLabel(TazLang.Get("autobuy_maxprice_label"), MyraLabel.TextStyle.P));
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
        addConfirmRow.Widgets.Add(new MyraButton(TazLang.Get("agent_add"), () =>
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
        addConfirmRow.Widgets.Add(new MyraButton(TazLang.Get("agent_cancel"), () =>
        {
            addEntryPanel.Visible = false;
            ClearAddFields();
        }));

        addEntryPanel.Widgets.Add(new MyraLabel(TazLang.Get("agent_addnewentry"), MyraLabel.TextStyle.H3));
        addEntryPanel.Widgets.Add(addFieldsRow1);
        addEntryPanel.Widgets.Add(addFieldsRow2);
        addEntryPanel.Widgets.Add(addConfirmRow);

        // Action buttons
        var actionRow = new HorizontalStackPanel { Spacing = 6 };
        actionRow.Widgets.Add(new MyraButton(TazLang.Get("agent_addmanualentry"), () => addEntryPanel.Visible = !addEntryPanel.Visible));
        actionRow.Widgets.Add(new MyraButton(TazLang.Get("agent_addfromtarget"), () =>
        {
            GameActions.Print(Client.Game.UO.World, TazLang.Get("autobuy_targetprompt"));
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
        }) { Tooltip = TazLang.Get("autobuy_addfromtarget_tooltip") });
        actionRow.Widgets.Add(new MyraButton(TazLang.Get("agent_import"), () =>
        {
            string? json = Clipboard.GetClipboardText();
            if (json.NotNullNotEmpty() && BuySellAgent.ImportFromJson(json, AgentType.Buy))
            {
                GameActions.Print(TazLang.Get("autobuy_imported"), Constants.HUE_SUCCESS);
                BuildEntriesList();
                return;
            }
            GameActions.Print(TazLang.Get("agent_invalidimport"), Constants.HUE_ERROR);
        }) { Tooltip = TazLang.Get("agent_import_tooltip") });
        actionRow.Widgets.Add(new MyraButton(TazLang.Get("agent_export"), () =>
        {
            BuySellAgent.GetJsonExport(AgentType.Buy)?.CopyToClipboard();
            GameActions.Print(TazLang.Get("autobuy_exported"), Constants.HUE_SUCCESS);
        }) { Tooltip = TazLang.Get("agent_export_tooltip") });

        root.Widgets.Add(actionRow);
        root.Widgets.Add(addEntryPanel);
        root.Widgets.Add(new ScrollViewer { MaxHeight = 300, Content = entriesPanel });

        return root;
    }
}
