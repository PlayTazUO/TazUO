#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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
            "Music Filter allows you to mute specific in-game music tracks by their ID.",
            MyraLabel.TextStyle.H3));

        var lastMusicPanel = new VerticalStackPanel { Spacing = 2 };
        var filtersPanel = new VerticalStackPanel { Spacing = 2 };

        void BuildFilterList()
        {
            filtersPanel.Widgets.Clear();
            var filterList = SoundFilterManager.Instance.FilteredMusic.OrderBy(x => x).ToList();

            if (filterList.Count == 0)
            {
                filtersPanel.Widgets.Add(new MyraLabel("No music filtered.", MyraLabel.TextStyle.P));
                return;
            }

            filtersPanel.Widgets.Add(new MyraLabel($"Total: {filterList.Count} track(s) filtered", MyraLabel.TextStyle.P));

            filtersPanel.Widgets.Add(MyraStyle.ApplyButtonDangerStyle(new MyraButton("Clear All Filters", () =>
            {
                SoundFilterManager.Instance.Clear(isMusic: true);
                BuildFilterList();
            })));

            var grid = new MyraGrid();
            grid.SetupWithHeaders(
                GridColumnInfo.Auto("Music ID"),
                GridColumnInfo.Fill("Actions")
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
                    new MyraButton("Play", () =>
                    {
                        Client.Game.Audio.StopMusic();
                        Client.Game.Audio.PlayMusic(current[0], skipIgnore: true);
                    })
                    {
                        Tooltip = "Test play this track (bypasses filter)",
                    }
                );
                actionsPanel.Widgets.Add(
                    MyraStyle.ApplyButtonDangerStyle(
                        new MyraButton(
                            "Delete",
                            () =>
                            {
                                SoundFilterManager.Instance.RemoveFilter(current[0], isMusic: true);
                                BuildFilterList();
                            }
                        )
                        {
                            Tooltip = "Delete this filter",
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
            lastMusicPanel.Widgets.Add(new MyraLabel("Recently played:", MyraLabel.TextStyle.H3));

            int c = 0;
            foreach ((int, string) track in Client.Game.Audio.LastPlayedMusic.GetItems())
            {
                c++;

                int id = track.Item1;

                var row = new HorizontalStackPanel { Spacing = 4 };
                row.Widgets.Add(new MyraLabel($"Music ID: {id} ({track.Item2})", MyraLabel.TextStyle.P));
                row.Widgets.Add(new MyraButton("Add Filter", () =>
                {
                    SoundFilterManager.Instance.AddFilter(id, isMusic: true);
                    BuildFilterList();
                }) { Tooltip = "Add this track to the filter list" });
                row.Widgets.Add(new MyraButton("Play Again", () =>
                {
                    Client.Game.Audio.StopMusic();
                    Client.Game.Audio.PlayMusic(id);
                }) { Tooltip = "Play this track again" });
                lastMusicPanel.Widgets.Add(row);
            }

            lastMusicPanel.Widgets.Add(new MyraButton("Refresh", () => BuildLastMusicSection())
            {
                Tooltip = "Refresh last played music display"
            }.PlaceBefore(new MyraLabel(
                              "Tip: Let music play in-game to see its ID above, then click Add Filter.",
                              MyraLabel.TextStyle.P)));

            if (c == 0)
            {
                var row = new HorizontalStackPanel { Spacing = 4 };
                row.Widgets.Add(new MyraLabel("No music played yet.", MyraLabel.TextStyle.P));
                row.Widgets.Add(new MyraButton("Refresh", () => BuildLastMusicSection())
                    { Tooltip = "Refresh last played music display" });
                lastMusicPanel.Widgets.Add(row);
            }
        }

        var addFilterPanel = new VerticalStackPanel { Visible = false, Spacing = 4 };
        var newMusicBox = new MyraInputBox { HintText = "Music ID (0-149)", Width = 120 };

        var addConfirmRow = new HorizontalStackPanel { Spacing = 4 };
        addConfirmRow.Widgets.Add(new MyraButton("Add", () =>
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
        addConfirmRow.Widgets.Add(new MyraButton("Test Play", () =>
        {
            if (int.TryParse(newMusicBox.Text, out int musicId))
                Client.Game.Audio.PlayMusic(Math.Clamp(musicId, 0, 149), false, true);
        }) { Tooltip = "Test play this music ID" });
        addConfirmRow.Widgets.Add(new MyraButton("Cancel", () =>
        {
            addFilterPanel.Visible = false;
            newMusicBox.Text = "";
        }));

        var addFieldRow = new HorizontalStackPanel { Spacing = 4 };
        addFieldRow.Widgets.Add(new MyraLabel("Music ID:", MyraLabel.TextStyle.P)
            { Tooltip = "Enter the numeric ID of the music track to filter (0-149)" });
        addFieldRow.Widgets.Add(newMusicBox);

        addFilterPanel.Widgets.Add(new MyraLabel("Add Music Filter:", MyraLabel.TextStyle.H3));
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
                    if (SoundFilterManager.Instance.FilteredMusic.Add(Math.Clamp(id, 0, 149)))
                        added++;
                }
                SoundFilterManager.Instance.Save(isMusic: true);
                BuildFilterList();
                GameActions.Print($"Added {added} music filter(s) from clipboard", Constants.HUE_SUCCESS);
            }
            catch (Exception ex)
            {
                GameActions.Print($"Import failed: {ex.Message}", Constants.HUE_ERROR);
            }
        }) { Tooltip = "Import filtered music tracks from clipboard JSON (adds to current filters)" });
        actionRow.Widgets.Add(new MyraButton("Export", () =>
        {
            try
            {
                string json = JsonSerializer.Serialize(
                    SoundFilterManager.Instance.FilteredMusic,
                    HashSetIntContext.Default.HashSetInt32);
                json.CopyToClipboard();
                GameActions.Print(
                    $"Exported {SoundFilterManager.Instance.FilteredMusic.Count} music filter(s) to clipboard",
                    Constants.HUE_SUCCESS);
            }
            catch (Exception ex)
            {
                GameActions.Print($"Export failed: {ex.Message}", Constants.HUE_ERROR);
            }
        }) { Tooltip = "Export all filtered music tracks as JSON to clipboard" });

        BuildLastMusicSection();
        root.Widgets.Add(lastMusicPanel);
        root.Widgets.Add(actionRow);
        root.Widgets.Add(addFilterPanel);
        root.Widgets.Add(new MyraLabel("Filtered Music:", MyraLabel.TextStyle.H3));
        BuildFilterList();
        root.Widgets.Add(new ScrollViewer { Height = 250, Content = filtersPanel });

        return root;
    }
}
