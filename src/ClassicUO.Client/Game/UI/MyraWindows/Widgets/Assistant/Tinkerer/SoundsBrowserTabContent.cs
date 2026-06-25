#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using Myra.Graphics2D.UI;
using SDL3;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Assistant.Tinkerer;

/// <summary>
/// Tinkerer tab for browsing and auditioning the game's sound effects. Lists every
/// sound entry (id + embedded name) with search and pagination; each row can be
/// played on demand.
/// </summary>
public static class SoundsBrowserTabContent
{
    private const int PAGE_SIZE = 100;

    public static Widget Build()
    {
        if (Client.Game?.UO?.FileManager?.Sounds == null)
            return new MyraLabel(TazLang.Get("tinkerer_sound_nodata", "Sound data not available"), MyraLabel.TextStyle.P);

        var loader = Client.Game.UO.FileManager.Sounds;

        var root = new VerticalStackPanel { Spacing = 6 };

        var allSounds = new List<KeyValuePair<int, string>>();
        var filteredResults = new List<KeyValuePair<int, string>>();
        int currentPage = 0;
        bool isLoading = false;

        string searchId = "";
        string searchName = "";

        var statusLabel = new MyraLabel(TazLang.Get("tinkerer_sound_loading", "Loading sounds..."), MyraLabel.TextStyle.P);
        var resultsPanel = new VerticalStackPanel { Spacing = 2 };
        var pageLabel = new MyraLabel("", MyraLabel.TextStyle.P);
        MyraButton prevBtn = null!;
        MyraButton nextBtn = null!;

        void BuildResultsPage()
        {
            resultsPanel.Widgets.Clear();

            if (filteredResults.Count == 0)
            {
                resultsPanel.Widgets.Add(new MyraLabel(TazLang.Get("tinkerer_sound_noresults", "No results"), MyraLabel.TextStyle.P));
                pageLabel.Text = "";
                if (prevBtn != null) prevBtn.Enabled = false;
                if (nextBtn != null) nextBtn.Enabled = false;
                return;
            }

            int totalPages = (filteredResults.Count + PAGE_SIZE - 1) / PAGE_SIZE;
            if (currentPage >= totalPages) currentPage = totalPages - 1;
            if (currentPage < 0) currentPage = 0;

            int start = currentPage * PAGE_SIZE;
            int end = Math.Min(start + PAGE_SIZE, filteredResults.Count);

            pageLabel.Text = TazLang.Get("tinkerer_sound_page", new[] { (currentPage + 1).ToString(), totalPages.ToString() });
            if (prevBtn != null) prevBtn.Enabled = currentPage > 0;
            if (nextBtn != null) nextBtn.Enabled = currentPage < totalPages - 1;

            var grid = new MyraGrid();
            grid.SetupWithHeaders(
                GridColumnInfo.Numeric(TazLang.Get("tinkerer_sound_col_id", "ID")),
                GridColumnInfo.Fill(TazLang.Get("tinkerer_sound_col_name", "Name")),
                GridColumnInfo.Auto(TazLang.Get("tinkerer_sound_col_actions", "Actions"))
            );

            for (int i = start; i < end; i++)
            {
                int gridRow = i - start + 1;
                int id = filteredResults[i].Key;
                string name = filteredResults[i].Value;

                grid.AddWidget(
                    new MyraLabel(id.ToString(), MyraLabel.TextStyle.P, MyraLabel.AlignMode.Right),
                    gridRow, 0);

                grid.AddWidget(
                    new MyraLabel(name, MyraLabel.TextStyle.P) { Tooltip = $"{id} (0x{id:X4}): {name}" },
                    gridRow, 1);

                int capturedId = id;
                var actions = new HorizontalStackPanel { Spacing = 4 };
                actions.Widgets.Add(new MyraButton(TazLang.Get("tinkerer_sound_play", "Play"),
                    () => Client.Game.Audio.PlaySound(capturedId, true))
                {
                    Tooltip = TazLang.Get("tinkerer_sound_play_tooltip", "Play this sound (bypasses filters)")
                });
                actions.Widgets.Add(new MyraButton(TazLang.Get("tinkerer_sound_copyid", "Copy ID"),
                    () => SDL.SDL_SetClipboardText(capturedId.ToString())));
                grid.AddWidget(actions, gridRow, 2);
            }

            resultsPanel.Widgets.Add(grid);
        }

        void PerformSearch()
        {
            filteredResults.Clear();
            currentPage = 0;

            bool hasIdFilter = !string.IsNullOrWhiteSpace(searchId);
            bool hasNameFilter = !string.IsNullOrWhiteSpace(searchName);

            foreach (var kvp in allSounds)
            {
                if (hasIdFilter && !kvp.Key.ToString().Contains(searchId))
                    continue;
                if (hasNameFilter && !kvp.Value.Contains(searchName, StringComparison.OrdinalIgnoreCase))
                    continue;
                filteredResults.Add(kvp);
            }

            statusLabel.Text = filteredResults.Count == 0
                ? TazLang.Get("tinkerer_sound_noresults", "No results")
                : TazLang.Get("tinkerer_sound_found", new[] { filteredResults.Count.ToString("N0") });
            BuildResultsPage();
        }

        void LoadSounds()
        {
            if (isLoading) return;
            isLoading = true;

            Task.Run(() =>
            {
                var list = new List<KeyValuePair<int, string>>();
                int count = loader.Count;

                for (int i = 0; i < count; i++)
                {
                    if (!loader.HasSound(i))
                        continue;

                    string name = loader.TryGetSoundName(i, out string n) && !string.IsNullOrWhiteSpace(n)
                        ? n
                        : TazLang.Get("tinkerer_sound_unnamed", "(unnamed)");

                    list.Add(new KeyValuePair<int, string>(i, name));
                }

                MainThreadQueue.EnqueueAction(() =>
                {
                    allSounds.Clear();
                    allSounds.AddRange(list);
                    isLoading = false;
                    PerformSearch();
                });
            });
        }

        // Search fields
        var idBox = new MyraInputBox { HintText = TazLang.Get("tinkerer_sound_idhint", "Filter by ID"), Width = 160 };
        idBox.TextChangedByUser += (_, _) => { searchId = idBox.Text ?? ""; PerformSearch(); };

        var nameBox = new MyraInputBox { HintText = TazLang.Get("tinkerer_sound_namehint", "Filter by name"), Width = 260 };
        nameBox.TextChangedByUser += (_, _) => { searchName = nameBox.Text ?? ""; PerformSearch(); };

        var searchRow = new HorizontalStackPanel { Spacing = 6, VerticalAlignment = VerticalAlignment.Center };
        searchRow.Widgets.Add(new MyraLabel(TazLang.Get("tinkerer_sound_id", "ID:"), MyraLabel.TextStyle.P));
        searchRow.Widgets.Add(idBox);
        searchRow.Widgets.Add(new MyraLabel(TazLang.Get("tinkerer_sound_name", "Name:"), MyraLabel.TextStyle.P));
        searchRow.Widgets.Add(nameBox);
        root.Widgets.Add(searchRow);

        var actionRow = new HorizontalStackPanel { Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
        actionRow.Widgets.Add(new MyraButton(TazLang.Get("tinkerer_sound_clear", "Clear"), () =>
        {
            searchId = ""; idBox.Text = "";
            searchName = ""; nameBox.Text = "";
            PerformSearch();
        }));
        actionRow.Widgets.Add(new MyraButton(TazLang.Get("tinkerer_sound_stopall", "Stop All"), () => Client.Game.Audio.StopSounds())
        {
            Tooltip = TazLang.Get("tinkerer_sound_stopall_tooltip", "Stop all currently playing sounds")
        });
        actionRow.Widgets.Add(statusLabel);
        root.Widgets.Add(actionRow);

        // Pagination controls
        prevBtn = new MyraButton(TazLang.Get("tinkerer_sound_prev", "< Prev"), () =>
        {
            if (currentPage > 0) { currentPage--; BuildResultsPage(); }
        }) { Enabled = false };
        nextBtn = new MyraButton(TazLang.Get("tinkerer_sound_next", "Next >"), () =>
        {
            int totalPages = (filteredResults.Count + PAGE_SIZE - 1) / PAGE_SIZE;
            if (currentPage < totalPages - 1) { currentPage++; BuildResultsPage(); }
        }) { Enabled = false };

        var pageRow = new HorizontalStackPanel { Spacing = 6, VerticalAlignment = VerticalAlignment.Center };
        pageRow.Widgets.Add(prevBtn);
        pageRow.Widgets.Add(pageLabel);
        pageRow.Widgets.Add(nextBtn);
        root.Widgets.Add(pageRow);

        root.Widgets.Add(new ScrollViewer { MaxHeight = 420, Content = resultsPanel });

        LoadSounds();

        return root;
    }
}
