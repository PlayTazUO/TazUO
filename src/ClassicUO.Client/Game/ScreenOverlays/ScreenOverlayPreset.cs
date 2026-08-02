using System.Collections.Generic;
using ClassicUO.Configuration.FeatureConfigs;
using ClassicUO.Configuration.FeatureConfigs.ScreenOverlays;
using ClassicUO.Renderer.Effects;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.ScreenOverlays
{
    public enum OverlayId
    {
        Poison,
        Bleed,
        TunnelVision,
        Fracture
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
        }
    }
}
