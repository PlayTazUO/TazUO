using System;
using System.Collections.Generic;
using ClassicUO.Game.ScreenDecorations.Manager;
using ClassicUO.Renderer;
using ClassicUO.Renderer.Effects;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

// The settings class shares its name with this namespace, which shadows it here.
using DecorationSettings = ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.ScreenDecorations;

namespace ClassicUO.Game.ScreenDecorations.Overlays
{
    /// <summary>Where an overlay is drawn, and so at which point in the frame.</summary>
    internal enum OverlayScope
    {
        /// <summary>Over the game world only, under every gump. Drawn by the scene.</summary>
        Viewport,

        /// <summary>Over the whole window, UI included. Drawn after everything else.</summary>
        FullScreen
    }

    /// <summary>
    /// Owns and draws the active set of status overlays (poison, bleed, tunnel vision, fracture,
    /// ...). Each overlay is one or more ordered layers, drawn back-to-front as one draw call each.
    /// Skips all work - no texture allocation, no GPU state changes - when the master toggle is off
    /// or nothing is active.
    /// <para>
    /// Drawn in two passes per frame, one per <see cref="OverlayScope"/>, because the two sit either
    /// side of the UI. Per-frame bookkeeping (time, fades, draw order) runs on whichever pass comes
    /// first, so a frame that skips one of them still advances.
    /// </para>
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

        /// <summary>
        /// Texture slot the shader's SceneSampler reads. The batcher owns slot 0 for the sprite
        /// being drawn, and slots 1 and 2 are the hue lookup tables - bound once at startup and
        /// never rebound, so borrowing one would break hueing for the rest of the session.
        /// </summary>
        private const int SCENE_SAMPLER = UltimaBatcher2D.SpareTextureSlot;

        internal sealed class ActiveOverlay
        {
            public ScreenOverlayPreset Preset;
            public readonly List<OverlayLayer> Layers = [];
            public int Priority;
            public float Envelope;
            public bool Hiding;

            /// <summary>Which pass draws it, and so what it is allowed to cover.</summary>
            public OverlayScope Scope;
        }

        private readonly Dictionary<OverlayId, ActiveOverlay> _active = new();

        // Reused every frame so the draw path allocates nothing.
        private readonly List<ActiveOverlay> _drawOrder = [];

        private Texture2D _noiseTexture;
        private ScreenOverlayEffect _effect;
        private float _time;

        /// <summary>Frame the per-frame bookkeeping last ran on, so the second pass of a frame does
        /// not advance fades twice. Negative until the first pass.</summary>
        private long _advancedTick = -1;

        /// <summary>Scopes already reported as having no scene to sample, so the warning cannot
        /// repeat every frame.</summary>
        private readonly HashSet<OverlayScope> _sceneWarned = [];

