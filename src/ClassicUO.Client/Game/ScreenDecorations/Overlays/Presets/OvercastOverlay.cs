using System.Collections.Generic;
using ClassicUO.Game.ScreenDecorations.Overlays.Presets.Layers;
using ClassicUO.Renderer.Effects;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.ScreenDecorations.Overlays.Presets;

/// <summary>
///     The sky closing over: an even drop in light, slow cloud masses dragging their shadows across
///     it, and a pale haze pooling toward the bottom of the screen.
///     <para>
///     The one shipped look that covers the whole screen rather than framing it. Everything else here
///     is something happening to the player, so it belongs at the periphery where the eye tolerates
///     it; weather is something happening to the world, and a vignette-shaped overcast reads as the
///     player going blind at the edges instead.
///     </para>
///     <para>
///     Constraints that keep this reading as weather rather than as a colour filter:
///     </para>
///     <list type="bullet">
///     <item>The gloom carries the light level and nothing else - it is nearly flat
///     (<see cref="OverlayNoise.FlatFloor"/> high) because a textured base layer reads as dirt on the
///     lens. All the visible structure belongs to the shadow pass above it.</item>
///     <item>The shadow pass and the haze travel at different screen speeds and different
///     anisotropies. Locking them together, as fluid layers must be, would collapse the two into one
///     substance and lose the depth entirely.</item>
///     <item>Substantial <see cref="OverlayNoise.WarpStrength"/> on the shadow pass. Cloud is gas: it
///     has to churn as it travels, or it reads as a texture being panned.</item>
///     <item>Zero <see cref="OverlayNoise.RidgeAmount"/> and fBm channels only. Ridged and Worley
///     both draw outlines, which on cloud produce a visible cell lattice.</item>
///     <item>No pulse anywhere. Daylight under cloud varies over tens of seconds, and anything fast
///     enough to notice reads as a flicker fault.</item>
///     </list>
/// </summary>
public sealed class OvercastOverlay : ScreenOverlayPreset
{
    #region Private members

    /// <summary>
    /// Weather arrives and clears over far longer than an injury does. At the shared 0.6s onset the
    /// sky snaps dark, which reads as a lighting bug rather than as cloud coming over.
    /// </summary>
    private const float FADE_IN_SECONDS = 1.5f;

    private const float FADE_OUT_SECONDS = 4.5f;

    /// <summary>
    /// Screen speed of the cloud shadows, in screen widths per second. Slow enough that a stationary
    /// player sees the light change rather than sees something move across the screen.
    /// </summary>
    private const float WIND_SCREEN_SPEED = 0.011f;

    /// <summary>Haze sits lower and near the ground, so it drags behind the cloud deck.</summary>
    private const float HAZE_SPEED_SCALE = 0.45f;

    /// <summary>
    /// Wind direction in screen space, mostly sideways with a slight downward component so the deck
    /// reads as passing overhead rather than sliding along a wall.
    /// </summary>
    private static readonly Vector2 _windDirection = new(1f, 0.22f);

    /// <summary>
    /// Relative weights against the caller's <see cref="Opacity" />. The base gloom is the light
    /// level; the shadows are what makes it weather.
    /// </summary>
    private const float GLOOM_OPACITY = 0.34f;

    private const float SHADOW_OPACITY = 0.40f;
    private const float HAZE_OPACITY = 0.17f;

    /// <summary>
    /// How much darker the shadow pass is than the base gloom. Scaled from the caller's colour
    /// rather than exposed separately, so retinting the overcast keeps the two the same weather.
    /// </summary>
    private const float SHADOW_DARKEN = 0.62f;

    /// <summary>
    /// Height of the haze lobe: the fraction of the field pushed toward the bottom of the screen.
    /// Well short of 1, which would leave the top of the screen visibly untouched by it.
    /// </summary>
    private const float HAZE_GROUND_BIAS = 0.55f;

    private const float HAZE_LOBE_POWER = 1.6f;

    /// <summary>
    /// The shader's own feather floor. A full-screen mask has no boundary to fade behind, so this is
    /// the smallest value that survives <see cref="OverlayParams.Clamp" /> untouched; it costs a soft
    /// spot a few pixels across at the exact centre of the screen and nothing else.
    /// </summary>
    private const float FULL_SCREEN_FEATHER = 0.01f;

