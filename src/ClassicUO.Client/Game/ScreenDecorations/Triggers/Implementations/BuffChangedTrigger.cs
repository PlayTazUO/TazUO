#nullable enable

using System;
using ClassicUO.Game.Managers;

namespace ClassicUO.Game.ScreenDecorations.Triggers.Implementations;

/// <summary>
/// Watches one buff type, answering to whichever moment of its life the rule was wired for.
/// <para>
/// <see cref="BuffTriggerMode.Added" /> and <see cref="BuffTriggerMode.Removed" /> are momentary, like
/// <see cref="SoundPlayedTrigger" /> or <see cref="ChatMessageTrigger" />: the event is an instant, so
/// the parameters' duration decides how long the effect runs. <see cref="BuffTriggerMode.Active" />
/// brackets the occurrence with the buff's own add and remove instead, the same real lifetime a
/// stateful trigger reports through <see cref="Ended" />.
/// </para>
/// </summary>
public sealed class BuffChangedTrigger : IEventTrigger
{
    #region Public events

    /// <inheritdoc />
    public event EventHandler<TriggerFiredArgs>? Fired;

    /// <inheritdoc />
    public event EventHandler? Ended;

    #endregion

    #region Private members

    private readonly BuffChangedParameters _parameters;

    #endregion

    #region Ctor

    /// <param name="parameters">Which buff to watch, and which moment of its life to answer to.</param>
    /// <exception cref="ArgumentNullException"><paramref name="parameters" /> is null.</exception>
    public BuffChangedTrigger(BuffChangedParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        _parameters = parameters;
    }

    #endregion

    #region Public methods

    /// <inheritdoc />
    public void Attach()
    {
        EventSink.OnBuffAddedInternal += OnBuffAdded;
        EventSink.OnBuffRemovedInternal += OnBuffRemoved;
    }

    /// <inheritdoc />
    public void Detach()
    {
        EventSink.OnBuffAddedInternal -= OnBuffAdded;
        EventSink.OnBuffRemovedInternal -= OnBuffRemoved;
    }

    /// <inheritdoc />
    public void Dispose() => Detach();

    #endregion

    #region Private methods

    private void OnBuffAdded(object? sender, BuffEventArgs e)
    {
        if ((short)e.Buff.Type != _parameters.BuffType)
            return;

        switch (_parameters.Mode)
        {
            case BuffTriggerMode.Added:
                // PlayerMobile.AddBuff() raises this on every packet for the buff, including a shard
                // resending one already active (e.g. refreshing its timer), not just the true first
                // application - accepted for now, since telling them apart needs plumbing the packet
                // handler's own alreadyExists check through to here.
                Fired?.Invoke(this, new TriggerFiredArgs { Signal = new TriggerSignal { Duration = _parameters.Duration } });
                break;

            case BuffTriggerMode.Active:
                Fired?.Invoke(this, new TriggerFiredArgs { Signal = TriggerSignal.Default });
                break;
        }
    }

    private void OnBuffRemoved(object? sender, BuffEventArgs e)
    {
        if ((short)e.Buff.Type != _parameters.BuffType)
            return;

        switch (_parameters.Mode)
        {
            case BuffTriggerMode.Removed:
                Fired?.Invoke(this, new TriggerFiredArgs { Signal = new TriggerSignal { Duration = _parameters.Duration } });
                break;

            case BuffTriggerMode.Active:
                Ended?.Invoke(this, EventArgs.Empty);
                break;
        }
    }

    #endregion
}
