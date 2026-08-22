#nullable enable

using System;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using ClassicUO.Utility.Collections;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra.Graphics2D;
using Myra.Graphics2D.TextureAtlases;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.ArtTexture;

/// <summary>
///     Renders a UO art graphic, optionally hued.
///     <para>
///         <b>
///             A hued instance holds a share of a GPU <see cref="Texture2D" />. Give it back via
///             <see cref="SetPlaced" /> when the owner leaves the screen, <see cref="Dispose" /> when done.
///         </b>
///     </para>
/// </summary>
internal class HuedTexture : IImage, IDisposable
{
    #region Private members

    /// <summary>
    ///     Shared so several widgets showing the same hued graphic pay for one texture between them. Leased, so a
    ///     bake outlives its viewers by nothing and can never be disposed out from under one. Render thread only.
    /// </summary>
    private static readonly LeaseCache<HuedArtKey, Texture2D?> _bakedArt = new();

    private readonly uint _graphic;

    /// <summary>The unhued sprite within the shared atlas. Never owned here, never disposed.</summary>
    private readonly TextureRegion? _atlasRegion;

    /// <summary>Whatever is currently being drawn: the atlas region, or a baked hue variant.</summary>
    private TextureRegion? _region;

    /// <summary>Requested hue. Outlives the lease, so an unplaced instance can rebuild the same appearance.</summary>
    private ushort _hue;

    /// <summary>Key of the lease currently held, or null when drawing straight from the atlas.</summary>
    private HuedArtKey? _leasedKey;

    /// <summary>Whether the owning widget is on a desktop. Nothing off-screen is worth holding a bake for.</summary>
    private bool _isPlaced;

    /// <summary>Latches on disposal so a callback outliving the widget cannot take a fresh lease.</summary>
    private bool _isDisposed;

    #endregion

    #region Public accessors

    /// <summary>This instance's contribution to the draw tint. Carries opacity only — the hue lives in the texels.</summary>
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
    ///     Switches which hue variant is drawn. The bake happens on first display, not here.
    /// </summary>
    /// <param name="hue">UO hue to apply. 0 restores the unhued atlas sprite.</param>
    /// <param name="alpha">Opacity multiplier, 0-1.</param>
    public void SetColorByHue(ushort hue, float alpha = 1f)
    {
        // White, so the draw-time multiply leaves the baked colors alone and only alpha takes effect.
        RenderColor = new Color(255, 255, 255, (byte)(MathHelper.Clamp(alpha, 0f, 1f) * 255f));

        _hue = hue;
        RefreshRegion();
    }

    /// <summary>
    ///     Takes or gives up the baked texture to match the owner's visibility. The hue survives, so an unplaced
    ///     instance costs nothing and comes back looking the same.
    /// </summary>
    /// <param name="isPlaced">Whether the owner is now on a desktop.</param>
    public void SetPlaced(bool isPlaced)
    {
        _isPlaced = isPlaced;
        RefreshRegion();
    }

    /// <inheritdoc />
    /// <remarks>
    ///     <paramref name="color" /> is the owning widget's tint, modulated over our own. Ours is white plus an
    ///     alpha, so it contributes no color of its own and the baked hue survives an untinted widget intact.
    /// </remarks>
    public void Draw(RenderContext context, Rectangle dest, Color color)
    {
        // Widgets are white unless someone tints them, and white modulates to nothing.
        Color tint = color == Color.White
            ? RenderColor
            : new Color(color.ToVector4() * RenderColor.ToVector4());

        _region?.Draw(context, dest, tint);
    }

    /// <summary>
    ///     Gives up the baked texture for good. Idempotent; drawing still works afterwards, just unhued.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        RefreshRegion();
        GC.SuppressFinalize(this);
    }

    #endregion

    #region Private methods

    /// <summary>
    ///     Repoints <see cref="_region" /> at whatever the current hue and placement call for, moving the lease
    ///     to match. Idempotent.
    /// </summary>
    private void RefreshRegion()
    {
        if (_atlasRegion == null)
            return;

        HuedArtKey? previous = _leasedKey;

        if (_hue == 0 || !_isPlaced || _isDisposed)
        {
            _region = _atlasRegion;
            _leasedKey = null;
        }
        else
        {
            HuedArtKey key = new(_graphic, _hue, IsPartialHue(_graphic));

            // Leased before the old one is dropped below: re-applying the hue already shown then goes 1 -> 2 -> 1
            // rather than disposing the texture and handing the same dead one back. Null bakes are memoized too,
            // since a variant that cannot be produced will not start being producible on the next hue switch.
            Texture2D? baked = _bakedArt.Lease(key, static k => Bake(k));

            _region = baked == null ? _atlasRegion : new TextureRegion(baked);
            _leasedKey = key;
        }

        // After _region is repointed, so it is never left aimed at a texture that just hit zero leases.
        if (previous.HasValue)
            _bakedArt.Release(previous.Value);
    }

    /// <summary>
    ///     Reads the partial-hue flag from tile data, which the renderer cannot reach for itself.
    /// </summary>
    /// <remarks>
    ///     Art indices and tile data indices are separate ranges, so a graphic with art can still fall outside
    ///     tile data; treat that as "hue everything", which is what the shader does without the partial flag.
    /// </remarks>
    private static bool IsPartialHue(uint graphic)
    {
        StaticTiles[] staticData = Client.Game.UO.FileManager.TileData.StaticData;
        return graphic < staticData.Length && staticData[graphic].IsPartialHue;
    }

    /// <summary>Applies the hue on the CPU and uploads the result as a standalone texture.</summary>
    /// <remarks>
    ///     Baked rather than shaded because Myra takes no custom effect, and its flat tint would dye a
    ///     partial-hue sprite whole — a dye tub's wood along with its liquid.
    /// </remarks>
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
