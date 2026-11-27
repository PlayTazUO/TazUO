#nullable enable

namespace ClassicUO.LegionScripting.ApiClasses;

/// <summary>
/// A mutable rectangle in pixels, carrying a control's position and size. Read one from
/// <see cref="ApiUiBaseControl.Bounds"/>, edit the fields, then assign it back to apply all four in
/// one non-blocking call.
/// <para>
/// A class rather than XNA's <c>Rectangle</c> because IronPython silently discards field writes on a
/// boxed value type, which would break the edit-then-assign pattern.
/// </para>
/// </summary>
public class ApiRect : PyApiObject
{
    #region Public accessors

    /// <summary>Left edge.</summary>
    public int X { get; set; }

    /// <summary>Top edge.</summary>
    public int Y { get; set; }

    /// <summary>Width in pixels.</summary>
    public int Width { get; set; }

    /// <summary>Height in pixels.</summary>
    public int Height { get; set; }

    #endregion

    #region Ctor

    /// <summary>Builds an empty rectangle, for setting the fields afterwards.</summary>
    public ApiRect()
    {
    }

    /// <summary>Builds a rectangle from all four fields.</summary>
    /// <param name="x">Left edge.</param>
    /// <param name="y">Top edge.</param>
    /// <param name="width">Width in pixels.</param>
    /// <param name="height">Height in pixels.</param>
    public ApiRect(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    #endregion

    #region Public methods

    /// <summary>Readable text form, so printing one while tuning a layout shows all four fields.</summary>
    /// <returns><c>ApiRect(X=.., Y=.., Width=.., Height=..)</c>.</returns>
    public override string ToString() => $"ApiRect(X={X}, Y={Y}, Width={Width}, Height={Height})";

    #endregion
}
