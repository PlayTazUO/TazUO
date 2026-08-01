using System;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.ScreenOverlays
{
    /// <summary>
    /// Trauma-model screen shake (Squirrel Eiserloh, GDC 2016). A CPU-side pixel offset applied to
    /// the render-target blit rectangle - not a shader effect. Only meaningful where such a
    /// rectangle exists; see <see cref="ClassicUO.GameController"/>'s render-target draw branch.
    /// </summary>
    internal sealed class ScreenShake
    {
        public static ScreenShake Instance { get; } = new();

        private const float DECAY_PER_SECOND = 1.2f;
        private const float MAX_OFFSET_PIXELS = 24f;
        private const float FREQUENCY = 20f;
        private const int NOISE_SAMPLES = 256;

        private readonly float[] _noiseX;
        private readonly float[] _noiseY;
        private float _trauma;
        private float _time;

        private ScreenShake()
        {
            _noiseX = BuildSmoothedNoise(1);
            _noiseY = BuildSmoothedNoise(2);
        }

        public void SetTrauma(float amount) => _trauma = MathHelper.Clamp(amount, 0f, 1f);

        public void AddTrauma(float amount) => _trauma = MathHelper.Clamp(_trauma + amount, 0f, 1f);

        /// <summary>
        /// Advances decay by <paramref name="dt"/> seconds and returns the current pixel offset,
        /// scaled by <paramref name="intensity"/> (the settings multiplier, already clamped to
        /// [0, 1] by the caller).
        /// </summary>
        public Point GetOffset(float dt, float intensity)
        {
            _trauma = MathHelper.Clamp(_trauma - DECAY_PER_SECOND * dt, 0f, 1f);
            _time += dt;

            if (_trauma <= 0f || intensity <= 0f)
                return Point.Zero;

            float shake = _trauma * _trauma;

            // Fixed offset between the two streams so X and Y never correlate.
            float x = MAX_OFFSET_PIXELS * shake * intensity * Sample(_noiseX, _time * FREQUENCY);
            float y = MAX_OFFSET_PIXELS * shake * intensity * Sample(_noiseY, _time * FREQUENCY + 37.1f);

            return new Point((int)MathF.Round(x), (int)MathF.Round(y));
        }

        private static float Sample(float[] table, float t)
        {
            int count = table.Length;
            float scaled = t % count;

            if (scaled < 0f)
                scaled += count;

            int i0 = (int)scaled;
            int i1 = (i0 + 1) % count;
            float frac = scaled - i0;

            return MathHelper.Lerp(table[i0], table[i1], frac);
        }

        // Precomputed, lightly-smoothed random samples. Sampled per frame, never regenerated -
        // uncorrelated per-frame randomness reads as buzzing, not shaking.
        private static float[] BuildSmoothedNoise(int seed)
        {
            float[] raw = new float[NOISE_SAMPLES];
            var rand = new Random(seed);

            for (int i = 0; i < NOISE_SAMPLES; i++)
                raw[i] = (float)(rand.NextDouble() * 2.0 - 1.0);

            float[] smoothed = new float[NOISE_SAMPLES];

            for (int i = 0; i < NOISE_SAMPLES; i++)
            {
                float prev = raw[(i - 1 + NOISE_SAMPLES) % NOISE_SAMPLES];
                float cur = raw[i];
                float next = raw[(i + 1) % NOISE_SAMPLES];
                smoothed[i] = (prev + 2f * cur + next) * 0.25f;
            }

            return smoothed;
        }
    }
}
