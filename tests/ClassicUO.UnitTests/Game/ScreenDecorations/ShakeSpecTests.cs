using System;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Profiles;
using ClassicUO.Game.ScreenDecorations.Shake;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.ScreenDecorations;

/// <summary>
/// A look owns the shape of its own shake, not just how hard it hits. The ramps are the whole of
/// that shape: it builds over the first window, holds, then falls away over the last.
/// </summary>
public class ShakeSpecTests
{
    private static float At(ShakeSpec spec, float seconds, float intensity = 1f)
    {
        return ShakeEnvelope.Evaluate(spec.ToRequest(intensity), seconds);
    }

    /// <summary>
    /// The default is an impact: peak on the first frame, falling from there. Exercised on the
    /// defaults themselves rather than an authored spec, since it is the shape every look that only
    /// sets a strength inherits.
    /// </summary>
    [Fact]
    public void TheDefaultEnvelopeIsAnImpact()
    {
        var spec = new ShakeSpec { Trauma = 1f };
        float duration = spec.DurationSeconds;

        // What makes it decay from the first frame rather than hold: nothing is left over for a hold.
        spec.RampDownSeconds.Should().Be(duration);
        spec.RampUpSeconds.Should().Be(0f);

        At(spec, 0f).Should().Be(1f);
        At(spec, duration * 0.5f).Should().BeLessThan(At(spec, 0f));
        At(spec, duration).Should().Be(0f);
    }

    /// <summary>
    /// The shape a quake wants: builds, holds at strength, then subsides. The hold is what the two
    /// windows leave between them, so it exists only where they do not span the whole duration.
    /// </summary>
    [Fact]
    public void RampsBuildToAHoldAndBackDown()
    {
        var spec = new ShakeSpec
        {
            Trauma = 1f,
            DurationSeconds = 4f,
            RampUpSeconds = 1f,
            RampDownSeconds = 1f,
            Curve = ShakeCurve.Linear
        };

        At(spec, 0f).Should().Be(0f);
        At(spec, 0.5f).Should().BeApproximately(0.5f, 1e-4f);

        // The hold: everything between the two ramps sits at full strength.
        At(spec, 1f).Should().BeApproximately(1f, 1e-4f);
        At(spec, 2f).Should().BeApproximately(1f, 1e-4f);
        At(spec, 3f).Should().BeApproximately(1f, 1e-4f);

        At(spec, 3.5f).Should().BeApproximately(0.5f, 1e-4f);
        At(spec, 4f).Should().Be(0f);
    }

    /// <summary>The occurrence scales the whole envelope and can only attenuate it.</summary>
    [Fact]
    public void OccurrenceIntensityScalesTheWholeEnvelope()
    {
        // Ramps cleared, so what is measured is the scaling rather than where in a ramp 0.5s lands.
        var spec = new ShakeSpec { Trauma = 1f, DurationSeconds = 1f, RampDownSeconds = 0f };

        At(spec, 0.5f, 0.25f).Should().BeApproximately(0.25f, 1e-4f);
        At(spec, 0.5f, 1f).Should().BeApproximately(1f, 1e-4f);
    }

    /// <summary>Trauma is a ceiling, so an occurrence cannot push a gentle look past what it
    /// authored.</summary>
    [Fact]
    public void TraumaIsTheCeiling()
    {
        var spec = new ShakeSpec { Trauma = 0.3f, DurationSeconds = 1f, RampDownSeconds = 0f };

        At(spec, 0.5f, 1f).Should().BeApproximately(0.3f, 1e-4f);
        spec.ToRequest(4f).Intensity.Should().Be(1f);
    }

    /// <summary>
    /// A profile shapes its arc with the ramps alone. Every gradient is reproducible with ramp
    /// windows, so a spec that also set one would be a second control over the same axis - free to
    /// disagree with the ramps beside it, and with no answer as to which was meant.
    /// </summary>
    [Fact]
    public void NoSpecEverAsksForAGradient()
    {
        new ShakeSpec { Trauma = 1f, DurationSeconds = 2f, RampUpSeconds = 0.5f }
            .ToRequest(1f)
            .Gradient
            .Should()
            .Be(ShakeGradient.Constant);
    }

    /// <summary>
    /// A ramp longer than the shake would otherwise never finish, leaving an envelope that only
    /// ever climbs. Capped, so an authored value cannot silently do nothing.
    /// </summary>
    [Fact]
    public void RampsAreCappedAtTheDuration()
    {
        var spec = new ShakeSpec
        {
            DurationSeconds = 2f,
            RampUpSeconds = 10f,
            RampDownSeconds = 10f
        };

        ShakeRequest request = spec.ToRequest(1f);

        request.RampUp.Should().Be(TimeSpan.FromSeconds(2));
        request.RampDown.Should().Be(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void NegativeValuesCannotReachTheAccumulator()
    {
        var spec = new ShakeSpec
        {
            Trauma = -1f,
            DurationSeconds = -5f,
            RampUpSeconds = -1f,
            RampDownSeconds = -1f,
            Frequency = -30f
        };

        ShakeRequest request = spec.ToRequest(1f);

        request.Duration.Should().Be(TimeSpan.Zero);
        request.Intensity.Should().Be(0f);
        request.RampUp.Should().Be(TimeSpan.Zero);
        request.RampDown.Should().Be(TimeSpan.Zero);
        request.Frequency.Should().Be(0f);
    }

    /// <summary>Rate is what separates a rumble from a rattle, so it has to reach the
    /// accumulator rather than being left at the shared default.</summary>
    [Fact]
    public void TheAuthoredRateReachesTheRequest()
    {
        new ShakeSpec { Frequency = 11f }.ToRequest(1f).Frequency.Should().Be(11f);
    }

    [Fact]
    public void CloneCarriesTheWholeEnvelope()
    {
        var original = new ShakeSpec
        {
            Trauma = 0.7f,
            DurationSeconds = 3f,
            RampUpSeconds = 0.5f,
            RampDownSeconds = 0.75f,
            Curve = ShakeCurve.Smooth,
            Frequency = 14f
        };

        ShakeSpec copy = original.Clone();
        copy.Should().BeEquivalentTo(original);

        copy.Trauma = 0.1f;
        original.Trauma.Should().Be(0.7f);
    }
}
