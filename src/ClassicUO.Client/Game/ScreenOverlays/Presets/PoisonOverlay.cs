using ClassicUO.Renderer.Effects;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.ScreenOverlays.Presets
{
    /// <summary>
    /// Billowy, drifting gas creeping in from the screen edge.
    /// </summary>
    public sealed class PoisonOverlay : ScreenOverlayPreset
    {
        public float Intensity { get; set; } = 1.0f;
        public Color Hue { get; set; } = new Color(96, 202, 74);
        public float Opacity { get; set; } = 0.65f;
        public float Radius { get; set; } = 0.42f;

        protected override OverlayParams Bake() =>
            new()
            {
                Shape = new OverlayShape
                {
                    Center = new Vector2(0.5f, 0.5f),
                    Radius = Radius,
                    Feather = 0.50f,
                    EdgeBlend = 0.35f,
                    FocusDir = new Vector2(0f, -1f),
                    FocusPower = 1f,
                    FocusAmount = 0f
                },
                Noise = new OverlayNoise
                {
                    Scale0 = new Vector2(2.5f, 2.5f),
                    Scale1 = new Vector2(5.0f, 5.0f),
                    Scroll0 = new Vector2(0.010f, -0.020f),
                    Scroll1 = new Vector2(-0.015f, -0.030f),
                    Channel0 = NoiseChannel.Red,
                    Channel1 = NoiseChannel.Green,
                    WarpStrength = 0.35f,
                    RidgeAmount = 0.00f,
                    Threshold = 0.45f,
                    Softness = 0.30f,
                    Amount = 0.85f
                },
                Appearance = new OverlayAppearance
                {
                    Tint = Hue,
                    Opacity = Opacity,
                    Intensity = Intensity,
                    PulseFreq = 0.35f,
                    PulseAmp = 0.25f
                }
            };
    }
}
