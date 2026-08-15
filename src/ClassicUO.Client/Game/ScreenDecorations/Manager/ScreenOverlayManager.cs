#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Profiles;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Rules;
using ClassicUO.Game.Managers;
using ClassicUO.Game.ScreenDecorations.Overlays;
using ClassicUO.Game.ScreenDecorations.Rules;
using ClassicUO.Game.ScreenDecorations.Shake;
using ClassicUO.Game.ScreenDecorations.Triggers;
using ClassicUO.Renderer;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;

// The settings class shares its name with its namespace, which shadows it here.
using DecorationSettings = ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.ScreenDecorations;

namespace ClassicUO.Game.ScreenDecorations.Manager;

/// <summary>
/// Decides which overlays should be running, and drives everything else that reacts to the same
/// state - screen shake included. <see cref="ScreenOverlayCompositor"/> is told what to draw and
/// nothing more.
/// <para>
/// Reconciles rather than reacts: on each pass it works out the set of overlays the rules in force
/// call for, and moves the compositor towards it. A missed transition therefore cannot leave an
/// overlay stuck on, and disabling a rule, re-pointing it, or editing its profile lands on the next
/// pass.
/// </para>
/// <para>
/// Threading: callable from anywhere. Every mutator marshals itself to the main thread, so the state
/// behind them is single-threaded without a lock in sight. <see cref="IsPreviewing"/> is the one
/// exception - it returns a value, so it cannot be fire-and-forget - and stays main thread only.
/// </para>
/// </summary>
internal sealed class ScreenOverlayManager
{
    #region Public accessors

    /// <summary>
    /// The type initializer builds this, so the CLR guarantees it happens once however many threads
    /// reach <see cref="Instance"/> first. Still deferred until something touches the class, and it
    /// allocates nothing beyond empty collections - the noise texture and the shader are the
    /// compositor's, and are built on the frame they are first drawn with.
    /// </summary>
    public static ScreenOverlayManager Instance { get; } = new();

    /// <summary>
    /// Extra canvas, in pixels, a viewport-scope shake needs to crop into instead of exposing a
    /// render target's unrendered edge. Zero while shake is off. Recomputed on the settings events
    /// <see cref="Watch"/> attaches to rather than every frame - the render targets that size
    /// against this are rebuilt on every frame's draw call, so re-deriving it there would mean a
    /// settings dereference per frame for a value that only ever changes on an options edit.
    /// </summary>
    public int ViewportShakeMarginPixels { get; private set; }

    #endregion

    #region Private members

    /// <summary>
    /// Priority a previewed overlay composites at. Above every rule, so previewing a look while the
    /// player happens to be poisoned shows the one that was asked for.
    /// </summary>
    private const int PREVIEW_PRIORITY = 100;

    /// <summary>
    /// Compositor slot the preview occupies. A fixed id rather than the previewed profile's, so
    /// previewing a look a rule is already showing does not fight that rule for its slot.
    /// </summary>
    private static readonly Guid _previewSlot = new("e4d1a7c8-9f52-4b6e-8a31-0c7d5e29b184");

    private readonly OverlayPassScheduler _scheduler;

    /// <summary>The rules in force, and the live trigger watching for each. Rebuilt whenever the
    /// rulebase changes; disabled rules are absent, so their triggers cost nothing.</summary>
    private readonly Dictionary<Guid, WatchedRule> _watching = [];

    /// <summary>
    /// The same rules in table order, which is what makes evaluation first-match. A dictionary's
    /// enumeration order is an implementation detail, and precedence is not.
    /// </summary>
    private readonly List<WatchedRule> _ordered = [];

    /// <summary>Effects already claimed this pass, so a lower rule cannot restate one. Reused so a
    /// pass allocates nothing.</summary>
    private readonly HashSet<Guid> _claimed = [];