    #endregion

    #region Ctor

    public OvercastOverlay()
    {
        FadeInSeconds = FADE_IN_SECONDS;
        FadeOutSeconds = FADE_OUT_SECONDS;
    }

    #endregion

    #region Public accessors

    public float Intensity { get; set; } = 1.0f;

    /// <summary>
    /// Colour of the overcast light. Cold and desaturated: warm greys read as dusk, and anything
    /// saturated reads as a status effect rather than as weather.
    /// </summary>
    public Color Hue { get; set; } = new(54, 60, 72);

    /// <summary>Colour of the low haze, added over everything else.</summary>
    public Color HazeHue { get; set; } = new(188, 196, 208);

    public float Opacity { get; set; } = 1.0f;

    #endregion

    #region Protected methods

    protected override void Bake(List<OverlayLayer> layers)
    {
        layers.Add(BakeGloom());
        layers.Add(BakeCloudShadow());
        layers.Add(BakeHaze());
    }

    #endregion

    #region Private methods

    /// <summary>
    /// The light level: an almost flat wash over the entire screen, with just enough slow variation
    /// that it does not read as a solid quad drawn over the frame.
    /// </summary>
    /// <returns>The layer.</returns>
    private OverlayLayer BakeGloom() =>
        new()
        {
            Blend = OverlayBlend.Alpha,
            Params = new OverlayParams
            {
                Shape = FullScreen(),
                Noise = new OverlayNoise
                {
                    // Coarse and near-isotropic: this field is only meant to keep the wash from
                    // being perfectly uniform, so any structure fine enough to resolve is wrong.
                    BaseScale = new Vector2(1.1f, 1.1f),
                    BaseScroll = Drift(new Vector2(1.1f, 1.1f), 1f),
                    BaseChannel = NoiseChannel.Red,
                    DetailScale = new Vector2(2.2f, 2.2f),
                    DetailScroll = Drift(new Vector2(2.2f, 2.2f), 1f),
                    DetailChannel = NoiseChannel.Green,
                    WarpStrength = 0.30f,
                    RidgeAmount = 0.00f,
                    // Low threshold and wide softness: almost everything survives, and what does not
                    // fades out over a long band rather than cutting a visible shape.
                    Threshold = 0.30f,
                    Softness = 0.45f,
                    // The only layer here allowed a floor. A base layer that goes to zero anywhere
                    // punches clear holes through to the unlit frame, which reads as broken.
                    FlatFloor = 0.78f
                },
                Appearance = Appearance(Hue, GLOOM_OPACITY)
            }
        };

    /// <summary>
    /// The cloud deck's shadows: discrete darker masses crossing the gloom. This pass is what makes
    /// the effect weather instead of a dimmer switch, so it carries all the texture and all the
    /// motion.
    /// </summary>
    /// <returns>The layer.</returns>
    private OverlayLayer BakeCloudShadow() =>
        new()
        {
            Blend = OverlayBlend.Alpha,
            Params = new OverlayParams
            {
                Shape = FullScreen(),
                Noise = new OverlayNoise
                {
                    // Mild horizontal stretch - a wind-driven deck elongates along its travel - but
                    // nowhere near streak territory, which would read as rain rather than cloud.
                    BaseScale = new Vector2(1.7f, 1.0f),
                    BaseScroll = Drift(new Vector2(1.7f, 1.0f), 1f),
                    BaseChannel = NoiseChannel.Red,
                    DetailScale = new Vector2(3.5f, 2.1f),
                    DetailScroll = Drift(new Vector2(3.5f, 2.1f), 1f),
                    DetailChannel = NoiseChannel.Green,
                    // High: the gas-vs-fluid dial, and cloud is firmly gas. Without it the masses
                    // hold their shape as they cross, which reads as a panned texture.
                    WarpStrength = 0.55f,
                    RidgeAmount = 0.00f,
                    Threshold = 0.50f,
                    // Cloud has no edge. A narrow band here gives the masses a defined boundary and
                    // turns them into floating blobs.
                    Softness = 0.30f,
                    FlatFloor = 0.00f
                },
                Appearance = Appearance(Darken(Hue, SHADOW_DARKEN), SHADOW_OPACITY)
            }
        };

