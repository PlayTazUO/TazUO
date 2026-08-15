#nullable enable

using System.Text.Json.Serialization;

namespace ClassicUO.Game.Logic;

/// <summary>
/// One node of a boolean expression tree: either a comparison against a field, or a group combining
/// other nodes. The tree is data - it carries no knowledge of what it is being asked about, which is
/// what lets one editor and one evaluator serve every consumer.
/// <para>
/// Persisted, so the discriminators here are stable across releases and must not be renamed.
/// </para>
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "node")]
[JsonDerivedType(typeof(LogicCondition), LogicCondition.Discriminator)]
[JsonDerivedType(typeof(LogicGroup), LogicGroup.Discriminator)]
public abstract class LogicNode
{
    /// <summary>
    /// How this node combines with whatever precedes it inside its parent. Carried by the node
    /// rather than by the group so that each join is its own choice - a bracket holding one
    /// connective for all of its lines means editing any join edits every other.
    /// <para>
    /// Meaningless on the first child, which has nothing before it to join to, and on the root.
    /// </para>
    /// </summary>
    public LogicConnective Join { get; set; } = LogicConnective.And;

    /// <summary>Deep copy, so editing one tree cannot write into another.</summary>
    /// <returns>An independent copy.</returns>
    public abstract LogicNode Clone();
}
