#nullable enable

using System;
using ClassicUO.Configuration;
using ClassicUO.Game.Logic;
using ClassicUO.Game.Managers;

namespace ClassicUO.Game.ScreenDecorations.Triggers.Implementations;

/// <summary>
/// What a condition may be written about when the subject is an item's property list: the three
/// things the packet carries.
/// </summary>
internal static class ObjectPropertiesLogic
{
    #region Internal constants

    /// <summary>Persisted by every condition naming this field. Stable across releases.</summary>
    internal const string SerialKey = "serial";

    /// <inheritdoc cref="SerialKey" />
    internal const string NameKey = "name";

    /// <inheritdoc cref="SerialKey" />
    internal const string DataKey = "data";

    #endregion

    #region Private members

    /// <summary>
    /// Lazy because the display names are localized, and a static initialiser would resolve them
    /// against whatever the language file held at type load - which, for a type first touched during
    /// startup, is not necessarily the user's.
    /// </summary>
    private static readonly Lazy<LogicSchema<OPLEventArgs>> _schema = new(Build);

    #endregion

    #region Internal accessors

    /// <summary>The fields, paired with how each is read off a property-list packet.</summary>
    internal static LogicSchema<OPLEventArgs> Schema => _schema.Value;

    #endregion

    #region Private methods

    private static LogicSchema<OPLEventArgs> Build() =>
        new(
            [
                (
                    new LogicField
                    {
                        Key = SerialKey,
                        DisplayName = TazLang.Get("overlaytrigger_opl_field_serial", "Serial"),
                        Kind = LogicValueKind.Integer,
                        Description = TazLang.Get(
                            "overlaytrigger_opl_field_serial_tooltip",
                            "The item or mobile the properties belong to. Write it in decimal,\n"
                            + "or in hex with an 0x prefix."
                        )
                    },
                    static opl => opl.Serial
                ),
                (
                    new LogicField
                    {
                        Key = NameKey,
                        DisplayName = TazLang.Get("overlaytrigger_opl_field_name", "Name"),
                        Kind = LogicValueKind.Text,
                        Description = TazLang.Get(
                            "overlaytrigger_opl_field_name_tooltip",
                            "The first line of the tooltip - the item's name,\n"
                            + "with any quantity and hue markup the server sent with it."
                        )
                    },
                    static opl => opl.Name
                ),
                (
                    new LogicField
                    {
                        Key = DataKey,
                        DisplayName = TazLang.Get("overlaytrigger_opl_field_data", "Properties"),
                        Kind = LogicValueKind.Text,
                        Description = TazLang.Get(
                            "overlaytrigger_opl_field_data_tooltip",
                            "Every remaining line of the tooltip, run together.\n"
                            + "This is where durability, resistances and item properties are."
                        )
                    },
                    static opl => opl.Data
                )
            ]
        );

    #endregion
}
