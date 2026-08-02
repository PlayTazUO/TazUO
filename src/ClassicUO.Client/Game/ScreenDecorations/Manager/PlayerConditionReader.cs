#nullable enable

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;

namespace ClassicUO.Game.ScreenDecorations.Manager;

/// <summary>
/// Reads a mobile's <see cref="PlayerCondition"/>s out of the game state.
/// <para>
/// Reads live game objects, so it must be called from the main thread.
/// </para>
/// </summary>
internal static class PlayerConditionReader
{
    /// <summary>
    /// Everything currently true of the player, buff-derived conditions included.
    /// </summary>
    /// <returns>The player's conditions, or <see cref="PlayerCondition.None"/> while out of game.</returns>
    public static PlayerCondition ReadPlayer()
    {
        PlayerMobile? player = World.Instance?.Player;

        if (player == null || player.IsDestroyed)
            return PlayerCondition.None;

        PlayerCondition conditions = Read(player);

        if (HasActiveBuff(player, BuffIconType.Bleed))
            conditions |= PlayerCondition.Bleeding;

        if (HasActiveBuff(player, BuffIconType.MortalStrike))
            conditions |= PlayerCondition.MortalStruck;

        return conditions;
    }

    /// <summary>
    /// The conditions any mobile's flags carry. <see cref="PlayerCondition.Bleeding"/> and
    /// <see cref="PlayerCondition.MortalStruck"/> are never reported here, including for the player:
    /// they come from buff icons, which the server sends for the player alone.
    /// </summary>
    /// <param name="mobile">The mobile to inspect; may be null.</param>
    /// <returns>Its condition mask, or <see cref="PlayerCondition.None"/> if it is gone.</returns>
    public static PlayerCondition Read(Mobile? mobile)
    {
        if (mobile == null || mobile.IsDestroyed)
            return PlayerCondition.None;

        PlayerCondition conditions = PlayerCondition.None;

        if (mobile.IsPoisoned)
            conditions |= PlayerCondition.Poisoned;

        if (mobile.IsParalyzed)
            conditions |= PlayerCondition.Paralyzed;

        if (mobile.IsHidden)
            conditions |= PlayerCondition.Hidden;

        if (mobile.IsDead)
            conditions |= PlayerCondition.Dead;

        return conditions;
    }

    /// <summary>
    /// Buffs are only ever removed by a server packet, so one whose own timer has run out is treated
    /// as gone here rather than left to linger until the server says otherwise.
    /// </summary>
    private static bool HasActiveBuff(PlayerMobile player, BuffIconType type)
    {
        if (!player.BuffIcons.TryGetValue(type, out BuffIcon buff))
            return false;

        return buff.Timer > Time.Ticks;
    }
}
