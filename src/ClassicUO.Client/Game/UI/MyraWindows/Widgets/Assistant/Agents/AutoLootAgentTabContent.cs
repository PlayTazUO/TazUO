#nullable enable
using System;
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
    private static readonly string[] PriorityLabels = {
        TazLang.Get("autoloot_priority_low", "Low"),
        TazLang.Get("autoloot_priority_normal", "Normal"),
        TazLang.Get("autoloot_priority_high", "High")
    };

    public static Widget Build()
    {
        Profile? profile = ProfileManager.CurrentProfile;

        var root = new VerticalStackPanel { Spacing = 6 };

        // Enable Auto Loot + Set Grab Bag
        var topRow = new HorizontalStackPanel { Spacing = 8 };
        topRow.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.EnableAutoLoot,
            b => profile.EnableAutoLoot = b,
            TazLang.Get("autoloot_enable", "Enable Auto Loot"),
            TazLang.Get("autoloot_enable_tooltip", "Auto Loot allows you to automatically pick up items from corpses based on configured criteria.")));
        topRow.Widgets.Add(new MyraButton(TazLang.Get("autoloot_setgrabbag", "Set Grab Bag"), () =>
        {
            GameActions.Print(Client.Game.UO.World, TazLang.Get("autoloot_targetgrabbag_prompt", "Target container to grab items into"));
            Client.Game.UO.World.TargetManager.SetTargeting(CursorTarget.SetGrabBag, 0, TargetType.Neutral);
        }) { Tooltip = TazLang.Get("autoloot_setgrabbag_tooltip", "Choose a container to grab items into") });
        root.Widgets.Add(topRow);

        // Options
        root.Widgets.Add(new MyraSpacer(15, 5));
        root.Widgets.Add(new MyraLabel(TazLang.Get("autoloot_options", "Options:"), MyraLabel.TextStyle.H2));

        var optRow1 = new HorizontalStackPanel { Spacing = 8 };
        optRow1.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.EnableScavenger,
            b => profile.EnableScavenger = b,
            TazLang.Get("autoloot_enable_scavenger", "Enable Scavenger"),
            TazLang.Get("autoloot_enable_scavenger_tooltip", "Scavenger option allows picking objects from ground.")));
        optRow1.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.EnableAutoLootProgressBar,
            b => profile.EnableAutoLootProgressBar = b,
            TazLang.Get("autoloot_enable_progressbar", "Enable Progress Bar"),
            TazLang.Get("autoloot_enable_progressbar_tooltip", "Shows a progress bar gump.")));
        root.Widgets.Add(optRow1);

        var optRow2 = new HorizontalStackPanel { Spacing = 8 };
        optRow2.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.AutoLootHumanCorpses,
            b => profile.AutoLootHumanCorpses = b,
            TazLang.Get("autoloot_human_corpses", "Auto Loot Human Corpses"),
            TazLang.Get("autoloot_human_corpses_tooltip", "Auto loots human corpses.")));
        optRow2.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.HueCorpseAfterAutoloot,
            b => profile.HueCorpseAfterAutoloot = b,
            TazLang.Get("autoloot_hue_corpse", "Hue Corpse After Processing"),
            TazLang.Get("autoloot_hue_corpse_tooltip", "Hue corpses after processing to make it easier to see if autoloot has processed them.")));
        root.Widgets.Add(optRow2);

        var optRow3 = new HorizontalStackPanel { Spacing = 8, VerticalAlignment = Myra.Graphics2D.UI.VerticalAlignment.Center };
        optRow3.Widgets.Add(new MyraLabel(TazLang.Get("autoloot_retry_delay_label", "Corpse retry delay (ms):"), MyraLabel.TextStyle.P)
        {
            Tooltip = TazLang.Get("autoloot_retry_delay_tooltip", "Milliseconds before a failed corpse is retried. Minimum 1000ms."),
            VerticalAlignment = Myra.Graphics2D.UI.VerticalAlignment.Center
        });
        var retrySpinner = new SpinButton
        {
            Integer = true,
            Value = profile.AutoLootRetryDelay,
            Minimum = 1000,
            Maximum = 600000,
            MinWidth = 100,
            Tooltip = TazLang.Get("autoloot_retry_delay_tooltip", "Milliseconds before a failed corpse is retried. Minimum 1000ms.")
        };
        retrySpinner.ValueChangedByUser += (_, _) =>
            profile.AutoLootRetryDelay = (int)Math.Clamp(retrySpinner.Value ?? 5000f, 1000f, 600000f);
        optRow3.Widgets.Add(retrySpinner);
        optRow3.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.DisableAutolootCorpseRetry,
            b => profile.DisableAutolootCorpseRetry = b,
            TazLang.Get("autoloot_disableretry"),
            TazLang.Get("autoloot_disableretry_tooltip")));
        root.Widgets.Add(optRow3);

        // Entries section
        root.Widgets.Add(new MyraSpacer(15, 5));
        root.Widgets.Add(new MyraLabel(TazLang.Get("autoloot_entries", "Entries:"), MyraLabel.TextStyle.H2));

        var entriesPanel = new VerticalStackPanel { Spacing = 4 };

        void BuildEntriesList()
        {
            entriesPanel.Widgets.Clear();
            List<AutoLootManager.AutoLootConfigEntry>? entries = AutoLootManager.Instance.AutoLootList;

            if (entries.Count == 0)
            {
                entriesPanel.Widgets.Add(new MyraLabel(TazLang.Get("autoloot_noentries", "No entries configured."), MyraLabel.TextStyle.P));
                return;
            }

            var grid = new MyraGrid();
            grid.SetupWithHeaders(
                GridColumnInfo.Auto(TazLang.Get("agent_col_art", "Art")),
                GridColumnInfo.Auto(TazLang.Get("agent_col_graphic", "Graphic")),
                GridColumnInfo.Auto(TazLang.Get("agent_col_hue", "Hue")),
                GridColumnInfo.Auto(TazLang.Get("autoloot_col_regex", "Regex")),
                GridColumnInfo.Auto(TazLang.Get("autoloot_col_priority", "Priority")),
                GridColumnInfo.Fill(TazLang.Get("autoloot_col_destination", "Destination")),
                GridColumnInfo.Auto(TazLang.Get("autoloot_col_order", "Order")),
                GridColumnInfo.Auto(TazLang.Get("agent_col_actions", "Actions"))
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
                        HintText = TazLang.Get("autoloot_hint_name", "Name"),
                        Tooltip = TazLang.Get("autoloot_name_tooltip", "Display name for this entry."),
                        MinWidth = 80,
                    };
                    nameBox.TextChangedByUser += (_, _) => entry.Name = nameBox.Text;
                    grid.AddWidget(nameBox, dataRow, 0);
                }

                // Graphic
                var graphicBox = new MyraInputBox
                {
                    Text = entry.Graphic == ushort.MaxValue ? "-1" : entry.Graphic.ToString(),
                    Tooltip = TazLang.Get("autoloot_graphic_tooltip", "Item graphic ID. Set to -1 to match any graphic."),
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
                grid.AddWidget(new MyraButton(TazLang.Get("autoloot_editregex", "Edit Regex"), () =>
                {
                    var regexInput = new MyraInputBox
                    {
                        Text = entry.RegexSearch ?? "",
                        Multiline = true,
                        Width = 300,
                        Height = 80,
                        Tooltip = TazLang.Get("autoloot_editregex_tooltip", "Regex to match against item name and properties.")
                    };
                    new MyraDialog(TazLang.Get("autoloot_editregex", "Edit Regex"), regexInput, ok =>
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
                    HintText = TazLang.Get("autoloot_destination_hint", "Serial (hex)"),
                    Tooltip = TazLang.Get("autoloot_destination_tooltip", "Destination container serial (hex). Leave empty to use grab bag."),
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
                destCell.Widgets.Add(new MyraButton(TazLang.Get("autoloot_target_destination", "Target"), () =>
                {
                    World.Instance.TargetManager.SetTargeting(targeted =>
                    {
                        if (targeted is Entity e && SerialHelper.IsItem(e))
                        {
                            entry.DestinationContainer = e.Serial;
                            destBox.Text = $"0x{e.Serial:X}";
                        }
                    });
                }) { Tooltip = TazLang.Get("autoloot_target_destination_tooltip", "Target a container to use as the destination for this entry.") });
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
                }) { Tooltip = TazLang.Get("autoloot_move_up_tooltip", "Move up") };
                var downBtn = new MyraButton(">", () =>
                {
                    int idx = entries.IndexOf(entry);
                    if (idx > 0)
                    {
                        (entries[idx], entries[idx - 1]) = (entries[idx - 1], entries[idx]);
                        BuildEntriesList();
                    }
                }) { Tooltip = TazLang.Get("autoloot_move_down_tooltip", "Move down") };
                if (i == entries.Count - 1) upBtn.Enabled = false;
                if (i == 0) downBtn.Enabled = false;
                orderRow.Widgets.Add(upBtn);
                orderRow.Widgets.Add(downBtn);
                grid.AddWidget(orderRow, dataRow, 6);

                var delBtn = new MyraButton(TazLang.Get("agent_delete", "Delete"), () =>
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
        var newNameBox = new MyraInputBox { HintText = TazLang.Get("autoloot_hint_name", "Name"), Width = 100 };
        var newGraphicBox = new MyraInputBox { HintText = TazLang.Get("agent_hint_graphic", "Graphic ID"), Width = 100, Tooltip = TazLang.Get("autoloot_new_graphic_tooltip", "Graphic (-1 = any)") };
        var newHueBox = MyraInputBox.Hue(ushort.MaxValue, 100, TazLang.Get("autoloot_hint_hue", "Hue (-1 = any)"));
        var newRegexBox = new MyraInputBox { HintText = TazLang.Get("autoloot_label_regex", "Regex (optional)"), Width = 200 };

        var addFieldsRow = new HorizontalStackPanel { Spacing = 4 };
        addFieldsRow.Widgets.Add(new MyraLabel(TazLang.Get("autoloot_label_name", "Name:"), MyraLabel.TextStyle.P));
        addFieldsRow.Widgets.Add(newNameBox);
        addFieldsRow.Widgets.Add(new MyraLabel(TazLang.Get("agent_graphic_label", "Graphic:"), MyraLabel.TextStyle.P));
        addFieldsRow.Widgets.Add(newGraphicBox);
        addFieldsRow.Widgets.Add(new MyraLabel(TazLang.Get("agent_hue_label", "Hue:"), MyraLabel.TextStyle.P));
        addFieldsRow.Widgets.Add(newHueBox);
        addFieldsRow.Widgets.Add(new MyraLabel(TazLang.Get("autoloot_label_regex", "Regex:"), MyraLabel.TextStyle.P));
        addFieldsRow.Widgets.Add(newRegexBox);

        var addConfirmRow = new HorizontalStackPanel { Spacing = 4 };
        addConfirmRow.Widgets.Add(new MyraButton(TazLang.Get("agent_add", "Add"), () =>
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
        addConfirmRow.Widgets.Add(new MyraButton(TazLang.Get("agent_cancel", "Cancel"), () =>
        {
            addEntryPanel.Visible = false;
            newGraphicBox.Text = "";
            newHueBox.Text = "";
            newRegexBox.Text = "";
        }));

        addEntryPanel.Widgets.Add(new MyraLabel(TazLang.Get("agent_addnewentry", "Add New Entry:"), MyraLabel.TextStyle.H3));
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
                importCharPanel.Widgets.Add(new MyraLabel(TazLang.Get("autoloot_import_nootherconfigs", "No other character configurations found."), MyraLabel.TextStyle.P));
            }
            else
            {
                importCharPanel.Widgets.Add(new MyraLabel(TazLang.Get("autoloot_import_selectchar", "Select a character to import from:"), MyraLabel.TextStyle.H3));
                foreach (KeyValuePair<string, List<AutoLootManager.AutoLootConfigEntry>> kv in otherConfigs.OrderBy(c => c.Key))
                {
                    string charName = kv.Key;
                    List<AutoLootManager.AutoLootConfigEntry> configs = kv.Value;
                    importCharPanel.Widgets.Add(new MyraButton(string.Format(TazLang.Get("autoloot_import_charitems_fmt", "{0} ({1} items)"), charName, configs.Count.ToString()), () =>
                    {
                        AutoLootManager.Instance.ImportFromOtherCharacter(charName, configs);
                        BuildEntriesList();
                        importCharPanel.Visible = false;
                    }));
                }
            }

            importCharPanel.Widgets.Add(new MyraButton(TazLang.Get("agent_cancel", "Cancel"), () => importCharPanel.Visible = false));
        }

        // Action buttons
        var actionRow = new HorizontalStackPanel { Spacing = 6 };
        actionRow.Widgets.Add(new MyraButton(TazLang.Get("agent_import", "Import"), () =>
        {
            string? json = Clipboard.GetClipboardText();
            if (json.NotNullNotEmpty() && AutoLootManager.Instance.ImportFromJson(json))
            {
                GameActions.Print(TazLang.Get("autoloot_imported", "Imported loot list!"), Constants.HUE_SUCCESS);
                BuildEntriesList();
                return;
            }
            GameActions.Print(TazLang.Get("agent_invalidimport", "Your clipboard does not have a valid export copied."), Constants.HUE_ERROR);
        }) { Tooltip = TazLang.Get("agent_import_tooltip", "Import from clipboard (must have a valid export copied).") });

        actionRow.Widgets.Add(new MyraButton(TazLang.Get("agent_export", "Export"), () =>
        {
            AutoLootManager.Instance.GetJsonExport()?.CopyToClipboard();
            GameActions.Print(TazLang.Get("autoloot_exported", "Exported loot list to your clipboard!"), Constants.HUE_SUCCESS);
        }) { Tooltip = TazLang.Get("agent_export_tooltip", "Export your list to clipboard.") });

        actionRow.Widgets.Add(new MyraButton(TazLang.Get("autoloot_importfromchar", "Import from Character"), () =>
        {
            BuildImportCharPanel();
            importCharPanel.Visible = !importCharPanel.Visible;
        }) { Tooltip = TazLang.Get("autoloot_importfromchar_tooltip", "Import autoloot configuration from another character.") });

        var addRow = new HorizontalStackPanel { Spacing = 6 };
        addRow.Widgets.Add(new MyraButton(TazLang.Get("agent_addmanualentry", "Add Manual Entry"), () => addEntryPanel.Visible = !addEntryPanel.Visible));
        addRow.Widgets.Add(new MyraButton(TazLang.Get("agent_addfromtarget", "Add from Target"), () =>
        {
            World.Instance.TargetManager.SetTargeting(targeted =>
            {
                if (targeted is Entity entity && SerialHelper.IsItem(entity))
                {
                    AutoLootManager.Instance.AddAutoLootEntry(entity.Graphic, entity.Hue, entity.Name);
                    BuildEntriesList();
                }
            });
        }) { Tooltip = TazLang.Get("autoloot_addfromtarget_tooltip", "Target an item to add it to the loot list.") });

        root.Widgets.Add(actionRow);
        root.Widgets.Add(addRow);
        root.Widgets.Add(addEntryPanel);
        root.Widgets.Add(importCharPanel);
        root.Widgets.Add(new ScrollViewer { MaxHeight = 300, Content = entriesPanel });

        return root;
    }
}
