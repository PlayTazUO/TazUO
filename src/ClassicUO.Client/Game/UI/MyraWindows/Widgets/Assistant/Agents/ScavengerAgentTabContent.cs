#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.MyraWindows.Widgets.ArtTexture;
using ClassicUO.Utility;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Assistant.Agents;

public static class ScavengerAgentTabContent
{
    private static readonly string[] PriorityLabels = { "Low", "Normal", "High" };

    public static Widget Build()
    {
        Profile? profile = ProfileManager.CurrentProfile;

        var root = new VerticalStackPanel { Spacing = 6 };

        // Enable Scavenger + Set Grab Bag
        var topRow = new HorizontalStackPanel { Spacing = 8 };
        topRow.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.EnableScavenger,
            b => profile.EnableScavenger = b,
            "Enable Scavenger",
            "Scavenger automatically picks up items on the ground based on configured criteria."));
        topRow.Widgets.Add(new MyraButton("Set Grab Bag", () =>
        {
            GameActions.Print(Client.Game.UO.World, "Target container to grab items into");
            Client.Game.UO.World.TargetManager.SetTargeting(CursorTarget.SetGrabBag, 0, TargetType.Neutral);
        }) { Tooltip = "Choose a container to grab items into" });
        root.Widgets.Add(topRow);

        // Entries panel (declared early so the list selector callbacks can rebuild it).
        var entriesPanel = new VerticalStackPanel { Spacing = 4 };

        // Scavenger list selection
        root.Widgets.Add(new MyraSpacer(15, 5));
        root.Widgets.Add(new MyraLabel("Scavenger Lists:", MyraLabel.TextStyle.H2));

        var listSelectRow = new HorizontalStackPanel { Spacing = 6, VerticalAlignment = VerticalAlignment.Center };

        var listCombo = new ComboView { MinWidth = 160, VerticalAlignment = VerticalAlignment.Center };
        bool suppressListEvent = false;
        MyraButton deleteListBtn = null!;

        void RefreshListCombo()
        {
            suppressListEvent = true;
            listCombo.ListView.Widgets.Clear();

            IReadOnlyList<ScavengerManager.ScavengerList> lists = ScavengerManager.Instance.Lists;
            int selectedIdx = 0;
            for (int i = 0; i < lists.Count; i++)
            {
                listCombo.ListView.Widgets.Add(new Label { Text = lists[i].Name });
                if (lists[i] == ScavengerManager.Instance.CurrentList) selectedIdx = i;
            }

            if (lists.Count > 0) listCombo.ListView.SelectedIndex = selectedIdx;
            if (deleteListBtn != null) deleteListBtn.Enabled = lists.Count > 1;
            suppressListEvent = false;
        }

        listCombo.ListView.SelectedIndexChanged += (_, _) =>
        {
            if (suppressListEvent) return;

            int? idx = listCombo.ListView.SelectedIndex;
            IReadOnlyList<ScavengerManager.ScavengerList> lists = ScavengerManager.Instance.Lists;
            if (idx.HasValue && idx.Value >= 0 && idx.Value < lists.Count)
            {
                ScavengerManager.Instance.SelectList(lists[idx.Value]);
                BuildEntriesList();
            }
        };
        listSelectRow.Widgets.Add(listCombo);

        listSelectRow.Widgets.Add(new MyraButton("New", () =>
        {
            var nameBox = new MyraInputBox { HintText = "List name", Width = 220 };
            new MyraDialog("New Scavenger List", nameBox, ok =>
            {
                if (!ok) return;
                ScavengerManager.Instance.AddList(nameBox.Text);
                RefreshListCombo();
                BuildEntriesList();
            });
        }) { Tooltip = "Create a new scavenger list and switch to it." });

        listSelectRow.Widgets.Add(new MyraButton("Rename", () =>
        {
            ScavengerManager.ScavengerList current = ScavengerManager.Instance.CurrentList;
            if (current == null) return;
            var nameBox = new MyraInputBox { Text = current.Name, HintText = "List name", Width = 220 };
            new MyraDialog("Rename Scavenger List", nameBox, ok =>
            {
                if (!ok || string.IsNullOrWhiteSpace(nameBox.Text)) return;
                ScavengerManager.Instance.RenameList(current, nameBox.Text);
                RefreshListCombo();
            });
        }) { Tooltip = "Rename the selected scavenger list." });

        deleteListBtn = new MyraButton("Delete List", () =>
        {
            if (ScavengerManager.Instance.Lists.Count <= 1)
            {
                GameActions.Print("You must have at least one scavenger list.", Constants.HUE_ERROR);
                return;
            }

            ScavengerManager.ScavengerList current = ScavengerManager.Instance.CurrentList;
            if (current == null) return;
            new MyraDialog("Delete Scavenger List",
                new MyraLabel($"Delete list \"{current.Name}\" and all of its entries?", MyraLabel.TextStyle.P),
                ok =>
                {
                    if (!ok || !ScavengerManager.Instance.DeleteList(current)) return;
                    RefreshListCombo();
                    BuildEntriesList();
                });
        }) { Tooltip = "Delete the selected scavenger list. At least one list must remain." };
        listSelectRow.Widgets.Add(MyraStyle.ApplyButtonDangerStyle(deleteListBtn));

        root.Widgets.Add(listSelectRow);

        // Entries section
        root.Widgets.Add(new MyraSpacer(15, 5));
        root.Widgets.Add(new MyraLabel("Entries:", MyraLabel.TextStyle.H2));

        void BuildEntriesList()
        {
            entriesPanel.Widgets.Clear();
            List<ScavengerManager.ScavengerEntry>? entries = ScavengerManager.Instance.ScavengerEntries;

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
                GridColumnInfo.Auto("Max"),
                GridColumnInfo.Fill("Destination"),
                GridColumnInfo.Auto("Order"),
                GridColumnInfo.Auto("Actions")
            );

            int dataRow = 1;
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                ScavengerManager.ScavengerEntry entry = entries[i];

                // Art image (col 0)
                if (entry.Graphic > 0)
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
                    Text = entry.Graphic == -1 ? "-1" : entry.Graphic.ToString(),
                    Tooltip = "Item graphic ID. Set to -1 to match any graphic.",
                };
                graphicBox.TextChangedByUser += (_, _) =>
                {
                    if (StringHelper.TryParseInt(graphicBox.Text, out int g))
                        entry.Graphic = g;
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
                    entry.Priority = (ScavengerManager.ScavengerPriority)p;
                    priorityLabel.Text = PriorityLabels[p];
                }));
                priorityRow.Widgets.Add(priorityLabel);
                priorityRow.Widgets.Add(new MyraButton(">", () =>
                {
                    int p = ((int)entry.Priority + 1) % PriorityLabels.Length;
                    entry.Priority = (ScavengerManager.ScavengerPriority)p;
                    priorityLabel.Text = PriorityLabels[p];
                }));
                grid.AddWidget(priorityRow, dataRow, 4);

                // Max amount to keep in the destination (0 = no limit)
                var maxBox = new MyraInputBox
                {
                    Text = entry.MaxAmount.ToString(),
                    Tooltip = "Max matching items to keep in the destination container. Counts items already in the destination.\n(0 = no limit)",
                    Width = 70,
                };
                maxBox.TextChangedByUser += (_, _) =>
                {
                    if (int.TryParse(maxBox.Text, out int max) && max >= 0)
                        entry.MaxAmount = max;
                };
                grid.AddWidget(maxBox, dataRow, 5);

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
                grid.AddWidget(destCell, dataRow, 6);

                // Up / Down reorder buttons (col 7)
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
                grid.AddWidget(orderRow, dataRow, 7);

                var delBtn = new MyraButton("Delete", () =>
                {
                    ScavengerManager.Instance.TryRemoveScavengerEntry(entry.Uid);
                    BuildEntriesList();
                });
                delBtn.VerticalAlignment = VerticalAlignment.Center;
                grid.AddWidget(MyraStyle.ApplyButtonDangerStyle(delBtn), dataRow, 8);

                dataRow += 1;
            }

            entriesPanel.Widgets.Add(grid);
        }

        BuildEntriesList();
        RefreshListCombo();

        // Add entry inline panel
        var addEntryPanel = new VerticalStackPanel { Visible = false, Spacing = 4 };
        var newNameBox = new MyraInputBox { HintText = "Name", Width = 100 };
        var newGraphicBox = new MyraInputBox { HintText = "Graphic ID", Width = 100, Tooltip = "Graphic (-1 = any)" };
        var newHueBox = MyraInputBox.Hue(ushort.MaxValue, 100, "Hue (-1 = any)");
        var newRegexBox = new MyraInputBox { HintText = "Regex (optional)", Width = 200 };
        var newMaxBox = new MyraInputBox { HintText = "Max (0 = no limit)", Width = 100, Tooltip = "Max matching items to keep in the destination container. 0 = no limit." };

        var addFieldsRow = new HorizontalStackPanel { Spacing = 4 };
        addFieldsRow.Widgets.Add(new MyraLabel("Name:", MyraLabel.TextStyle.P));
        addFieldsRow.Widgets.Add(newNameBox);
        addFieldsRow.Widgets.Add(new MyraLabel("Graphic:", MyraLabel.TextStyle.P));
        addFieldsRow.Widgets.Add(newGraphicBox);
        addFieldsRow.Widgets.Add(new MyraLabel("Hue:", MyraLabel.TextStyle.P));
        addFieldsRow.Widgets.Add(newHueBox);
        addFieldsRow.Widgets.Add(new MyraLabel("Regex:", MyraLabel.TextStyle.P));
        addFieldsRow.Widgets.Add(newRegexBox);
        addFieldsRow.Widgets.Add(new MyraLabel("Max:", MyraLabel.TextStyle.P));
        addFieldsRow.Widgets.Add(newMaxBox);

        var addConfirmRow = new HorizontalStackPanel { Spacing = 4 };
        addConfirmRow.Widgets.Add(new MyraButton("Add", () =>
        {
            if (StringHelper.TryParseInt(newGraphicBox.Text, out int graphic))
            {
                if (graphic > ushort.MaxValue)
                    return;

                if (!MyraInputBox.TryParseHue(newHueBox.Text, out ushort hue))
                    hue = ushort.MaxValue;

                ScavengerManager.ScavengerEntry? entry = ScavengerManager.Instance.AddScavengerEntry(graphic, hue, newNameBox.Text);
                if (entry == null) return;

                entry.RegexSearch = newRegexBox.Text;
                if (int.TryParse(newMaxBox.Text, out int maxAmount) && maxAmount >= 0)
                    entry.MaxAmount = maxAmount;

                newNameBox.Text = "";
                newGraphicBox.Text = "";
                newHueBox.Text = "";
                newRegexBox.Text = "";
                newMaxBox.Text = "";
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

        // Action buttons
        var actionRow = new HorizontalStackPanel { Spacing = 6 };
        actionRow.Widgets.Add(new MyraButton("Import", () =>
        {
            string? json = Clipboard.GetClipboardText();
            if (json.NotNullNotEmpty() && ScavengerManager.Instance.ImportFromJson(json))
            {
                GameActions.Print("Imported scavenger list!", Constants.HUE_SUCCESS);
                BuildEntriesList();
                return;
            }
            GameActions.Print("Your clipboard does not have a valid export copied.", Constants.HUE_ERROR);
        }) { Tooltip = "Import from clipboard (must have a valid export copied)." });

        actionRow.Widgets.Add(new MyraButton("Export", () =>
        {
            ScavengerManager.Instance.GetJsonExport()?.CopyToClipboard();
            GameActions.Print("Exported scavenger list to your clipboard!", Constants.HUE_SUCCESS);
        }) { Tooltip = "Export your list to clipboard." });

        var addRow = new HorizontalStackPanel { Spacing = 6 };
        addRow.Widgets.Add(new MyraButton("Add Manual Entry", () => addEntryPanel.Visible = !addEntryPanel.Visible));
        addRow.Widgets.Add(new MyraButton("Add from Target", () =>
        {
            World.Instance.TargetManager.SetTargeting(targeted =>
            {
                if (targeted is Entity entity && SerialHelper.IsItem(entity))
                {
                    ScavengerManager.Instance.AddScavengerEntry(entity.Graphic, entity.Hue, entity.Name);
                    BuildEntriesList();
                }
            });
        }) { Tooltip = "Target an item to add it to the scavenger list." });

        root.Widgets.Add(actionRow);
        root.Widgets.Add(addRow);
        root.Widgets.Add(addEntryPanel);
        root.Widgets.Add(new ScrollViewer { MaxHeight = 300, Content = entriesPanel });

        return root;
    }
}
