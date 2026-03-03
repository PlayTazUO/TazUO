#nullable enable
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.Utility;
using Myra.Graphics2D.UI;
using TextBox = Myra.Graphics2D.UI.TextBox;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Assistant.Agents;

public static class OrganizerAgentTabContent
{
    public static Widget Build()
    {
        OrganizerConfig? selectedConfig = null;
        var leftPanel = new VerticalStackPanel { Spacing = 4 };
        var rightPanel = new VerticalStackPanel { Spacing = 4 };

        void BuildItemsGrid(VerticalStackPanel itemsPanel)
        {
            itemsPanel.Widgets.Clear();
            if (selectedConfig == null || selectedConfig.ItemConfigs.Count == 0)
            {
                itemsPanel.Widgets.Add(new MyraLabel("No items configured.", MyraLabel.Style.P));
                return;
            }

            var grid = new MyraGrid();
            grid.AddColumn(new Proportion(ProportionType.Auto));     // Art
            grid.AddColumn(new Proportion(ProportionType.Auto));     // Hue
            grid.AddColumn(new Proportion(ProportionType.Auto));     // Amount
            grid.AddColumn(new Proportion(ProportionType.Fill));     // Destination
            grid.AddColumn(new Proportion(ProportionType.Auto));     // Enabled
            grid.AddColumn(new Proportion(ProportionType.Auto));     // Del
            MyraStyle.ApplyStandardGridStyling(grid);

            grid.AddWidget(new MyraLabel("Graphic", MyraLabel.Style.H3), 0, 0);
            grid.AddWidget(new MyraLabel("Hue", MyraLabel.Style.H3), 0, 1);
            grid.AddWidget(new MyraLabel("Amount", MyraLabel.Style.H3), 0, 2);
            grid.AddWidget(new MyraLabel("Destination", MyraLabel.Style.H3), 0, 3);
            grid.AddWidget(new MyraLabel("Enabled", MyraLabel.Style.H3), 0, 4);
            grid.AddWidget(new MyraLabel("Del", MyraLabel.Style.H3), 0, 5);

            int dataRow = 1;
            for (int i = selectedConfig.ItemConfigs.Count - 1; i >= 0; i--)
            {
                OrganizerItemConfig item = selectedConfig.ItemConfigs[i];

                // Art / Graphic
                Widget artWidget = item.Graphic > 0
                    ? new MyraArtTexture((uint)item.Graphic) { Tooltip = $"Graphic: {item.Graphic:X4}" }
                    : new MyraLabel($"{item.Graphic:X4}", MyraLabel.Style.P);
                grid.AddWidget(artWidget, dataRow, 0);

                // Hue
                var hueBox = new TextBox
                {
                    Text = item.Hue == ushort.MaxValue ? "ANY" : item.Hue.ToString(),
                    Width = 55,
                    Tooltip = "Set to ANY to match any hue."
                };
                hueBox.TextChangedByUser += (_, _) =>
                {
                    if (hueBox.Text == "ANY")
                        item.Hue = ushort.MaxValue;
                    else if (ushort.TryParse(hueBox.Text, NumberStyles.HexNumber, null, out ushort hue))
                        item.Hue = hue == 0xFFFF ? ushort.MaxValue : hue;
                };
                grid.AddWidget(hueBox, dataRow, 1);

                // Amount
                var amountBox = new TextBox
                {
                    Text = item.Amount.ToString(),
                    Width = 55,
                    Tooltip = "Amount to move. Takes into account items already in destination.\n(0 = move all)"
                };
                amountBox.TextChangedByUser += (_, _) =>
                {
                    if (ushort.TryParse(amountBox.Text, out ushort amount))
                        item.Amount = amount;
                };
                grid.AddWidget(amountBox, dataRow, 2);

                // Destination (rebuild the cell in-place via a container panel)
                var destCell = new HorizontalStackPanel { Spacing = 2 };
                OrganizerItemConfig captured = item;

                void BuildDestCell()
                {
                    destCell.Widgets.Clear();
                    if (captured.DestContSerial != 0)
                    {
                        destCell.Widgets.Add(new MyraLabel($"{captured.DestContSerial:X}", MyraLabel.Style.P)
                            { Tooltip = "Per-item destination" });
                        destCell.Widgets.Add(MyraStyle.ApplyButtonDangerStyle(new MyraButton("X", () =>
                        {
                            captured.DestContSerial = 0;
                            BuildDestCell();
                        }) { Tooltip = "Clear and use config destination" }));
                    }
                    else
                    {
                        destCell.Widgets.Add(new MyraLabel("Config", MyraLabel.Style.P)
                            { Tooltip = "Using configuration's destination" });
                        destCell.Widgets.Add(new MyraButton("Set", () =>
                        {
                            GameActions.Print("Select [DESTINATION] Container for this item", 82);
                            World.Instance.TargetManager.SetTargeting(destination =>
                            {
                                if (destination is Entity destEntity && SerialHelper.IsItem(destEntity))
                                {
                                    captured.DestContSerial = destEntity.Serial;
                                    GameActions.Print($"Per-item destination set to {destEntity.Serial:X}", Constants.HUE_SUCCESS);
                                    BuildDestCell();
                                }
                                else
                                    GameActions.Print("Only items can be selected!");
                            });
                        }) { Tooltip = "Set per-item destination" });
                    }
                }

                BuildDestCell();
                grid.AddWidget(destCell, dataRow, 3);

                // Enabled
                grid.AddWidget(MyraCheckButton.CreateWithCallback(item.Enabled, b => item.Enabled = b), dataRow, 4);

                // Delete
                grid.AddWidget(MyraStyle.ApplyButtonDangerStyle(new MyraButton("X", () =>
                {
                    selectedConfig.DeleteItemConfig(captured);
                    BuildItemsGrid(itemsPanel);
                }) { Tooltip = "Delete this item" }), dataRow, 5);

                dataRow++;
            }

            itemsPanel.Widgets.Add(grid);
        }

        void BuildConfigList()
        {
            leftPanel.Widgets.Clear();
            leftPanel.Widgets.Add(new MyraButton("Add Organizer", () =>
            {
                OrganizerConfig newConfig = OrganizerAgent.Instance.NewOrganizerConfig();
                selectedConfig = newConfig;
                BuildConfigList();
                BuildConfigDetails();
            }));
            leftPanel.Widgets.Add(new MyraLabel("List", MyraLabel.Style.H3));

            foreach (OrganizerConfig config in OrganizerAgent.Instance.OrganizerConfigs)
            {
                OrganizerConfig capturedConfig = config;
                int enabledItems = config.ItemConfigs.Count(ic => ic.Enabled);
                var btn = new MyraButton(config.Name, () =>
                {
                    selectedConfig = capturedConfig;
                    BuildConfigDetails();
                }) { Tooltip = $"{enabledItems} enabled items" };
                leftPanel.Widgets.Add(btn);
            }
        }

        void BuildConfigDetails()
        {
            rightPanel.Widgets.Clear();
            if (selectedConfig == null)
            {
                rightPanel.Widgets.Add(new MyraLabel("Select an organizer to view details", MyraLabel.Style.P));
                return;
            }

            // Enabled + Name
            var topRow = new HorizontalStackPanel { Spacing = 8 };
            topRow.Widgets.Add(MyraCheckButton.CreateWithCallback(
                selectedConfig.Enabled, b => selectedConfig.Enabled = b, "Enabled"));
            var nameBox = new TextBox { Text = selectedConfig.Name, Width = 150 };
            nameBox.TextChangedByUser += (_, _) =>
            {
                if (!string.IsNullOrWhiteSpace(nameBox.Text))
                    selectedConfig.Name = nameBox.Text;
            };
            topRow.Widgets.Add(new MyraLabel("Name:", MyraLabel.Style.P));
            topRow.Widgets.Add(nameBox);
            rightPanel.Widgets.Add(topRow);

            // Action buttons
            var actionRow = new HorizontalStackPanel { Spacing = 4 };
            actionRow.Widgets.Add(new MyraButton("Run Organizer", () =>
                OrganizerAgent.Instance.RunOrganizer(selectedConfig.Name)));
            actionRow.Widgets.Add(new MyraButton("Duplicate", () =>
            {
                OrganizerConfig? duped = OrganizerAgent.Instance.DupeConfig(selectedConfig);
                if (duped != null)
                {
                    selectedConfig = duped;
                    BuildConfigList();
                    BuildConfigDetails();
                }
            }));
            actionRow.Widgets.Add(new MyraButton("Create Macro", () =>
            {
                OrganizerAgent.Instance.CreateOrganizerMacroButton(selectedConfig.Name);
                GameActions.Print($"Created Organizer Macro: {selectedConfig.Name}");
            }));
            actionRow.Widgets.Add(new MyraButton("Import", () =>
            {
                string? json = Clipboard.GetClipboardText();
                if (json.NotNullNotEmpty() && OrganizerAgent.Instance.ImportFromJson(json))
                {
                    BuildConfigList();
                    return;
                }
                GameActions.Print("Your clipboard does not have a valid export copied.", Constants.HUE_ERROR);
            }) { Tooltip = "Import from clipboard (must have a valid export copied)." });
            actionRow.Widgets.Add(new MyraButton("Export", () =>
            {
                OrganizerAgent.Instance.GetJsonExport(selectedConfig)?.CopyToClipboard();
                GameActions.Print("Exported organizer to your clipboard!", Constants.HUE_SUCCESS);
            }) { Tooltip = "Export this organizer to clipboard." });
            actionRow.Widgets.Add(MyraStyle.ApplyButtonDangerStyle(new MyraButton("Delete", () =>
            {
                OrganizerAgent.Instance.DeleteConfig(selectedConfig);
                List<OrganizerConfig> configs = OrganizerAgent.Instance.OrganizerConfigs;
                selectedConfig = configs.Count > 0 ? configs[0] : null;
                BuildConfigList();
                BuildConfigDetails();
            })));
            rightPanel.Widgets.Add(actionRow);

            // Container settings
            rightPanel.Widgets.Add(new MyraLabel("Container Settings:", MyraLabel.Style.H3));
            var contRow = new HorizontalStackPanel { Spacing = 4 };
            contRow.Widgets.Add(new MyraButton("Set Source Container", () =>
            {
                GameActions.Print("Select [SOURCE] Container", 82);
                World.Instance.TargetManager.SetTargeting(source =>
                {
                    if (source is Entity sourceEntity && SerialHelper.IsItem(sourceEntity))
                    {
                        if (selectedConfig == null) return;
                        selectedConfig.SourceContSerial = sourceEntity.Serial;
                        GameActions.Print($"Source container set to 0x{sourceEntity.Serial:X4} ({sourceEntity.Name})", Constants.HUE_SUCCESS);
                        BuildConfigDetails();
                    }
                    else
                        GameActions.Print("Only items can be selected!");
                });
            }));
            contRow.Widgets.Add(new MyraButton("Set Destination Container", () =>
            {
                GameActions.Print("Select [DESTINATION] Container", 82);
                World.Instance.TargetManager.SetTargeting(destination =>
                {
                    if (destination is Entity destEntity && SerialHelper.IsItem(destEntity))
                    {
                        if (selectedConfig == null) return;
                        selectedConfig.DestContSerial = destEntity.Serial;
                        GameActions.Print($"Destination container set to 0x{destEntity.Serial:X4} ({destEntity.Name})", Constants.HUE_SUCCESS);
                        BuildConfigDetails();
                    }
                    else
                        GameActions.Print("Only items can be selected!");
                });
            }));
            rightPanel.Widgets.Add(contRow);

            var contInfoRow = new HorizontalStackPanel { Spacing = 12 };
            string sourceText = selectedConfig.SourceContSerial != 0
                ? $"Source: (0x{selectedConfig.SourceContSerial:X4})"
                : "Source: Your backpack";
            contInfoRow.Widgets.Add(new MyraLabel(sourceText, MyraLabel.Style.P));
            string destText = selectedConfig.DestContSerial != 0
                ? $"Destination: (0x{selectedConfig.DestContSerial:X4})"
                : "Destination: Not set";
            contInfoRow.Widgets.Add(new MyraLabel(destText, MyraLabel.Style.P));
            rightPanel.Widgets.Add(contInfoRow);

            // Items section
            rightPanel.Widgets.Add(new MyraLabel("Items to Organize:", MyraLabel.Style.H3));

            var itemsPanel = new VerticalStackPanel { Spacing = 2 };

            // Add item buttons
            var addEntryPanel = new VerticalStackPanel { Visible = false, Spacing = 4 };
            var newGraphicBox = new TextBox { HintText = "Graphic (hex, e.g. 0EED)", Width = 150 };
            var newHueBox = new TextBox { HintText = "Hue (-1=any)", Width = 80 };

            var addItemRow = new HorizontalStackPanel { Spacing = 4 };
            addItemRow.Widgets.Add(new MyraButton("Target Item to Add", () =>
            {
                World.Instance.TargetManager.SetTargeting(obj =>
                {
                    if (obj is Entity objEntity && SerialHelper.IsItem(objEntity))
                    {
                        if (selectedConfig == null) return;
                        OrganizerItemConfig newItemConfig = selectedConfig.NewItemConfig();
                        newItemConfig.Graphic = objEntity.Graphic;
                        newItemConfig.Hue = objEntity.Hue;
                        GameActions.Print($"Added item: Graphic {objEntity.Graphic:X}, Hue {objEntity.Hue:X}");
                        BuildItemsGrid(itemsPanel);
                    }
                    else
                        GameActions.Print("Only items can be added!");
                });
            }));
            addItemRow.Widgets.Add(new MyraButton("Add Item Manually", () => addEntryPanel.Visible = !addEntryPanel.Visible));
            rightPanel.Widgets.Add(addItemRow);

            // Manual add form
            var addFieldsRow = new HorizontalStackPanel { Spacing = 4 };
            addFieldsRow.Widgets.Add(new MyraLabel("Graphic:", MyraLabel.Style.P) { Tooltip = "Hex value, e.g. 0EED." });
            addFieldsRow.Widgets.Add(newGraphicBox);
            addFieldsRow.Widgets.Add(new MyraLabel("Hue:", MyraLabel.Style.P) { Tooltip = "Set to -1 to match any hue." });
            addFieldsRow.Widgets.Add(newHueBox);

            var addConfirmRow = new HorizontalStackPanel { Spacing = 4 };
            addConfirmRow.Widgets.Add(new MyraButton("Add", () =>
            {
                if (ushort.TryParse(newGraphicBox.Text, NumberStyles.HexNumber, null, out ushort graphic))
                {
                    OrganizerItemConfig newItemConfig = selectedConfig.NewItemConfig();
                    newItemConfig.Graphic = graphic;

                    if (int.TryParse(newHueBox.Text, out int hue) && hue is >= -1 and <= ushort.MaxValue)
                        newItemConfig.Hue = hue == -1 ? ushort.MaxValue : (ushort)hue;

                    newGraphicBox.Text = "";
                    newHueBox.Text = "";
                    addEntryPanel.Visible = false;
                    BuildItemsGrid(itemsPanel);
                }
            }));
            addConfirmRow.Widgets.Add(new MyraButton("Cancel", () =>
            {
                addEntryPanel.Visible = false;
                newGraphicBox.Text = "";
                newHueBox.Text = "";
            }));

            addEntryPanel.Widgets.Add(new MyraLabel("Manual Entry:", MyraLabel.Style.H3));
            addEntryPanel.Widgets.Add(addFieldsRow);
            addEntryPanel.Widgets.Add(addConfirmRow);
            rightPanel.Widgets.Add(addEntryPanel);

            BuildItemsGrid(itemsPanel);
            rightPanel.Widgets.Add(new ScrollViewer { Height = 250, Content = itemsPanel });
        }

        BuildConfigList();
        BuildConfigDetails();

        var root = new HorizontalStackPanel { Spacing = 8 };
        root.Widgets.Add(new ScrollViewer { Width = 160, Content = leftPanel });
        root.Widgets.Add(rightPanel);
        return root;
    }
}
