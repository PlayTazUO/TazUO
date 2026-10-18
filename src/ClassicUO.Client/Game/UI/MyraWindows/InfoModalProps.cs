#nullable enable

using System;
using ClassicUO.Game.UI.Controls;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.MyraWindows;

/// <summary>
///     Chrome, placement and size limits of an <see cref="InfoModal" />. Changes apply live - the
///     modal watches this object and reconfigures itself.
/// </summary>
public class InfoModalProps : MyraCommonProps
{
    #region Public accessors

    /// <summary>
    ///     Whether the title bar carries a close button. Right-click dismissal works either way.
    /// </summary>
    public bool Closable { get; set => SetField(ref field, value); } = true;

    /// <summary>
    ///     Whether the dialog can be minimized to its title bar.
    /// </summary>
    public bool Minimizable { get; set => SetField(ref field, value); } = true;

    /// <summary>
    ///     Top-left corner, in screen coordinates. Null centers the dialog.
    /// </summary>
    /// <remarks>
    ///     Invoked per open, not once, so a dialog placed against its owner survives the owner moving.
    /// </remarks>
    public Func<Point>? Placement { get; set => SetField(ref field, value); }

    #endregion

    #region Public methods

    /// <summary>
    ///     The size limits as an ordered pair: the minimum floored at zero, the maximum lifted to
    ///     meet it where a consumer has set the two crossed.
    /// </summary>
    /// <remarks>
    ///     Consumers clamp through <see cref="Math.Clamp(int,int,int)" />, which throws on crossed
    ///     bounds - the drag-resize path once per mouse-move. Nothing stops the four inherited
    ///     properties being set in any order, so they are ordered here rather than at each use.
    /// </remarks>
    /// <returns>The minimum and maximum size, in that order.</returns>
    public (Point Min, Point Max) GetOrderedSizeLimits()
    {
        var min = new Point(Math.Max(0, MinWidth), Math.Max(0, MinHeight));
        var max = new Point(Math.Max(min.X, MaxWidth), Math.Max(min.Y, MaxHeight));

        return (min, max);
    }

    #endregion

    #region Private members

    /// <summary>
    ///     Wide enough for a title and a line of prose, and tall enough that a one-line body
    ///     does not collapse into a strip.
    /// </summary>
    private const int DEFAULT_MIN_WIDTH = 300;

    private const int DEFAULT_MIN_HEIGHT = 120;

    /// <summary>
    ///     A ceiling is what keeps a long body from measuring out taller than the screen; the
    ///     inherited window maximum is too generous to serve as one.
    /// </summary>
    private const int DEFAULT_MAX_WIDTH = 700;

    private const int DEFAULT_MAX_HEIGHT = 600;

    #endregion

    #region Ctor

    /// <summary>
    ///     Initializes a new instance of the <see cref="InfoModalProps" /> class, with size limits
    ///     suited to a read-only dialog rather than to a full window.
    /// </summary>
    public InfoModalProps()
    {
        MinWidth = DEFAULT_MIN_WIDTH;
        MinHeight = DEFAULT_MIN_HEIGHT;
        MaxWidth = DEFAULT_MAX_WIDTH;
        MaxHeight = DEFAULT_MAX_HEIGHT;
    }

    #endregion
}
