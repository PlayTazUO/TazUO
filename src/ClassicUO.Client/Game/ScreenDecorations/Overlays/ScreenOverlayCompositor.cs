#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Profiles;
using ClassicUO.Game.Managers;
using ClassicUO.Game.ScreenDecorations.Manager;
using ClassicUO.Renderer;
using ClassicUO.Renderer.Effects;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

// The settings class shares its name with this namespace, which shadows it here.
using DecorationSettings = ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.ScreenDecorations;

namespace ClassicUO.Game.ScreenDecorations.Overlays;

/// <summary>Where an overlay is drawn, and so at which point in the frame.</summary>
internal enum OverlayScope
{
    /// <summary>Over the game world only, under every gump. Drawn by the scene.</summary>
    Viewport,

    /// <summary>Over the whole window, UI included. Drawn after everything else.</summary>
    FullScreen
}

/// <summary>
/// Owns and draws the active set of overlays. Each is one or more ordered layers, drawn back-to-front
/// as one draw call each. Skips all work - no texture allocation, no GPU state changes - when the
/// master toggle is off or nothing is active.
/// <para>
/// Slots are keyed by the rule that raised them, not by the look they draw: two rules pointing at
/// one profile are two occurrences and both composite, while one rule firing twice reconfigures
/// what is already on screen rather than stacking a second copy of it.
/// </para>
/// <para>
/// Drawn in two passes per frame, one per <see cref="OverlayScope"/>, because the two sit either
/// side of the UI. Per-frame bookkeeping (time, fades, draw order) runs on whichever pass comes
/// first, so a frame that skips one of them still advances.
/// </para>
/// <para>
/// Composition only: it draws what it is told to and knows nothing about why. What the player's
/// state means for which overlay runs is <see cref="ScreenOverlayManager"/>'s business.
/// </para>
/// <para>
/// Threading: <see cref="Show"/> and <see cref="Hide"/> marshal themselves to the main thread and so
/// may be called from anywhere. <see cref="Draw"/> may not - it needs a live batcher and a bound
/// graphics device, and deferring it past the frame it belongs to would mean nothing.
/// </para>
/// </summary>
internal sealed class ScreenOverlayCompositor
{
    #region Public accessors

    /// <summary>
    /// Built by the type initializer, so it is created once no matter which thread reaches
    /// <see cref="Instance"/> first. The GPU resources are not part of it - <see cref="_noiseTexture"/>
    /// and <see cref="_effect"/> need a device and are built on the first frame that draws.
    /// </summary>
    public static ScreenOverlayCompositor Instance { get; } = new();

    #endregion

    #region Private members

    private const float MIN_FADE_SECONDS = 0.01f;
    private const float TIME_WRAP_SECONDS = 3600f;

    /// <summary>
    /// Texture slot the shader's SceneSampler reads. The batcher owns slot 0 for the sprite being
    /// drawn, and slots 1 and 2 are the hue lookup tables - bound once at startup and never rebound,
    /// so borrowing one would break hueing for the rest of the session.
    /// </summary>
    private const int SCENE_SAMPLER = UltimaBatcher2D.SpareTextureSlot;

    private readonly Dictionary<Guid, ActiveOverlay> _active = [];

    // Reused every frame so the draw path allocates nothing.
    private readonly List<ActiveOverlay> _drawOrder = [];

    private Texture2D? _noiseTexture;
    private ScreenOverlayEffect? _effect;
    private float _time;

    /// <summary>Frame the per-frame bookkeeping last ran on, so the second pass of a frame does not
    /// advance fades twice. Negative until the first pass.</summary>
    private long _advancedTick = -1;

    /// <summary>Scopes already reported as having no scene to sample, so the warning cannot repeat
    /// every frame.</summary>
    private readonly HashSet<OverlayScope> _sceneWarned = [];

    #endregion

    #region Public methods

    /// <summary>
    /// Activates, or re-configures if already active, the overlay occupying <paramref name="id"/>.
    /// Fades in from its current envelope value, never popping. Accepts whatever it is given - the
    /// concurrency cap is <see cref="ScreenOverlayManager"/>'s call, since a silent drop here would
    /// leave that class believing the overlay was still on screen.
    /// <para>
    /// Restating an already-active overlay adjusts it in place: the envelope is untouched, so a
    /// nearer second earthquake strengthens the one on screen rather than restarting its fade.
    /// </para>
    /// </summary>
    /// <param name="id">The slot to occupy; one overlay per slot.</param>
    /// <param name="profile">What to draw, and how it fades and is scoped.</param>
    /// <param name="intensity">Strength of the occurrence behind it, 0-1. Scales the profile's own
    /// values rather than replacing them, so the profile stays the ceiling.</param>
    /// <param name="priority">Higher composites on top and survives the concurrency cap.</param>
    /// <exception cref="ArgumentNullException">The profile is null.</exception>
    public void Show(Guid id, EffectProfile profile, float intensity = 1f, int priority = 0)
    {
        ArgumentNullException.ThrowIfNull(profile);

        // Dispatched out of line rather than through a lambda here: capturing these parameters would
        // put the closure allocation at method entry, ahead of the branch, so the main-thread path
        // would pay for it on every overlay raised.
        if (!MainThreadQueue.IsMainThread)
        {
            DispatchShow(id, profile, intensity, priority);
            return;
        }

        OverlayScope scope = profile.FullScreen ? OverlayScope.FullScreen : OverlayScope.Viewport;

        if (_active.TryGetValue(id, out ActiveOverlay? existing))
        {
            existing.Profile = profile;
            existing.Priority = priority;
            existing.Hiding = false;
            existing.Scope = scope;
            existing.Intensity = intensity;
            profile.BakeClamped(existing.Layers);

            return;
        }

        var added = new ActiveOverlay
        {
            Profile = profile,
            Priority = priority,
            Envelope = 0f,
            Hiding = false,
            Intensity = intensity,
            Scope = scope
        };

        profile.BakeClamped(added.Layers);
        _active[id] = added;
    }