    /// <summary>
    /// Pale haze pooling toward the bottom of the screen - the fog term, and the only pass that
    /// lightens rather than darkens. Weighted downward because held-up air is clear and the murk
    /// collects at ground level; an even pale wash just washes out the contrast.
    /// </summary>
    /// <returns>The layer.</returns>
    private OverlayLayer BakeHaze()
    {
        OverlayShape shape = FullScreen();
        // Screen uv runs downward, so this lobe points at the bottom of the frame.
        shape.FocusDir = new Vector2(0f, 1f);
        shape.FocusPower = HAZE_LOBE_POWER;
        shape.FocusAmount = HAZE_GROUND_BIAS;

        return new OverlayLayer
        {
            Blend = OverlayBlend.Alpha,
            Params = new OverlayParams
            {
                Shape = shape,
                Noise = new OverlayNoise
                {
                    // Strongly flattened: haze lies in banks, and the horizontal stretch is what
                    // separates it from the cloud above rather than looking like more of it.
                    BaseScale = new Vector2(2.6f, 0.8f),
                    BaseScroll = Drift(new Vector2(2.6f, 0.8f), HAZE_SPEED_SCALE),
                    BaseChannel = NoiseChannel.Green,
                    DetailScale = new Vector2(5.0f, 1.5f),
                    DetailScroll = Drift(new Vector2(5.0f, 1.5f), HAZE_SPEED_SCALE),
                    DetailChannel = NoiseChannel.Red,
                    // Lower than the cloud: haze drifts as a body more than it churns.
                    WarpStrength = 0.22f,
                    RidgeAmount = 0.00f,
                    Threshold = 0.44f,
                    Softness = 0.34f,
                    // A little, so the banks sit in a general murk rather than on clear air.
                    FlatFloor = 0.22f
                },
                Appearance = Appearance(HazeHue, HAZE_OPACITY)
            }
        };
    }

    /// <summary>
    /// A mask covering the entire frame.
    /// <para>
    /// Border-shaped at full reach rather than radial: the radial distance is normalised to screen
    /// width, so a radial mask at full reach still falls away well before the top and bottom edges
    /// of a widescreen display and leaves the sky lighter than the middle of the screen. The feather
    /// is only there to satisfy the shader's minimum, and costs a soft spot a few pixels wide at the
    /// exact centre.
    /// </para>
    /// </summary>
    /// <returns>The shape.</returns>
    private static OverlayShape FullScreen() =>
        new()
        {
            Center = new Vector2(0.5f, 0.5f),
            Reach = LayerReach.Max,
            Feather = FULL_SCREEN_FEATHER,
            EdgeBlend = 1.00f,
            CornerBias = 0.00f,
            // No jitter: there is no boundary on screen to break up.
            FocusDir = new Vector2(0f, -1f),
            FocusPower = 1f,
            FocusAmount = 0f
        };

    /// <summary>
    /// Shared appearance block. Every layer here is steady - see the no-pulse constraint in the
    /// type's remarks - so only colour and weight vary.
    /// </summary>
    /// <param name="tint">The layer's colour.</param>
    /// <param name="weight">Its share of <see cref="Opacity" />.</param>
    /// <returns>The appearance parameters.</returns>
    private OverlayAppearance Appearance(Color tint, float weight) =>
        new()
        {
            Tint = tint,
            Opacity = Opacity * weight,
            Intensity = Intensity,
            PulseFreq = 0.00f,
            PulseAmp = 0.00f
        };

    /// <summary>
    /// Texture-space scroll giving a field of the given scale a constant on-screen velocity. On-screen
    /// speed is <c>Scroll / Scale</c>, so deriving it this way keeps the layers travelling together
    /// however their frequencies are retuned.
    /// </summary>
    /// <param name="scale">The field's frequency.</param>
    /// <param name="speedScale">Multiplier on the shared wind speed.</param>
    /// <returns>The scroll velocity.</returns>
    private static Vector2 Drift(Vector2 scale, float speedScale) =>
        scale * _windDirection * (WIND_SCREEN_SPEED * speedScale);

    private static Color Darken(Color color, float amount) =>
        new((int)(color.R * amount), (int)(color.G * amount), (int)(color.B * amount));

    #endregion
}
