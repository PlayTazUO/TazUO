#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Utility;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Assistant.Filters;

public static class SoundFilterTabContent
{
    public static Widget Build()
    {
        var root = new VerticalStackPanel { Spacing = 6 };

        root.Widgets.Add(new MyraLabel(
            TazLang.Get("sound_filter_tabs_desc", "Sound Filter allows you to mute specific in-game sounds by their ID."),
            MyraLabel.TextStyle.H3));

        var lastSoundPanel = new VerticalStackPanel { Spacing = 2 };
        var filtersPanel = new VerticalStackPanel { Spacing = 2 };

        void BuildFilterList()
        {
            filtersPanel.Widgets.Clear();
            var filterList = SoundFilterManager.Instance.FilteredSounds.OrderBy(x => x).ToList();

            if (filterList.Count == 0)
            {
                filtersPanel.Widgets.Add(new MyraLabel(TazLang.Get("sound_filter_tabs_empty_filtered", "No sounds filtered."), MyraLabel.TextStyle.P));
                return;
            }

            filtersPanel.Widgets.Add(new MyraLabel(TazLang.Get("sound_filter_tabs_total_fmt", new[] { filterList.Count.ToString() }), MyraLabel.TextStyle.P));

            filtersPanel.Widgets.Add(MyraStyle.ApplyButtonDangerStyle(new MyraButton(TazLang.Get("sound_filter_tabs_btn_clear_all", "Clear All Filters"), () =>
            {
                SoundFilterManager.Instance.Clear();
                BuildFilterList();
            })));

            var grid = new MyraGrid();
            grid.SetupWithHeaders(
                GridColumnInfo.Auto(TazLang.Get("sound_filter_tabs_col_sound_id", "Sound ID")),
                GridColumnInfo.Fill(TazLang.Get("sound_filter_tabs_col_actions", "Actions"))
            );

            int dataRow = 1;
            for (int i = filterList.Count - 1; i >= 0; i--)
            {
                int soundId = filterList[i];

                // Track current ID so we can remove-old/add-new on edit without rebuilding
                int[] current = { soundId };
                var soundBox = new MyraInputBox { Text = soundId.ToString() };
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
                var actionsPanel = new HorizontalStackPanel { Spacing = 4 };
                actionsPanel.Widgets.Add(
                    new MyraButton(TazLang.Get("sound_filter_tabs_btn_play", "Play"), () => Client.Game.Audio.PlaySound(current[0], true))
                    {
                        Tooltip = TazLang.Get("sound_filter_tabs_tooltip_play", "Test play this sound (bypasses filter)"),
                    }
                );
                actionsPanel.Widgets.Add(
                    MyraStyle.ApplyButtonDangerStyle(
                        new MyraButton(
                            TazLang.Get("shared_delete", "Delete"),
                            () =>
                            {
                                SoundFilterManager.Instance.RemoveFilter(current[0]);
                                BuildFilterList();
                            }
                        )
                        {
                            Tooltip = TazLang.Get("sound_filter_tabs_tooltip_delete_filter", "Delete this filter"),
                        }
                    )
                );

                grid.AddWidget(actionsPanel, dataRow, 1);


                dataRow++;
            }

            filtersPanel.Widgets.Add(grid);
        }

        void BuildLastSoundSection()
        {
            lastSoundPanel.Widgets.Clear();
            lastSoundPanel.Widgets.Add(new MyraLabel(TazLang.Get("sound_filter_tabs_label_recently_played", "Recently played:"), MyraLabel.TextStyle.H3));

            int c = 0;
            foreach ((int, string) sound in Client.Game.Audio.LastPlayedSounds.GetItems())
            {
                c++;

                int id = sound.Item1;

                var row = new HorizontalStackPanel { Spacing = 4 };
                row.Widgets.Add(new MyraLabel(TazLang.Get("sound_filter_tabs_sound_id_fmt", new[] { id.ToString(), sound.Item2 }), MyraLabel.TextStyle.P));
                row.Widgets.Add(new MyraButton(TazLang.Get("sound_filter_tabs_btn_add_filter", "Add Filter"), () =>
                {
                    SoundFilterManager.Instance.AddFilter(id);
                    BuildFilterList();
                }) { Tooltip = TazLang.Get("sound_filter_tabs_tooltip_add_filter", "Add this sound to the filter list") });
                row.Widgets.Add(new MyraButton(TazLang.Get("sound_filter_tabs_btn_play_again", "Play Again"), () =>
                    Client.Game.Audio.PlaySound(id, true)) { Tooltip = TazLang.Get("sound_filter_tabs_tooltip_play_again", "Play this sound again") });
                lastSoundPanel.Widgets.Add(row);
            }

            lastSoundPanel.Widgets.Add(new MyraButton(TazLang.Get("shared_refresh", "Refresh"), () => BuildLastSoundSection())
            {
                Tooltip = TazLang.Get("sound_filter_tabs_tooltip_refresh", "Refresh last played sound display")
            }.PlaceBefore(new MyraLabel(
                                          TazLang.Get("sound_filter_tabs_tip", "Tip: Play a sound in-game to see its ID above, then click Add Filter."),
                                          MyraLabel.TextStyle.P)));

            if(c == 0)
            {
                var row = new HorizontalStackPanel { Spacing = 4 };
                row.Widgets.Add(new MyraLabel(TazLang.Get("sound_filter_tabs_empty_no_sound_played", "No sound played yet."), MyraLabel.TextStyle.P));
                row.Widgets.Add(new MyraButton(TazLang.Get("shared_refresh", "Refresh"), () => BuildLastSoundSection())
                    { Tooltip = TazLang.Get("sound_filter_tabs_tooltip_refresh", "Refresh last played sound display") });
                lastSoundPanel.Widgets.Add(row);
            }
        }

        var addFilterPanel = new VerticalStackPanel { Visible = false, Spacing = 4 };
        var newSoundBox = new MyraInputBox { HintText = TazLang.Get("sound_filter_tabs_hint_sound_id", "Sound ID (0-65535)"), Width = 120 };

        var addConfirmRow = new HorizontalStackPanel { Spacing = 4 };
        addConfirmRow.Widgets.Add(new MyraButton(TazLang.Get("shared_add", "Add"), () =>
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
        addConfirmRow.Widgets.Add(new MyraButton(TazLang.Get("sound_filter_tabs_btn_test_play", "Test Play"), () =>
        {
            if (int.TryParse(newSoundBox.Text, out int soundId))
                Client.Game.Audio.PlaySound(Math.Clamp(soundId, 0, 65535), true);
        }) { Tooltip = TazLang.Get("sound_filter_tabs_tooltip_test_play", "Test play this sound ID") });
        addConfirmRow.Widgets.Add(new MyraButton(TazLang.Get("shared_cancel", "Cancel"), () =>
        {
            addFilterPanel.Visible = false;
            newSoundBox.Text = "";
        }));

        var addFieldRow = new HorizontalStackPanel { Spacing = 4 };
        addFieldRow.Widgets.Add(new MyraLabel(TazLang.Get("sound_filter_tabs_label_sound_id", "Sound ID:"), MyraLabel.TextStyle.P)
            { Tooltip = TazLang.Get("sound_filter_tabs_tooltip_sound_id_field", "Enter the numeric ID of the sound to filter (0-65535)") });
        addFieldRow.Widgets.Add(newSoundBox);

        addFilterPanel.Widgets.Add(new MyraLabel(TazLang.Get("sound_filter_tabs_label_add_sound_filter", "Add Sound Filter:"), MyraLabel.TextStyle.H3));
        addFilterPanel.Widgets.Add(addFieldRow);
        addFilterPanel.Widgets.Add(addConfirmRow);

        var actionRow = new HorizontalStackPanel { Spacing = 4 };
        actionRow.Widgets.Add(new MyraButton(TazLang.Get("sound_filter_tabs_btn_add_filter_entry", "Add Filter Entry"), () => addFilterPanel.Visible = !addFilterPanel.Visible));
        actionRow.Widgets.Add(new MyraButton(TazLang.Get("shared_import", "Import"), () =>
        {
            try
            {
                string? json = Clipboard.GetClipboardText();
                if (string.IsNullOrWhiteSpace(json))
                {
                    GameActions.Print(TazLang.Get("sound_filter_tabs_msg_clipboard_empty", "Clipboard is empty"), Constants.HUE_ERROR);
                    return;
                }

                HashSet<int>? importedFilters = JsonSerializer.Deserialize(json, HashSetIntContext.Default.HashSetInt32);
                if (importedFilters == null)
                {
                    GameActions.Print(TazLang.Get("sound_filter_tabs_error_parse_failed", "Failed to parse clipboard data"), Constants.HUE_ERROR);
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
                GameActions.Print(TazLang.Get("sound_filter_tabs_msg_added_fmt", new[] { added.ToString() }), Constants.HUE_SUCCESS);
            }
            catch (Exception ex)
            {
                GameActions.Print(TazLang.Get("sound_filter_tabs_error_import_failed_fmt", new[] { ex.Message }), Constants.HUE_ERROR);
            }
        }) { Tooltip = TazLang.Get("sound_filter_tabs_tooltip_import", "Import filtered sounds from clipboard JSON (adds to current filters)") });
        actionRow.Widgets.Add(new MyraButton(TazLang.Get("shared_export", "Export"), () =>
        {
            try
            {
                string json = JsonSerializer.Serialize(
                    SoundFilterManager.Instance.FilteredSounds,
                    HashSetIntContext.Default.HashSetInt32);
                json.CopyToClipboard();
                GameActions.Print(
                    TazLang.Get("sound_filter_tabs_msg_exported_fmt", new[] { SoundFilterManager.Instance.FilteredSounds.Count.ToString() }),
                    Constants.HUE_SUCCESS);
            }
            catch (Exception ex)
            {
                GameActions.Print(TazLang.Get("sound_filter_tabs_error_export_failed_fmt", new[] { ex.Message }), Constants.HUE_ERROR);
            }
        }) { Tooltip = TazLang.Get("sound_filter_tabs_tooltip_export", "Export all filtered sounds as JSON to clipboard") });

        BuildLastSoundSection();
        root.Widgets.Add(lastSoundPanel);
        root.Widgets.Add(actionRow);
        root.Widgets.Add(addFilterPanel);
        root.Widgets.Add(new MyraLabel(TazLang.Get("sound_filter_tabs_label_filtered_sounds", "Filtered Sounds:"), MyraLabel.TextStyle.H3));
        BuildFilterList();
        root.Widgets.Add(new ScrollViewer { Height = 250, Content = filtersPanel });

        return root;
    }
}
