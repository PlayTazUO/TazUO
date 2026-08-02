using System;
using System.Collections.Generic;
using ClassicUO.Configuration;

using ClassicUO.Game.ScreenDecorations.Manager;
using ClassicUO.Renderer;
using ClassicUO.Renderer.Effects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

// The settings class shares its name with this namespace, which shadows it here.
using DecorationSettings = ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.ScreenDecorations;

namespace ClassicUO.Game.ScreenDecorations.Overlays
{
    /// <summary>
    /// Owns and draws the active set of full-screen status overlays (poison, bleed, tunnel vision,
    /// fracture, ...). Each overlay is one or more ordered layers, drawn back-to-front as one draw
    /// call each. Skips all work - no texture allocation, no GPU state changes - when the master
    /// toggle is off or nothing is active.
    /// <para>
    /// Composition only: it draws what it is told to and knows nothing about why. What the player's
    /// state means for which overlay runs is <see cref="ScreenOverlayManager"/>'s business.
    /// </para>
    /// </summary>
    internal sealed class ScreenOverlayCompositor
    {
        public static ScreenOverlayCompositor Instance
        {
            get
            {
                field ??= new ScreenOverlayCompositor();
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
            public readonly List<OverlayLayer> Layers = [];
            public int Priority;
            public float Envelope;
            public bool Hiding;

            /// <summary>Draw over the whole window rather than just the game viewport.</summary>
            public bool FullScreen;
        }

        private readonly Dictionary<OverlayId, ActiveOverlay> _active = new();

        // Reused every frame so the draw path allocates nothing.
        private readonly List<ActiveOverlay> _drawOrder = [];

        private Texture2D _noiseTexture;
        private ScreenOverlayEffect _effect;
        private float _time;

        /// <summary>
        /// Activates (or re-configures, if already active) an overlay. Fades in from its current
        /// envelope value, never popping. Over the concurrency cap, the lowest-priority active
        /// overlay is evicted to make room.
        /// </summary>
        /// <param name="id">The slot to occupy; one overlay per slot.</param>
        /// <param name="preset">What to draw.</param>
        /// <param name="priority">Higher composites on top and survives the concurrency cap.</param>
        /// <param name="fullScreen">Draw over the whole window instead of the game viewport.</param>
        public void Show(OverlayId id, ScreenOverlayPreset preset, int priority = 0, bool fullScreen = false)
        {
            if (_active.TryGetValue(id, out ActiveOverlay existing))
            {
                existing.Preset = preset;
                existing.Priority = priority;
                existing.Hiding = false;
                existing.FullScreen = fullScreen;
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
                Hiding = false,
                FullScreen = fullScreen
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
            DecorationSettings settings = DecorationSettings.Current;

            if (!settings.OverlaysActive || _active.Count == 0)
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
            float globalIntensity = MathHelper.Clamp(settings.Overlays.Intensity, 0f, 1f);

            // Neither the technique nor the projection varies per layer, so they are uploaded once.
            _effect.CurrentTechnique = _effect.Techniques["T0"];
            _effect.MatrixTransform.SetValue(ortho);

            batcher.SetSampler(SamplerState.LinearWrap);
            BlendState activeBlend = null;

            // Resolved on first use: a frame whose overlays are all full-screen never asks the scene
            // where the viewport is, and a frame with nothing to draw returned above.
            Rectangle? viewport = null;

            foreach (ActiveOverlay o in _drawOrder)
            {
                Rectangle target = o.FullScreen ? destRect : viewport ??= ViewportWithin(destRect, vp);

                // Feeds the shader's aspect and distance maths, so it has to be the rectangle being
                // filled - a viewport overlay given the window size would sample as if full screen.
                var screenSize = new Vector2(target.Width, target.Height);

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
                    batcher.Draw(_noiseTexture, target, Vector3.Zero, 0f);
                    batcher.End();
                }
            }

            batcher.SetSampler(null);
            batcher.SetBlendState(null);
        }

        /// <summary>
        /// The game viewport, expressed in the same space as <paramref name="destRect"/>.
        /// <para>
        /// <see cref="Camera.Bounds"/> is the viewport in window coordinates, kept current by
        /// WorldViewportGump as it is moved and resized, so there is nothing to look up per frame.
        /// It is scaled here because destRect carries the render scale - and its origin, because it
        /// carries the screen shake.
        /// </para>
        /// </summary>
        private static Rectangle ViewportWithin(Rectangle destRect, Viewport window)
        {
            Rectangle bounds = Client.Game.Scene?.Camera.Bounds ?? Rectangle.Empty;

            if (bounds.IsEmpty || window.Width <= 0)
                return destRect;

            float scale = destRect.Width / (float)window.Width;

            return new Rectangle(
                destRect.X + (int)(bounds.X * scale),
                destRect.Y + (int)(bounds.Y * scale),
                (int)(bounds.Width * scale),
                (int)(bounds.Height * scale)
            );
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
                    (finishedHiding ??= []).Add(kvp.Key);
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
