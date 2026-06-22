#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Assistant.Tinkerer;

public static class CililocsTabContent
{
    private const int PAGE_SIZE = 100;

    public static Widget Build()
    {
        var root = new VerticalStackPanel { Spacing = 6 };

        string uoDir = Settings.GlobalSettings.UltimaOnlineDirectory;
        var loadedEntries = new Dictionary<int, string>();
        var filteredResults = new List<KeyValuePair<int, string>>();
        int currentPage = 0;
        bool isLoading = false;

        string searchId = "";
        string searchText = "";

        var statusLabel = new MyraLabel(TazLang.Get("tinkerer_cliloc_msg_select_language", "Select a language to load clilocs"), MyraLabel.TextStyle.P);
        var countLabel = new MyraLabel("", MyraLabel.TextStyle.P);
        var resultsPanel = new VerticalStackPanel { Spacing = 2 };
        var pageLabel = new MyraLabel("", MyraLabel.TextStyle.P);
        MyraButton prevBtn = null!;
        MyraButton nextBtn = null!;

        List<string> langCodes = DetectLanguages(uoDir);

        void BuildResultsPage()
        {
            resultsPanel.Widgets.Clear();

            if (filteredResults.Count == 0)
            {
                resultsPanel.Widgets.Add(new MyraLabel(TazLang.Get("tinkerer_cliloc_empty_no_results", "No results"), MyraLabel.TextStyle.P));
                pageLabel.Text = "";
                if (prevBtn != null) prevBtn.Enabled = false;
                if (nextBtn != null) nextBtn.Enabled = false;
                return;
            }

            int totalPages = (filteredResults.Count + PAGE_SIZE - 1) / PAGE_SIZE;
            int start = currentPage * PAGE_SIZE;
            int end = Math.Min(start + PAGE_SIZE, filteredResults.Count);

            pageLabel.Text = TazLang.Get("tinkerer_cliloc_msg_page_fmt", new[] { (currentPage + 1).ToString(), totalPages.ToString() });
            if (prevBtn != null) prevBtn.Enabled = currentPage > 0;
            if (nextBtn != null) nextBtn.Enabled = currentPage < totalPages - 1;

            var grid = new MyraGrid();
            grid.SetupWithHeaders(
                GridColumnInfo.Numeric(TazLang.Get("tinkerer_cliloc_col_id", "ID")),
                GridColumnInfo.Fill(TazLang.Get("tinkerer_cliloc_col_text", "Text"))
            );

            for (int i = start; i < end; i++)
            {
                int gridRow = i - start + 1;
                int id = filteredResults[i].Key;
                string text = filteredResults[i].Value;

                grid.AddWidget(
                    new MyraLabel(id.ToString(), MyraLabel.TextStyle.P, MyraLabel.AlignMode.Right),
                    gridRow, 0);

                var textLabel = new MyraLabel(text, MyraLabel.TextStyle.P)
                    { MaxWidth = 600, Tooltip = TazLang.Get("tinkerer_cliloc_tooltip_fmt", new[] { id.ToString(), text }) };
                grid.AddWidget(textLabel, gridRow, 1);
            }

            resultsPanel.Widgets.Add(grid);
        }

        void PerformSearch()
        {
            if (loadedEntries.Count == 0)
            {
                statusLabel.Text = TazLang.Get("tinkerer_cliloc_msg_no_clilocs_loaded", "No clilocs loaded. Select a language first.");
                return;
            }

            filteredResults.Clear();
            currentPage = 0;

            bool hasIdFilter = !string.IsNullOrWhiteSpace(searchId);
            bool hasTextFilter = !string.IsNullOrWhiteSpace(searchText);
            string textLower = searchText.ToLowerInvariant();

            foreach (var kvp in loadedEntries)
            {
                if (hasIdFilter && !kvp.Key.ToString().Contains(searchId))
                    continue;
                if (hasTextFilter && !kvp.Value.Contains(textLower, StringComparison.OrdinalIgnoreCase))
                    continue;
                filteredResults.Add(kvp);
            }

            filteredResults.Sort((a, b) => a.Key.CompareTo(b.Key));
            statusLabel.Text = filteredResults.Count == 0
                ? TazLang.Get("tinkerer_cliloc_msg_no_results_found", "No results found")
                : TazLang.Get("tinkerer_cliloc_msg_found_fmt", new[] { filteredResults.Count.ToString("N0") });
            BuildResultsPage();
        }

        void LoadLanguage(string langCode)
        {
            if (isLoading) return;
            isLoading = true;
            statusLabel.Text = TazLang.Get("tinkerer_cliloc_msg_loading_fmt", new[] { langCode });
            countLabel.Text = "";
            loadedEntries.Clear();
            filteredResults.Clear();
            resultsPanel.Widgets.Clear();

            string path = Path.Combine(uoDir, $"Cliloc.{langCode}");

            Task.Run(() =>
            {
                try
                {
                    Dictionary<int, string> entries = ClilocLoader.LoadFromFile(path);
                    MainThreadQueue.EnqueueAction(() =>
                    {
                        loadedEntries = entries;
                        countLabel.Text = TazLang.Get("tinkerer_cliloc_msg_entries_fmt", new[] { entries.Count.ToString("N0") });
                        statusLabel.Text = TazLang.Get("tinkerer_cliloc_msg_ready", "Ready — enter search terms or leave blank to show all");
                        isLoading = false;
                        PerformSearch();
                    });
                }
                catch (Exception ex)
                {
                    MainThreadQueue.EnqueueAction(() =>
                    {
                        statusLabel.Text = TazLang.Get("tinkerer_cliloc_error_loading_fmt", new[] { ex.Message });
                        isLoading = false;
                    });
                }
            });
        }

        // Language selector
        var langRow = new HorizontalStackPanel { Spacing = 6, VerticalAlignment = VerticalAlignment.Center };
        langRow.Widgets.Add(new MyraLabel(TazLang.Get("tinkerer_cliloc_label_language", "Language:"), MyraLabel.TextStyle.P));

        if (langCodes.Count == 0)
        {
            langRow.Widgets.Add(new MyraLabel(TazLang.Get("tinkerer_cliloc_msg_no_cliloc_files", "No Cliloc files found in UO directory"), MyraLabel.TextStyle.P));
        }
        else
        {
#pragma warning disable CS0618
            var combo = new ComboBox { VerticalAlignment = VerticalAlignment.Center };
            foreach (string code in langCodes)
                combo.Items.Add(new ListItem(code.ToUpperInvariant()) { Tag = code });

            int defaultIdx = langCodes.FindIndex(c => c.Equals("enu", StringComparison.OrdinalIgnoreCase));
            combo.SelectedIndex = defaultIdx >= 0 ? defaultIdx : 0;

            combo.SelectedIndexChanged += (_, _) =>
            {
                if (combo.SelectedItem?.Tag is string code)
                    LoadLanguage(code);
            };
#pragma warning restore CS0618

            langRow.Widgets.Add(combo);
        }
        langRow.Widgets.Add(countLabel);
        root.Widgets.Add(langRow);

        // Search fields
        root.Widgets.Add(new MyraLabel(TazLang.Get("tinkerer_cliloc_label_search", "Search:"), MyraLabel.TextStyle.H3));

        var idBox = new MyraInputBox { HintText = TazLang.Get("tinkerer_cliloc_hint_filter_id", "Filter by ID (partial match)"), Width = 200 };
        idBox.TextChangedByUser += (_, _) => searchId = idBox.Text ?? "";

        var textBox = new MyraInputBox { HintText = TazLang.Get("tinkerer_cliloc_hint_filter_text", "Filter by text (partial match)"), Width = 320 };
        textBox.TextChangedByUser += (_, _) => searchText = textBox.Text ?? "";

        var searchRow = new HorizontalStackPanel { Spacing = 6 };
        searchRow.Widgets.Add(new MyraLabel(TazLang.Get("tinkerer_cliloc_label_id", "ID:"), MyraLabel.TextStyle.P));
        searchRow.Widgets.Add(idBox);
        searchRow.Widgets.Add(new MyraLabel(TazLang.Get("tinkerer_cliloc_label_text", "Text:"), MyraLabel.TextStyle.P));
        searchRow.Widgets.Add(textBox);
        root.Widgets.Add(searchRow);

        var actionRow = new HorizontalStackPanel { Spacing = 4 };
        actionRow.Widgets.Add(new MyraButton(TazLang.Get("tinkerer_cliloc_btn_search", "Search"), () => PerformSearch()));
        actionRow.Widgets.Add(new MyraButton(TazLang.Get("tinkerer_cliloc_btn_clear", "Clear"), () =>
        {
            searchId = ""; idBox.Text = "";
            searchText = ""; textBox.Text = "";
            PerformSearch();
        }));
        root.Widgets.Add(actionRow);
        root.Widgets.Add(statusLabel);

        // Pagination controls
        prevBtn = new MyraButton(TazLang.Get("tinkerer_cliloc_btn_prev", "< Prev"), () =>
        {
            if (currentPage > 0) { currentPage--; BuildResultsPage(); }
        }) { Enabled = false };
        nextBtn = new MyraButton(TazLang.Get("tinkerer_cliloc_btn_next", "Next >"), () =>
        {
            int totalPages = (filteredResults.Count + PAGE_SIZE - 1) / PAGE_SIZE;
            if (currentPage < totalPages - 1) { currentPage++; BuildResultsPage(); }
        }) { Enabled = false };

        var pageRow = new HorizontalStackPanel { Spacing = 6 };
        pageRow.Widgets.Add(prevBtn);
        pageRow.Widgets.Add(pageLabel);
        pageRow.Widgets.Add(nextBtn);
        root.Widgets.Add(pageRow);
        root.Widgets.Add(new ScrollViewer { MaxHeight = 400, Content = resultsPanel });

        // Auto-load default language when tab opens
        if (langCodes.Count > 0)
        {
            int defaultIdx = langCodes.FindIndex(c => c.Equals("enu", StringComparison.OrdinalIgnoreCase));
            LoadLanguage(langCodes[defaultIdx >= 0 ? defaultIdx : 0]);
        }

        return root;
    }

    private static List<string> DetectLanguages(string uoDir)
    {
        var langs = new List<string>();
        if (!Directory.Exists(uoDir)) return langs;

        foreach (string file in Directory.GetFiles(uoDir, "Cliloc.*", SearchOption.TopDirectoryOnly))
        {
            string ext = Path.GetExtension(file);
            if (ext.Length > 1)
                langs.Add(ext.Substring(1));
        }

        langs.Sort(StringComparer.OrdinalIgnoreCase);
        return langs;
    }
}
