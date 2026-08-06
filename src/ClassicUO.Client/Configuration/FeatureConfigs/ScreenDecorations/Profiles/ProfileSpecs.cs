#nullable enable

using System;
using System.ComponentModel;
using System.Text.Json.Serialization;
using ClassicUO.Game.ScreenDecorations.Shake;

namespace ClassicUO.Configuration.FeatureConfigs.ScreenDecorations;

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
/// The shape is <c>Trauma x gradient(progress) x rampUp x rampDown</c>. The gradient is the overall
/// arc; the ramps decide only how abruptly it starts and stops. A quake wants
/// <see cref="ShakeGradient.Constant"/> with both ramps set - it builds, holds at strength, and
/// drops away - where an impact wants <see cref="ShakeGradient.Decay"/> with no ramp at all.
/// </para>
/// </summary>
public sealed class ShakeSpec
{
    #region Private members

    private const float DEFAULT_TRAUMA = 0.5f;

    /// <summary>Shaped as an impact by default: full strength immediately, falling away.</summary>
    private const float DEFAULT_DURATION_SECONDS = 0.6f;

    #endregion

    #region Public accessors

    /// <summary>
    /// How hard it hits, 0-1, before the occurrence's own intensity and the global shake setting
    /// scale it down.
    /// </summary>
    [Description("How hard the shake hits, 0-1, at full occurrence strength.")]
    public float Trauma { get; set; } = DEFAULT_TRAUMA;

    /// <summary>Total length of the shake, ramps included.</summary>
    [Description("Total length of the shake in seconds, ramps included.")]
    public float DurationSeconds { get; set; } = DEFAULT_DURATION_SECONDS;

    /// <summary>
    /// How long it takes to reach full strength. Zero starts at full amplitude on the first frame,
    /// which is what an impact wants and what anything building up does not.
    /// </summary>
    [Description(
        "Seconds spent building to full strength. Zero starts at full\n"
        + "amplitude on the first frame - right for an impact, wrong for\n"
        + "anything that should be felt approaching."
    )]
    public float RampUpSeconds { get; set; }

    /// <summary>
    /// How long it takes to fall away at the end. Zero stops dead, which is audible as a click in
    /// the motion unless the gradient has already brought it to nothing.
    /// </summary>
    [Description(
        "Seconds spent falling away at the end. Zero stops dead, which\n"
        + "reads as a jolt unless the gradient already brought it to\n"
        + "nothing."
    )]
    public float RampDownSeconds { get; set; }

    /// <summary>The overall arc across the whole duration, independent of the ramps.</summary>
    [Description(
        "The arc across the whole duration, independent of the ramps.\n"
        + "Constant holds at strength; Decay starts at peak and falls;\n"
        + "Swell builds; Pulse peaks halfway."
    )]
    public ShakeGradient Gradient { get; set; } = ShakeGradient.Decay;

    /// <summary>Easing applied to both ramps and to the gradient.</summary>
    [Description(
        "Easing applied to both ramps and to the gradient. Smooth is\n"
        + "the least mechanical; EaseIn is slow to build, EaseOut slow\n"
        + "to finish."
    )]
    public ShakeCurve Curve { get; set; } = ShakeCurve.EaseOut;

    /// <summary>
    /// Shake rate in Hz, or zero for the default. Lower reads as a heavy rumble, higher as a rattle
    /// - it is what separates a quake from a hit far more than trauma does.
    /// </summary>
    [Description(
        "Shake rate in Hz; zero uses the default. Lower reads as a\n"
        + "heavy rumble, higher as a rattle."
    )]
    public float Frequency { get; set; }

    /// <summary>The configured length as a span, floored at zero.</summary>
    [JsonIgnore]
    [Browsable(false)]
    public TimeSpan Duration => TimeSpan.FromSeconds(Math.Max(DurationSeconds, 0f));

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
            Gradient = Gradient,
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

        return new ShakeRequest
        {
            Duration = TimeSpan.FromSeconds(duration),
            Intensity = Math.Clamp(Trauma * intensity, 0f, 1f),
            RampUp = TimeSpan.FromSeconds(Math.Clamp(RampUpSeconds, 0f, duration)),
            RampDown = TimeSpan.FromSeconds(Math.Clamp(RampDownSeconds, 0f, duration)),
            Gradient = Gradient,
            Curve = Curve,
            Frequency = Math.Max(Frequency, 0f)
        };
    }

    #endregion
}
