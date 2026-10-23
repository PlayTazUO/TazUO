#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Xna.Framework;
using Lock = System.Threading.Lock;

namespace ClassicUO.Game.ScreenDecorations.Shake;

/// <summary>
/// What raised a shake. Each source is capped on its own, so one of them looping cannot evict the
/// other's shakes.
/// </summary>
internal enum ShakeSource
{
    /// <summary>A rule firing, or a preview of the look it raises.</summary>
    Rule,

    /// <summary>A Python script, via the scripting API.</summary>
    Script
}

/// <summary>
/// Trauma-model screen shake (Squirrel Eiserloh, GDC 2016). A CPU-side pixel offset applied to
/// the render-target blit rectangle - not a shader effect. Only meaningful where such a
/// rectangle exists; see <see cref="ClassicUO.GameController"/>'s render-target draw branch.
/// <para>
/// Two ways in: <see cref="AddTrauma"/> for an instant hit that decays on its own, and
/// <see cref="Trauma(in ShakeRequest, ShakeSource, object)"/> for a shake with a fixed duration and
/// a shaped envelope. Both feed the same offset, so they can overlap.
/// </para>
/// <para>
/// Shakes are raised from wherever the event that caused them is handled - network, script or UI
/// threads - while the offset is read from the draw loop, so every entry point takes the same
/// lock. The contended section is a handful of arithmetic over at most
/// <see cref="MAX_ACTIVE_SHAKES"/> entries.
/// </para>
/// </summary>
internal sealed class ScreenShake
{
    #region Public accessors

    /// <summary>
    /// Shake confined to the game world, leaving the gumps and cursor still.
    /// </summary>
    public static ScreenShake Viewport { get; } = new(0f);

    /// <summary>
    /// Shake that displaces the whole window, UI included.
    /// <para>
    /// A separate accumulator rather than a flag on the offset, because the two are applied to
    /// different rectangles at different points in the frame: one profile can shake the world
    /// while another shakes the window, and each has to decay on its own. Their contributions
    /// compound on the world, which is correct - two things really are shaking it.
    /// </para>
    /// </summary>
    public static ScreenShake Window { get; } = new(PHASE_STEP * 0.5f);

    /// <summary>
    /// The accumulator a request of the given scope belongs to.
    /// </summary>
    /// <param name="fullScreen">Whether the shake should displace the whole window.</param>
    /// <returns>The accumulator.</returns>
    public static ScreenShake For(bool fullScreen) => fullScreen ? Window : Viewport;

    public bool IsShaking
    {
        get
        {
            lock (_sync)
                return HasWork;
        }
    }

    #endregion

    #region Internal members

    /// <summary>Hard per-axis ceiling on shake displacement, regardless of intensity - the final
    /// clamp in <see cref="GetOffset"/> enforces it. Internal so render-target margins sized to
    /// absorb the shake can read the same constant instead of duplicating it.</summary>
    internal const float MaxOffsetPixels = 24f;

    /// <summary>
    /// Whether the shake system is on in settings. Written by
    /// <see cref="ClassicUO.Game.ScreenDecorations.Manager.ScreenOverlayManager"/> on settings
    /// change, not read from settings here - every entry point below checks it so trauma raised
    /// while off is discarded rather than banked for an instant jolt on re-enable.
    /// <para>
    /// Volatile rather than lock-guarded: it gates nothing compound, so the only guarantees needed
    /// are that a reader cannot hoist it out of a loop and that the write lands before the
    /// accumulator clears that follow it. Taking <see cref="_sync"/> for it would put a lock on
    /// every raise from every thread to read one bool.
    /// </para>
    /// <para>
    /// Leaves one window open: a raise can pass the check here, stall, and take the lock after a
    /// concurrent disable has already cleared, banking one shake across the toggle. Costs a single
    /// stray jolt, and only a lock-held read would close it.
    /// </para>
    /// <para>
    /// Starts false, and the manager clears it again on leaving the world: there is no rectangle to
    /// displace outside a world session, so a shake raised there would bank trauma nothing could
    /// draw and nothing would ask for.
    /// </para>
    /// </summary>
    internal static volatile bool Enabled;

    #endregion

    #region Private members

    private const float DECAY_PER_SECOND = 1.2f;

    private const float FREQUENCY = 20f;
    private const int NOISE_SAMPLES = 256;

    /// <summary>
    /// How many shaped shakes a rule may hold. Past a handful they sum into mush anyway, and the
    /// list must not grow without bound if something schedules one per frame.
    /// </summary>
    private const int MAX_RULE_SHAKES = 8;

