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
/// Tinkerer tab for browsing gump art. A paged square grid of gump graphics on
/// the left; clicking a cell shows an enlarged preview with zoom controls and
/// basic metadata on the right.
/// </summary>
public static class GumpBrowserTabContent
{
    private const int COLS = 8;
    private const int ROWS = 8;
    private const int PAGE_SIZE = COLS * ROWS;
    private const int CELL = 44;
    private const int CELL_ID_FONT = 10;

    private const int ZOOM_MIN = 32;
    private const int ZOOM_MAX = 512;
    private const int ZOOM_STEP = 32;
    private const int ZOOM_DEFAULT = 128;

    private static readonly SolidBrush SelectedBorder = new SolidBrush(Color.Gold);

    public static Widget Build()
    {
        if (Client.Game?.UO?.Gumps == null)
            return new MyraLabel(TazLang.Get("tinkerer_gump_nodata", "Gump data not available"), MyraLabel.TextStyle.P);

        // Upper bound on browsable gump graphics. Gump art is indexed by gump
        // graphic id; cap to the available file entries / max range.
        int maxGraphic = GumpsLoader.MAX_GUMP_DATA_INDEX_COUNT;
        var gumpFile = Client.Game.UO.FileManager?.Gumps?.File;
        if (gumpFile?.Entries != null && gumpFile.Entries.Length < maxGraphic)
            maxGraphic = gumpFile.Entries.Length;

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
                    TazLang.Get("tinkerer_gump_selectprompt", "Select a gump graphic to view details."),
                    MyraLabel.TextStyle.P));
                return;
            }

            uint id = (uint)selectedGraphic;
            ref readonly SpriteInfo gump = ref Client.Game.UO.Gumps.GetGump(id);
            bool hasArt = gump.Texture != null;

            // Preview, scaled to the requested zoom while preserving aspect ratio.
            if (hasArt)
            {
                var preview = new MyraGumpTexture(id, zoomSize);
                int natW = gump.UV.Width;
                int natH = gump.UV.Height;
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
                detailPanel.Widgets.Add(new MyraLabel(TazLang.Get("tinkerer_gump_noart", "(No art at this graphic)"), MyraLabel.TextStyle.P));
            }

            // Zoom controls
            var zoomRow = new HorizontalStackPanel { Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
            zoomRow.Widgets.Add(new MyraButton(TazLang.Get("tinkerer_gump_zoomout", "-"), () =>
            {
                zoomSize = Math.Max(ZOOM_MIN, zoomSize - ZOOM_STEP);
                BuildDetail();
            }));
            zoomRow.Widgets.Add(new MyraLabel(TazLang.Get("tinkerer_gump_zoomlevel", new[] { zoomSize.ToString() }), MyraLabel.TextStyle.P));
            zoomRow.Widgets.Add(new MyraButton(TazLang.Get("tinkerer_gump_zoomin", "+"), () =>
            {
                zoomSize = Math.Min(ZOOM_MAX, zoomSize + ZOOM_STEP);
                BuildDetail();
            }));
            zoomRow.Widgets.Add(new MyraButton(TazLang.Get("tinkerer_gump_reset", "Reset"), () =>
            {
                zoomSize = ZOOM_DEFAULT;
                BuildDetail();
            }));
            detailPanel.Widgets.Add(zoomRow);

            // Info
            detailPanel.Widgets.Add(new MyraLabel(
                TazLang.Get("tinkerer_gump_graphicid", new[] { id.ToString(), $"0x{id:X4}" }), MyraLabel.TextStyle.P));
            detailPanel.Widgets.Add(new MyraLabel(
                hasArt
                    ? TazLang.Get("tinkerer_gump_dimensions", new[] { gump.UV.Width.ToString(), gump.UV.Height.ToString() })
                    : TazLang.Get("tinkerer_gump_dimensions_noart", "Dimensions: No art"),
                MyraLabel.TextStyle.P));

            detailPanel.Widgets.Add(new MyraButton(TazLang.Get("tinkerer_gump_copyid", "Copy ID"), () => SDL.SDL_SetClipboardText(id.ToString())));
        }

        void BuildPage()
        {
            if (currentPage < 0) currentPage = 0;
            if (currentPage >= totalPages) currentPage = totalPages - 1;

            gridPanel.Widgets.Clear();
            pageLabel.Text = TazLang.Get("tinkerer_gump_page", new[] { (currentPage + 1).ToString(), totalPages.ToString() });
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

        // Jump-to-graphic row
        var jumpRow = new HorizontalStackPanel { Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
        var jumpBox = new MyraInputBox
        {
            HintText = TazLang.Get("tinkerer_gump_jumphint", "Graphic # or 0x.."),
            Width = 120,
            InputFilter = MyraInputBox.HueInputFilter
        };
        void DoJump()
        {
            if (TryParseGraphic(jumpBox.Text, out int gfx))
                JumpTo(gfx);
        }
        jumpBox.TextChangedByUser += (_, _) => DoJump();
        jumpRow.Widgets.Add(new MyraLabel(TazLang.Get("tinkerer_gump_goto", "Go to:"), MyraLabel.TextStyle.P));
        jumpRow.Widgets.Add(jumpBox);
        jumpRow.Widgets.Add(new MyraButton(TazLang.Get("tinkerer_gump_jump", "Jump"), DoJump));
        leftColumn.Widgets.Add(jumpRow);

        // Pagination controls
        prevBtn = new MyraButton(TazLang.Get("tinkerer_gump_prev", "< Prev"), () => { currentPage--; BuildPage(); }) { Enabled = false };
        nextBtn = new MyraButton(TazLang.Get("tinkerer_gump_next", "Next >"), () => { currentPage++; BuildPage(); }) { Enabled = false };
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

        var art = new MyraGumpTexture((uint)id, CELL - 4)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        cell.Widgets.Add(art);

        // Overlay the graphic id at the bottom of the cell so it can be read
        // at a glance without hovering for the tooltip.
        var idLabel = new MyraLabel(id.ToString(), CELL_ID_FONT)
        {
            Wrap = false,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom
        };
        cell.Widgets.Add(idLabel);

        cell.TouchDown += (_, _) => onSelect(id);
        return cell;
    }

    private static string BuildCellTooltip(int id)
    {
        return TazLang.Get("tinkerer_gump_tooltip", new[] { id.ToString(), $"0x{id:X4}" });
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
