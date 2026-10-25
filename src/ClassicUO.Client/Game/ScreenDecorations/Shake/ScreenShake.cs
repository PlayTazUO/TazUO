#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Xna.Framework;
using Lock = System.Threading.Lock;

namespace ClassicUO.Game.ScreenDecorations.Shake;

/// <summary>
///     Trauma-model screen shake (Squirrel Eiserloh, GDC 2016), computed on the CPU as a pixel offset.
///     <para>
///         Two ways in: <see cref="AddTrauma" /> for an instant hit that decays on its own, and
///         <see cref="Trauma(in ShakeRequest, in ShakeOrigin)" /> for a fixed duration and a shaped
///         envelope. Both feed the same offset, so they can overlap.
///     </para>
///     <para>
///         Raises come from network, script and UI threads while the offset is read from the draw loop,
///         so every entry point takes the same lock.
///     </para>
/// </summary>
internal sealed class ScreenShake
{
    #region Public accessors

    /// <summary>Shake confined to the game world, leaving the gumps and cursor still.</summary>
    public static ScreenShake Viewport { get; } = new(0f);

    /// <summary>
    ///     Shake that displaces the whole window, UI included. Decays independently of
    ///     <see cref="Viewport" />, and the two compound on the world when both are running.
    /// </summary>
    public static ScreenShake Window { get; } = new(PHASE_STEP * 0.5f);

    /// <summary>The accumulator a request of the given scope belongs to.</summary>
    /// <param name="fullScreen">Whether the shake should displace the whole window.</param>
    /// <returns>The accumulator.</returns>
    public static ScreenShake For(bool fullScreen) => fullScreen ? Window : Viewport;

    /// <summary>Whether anything is currently displacing this scope.</summary>
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

    /// <summary>
    ///     Hard per-axis ceiling on displacement, whatever the intensity. Internal so render-target
    ///     margins sized to absorb the shake can read the same number.
    /// </summary>
    internal const float MaxOffsetPixels = 24f;

    /// <summary>
    ///     Whether shake is on, mirrored here by
    ///     <see cref="ClassicUO.Game.ScreenDecorations.Manager.ScreenOverlayManager" /> on settings and
    ///     world changes. Every entry point checks it, so a raise while off is discarded rather than
    ///     banked for a jolt on re-enable. Volatile rather than locked - it gates nothing compound, and a
    ///     raise racing a disable costs at most one stray jolt.
    /// </summary>
    internal static volatile bool Enabled;

    #endregion

    #region Private members

    private const float DECAY_PER_SECOND = 1.2f;

    private const float FREQUENCY = 20f;
    private const int NOISE_SAMPLES = 256;

    /// <summary>How many shaped shakes a rule may hold. Past a handful they sum into mush anyway.</summary>
    private const int MAX_RULE_SHAKES = 8;

    /// <summary>
    ///     How many a script may hold. Alongside <see cref="MAX_RULE_SHAKES" /> rather than carved out of
    ///     it, so a looping script cannot crowd out the player's own.
    /// </summary>
    private const int MAX_SCRIPT_SHAKES = 4;

    /// <summary>Ceiling on the list itself, which the two independent caps add up to.</summary>
    private const int MAX_ACTIVE_SHAKES = MAX_RULE_SHAKES + MAX_SCRIPT_SHAKES;

    /// <summary>Walked between shakes so no two sample in lockstep, which doubles the apparent amplitude.</summary>
    private const float PHASE_STEP = 61.7f;

    /// <summary>Fixed offset between the two streams so X and Y never correlate.</summary>
    private const float AXIS_PHASE = 37.1f;

    private readonly float[] _noiseX;
    private readonly float[] _noiseY;

    /// <summary>Sized to the ceiling up front, since the caps mean it never has to grow.</summary>
    private readonly List<ActiveShake> _active = new(MAX_ACTIVE_SHAKES);

    private readonly Lock _sync = new();