    /// <summary>
    /// How many a script may hold, alongside <see cref="MAX_RULE_SHAKES"/> rather than carved out of
    /// it: a script looping on <c>StartScreenShake</c> must not displace or crowd out the shakes the
    /// player's own state is raising, and those are the ones that carry information.
    /// </summary>
    private const int MAX_SCRIPT_SHAKES = 4;

    /// <summary>Ceiling on the list itself, which the two independent caps add up to.</summary>
    private const int MAX_ACTIVE_SHAKES = MAX_RULE_SHAKES + MAX_SCRIPT_SHAKES;

    /// <summary>Walked between shakes so two of them never sample the noise in lockstep, which
    /// would read as one shake at twice the amplitude.</summary>
    private const float PHASE_STEP = 61.7f;

    /// <summary>Fixed offset between the two streams so X and Y never correlate.</summary>
    private const float AXIS_PHASE = 37.1f;

    private readonly float[] _noiseX;
    private readonly float[] _noiseY;
    /// <summary>Sized to the ceiling up front: the per-source caps mean it never has to grow, and
    /// it is walked under the lock every frame.</summary>
    private readonly List<ActiveShake> _active = new(MAX_ACTIVE_SHAKES);
    private readonly Lock _sync = new();

    /// <summary>
    /// Handles the two accumulators hand out, shared between them so one id names exactly one shake
    /// however it was scoped - a caller holding a handle does not know, and should not have to know,
    /// which rectangle its shake ended up displacing.
    /// </summary>
    private static uint _nextId;

    private float _trauma;
    private float _time;
    private float _nextPhase;

    /// <summary>Caller holds <see cref="_sync"/>.</summary>
    private bool HasWork => _trauma > 0f || _active.Count > 0;

    #endregion

    #region Ctor

    /// <param name="phaseOffset">Where this accumulator starts walking the noise table. Set
    /// apart per instance so two scopes shaking at once do not sample in lockstep, which would
    /// read as one shake at twice the amplitude.</param>
    private ScreenShake(float phaseOffset)
    {
        _nextPhase = phaseOffset;
        _noiseX = BuildSmoothedNoise(1);
        _noiseY = BuildSmoothedNoise(2);
    }

    #endregion

    #region Public methods

    public void SetTrauma(float amount)
    {
        if (!Enabled)
            return;

        lock (_sync)
            _trauma = MathHelper.Clamp(amount, 0f, 1f);
    }

    public void AddTrauma(float amount)
    {
        if (!Enabled)
            return;

        lock (_sync)
            _trauma = MathHelper.Clamp(_trauma + amount, 0f, 1f);
    }

    /// <summary>Even shake for <paramref name="duration"/>, starting and stopping abruptly.</summary>
    public void Trauma(TimeSpan duration, float intensity) =>
        Trauma(ShakeRequest.Constant(duration, intensity));

    /// <summary>Even shake eased in and out over the given windows.</summary>
    public void Trauma(TimeSpan duration, float intensity, TimeSpan rampUp, TimeSpan rampDown)
    {
        var request = ShakeRequest.Constant(duration, intensity);
        request.RampUp = rampUp;
        request.RampDown = rampDown;
        request.Curve = ShakeCurve.Smooth;

        Trauma(request);
    }

    /// <summary>
    /// Schedules a shaped shake. Ignored if it would do nothing.
    /// <para>
    /// Caps are per source and independent, so a source neither evicts nor crowds out another's
    /// shakes: once this source has filled its own allowance the quietest of <em>its</em> shakes is
    /// displaced, since that is the one least likely to be missed.
    /// </para>
    /// </summary>
    /// <param name="request">The envelope to schedule.</param>
    /// <param name="source">What raised it, which decides the cap it counts against.</param>
    /// <param name="owner">Opaque handle on whatever raised it, for <see cref="CancelByOwner"/>.
    /// Only scripts need one.</param>
    /// <returns>The shake's id, for <see cref="Cancel"/>, or 0 if nothing was scheduled.</returns>
    public uint Trauma(in ShakeRequest request, ShakeSource source = ShakeSource.Rule, object? owner = null)
    {
        if (!Enabled || request.Duration <= TimeSpan.Zero || request.Intensity <= 0f)
            return 0;

        uint id = Interlocked.Increment(ref _nextId);

        lock (_sync)
        {
            var shake = new ActiveShake
            {
                Id = id,
                Request = request,
                Phase = NextPhase(),
                Source = source,
                Owner = owner
            };

            // The per-source caps sum to the list ceiling, so filling one is the only way the list
            // can be full - which is what keeps a script's shakes off the rules' allowance.
            if (CountOf(source) >= CapOf(source))
            {
                _active[IndexOfQuietest(source)] = shake;

                return id;
            }

            _active.Add(shake);
        }

        return id;
    }

