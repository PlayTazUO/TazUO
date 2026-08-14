using System;

namespace ClassicUO.Game.ScreenDecorations.Shake;

/// <summary>
/// Shape of a shake's ramps and of its gradient over time.
/// </summary>
public enum ShakeCurve
{
    Linear,

    /// <summary>Smoothstep - eased at both ends. Reads as the least mechanical of the four.</summary>
    Smooth,

    /// <summary>Slow to build, fast to finish.</summary>
    EaseIn,

    /// <summary>Fast to build, slow to finish.</summary>
    EaseOut
}

/// <summary>
/// How intensity travels across the shake's whole duration, independently of the ramps.
/// </summary>
public enum ShakeGradient
{
    /// <summary>Full intensity throughout. The default so that <c>default(ShakeRequest)</c> is inert
    /// only because its duration is zero, never because of a silently-chosen gradient.</summary>
    Constant,

    /// <summary>Starts at full intensity and falls to nothing - an impact.</summary>
    Decay,

    /// <summary>Builds from nothing to full intensity - something approaching.</summary>
    Swell,

    /// <summary>Builds to full intensity at the midpoint, then falls away.</summary>
    Pulse
}

/// <summary>
/// One shake's envelope. Amplitude at any moment is
/// <c>Intensity * gradient(progress) * rampUp(elapsed) * rampDown(remaining)</c>, so the gradient
/// decides the overall arc and the ramps decide how abruptly it starts and stops.
/// </summary>
public struct ShakeRequest
{
    /// <summary>Nothing is scheduled for a non-positive duration; use <see cref="ScreenShake.AddTrauma"/>
    /// for an instant hit that decays on its own.</summary>
    public TimeSpan Duration;

    /// <summary>Peak trauma, clamped to [0, 1]. Matches the scale used by
    /// <see cref="ScreenShake.SetTrauma"/>.</summary>
    public float Intensity;

    /// <summary>Fade-in window from the start. Zero means the shake is at full amplitude on the
    /// first frame.</summary>
    public TimeSpan RampUp;

    /// <summary>Fade-out window before the end. Zero means it stops dead.</summary>
    public TimeSpan RampDown;

    public ShakeGradient Gradient;

    /// <summary>Applied to both ramps and to the gradient.</summary>
    public ShakeCurve Curve;

    /// <summary>Shake rate in Hz; non-positive uses the default. Lower reads as a heavy rumble,
    /// higher as a rattle.</summary>
    public float Frequency;

    /// <summary>Peak displacement at full trauma; non-positive uses the default.</summary>
    public float MaxOffsetPixels;

    /// <summary>Even intensity for the whole duration.</summary>
    public static ShakeRequest Constant(TimeSpan duration, float intensity) =>
        new() { Duration = duration, Intensity = intensity, Gradient = ShakeGradient.Constant };

    /// <summary>Hits at full intensity and falls away - explosions, heavy landings.</summary>
    public static ShakeRequest Decay(TimeSpan duration, float intensity) =>
        new() { Duration = duration, Intensity = intensity, Gradient = ShakeGradient.Decay, Curve = ShakeCurve.EaseOut };

    /// <summary>Grows into full intensity - a charge-up, something drawing closer.</summary>
    public static ShakeRequest Swell(TimeSpan duration, float intensity) =>
        new() { Duration = duration, Intensity = intensity, Gradient = ShakeGradient.Swell, Curve = ShakeCurve.EaseIn };

    /// <summary>Rises to a peak halfway through, then falls.</summary>
    public static ShakeRequest Pulse(TimeSpan duration, float intensity) =>
        new() { Duration = duration, Intensity = intensity, Gradient = ShakeGradient.Pulse, Curve = ShakeCurve.Smooth };
}
