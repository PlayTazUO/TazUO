#nullable enable

using Microsoft.Xna.Framework;

namespace ClassicUO.LegionScripting.ApiClasses;

/// <summary>
/// A per-axis pair of values, for wherever an API knob differs between X and Y. Build one with
/// `API.VisualEffects.NewVector2`, or assign to the `X` and `Y` of the one a layer already carries.
/// </summary>
public class ApiVector2
{
    #region Public accessors

    public double X { get; set; }

    public double Y { get; set; }

    #endregion

    #region Ctor

    public ApiVector2()
    {
    }

    public ApiVector2(double x, double y)
    {
        X = x;
        Y = y;
    }

    #endregion

    #region Public methods

    public override string ToString() => $"({X}, {Y})";

    #region Python Emulation

    public string __str__() => ToString();

    public string __repr__() => ToString();

    #endregion

    #endregion

    #region Internal methods

    internal static ApiVector2 From(Vector2 value) => new(value.X, value.Y);

    #endregion
}
