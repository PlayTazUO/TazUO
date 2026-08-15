#nullable enable

using System;

namespace ClassicUO.Game.ScreenDecorations.Triggers;

/// <summary>How proximity is turned into intensity.</summary>
public enum FalloffCurve
{
    /// <summary>Distance only filters; everything inside the band is full strength.</summary>
    Flat,

    /// <summary>Strength falls evenly across the band.</summary>
    Linear,

    /// <summary>Squared. Most of the scale is spent on the tiles nearest the player, which is what
    /// keeps everything in sight from feeling much the same.</summary>
    Quadratic,

    /// <summary>Cubed. Quadratic, more so - all but the closest few tiles read as faint.</summary>
    Cubic,

    /// <summary>Square root. Falls off slowly, so something at the far edge of the band still
    /// registers strongly.</summary>
    SquareRoot,

    /// <summary>Raised to <see cref="ProximityMath.Shape" />'s exponent, for a curve none of the
    /// named ones give.</summary>
    Custom
}

/// <summary>
/// Turning "how far away did that happen" into "how strongly should it show". Shared by every
/// trigger that scales an occurrence by distance, so they all agree on what a tile is worth.
/// <para>
/// Split in two on purpose: <see cref="Nearness" /> maps a distance onto 0-1 across a band, and
/// <see cref="Shape" /> bends that line into a curve. A trigger that wants the client's own audio
/// falloff asks for the band the client can hear over and a <see cref="FalloffCurve.Quadratic" />
/// curve; one that wants something else changes only the half it disagrees with.
/// </para>
/// </summary>
public static class ProximityMath
{
    /// <summary>
    /// Distance between two tiles the way the client measures it everywhere else: chebyshev, so
    /// something three tiles out on both axes is three away rather than four.
    /// </summary>
    /// <param name="fromX">First tile.</param>
    /// <param name="fromY">First tile.</param>
    /// <param name="toX">Second tile.</param>
    /// <param name="toY">Second tile.</param>
    /// <returns>The distance in tiles.</returns>
    public static int Distance(int fromX, int fromY, int toX, int toY) =>
        Math.Max(Math.Abs(fromX - toX), Math.Abs(fromY - toY));

    /// <summary>
    /// Where <paramref name="distance" /> sits in the band, as 1 at the near edge falling towards 0
    /// at the far one. Zero outside the band entirely.
    /// </summary>
    /// <param name="distance">Tiles away.</param>
    /// <param name="minDistance">Near edge; anything closer is outside the band.</param>
    /// <param name="maxDistance">Far edge; anything further is outside the band.</param>
    /// <returns>Nearness in 0-1.</returns>
    /// <remarks>
    /// The far edge is divided by the band's width plus one, not its width, so something sitting
    /// exactly on it still registers faintly rather than vanishing. That matches the audio manager's
    /// own falloff denominator, which is what lets a visual fade out exactly as the sound
    /// justifying it does.
    /// </remarks>
    public static float Nearness(int distance, int minDistance, int maxDistance)
    {
        if (maxDistance < minDistance || distance < minDistance || distance > maxDistance)
            return 0f;

        return 1f - (float)(distance - minDistance) / (maxDistance - minDistance + 1);
    }

    /// <summary>
    /// Bends a nearness into the intensity curve asked for.
    /// </summary>
    /// <param name="nearness">Nearness in 0-1, as returned by <see cref="Nearness" />.</param>
    /// <param name="curve">Which curve to apply.</param>
    /// <param name="exponent">Power for <see cref="FalloffCurve.Custom" />; ignored otherwise.
    /// Floored just above zero, since zero would flatten every distance to full strength and a
    /// negative one would invert the curve.</param>
    /// <returns>The shaped value, in 0-1.</returns>
    /// <remarks>
    /// Nothing outside the band reaches full strength: a zero nearness stays zero under every curve,
    /// <see cref="FalloffCurve.Flat" /> included, so the band keeps filtering no matter how intensity
    /// is shaped.
    /// </remarks>
    public static float Shape(float nearness, FalloffCurve curve, float exponent = 2f)
    {
        if (nearness <= 0f)
            return 0f;

        float clamped = Math.Min(nearness, 1f);

        return curve switch
        {
            FalloffCurve.Flat => 1f,
            FalloffCurve.Linear => clamped,
            FalloffCurve.Quadratic => clamped * clamped,
            FalloffCurve.Cubic => clamped * clamped * clamped,
            FalloffCurve.SquareRoot => MathF.Sqrt(clamped),
            FalloffCurve.Custom => MathF.Pow(clamped, MathF.Max(exponent, float.Epsilon)),
            _ => clamped
        };
    }

    /// <summary>
    /// Maps a 0-1 amount onto a strength range, so a trigger can say what its faintest occurrence is
    /// still worth. A quake at the edge of earshot is not nothing - the client only plays the sound
    /// within view range at all - it just should not compete with one underfoot.
    /// </summary>
    /// <param name="from">Strength at an amount of 0.</param>
    /// <param name="to">Strength at an amount of 1.</param>
    /// <param name="amount">Where between them, 0-1.</param>
    /// <returns>The interpolated strength.</returns>
    public static float Lerp(float from, float to, float amount) => from + (to - from) * amount;
}
