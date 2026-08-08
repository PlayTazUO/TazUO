#nullable enable

namespace ClassicUO.Game.ScreenDecorations.Triggers.Implementations;

/// <summary>Reports the poison flag for as long as it is set.</summary>
internal sealed class PlayerPoisonedTrigger : IPollingTrigger
{
    /// <summary>Nothing to hook: the state is read where it lives.</summary>
    public void Attach()
    {
    }

    /// <inheritdoc />
    public void Detach()
    {
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }

    /// <inheritdoc />
    public TriggerSignal? Sample() =>
        World.Instance?.Player?.IsPoisoned == true ? TriggerSignal.Default : null;
}