    /// <summary>What this manager has asked the compositor for, and on what terms. Not the
    /// compositor's own set: that one still holds overlays part-way through their fade-out, which
    /// must not read as "already showing".</summary>
    private readonly Dictionary<Guid, ShownState> _showing = [];

    /// <summary>This pass's answer. Reused so a pass allocates nothing.</summary>
    private readonly Dictionary<Guid, RuleDemand> _desired = [];

    /// <summary>Slots being taken down this pass. Reused for the same reason.</summary>
    private readonly List<Guid> _retiring = [];

    /// <summary>This pass's demands ranked by priority, for applying the concurrency cap. Reused for
    /// the same reason.</summary>
    private readonly List<RuleDemand> _ranked = [];

    /// <summary>Whether the world is loaded. One half of what decides passes run; the settings are
    /// the other half.</summary>
    private bool _inWorld;

    /// <summary>
    /// The settings the change subscriptions are attached to. Kept rather than re-read, so the same
    /// instance is detached from later: <see cref="DecorationSettings.Current"/> is replaced
    /// wholesale when a profile is loaded.
    /// </summary>
    private DecorationSettings? _watched;

    /// <summary>Shake's on/off state as of the last recompute, so turning it back on can be told
    /// apart from any other settings edit - that edge is what triggers the reset in
    /// <see cref="RecomputeShakeState"/>.</summary>
    private bool _shakeWasActive;

    /// <summary>
    /// This frame's shake displacement per scope, and the frame each was sampled on. Indexed by
    /// scope (0 viewport, 1 window) because the two decay independently. Render thread only.
    /// </summary>
    private readonly Point[] _shakeOffset = new Point[2];

    private readonly long[] _shakeTick = [-1, -1];

    /// <summary>
    /// The profile being previewed from the options, shown regardless of the player's state and of
    /// any rule - the point of a preview is to see a look nothing is raising yet. One at a time:
    /// several at once would composite together and show nothing useful about any of them.
    /// </summary>
    private Guid? _previewProfileId;

    /// <summary>
    /// Makes the next pass re-state every live occurrence rather than skipping the unchanged ones,
    /// which is what re-bakes an edited look. A flag rather than forgetting <see cref="_showing"/>:
    /// that dictionary is the only record of what the compositor holds, and a pass that cannot see a
    /// slot cannot retire it either.
    /// </summary>
    private bool _restateAll;

    #endregion

    #region Ctor

