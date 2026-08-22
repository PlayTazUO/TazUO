#nullable enable

using System;
using System.Collections.Generic;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.UI.MyraWindows.Widgets.ArtTexture;
using ClassicUO.Renderer;
using Myra.Graphics2D.UI;
using SDL3;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Assistant.Tinkerer;

/// <summary>
///     Shared builder for a graphic's ItemData (TileData) view. Used both by the
///     <see cref="ItemDataViewMyraWindow" /> pop-out and the Tinkerer ItemData tab's detail panel, so
///     the two can never drift apart.
/// </summary>
internal static class ItemDataInfo
{
    private const int PREVIEW_MAX = 128;

    /// <summary>Builds the art preview and every TileData field for <paramref name="graphic" />.</summary>
    public static VerticalStackPanel Build(uint graphic, int width = 280)
    {
        var panel = new VerticalStackPanel { Spacing = 4, Width = width };

        panel.Widgets.Add(BuildPreview(graphic));

        StaticTiles[]? sd = Client.Game?.UO?.FileManager?.TileData?.StaticData;
        StaticTiles? st = sd != null && graphic < sd.Length ? sd[graphic] : (StaticTiles?)null;

        panel.Widgets.Add(new MyraLabel(
            TazLang.Get("tinkerer_itemdata_graphicid", [graphic.ToString(), $"0x{graphic:X4}"]),
            MyraLabel.TextStyle.P));

        if (st == null)
        {
            panel.Widgets.Add(new MyraLabel(TazLang.Get("tinkerer_itemdata_tiledata_nodata", "TileData: No data"), MyraLabel.TextStyle.P));
            panel.Widgets.Add(CopyButton(graphic));
            return panel;
        }

        StaticTiles data = st.Value;
        string name = string.IsNullOrEmpty(data.Name) ? TazLang.Get("tinkerer_itemdata_unnamed", "(unnamed)") : data.Name;
        panel.Widgets.Add(new MyraLabel(TazLang.Get("tinkerer_itemdata_name", [name]), MyraLabel.TextStyle.P));
        panel.Widgets.Add(new MyraLabel(TazLang.Get("tinkerer_itemdata_flags_raw", [$"{(ulong)data.Flags:X}"]), MyraLabel.TextStyle.P));
        panel.Widgets.Add(new MyraLabel(TazLang.Get("tinkerer_itemdata_flags_list", [FormatFlags(data.Flags)]), MyraLabel.TextStyle.P));
        panel.Widgets.Add(new MyraLabel(TazLang.Get("tinkerer_itemdata_weight", [data.Weight.ToString()]), MyraLabel.TextStyle.P));
        panel.Widgets.Add(new MyraLabel(TazLang.Get("tinkerer_itemdata_height", [data.Height.ToString()]), MyraLabel.TextStyle.P));
        panel.Widgets.Add(new MyraLabel(TazLang.Get("tinkerer_itemdata_layer", [data.Layer.ToString(), ((Layer)data.Layer).ToString()]), MyraLabel.TextStyle.P));
        panel.Widgets.Add(new MyraLabel(TazLang.Get("tinkerer_itemdata_count", [data.Count.ToString()]), MyraLabel.TextStyle.P));
        panel.Widgets.Add(new MyraLabel(TazLang.Get("tinkerer_itemdata_animid", [data.AnimID.ToString(), $"0x{data.AnimID:X4}"]), MyraLabel.TextStyle.P));
        panel.Widgets.Add(new MyraLabel(TazLang.Get("tinkerer_itemdata_hue", [data.Hue.ToString(), $"0x{data.Hue:X4}"]), MyraLabel.TextStyle.P));
        panel.Widgets.Add(new MyraLabel(TazLang.Get("tinkerer_itemdata_lightindex", [data.LightIndex.ToString()]), MyraLabel.TextStyle.P));

        panel.Widgets.Add(CopyButton(graphic));
        return panel;
    }

    /// <summary>Art preview scaled to <see cref="PREVIEW_MAX" /> while preserving aspect ratio.</summary>
    private static Widget BuildPreview(uint graphic)
    {
        if (Client.Game?.UO?.Arts == null)
            return new MyraLabel(TazLang.Get("tinkerer_itemdata_noart", "(No art at this graphic)"), MyraLabel.TextStyle.P);

        ref readonly SpriteInfo art = ref Client.Game.UO.Arts.GetArt(graphic);
        if (art.Texture == null)
            return new MyraLabel(TazLang.Get("tinkerer_itemdata_noart", "(No art at this graphic)"), MyraLabel.TextStyle.P);

        var preview = new MyraArtTexture(graphic, 0, PREVIEW_MAX);
        int natW = art.UV.Width;
        int natH = art.UV.Height;
        if (natW > 0 && natH > 0)
        {
            float scale = (float)PREVIEW_MAX / Math.Max(natW, natH);
            preview.Width = Math.Max(1, (int)Math.Round(natW * scale));
            preview.Height = Math.Max(1, (int)Math.Round(natH * scale));
            preview.MaxWidth = preview.Width;
            preview.MaxHeight = preview.Height;
        }

        return new Panel { Width = PREVIEW_MAX, Height = PREVIEW_MAX, Widgets = { Configure(preview) } };
    }

    /// <summary>Comma-separated names of every set <see cref="TileFlag" />, or "(none)".</summary>
    private static string FormatFlags(TileFlag flags)
    {
        var set = new List<string>();
        foreach (TileFlag flag in Enum.GetValues<TileFlag>())
        {
            if (flag == TileFlag.None)
                continue;

            if ((flags & flag) == flag)
                set.Add(flag.ToString());
        }

        return set.Count == 0
            ? TazLang.Get("tinkerer_itemdata_flags_none", "(none)")
            : string.Join(", ", set);
    }

    private static MyraButton CopyButton(uint graphic) =>
        new(TazLang.Get("tinkerer_itemdata_copyid", "Copy ID"), () => SDL.SDL_SetClipboardText(graphic.ToString()));

    private static Widget Configure(Widget w)
    {
        w.HorizontalAlignment = HorizontalAlignment.Center;
        w.VerticalAlignment = VerticalAlignment.Center;
        return w;
    }
}
