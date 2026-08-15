using System;
using ClassicUO.Game.ScreenDecorations.Shake;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.ScreenDecorations;

public class ShakeEnvelopeTests
{
    private static readonly TimeSpan OneSecond = TimeSpan.FromSeconds(1);

    [Fact]
    public void ConstantHoldsIntensityThroughout()
    {
        ShakeRequest request = ShakeRequest.Constant(OneSecond, 0.8f);

        ShakeEnvelope.Evaluate(request, 0f).Should().BeApproximately(0.8f, 1e-5f);
        ShakeEnvelope.Evaluate(request, 0.5f).Should().BeApproximately(0.8f, 1e-5f);
        ShakeEnvelope.Evaluate(request, 1f).Should().BeApproximately(0.8f, 1e-5f);
    }

    [Fact]
    public void DecayStartsFullAndEndsAtZero()
    {
        ShakeRequest request = ShakeRequest.Decay(OneSecond, 1f);

        ShakeEnvelope.Evaluate(request, 0f).Should().BeApproximately(1f, 1e-5f);
        ShakeEnvelope.Evaluate(request, 1f).Should().BeApproximately(0f, 1e-5f);
        ShakeEnvelope.Evaluate(request, 0.25f).Should().BeGreaterThan(ShakeEnvelope.Evaluate(request, 0.75f));
    }

    [Fact]
    public void SwellStartsAtZeroAndEndsFull()
    {
        ShakeRequest request = ShakeRequest.Swell(OneSecond, 1f);

        ShakeEnvelope.Evaluate(request, 0f).Should().BeApproximately(0f, 1e-5f);
        ShakeEnvelope.Evaluate(request, 1f).Should().BeApproximately(1f, 1e-5f);
        ShakeEnvelope.Evaluate(request, 0.75f).Should().BeGreaterThan(ShakeEnvelope.Evaluate(request, 0.25f));
    }

    [Fact]
    public void PulsePeaksAtTheMidpoint()
    {
        ShakeRequest request = ShakeRequest.Pulse(OneSecond, 1f);

        ShakeEnvelope.Evaluate(request, 0.5f).Should().BeApproximately(1f, 1e-5f);
        ShakeEnvelope.Evaluate(request, 0f).Should().BeApproximately(0f, 1e-5f);
        ShakeEnvelope.Evaluate(request, 1f).Should().BeApproximately(0f, 1e-5f);
    }

    [Fact]
    public void RampsFadeBothEndsWithoutTouchingTheMiddle()
    {
        ShakeRequest request = ShakeRequest.Constant(OneSecond, 1f);
        request.RampUp = TimeSpan.FromSeconds(0.2);
        request.RampDown = TimeSpan.FromSeconds(0.2);

        ShakeEnvelope.Evaluate(request, 0f).Should().BeApproximately(0f, 1e-5f);
        ShakeEnvelope.Evaluate(request, 0.1f).Should().BeApproximately(0.5f, 1e-5f);
        ShakeEnvelope.Evaluate(request, 0.5f).Should().BeApproximately(1f, 1e-5f);
        ShakeEnvelope.Evaluate(request, 0.9f).Should().BeApproximately(0.5f, 1e-5f);
        ShakeEnvelope.Evaluate(request, 1f).Should().BeApproximately(0f, 1e-5f);
    }

    [Fact]
    public void ZeroRampMeansAbruptStartAndStop()
    {
        ShakeRequest request = ShakeRequest.Constant(OneSecond, 1f);

        ShakeEnvelope.Evaluate(request, 0f).Should().BeApproximately(1f, 1e-5f);
        ShakeEnvelope.Evaluate(request, 1f).Should().BeApproximately(1f, 1e-5f);
    }

    [Fact]
    public void IsSpentOutsideItsDuration()
    {
        ShakeRequest request = ShakeRequest.Constant(OneSecond, 1f);

        ShakeEnvelope.Evaluate(request, 1.01f).Should().Be(0f);
        ShakeEnvelope.Evaluate(request, -0.01f).Should().Be(0f);
    }

    [Fact]
    public void NonPositiveDurationYieldsNothing()
    {
        ShakeRequest request = ShakeRequest.Constant(TimeSpan.Zero, 1f);

        ShakeEnvelope.Evaluate(request, 0f).Should().Be(0f);
    }

    [Fact]
    public void IntensityIsClampedToTraumaRange()
    {
        ShakeRequest request = ShakeRequest.Constant(OneSecond, 4f);

        ShakeEnvelope.Evaluate(request, 0.5f).Should().BeApproximately(1f, 1e-5f);
    }

    [Theory]
    [InlineData(ShakeCurve.Linear, 0.5f)]
    [InlineData(ShakeCurve.Smooth, 0.5f)]
    [InlineData(ShakeCurve.EaseIn, 0.25f)]
    [InlineData(ShakeCurve.EaseOut, 0.75f)]
    public void CurveShapesTheRamp(ShakeCurve curve, float expectedAtHalfRamp)
    {
        ShakeRequest request = ShakeRequest.Constant(OneSecond, 1f);
        request.RampUp = TimeSpan.FromSeconds(0.4);
        request.Curve = curve;

        ShakeEnvelope.Evaluate(request, 0.2f).Should().BeApproximately(expectedAtHalfRamp, 1e-5f);
    }
}
