#nullable enable

using System;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Profiles;
using ClassicUO.Game.ScreenDecorations.Shake;

namespace ClassicUO.LegionScripting.ApiClasses.VisualEffects;

/// <summary>
///     How a visual effect arrives and leaves.
///     <para>
///         Onset reads as something happening, so it wants to be noticed; leaving is only the absence of
///         that, and a fast fade-out draws attention to the effect ending rather than to it being over.
///         Hence the client's own defaults are quick in, unhurried out.
///     </para>
///     <warn title="Photosensitive Epilepsy Warning">
///         Visual effects may contain flashing lights or rapid color changes.
///
///         If you have an epileptic condition or have had seizures of any kind, please consult your physician before
///         enabling the system or using the VFX APIs.
///
///         Note that while the in-game UI has some safeguards meant to reduce PSE risks, certain API usage patterns can
///         bypass those.
///
///         Near-zero fades make an effect appear and vanish at once, so starting and stopping one in a loop
///         strobes the whole screen.
///         Keep both above a tenth of a second unless the effect is started once and left to run.
///     </warn>
/// </summary>
public class ApiFadeSpec : PyApiObject
{
    #region Public accessors

    /// <summary>Seconds to reach full strength from nothing. Clamped to 0-86400; the client floors
    /// the effective fade at 0.01s.</summary>
    public double InSeconds { get; set; } = _defaults.InSeconds;

    /// <summary>Seconds to fade away once nothing is asking for the effect any more. Same range as
    /// `InSeconds`.</summary>
    public double OutSeconds { get; set; } = _defaults.OutSeconds;

    #endregion

    #region Private members

    /// <summary>
    /// The client's own timing, read rather than retyped so a script starting from a blank
    /// composition gets whatever the client currently considers right.
    /// </summary>
    private static readonly FadeSpec _defaults = new();

    #endregion

    #region Internal methods

    /// <summary>Projects an authored fade timing into a DTO.</summary>
    /// <param name="fade">The timing to copy.</param>
    /// <returns>The DTO.</returns>
    internal static ApiFadeSpec From(FadeSpec fade) => new() { InSeconds = fade.InSeconds, OutSeconds = fade.OutSeconds };

    /// <summary>Flattens this to the authoring type the compositor reads its envelope rate from.</summary>
    /// <returns>A fresh timing; mutating the DTO afterwards cannot reach it.</returns>
    /// <exception cref="ArgumentException">Either span is not a real number.</exception>
    internal FadeSpec ToInternal() =>
        new()
        {
            InSeconds = (float)VfxInputValidator.Seconds(InSeconds, nameof(InSeconds)),
            OutSeconds = (float)VfxInputValidator.Seconds(OutSeconds, nameof(OutSeconds))
        };

    #endregion
}

/// <summary>
/// Screen shake a visual effect includes, fired once as the effect arrives. Build one with
/// `API.VisualEffects.NewShakeSpec` and hang it on `ApiEffectProfile.Shake`.
/// <para>
/// For a shake on its own, with no look attached, use `ApiShakeConfig` and `API.VisualEffects.StartShake`
/// instead. This type is the one that travels with a composition, so it has no scope of its own -
/// `ApiEffectProfile.FullScreen` decides which rectangle it displaces - and no gradient, since a
/// composition shapes its arc with the two ramps.
/// </para>
/// <para>
/// The shape is `Trauma x rampUp x rampDown`. An impact sets only the ramp down, so it starts at
/// full and decays; a quake sets both and leaves a hold between them, so it builds, holds, subsides.
/// </para>
/// </summary>
public class ApiShakeSpec : PyApiObject
{
    #region Public accessors

    /// <summary>
    /// How far the screen moves at the peak, 0 to 1, before the effect's own intensity and the
    /// user's shake setting scale it down. Linear in pixels: 0.5 really is half as far as 1.
    /// </summary>
    public double Trauma { get; set; } = _defaults.Trauma;

    /// <summary>Total length of the shake in seconds, ramps included.</summary>
    public double DurationSeconds { get; set; } = _defaults.DurationSeconds;

    /// <summary>
    /// Seconds spent building to full strength. Zero starts at full amplitude on the first frame,
    /// which is what an impact wants and what anything building up does not.
    /// </summary>
    public double RampUpSeconds { get; set; } = _defaults.RampUpSeconds;

    /// <summary>
    /// Seconds spent falling away at the end. Zero stops dead, which reads as a jolt. Set it to the
    /// whole duration for an impact that decays from the first frame.
    /// </summary>
    public double RampDownSeconds { get; set; } = _defaults.RampDownSeconds;

    /// <summary>Easing applied to both ramps.</summary>
    public ApiVisualEffects.ShakeCurve Curve { get; set; } = (ApiVisualEffects.ShakeCurve)_defaults.Curve;

    /// <summary>Shake rate in Hz, or zero for the client default. Lower reads as a heavy rumble,
    /// higher as a rattle - it separates a quake from a hit far more than trauma does.</summary>
    public double Frequency { get; set; } = _defaults.Frequency;

    #endregion

    #region Private members

    /// <summary>
    /// The client's own envelope - an impact that decays - read rather than retyped so a script
    /// starting from a blank shake gets whatever the client currently considers right.
    /// </summary>
    private static readonly ShakeSpec _defaults = new();

    #endregion

    #region Internal methods

    /// <summary>Projects an authored shake into a DTO.</summary>
    /// <param name="shake">The shake to copy.</param>
    /// <returns>The DTO.</returns>
    internal static ApiShakeSpec From(ShakeSpec shake) =>
        new()
        {
            Trauma = shake.Trauma,
            DurationSeconds = shake.DurationSeconds,
            RampUpSeconds = shake.RampUpSeconds,
            RampDownSeconds = shake.RampDownSeconds,
            Curve = (ApiVisualEffects.ShakeCurve)shake.Curve,
            Frequency = shake.Frequency
        };

    /// <summary>Flattens this to the authoring type a composition carries.</summary>
    /// <returns>A fresh shake; mutating the DTO afterwards cannot reach it.</returns>
    /// <exception cref="ArgumentException">A value is not a real number, or <see cref="Curve"/> is
    /// set to something the API does not offer.</exception>
    internal ShakeSpec ToInternal() =>
        new()
        {
            Trauma = VfxInputValidator.Intensity(Trauma, nameof(Trauma)),
            DurationSeconds = (float)VfxInputValidator.Seconds(DurationSeconds, nameof(DurationSeconds)),
            RampUpSeconds = (float)VfxInputValidator.Seconds(RampUpSeconds, nameof(RampUpSeconds)),
            RampDownSeconds = (float)VfxInputValidator.Seconds(RampDownSeconds, nameof(RampDownSeconds)),
            Curve = (ShakeCurve)VfxInputValidator.Defined(Curve, nameof(Curve)),
            Frequency = VfxInputValidator.Number(Frequency, nameof(Frequency))
        };

    #endregion
}
