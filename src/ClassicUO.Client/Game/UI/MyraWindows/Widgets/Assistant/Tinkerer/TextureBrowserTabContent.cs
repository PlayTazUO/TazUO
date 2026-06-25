#nullable enable
using System;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;
using SDL3;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Assistant.Tinkerer;

/// <summary>
/// Tinkerer tab for browsing texmaps (the stretched land textures referenced by
/// land tiles via their TexID). A paged square grid of textures on the left;
/// clicking a cell shows an enlarged preview with zoom controls and metadata on
/// the right, including which land graphics reference the texture.
/// </summary>
public static class TextureBrowserTabContent
{
    private const int COLS = 8;
    private const int ROWS = 8;
    private const int PAGE_SIZE = COLS * ROWS;
    private const int CELL = 44;

    private const int ZOOM_MIN = 32;
    private const int ZOOM_MAX = 512;
    private const int ZOOM_STEP = 32;
    private const int ZOOM_DEFAULT = 128;

    private static readonly SolidBrush SelectedBorder = new SolidBrush(Color.Gold);

    public static Widget Build()
    {
        if (Client.Game?.UO?.Texmaps == null)
            return new MyraLabel(TazLang.Get("tinkerer_texture_nodata", "Texture data not available"), MyraLabel.TextStyle.P);

        // Texmaps are indexed by texture id; cap to the available texmap range.
        int maxGraphic = TexmapsLoader.MAX_LAND_TEXTURES_DATA_INDEX_COUNT;
        var entries = Client.Game.UO.FileManager?.Texmaps?.File?.Entries;
        if (entries != null && entries.Length < maxGraphic)
            maxGraphic = entries.Length;

        int totalPages = Math.Max(1, (maxGraphic + PAGE_SIZE - 1) / PAGE_SIZE);

        int currentPage = 0;
        int selectedGraphic = -1;
        int zoomSize = ZOOM_DEFAULT;

        // --- Left (grid) column -------------------------------------------------
        var gridPanel = new VerticalStackPanel { Spacing = 1 };
        var pageLabel = new MyraLabel("", MyraLabel.TextStyle.P);
        MyraButton prevBtn = null!;
        MyraButton nextBtn = null!;

        // --- Right (detail) column ---------------------------------------------
        var detailPanel = new VerticalStackPanel { Spacing = 4, Width = 280 };

        void BuildDetail()
        {
            detailPanel.Widgets.Clear();

            if (selectedGraphic < 0)
            {
                detailPanel.Widgets.Add(new MyraLabel(
                    TazLang.Get("tinkerer_texture_selectprompt", "Select a texture to view details."),
                    MyraLabel.TextStyle.P));
                return;
            }

            uint id = (uint)selectedGraphic;
            ref readonly SpriteInfo tex = ref Client.Game.UO.Texmaps.GetTexmap(id);
            bool hasArt = tex.Texture != null;

            // Preview, scaled to the requested zoom while preserving aspect ratio.
            if (hasArt)
            {
                var preview = new MyraTexmapTexture(id, zoomSize);
                int natW = tex.UV.Width;
                int natH = tex.UV.Height;
                if (natW > 0 && natH > 0)
                {
                    float scale = (float)zoomSize / Math.Max(natW, natH);
                    preview.Width = Math.Max(1, (int)Math.Round(natW * scale));
                    preview.Height = Math.Max(1, (int)Math.Round(natH * scale));
                    preview.MaxWidth = preview.Width;
                    preview.MaxHeight = preview.Height;
                }
                detailPanel.Widgets.Add(new Panel
                {
                    Width = ZOOM_MAX,
                    Height = zoomSize,
                    Widgets = { Configure(preview) }
                });
            }
            else
            {
                detailPanel.Widgets.Add(new MyraLabel(TazLang.Get("tinkerer_texture_noart", "(No texture at this id)"), MyraLabel.TextStyle.P));
            }

            // Zoom controls
            var zoomRow = new HorizontalStackPanel { Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
            zoomRow.Widgets.Add(new MyraButton(TazLang.Get("tinkerer_texture_zoomout", "-"), () =>
            {
                zoomSize = Math.Max(ZOOM_MIN, zoomSize - ZOOM_STEP);
                BuildDetail();
            }));
            zoomRow.Widgets.Add(new MyraLabel(TazLang.Get("tinkerer_texture_zoomlevel", new[] { zoomSize.ToString() }), MyraLabel.TextStyle.P));
            zoomRow.Widgets.Add(new MyraButton(TazLang.Get("tinkerer_texture_zoomin", "+"), () =>
            {
                zoomSize = Math.Min(ZOOM_MAX, zoomSize + ZOOM_STEP);
                BuildDetail();
            }));
            zoomRow.Widgets.Add(new MyraButton(TazLang.Get("tinkerer_texture_reset", "Reset"), () =>
            {
                zoomSize = ZOOM_DEFAULT;
                BuildDetail();
            }));
            detailPanel.Widgets.Add(zoomRow);

            // Info
            detailPanel.Widgets.Add(new MyraLabel(
                TazLang.Get("tinkerer_texture_texid", new[] { id.ToString(), $"0x{id:X4}" }), MyraLabel.TextStyle.P));
            detailPanel.Widgets.Add(new MyraLabel(
                hasArt
                    ? TazLang.Get("tinkerer_texture_dimensions", new[] { tex.UV.Width.ToString(), tex.UV.Height.ToString() })
                    : TazLang.Get("tinkerer_texture_dimensions_noart", "Dimensions: No texture"),
                MyraLabel.TextStyle.P));

            // Land tiles referencing this texture, if any.
            int landGraphic = FindLandUsingTexture(id);
            if (landGraphic >= 0)
            {
                var ld = Client.Game.UO.FileManager?.TileData?.LandData;
                string name = ld != null && landGraphic < ld.Length && !string.IsNullOrEmpty(ld[landGraphic].Name)
                    ? ld[landGraphic].Name
                    : TazLang.Get("tinkerer_texture_unnamed", "(unnamed)");
                detailPanel.Widgets.Add(new MyraLabel(
                    TazLang.Get("tinkerer_texture_usedby", new[] { landGraphic.ToString(), $"0x{landGraphic:X4}", name }),
                    MyraLabel.TextStyle.P));
            }
            else
            {
                detailPanel.Widgets.Add(new MyraLabel(
                    TazLang.Get("tinkerer_texture_usedby_none", "Used by land: (none)"), MyraLabel.TextStyle.P));
            }

            detailPanel.Widgets.Add(new MyraButton(TazLang.Get("tinkerer_texture_copyid", "Copy ID"), () => SDL.SDL_SetClipboardText(id.ToString())));
        }

        void BuildPage()
        {
            if (currentPage < 0) currentPage = 0;
            if (currentPage >= totalPages) currentPage = totalPages - 1;

            gridPanel.Widgets.Clear();
            pageLabel.Text = TazLang.Get("tinkerer_texture_page", new[] { (currentPage + 1).ToString(), totalPages.ToString() });
            prevBtn.Enabled = currentPage > 0;
            nextBtn.Enabled = currentPage < totalPages - 1;

            int start = currentPage * PAGE_SIZE;

            for (int r = 0; r < ROWS; r++)
            {
                var rowPanel = new HorizontalStackPanel { Spacing = 1 };
                for (int c = 0; c < COLS; c++)
                {
                    int id = start + r * COLS + c;
                    if (id >= maxGraphic)
                    {
                        rowPanel.Widgets.Add(new Panel { Width = CELL, Height = CELL });
                        continue;
                    }

                    rowPanel.Widgets.Add(BuildCell(id, selectedGraphic, gfx =>
                    {
                        selectedGraphic = gfx;
                        BuildPage();
                        BuildDetail();
                    }));
                }
                gridPanel.Widgets.Add(rowPanel);
            }
        }

        void JumpTo(int id)
        {
            if (id < 0 || id >= maxGraphic) return;
            selectedGraphic = id;
            currentPage = id / PAGE_SIZE;
            BuildPage();
            BuildDetail();
        }

        var leftColumn = new VerticalStackPanel { Spacing = 4 };

        // Jump-to-texture row
        var jumpRow = new HorizontalStackPanel { Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
        var jumpBox = new MyraInputBox
        {
            HintText = TazLang.Get("tinkerer_texture_jumphint", "Texture # or 0x.."),
            Width = 120,
            InputFilter = MyraInputBox.HueInputFilter
        };
        void DoJump()
        {
            if (TryParseGraphic(jumpBox.Text, out int gfx))
                JumpTo(gfx);
        }
        jumpBox.TextChangedByUser += (_, _) => DoJump();
        jumpRow.Widgets.Add(new MyraLabel(TazLang.Get("tinkerer_texture_goto", "Go to:"), MyraLabel.TextStyle.P));
        jumpRow.Widgets.Add(jumpBox);
        jumpRow.Widgets.Add(new MyraButton(TazLang.Get("tinkerer_texture_jump", "Jump"), DoJump));
        leftColumn.Widgets.Add(jumpRow);

        // Pagination controls
        prevBtn = new MyraButton(TazLang.Get("tinkerer_texture_prev", "< Prev"), () => { currentPage--; BuildPage(); }) { Enabled = false };
        nextBtn = new MyraButton(TazLang.Get("tinkerer_texture_next", "Next >"), () => { currentPage++; BuildPage(); }) { Enabled = false };
        var pageRow = new HorizontalStackPanel { Spacing = 6, VerticalAlignment = VerticalAlignment.Center };
        pageRow.Widgets.Add(prevBtn);
        pageRow.Widgets.Add(pageLabel);
        pageRow.Widgets.Add(nextBtn);
        leftColumn.Widgets.Add(pageRow);

        leftColumn.Widgets.Add(new ScrollViewer { MaxHeight = 450, Content = gridPanel });

        var root = new HorizontalStackPanel { Spacing = 12 };
        root.Widgets.Add(leftColumn);
        root.Widgets.Add(detailPanel);

        BuildPage();
        BuildDetail();
        return root;
    }

    private static Widget BuildCell(int id, int selectedGraphic, Action<int> onSelect)
    {
        var cell = new Panel
        {
            Width = CELL,
            Height = CELL,
            BorderThickness = new Thickness(1),
            Tooltip = BuildCellTooltip(id)
        };

        if (id == selectedGraphic)
            cell.Border = SelectedBorder;

        var art = new MyraTexmapTexture((uint)id, CELL - 4)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        cell.Widgets.Add(art);

        cell.TouchDown += (_, _) => onSelect(id);
        return cell;
    }

    private static string BuildCellTooltip(int id)
    {
        return TazLang.Get("tinkerer_texture_tooltip", new[] { id.ToString(), $"0x{id:X4}" });
    }

    /// <summary>
    /// Returns the first land graphic whose TexID matches the given texture id,
    /// or -1 if none reference it.
    /// </summary>
    private static int FindLandUsingTexture(uint texId)
    {
        var ld = Client.Game.UO.FileManager?.TileData?.LandData;
        if (ld == null)
            return -1;

        for (int i = 0; i < ld.Length; i++)
        {
            if (ld[i].TexID == texId)
                return i;
        }

        return -1;
    }

    private static bool TryParseGraphic(string? text, out int graphic)
    {
        graphic = 0;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        text = text.Trim();
        if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return int.TryParse(text.AsSpan(2), System.Globalization.NumberStyles.HexNumber,
                System.Globalization.CultureInfo.InvariantCulture, out graphic);
        }

        return StringHelper.TryParseInt(text, out graphic);
    }

    private static Widget Configure(Widget w)
    {
        w.HorizontalAlignment = HorizontalAlignment.Center;
        w.VerticalAlignment = VerticalAlignment.Center;
        return w;
    }
}
