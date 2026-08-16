#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Logic;

namespace ClassicUO.Game.ScreenDecorations.Triggers.Implementations;

/// <summary>
/// What a condition may be written about when the subject is specifically the client's own
/// character: everything <see cref="MobileAttributeLogic" /> already covers, plus the stat totals,
/// resistances and locks the server only ever sends about the player - another mobile's are never on
/// the wire, so a schema built over <see cref="Mobile" /> alone has no way to read them.
/// </summary>
internal static class PlayerAttributeLogic
{
    #region Private members

    private const string KEY_PREFIX = "overlaytrigger_playerattr_field_";

    private static readonly Lazy<LogicSchema<PlayerMobile>> _schema = new(Build);

    #endregion

    #region Internal accessors

    /// <summary>The fields, paired with how each is read off the player - the mobile-level ones
    /// inherited from <see cref="MobileAttributeLogic" />, followed by the player-only ones.</summary>
    internal static LogicSchema<PlayerMobile> Schema => _schema.Value;

    #endregion

    #region Private methods

    /// <summary>Folds <see cref="MobileAttributeLogic" />'s fields into the player-only ones.</summary>
    /// <returns>The schema.</returns>
    private static LogicSchema<PlayerMobile> Build()
    {
        // A function that can read any Mobile can certainly read a PlayerMobile, so the inherited
        // fields need no adapting beyond the delegate variance the compiler already grants them.
        // LogicFieldEntry itself is not variant - it is a struct, not a delegate - so the wrapper is
        // rebuilt even though only its Resolve component's type actually changes.
        IEnumerable<LogicFieldEntry<PlayerMobile>> inherited = MobileAttributeLogic.Fields.Select(
            static entry => new LogicFieldEntry<PlayerMobile>(entry.Field, entry.Resolve)
        );

        return new LogicSchema<PlayerMobile>([.. inherited, .. PlayerOnlyFields()]);
    }

