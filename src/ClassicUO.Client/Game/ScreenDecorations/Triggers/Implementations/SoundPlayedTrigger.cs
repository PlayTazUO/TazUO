#nullable enable

using System;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;

namespace ClassicUO.Game.ScreenDecorations.Triggers.Implementations;

/// <summary>
/// Listens for one sound and raises an occurrence scaled by how near it was played.
/// <para>
/// Headless: a sound is announced when it starts and nothing
/// says when it stopped, so one occurrence runs for the span its parameters declare. Re-raised rather
/// than stacked if the sound plays again inside that span, so a repeating source holds the effect up
/// throughout.
/// </para>
/// </summary>
public sealed class SoundPlayedTrigger : IEventTrigger
{
    #region Public events

    /// <inheritdoc />
    public event EventHandler<TriggerFiredArgs>? Fired;

    /// <summary>Never raised - nothing announces the end of a sound, and the signal's own duration is
    /// what retires it. Accessors are empty rather than the event being omitted, because the manager
    /// subscribes to every event trigger without asking which shape it is.</summary>
    public event EventHandler? Ended
    {
        add { }
        remove { }
    }

    #endregion

    #region Private members

    private readonly SoundPlayedParameters _parameters;

    #endregion

    #region Ctor

    /// <param name="parameters">What to listen for, and how to scale it.</param>
    /// <exception cref="ArgumentNullException"><paramref name="parameters" /> is null.</exception>
    public SoundPlayedTrigger(SoundPlayedParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        _parameters = parameters;
    }

    #endregion

    #region Public methods

    /// <inheritdoc />
    public void Attach() => EventSink.SoundPlayed += OnSoundPlayed;

    /// <inheritdoc />
    public void Detach() => EventSink.SoundPlayed -= OnSoundPlayed;

    /// <inheritdoc />
    public void Dispose() => Detach();

    #endregion

    #region Internal methods

    /// <summary>
    /// What one sound is worth, or null if it is not this rule's sound or falls outside its band.
    /// <para>
    /// Kept apart from the event handler, and given the world state rather than reading it, so the
    /// filtering and the curve can be exercised without a loaded world behind them.
    /// </para>
    /// </summary>
    /// <param name="parameters">The rule's parameters.</param>
    /// <param name="soundIndex">Index of the sound that played.</param>
    /// <param name="soundX">Tile the sound came from.</param>
    /// <param name="soundY">Tile the sound came from.</param>
    /// <param name="playerX">The player's tile.</param>
    /// <param name="playerY">The player's tile.</param>
    /// <param name="viewRange">Tiles the client can see, which is also its audible cutoff.</param>
    /// <returns>The signal to raise, or null for a sound this rule does not answer to.</returns>
    internal static TriggerSignal? Evaluate(
        SoundPlayedParameters parameters,
        int soundIndex,
        int soundX,
        int soundY,
        int playerX,
        int playerY,
        int viewRange
    )
    {
        if (soundIndex != parameters.SoundIndex || viewRange <= 0)
            return null;

        // Zero means the client's own audible range. A configured band wider than that would claim
        // sounds the client never plays, so it is clamped to what can actually be heard.
        int maxDistance = parameters.MaxDistance <= 0
            ? viewRange
            : Math.Min(parameters.MaxDistance, viewRange);

        int minDistance = Math.Max(parameters.MinDistance, 0);
        int distance = ProximityMath.Distance(soundX, soundY, playerX, playerY);

        float nearness = ProximityMath.Nearness(distance, minDistance, maxDistance);
        float shaped = ProximityMath.Shape(nearness, parameters.Curve, parameters.CurveExponent);

        if (shaped <= 0f)
            return null;

        // Not ordered: a band deliberately set weak-near-strong-far is a legitimate look, so the two
        // ends are taken as authored rather than sorted into a range.
        float intensity = ProximityMath.Lerp(parameters.MinIntensity, parameters.MaxIntensity, shaped);

        return new TriggerSignal
        {
            Intensity = Math.Clamp(intensity, 0f, 1f),
            Duration = parameters.Duration
        };
    }

    #endregion

    #region Private methods

    private void OnSoundPlayed(object? sender, SoundEventArgs e)
    {
        // Cheap reject before anything else is touched: most sounds are not this rule's, and this
        // runs on every one the client plays.
        if (e.Index != _parameters.SoundIndex)
            return;

        // Skipping MT dispatch: only atomics read here. If non-atomics are added, MT dispatch
        // (with a cancellation token!) becomes necessary - the bubbling variant blocks otherwise.
        World? world = World.Instance;
        PlayerMobile? player = world?.Player;

        if (player == null)
            return;

        TriggerSignal? signal = Evaluate(
            _parameters,
            e.Index,
            e.X,
            e.Y,
            player.X,
            player.Y,
            world!.ClientViewRange
        );

        if (signal is not { } raised)
            return;

        Fired?.Invoke(this, new TriggerFiredArgs { Signal = raised });
    }

    #endregion
}
