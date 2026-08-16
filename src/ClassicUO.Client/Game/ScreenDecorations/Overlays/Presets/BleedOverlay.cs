using System.Collections.Generic;
using ClassicUO.Game.ScreenDecorations.Overlays.Presets.Layers;
using ClassicUO.Renderer.Effects;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.ScreenDecorations.Overlays.Presets;

/// <summary>
///     Dark fluid clinging to the screen border: raggedly terminated streaks - both thin runs and
///     wider ones, for a fall with some visual rhythm to it rather than one uniform width - with a
///     coarser sputter of spatter riding beside them and carrying reach on toward the centre of the
///     screen. Weighted toward the streak layers - the sputter is an accent that extends reach and
///     breaks up the silhouette.
///     <para>
///     Constraints that keep this reading as fluid rather than as gas, cells, or cloud. Every one of
///     them was learned by violating it:
///     </para>
///     <list type="bullet">
///     <item>Zero <see cref="OverlayNoise.RidgeAmount"/>, and no Blue or Alpha channel. All three
///     draw outlines around cells; they belong to the fracture preset.</item>
///     <item><see cref="OverlayNoise.FlatFloor"/> of exactly 0 on every layer. Any floor makes the
///     mask render its own geometry, which is a soft-edged rectangle.</item>
///     <item>Weight comes from opacity and threshold, never from coverage alone. Lowering
///     thresholds to add body just fills the trim in solid.</item>
///     <item>All layers derive their scroll from the same base screen speed, each with its own
///     fixed <see cref="OverlayNoise.Offset"/>. The two streak passes stay within about 10% of each
///     other's speed - matching exactly reads as one texture copy-pasted at a different scale - and
///     only the sputter is allowed to drift much further, being the coarser, more independent mass.</item>
///     <item>Vertical anisotropy for streaks rather than blobs - but only moderate. Past about 5:1
///     the streaks get thin enough to read as wisps. The sputter inverts this: near-1:1 so its
///     features read as spatter, not as a second streak field.</item>
///     <item>Corner weighting via <see cref="OverlayShape.CornerBias"/>, never by blending
///     <see cref="OverlayShape.EdgeBlend"/> toward radial. The radial term is width-normalised, so
///     on a widescreen display it lands almost entirely on the left and right edges.</item>
///     </list>
/// </summary>
public sealed class BleedOverlay : ScreenOverlayPreset
{
    // Screen speed is scroll/scale, not scroll. Every layer derives its scroll from this so the
    // relationship survives a scale change - the streaks are locked to it exactly, and only the
    // sputter is allowed to drift.
    private const float FLOW_SCREEN_SPEED = 0.010f;

    // Sputter reads as heavier spatter thrown less forcefully than the streaks running under it, so
    // it drifts down slower rather than faster.
    private const float SPUTTER_FLOW_SCALE = 0.7f;

    // The wide streak pass is an accent riding alongside the thin one, not a second full mass - full
    // Opacity on both would read as twice the blood the caller asked for.
    private const float WIDE_STREAK_OPACITY_SCALE = 0.60f;

    // The sputter is an accent riding alongside the streaks, not a second full mass - at full
    // Opacity the pair would read as twice the blood the caller asked for.
    private const float SPUTTER_OPACITY_SCALE = 0.85f;

    // Deliberately large: the streaks alone leave the screen centre empty, and the sputter is what
    // was asked to carry the fluid the rest of the way there.
    private const float SPUTTER_REACH_MARGIN = 0.30f;

    // Small: the wide pass is meant to sit almost on top of the thin one, just enough off it that
    // the two boundaries never land exactly together, which reads as a single hard-edged ring.
    private const float WIDE_STREAK_REACH_MARGIN = 0.06f;

    public float Intensity { get; set; } = 1.0f;

    /// <summary>Blood colour of the rivulet pass.</summary>
    public Color Hue { get; set; } = new(150, 16, 20);

    public float Opacity { get; set; } = 1.0f;

    /// <summary>
    /// How far in from the screen edge the fluid reaches. Larger is thicker. Pushed well past the
    /// old film-layer default so the streaks alone read as blood on the camera rather than a thin
    /// trim, with visible mass creeping toward the centre of the screen. The sputter layer reaches
    /// much further still; see <see cref="SputterReach" />.
    /// </summary>
    public float Reach { get; set; } = 0.8f;

    /// <summary>Deepest of the three, measured off <see cref="Reach" />.</summary>
    private float SputterReach => LayerReach.Deeper(Reach, SPUTTER_REACH_MARGIN);

    /// <summary>Just inside <see cref="Reach" />, so it never boundary-ties with the thin pass.</summary>
    private float WideStreakReach => LayerReach.Shallower(Reach, WIDE_STREAK_REACH_MARGIN);

