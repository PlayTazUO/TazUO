using System.Collections.Generic;
using ClassicUO.Game.ScreenDecorations.Overlays.Presets.Layers;
using ClassicUO.Renderer.Effects;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.ScreenDecorations.Overlays.Presets;

/// <summary>
///     Distance blurring away into haze: an out-of-focus band around the screen edge with a pale wash
///     over it, both reaching to the centre so the player reads as engulfed rather than standing at
///     the edge of a bank. Both layers breathe together on a slow, shallow pulse - vapour ebbing and
///     flowing rather than a static fill.
///     <para>
///     The blur carries the effect and the wash only tints what it has already softened, which is why
///     the wash is so weak. A fog built from tint alone has to be near-opaque before it reads as
///     anything but a colour filter, and at that strength it hides the world instead of receding it.
///     The blur stops a little short of the tint's own reach, so the out-of-focus band stays a rim
///     rather than blurring the whole view.
///     </para>
/// </summary>
public sealed class FogOverlay : ScreenOverlayPreset
{
    /// <summary>Slow enough to read as vapour drifting closer and receding, not a strobe.</summary>
    private const float PULSE_FREQ_HZ = 0.18f;

    /// <summary>Weak - the ebb and flow should be felt more than seen.</summary>
    private const float PULSE_AMP = 0.08f;

    #region Ctor

    public FogOverlay()
    {
        // A bank this heavy shouldn't slam in or vanish - both slower than the base default so it
        // reads as rolling in and thinning out rather than switching on and off.
        FadeInSeconds = 3f;
        FadeOutSeconds = 4f;
    }

    #endregion

    public float Intensity { get; set; } = 1.0f;

    /// <summary>Colour of the wash over the blurred band.</summary>
    public Color Hue { get; set; } = new(198, 206, 214);

    public float Opacity { get; set; } = 0.30f;

    /// <summary>How far in from the screen edge the fog reaches. 1 covers the whole render target, for
    /// the effect of being engulfed rather than standing at the edge of a bank.</summary>
    public float Reach { get; set; } = 1.00f;

    /// <summary>Strength of the blur where the fog is thickest, 1 being fully out of focus.</summary>
    public float Blur { get; set; } = 0.78f;

    protected override void Bake(List<OverlayLayer> layers)
    {
        // The blur must be the bottom layer: it samples the frame from before this pass, so anything
        // drawn beneath it here would be replaced rather than softened.
        layers.Add(
            SamplingLayers.Blur(
                SamplingShape.Vignette(
                    // Stops short of the tint's own reach - engulfing colour, with the out-of-focus
                    // band staying a rim around it instead of blurring the whole view.
                    LayerReach.Shallower(Reach, 0.10f),
                    // Wide and soft: fog has no boundary, and a short feather turns the blur into a
                    // visible ring of smeared pixels.
                    0.55f,
                    Blur,
                    // Noise modulation of the blur strength, so the bank drifts rather than sitting
                    // on the screen like a lens.
                    0.45f
                )
                with
                {
                    PulseFreq = PULSE_FREQ_HZ,
                    PulseAmp = PULSE_AMP
                },
                // Blur radius as a fraction of screen width. Past ~1.5% the terrain stops reading as
                // distant and starts reading as a smeared texture.
                0.010f
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
                    Reach = Reach,
                    Feather = 0.60f,
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
                    PulseFreq = PULSE_FREQ_HZ,
                    PulseAmp = PULSE_AMP
                }
            }
        };
}
