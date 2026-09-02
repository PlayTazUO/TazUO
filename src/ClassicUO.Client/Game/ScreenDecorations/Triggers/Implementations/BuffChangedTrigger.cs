#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Game.Managers;

namespace ClassicUO.Game.ScreenDecorations.Triggers.Implementations;

/// <summary>
/// Watches the configured buff types, answering to whichever moment of their life the rule was wired for.
/// <para>
/// <see cref="BuffTriggerMode.Added" /> and <see cref="BuffTriggerMode.Removed" /> are momentary, so
/// the parameters' duration decides how long the effect runs and any watched buff fires it.
/// <see cref="BuffTriggerMode.Active" /> brackets it with the buff's own add and remove instead,
/// reported through <see cref="Ended" />, and watches a single buff - see <see cref="_buffTypes" />.
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

    /// <summary>
    /// Membership test for the buff handlers, which run on every buff the player gains or loses.
    /// <para>
    /// Holds one type under <see cref="BuffTriggerMode.Active" />, where a second would end the effect
    /// while the first was still up. The editor offers a single picker; a hand-edited config keeps the
    /// first of several.
    /// </para>
    /// </summary>
    private readonly HashSet<short> _buffTypes;

    #endregion

    #region Ctor

    /// <param name="parameters">Which buffs to watch, and which moment of their life to answer to.</param>
    /// <exception cref="ArgumentNullException"><paramref name="parameters" /> is null.</exception>
    public BuffChangedTrigger(BuffChangedParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        _parameters = parameters;

        _buffTypes = parameters.Mode == BuffTriggerMode.Active
            ? [..parameters.BuffTypes.Take(1)]
            : [..parameters.BuffTypes];
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
        if (!_buffTypes.Contains((short)e.Buff.Type))
            return;

        switch (_parameters.Mode)
        {
            case BuffTriggerMode.Added:
                // Raised on every packet for the buff, including a shard refreshing an active one.
                // Telling those apart needs the packet handler's alreadyExists check plumbed here.
                Fired?.Invoke(this, new TriggerFiredArgs { Signal = new TriggerSignal { Duration = _parameters.Duration } });
                break;

            case BuffTriggerMode.Active:
                Fired?.Invoke(this, new TriggerFiredArgs { Signal = TriggerSignal.Default });
                break;
        }
    }

    private void OnBuffRemoved(object? sender, BuffEventArgs e)
    {
        if (!_buffTypes.Contains((short)e.Buff.Type))
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
