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
        new(PlayerCondition.MortalStruck, OverlayEffect.MortalStrike, OverlayId.MortalStrike, 30, 0.45f),
        new(PlayerCondition.Poisoned, OverlayEffect.Poison, OverlayId.Poison, 20, 0f),
        new(PlayerCondition.Bleeding, OverlayEffect.Bleed, OverlayId.Bleed, 10, 0.25f)
    ];

    /// <summary>Guards the fields below. Never held across a reconcile pass - that runs on the main
    /// thread, and blocking the timer thread on it would let passes queue up behind a stalled
    /// frame.</summary>
    private readonly Lock _sync = new();

    /// <summary>The ids this manager has asked for. Not the compositor's own set: that one still
    /// holds overlays part-way through their fade-out, which must not read as "already showing".</summary>
    private readonly HashSet<OverlayId> _shown = [];

    private CancellationTokenSource? _cancellation;

    /// <summary>Set while a pass is queued or running, so a slow frame cannot leave several passes
    /// stacked up waiting on the main thread.</summary>
    private bool _passPending;

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
    /// Offsets the blit rectangle by the current screen shake. Must be applied before the render
    /// target is drawn with it: that blit is the only thing that can move the world, and
    /// <see cref="Draw"/> is then given the same shaken rectangle so overlays travel with it.
    /// </summary>
    /// <param name="destRect">The rectangle the render target is about to be drawn into.</param>
    /// <returns>The same rectangle, displaced by this frame's shake.</returns>
    public Rectangle ApplyShake(Rectangle destRect)
    {
        DecorationSettings settings = DecorationSettings.Current;

        // GetOffset is what decays the trauma, so it is called even while shake is off - at zero
        // intensity - rather than banking whatever was pending until it is switched back on.
        float intensity = settings.ShakeActive ? MathHelper.Clamp(settings.Shake.Intensity, 0f, 1f) : 0f;

        destRect.Offset(ScreenShake.Instance.GetOffset(Time.Delta, intensity));

        return destRect;
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

            foreach (EffectMapping mapping in _mappings)
            {
                OverlayEffectGeneralSettings effect = settings.Overlays.GetSettings(mapping.Effect);
                bool wanted = systemActive && effect.Enabled && (conditions & mapping.Condition) != 0;

                if (wanted == IsShown(mapping.Id))
                    continue;

                if (wanted)
                    Start(mapping, effect);
                else
                    Stop(mapping.Id);
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

    private void Start(in EffectMapping mapping, OverlayEffectGeneralSettings settings)
    {
        ScreenOverlayPreset? preset = ResolvePreset(mapping.Effect, settings);

        if (preset == null)
            return;

        OverlayScope scope = settings.FullScreen ? OverlayScope.FullScreen : OverlayScope.Viewport;

        ScreenOverlayCompositor.Instance.Show(mapping.Id, preset, mapping.Priority, scope);

        lock (_sync)
            _shown.Add(mapping.Id);

        // Gated separately: someone who turned shake off still wants the tint.
        if (mapping.OnsetTrauma > 0f && DecorationSettings.Current.ShakeActive)
            ScreenShake.Instance.Trauma(ShakeRequest.Decay(_onsetShakeDuration, mapping.OnsetTrauma));
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
    /// <param name="Id">The compositor slot it occupies.</param>
    /// <param name="Priority">Higher composites on top and survives the concurrency cap.</param>
    /// <param name="OnsetTrauma">Screen shake to fire when it starts; zero for none.</param>
    private readonly record struct EffectMapping(
        PlayerCondition Condition,
        OverlayEffect Effect,
        OverlayId Id,
        int Priority,
        float OnsetTrauma
    );
}
