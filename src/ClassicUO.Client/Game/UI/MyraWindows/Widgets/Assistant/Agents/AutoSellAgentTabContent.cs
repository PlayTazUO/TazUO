#nullable enable
using System.Collections.Generic;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Utility;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Assistant.Agents;

public static class AutoSellAgentTabContent
{
    public static Widget Build()
    {
        Profile? profile = ProfileManager.CurrentProfile;
        if (profile == null)
            return new MyraLabel(TazLang.Get("autosell_profilenotloaded"), MyraLabel.TextStyle.P);

        var root = new VerticalStackPanel { Spacing = 6 };

        root.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.SellAgentEnabled, b => profile.SellAgentEnabled = b, TazLang.Get("autosell_enable")));

        root.Widgets.Add(new MyraLabel(TazLang.Get("autosell_options"), MyraLabel.TextStyle.H3));
        root.Widgets.Add(MyraHSlider.SliderWithLabel(
            TazLang.Get("autosell_maxitems"),
            out _,
            v => profile.SellAgentMaxItems = (int)v,
            0, 1000,
            profile.SellAgentMaxItems));
        root.Widgets.Add(MyraHSlider.SliderWithLabel(
            TazLang.Get("autosell_maxuniques"),
            out _,
            v => profile.SellAgentMaxUniques = (int)v,
            0, 100,
            profile.SellAgentMaxUniques));

        root.Widgets.Add(new MyraLabel(TazLang.Get("autosell_entries"), MyraLabel.TextStyle.H3));

        var entriesPanel = new VerticalStackPanel { Spacing = 4 };

        void BuildEntriesList()
        {
            entriesPanel.Widgets.Clear();
            List<BuySellItemConfig> entries = BuySellAgent.Instance?.SellConfigs ?? new List<BuySellItemConfig>();

            if (entries.Count == 0)
            {
                entriesPanel.Widgets.Add(new MyraLabel(TazLang.Get("autosell_noentries"), MyraLabel.TextStyle.H3));
                return;
            }

            var grid = new MyraGrid();
            grid.SetupWithHeaders(
                GridColumnInfo.Auto(TazLang.Get("agent_col_art")),
                GridColumnInfo.Fill(TazLang.Get("agent_col_graphic")),
                GridColumnInfo.Fill(TazLang.Get("agent_col_hue")),
                GridColumnInfo.Fill(TazLang.Get("agent_col_maxamount")),
                GridColumnInfo.Fill(TazLang.Get("autosell_col_minonhand")),
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
                    Tooltip = TazLang.Get("autosell_minonhand_tooltip"),
                };
                restockBox.TextChangedByUser += (_, _) =>
                {
                    if (ushort.TryParse(restockBox.Text, out ushort r)) entry.RestockUpTo = r;
                };
                grid.AddWidget(restockBox, dataRow, 4);

                var cb = MyraCheckButton.CreateWithCallback(entry.Enabled, b => entry.Enabled = b);
                cb.HorizontalAlignment = HorizontalAlignment.Center;
                grid.AddWidget(cb, dataRow, 5);

                grid.AddWidget(MyraStyle.ApplyButtonDangerStyle(new MyraButton(TazLang.Get("agent_delete"), () =>
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
        var newGraphicBox = new MyraInputBox { HintText= "Graphic ID", Width= 80 };
        var newHueBox = MyraInputBox.Hue(ushort.MaxValue, 80, "Hue (-1=any)");
        var newMaxAmountBox = new MyraInputBox { HintText = "Max Amount (0=unlimited)", Width = 130 };
        var newRestockBox = new MyraInputBox { HintText = "Min on Hand (0=disabled)", Width = 130 };

        var addFieldsRow1 = new HorizontalStackPanel { Spacing = 4 };
        addFieldsRow1.Widgets.Add(new MyraLabel(TazLang.Get("agent_graphic_label"), MyraLabel.TextStyle.P));
        addFieldsRow1.Widgets.Add(newGraphicBox);
        addFieldsRow1.Widgets.Add(new MyraLabel(TazLang.Get("agent_hue_label"), MyraLabel.TextStyle.P));
        addFieldsRow1.Widgets.Add(newHueBox);

        var addFieldsRow2 = new HorizontalStackPanel { Spacing = 4 };
        addFieldsRow2.Widgets.Add(new MyraLabel(TazLang.Get("agent_maxamount_label"), MyraLabel.TextStyle.P));
        addFieldsRow2.Widgets.Add(newMaxAmountBox);
        addFieldsRow2.Widgets.Add(new MyraLabel(TazLang.Get("autosell_minonhand_label"), MyraLabel.TextStyle.P));
        addFieldsRow2.Widgets.Add(newRestockBox);

        void ClearAddFields()
        {
            newGraphicBox.Text = "";
            newHueBox.Text = "";
            newMaxAmountBox.Text = "";
            newRestockBox.Text = "";
        }

        var addConfirmRow = new HorizontalStackPanel { Spacing = 4 };
        addConfirmRow.Widgets.Add(new MyraButton(TazLang.Get("agent_add"), () =>
        {
            if (StringHelper.TryParseInt(newGraphicBox.Text, out int graphic))
            {
                BuySellItemConfig newConfig = BuySellAgent.Instance.NewSellConfig();
                newConfig.Graphic = (ushort)graphic;

                if (MyraInputBox.TryParseHue(newHueBox.Text, out ushort hue))
                    newConfig.Hue = hue;
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
            GameActions.Print(Client.Game.UO.World, TazLang.Get("autosell_targetprompt"));
            World.Instance.TargetManager.SetTargeting(targeted =>
            {
                if (targeted is Entity entity && SerialHelper.IsItem(entity))
                {
                    if (BuySellAgent.Instance.TryGetSellConfig(entity.Graphic, entity.Hue, out _))
                        return;
                    BuySellItemConfig newConfig = BuySellAgent.Instance.NewSellConfig();
                    newConfig.Graphic = entity.Graphic;
                    newConfig.Hue = entity.Hue;
                    BuildEntriesList();
                }
            });
        }) { Tooltip = TazLang.Get("autosell_addfromtarget_tooltip") });
        actionRow.Widgets.Add(new MyraButton(TazLang.Get("autosell_addfromcontainer"), () =>
        {
            GameActions.Print(Client.Game.UO.World, TazLang.Get("autosell_addfromcontainer_prompt"));
            World.Instance.TargetManager.SetTargeting(targeted =>
            {
                if (targeted is Item container)
                {
                    int added = 0;
                    for (LinkedObject i = container.Items; i != null; i = i.Next)
                    {
                        if (i is Item item)
                        {
                            if (BuySellAgent.Instance.TryGetSellConfig(item.Graphic, item.Hue, out _))
                                continue;
                            BuySellItemConfig newConfig = BuySellAgent.Instance.NewSellConfig();
                            newConfig.Graphic = item.Graphic;
                            newConfig.Hue = item.Hue;
                            added++;
                        }
                    }
                    GameActions.Print(Client.Game.UO.World, TazLang.Get("autosell_addedfromcontainer", new string[] { added.ToString() }));
                    BuildEntriesList();
                }
            });
        }) { Tooltip = TazLang.Get("autosell_addfromcontainer_tooltip") });
        actionRow.Widgets.Add(MyraStyle.ApplyButtonDangerStyle(new MyraButton(TazLang.Get("autosell_clearall"), () =>
        {
            BuySellAgent.Instance.SellConfigs?.Clear();
            BuildEntriesList();
        }) { Tooltip = TazLang.Get("autosell_clearall_tooltip") }));
        actionRow.Widgets.Add(new MyraButton(TazLang.Get("agent_import"), () =>
        {
            string? json = Clipboard.GetClipboardText();
            if (json.NotNullNotEmpty() && BuySellAgent.ImportFromJson(json, AgentType.Sell))
            {
                GameActions.Print(TazLang.Get("autosell_imported"), Constants.HUE_SUCCESS);
                BuildEntriesList();
                return;
            }
            GameActions.Print(TazLang.Get("agent_invalidimport"), Constants.HUE_ERROR);
        }) { Tooltip = TazLang.Get("agent_import_tooltip") });
        actionRow.Widgets.Add(new MyraButton(TazLang.Get("agent_export"), () =>
        {
            BuySellAgent.GetJsonExport(AgentType.Sell)?.CopyToClipboard();
            GameActions.Print(TazLang.Get("autosell_exported"), Constants.HUE_SUCCESS);
        }) { Tooltip = TazLang.Get("agent_export_tooltip") });

        root.Widgets.Add(actionRow);
        root.Widgets.Add(addEntryPanel);
        root.Widgets.Add(new ScrollViewer { MaxHeight = 300, Content = entriesPanel });

        return root;
    }
}
