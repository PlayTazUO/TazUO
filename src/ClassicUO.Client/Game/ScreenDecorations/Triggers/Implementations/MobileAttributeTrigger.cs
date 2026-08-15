#nullable enable

using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Logic;

namespace ClassicUO.Game.ScreenDecorations.Triggers.Implementations;

/// <summary>
/// Polls one mobile's state against a rule's expression. There is no event for "a mobile's state
/// changed", so this is read where it lives, every reconcile pass.
/// <para>
/// What the tree runs against is left to <see cref="SelectSubject" /> rather than fixed here: a
/// concrete trigger is nothing more than a choice of subject over a shared schema and evaluator.
/// <see cref="PlayerAttributeTrigger" /> answers it with the client's own character; a trigger for an
/// NPC or another player's would answer it with whatever picks that mobile out - a target, a
/// serial, another trigger's match - none of which this base needs to know about.
/// </para>
/// </summary>
/// <typeparam name="TSubject">The kind of mobile this trigger reads. Determines which schema its
/// expression is written against.</typeparam>
internal abstract class MobileAttributeTrigger<TSubject> : IPollingTrigger
    where TSubject : Mobile
{
    #region Private members

    private readonly LogicGroup _filter;
    private readonly LogicEvaluator<TSubject> _evaluator;

    #endregion

    #region Ctor

    /// <param name="filter">The rule's expression. An empty tree matches every poll.</param>
    /// <param name="schema">What the expression may be written about.</param>
    protected MobileAttributeTrigger(LogicGroup filter, LogicSchema<TSubject> schema)
    {
        _filter = filter;
        _evaluator = new LogicEvaluator<TSubject>(schema);
    }

    #endregion

    #region Public methods

    /// <summary>Nothing to hook: the state is read where it lives.</summary>
    public virtual void Attach()
    {
    }

    /// <inheritdoc />
    public virtual void Detach()
    {
    }

    /// <inheritdoc />
    public virtual void Dispose()
    {
    }

    /// <inheritdoc />
    public TriggerSignal? Sample() =>
        SelectSubject() is { } subject && _evaluator.Evaluate(_filter, subject) ? TriggerSignal.Default : null;

    #endregion

    #region Protected methods

    /// <summary>
    /// The mobile this poll evaluates against, or null while there is none to test - the player
    /// hasn't entered the world yet, nothing is targeted, whatever the concrete trigger's notion of
    /// "the subject" fails to resolve to right now.
    /// </summary>
    protected abstract TSubject? SelectSubject();

    #endregion
}
