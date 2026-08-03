using System.Collections.Generic;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations;
using ClassicUO.Renderer.Effects;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.ScreenDecorations.Overlays
{
    /// <summary>
    /// A slot in the compositor. One overlay per id at a time, so re-showing an id reconfigures what
    /// is already on screen instead of stacking a second copy of it.
    /// </summary>
    public enum OverlayId
    {
        Poison,
        Bleed,
        TunnelVision,
        Fracture,
        MortalStrike,
        Fog,
        Drunk,
        Concussion
    }

    /// <summary>
    /// Bakes a small, call-site-friendly set of tunables down to the ordered <see cref="OverlayLayer"/>
    /// list the draw loop needs. Concrete presets expose only the handful of values worth tuning per
    /// use; how many layers they decompose into is an implementation detail of the preset.
    /// </summary>
    public abstract class ScreenOverlayPreset
    {
        /// <summary>
        /// Layers cost one draw call each. A preset wanting more than this is describing several
        /// effects rather than one, and should be split across <see cref="OverlayId"/>s so the
        /// manager's concurrency rules apply to it.
        /// </summary>
        public const int MaxLayers = 4;

        public float FadeInSeconds { get; set; } = 0.4f;
        public float FadeOutSeconds { get; set; } = 0.8f;

        /// <summary>
        /// Appends this preset's layers back-to-front: index 0 is drawn first and ends up underneath.
        /// A single-layer preset appends exactly one.
        /// </summary>
        protected abstract void Bake(List<OverlayLayer> layers);

        /// <summary>
        /// Snapshots this preset's baked layers as an editable profile, so authoring can start from
        /// a working composition rather than an empty stack.
        /// </summary>
        public OverlayEffectProfile ToProfile(string name)
        {
            var layers = new List<OverlayLayer>();
            BakeClamped(layers);

            return new OverlayEffectProfile
            {
                Name = name,
                BasePreset = GetType().Name,
                FadeInSeconds = FadeInSeconds,
                FadeOutSeconds = FadeOutSeconds,
                Layers = layers
            };
        }

        /// <summary>
        /// Refills <paramref name="layers"/> with clamped, budget-capped layers. Every layer is
        /// clamped independently, so composing layers can never be used to route around the
        /// pulse-frequency ceiling in <see cref="OverlayParams.Clamp"/>.
        /// </summary>
        internal void BakeClamped(List<OverlayLayer> layers)
        {
            layers.Clear();
            Bake(layers);

            if (layers.Count > MaxLayers)
            {
                Log.Warn($"{GetType().Name} baked {layers.Count} overlay layers; truncating to {MaxLayers}.");
                layers.RemoveRange(MaxLayers, layers.Count - MaxLayers);
            }

            for (int i = 0; i < layers.Count; i++)
            {
                OverlayLayer layer = layers[i];
                layer.Params.Clamp();
                layers[i] = layer;
            }

            WarnOnMisplacedSampling(layers, GetType().Name);
        }

        /// <summary>
        /// Reports layer stacks that cannot composite the way their author meant.
        /// <para>
        /// A sampling layer reads the frame from before the overlay pass, so it is blind to anything
        /// this preset drew underneath it - and it composites at its own alpha, which means it
        /// overwrites those layers rather than distorting them. Both misuses draw something, just not
        /// what was intended, so they are reported rather than corrected: the fix is a decision about
        /// the composition, not a mechanical reorder.
        /// </para>
        /// </summary>
        /// <param name="layers">The baked stack, in draw order.</param>
        /// <param name="presetName">Name used to identify the offending preset in the log.</param>
        internal static void WarnOnMisplacedSampling(List<OverlayLayer> layers, string presetName)
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
                        $"{presetName} has a sampling layer above a painted one; it will replace that layer " +
                        "rather than distort it. Sampling layers belong at the bottom of the stack."
                    );
                }

                if (sampledBelow)
                {
                    Log.Warn(
                        $"{presetName} has two sampling layers; both read the same pre-pass frame, so the " +
                        "upper one overwrites the lower instead of compounding with it."
                    );
                }

                sampledBelow = true;
            }
        }
    }
}
