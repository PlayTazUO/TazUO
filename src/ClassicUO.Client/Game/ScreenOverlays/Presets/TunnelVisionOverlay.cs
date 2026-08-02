using System.Collections.Generic;
using ClassicUO.Renderer.Effects;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.ScreenOverlays.Presets
{
    /// <summary>
    /// Flat, tightening black vignette. No noise field - motion comes only from the fade envelope
    /// and from <see cref="Reach"/> being adjusted by the caller over time.
    /// </summary>
    public sealed class TunnelVisionOverlay : ScreenOverlayPreset
    {
        public float Intensity { get; set; } = 1.0f;
        public float Opacity { get; set; } = 0.88f;
        public float Reach { get; set; } = 0.70f;

        protected override void Bake(List<OverlayLayer> layers) =>
            layers.Add(new OverlayLayer
            {
                Params = new OverlayParams
                {
                    Shape = new OverlayShape
                    {
                        Center = new Vector2(0.5f, 0.5f),
                        Reach = Reach,
                        Feather = 0.18f,
                        EdgeBlend = 0.00f,
                        FocusDir = new Vector2(0f, -1f),
                        FocusPower = 1f,
                        FocusAmount = 0f
                    },
                    Noise = new OverlayNoise
                    {
                        BaseScale = new Vector2(3f, 3f),
                        DetailScale = new Vector2(6f, 6f),
                        BaseScroll = Vector2.Zero,
                        DetailScroll = Vector2.Zero,
                        BaseChannel = NoiseChannel.Red,
                        DetailChannel = NoiseChannel.Green,
                        WarpStrength = 0.00f,
                        RidgeAmount = 0.00f,
                        Threshold = 0.5f,
                        Softness = 0.2f,
                        FlatFloor = 1.00f
                    },
                    Appearance = new OverlayAppearance
                    {
                        Tint = Color.Black,
                        Opacity = Opacity,
                        Intensity = Intensity,
                        PulseFreq = 0.05f,
                        PulseAmp = 0.02f
                    }
                }
            });
    }
}
