#nullable enable
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Utility;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Assistant.Filters;

public static class GraphicReplacementTabContent
{
    private static readonly string[] TypeNames = { "Mobile", "Land", "Static" };
    private static readonly byte[] TypeValues = { 1, 2, 3 };

    private static string GetTypeName(byte t) => t switch { 1 => "Mobile", 2 => "Land", _ => "Static" };

    public static Widget Build()
    {
        var root = new VerticalStackPanel { Spacing = 6 };

        root.Widgets.Add(new MyraLabel(
            "Replace graphics with other graphics. Mobile = animations, Land = terrain tiles, Static = items/statics.",
            MyraLabel.TextStyle.H3));

        var filtersPanel = new VerticalStackPanel { Spacing = 2 };

        void BuildFilterList()
        {
            filtersPanel.Widgets.Clear();
            Dictionary<(ushort, byte), GraphicChangeFilter> filters = GraphicsReplacement.GraphicFilters;

            if (filters.Count == 0)
            {
                filtersPanel.Widgets.Add(new MyraLabel("No replacements configured.", MyraLabel.TextStyle.H3));
                return;
            }

            var grid = new MyraGrid();
            grid.SetupWithHeaders(
                GridColumnInfo.Auto("Original"),
                GridColumnInfo.Auto("Type"),
                GridColumnInfo.Fill("Replacement"),
                GridColumnInfo.Fill("Preview"),
                GridColumnInfo.Fill("New Hue"),
                GridColumnInfo.Auto("Actions")
            );

            var filterList = filters.Values.ToList();
            int dataRow = 1;
            for (int i = filterList.Count - 1; i >= 0; i--)
            {
                GraphicChangeFilter filter = filterList[i];

                // Original — show as label (changing original = key change, use delete+re-add)
                grid.AddWidget(new MyraLabel($"0x{filter.OriginalGraphic:X4}", MyraLabel.TextStyle.P, MyraLabel.AlignMode.Right), dataRow, 0);

                // Type — cycle button using wrapper panel (key change requires rebuild)
                var typeWrapper = new HorizontalStackPanel();
                void BuildTypeBtn()
                {
                    typeWrapper.Widgets.Clear();
                    var btn = new MyraButton(GetTypeName(filter.OriginalType), () =>
                    {
                        int idx = System.Array.IndexOf(TypeValues, filter.OriginalType);
                        byte newType = TypeValues[(idx + 1) % TypeValues.Length];
                        GraphicsReplacement.DeleteFilter(filter.OriginalGraphic, filter.OriginalType);
                        GraphicsReplacement.NewFilter(
                            filter.OriginalGraphic, newType,
                            filter.ReplacementGraphic, newType,
                            filter.NewHue);
                        BuildFilterList();
                    }) { Tooltip = "Click to cycle: Mobile / Land / Static", MinWidth = 65 };
                    btn.Content.HorizontalAlignment = HorizontalAlignment.Center;
                    typeWrapper.Widgets.Add(btn);
                }
                BuildTypeBtn();
                grid.AddWidget(typeWrapper, dataRow, 1);

                // Preview wrapper — rebuilt in-place when replacement changes
                var previewWrapper = new HorizontalStackPanel { Spacing = 2 };
                void BuildPreview()
                {
                    previewWrapper.Widgets.Clear();
                    if (filter.OriginalType == 3)
                    {
                        previewWrapper.Widgets.Add(new MyraArtTexture(filter.OriginalGraphic));
                        previewWrapper.Widgets.Add(new MyraLabel("→", MyraLabel.TextStyle.P));
                        previewWrapper.Widgets.Add(new MyraArtTexture(filter.ReplacementGraphic));
                    }
                    else
                    {
                        previewWrapper.Widgets.Add(new MyraLabel(
                            $"0x{filter.OriginalGraphic:X4} → 0x{filter.ReplacementGraphic:X4}", MyraLabel.TextStyle.P));
                    }
                }
                BuildPreview();
                grid.AddWidget(previewWrapper, dataRow, 3);

                // Replacement Graphic — inline edit, immediate commit + preview update
                var replacementBox = new MyraInputBox { Text = $"0x{filter.ReplacementGraphic:X4}" };
                replacementBox.TextChangedByUser += (_, _) =>
                {
                    string txt = replacementBox.Text ?? "";
                    if (StringHelper.TryParseInt(txt, out int newReplacement) && newReplacement is >= 0 and <= ushort.MaxValue)
                    {
                        filter.ReplacementGraphic = (ushort)newReplacement;
                        filter.ReplacementType = filter.OriginalType;
                        BuildPreview();
                    }
                };
                grid.AddWidget(replacementBox, dataRow, 2);

                // Hue — inline edit, immediate commit
                var hueBox = MyraInputBox.Hue(filter.NewHue);
                hueBox.TextChangedByUser += (_, _) =>
                {
                    if (MyraInputBox.TryParseHue(hueBox.Text, out ushort hue))
                        filter.NewHue = hue;
                };
                grid.AddWidget(hueBox, dataRow, 4);

                // Delete
                ushort capturedOrigGraphic = filter.OriginalGraphic;
                byte capturedOrigType = filter.OriginalType;
                grid.AddWidget(MyraStyle.ApplyButtonDangerStyle(new MyraButton("Delete", () =>
                {
                    GraphicsReplacement.DeleteFilter(capturedOrigGraphic, capturedOrigType);
                    BuildFilterList();
                }) { Tooltip = "Delete this replacement" }), dataRow, 5);

                dataRow++;
            }

            filtersPanel.Widgets.Add(grid);
        }

        // Add entry panel
        var addEntryPanel = new VerticalStackPanel { Visible = false, Spacing = 4 };
        var newOriginalBox = new MyraInputBox { HintText = "Original graphic (e.g. 0x0EED)", Width = 170 };
        var newReplacementBox = new MyraInputBox { HintText = "Replacement graphic", Width = 170 };
        var newHueBox = MyraInputBox.Hue(ushort.MaxValue, 120, "Hue (-1 = unchanged)");
        int[] newTypeIndex = { 2 }; // Default: Static

        var newTypeWrapper = new HorizontalStackPanel();
        var validationLabel = new MyraLabel("", MyraLabel.TextStyle.P) { Visible = false };

        void BuildNewTypeBtn()
        {
            newTypeWrapper.Widgets.Clear();
            newTypeWrapper.Widgets.Add(new MyraButton(TypeNames[newTypeIndex[0]], () =>
            {
                newTypeIndex[0] = (newTypeIndex[0] + 1) % TypeNames.Length;
                BuildNewTypeBtn();
            }) { Tooltip = "Click to cycle: Mobile / Land / Static" });
        }
        BuildNewTypeBtn();

        var addConfirmRow = new HorizontalStackPanel { Spacing = 4 };
        addConfirmRow.Widgets.Add(new MyraButton("Add", () =>
        {
            string origText = newOriginalBox.Text ?? "";
            string replText = newReplacementBox.Text ?? "";

            if (!StringHelper.TryParseInt(origText, out int origGraphic) ||
                !StringHelper.TryParseInt(replText, out int replGraphic))
                return;

            if (!MyraInputBox.TryParseHue(newHueBox.Text, out ushort hue))
            {
                if (!string.IsNullOrEmpty(newHueBox.Text))
                {
                    validationLabel.Text = $"Invalid hue: '{newHueBox.Text}'. Must be 0-65535, 0x hex, or -1";
                    validationLabel.Visible = true;
                    return;
                }

                hue = ushort.MaxValue;
            }

            validationLabel.Visible = false;
            byte type = TypeValues[newTypeIndex[0]];
            GraphicsReplacement.NewFilter((ushort)origGraphic, type, (ushort)replGraphic, type, hue);

            newOriginalBox.Text = "";
            newReplacementBox.Text = "";
            newHueBox.Text = "";
            newTypeIndex[0] = 2;
            BuildNewTypeBtn();
            addEntryPanel.Visible = false;
            BuildFilterList();
        }));
        addConfirmRow.Widgets.Add(new MyraButton("Cancel", () =>
        {
            addEntryPanel.Visible = false;
            newOriginalBox.Text = "";
            newReplacementBox.Text = "";
            newHueBox.Text = "";
            validationLabel.Visible = false;
        }));

        var addFieldsRow1 = new HorizontalStackPanel { Spacing = 4 };
        addFieldsRow1.Widgets.Add(new MyraLabel("Original:", MyraLabel.TextStyle.P));
        addFieldsRow1.Widgets.Add(newOriginalBox);
        addFieldsRow1.Widgets.Add(new MyraLabel("Replacement:", MyraLabel.TextStyle.P));
        addFieldsRow1.Widgets.Add(newReplacementBox);

        var addFieldsRow2 = new HorizontalStackPanel { Spacing = 4 };
        addFieldsRow2.Widgets.Add(new MyraLabel("Type:", MyraLabel.TextStyle.P));
        addFieldsRow2.Widgets.Add(newTypeWrapper);
        addFieldsRow2.Widgets.Add(new MyraLabel("New Hue:", MyraLabel.TextStyle.P));
        addFieldsRow2.Widgets.Add(newHueBox);

        addEntryPanel.Widgets.Add(new MyraLabel("New Entry:", MyraLabel.TextStyle.H3));
        addEntryPanel.Widgets.Add(addFieldsRow1);
        addEntryPanel.Widgets.Add(addFieldsRow2);
        addEntryPanel.Widgets.Add(validationLabel);
        addEntryPanel.Widgets.Add(addConfirmRow);

        var actionRow = new HorizontalStackPanel { Spacing = 4 };
        actionRow.Widgets.Add(new MyraButton("Add Entry", () => addEntryPanel.Visible = !addEntryPanel.Visible));
        actionRow.Widgets.Add(new MyraButton("Target Entity", () =>
        {
            if (World.Instance == null) return;
            World.Instance.TargetManager.SetTargeting(targeted =>
            {
                if (targeted == null) return;
                ushort graphic = 0;
                ushort hue = 0;
                byte entityType = 3;

                if (targeted is Mobile mob) { graphic = mob.Graphic; hue = mob.Hue; entityType = 1; }
                else if (targeted is Land land) { graphic = land.Graphic; hue = land.Hue; entityType = 2; }
                else if (targeted is Entity entity) { graphic = entity.Graphic; hue = entity.Hue; }
                else if (targeted is Static stat) { graphic = stat.Graphic; hue = stat.Hue; }
                else if (targeted is GameObject obj) { graphic = obj.Graphic; hue = obj.Hue; }
                else return;

                GraphicsReplacement.NewFilter(graphic, entityType, graphic, entityType, hue);
                BuildFilterList();
            });
        }) { Tooltip = "Target an entity to add it to the replacement list" });
        actionRow.Widgets.Add(new MyraButton("Import", () =>
        {
            string? json = Clipboard.GetClipboardText();
            if (json.NotNullNotEmpty() && GraphicsReplacement.ImportFromJson(json))
            {
                BuildFilterList();
                return;
            }
            GameActions.Print("Your clipboard does not have a valid export copied.", Constants.HUE_ERROR);
        }) { Tooltip = "Import from your clipboard, must have a valid export copied." });
        actionRow.Widgets.Add(new MyraButton("Export", () =>
        {
            GraphicsReplacement.GetJsonExport()?.CopyToClipboard();
            GameActions.Print("Exported graphic filters to your clipboard!", Constants.HUE_SUCCESS);
        }) { Tooltip = "Export your filters to your clipboard." });
        actionRow.Widgets.Add(new MyraButton("Apply to All Entities", () =>
        {
            World? world = World.Instance;
            if (world == null) return;
            int count = 0;
            foreach (Mobile mobile in world.Mobiles.Values.ToList())
                if (!mobile.IsDestroyed && mobile.OriginalGraphic != 0) { mobile.Graphic = mobile.OriginalGraphic; count++; }
            foreach (Item item in world.Items.Values.ToList())
                if (!item.IsDestroyed && item.OriginalGraphic != 0) { item.Graphic = item.OriginalGraphic; count++; }
            GameActions.Print($"Refreshed {count} entities with graphic replacements");
        }) { Tooltip = "Reapply graphic replacements to all entities currently in the world" });

        root.Widgets.Add(actionRow);
        root.Widgets.Add(addEntryPanel);
        root.Widgets.Add(new MyraLabel("Current Graphic Replacements:", MyraLabel.TextStyle.H3));
        BuildFilterList();
        root.Widgets.Add(new ScrollViewer { Height = 300, Content = filtersPanel });

        return root;
    }
}
