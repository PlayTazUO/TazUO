#nullable enable

using System.Collections.Generic;
using ClassicUO.Game.ScreenDecorations.Overlays.Presets.Layers;
using ClassicUO.Renderer.Effects;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.ScreenDecorations.Overlays.Presets;

/// <summary>
/// Hyperspace jump: a small sharp core with everything outside it drawn into streaks, lit by a cold
/// additive rim. A one-shot - the sense of being flung somewhere comes from the fade envelope, so
/// this is the one preset with no pulse, no wobble and no swim. Anything that breathes turns a jump
/// into a state.
/// <para>
/// Both halves are deliberately the inverse of <see cref="TunnelVisionOverlay" />, which is the look
/// this slides into if either is mistuned. A <em>painted dark</em> vignette is tunnel vision by
/// definition, so the second layer brightens additively instead of covering; and a long feather
/// leaves most of the screen at partial strength, which reads as soft focus rather than speed, so
/// the mask reaches full close in and the gradient is short.
/// </para>
/// <para>
/// Radial rather than a disk blur for the reason <see cref="DrunkOverlay" /> uses it, pushed far
/// harder: the taps march along the ray from the centre, so the focal point stays perfectly sharp
/// however strong the smear is. That is what reads as hurtling forward instead of as a smeared
/// screen.
/// </para>
/// </summary>
public sealed class TeleportOverlay : ScreenOverlayPreset
{
    /// <summary>Full screen. There is nothing to preserve at the edges - the streaks are the effect,
    /// and the sharp centre comes from the technique, not from the mask.</summary>
    private const float STREAK_REACH = LayerReach.Max;

    /// <summary>
    /// Short, and the parameter that decides whether this is a warp or a blur. The mask rises from
    /// nothing at the centre to full at the rim, so this is the width of the band between the sharp
    /// core and full speed. Long values leave the whole mid-screen half-smeared, which the eye reads
    /// as needing glasses; short ones give a small clear core with everything past it travelling.
    /// </summary>
    private const float STREAK_FEATHER = 0.28f;

    /// <summary>
    /// The highest count compiled, and this preset needs all of it. Tap spacing grows with distance
    /// from the centre, so at this <see cref="Zoom" /> the corners are the first thing to break into
    /// distinct ghost copies of the frame. The shader's per-pixel tap dither is what makes the count
    /// survivable at all here; without it nothing above roughly 0.15 zoom holds together.
    /// </summary>
    private const OverlaySampleTaps STREAK_TAPS = OverlaySampleTaps.Sixteen;

    /// <summary>How far short of the streaks the rim light stops, so the two boundaries do not land
    /// together and read as one hard ring.</summary>
    private const float GLOW_REACH_MARGIN = 0.18f;

    /// <summary>Soft, unlike the streak mask. This is light bleeding inward, and light with a
    /// defined edge reads as a painted ring.</summary>
    private const float GLOW_FEATHER = 0.55f;

    public float Intensity { get; set; } = 1.0f;

    /// <summary>
    /// How far along the centre ray the taps march, and the speed dial. The whole difference between
    /// this preset and <see cref="DrunkOverlay" />, which asks for about a tenth of it.
    /// </summary>
    public float Zoom { get; set; } = 0.62f;

    /// <summary>Strength of the smear outside the core. Full: there is nothing worth keeping legible
    /// out there during a jump.</summary>
    public float Streaks { get; set; } = 1.0f;

    /// <summary>
    /// Radians the streaks twist through on the way in. This is what separates a warp from a plain
    /// zoom blur - straight rays read as rushing toward something, a spiral reads as being pulled
    /// through. Kept well under the half-turn ceiling: past roughly a third of a turn the streaks
    /// start to wrap and it becomes a whirlpool, which is a different effect.
    /// </summary>
    public float Swirl { get; set; } = 0.85f;

    /// <summary>
    /// Rim light colour. Cold and bright - this is the frame's own light stretched past the player,
    /// and it is added rather than painted, so it tints what the streaks left behind instead of
    /// hiding it.
    /// </summary>
    public Color Glow { get; set; } = new(0x8A, 0xB4, 0xFF);

    /// <summary>
    /// Kept low. Additive accumulates on whatever the streaks resolved to, so this reaches far
    /// brighter than the same number would as an alpha layer - and a jump that blows the frame to
    /// white stops showing the streaks that make it a jump.
    /// </summary>
    public float GlowOpacity { get; set; } = 0.30f;

    public TeleportOverlay()
    {
        // Arrives faster than anything else here and leaves only a little slower. A jump is over
        // before it can be studied; a gentle fade in would announce it in advance.
        FadeInSeconds = 0.08f;
        FadeOutSeconds = 0.55f;
    }

    /// <inheritdoc />
    protected override void Bake(List<OverlayLayer> layers)
    {
        // Bottom: it samples the pre-pass frame, so the rim light above it would be replaced rather
        // than smeared into the streaks.
        layers.Add(
            SamplingLayers.Radial(
                SamplingShape.Vignette(STREAK_REACH, STREAK_FEATHER, Streaks),
                Zoom,
                STREAK_TAPS,
                Swirl
            )
        );

        layers.Add(BakeGlow());
    }

    /// <summary>
    /// The cold light the streaks run out into. Flat-floored and still: a textured rim would drift
    /// and read as fog or smoke going past, which is a different effect entirely.
    /// </summary>
    /// <returns>The layer.</returns>
    private OverlayLayer BakeGlow() =>
        new()
        {
            Blend = OverlayBlend.Additive,
            Params = new OverlayParams
            {
                Shape = new OverlayShape
                {
                    Center = new Vector2(0.5f, 0.5f),
                    Reach = LayerReach.Shallower(STREAK_REACH, GLOW_REACH_MARGIN),
                    Feather = GLOW_FEATHER,
                    EdgeBlend = 0f,
                    FocusDir = new Vector2(0f, -1f),
                    FocusPower = 1f,
                    FocusAmount = 0f
                },
                Noise = OverlayParams.Default.Noise with { FlatFloor = 1f },
                Appearance = new OverlayAppearance
                {
                    Tint = Glow,
                    Opacity = GlowOpacity,
                    Intensity = Intensity
                }
            }
        };
}