    /// <summary>Shared between both accumulators, so one ID names exactly one shake whichever scope it landed in.</summary>
    private static uint _nextId;

    private float _trauma;

    /// <summary>
    ///     Where in the noise table the decaying accumulator is now. Walked and wrapped per frame, never
    ///     multiplied out of a running clock - a clock's precision thins as it grows until the position
    ///     stops moving at all, leaving a shake that pushes and releases without oscillating.
    /// </summary>
    private float _traumaPosition;

    private float _nextPhase;

    /// <summary>Caller holds <see cref="_sync" />.</summary>
    private bool HasWork => _trauma > 0f || _active.Count > 0;

    #endregion

    #region Ctor

    /// <param name="phaseOffset">
    ///     Where this accumulator starts walking the noise table. Set apart per instance so two scopes
    ///     shaking at once do not sample in lockstep.
    /// </param>
    private ScreenShake(float phaseOffset)
    {
        _nextPhase = phaseOffset;
        _traumaPosition = phaseOffset;
        _noiseX = BuildSmoothedNoise(1);
        _noiseY = BuildSmoothedNoise(2);
    }

    #endregion

    #region Public methods

    /// <summary>Sets the decaying accumulator outright. Clamped to [0, 1].</summary>
    public void SetTrauma(float amount)
    {
        if (!Enabled)
            return;

        lock (_sync)
            _trauma = MathHelper.Clamp(amount, 0f, 1f);
    }

    /// <summary>Adds to the decaying accumulator, so repeated hits build. Total clamped to [0, 1].</summary>
    public void AddTrauma(float amount)
    {
        if (!Enabled)
            return;

        lock (_sync)
            _trauma = MathHelper.Clamp(_trauma + amount, 0f, 1f);
    }

    /// <summary>Even shake for <paramref name="duration" />, starting and stopping abruptly.</summary>
    /// <param name="duration">How long it runs.</param>
    /// <param name="intensity">Peak trauma, 0-1.</param>
    /// <param name="origin">Who is asking.</param>
    public void Trauma(TimeSpan duration, float intensity, in ShakeOrigin origin) =>
        Trauma(ShakeRequest.Constant(duration, intensity), origin);

    /// <summary>Even shake eased in and out over the given windows.</summary>
    /// <param name="duration">How long it runs, ramps included.</param>
    /// <param name="intensity">Peak trauma, 0-1.</param>
    /// <param name="rampUp">Fade-in window from the start.</param>
    /// <param name="rampDown">Fade-out window before the end.</param>
    /// <param name="origin">Who is asking.</param>
    public void Trauma(TimeSpan duration, float intensity, TimeSpan rampUp, TimeSpan rampDown, in ShakeOrigin origin)
    {
        var request = ShakeRequest.Constant(duration, intensity);
        request.RampUp = rampUp;
        request.RampDown = rampDown;
        request.Curve = ShakeCurve.Smooth;

        Trauma(request, origin);
    }

    /// <summary>
    ///     Schedules a shaped shake. Ignored if it would do nothing.
    ///     <para>
    ///         Allowances are per source and independent: filling one displaces the quietest of
    ///         <em>its own</em> shakes, never another source's.
    ///     </para>
    /// </summary>
    /// <param name="request">The envelope to schedule.</param>
    /// <param name="origin">Who is asking, which decides the allowance and what can cancel it.</param>
    /// <returns>The shake's ID, for <see cref="Cancel" />, or 0 if nothing was scheduled.</returns>
    public uint Trauma(in ShakeRequest request, in ShakeOrigin origin)
    {
        if (!Enabled || request.Duration <= TimeSpan.Zero || request.Intensity <= 0f)
            return 0;

        uint id = Interlocked.Increment(ref _nextId);

        lock (_sync)
        {
            var shake = new ActiveShake { Id = id, Request = request, Position = NextPhase(), Origin = origin };

            // Resolved into the stored copy, so the held request is the effective one.
            if (shake.Request.Frequency <= 0f)
                shake.Request.Frequency = FREQUENCY;

            // The caps sum to the list ceiling, so filling one is the only way the list can be full.
            if (CountOf(origin.Source) >= CapOf(origin.Source))
            {
                _active[IndexOfQuietest(origin.Source)] = shake;

                return id;
            }

            _active.Add(shake);
        }

        return id;
    }

