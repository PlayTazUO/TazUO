#nullable enable

using ClassicUO.Game.GameObjects;

namespace ClassicUO.Game.ScreenDecorations.Triggers.Implementations;

/// <summary>
/// A <see cref="MobileAttributeTrigger{TSubject}" /> whose subject is always the client's own
/// character.
/// </summary>
internal sealed class PlayerAttributeTrigger : MobileAttributeTrigger<PlayerMobile>
{
    #region Ctor

    /// <param name="parameters">The rule's values for this trigger.</param>
    public PlayerAttributeTrigger(PlayerAttributeParameters parameters)
        : base(parameters.Filter, PlayerAttributeLogic.Schema)
    {
    }

    #endregion

    #region Protected methods

    /// <inheritdoc />
    protected override PlayerMobile? SelectSubject() => World.Instance?.Player;

    #endregion
}
