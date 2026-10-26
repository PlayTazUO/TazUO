#nullable enable

using System;
using ClassicUO.Game.ScreenDecorations.Shake;

namespace ClassicUO.LegionScripting.ApiClasses.VisualEffects;

/// <summary>
/// A screen shake with no look attached, for `API.VisualEffects.StartShake`. Build one with
/// `API.VisualEffects.NewShakeConfig`.
/// <para>
/// Amplitude at any moment is `Intensity x gradient(progress) x rampUp(elapsed) x
/// rampDown(remaining)`, so `Gradient` decides the overall arc and the two ramps decide how abruptly
/// it starts and stops. Unlike `ApiShakeSpec`, which travels inside a composition, this one carries
/// its own scope and gradient because there is no composition to take them from.
/// </para>
/// </summary>
public class ApiShakeConfig : PyApiObject
{
    #region Public accessors

    /// <summary>
    /// Total length of the shake in seconds, ramps included. Must be above zero - there is no
    /// open-ended shake, since a shake nothing is sustaining would never stop - and clamped to
    /// 86400 at the top.
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
    public ApiVisualEffects.ShakeGradient Gradient { get; set; }

    /// <summary>Easing applied to both ramps and to the gradient.</summary>
    public ApiVisualEffects.ShakeCurve Curve { get; set; }

    /// <summary>Shake rate in Hz, or zero for the client default. Lower reads as a heavy rumble,
    /// higher as a rattle.</summary>
    public double Frequency { get; set; }

    /// <summary>Peak displacement in pixels at full trauma, 0 to 24. Zero takes the client's own 24,
    /// and anything above it is clamped down: 24 is what the world render target is padded by, so a
    /// larger offset would shake past the pixels there are and expose the window's empty edge.</summary>
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
    /// <exception cref="ArgumentException">A value is not a real number, or <see cref="Gradient"/>
    /// or <see cref="Curve"/> is set to something the API does not offer.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><see cref="Seconds"/> is zero or less.</exception>
    internal ShakeRequest ToInternal()
    {
        // Bounded before the comparison, so a non-finite duration cannot slip past it into
        // TimeSpan.FromSeconds, which throws on NaN and overflows well below double's range.
        double duration = VfxInputValidator.Seconds(Seconds, nameof(Seconds));

        if (duration <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(Seconds),
                Seconds,
                "Set Seconds to how long the shake should last. A shake has no 'until stopped' setting - "
                + "nothing would ever end it - so it needs a length above zero."
            );
        }

        // Each ramp is capped at the duration so a value that could never be reached cannot
        // silently do nothing. Overlapping ramps are left alone: they multiply, so the peak simply
        // never reaches full, which is a legitimate short shake rather than a mistake to correct.
        return new ShakeRequest
        {
            Duration = TimeSpan.FromSeconds(duration),
            Intensity = VfxInputValidator.Intensity(Intensity, nameof(Intensity)),
            RampUp = TimeSpan.FromSeconds(Math.Min(VfxInputValidator.Seconds(RampUpSeconds, nameof(RampUpSeconds)), duration)),
            RampDown = TimeSpan.FromSeconds(Math.Min(VfxInputValidator.Seconds(RampDownSeconds, nameof(RampDownSeconds)), duration)),
            Gradient = (ShakeGradient)VfxInputValidator.Defined(Gradient, nameof(Gradient)),
            Curve = (ShakeCurve)VfxInputValidator.Defined(Curve, nameof(Curve)),
            Frequency = Math.Max(VfxInputValidator.Number(Frequency, nameof(Frequency)), 0f),

            // Clamped to the ceiling the renderer is built for, not just to a sane-looking number:
            // window-scoped shake offsets the screen blit with nothing padding it, so an unbounded
            // offset slides the whole frame off the backbuffer and leaves black.
            MaxOffsetPixels = Math.Clamp(
                VfxInputValidator.Number(MaxOffsetPixels, nameof(MaxOffsetPixels)),
                0f,
                ScreenShake.MaxOffsetPixels
            )
        };
    }

    #endregion
}
