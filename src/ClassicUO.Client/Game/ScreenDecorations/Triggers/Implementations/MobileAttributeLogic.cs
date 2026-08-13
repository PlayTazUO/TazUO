#nullable enable

using System;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Logic;

namespace ClassicUO.Game.ScreenDecorations.Triggers.Implementations;

/// <summary>
/// What a condition may be written about when the subject is any <see cref="Mobile" /> - a player,
/// an NPC, another player's character. Kept separate from <see cref="PlayerAttributeLogic" /> because
/// most of what makes a mobile worth reading (vitals, notoriety, war mode, mount) has nothing to do
/// with it being the client's own character - only <see cref="PlayerMobile" /> exposes stat totals,
/// resistances and the like, since the server never sends another mobile's.
/// </summary>
internal static class MobileAttributeLogic
{
    #region Private members

    private const string KEY_PREFIX = "overlaytrigger_mobileattr_field_";

    private static readonly Lazy<LogicFieldEntry<Mobile>[]> _fields = new(BuildFields);
    private static readonly Lazy<LogicSchema<Mobile>> _schema = new(() => new LogicSchema<Mobile>(Fields));

    #endregion

    #region Internal accessors

    /// <summary>The fields and accessors, exposed raw so <see cref="PlayerAttributeLogic" /> can fold
    /// them into its own schema rather than repeat them.</summary>
    internal static LogicFieldEntry<Mobile>[] Fields => _fields.Value;

    /// <summary>The schema, for a trigger whose subject is any mobile.</summary>
    internal static LogicSchema<Mobile> Schema => _schema.Value;

    #endregion

    #region Private methods

    private static LogicFieldEntry<Mobile>[] BuildFields() =>
        [
            // Identity
            Field("name", "Name", LogicValueKind.Text, static m => m.Name),
            Field("title", "Title", LogicValueKind.Text, static m => m.Title),
            Field("race", "Race", LogicValueKind.Enum, static m => m.Race, null, typeof(RaceType)),
            Field("isfemale", "Female", LogicValueKind.Boolean, static m => m.IsFemale),
            Field("ishuman", "Human", LogicValueKind.Boolean, static m => m.IsHuman),
            Field("isgargoyle", "Gargoyle", LogicValueKind.Boolean, static m => m.IsGargoyle),
            Field(
                "notorietyflag",
                "Notoriety",
                LogicValueKind.Enum,
                static m => m.NotorietyFlag,
                "The color-coded standing shown over the mobile's head.",
                typeof(NotorietyFlag)
            ),

            // Vitals
            Field("hits", "Hit Points", LogicValueKind.Integer, static m => m.Hits),
            Field("hitsmax", "Max Hit Points", LogicValueKind.Integer, static m => m.HitsMax),
            Field("hitsdiff", "Missing Hit Points", LogicValueKind.Integer, static m => m.HitsDiff, "Max Hit Points minus Hit Points."),
            Field("mana", "Mana", LogicValueKind.Integer, static m => m.Mana),
            Field("manamax", "Max Mana", LogicValueKind.Integer, static m => m.ManaMax),
            Field("manadiff", "Missing Mana", LogicValueKind.Integer, static m => m.ManaDiff, "Max Mana minus Mana."),
            Field("stamina", "Stamina", LogicValueKind.Integer, static m => m.Stamina),
            Field("staminamax", "Max Stamina", LogicValueKind.Integer, static m => m.StaminaMax),
            Field("stamdiff", "Missing Stamina", LogicValueKind.Integer, static m => m.StamDiff, "Max Stamina minus Stamina."),

            // State flags
            Field("ispoisoned", "Poisoned", LogicValueKind.Boolean, static m => m.IsPoisoned),
            Field("isparalyzed", "Paralyzed", LogicValueKind.Boolean, static m => m.IsParalyzed),
            Field("isdead", "Dead", LogicValueKind.Boolean, static m => m.IsDead),
            Field("ishidden", "Hidden", LogicValueKind.Boolean, static m => m.IsHidden),
            Field("ismounted", "Mounted", LogicValueKind.Boolean, static m => m.IsMounted),
            Field("isflying", "Flying", LogicValueKind.Boolean, static m => m.IsFlying),
            Field("isdrivingboat", "Driving Boat", LogicValueKind.Boolean, static m => m.IsDrivingBoat),
            Field("iswalking", "Walking", LogicValueKind.Boolean, static m => m.IsWalking),
            Field("isrunning", "Running", LogicValueKind.Boolean, static m => m.IsRunning),
            Field("inwarmode", "In War Mode", LogicValueKind.Boolean, static m => m.InWarMode),
            Field("inparty", "In Party", LogicValueKind.Boolean, static m => m.InParty),
            Field(
                "isyellowhits",
                "Yellow Health Bar",
                LogicValueKind.Boolean,
                static m => m.IsYellowHits,
                "The health bar renders yellow rather than the usual gray/blue."
            ),
            Field("isattackable", "Attackable", LogicValueKind.Boolean, static m => m.IsAttackable),
            Field(
                "speedmode",
                "Speed Mode",
                LogicValueKind.Enum,
                static m => m.SpeedMode,
                "What is currently limiting the mobile's movement, if anything.",
                typeof(CharacterSpeedType)
            )
        ];

    private static LogicFieldEntry<Mobile> Field(
        string key,
        string displayFallback,
        LogicValueKind kind,
        Func<Mobile, object?> read,
        string? tooltipFallback = null,
        Type? enumType = null
    ) =>
        AttributeSchemaField.Build(KEY_PREFIX, key, displayFallback, kind, read, tooltipFallback, enumType);

    #endregion
}
