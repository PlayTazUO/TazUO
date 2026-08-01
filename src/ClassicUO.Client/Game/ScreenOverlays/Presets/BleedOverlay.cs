using System.Collections.Generic;
using ClassicUO.Renderer.Effects;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.ScreenOverlays.Presets;

/// <summary>
///     Dark fluid clinging to the screen border: a broad wet film, distinct rivulets running over
///     it, and a dim specular riding the rivulets. Weighted heavily toward the two dark basal
///     layers - the specular exists to define a surface, not to be seen.
///     <para>
///     Constraints that keep this reading as fluid rather than as gas, cells, or cloud. Every one of
///     them was learned by violating it:
///     </para>
///     <list type="bullet">
///     <item>Near-zero <see cref="OverlayNoise.WarpStrength"/>. The gas-vs-fluid dial.</item>
///     <item>Zero <see cref="OverlayNoise.RidgeAmount"/>, and no Blue or Alpha channel. All three
///     draw outlines around cells; they belong to the fracture preset.</item>
///     <item><see cref="OverlayNoise.FlatFloor"/> of exactly 0 on every layer. Any floor makes the
///     mask render its own geometry, which is a soft-edged rectangle.</item>
///     <item>Weight comes from opacity inside the streaks, never from coverage. Lowering thresholds
///     to add body just fills the trim in solid.</item>
///     <item>All basal layers travel at the same screen speed. Two dark layers sliding against each
///     other stop being one substance and read as cloud shadow over terrain.</item>
///     <item>Vertical anisotropy for streaks rather than blobs - but only moderate. Past about 5:1
///     the rivulets get thin enough to read as wisps.</item>
///     <item>Corner weighting via <see cref="OverlayShape.CornerBias"/>, never by blending
///     <see cref="OverlayShape.EdgeBlend"/> toward radial. The radial term is width-normalised, so
///     on a widescreen display it lands almost entirely on the left and right edges.</item>
///     </list>
/// </summary>
public sealed class BleedOverlay : ScreenOverlayPreset
{
    // Screen speed is scroll/scale, not scroll. Every layer derives its scroll from this so the
    // relationship survives a scale change - the film and runs are locked to it exactly, and only
    // the specular is allowed to drift.
    private const float FLOW_SCREEN_SPEED = 0.010f;
    private const float HIGHLIGHT_FLOW_SCALE = 1.2f;

    // Relative weights against the caller's Opacity. The specular is faint because it is additive
    // and therefore the only thing that can lighten the composite.
    private const float RUNS_OPACITY_SCALE = 0.96f;
    private const float HIGHLIGHT_OPACITY_SCALE = 0.14f;

    // Each pass stops slightly shorter than the one below it, so the composite thins out toward the
    // middle of the screen instead of all three ending together.
    private const float RUNS_REACH_SCALE = 0.90f;
    private const float HIGHLIGHT_REACH_SCALE = 0.82f;

    public float Intensity { get; set; } = 1.0f;

    /// <summary>
    /// Blood colour of the rivulet pass. The film tint is scaled from this rather than exposed
    /// separately, so changing it keeps the layers the same substance.
    /// </summary>
    public Color Hue { get; set; } = new(150, 16, 20);

    /// <summary>Specular tint of the highlight pass, added on top of everything else.</summary>
    public Color HighlightHue { get; set; } = new(198, 46, 34);

    public float Opacity { get; set; } = 1.0f;

    /// <summary>How far in from the screen edge the fluid reaches. Larger is thicker.</summary>
    public float Reach { get; set; } = 0.26f;

    protected override void Bake(List<OverlayLayer> layers)
    {
        layers.Add(BakeFilm());
        layers.Add(BakeRuns());
        layers.Add(BakeHighlight());
    }

