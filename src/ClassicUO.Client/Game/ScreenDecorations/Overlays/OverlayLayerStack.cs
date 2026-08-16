#nullable enable

using System.Collections.Generic;
using ClassicUO.Renderer.Effects;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.ScreenDecorations.Overlays;

/// <summary>
/// The rules a stack of baked layers has to obey before it reaches the draw loop, shared by the
/// shipped presets and by authored profiles - both are stacks, and both can be composed wrongly.
/// </summary>
internal static class OverlayLayerStack
{
    /// <summary>
    /// Layers cost one draw call each. A composition wanting more than this is describing several
    /// effects rather than one, and should be split across rules so the manager's concurrency rules
    /// apply to it.
    /// </summary>
    public const int MaxLayers = 4;

    /// <summary>
    /// Caps <paramref name="layers"/> to the budget and reports compositions that cannot work.
    /// </summary>
    /// <param name="layers">The baked stack, in draw order.</param>
    /// <param name="sourceName">Name used to identify the offending composition in the log.</param>
    internal static void Finish(List<OverlayLayer> layers, string sourceName)
    {
        if (layers.Count > MaxLayers)
        {
            Log.Warn($"{sourceName} baked {layers.Count} overlay layers; truncating to {MaxLayers}.");
            layers.RemoveRange(MaxLayers, layers.Count - MaxLayers);
        }

        WarnOnMisplacedSampling(layers, sourceName);
    }

    /// <summary>
    /// Reports layer stacks that cannot composite the way their author meant.
    /// <para>
    /// A sampling layer reads the frame from before the overlay pass, so it is blind to anything the
    /// same stack drew underneath it - and it composites at its own alpha, which means it overwrites
    /// those layers rather than distorting them. Both misuses draw something, just not what was
    /// intended, so they are reported rather than corrected: the fix is a decision about the
    /// composition, not a mechanical reorder.
    /// </para>
    /// </summary>
    /// <param name="layers">The baked stack, in draw order.</param>
    /// <param name="sourceName">Name used to identify the offending composition in the log.</param>
    private static void WarnOnMisplacedSampling(List<OverlayLayer> layers, string sourceName)
    {
        bool paintedBelow = false;
        bool sampledBelow = false;

        foreach (OverlayLayer layer in layers)
        {
            if (!layer.Params.Sampling.ReadsScene)
            {
                paintedBelow = true;
                continue;
            }

            if (paintedBelow)
            {
                Log.Warn(
                    $"{sourceName} has a sampling layer above a painted one; it will replace that layer " +
                    "rather than distort it. Sampling layers belong at the bottom of the stack."
                );
            }

            if (sampledBelow)
            {
                Log.Warn(
                    $"{sourceName} has two sampling layers; both read the same pre-pass frame, so the " +
                    "upper one overwrites the lower instead of compounding with it."
                );
            }

            sampledBelow = true;
        }
    }
}
