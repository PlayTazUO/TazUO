#nullable enable

using System;
using System.Collections.Generic;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.UI.MyraWindows.Widgets.Search;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;
using Myra.Utility.Search;
using SDL3;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Assistant.Tinkerer;

/// <summary>
///     Tinkerer tab for browsing item (static) art. A paged square grid of art
///     graphics on the left; clicking a cell shows an enlarged preview with zoom
///     controls and TileData metadata on the right.
/// </summary>
public static class ArtBrowserTabContent
{
    /// <summary>
    /// Builds the tab's content widget
    /// </summary>
    /// <returns>A ready-to-render widget</returns>
    public static Widget Build()
    {
        if (Client.Game?.UO?.Arts == null)
            return new MyraLabel(TazLang.Get("tinkerer_art_nodata", "Art data not available"), MyraLabel.TextStyle.P);

        return new Builder().BuildRoot();
    }

    /// <summary>
    ///     Holds the per-open browser state (page, selection, zoom) and builds the widget tree around
    ///     it. Instantiated fresh each time the tab is built, so state does not leak between opens.
    /// </summary>
    private sealed class Builder
    {
        private const int COLS = 8;
        private const int ROWS = 8;
        private const int PAGE_SIZE = COLS * ROWS;
        private const int CELL = 44;

        private const int ZOOM_MIN = 32;
        private const int ZOOM_MAX = 512;
        private const int ZOOM_STEP = 32;
        private const int ZOOM_DEFAULT = 128;

        private static readonly SolidBrush _selectedBorder = new(Color.Gold);

        private readonly VerticalStackPanel _gridPanel = new() { Spacing = 1 };
        private readonly VerticalStackPanel _detailPanel = new() { Spacing = 4, Width = 280 };

        private readonly int _maxGraphic;

        /// <summary>
        ///     Every graphic that carries a TileData name, as (id, "id - name"). Backs both the
        ///     go-to picker's name list and the filter search below it.
        /// </summary>
        private readonly List<(int Id, string Label)> _namedEntries = [];

        private readonly ContainsThenLevenshteinSearchStrategy _filterStrategy = new();

        private int _currentPage;
        private int _selectedGraphic = -1;
        private int _zoomSize = ZOOM_DEFAULT;
        private string _filterQuery = string.Empty;

        /// <summary>Cache of <see cref="FilteredIds" />'s last result. Invalidated where
        /// <see cref="_filterQuery" /> changes, so a plain page/selection refresh does not rescan every
        /// named entry.</summary>
        private List<int>? _cachedFilteredIds;

        private MyraButton _prevBtn = null!;
        private MyraButton _nextBtn = null!;
        private MyraLabel _pageLabel = null!;
        private MyraInputBox _filterBox = null!;
        private IndexedComboPicker _gotoPicker = null!;

        /// <summary>
        ///     Set while <see cref="SelectGraphic" /> is echoing the selection into the go-to picker, so that
        ///     echo does not re-enter <see cref="JumpTo" /> and clear the active filter out from under a grid click.
        /// </summary>
        private bool _suppressGotoJump;

        public Builder()
        {
            // Upper bound on browsable item graphics. Static art is indexed by item
            // graphic id; cap to the available TileData / 0x10000 range.
            int maxGraphic = 0x10000;
            StaticTiles[]? staticData = GetStaticData();
            if (staticData != null && staticData.Length < maxGraphic)
                maxGraphic = staticData.Length;

            _maxGraphic = maxGraphic;

            if (staticData == null)
                return;

            int count = Math.Min(_maxGraphic, staticData.Length);
            for (int i = 0; i < count; i++)
            {
                string name = staticData[i].Name;
                if (string.IsNullOrEmpty(name))
                    continue;

                _namedEntries.Add((i, $"{i} - {name}"));
            }
        }

        public HorizontalStackPanel BuildRoot()
        {
            var leftColumn = new VerticalStackPanel { Spacing = 4 };
            leftColumn.Widgets.Add(BuildJumpRow());
            leftColumn.Widgets.Add(BuildFilterRow());
            leftColumn.Widgets.Add(BuildPaginationRow());
            leftColumn.Widgets.Add(new ScrollViewer { MaxHeight = 450, Content = _gridPanel });

            var root = new HorizontalStackPanel { Spacing = 12 };
            root.Widgets.Add(leftColumn);
            root.Widgets.Add(_detailPanel);

            RefreshPage();
            RefreshDetail();
            return root;
        }