    /// <summary>
    /// Begins fading the overlay out. It keeps drawing, at shrinking intensity, until the fade
    /// completes, then is dropped.
    /// </summary>
    /// <param name="id">The slot to release. Unknown ids are ignored.</param>
    public void Hide(Guid id)
    {
        if (!MainThreadQueue.IsMainThread)
        {
            DispatchHide(id);
            return;
        }

        if (_active.TryGetValue(id, out ActiveOverlay? overlay))
            overlay.Hiding = true;
    }

    /// <summary>
    /// Drops everything on screen at once, fade skipped. For the system being switched off, where
    /// <see cref="Draw"/> stops running: fades only advance from there, so a <see cref="Hide"/> at
    /// that point would freeze each overlay at its current envelope instead of retiring it, and
    /// switching back on would show that stack again before a reconcile pass could take it down.
    /// </summary>
    public void Clear()
    {
        if (!MainThreadQueue.IsMainThread)
        {
            DispatchClear();
            return;
        }

        _active.Clear();
        _drawOrder.Clear();
    }

    /// <summary>
    /// Draws the overlays belonging to <paramref name="scope"/>.
    /// </summary>
    /// <param name="batcher">The batcher to draw with; must not be mid-batch.</param>
    /// <param name="destRect">The rectangle to fill, in the batcher's coordinate space.</param>
    /// <param name="scope">Which half of the active set to draw.</param>
    /// <param name="scene">The frame as it stood before this pass, for layers that distort it rather
    /// than paint over it. Those layers are skipped where no source is available.</param>
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

        // The projection does not vary per layer, so it is uploaded once. The technique does - a
        // sampling layer runs a different pixel shader from a tint layer.
        _effect!.MatrixTransform.SetValue(ortho);

        batcher.SetSampler(SamplerState.LinearWrap);
        BlendState? activeBlend = null;
        bool sceneBound = false;

        // Feeds the shader's aspect and distance maths, so it has to be the rectangle being filled -
        // a viewport overlay given the window size would sample as if full screen.
        var screenSize = new Vector2(destRect.Width, destRect.Height);
        OverlaySceneMap sceneMap = scene.ToMap();

