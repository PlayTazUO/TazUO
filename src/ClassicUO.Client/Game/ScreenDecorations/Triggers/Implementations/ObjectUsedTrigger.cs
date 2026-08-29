#nullable enable

using System;
using System.Collections.Generic;
using ClassicUO.Game.Managers;

namespace ClassicUO.Game.ScreenDecorations.Triggers.Implementations;

/// <summary>
/// Listens for a watched object being double-clicked and raises an occurrence for its configured
/// span.
/// </summary>
public sealed class ObjectUsedTrigger : IEventTrigger
{
    #region Public events

    /// <inheritdoc />
    public event EventHandler<TriggerFiredArgs>? Fired;

    /// <summary>Never raised - a use has no natural end, only the signal's own duration retires it.
    /// Accessors are empty rather than the event being omitted, because the manager subscribes to
    /// every event trigger without asking which shape it is.</summary>
    public event EventHandler? Ended
    {
        add { }
        remove { }
    }

    #endregion

    #region Private members

    private readonly ObjectUsedParameters _parameters;

    /// <summary>Membership test for <see cref="OnObjectUsed" />, which runs on every double-click the
    /// client sends and cannot afford a list scan per one.</summary>
    private readonly HashSet<uint> _serials;

    #endregion

    #region Ctor

    /// <param name="parameters">Which objects to watch, and how long an occurrence runs.</param>
    /// <exception cref="ArgumentNullException"><paramref name="parameters" /> is null.</exception>
    public ObjectUsedTrigger(ObjectUsedParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        _parameters = parameters;
        _serials = [..parameters.Serials];
    }

    #endregion

    #region Public methods

    /// <inheritdoc />
    public void Attach() => EventSink.OnObjectUsed += OnObjectUsed;

    /// <inheritdoc />
    public void Detach() => EventSink.OnObjectUsed -= OnObjectUsed;

    /// <inheritdoc />
    public void Dispose() => Detach();

    #endregion

    #region Private methods

    private void OnObjectUsed(object? sender, uint serial)
    {
        if (!_serials.Contains(serial))
            return;

        Fired?.Invoke(this, new TriggerFiredArgs { Signal = new TriggerSignal { Duration = _parameters.Duration } });
    }

    #endregion
}
