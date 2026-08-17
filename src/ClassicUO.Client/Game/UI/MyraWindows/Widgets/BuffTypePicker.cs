#nullable enable

using System;
using System.Collections.Generic;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

/// <summary>
///     Picks a buff by its <see cref="BuffIconType" />: a searchable list of every type this client
///     knows, and a raw number field for one a shard sends that the enum has no name for.
///     <para>
///         An <see cref="IndexedComboPicker" /> seeded with every known <see cref="BuffIconType" /> - the
///         enum only names what this client shipped with, but the id is what the server actually sends
///         and is what gets stored either way.
///     </para>
/// </summary>
public sealed class BuffTypePicker : IndexedComboPicker
{
    #region Public events

    /// <summary>Raised when the chosen type changes, from either input.</summary>
    public event EventHandler<short>? TypeChanged;

    #endregion

    #region Public accessors

    /// <summary>The chosen buff type id. Setting it moves both inputs.</summary>
    public short BuffType
    {
        get => (short)Value;
        set => Value = value;
    }

    #endregion

    #region Private members

    /// <summary>
    ///     Every known type, labeled. Built once: the enum is fixed at compile time, unlike the
    ///     sound data a <see cref="SoundIndexPicker" /> reads.
    /// </summary>
    private static readonly List<(int Value, string Label)> _entries = BuildEntries();

    #endregion

    #region Ctor

    /// <param name="buffType">The type to start on.</param>
    /// <param name="numberWidth">Width for the raw-number field.</param>
    /// <param name="nameWidth">Width for the name list.</param>
    public BuffTypePicker(short buffType, int numberWidth, int nameWidth)
        : base(buffType, _entries, short.MinValue, short.MaxValue)
    {
        NumberInput.Width = numberWidth;
        NumberInput.Tooltip = TazLang.Get(
            "overlaytrigger_buff_number_tooltip",
            "The buff's numeric ID.\n"
            + "Type one here for a buff the list has no name for."
        );
        NameList.Width = nameWidth;

        ValueChanged += (_, value) => TypeChanged?.Invoke(this, (short)value);
    }

    #endregion

    #region Private methods

    private static List<(int, string)> BuildEntries()
    {
        var entries = new List<(int, string)>();

        foreach (BuffIconType type in Enum.GetValues<BuffIconType>())
        {
            short id = (short)type;
            entries.Add((id, $"{id} - {type}"));
        }

        return entries;
    }

    #endregion
}