        foreach (ActiveOverlay overlay in _drawOrder)
        {
            if (overlay.Scope != scope)
                continue;

            foreach (OverlayLayer layer in overlay.Layers)
            {
                OverlayParams p = layer.Params;

                // Envelope is the fade; intensity is what the occurrence asked for. Both scale the
                // authored value rather than replacing it, so the profile stays the ceiling.
                p.Appearance.Intensity *= overlay.Envelope * overlay.Intensity;

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
                    // previous layer's End() flushed it, so SetBlendState's own Flush() is a no-op
                    // and this is just a field assignment.
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

    #endregion

    #region Internal methods

    /// <summary>
    /// Trims <paramref name="orderedHighestFirst"/> to what fits in <paramref name="budget"/>
    /// layers. Overlays are dropped whole rather than truncated - a half-drawn composition (a fluid
    /// body with its highlight missing) reads as a rendering bug, not as a cheaper effect - and the
    /// first overlay that does not fit stops the scan, so a cheap low-priority overlay can never
    /// displace a more important one that was too expensive.
    /// </summary>
    /// <param name="orderedHighestFirst">The candidates, most important first.</param>
    /// <param name="budget">Layers this frame may draw.</param>
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

    /// <summary>One occupied compositor slot. Internal for the budget tests.</summary>
    internal sealed class ActiveOverlay
    {
        /// <summary>The look being drawn, which also supplies the fade timing and the scope.</summary>
        public EffectProfile Profile = new();

        public readonly List<OverlayLayer> Layers = [];

        public int Priority;

        public float Envelope;

        public bool Hiding;

        /// <summary>Strength of the occurrence that raised this. Held rather than baked in, so it
        /// can be restated without a re-bake.</summary>
        public float Intensity = 1f;

        /// <summary>Which pass draws it, and so what it is allowed to cover.</summary>
        public OverlayScope Scope;
    }

    #endregion

    #region Private methods

    /// <summary>Off-thread half of <see cref="Show" />, kept out of line for its closure.</summary>
    /// <param name="id">The slot to occupy.</param>
    /// <param name="profile">What to draw.</param>
    /// <param name="intensity">Strength of the occurrence behind it.</param>
    /// <param name="priority">Higher composites on top.</param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void DispatchShow(Guid id, EffectProfile profile, float intensity, int priority) =>
        MainThreadQueue.InvokeOnMainThread(() => Show(id, profile, intensity, priority));

    /// <summary>Off-thread half of <see cref="Hide" />, kept out of line for its closure.</summary>
    /// <param name="id">The slot to release.</param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void DispatchHide(Guid id) => MainThreadQueue.InvokeOnMainThread(() => Hide(id));

    /// <summary>Off-thread half of <see cref="Clear" />.</summary>
    private void DispatchClear() => MainThreadQueue.InvokeOnMainThread(Clear);

    /// <summary>
    /// The user's cap on simultaneous overlays, read live so a mid-session change applies at once.
    /// Used here only for the layer budget; the cap itself is applied before reaching this class.
    /// </summary>
    internal static int ConcurrencyCap() =>
        Math.Clamp(
            DecorationSettings.Current.Overlays.MaxConcurrent,
            OverlaySystemSettings.MinConcurrent,
            OverlaySystemSettings.MaxAllowedConcurrent
        );

    /// <summary>
    /// Reports a distortion layer dropped for want of a scene to read, once per scope. Silence here
    /// is indistinguishable from a mistuned effect, and the causes are all invisible from the
    /// profile: no screen render target, or a scene pass that never composited one.
    /// </summary>
    private void WarnSceneUnavailable(OverlayScope scope)
    {
        if (!_sceneWarned.Add(scope))
            return;

        Log.Warn($"Overlay sampling layers skipped in the {scope} pass: no scene texture to read.");
    }

    /// <summary>
    /// Linear so taps between texels are interpolated rather than snapped, and clamped because a tap
    /// that runs off the edge of the scene must smear the border pixel rather than fetch the
    /// opposite side of the screen. Set through the batcher, which reasserts every sampler on each
    /// flush and would otherwise overwrite it.
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
    /// Advances animation time, fades and draw order, once per frame regardless of how many passes
    /// call it.
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
    /// Whether this pass has anything to draw at all, checked before any GPU state is touched. A
    /// pass whose only layers need the scene and cannot have it counts as empty.
    /// </summary>
    private bool HasDrawable(OverlayScope scope, bool sceneAvailable)
    {
        foreach (ActiveOverlay overlay in _drawOrder)
        {
            if (overlay.Scope != scope)
                continue;

            if (sceneAvailable)
                return true;

            foreach (OverlayLayer layer in overlay.Layers)
            {
                if (!layer.Params.Sampling.ReadsScene)
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Fills <see cref="_drawOrder"/> with the overlays to draw this frame, lowest priority first so
    /// the highest priority composites on top.
    /// </summary>
    private void BuildDrawOrder()
    {
        _drawOrder.Clear();
        _drawOrder.AddRange(_active.Values);

        // Sorted highest-first only so the budget keeps the most important overlays; the list is
        // flipped back to draw order below.
        _drawOrder.Sort(static (a, b) => b.Priority.CompareTo(a.Priority));

        // The concurrency cap is about legibility; this is about cost, and with multi-layer profiles
        // the two stopped being the same number. Set to the arithmetic worst case so it only ever
        // bites if the cap is raised - overlays are optional decoration, and silently dropping one
        // to save a fraction of a millisecond is the worse trade.
        ApplyBudget(_drawOrder, ConcurrencyCap() * OverlayLayerStack.MaxLayers);

        _drawOrder.Reverse();
    }

    private void AdvanceEnvelopes(float dt)
    {
        List<Guid>? finishedHiding = null;

        foreach (KeyValuePair<Guid, ActiveOverlay> entry in _active)
        {
            ActiveOverlay overlay = entry.Value;

            float target = overlay.Hiding ? 0f : 1f;
            float fadeSeconds = overlay.Hiding ? overlay.Profile.Fade.OutSeconds : overlay.Profile.Fade.InSeconds;
            float rate = 1f / MathF.Max(fadeSeconds, MIN_FADE_SECONDS);

            overlay.Envelope = MoveTowards(overlay.Envelope, target, rate * dt);

            if (overlay.Hiding && overlay.Envelope <= 0f)
                (finishedHiding ??= []).Add(entry.Key);
        }

        if (finishedHiding == null)
            return;

        foreach (Guid id in finishedHiding)
            _active.Remove(id);
    }

    private static float MoveTowards(float current, float target, float maxDelta)
    {
        if (MathF.Abs(target - current) <= maxDelta)
            return target;

        return current + MathF.Sign(target - current) * maxDelta;
    }

    private void EnsureResources(GraphicsDevice gd)
    {
        // The noise texture and effect are built once for the entire app lifecycle.
        // Perlin-noise generation specifically is relatively time-consuming (~20ms) so important to do only once.
        _noiseTexture ??= NoiseTextureFactory.Create(gd);
        _effect ??= new ScreenOverlayEffect(gd);
    }

    #endregion
}
