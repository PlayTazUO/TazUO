#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations;
using ClassicUO.Game.Managers;
using ClassicUO.Game.ScreenDecorations.Manager.Triggers;
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
/// <para>
/// The timer exists only while there is something for it to do: in the world, with overlays
/// switched on. Switching them off tears the loop down rather than leaving it waking the main
/// thread twice a second to re-discover that the feature is disabled, which is the state most
/// clients are in.
/// </para>
/// <para>
/// Threading: every entry point except the shake accessors is main thread only, asserted in debug
/// builds. Only <see cref="_cancellation"/> and <see cref="_passPending"/> cross threads - the timer
/// touches nothing else - so they alone are under <see cref="_sync"/> and the rest of the state
/// needs no guarding.
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
    private static readonly TimeSpan _reconcileInterval = TimeSpan.FromMilliseconds(350);

    /// <summary>How hard an onset shake hits and for how long. Shaped as an impact: full strength
    /// immediately, falling away.</summary>
    private static readonly TimeSpan _onsetShakeDuration = TimeSpan.FromSeconds(0.6);

    /// <summary>
    /// Priority a previewed overlay composites at. Above every mapping, so previewing an effect
    /// while the player happens to be poisoned shows the one that was asked for.
    /// </summary>
    private const int PREVIEW_PRIORITY = 100;

    /// <summary>
    /// Guards <see cref="_cancellation"/> and <see cref="_passPending"/>, the only state the timer
    /// thread reaches. Never held across a reconcile pass - that runs on the main thread, and
    /// blocking the timer thread on it would let passes queue up behind a stalled frame.
    /// </summary>
    private readonly Lock _sync = new();

    /// <summary>Cancels the reconcile loop. Null exactly when no loop is running, which is what
    /// <see cref="IsRunning"/> reports. Read from the timer thread by <see cref="QueuePass"/>.</summary>
    private CancellationTokenSource? _cancellation;

    /// <summary>Set while a pass is queued or running, so a slow frame cannot leave several passes
    /// stacked up waiting on the main thread. Set by the timer thread, cleared by the main
    /// one.</summary>
    private bool _passPending;

    /// <summary>What this manager has asked for, and on what terms. Not the compositor's own set:
    /// that one still holds overlays part-way through their fade-out, which must not read as
    /// "already showing". The demand is kept so a pass can tell a restated one from an unchanged
    /// one and re-apply only when something actually moved.</summary>
    private readonly Dictionary<OverlayId, OverlayDemand> _showing = [];

    /// <summary>
    /// Registered event triggers and the latest signal each has raised. Keyed by the trigger itself
    /// - it is already unique and the caller already holds it, so handing back an id would only be
    /// one more thing to lose.
    /// </summary>
    private readonly Dictionary<IEffectEventTrigger, EventSignal> _eventTriggers = [];

    /// <summary>Whether the shipped event triggers have been created. Once per session, not once
    /// per world: registrations outlive a trip to the login screen.</summary>
    private bool _builtInTriggersAdded;

    /// <summary>Whether the world is loaded. One half of what decides the loop runs; the settings
    /// are the other half.</summary>
    private bool _inWorld;

    /// <summary>
    /// The settings the change subscriptions are attached to. Kept rather than re-read, so the same
    /// instance is detached from later: <see cref="DecorationSettings.Current"/> is replaced
    /// wholesale when a profile is loaded.
    /// </summary>
    private DecorationSettings? _watched;

    /// <summary>This frame's shake displacement and the frame it was sampled on. Render thread
    /// only.</summary>
    private Point _shakeOffset;

    private long _shakeTick = -1;

    /// <summary>
    /// The effect being previewed from the options, shown regardless of the player's state and of
    /// its own enabled toggle - the point of a preview is to see an effect you have not turned on
    /// yet. One at a time: several at once would composite together and show nothing useful about
    /// any of them.
    /// </summary>
    private OverlayEffectSlot? _preview;

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
    public static void DrawViewportOverlays(UltimaBatcher2D batcher, Rectangle viewport, ScreenOverlaySource scene) =>
        ScreenOverlayCompositor.Instance.Draw(batcher, viewport, OverlayScope.Viewport, scene);

    /// <summary>
    /// Draws the window-scoped overlays, over everything the frame has drawn.
    /// </summary>
    /// <param name="batcher">The batcher to draw with; must not be mid-batch.</param>
    /// <param name="destRect">The rectangle the screen was blitted into, shake included.</param>
    /// <param name="scene">The composited frame, for layers that distort it.</param>
    public static void DrawFullScreenOverlays(UltimaBatcher2D batcher, Rectangle destRect, ScreenOverlaySource scene) =>
        ScreenOverlayCompositor.Instance.Draw(batcher, destRect, OverlayScope.FullScreen, scene);

    /// <summary>
    /// Marks the world as loaded and starts reconciling, if the settings call for it. Idempotent - a
    /// second call while already in the world does nothing.
    /// <para>
    /// Also subscribes to the two switches that gate the whole system, so turning overlays on or off
    /// starts or stops the loop then and there rather than on the next pass - there is no next pass
    /// once it has stopped.
    /// </para>
    /// </summary>
    public void Start()
    {
        AssertMainThread();

        if (_inWorld)
            return;

        AddBuiltInEventTriggers();

        _inWorld = true;
        Watch(DecorationSettings.Current);

        foreach (IEffectEventTrigger trigger in _eventTriggers.Keys)
            AttachTrigger(trigger);

        SyncReconciler();
    }

    /// <summary>
    /// Adds an event-driven trigger. It begins listening at once if the world is loaded, and on the
    /// next <see cref="Start" /> otherwise - a registration survives a trip to the login screen.
    /// <para>
    /// The trigger is its own handle: pass the same instance to <see cref="UnregisterTrigger" />.
    /// Registering one twice does nothing.
    /// </para>
    /// </summary>
    /// <param name="trigger">The trigger to add.</param>
    /// <exception cref="ArgumentNullException">The trigger is null.</exception>
    public void RegisterTrigger(IEffectEventTrigger trigger)
    {
        AssertMainThread();
        ArgumentNullException.ThrowIfNull(trigger);

        if (!_eventTriggers.TryAdd(trigger, new EventSignal()))
            return;

        trigger.Activated += OnTriggerActivated;
        trigger.Deactivated += OnTriggerDeactivated;

        if (_inWorld)
            AttachTrigger(trigger);
    }

    /// <summary>
    /// Removes a trigger added by <see cref="RegisterTrigger" /> and drops whatever it was asking
    /// for. Unknown triggers are ignored.
    /// </summary>
    /// <param name="trigger">The instance that was registered.</param>
    public void UnregisterTrigger(IEffectEventTrigger trigger)
    {
        AssertMainThread();

        if (trigger == null || !_eventTriggers.Remove(trigger))
            return;

        trigger.Activated -= OnTriggerActivated;
        trigger.Deactivated -= OnTriggerDeactivated;

        if (_inWorld)
            DetachTrigger(trigger);

        // Its signal is gone, so anything it was holding up should come down.
        QueuePass();
    }

    /// <summary>
    /// Whether <paramref name="effectSlot" /> is the one currently being previewed.
    /// </summary>
    /// <param name="effectSlot">The effect to test.</param>
    /// <returns>True if it is the active preview.</returns>
    public bool IsPreviewing(OverlayEffectSlot effectSlot)
    {
        AssertMainThread();

        return _preview == effectSlot;
    }

    /// <summary>
    /// Shows or stops showing <paramref name="effectSlot" /> irrespective of the player's state, for
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
    /// <param name="effectSlot">The effect to preview.</param>
    /// <param name="previewing">True to show it, false to stop.</param>
    public void SetPreview(OverlayEffectSlot effectSlot, bool previewing)
    {
        AssertMainThread();

        if (!previewing && _preview != effectSlot)
            return;

        _preview = previewing ? effectSlot : null;

        QueuePass();
    }

    /// <summary>Stops any preview. For closing the options, where nothing is left to drive it.</summary>
    public void ClearPreview()
    {
        AssertMainThread();

        if (_preview == null)
            return;

        _preview = null;

        QueuePass();
    }

    /// <summary>
    /// Stops reconciling and fades out everything that was running. For leaving the world, where the
    /// state that justified an overlay is about to stop existing.
    /// </summary>
    public void Reset()
    {
        AssertMainThread();

        _inWorld = false;
        Unwatch();

        // Triggers stay registered but stop listening, and anything they were asserting is dropped:
        // the world their signals described is going away.
        foreach ((IEffectEventTrigger trigger, EventSignal signal) in _eventTriggers)
        {
            DetachTrigger(trigger);
            signal.Clear();
        }

        SyncReconciler();
    }

    #endregion

    #region Private methods

    /// <summary>Registers the triggers shipped with the client, once per session.</summary>
    private void AddBuiltInEventTriggers()
    {
        if (_builtInTriggersAdded)
            return;

        _builtInTriggersAdded = true;

        foreach (IEffectEventTrigger trigger in EffectTriggerRegistry.CreateEventTriggers())
            RegisterTrigger(trigger);
    }

    /// <summary>
    /// Lets a trigger hook its own event source. Wrapped, because that is arbitrary code and one
    /// trigger failing to attach must not stop the rest - or the world - from coming up.
    /// </summary>
    /// <param name="trigger">The trigger to attach.</param>
    private static void AttachTrigger(IEffectEventTrigger trigger)
    {
        try
        {
            trigger.Register();
        }
        catch (Exception e)
        {
            Log.Error($"Overlay trigger {trigger.GetType().Name} failed to register: {e}");
        }
    }

    /// <summary>Unhooks a trigger's event source. Wrapped for the same reason as
    /// <see cref="AttachTrigger" />, and more so: failing here leaks a subscription.</summary>
    /// <param name="trigger">The trigger to detach.</param>
    private static void DetachTrigger(IEffectEventTrigger trigger)
    {
        try
        {
            trigger.UnRegister();
        }
        catch (Exception e)
        {
            Log.Error($"Overlay trigger {trigger.GetType().Name} failed to unregister: {e}");
        }
    }

    /// <summary>
    /// Records an occurrence. Marshalled, because a trigger raises this from wherever its source
    /// lives and the rest of this class is main thread only.
    /// </summary>
    private void OnTriggerActivated(object sender, EffectActivationArgs e)
    {
        if (sender is not IEffectEventTrigger trigger)
            return;

        OverlayModulation modulation = e?.Modulation ?? OverlayModulation.Default;

        MainThreadQueue.InvokeOnMainThread(() => ApplyActivation(trigger, modulation));
    }

    private void OnTriggerDeactivated(object sender, EventArgs e)
    {
        if (sender is not IEffectEventTrigger trigger)
            return;

        MainThreadQueue.InvokeOnMainThread(() => ApplyDeactivation(trigger));
    }

    /// <summary>
    /// Raises a trigger's signal. Re-activating one already up restates its modulation and, for a
    /// headless trigger, restarts the clock - a second quake landing inside the first extends it
    /// rather than being swallowed.
    /// </summary>
    /// <param name="trigger">The trigger that fired.</param>
    /// <param name="modulation">What it is asking for.</param>
    private void ApplyActivation(IEffectEventTrigger trigger, OverlayModulation modulation)
    {
        AssertMainThread();

        // Unregistered between the raise and this running, if it was marshalled.
        if (!_eventTriggers.TryGetValue(trigger, out EventSignal signal))
            return;

        signal.Active = true;
        signal.Modulation = modulation;
        signal.ExpiresAt = trigger.HeadlessDuration is { } duration ? DateTime.UtcNow + duration : null;

        QueuePass();
    }

    private void ApplyDeactivation(IEffectEventTrigger trigger)
    {
        AssertMainThread();

        if (!_eventTriggers.TryGetValue(trigger, out EventSignal signal) || !signal.Active)
            return;

        signal.Clear();
        QueuePass();
    }

    /// <summary>
    /// Retires headless signals whose span has run out. They have no second event to end them, so
    /// the pass that would otherwise only read state is what takes them down.
    /// </summary>
    private void ExpireHeadlessSignals()
    {
        DateTime now = DateTime.UtcNow;

        foreach (EventSignal signal in _eventTriggers.Values)
        {
            // Null never compares true, which is what leaves stateful signals alone.
            if (signal.Active && signal.ExpiresAt <= now)
                signal.Clear();
        }
    }

    /// <summary>
    /// Attaches to the switches that gate the system. Both objects are needed: property change
    /// notifications do not bubble from the nested settings to their parent.
    /// </summary>
    /// <param name="settings">The settings to follow.</param>
    private void Watch(DecorationSettings settings)
    {
        Unwatch();

        _watched = settings;
        _watched.PropertyChanged += OnSettingsChanged;
        _watched.Overlays.PropertyChanged += OnSettingsChanged;
    }

    /// <summary>Detaches from whatever <see cref="Watch"/> attached to.</summary>
    private void Unwatch()
    {
        if (_watched == null)
            return;

        _watched.PropertyChanged -= OnSettingsChanged;
        _watched.Overlays.PropertyChanged -= OnSettingsChanged;
        _watched = null;
    }

    /// <summary>
    /// Not filtered by property name: the two that matter are on different objects, and the work
    /// this does when nothing relevant changed is a bool comparison under a lock, at the rate a
    /// person can move an options widget.
    /// </summary>
    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e) => SyncReconciler();

    /// <summary>
    /// Brings the reconcile loop in line with whether anything could need it. Starting fires a pass
    /// immediately rather than after the first interval, so an overlay the player already qualifies
    /// for does not wait to appear; stopping fades out whatever was showing, since nothing is left
    /// to take it down later.
    /// </summary>
    private void SyncReconciler()
    {
        AssertMainThread();

        bool wanted = _inWorld && DecorationSettings.Current.OverlaysActive;

        CancellationTokenSource? started = null;
        CancellationTokenSource? stopped = null;

        lock (_sync)
        {
            if (wanted == (_cancellation != null))
                return;

            if (wanted)
            {
                started = _cancellation = new CancellationTokenSource();
            }
            else
            {
                stopped = _cancellation;
                _cancellation = null;
                _passPending = false;
            }
        }

        if (started != null)
        {
            CancellationToken token = started.Token;

            _ = Task.Run(() => ReconcilerLoop(token), token);
            QueuePass();

            return;
        }

        stopped?.Cancel();
        stopped?.Dispose();

        // A preview cannot outlive the loop: with nothing reconciling, there would be no path back
        // from it.
        _preview = null;

        foreach (OverlayId id in _showing.Keys)
            ScreenOverlayCompositor.Instance.Hide(id);

        _showing.Clear();
    }

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

    /// <summary>
    /// Asks for a pass on the main thread. Drops the request when no loop is running, so callers
    /// that fire on demand - <see cref="SetPreview"/> - cost nothing while the system is off.
    /// </summary>
    private void QueuePass()
    {
        lock (_sync)
        {
            if (_passPending || _cancellation == null)
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

            // Nothing is wanted while the system is off. The loop is normally torn down before a
            // pass can observe that, but reconciling it anyway is what keeps a missed transition
            // from leaving an overlay stuck on screen.
            bool systemActive = settings.OverlaysActive;
            OverlayEffectSlot? preview = _preview;

            ExpireHeadlessSignals();

            // Every effect, not just the triggered ones: an effect nothing triggers still has to be
            // reconciled, or a preview of it could never be taken down again.
            foreach (OverlayEffectSlot effect in OverlaySystemSettings.AllEffects)
            {
                OverlayEffectGeneralSettings effectSettings = settings.Overlays.GetSettings(effect);
                OverlayId id = SlotFor(effect);

                OverlayDemand? demand = systemActive
                    ? ResolveDemand(effect, effectSettings, effect == preview)
                    : null;

                bool shown = _showing.TryGetValue(id, out OverlayDemand current);

                if (demand == null)
                {
                    if (shown)
                        Stop(id);

                    continue;
                }

                // Already running on the same terms. One effect owns one slot, so a second reason to
                // show it has nothing left to do - but a changed one does, since the occurrence
                // behind it may have grown.
                if (shown && current == demand.Value)
                    continue;

                Start(effect, effectSettings, demand.Value, shown);
            }
        }
        finally
        {
            lock (_sync)
                _passPending = false;
        }
    }

    /// <summary>
    /// What, if anything, is asking for <paramref name="effectSlot" /> right now.
    /// <para>
    /// A preview answers before any condition is read. It is deliberate, so the player's state is
    /// irrelevant, and so is the effect's own toggle - previewing is how you decide whether to set
    /// that toggle at all. It also fires no onset shake: it is being looked at, not reacted to.
    /// </para>
    /// <para>
    /// Event triggers answer next, because their signals are already known where a poll has to reach
    /// into the game. An occurrence outranks an ambient state for the same effect regardless of
    /// priority - only one instance can be up, and the discrete thing that just happened is the one
    /// worth showing.
    /// </para>
    /// <para>
    /// Failing that, the first polling trigger that fires wins and the rest go unevaluated. A
    /// further match could not change what is drawn, and the conditions reach into live game state,
    /// which is worth not doing for no result.
    /// </para>
    /// </summary>
    /// <param name="effectSlot">The effect to judge.</param>
    /// <param name="settings">Its settings, for the enabled toggle.</param>
    /// <param name="previewing">Whether this is the effect being previewed from the options.</param>
    /// <returns>How it should composite, or null if nothing wants it.</returns>
    private OverlayDemand? ResolveDemand(
        OverlayEffectSlot effectSlot,
        OverlayEffectGeneralSettings settings,
        bool previewing
    )
    {
        if (previewing)
            return new OverlayDemand(PREVIEW_PRIORITY, OverlayModulation.Default);

        if (!settings.Enabled)
            return null;

        OverlayDemand? fromEvent = ResolveEventDemand(effectSlot);

        if (fromEvent != null)
            return fromEvent;

        foreach (EffectPollingTrigger trigger in EffectTriggerRegistry.GetTriggersForEffect(effectSlot))
        {
            if (IsTriggerConditionMet(trigger))
                return new OverlayDemand(trigger.Priority, new OverlayModulation { OnsetTrauma = trigger.OnsetTrauma });
        }

        return null;
    }

    /// <summary>
    /// The strongest live event signal for <paramref name="effectSlot" />. Highest priority wins where
    /// several are up at once, since that is the one the compositor would favour anyway.
    /// </summary>
    /// <param name="effectSlot">The effect to look for.</param>
    /// <returns>Its demand, or null if no registered trigger is asserting it.</returns>
    private OverlayDemand? ResolveEventDemand(OverlayEffectSlot effectSlot)
    {
        OverlayDemand? best = null;

        foreach ((IEffectEventTrigger trigger, EventSignal signal) in _eventTriggers)
        {
            if (!signal.Active || trigger.EffectSlot != effectSlot)
                continue;

            if (best == null || trigger.Priority > best.Value.Priority)
                best = new OverlayDemand(trigger.Priority, signal.Modulation);
        }

        return best;
    }

    /// <summary>
    /// Evaluates one trigger's condition. Conditions are arbitrary delegates reading live game
    /// state; one throwing must not take down the pass and leave every other effect unreconciled.
    /// </summary>
    /// <param name="pollingTrigger">The trigger to test.</param>
    /// <returns>Whether it fires, false if it threw.</returns>
    private static bool IsTriggerConditionMet(EffectPollingTrigger pollingTrigger)
    {
        try
        {
            return pollingTrigger.Condition();
        }
        catch (Exception e)
        {
            Log.Warn($"Failed to evaluate overlay condition: {e}");
            return false;
        }
    }

    /// <summary>
    /// Hands one effect to the compositor and records the terms it is running on.
    /// </summary>
    /// <param name="effectSlot">The effect to show.</param>
    /// <param name="settings">Its settings, supplying the profile and the drawing scope.</param>
    /// <param name="demand">What is asking for it, and how.</param>
    /// <param name="restating">Whether this is adjusting an overlay already on screen rather than
    /// raising a new one.</param>
    private void Start(
        OverlayEffectSlot effectSlot,
        OverlayEffectGeneralSettings settings,
        OverlayDemand demand,
        bool restating
    )
    {
        ScreenOverlayPreset? preset = ResolvePreset(effectSlot, settings);

        if (preset == null)
            return;

        OverlayId id = SlotFor(effectSlot);
        OverlayScope scope = settings.FullScreen ? OverlayScope.FullScreen : OverlayScope.Viewport;

        ScreenOverlayCompositor.Instance.Show(id, preset, demand.Modulation, demand.Priority, scope);
        _showing[id] = demand;

        // Only a genuine onset shakes. Restating a modulation is the same occurrence continuing, and
        // re-hitting the player for it would turn a sustained effect into a rattle.
        if (restating || demand.Modulation.OnsetTrauma <= 0f)
            return;

        // Gated separately: someone who turned shake off still wants the tint.
        if (DecorationSettings.Current.ShakeActive)
            ScreenShake.Instance.Trauma(ShakeRequest.Decay(_onsetShakeDuration, demand.Modulation.OnsetTrauma));
    }

    /// <summary>
    /// The compositor slot an effect occupies. One per effect, so showing an effect twice replaces
    /// rather than stacks.
    /// </summary>
    /// <param name="effectSlot">The effect to place.</param>
    /// <returns>Its slot.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The effect has no slot, which means one was
    /// added to <see cref="OverlayEffectSlot" /> without a home here.</exception>
    private static OverlayId SlotFor(OverlayEffectSlot effectSlot) =>
        effectSlot switch
        {
            OverlayEffectSlot.Bleed => OverlayId.Bleed,
            OverlayEffectSlot.Poison => OverlayId.Poison,
            OverlayEffectSlot.MortalStrike => OverlayId.MortalStrike,
            OverlayEffectSlot.Fog => OverlayId.Fog,
            OverlayEffectSlot.Drunk => OverlayId.Drunk,
            OverlayEffectSlot.Concussion => OverlayId.Concussion,
            _ => throw new ArgumentOutOfRangeException(nameof(effectSlot), effectSlot, @"No overlay slot for this effect")
        };

    private void Stop(OverlayId id)
    {
        ScreenOverlayCompositor.Instance.Hide(id);
        _showing.Remove(id);
    }

    /// <summary>
    /// Enforces the confinement most of this class's state relies on. Debug only - it is a wiring
    /// mistake, not a runtime condition, and the cost of being wrong is silent corruption rather
    /// than an exception that would point at it.
    /// </summary>
    /// <param name="caller">Filled in by the compiler.</param>
    [Conditional("DEBUG")]
    private static void AssertMainThread([CallerMemberName] string? caller = null) =>
        Debug.Assert(MainThreadQueue.IsMainThread, $"ScreenOverlayManager.{caller} must run on the main thread");

    /// <summary>
    /// The user's chosen profile if there is one, otherwise the stock look. Null for an effect that
    /// has neither, which is how the ones without a built-in preset stay dormant until someone
    /// authors a profile for them.
    /// </summary>
    private static ScreenOverlayPreset? ResolvePreset(OverlayEffectSlot effectSlot, OverlayEffectGeneralSettings settings)
    {
        OverlayEffectProfile? profile = settings.ResolveProfile();

        if (profile != null)
            return new CustomOverlayPreset(profile);

        return BuiltInOverlayPresets.Create(effectSlot);
    }

    #endregion

    /// <summary>
    /// A settled call for one effect to be showing, with whatever the winning trigger - or the
    /// preview - asks it to composite as. Null instead of this means nothing wants the effect.
    /// <para>
    /// Compared by value between passes, which is what tells a restated demand from an unchanged
    /// one: a trigger asking for more than it was is a re-apply, everything else is a no-op.
    /// </para>
    /// </summary>
    /// <param name="Priority">Higher composites on top and survives the concurrency cap.</param>
    /// <param name="Modulation">How far this occurrence departs from the effect's profile.</param>
    private readonly record struct OverlayDemand(int Priority, OverlayModulation Modulation);

    /// <summary>
    /// The latest thing one registered event trigger said. Mutable and long-lived - a signal
    /// outlives the events that change it - so a class rather than a struct in the dictionary.
    /// </summary>
    private sealed class EventSignal
    {
        /// <summary>Whether the occurrence is currently being asserted.</summary>
        public bool Active;

        /// <summary>What the most recent activation asked for.</summary>
        public OverlayModulation Modulation = OverlayModulation.Default;

        /// <summary>When a headless activation lapses, or null for a trigger that ends itself.</summary>
        public DateTime? ExpiresAt;

        /// <summary>Drops the signal, whichever shape it was.</summary>
        public void Clear()
        {
            Active = false;
            ExpiresAt = null;
        }
    }
}
