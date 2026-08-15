#nullable enable

using System;
using System.ComponentModel;
using System.Text.Json.Serialization;
using ClassicUO.Game.ScreenDecorations.Shake;
using Myra.Graphics2D.UI.Properties;

namespace ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Profiles;

/// <summary>
/// How a look arrives and leaves.
/// <para>
/// Onset is quicker than release, and both are unhurried. Arriving reads as something happening to
/// the player, so it wants to be noticed; leaving is only the absence of that, and a fast fade-out
/// draws attention to the effect ending rather than to being well again.
/// </para>
/// </summary>
public sealed class FadeSpec
{
    private const float DEFAULT_IN_SECONDS = 0.6f;
    private const float DEFAULT_OUT_SECONDS = 2f;

    /// <summary>Seconds to reach full strength from nothing.</summary>
    [Description("Seconds to reach full strength from nothing.")]
    public float InSeconds { get; set; } = DEFAULT_IN_SECONDS;

    /// <summary>Seconds to fade away once nothing is asking for the effect any more.</summary>
    [Description("Seconds to fade away once nothing is asking for the effect.")]
    public float OutSeconds { get; set; } = DEFAULT_OUT_SECONDS;

    /// <summary>Copy, so editing one profile's timing cannot write into another's.</summary>
    /// <returns>An independent copy.</returns>
    public FadeSpec Clone() => new() { InSeconds = InSeconds, OutSeconds = OutSeconds };
}

/// <summary>
/// Screen shake a look includes. Not a layer: shake is not drawn, and two composed shakes within one
/// look cannot mean anything.
/// <para>
/// Fired once, when the effect starts. Restating an occurrence that is already up does not re-hit
/// the player: a sustained effect that shook on every reconcile pass would be a rattle.
/// </para>
/// <para>
/// The shape is <c>Trauma x rampUp x rampDown</c>: it builds over the first window, holds, then
/// falls away over the last. A quake sets both and leaves a hold between them; an impact sets only
/// the ramp down, so it starts at full and decays. Every arc worth having is one of those two with
/// the windows moved, which is why there is no second shaping axis beside them.
/// </para>
/// </summary>
public sealed class ShakeSpec
{
    #region Private members

    private const float DEFAULT_TRAUMA = 0.5f;

    private const float DEFAULT_DURATION_SECONDS = 0.6f;

    /// <summary>
    /// Defaults to the whole duration, which is what shapes the default as an impact: full strength
    /// on the first frame, falling away to nothing by the end.
    /// </summary>
    private const float DEFAULT_RAMP_DOWN_SECONDS = DEFAULT_DURATION_SECONDS;

    #endregion

    #region Public accessors

    /// <summary>
    /// How hard it hits, as a fraction of the maximum displacement, before the occurrence's own
    /// intensity and the global shake setting scale it down. Linear in pixels: 0.5 really is half as
    /// far as 1.
    /// </summary>
    [LocalizedDisplayName("visualeffects_shake_trauma", "Strength")]
    [LocalizedDescription(
        "visualeffects_shake_trauma_tooltip",
        "How far the screen moves at the peak, from 0 to 1,\n"
        + "at full occurrence strength. 0.5 moves half as far as 1."
    )]
    public float Trauma { get; set; } = DEFAULT_TRAUMA;

    /// <summary>Total length of the shake, ramps included.</summary>
    [LocalizedDisplayName("visualeffects_shake_duration", "Duration (s)")]
    [LocalizedDescription(
        "visualeffects_shake_duration_tooltip",
        "Total length of the shake in seconds, ramps included."
    )]
    public float DurationSeconds { get; set; } = DEFAULT_DURATION_SECONDS;