    private static LogicFieldEntry<PlayerMobile>[] PlayerOnlyFields() =>
        [
            Field("iscasting", "Casting", LogicValueKind.Boolean, static p => p.IsCasting),
            Field(
                "primaryability",
                "Primary Ability",
                LogicValueKind.Enum,
                static p => p.PrimaryAbility,
                "The weapon special move bound to the primary ability slot.",
                typeof(Ability)
            ),
            Field(
                "secondaryability",
                "Secondary Ability",
                LogicValueKind.Enum,
                static p => p.SecondaryAbility,
                "The weapon special move bound to the secondary ability slot.",
                typeof(Ability)
            ),

            // Core stats and their locks
            Field("strength", "Strength", LogicValueKind.Integer, static p => p.Strength),
            Field("strengthincrease", "Strength Increase", LogicValueKind.Integer, static p => p.StrengthIncrease),
            Field(
                "strlock",
                "Strength Lock",
                LogicValueKind.Enum,
                static p => p.StrLock,
                "Whether Strength is free to rise, free to fall, or locked at its current value.",
                typeof(Lock)
            ),
            Field("dexterity", "Dexterity", LogicValueKind.Integer, static p => p.Dexterity),
            Field("dexterityincrease", "Dexterity Increase", LogicValueKind.Integer, static p => p.DexterityIncrease),
            Field(
                "dexlock",
                "Dexterity Lock",
                LogicValueKind.Enum,
                static p => p.DexLock,
                "Whether Dexterity is free to rise, free to fall, or locked at its current value.",
                typeof(Lock)
            ),
            Field("intelligence", "Intelligence", LogicValueKind.Integer, static p => p.Intelligence),
            Field("intelligenceincrease", "Intelligence Increase", LogicValueKind.Integer, static p => p.IntelligenceIncrease),
            Field(
                "intlock",
                "Intelligence Lock",
                LogicValueKind.Enum,
                static p => p.IntLock,
                "Whether Intelligence is free to rise, free to fall, or locked at its current value.",
                typeof(Lock)
            ),
            Field(
                "statscap",
                "Stats Cap",
                LogicValueKind.Integer,
                static p => p.StatsCap,
                "The server's total stat cap - what Strength, Dexterity and Intelligence together may not exceed."
            ),

            // Carry weight, followers, currency
            Field("weight", "Weight", LogicValueKind.Integer, static p => p.Weight),
            Field("weightmax", "Max Weight", LogicValueKind.Integer, static p => p.WeightMax),
            Field("followers", "Followers", LogicValueKind.Integer, static p => p.Followers),
            Field("followersmax", "Max Followers", LogicValueKind.Integer, static p => p.FollowersMax),
            Field("gold", "Gold", LogicValueKind.Integer, static p => p.Gold),
            Field("luck", "Luck", LogicValueKind.Integer, static p => p.Luck),
            Field(
                "tithingpoints",
                "Tithing Points",
                LogicValueKind.Integer,
                static p => p.TithingPoints,
                "Points banked with a shrine for casting Chivalry spells."
            ),
            Field(
                "deathscreentimer",
                "Death Screen Timer",
                LogicValueKind.Integer,
                static p => p.DeathScreenTimer,
                "Milliseconds left on the death screen's timer, or 0 outside of it."
            ),

            // Combat bonuses (totals from equipment, buffs and skills)
            Field("damagemin", "Damage Min", LogicValueKind.Integer, static p => p.DamageMin),
            Field("damagemax", "Damage Max", LogicValueKind.Integer, static p => p.DamageMax),
            Field("damageincrease", "Damage Increase", LogicValueKind.Integer, static p => p.DamageIncrease),
            Field("spelldamageincrease", "Spell Damage Increase", LogicValueKind.Integer, static p => p.SpellDamageIncrease),
            Field("hitchanceincrease", "Hit Chance Increase", LogicValueKind.Integer, static p => p.HitChanceIncrease),
            Field("defensechanceincrease", "Defense Chance Increase", LogicValueKind.Integer, static p => p.DefenseChanceIncrease),
            Field("swingspeedincrease", "Swing Speed Increase", LogicValueKind.Integer, static p => p.SwingSpeedIncrease),
            Field("reflectphysicaldamage", "Reflect Physical Damage", LogicValueKind.Integer, static p => p.ReflectPhysicalDamage),
            Field("hitpointsincrease", "Hit Points Increase", LogicValueKind.Integer, static p => p.HitPointsIncrease),
            Field("hitpointsregeneration", "Hit Points Regeneration", LogicValueKind.Integer, static p => p.HitPointsRegeneration),
            Field("manaincrease", "Mana Increase", LogicValueKind.Integer, static p => p.ManaIncrease),
            Field("manaregeneration", "Mana Regeneration", LogicValueKind.Integer, static p => p.ManaRegeneration),
            Field("staminaincrease", "Stamina Increase", LogicValueKind.Integer, static p => p.StaminaIncrease),
            Field("staminaregeneration", "Stamina Regeneration", LogicValueKind.Integer, static p => p.StaminaRegeneration),
            Field("fastercasting", "Faster Casting", LogicValueKind.Integer, static p => p.FasterCasting),
            Field("fastercastrecovery", "Faster Cast Recovery", LogicValueKind.Integer, static p => p.FasterCastRecovery),
            Field("lowermanacost", "Lower Mana Cost", LogicValueKind.Integer, static p => p.LowerManaCost),
            Field("lowerreagentcost", "Lower Reagent Cost", LogicValueKind.Integer, static p => p.LowerReagentCost),
            Field("enhancepotions", "Enhance Potions", LogicValueKind.Integer, static p => p.EnhancePotions),

            // Resistances and their caps
            Field("physicalresistance", "Physical Resistance", LogicValueKind.Integer, static p => p.PhysicalResistance),
            Field("maxphysicresistance", "Max Physical Resistance", LogicValueKind.Integer, static p => p.MaxPhysicResistence),
            Field("fireresistance", "Fire Resistance", LogicValueKind.Integer, static p => p.FireResistance),
            Field("maxfireresistance", "Max Fire Resistance", LogicValueKind.Integer, static p => p.MaxFireResistence),
            Field("coldresistance", "Cold Resistance", LogicValueKind.Integer, static p => p.ColdResistance),
            Field("maxcoldresistance", "Max Cold Resistance", LogicValueKind.Integer, static p => p.MaxColdResistence),
            Field("poisonresistance", "Poison Resistance", LogicValueKind.Integer, static p => p.PoisonResistance),
            Field("maxpoisonresistance", "Max Poison Resistance", LogicValueKind.Integer, static p => p.MaxPoisonResistence),
            Field("energyresistance", "Energy Resistance", LogicValueKind.Integer, static p => p.EnergyResistance),
            Field("maxenergyresistance", "Max Energy Resistance", LogicValueKind.Integer, static p => p.MaxEnergyResistence),
            Field("maxhitpointsincrease", "Max Hit Points Increase", LogicValueKind.Integer, static p => p.MaxHitPointsIncrease),
            Field("maxmanaincrease", "Max Mana Increase", LogicValueKind.Integer, static p => p.MaxManaIncrease),
            Field("maxstaminaincrease", "Max Stamina Increase", LogicValueKind.Integer, static p => p.MaxStaminaIncrease),
            Field(
                "maxdefensechanceincrease",
                "Max Defense Chance Increase",
                LogicValueKind.Integer,
                static p => p.MaxDefenseChanceIncrease
            )
        ];

    private static LogicFieldEntry<PlayerMobile> Field(
        string key,
        string displayFallback,
        LogicValueKind kind,
        Func<PlayerMobile, object?> read,
        string? tooltipFallback = null,
        Type? enumType = null
    ) =>
        AttributeSchemaField.Build(KEY_PREFIX, key, displayFallback, kind, read, tooltipFallback, enumType);

    #endregion
}
