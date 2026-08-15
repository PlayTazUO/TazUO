using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Lock = System.Threading.Lock;

namespace ClassicUO.Game.ScreenDecorations.Shake;

/// <summary>
/// Trauma-model screen shake (Squirrel Eiserloh, GDC 2016). A CPU-side pixel offset applied to
/// the render-target blit rectangle - not a shader effect. Only meaningful where such a
/// rectangle exists; see <see cref="ClassicUO.GameController"/>'s render-target draw branch.
/// <para>
/// Two ways in: <see cref="AddTrauma"/> for an instant hit that decays on its own, and
/// <see cref="Trauma(in ShakeRequest)"/> for a shake with a fixed duration and a shaped envelope.
/// Both feed the same offset, so they can overlap.
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
    /// </summary>
    internal static volatile bool Enabled = true;

    #endregion

    #region Private members

    private const float DECAY_PER_SECOND = 1.2f;

    private const float FREQUENCY = 20f;
    private const int NOISE_SAMPLES = 256;

    /// <summary>Cap on concurrent shaped shakes. Past a handful they sum into mush anyway, and
    /// the list must not grow without bound if something schedules one per frame.</summary>
    private const int MAX_ACTIVE_SHAKES = 8;

    /// <summary>Walked between shakes so two of them never sample the noise in lockstep, which
    /// would read as one shake at twice the amplitude.</summary>
    private const float PHASE_STEP = 61.7f;

    /// <summary>Fixed offset between the two streams so X and Y never correlate.</summary>
    private const float AXIS_PHASE = 37.1f;

    private readonly float[] _noiseX;
    private readonly float[] _noiseY;
    private readonly List<ActiveShake> _active = [];
    private readonly Lock _sync = new();

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
    /// Schedules a shaped shake. Ignored if it would do nothing; if the cap is already reached
    /// the quietest shake in flight is displaced, since it is the one least likely to be missed.
    /// </summary>
    public void Trauma(in ShakeRequest request)
    {
        if (!Enabled || request.Duration <= TimeSpan.Zero || request.Intensity <= 0f)
            return;

        lock (_sync)
        {
            var shake = new ActiveShake { Request = request, Phase = NextPhase() };

            if (_active.Count < MAX_ACTIVE_SHAKES)
                _active.Add(shake);
            else
                _active[IndexOfQuietest()] = shake;
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

    private int IndexOfQuietest()
    {
        int quietest = 0;
        float lowest = float.MaxValue;

        for (int i = 0; i < _active.Count; i++)
        {
            float amplitude = ShakeEnvelope.Evaluate(_active[i].Request, _active[i].Elapsed);

            if (amplitude >= lowest)
                continue;

            lowest = amplitude;
            quietest = i;
        }

        return quietest;
    }

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
        public ShakeRequest Request;
        public float Elapsed;
        public float Phase;
    }
}
