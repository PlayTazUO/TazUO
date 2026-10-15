#nullable enable

using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Profiles;
using ClassicUO.Game.ScreenDecorations.Shake;

namespace ClassicUO.LegionScripting.ApiClasses;

/// <summary>
/// How a visual effect arrives and leaves.
/// <para>
/// Onset reads as something happening, so it wants to be noticed; leaving is only the absence of
/// that, and a fast fade-out draws attention to the effect ending rather than to it being over.
/// Hence the client's own defaults are quick in, unhurried out.
/// </para>
/// </summary>
public class ApiFadeSpec
{
    #region Public accessors

    /// <summary>Seconds to reach full strength from nothing.</summary>
    public double InSeconds { get; set; } = _defaults.InSeconds;

    /// <summary>Seconds to fade away once nothing is asking for the effect any more.</summary>
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

    internal static ApiFadeSpec From(FadeSpec fade) => new() { InSeconds = fade.InSeconds, OutSeconds = fade.OutSeconds };

    internal FadeSpec ToInternal() => new() { InSeconds = (float)InSeconds, OutSeconds = (float)OutSeconds };

    #endregion
}

/// <summary>
/// Screen shake a visual effect includes, fired once as the effect arrives. Build one with
/// `API.NewShakeSpec` and hang it on `ApiEffectProfile.Shake`.
/// <para>
/// For a shake on its own, with no look attached, use `ApiShakeConfig` and `API.StartScreenShake`
/// instead. This type is the one that travels with a composition, so it has no scope of its own -
/// `ApiEffectProfile.FullScreen` decides which rectangle it displaces - and no gradient, since a
/// composition shapes its arc with the two ramps.
/// </para>
/// <para>
/// The shape is `Trauma x rampUp x rampDown`. An impact sets only the ramp down, so it starts at
/// full and decays; a quake sets both and leaves a hold between them, so it builds, holds, subsides.
/// </para>
/// </summary>
public class ApiShakeSpec
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
    public LegionAPI.ShakeCurve Curve { get; set; } = (LegionAPI.ShakeCurve)_defaults.Curve;

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

    internal static ApiShakeSpec From(ShakeSpec shake) =>
        new()
        {
            Trauma = shake.Trauma,
            DurationSeconds = shake.DurationSeconds,
            RampUpSeconds = shake.RampUpSeconds,
            RampDownSeconds = shake.RampDownSeconds,
            Curve = (LegionAPI.ShakeCurve)shake.Curve,
            Frequency = shake.Frequency
        };

    internal ShakeSpec ToInternal() =>
        new()
        {
            Trauma = (float)Trauma,
            DurationSeconds = (float)DurationSeconds,
            RampUpSeconds = (float)RampUpSeconds,
            RampDownSeconds = (float)RampDownSeconds,
            Curve = (ShakeCurve)ApiOverlayGuard.Defined(Curve, nameof(Curve)),
            Frequency = (float)Frequency
        };

    #endregion
}
