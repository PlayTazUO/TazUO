using ClassicUO.Renderer.Effects;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.ScreenOverlays.Presets
{
    /// <summary>
    /// Sharp static crack pattern. Deliberately zero scroll - its life comes from the fade-in
    /// envelope, not from motion.
    /// </summary>
    public sealed class FractureOverlay : ScreenOverlayPreset
    {
        public float Intensity { get; set; } = 1.0f;
        public Color Hue { get; set; } = new Color(190, 210, 225);
        public float Opacity { get; set; } = 0.45f;
        public float Radius { get; set; } = 0.25f;

        protected override OverlayParams Bake() =>
            new()
            {
                Shape = new OverlayShape
                {
                    Center = new Vector2(0.5f, 0.5f),
                    Radius = Radius,
                    Feather = 0.50f,
                    EdgeBlend = 1.00f,
                    FocusDir = new Vector2(0f, -1f),
                    FocusPower = 1f,
                    FocusAmount = 0f
                },
                Noise = new OverlayNoise
                {
                    Scale0 = new Vector2(1.5f, 1.5f),
                    Scale1 = new Vector2(1.8f, 1.8f),
                    Scroll0 = Vector2.Zero,
                    Scroll1 = Vector2.Zero,
                    Channel0 = NoiseChannel.Blue,
                    Channel1 = NoiseChannel.Alpha,
                    WarpStrength = 0.08f,
                    RidgeAmount = 0.60f,
                    Threshold = 0.80f,
                    Softness = 0.05f,
                    Amount = 1.00f
                },
                Appearance = new OverlayAppearance
                {
                    Tint = Hue,
                    Opacity = Opacity,
                    Intensity = Intensity,
                    PulseFreq = 0.00f,
                    PulseAmp = 0.00f
                }
            };
    }
}
