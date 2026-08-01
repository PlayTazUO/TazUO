using ClassicUO.Renderer.Effects;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.ScreenOverlays.Presets
{
    /// <summary>
    /// Flat, tightening black vignette. No noise field - motion comes only from the fade envelope
    /// and from <see cref="Radius"/> being adjusted by the caller over time.
    /// </summary>
    public sealed class TunnelVisionOverlay : ScreenOverlayPreset
    {
        public float Intensity { get; set; } = 1.0f;
        public float Opacity { get; set; } = 0.85f;
        public float Radius { get; set; } = 0.30f;

        protected override OverlayParams Bake() =>
            new()
            {
                Shape = new OverlayShape
                {
                    Center = new Vector2(0.5f, 0.5f),
                    Radius = Radius,
                    Feather = 0.18f,
                    EdgeBlend = 0.00f,
                    FocusDir = new Vector2(0f, -1f),
                    FocusPower = 1f,
                    FocusAmount = 0f
                },
                Noise = new OverlayNoise
                {
                    Scale0 = new Vector2(3f, 3f),
                    Scale1 = new Vector2(6f, 6f),
                    Scroll0 = Vector2.Zero,
                    Scroll1 = Vector2.Zero,
                    Channel0 = NoiseChannel.Red,
                    Channel1 = NoiseChannel.Green,
                    WarpStrength = 0.00f,
                    RidgeAmount = 0.00f,
                    Threshold = 0.5f,
                    Softness = 0.2f,
                    Amount = 0.00f
                },
                Appearance = new OverlayAppearance
                {
                    Tint = Color.Black,
                    Opacity = Opacity,
                    Intensity = Intensity,
                    PulseFreq = 0.12f,
                    PulseAmp = 0.08f
                }
            };
    }
}
