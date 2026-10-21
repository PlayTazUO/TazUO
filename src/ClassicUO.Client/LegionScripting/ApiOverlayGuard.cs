#nullable enable

using System;
using ClassicUO.LegionScripting.ApiClasses.VisualEffects;
using Microsoft.Xna.Framework;

namespace ClassicUO.LegionScripting;

/// <summary>
/// Conversions the visual effect DTOs share on their way to the internal config types.
/// <para>
/// Lives outside <c>ApiClasses/</c> deliberately: the documentation generator turns every class in
/// that directory into a page, and a helper with no script-visible members would be an empty one.
/// </para>
/// </summary>
internal static class ApiOverlayGuard
{
    /// <summary>
    /// Rejects an enum value a script invented, which a plain cast would carry into the shader as a
    /// technique or channel index nothing handles.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <param name="field">Name of the field being converted, for the message.</param>
    /// <typeparam name="T">The API-exported enum.</typeparam>
    /// <returns><paramref name="value"/>, when it names something.</returns>
    /// <exception cref="ArgumentException">The value is not one of the enum's members.</exception>
    internal static T Defined<T>(T value, string field) where T : struct, Enum
    {
        if (!Enum.IsDefined(value))
            throw new ArgumentException($"{field} is not a valid {typeof(T).Name} value ({value:D}).", field);

        return value;
    }

    /// <summary>
    /// A per-axis knob as the renderer wants it. Null reads as zero rather than throwing: a script
    /// clearing an axis pair is asking for no displacement, which is a legitimate thing to want.
    /// </summary>
    /// <param name="value">The pair, or null.</param>
    /// <returns>The vector.</returns>
    internal static Vector2 Vector(ApiVector2? value) => value == null ? Vector2.Zero : new Vector2((float)value.X, (float)value.Y);

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
}
