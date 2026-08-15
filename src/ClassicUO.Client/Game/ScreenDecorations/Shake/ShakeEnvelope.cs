using System;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.ScreenDecorations.Shake;

/// <summary>
/// Turns a <see cref="ShakeRequest"/> plus an elapsed time into an amplitude. Kept apart from
/// <see cref="ScreenShake"/> so the envelope shapes can be reasoned about - and tested - without
/// the noise tables or the frame loop.
/// </summary>
internal static class ShakeEnvelope
{
    /// <summary>
    /// Trauma at <paramref name="elapsedSeconds"/> into the request, or zero once it is spent.
    /// </summary>
    public static float Evaluate(in ShakeRequest request, float elapsedSeconds)
    {
        float duration = (float)request.Duration.TotalSeconds;

        if (duration <= 0f || elapsedSeconds < 0f || elapsedSeconds > duration)
            return 0f;

        float amplitude = Gradient(request.Gradient, request.Curve, elapsedSeconds / duration);

        amplitude *= Ramp(request.Curve, elapsedSeconds, (float)request.RampUp.TotalSeconds);
        amplitude *= Ramp(request.Curve, duration - elapsedSeconds, (float)request.RampDown.TotalSeconds);

        return MathHelper.Clamp(request.Intensity, 0f, 1f) * amplitude;
    }

    /// <param name="distance">Time from the edge the ramp is anchored to.</param>
    /// <param name="window">Ramp length; non-positive leaves that edge abrupt.</param>
    private static float Ramp(ShakeCurve curve, float distance, float window)
    {
        if (window <= 0f)
            return 1f;

        return Shape(curve, MathHelper.Clamp(distance / window, 0f, 1f));
    }

    private static float Gradient(ShakeGradient gradient, ShakeCurve curve, float progress) =>
        gradient switch
        {
            ShakeGradient.Decay => 1f - Shape(curve, progress),
            ShakeGradient.Swell => Shape(curve, progress),
            ShakeGradient.Pulse => Shape(curve, 1f - MathF.Abs(2f * progress - 1f)),
            _ => 1f
        };

    private static float Shape(ShakeCurve curve, float t) =>
        curve switch
        {
            ShakeCurve.Smooth => t * t * (3f - 2f * t),
            ShakeCurve.EaseIn => t * t,
            ShakeCurve.EaseOut => t * (2f - t),
            _ => t
        };
}
