#nullable enable
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.Utility;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;
using TextBox = Myra.Graphics2D.UI.TextBox;

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
        root.Widgets.Add(new MyraLabel("Options:", MyraLabel.Style.H3));

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
        root.Widgets.Add(new MyraLabel("Entries:", MyraLabel.Style.H3));

        var entriesPanel = new VerticalStackPanel { Spacing = 4 };

        void BuildEntriesList()
        {
            entriesPanel.Widgets.Clear();
            List<AutoLootManager.AutoLootConfigEntry>? entries = AutoLootManager.Instance.AutoLootList;

            if (entries.Count == 0)
            {
                entriesPanel.Widgets.Add(new MyraLabel("No entries configured.", MyraLabel.Style.P));
                return;
            }

            // 7 columns: Art | Graphic | Hue | Regex | Priority | Destination | Actions
            var grid = new MyraGrid();
            grid.AddColumn(null, 7);
            grid.Border = new SolidBrush(MyraStyle.GridBorderColor);
            grid.BorderThickness = new Thickness(1);
            grid.GridLinesColor = MyraStyle.GridBorderColor;
            grid.ShowGridLines = true;

            // Header row
            grid.AddWidget(new MyraLabel("Art", MyraLabel.Style.H3), 0, 0);
            grid.AddWidget(new MyraLabel("Graphic", MyraLabel.Style.H3), 0, 1);
            grid.AddWidget(new MyraLabel("Hue", MyraLabel.Style.H3), 0, 2);
            grid.AddWidget(new MyraLabel("Regex", MyraLabel.Style.H3), 0, 3);
            grid.AddWidget(new MyraLabel("Priority", MyraLabel.Style.H3), 0, 4);
            grid.AddWidget(new MyraLabel("Destination", MyraLabel.Style.H3), 0, 5);
            grid.AddWidget(new MyraLabel("Actions", MyraLabel.Style.H3), 0, 6);

            int dataRow = 1;
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                AutoLootManager.AutoLootConfigEntry entry = entries[i];

                // Art image (col 0)
                if(entry.Graphic > 0)
                    grid.AddWidget(new MyraArtTexture((uint)entry.Graphic) { Tooltip = entry.Name }, dataRow, 0);

                // Graphic
                var graphicBox = new TextBox
                {
                    Text = entry.Graphic.ToString(),
                    Tooltip = "Item graphic ID. Set to -1 to match any graphic."
                };
                graphicBox.TextChangedByUser += (_, _) =>
                {
                    if (StringHelper.TryParseInt(graphicBox.Text, out int g))
                        entry.Graphic = g;
                };
                grid.AddWidget(graphicBox, dataRow, 1);

                // Hue
                var hueBox = new TextBox
                {
                    Text = entry.Hue == ushort.MaxValue ? "-1" : entry.Hue.ToString(),
                    Tooltip = "Item hue. Set to -1 to match any hue."
                };
                hueBox.TextChangedByUser += (_, _) =>
                {
                    if (hueBox.Text == "-1")
                        entry.Hue = ushort.MaxValue;
                    else if (ushort.TryParse(hueBox.Text, out ushort h))
                        entry.Hue = h;
                };
                grid.AddWidget(hueBox, dataRow, 2);

                // Regex edit — opens a MyraDialog (own Desktop, registered with UIManager)
                grid.AddWidget(new MyraButton("Edit Regex", () =>
                {
                    var regexInput = new TextBox
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
                var priorityLabel = new MyraLabel(PriorityLabels[(int)entry.Priority], MyraLabel.Style.P);
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
                var destBox = new TextBox
                {
                    Text = entry.DestinationContainer == 0 ? "" : $"0x{entry.DestinationContainer:X}",
                    HintText = "Serial (hex)",
                    Tooltip = "Destination container serial (hex). Leave empty to use grab bag."
                };
                destBox.TextChangedByUser += (_, _) =>
                {
                    if (string.IsNullOrWhiteSpace(destBox.Text))
                        entry.DestinationContainer = 0;
                    else if (uint.TryParse(destBox.Text.Replace("0x", "").Replace("0X", ""), NumberStyles.HexNumber, null, out uint serial))
                        entry.DestinationContainer = serial;
                };
                var destCell = new HorizontalStackPanel { Spacing = 4 };
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

                grid.AddWidget(new MyraButton("Delete", () =>
                {
                    AutoLootManager.Instance.TryRemoveAutoLootEntry(entry.Uid);
                    BuildEntriesList();
                }), dataRow, 6);

                dataRow += 1;
            }

            entriesPanel.Widgets.Add(grid);
        }

        BuildEntriesList();

        // Add entry inline panel
        var addEntryPanel = new VerticalStackPanel { Visible = false, Spacing = 4 };
        var newGraphicBox = new TextBox { HintText = "Graphic ID", Width = 80 };
        var newHueBox = new TextBox { HintText = "Hue (-1=any)", Width = 80 };
        var newRegexBox = new TextBox { HintText = "Regex (optional)", Width = 200 };

        var addFieldsRow = new HorizontalStackPanel { Spacing = 4 };
        addFieldsRow.Widgets.Add(new MyraLabel("Graphic:", MyraLabel.Style.P));
        addFieldsRow.Widgets.Add(newGraphicBox);
        addFieldsRow.Widgets.Add(new MyraLabel("Hue:", MyraLabel.Style.P));
        addFieldsRow.Widgets.Add(newHueBox);
        addFieldsRow.Widgets.Add(new MyraLabel("Regex:", MyraLabel.Style.P));
        addFieldsRow.Widgets.Add(newRegexBox);

        var addConfirmRow = new HorizontalStackPanel { Spacing = 4 };
        addConfirmRow.Widgets.Add(new MyraButton("Add", () =>
        {
            if (StringHelper.TryParseInt(newGraphicBox.Text, out int graphic))
            {
                if (graphic < 0 || graphic > ushort.MaxValue)
                    return;

                ushort hue = ushort.MaxValue;
                if (!string.IsNullOrEmpty(newHueBox.Text) && newHueBox.Text != "-1")
                    ushort.TryParse(newHueBox.Text, out hue);

                AutoLootManager.AutoLootConfigEntry? entry = AutoLootManager.Instance.AddAutoLootEntry((ushort)graphic, hue, "");
                entry.RegexSearch = newRegexBox.Text;

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

        addEntryPanel.Widgets.Add(new MyraLabel("Add New Entry:", MyraLabel.Style.H3));
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
                importCharPanel.Widgets.Add(new MyraLabel("No other character configurations found.", MyraLabel.Style.P));
            }
            else
            {
                importCharPanel.Widgets.Add(new MyraLabel("Select a character to import from:", MyraLabel.Style.H3));
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
        root.Widgets.Add(new ScrollViewer { Height = 300, Content = entriesPanel });

        return root;
    }
}