        /// <summary>
        ///     Jumps the grid/detail view to a graphic, reachable either by typing its raw ID (decimal or
        ///     0x-hex) or by finding it in a searchable list of TileData names. Only graphics with a name
        ///     are listed, since an empty name would just be noise in the dropdown - the number field is
        ///     what carries an unnamed one.
        /// </summary>
        private HorizontalStackPanel BuildJumpRow()
        {
            var jumpRow = new HorizontalStackPanel { Spacing = 4, VerticalAlignment = VerticalAlignment.Center };

            _gotoPicker = new IndexedComboPicker(
                Math.Max(_selectedGraphic, 0),
                _namedEntries,
                0,
                Math.Max(0, _maxGraphic - 1),
                new HexInputBox()
            )
            {
                NumberInput =
                {
                    Width = 120,
                    HintText = TazLang.Get("tinkerer_art_jumphint", "Graphic # or 0x.."),
                    Tooltip = TazLang.Get("tinkerer_art_jumphint", "Graphic # or 0x..")
                },
                NameList = { Width = 220, SearchHintText = TazLang.Get("tinkerer_art_searchhint", "Search for art.."), TooltipSelector = name => name }
            };
            MyraStyle.ApplySearchComboBoxPopupBorder(_gotoPicker.NameList);
            _gotoPicker.ValueChanged += (_, id) =>
            {
                if (!_suppressGotoJump)
                    JumpTo(id);
            };

            jumpRow.Widgets.Add(new MyraLabel(TazLang.Get("tinkerer_art_goto", "Go to:"), MyraLabel.TextStyle.P));
            jumpRow.Widgets.Add(_gotoPicker);
            return jumpRow;
        }