        /// <summary>
        /// Activates (or re-configures, if already active) an overlay. Fades in from its current
        /// envelope value, never popping. Over the concurrency cap, the lowest-priority active
        /// overlay is evicted to make room.
        /// </summary>
        /// <param name="id">The slot to occupy; one overlay per slot.</param>
        /// <param name="preset">What to draw.</param>
        /// <param name="priority">Higher composites on top and survives the concurrency cap.</param>
        /// <param name="scope">Where it is drawn; the game viewport by default.</param>
        public void Show(
            OverlayId id,
            ScreenOverlayPreset preset,
            int priority = 0,
            OverlayScope scope = OverlayScope.Viewport
        )
        {
            if (_active.TryGetValue(id, out ActiveOverlay existing))
            {
                existing.Preset = preset;
                existing.Priority = priority;
                existing.Hiding = false;
                existing.Scope = scope;
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
                Scope = scope
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

        /// <summary>
        /// Draws the overlays belonging to <paramref name="scope"/>.
        /// </summary>
        /// <param name="batcher">The batcher to draw with; must not be mid-batch.</param>
        /// <param name="destRect">The rectangle to fill, in the batcher's coordinate space.</param>
        /// <param name="scope">Which half of the active set to draw.</param>
        /// <param name="scene">The frame as it stood before this pass, for layers that distort it
        /// rather than paint over it. Those layers are skipped where no source is available.</param>
        public void Draw(UltimaBatcher2D batcher, Rectangle destRect, OverlayScope scope, ScreenOverlaySource scene)
        {
            DecorationSettings settings = DecorationSettings.Current;

            if (!settings.OverlaysActive || _active.Count == 0)
                return;

            AdvanceFrame();

            // Checked before any GPU state is touched: the other pass usually has nothing to do.
            if (!HasDrawable(scope, scene.IsAvailable))
                return;

            GraphicsDevice gd = batcher.GraphicsDevice;
            EnsureResources(gd);

            Viewport vp = gd.Viewport;
            var ortho = Matrix.CreateOrthographicOffCenter(0, vp.Width, vp.Height, 0, 0, 1);
            float globalIntensity = MathHelper.Clamp(settings.Overlays.Intensity, 0f, 1f);

            // The projection does not vary per layer, so it is uploaded once. The technique does -
            // a sampling layer runs a different pixel shader from a tint layer.
            _effect.MatrixTransform.SetValue(ortho);

            batcher.SetSampler(SamplerState.LinearWrap);
            BlendState activeBlend = null;
            bool sceneBound = false;

            // Feeds the shader's aspect and distance maths, so it has to be the rectangle being
            // filled - a viewport overlay given the window size would sample as if full screen.
            var screenSize = new Vector2(destRect.Width, destRect.Height);
            OverlaySceneMap sceneMap = scene.ToMap();

            foreach (ActiveOverlay o in _drawOrder)
            {
                if (o.Scope != scope)
                    continue;

                foreach (OverlayLayer layer in o.Layers)
                {
                    OverlayParams p = layer.Params;
                    p.Appearance.Intensity *= o.Envelope;

                    if (p.Appearance.Intensity <= 0f)
                        continue;

                    if (p.Sampling.ReadsScene && !scene.IsAvailable)
                    {
                        WarnSceneUnavailable(scope);
                        continue;
                    }

                    var wanted = layer.Blend.ToBlendState();

                    if (!ReferenceEquals(wanted, activeBlend))
                    {
                        // Costs nothing here: the batch is always empty at this point because the
                        // previous layer's End() flushed it, so SetBlendState's own Flush() is a
                        // no-op and this is just a field assignment.
                        batcher.SetBlendState(wanted);
                        activeBlend = wanted;
                    }

                    if (p.Sampling.ReadsScene && !sceneBound)
                    {
                        BindScene(batcher, scene);
                        sceneBound = true;
                    }

                    _effect.SetTechnique(p.Sampling);
                    _effect.Apply(p, _time, screenSize, globalIntensity, sceneMap);

                    batcher.Begin(_effect);
                    batcher.Draw(_noiseTexture, destRect, Vector3.Zero, 0f);
                    batcher.End();
                }
            }

            if (sceneBound)
                UnbindScene(batcher);

            batcher.SetSampler(null);
            batcher.SetBlendState(null);
        }

        /// <summary>
        /// Reports a distortion layer dropped for want of a scene to read, once per scope. Silence
        /// here is indistinguishable from a mistuned effect, and the causes are all invisible from
        /// the preset: no screen render target, or a scene pass that never composited one.
        /// </summary>
        private void WarnSceneUnavailable(OverlayScope scope)
        {
            if (!_sceneWarned.Add(scope))
                return;

            Log.Warn($"Overlay sampling layers skipped in the {scope} pass: no scene texture to read.");
        }

        /// <summary>
        /// Linear so taps between texels are interpolated rather than snapped, and clamped because a
        /// tap that runs off the edge of the scene must smear the border pixel rather than fetch the
        /// opposite side of the screen. Set through the batcher, which reasserts every sampler on
        /// each flush and would otherwise overwrite it.
        /// </summary>
        private static void BindScene(UltimaBatcher2D batcher, ScreenOverlaySource scene)
        {
            batcher.GraphicsDevice.Textures[SCENE_SAMPLER] = scene.Texture;
            batcher.SetSpareSampler(SamplerState.LinearClamp);
        }

        /// <summary>
        /// Mandatory before the frame ends. The scene texture is a render target that gets bound for
        /// drawing again next frame, and binding a target still bound as a texture is an error.
        /// </summary>
        private static void UnbindScene(UltimaBatcher2D batcher)
        {
            batcher.SetSpareSampler(null);
            batcher.GraphicsDevice.Textures[SCENE_SAMPLER] = null;
        }

        /// <summary>
        /// Advances animation time, fades and draw order, once per frame regardless of how many
        /// passes call it.
        /// </summary>
        private void AdvanceFrame()
        {
            if (_advancedTick == Time.Ticks)
                return;

            _advancedTick = Time.Ticks;

            float dt = Time.Delta;
            _time = (_time + dt) % TIME_WRAP_SECONDS;

            AdvanceEnvelopes(dt);
            BuildDrawOrder();
        }

        /// <summary>
        /// Whether this pass has anything to draw at all, checked before any GPU state is touched.
        /// A pass whose only layers need the scene and cannot have it counts as empty.
        /// </summary>
        private bool HasDrawable(OverlayScope scope, bool sceneAvailable)
        {
            foreach (ActiveOverlay o in _drawOrder)
            {
                if (o.Scope != scope)
                    continue;

                if (sceneAvailable)
                    return true;

                foreach (OverlayLayer layer in o.Layers)
                {
                    if (!layer.Params.Sampling.ReadsScene)
                        return true;
                }
            }

            return false;
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
