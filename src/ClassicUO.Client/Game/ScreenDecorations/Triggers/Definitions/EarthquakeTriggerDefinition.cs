#nullable enable

using System;
using ClassicUO.Configuration;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;

namespace ClassicUO.Game.ScreenDecorations.Triggers.Definitions;

/// <summary>
/// The ground moving, taken from the earthquake sound the server plays.
/// </summary>
public sealed class EarthquakeTriggerDefinition : ITriggerDefinition
{
    /// <inheritdoc />
    public string Id => "earthquake";

    /// <inheritdoc />
    public string DisplayName => TazLang.Get("overlaytrigger_earthquake", "Earthquake sound");

    /// <inheritdoc />
    public TriggerKind Kind => TriggerKind.Event;

    /// <inheritdoc />
    public Type? ParameterType => null;

    /// <summary>Nothing announces the end of a quake; the signal's own duration retires it.</summary>
    public bool IsStateful => false;

    /// <inheritdoc />
    public ITriggerInstance Create(TriggerParameters? parameters) => new EarthquakeTrigger();

    /// <inheritdoc />
    public TriggerParameters? CreateDefaultParameters() => null;
}

/// <summary>
/// Listens for the client's earthquake sound. Headless: the packet says a quake happened and nothing
/// says when it stopped, so one occurrence runs for a fixed span.
/// <para>
/// Scaled by how near the sound is, because the same quake felt across the map and underfoot should
/// not read the same. The sound carries the tile it came from, which is the only distance
/// information available - there is no quake packet to ask.
/// </para>
/// </summary>
public sealed class EarthquakeTrigger : IEventTrigger
{
    #region Public events

    /// <inheritdoc />
    public event EventHandler<TriggerFiredArgs>? Fired;

    /// <summary>Never raised - nothing announces the end of a quake, and the signal's own duration
    /// is what retires it. Accessors are empty rather than the event being omitted, because the
    /// manager subscribes to every event trigger without asking which shape it is.</summary>
    public event EventHandler? Ended
    {
        add { }
        remove { }
    }

    #endregion

    #region Private members

    private const int EARTHQUAKE_SOUND_INDEX = 755;

    /// <summary>
    /// Strength a quake at the edge of earshot still gets. Not zero: the client only plays the
    /// sound at all within view range, so anything that reaches the player is worth showing - it
    /// just should not compete with one underfoot.
    /// </summary>
    private const float MIN_INTENSITY = 0.25f;

    /// <summary>
    /// Roughly how long the client's own earthquake sound runs. Restarted rather than stacked if a
    /// second quake lands inside it, so a sustained sequence holds the effect up throughout.
    /// </summary>
    private const float OCCURRENCE_SECONDS = 3f;

    #endregion

    #region Public methods

    /// <inheritdoc />
    public void Attach() => EventSink.SoundPlayed += OnSoundPlayed;

    /// <inheritdoc />
    public void Detach() => EventSink.SoundPlayed -= OnSoundPlayed;

    /// <inheritdoc />
    public void Dispose() => Detach();

    #endregion

    #region Private methods

    private void OnSoundPlayed(object? sender, SoundEventArgs e)
    {
        if (e.Index != EARTHQUAKE_SOUND_INDEX)
            return;

        // Runs inline today - the packet handler that raises this is already on the main thread - so
        // the marshalling costs a branch. Kept for when that stops being true: a mobile's position
        // and the view range are main-thread state, and reading them from under the movement code
        // gives a distance that was never real.
        (bool inWorld, int playerX, int playerY, int viewRange) = MainThreadQueue.BubblingInvokeOnMainThread(
            () =>
            {
                World? world = World.Instance;
                PlayerMobile? player = world?.Player;

                return player == null
                    ? (false, 0, 0, 0)
                    : (true, player.X, player.Y, world!.ClientViewRange);
            }
        );

        if (!inWorld)
            return;

        float nearness = Nearness(e.X, e.Y, playerX, playerY, viewRange);

        if (nearness <= 0f)
            return;

        var signal = new TriggerSignal
        {
            Intensity = Lerp(MIN_INTENSITY, 1f, nearness),
            Duration = TimeSpan.FromSeconds(OCCURRENCE_SECONDS)
        };

        Fired?.Invoke(this, new TriggerFiredArgs { Signal = signal });
    }

    /// <summary>
    /// How close the quake is: 1 underfoot, falling to 0 past the range the client would still play
    /// the sound at. Squared, so most of the scale is spent on the few tiles around the player -
    /// spreading it evenly makes everything within sight feel much the same.
    /// </summary>
    /// <param name="soundX">Tile the sound came from.</param>
    /// <param name="soundY">Tile the sound came from.</param>
    /// <param name="playerX">The player's tile.</param>
    /// <param name="playerY">The player's tile.</param>
    /// <param name="viewRange">Tiles the client can see, which is also its audible cutoff.</param>
    /// <returns>Nearness in 0-1; zero for a quake too far off to register.</returns>
    internal static float Nearness(int soundX, int soundY, int playerX, int playerY, int viewRange)
    {
        int distance = Math.Max(Math.Abs(soundX - playerX), Math.Abs(soundY - playerY));

        if (viewRange <= 0 || distance > viewRange)
            return 0f;

        // Matches the audio manager's own falloff denominator, so the visual fades out exactly as
        // the sound that justifies it does.
        float nearness = 1f - (float)distance / (viewRange + 1);

        return nearness * nearness;
    }

    private static float Lerp(float from, float to, float amount) => from + (to - from) * amount;

    #endregion
}
