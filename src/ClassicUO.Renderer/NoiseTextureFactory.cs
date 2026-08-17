using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Renderer;

/// <summary>
/// Tileable Perlin/Worley noise. Every lattice coordinate is wrapped to a fixed period before
/// hashing, so Noise(x, y) == Noise(x + period, y) == Noise(x, y + period) exactly. This is what
/// lets the generated texture scroll forever with no seam.
/// </summary>
public static class PeriodicNoise
{
    private static readonly Vector2[] _gradients =
    [
        new(1, 0), new(-1, 0), new(0, 1), new(0, -1),
        new(1, 1), new(-1, 1), new(1, -1), new(-1, -1)
    ];

    public static int FloorMod(int a, int m)
    {
        int r = a % m;
        return r < 0 ? r + m : r;
    }

    private static uint Hash(int x, int y, int seed)
    {
        unchecked
        {
            uint h = (uint)(x * 374761393 + y * 668265263 + seed * 2058325987);
            h = (h ^ (h >> 13)) * 1274126177u;
            h ^= h >> 16;
            return h;
        }
    }

    private static float Fade(float t) => t * t * t * (t * (t * 6f - 15f) + 10f);

    private static float Lerp(float a, float b, float t) => a + t * (b - a);

    private static float GradDot(int latticeX, int latticeY, int period, int seed, float dx, float dy)
    {
        uint h = Hash(FloorMod(latticeX, period), FloorMod(latticeY, period), seed);
        Vector2 g = _gradients[h % _gradients.Length];
        return g.X * dx + g.Y * dy;
    }

    /// <summary>
    /// Classic Perlin gradient noise on a lattice wrapped to <paramref name="period"/>, returned
    /// in [0, 1]. <paramref name="x"/>/<paramref name="y"/> are lattice-space coordinates, i.e.
    /// already multiplied by the desired frequency.
    /// </summary>
    public static float Perlin(float x, float y, int period, int seed)
    {
        int x0 = (int)MathF.Floor(x);
        int y0 = (int)MathF.Floor(y);

        float sx = x - x0;
        float sy = y - y0;

        float n00 = GradDot(x0, y0, period, seed, sx, sy);
        float n10 = GradDot(x0 + 1, y0, period, seed, sx - 1f, sy);
        float n01 = GradDot(x0, y0 + 1, period, seed, sx, sy - 1f);
        float n11 = GradDot(x0 + 1, y0 + 1, period, seed, sx - 1f, sy - 1f);

        float u = Fade(sx);
        float v = Fade(sy);

        float nx0 = Lerp(n00, n10, u);
        float nx1 = Lerp(n01, n11, u);

        float n = Lerp(nx0, nx1, v);
        return MathHelper.Clamp(n * 0.5f + 0.5f, 0f, 1f);
    }

    /// <summary>
    /// Fractal Brownian motion: sum of <paramref name="octaves"/> Perlin layers, each doubling in
    /// frequency and period so every octave — and therefore the sum — tiles at uv period 1.
    /// </summary>
    public static float Fbm(float u, float v, int basePeriod, int octaves, int seed)
    {
        float sum = 0f;
        float amplitude = 0.5f;
        float norm = 0f;

        for (int i = 0; i < octaves; i++)
        {
            int period = basePeriod << i;
            sum += Perlin(u * period, v * period, period, seed + i * 101) * amplitude;
            norm += amplitude;
            amplitude *= 0.5f;
        }

        return norm > 0f ? sum / norm : 0f;
    }

    /// <summary>
    /// 1 - |2n - 1|, folding billowy fbm into sharp ridge/filament structures.
    /// </summary>
    public static float Ridge(float n) => 1f - MathF.Abs(n * 2f - 1f);

    /// <summary>Wraps the cell before hashing, as <see cref="GradDot"/> does for lattice points -
    /// the wrap is what makes opposite edges of the tile resolve to the same feature point.</summary>
    private static Vector2 CellFeaturePoint(int cellX, int cellY, int period, int seed)
    {
        uint h = Hash(FloorMod(cellX, period), FloorMod(cellY, period), seed);
        float fx = (h & 0xFFFF) / 65535f;
        float fy = ((h >> 16) & 0xFFFF) / 65535f;
        return new Vector2(fx, fy);
    }

    /// <summary>
    /// Worley cellular noise, returned as F2 - F1 (distance to second-nearest feature point minus
    /// distance to nearest), wrapped to <paramref name="period"/> cells across uv 0..1.
    /// </summary>
    public static float Worley(float u, float v, int period, int seed)
    {
        float x = u * period;
        float y = v * period;
        int cx = (int)MathF.Floor(x);
        int cy = (int)MathF.Floor(y);

        float f1 = float.MaxValue;
        float f2 = float.MaxValue;

        for (int oy = -1; oy <= 1; oy++)
        {
            for (int ox = -1; ox <= 1; ox++)
            {
                int cellX = cx + ox;
                int cellY = cy + oy;
                Vector2 fp = CellFeaturePoint(cellX, cellY, period, seed);

                float px = cellX + fp.X;
                float py = cellY + fp.Y;
                float dx = px - x;
                float dy = py - y;
                float dist = MathF.Sqrt(dx * dx + dy * dy);

                if (dist < f1)
                {
                    f2 = f1;
                    f1 = dist;
                }
                else if (dist < f2)
                {
                    f2 = dist;
                }
            }
        }

        return MathHelper.Clamp(f2 - f1, 0f, 1f);
    }
}

/// <summary>
/// Builds the single tiling noise texture consumed by <see cref="ClassicUO.Renderer.Effects.ScreenOverlayEffect"/>. One
/// 256x256 <see cref="SurfaceFormat.Color"/> texture packing four independent tileable layers,
/// one per channel.
/// </summary>
public static class NoiseTextureFactory
{
    public const int TextureSize = 256;

    // Fixed, not time-derived: a constant seed keeps visual regressions reproducible.
    private const int SEED = 20260801;

    private const int GAS_BASE_PERIOD = 4;
    private const int DETAIL_BASE_PERIOD = 8;
    private const int RIDGE_BASE_PERIOD = 4;
    private const int WORLEY_PERIOD = 8;
    private const int OCTAVES = 4;

    public static Texture2D Create(GraphicsDevice device)
    {
        var pixels = new Color[TextureSize * TextureSize];

        for (int y = 0; y < TextureSize; y++)
        {
            float v = y / (float)TextureSize;

            for (int x = 0; x < TextureSize; x++)
            {
                float u = x / (float)TextureSize;

                float r = PeriodicNoise.Fbm(u, v, GAS_BASE_PERIOD, OCTAVES, SEED);
                float g = PeriodicNoise.Fbm(u, v, DETAIL_BASE_PERIOD, OCTAVES, SEED + 1000);

                float ridgeFbm = PeriodicNoise.Fbm(u, v, RIDGE_BASE_PERIOD, OCTAVES, SEED + 2000);
                float b = PeriodicNoise.Ridge(ridgeFbm);

                // Inverted: F2-F1 is ~0 AT cell boundaries and grows toward cell centers, so a
                // raw store would light up interiors, not edges. Cracks need the edges bright.
                float a = 1f - PeriodicNoise.Worley(u, v, WORLEY_PERIOD, SEED + 3000);

                pixels[y * TextureSize + x] = new Color(r, g, b, a);
            }
        }

        var texture = new Texture2D(device, TextureSize, TextureSize, false, SurfaceFormat.Color);
        texture.SetData(pixels);
        return texture;
    }
}
