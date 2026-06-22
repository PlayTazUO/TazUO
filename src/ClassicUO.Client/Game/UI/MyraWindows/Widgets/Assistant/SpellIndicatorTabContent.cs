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
            return new MyraLabel(TazLang.Get("spell_indicator_tabs_profile_not_loaded", "Profile not loaded"), MyraLabel.TextStyle.P);

        SpellRangeInfo? selectedSpell = null;
        var searchBox = new MyraInputBox { HintText = TazLang.Get("spell_indicator_tabs_hint_search", "Search spells..."), MinWidth = 200 };
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
                spellListPanel.Widgets.Add(new MyraLabel(TazLang.Get("spell_indicator_tabs_empty_no_spells", "No spell indicators configured"), MyraLabel.TextStyle.P));
                return;
            }

            spellListPanel.Widgets.Add(new MyraLabel(TazLang.Get("spell_indicator_tabs_label_all_spells", "All Spell Indicators:"), MyraLabel.TextStyle.H2));

            var grid = new MyraGrid();
            grid.SetupWithHeaders(
                GridColumnInfo.Auto(TazLang.Get("spell_indicator_tabs_col_id", "ID")),
                GridColumnInfo.Fill(TazLang.Get("spell_indicator_tabs_col_name", "Name")),
                GridColumnInfo.Fill(TazLang.Get("spell_indicator_tabs_col_power_words", "Power Words")),
                GridColumnInfo.Numeric(TazLang.Get("spell_indicator_tabs_col_cast_range", "Cast Range")),
                GridColumnInfo.Numeric(TazLang.Get("spell_indicator_tabs_col_cursor_size", "Cursor Size")),
                GridColumnInfo.Numeric(TazLang.Get("spell_indicator_tabs_col_cast_time", "Cast Time")),
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
                grid.AddWidget(new MyraButton(TazLang.Get("spell_indicator_tabs_btn_edit", "Edit"), () =>
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
            spellEditorPanel.Widgets.Add(new MyraLabel(TazLang.Get("spell_indicator_tabs_label_spell_config", "Spell Configuration:"), MyraLabel.TextStyle.H2));

            void Save() => SpellVisualRangeManager.Instance.DelayedSave();

            var grid = new MyraGrid();
            grid.AddColumn(new Proportion(ProportionType.Pixels, 200));
            grid.AddColumn(new Proportion(ProportionType.Pixels, 8));
            grid.AddColumn(new Proportion(ProportionType.Auto));

            int row = 0;

            grid.AddWidget(new MyraLabel(TazLang.Get("spell_indicator_tabs_label_spell_id", "Spell ID:"), MyraLabel.TextStyle.P), row, 0);
            grid.AddWidget(new MyraLabel(spell.ID.ToString(), MyraLabel.TextStyle.P), row, 2);
            row++;

            grid.AddWidget(new MyraLabel(TazLang.Get("spell_indicator_tabs_label_name", "Name:"), MyraLabel.TextStyle.P), row, 0);
            var nameBox = new MyraInputBox { Text = spell.Name, MinWidth = 200 };
            nameBox.TextChangedByUser += (_, _) =>
            {
                spell.Name = nameBox.Text ?? "";
                Save();
            };
            grid.AddWidget(nameBox, row, 2);
            row++;

            grid.AddWidget(new MyraLabel(TazLang.Get("spell_indicator_tabs_label_power_words", "Power Words:"), MyraLabel.TextStyle.P), row, 0);
            var powerWordsBox = new MyraInputBox
            {
                MinWidth = 200,
                Text = spell.PowerWords ?? "",
                Tooltip = TazLang.Get("spell_indicator_tabs_tooltip_power_words", "Power words must be exact, this is the best way we can detect spells."),
            };
            powerWordsBox.TextChangedByUser += (_, _) =>
            {
                spell.PowerWords = powerWordsBox.Text ?? "";
                Save();
            };
            grid.AddWidget(powerWordsBox, row, 2);
            row++;

            grid.AddWidget(new MyraLabel(TazLang.Get("spell_indicator_tabs_label_cursor_size", "Cursor Size:"), MyraLabel.TextStyle.P), row, 0);
            var cursorSizeSpinner = new SpinButton
            {
                Integer = true,
                Value = spell.CursorSize,
                MinWidth = 100,
                Tooltip = TazLang.Get("spell_indicator_tabs_tooltip_cursor_size", "Area to show around the cursor, for area spells that affect the area near the target.")
            };
            cursorSizeSpinner.ValueChangedByUser += (_, _) =>
            {
                spell.CursorSize = (int)Math.Clamp(cursorSizeSpinner.Value ?? 0f, 0f, int.MaxValue);
                Save();
            };
            grid.AddWidget(cursorSizeSpinner, row, 2);
            row++;

            grid.AddWidget(new MyraLabel(TazLang.Get("spell_indicator_tabs_label_cast_range", "Cast Range:"), MyraLabel.TextStyle.P), row, 0);
            var castRangeSpinner = new SpinButton { Integer = true, Value = spell.CastRange, MinWidth = 100 };
            castRangeSpinner.ValueChangedByUser += (_, _) =>
            {
                spell.CastRange = (int)Math.Clamp(castRangeSpinner.Value ?? 1f, 1f, int.MaxValue);
                Save();
            };
            grid.AddWidget(castRangeSpinner, row, 2);
            row++;

            grid.AddWidget(new MyraLabel(TazLang.Get("spell_indicator_tabs_label_cast_time", "Cast Time:"), MyraLabel.TextStyle.P), row, 0);
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

            grid.AddWidget(new MyraLabel(TazLang.Get("spell_indicator_tabs_label_max_duration", "Max Duration:"), MyraLabel.TextStyle.P), row, 0);
            var maxDurSpinner = new SpinButton
            {
                Integer = true,
                Value = spell.MaxDuration,
                MinWidth = 100,
                Tooltip = TazLang.Get("spell_indicator_tabs_tooltip_max_duration", "Fallback in case spell detection fails.")
            };
            maxDurSpinner.ValueChangedByUser += (_, _) =>
            {
                spell.MaxDuration = (int)Math.Clamp(maxDurSpinner.Value ?? 0f, 0f, int.MaxValue);
                Save();
            };
            grid.AddWidget(maxDurSpinner, row, 2);
            row++;

            grid.AddWidget(new MyraLabel(TazLang.Get("spell_indicator_tabs_label_cursor_hue", "Cursor Hue:"), MyraLabel.TextStyle.P), row, 0);
            var cursorHueSpinner = new SpinButton { Integer = true, Value = spell.CursorHue, MinWidth = 100 };
            cursorHueSpinner.ValueChangedByUser += (_, _) =>
            {
                spell.CursorHue = (ushort)Math.Clamp(cursorHueSpinner.Value ?? 0f, 0f, ushort.MaxValue);
                Save();
            };
            grid.AddWidget(cursorHueSpinner, row, 2);
            row++;

            grid.AddWidget(new MyraLabel(TazLang.Get("spell_indicator_tabs_label_range_hue", "Range Hue:"), MyraLabel.TextStyle.P), row, 0);
            var rangeHueSpinner = new SpinButton { Integer = true, Value = spell.Hue, MinWidth = 100 };
            rangeHueSpinner.ValueChangedByUser += (_, _) =>
            {
                spell.Hue = (ushort)Math.Clamp(rangeHueSpinner.Value ?? 0f, 0f, ushort.MaxValue);
                Save();
            };
            grid.AddWidget(rangeHueSpinner, row, 2);
            row++;

            grid.AddWidget(new MyraLabel(TazLang.Get("spell_indicator_tabs_label_is_linear", "Is Linear:"), MyraLabel.TextStyle.P), row, 0);
            grid.AddWidget(MyraCheckButton.CreateWithCallback(spell.IsLinear, b =>
            {
                spell.IsLinear = b;
                Save();
            }, tooltip: TazLang.Get("spell_indicator_tabs_tooltip_is_linear", "Used for spells like wall of stone that create a line.")), row, 2);
            row++;

            grid.AddWidget(new MyraLabel(TazLang.Get("spell_indicator_tabs_label_show_range_during_cast", "Show Range During Cast:"), MyraLabel.TextStyle.P), row, 0);
            grid.AddWidget(MyraCheckButton.CreateWithCallback(spell.ShowCastRangeDuringCasting, b =>
            {
                spell.ShowCastRangeDuringCasting = b;
                Save();
            }), row, 2);
            row++;

            grid.AddWidget(new MyraLabel(TazLang.Get("spell_indicator_tabs_label_freeze_while_casting", "Freeze While Casting:"), MyraLabel.TextStyle.P), row, 0);
            grid.AddWidget(MyraCheckButton.CreateWithCallback(spell.FreezeCharacterWhileCasting, b =>
            {
                spell.FreezeCharacterWhileCasting = b;
                Save();
            }, tooltip: TazLang.Get("spell_indicator_tabs_tooltip_freeze", "Prevent yourself from moving and disrupting your spell.")), row, 2);
            row++;

            grid.AddWidget(new MyraLabel(TazLang.Get("spell_indicator_tabs_label_expect_target_cursor", "Expect Target Cursor:"), MyraLabel.TextStyle.P), row, 0);
            grid.AddWidget(MyraCheckButton.CreateWithCallback(spell.ExpectTargetCursor, b =>
            {
                spell.ExpectTargetCursor = b;
                Save();
            }), row, 2);

            spellEditorPanel.Widgets.Add(grid);

            var deleteConfirmLabel = new MyraLabel(TazLang.Get("spell_indicator_tabs_label_delete_confirm_fmt", new[] { spell.Name }), MyraLabel.TextStyle.P);
            var deleteConfirm = new HorizontalStackPanel { Spacing = 4, Visible = false };
            deleteConfirm.Widgets.Add(deleteConfirmLabel);
            deleteConfirm.Widgets.Add(MyraStyle.ApplyButtonDangerStyle(new MyraButton(TazLang.Get("shared_yes", "Yes"), () =>
            {
                SpellVisualRangeManager.Instance.SpellRangeCache.Remove(spell.ID);
                Save();
                ClearSelection();
            })));
            deleteConfirm.Widgets.Add(new MyraButton(TazLang.Get("shared_no", "No"), () => deleteConfirm.Visible = false));

            var btnRow = new HorizontalStackPanel { Spacing = 4 };
            btnRow.Widgets.Add(MyraStyle.ApplyButtonDangerStyle(new MyraButton(TazLang.Get("spell_indicator_tabs_btn_delete_spell", "Delete Spell"), () =>
            {
                deleteConfirmLabel.Text = TazLang.Get("spell_indicator_tabs_label_delete_confirm_fmt", new[] { spell.Name });
                deleteConfirm.Visible = !deleteConfirm.Visible;
            }) { Tooltip = TazLang.Get("spell_indicator_tabs_tooltip_delete", "Delete this spell indicator configuration.") }));
            btnRow.Widgets.Add(new MyraButton(TazLang.Get("spell_indicator_tabs_btn_back_to_list", "Back to List"), ClearSelection));

            spellEditorPanel.Widgets.Add(btnRow);
            spellEditorPanel.Widgets.Add(deleteConfirm);
        }

        // Add New Spell panel
        var newIdBox = new MyraInputBox { MinWidth = 150, HintText = TazLang.Get("spell_indicator_tabs_hint_spell_id", "Spell ID (number)") };
        var newNameBox = new MyraInputBox { MinWidth = 200, HintText = TazLang.Get("spell_indicator_tabs_hint_spell_name", "Spell Name") };
        var addErrorLabel = new MyraLabel("", MyraLabel.TextStyle.P) { Visible = false };

        var addGrid = new MyraGrid();
        addGrid.AddColumn(new Proportion(ProportionType.Pixels, 100));
        addGrid.AddColumn(new Proportion(ProportionType.Pixels, 8));
        addGrid.AddColumn(new Proportion(ProportionType.Auto));
        addGrid.AddWidget(new MyraLabel(TazLang.Get("spell_indicator_tabs_label_spell_id", "Spell ID:"), MyraLabel.TextStyle.P), 0, 0);
        addGrid.AddWidget(newIdBox, 0, 2);
        addGrid.AddWidget(new MyraLabel(TazLang.Get("spell_indicator_tabs_label_spell_name", "Spell Name:"), MyraLabel.TextStyle.P), 1, 0);
        addGrid.AddWidget(newNameBox, 1, 2);

        var addBtnRow = new HorizontalStackPanel { Spacing = 4 };
        addBtnRow.Widgets.Add(new MyraButton(TazLang.Get("spell_indicator_tabs_btn_create_spell", "Create Spell"), () =>
        {
            string idText = newIdBox.Text ?? "";
            string nameText = newNameBox.Text ?? "";

            if (string.IsNullOrWhiteSpace(idText) || string.IsNullOrWhiteSpace(nameText))
            {
                addErrorLabel.Text = TazLang.Get("spell_indicator_tabs_error_fill_fields", "Please fill in both Spell ID and Name.");
                addErrorLabel.Visible = true;
                return;
            }

            if (!int.TryParse(idText, out int spellId))
            {
                addErrorLabel.Text = TazLang.Get("spell_indicator_tabs_error_invalid_id", "Spell ID must be a valid number.");
                addErrorLabel.Visible = true;
                return;
            }

            if (spellId <= 0)
            {
                addErrorLabel.Text = TazLang.Get("spell_indicator_tabs_error_positive_id", "Spell ID must be a positive number.");
                addErrorLabel.Visible = true;
                return;
            }

            if (SpellVisualRangeManager.Instance.SpellRangeCache.ContainsKey(spellId))
            {
                addErrorLabel.Text = TazLang.Get("spell_indicator_tabs_error_id_exists", "A spell with this ID already exists.");
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
        addBtnRow.Widgets.Add(new MyraButton(TazLang.Get("shared_cancel", "Cancel"), () =>
        {
            newIdBox.Text = "";
            newNameBox.Text = "";
            addErrorLabel.Visible = false;
            ClearSelection();
        }));

        addNewPanel.Widgets.Add(new MyraLabel(TazLang.Get("spell_indicator_tabs_label_create_new", "Create a new spell indicator configuration:"), MyraLabel.TextStyle.H2));
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
        searchRow.Widgets.Add(new MyraLabel(TazLang.Get("spell_indicator_tabs_label_spell_search", "Spell search:"), MyraLabel.TextStyle.P));
        searchRow.Widgets.Add(searchBox);
        searchRow.Widgets.Add(new MyraButton(TazLang.Get("spell_indicator_tabs_btn_clear", "Clear"), ClearSelection));
        searchRow.Widgets.Add(new MyraButton(TazLang.Get("spell_indicator_tabs_btn_add_new_spell", "Add New Spell"), () =>
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
            TazLang.Get("spell_indicator_tabs_checkbox_enable", "Enable Spell Indicators"),
            TazLang.Get("spell_indicator_tabs_checkbox_enable_tooltip", "Enable visual spell range indicators that show casting range and area of effect for spells.")));
        root.Widgets.Add(searchRow);
        root.Widgets.Add(spellListPanel);
        root.Widgets.Add(spellEditorPanel);
        root.Widgets.Add(addNewPanel);

        return root;
    }
}
