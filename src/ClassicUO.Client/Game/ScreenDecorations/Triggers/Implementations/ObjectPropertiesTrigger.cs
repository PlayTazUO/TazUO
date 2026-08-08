#nullable enable

using System;
using ClassicUO.Game.Logic;
using ClassicUO.Game.Managers;

namespace ClassicUO.Game.ScreenDecorations.Triggers.Implementations;

/// <summary>
/// Watches incoming property lists for one rule's expression.
/// </summary>
internal sealed class ObjectPropertiesTrigger : IEventTrigger
{
    #region Public events

    /// <inheritdoc />
    public event EventHandler<TriggerFiredArgs>? Fired;

    /// <summary>Never raised - a property list has no end to report, and the parameters' duration is
    /// what retires it. Accessors are empty rather than the event being omitted, because the manager
    /// subscribes to every event trigger without asking which shape it is.</summary>
    public event EventHandler? Ended
    {
        add { }
        remove { }
    }

    #endregion

    #region Private members

    private readonly ObjectPropertiesParameters _parameters;

    /// <summary>
    /// Built once per rule rather than per packet. Property lists arrive in bursts - one per item as
    /// a container opens - and the evaluator caches whatever regexes the tree compiles for as long
    /// as it lives.
    /// </summary>
    private readonly LogicEvaluator<OPLEventArgs> _evaluator;

    #endregion

    #region Ctor

    public ObjectPropertiesTrigger(ObjectPropertiesParameters parameters)
    {
        _parameters = parameters;
        _evaluator = new LogicEvaluator<OPLEventArgs>(ObjectPropertiesLogic.Schema);
    }

    #endregion

    #region Public methods

    /// <inheritdoc />
    public void Attach() => EventSink.OPLOnReceive += OnPropertiesReceived;

    /// <inheritdoc />
    public void Detach() => EventSink.OPLOnReceive -= OnPropertiesReceived;

    /// <inheritdoc />
    public void Dispose() => Detach();

    #endregion

    #region Private methods

    private void OnPropertiesReceived(object? sender, OPLEventArgs? e)
    {
        if (e == null || !_evaluator.Evaluate(_parameters.Filter, e))
            return;

        var signal = new TriggerSignal { Duration = _parameters.Duration };

        Fired?.Invoke(this, new TriggerFiredArgs { Signal = signal });
    }

    #endregion
}
