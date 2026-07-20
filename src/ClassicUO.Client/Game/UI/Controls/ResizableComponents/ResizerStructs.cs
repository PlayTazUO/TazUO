using System;

namespace ClassicUO.Game.UI.Controls.ResizableComponents;

/// <summary>
///     Identifies the edges of a window that can be used as resize handles.
/// </summary>
[Flags]
public enum ResizeEdges
{
    /// <summary>
    ///     No resize edges enabled.
    /// </summary>
    None = 0,

    /// <summary>
    ///     The left edge.
    /// </summary>
    Left = 1,

    /// <summary>
    ///     The top edge.
    /// </summary>
    Top = 1 << 2,

    /// <summary>
    ///     The right edge.
    /// </summary>
    Right = 1 << 3,

    /// <summary>
    ///     The bottom edge.
    /// </summary>
    Bottom = 1 << 4,

    /// <summary>
    ///     All edges (left, top, right, and bottom).
    /// </summary>
    All = Left | Top | Right | Bottom
}

/// <summary>
///     Provides data for the <see cref="ResizableWindow.Resized" /> event.
/// </summary>
public class ResizeEventArgs : EventArgs
{
    /// <summary>
    ///     Gets or sets the window's new width, in pixels.
    /// </summary>
    public int NewWidth { get; set; }

    /// <summary>
    ///     Gets or sets the window's new height, in pixels.
    /// </summary>
    public int NewHeight { get; set; }
}

/// <summary>
///     Defines the configurable properties of a resize handle, such as which edges are active
///     and the pixel radii used to detect the cursor over them.
/// </summary>
public class ResizerProperties : MyraCommonProps
{
    /// <summary>
    ///     Gets or sets which edges of the window act as resize handles.
    /// </summary>
    public ResizeEdges Placements { get; set => SetField(ref field, value); } = ResizeEdges.All;

    /// <summary>
    ///     Gets or sets the radius, in pixels, of the circular hit area used to detect the cursor
    ///     over a corner resize handle.
    /// </summary>
    public uint CornerTriggerRadiusPx { get; set => SetField(ref field, value); } = 18;

    /// <summary>
    ///     Gets or sets the thickness, in pixels, of the hit area strip used to detect the cursor
    ///     over an edge (non-corner) resize handle.
    /// </summary>
    public uint EdgeTriggerBandWidthPx { get; set => SetField(ref field, value); } = 8;
}

/// <summary>
///     Specifies which scrollbars a resizable window's content wrapper may display.
/// </summary>
[Flags]
public enum ScrollViewerMode
{
    /// <summary>
    ///     No scrollbars; the content is not wrapped in a scroll viewer.
    /// </summary>
    None = 0,

    /// <summary>
    ///     A horizontal scrollbar is shown when needed.
    /// </summary>
    Horizontal = 1,

    /// <summary>
    ///     A vertical scrollbar is shown when needed.
    /// </summary>
    Vertical = 1 << 1,

    /// <summary>
    ///     Both horizontal and vertical scrollbars are shown when needed.
    /// </summary>
    Both = Horizontal | Vertical
}

/// <summary>
///     Defines the resize behavior of a <see cref="ResizableWindow" />, including whether
///     resizing is enabled, which edges are active, size limits, and how content is scrolled.
/// </summary>
public class ResizeBehavior : ResizerProperties
{
    /// <summary>
    ///     Gets or sets a value indicating whether resizing is enabled.
    /// </summary>
    public bool Enabled { get; set => SetField(ref field, value); } = true;

    /// <summary>
    ///     Gets or sets which scrollbars are displayed when the window's content overflows its bounds.
    /// </summary>
    public ScrollViewerMode ScrollerMode { get; set => SetField(ref field, value); } = ScrollViewerMode.Both;
}