    /// <summary>
    /// Broad wet wash over the whole trim - the mass the other two sit on. Lowest threshold of the
    /// three, so it is the most continuous, but still fully noise-driven.
    /// </summary>
    private OverlayLayer BakeFilm() =>
        new()
        {
            Blend = OverlayBlend.Alpha,
            Params = new OverlayParams
            {
                Shape = new OverlayShape
                {
                    Center = new Vector2(0.5f, 0.5f),
                    Reach = Reach,
                    Feather = 0.16f,
                    // Pure border. Blending radial in to weight the corners looks correct on paper
                    // but the radial term is width-normalised, so it lands almost entirely on the
                    // left and right edges of a widescreen display; CornerBias does the same job per
                    // axis and stays even all the way round.
                    EdgeBlend = 1.00f,
                    CornerBias = 1.00f,
                    Jitter = Jitter(0.55f, 0.60f, new Vector2(2.2f, 0.5f), NoiseChannel.Green),
                    FocusDir = new Vector2(0f, -1f),
                    FocusPower = 1f,
                    FocusAmount = 0f
                },
                Noise = new OverlayNoise
                {
                    BaseScale = new Vector2(3.0f, 0.8f),
                    BaseScroll = Flow(0.8f, 1f),
                    BaseChannel = NoiseChannel.Red,
                    DetailScale = new Vector2(5.2f, 1.5f),
                    DetailScroll = Flow(1.5f, 1f),
                    DetailChannel = NoiseChannel.Green,
                    WarpStrength = 0.07f,
                    RidgeAmount = 0.00f,
                    Threshold = 0.50f,
                    Softness = 0.10f,
                    FlatFloor = 0.00f
                },
                Appearance = new OverlayAppearance
                {
                    Tint = Scale(Hue, 0.50f, 0.70f, 0.45f),
                    Opacity = Opacity,
                    Intensity = Intensity,
                    // No pulsation - a continuous fluid flow shouldn't flash/breathe.
                    PulseFreq = 0.00f,
                    PulseAmp = 0.00f
                }
            }
        };

    /// <summary>
    /// The rivulets themselves: the most anisotropic and most raggedly terminated pass, at the
    /// caller's full blood colour, locked to the film's flow speed.
    /// </summary>
    private OverlayLayer BakeRuns() =>
        new()
        {
            Blend = OverlayBlend.Alpha,
            Params = new OverlayParams
            {
                Shape = new OverlayShape
                {
                    Center = new Vector2(0.5f, 0.5f),
                    Reach = Reach * RUNS_REACH_SCALE,
                    // Narrow, so a run ends rather than trailing off into haze.
                    Feather = 0.12f,
                    EdgeBlend = 1.00f,
                    CornerBias = 1.00f,
                    // Highest of the three: run lengths vary far more than the film's boundary does.
                    Jitter = Jitter(0.60f, 0.70f, new Vector2(3.0f, 0.5f), NoiseChannel.Red),
                    FocusDir = new Vector2(0f, -1f),
                    FocusPower = 1f,
                    FocusAmount = 0f
                },
                Noise = new OverlayNoise
                {
                    // ~5:1 anisotropy: high U frequency, low V frequency, so features come out as
                    // long narrow rivulets instead of the isotropic blobs that read as gas.
                    BaseScale = new Vector2(4.2f, 0.8f),
                    BaseScroll = Flow(0.8f, 1f),
                    BaseChannel = NoiseChannel.Red,
                    DetailScale = new Vector2(7.0f, 1.5f),
                    DetailScroll = Flow(1.5f, 1f),
                    DetailChannel = NoiseChannel.Green,
                    WarpStrength = 0.06f,
                    RidgeAmount = 0.00f,
                    Threshold = 0.60f,
                    // A wide softness band is a soft-edged cloud. Fluid has a meniscus.
                    Softness = 0.06f,
                    FlatFloor = 0.00f
                },
                Appearance = new OverlayAppearance
                {
                    Tint = Hue,
                    Opacity = Opacity * RUNS_OPACITY_SCALE,
                    Intensity = Intensity,
                    PulseFreq = 0.00f,
                    PulseAmp = 0.00f
                }
            }
        };

