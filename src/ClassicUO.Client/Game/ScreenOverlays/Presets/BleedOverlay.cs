using ClassicUO.Renderer.Effects;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.ScreenOverlays.Presets;

/// <summary>
///     Fluid, downward-streaking red trim around the screen border.
/// </summary>
public sealed class BleedOverlay : ScreenOverlayPreset
{
    public float Intensity { get; set; } = 1.0f;
    public Color Hue { get; set; } = new(187, 14, 26);
    public float Opacity { get; set; } = 0.75f;
    public float Radius { get; set; } = 0.50f;

    protected override OverlayParams Bake() =>
        new()
        {
            Shape = new OverlayShape
            {
                Center = new Vector2(0.5f, 0.5f),
                Radius = Radius,
                Feather = 0.35f,
                EdgeBlend = 0.80f,
                FocusDir = new Vector2(0f, -1f),
                FocusPower = 1f,
                FocusAmount = 0f
            },
            Noise = new OverlayNoise
            {
                Scale0 = new Vector2(3.0f, 2.0f),
                Scale1 = new Vector2(5.0f, 3.5f),
                // Negative V scroll: increasing V samples further down the texture, which pulls
                // rows in from below and reads on screen as downward flow, not upward.
                Scroll0 = new Vector2(0.007f, -0.032f),
                Scroll1 = new Vector2(-0.009f, -0.055f),
                Channel0 = NoiseChannel.Red,
                Channel1 = NoiseChannel.Green,
                WarpStrength = 0.32f,
                RidgeAmount = 0.18f,
                Threshold = 0.55f,
                Softness = 0.18f,
                Amount = 0.90f
            },
            Appearance = new OverlayAppearance
            {
                Tint = Hue,
                Opacity = Opacity,
                Intensity = Intensity,
                // No pulsation - a continuous fluid flow shouldn't flash/breathe.
                PulseFreq = 0.00f,
                PulseAmp = 0.00f
            }
        };
}
