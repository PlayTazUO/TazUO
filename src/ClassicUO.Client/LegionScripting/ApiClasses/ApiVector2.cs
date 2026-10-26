#nullable enable

using System.Globalization;
using Microsoft.Xna.Framework;

namespace ClassicUO.LegionScripting.ApiClasses;

/// <summary>
/// A per-axis pair of values, for wherever an API knob differs between X and Y. Build one with
/// `API.VisualEffects.NewVector2`, or assign to the `X` and `Y` of the one a layer already carries.
/// </summary>
public class ApiVector2 : PyApiObject
{
    #region Public accessors

    /// <summary>Horizontal component. What it means is the knob's, not this class's - a frequency,
    /// a velocity or a position, depending on what carries it.</summary>
    public double X { get; set; }

    /// <summary>Vertical component.</summary>
    public double Y { get; set; }

    #endregion

    #region Ctor

    /// <summary>Builds a pair at the origin, for setting the axes afterwards.</summary>
    public ApiVector2()
    {
    }

    /// <summary>Builds a pair from both axes.</summary>
    /// <param name="x">Horizontal component.</param>
    /// <param name="y">Vertical component.</param>
    public ApiVector2(double x, double y)
    {
        X = x;
        Y = y;
    }

    #endregion

    #region Public methods

    /// <summary>Both axes, so printing one while tuning a layer is readable.</summary>
    /// <returns><c>(X, Y)</c>.</returns>
    public override string ToString() =>
        // Formatted as the base class formats numbers: the axes are floats behind the double a
        // script sets, so the exact form prints digits nobody typed, and a comma-decimal locale
        // would put separators inside what reads as Python output.
        $"({X.ToString("G6", CultureInfo.InvariantCulture)}, {Y.ToString("G6", CultureInfo.InvariantCulture)})";

    #endregion

    #region Internal methods

    /// <summary>Projects a renderer vector into the script-visible pair.</summary>
    /// <param name="value">The vector to copy.</param>
    /// <returns>The DTO.</returns>
    internal static ApiVector2 From(Vector2 value) => new(value.X, value.Y);

    #endregion
}
