#nullable enable
using System;
using System.Linq;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Managers.SpellVisualRange;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Assistant;

public static class SpellIndicatorTabContent
{
    public static Widget Build()
    {
        Profile profile = ProfileManager.CurrentProfile;
        if (profile == null)
            return new MyraLabel("未加载配置文件", MyraLabel.TextStyle.P);

        SpellRangeInfo? selectedSpell = null;
        var searchBox = new MyraInputBox { HintText = "搜索法术...", MinWidth = 200 };
        var spellListPanel = new VerticalStackPanel { Spacing = 2 };
        var spellEditorPanel = new VerticalStackPanel { Spacing = 4, Visible = false };
        var addNewPanel = new VerticalStackPanel { Spacing = 4, Visible = false };

        void ShowList()
        {
            spellListPanel.Visible = true;
            spellEditorPanel.Visible = false;
            addNewPanel.Visible = false;
        }

        void ShowEditor()
        {
            spellListPanel.Visible = false;
            spellEditorPanel.Visible = true;
            addNewPanel.Visible = false;
        }

        void ShowAddNew()
        {
            spellListPanel.Visible = false;
            spellEditorPanel.Visible = false;
            addNewPanel.Visible = true;
        }

        void ClearSelection()
        {
            selectedSpell = null;
            searchBox.Text = "";
            BuildSpellList();
            ShowList();
        }

        void BuildSpellList()
        {
            spellListPanel.Widgets.Clear();

            var spells = SpellVisualRangeManager.Instance.SpellRangeCache.Values.OrderBy(s => s.Name).ToList();

            if (spells.Count == 0)
            {
                spellListPanel.Widgets.Add(new MyraLabel("没有配置法术指示器", MyraLabel.TextStyle.P));
                return;
            }

            spellListPanel.Widgets.Add(new MyraLabel("所有法术指示器:", MyraLabel.TextStyle.H2));

            var grid = new MyraGrid();
            grid.SetupWithHeaders(
                GridColumnInfo.Auto("ID"),
                GridColumnInfo.Fill("名称"),
                GridColumnInfo.Fill("咒语"),
                GridColumnInfo.Numeric("施法范围"),
                GridColumnInfo.Numeric("光标大小"),
                GridColumnInfo.Numeric("施法时间"),
                GridColumnInfo.Auto("")
            );

            int row = 1;
            foreach (SpellRangeInfo spell in spells)
            {
                SpellRangeInfo s = spell;
                grid.AddWidget(new MyraLabel(s.ID.ToString(), MyraLabel.TextStyle.P, MyraLabel.AlignMode.Right), row,
                    0);
                grid.AddWidget(new MyraLabel(s.Name, MyraLabel.TextStyle.P), row, 1);
                grid.AddWidget(new MyraLabel(s.PowerWords ?? "", MyraLabel.TextStyle.P), row, 2);
                grid.AddWidget(new MyraLabel(s.CastRange.ToString(), MyraLabel.TextStyle.P, MyraLabel.AlignMode.Right),
                    row, 3);
                grid.AddWidget(new MyraLabel(s.CursorSize.ToString(), MyraLabel.TextStyle.P, MyraLabel.AlignMode.Right),
                    row, 4);
                grid.AddWidget(
                    new MyraLabel(s.CastTime.ToString("F1"), MyraLabel.TextStyle.P, MyraLabel.AlignMode.Right), row, 5);
                grid.AddWidget(new MyraButton("编辑", () =>
                {
                    selectedSpell = s;
                    searchBox.Text = s.Name;
                    BuildEditor(s);
                    ShowEditor();
                }), row, 6);
                row++;
            }

            var scrollViewer = new ScrollViewer { MaxHeight = 300, Content = grid };
            spellListPanel.Widgets.Add(scrollViewer);
        }

        void BuildEditor(SpellRangeInfo spell)
        {
            spellEditorPanel.Widgets.Clear();
            spellEditorPanel.Widgets.Add(new MyraLabel("法术配置:", MyraLabel.TextStyle.H2));

            void Save() => SpellVisualRangeManager.Instance.DelayedSave();

            var grid = new MyraGrid();
            grid.AddColumn(new Proportion(ProportionType.Pixels, 200));
            grid.AddColumn(new Proportion(ProportionType.Pixels, 8));
            grid.AddColumn(new Proportion(ProportionType.Auto));

            int row = 0;

            grid.AddWidget(new MyraLabel("法术ID:", MyraLabel.TextStyle.P), row, 0);
            grid.AddWidget(new MyraLabel(spell.ID.ToString(), MyraLabel.TextStyle.P), row, 2);
            row++;

            grid.AddWidget(new MyraLabel("名称:", MyraLabel.TextStyle.P), row, 0);
            var nameBox = new MyraInputBox { Text = spell.Name, MinWidth = 200 };
            nameBox.TextChangedByUser += (_, _) =>
            {
                spell.Name = nameBox.Text ?? "";
                Save();
            };
            grid.AddWidget(nameBox, row, 2);
            row++;

            grid.AddWidget(new MyraLabel("咒语:", MyraLabel.TextStyle.P), row, 0);
            var powerWordsBox = new MyraInputBox
            {
                MinWidth = 200,
                Text = spell.PowerWords ?? "",
                Tooltip = "咒语必须精确匹配，这是我们检测法术的最佳方式。",
            };
            powerWordsBox.TextChangedByUser += (_, _) =>
            {
                spell.PowerWords = powerWordsBox.Text ?? "";
                Save();
            };
            grid.AddWidget(powerWordsBox, row, 2);
            row++;

            grid.AddWidget(new MyraLabel("光标大小:", MyraLabel.TextStyle.P), row, 0);
            var cursorSizeSpinner = new SpinButton
            {
                Integer = true,
                Value = spell.CursorSize,
                MinWidth = 100,
                Tooltip = "光标周围显示的区域，用于影响目标附近区域的范围法术。"
            };
            cursorSizeSpinner.ValueChangedByUser += (_, _) =>
            {
                spell.CursorSize = (int)Math.Clamp(cursorSizeSpinner.Value ?? 0f, 0f, int.MaxValue);
                Save();
            };
            grid.AddWidget(cursorSizeSpinner, row, 2);
            row++;

            grid.AddWidget(new MyraLabel("施法范围:", MyraLabel.TextStyle.P), row, 0);
            var castRangeSpinner = new SpinButton { Integer = true, Value = spell.CastRange, MinWidth = 100 };
            castRangeSpinner.ValueChangedByUser += (_, _) =>
            {
                spell.CastRange = (int)Math.Clamp(castRangeSpinner.Value ?? 1f, 1f, int.MaxValue);
                Save();
            };
            grid.AddWidget(castRangeSpinner, row, 2);
            row++;

            grid.AddWidget(new MyraLabel("施法时间:", MyraLabel.TextStyle.P), row, 0);
            var castTimeBox = new MyraInputBox { Text = spell.CastTime.ToString(), MinWidth = 100 };
            castTimeBox.TextChangedByUser += (_, _) =>
            {
                if (double.TryParse(castTimeBox.Text, out double v))
                {
                    spell.CastTime = Math.Max(0.0, v);
                    Save();
                }
            };
            grid.AddWidget(castTimeBox, row, 2);
            row++;

            grid.AddWidget(new MyraLabel("最大持续时间:", MyraLabel.TextStyle.P), row, 0);
            var maxDurSpinner = new SpinButton
            {
                Integer = true,
                Value = spell.MaxDuration,
                MinWidth = 100,
                Tooltip = "法术检测失败时的后备方案。"
            };
            maxDurSpinner.ValueChangedByUser += (_, _) =>
            {
                spell.MaxDuration = (int)Math.Clamp(maxDurSpinner.Value ?? 0f, 0f, int.MaxValue);
                Save();
            };
            grid.AddWidget(maxDurSpinner, row, 2);
            row++;

            grid.AddWidget(new MyraLabel("光标色调:", MyraLabel.TextStyle.P), row, 0);
            var cursorHueSpinner = new SpinButton { Integer = true, Value = spell.CursorHue, MinWidth = 100 };
            cursorHueSpinner.ValueChangedByUser += (_, _) =>
            {
                spell.CursorHue = (ushort)Math.Clamp(cursorHueSpinner.Value ?? 0f, 0f, ushort.MaxValue);
                Save();
            };
            grid.AddWidget(cursorHueSpinner, row, 2);
            row++;

            grid.AddWidget(new MyraLabel("范围色调:", MyraLabel.TextStyle.P), row, 0);
            var rangeHueSpinner = new SpinButton { Integer = true, Value = spell.Hue, MinWidth = 100 };
            rangeHueSpinner.ValueChangedByUser += (_, _) =>
            {
                spell.Hue = (ushort)Math.Clamp(rangeHueSpinner.Value ?? 0f, 0f, ushort.MaxValue);
                Save();
            };
            grid.AddWidget(rangeHueSpinner, row, 2);
            row++;

            grid.AddWidget(new MyraLabel("是否线性:", MyraLabel.TextStyle.P), row, 0);
            grid.AddWidget(MyraCheckButton.CreateWithCallback(spell.IsLinear, b =>
            {
                spell.IsLinear = b;
                Save();
            }, tooltip: "用于像石墙这样创建直线的法术。"), row, 2);
            row++;

            grid.AddWidget(new MyraLabel("施法时显示范围:", MyraLabel.TextStyle.P), row, 0);
            grid.AddWidget(MyraCheckButton.CreateWithCallback(spell.ShowCastRangeDuringCasting, b =>
            {
                spell.ShowCastRangeDuringCasting = b;
                Save();
            }), row, 2);
            row++;

            grid.AddWidget(new MyraLabel("施法时冻结:", MyraLabel.TextStyle.P), row, 0);
            grid.AddWidget(MyraCheckButton.CreateWithCallback(spell.FreezeCharacterWhileCasting, b =>
            {
                spell.FreezeCharacterWhileCasting = b;
                Save();
            }, tooltip: "防止自己移动并中断法术。"), row, 2);
            row++;

            grid.AddWidget(new MyraLabel("预期目标光标:", MyraLabel.TextStyle.P), row, 0);
            grid.AddWidget(MyraCheckButton.CreateWithCallback(spell.ExpectTargetCursor, b =>
            {
                spell.ExpectTargetCursor = b;
                Save();
            }), row, 2);

            spellEditorPanel.Widgets.Add(grid);

            var deleteConfirmLabel = new MyraLabel($"删除 '{spell.Name}'？", MyraLabel.TextStyle.P);
            var deleteConfirm = new HorizontalStackPanel { Spacing = 4, Visible = false };
            deleteConfirm.Widgets.Add(deleteConfirmLabel);
            deleteConfirm.Widgets.Add(MyraStyle.ApplyButtonDangerStyle(new MyraButton("是", () =>
            {
                SpellVisualRangeManager.Instance.SpellRangeCache.Remove(spell.ID);
                Save();
                ClearSelection();
            })));
            deleteConfirm.Widgets.Add(new MyraButton("否", () => deleteConfirm.Visible = false));

            var btnRow = new HorizontalStackPanel { Spacing = 4 };
            btnRow.Widgets.Add(MyraStyle.ApplyButtonDangerStyle(new MyraButton("删除法术", () =>
            {
                deleteConfirmLabel.Text = $"删除 '{spell.Name}'？";
                deleteConfirm.Visible = !deleteConfirm.Visible;
            }) { Tooltip = "删除此法术指示器配置。" }));
            btnRow.Widgets.Add(new MyraButton("返回列表", ClearSelection));

            spellEditorPanel.Widgets.Add(btnRow);
            spellEditorPanel.Widgets.Add(deleteConfirm);
        }

        // Add New Spell panel
        var newIdBox = new MyraInputBox { MinWidth = 150, HintText = "法术ID（数字）" };
        var newNameBox = new MyraInputBox { MinWidth = 200, HintText = "法术名称" };
        var addErrorLabel = new MyraLabel("", MyraLabel.TextStyle.P) { Visible = false };

        var addGrid = new MyraGrid();
        addGrid.AddColumn(new Proportion(ProportionType.Pixels, 100));
        addGrid.AddColumn(new Proportion(ProportionType.Pixels, 8));
        addGrid.AddColumn(new Proportion(ProportionType.Auto));
        addGrid.AddWidget(new MyraLabel("法术ID:", MyraLabel.TextStyle.P), 0, 0);
        addGrid.AddWidget(newIdBox, 0, 2);
        addGrid.AddWidget(new MyraLabel("法术名称:", MyraLabel.TextStyle.P), 1, 0);
        addGrid.AddWidget(newNameBox, 1, 2);

        var addBtnRow = new HorizontalStackPanel { Spacing = 4 };
        addBtnRow.Widgets.Add(new MyraButton("创建法术", () =>
        {
            string idText = newIdBox.Text ?? "";
            string nameText = newNameBox.Text ?? "";

            if (string.IsNullOrWhiteSpace(idText) || string.IsNullOrWhiteSpace(nameText))
            {
                addErrorLabel.Text = "请填写法术ID和名称。";
                addErrorLabel.Visible = true;
                return;
            }

            if (!int.TryParse(idText, out int spellId))
            {
                addErrorLabel.Text = "法术ID必须是有效数字。";
                addErrorLabel.Visible = true;
                return;
            }

            if (spellId <= 0)
            {
                addErrorLabel.Text = "法术ID必须是正数。";
                addErrorLabel.Visible = true;
                return;
            }

            if (SpellVisualRangeManager.Instance.SpellRangeCache.ContainsKey(spellId))
            {
                addErrorLabel.Text = "此ID的法术已存在。";
                addErrorLabel.Visible = true;
                return;
            }

            var newSpell = new SpellRangeInfo
            {
                ID = spellId,
                Name = nameText.Trim(),
                PowerWords = "",
                CursorSize = 0,
                CastRange = 1,
                Hue = 32,
                CursorHue = 10,
                MaxDuration = 10,
                IsLinear = false,
                CastTime = 0.0,
                ShowCastRangeDuringCasting = false,
                FreezeCharacterWhileCasting = false,
                ExpectTargetCursor = false
            };

            SpellVisualRangeManager.Instance.SpellRangeCache.Add(spellId, newSpell);
            SpellVisualRangeManager.Instance.DelayedSave();

            newIdBox.Text = "";
            newNameBox.Text = "";
            addErrorLabel.Visible = false;

            selectedSpell = newSpell;
            searchBox.Text = newSpell.Name;
            BuildEditor(newSpell);
            ShowEditor();
        }));
        addBtnRow.Widgets.Add(new MyraButton("Cancel", () =>
        {
            newIdBox.Text = "";
            newNameBox.Text = "";
            addErrorLabel.Visible = false;
            ClearSelection();
        }));

        addNewPanel.Widgets.Add(new MyraLabel("创建新的法术指示器配置:", MyraLabel.TextStyle.H2));
        addNewPanel.Widgets.Add(addGrid);
        addNewPanel.Widgets.Add(addErrorLabel);
        addNewPanel.Widgets.Add(addBtnRow);

        // Wire up search box
        searchBox.TextChangedByUser += (_, _) =>
        {
            string query = searchBox.Text ?? "";
            if (string.IsNullOrWhiteSpace(query))
            {
                if (selectedSpell != null)
                {
                    selectedSpell = null;
                    BuildSpellList();
                    ShowList();
                }

                return;
            }

            SpellRangeInfo? found = null;
            if (SpellDefinition.TryGetSpellFromName(query, out SpellDefinition spellDef))
                SpellVisualRangeManager.Instance.SpellRangeCache.TryGetValue(spellDef.ID, out found);

            string lowerQuery = query.ToLower();
            found ??= SpellVisualRangeManager.Instance.SpellRangeCache.Values
                .FirstOrDefault(s => s.Name.ToLower().Contains(lowerQuery));

            if (found != null && found != selectedSpell)
            {
                selectedSpell = found;
                BuildEditor(found);
                ShowEditor();
            }
        };

        var searchRow = new HorizontalStackPanel { Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
        searchRow.Widgets.Add(new MyraLabel("法术搜索:", MyraLabel.TextStyle.P));
        searchRow.Widgets.Add(searchBox);
        searchRow.Widgets.Add(new MyraButton("清除", ClearSelection));
        searchRow.Widgets.Add(new MyraButton("添加新法术", () =>
        {
            if (addNewPanel.Visible)
                ClearSelection();
            else
            {
                selectedSpell = null;
                searchBox.Text = "";
                ShowAddNew();
            }
        }));

        BuildSpellList();

        var root = new VerticalStackPanel { Spacing = 6 };
        root.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.EnableSpellIndicators,
            b => profile.EnableSpellIndicators = b,
            "启用法术指示器",
            "启用可视化法术范围指示器，显示法术的施法范围和效果区域。"));
        root.Widgets.Add(searchRow);
        root.Widgets.Add(spellListPanel);
        root.Widgets.Add(spellEditorPanel);
        root.Widgets.Add(addNewPanel);

        return root;
    }
}
