using System;
using Microsoft.Xna.Framework;

namespace ClassicUO.Input;

/// <summary>
///     Represents the event arguments for mouse left button click state changes
/// </summary>
/// <param name="previous">The previous left-click state</param>
/// <param name="current">The current left-click state</param>
public class MouseLeftButtonClickStateChangedEventArgs(bool previous, bool current) : EventArgs
{
    public readonly bool Previous = previous;
    public readonly bool Current = current;
}

/// <summary>
///     Represents the event arguments for mouse movement events
/// </summary>
/// <param name="previous">The previous mouse position</param>
/// <param name="current">The current mouse position</param>
public class MouseMovedEventArgs(Point previous, Point current) : EventArgs
{
    public readonly Point Previous = previous;
    public readonly Point Current = current;
}
