#nullable enable

using System;
using ClassicUO.Game.Logic;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Logic;

/// <summary>
/// What every part of a <see cref="LogicBuilder" /> needs from the builder that owns it: the fields
/// it may offer, and the two ways of reporting a change. Passed as one object because it is threaded
/// through every nested group and row, and four loose arguments at each level is how they drift.
/// </summary>
internal sealed class LogicEditorContext
{
    /// <summary>The fields conditions may be written about.</summary>
    public required ILogicSchema Schema { get; init; }

    /// <summary>
    /// The tree was edited in a way the widgets already on screen express correctly - a typed value,
    /// a toggled flag. Reports the change without disturbing the layout, so the caret survives.
    /// </summary>
    public required Action Changed { get; init; }

    /// <summary>
    /// The tree's shape changed: a row added or removed, or an operator whose applicable flags
    /// differ from the last one's. Reports the change and rebuilds the widgets under the builder.
    /// </summary>
    public required Action Rebuild { get; init; }

    /// <summary>Whether the tree is shown for reading only. Settable rather than fixed at
    /// construction, since the builder's own switch can be thrown after it has been built.</summary>
    public required bool ReadOnly { get; set; }
}
