using ClassicUO.Renderer;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Renderer;

public class PeriodicNoiseTests
{
    private const float Epsilon = 0.0001f;

    [Theory]
    [InlineData(0f, 0f, 8, 1)]
    [InlineData(2.3f, 5.7f, 8, 1)]
    [InlineData(1.1f, 6.9f, 4, 42)]
    [InlineData(3.5f, 3.5f, 16, 7)]
    public void Perlin_TilesOnX(float x, float y, int period, int seed)
    {
        float a = PeriodicNoise.Perlin(x, y, period, seed);
        float b = PeriodicNoise.Perlin(x + period, y, period, seed);

        b.Should().BeApproximately(a, Epsilon);
    }

    [Theory]
    [InlineData(0f, 0f, 8, 1)]
    [InlineData(2.3f, 5.7f, 8, 1)]
    [InlineData(1.1f, 6.9f, 4, 42)]
    [InlineData(3.5f, 3.5f, 16, 7)]
    public void Perlin_TilesOnY(float x, float y, int period, int seed)
    {
        float a = PeriodicNoise.Perlin(x, y, period, seed);
        float b = PeriodicNoise.Perlin(x, y + period, period, seed);

        b.Should().BeApproximately(a, Epsilon);
    }

    [Theory]
    [InlineData(0.1f, 0.4f, 4, 4, 1)]
    [InlineData(0.73f, 0.02f, 8, 4, 99)]
    public void Fbm_TilesAcrossUvPeriod1(float u, float v, int basePeriod, int octaves, int seed)
    {
        float a = PeriodicNoise.Fbm(u, v, basePeriod, octaves, seed);
        float b = PeriodicNoise.Fbm(u + 1f, v, basePeriod, octaves, seed);
        float c = PeriodicNoise.Fbm(u, v + 1f, basePeriod, octaves, seed);

        b.Should().BeApproximately(a, Epsilon);
        c.Should().BeApproximately(a, Epsilon);
    }

    [Theory]
    [InlineData(0.1f, 0.4f, 8, 1)]
    [InlineData(0.66f, 0.9f, 8, 99)]
    public void Worley_TilesAcrossUvPeriod1(float u, float v, int period, int seed)
    {
        float a = PeriodicNoise.Worley(u, v, period, seed);
        float b = PeriodicNoise.Worley(u + 1f, v, period, seed);
        float c = PeriodicNoise.Worley(u, v + 1f, period, seed);

        b.Should().BeApproximately(a, Epsilon);
        c.Should().BeApproximately(a, Epsilon);
    }

    [Fact]
    public void Perlin_StaysWithinUnitRange()
    {
        for (int i = 0; i < 200; i++)
        {
            float x = i * 0.37f;
            float y = i * 0.61f;
            float n = PeriodicNoise.Perlin(x, y, 8, 3);

            n.Should().BeInRange(0f, 1f);
        }
    }

    [Fact]
    public void Ridge_MapsMidpointToOne()
    {
        PeriodicNoise.Ridge(0.5f).Should().BeApproximately(1f, Epsilon);
        PeriodicNoise.Ridge(0f).Should().BeApproximately(0f, Epsilon);
        PeriodicNoise.Ridge(1f).Should().BeApproximately(0f, Epsilon);
    }
}
