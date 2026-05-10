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
    private static readonly string[] PriorityLabels = { "Low", "Normal", "High" };

    public static Widget Build()
    {
        Profile? profile = ProfileManager.CurrentProfile;

        var root = new VerticalStackPanel { Spacing = 6 };

        // Enable Auto Loot + Set Grab Bag
        var topRow = new HorizontalStackPanel { Spacing = 8 };
        topRow.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.EnableAutoLoot,
            b => profile.EnableAutoLoot = b,
            "Enable Auto Loot",
            "Auto Loot allows you to automatically pick up items from corpses based on configured criteria."));
        topRow.Widgets.Add(new MyraButton("Set Grab Bag", () =>
        {
            GameActions.Print(Client.Game.UO.World, "Target container to grab items into");
            Client.Game.UO.World.TargetManager.SetTargeting(CursorTarget.SetGrabBag, 0, TargetType.Neutral);
        }) { Tooltip = "Choose a container to grab items into" });
        root.Widgets.Add(topRow);

        // Options
        root.Widgets.Add(new MyraSpacer(15, 5));
        root.Widgets.Add(new MyraLabel("Options:", MyraLabel.TextStyle.H2));

        var optRow1 = new HorizontalStackPanel { Spacing = 8 };
        optRow1.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.EnableScavenger,
            b => profile.EnableScavenger = b,
            "Enable Scavenger",
            "Scavenger option allows picking objects from ground."));
        optRow1.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.EnableAutoLootProgressBar,
            b => profile.EnableAutoLootProgressBar = b,
            "Enable Progress Bar",
            "Shows a progress bar gump."));
        root.Widgets.Add(optRow1);

        var optRow2 = new HorizontalStackPanel { Spacing = 8 };
        optRow2.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.AutoLootHumanCorpses,
            b => profile.AutoLootHumanCorpses = b,
            "Auto Loot Human Corpses",
            "Auto loots human corpses."));
        optRow2.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.HueCorpseAfterAutoloot,
            b => profile.HueCorpseAfterAutoloot = b,
            "Hue Corpse After Processing",
            "Hue corpses after processing to make it easier to see if autoloot has processed them."));
        root.Widgets.Add(optRow2);

        // Entries section
        root.Widgets.Add(new MyraSpacer(15, 5));
        root.Widgets.Add(new MyraLabel("Entries:", MyraLabel.TextStyle.H2));

        var entriesPanel = new VerticalStackPanel { Spacing = 4 };

        void BuildEntriesList()
        {
            entriesPanel.Widgets.Clear();
            List<AutoLootManager.AutoLootConfigEntry>? entries = AutoLootManager.Instance.AutoLootList;

            if (entries.Count == 0)
            {
                entriesPanel.Widgets.Add(new MyraLabel("No entries configured.", MyraLabel.TextStyle.P));
                return;
            }

            var grid = new MyraGrid();
            grid.SetupWithHeaders(
                GridColumnInfo.Auto("Art"),
                GridColumnInfo.Auto("Graphic"),
                GridColumnInfo.Auto("Hue"),
                GridColumnInfo.Auto("Regex"),
                GridColumnInfo.Auto("Priority"),
                GridColumnInfo.Fill("Destination"),
                GridColumnInfo.Auto("Order"),
                GridColumnInfo.Auto("Actions")
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
                        HintText = "Name",
                        Tooltip = "Display name for this entry.",
                        MinWidth = 80,
                    };
                    nameBox.TextChangedByUser += (_, _) => entry.Name = nameBox.Text;
                    grid.AddWidget(nameBox, dataRow, 0);
                }

                // Graphic
                var graphicBox = new MyraInputBox
                {
                    Text = entry.Graphic == ushort.MaxValue ? "-1" : entry.Graphic.ToString(),
                    Tooltip = "Item graphic ID. Set to -1 to match any graphic.",
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
                grid.AddWidget(new MyraButton("Edit Regex", () =>
                {
                    var regexInput = new MyraInputBox
                    {
                        Text = entry.RegexSearch ?? "",
                        Multiline = true,
                        Width = 300,
                        Height = 80,
                        Tooltip = "Regex to match against item name and properties."
                    };
                    new MyraDialog("Edit Regex", regexInput, ok =>
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
                    HintText = "Serial (hex)",
                    Tooltip = "Destination container serial (hex). Leave empty to use grab bag.",
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
                destCell.Widgets.Add(new MyraButton("Target", () =>
                {
                    World.Instance.TargetManager.SetTargeting(targeted =>
                    {
                        if (targeted is Entity e && SerialHelper.IsItem(e))
                        {
                            entry.DestinationContainer = e.Serial;
                            destBox.Text = $"0x{e.Serial:X}";
                        }
                    });
                }) { Tooltip = "Target a container to use as the destination for this entry." });
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
                }) { Tooltip = "Move up" };
                var downBtn = new MyraButton(">", () =>
                {
                    int idx = entries.IndexOf(entry);
                    if (idx > 0)
                    {
                        (entries[idx], entries[idx - 1]) = (entries[idx - 1], entries[idx]);
                        BuildEntriesList();
                    }
                }) { Tooltip = "Move down" };
                if (i == entries.Count - 1) upBtn.Enabled = false;
                if (i == 0) downBtn.Enabled = false;
                orderRow.Widgets.Add(upBtn);
                orderRow.Widgets.Add(downBtn);
                grid.AddWidget(orderRow, dataRow, 6);

                var delBtn = new MyraButton("Delete", () =>
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
        var newNameBox = new MyraInputBox { HintText = "Name", Width = 100 };
        var newGraphicBox = new MyraInputBox { HintText = "Graphic ID", Width = 100, Tooltip = "Graphic (-1 = any)" };
        var newHueBox = MyraInputBox.Hue(ushort.MaxValue, 100, "Hue (-1 = any)");
        var newRegexBox = new MyraInputBox { HintText = "Regex (optional)", Width = 200 };

        var addFieldsRow = new HorizontalStackPanel { Spacing = 4 };
        addFieldsRow.Widgets.Add(new MyraLabel("Name:", MyraLabel.TextStyle.P));
        addFieldsRow.Widgets.Add(newNameBox);
        addFieldsRow.Widgets.Add(new MyraLabel("Graphic:", MyraLabel.TextStyle.P));
        addFieldsRow.Widgets.Add(newGraphicBox);
        addFieldsRow.Widgets.Add(new MyraLabel("Hue:", MyraLabel.TextStyle.P));
        addFieldsRow.Widgets.Add(newHueBox);
        addFieldsRow.Widgets.Add(new MyraLabel("Regex:", MyraLabel.TextStyle.P));
        addFieldsRow.Widgets.Add(newRegexBox);

        var addConfirmRow = new HorizontalStackPanel { Spacing = 4 };
        addConfirmRow.Widgets.Add(new MyraButton("Add", () =>
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
        addConfirmRow.Widgets.Add(new MyraButton("Cancel", () =>
        {
            addEntryPanel.Visible = false;
            newGraphicBox.Text = "";
            newHueBox.Text = "";
            newRegexBox.Text = "";
        }));

        addEntryPanel.Widgets.Add(new MyraLabel("Add New Entry:", MyraLabel.TextStyle.H3));
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
                importCharPanel.Widgets.Add(new MyraLabel("No other character configurations found.", MyraLabel.TextStyle.P));
            }
            else
            {
                importCharPanel.Widgets.Add(new MyraLabel("Select a character to import from:", MyraLabel.TextStyle.H3));
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

            importCharPanel.Widgets.Add(new MyraButton("Cancel", () => importCharPanel.Visible = false));
        }

        // Action buttons
        var actionRow = new HorizontalStackPanel { Spacing = 6 };
        actionRow.Widgets.Add(new MyraButton("Import", () =>
        {
            string? json = Clipboard.GetClipboardText();
            if (json.NotNullNotEmpty() && AutoLootManager.Instance.ImportFromJson(json))
            {
                GameActions.Print("Imported loot list!", Constants.HUE_SUCCESS);
                BuildEntriesList();
                return;
            }
            GameActions.Print("Your clipboard does not have a valid export copied.", Constants.HUE_ERROR);
        }) { Tooltip = "Import from clipboard (must have a valid export copied)." });

        actionRow.Widgets.Add(new MyraButton("Export", () =>
        {
            AutoLootManager.Instance.GetJsonExport()?.CopyToClipboard();
            GameActions.Print("Exported loot list to your clipboard!", Constants.HUE_SUCCESS);
        }) { Tooltip = "Export your list to clipboard." });

        actionRow.Widgets.Add(new MyraButton("Import from Character", () =>
        {
            BuildImportCharPanel();
            importCharPanel.Visible = !importCharPanel.Visible;
        }) { Tooltip = "Import autoloot configuration from another character." });

        var addRow = new HorizontalStackPanel { Spacing = 6 };
        addRow.Widgets.Add(new MyraButton("Add Manual Entry", () => addEntryPanel.Visible = !addEntryPanel.Visible));
        addRow.Widgets.Add(new MyraButton("Add from Target", () =>
        {
            World.Instance.TargetManager.SetTargeting(targeted =>
            {
                if (targeted is Entity entity && SerialHelper.IsItem(entity))
                {
                    AutoLootManager.Instance.AddAutoLootEntry(entity.Graphic, entity.Hue, entity.Name);
                    BuildEntriesList();
                }
            });
        }) { Tooltip = "Target an item to add it to the loot list." });

        root.Widgets.Add(actionRow);
        root.Widgets.Add(addRow);
        root.Widgets.Add(addEntryPanel);
        root.Widgets.Add(importCharPanel);
        root.Widgets.Add(new ScrollViewer { MaxHeight = 300, Content = entriesPanel });

        return root;
    }
}
