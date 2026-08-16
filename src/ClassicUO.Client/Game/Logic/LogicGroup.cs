#nullable enable

using System.Collections.Generic;
using System.Linq;

namespace ClassicUO.Game.Logic;

/// <summary>
/// A bracket: some nodes, each carrying the connective that joins it to the one above.
/// <para>
/// Read strictly left to right, so <c>a AND b OR c</c> is <c>(a AND b) OR c</c>. No operator binds
/// tighter than another, because a precedence rule the editor cannot show is a precedence rule the
/// user has to already know - nesting a bracket is how a different grouping is expressed, and that
/// one is visible.
/// </para>
/// </summary>
public sealed class LogicGroup : LogicNode
{
    /// <summary>Persisted discriminator. Stable across releases.</summary>
    internal const string Discriminator = "group";

    /// <summary>The nodes under this group, in the order they are shown.</summary>
    public List<LogicNode> Children { get; set; } = [];

    /// <summary>
    /// Whether this group has nothing to say. An empty group is deliberately not a contradiction:
    /// see <see cref="LogicEvaluator{TSubject}" /> for why it passes everything through.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsEmpty => Children.Count == 0;

    /// <inheritdoc />
    public override LogicNode Clone() =>
        new LogicGroup
        {
            Join = Join,
            Children = [.. Children.Select(child => child.Clone())]
        };
}
