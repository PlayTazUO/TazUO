using System.Collections.Generic;
using ClassicUO.Renderer.Effects;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.ScreenDecorations.Overlays.Presets
{
    /// <summary>
    /// Billowy, drifting gas creeping in from the screen edge.
    /// </summary>
    public sealed class PoisonOverlay : ScreenOverlayPreset
    {
        public float Intensity { get; set; } = 1.0f;
        public Color Hue { get; set; } = new Color(96, 202, 74);
        public float Opacity { get; set; } = 0.65f;
        public float Reach { get; set; } = 0.58f;

        protected override void Bake(List<OverlayLayer> layers) =>
            layers.Add(new OverlayLayer
            {
                Params = new OverlayParams
                {
                    Shape = new OverlayShape
                    {
                        Center = new Vector2(0.5f, 0.5f),
                        Reach = Reach,
                        Feather = 0.50f,
                        EdgeBlend = 0.35f,
                        FocusDir = new Vector2(0f, -1f),
                        FocusPower = 1f,
                        FocusAmount = 0f
                    },
                    Noise = new OverlayNoise
                    {
                        BaseScale = new Vector2(2.5f, 2.5f),
                        DetailScale = new Vector2(5.0f, 5.0f),
                        BaseScroll = new Vector2(0.010f, -0.020f),
                        DetailScroll = new Vector2(-0.015f, -0.030f),
                        BaseChannel = NoiseChannel.Red,
                        DetailChannel = NoiseChannel.Green,
                        WarpStrength = 0.35f,
                        RidgeAmount = 0.00f,
                        Threshold = 0.45f,
                        Softness = 0.30f,
                        FlatFloor = 0.15f
                    },
                    Appearance = new OverlayAppearance
                    {
                        Tint = Hue,
                        Opacity = Opacity,
                        Intensity = Intensity,
                        PulseFreq = 0.35f,
                        PulseAmp = 0.25f
                    }
                }
            });
    }
}
