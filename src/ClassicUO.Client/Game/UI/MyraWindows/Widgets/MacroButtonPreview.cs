#nullable enable
using System;
using ClassicUO.Assets;
using ClassicUO.Game.Managers;
using ClassicUO.Renderer;
using FontStashSharp.RichText;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

/// <summary>
///     Live preview of a macro's on-screen button, matching how <c>MacroButtonGump</c> draws it: a
///     hue-shaded backing plate, the chosen gump graphic over it, and the macro name unless the macro
///     hides its label.
/// </summary>
/// <remarks>
///     Nothing here watches the macro, so the owner must call <see cref="Refresh" /> after editing it;
///     <see cref="RefreshScale" /> is the cheap path for scale-only edits, which only resize what is
///     already baked. A hued graphic owns a GPU texture for as long as the widget is on a desktop,
///     which leaving gives back, so <see cref="Dispose" /> only matters for retiring a placed one.
/// </remarks>
public sealed class MacroButtonPreview : Panel, IDisposable
{
    #region Public events

    /// <summary>Raised when <see cref="ShowActiveState" /> changes, including from a click on the preview.</summary>
    public event EventHandler<bool>? ShowActiveStateChanged;

    #endregion

    #region Private members

    /// <summary>Plate size used when no graphic is set, matching <c>MacroButtonGump</c>'s own default.</summary>
    private const int PLATE_WIDTH = 88;
    private const int PLATE_HEIGHT = 44;

    /// <summary>Hue shade the plate's gray resolves to — <c>MacroButtonGump</c> fills it with Color(30, 30, 30).</summary>
    private const ushort PLATE_SHADE = 30 >> 3;

    /// <summary>Shade of its hue the label is drawn at; the brightest, as the font renderer does.</summary>
    private const ushort LABEL_SHADE = 31;

    private static readonly Color PlateGray = new(30, 30, 30);

    /// <summary>The macro being mirrored. Read on every refresh, never written to.</summary>
    private readonly Macro _macro;

    /// <summary>The gump graphic, stretched over the plate.</summary>
    private readonly OverlayImage _graphicImage;

    /// <summary>The macro name, centred over the graphic.</summary>
    private readonly OverlayLabel _nameLabel;

    /// <summary>The hue bake currently on display, if any. Owned here; released with the widget or its placement.</summary>
    private Texture2D? _baked;

    /// <summary>Unscaled size the button derives its dimensions from: the graphic's, or the default plate's.</summary>
    private Point _sourceSize = new(PLATE_WIDTH, PLATE_HEIGHT);

    /// <summary>Which of the macro's two appearances is on display.</summary>
    private bool _showActiveState;

    #endregion

    #region Ctor

    /// <summary>
    ///     Builds a preview of the given macro's button.
    /// </summary>
    /// <param name="macro">The macro to mirror. Read on every refresh, never written to.</param>
    public MacroButtonPreview(Macro macro)
    {
        _macro = macro;

        HorizontalAlignment = HorizontalAlignment.Center;
        VerticalAlignment = VerticalAlignment.Center;

        _graphicImage = new OverlayImage
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        _nameLabel = new OverlayLabel(macro.LabelFor(false))
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center,
            TextAlign = TextHorizontalAlignment.Center
        };

        Widgets.Add(_graphicImage);
        Widgets.Add(_nameLabel);

        // The two states are the whole point of the preview, and clicking the button to see its other
        // face is the one gesture that needs no label.
        TouchUp += (_, _) => ShowActiveState = !ShowActiveState;

