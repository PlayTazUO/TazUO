#nullable enable

using System.ComponentModel;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Effects;
using ClassicUO.Renderer.Effects;

namespace ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Profiles;

/// <summary>
/// One effect's participation in a composition: the effect, and how it combines with what is already
/// beneath it. Order is the list index.
/// <para>
/// A wrapper rather than a blend field on <see cref="LayerEffect" /> because blending is a property
/// of the stack, not of the look - the same blur belongs in two profiles blended differently. Today
/// it holds only the blend; it is where any further relationship (masking one layer by another)
/// would land.
/// </para>
/// </summary>
public sealed class ProfileLayer
{
    /// <summary>
    /// What the composer calls this layer, or null to fall back to its technique. A stack of four
    /// blurs is otherwise four identical rows.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>What this layer draws. Null only from a hand-edited file, and skipped when baked.</summary>
    [Description("What this layer draws.")]
    public LayerEffect? Effect { get; set; }

    /// <summary>
    /// How the layer composites against what is beneath it. A highlight pass wants Additive - a
    /// covering highlight reads as a second flat colour, an additive one as light caught on the
    /// layer under it.
    /// </summary>
    [Description(
        "How the layer composites: Alpha covers what is behind it,\n"
        + "Additive brightens it. A highlight pass wants Additive - a\n"
        + "covering highlight reads as a second flat colour."
    )]
    public OverlayBlend Blend { get; set; }

    /// <summary>Copy, so editing one profile's layer cannot write into another's.</summary>
    /// <returns>An independent copy.</returns>
    public ProfileLayer Clone() => new() { Name = Name, Effect = Effect?.Clone(), Blend = Blend };

    /// <summary>Flattens this to the draw loop's own form.</summary>
    /// <returns>The baked layer.</returns>
    internal OverlayLayer Bake() =>
        new()
        {
            Params = Effect?.Bake() ?? OverlayParams.Default,
            Blend = Blend
        };
}
