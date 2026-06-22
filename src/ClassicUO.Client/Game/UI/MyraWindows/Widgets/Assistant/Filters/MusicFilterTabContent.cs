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

public static class MusicFilterTabContent
{
    public static Widget Build()
    {
        var root = new VerticalStackPanel { Spacing = 6 };

        root.Widgets.Add(new MyraLabel(
            TazLang.Get("music_filter_tabs_desc", "Music Filter allows you to mute specific in-game music tracks by their ID."),
            MyraLabel.TextStyle.H3));

        var lastMusicPanel = new VerticalStackPanel { Spacing = 2 };
        var filtersPanel = new VerticalStackPanel { Spacing = 2 };

        void BuildFilterList()
        {
            filtersPanel.Widgets.Clear();
            var filterList = SoundFilterManager.Instance.FilteredMusic.OrderBy(x => x).ToList();

            if (filterList.Count == 0)
            {
                filtersPanel.Widgets.Add(new MyraLabel(TazLang.Get("music_filter_tabs_empty_filtered", "No music filtered."), MyraLabel.TextStyle.P));
                return;
            }

            filtersPanel.Widgets.Add(new MyraLabel(TazLang.Get("music_filter_tabs_total_fmt", new[] { filterList.Count.ToString() }), MyraLabel.TextStyle.P));

            filtersPanel.Widgets.Add(MyraStyle.ApplyButtonDangerStyle(new MyraButton(TazLang.Get("music_filter_tabs_btn_clear_all", "Clear All Filters"), () =>
            {
                SoundFilterManager.Instance.Clear(isMusic: true);
                BuildFilterList();
            })));

            var grid = new MyraGrid();
            grid.SetupWithHeaders(
                GridColumnInfo.Auto(TazLang.Get("music_filter_tabs_col_music_id", "Music ID")),
                GridColumnInfo.Fill(TazLang.Get("music_filter_tabs_col_actions", "Actions"))
            );

            int dataRow = 1;
            for (int i = filterList.Count - 1; i >= 0; i--)
            {
                int musicId = filterList[i];

                int[] current = { musicId };
                var musicBox = new MyraInputBox { Text = musicId.ToString() };
                musicBox.TextChangedByUser += (_, _) =>
                {
                    if (int.TryParse(musicBox.Text, out int newId))
                    {
                        newId = Math.Clamp(newId, 0, 149);
                        if (newId != current[0])
                        {
                            SoundFilterManager.Instance.RemoveFilter(current[0], isMusic: true);
                            SoundFilterManager.Instance.AddFilter(newId, isMusic: true);
                            current[0] = newId;
                        }
                    }
                };
                grid.AddWidget(musicBox, dataRow, 0);

                var actionsPanel = new HorizontalStackPanel { Spacing = 4 };
                actionsPanel.Widgets.Add(
                    new MyraButton(TazLang.Get("music_filter_tabs_btn_play", "Play"), () =>
                    {
                        Client.Game.Audio.StopMusic();
                        Client.Game.Audio.PlayMusic(current[0], skipIgnore: true);
                    })
                    {
                        Tooltip = TazLang.Get("music_filter_tabs_tooltip_play", "Test play this track (bypasses filter)"),
                    }
                );
                actionsPanel.Widgets.Add(
                    MyraStyle.ApplyButtonDangerStyle(
                        new MyraButton(
                            TazLang.Get("shared_delete", "Delete"),
                            () =>
                            {
                                SoundFilterManager.Instance.RemoveFilter(current[0], isMusic: true);
                                BuildFilterList();
                            }
                        )
                        {
                            Tooltip = TazLang.Get("music_filter_tabs_tooltip_delete_filter", "Delete this filter"),
                        }
                    )
                );

                grid.AddWidget(actionsPanel, dataRow, 1);
                dataRow++;
            }

            filtersPanel.Widgets.Add(grid);
        }

        void BuildLastMusicSection()
        {
            lastMusicPanel.Widgets.Clear();
            lastMusicPanel.Widgets.Add(new MyraLabel(TazLang.Get("music_filter_tabs_label_recently_played", "Recently played:"), MyraLabel.TextStyle.H3));

            int c = 0;
            foreach ((int, string) track in Client.Game.Audio.LastPlayedMusic.GetItems())
            {
                c++;

                int id = track.Item1;

                var row = new HorizontalStackPanel { Spacing = 4 };
                row.Widgets.Add(new MyraLabel(TazLang.Get("music_filter_tabs_music_id_fmt", new[] { id.ToString(), track.Item2 }), MyraLabel.TextStyle.P));
                row.Widgets.Add(new MyraButton(TazLang.Get("music_filter_tabs_btn_add_filter", "Add Filter"), () =>
                {
                    SoundFilterManager.Instance.AddFilter(id, isMusic: true);
                    BuildFilterList();
                }) { Tooltip = TazLang.Get("music_filter_tabs_tooltip_add_filter", "Add this track to the filter list") });
                row.Widgets.Add(new MyraButton(TazLang.Get("music_filter_tabs_btn_play_again", "Play Again"), () =>
                {
                    Client.Game.Audio.StopMusic();
                    Client.Game.Audio.PlayMusic(id);
                }) { Tooltip = TazLang.Get("music_filter_tabs_tooltip_play_again", "Play this track again") });
                lastMusicPanel.Widgets.Add(row);
            }

            lastMusicPanel.Widgets.Add(new MyraButton(TazLang.Get("shared_refresh", "Refresh"), () => BuildLastMusicSection())
            {
                Tooltip = TazLang.Get("music_filter_tabs_tooltip_refresh", "Refresh last played music display")
            }.PlaceBefore(new MyraLabel(
                              TazLang.Get("music_filter_tabs_tip", "Tip: Let music play in-game to see its ID above, then click Add Filter."),
                              MyraLabel.TextStyle.P)));

            if (c == 0)
            {
                var row = new HorizontalStackPanel { Spacing = 4 };
                row.Widgets.Add(new MyraLabel(TazLang.Get("music_filter_tabs_empty_no_music_played", "No music played yet."), MyraLabel.TextStyle.P));
                row.Widgets.Add(new MyraButton(TazLang.Get("shared_refresh", "Refresh"), () => BuildLastMusicSection())
                    { Tooltip = TazLang.Get("music_filter_tabs_tooltip_refresh", "Refresh last played music display") });
                lastMusicPanel.Widgets.Add(row);
            }
        }

        var addFilterPanel = new VerticalStackPanel { Visible = false, Spacing = 4 };
        var newMusicBox = new MyraInputBox { HintText = TazLang.Get("music_filter_tabs_hint_music_id", "Music ID (0-149)"), Width = 120 };

        var addConfirmRow = new HorizontalStackPanel { Spacing = 4 };
        addConfirmRow.Widgets.Add(new MyraButton(TazLang.Get("shared_add", "Add"), () =>
        {
            if (int.TryParse(newMusicBox.Text, out int musicId))
            {
                musicId = Math.Clamp(musicId, 0, 149);
                SoundFilterManager.Instance.AddFilter(musicId, isMusic: true);
                newMusicBox.Text = "";
                addFilterPanel.Visible = false;
                BuildFilterList();
            }
        }));
        addConfirmRow.Widgets.Add(new MyraButton(TazLang.Get("music_filter_tabs_btn_test_play", "Test Play"), () =>
        {
            if (int.TryParse(newMusicBox.Text, out int musicId))
                Client.Game.Audio.PlayMusic(Math.Clamp(musicId, 0, 149), false, true);
        }) { Tooltip = TazLang.Get("music_filter_tabs_tooltip_test_play", "Test play this music ID") });
        addConfirmRow.Widgets.Add(new MyraButton(TazLang.Get("shared_cancel", "Cancel"), () =>
        {
            addFilterPanel.Visible = false;
            newMusicBox.Text = "";
        }));

        var addFieldRow = new HorizontalStackPanel { Spacing = 4 };
        addFieldRow.Widgets.Add(new MyraLabel(TazLang.Get("music_filter_tabs_label_music_id", "Music ID:"), MyraLabel.TextStyle.P)
            { Tooltip = TazLang.Get("music_filter_tabs_tooltip_music_id_field", "Enter the numeric ID of the music track to filter (0-149)") });
        addFieldRow.Widgets.Add(newMusicBox);

        addFilterPanel.Widgets.Add(new MyraLabel(TazLang.Get("music_filter_tabs_label_add_music_filter", "Add Music Filter:"), MyraLabel.TextStyle.H3));
        addFilterPanel.Widgets.Add(addFieldRow);
        addFilterPanel.Widgets.Add(addConfirmRow);

        var actionRow = new HorizontalStackPanel { Spacing = 4 };
        actionRow.Widgets.Add(new MyraButton(TazLang.Get("music_filter_tabs_btn_add_filter_entry", "Add Filter Entry"), () => addFilterPanel.Visible = !addFilterPanel.Visible));
        actionRow.Widgets.Add(new MyraButton(TazLang.Get("shared_import", "Import"), () =>
        {
            try
            {
                string? json = Clipboard.GetClipboardText();
                if (string.IsNullOrWhiteSpace(json))
                {
                    GameActions.Print(TazLang.Get("music_filter_tabs_msg_clipboard_empty", "Clipboard is empty"), Constants.HUE_ERROR);
                    return;
                }

                HashSet<int>? importedFilters = JsonSerializer.Deserialize(json, HashSetIntContext.Default.HashSetInt32);
                if (importedFilters == null)
                {
                    GameActions.Print(TazLang.Get("music_filter_tabs_error_parse_failed", "Failed to parse clipboard data"), Constants.HUE_ERROR);
                    return;
                }

                int added = 0;
                foreach (int id in importedFilters)
                {
                    if (SoundFilterManager.Instance.FilteredMusic.Add(Math.Clamp(id, 0, 149)))
                        added++;
                }
                SoundFilterManager.Instance.Save(isMusic: true);
                BuildFilterList();
                GameActions.Print(TazLang.Get("music_filter_tabs_msg_added_fmt", new[] { added.ToString() }), Constants.HUE_SUCCESS);
            }
            catch (Exception ex)
            {
                GameActions.Print(TazLang.Get("music_filter_tabs_error_import_failed_fmt", new[] { ex.Message }), Constants.HUE_ERROR);
            }
        }) { Tooltip = TazLang.Get("music_filter_tabs_tooltip_import", "Import filtered music tracks from clipboard JSON (adds to current filters)") });
        actionRow.Widgets.Add(new MyraButton(TazLang.Get("shared_export", "Export"), () =>
        {
            try
            {
                string json = JsonSerializer.Serialize(
                    SoundFilterManager.Instance.FilteredMusic,
                    HashSetIntContext.Default.HashSetInt32);
                json.CopyToClipboard();
                GameActions.Print(
                    TazLang.Get("music_filter_tabs_msg_exported_fmt", new[] { SoundFilterManager.Instance.FilteredMusic.Count.ToString() }),
                    Constants.HUE_SUCCESS);
            }
            catch (Exception ex)
            {
                GameActions.Print(TazLang.Get("music_filter_tabs_error_export_failed_fmt", new[] { ex.Message }), Constants.HUE_ERROR);
            }
        }) { Tooltip = TazLang.Get("music_filter_tabs_tooltip_export", "Export all filtered music tracks as JSON to clipboard") });

        BuildLastMusicSection();
        root.Widgets.Add(lastMusicPanel);
        root.Widgets.Add(actionRow);
        root.Widgets.Add(addFilterPanel);
        root.Widgets.Add(new MyraLabel(TazLang.Get("music_filter_tabs_label_filtered_music", "Filtered Music:"), MyraLabel.TextStyle.H3));
        BuildFilterList();
        root.Widgets.Add(new ScrollViewer { Height = 250, Content = filtersPanel });

        return root;
    }
}
