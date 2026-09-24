namespace ClassicUO.Common.Enums;

/// <summary>
/// Action performed by a health-bar quick heal/cure button. Chosen by the user and stored on the
/// profile as <see cref="ClassicUO.Configuration.Profile.QuickHealAction"/> /
/// <see cref="ClassicUO.Configuration.Profile.QuickCureAction"/>.
/// </summary>
public enum HealthBarQuickAction
{
    Bandage = 0,
    Heal = 1,
    GreaterHeal = 2,
    Cure = 3,
    ArchCure = 4,
    RemoveCurse = 5,
    CloseWounds = 6,
    CleansingWinds = 7
}
