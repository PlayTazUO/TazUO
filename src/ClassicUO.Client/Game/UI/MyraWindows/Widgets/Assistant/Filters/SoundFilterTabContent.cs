#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using ClassicUO.Game.Managers;
using ClassicUO.Utility;
using Myra.Graphics2D.UI;
using TextBox = Myra.Graphics2D.UI.TextBox;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Assistant.Filters;

public static class SoundFilterTabContent
{
    public static Widget Build()
    {
        var root = new VerticalStackPanel { Spacing = 6 };

        root.Widgets.Add(new MyraLabel(
            "Sound Filter allows you to mute specific in-game sounds by their ID.",
            MyraLabel.Style.P));

        var lastSoundPanel = new VerticalStackPanel { Spacing = 2 };
        var filtersPanel = new VerticalStackPanel { Spacing = 2 };

        void BuildFilterList()
        {
            filtersPanel.Widgets.Clear();
            List<int> filterList = SoundFilterManager.Instance.FilteredSounds.OrderBy(x => x).ToList();

            if (filterList.Count == 0)
            {
                filtersPanel.Widgets.Add(new MyraLabel("No sounds filtered.", MyraLabel.Style.P));
                return;
            }

            filtersPanel.Widgets.Add(new MyraLabel($"Total: {filterList.Count} sound(s) filtered", MyraLabel.Style.P));

            filtersPanel.Widgets.Add(MyraStyle.ApplyButtonDangerStyle(new MyraButton("Clear All Filters", () =>
            {
                SoundFilterManager.Instance.Clear();
                BuildFilterList();
            })));

            var grid = new MyraGrid();
            grid.AddColumn(new Proportion(ProportionType.Auto));
            grid.AddColumn(new Proportion(ProportionType.Auto));
            grid.AddColumn(new Proportion(ProportionType.Auto));
            MyraStyle.ApplyStandardGridStyling(grid);

            grid.AddWidget(new MyraLabel("Sound ID", MyraLabel.Style.H3), 0, 0);
            grid.AddWidget(new MyraLabel("Actions", MyraLabel.Style.H3), 0, 1);
            grid.AddWidget(new MyraLabel("Del", MyraLabel.Style.H3), 0, 2);

            int dataRow = 1;
            for (int i = filterList.Count - 1; i >= 0; i--)
            {
                int soundId = filterList[i];

                // Track current ID so we can remove-old/add-new on edit without rebuilding
                int[] current = { soundId };
                var soundBox = new TextBox { Text = soundId.ToString(), Width = 80 };
                soundBox.TextChangedByUser += (_, _) =>
                {
                    if (int.TryParse(soundBox.Text, out int newId))
                    {
                        newId = Math.Clamp(newId, 0, 65535);
                        if (newId != current[0])
                        {
                            SoundFilterManager.Instance.RemoveFilter(current[0]);
                            SoundFilterManager.Instance.AddFilter(newId);
                            current[0] = newId;
                        }
                    }
                };
                grid.AddWidget(soundBox, dataRow, 0);

                int capturedId = soundId;
                var actionsPanel = new HorizontalStackPanel { Spacing = 2 };
                actionsPanel.Widgets.Add(new MyraButton("Play", () =>
                    Client.Game.Audio.PlaySound(current[0], true))
                { Tooltip = "Test play this sound (bypasses filter)" });
                grid.AddWidget(actionsPanel, dataRow, 1);

                grid.AddWidget(MyraStyle.ApplyButtonDangerStyle(new MyraButton("X", () =>
                {
                    SoundFilterManager.Instance.RemoveFilter(current[0]);
                    BuildFilterList();
                }) { Tooltip = "Delete this filter" }), dataRow, 2);

                dataRow++;
            }

            filtersPanel.Widgets.Add(grid);
        }

        void BuildLastSoundSection()
        {
            lastSoundPanel.Widgets.Clear();
            lastSoundPanel.Widgets.Add(new MyraLabel("Last Sound Played:", MyraLabel.Style.H3));

            int lastSoundId = Client.Game.Audio.LastPlayedSoundId;
            if (lastSoundId >= 0)
            {
                var row = new HorizontalStackPanel { Spacing = 4 };
                row.Widgets.Add(new MyraLabel($"Sound ID: {lastSoundId}", MyraLabel.Style.P));
                row.Widgets.Add(new MyraButton("Add Filter", () =>
                {
                    SoundFilterManager.Instance.AddFilter(lastSoundId);
                    BuildFilterList();
                }) { Tooltip = "Add this sound to the filter list" });
                row.Widgets.Add(new MyraButton("Play Again", () =>
                    Client.Game.Audio.PlaySound(lastSoundId, true))
                { Tooltip = "Play this sound again" });
                row.Widgets.Add(new MyraButton("Refresh", () => BuildLastSoundSection())
                    { Tooltip = "Refresh last played sound display" });
                lastSoundPanel.Widgets.Add(row);
                lastSoundPanel.Widgets.Add(new MyraLabel(
                    "Tip: Play a sound in-game to see its ID above, then click Add Filter.",
                    MyraLabel.Style.P));
            }
            else
            {
                var row = new HorizontalStackPanel { Spacing = 4 };
                row.Widgets.Add(new MyraLabel("No sound played yet.", MyraLabel.Style.P));
                row.Widgets.Add(new MyraButton("Refresh", () => BuildLastSoundSection())
                    { Tooltip = "Refresh last played sound display" });
                lastSoundPanel.Widgets.Add(row);
            }
        }

        var addFilterPanel = new VerticalStackPanel { Visible = false, Spacing = 4 };
        var newSoundBox = new TextBox { HintText = "Sound ID (0-65535)", Width = 120 };

        var addConfirmRow = new HorizontalStackPanel { Spacing = 4 };
        addConfirmRow.Widgets.Add(new MyraButton("Add", () =>
        {
            if (int.TryParse(newSoundBox.Text, out int soundId))
            {
                soundId = Math.Clamp(soundId, 0, 65535);
                SoundFilterManager.Instance.AddFilter(soundId);
                newSoundBox.Text = "";
                addFilterPanel.Visible = false;
                BuildFilterList();
            }
        }));
        addConfirmRow.Widgets.Add(new MyraButton("Test Play", () =>
        {
            if (int.TryParse(newSoundBox.Text, out int soundId))
                Client.Game.Audio.PlaySound(Math.Clamp(soundId, 0, 65535), true);
        }) { Tooltip = "Test play this sound ID" });
        addConfirmRow.Widgets.Add(new MyraButton("Cancel", () =>
        {
            addFilterPanel.Visible = false;
            newSoundBox.Text = "";
        }));

        var addFieldRow = new HorizontalStackPanel { Spacing = 4 };
        addFieldRow.Widgets.Add(new MyraLabel("Sound ID:", MyraLabel.Style.P)
            { Tooltip = "Enter the numeric ID of the sound to filter (0-65535)" });
        addFieldRow.Widgets.Add(newSoundBox);

        addFilterPanel.Widgets.Add(new MyraLabel("Add Sound Filter:", MyraLabel.Style.H3));
        addFilterPanel.Widgets.Add(addFieldRow);
        addFilterPanel.Widgets.Add(addConfirmRow);

        var actionRow = new HorizontalStackPanel { Spacing = 4 };
        actionRow.Widgets.Add(new MyraButton("Add Filter Entry", () => addFilterPanel.Visible = !addFilterPanel.Visible));
        actionRow.Widgets.Add(new MyraButton("Import", () =>
        {
            try
            {
                string? json = Clipboard.GetClipboardText();
                if (string.IsNullOrWhiteSpace(json))
                {
                    GameActions.Print("Clipboard is empty", Constants.HUE_ERROR);
                    return;
                }

                HashSet<int>? importedFilters = JsonSerializer.Deserialize(json, HashSetIntContext.Default.HashSetInt32);
                if (importedFilters == null)
                {
                    GameActions.Print("Failed to parse clipboard data", Constants.HUE_ERROR);
                    return;
                }

                int added = 0;
                foreach (int id in importedFilters)
                {
                    if (SoundFilterManager.Instance.FilteredSounds.Add(Math.Clamp(id, 0, 65535)))
                        added++;
                }
                SoundFilterManager.Instance.Save();
                BuildFilterList();
                GameActions.Print($"Added {added} sound filter(s) from clipboard", Constants.HUE_SUCCESS);
            }
            catch (Exception ex)
            {
                GameActions.Print($"Import failed: {ex.Message}", Constants.HUE_ERROR);
            }
        }) { Tooltip = "Import filtered sounds from clipboard JSON (adds to current filters)" });
        actionRow.Widgets.Add(new MyraButton("Export", () =>
        {
            try
            {
                string json = JsonSerializer.Serialize(
                    SoundFilterManager.Instance.FilteredSounds,
                    HashSetIntContext.Default.HashSetInt32);
                json.CopyToClipboard();
                GameActions.Print(
                    $"Exported {SoundFilterManager.Instance.FilteredSounds.Count} sound filter(s) to clipboard",
                    Constants.HUE_SUCCESS);
            }
            catch (Exception ex)
            {
                GameActions.Print($"Export failed: {ex.Message}", Constants.HUE_ERROR);
            }
        }) { Tooltip = "Export all filtered sounds as JSON to clipboard" });

        BuildLastSoundSection();
        root.Widgets.Add(lastSoundPanel);
        root.Widgets.Add(actionRow);
        root.Widgets.Add(addFilterPanel);
        root.Widgets.Add(new MyraLabel("Filtered Sounds:", MyraLabel.Style.H3));
        BuildFilterList();
        root.Widgets.Add(new ScrollViewer { Height = 250, Content = filtersPanel });

        return root;
    }
}
