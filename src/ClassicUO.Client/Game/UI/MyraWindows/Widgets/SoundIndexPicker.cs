#nullable enable

using System;
using System.Collections.Generic;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.UI.MyraWindows.Widgets.Search;
using ClassicUO.Utility.Logging;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

/// <summary>
/// Marks an <see cref="int" /> property as a sound index, so the rule editor offers
/// <see cref="SoundIndexPicker" /> for it.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class SoundIndexEditorAttribute : Attribute;

/// <summary>
/// Picks a sound by index: the raw number, a searchable list of the names the loaded sound data
/// carries, and a button to hear whichever is chosen.
/// <para>
/// Both inputs are needed. Indices mean nothing to read, so a bare number field cannot be used
/// without the sound browser open beside it; but the names come from the shard's own data files, so a
/// client with none, or a shard that added sounds this one has not indexed, still has to be able to
/// take a number outright. The number is what is stored either way - the list only writes into it.
/// </para>
/// </summary>
public sealed class SoundIndexPicker : HorizontalStackPanel
{
    #region Public events

    /// <summary>Raised when the chosen index changes, from either input.</summary>
    public event EventHandler<int>? IndexChanged;

    #endregion

    #region Public accessors

    /// <summary>The chosen sound index. Setting it moves both inputs.</summary>
    public int Index
    {
        get => _input.Value;
        set => _input.Value = value;
    }

    #endregion

    #region Private members

    private const int SPACING = 6;

    /// <summary>
    /// The named sounds, read once per session. Building it walks every sound header in the file, so
    /// it is shared across pickers rather than rebuilt per rule editor opened.
    /// </summary>
    private static SoundCatalogue? _catalogue;

    /// <summary>Label for each catalogued index, and the index each label names. Built alongside
    /// <see cref="_catalogue" />: how a sound is written on screen is this widget's business rather
    /// than the catalogue's.</summary>
    private static readonly Dictionary<int, string> _labels = [];

    private static readonly Dictionary<string, int> _indices = [];

    private static readonly List<string> _orderedLabels = [];

    private readonly IntegerInputBox _input;

    private readonly ContainsLevenshteinComboBox _names;

    /// <summary>Set while one input is moving the other, so the echo back does not re-enter.</summary>
    private bool _syncing;

    #endregion

    #region Ctor

    /// <param name="index">The index to start on.</param>
    /// <param name="numberWidth">Width for the raw-number field.</param>
    /// <param name="nameWidth">Width for the name list.</param>
    public SoundIndexPicker(int index, int numberWidth, int nameWidth)
    {
        Spacing = SPACING;

        EnsureCatalogue();

        _input = new IntegerInputBox
        {
            MinValue = 0,
            Width = numberWidth,
            VerticalAlignment = VerticalAlignment.Center,

            // Its own, so a bare number field beside a list of names says what it is for rather than
            // repeating whatever the row as a whole is captioned with.
            Tooltip = TazLang.Get(
                "overlaytrigger_sound_number_tooltip",
                "The sound's number in the client's data.\n"
                + "Type one here for a sound the list has no name for."
            )
        };

        // addSelectedItemIfMissing is off: an index the data has no name for is a real choice, but
        // adding a row for it would put a made-up entry in a list that otherwise mirrors the data
        // files. The number field carries it instead, and the list shows nothing selected.
        _names = new ContainsLevenshteinComboBox(
            LabelFor(index) ?? string.Empty,
            _orderedLabels,
            OnNameChosen,
            addSelectedItemIfMissing: false
        )
        {
            VerticalAlignment = VerticalAlignment.Center,
            TooltipSelector = name => name,
            Width = nameWidth
        };

        MyraStyle.ApplySearchComboBoxPopupBorder(_names);

        _input.Value = index;
        _input.ValueChanged += (_, args) => OnNumberTyped(args.NewValue);

        Widgets.Add(_input);
        Widgets.Add(_names);
        Widgets.Add(PlayButton());
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Plays the chosen sound, filters bypassed - the point is to hear what was picked, not what the
    /// player's audio settings would let through during play.
    /// </summary>
    private MyraButton PlayButton() =>
        new(
            TazLang.Get("overlaytrigger_sound_play", "Play"),
            () => Client.Game?.Audio?.PlaySound(Index, true)
        )
        {
            VerticalAlignment = VerticalAlignment.Center,
            Tooltip = TazLang.Get("overlaytrigger_sound_play_tooltip", "Hear the chosen sound.")
        };

    private void OnNumberTyped(int index)
    {
        if (_syncing)
            return;

        _syncing = true;

        try
        {
            // Null where the index has no name, which clears the list rather than leaving it
            // pointing at whatever was chosen before.
            _names.SelectedIndex = PositionOf(index);
        }
        finally
        {
            _syncing = false;
        }

        IndexChanged?.Invoke(this, index);
    }

    private void OnNameChosen(string? label)
    {
        if (_syncing || label == null || !_indices.TryGetValue(label, out int index))
            return;

        _syncing = true;

        try
        {
            _input.Value = index;
        }
        finally
        {
            _syncing = false;
        }

        IndexChanged?.Invoke(this, index);
    }

    private static string? LabelFor(int index) => _labels.GetValueOrDefault(index);

    private static int? PositionOf(int index)
    {
        string? label = LabelFor(index);

        if (label == null)
            return null;

        int position = _orderedLabels.IndexOf(label);

        return position < 0 ? null : position;
    }

    /// <summary>
    /// Reads the sound names once and builds the label lookups over them. Leaves everything empty
    /// where no sound data is loaded, which makes the number field the only way in - correct, rather
    /// than an error: the picker is still usable, just not browsable.
    /// </summary>
    private static void EnsureCatalogue()
    {
        if (_catalogue != null)
            return;

        try
        {
            _catalogue = SoundCatalogue.Build(Client.Game?.UO?.FileManager?.Sounds);
        }
        catch (Exception e)
        {
            // A malformed or partly written sound file must not take the rule editor down with it;
            // the number field still works without a single name.
            Log.Warn($"Sound names could not be read for the overlay rule editor: {e}");

            _catalogue = SoundCatalogue.Build(null);
        }

        foreach (SoundEntry entry in _catalogue.Entries)
        {
            // The index is part of the label, not decoration: it is what makes two sounds sharing a
            // name distinguishable, and what the player types into the box beside it.
            string label = $"{entry.Index} - {entry.Name}";

            _labels[entry.Index] = label;
            _indices[label] = entry.Index;
            _orderedLabels.Add(label);
        }
    }

    #endregion
}
