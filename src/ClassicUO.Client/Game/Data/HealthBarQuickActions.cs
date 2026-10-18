using ClassicUO.Common.Enums;
using ClassicUO.Configuration;

namespace ClassicUO.Game.Data
{
    /// <summary>
    /// Maps <see cref="HealthBarQuickAction"/> values to their spell IDs and localized names. Spell IDs
    /// are full indexes as understood by <see cref="SpellDefinition.FullIndexGetSpell"/>.
    /// </summary>
    public static class HealthBarQuickActions
    {
        public const int MageryHeal = 4;
        public const int MageryCure = 11;
        public const int MageryArchCure = 25;
        public const int MageryGreaterHeal = 29;
        public const int ChivalryCloseWounds = 202;
        public const int ChivalryRemoveCurse = 209;
        public const int MysticismCleansingWinds = 688;

        /// <summary>
        /// The spell cast by <paramref name="action"/>, or -1 for actions that are not spells (bandages).
        /// </summary>
        public static int GetSpellId(this HealthBarQuickAction action) => action switch
        {
            HealthBarQuickAction.Heal => MageryHeal,
            HealthBarQuickAction.GreaterHeal => MageryGreaterHeal,
            HealthBarQuickAction.Cure => MageryCure,
            HealthBarQuickAction.ArchCure => MageryArchCure,
            HealthBarQuickAction.RemoveCurse => ChivalryRemoveCurse,
            HealthBarQuickAction.CloseWounds => ChivalryCloseWounds,
            HealthBarQuickAction.CleansingWinds => MysticismCleansingWinds,
            _ => -1
        };

        /// <summary>
        /// The localized display name for <paramref name="action"/>, falling back to the enum name when
        /// no translation exists.
        /// </summary>
        public static string GetDisplayName(this HealthBarQuickAction action) =>
            TazLang.Get($"healthbar_quickaction_{action.ToString().ToLowerInvariant()}", action.ToString());
    }
}
