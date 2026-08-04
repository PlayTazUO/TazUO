#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations;
using ClassicUO.Game.ScreenDecorations.Overlays;
using ClassicUO.Game.UI.MyraWindows.Options.Tabs;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.Renderer.Effects;
using FontStashSharp.RichText;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Properties;
using Myra.Graphics2D.UI.WrapPanel;

namespace ClassicUO.Game.UI.MyraWindows.Options.Editors.Overlays;

/// <summary>
/// Edits one <see cref="OverlayEffectProfile"/>: its fade timing and one layer at a time, the layer
/// itself through a <see cref="PropertyGrid"/> over <see cref="OverlayLayer"/>.
/// <para>
/// Reflecting over the layer struct rather than hand-listing its parameters is deliberate - the
/// shader has around thirty five of them and they change as it is tuned.
/// </para>
/// </summary>
internal sealed class OverlayProfileEditor : Widget
{
    private const int INPUT_WIDTH = 80;
    private const float MAX_FADE_SECONDS = 10f;

    private const int ROW_SPACING = 12;
    private const int COLUMN_SPACING = 12;
    private const int GROUP_SPACING = 10;

    private const int GLYPH_FONT_SIZE = 24;
    private const int GLYPH_BUTTON_SIZE = StyleConstantsDefaults.TOOLBAR_BUTTON_SIZE;

    /// <summary>Extra gap between an editor and its reset button, on top of the row's own spacing.</summary>
    private const int RESET_BUTTON_GAP = 2;

    /// <summary>
    /// Nudge for the expander glyph, which sits above the centre of its line: the symbol font's
    /// ascent leaves more room over the arrow than under it.
    /// </summary>
    private const int EXPANDER_MARK_TOP_OFFSET = 2;

    /// <summary>Reset targets. Boxed once: every lookup reads it through reflection.</summary>
    private static readonly object _defaultLayer = new OverlayLayer { Params = OverlayParams.Default };

    private readonly OverlayEffectProfile _profile;
    private readonly Action _onChanged;

    /// <summary>Built-in profiles are shown so they can be read and copied, never edited.</summary>
    private readonly bool _readOnly;

    private int _selectedLayer;

    public OverlayProfileEditor(OverlayEffectProfile profile, Action onChanged)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(onChanged);

        _profile = profile;
        _onChanged = onChanged;
        _readOnly = profile.IsBuiltIn;

