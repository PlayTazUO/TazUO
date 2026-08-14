#nullable enable

namespace ClassicUO.Game.Logic;

/// <summary>
/// Implemented by trigger parameters whose matching is an expression rather than a fixed set of
/// fields. The rule editor answers this with a
/// <see cref="ClassicUO.Game.UI.MyraWindows.Widgets.Logic.LogicBuilder" /> beneath the parameter
/// grid, which is where the tree is authored.
/// <para>
/// The tree is deliberately not shown <em>in</em> the grid: a bracket nests, grows and shrinks, and
/// a property grid row is a single-line editor beside a label.
/// </para>
/// </summary>
public interface ILogicFilterParameters
{
    /// <summary>The tree, edited in place. Never null - an empty root is what an unfiltered trigger
    /// looks like.</summary>
    LogicGroup Filter { get; }

    /// <summary>The fields the tree may be written about.</summary>
    ILogicSchema FilterSchema { get; }
}