    private ScreenOverlayManager()
    {
        _scheduler = new OverlayPassScheduler(RunPass);
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Drives the reconcile passes. Called once per frame from the scene update; with the system off
    /// it is a field read and a branch.
    /// </summary>
    public void Tick() => _scheduler.Tick();

    /// <summary>
    /// Offsets the window blit by this frame's shake, when shake is window-scoped. Applied to the
    /// rectangle the screen render target is drawn with, which is the only thing that can displace
    /// the UI along with the world.
    /// </summary>
    /// <param name="destRect">The rectangle the render target is about to be drawn into.</param>
    /// <returns>The same rectangle, displaced if the shake covers the window.</returns>
    public Rectangle ApplyWindowShake(Rectangle destRect)
    {
        destRect.Offset(FrameShakeOffset(fullScreen: true));

        return destRect;
    }

    /// <summary>
    /// This frame's shake displacement for the viewport scope, in pixels. Meant to move the crop
    /// taken from the (margin-padded) world render target rather than where that crop is drawn - a
    /// shifted source reveals real rendered pixels at the edge, where a shifted destination would
    /// expose the target's empty margin instead.
    /// </summary>
    /// <returns>The offset, zero if shake is off or window-scoped only.</returns>
    public Point ViewportShakeOffset() => FrameShakeOffset(fullScreen: false);

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
    /// Also subscribes to the switches that gate the whole system, so turning overlays on or off
    /// starts or stops the work then and there rather than on the next pass - there is no next pass
    /// once it has stopped.
    /// </para>
    /// </summary>
    public void Start()
    {
        // The marshalling idiom used by every mutator here: a method group capturing only `this`
        // compiles to an instance method, so the delegate is built inside the branch and the
        // main-thread path allocates nothing. Methods taking parameters cannot do this - see
        // SetPreview.
        if (!MainThreadQueue.IsMainThread)
        {
            MainThreadQueue.InvokeOnMainThread(Start);
            return;
        }

        if (_inWorld)
            return;

        _inWorld = true;
        Watch(DecorationSettings.Current);

        SyncRules();
    }

    /// <summary>
    /// Rebuilds the live wiring from the rulebase. For the options, which edit rules in place: those
    /// edits raise no collection change, and a rule pointed at a new trigger has to stop watching the
    /// old one.
    /// </summary>
    public void RulesChanged()
    {
        if (!MainThreadQueue.IsMainThread)
        {
            MainThreadQueue.InvokeOnMainThread(RulesChanged);
            return;
        }

        SyncRules();
    }

    /// <summary>
    /// Re-applies everything on screen, picking up edits to the looks themselves. For the profile
    /// composer: a layer change does not alter what any rule is asserting, so without this the
    /// reconcile pass would find nothing to do and the compositor would keep drawing the stack it
    /// baked when the effect was raised.
    /// </summary>
    public void ProfilesChanged()
    {
        if (!MainThreadQueue.IsMainThread)
        {
            MainThreadQueue.InvokeOnMainThread(ProfilesChanged);
            return;
        }

        // Re-stating rather than hiding anything: the next pass re-asserts every live occurrence,
        // which is what re-bakes it, and does so without restarting a single fade.
        _restateAll = true;

        _scheduler.RequestPass();
    }

    /// <summary>
    /// Whether <paramref name="profileId"/> is the look currently being previewed.
    /// </summary>
    /// <param name="profileId">The profile to test.</param>
    /// <returns>True if it is the active preview.</returns>
    public bool IsPreviewing(Guid profileId)
    {
        AssertMainThread();

        return _previewProfileId == profileId;
    }

    /// <summary>
    /// Shows or stops showing a look irrespective of the player's state, for tuning it in the
    /// options.
    /// <para>
    /// Starting one preview stops any other. Takes effect on the next pass rather than immediately,
    /// so it goes through the same path as a real occurrence and cannot leave the compositor holding
    /// something the manager has forgotten about. It fires no shake: the look is being examined, not
    /// reacted to.
    /// </para>
    /// <para>
    /// Still subject to the two system toggles: with screen decorations or overlays switched off
    /// nothing is drawn, and a preview is not a reason to override that.
    /// </para>
    /// </summary>
    /// <param name="profileId">The profile to preview.</param>
    /// <param name="previewing">True to show it, false to stop.</param>
    public void SetPreview(Guid profileId, bool previewing)
    {
        // Dispatched through a separate method rather than a lambda here: a lambda capturing these
        // parameters would have its closure allocated on method entry, before the branch, so the
        // main-thread path would pay for it too.
        if (!MainThreadQueue.IsMainThread)
        {
            DispatchSetPreview(profileId, previewing);
            return;
        }

        if (!previewing && _previewProfileId != profileId)
            return;

        _previewProfileId = previewing ? profileId : null;

        _scheduler.RequestPass();
    }

    /// <summary>Stops any preview. For closing the options, where nothing is left to drive it.</summary>
    public void ClearPreview()
    {
        if (!MainThreadQueue.IsMainThread)
        {
            MainThreadQueue.InvokeOnMainThread(ClearPreview);
            return;
        }

        if (_previewProfileId == null)
            return;

        _previewProfileId = null;

        _scheduler.RequestPass();
    }

    /// <summary>
    /// Stops reconciling and fades out everything that was running. For leaving the world, where the
    /// state that justified an overlay is about to stop existing.
    /// </summary>
    public void Reset()
    {
        if (!MainThreadQueue.IsMainThread)
        {
            MainThreadQueue.InvokeOnMainThread(Reset);
            return;
        }

        _inWorld = false;
        Unwatch();
        TearDownWatching();
        SyncReconciler();
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Off-thread half of <see cref="SetPreview" />. Kept out of line so the closure its lambda needs
    /// is never allocated on the main-thread path.
    /// </summary>
    /// <param name="profileId">The profile to preview.</param>
    /// <param name="previewing">True to show it, false to stop.</param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void DispatchSetPreview(Guid profileId, bool previewing) =>
        MainThreadQueue.InvokeOnMainThread(() => SetPreview(profileId, previewing));

    /// <summary>
    /// One scope's shake displacement for this frame, computed once however many passes ask for it.
    /// <para>
    /// Sampling is what decays the trauma, so each accumulator must be sampled exactly once per
    /// frame: twice and the shake dies at double speed, never and it accumulates.
    /// </para>
    /// <para>
    /// The early-out below is an efficiency choice, not a correctness one: <see cref="ScreenShake"/>
    /// would already answer zero on its own while off, since <see cref="RecomputeShakeState"/> clears
    /// both accumulators on every on/off transition and <see cref="ScreenShake.Enabled"/> stops
    /// anything from being raised in between. Skipping the call avoids paying for a lock and a
    /// decay step to hear an answer already known - the point of this system being off is that it
    /// costs as close to nothing as possible.
    /// </para>
    /// </summary>
    /// <param name="fullScreen">Which accumulator to read.</param>
    /// <returns>The displacement, zero while shake is off.</returns>
    private Point FrameShakeOffset(bool fullScreen)
    {
        // ScreenShake.Enabled is RecomputeShakeState's cached mirror of DecorationSettings.Current
        // .ShakeActive - one static field read here instead of the settings dereference chain.
        //
        // Micro-optimization, since this runs on every tick.
        if (!ScreenShake.Enabled)
            return Point.Zero;

        int scope = fullScreen ? 1 : 0;

        if (_shakeTick[scope] == Time.Ticks)
            return _shakeOffset[scope];

        _shakeTick[scope] = Time.Ticks;

        float intensity = MathHelper.Clamp(DecorationSettings.Current.Shake.Intensity, 0f, 1f);
        _shakeOffset[scope] = ScreenShake.For(fullScreen).GetOffset(Time.Delta, intensity);

        return _shakeOffset[scope];
    }

    /// <summary>
    /// Rebuilds <see cref="_watching"/> from the rules in force. A full teardown rather than a diff:
    /// this runs on a person editing the rulebase, not per frame, and reconciling instance lifetimes
    /// against changed parameters would be far more machinery than the case is worth.
    /// </summary>
    private void SyncRules()
    {
        AssertMainThread();

        TearDownWatching();

        OverlaySystemSettings overlays = DecorationSettings.Current.Overlays;

        foreach (OverlayRule rule in overlays.ResolveRules())
        {
            WatchedRule? watched = Build(rule, overlays);

            if (watched == null)
                continue;

            _watching[rule.Id] = watched;
            _ordered.Add(watched);

            if (_inWorld)
                watched.Attach(ApplyFired, ApplyEnded);
        }

        SyncReconciler();
    }

    /// <summary>
    /// Wires one rule up, or reports why it cannot be. A rule that names something this build does
    /// not have is skipped rather than fatal: it may have been written by a newer client, or point
    /// at a profile that has since been deleted.
    /// </summary>
    /// <param name="rule">The rule to wire.</param>
    /// <param name="overlays">The settings supplying the profile pool.</param>
    /// <returns>The live wiring, or null if the rule cannot run.</returns>
    private static WatchedRule? Build(OverlayRule rule, OverlaySystemSettings overlays)
    {
        if (!rule.Enabled)
            return null;

        ITriggerDefinition? definition = TriggerCatalog.Instance.Find(rule.Trigger.DefinitionId);

        if (definition == null)
        {
            Log.Warn($"Overlay rule '{rule.Name}' names an unknown trigger '{rule.Trigger.DefinitionId}' and will not run.");
            return null;
        }

        EffectProfile? profile = overlays.FindProfile(rule.ProfileId);

        if (profile == null)
        {
            Log.Warn($"Overlay rule '{rule.Name}' points at a profile that no longer exists and will not run.");
            return null;
        }

        try
        {
            ITriggerInstance instance = definition.Create(rule.Trigger.Parameters ?? definition.CreateDefaultParameters());

            return new WatchedRule(rule, profile, instance, definition.Kind);
        }
        catch (Exception e)
        {
            Log.Error($"Overlay rule '{rule.Name}' could not build its trigger: {e}");
            return null;
        }
    }

    /// <summary>Detaches, unsubscribes and disposes every live trigger.</summary>
    private void TearDownWatching()
    {
        foreach (WatchedRule watched in _ordered)
        {
            watched.Detach();
            watched.Dispose();
        }

        _watching.Clear();
        _ordered.Clear();
    }

    /// <summary>
    /// Records an occurrence. Marshalled, because a trigger raises this from wherever its source
    /// lives and the rest of this class is main thread only.
    /// </summary>
    private void ApplyFired(WatchedRule watched, TriggerSignal signal) =>
        MainThreadQueue.InvokeOnMainThread(
            () =>
            {
                // Re-synced away between the raise and this running if it was marshaled.
                if (!IsCurrent(watched))
                    return;

                watched.Raise(signal);
                _scheduler.RequestPass();
            }
        );

    private void ApplyEnded(WatchedRule watched) =>
        MainThreadQueue.InvokeOnMainThread(
            () =>
            {
                AssertMainThread();

                if (!IsCurrent(watched))
                    return;

                watched.ClearSignal();
                _scheduler.RequestPass();
            }
        );

    private bool IsCurrent(WatchedRule watched) =>
        _watching.TryGetValue(watched.Rule.Id, out WatchedRule? current) && ReferenceEquals(current, watched);

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
        _watched.Shake.PropertyChanged += OnSettingsChanged;

        RecomputeShakeState();
    }

    /// <summary>Detaches from whatever <see cref="Watch"/> attached to.</summary>
    private void Unwatch()
    {
        if (_watched == null)
            return;

        _watched.PropertyChanged -= OnSettingsChanged;
        _watched.Overlays.PropertyChanged -= OnSettingsChanged;
        _watched.Shake.PropertyChanged -= OnSettingsChanged;
        _watched = null;

        ViewportShakeMarginPixels = 0;
        ScreenShake.Enabled = false;

        // So the next Watch() (a fresh world session) resets both accumulators if shake happens to
        // already be on, rather than carrying over whatever a previous session left mid-decay.
        _shakeWasActive = false;
    }

    /// <summary>
    /// Refreshes everything that depends on whether shake is on: the render-target margin viewport
    /// shake crops into, and the low-level gate that keeps <see cref="ScreenShake"/> from
    /// accumulating trauma while off. Either transition edge also resets both accumulators - off, so
    /// <see cref="ScreenShake.HasWork"/> is already false the instant <see cref="FrameShakeOffset"/>
    /// starts skipping it, rather than waiting out however much trauma was left to decay; on, so
    /// nothing raised (and discarded) while off replays as a jolt now that it's heard again.
    /// </summary>
    private void RecomputeShakeState()
    {
        bool active = DecorationSettings.Current.ShakeActive;

        ScreenShake.Enabled = active;
        ViewportShakeMarginPixels = active ? (int)(ScreenShake.MaxOffsetPixels * 2f) : 0;

        if (active != _shakeWasActive)
        {
            ScreenShake.Viewport.Clear();
            ScreenShake.Window.Clear();
        }

        _shakeWasActive = active;
    }

    /// <summary>
    /// Marshals like every other entry point: the settings are statically reachable, so this is
    /// raised by whichever thread wrote the property, and everything below mutates wiring unguarded.
    /// </summary>
    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!MainThreadQueue.IsMainThread)
        {
            DispatchSettingsChanged(e.PropertyName);
            return;
        }

