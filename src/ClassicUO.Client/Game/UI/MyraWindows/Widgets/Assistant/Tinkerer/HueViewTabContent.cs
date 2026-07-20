#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using ClassicUO.Assets;
using Microsoft.Xna.Framework;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Assistant.Tinkerer;

public static class HueViewTabContent
{
    private const int PAGE_SIZE = 50;
    private const int SWATCH_W = 10;
    private const int SWATCH_H = 18;
    private const int COLORS_PER_HUE = 32;
    private const int HUE_ID_WIDTH = 46;

    public static Widget Build()
    {
        HuesLoader huesLoader = Client.Game.UO.FileManager.Hues;
        if (huesLoader == null || huesLoader.HuesCount == 0)
            return new MyraLabel("Hue data not available", MyraLabel.TextStyle.P);

        var root = new VerticalStackPanel { Spacing = 4 };

        string filterNum = "";
        string filterName = "";
        List<(int id, string name)> allHues = BuildHueList(huesLoader);
        var filteredHues = new List<(int id, string name)>(allHues);
        int currentPage = 0;

        var statusLabel = new MyraLabel("", MyraLabel.TextStyle.P);
        var pageLabel = new MyraLabel("", MyraLabel.TextStyle.P);
        var resultsPanel = new VerticalStackPanel { Spacing = 1 };
        MyraButton prevBtn = null!;
        MyraButton nextBtn = null!;

        void BuildPage()
        {
            resultsPanel.Widgets.Clear();

            int total = filteredHues.Count;
            int totalPages = Math.Max(1, (total + PAGE_SIZE - 1) / PAGE_SIZE);
            if (currentPage >= totalPages) currentPage = totalPages - 1;

            statusLabel.Text = $"{total:N0} hue{(total == 1 ? "" : "s")}";
            pageLabel.Text = $"Page {currentPage + 1} of {totalPages}";
            prevBtn.Enabled = currentPage > 0;
            nextBtn.Enabled = currentPage < totalPages - 1;

            int start = currentPage * PAGE_SIZE;
            int end = Math.Min(start + PAGE_SIZE, total);

            for (int i = start; i < end; i++)
            {
                var (hueId, name) = filteredHues[i];
                resultsPanel.Widgets.Add(BuildHueRow(huesLoader, hueId, name));
            }
        }

        void ApplyFilter()
        {
            filteredHues.Clear();
            foreach (var (id, name) in allHues)
            {
                if (!string.IsNullOrEmpty(filterNum) && !id.ToString().Contains(filterNum))
                    continue;
                if (!string.IsNullOrEmpty(filterName) && !name.Contains(filterName, StringComparison.OrdinalIgnoreCase))
                    continue;
                filteredHues.Add((id, name));
            }
            currentPage = 0;
            BuildPage();
        }

        // Filter row
        var filterRow = new HorizontalStackPanel { Spacing = 6, VerticalAlignment = VerticalAlignment.Center };
        var numBox = new MyraInputBox { HintText = "Filter by #", Width = 100 };
        numBox.TextChangedByUser += (_, _) => filterNum = numBox.Text ?? "";
        var nameBox = new MyraInputBox { HintText = "Filter by name", Width = 180 };
        nameBox.TextChangedByUser += (_, _) => filterName = nameBox.Text ?? "";

        filterRow.Widgets.Add(new MyraLabel("#:", MyraLabel.TextStyle.P));
        filterRow.Widgets.Add(numBox);
        filterRow.Widgets.Add(new MyraLabel("Name:", MyraLabel.TextStyle.P));
        filterRow.Widgets.Add(nameBox);
        filterRow.Widgets.Add(new MyraButton("Search", ApplyFilter));
        filterRow.Widgets.Add(new MyraButton("Clear", () =>
        {
            filterNum = ""; numBox.Text = "";
            filterName = ""; nameBox.Text = "";
            ApplyFilter();
        }));
        root.Widgets.Add(filterRow);
        root.Widgets.Add(statusLabel);

        // Pagination controls
        prevBtn = new MyraButton("< Prev", () => { currentPage--; BuildPage(); }) { Enabled = false };
        nextBtn = new MyraButton("Next >", () => { currentPage++; BuildPage(); }) { Enabled = false };

        var pageRow = new HorizontalStackPanel { Spacing = 6 };
        pageRow.Widgets.Add(prevBtn);
        pageRow.Widgets.Add(pageLabel);
        pageRow.Widgets.Add(nextBtn);
        root.Widgets.Add(pageRow);

        // Column headers
        var headerRow = new HorizontalStackPanel { Spacing = 2 };
        headerRow.Widgets.Add(new MyraLabel("ID", MyraLabel.TextStyle.TableHeader) { Width = HUE_ID_WIDTH });
        headerRow.Widgets.Add(new MyraLabel($"Colors (0–{COLORS_PER_HUE - 1})", MyraLabel.TextStyle.TableHeader));
        root.Widgets.Add(headerRow);

        root.Widgets.Add(new ScrollViewer { MaxHeight = 450, Content = resultsPanel });

        BuildPage();
        return root;
    }

    private static List<(int id, string name)> BuildHueList(HuesLoader huesLoader)
    {
        int total = huesLoader.HuesCount;
        var list = new List<(int id, string name)>(total);
        for (int i = 1; i <= total; i++)
            list.Add((i, GetHueName(huesLoader, i)));
        return list;
    }

    private static unsafe string GetHueName(HuesLoader huesLoader, int hueId)
    {
        int idx = hueId - 1;
        int g = idx >> 3;
        int e = idx % 8;
        HuesGroup[] huesRange = huesLoader.HuesRange;
        if (g >= huesRange.Length) return "";

        fixed (HuesGroup* pGroup = &huesRange[g])
        {
            HuesBlock* blocks = (HuesBlock*)(void*)(&pGroup->Entries);
            HuesBlock* pBlock = blocks + e;
            byte* namePtr = pBlock->Name;
            int len = 0;
            while (len < 20 && namePtr[len] != 0) len++;
            return len == 0 ? "" : Encoding.ASCII.GetString(namePtr, len);
        }
    }

    private static Widget BuildHueRow(HuesLoader huesLoader, int hueId, string name)
    {
        var row = new HorizontalStackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };

        row.Widgets.Add(new MyraLabel(hueId.ToString(), MyraLabel.TextStyle.P)
        {
            Width = HUE_ID_WIDTH,
            Tooltip = string.IsNullOrEmpty(name) ? null : name
        });

        var swatchRow = new HorizontalStackPanel { Spacing = 0 };
        for (int c = 0; c < COLORS_PER_HUE; c++)
        {
            uint rgba = huesLoader.GetHueColorRgba8888((ushort)c, (ushort)hueId);
            var color = new Color { PackedValue = rgba };

            var swatch = new Panel
            {
                Width = SWATCH_W,
                Height = SWATCH_H,
                Background = new SolidBrush(color),
                Tooltip = $"Hue {hueId} / Index {c}: #{color.R:X2}{color.G:X2}{color.B:X2}"
            };
            swatchRow.Widgets.Add(swatch);
        }
        row.Widgets.Add(swatchRow);

        if (!string.IsNullOrEmpty(name))
            row.Widgets.Add(new MyraLabel($"  {name}", MyraLabel.TextStyle.P));

        return row;
    }
}
