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
    private static readonly string[] TypeNames = { "目标", "地面", "静态" };
    private static readonly byte[] TypeValues = { 1, 2, 3 };

    private static string GetTypeName(byte t) => t switch { 1 => "目标", 2 => "地面", _ => "静态" };

    public static Widget Build()
    {
        var root = new VerticalStackPanel { Spacing = 6 };

        root.Widgets.Add(new MyraLabel(
            "用其他图形替换图形。目标 = 动画, 地面 = 地形瓦片, 静态 = 物品/静态对象。",
            MyraLabel.TextStyle.H3));

        var filtersPanel = new VerticalStackPanel { Spacing = 2 };

        void BuildFilterList()
        {
            filtersPanel.Widgets.Clear();
            Dictionary<(ushort, byte), GraphicChangeFilter> filters = GraphicsReplacement.GraphicFilters;

            if (filters.Count == 0)
            {
                filtersPanel.Widgets.Add(new MyraLabel("没有配置替换。", MyraLabel.TextStyle.H3));
                return;
            }

            var grid = new MyraGrid();
            grid.SetupWithHeaders(
                GridColumnInfo.Auto("原始"),
                GridColumnInfo.Auto("类型"),
                GridColumnInfo.Fill("替换"),
                GridColumnInfo.Fill("预览"),
                GridColumnInfo.Fill("新色调"),
                GridColumnInfo.Auto("操作")
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
                    }) { Tooltip = "点击循环切换: 目标 / 地面 / 静态", MinWidth = 65 };
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
                grid.AddWidget(MyraStyle.ApplyButtonDangerStyle(new MyraButton("删除", () =>
                {
                    GraphicsReplacement.DeleteFilter(capturedOrigGraphic, capturedOrigType);
                    BuildFilterList();
                }) { Tooltip = "删除此替换" }), dataRow, 5);

                dataRow++;
            }

            filtersPanel.Widgets.Add(grid);
        }

        // Add entry panel
        var addEntryPanel = new VerticalStackPanel { Visible = false, Spacing = 4 };
        var newOriginalBox = new MyraInputBox { HintText = "原始图形 (例如 0x0EED)", Width = 170 };
        var newReplacementBox = new MyraInputBox { HintText = "替换图形", Width = 170 };
        var newHueBox = MyraInputBox.Hue(ushort.MaxValue, 120, "色调 (-1 = 不变)");
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
            }) { Tooltip = "点击循环切换: 目标 / 地面 / 静态" });
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
                    validationLabel.Text = $"无效色调: '{newHueBox.Text}'。必须为 0-65535、0x 十六进制或 -1";
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
        addFieldsRow1.Widgets.Add(new MyraLabel("原始:", MyraLabel.TextStyle.P));
        addFieldsRow1.Widgets.Add(newOriginalBox);
        addFieldsRow1.Widgets.Add(new MyraLabel("替换:", MyraLabel.TextStyle.P));
        addFieldsRow1.Widgets.Add(newReplacementBox);

        var addFieldsRow2 = new HorizontalStackPanel { Spacing = 4 };
        addFieldsRow2.Widgets.Add(new MyraLabel("类型:", MyraLabel.TextStyle.P));
        addFieldsRow2.Widgets.Add(newTypeWrapper);
        addFieldsRow2.Widgets.Add(new MyraLabel("新色调:", MyraLabel.TextStyle.P));
        addFieldsRow2.Widgets.Add(newHueBox);

        addEntryPanel.Widgets.Add(new MyraLabel("新条目:", MyraLabel.TextStyle.H3));
        addEntryPanel.Widgets.Add(addFieldsRow1);
        addEntryPanel.Widgets.Add(addFieldsRow2);
        addEntryPanel.Widgets.Add(validationLabel);
        addEntryPanel.Widgets.Add(addConfirmRow);

        var actionRow = new HorizontalStackPanel { Spacing = 4 };
        actionRow.Widgets.Add(new MyraButton("添加条目", () => addEntryPanel.Visible = !addEntryPanel.Visible));
        actionRow.Widgets.Add(new MyraButton("目标实体", () =>
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
        }) { Tooltip = "目标一个实体以将其添加到替换列表" });
        actionRow.Widgets.Add(new MyraButton("导入", () =>
        {
            string? json = Clipboard.GetClipboardText();
            if (json.NotNullNotEmpty() && GraphicsReplacement.ImportFromJson(json))
            {
                BuildFilterList();
                return;
            }
            GameActions.Print("您的剪贴板中没有有效的导出数据。", Constants.HUE_ERROR);
        }) { Tooltip = "从剪贴板导入，必须有有效的导出数据。" });
        actionRow.Widgets.Add(new MyraButton("导出", () =>
        {
            GraphicsReplacement.GetJsonExport()?.CopyToClipboard();
            GameActions.Print("已将图形过滤器导出到剪贴板!", Constants.HUE_SUCCESS);
        }) { Tooltip = "将过滤器导出到剪贴板。" });
        actionRow.Widgets.Add(new MyraButton("应用到所有实体", () =>
        {
            World? world = World.Instance;
            if (world == null) return;
            int count = 0;
            foreach (Mobile mobile in world.Mobiles.Values.ToList())
                if (!mobile.IsDestroyed && mobile.OriginalGraphic != 0) { mobile.Graphic = mobile.OriginalGraphic; count++; }
            foreach (Item item in world.Items.Values.ToList())
                if (!item.IsDestroyed && item.OriginalGraphic != 0) { item.Graphic = item.OriginalGraphic; count++; }
            GameActions.Print($"Refreshed {count} entities with graphic replacements");
        }) { Tooltip = "将图形替换重新应用到当前世界中的所有实体" });

        root.Widgets.Add(actionRow);
        root.Widgets.Add(addEntryPanel);
        root.Widgets.Add(new MyraLabel("当前图形替换:", MyraLabel.TextStyle.H3));
        BuildFilterList();
        root.Widgets.Add(new ScrollViewer { Height = 300, Content = filtersPanel });

        return root;
    }
}
