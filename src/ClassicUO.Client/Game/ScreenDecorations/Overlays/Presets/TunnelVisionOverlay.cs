using System.Collections.Generic;
using ClassicUO.Renderer.Effects;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.ScreenDecorations.Overlays.Presets;

/// <summary>
/// Flat, tightening black vignette. No noise field - motion comes only from the fade envelope
/// and from <see cref="Reach"/> being adjusted by the caller over time.
/// </summary>
public sealed class TunnelVisionOverlay : ScreenOverlayPreset
{
    public float Intensity { get; set; } = 1.0f;
    public float Opacity { get; set; } = 0.88f;
    public float Reach { get; set; } = 0.70f;

    public TunnelVisionOverlay()
    {
        FadeInSeconds = 2f;
        FadeOutSeconds = 3.5f;
    }

    protected override void Bake(List<OverlayLayer> layers) =>
        layers.Add(new OverlayLayer
        {
            Params = new OverlayParams
            {
                Shape = new OverlayShape
                {
                    Center = new Vector2(0.5f, 0.5f),
                    WobbleFreq = new Vector2(0.02f, 0.02f),
                    WobbleAmp = 0.007f,
                    Reach = Reach,
                    Feather = 0.12f,
                    EdgeBlend = 0.00f,
                    CornerBias = 0f,
                    Jitter = new OverlayJitter
                    {
                        ReachAmount = 0.1f,
                        FeatherAmount = 0.22f,
                        Scale = new Vector2(2f, 2f),
                        Scroll = Vector2.Zero,
                        Channel = NoiseChannel.Red
                    },
                    FocusDir = Vector2.Zero,
                    FocusPower = 1f,
                    FocusAmount = 0f
                },
                Noise = new OverlayNoise
                {
                    BaseScale = new Vector2(3f, 3f),
                    DetailScale = new Vector2(6f, 6f),
                    BaseScroll = Vector2.Zero,
                    DetailScroll = new Vector2(0.07f, 0.03f),
                    BaseChannel = NoiseChannel.Red,
                    DetailChannel = NoiseChannel.Green,
                    Offset = Vector2.Zero,
                    WarpStrength = 0.2f,
                    RidgeAmount = 0.00f,
                    Threshold = 0.5f,
                    Softness = 0.2f,
                    FlatFloor = 0.97f
                },
                Appearance = new OverlayAppearance
                {
                    Tint = new Color(0x0D, 0x0D, 0x0D),
                    Opacity = Opacity,
                    Intensity = Intensity,
                    PulseFreq = 0.06f,
                    PulseAmp = 0.03f
                }
            }
        });
}