    protected override void Bake(List<OverlayLayer> layers)
    {
        layers.Add(BakeStreaksThin());
        layers.Add(BakeStreaksWide());
        layers.Add(BakeSputter());
    }

    /// <summary>
    /// The fine streaks: the most anisotropic and most raggedly terminated pass, at the caller's
    /// full blood colour. The base layer, so it carries <see cref="Reach" /> and a wide feather to
    /// cover the screen as a spreading wash. Widened and given a touch more body than the original
    /// pass - it was reading thin enough to feel transparent - while <see cref="BakeStreaksWide" />
    /// rides alongside it for the occasional wider run.
    /// </summary>
    private OverlayLayer BakeStreaksThin() =>
        new()
        {
            Blend = OverlayBlend.Alpha,
            Params = new OverlayParams
            {
                Shape = new OverlayShape
                {
                    Center = new Vector2(0.5f, 0.5f),
                    Reach = Reach,
                    // Wider than a pure streak edge would need - this layer carries the
                    // screen-covering mass, so its transition has to read as a spreading wash, not
                    // a crisp trim.
                    Feather = 0.20f,
                    EdgeBlend = 1.00f,
                    CornerBias = 1.00f,
                    Jitter = Jitter(0.60f, 0.70f, new Vector2(3.0f, 0.5f), NoiseChannel.Red),
                    FocusDir = new Vector2(0f, -1f),
                    FocusPower = 1f,
                    FocusAmount = 0f
                },
                Noise = new OverlayNoise
                {
                    // ~4.4:1 anisotropy: high U frequency, low V frequency, so features come out as
                    // long narrow streaks instead of the isotropic blobs that read as gas. Frequency
                    // pulled down about 20% from the original pass to widen each streak by the same
                    // fraction.
                    BaseScale = new Vector2(3.5f, 0.8f),
                    BaseScroll = Flow(0.8f, 1f),
                    BaseChannel = NoiseChannel.Red,
                    DetailScale = new Vector2(5.8f, 1.5f),
                    DetailScroll = Flow(1.5f, 1f),
                    DetailChannel = NoiseChannel.Green,
                    // The anchor layer - everything else is offset and re-timed relative to this one.
                    Offset = Vector2.Zero,
                    // Raised off near-zero - a little churn breaks the runs into uneven trails
                    // instead of straight painted bands, without enough warp to read as billowing gas.
                    WarpStrength = 0.12f,
                    RidgeAmount = 0.00f,
                    // Pulled back down from an even higher pass - that read as resolved streaks but
                    // thin enough to feel see-through. Higher still keeps less than the old rivulet
                    // pass, so streaks stay distinct rather than one continuous painted band.
                    Threshold = 0.60f,
                    // Tightened alongside Threshold: a streak needs a crisp meniscus, not a
                    // soft-edged cloud.
                    Softness = 0.05f,
                    FlatFloor = 0.00f
                },
                Appearance = new OverlayAppearance
                {
                    Tint = Hue,
                    Opacity = Opacity,
                    Intensity = Intensity,
                    PulseFreq = 0.00f,
                    PulseAmp = 0.00f
                }
            }
        };

    /// <summary>
    /// Coarser accent riding alongside <see cref="BakeStreaksThin" />: lower frequency for
    /// noticeably wider runs, a looser threshold so only some of them resolve, and a different noise
    /// channel pairing so its runs land at different screen positions than the fine pass rather than
    /// simply thickening it uniformly. The dynamism this buys - thin streaks most places, occasional
    /// wide ones - is the point; a single wider pass would just be a thicker uniform streak.
    /// </summary>
    private OverlayLayer BakeStreaksWide() =>
        new()
        {
            Blend = OverlayBlend.Alpha,
            Params = new OverlayParams
            {
                Shape = new OverlayShape
                {
                    Center = new Vector2(0.5f, 0.5f),
                    Reach = WideStreakReach,
                    Feather = 0.20f,
                    EdgeBlend = 1.00f,
                    CornerBias = 1.00f,
                    // Speed here must match the Noise block's below, or the ragged edge crawls
                    // across the fill instead of travelling with it.
                    // X kept below the fill's own BaseScale.X (2.2) - coarser than the detail it
                    // displaces, or the boundary just buzzes at the fill's own rate.
                    Jitter = Jitter(0.60f, 0.70f, new Vector2(1.6f, 0.40f), NoiseChannel.Green, 0.90f),
                    FocusDir = new Vector2(0f, -1f),
                    FocusPower = 1f,
                    FocusAmount = 0f
                },
                Noise = new OverlayNoise
                {
                    // ~2.9:1 - still reads as streaks rather than blobs, just chunkier ones.
                    BaseScale = new Vector2(2.2f, 0.75f),
                    // 10% off the thin pass's screen speed - matching it exactly made the two read
                    // as one rigid mass gliding in lockstep, which is what looked "scripted".
                    BaseScroll = Flow(0.75f, 0.90f),
                    // Swapped from the thin pass's Red/Green pairing so the two fields decorrelate
                    // instead of tracing the same runs at a different scale.
                    BaseChannel = NoiseChannel.Green,
                    DetailScale = new Vector2(3.7f, 1.3f),
                    DetailScroll = Flow(1.3f, 0.90f),
                    DetailChannel = NoiseChannel.Red,
                    // Fixed, time-independent shift so this pass starts from a different point in
                    // the noise field than the thin pass instead of a scaled copy of the same runs.
                    Offset = new Vector2(0.37f, 0.61f),
                    WarpStrength = 0.12f,
                    RidgeAmount = 0.00f,
                    // Looser than the thin pass - at low frequency a matching threshold would cover
                    // too much of the screen, so this keeps only the more prominent wide runs.
                    Threshold = 0.56f,
                    Softness = 0.03f,
                    FlatFloor = 0.00f
                },
                Appearance = new OverlayAppearance
                {
                    Tint = Hue,
                    Opacity = Opacity * WIDE_STREAK_OPACITY_SCALE,
                    Intensity = Intensity,
                    PulseFreq = 0.00f,
                    PulseAmp = 0.00f
                }
            }
        };

