#nullable enable

using System.Collections.Generic;
using ClassicUO.Common;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using Myra.Graphics2D.UI;
using Label = Myra.Graphics2D.UI.Label;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

/// <summary>Options tab source for the info-bar manager (add, remove, and configure stat readouts)</summary>
public static class InfoBarsTab
{
    /// <summary>Returns the option fragment containing the info-bar editor widget</summary>
    internal static IOptionSource GetContent() =>
        OptionsUi.Vertical(
            Option.Custom(
                GetContentWidget,
                new SearchMetadata(
                    TazLang.Get("mog_buttoninfobar"),
                    Keywords: [TazLang.Get("mog_kw_infobarspaced"), TazLang.Get("mog_kw_infobar"), TazLang.Get("mog_kw_stat")]
                )
            )
        ).WithSearch(
            new SearchMetadata(
                TazLang.Get("mog_buttoninfobar"),
                [TazLang.Get("mog_kw_infobarspaced"), TazLang.Get("mog_kw_infobar"), TazLang.Get("mog_kw_stat")]
            )
        );

    private static Widget GetContentWidget()
    {
        Profile profile = ProfileManager.CurrentProfile;

        var root = new VerticalStackPanel { Spacing = 6 };

        // Show InfoBar checkbox
        root.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.ShowInfoBar,
            b =>
            {
                profile.ShowInfoBar = b;
                InfoBarGump? infoBarGump = UIManager.GetGump<InfoBarGump>();
                if (b)
                {
                    if (infoBarGump == null)
                        UIManager.Add(new InfoBarGump(World.Instance) { X = 300, Y = 300 });
                    else
                    {
                        infoBarGump.ResetItems();
                        infoBarGump.SetInScreen();
                    }
                }
                else
                {
                    infoBarGump?.Dispose();
                }
            },
            TazLang.Get("mog_infobars_showinfobar")
        ));

        root.Widgets.Add(new MyraSpacer(1, 2));

        root.Widgets.Add(OptionTabCommons.StyledFontSelector(
            TazLang.Get("mog_infobars_infobarfont"),
            new Accessor<string>(() => profile.InfoBarFont),
            _ => InfoBarGump.UpdateAllOptions()
        ));

        // Highlight type combo box
        var highlightCombo = new ComboView { MinWidth = 150, VerticalAlignment = VerticalAlignment.Center };
        highlightCombo.ListView.Widgets.Add(new Label { Text = TazLang.Get("mog_infobars_highlightopt_textcolor") });
        highlightCombo.ListView.Widgets.Add(new Label { Text = TazLang.Get("mog_infobars_highlightopt_coloredbars") });
        highlightCombo.ListView.SelectedIndex = profile.InfoBarHighlightType;
        highlightCombo.ListView.SelectedIndexChanged += (_, _) =>
        {
            if (highlightCombo.ListView.SelectedIndex.HasValue)
                profile.InfoBarHighlightType = highlightCombo.ListView.SelectedIndex.Value;
        };
        root.Widgets.Add(new MyraLabel(TazLang.Get("mog_infobars_highlighttype"), MyraLabel.TextStyle.P).PlaceBefore(highlightCombo));

        root.Widgets.Add(new MyraSpacer(1, 4));

        // Column headers
        var headers = new HorizontalStackPanel { Spacing = 4 };
        headers.Widgets.Add(new MyraLabel(TazLang.Get("mog_infobars_label"), MyraLabel.TextStyle.TableHeader) { Width = 130, MinWidth = 130 });
        headers.Widgets.Add(new MyraLabel(TazLang.Get("mog_infobars_color"), MyraLabel.TextStyle.TableHeader) { Width = 60, MinWidth = 60 });
        headers.Widgets.Add(new MyraLabel(TazLang.Get("mog_infobars_data"), MyraLabel.TextStyle.TableHeader) { Width = 170, MinWidth = 170 });
        root.Widgets.Add(headers);

        // Items list panel (rebuilt dynamically on add/remove)
        var itemsPanel = new VerticalStackPanel { Spacing = 3 };

        // Add item button
        root.Widgets.Add(new MyraButton(TazLang.Get("mog_infobars_additem"), () =>
        {
            var ibi = new InfoBarItem(TazLang.Get("mog_infobars_hp"), InfoBarVars.HP, 0x3B9);
            World.Instance.InfoBars?.AddItem(ibi);
            UIManager.GetGump<InfoBarGump>()?.ResetItems();
            itemsPanel.Widgets.Add(BuildItemRow(ibi, itemsPanel));
        }));

        // Populate existing items
        List<InfoBarItem> existingItems = World.Instance.InfoBars?.GetInfoBars() ?? [];
        foreach (InfoBarItem item in existingItems)
            itemsPanel.Widgets.Add(BuildItemRow(item, itemsPanel));

        root.Widgets.Add(itemsPanel.WrapInScroll(400));

        return root;
    }

    private static Widget BuildItemRow(InfoBarItem item, VerticalStackPanel parent)
    {
        var row = new HorizontalStackPanel { Spacing = 4, VerticalAlignment = VerticalAlignment.Center };

        // Label text input
        var labelInput = new MyraInputBox { Text = item.label, Width = 130, MinWidth = 130 };
        labelInput.TextChangedByUser += (_, _) =>
        {
            item.label = labelInput.Text;
            UIManager.GetGump<InfoBarGump>()?.ResetItems();
        };
        row.Widgets.Add(labelInput);

        // Hue picker button
        var hueBtn = new MyraButton(TazLang.Get("mog_infobars_color"), () =>
        {
            UIManager.GetGump<ModernColorPicker>()?.Dispose();
            UIManager.Add(new ModernColorPicker(World.Instance, h =>
            {
                item.hue = h;
                UIManager.GetGump<InfoBarGump>()?.ResetItems();
            }));
        })
        {
            Width = 60,
            MinWidth = 60,
            Tooltip = TazLang.Get("mog_infobars_huetooltipformat", [item.hue.ToString("X")])
        };
        row.Widgets.Add(hueBtn);

        // Variable type combo box
        var varCombo = new ComboView { Width = 170, MinWidth = 170 };
        foreach (string v in InfoBarManager.GetVars())
            varCombo.ListView.Widgets.Add(new Label { Text = v });
        varCombo.ListView.SelectedIndex = (int)item.var;
        varCombo.ListView.SelectedIndexChanged += (_, _) =>
        {
            if (varCombo.ListView.SelectedIndex.HasValue)
            {
                item.var = (InfoBarVars)varCombo.ListView.SelectedIndex.Value;
                UIManager.GetGump<InfoBarGump>()?.ResetItems();
            }
        };
        row.Widgets.Add(varCombo);

        // Delete button
        var deleteBtn = new MyraButton(TazLang.Get("mog_infobars_deletebuttonlabel"), () =>
        {
            World.Instance.InfoBars?.RemoveItem(item);
            UIManager.GetGump<InfoBarGump>()?.ResetItems();
            parent.Widgets.Remove(row);
        });
        MyraStyle.ApplyButtonDangerStyle(deleteBtn);
        row.Widgets.Add(deleteBtn);

        return row;
    }
}
