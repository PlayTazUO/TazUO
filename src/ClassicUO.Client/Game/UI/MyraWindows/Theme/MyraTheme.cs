#nullable enable

using System;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.MyraWindows.Theme;

/// <summary>
/// The palette in force. Everything that draws a colour by hand reads
/// <see cref="Current" /> at build time rather than caching it, so swapping the palette and
/// rebuilding a window is all a theme change takes.
/// <para>
/// Translucent colours are built through <see cref="Color.FromNonPremultiplied" />. Myra draws under
/// <c>BlendState.AlphaBlend</c>, which in FNA is premultiplied - source blend <c>One</c>, destination
/// <c>InverseSourceAlpha</c> - so a raw <c>new Color(255, 255, 255, 20)</c> is drawn at full white
/// over 92% of what is behind it and blows out to near-solid white. A black scrim survives the
/// mistake, which is why every other colour in the client gets away with the raw constructor.
/// </para>
/// </summary>
public static class MyraTheme
{
    #region Public events

    /// <summary>Raised after <see cref="Current" /> is replaced, for anything that has to redraw
    /// rather than merely be rebuilt.</summary>
    public static event EventHandler? Changed;

    #endregion

    #region Public accessors

    /// <summary>
    /// The palette every Myra window draws from.
    /// <para>
    /// Anything written into the Myra stylesheet is read from here by
    /// <see cref="MyraStyle.SetDefault" />, so a palette swap has to be followed by a call to it.
    /// Everything else reads the palette as it builds, and needs only a rebuild.
    /// </para>
    /// </summary>
    /// <exception cref="ArgumentNullException">Set to null.</exception>
    public static MyraPalette Current
    {
        // Falls back rather than being initialised to Dark. A static initialiser would run in
        // declaration order and read Dark's backing store before Dark's own initialiser had filled
        // it - null, silently, until the first access threw. Resolving on read has no order to get
        // wrong, whatever a later edit does to the order of the members here.
        get => field ?? Dark;

        set
        {
            ArgumentNullException.ThrowIfNull(value);

            if (ReferenceEquals(field, value))
                return;

            field = value;

            Changed?.Invoke(null, EventArgs.Empty);
        }
    }

    /// <summary>The shipped dark palette, matching the modern UI skin.</summary>
    public static MyraPalette Dark { get; } = new()
    {
        Name = "Dark",
        PanelFill = Color.FromNonPremultiplied(0, 0, 0, 25),
        PanelBorder = Color.FromNonPremultiplied(0, 0, 0, 125),
        Notice = new Color(235, 200, 120),
        NoticeBorderAlpha = 0.35f,
        ModifiedValue = new Color(235, 200, 120),

        // Alternating dark and light rather than one tint deepened step by step: a stack of tints
        // all darker than the panel behind them differs from its neighbour by a few percent, and
        // depth stops being readable two levels in. Swapping direction each level makes every
        // boundary a contrast edge.
        NestingFills =
        [
            Color.FromNonPremultiplied(0, 0, 0, 70),
            Color.FromNonPremultiplied(255, 255, 255, 22),
            Color.FromNonPremultiplied(0, 0, 0, 95),
            Color.FromNonPremultiplied(255, 255, 255, 34),
            Color.FromNonPremultiplied(0, 0, 0, 120)
        ],

        // Light rather than a darker shade of the fill: the panel these sit on is already dark, so
        // a dark border is the one thing guaranteed not to read as an edge. Each level is brighter
        // than the last, which is what makes an inner bracket look nearer than the one holding it.
        NestingBorders =
        [
            Color.FromNonPremultiplied(120, 140, 190, 150),
            Color.FromNonPremultiplied(150, 170, 215, 180),
            Color.FromNonPremultiplied(180, 200, 235, 205),
            Color.FromNonPremultiplied(205, 220, 245, 225),
            Color.FromNonPremultiplied(230, 240, 255, 245)
        ],

        DisabledText = new Color(128, 128, 128),
        DisabledFill = Color.FromNonPremultiplied(0, 0, 0, 90)
    };

    #endregion
}
