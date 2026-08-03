#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations;
using ClassicUO.Game.Managers;
using ClassicUO.Game.ScreenDecorations.Overlays;
using ClassicUO.Game.ScreenDecorations.Overlays.Presets;
using ClassicUO.Game.ScreenDecorations.Shake;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using ClassicUO.Utility.Logging;

// The settings class shares its name with its namespace, which shadows it here.
using DecorationSettings = ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.ScreenDecorations;
using Lock = System.Threading.Lock;

namespace ClassicUO.Game.ScreenDecorations.Manager;

/// <summary>
/// Decides which overlays should be running, and drives everything else that reacts to the same
/// state - screen shake included. <see cref="ScreenOverlayCompositor"/> is told what to draw and
/// nothing more.
/// <para>
/// Reconciles rather than reacts: on each pass it works out the set of overlays the player's state
/// and the current settings call for, and moves the compositor towards it. A missed transition
/// therefore cannot leave an overlay stuck on, and toggling an effect off, or editing its profile in
/// the options, lands on the next pass.
/// </para>
/// <para>
/// Those passes run on a timer of their own rather than per frame - status changes at human pace,
/// and a quarter-second of latency on a poison tint is not perceptible. The timer thread only asks
/// for a pass; the pass itself is marshalled onto the main thread, because it reads live game
/// objects and mutates the compositor's active set while the draw loop is walking it.
/// </para>
/// </summary>
internal sealed class ScreenOverlayManager
{
    #region Public accessors

    public static ScreenOverlayManager Instance
    {
        get
        {
            field ??= new ScreenOverlayManager();
            return field;
        }
    }

    #endregion

    #region Private members

    /// <summary>
    /// Gap between reconcile passes. The floor on how long an overlay can lag the state that
    /// justifies it, and half the average lag.
    /// </summary>
    private static readonly TimeSpan _reconcileInterval = TimeSpan.FromMilliseconds(750);

    /// <summary>How hard an onset shake hits and for how long. Shaped as an impact: full strength
    /// immediately, falling away.</summary>
    private static readonly TimeSpan _onsetShakeDuration = TimeSpan.FromSeconds(0.6);

    /// <summary>
    /// What the player being in a given state means. Ordered by priority, highest first, purely for
    /// readability - <see cref="EffectMapping.Priority"/> is what the compositor sorts on.
    /// </summary>
    private static readonly EffectMapping[] _mappings =
    [
        new(PlayerCondition.MortalStruck, OverlayEffect.MortalStrike, 30, 0.45f),
        new(PlayerCondition.Poisoned, OverlayEffect.Poison, 20, 0f),
        new(PlayerCondition.Bleeding, OverlayEffect.Bleed, 10, 0.25f)
    ];

    /// <summary>
    /// Priority a previewed overlay composites at. Above every mapping, so previewing an effect
    /// while the player happens to be poisoned shows the one that was asked for.
    /// </summary>
    private const int PREVIEW_PRIORITY = 100;

    /// <summary>Guards the fields below. Never held across a reconcile pass - that runs on the main
    /// thread, and blocking the timer thread on it would let passes queue up behind a stalled
    /// frame.</summary>
    private readonly Lock _sync = new();

    /// <summary>The ids this manager has asked for. Not the compositor's own set: that one still
    /// holds overlays part-way through their fade-out, which must not read as "already showing".</summary>
    private readonly HashSet<OverlayId> _shown = [];

    private CancellationTokenSource? _cancellation;

    /// <summary>This frame's shake displacement and the frame it was sampled on. Render thread
    /// only, so they are not under <see cref="_sync"/>.</summary>
    private Point _shakeOffset;

    private long _shakeTick = -1;

    /// <summary>Set while a pass is queued or running, so a slow frame cannot leave several passes
    /// stacked up waiting on the main thread.</summary>
    private bool _passPending;

    /// <summary>
    /// The effect being previewed from the options, shown regardless of the player's state and of
    /// its own enabled toggle - the point of a preview is to see an effect you have not turned on
    /// yet. One at a time: several at once would composite together and show nothing useful about
    /// any of them.
    /// </summary>
    private OverlayEffect? _preview;

