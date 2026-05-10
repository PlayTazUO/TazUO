using System;

namespace ClassicUO.Game.UI.Controls.ResizableComponents;

[Flags]
public enum ResizeEdges
{
    None = 0,
    Left = 1,
    Top = 1 << 2,
    Right = 1 << 3,
    Bottom = 1 << 4,
    All = Left | Top | Right | Bottom
}

public class ResizeEventArgs : EventArgs
{
    public int NewWidth { get; set; }
    public int NewHeight { get; set; }
}

public class ResizerProperties : MyraCommonProps
{
    public ResizeEdges Placements { get; set; } = ResizeEdges.All;
    public uint CornerTriggerRadiusPx { get; set; } = 18;
    public uint EdgeTriggerBandWidthPx { get; set; } = 8;
}

[Flags]
public enum ScrollViewerMode
{
    None = 0,
    Horizontal = 1,
    Vertical = 1 << 1,
    Both = Horizontal | Vertical
}

public class ResizeBehavior : ResizerProperties
{
    public bool Enabled { get; set; } = true;
    public ScrollViewerMode ScrollerMode { get; set; } = ScrollViewerMode.Both;
}