    /// <summary>
    /// Coarse spatter riding beside the streaks: near-isotropic noise so its features read as
    /// discrete sputter rather than a second streak field, sparser and slower-moving than the
    /// streaks it accompanies, and carrying reach much further toward the centre of the screen than
    /// either streak pass - it's what keeps the middle of the screen from reading empty.
    /// </summary>
    private OverlayLayer BakeSputter() =>
        new()
        {
            Blend = OverlayBlend.Alpha,
            Params = new OverlayParams
            {
                Shape = new OverlayShape
                {
                    Center = new Vector2(0.5f, 0.5f),
                    Reach = SputterReach,
                    Feather = 0.16f,
                    EdgeBlend = 1.00f,
                    CornerBias = 1.00f,
                    Jitter = Jitter(0.55f, 0.65f, new Vector2(2.2f, 1.0f), NoiseChannel.Green, SPUTTER_FLOW_SCALE),
                    FocusDir = new Vector2(0f, -1f),
                    FocusPower = 1f,
                    FocusAmount = 0f
                },
                Noise = new OverlayNoise
                {
                    // Near-1:1 anisotropy - the inverse of the streak pass. Discrete flecks, not
                    // long runs. Raised frequency so each fleck resolves as a small glob or droplet
                    // rather than a blob wide enough to read as cloud.
                    BaseScale = new Vector2(4.5f, 4.0f),
                    BaseScroll = Flow(4.0f, SPUTTER_FLOW_SCALE),
                    BaseChannel = NoiseChannel.Red,
                    DetailScale = new Vector2(7.5f, 6.5f),
                    DetailScroll = Flow(6.5f, SPUTTER_FLOW_SCALE),
                    DetailChannel = NoiseChannel.Green,
                    // A third fixed shift, distinct from both streak passes.
                    Offset = new Vector2(0.71f, 0.14f),
                    // Raised alongside the streak pass - enough churn to round the droplet edges
                    // unevenly instead of stamping the same fleck shape everywhere.
                    WarpStrength = 0.14f,
                    RidgeAmount = 0.00f,
                    // Pulled back a little from an even higher pass - that kept the spatter thin
                    // enough to feel transparent. Still high enough that droplets stay individual
                    // rather than merging into a continuous speckled mass.
                    Threshold = 0.58f,
                    // Tightened with Threshold - a droplet needs a defined edge, not a soft-edged dot.
                    Softness = 0.05f,
                    FlatFloor = 0.00f
                },
                Appearance = new OverlayAppearance
                {
                    Tint = Hue,
                    Opacity = Opacity * SPUTTER_OPACITY_SCALE,
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
    /// layer's detail noise - at detail frequency the boundary just buzzes. <paramref name="speedScale"/>
    /// must match the layer's own flow speed scale, or the ragged edge crawls across the fluid
    /// instead of travelling with it.
    /// </summary>
    private static OverlayJitter Jitter(
        float reachAmount,
        float featherAmount,
        Vector2 scale,
        NoiseChannel channel,
        float speedScale = 1f
    ) =>
        new()
        {
            ReachAmount = reachAmount,
            FeatherAmount = featherAmount,
            Scale = scale,
            Scroll = Flow(scale.Y, speedScale),
            Channel = channel
        };
}
