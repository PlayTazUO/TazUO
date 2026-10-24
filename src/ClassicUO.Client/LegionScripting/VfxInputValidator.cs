#nullable enable

using System;
using ClassicUO.LegionScripting.ApiClasses;
using Microsoft.Xna.Framework;

namespace ClassicUO.LegionScripting;

/// <summary>
/// Conversions the visual effect DTOs share on their way to the internal config types.
/// <para>
/// Lives outside <c>ApiClasses/</c> deliberately: the documentation generator turns every class in
/// that directory into a page, and a helper with no script-visible members would be an empty one.
/// </para>
/// </summary>
internal static class VfxInputValidator
{
    /// <summary>
    /// Longest span the API accepts, past which a value is taken as a slip rather than an intent.
    /// Anything longer is clamped to it: a day-long effect already outlives any session it was
    /// started in, and "until stopped" has its own spelling.
    /// </summary>
    internal const double MaxSeconds = 86400d;

    /// <summary>
    /// A script-supplied number as the renderer wants it.
    /// <para>
    /// Non-finite values are rejected rather than coerced, because there is no sane substitute and
    /// they do not stay contained: NaN survives every <c>MathHelper.Clamp</c> on the bake path, and
    /// a NaN fade reaches <c>MathF.Sign</c> inside the compositor's envelope advance, which throws.
    /// </para>
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="field">Name of the field being converted, for the message.</param>
    /// <returns><paramref name="value"/> narrowed to single precision.</returns>
    /// <exception cref="ArgumentException">The value is not a real number - it is infinity, the
    /// result of something like 0/0, or too large to draw with.</exception>
    internal static float Number(double value, string field)
    {
        float narrowed = (float)value;

        if (!float.IsFinite(narrowed))
        {
            throw new ArgumentException(
                $"{field} is {Describe(value)}, which cannot be drawn. It usually means a calculation "
                + "divided by zero or overflowed. Set it to an ordinary number.",
                field
            );
        }

        return narrowed;
    }

    /// <summary>
    /// A script-supplied duration, clamped to <see cref="MaxSeconds"/>. Negative reads as zero,
    /// which each caller gives its own meaning - "no timeout" for a run length, "no ramp" for a
    /// window.
    /// </summary>
    /// <param name="value">The span in seconds.</param>
    /// <param name="field">Name of the field being converted, for the message.</param>
    /// <returns>The span, within range.</returns>
    /// <exception cref="ArgumentException">The value is not a real number of seconds - it is
    /// infinity or the result of something like 0/0.</exception>
    internal static double Seconds(double value, string field)
    {
        if (!double.IsFinite(value))
        {
            // No advice about what 0 means here: this runs for durations, fade windows and shake
            // ramps alike, and 0 means something different to each of them.
            throw new ArgumentException(
                $"{field} is {Describe(value)}, which is not a length of time. It usually means a "
                + "calculation divided by zero or overflowed. Set it to a number of seconds.",
                field
            );
        }

        return Math.Clamp(value, 0d, MaxSeconds);
    }

    /// <summary>
    /// A script-supplied 0-1 strength. Clamped rather than rejected at the ends - an intensity above
    /// 1 is asking for "as strong as it goes" - but still rejected when non-finite, since nothing in
    /// that range is a nearest value.
    /// </summary>
    /// <param name="value">The strength to convert.</param>
    /// <param name="field">Name of the field being converted, for the message.</param>
    /// <returns>The strength, within 0-1.</returns>
    /// <exception cref="ArgumentException">The value is not a real number - it is infinity or the
    /// result of something like 0/0.</exception>
    internal static float Intensity(double value, string field) => Math.Clamp(Number(value, field), 0f, 1f);

    /// <summary>
    /// Rejects an enum value a script invented, which a plain cast would carry into the shader as a
    /// technique or channel index nothing handles.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <param name="field">Name of the field being converted, for the message.</param>
    /// <typeparam name="T">The API-exported enum.</typeparam>
    /// <returns><paramref name="value"/>, when it names something.</returns>
    /// <exception cref="ArgumentException">The value is not one of the choices the API offers.</exception>
    internal static T Defined<T>(T value, string field) where T : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            // The choices are listed rather than only named: a script that got here passed a bare
            // number, so pointing at the type by name would not tell it what to pass instead.
            throw new ArgumentException(
                $"{field} is set to {value:D}, which is not one of the available options. "
                + $"Use one of API.VisualEffects.{typeof(T).Name}: {string.Join(", ", Enum.GetNames<T>())}.",
                field
            );
        }

        return value;
    }

    /// <summary>
    /// A per-axis knob as the renderer wants it. Null reads as zero rather than throwing: a script
    /// clearing an axis pair is asking for no displacement, which is a legitimate thing to want.
    /// </summary>
    /// <param name="value">The pair, or null.</param>
    /// <param name="field">Name of the field being converted, for the message.</param>
    /// <returns>The vector.</returns>
    /// <exception cref="ArgumentException">Either axis is not a real number.</exception>
    internal static Vector2 Vector(ApiVector2? value, string field) =>
        value == null
            ? Vector2.Zero
            : new Vector2(Number(value.X, $"{field}.X"), Number(value.Y, $"{field}.Y"));

    /// <summary>
    /// Builds a colour from script-supplied channels. Clamped rather than rejected - a channel out
    /// of range is an arithmetic slip in the script, and refusing to draw over it would be a worse
    /// answer than showing the nearest colour.
    /// </summary>
    /// <param name="red">Red channel, 0-255.</param>
    /// <param name="green">Green channel, 0-255.</param>
    /// <param name="blue">Blue channel, 0-255.</param>
    /// <param name="alpha">Alpha channel, 0-255.</param>
    /// <returns>The colour.</returns>
    internal static Color Rgba(int red, int green, int blue, int alpha) =>
        new(Channel(red), Channel(green), Channel(blue), Channel(alpha));

    private static byte Channel(int value) => (byte)Math.Clamp(value, byte.MinValue, byte.MaxValue);

    /// <summary>
    /// Names a rejected value in words. "NaN" and "Infinity" read as jargon to a script author, and
    /// a number too big for the renderer prints as something they never typed.
    /// </summary>
    /// <param name="value">The rejected value.</param>
    /// <returns>The description.</returns>
    private static string Describe(double value) =>
        double.IsNaN(value) ? "not a number"
        : double.IsPositiveInfinity(value) ? "infinity"
        : double.IsNegativeInfinity(value) ? "negative infinity"
        : $"too large a number ({value})";
}
