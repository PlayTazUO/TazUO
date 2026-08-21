#nullable enable
using System;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using ClassicUO.Utility.Collections;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra.Graphics2D;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.ArtTexture;

/// <summary>
///     Renders a UO art graphic, optionally hued, as a Myra <see cref="IImage" />.
/// </summary>
/// <remarks>
///     <para>
///         Myra draws through its own <c>SpriteBatch</c> with no way to inject the client's hue shader, and its
///         only recoloring knob is a flat tint multiplied over every texel. That is wrong for UO hues: real hue
///         rendering remaps each texel through a 32-shade ramp and, for partial-hue graphics, skips colored
///         texels entirely. A flat multiply dyes the whole sprite — a dye tub's wood along with its liquid.
///     </para>
///     <para>
///         So the hue is baked into pixels up front via <see cref="ClassicUO.Renderer.Arts.Art.GetHuedArtPixels" /> and
///         the
///         result uploaded to its own texture, leaving the render tint to carry nothing but alpha. Unhued
///         graphics skip all of this and keep drawing straight from the shared atlas.
///     </para>
/// </remarks>
internal class HuedTexture : IImage
{
    #region Private members

    /// <summary>
    ///     Cap on distinct baked (graphic, hue) variants held at once. Each costs one small texture, and only
    ///     hue swatches ever populate this, so the ceiling exists to bound a pathological session rather than
    ///     to manage ordinary use.
    /// </summary>
    private const int BAKED_ART_CACHE_CAPACITY = 256;

    /// <summary>
    ///     Shared so that reopening a window reuses earlier bakes instead of re-uploading them. Textures are
    ///     owned by the cache and disposed on eviction, which is what keeps them off Myra's widget lifetime —
    ///     <see cref="Image" /> is not <see cref="IDisposable" />, so a per-widget texture would simply leak.
    /// </summary>
    /// <remarks>
    ///     Bound to the render thread: <see cref="BoundedCache{TKey,TValue}" /> asserts it in debug builds, and
    ///     the constraint is real regardless — creating a <see cref="Texture2D" /> off the render thread is
    ///     invalid no matter how the cache is synchronized. Every caller reaches here from a UI event raised by
    ///     the game loop, so this holds today.
    /// </remarks>
    private static readonly BoundedCache<HuedArtKey, Texture2D?> _bakedArt = new(BAKED_ART_CACHE_CAPACITY);

    private readonly uint _graphic;

    /// <summary>The unhued sprite within the shared atlas. Never owned here, never disposed.</summary>
    private readonly TextureRegion? _atlasRegion;

    /// <summary>Whatever is currently being drawn: the atlas region, or a baked hue variant.</summary>
    private TextureRegion? _region;

    #endregion

    #region Public accessors

    /// <summary>Tint applied at draw time. Carries opacity only — the hue lives in the texels.</summary>
    public Color RenderColor { get; set; }

    /// <summary>Native size of the trimmed sprite, in pixels.</summary>
    public Point Size { get; }

    /// <summary>False when the graphic could not be resolved, in which case nothing should be drawn.</summary>
    public bool IsValid => _region != null;

    #endregion

    #region Ctor

    /// <summary>
    ///     Resolves an art graphic for display.
    /// </summary>
    /// <param name="graphic">Art graphic ID, without the 0x4000 offset.</param>
    /// <param name="hue">Initial UO hue. 0 renders the sprite unhued.</param>
    /// <exception cref="ArgumentException">Thrown in debug builds when the graphic has no art.</exception>
    public HuedTexture(uint graphic, ushort hue)
    {
        _graphic = graphic;

        SpriteInfo artInfo = Client.Game.UO.Arts.GetArt(graphic);

        if (artInfo.Texture == null)
        {
            // Throw in debug, warn and return empty in release.
#if DEBUG
            throw new ArgumentException($@"Could not find texture for graphic '{graphic}'", nameof(graphic));
#else
            Utility.Logging.Log.Warn($"Could not find texture for graphic '{graphic}'");
            return;
#endif
        }

        // artInfo.UV is the sub-rectangle within the shared atlas texture.
        // Passing just the Texture2D would render the entire atlas page;
        // supplying artInfo.UV scopes it to only this sprite.

        // That said, the actual relevant bounds may be smaller than the sprite suggests, so another step is required here.
        Rectangle actualUv = Client.Game.UO.Arts.GetRealArtBounds(graphic);

        Size = new Point(actualUv.Width, actualUv.Height);
        _atlasRegion = new TextureRegion(new TextureRegion(artInfo.Texture, artInfo.UV), actualUv);

        SetColorByHue(hue);
    }

    #endregion

    #region Public methods

    /// <summary>
    ///     Switches which hue variant is drawn, baking it on first use.
    /// </summary>
    /// <param name="hue">UO hue to apply. 0 restores the unhued atlas sprite.</param>
    /// <param name="alpha">Opacity multiplier, 0-1.</param>
    public void SetColorByHue(ushort hue, float alpha = 1f)
    {
        // White, so the draw-time multiply leaves the baked colors alone and only alpha takes effect.
        RenderColor = new Color(255, 255, 255, (byte)(MathHelper.Clamp(alpha, 0f, 1f) * 255f));

        if (_atlasRegion == null)
            return;

        _region = hue == 0
            ? _atlasRegion
            : BakedRegion(_graphic, hue);
    }

    /// <inheritdoc />
    public void Draw(RenderContext context, Rectangle dest, Color color) => _region?.Draw(context, dest, RenderColor);

    #endregion

    #region Private methods

    /// <summary>Fetches the baked variant for a hue, falling back to the atlas sprite if the bake yields nothing.</summary>
    private TextureRegion? BakedRegion(uint graphic, ushort hue)
    {
        // Partial hue is a per-graphic property of the tile data, which the renderer cannot read for itself.
        // Art indices and tile data indices are separate ranges, so a graphic with art can still fall outside
        // tile data; treat that as "hue everything", which is what the shader does without the partial flag.
        StaticTiles[] staticData = Client.Game.UO.FileManager.TileData.StaticData;
        bool partialHue = graphic < staticData.Length && staticData[graphic].IsPartialHue;

        // A null bake is memoized deliberately: it means this variant cannot be produced at all, so retrying
        // it on every hue switch would burn the same work for the same nothing.
        Texture2D? baked = _bakedArt.GetOrAdd(new HuedArtKey(graphic, hue, partialHue), static key => Bake(key));

        return baked == null ? _atlasRegion : new TextureRegion(baked);
    }

    /// <summary>Applies the hue on the CPU and uploads the result as a standalone texture.</summary>
    private static Texture2D? Bake(HuedArtKey key)
    {
        uint[] pixels = Client.Game.UO.Arts.GetHuedArtPixels(key.Graphic, key.Hue, key.PartialHue, out Rectangle bounds);

        if (pixels.Length == 0)
            return null;

        Texture2D texture = new(Client.Game.GraphicsDevice, bounds.Width, bounds.Height, false, SurfaceFormat.Color);
        texture.SetData(pixels);

        return texture;
    }

    #endregion
}