    /// <summary>
    ///     Ends one shaped shake early, if <paramref name="owner" /> is the one that raised it. Ownership
    ///     is checked rather than trusted: IDs are small consecutive integers, so a caller holding one
    ///     could otherwise walk the range and cancel whatever it found.
    /// </summary>
    /// <param name="id">
    ///     The ID <see cref="Trauma(in ShakeRequest, in ShakeOrigin)" /> returned. Unknown, expired and
    ///     displaced IDs are ignored, as is 0.
    /// </param>
    /// <param name="owner">The owner that raised it.</param>
    /// <returns>True if a shake <paramref name="owner" /> raised was still running under that ID.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="owner" /> is null.</exception>
    public bool Cancel(uint id, object owner)
    {
        ArgumentNullException.ThrowIfNull(owner);

        if (id == 0)
            return false;

        lock (_sync)
            for (int i = 0; i < _active.Count; i++)
            {
                if (_active[i].Id != id || !ReferenceEquals(_active[i].Origin.Owner, owner))
                    continue;

                _active.RemoveAt(i);

                return true;
            }

        return false;
    }

    /// <summary>
    ///     Ends every shaped shake <paramref name="owner" /> raised. The teardown a script relies on:
    ///     a long shake outliving its script has nothing left that could stop it.
    /// </summary>
    /// <param name="owner">The owner that raised them.</param>
    /// <exception cref="ArgumentNullException"><paramref name="owner" /> is null.</exception>
    public void CancelByOwner(object owner)
    {
        ArgumentNullException.ThrowIfNull(owner);

        lock (_sync)
            for (int i = _active.Count - 1; i >= 0; i--)
                if (ReferenceEquals(_active[i].Origin.Owner, owner))
                    _active.RemoveAt(i);
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
    ///     Advances the accumulator and returns this frame's offset. Must be called exactly once per
    ///     frame per scope: twice and the shake decays at double speed, never and it stalls.
    /// </summary>
    /// <param name="dt">Seconds since the last call.</param>
    /// <param name="intensity">The settings multiplier, already clamped to [0, 1] by the caller.</param>
    /// <returns>The displacement in pixels, or <see cref="Point.Zero" /> while nothing is shaking.</returns>
    public Point GetOffset(float dt, float intensity)
    {
        lock (_sync)
        {
            _trauma = MathHelper.Clamp(_trauma - DECAY_PER_SECOND * dt, 0f, 1f);
            _traumaPosition = WrapPosition(_traumaPosition + dt * FREQUENCY);

            Advance(dt);

            if (intensity <= 0f || !HasWork)
                return Point.Zero;

            float x = 0f;
            float y = 0f;
            float limit = 0f;

            if (_trauma > 0f)
            {
                // Squared here, not in Accumulate: the square is what makes a hit fall away sharply,
                // and applying it to a shaped request would bend every authored curve into it.
                Accumulate(_trauma * _trauma, _traumaPosition, MaxOffsetPixels, ref x, ref y);
                limit = MaxOffsetPixels;
            }

            foreach (ActiveShake shake in _active)
            {
                float amplitude = ShakeEnvelope.Evaluate(shake.Request, shake.Elapsed);

                if (amplitude <= 0f)
                    continue;

                float maxPixels = shake.Request.MaxOffsetPixels > 0f ? shake.Request.MaxOffsetPixels : MaxOffsetPixels;

                Accumulate(amplitude, shake.Position, maxPixels, ref x, ref y);
                limit = MathF.Max(limit, maxPixels);
            }

            // Contributions sum, so the clamp keeps overlapping shakes from throwing the screen
            // further than any one of them asked for.
            x = MathHelper.Clamp(x, -limit, limit) * intensity;
            y = MathHelper.Clamp(y, -limit, limit) * intensity;

            return new Point((int)MathF.Round(x), (int)MathF.Round(y));
        }
    }

    #endregion

    #region Private methods

    // The instance helpers below touch mutable state and are only called with _sync held. The static
    // ones are pure: the noise tables are built in the constructor and never written again.

    /// <summary>Ages every active shake, dropping the ones that are spent.</summary>
    private void Advance(float dt)
    {
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            ActiveShake shake = _active[i];
            shake.Elapsed += dt;
            shake.Position = WrapPosition(shake.Position + dt * shake.Request.Frequency);

            if (shake.Elapsed > (float)shake.Request.Duration.TotalSeconds)
                _active.RemoveAt(i);
            else
                _active[i] = shake;
        }
    }