        Refresh();
    }

    #endregion

    #region Public accessors

    /// <summary>
    ///     Whether to show the button as it looks while the macro runs. Changing it re-bakes and raises
    ///     <see cref="ShowActiveStateChanged" />.
    /// </summary>
    public bool ShowActiveState
    {
        get => _showActiveState;
        set
        {
            if (_showActiveState == value)
                return;

            _showActiveState = value;
            Refresh();
            ShowActiveStateChanged?.Invoke(this, value);
        }
    }

    #endregion

    #region Public methods

    /// <summary>Re-reads every macro property, re-baking the graphic for the state being shown.</summary>
    public void Refresh()
    {
        RefreshGraphic();
        RefreshScale();

        ushort hue = _macro.HueFor(_showActiveState);
        Background = new SolidBrush(hue == 0 ? PlateGray : HueShade(hue, PLATE_SHADE));

        // Without a graphic the live button outlines the plate rather than filling it; mirror that.
        bool hasGraphic = _macro.GraphicFor(_showActiveState).HasValue;
        Border = hasGraphic ? null : new SolidBrush(Color.Gray);
        BorderThickness = hasGraphic ? new Thickness(0) : new Thickness(1);

        // Empty is how the macro says "no label", matching what the live button draws.
        string label = _macro.LabelFor(_showActiveState);

        _nameLabel.Text = label;
        _nameLabel.Visible = !string.IsNullOrEmpty(label);
        _nameLabel.TextColor = LabelColor();
    }

    /// <summary>Resizes to the macro's current scale without re-baking. Safe to call per slider tick.</summary>
    public void RefreshScale()
    {
        float factor = _macro.Scale / 100f;

        Width = Math.Max(1, (int)(_sourceSize.X * factor));
        Height = Math.Max(1, (int)(_sourceSize.Y * factor));
    }

    /// <summary>Gives up the hue bake. Idempotent; the widget still draws the unhued graphic after.</summary>
    public void Dispose()
    {
        ReleaseBake();
        GC.SuppressFinalize(this);
    }

    #endregion

    #region Protected methods

    /// <inheritdoc />
    /// <remarks>Placement is the only lifetime signal Myra gives a widget, and the bake needs one.</remarks>
    protected override void OnPlacedChanged()
    {
        base.OnPlacedChanged();

        RefreshGraphic();
    }

    #endregion

    #region Private methods

    /// <summary>
    ///     Repoints the graphic at whatever the macro's current graphic and hue call for, baking a hued
    ///     texture when one is needed and this widget is placed.
    /// </summary>
    private void RefreshGraphic()
    {
        ReleaseBake();

        // Always the resting graphic: that is what fixes the live button's size, which it keeps while
        // running so switching state cannot move it or reflow its anchor group.
        _sourceSize = RestingSize();

        if (_macro.GraphicFor(_showActiveState) is not { } graphic)
        {
            _graphicImage.Renderable = null;

            return;
        }

        ref readonly SpriteInfo sprite = ref Client.Game.UO.Gumps.GetGump(graphic);

        if (sprite.Texture == null)
        {
            _graphicImage.Renderable = null;

            return;
        }

        ushort hue = _macro.HueFor(_showActiveState);

        // The atlas region is already the right pixels for an unhued graphic, and nothing off-screen
        // is worth holding a bake for.
        if (hue == 0 || !IsPlaced)
        {
            _graphicImage.Renderable = new TextureRegion(sprite.Texture, sprite.UV);

            return;
        }

        uint[] pixels = Client.Game.UO.Gumps.GetHuedGumpPixels(
            graphic,
            hue,
            IsPartialHue(graphic),
            out Rectangle bounds
        );

        if (pixels.Length == 0)
        {
            _graphicImage.Renderable = new TextureRegion(sprite.Texture, sprite.UV);

            return;
        }

        _baked = new Texture2D(Client.Game.GraphicsDevice, bounds.Width, bounds.Height, false, SurfaceFormat.Color);
        _baked.SetData(pixels);
        _graphicImage.Renderable = new TextureRegion(_baked);
    }

    /// <summary>Unscaled size the resting graphic gives the button, or the default plate's.</summary>
    private Point RestingSize()
    {
        if (_macro.Graphic is not { } graphic)
            return new Point(PLATE_WIDTH, PLATE_HEIGHT);

        ref readonly SpriteInfo sprite = ref Client.Game.UO.Gumps.GetGump(graphic);

        return sprite.Texture == null ? new Point(PLATE_WIDTH, PLATE_HEIGHT) : new Point(sprite.UV.Width, sprite.UV.Height);
    }

    /// <summary>Drops the hue bake and the image pointing at it. Idempotent.</summary>
    private void ReleaseBake()
    {
        if (_baked == null)
            return;

        // Repointed first so the image is never left aimed at a disposed texture.
        _graphicImage.Renderable = null;
        _baked.Dispose();
        _baked = null;
    }

    /// <summary>
    ///     Reads the partial-hue flag from tile data.
    /// </summary>
    /// <remarks>
    ///     Gump indices and tile data indices are separate ranges, so a gump can fall outside tile data
    ///     entirely; treat that as "hue everything", which is what the shader does without the flag.
    /// </remarks>
    private static bool IsPartialHue(ushort graphic)
    {
        StaticTiles[] staticData = Client.Game.UO.FileManager.TileData.StaticData;

        return graphic < staticData.Length && staticData[graphic].IsPartialHue;
    }

    /// <summary>
    ///     The label's colour for the state being shown, hue and opacity together.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Hue 0 is the font's own colour rather than a ramp lookup, which at 0 would resolve to
    ///         black - the live button draws unhued text there, not black text.
    ///     </para>
    ///     <para>
    ///         Premultiplied, because Myra draws through <c>BlendState.AlphaBlend</c>, which in FNA is
    ///         <c>src One, dst InverseSourceAlpha</c>. Setting the alpha without scaling the channels to
    ///         match leaves the blend at <c>src + dst</c> as alpha falls, so the text brightens into the
    ///         plate instead of fading - the opposite of what the setting says.
    ///     </para>
    /// </remarks>
    /// <returns>The colour, with the macro's opacity premultiplied into it.</returns>
    private Color LabelColor()
    {
        ushort hue = _macro.LabelHueFor(_showActiveState);
        Color color = hue == 0 ? Color.White : HueShade(hue, LABEL_SHADE);

        float opacity = Math.Clamp(_macro.LabelOpacityFor(_showActiveState) / (float)Macro.FULL_OPACITY, 0f, 1f);

        return Color.FromNonPremultiplied(color.R, color.G, color.B, (int)(255 * opacity));
    }

    /// <summary>Resolves one shade of a UO hue to a drawable color.</summary>
    /// <param name="hue">UO hue, 1-based as stored in item data.</param>
    /// <param name="shade">Index into the hue's 32-shade ramp, darkest first.</param>
    private static Color HueShade(ushort hue, ushort shade) =>
        new() { PackedValue = Client.Game.UO.FileManager.Hues.GetHueColorRgba8888(shade, hue) };

    #endregion

    #region Nested types

    /// <summary>The plate's graphic, which must not swallow the click that flips the previewed state.</summary>
    private sealed class OverlayImage : Image
    {
        /// <inheritdoc />
        public override bool InputFallsThrough(Point localPos) => true;
    }

    /// <summary>The macro name, which must not swallow the click that flips the previewed state.</summary>
    private sealed class OverlayLabel(string text) : MyraLabel(text, TextStyle.P)
    {
        /// <inheritdoc />
        public override bool InputFallsThrough(Point localPos) => true;
    }

    #endregion
}
