#nullable enable

using System;
using ClassicUO.Assets;
using FontStashSharp;
using FontStashSharp.RichText;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

/// <summary>
/// A square button whose whole content is one glyph. The standard one - a toolbar icon, a reset
/// mark, a close cross - so that every icon in the UI is the same size and sits on the same pixel.
/// <para>
/// The glyph is centred from the font's own metrics rather than from a nudge chosen by eye.
/// <see cref="SpriteFontBase.TextBounds" /> reports where the ink actually lands, which is not where
/// the line box is: a symbol font leaves far more room above a glyph than below it, so anything
/// drawn at the top of its line box sits high. Measuring means a new glyph needs no tuning, and the
/// several places that draw one cannot drift apart.
/// </para>
/// </summary>
public class IconButton : BasicButton
{
    #region Public accessors

    /// <summary>The glyph shown. Re-centres on assignment.</summary>
    public string Glyph
    {
        get => field;
        set
        {
            if (field == value)
                return;

            field = value;
            Refresh();
        }
    } = string.Empty;

    /// <summary>Point size the glyph is drawn at. Re-centres on assignment.</summary>
    public int GlyphSize
    {
        get => field;
        set
        {
            if (field == value)
                return;

            field = value;
            Refresh();
        }
    } = DEFAULT_GLYPH_SIZE;

    /// <summary>
    /// Extra pixels applied on top of the measured centring, for a glyph the metrics get wrong -
    /// FontStashSharp reports no ink at all for a whitespace-like code point, and a few symbols
    /// carry bearings that read oddly beside neighbouring icons. Zero is the normal case.
    /// </summary>
    public Point Nudge
    {
        get => field;
        set
        {
            if (field == value)
                return;

            field = value;
            Refresh();
        }
    }

    #endregion

    #region Private members

    /// <summary>Matches the property grid's expander and reset marks, which is what most glyphs sit
    /// beside.</summary>
    private const int DEFAULT_GLYPH_SIZE = 24;

    /// <summary>
    /// Hover and press fills for a borderless button - just enough to read as live. Applied via
    /// <c>Color * factor</c>, which premultiplies: a literal <c>new Color(255, 255, 255, 30)</c> has
    /// colour channels above its alpha, which the premultiplied blend draws as solid white.
    /// </summary>
    private const float FLAT_HOVER_STRENGTH = 0.12f;
    private const float FLAT_PRESSED_STRENGTH = 0.22f;

    private readonly Label _label;

    /// <summary>
    /// Holds off re-centring while a batch of glyph properties is assigned - see
    /// <see cref="SetGlyphAndSize" />.
    /// </summary>
    private bool _deferRefresh;

    #endregion

    #region Ctor