    /// <summary>
    /// Ends one shaped shake early, if <paramref name="owner"/> is the one that raised it.
    /// <para>
    /// Ownership is checked rather than trusted: ids are monotonic small integers, so without it any
    /// caller holding one handle could walk the range and cancel a rule's shakes or another script's
    /// - which is exactly what the per-source caps exist to prevent.
    /// </para>
    /// </summary>
    /// <param name="id">The id <see cref="Trauma(in ShakeRequest, ShakeSource, object)"/> returned.
    /// Unknown, expired and displaced ids are ignored, as is 0.</param>
    /// <param name="owner">The owner passed to
    /// <see cref="Trauma(in ShakeRequest, ShakeSource, object)"/>.</param>
    /// <returns>True if a shake <paramref name="owner"/> raised was still running under that id.</returns>
    public bool Cancel(uint id, object owner)
    {
        ArgumentNullException.ThrowIfNull(owner);

        if (id == 0)
            return false;

        lock (_sync)
        {
            for (int i = 0; i < _active.Count; i++)
            {
                if (_active[i].Id != id || !ReferenceEquals(_active[i].Owner, owner))
                    continue;

                _active.RemoveAt(i);

                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Ends every shaped shake <paramref name="owner"/> raised. The teardown a script relies on: a
    /// long shake outliving the script that asked for it has nothing left that could stop it.
    /// </summary>
    /// <param name="owner">The owner passed to <see cref="Trauma(in ShakeRequest, ShakeSource, object)"/>.</param>
    public void CancelByOwner(object owner)
    {
        ArgumentNullException.ThrowIfNull(owner);

        lock (_sync)
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                if (ReferenceEquals(_active[i].Owner, owner))
                    _active.RemoveAt(i);
            }
        }
    }

    /// <summary>Ends everything at once, shaped or decaying.</summary>
    public void Clear()
    {
        lock (_sync)
        {
            _active.Clear();
            _trauma = 0f;
        }
    }

    /// <summary>
    /// Advances by <paramref name="dt"/> seconds and returns the current pixel offset, scaled by
    /// <paramref name="intensity"/> (the settings multiplier, already clamped to [0, 1] by the
    /// caller).
    /// </summary>
    public Point GetOffset(float dt, float intensity)
    {
        lock (_sync)
        {
            _trauma = MathHelper.Clamp(_trauma - DECAY_PER_SECOND * dt, 0f, 1f);
            _time += dt;

            Advance(dt);

            if (intensity <= 0f || !HasWork)
                return Point.Zero;

            float x = 0f;
            float y = 0f;
            float limit = 0f;

            if (_trauma > 0f)
            {
                // Squared here rather than inside Accumulate. This accumulator is the classic
                // trauma model, where the square is what makes a hit fall away sharply as it
                // decays; a shaped request already carries an authored envelope, and squaring that
                // too would bend every curve a profile asks for into its own square.
                Accumulate(_trauma * _trauma, FREQUENCY, MaxOffsetPixels, 0f, ref x, ref y);
                limit = MaxOffsetPixels;
            }

            foreach (ActiveShake shake in _active)
            {
                float amplitude = ShakeEnvelope.Evaluate(shake.Request, shake.Elapsed);

                if (amplitude <= 0f)
                    continue;

                float maxPixels = shake.Request.MaxOffsetPixels > 0f ? shake.Request.MaxOffsetPixels : MaxOffsetPixels;
                float frequency = shake.Request.Frequency > 0f ? shake.Request.Frequency : FREQUENCY;

                Accumulate(amplitude, frequency, maxPixels, shake.Phase, ref x, ref y);
                limit = MathF.Max(limit, maxPixels);
            }

            // Contributions are summed, so overlapping shakes are clamped rather than allowed to
            // throw the screen further than any one of them asked for.
            x = MathHelper.Clamp(x, -limit, limit) * intensity;
            y = MathHelper.Clamp(y, -limit, limit) * intensity;

            return new Point((int)MathF.Round(x), (int)MathF.Round(y));
        }
    }

    #endregion

    #region Private methods

    // The instance helpers below all touch mutable state and are only called with _sync held.
    // The static ones are pure and need no lock: the noise tables are built in the constructor
    // and never written again.

    private void Advance(float dt)
    {
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            ActiveShake shake = _active[i];
            shake.Elapsed += dt;

            if (shake.Elapsed > (float)shake.Request.Duration.TotalSeconds)
                _active.RemoveAt(i);
            else
                _active[i] = shake;
        }
    }