        ApplySettingsChange(e.PropertyName);
    }

    /// <summary>
    /// Off-thread half of <see cref="OnSettingsChanged" />. Out of line so its closure never lands on
    /// the main-thread path.
    /// </summary>
    /// <param name="propertyName">The property that changed.</param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void DispatchSettingsChanged(string? propertyName) =>
        MainThreadQueue.InvokeOnMainThread(() => ApplySettingsChange(propertyName));

    /// <summary>
    /// A changed pool means the wiring is stale; anything else only decides whether the work runs at
    /// all. Not filtered more finely than that: the cost of reconciling when nothing relevant moved
    /// is a dictionary rebuild, at the rate a person can move an options' widget.
    /// </summary>
    /// <param name="propertyName">The property that changed.</param>
    private void ApplySettingsChange(string? propertyName)
    {
        AssertMainThread();

        RecomputeShakeState();

        if (propertyName is nameof(OverlaySystemSettings.Rules)
            or nameof(OverlaySystemSettings.Profiles)
            or nameof(OverlaySystemSettings.BuiltInRuleStates))
        {
            SyncRules();
            return;
        }

        SyncReconciler();
    }

    /// <summary>
    /// Brings the background work in line with whether anything could need it. Starting fires a pass
    /// immediately rather than after the first interval, so an overlay the player already qualifies
    /// for does not wait to appear; stopping fades out whatever was showing, since nothing is left
    /// to take it down later.
    /// </summary>
    private void SyncReconciler()
    {
        AssertMainThread();

        bool wanted = _inWorld && DecorationSettings.Current.OverlaysActive;

        _scheduler.SetEnabled(wanted);

        if (!wanted)
        {
            // A preview cannot outlive the passes: with nothing reconciling, there would be no path
            // back from it.
            _previewProfileId = null;

            // Dropped rather than hidden. Hiding only starts a fade, and fades advance inside the
            // compositor's draw, which stops with the system - so the stack would sit frozen at its
            // last envelope and reappear at that strength the moment the system came back on.
            ScreenOverlayCompositor.Instance.Clear();
            _showing.Clear();

            return;
        }

        _scheduler.SetPollingNeeded(NeedsPolling());
        _scheduler.RequestPass();
    }

    private bool NeedsPolling() => _ordered.Any(watched => watched.Kind == TriggerKind.Poll);

    /// <summary>
    /// Main thread. Reads what every rule is asserting and brings the compositor in line with it.
    /// </summary>
    private void RunPass()
    {
        // Read once and handed to both halves: DateTime.UtcNow is around 50x the cost of reading the
        // frame clock, so a pass takes it exactly once and works in frame ticks from there.
        DateTime now = DateTime.UtcNow;

        ExpireLapsedSignals(now);
        Resolve();
        ApplyConcurrencyCap(_desired, _ranked, ScreenOverlayCompositor.ConcurrencyCap());
        Reconcile();

        _scheduler.ScheduleExpiry(NextExpiry(now));
    }

    /// <summary>Retires occurrences whose declared span has run out.</summary>
    /// <param name="now">The instant to judge against.</param>
    private void ExpireLapsedSignals(DateTime now)
    {
        foreach (WatchedRule watched in _ordered)
            watched.ExpireIfLapsed(now);
    }

    /// <summary>
    /// When the soonest live occurrence lapses, as a frame-clock deadline, or null if none will.
    /// </summary>
    /// <param name="now">The instant to measure the deadline from.</param>
    /// <returns>The frame clock reading to wake at.</returns>
    private uint? NextExpiry(DateTime now)
    {
        DateTime? earliest = null;

        foreach (WatchedRule watched in _ordered)
        {
            if (watched.ExpiresAt is not { } at)
                continue;

            if (earliest == null || at < earliest)
                earliest = at;
        }

        return earliest is { } soonest ? OverlayPassScheduler.ToDeadline(soonest, now) : null;
    }

    /// <summary>
    /// Fills <see cref="_desired"/> with everything that should be composited right now.
    /// <para>
    /// First match wins, firewall-style: the table is walked top to bottom and the first firing rule
    /// claims its effect, so every rule below it that raises the same look is skipped. Without that,
    /// two rules on one profile would composite twice, and where the look decides something singular
    /// - whether the shake moves the window or only the world - there would be no answer as to which
    /// of them meant it.
    /// </para>
    /// <para>
    /// Claimed per profile rather than per rule, because the profile is what "the same effect" means
    /// here. Two rules raising genuinely different looks both draw, which is what composition is for.
    /// </para>
    /// </summary>
    private void Resolve()
    {
        _desired.Clear();
        _claimed.Clear();

        DecorationSettings settings = DecorationSettings.Current;

        // Nothing is wanted while the system is off. The work is normally torn down before a pass
        // can observe that, but reconciling anyway is what keeps a missed transition from leaving an
        // overlay stuck on screen.
        if (!settings.OverlaysActive)
            return;

        // Before the rules, and claiming its look like one: previewing what a rule already shows
        // would otherwise draw the stack twice at doubled alpha. Claiming first also lets it win.
        AddPreview(settings);

        SelectFirstMatches(_ordered, _claimed, _desired);
    }

    /// <summary>
    /// Adds the previewed look, if there is one, and claims it against the rules.
    /// </summary>
    /// <param name="settings">The settings supplying the profile pool.</param>
    private void AddPreview(DecorationSettings settings)
    {
        if (_previewProfileId is not { } previewId)
            return;

        EffectProfile? preview = settings.Overlays.FindProfile(previewId);

        if (preview == null)
            return;

        _claimed.Add(preview.Id);
        _desired[_previewSlot] = new RuleDemand(_previewSlot, preview, PREVIEW_PRIORITY, TriggerSignal.Default);
    }

    /// <summary>
    /// Drops the weakest demands until no more than the user's cap remain. Applied here, not in the
    /// compositor: only this class records what it asked for, and a drop it cannot see is never
    /// re-asserted. Dropping here retires through the normal path, so it fades and returns when there
    /// is room. Internal and static so the policy can be tested, like <see cref="SelectFirstMatches"/>.
    /// </summary>
    /// <param name="desired">This pass's demands; trimmed in place.</param>
    /// <param name="ranked">Scratch list, cleared by this method.</param>
    /// <param name="cap">Most overlays that may composite at once.</param>
    internal static void ApplyConcurrencyCap(Dictionary<Guid, RuleDemand> desired, List<RuleDemand> ranked, int cap)
    {
        if (desired.Count <= cap)
            return;

        ranked.Clear();

        foreach (RuleDemand demand in desired.Values)
            ranked.Add(demand);

        // ID breaks ties so survivors don't depend on hash order - equal-priority rules swapping
        // between passes would cross-fade every poll.
        ranked.Sort(
            static (left, right) => left.Priority != right.Priority
                ? right.Priority.CompareTo(left.Priority)
                : left.RuleId.CompareTo(right.RuleId)
        );

        for (int i = cap; i < ranked.Count; i++)
            desired.Remove(ranked[i].RuleId);
    }

    /// <summary>
    /// Walks the rules in table order and lets the first firing one claim each effect.
    /// <para>
    /// Internal and static so the precedence rule can be exercised on its own: it is the one part of
    /// a pass that is a policy rather than plumbing.
    /// </para>
    /// </summary>
    /// <param name="ordered">The rules in force, in table order.</param>
    /// <param name="claimed">Scratch set of already-claimed profile ids; cleared by the caller.</param>
    /// <param name="desired">Filled with the winning demand per effect, keyed by rule id.</param>
    internal static void SelectFirstMatches(
        List<WatchedRule> ordered,
        HashSet<Guid> claimed,
        Dictionary<Guid, RuleDemand> desired
    )
    {
        foreach (WatchedRule watched in ordered)
        {
            // Checked before sampling, so a rule that lost the effect to one above it does not even
            // reach into live game state to find out it was not needed.
            if (claimed.Contains(watched.Profile.Id))
                continue;

            TriggerSignal? signal = watched.Sample();

            if (signal == null)
                continue;

            claimed.Add(watched.Profile.Id);

            desired[watched.Rule.Id] = new RuleDemand(
                watched.Rule.Id,
                watched.Profile,
                watched.Rule.Priority,
                signal.Value
            );
        }
    }

    /// <summary>Moves the compositor to <see cref="_desired"/>.</summary>
    private void Reconcile()
    {
        _retiring.Clear();

        foreach (Guid id in _showing.Keys)
        {
            if (!_desired.ContainsKey(id))
                _retiring.Add(id);
        }

        foreach (Guid id in _retiring)
        {
            ScreenOverlayCompositor.Instance.Hide(id);
            _showing.Remove(id);
        }

        foreach (RuleDemand demand in _desired.Values)
        {
            var state = new ShownState(demand.Profile.Id, demand.Priority, demand.Signal.Intensity);
            bool shown = _showing.TryGetValue(demand.RuleId, out ShownState current);

            // Already running on the same terms. A restated occurrence still has work to do, since
            // the one behind it may have grown.
            if (shown && !_restateAll && current == state)
                continue;

            ScreenOverlayCompositor.Instance.Show(demand.RuleId, demand.Profile, demand.Signal.Intensity, demand.Priority);
            _showing[demand.RuleId] = state;

            if (!shown)
                FireOnsetShake(demand);
        }

        _restateAll = false;
    }

    /// <summary>
    /// Hits the player with whatever shake the look includes, once, as it arrives. Restating an
    /// occurrence is the same one continuing, and re-hitting for it would turn a sustained effect
    /// into a rattle.
    /// <para>
    /// Fires for a preview too: a preview is a rehearsal of the whole look, and shake is part of
    /// what a look is. It still only fires on onset, so toggling the preview shakes once.
    /// </para>
    /// </summary>
    /// <param name="demand">The occurrence being raised.</param>
    private static void FireOnsetShake(RuleDemand demand)
    {
        if (demand.Profile.Shake is not { } shake || shake.Trauma <= 0f)
            return;

        // Gated separately: someone who turned shake off still wants the tint.
        if (!DecorationSettings.Current.ShakeActive)
            return;

        // The look's own scope decides which rectangle moves, so one profile can rattle the world
        // while another rattles the window. The envelope - ramps, gradient, rate - is the profile's
        // too; nothing here reshapes it beyond scaling by how strong this occurrence is.
        ScreenShake.For(demand.Profile.FullScreen).Trauma(shake.ToRequest(demand.Signal.Intensity));
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

    #endregion

    /// <summary>
    /// The terms one slot is currently running on. Compared by value between passes, which is what
    /// tells a restated occurrence from an unchanged one: a trigger asking for more than it was is a
    /// re-apply, everything else is a no-op.
    /// <para>
    /// <see cref="ProfileId"/> has to be part of that comparison, not just <see cref="Priority"/> and
    /// <see cref="Intensity"/>: a slot id is stable across a profile swap (a rule re-pointed at a
    /// different look, or preview switched from one look to another), so without it those two would
    /// be indistinguishable from an unchanged occurrence and the old look's layers would keep
    /// compositing until something else forced a restate.
    /// </para>
    /// </summary>
    /// <param name="ProfileId">The look currently occupying the slot.</param>
    /// <param name="Priority">Higher composites on top and survives the concurrency cap.</param>
    /// <param name="Intensity">How strongly the occurrence behind it asked to be drawn.</param>
    private readonly record struct ShownState(Guid ProfileId, int Priority, float Intensity);
}