    private bool IsRunning
    {
        get
        {
            lock (_sync)
                return _cancellation != null;
        }
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Offsets the window blit by this frame's shake, when shake is window-scoped. Applied to the
    /// rectangle the screen render target is drawn with, which is the only thing that can displace
    /// the UI along with the world.
    /// </summary>
    /// <param name="destRect">The rectangle the render target is about to be drawn into.</param>
    /// <returns>The same rectangle, displaced if the shake covers the window.</returns>
    public Rectangle ApplyWindowShake(Rectangle destRect)
    {
        // Called unconditionally, because this is what advances the decay - see FrameShakeOffset.
        Point offset = FrameShakeOffset();

        if (DecorationSettings.Current.Shake.FullScreen)
            destRect.Offset(offset);

        return destRect;
    }

    /// <summary>
    /// Offsets the world composite by this frame's shake, when shake is viewport-scoped. Applied
    /// inside the scene so the gumps and cursor stay put while the world moves under them.
    /// </summary>
    /// <param name="destRect">The rectangle the world render target is about to be drawn into.</param>
    /// <returns>The same rectangle, displaced if the shake is confined to the viewport.</returns>
    public Rectangle ApplyViewportShake(Rectangle destRect)
    {
        Point offset = FrameShakeOffset();

        if (!DecorationSettings.Current.Shake.FullScreen)
            destRect.Offset(offset);

        return destRect;
    }

    /// <summary>
    /// This frame's shake displacement, computed once however many passes ask for it.
    /// <para>
    /// Sampling is what decays the trauma, so it must happen exactly once per frame: twice and the
    /// shake dies at double speed, never and it accumulates. It is also sampled while shake is
    /// switched off - at zero intensity - so pending trauma drains away rather than being banked
    /// until someone turns it back on.
    /// </para>
    /// </summary>
    private Point FrameShakeOffset()
    {
        if (_shakeTick == Time.Ticks)
            return _shakeOffset;

        _shakeTick = Time.Ticks;

        DecorationSettings settings = DecorationSettings.Current;
        float intensity = settings.ShakeActive ? MathHelper.Clamp(settings.Shake.Intensity, 0f, 1f) : 0f;

        _shakeOffset = ScreenShake.Instance.GetOffset(Time.Delta, intensity);

        return _shakeOffset;
    }

    /// <summary>
    /// Draws the viewport-scoped overlays. Called by the scene once the world is composited but
    /// before any gump is drawn, which is what keeps them off the UI.
    /// </summary>
    /// <param name="batcher">The batcher to draw with; must not be mid-batch.</param>
    /// <param name="viewport">The game viewport, in the batcher's coordinate space.</param>
    /// <param name="scene">The world as already rendered, for layers that distort it.</param>
    public void DrawViewportOverlays(UltimaBatcher2D batcher, Rectangle viewport, ScreenOverlaySource scene) =>
        ScreenOverlayCompositor.Instance.Draw(batcher, viewport, OverlayScope.Viewport, scene);

    /// <summary>
    /// Draws the window-scoped overlays, over everything the frame has drawn.
    /// </summary>
    /// <param name="batcher">The batcher to draw with; must not be mid-batch.</param>
    /// <param name="destRect">The rectangle the screen was blitted into, shake included.</param>
    /// <param name="scene">The composited frame, for layers that distort it.</param>
    public void DrawFullScreenOverlays(UltimaBatcher2D batcher, Rectangle destRect, ScreenOverlaySource scene) =>
        ScreenOverlayCompositor.Instance.Draw(batcher, destRect, OverlayScope.FullScreen, scene);

    /// <summary>
    /// Begins reconciling on an interval. Idempotent - a second call while already running does
    /// nothing.
    /// </summary>
    public void Start()
    {
        CancellationToken token;

        lock (_sync)
        {
            if (_cancellation != null)
                return;

            _cancellation = new CancellationTokenSource();
            token = _cancellation.Token;
        }

        _ = Task.Run(() => ReconcilerLoop(token), token);
    }

    /// <summary>
    /// Whether <paramref name="effect" /> is the one currently being previewed.
    /// </summary>
    /// <param name="effect">The effect to test.</param>
    /// <returns>True if it is the active preview.</returns>
    public bool IsPreviewing(OverlayEffect effect)
    {
        lock (_sync)
            return _preview == effect;
    }

    /// <summary>
    /// Shows or stops showing <paramref name="effect" /> irrespective of the player's state, for
    /// tuning it in the options.
    /// <para>
    /// Starting one preview stops any other. Takes effect on the next reconcile pass rather than
    /// immediately, so it goes through the same path as a real onset and cannot leave the compositor
    /// holding something the manager has forgotten about.
    /// </para>
    /// <para>
    /// Still subject to the two system toggles: with screen decorations or overlays switched off
    /// nothing is drawn, and a preview is not a reason to override that.
    /// </para>
    /// </summary>
    /// <param name="effect">The effect to preview.</param>
    /// <param name="previewing">True to show it, false to stop.</param>
    public void SetPreview(OverlayEffect effect, bool previewing)
    {
        lock (_sync)
        {
            if (!previewing && _preview != effect)
                return;

            _preview = previewing ? effect : null;
        }

        QueuePass();
    }

    /// <summary>Stops any preview. For closing the options, where nothing is left to drive it.</summary>
    public void ClearPreview()
    {
        lock (_sync)
        {
            if (_preview == null)
                return;

            _preview = null;
        }

        QueuePass();
    }

    /// <summary>
    /// Stops reconciling and fades out everything that was running. For leaving the world, where the
    /// state that justified an overlay is about to stop existing.
    /// </summary>
    public void Reset()
    {
        CancellationTokenSource? cancellation;
        OverlayId[] shown;

        lock (_sync)
        {
            cancellation = _cancellation;
            _cancellation = null;
            _passPending = false;
            _preview = null;

            shown = [.. _shown];
            _shown.Clear();
        }

        cancellation?.Cancel();
        cancellation?.Dispose();

        foreach (OverlayId id in shown)
            ScreenOverlayCompositor.Instance.Hide(id);
    }

    #endregion

    #region Private methods

    private async Task ReconcilerLoop(CancellationToken token)
    {
        try
        {
            using var timer = new PeriodicTimer(_reconcileInterval);

            while (await timer.WaitForNextTickAsync(token))
                QueuePass();
        }
        catch (OperationCanceledException)
        {
            // Reset() during the wait. Nothing to unwind: it has already cleared the overlays.
        }
        catch (Exception e)
        {
            // The loop is the only thing driving overlays; dying silently would leave them frozen
            // on whatever was last shown, with no clue as to why.
            Log.Error($"Screen overlay reconcile loop stopped: {e}");
        }
    }

    private void QueuePass()
    {
        lock (_sync)
        {
            if (_passPending)
                return;

            _passPending = true;
        }

        MainThreadQueue.EnqueueAction(RunPass);
    }

    /// <summary>
    /// Main thread. Reads the player's state and brings the compositor in line with it.
    /// </summary>
    private void RunPass()
    {
        try
        {
            // A pass queued just before Reset() still runs; without this it would re-show what Reset
            // just took down.
            if (!IsRunning)
                return;

            DecorationSettings settings = DecorationSettings.Current;

            // Everything is unwanted while the systems are off, so the same pass that starts
            // overlays is what takes them down when the toggle flips.
            bool systemActive = settings.OverlaysActive;
            PlayerCondition conditions = systemActive ? PlayerConditionReader.ReadPlayer() : PlayerCondition.None;

            OverlayEffect? preview;

            lock (_sync)
                preview = _preview;

            // Every effect, not just the mapped ones: an effect with no condition behind it still
            // has to be reconciled, or a preview of it could never be taken down again.
            foreach (OverlayEffect effect in OverlaySystemSettings.AllEffects)
            {
                OverlayEffectGeneralSettings effectSettings = settings.Overlays.GetSettings(effect);
                EffectMapping? mapping = FindMapping(effect);

                bool triggered = effectSettings.Enabled
                                 && mapping != null
                                 && (conditions & mapping.Value.Condition) != 0;

                bool previewing = effect == preview;
                bool wanted = systemActive && (triggered || previewing);
                OverlayId id = SlotFor(effect);

                if (wanted == IsShown(id))
                    continue;

                if (!wanted)
                {
                    Stop(id);
                    continue;
                }

                // A preview outranks anything the player's state asks for, and fires no onset shake:
                // it is being looked at deliberately, not reacted to.
                int priority = previewing ? PREVIEW_PRIORITY : mapping!.Value.Priority;
                float trauma = previewing ? 0f : mapping!.Value.OnsetTrauma;

                Start(effect, effectSettings, priority, trauma);
            }
        }
        finally
        {
            lock (_sync)
                _passPending = false;
        }
    }

    private bool IsShown(OverlayId id)
    {
        lock (_sync)
            return _shown.Contains(id);
    }

    /// <summary>
    /// Hands one effect to the compositor and records that it is running.
    /// </summary>
    /// <param name="effect">The effect to show.</param>
    /// <param name="settings">Its settings, supplying the profile and the drawing scope.</param>
    /// <param name="priority">Composite order against the other active overlays.</param>
    /// <param name="onsetTrauma">Screen shake to fire alongside it; zero for none.</param>
    private void Start(OverlayEffect effect, OverlayEffectGeneralSettings settings, int priority, float onsetTrauma)
    {
        ScreenOverlayPreset? preset = ResolvePreset(effect, settings);

        if (preset == null)
            return;

        OverlayId id = SlotFor(effect);
        OverlayScope scope = settings.FullScreen ? OverlayScope.FullScreen : OverlayScope.Viewport;

        ScreenOverlayCompositor.Instance.Show(id, preset, priority, scope);

        lock (_sync)
            _shown.Add(id);

        // Gated separately: someone who turned shake off still wants the tint.
        if (onsetTrauma > 0f && DecorationSettings.Current.ShakeActive)
            ScreenShake.Instance.Trauma(ShakeRequest.Decay(_onsetShakeDuration, onsetTrauma));
    }

    /// <summary>
    /// The compositor slot an effect occupies. One per effect, so showing an effect twice replaces
    /// rather than stacks.
    /// </summary>
    /// <param name="effect">The effect to place.</param>
    /// <returns>Its slot.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The effect has no slot, which means one was
    /// added to <see cref="OverlayEffect" /> without a home here.</exception>
    private static OverlayId SlotFor(OverlayEffect effect) =>
        effect switch
        {
            OverlayEffect.Bleed => OverlayId.Bleed,
            OverlayEffect.Poison => OverlayId.Poison,
            OverlayEffect.MortalStrike => OverlayId.MortalStrike,
            OverlayEffect.Fog => OverlayId.Fog,
            OverlayEffect.Drunk => OverlayId.Drunk,
            OverlayEffect.Concussion => OverlayId.Concussion,
            _ => throw new ArgumentOutOfRangeException(nameof(effect), effect, "No overlay slot for this effect.")
        };

    /// <summary>
    /// The player state that triggers <paramref name="effect" />, if any.
    /// </summary>
    /// <param name="effect">The effect to look up.</param>
    /// <returns>Its mapping, or null for an effect nothing triggers yet.</returns>
    private static EffectMapping? FindMapping(OverlayEffect effect)
    {
        foreach (EffectMapping mapping in _mappings)
        {
            if (mapping.Effect == effect)
                return mapping;
        }

        return null;
    }

    private void Stop(OverlayId id)
    {
        ScreenOverlayCompositor.Instance.Hide(id);

        lock (_sync)
            _shown.Remove(id);
    }

    /// <summary>
    /// The user's chosen profile if there is one, otherwise the stock look. Null for an effect that
    /// has neither, which is how the ones without a built-in preset stay dormant until someone
    /// authors a profile for them.
    /// </summary>
    private static ScreenOverlayPreset? ResolvePreset(OverlayEffect effect, OverlayEffectGeneralSettings settings)
    {
        OverlayEffectProfile? profile = settings.ResolveProfile();

        if (profile != null)
            return new CustomOverlayPreset(profile);

        return BuiltInOverlayPresets.Create(effect);
    }

    #endregion

    /// <param name="Condition">The player state that calls for this effect.</param>
    /// <param name="Effect">The configurable effect, and so the settings and profiles behind it.</param>
    /// <param name="Priority">Higher composites on top and survives the concurrency cap.</param>
    /// <param name="OnsetTrauma">Screen shake to fire when it starts; zero for none.</param>
    private readonly record struct EffectMapping(
        PlayerCondition Condition,
        OverlayEffect Effect,
        int Priority,
        float OnsetTrauma
    );
}