    /// <summary>
    /// Builds an icon button.
    /// </summary>
    /// <param name="glyph">The glyph to draw. Taken from Noto Sans Symbols 2, which is where the
    /// arrows, ticks and crosses the UI uses live - the body font has no glyph for any of them and
    /// renders nothing at all.</param>
    /// <param name="onClick">Invoked on click.</param>
    /// <param name="tooltip">Optional tooltip. Worth supplying: a bare glyph has nothing else
    /// naming it.</param>
    /// <param name="size">Button width and height.</param>
    /// <param name="glyphSize">Point size the glyph is drawn at.</param>
    public IconButton(
        string glyph,
        Action onClick,
        string? tooltip = null,
        int size = StyleConstantsDefaults.TOOLBAR_BUTTON_SIZE,
        int glyphSize = DEFAULT_GLYPH_SIZE
    ) : base(onClick)
    {
        _label = new Label
        {
            Wrap = false,
            SingleLine = true,
            TextAlign = TextHorizontalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Padding = new Thickness(0),
            Margin = new Thickness(0)
        };

        Width = size;
        Height = size;
        Content = _label;
        Tooltip = tooltip;
        Padding = new Thickness(0);
        Margin = new Thickness(0);
        VerticalAlignment = VerticalAlignment.Center;

        SetGlyphAndSize(glyph, glyphSize);
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Builds a chrome-less icon button: the bare glyph, no border or resting fill. For an icon
    /// that sits inside another control - a clear chip in a text field, an inline dismiss - where
    /// a border would read as a second, nested control rather than as an affordance on the first.
    /// </summary>
    /// <param name="glyph">The glyph to draw, from the symbol font.</param>
    /// <param name="onClick">Invoked on click.</param>
    /// <param name="tooltip">Optional tooltip. Worth supplying: a bare glyph has nothing else naming it.</param>
    /// <param name="size">Button width and height.</param>
    /// <param name="glyphSize">Point size the glyph is drawn at.</param>
    /// <returns>The button.</returns>
    public static IconButton Flat(
        string glyph,
        Action onClick,
        string? tooltip = null,
        int size = StyleConstantsDefaults.TOOLBAR_BUTTON_SIZE,
        int glyphSize = DEFAULT_GLYPH_SIZE
    ) => new IconButton(glyph, onClick, tooltip, size, glyphSize).MakeBorderless();

    /// <summary>
    /// Strips the button chrome from an existing button, leaving the bare glyph. Prefer
    /// <see cref="Flat"/> when building one; this is for a button handed to you already made. For an icon that sits inside another
    /// control - a clear chip in a text field - where a border would read as a second, nested
    /// control rather than as an affordance on the first.
    /// <para>
    /// A faint hover and press fill are kept: with nothing at all the glyph gives no sign it is
    /// interactive. Input is unaffected either way, since only <see cref="Container"/> treats a
    /// null background as a reason to let clicks through.
    /// </para>
    /// </summary>
    /// <returns>This button, for chaining.</returns>
    public IconButton MakeBorderless()
    {
        Background = null;
        Border = null;
        BorderThickness = new Thickness(0);
        FocusedBackground = null;

        OverBackground = new SolidBrush(Color.White * FLAT_HOVER_STRENGTH);
        PressedBackground = new SolidBrush(Color.Black * FLAT_PRESSED_STRENGTH);

        return this;
    }

    /// <summary>
    /// Swaps the glyph and its size together, for a button whose icon changes with its state - a
    /// minimize/restore toggle, say - re-centring once for the pair.
    /// </summary>
    /// <param name="glyph">The new glyph.</param>
    /// <param name="glyphSize">The new point size.</param>
    public void SetIcon(string glyph, int glyphSize)
    {
        if (Glyph == glyph && GlyphSize == glyphSize)
            return;

        SetGlyphAndSize(glyph, glyphSize);
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Assigns both glyph properties as one change. Each setter re-centres on its own, so the pair
    /// would otherwise measure the glyph twice - once against a size about to be replaced.
    /// </summary>
    /// <param name="glyph">The glyph to draw.</param>
    /// <param name="glyphSize">Point size to draw it at.</param>
    private void SetGlyphAndSize(string glyph, int glyphSize)
    {
        _deferRefresh = true;

        try
        {
            Glyph = glyph;
            GlyphSize = glyphSize;
        }
        finally
        {
            _deferRefresh = false;
        }

        Refresh();
    }

    /// <summary>
    /// Re-measures the glyph and seats it in the middle of the button.
    /// <para>
    /// The label is sized to the button and draws from the top of its own bounds, so the offset is
    /// the gap that would put the ink box's centre on the button's centre. Both axes, because a
    /// symbol's horizontal bearings are no more reliable than its vertical ones.
    /// </para>
    /// </summary>
    private void Refresh()
    {
        if (_deferRefresh)
            return;

        SpriteFontBase font = TrueTypeLoader.Instance.GetFont(EmbeddedFontNames.NOTO_SANS_2_SYMBOLS, GlyphSize);

        _label.Font = font;
        _label.Text = Glyph;
        _label.Width = Width;
        _label.Height = Height;

        Point offset = CenteringOffset(font, Glyph, Width ?? 0, Height ?? 0);

        _label.Left = offset.X + Nudge.X;
        _label.Top = offset.Y + Nudge.Y;
    }

    /// <summary>
    /// How far the glyph has to move for its ink to sit in the middle of the button.
    /// </summary>
    /// <param name="font">The font the glyph is drawn with.</param>
    /// <param name="glyph">The glyph.</param>
    /// <param name="width">Button width.</param>
    /// <param name="height">Button height.</param>
    /// <returns>The offset, or zero where the glyph has no ink to measure.</returns>
    private static Point CenteringOffset(SpriteFontBase font, string glyph, int width, int height)
    {
        if (string.IsNullOrEmpty(glyph) || width <= 0 || height <= 0)
            return Point.Zero;

        Bounds ink = font.TextBounds(glyph, Vector2.Zero);

        // A glyph the font has no outline for measures empty. Nothing useful to centre, and the
        // arithmetic below would throw the label clean out of the button.
        if (ink.X2 <= ink.X || ink.Y2 <= ink.Y)
            return Point.Zero;

        float inkCentreY = (ink.Y + ink.Y2) / 2f;

        // Horizontally the label already centres the glyph's advance width, so what is left is the
        // ink's own bearing inside that width - the difference between where the glyph is drawn and
        // where its outline actually sits.
        float advanceWidth = font.MeasureString(glyph).X;
        float inkCentreX = (ink.X + ink.X2) / 2f;

        return new Point(
            (int)MathF.Round(advanceWidth / 2f - inkCentreX),
            (int)MathF.Round(height / 2f - inkCentreY)
        );
    }

    #endregion
}
