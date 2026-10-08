#nullable enable

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra.Graphics2D;
using Myra.Graphics2D.TextureAtlases;

namespace ClassicUO.Game.UI.MyraWindows.Theme;

/// <summary>
///     A stylesheet image that can regenerate its texture without invalidating the holders of it.
/// </summary>
/// <remarks>
///     For style art the client draws itself rather than loads, which has to be remade when the UI font
///     changes. Myra copies a style's <see cref="IImage" /> into each widget as it is built and never
///     re-reads it, so putting a new image into the stylesheet would leave open windows drawing the old
///     one - and disposing its texture would leave them drawing a dead one. Handing out this instead
///     keeps one image the stylesheet and every widget share, so a <see cref="Regenerate" /> reaches
///     all of them at once and the texture it replaces has no readers left.
///     <para>
///         The generator must return a texture nothing else holds, since this disposes each one it
///         replaces: draw a new one per call, never a loaded asset or anything from a shared cache.
///         Taking a generator rather than a texture is what makes that the only way to misuse this.
///     </para>
///     <para>Not thread safe, and not meant to be - regeneration comes from the UI thread's restyle.</para>
/// </remarks>
internal sealed class SwappableImage : IImage
{
    #region Private members

    private readonly Func<Texture2D> _generate;

    private TextureRegion _region;

    #endregion

    #region Ctor

    /// <summary>Builds an image over a first generated texture.</summary>
    /// <param name="generate">
    ///     Draws the texture, now and on every <see cref="Regenerate" />. Reads whatever the current
    ///     style calls for, so it must not capture values a restyle changes.
    /// </param>
    /// <exception cref="ArgumentNullException">The generator is null.</exception>
    public SwappableImage(Func<Texture2D> generate)
    {
        ArgumentNullException.ThrowIfNull(generate);

        _generate = generate;
        _region = new TextureRegion(generate());
    }

    #endregion

    #region Public accessors

    /// <inheritdoc />
    public Point Size => _region.Size;

    #endregion

    #region Public methods

    /// <summary>
    ///     Redraws the texture for the current style and disposes the one it replaces, leaving every
    ///     widget already holding this image showing the new one.
    /// </summary>
    public void Regenerate()
    {
        Texture2D outgoing = _region.Texture;

        _region = new TextureRegion(_generate());

        // Swapped in first, so a draw racing this never reaches the disposed texture. The identity
        // check covers a generator that hands back the texture it was already given.
        if (outgoing != _region.Texture)
            outgoing.Dispose();
    }

    /// <inheritdoc />
    public void Draw(RenderContext context, Rectangle dest, Color color) => _region.Draw(context, dest, color);

    #endregion
}
