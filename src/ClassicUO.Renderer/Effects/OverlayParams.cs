using Microsoft.Xna.Framework;

namespace ClassicUO.Renderer.Effects
{
    /// <summary>
    /// Which packed channel of the noise texture a noise layer reads.
    /// </summary>
    public enum NoiseChannel
    {
        Red,
        Green,
        Blue,
        Alpha
    }

    public static class NoiseChannelExtensions
    {
        public static Vector4 ToSelector(this NoiseChannel channel)
        {
            switch (channel)
            {
                case NoiseChannel.Red: return new Vector4(1, 0, 0, 0);
                case NoiseChannel.Green: return new Vector4(0, 1, 0, 0);
                case NoiseChannel.Blue: return new Vector4(0, 0, 1, 0);
                default: return new Vector4(0, 0, 0, 1);
            }
        }
    }

    /// <summary>
    /// Where on screen the effect lives: vignette/border shape and directional focus.
    /// </summary>
    public struct OverlayShape
    {
        public Vector2 Center;
        public float Radius;
        public float Feather;
        public float EdgeBlend;
        public Vector2 FocusDir;
        public float FocusPower;
        public float FocusAmount;
    }

    /// <summary>
    /// How the effect moves: scrolling/warped noise field parameters.
    /// </summary>
    public struct OverlayNoise
    {
        public Vector2 Scale0, Scale1;
        public Vector2 Scroll0, Scroll1;
        public NoiseChannel Channel0, Channel1;
        public float WarpStrength;
        public float RidgeAmount;
        public float Threshold;
        public float Softness;
        public float Amount;
    }

    /// <summary>
    /// Colour and time-varying strength of the effect.
    /// </summary>
    public struct OverlayAppearance
    {
        public Color Tint;
        public float Opacity;
        public float Intensity;
        public float PulseFreq;
        public float PulseAmp;
    }

    public struct OverlayParams
    {
        // Flashing above ~3 Hz is a photosensitive-epilepsy hazard. Hard ceiling, not configurable
        // upward by any preset or setting.
        public const float MaxPulseFreqHz = 3.0f;

        private const float MinFeather = 0.01f;

        public OverlayShape Shape;
        public OverlayNoise Noise;
        public OverlayAppearance Appearance;

        public static OverlayParams Default => new OverlayParams
        {
            Shape = new OverlayShape
            {
                Center = new Vector2(0.5f, 0.5f),
                Radius = 0.4f,
                Feather = 0.3f,
                EdgeBlend = 0f,
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
                WarpStrength = 0.2f,
                RidgeAmount = 0f,
                Threshold = 0.5f,
                Softness = 0.2f,
                Amount = 1f
            },
            Appearance = new OverlayAppearance
            {
                Tint = Color.White,
                Opacity = 0.5f,
                Intensity = 1f,
                PulseFreq = 0f,
                PulseAmp = 0f
            }
        };

        /// <summary>
        /// Enforces the safety and sanity bounds every preset and every runtime override must obey.
        /// Mandatory before upload — never skip this to let a caller "just try a higher value".
        /// </summary>
        public void Clamp()
        {
            Appearance.PulseFreq = MathHelper.Clamp(Appearance.PulseFreq, 0f, MaxPulseFreqHz);
            Appearance.PulseAmp = MathHelper.Clamp(Appearance.PulseAmp, 0f, 1f);
            Appearance.Opacity = MathHelper.Clamp(Appearance.Opacity, 0f, 1f);
            Appearance.Intensity = MathHelper.Clamp(Appearance.Intensity, 0f, 1f);

            Noise.Amount = MathHelper.Clamp(Noise.Amount, 0f, 1f);
            Noise.RidgeAmount = MathHelper.Clamp(Noise.RidgeAmount, 0f, 1f);

            Shape.FocusAmount = MathHelper.Clamp(Shape.FocusAmount, 0f, 1f);
            Shape.Feather = MathHelper.Max(Shape.Feather, MinFeather);
        }
    }
}
