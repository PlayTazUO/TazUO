using System.Collections.Generic;
using ClassicUO.Game.ScreenDecorations.Overlays.Presets.Layers;
using ClassicUO.Renderer.Effects;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.ScreenDecorations.Overlays.Presets;

/// <summary>
///     Distance blurring away into haze: an out-of-focus band around the screen edge with a pale wash
///     over it.
///     <para>
///     The blur carries the effect and the wash only tints what it has already softened, which is why
///     the wash is so weak. A fog built from tint alone has to be near-opaque before it reads as
///     anything but a colour filter, and at that strength it hides the world instead of receding it.
///     </para>
/// </summary>
public sealed class FogOverlay : ScreenOverlayPreset
{
    /// <summary>Wide and soft: fog has no boundary, and a short feather turns the blur into a
    /// visible ring of smeared pixels.</summary>
    private const float BLUR_FEATHER = 0.55f;

    private const float WASH_FEATHER = 0.60f;

    /// <summary>Blur radius as a fraction of screen width. Past ~1.5% the terrain stops reading as
    /// distant and starts reading as a smeared texture.</summary>
    private const float BLUR_RADIUS = 0.010f;

    /// <summary>Noise modulation of the blur strength, so the bank drifts rather than sitting on the
    /// screen like a lens.</summary>
    private const float BLUR_SWIM = 0.45f;

    public float Intensity { get; set; } = 1.0f;

    /// <summary>Colour of the wash over the blurred band.</summary>
    public Color Hue { get; set; } = new(198, 206, 214);

    public float Opacity { get; set; } = 0.30f;

    /// <summary>How far in from the screen edge the fog reaches.</summary>
    public float Reach { get; set; } = 0.55f;

    /// <summary>Strength of the blur where the fog is thickest, 1 being fully out of focus.</summary>
    public float Blur { get; set; } = 0.85f;

    protected override void Bake(List<OverlayLayer> layers)
    {
        // The blur must be the bottom layer: it samples the frame from before this pass, so anything
        // drawn beneath it here would be replaced rather than softened.
        layers.Add(
            SamplingLayers.Blur(
                SamplingShape.Vignette(Reach, BLUR_FEATHER, Blur, BLUR_SWIM),
                BLUR_RADIUS
            )
        );

        layers.Add(BakeWash());
    }

    private OverlayLayer BakeWash() =>
        new()
        {
            Blend = OverlayBlend.Alpha,
            Params = new OverlayParams
            {
                Shape = new OverlayShape
                {
                    Center = new Vector2(0.5f, 0.5f),
                    // Stops slightly shorter than the blur, so the softening arrives before the
                    // colour does rather than the two announcing themselves together.
                    Reach = Reach * 0.92f,
                    Feather = WASH_FEATHER,
                    EdgeBlend = 0.00f,
                    FocusDir = new Vector2(0f, -1f),
                    FocusPower = 1f,
                    FocusAmount = 0f
                },
                Noise = new OverlayNoise
                {
                    BaseScale = new Vector2(1.8f, 1.8f),
                    DetailScale = new Vector2(3.6f, 3.6f),
                    BaseScroll = new Vector2(0.008f, -0.003f),
                    DetailScroll = new Vector2(-0.006f, -0.005f),
                    BaseChannel = NoiseChannel.Red,
                    DetailChannel = NoiseChannel.Green,
                    WarpStrength = 0.25f,
                    RidgeAmount = 0.00f,
                    Threshold = 0.42f,
                    Softness = 0.38f,
                    FlatFloor = 0.55f
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
}
