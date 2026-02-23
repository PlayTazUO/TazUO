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
            return new MyraLabel("Profile not loaded", MyraLabel.Style.P);

        SpellRangeInfo? selectedSpell = null;
        var searchBox = new TextBox { MinWidth = 200, HintText = "Search spells..." };

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
                spellListPanel.Widgets.Add(new MyraLabel("No spell indicators configured", MyraLabel.Style.P));
                return;
            }

            spellListPanel.Widgets.Add(new MyraLabel("All Spell Indicators:", MyraLabel.Style.H2));

            var grid = new MyraGrid();
            grid.AddColumn(new Proportion(ProportionType.Auto), 13);
            grid.AddColumn(new Proportion(ProportionType.Pixels, 10));

            grid.AddWidget(new MyraLabel("ID", MyraLabel.Style.H3), 0, 0);
            grid.AddWidget(new MyraLabel("Name", MyraLabel.Style.H3), 0, 2);
            grid.AddWidget(new MyraLabel("Power Words", MyraLabel.Style.H3), 0, 4);
            grid.AddWidget(new MyraLabel("Cast Range", MyraLabel.Style.H3), 0, 6);
            grid.AddWidget(new MyraLabel("Cursor Size", MyraLabel.Style.H3), 0, 8);
            grid.AddWidget(new MyraLabel("Cast Time", MyraLabel.Style.H3), 0, 10);

            int row = 1;
            foreach (SpellRangeInfo spell in spells)
            {
                SpellRangeInfo s = spell;
                grid.AddWidget(new MyraLabel(s.ID.ToString(), MyraLabel.Style.P), row, 0);
                grid.AddWidget(new MyraLabel(s.Name, MyraLabel.Style.P), row, 2);
                grid.AddWidget(new MyraLabel(s.PowerWords ?? "", MyraLabel.Style.P), row, 4);
                grid.AddWidget(new MyraLabel(s.CastRange.ToString(), MyraLabel.Style.P), row, 6);
                grid.AddWidget(new MyraLabel(s.CursorSize.ToString(), MyraLabel.Style.P), row, 8);
                grid.AddWidget(new MyraLabel(s.CastTime.ToString("F1"), MyraLabel.Style.P), row, 10);
                grid.AddWidget(new MyraButton("Edit", () =>
                {
                    selectedSpell = s;
                    searchBox.Text = s.Name;
                    BuildEditor(s);
                    ShowEditor();
                }), row, 12);
                row++;
            }

            var scrollViewer = new ScrollViewer { Height = 300, Content = grid };
            spellListPanel.Widgets.Add(scrollViewer);
        }

        void BuildEditor(SpellRangeInfo spell)
        {
            spellEditorPanel.Widgets.Clear();
            spellEditorPanel.Widgets.Add(new MyraLabel("Spell Configuration:", MyraLabel.Style.H2));

            void Save() => SpellVisualRangeManager.Instance.DelayedSave();

            var grid = new MyraGrid();
            grid.AddColumn(new Proportion(ProportionType.Pixels, 200));
            grid.AddColumn(new Proportion(ProportionType.Pixels, 8));
            grid.AddColumn(new Proportion(ProportionType.Auto));

            int row = 0;

            grid.AddWidget(new MyraLabel("Spell ID:", MyraLabel.Style.P), row, 0);
            grid.AddWidget(new MyraLabel(spell.ID.ToString(), MyraLabel.Style.P), row, 2);
            row++;

            grid.AddWidget(new MyraLabel("Name:", MyraLabel.Style.P), row, 0);
            var nameBox = new TextBox { Text = spell.Name, MinWidth = 200 };
            nameBox.TextChangedByUser += (_, _) => { spell.Name = nameBox.Text ?? ""; Save(); };
            grid.AddWidget(nameBox, row, 2);
            row++;

            grid.AddWidget(new MyraLabel("Power Words:", MyraLabel.Style.P), row, 0);
            var powerWordsBox = new TextBox { Text = spell.PowerWords ?? "", MinWidth = 200, Tooltip = "Power words must be exact, this is the best way we can detect spells." };
            powerWordsBox.TextChangedByUser += (_, _) => { spell.PowerWords = powerWordsBox.Text ?? ""; Save(); };
            grid.AddWidget(powerWordsBox, row, 2);
            row++;

            grid.AddWidget(new MyraLabel("Cursor Size:", MyraLabel.Style.P), row, 0);
            var cursorSizeSpinner = new SpinButton { Integer = true, Value = spell.CursorSize, MinWidth = 100, Tooltip = "Area to show around the cursor, for area spells that affect the area near the target." };
            cursorSizeSpinner.ValueChangedByUser += (_, _) => { spell.CursorSize = (int)Math.Clamp(cursorSizeSpinner.Value ?? 0f, 0f, int.MaxValue); Save(); };
            grid.AddWidget(cursorSizeSpinner, row, 2);
            row++;

            grid.AddWidget(new MyraLabel("Cast Range:", MyraLabel.Style.P), row, 0);
            var castRangeSpinner = new SpinButton { Integer = true, Value = spell.CastRange, MinWidth = 100 };
            castRangeSpinner.ValueChangedByUser += (_, _) => { spell.CastRange = (int)Math.Clamp(castRangeSpinner.Value ?? 1f, 1f, int.MaxValue); Save(); };
            grid.AddWidget(castRangeSpinner, row, 2);
            row++;

            grid.AddWidget(new MyraLabel("Cast Time:", MyraLabel.Style.P), row, 0);
            var castTimeBox = new TextBox { Text = spell.CastTime.ToString(), MinWidth = 100 };
            castTimeBox.TextChangedByUser += (_, _) => { if (double.TryParse(castTimeBox.Text, out double v)) { spell.CastTime = Math.Max(0.0, v); Save(); } };
            grid.AddWidget(castTimeBox, row, 2);
            row++;

            grid.AddWidget(new MyraLabel("Max Duration:", MyraLabel.Style.P), row, 0);
            var maxDurSpinner = new SpinButton { Integer = true, Value = spell.MaxDuration, MinWidth = 100, Tooltip = "Fallback in case spell detection fails." };
            maxDurSpinner.ValueChangedByUser += (_, _) => { spell.MaxDuration = (int)Math.Clamp(maxDurSpinner.Value ?? 0f, 0f, int.MaxValue); Save(); };
            grid.AddWidget(maxDurSpinner, row, 2);
            row++;

            grid.AddWidget(new MyraLabel("Cursor Hue:", MyraLabel.Style.P), row, 0);
            var cursorHueSpinner = new SpinButton { Integer = true, Value = spell.CursorHue, MinWidth = 100 };
            cursorHueSpinner.ValueChangedByUser += (_, _) => { spell.CursorHue = (ushort)Math.Clamp(cursorHueSpinner.Value ?? 0f, 0f, ushort.MaxValue); Save(); };
            grid.AddWidget(cursorHueSpinner, row, 2);
            row++;

            grid.AddWidget(new MyraLabel("Range Hue:", MyraLabel.Style.P), row, 0);
            var rangeHueSpinner = new SpinButton { Integer = true, Value = spell.Hue, MinWidth = 100 };
            rangeHueSpinner.ValueChangedByUser += (_, _) => { spell.Hue = (ushort)Math.Clamp(rangeHueSpinner.Value ?? 0f, 0f, ushort.MaxValue); Save(); };
            grid.AddWidget(rangeHueSpinner, row, 2);
            row++;

            grid.AddWidget(new MyraLabel("Is Linear:", MyraLabel.Style.P), row, 0);
            grid.AddWidget(MyraCheckButton.CreateWithCallback(spell.IsLinear, b => { spell.IsLinear = b; Save(); }, tooltip: "Used for spells like wall of stone that create a line."), row, 2);
            row++;

            grid.AddWidget(new MyraLabel("Show Range During Cast:", MyraLabel.Style.P), row, 0);
            grid.AddWidget(MyraCheckButton.CreateWithCallback(spell.ShowCastRangeDuringCasting, b => { spell.ShowCastRangeDuringCasting = b; Save(); }), row, 2);
            row++;

            grid.AddWidget(new MyraLabel("Freeze While Casting:", MyraLabel.Style.P), row, 0);
            grid.AddWidget(MyraCheckButton.CreateWithCallback(spell.FreezeCharacterWhileCasting, b => { spell.FreezeCharacterWhileCasting = b; Save(); }, tooltip: "Prevent yourself from moving and disrupting your spell."), row, 2);
            row++;

            grid.AddWidget(new MyraLabel("Expect Target Cursor:", MyraLabel.Style.P), row, 0);
            grid.AddWidget(MyraCheckButton.CreateWithCallback(spell.ExpectTargetCursor, b => { spell.ExpectTargetCursor = b; Save(); }), row, 2);

            spellEditorPanel.Widgets.Add(grid);

            var deleteConfirmLabel = new MyraLabel($"Delete '{spell.Name}'?", MyraLabel.Style.P);
            var deleteConfirm = new HorizontalStackPanel { Spacing = 4, Visible = false };
            deleteConfirm.Widgets.Add(deleteConfirmLabel);
            deleteConfirm.Widgets.Add(new MyraButton("Yes", () =>
            {
                SpellVisualRangeManager.Instance.SpellRangeCache.Remove(spell.ID);
                Save();
                ClearSelection();
            }));
            deleteConfirm.Widgets.Add(new MyraButton("No", () => deleteConfirm.Visible = false));

            var btnRow = new HorizontalStackPanel { Spacing = 4 };
            btnRow.Widgets.Add(new MyraButton("Delete Spell", () =>
            {
                deleteConfirmLabel.Text = $"Delete '{spell.Name}'?";
                deleteConfirm.Visible = !deleteConfirm.Visible;
            }) { Tooltip = "Delete this spell indicator configuration." });
            btnRow.Widgets.Add(new MyraButton("Back to List", ClearSelection));

            spellEditorPanel.Widgets.Add(btnRow);
            spellEditorPanel.Widgets.Add(deleteConfirm);
        }

        // Add New Spell panel
        var newIdBox = new TextBox { MinWidth = 150, HintText = "Spell ID (number)" };
        var newNameBox = new TextBox { MinWidth = 200, HintText = "Spell Name" };
        var addErrorLabel = new MyraLabel("", MyraLabel.Style.P) { Visible = false };

        var addGrid = new MyraGrid();
        addGrid.AddColumn(new Proportion(ProportionType.Pixels, 100));
        addGrid.AddColumn(new Proportion(ProportionType.Pixels, 8));
        addGrid.AddColumn(new Proportion(ProportionType.Auto));
        addGrid.AddWidget(new MyraLabel("Spell ID:", MyraLabel.Style.P), 0, 0);
        addGrid.AddWidget(newIdBox, 0, 2);
        addGrid.AddWidget(new MyraLabel("Spell Name:", MyraLabel.Style.P), 1, 0);
        addGrid.AddWidget(newNameBox, 1, 2);

        var addBtnRow = new HorizontalStackPanel { Spacing = 4 };
        addBtnRow.Widgets.Add(new MyraButton("Create Spell", () =>
        {
            string idText = newIdBox.Text ?? "";
            string nameText = newNameBox.Text ?? "";

            if (string.IsNullOrWhiteSpace(idText) || string.IsNullOrWhiteSpace(nameText))
            {
                addErrorLabel.Text = "Please fill in both Spell ID and Name.";
                addErrorLabel.Visible = true;
                return;
            }
            if (!int.TryParse(idText, out int spellId))
            {
                addErrorLabel.Text = "Spell ID must be a valid number.";
                addErrorLabel.Visible = true;
                return;
            }
            if (spellId <= 0)
            {
                addErrorLabel.Text = "Spell ID must be a positive number.";
                addErrorLabel.Visible = true;
                return;
            }
            if (SpellVisualRangeManager.Instance.SpellRangeCache.ContainsKey(spellId))
            {
                addErrorLabel.Text = "A spell with this ID already exists.";
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

        addNewPanel.Widgets.Add(new MyraLabel("Create a new spell indicator configuration:", MyraLabel.Style.H2));
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

        var searchRow = new HorizontalStackPanel { Spacing = 4 };
        searchRow.Widgets.Add(new MyraLabel("Spell search:", MyraLabel.Style.P));
        searchRow.Widgets.Add(searchBox);
        searchRow.Widgets.Add(new MyraButton("Clear", ClearSelection));
        searchRow.Widgets.Add(new MyraButton("Add New Spell", () =>
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
            "Enable Spell Indicators",
            "Enable visual spell range indicators that show casting range and area of effect for spells."));
        root.Widgets.Add(searchRow);
        root.Widgets.Add(spellListPanel);
        root.Widgets.Add(spellEditorPanel);
        root.Widgets.Add(addNewPanel);

        return root;
    }
}