        // Stacked rather than wrapped, so the rows below stay one block. Each row wraps on its own,
        // but a vertical WrapPanel here would answer an expanded parameter table by moving it into
        // a second column beside the toolbar.
        ChildrenLayout = new StackPanelLayout(Orientation.Vertical) { Spacing = MyraStyle.STANDARD_SPACING };
        Rebuild();
    }

    private void Rebuild()
    {
        _selectedLayer = Math.Clamp(_selectedLayer, 0, Math.Max(_profile.Layers.Count - 1, 0));

        Children.Clear();
        Children.Add(BuildFadeRow());
        Children.Add(BuildLayerToolbar());
        Children.Add(OptionTabCommons.StyledHorizontalSeparator());
        Children.Add(BuildLayerGrid());
    }

    private WrapPanel BuildFadeRow() =>
        OptionTabCommons.StyledHorizontalWrapPanel(
            LabeledFloat(
                TazLang.Get("visualeffects_fadein", "Fade in (s)"),
                _profile.FadeInSeconds,
                v => _profile.FadeInSeconds = v
            ),
            LabeledFloat(
                TazLang.Get("visualeffects_fadeout", "Fade out (s)"),
                _profile.FadeOutSeconds,
                v => _profile.FadeOutSeconds = v
            )
        );

    private Widget LabeledFloat(string label, float value, Action<float> commit)
    {
        var input = new FloatInputBox
        {
            MinValue = 0f,
            MaxValue = MAX_FADE_SECONDS,
            Width = INPUT_WIDTH,
            Enabled = !_readOnly,
            Value = value
        };

        // Committing on focus loss rather than per keystroke; the value is persisted, and a save on
        // every character typed is both wasteful and jumpy.
        input.KeyboardFocusChanged += (_, _) =>
        {
            if (input.IsKeyboardFocused)
                return;

            commit(input.Value);
            _onChanged();
        };

        return OptionTabCommons.StyledStackPanel(
            Orientation.Horizontal,
            new MyraLabel(label, MyraLabel.TextStyle.P) { VerticalAlignment = VerticalAlignment.Center },
            input
        );
    }

    private Widget BuildLayerToolbar()
    {
        bool canAdd = !_readOnly && _profile.Layers.Count < ScreenOverlayPreset.MaxLayers;
        bool canRemove = !_readOnly && _profile.Layers.Count > 1;

        string layerWord = TazLang.Get("visualeffects_layer", "Layer");

        string[] names = Enumerable
            .Range(1, _profile.Layers.Count)
            .Select(i => $"{layerWord} {i}")
            .ToArray();

        Widget combo = OptionTabCommons.CreateOptionsComboBox(
            layerWord,
            names.ElementAtOrDefault(_selectedLayer) ?? string.Empty,
            names,
            selected =>
            {
                int index = Array.IndexOf(names, selected);

                if (index < 0 || index == _selectedLayer)
                    return;

                _selectedLayer = index;
                Rebuild();
            }
        );

        combo.Margin = new Thickness(0, 0, 20, 0);

        return OptionTabCommons.StyledHorizontalWrapPanel(
            combo,
            new MyraButton(TazLang.Get("visualeffects_addlayer", "Add layer"), AddLayer) { Enabled = canAdd },
            MyraStyle.ApplyButtonDangerStyle(
                new MyraButton(TazLang.Get("visualeffects_removelayer", "Remove layer"), RemoveLayer) { Enabled = canRemove }
            )
        );
    }

    /// <summary>
    /// The grid edits a boxed copy of the layer, which is what lets it write through the nested
    /// value types; the box is copied back into the list on every change.
    /// </summary>
    private Widget BuildLayerGrid()
    {
        if (_profile.Layers.Count == 0)
            return new MyraLabel(TazLang.Get("visualeffects_nolayers", "This profile has no layers."), MyraLabel.TextStyle.P);

        object boxed = _profile.Layers[_selectedLayer];

        var grid = new PropertyGrid
        {
            IgnoreCollections = true,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            RowSpacing = ROW_SPACING,
            ColumnSpacing = COLUMN_SPACING,
            GroupSpacing = GROUP_SPACING,
            GroupSeparators = true,
            ToggleGroupsOnSingleClick = true,
            DefaultValueProvider = DefaultValueOf,
            ResetButtonFactory = CreateResetButton,
            MarkContentFactory = CreateExpanderMark
        };

        // Assigning it builds the rows, and the sub-grids copy the settings above from their parent
        // as they are created.
        grid.Object = boxed;

        // After the rows exist: Enabled is pushed to the children present at the time it is set,
        // so disabling an empty grid would leave everything built afterwards editable.
        grid.Enabled = !_readOnly;

        grid.PropertyChanged += (_, _) =>
        {
            if (_readOnly)
                return;

            _profile.Layers[_selectedLayer] = (OverlayLayer)boxed;
            _onChanged();
        };

        return grid;
    }

    /// <summary>
    /// The grid's own reset button and expander mark are drawn from the skin's 8px tree glyphs.
    /// Both are supplied from here instead, out of the symbol font the rest of the UI uses.
    /// </summary>
    private static Widget CreateResetButton(Record record, Action reset)
    {
        var button = new Button
        {
            Width = GLYPH_BUTTON_SIZE,
            Height = GLYPH_BUTTON_SIZE,
            Tooltip = TazLang.Get("visualeffects_resettodefault", "Reset to default"),
            Content = GlyphLabel(StyleConstantsDefaults.RESET_LABEL_ICON_TEXT),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(RESET_BUTTON_GAP, 0, 0, 0)
        };

        button.Click += (_, _) => reset();

        return button;
    }

    /// <summary>
    /// Unsized, unlike the reset glyph: the mark's own button has had its padding stripped, so a
    /// fixed-height label would draw its text from the top of that box instead of centred in it.
    /// </summary>
    private static Widget CreateExpanderMark(bool expanded) =>
        new MyraLabel(expanded ? "⮟" : "⮞", GLYPH_FONT_SIZE)
        {
            Font = TrueTypeLoader.Instance.GetFont(EmbeddedFontNames.NOTO_SANS_2_SYMBOLS, GLYPH_FONT_SIZE),
            Wrap = false,
            SingleLine = true,
            TextAlign = TextHorizontalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Top = EXPANDER_MARK_TOP_OFFSET
        };

    /// <summary>
    /// Sized to fill its button, whose own padding is what keeps the glyph centred.
    /// </summary>
    private static MyraLabel GlyphLabel(string glyph) =>
        new(glyph, GLYPH_FONT_SIZE)
        {
            Font = TrueTypeLoader.Instance.GetFont(EmbeddedFontNames.NOTO_SANS_2_SYMBOLS, GLYPH_FONT_SIZE),
            Wrap = false,
            SingleLine = true,
            TextAlign = TextHorizontalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Width = GLYPH_BUTTON_SIZE,
            Height = GLYPH_BUTTON_SIZE
        };

    /// <summary>
    /// Walks <see cref="_defaultLayer"/> down the same path the grid is showing, so every parameter
    /// gets its reset value from <see cref="OverlayParams.Default"/> rather than from a duplicated
    /// table of constants.
    /// </summary>
    private static object DefaultValueOf(PropertyGrid grid, Record record)
    {
        object owner = _defaultLayer;

        foreach (Record step in grid.ParentRecords)
        {
            owner = step.GetValue(owner);

            if (owner == null)
                return null;
        }

        return record.GetValue(owner);
    }

    private void AddLayer()
    {
        if (_profile.Layers.Count >= ScreenOverlayPreset.MaxLayers)
            return;

        // Seeded from the layer on screen rather than from Default: a new layer is nearly always a
        // variation of the one being tuned, and Default shares no scale, channel or tint with it.
        OverlayLayer seed = _profile.Layers.Count > 0
            ? _profile.Layers[_selectedLayer]
            : new OverlayLayer { Params = OverlayParams.Default };

        _profile.Layers.Add(seed);
        _selectedLayer = _profile.Layers.Count - 1;

        _onChanged();
        Rebuild();
    }

    private void RemoveLayer()
    {
        if (_profile.Layers.Count <= 1)
            return;

        _profile.Layers.RemoveAt(_selectedLayer);
        _selectedLayer = Math.Min(_selectedLayer, _profile.Layers.Count - 1);

        _onChanged();
        Rebuild();
    }
}
