#nullable enable

using System;
using ClassicUO.Game.ScreenDecorations.Shake;

namespace ClassicUO.LegionScripting.ApiClasses;

/// <summary>
/// A screen shake with no look attached, for `API.StartScreenShake`. Build one with
/// `API.NewShakeConfig`.
/// <para>
/// Amplitude at any moment is `Intensity x gradient(progress) x rampUp(elapsed) x
/// rampDown(remaining)`, so `Gradient` decides the overall arc and the two ramps decide how abruptly
/// it starts and stops. Unlike `ApiShakeSpec`, which travels inside a composition, this one carries
/// its own scope and gradient because there is no composition to take them from.
/// </para>
/// </summary>
public class ApiShakeConfig
{
    #region Public accessors

    /// <summary>
    /// Total length of the shake in seconds, ramps included. Must be above zero - there is no
    /// open-ended shake, since a shake nothing is sustaining would never stop.
    /// </summary>
    public double Seconds { get; set; } = 1.0;

    /// <summary>
    /// Peak trauma, 0 to 1, before the user's own shake setting scales it down. Linear in pixels.
    /// </summary>
    public double Intensity { get; set; } = 0.5;

    /// <summary>Seconds spent building to full strength. Zero is at full amplitude on the first
    /// frame, which is what an impact wants.</summary>
    public double RampUpSeconds { get; set; }

    /// <summary>Seconds spent falling away at the end. Zero stops dead, which reads as a jolt.</summary>
    public double RampDownSeconds { get; set; }

    /// <summary>How intensity travels across the whole duration, independently of the ramps.</summary>
    public LegionAPI.ShakeGradient Gradient { get; set; }

    /// <summary>Easing applied to both ramps and to the gradient.</summary>
    public LegionAPI.ShakeCurve Curve { get; set; }

    /// <summary>Shake rate in Hz, or zero for the client default. Lower reads as a heavy rumble,
    /// higher as a rattle.</summary>
    public double Frequency { get; set; }

    /// <summary>Peak displacement in pixels at full trauma, or zero for the client default. Capped
    /// by the client whatever is asked for.</summary>
    public double MaxOffsetPixels { get; set; }

    /// <summary>
    /// Whether the shake displaces the whole window, UI included, rather than the game world alone.
    /// Window scope moves the gumps and the cursor with it, which makes them hard to use for as long
    /// as the shake lasts.
    /// </summary>
    public bool FullScreen { get; set; }

    #endregion

    #region Internal methods

    /// <summary>
    /// Builds the request the shake accumulator takes.
    /// </summary>
    /// <returns>The request.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><see cref="Seconds"/> is not above zero.</exception>
    internal ShakeRequest ToInternal()
    {
        if (Seconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(Seconds), Seconds, "A screen shake needs a duration above zero.");

        // Each ramp is capped at the duration so a value that could never be reached cannot
        // silently do nothing. Overlapping ramps are left alone: they multiply, so the peak simply
        // never reaches full, which is a legitimate short shake rather than a mistake to correct.
        return new ShakeRequest
        {
            Duration = TimeSpan.FromSeconds(Seconds),
            Intensity = (float)Math.Clamp(Intensity, 0d, 1d),
            RampUp = TimeSpan.FromSeconds(Math.Clamp(RampUpSeconds, 0d, Seconds)),
            RampDown = TimeSpan.FromSeconds(Math.Clamp(RampDownSeconds, 0d, Seconds)),
            Gradient = (ShakeGradient)ApiOverlayGuard.Defined(Gradient, nameof(Gradient)),
            Curve = (ShakeCurve)ApiOverlayGuard.Defined(Curve, nameof(Curve)),
            Frequency = (float)Math.Max(Frequency, 0d),
            MaxOffsetPixels = (float)Math.Max(MaxOffsetPixels, 0d)
        };
    }

    #endregion
}