    /// <summary>
    /// Shares the rivulet pass's primary noise scale, channel and jitter field, so it rides the same
    /// runs and terminates where they do, but at a much higher threshold so it resolves to thin
    /// slivers of that same field rather than an independent pattern.
    /// </summary>
    private OverlayLayer BakeHighlight() =>
        new()
        {
            Blend = OverlayBlend.Additive,
            Params = new OverlayParams
            {
                Shape = new OverlayShape
                {
                    Center = new Vector2(0.5f, 0.5f),
                    Reach = Reach * HIGHLIGHT_REACH_SCALE,
                    Feather = 0.10f,
                    EdgeBlend = 1.00f,
                    CornerBias = 1.00f,
                    Jitter = Jitter(0.60f, 0.70f, new Vector2(3.0f, 0.5f), NoiseChannel.Red),
                    FocusDir = new Vector2(0f, -1f),
                    FocusPower = 1f,
                    FocusAmount = 0f
                },
                Noise = new OverlayNoise
                {
                    BaseScale = new Vector2(4.2f, 0.8f),
                    // The one layer allowed to drift. The shader has no per-layer UV offset, and a
                    // fixed one would be wrong anyway: a highlight on running fluid travels relative
                    // to the bulk, and that slow slide is what stops the two layers from looking
                    // like one texture drawn twice. Kept small - at larger ratios it stops being a
                    // specular on the runs and becomes a second thing moving over them.
                    BaseScroll = Flow(0.8f, HIGHLIGHT_FLOW_SCALE),
                    BaseChannel = NoiseChannel.Red,
                    DetailScale = new Vector2(8.5f, 1.9f),
                    DetailScroll = Flow(1.9f, HIGHLIGHT_FLOW_SCALE),
                    DetailChannel = NoiseChannel.Green,
                    WarpStrength = 0.05f,
                    RidgeAmount = 0.00f,
                    Threshold = 0.78f,
                    Softness = 0.06f,
                    FlatFloor = 0.00f
                },
                Appearance = new OverlayAppearance
                {
                    Tint = HighlightHue,
                    Opacity = Opacity * HIGHLIGHT_OPACITY_SCALE,
                    Intensity = Intensity,
                    PulseFreq = 0.00f,
                    PulseAmp = 0.00f
                }
            }
        };

    /// <summary>
    /// Downward scroll for a field of the given vertical scale, so that every layer travels at
    /// <see cref="FLOW_SCREEN_SPEED"/> times <paramref name="speedScale"/> screen heights per second
    /// regardless of how the scales are tuned.
    /// </summary>
    private static Vector2 Flow(float verticalScale, float speedScale) =>
        new(0f, -FLOW_SCREEN_SPEED * speedScale * verticalScale);

    /// <summary>
    /// Boundary flux for one layer. <paramref name="reachAmount"/> varies how far the layer reaches;
    /// <paramref name="featherAmount"/> varies the length of the gradient behind it, so a deep run
    /// tapers away and a shallow one ends bluntly. <paramref name="scale"/> is coarser than the
    /// layer's detail noise - at detail frequency the boundary just buzzes.
    /// </summary>
    private static OverlayJitter Jitter(float reachAmount, float featherAmount, Vector2 scale, NoiseChannel channel) =>
        new()
        {
            ReachAmount = reachAmount,
            FeatherAmount = featherAmount,
            Scale = scale,
            // Locked to the flow, so the ragged edge travels with the fluid rather than crawling
            // across it.
            Scroll = Flow(scale.Y, 1f),
            Channel = channel
        };

    private static Color Scale(Color color, float red, float green, float blue) =>
        new((int)(color.R * red), (int)(color.G * green), (int)(color.B * blue));
}
