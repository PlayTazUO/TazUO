using System;
using System.Collections.Generic;
using ClassicUO.Configuration;
using ClassicUO.Renderer;
using ClassicUO.Renderer.Effects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.ScreenOverlays
{
    /// <summary>
    /// Owns and draws the active set of full-screen status overlays (poison, bleed, tunnel vision,
    /// fracture, ...). Each overlay is one or more ordered layers, drawn back-to-front as one draw
    /// call each. Skips all work - no texture allocation, no GPU state changes - when the master
    /// toggle is off or nothing is active.
    /// </summary>
    internal sealed class ScreenOverlayManager
    {
        public static ScreenOverlayManager Instance
        {
            get
            {
                field ??= new ScreenOverlayManager();
                return field;
            }
            private set;
        }

        private const int MAX_CONCURRENT = 4;

        // The MAX_CONCURRENT cap above is about legibility: more than four tinted fields on screen
        // at once is unreadable regardless of what they cost. This one is about cost, and with
        // multi-layer presets the two stopped being the same number. Set to the arithmetic worst
        // case (MAX_CONCURRENT x ScreenOverlayPreset.MaxLayers) so it only ever bites if those caps
        // are raised - overlays are optional decoration, and silently dropping one to save ~0.3ms
        // is the worse trade. Lower it if overlays ever show up in a frame-time profile.
        private const int MAX_LAYERS_PER_FRAME = MAX_CONCURRENT * ScreenOverlayPreset.MaxLayers;

        private const float MIN_FADE_SECONDS = 0.01f;
        private const float TIME_WRAP_SECONDS = 3600f;

        internal sealed class ActiveOverlay
        {
            public ScreenOverlayPreset Preset;
            public readonly List<OverlayLayer> Layers = new();
            public int Priority;
            public float Envelope;
            public bool Hiding;
        }

        private readonly Dictionary<OverlayId, ActiveOverlay> _active = new();

        // Reused every frame so the draw path allocates nothing.
        private readonly List<ActiveOverlay> _drawOrder = new();

        private Texture2D _noiseTexture;
        private ScreenOverlayEffect _effect;
        private float _time;

        /// <summary>
        /// Activates (or re-configures, if already active) an overlay. Fades in from its current
        /// envelope value, never popping. Over the concurrency cap, the lowest-priority active
        /// overlay is evicted to make room.
        /// </summary>
        public void Show(OverlayId id, ScreenOverlayPreset preset, int priority = 0)
        {
            if (_active.TryGetValue(id, out ActiveOverlay existing))
            {
                existing.Preset = preset;
                existing.Priority = priority;
                existing.Hiding = false;
                preset.BakeClamped(existing.Layers);
                return;
            }

            if (_active.Count >= MAX_CONCURRENT)
            {
                OverlayId lowest = default;
                int lowestPriority = int.MaxValue;

                foreach (KeyValuePair<OverlayId, ActiveOverlay> kvp in _active)
                {
                    if (kvp.Value.Priority < lowestPriority)
                    {
                        lowestPriority = kvp.Value.Priority;
                        lowest = kvp.Key;
                    }
                }

                _active.Remove(lowest);
            }

            var added = new ActiveOverlay
            {
                Preset = preset,
                Priority = priority,
                Envelope = 0f,
                Hiding = false
            };

            preset.BakeClamped(added.Layers);
            _active[id] = added;
        }

        /// <summary>
        /// Begins fading the overlay out. It keeps drawing (at shrinking intensity) until the fade
        /// completes, then is dropped.
        /// </summary>
        public void Hide(OverlayId id)
        {
            if (_active.TryGetValue(id, out ActiveOverlay o))
                o.Hiding = true;
        }

        public void Draw(UltimaBatcher2D batcher, Rectangle destRect)
        {
            Profile profile = ProfileManager.CurrentProfile;

            if (profile is not { ScreenOverlaysEnabled: true } || _active.Count == 0)
                return;

            float dt = Time.Delta;
            _time = (_time + dt) % TIME_WRAP_SECONDS;

            AdvanceEnvelopes(dt);
            BuildDrawOrder();

            if (_drawOrder.Count == 0)
                return;

            GraphicsDevice gd = batcher.GraphicsDevice;
            EnsureResources(gd);

            Viewport vp = gd.Viewport;
            var ortho = Matrix.CreateOrthographicOffCenter(0, vp.Width, vp.Height, 0, 0, 1);
            var screenSize = new Vector2(destRect.Width, destRect.Height);
            float globalIntensity = MathHelper.Clamp(profile.ScreenOverlayIntensity, 0f, 1f);

            // Neither the technique nor the projection varies per layer, so they are uploaded once.
            _effect.CurrentTechnique = _effect.Techniques["T0"];
            _effect.MatrixTransform.SetValue(ortho);

            batcher.SetSampler(SamplerState.LinearWrap);
            BlendState activeBlend = null;

            foreach (ActiveOverlay o in _drawOrder)
            {
                foreach (OverlayLayer layer in o.Layers)
                {
                    OverlayParams p = layer.Params;
                    p.Appearance.Intensity *= o.Envelope;

                    if (p.Appearance.Intensity <= 0f)
                        continue;

                    var wanted = layer.Blend.ToBlendState();

                    if (!ReferenceEquals(wanted, activeBlend))
                    {
                        // Costs nothing here: the batch is always empty at this point because the
                        // previous layer's End() flushed it, so SetBlendState's own Flush() is a
                        // no-op and this is just a field assignment.
                        batcher.SetBlendState(wanted);
                        activeBlend = wanted;
                    }

                    _effect.Apply(p, _time, screenSize, globalIntensity);

                    batcher.Begin(_effect);
                    batcher.Draw(_noiseTexture, destRect, Vector3.Zero, 0f);
                    batcher.End();
                }
            }

            batcher.SetSampler(null);
            batcher.SetBlendState(null);
        }

        /// <summary>
        /// Fills <see cref="_drawOrder"/> with the overlays to draw this frame, lowest priority
        /// first so the highest priority composites on top.
        /// </summary>
        private void BuildDrawOrder()
        {
            _drawOrder.Clear();
            _drawOrder.AddRange(_active.Values);

            // Sorted highest-first only so the budget keeps the most important overlays; the list is
            // flipped back to draw order below.
            _drawOrder.Sort(static (a, b) => b.Priority.CompareTo(a.Priority));

            ApplyBudget(_drawOrder, MAX_LAYERS_PER_FRAME);
            _drawOrder.Reverse();
        }

        /// <summary>
        /// Trims <paramref name="orderedHighestFirst"/> to what fits in <paramref name="budget"/>
        /// layers. Overlays are dropped whole rather than truncated - a half-drawn composition (a
        /// fluid body with its highlight missing) reads as a rendering bug, not as a cheaper effect
        /// - and the first overlay that does not fit stops the scan, so a cheap low-priority overlay
        /// can never displace a more important one that was too expensive.
        /// </summary>
        internal static void ApplyBudget(List<ActiveOverlay> orderedHighestFirst, int budget)
        {
            int kept = 0;

            for (int i = 0; i < orderedHighestFirst.Count; i++)
            {
                int layers = orderedHighestFirst[i].Layers.Count;

                if (layers > budget)
                    break;

                budget -= layers;
                kept++;
            }

            orderedHighestFirst.RemoveRange(kept, orderedHighestFirst.Count - kept);
        }

        private void AdvanceEnvelopes(float dt)
        {
            List<OverlayId> finishedHiding = null;

            foreach (KeyValuePair<OverlayId, ActiveOverlay> kvp in _active)
            {
                ActiveOverlay o = kvp.Value;

                float target = o.Hiding ? 0f : 1f;
                float fadeSeconds = o.Hiding ? o.Preset.FadeOutSeconds : o.Preset.FadeInSeconds;
                float rate = 1f / MathF.Max(fadeSeconds, MIN_FADE_SECONDS);

                o.Envelope = MoveTowards(o.Envelope, target, rate * dt);

                if (o.Hiding && o.Envelope <= 0f)
                    (finishedHiding ??= new List<OverlayId>()).Add(kvp.Key);
            }

            if (finishedHiding != null)
            {
                foreach (OverlayId id in finishedHiding)
                    _active.Remove(id);
            }
        }

        private static float MoveTowards(float current, float target, float maxDelta)
        {
            if (MathF.Abs(target - current) <= maxDelta)
                return target;

            return current + MathF.Sign(target - current) * maxDelta;
        }

        private void EnsureResources(GraphicsDevice gd)
        {
            _noiseTexture ??= NoiseTextureFactory.Create(gd);
            _effect ??= new ScreenOverlayEffect(gd);
        }
    }
}
