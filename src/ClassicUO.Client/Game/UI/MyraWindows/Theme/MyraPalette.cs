#nullable enable

using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.MyraWindows.Theme;

/// <summary>
/// Every colour the Myra windows draw that is not part of a nine-patch skin, named by what it means
/// rather than by what it looks like.
/// <para>
/// One object, swapped whole, so that themes are a matter of supplying another rather than of
/// hunting down literals. Nothing here should be read at type-load time into a
/// <see langword="static" /> <see langword="readonly" /> field elsewhere - that would pin the colour
/// to whichever palette happened to be current at startup.
/// </para>
/// </summary>
public sealed class MyraPalette
{
    #region Public accessors

    /// <summary>Name shown wherever a theme is chosen.</summary>
    public required string Name { get; init; }

    /// <summary>Fill behind a framed area - a rulebase, a banner, a grouped panel.</summary>
    public required Color PanelFill { get; init; }

    /// <summary>Outline of a framed area, and of the grids drawn inside one.</summary>
    public required Color PanelBorder { get; init; }

    /// <summary>
    /// Something the user should read but not act on: a read-only notice, a caption explaining why
    /// a control is unavailable. Warm rather than red - it is information, not a fault.
    /// </summary>
    public required Color Notice { get; init; }

    /// <summary>How far back <see cref="Notice" /> is held when it frames rather than writes.</summary>
    public required float NoticeBorderAlpha { get; init; }

    /// <summary>A value that has been moved off its default, in a property grid.</summary>
    public required Color ModifiedValue { get; init; }

    /// <summary>
    /// Fills for nested brackets, outermost first. Read modulo the list's length, so a palette need
    /// not predict how deeply a tree will nest.
    /// </summary>
    public required IReadOnlyList<Color> NestingFills { get; init; }

    /// <summary>Outlines for nested brackets, outermost first. Read the same way as
    /// <see cref="NestingFills" />.</summary>
    public required IReadOnlyList<Color> NestingBorders { get; init; }

    /// <summary>Text of a widget that cannot be used.</summary>
    public required Color DisabledText { get; init; }

    /// <summary>Backing behind a widget that cannot be used - what makes a control read as
    /// unavailable rather than merely dark.</summary>
    public required Color DisabledFill { get; init; }

    #endregion

    #region Public methods

    /// <summary>
    /// The colour for a given nesting depth, wrapping once the list runs out.
    /// </summary>
    /// <param name="ramp">Either of the nesting lists.</param>
    /// <param name="depth">Nesting depth, from zero.</param>
    /// <returns>The colour.</returns>
    public static Color AtDepth(IReadOnlyList<Color> ramp, int depth) =>
        ramp.Count == 0 ? Color.Transparent : ramp[depth % ramp.Count];

    #endregion
}