    /// <summary>
    /// How long it takes to reach full strength. Zero starts at full amplitude on the first frame,
    /// which is what an impact wants and what anything building up does not.
    /// </summary>
    [LocalizedDisplayName("visualeffects_shake_rampup", "Ramp up (s)")]
    [LocalizedDescription(
        "visualeffects_shake_rampup_tooltip",
        "Seconds spent building to full strength.\n"
        + "Zero starts at full amplitude on the first frame -\n"
        + "right for an impact, wrong for anything that should be felt\n"
        + "approaching."
    )]
    public float RampUpSeconds { get; set; }

    /// <summary>
    /// How long it takes to fall away at the end. Zero stops dead, which reads as a jolt. Set to the
    /// whole duration for an impact that decays from the first frame.
    /// </summary>
    [LocalizedDisplayName("visualeffects_shake_rampdown", "Ramp down (s)")]
    [LocalizedDescription(
        "visualeffects_shake_rampdown_tooltip",
        "Seconds spent falling away at the end. Zero stops dead,\n"
        + "which reads as a jolt. Set it to the whole duration for\n"
        + "an impact that decays from the first frame."
    )]
    public float RampDownSeconds { get; set; } = DEFAULT_RAMP_DOWN_SECONDS;

    /// <summary>Easing applied to both ramps.</summary>
    [LocalizedDisplayName("visualeffects_shake_curve", "Easing")]
    [LocalizedDescription(
        "visualeffects_shake_curve_tooltip",
        "Easing applied to both ramps.\n"
        + "Smooth is the least mechanical; EaseIn holds then drops\n"
        + "sharply, EaseOut moves off the peak at once."
    )]
    public ShakeCurve Curve { get; set; } = ShakeCurve.EaseIn;

    /// <summary>
    /// Shake rate in Hz, or zero for the default. Lower reads as a heavy rumble, higher as a rattle
    /// - it is what separates a quake from a hit far more than trauma does.
    /// </summary>
    [LocalizedDisplayName("visualeffects_shake_frequency", "Rate (Hz)")]
    [LocalizedDescription(
        "visualeffects_shake_frequency_tooltip",
        "Shake rate in Hz; zero uses the default.\n"
        + "Lower reads as a heavy rumble, higher as a rattle."
    )]
    public float Frequency { get; set; }

    #endregion

    #region Public methods

    /// <summary>Copy, so editing one profile's shake cannot write into another's.</summary>
    /// <returns>An independent copy.</returns>
    public ShakeSpec Clone() =>
        new()
        {
            Trauma = Trauma,
            DurationSeconds = DurationSeconds,
            RampUpSeconds = RampUpSeconds,
            RampDownSeconds = RampDownSeconds,
            Curve = Curve,
            Frequency = Frequency
        };

    #endregion

    #region Internal methods

    /// <summary>
    /// Builds the request the accumulator takes, scaled by how strong this occurrence is.
    /// <para>
    /// Each ramp is capped at the duration so an authored value cannot silently do nothing. Ramps
    /// that overlap are left alone: they multiply, so the peak simply never reaches full, which is a
    /// legitimate short shake rather than a mistake to correct.
    /// </para>
    /// </summary>
    /// <param name="intensity">The occurrence's own strength, 0-1.</param>
    /// <returns>The request.</returns>
    internal ShakeRequest ToRequest(float intensity)
    {
        float duration = Math.Max(DurationSeconds, 0f);

        // Gradient is left at its default. A profile shapes its arc with the ramps alone - every
        // gradient is reproducible with ramp windows, so offering both would be two controls over
        // one axis, disagreeing whenever they were set to different things.
        return new ShakeRequest
        {
            Duration = TimeSpan.FromSeconds(duration),
            Intensity = Math.Clamp(Trauma * intensity, 0f, 1f),
            RampUp = TimeSpan.FromSeconds(Math.Clamp(RampUpSeconds, 0f, duration)),
            RampDown = TimeSpan.FromSeconds(Math.Clamp(RampDownSeconds, 0f, duration)),
            Curve = Curve,
            Frequency = Math.Max(Frequency, 0f)
        };
    }

    #endregion
}
