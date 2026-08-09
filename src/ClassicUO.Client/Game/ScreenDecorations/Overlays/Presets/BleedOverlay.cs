using System.Collections.Generic;
using ClassicUO.Game.ScreenDecorations.Overlays.Presets.Layers;
using ClassicUO.Renderer.Effects;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.ScreenDecorations.Overlays.Presets;

/// <summary>
///     Dark fluid clinging to the screen border: raggedly terminated streaks with a coarser
///     sputter of spatter riding beside them, and a dim specular over the streaks. Weighted toward
///     the streak layer - the sputter is an accent that extends reach and breaks up the silhouette,
///     the specular exists to define a surface, not to be seen.
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
///     <item>Weight comes from opacity and threshold, never from coverage alone. Lowering
///     thresholds to add body just fills the trim in solid.</item>
///     <item>All three layers derive their scroll from the same screen speed - only the specular
///     and the sputter are allowed to drift off it, or every pass reads as one texture copy-pasted.</item>
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
    // specular and the sputter are allowed to drift.
    private const float FLOW_SCREEN_SPEED = 0.010f;
    private const float HIGHLIGHT_FLOW_SCALE = 1.2f;

    // Sputter reads as heavier spatter thrown less forcefully than the streaks running under it, so
    // it drifts down slower rather than faster.
    private const float SPUTTER_FLOW_SCALE = 0.7f;

    // Relative weight against the caller's Opacity. The specular is faint because it is additive
    // and therefore the only thing that can lighten the composite.
    private const float HIGHLIGHT_OPACITY_SCALE = 0.14f;

    // The sputter is an accent riding alongside the streaks, not a second full mass - at full
    // Opacity the pair would read as twice the blood the caller asked for.
    private const float SPUTTER_OPACITY_SCALE = 0.85f;

    // The specular stops shorter than the streaks beneath it, so the composite thins out toward the
    // middle of the screen instead of both ending together. A fixed margin rather than a fraction of
    // Reach: this preset's Reach defaults well past 0.2, where a proportional margin would be only a
    // couple of hundredths wide and the two boundaries would land on top of one another.
    private const float HIGHLIGHT_REACH_MARGIN = 0.040f;

    // The sputter reaches slightly deeper than the streaks it rides beside - coarse spatter carries
    // further than a fine run before it runs out of momentum.
    private const float SPUTTER_REACH_MARGIN = 0.05f;

    public float Intensity { get; set; } = 1.0f;

    /// <summary>Blood colour of the rivulet pass.</summary>
    public Color Hue { get; set; } = new(150, 16, 20);

    /// <summary>Specular tint of the highlight pass, added on top of everything else.</summary>
    public Color HighlightHue { get; set; } = new(198, 46, 34);

    public float Opacity { get; set; } = 1.0f;

    /// <summary>
    /// How far in from the screen edge the fluid reaches. Larger is thicker. Pushed well past the
    /// old film-layer default so the streaks alone read as blood on the camera rather than a thin
    /// trim, with visible mass creeping toward the centre of the screen. The sputter layer reaches a
    /// little further still; see <see cref="SputterReach" />.
    /// </summary>
    public float Reach { get; set; } = 0.40f;

    /// <summary>Shortest of the three, measured off <see cref="Reach" />.</summary>
    private float HighlightReach => LayerReach.Shallower(Reach, HIGHLIGHT_REACH_MARGIN);

    /// <summary>Deepest of the three, measured off <see cref="Reach" />.</summary>
    private float SputterReach => LayerReach.Deeper(Reach, SPUTTER_REACH_MARGIN);

    protected override void Bake(List<OverlayLayer> layers)
    {
        layers.Add(BakeStreaks());
        layers.Add(BakeSputter());
        layers.Add(BakeHighlight());
    }

    /// <summary>
    /// The streaks themselves: the most anisotropic and most raggedly terminated pass, at the
    /// caller's full blood colour. The base layer, so it carries <see cref="Reach" /> and a
    /// wide feather to cover the screen as a spreading wash. Threshold sits higher and softness
    /// tighter than the old rivulet pass so each streak resolves thinner, with the sputter layer
    /// carrying the body that would otherwise need thickening this one to supply.
    /// </summary>
    private OverlayLayer BakeStreaks() =>
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
                    // ~5:1 anisotropy: high U frequency, low V frequency, so features come out as
                    // long narrow streaks instead of the isotropic blobs that read as gas.
                    BaseScale = new Vector2(4.2f, 0.8f),
                    BaseScroll = Flow(0.8f, 1f),
                    BaseChannel = NoiseChannel.Red,
                    DetailScale = new Vector2(7.0f, 1.5f),
                    DetailScroll = Flow(1.5f, 1f),
                    DetailChannel = NoiseChannel.Green,
                    WarpStrength = 0.06f,
                    RidgeAmount = 0.00f,
                    // Raised from the old rivulet pass - higher keeps less, so each streak resolves
                    // narrower instead of a continuous painted band. Pulled back from an even higher
                    // first pass, which thinned the streaks past their body and into wisps.
                    Threshold = 0.55f,
                    // Tightened alongside Threshold: a thin streak needs a crisp meniscus, not a
                    // soft-edged cloud.
                    Softness = 0.06f,
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
    /// Coarse spatter riding beside the streaks: near-isotropic noise so its features read as
    /// discrete sputter rather than a second streak field, sparser and slower-moving than the
    /// streaks it accompanies, and reaching a little deeper toward the centre of the screen.
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
                    // long runs.
                    BaseScale = new Vector2(1.8f, 1.5f),
                    BaseScroll = Flow(1.5f, SPUTTER_FLOW_SCALE),
                    BaseChannel = NoiseChannel.Red,
                    DetailScale = new Vector2(3.0f, 2.5f),
                    DetailScroll = Flow(2.5f, SPUTTER_FLOW_SCALE),
                    DetailChannel = NoiseChannel.Green,
                    WarpStrength = 0.07f,
                    RidgeAmount = 0.00f,
                    // Pulled well back from an initial pass that left the sputter reading as
                    // scattered dots instead of spatter.
                    Threshold = 0.48f,
                    Softness = 0.08f,
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
    /// Shares the streak pass's primary noise scale, channel and jitter field, so it rides the same
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
                    Reach = HighlightReach,
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