        /// <summary>
        ///     Free-text filter over the same TileData names the go-to list searches, narrowing the grid
        ///     to matches so the page-through/pick flow doubles as a browse-by-name flow. Unlike the go-to
        ///     picker this never jumps or selects - it only changes what the grid shows.
        /// </summary>
        private HorizontalStackPanel BuildFilterRow()
        {
            _filterBox = new MyraInputBox { HintText = TazLang.Get("tinkerer_art_filterhint", "Filter by name.."), Width = 220 };
            _filterBox.TextChangedByUser += (_, _) =>
            {
                _filterQuery = _filterBox.Text?.Trim() ?? string.Empty;
                _cachedFilteredIds = null;
                _currentPage = 0;
                RefreshPage();
            };

            var filterRow = new HorizontalStackPanel { Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
            filterRow.Widgets.Add(new MyraLabel(TazLang.Get("tinkerer_art_filter", "Filter:"), MyraLabel.TextStyle.P));
            filterRow.Widgets.Add(_filterBox);
            return filterRow;
        }

        /// <summary>
        ///     <see cref="FilteredIds" /> for the current <see cref="_filterQuery" />, reusing the last
        ///     result until the filter box invalidates it - a plain grid click re-runs RefreshPage()
        ///     without touching the filter, and the scan is a Levenshtein pass over every named entry.
        /// </summary>
        private List<int> CachedFilteredIds() => _cachedFilteredIds ??= FilteredIds();

        /// <summary>Named graphics matching <see cref="_filterQuery" />, best match first.</summary>
        private List<int> FilteredIds()
        {
            var matches = new List<(int Id, double Score)>();
            foreach ((int id, string label) in _namedEntries)
            {
                SearchMatch match = _filterStrategy.Match(label, _filterQuery);
                if (match.IsMatch)
                    matches.Add((id, match.Score));
            }

            matches.Sort((a, b) => b.Score.CompareTo(a.Score));

            var ids = new List<int>(matches.Count);
            foreach ((int id, _) in matches)
                ids.Add(id);
            return ids;
        }

        private HorizontalStackPanel BuildPaginationRow()
        {
            _prevBtn = new MyraButton(TazLang.Get("tinkerer_art_prev", "< Prev"), () =>
            {
                _currentPage--;
                RefreshPage();
            }) { Enabled = false };
            _nextBtn = new MyraButton(TazLang.Get("tinkerer_art_next", "Next >"), () =>
            {
                _currentPage++;
                RefreshPage();
            }) { Enabled = false };
            _pageLabel = new MyraLabel("", MyraLabel.TextStyle.P);

            var pageRow = new HorizontalStackPanel { Spacing = 6, VerticalAlignment = VerticalAlignment.Center };
            pageRow.Widgets.Add(_prevBtn);
            pageRow.Widgets.Add(_pageLabel);
            pageRow.Widgets.Add(_nextBtn);
            return pageRow;
        }

        /// <summary>
        ///     Rebuilds the grid for the current page and updates the pagination controls, over
        ///     the filtered id list when a filter is active, over the raw id range otherwise.
        /// </summary>
        private void RefreshPage()
        {
            List<int>? filtered = _filterQuery.Length > 0 ? CachedFilteredIds() : null;
            int totalItems = filtered?.Count ?? _maxGraphic;
            int totalPages = Math.Max(1, (totalItems + PAGE_SIZE - 1) / PAGE_SIZE);

            if (_currentPage < 0) _currentPage = 0;
            if (_currentPage >= totalPages) _currentPage = totalPages - 1;

            _gridPanel.Widgets.Clear();

            int start = _currentPage * PAGE_SIZE;

            for (int r = 0; r < ROWS; r++)
            {
                var rowPanel = new HorizontalStackPanel { Spacing = 1 };
                for (int c = 0; c < COLS; c++)
                {
                    int index = start + r * COLS + c;
                    if (index >= totalItems)
                    {
                        rowPanel.Widgets.Add(new Panel { Width = CELL, Height = CELL });
                        continue;
                    }

                    rowPanel.Widgets.Add(BuildCell(filtered?[index] ?? index));
                }

                _gridPanel.Widgets.Add(rowPanel);
            }

            _pageLabel.Text = TazLang.Get("tinkerer_art_page", [(_currentPage + 1).ToString(), totalPages.ToString()]);
            _prevBtn.Enabled = _currentPage > 0;
            _nextBtn.Enabled = _currentPage < totalPages - 1;
        }

        private Panel BuildCell(int id)
        {
            var cell = new Panel { Width = CELL, Height = CELL, BorderThickness = new Thickness(1), Tooltip = BuildCellTooltip(id) };

            if (id == _selectedGraphic)
                cell.Border = _selectedBorder;

            var art = new MyraArtTexture((uint)id, maxSize: CELL - 4)
            {
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
            };
            cell.Widgets.Add(art);

            cell.TouchDown += (_, _) => SelectGraphic(id);
            return cell;
        }

        private static string BuildCellTooltip(int id)
        {
            StaticTiles[]? sd = GetStaticData();
            if (sd != null && id < sd.Length && !string.IsNullOrEmpty(sd[id].Name))
                return TazLang.Get("tinkerer_art_tooltip_named", [id.ToString(), $"0x{id:X4}", sd[id].Name]);
            return TazLang.Get("tinkerer_art_tooltip", [id.ToString(), $"0x{id:X4}"]);
        }

        /// <summary>
        ///     Jumps straight to a graphic, driven by the go-to picker. Clears any active name filter
        ///     first - the filter narrows the browse grid, but a jump targets a specific graphic that
        ///     might not be among the current matches.
        /// </summary>
        private void JumpTo(int id)
        {
            if (id < 0 || id >= _maxGraphic) return;

            _filterQuery = string.Empty;
            _filterBox.Text = string.Empty;

            _selectedGraphic = id;
            _currentPage = id / PAGE_SIZE;
            RefreshPage();
            RefreshDetail();
        }

        private void SelectGraphic(int id)
        {
            _selectedGraphic = id;

            _suppressGotoJump = true;
            try
            {
                _gotoPicker.Value = id;
            }
            finally
            {
                _suppressGotoJump = false;
            }

            RefreshPage();
            RefreshDetail();
        }

        /// <summary>Rebuilds the right-hand detail panel (preview, zoom controls, TileData info) for the current selection.</summary>
        private void RefreshDetail()
        {
            _detailPanel.Widgets.Clear();

            if (_selectedGraphic < 0)
            {
                _detailPanel.Widgets.Add(new MyraLabel(
                    TazLang.Get("tinkerer_art_selectprompt", "Select an art graphic to view details."),
                    MyraLabel.TextStyle.P));
                return;
            }

            uint id = (uint)_selectedGraphic;

            _detailPanel.Widgets.Add(BuildPreview(id));
            _detailPanel.Widgets.Add(BuildZoomRow());
            AddInfoRows(id);
        }

        /// <summary>Art preview scaled to <see cref="_zoomSize" /> while preserving aspect ratio.</summary>
        private Widget BuildPreview(uint id)
        {
            ref readonly SpriteInfo art = ref Client.Game!.UO!.Arts.GetArt(id);
            if (art.Texture == null)
                return new MyraLabel(TazLang.Get("tinkerer_art_noart", "(No art at this graphic)"), MyraLabel.TextStyle.P);

            var preview = new MyraArtTexture(id, 0, _zoomSize);
            int natW = art.UV.Width;
            int natH = art.UV.Height;

            if (natW > 0 && natH > 0)
            {
                float scale = (float)_zoomSize / Math.Max(natW, natH);
                preview.Width = Math.Max(1, (int)Math.Round(natW * scale));
                preview.Height = Math.Max(1, (int)Math.Round(natH * scale));
                preview.MaxWidth = preview.Width;
                preview.MaxHeight = preview.Height;
            }

            return new Panel { Width = ZOOM_MAX, Height = _zoomSize, Widgets = { Configure(preview) } };
        }

        private HorizontalStackPanel BuildZoomRow()
        {
            var zoomRow = new HorizontalStackPanel { Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
            zoomRow.Widgets.Add(new MyraButton(TazLang.Get("tinkerer_art_zoomout", "-"), () => SetZoom(Math.Max(ZOOM_MIN, _zoomSize - ZOOM_STEP))));
            zoomRow.Widgets.Add(new MyraLabel(TazLang.Get("tinkerer_art_zoomlevel", [_zoomSize.ToString()]), MyraLabel.TextStyle.P));
            zoomRow.Widgets.Add(new MyraButton(TazLang.Get("tinkerer_art_zoomin", "+"), () => SetZoom(Math.Min(ZOOM_MAX, _zoomSize + ZOOM_STEP))));
            zoomRow.Widgets.Add(new MyraButton(TazLang.Get("tinkerer_art_reset", "Reset"), () => SetZoom(ZOOM_DEFAULT)));
            return zoomRow;
        }

        private void SetZoom(int zoomSize)
        {
            _zoomSize = zoomSize;
            RefreshDetail();
        }

        /// <summary>
        ///     Appends graphic ID, dimensions, and TileData rows (name/flags/weight/...) for <paramref name="id" /> to the
        ///     detail panel.
        /// </summary>
        private void AddInfoRows(uint id)
        {
            ref readonly SpriteInfo art = ref Client.Game!.UO!.Arts.GetArt(id);
            bool hasArt = art.Texture != null;

            _detailPanel.Widgets.Add(new MyraLabel(
                TazLang.Get("tinkerer_art_graphicid", [id.ToString(), $"0x{id:X4}"]), MyraLabel.TextStyle.P));
            _detailPanel.Widgets.Add(new MyraLabel(
                hasArt
                    ? TazLang.Get("tinkerer_art_dimensions", [art.UV.Width.ToString(), art.UV.Height.ToString()])
                    : TazLang.Get("tinkerer_art_dimensions_noart", "Dimensions: No art"),
                MyraLabel.TextStyle.P));

            StaticTiles[]? sd = GetStaticData();
            if (sd != null && id < sd.Length)
            {
                StaticTiles st = sd[id];
                string name = string.IsNullOrEmpty(st.Name) ? TazLang.Get("tinkerer_art_unnamed", "(unnamed)") : st.Name;
                _detailPanel.Widgets.Add(new MyraLabel(TazLang.Get("tinkerer_art_name", [name]), MyraLabel.TextStyle.P));
                _detailPanel.Widgets.Add(new MyraLabel(TazLang.Get("tinkerer_art_flags", [st.Flags.ToString()]), MyraLabel.TextStyle.P));
                _detailPanel.Widgets.Add(new MyraLabel(TazLang.Get("tinkerer_art_weight", [st.Weight.ToString()]), MyraLabel.TextStyle.P));
                _detailPanel.Widgets.Add(new MyraLabel(TazLang.Get("tinkerer_art_height", [st.Height.ToString()]), MyraLabel.TextStyle.P));
                _detailPanel.Widgets.Add(new MyraLabel(TazLang.Get("tinkerer_art_layer", [st.Layer.ToString()]), MyraLabel.TextStyle.P));
                _detailPanel.Widgets.Add(new MyraLabel(TazLang.Get("tinkerer_art_animid", [st.AnimID.ToString(), $"0x{st.AnimID:X4}"]), MyraLabel.TextStyle.P));
                _detailPanel.Widgets.Add(new MyraLabel(TazLang.Get("tinkerer_art_hue", [st.Hue.ToString()]), MyraLabel.TextStyle.P));
                _detailPanel.Widgets.Add(new MyraLabel(TazLang.Get("tinkerer_art_lightindex", [st.LightIndex.ToString()]), MyraLabel.TextStyle.P));
            }
            else
                _detailPanel.Widgets.Add(new MyraLabel(TazLang.Get("tinkerer_art_tiledata_nodata", "TileData: No data"), MyraLabel.TextStyle.P));

            _detailPanel.Widgets.Add(new MyraButton(TazLang.Get("tinkerer_art_copyid", "Copy ID"), () => SDL.SDL_SetClipboardText(id.ToString())));
        }

        private static Widget Configure(Widget w)
        {
            w.HorizontalAlignment = HorizontalAlignment.Center;
            w.VerticalAlignment = VerticalAlignment.Center;
            return w;
        }
    }

    private static StaticTiles[]? GetStaticData() => Client.Game?.UO?.FileManager?.TileData?.StaticData;
}