    /// <summary>
    ///     Adds one contribution to the running totals. <paramref name="amplitude" /> is final - any
    ///     shaping is already applied - and scales <paramref name="maxPixels" />.
    /// </summary>
    private void Accumulate(float amplitude, float position, float maxPixels, ref float x, ref float y)
    {
        x += maxPixels * amplitude * Sample(_noiseX, position);
        y += maxPixels * amplitude * Sample(_noiseY, position + AXIS_PHASE);
    }

    /// <summary>
    ///     Holds a position to one pass of the table, so what reaches <see cref="Sample" /> stays small
    ///     enough to keep its fractional part however long the client has been up.
    /// </summary>
    private static float WrapPosition(float position)
    {
        position %= NOISE_SAMPLES;
        return position < 0f ? position + NOISE_SAMPLES : position;
    }

    /// <summary>The next shake's starting place in the table, a step on from the last.</summary>
    private float NextPhase()
    {
        _nextPhase = (_nextPhase + PHASE_STEP) % NOISE_SAMPLES;

        return _nextPhase;
    }

    /// <summary>
    ///     Index of the faintest shake this source has in flight - the one a new request of the same
    ///     source displaces - or -1 if it has none.
    /// </summary>
    private int IndexOfQuietest(ShakeSource source)
    {
        int quietest = -1;
        float lowest = float.MaxValue;

        for (int i = 0; i < _active.Count; i++)
        {
            if (_active[i].Origin.Source != source)
                continue;

            float amplitude = ShakeEnvelope.Evaluate(_active[i].Request, _active[i].Elapsed);

            if (amplitude >= lowest)
                continue;

            lowest = amplitude;
            quietest = i;
        }

        return quietest;
    }

    /// <summary>Caller holds <see cref="_sync" />.</summary>
    private int CountOf(ShakeSource source)
    {
        int count = 0;

        foreach (ActiveShake shake in _active)
            if (shake.Origin.Source == source)
                count++;

        return count;
    }

    private static int CapOf(ShakeSource source) => source == ShakeSource.Script ? MAX_SCRIPT_SHAKES : MAX_RULE_SHAKES;

    /// <summary>Reads a table at a fractional entry, wrapping and interpolating. Answers -1 to 1.</summary>
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

    /// <summary>
    ///     One axis's noise table, seeded so the axes never correlate. Smoothed and reused rather than
    ///     drawn per frame: uncorrelated randomness reads as buzzing, not shaking.
    /// </summary>
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

        /// <summary>The envelope, with <see cref="ShakeRequest.Frequency" /> resolved to a real rate.</summary>
        public ShakeRequest Request;

        public float Elapsed;

        /// <summary>
        ///     Where in the noise table this shake is now. Seeded apart from the others in flight so no
        ///     two sample in lockstep, then walked per frame.
        /// </summary>
        public float Position;

        /// <summary>Who raised it: the cap it counts against, and what can cancel it.</summary>
        public ShakeOrigin Origin;
    }
}