    /// <summary>
    /// Adds one contribution's displacement to the running total.
    /// </summary>
    /// <param name="amplitude">Final amplitude, 0-1, as a fraction of <paramref name="maxPixels"/>.
    /// Taken as authored - any shaping the caller wants has already been applied.</param>
    /// <param name="frequency">Rate to walk the noise table at, in Hz.</param>
    /// <param name="maxPixels">Displacement at an amplitude of 1.</param>
    /// <param name="phase">Where in the noise table this contribution starts.</param>
    /// <param name="x">Running total, added to.</param>
    /// <param name="y">Running total, added to.</param>
    private void Accumulate(float amplitude, float frequency, float maxPixels, float phase, ref float x, ref float y)
    {
        x += maxPixels * amplitude * Sample(_noiseX, _time * frequency + phase);
        y += maxPixels * amplitude * Sample(_noiseY, _time * frequency + phase + AXIS_PHASE);
    }

    private float NextPhase()
    {
        _nextPhase = (_nextPhase + PHASE_STEP) % NOISE_SAMPLES;

        return _nextPhase;
    }

    /// <summary>
    /// The faintest shake this source has in flight, which is the one a new request of the same
    /// source displaces.
    /// </summary>
    /// <param name="source">Only this source's shakes are candidates.</param>
    /// <returns>Its index, or -1 if the source has nothing running - which its only caller has
    /// already ruled out by reaching the source's cap.</returns>
    private int IndexOfQuietest(ShakeSource source)
    {
        int quietest = -1;
        float lowest = float.MaxValue;

        for (int i = 0; i < _active.Count; i++)
        {
            if (_active[i].Source != source)
                continue;

            float amplitude = ShakeEnvelope.Evaluate(_active[i].Request, _active[i].Elapsed);

            if (amplitude >= lowest)
                continue;

            lowest = amplitude;
            quietest = i;
        }

        return quietest;
    }

    /// <summary>Caller holds <see cref="_sync"/>.</summary>
    private int CountOf(ShakeSource source)
    {
        int count = 0;

        foreach (ActiveShake shake in _active)
        {
            if (shake.Source == source)
                count++;
        }

        return count;
    }

    private static int CapOf(ShakeSource source) => source == ShakeSource.Script ? MAX_SCRIPT_SHAKES : MAX_RULE_SHAKES;

    private static float Sample(float[] table, float t)
    {
        int count = table.Length;
        float scaled = t % count;

        if (scaled < 0f)
            scaled += count;

        int i0 = (int)scaled;
        int i1 = (i0 + 1) % count;
        float frac = scaled - i0;

        return MathHelper.Lerp(table[i0], table[i1], frac);
    }

    // Precomputed, lightly-smoothed random samples. Sampled per frame, never regenerated -
    // uncorrelated per-frame randomness reads as buzzing, not shaking.
    private static float[] BuildSmoothedNoise(int seed)
    {
        float[] raw = new float[NOISE_SAMPLES];
        var rand = new Random(seed);

        for (int i = 0; i < NOISE_SAMPLES; i++)
            raw[i] = (float)(rand.NextDouble() * 2.0 - 1.0);

        float[] smoothed = new float[NOISE_SAMPLES];

        for (int i = 0; i < NOISE_SAMPLES; i++)
        {
            float prev = raw[(i - 1 + NOISE_SAMPLES) % NOISE_SAMPLES];
            float cur = raw[i];
            float next = raw[(i + 1) % NOISE_SAMPLES];
            smoothed[i] = (prev + 2f * cur + next) * 0.25f;
        }

        return smoothed;
    }

    #endregion

    private struct ActiveShake
    {
        /// <summary>Identity callers stop it by. Unique across both accumulators.</summary>
        public uint Id;

        public ShakeRequest Request;
        public float Elapsed;
        public float Phase;

        /// <summary>What raised it, and so which cap it counts against.</summary>
        public ShakeSource Source;

        /// <summary>Whatever raised it, for bulk cancellation. Null for everything but scripts.</summary>
        public object? Owner;
    }
}
