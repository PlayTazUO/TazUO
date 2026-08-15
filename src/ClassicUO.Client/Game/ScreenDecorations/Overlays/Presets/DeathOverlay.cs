using System.Collections.Generic;
using ClassicUO.Renderer.Effects;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.ScreenDecorations.Overlays.Presets;

/// <summary>
///     A near-black shroud pooling in from the top of the screen: the last thing seen before the
///     world goes dark. A single ridged mass, so the edge reads as a crack racing in rather than a
///     wash settling over the view, with a slow pulse so it does not just sit there once arrived.
/// </summary>
public sealed class DeathOverlay : ScreenOverlayPreset
{
    public float Intensity { get; set; } = 1.0f;

    public Color Hue { get; set; } = new(18, 18, 18);

    public float Opacity { get; set; } = 0.6f;

    public DeathOverlay()
    {
        FadeInSeconds = 1.4f;
        FadeOutSeconds = 3f;
    }

    protected override void Bake(List<OverlayLayer> layers) =>
        layers.Add(
            new OverlayLayer
            {
                Blend = OverlayBlend.Alpha,
                Params = new OverlayParams
                {
                    Shape = new OverlayShape
                    {
                        Center = new Vector2(0.5f, 0.06f),
                        WobbleFreq = new Vector2(0.4f, 0.3f),
                        WobbleAmp = 0f,
                        Reach = 0.25f,
                        Feather = 0.3f,
                        EdgeBlend = 0f,
                        CornerBias = 0f,
                        Jitter = new OverlayJitter
                        {
                            ReachAmount = 0f,
                            FeatherAmount = 0f,
                            Scale = new Vector2(2f, 2f),
                            Scroll = Vector2.Zero,
                            Channel = NoiseChannel.Red
                        },
                        FocusDir = new Vector2(0f, -1f),
                        FocusPower = 1f,
                        FocusAmount = 0f
                    },
                    Noise = new OverlayNoise
                    {
                        BaseScale = new Vector2(7f, 3f),
                        BaseScroll = Vector2.Zero,
                        BaseChannel = NoiseChannel.Red,
                        DetailScale = new Vector2(6f, 6f),
                        DetailScroll = new Vector2(0f, 0.1f),
                        DetailChannel = NoiseChannel.Green,
                        Offset = Vector2.Zero,
                        WarpStrength = 0.2f,
                        RidgeAmount = 0f,
                        Threshold = 0.5f,
                        Softness = 0.2f,
                        FlatFloor = 0.1f
                    },
                    Appearance = new OverlayAppearance
                    {
                        Tint = Hue,
                        Opacity = Opacity,
                        Intensity = Intensity,
                        PulseFreq = 0.2f,
                        PulseAmp = 0.1f
                    }
                }
            }
        );
}
