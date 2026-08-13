#nullable enable

using System;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Logic;

namespace ClassicUO.Game.ScreenDecorations.Triggers.Implementations;

/// <summary>
/// Builds one (<see cref="LogicField" />, accessor) pair for a schema read off a <see cref="Mobile" />
/// or one of its subclasses. Shared by <see cref="MobileAttributeLogic" /> and
/// <see cref="PlayerAttributeLogic" /> so splitting the fields common to every mobile from the ones
/// only a <see cref="PlayerMobile" /> has does not mean two copies of the same boilerplate.
/// </summary>
internal static class AttributeSchemaField
{
    /// <param name="keyPrefix">Namespaces the generated language keys - the two schemas share no
    /// fields, but would otherwise share a flat "field_hits"-style key.</param>
    /// <param name="key">Persisted key. Stable across releases.</param>
    /// <param name="displayFallback">English display name, shown when no translation exists.</param>
    /// <param name="kind">What the field holds.</param>
    /// <param name="read">How the value is read off the subject.</param>
    /// <param name="tooltipFallback">English tooltip, or null for a field whose name needs no
    /// explaining.</param>
    /// <param name="enumType">The backing enum, required when <paramref name="kind" /> is
    /// <see cref="LogicValueKind.Enum" />.</param>
    /// <returns>The field and its accessor, ready for a schema.</returns>
    internal static LogicFieldEntry<TSubject> Build<TSubject>(
        string keyPrefix,
        string key,
        string displayFallback,
        LogicValueKind kind,
        Func<TSubject, object?> read,
        string? tooltipFallback = null,
        Type? enumType = null
    )
        where TSubject : Mobile =>
    (
        new LogicField
        {
            Key = key,
            DisplayName = TazLang.Get($"{keyPrefix}{key}", displayFallback),
            Kind = kind,
            Description = tooltipFallback == null ? null : TazLang.Get($"{keyPrefix}{key}_tooltip", tooltipFallback),
            EnumType = enumType
        },
        read
    );
}
