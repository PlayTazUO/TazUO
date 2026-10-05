#nullable enable
using System;
using System.Linq;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Game.UI.MyraWindows.Theme;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows;

/// <summary>
///     Editor for how a macro's standalone button looks on screen — its label, scale, hue and gump
///     graphic — with a live preview of the result.
/// </summary>
/// <remarks>
///     Edits are written straight onto the live macro, which is what lets the preview - and the real
///     button already on screen - show them as they happen. Closing therefore has to put back what was
///     there on open, or "close without saving" would still have changed the macro for the session.
///     Open through <see cref="Show" />, which keeps a single editor across call sites.
/// </remarks>
public sealed class MacroButtonEditorWindow : MyraControl
{
    #region Private members

    /// <summary>Scale bounds, in percent, matching what the macro itself accepts.</summary>
    private const int MIN_SCALE = 10;

    private const int MAX_SCALE = 200;

    /// <summary>Keeps the preview box from resizing under the controls as the button grows and shrinks.</summary>
    private const int PREVIEW_MIN_HEIGHT = 120;

    /// <summary>Widths of the graphic picker's two halves: the number field and the searchable list.</summary>
    private const int GRAPHIC_NUMBER_WIDTH = 110;

    private const int GRAPHIC_LIST_WIDTH = 130;

    private const int SLIDER_WIDTH = 180;

    /// <summary>
    ///     Inset of the setting rows within their section, kept equal on both sides so the
    ///     right-pinned controls do not sit flush against the border.
    /// </summary>
    private const int ROW_INSET = 20;

    /// <summary>
    ///     Gap between setting rows. Wider than the shared spacing, which packs rows of mixed
    ///     heights - sliders, swatches, radio groups - too tightly to scan.
    /// </summary>
    private const int ROW_SPACING = 10;

    /// <summary>Gap between the window's sections.</summary>
    private const int SECTION_SPACING = 8;

    /// <summary>Gap between the settings grid's columns: the override gate, the caption, the controls.</summary>
    private const int COLUMN_SPACING = 8;

    /// <summary>The macro being edited. Written to live, and rolled back on close unless saved.</summary>
    private readonly Macro _macro;

    private readonly MacroButtonPreview _preview;

    /// <summary>
    ///     The appearance the macro reverts to when the window closes: what it had on open, or what the
    ///     last <see cref="Save" /> committed.
    /// </summary>
    private MacroButtonAppearance _committed;

    /// <summary>Guards the revert, which several independent close paths all have to reach.</summary>
    private bool _closed;

    /// <summary>
    ///     The active hue's selector, held so the inactive hue can keep it in step while it is set to
    ///     follow - otherwise it would sit showing a hue the button no longer uses.
    /// </summary>
    private HueSelector _activeHueSelector = null!;

    /// <summary>
    ///     The active graphic's picker, held for the same reason as <see cref="_activeHueSelector" /> and
    ///     so its gate can adopt whatever it currently shows.
    /// </summary>
    private GumpGraphicPicker _activeGraphicPicker = null!;

    /// <summary>Set while the inactive graphic is moving the active picker, so the echo is not read as an edit.</summary>
    private bool _mirroringActiveGraphic;

    /// <summary>Names which state the preview is showing; kept in step with it, clicks included.</summary>
    private readonly MyraLabel _previewStateLabel = new(string.Empty, MyraLabel.TextStyle.P) { HorizontalAlignment = HorizontalAlignment.Center };

    #endregion

    #region Ctor

    /// <summary>
    ///     Opens an editor over a macro. Private because <see cref="Show" /> owns the single-instance
    ///     rule and the placement.
    /// </summary>
    /// <param name="macro">The macro whose button is being edited. Edited in place; see the type's remarks.</param>
    private MacroButtonEditorWindow(Macro macro)
        : base(TazLang.GetEx("macrobtneditor_titlefor", "Macro Button Editor - {0}", [macro.Name]))
    {
        _macro = macro;
        _committed = MacroButtonAppearance.Capture(macro);

        _preview = new MacroButtonPreview(macro);
        _preview.ShowActiveStateChanged += (_, _) => SyncPreviewStateLabel();
        SyncPreviewStateLabel();

        // The title-bar close goes straight to the base's dispose flag without passing through
        // Dispose(), so the revert needs this hook as well as the override.
        _rootWindow.Closed += (_, _) => Revert();

        Build();
        CenterInViewPort();
    }

    #endregion

    #region Public methods

    /// <summary>
    ///     Opens the editor for a macro, replacing any editor already open so the two cannot disagree
    ///     about the same macro.
    /// </summary>
    /// <param name="macro">The macro whose button is being edited.</param>
    /// <param name="position">Where to place the window; centered in the viewport when omitted.</param>
    public static void Show(Macro macro, Vector2? position = null)
    {
        foreach (IGui gump in UIManager.Gumps.ToList())
            if (gump is MacroButtonEditorWindow { IsDisposed: false } existing)
                existing.Dispose();

        var window = new MacroButtonEditorWindow(macro);
        UIManager.Add(window);

        if (position.HasValue)
            window.SetPosition((int)position.Value.X, (int)position.Value.Y);

        window.SetInScreen();
        window.BringOnTop();
    }

    /// <inheritdoc />
    /// <remarks>Reverts anything the user did not save; see the type's remarks.</remarks>
    public override void Dispose()
    {
        Revert();
        _preview.Dispose();
        base.Dispose();
    }

    #endregion

    #region Private methods

    /// <summary>Assembles the window: the settings, the preview, and the save/close row.</summary>
    private void Build()
    {
        var root = new VerticalStackPanel { Spacing = SECTION_SPACING, MinWidth = 350 };

        root.Widgets.Add(BuildAppearanceSection());
        root.Widgets.Add(BuildPreviewSection());
        root.Widgets.Add(BuildButtonRow());

        SetRootContent(root);
    }

    /// <summary>Builds the settings section: every property of the button's two states.</summary>
    /// <remarks>
    ///     A grid, so the three columns - the override gate, the caption, the controls - line up down
    ///     the section whatever the rows contain. Sized to its contents rather than stretched, which
    ///     keeps the controls beside their captions instead of flung at the window's right edge when
    ///     the user widens it.
    /// </remarks>
    /// <returns>The section, ready to add to the window root.</returns>
    private VisualContainer BuildAppearanceSection()
    {
        var rows = new Grid
        {
            ColumnSpacing = COLUMN_SPACING,
            RowSpacing = ROW_SPACING,
            Padding = new Thickness(ROW_INSET, 0, ROW_INSET, 0),
            ColumnsProportions =
            {
                new Proportion(ProportionType.Auto),
                new Proportion(ProportionType.Auto),
                new Proportion(ProportionType.Auto)
            }
        };

        // Built before the inactive rows, whose handlers reach for these.
        Widget activeHue = BuildActiveHueControls();
        Widget activeGraphic = BuildActiveGraphicControls();

        AddFullWidthRow(rows, BuildHideLabelToggle());
        AddRow(rows, null, TazLang.Get("macrobtneditor_scale", "Scale"), null, BuildScaleSlider());

        AddRow(
            rows,
            null,
            TazLang.Get("macrobtneditor_inactivehue", "Inactive hue"),
            TazLang.Get("macrobtneditor_inactivehue_tooltip", "Hue of the button at rest"),
            BuildInactiveHueControls()
        );

        AddRow(rows, BuildActiveHueGate(activeHue), TazLang.Get("macrobtneditor_activehue", "Active hue"),
            TazLang.Get("macrobtneditor_activehue_tooltip",
                "Hue of the button while the macro is running.\nUnticked, it keeps the inactive hue."),
            activeHue);

        AddRow(
            rows,
            null,
            TazLang.Get("macrobtneditor_inactivegraphic", "Inactive graphic"),
            TazLang.Get("macrobtneditor_inactivegraphic_gatetooltip",
                "Gump graphic of the button at rest. Default draws no graphic"),
            BuildInactiveGraphicControls()
        );

        AddRow(rows, BuildActiveGraphicGate(activeGraphic), TazLang.Get("macrobtneditor_activegraphic", "Active graphic"),
            TazLang.Get("macrobtneditor_activegraphic_gatetooltip",
                "Gump graphic shown while the macro is running.\nUnticked, it keeps the inactive graphic."),
            activeGraphic);

        return new VisualContainer(
            new VisualContainerProps { LabelText = TazLang.Get("macrobtneditor_appearance", "Appearance") },
            rows
        ) { Margin = new Thickness(0, 10, 0, 0), HorizontalAlignment = HorizontalAlignment.Stretch };
    }

    /// <summary>
    ///     Places one setting across the grid's three columns, remembering the caption so a gate can dim
    ///     it along with the controls it governs.
    /// </summary>
    /// <param name="grid">The section grid.</param>
    /// <param name="gate">The override toggle, or null for a setting that is always in force.</param>
    /// <param name="label">The caption.</param>
    /// <param name="tooltip">Tooltip shared by the caption and the gate.</param>
    /// <param name="controls">The setting's controls.</param>
    private static void AddRow(Grid grid, GateToggle? gate, string label, string? tooltip, Widget controls)
    {
        int row = grid.RowsProportions.Count;
        grid.RowsProportions.Add(new Proportion(ProportionType.Auto));

        var caption = new MyraLabel(label, MyraLabel.TextStyle.P) { Tooltip = tooltip };

        if (gate != null)
        {
            gate.CheckBox.Tooltip = tooltip;
            Place(grid, gate.CheckBox, row, 0);
            gate.Bind(caption);
        }

        Place(grid, caption, row, 1);
        Place(grid, controls, row, 2);
    }

    /// <summary>Places a widget that owns the whole row, such as a standalone toggle.</summary>
    /// <param name="grid">The section grid.</param>
    /// <param name="widget">The widget to place.</param>
    private static void AddFullWidthRow(Grid grid, Widget widget)
    {
        int row = grid.RowsProportions.Count;
        grid.RowsProportions.Add(new Proportion(ProportionType.Auto));

        Place(grid, widget, row, 0);
        Grid.SetColumnSpan(widget, 3);
    }

    /// <summary>Puts a widget in one grid cell, centred so mixed-height rows sit on a common line.</summary>
    /// <param name="grid">The section grid.</param>
    /// <param name="widget">The widget to place.</param>
    /// <param name="row">Target row.</param>
    /// <param name="column">Target column.</param>
    private static void Place(Grid grid, Widget widget, int row, int column)
    {
        widget.VerticalAlignment = VerticalAlignment.Center;

        Grid.SetRow(widget, row);
        Grid.SetColumn(widget, column);
        grid.Widgets.Add(widget);
    }

    /// <summary>Toggle for whether the button draws the macro's name over its graphic.</summary>
    /// <returns>The check button, already bound to the macro.</returns>
    private MyraCheckButton BuildHideLabelToggle() =>
        MyraCheckButton.CreateWithCallback(
            _macro.HideLabel,
            hidden =>
            {
                _macro.HideLabel = hidden;
                _preview.Refresh();
            },
            TazLang.Get("macrobtneditor_hidelabel", "Hide Label"),
            TazLang.Get("macrobtneditor_hidelabel_tooltip", "Hide the macro's name on the button")
        );

    /// <summary>Slider for the button's size, as a percentage of the graphic's native size.</summary>
    /// <returns>The slider.</returns>
    private Widget BuildScaleSlider()
    {
        var slider = LabeledHorizontalSlider.CreateSliderWithCallback(
            MIN_SCALE,
            MAX_SCALE,
            _macro.Scale,
            scale =>
            {
                _macro.Scale = (byte)scale;

                // Scale alone never invalidates the hue bake, so stay off the re-baking path: this
                // fires on every pixel of a slider drag.
                _preview.RefreshScale();
            }
        );

        slider.Width = SLIDER_WIDTH;

        return slider;
    }

    /// <summary>Hue of the button at rest, which the active hue falls back to while its gate is off.</summary>
    /// <returns>The selector.</returns>
    private Widget BuildInactiveHueControls()
    {
        var selector = new HueSelector(_macro.Hue);

        selector.HueChanged += (_, hue) =>
        {
            _macro.Hue = hue;

            // Mirrored rather than left stale: a disabled selector still reads as the active hue, and
            // showing a value the button no longer uses is exactly what the gate exists to clarify.
            if (!_macro.ActiveHue.HasValue)
                _activeHueSelector.Hue = hue;

            _preview.Refresh();
        };

        return selector;
    }

    /// <summary>Hue of the button while the macro runs, in force only while its gate is ticked.</summary>
    /// <returns>The selector.</returns>
    private Widget BuildActiveHueControls()
    {
        var selector = new HueSelector(_macro.HueFor(true));
        _activeHueSelector = selector;

        selector.HueChanged += (_, hue) =>
        {
            _macro.ActiveHue = hue;
            _preview.Refresh();
        };

        return selector;
    }

    /// <summary>
    ///     The active hue's override gate. Ticking it adopts whatever the selector already shows, which
    ///     is the inherited hue, so the button does not jump to a colour the user never saw.
    /// </summary>
    /// <param name="controls">The selector the gate governs.</param>
    /// <returns>The gate.</returns>
    private GateToggle BuildActiveHueGate(Widget controls) =>
        new(_macro.ActiveHue.HasValue, controls, isOn =>
        {
            _macro.ActiveHue = isOn ? _activeHueSelector.Hue : null;
            _preview.Refresh();
        });

    /// <summary>Gump graphic of the button at rest. Its Default entry means the button draws a bare plate.</summary>
    /// <remarks>
    ///     Carries the sizing warning, since this is the graphic the size is taken from.
    /// </remarks>
    /// <returns>The picker, with its warning chip.</returns>
    private Widget BuildInactiveGraphicControls()
    {
        var picker = BuildGraphicPicker(
            _macro.Graphic,
            TazLang.Get("macrobtneditor_inactivegraphic_gatetooltip",
                "Gump graphic of the button at rest. Default draws no graphic")
        );

        picker.GraphicChanged += (_, graphic) =>
        {
            _macro.Graphic = graphic;

            if (!_macro.ActiveGraphic.HasValue)
                MirrorToActiveGraphic(graphic);

            _preview.Refresh();
        };

        var row = new HorizontalStackPanel
        {
            Spacing = MyraStyle.STANDARD_SPACING,
            VerticalAlignment = VerticalAlignment.Center
        };

        row.Widgets.Add(picker);
        row.Widgets.Add(new WarningChip(TazLang.Get("macrobtneditor_sizewarning",
            "This graphic alone sets the button's size, together with Scale.\nThe active graphic is stretched to fit it, so the button never resizes while the macro runs.")));

        return row;
    }

    /// <summary>
    ///     Gump graphic of the button while the macro runs, in force only while its gate is ticked.
    /// </summary>
    /// <remarks>
    ///     Its Default entry means "draw nothing while running", which is not what the gate means -
    ///     unticking the gate inherits the inactive graphic instead. The two are stored apart; see
    ///     <see cref="Macro.ACTIVE_GRAPHIC_NONE" />.
    /// </remarks>
    /// <returns>The picker.</returns>
    private Widget BuildActiveGraphicControls()
    {
        GumpGraphicPicker picker = BuildGraphicPicker(
            _macro.GraphicFor(true),
            TazLang.Get("macrobtneditor_activegraphic_gatetooltip",
                "Gump graphic shown while the macro is running.\nUnticked, it keeps the inactive graphic.")
        );

        _activeGraphicPicker = picker;

        picker.GraphicChanged += (_, graphic) =>
        {
            // Mirroring moves this picker while the gate is off; committing then would switch the gate
            // on behind the user's back.
            if (_mirroringActiveGraphic || !_macro.ActiveGraphic.HasValue)
                return;

            _macro.ActiveGraphic = ToActiveGraphic(graphic);
            _preview.Refresh();
        };

        return picker;
    }

    /// <summary>
    ///     The active graphic's override gate. Ticking it adopts whatever the picker already shows.
    /// </summary>
    /// <param name="controls">The picker the gate governs.</param>
    /// <returns>The gate.</returns>
    private GateToggle BuildActiveGraphicGate(Widget controls) =>
        new(_macro.ActiveGraphic.HasValue, controls, isOn =>
        {
            _macro.ActiveGraphic = isOn ? ToActiveGraphic(_activeGraphicPicker.Graphic) : null;
            _preview.Refresh();
        });

    /// <summary>Moves the active picker without letting its change handler treat that as an edit.</summary>
    /// <param name="graphic">The graphic to show.</param>
    private void MirrorToActiveGraphic(ushort? graphic)
    {
        _mirroringActiveGraphic = true;

        try
        {
            _activeGraphicPicker.Graphic = graphic;
        }
        finally
        {
            _mirroringActiveGraphic = false;
        }
    }

    /// <summary>
    ///     Maps what the picker reports onto the active graphic's storage, where null is already spoken
    ///     for by the gate.
    /// </summary>
    /// <param name="graphic">The picker's graphic, null for its Default entry.</param>
    /// <returns>The value to store.</returns>
    private static int ToActiveGraphic(ushort? graphic) => graphic ?? Macro.ACTIVE_GRAPHIC_NONE;

    /// <summary>Builds a gump-graphic picker sized for this window's control column.</summary>
    /// <param name="graphic">The graphic to start on.</param>
    /// <param name="tooltip">Tooltip for the number field.</param>
    /// <returns>The picker.</returns>
    private static GumpGraphicPicker BuildGraphicPicker(ushort? graphic, string tooltip) =>
        new(graphic)
        {
            NumberInput =
            {
                Width = GRAPHIC_NUMBER_WIDTH,
                HintText = TazLang.Get("macrobtneditor_graphic_hint", "Graphic # or 0x.."),
                Tooltip = tooltip
            },
            NameList =
            {
                Width = GRAPHIC_LIST_WIDTH,
                SearchHintText = TazLang.Get("macrobtneditor_graphic_searchhint", "Search for a gump..")
            }
        };

    /// <summary>Builds the preview box and the line naming which state it is showing.</summary>
    /// <returns>The section, ready to add to the window root.</returns>
    private VisualContainer BuildPreviewSection() =>
        new(
            new VisualContainerProps { LabelText = TazLang.Get("macrobtneditor_preview", "Preview"), LabelHorizontalAlignment = HorizontalAlignment.Center },
            new Panel { MinHeight = PREVIEW_MIN_HEIGHT, HorizontalAlignment = HorizontalAlignment.Stretch, Widgets = { _preview } },
            _previewStateLabel
        );

    /// <summary>
    ///     Names the state the preview is showing, and says the preview is clickable.
    /// </summary>
    /// <remarks>
    ///     Editing-only: the running macro drives the real button, and a short macro's active state is
    ///     over far too quickly to judge a graphic by.
    /// </remarks>
    private void SyncPreviewStateLabel() =>
        _previewStateLabel.Text = _preview.ShowActiveState
            ? TazLang.Get("macrobtneditor_previewstate_active", "Active - click to toggle")
            : TazLang.Get("macrobtneditor_previewstate_inactive", "Inactive - click to toggle");

    /// <summary>Builds the save/close row. Close discards, so the two are not interchangeable.</summary>
    /// <returns>The row, right-aligned.</returns>
    private HorizontalStackPanel BuildButtonRow()
    {
        var row = new HorizontalStackPanel { Spacing = MyraStyle.STANDARD_SPACING, HorizontalAlignment = HorizontalAlignment.Right };

        row.Widgets.Add(new MyraButton(TazLang.Get("macrobtneditor_save", "Save"), Save)
        {
            Tooltip = TazLang.Get("macrobtneditor_save_tooltip", "Keep these changes and write them to disk")
        });

        // Dispose() rather than the base's flag, so the revert runs.
        row.Widgets.Add(new MyraButton(TazLang.Get("uicommons_close", "Close"), Dispose)
        {
            Tooltip = TazLang.Get("macrobtneditor_close_tooltip", "Discard any changes made since the last save")
        });

        return row;
    }

    /// <summary>
    ///     Commits the current appearance, persists the macro list, and re-reads the macro into any
    ///     button already on screen, which caches the appearance fields rather than reading them per frame.
    /// </summary>
    private void Save()
    {
        _committed = MacroButtonAppearance.Capture(_macro);

        World.Instance.Macros.Save();
        RefreshLiveButtons();
    }

    /// <summary>
    ///     Puts back the last committed appearance. Idempotent, since several close paths reach it.
    /// </summary>
    private void Revert()
    {
        if (_closed)
            return;

        _closed = true;

        if (_committed.Matches(_macro))
            return;

        _committed.ApplyTo(_macro);
        RefreshLiveButtons();
    }

    /// <summary>Re-reads the macro into every button showing it, which caches its appearance.</summary>
    private void RefreshLiveButtons()
    {
        foreach (MacroButtonGump button in UIManager.Gumps.OfType<MacroButtonGump>().ToList())
            if (button.TheMacro == _macro)
                button.TheMacro = _macro;
    }

    #endregion

    #region Nested types

    /// <summary>
    ///     The tick box that decides whether one "active" setting overrides its resting counterpart,
    ///     together with the dimming that tells the user which way it is set.
    /// </summary>
    /// <remarks>
    ///     Greying the caption as well as the controls is the whole point: a selector showing an
    ///     inherited value is indistinguishable from one deliberately set to the same value, and at hue
    ///     0 "unset" and "no hue" look identical.
    /// </remarks>
    private sealed class GateToggle
    {
        /// <summary>The tick box, for the caller to place.</summary>
        public MyraCheckButton CheckBox { get; }

        /// <summary>What the gate enables and disables.</summary>
        private readonly Widget _controls;

        /// <summary>The row's caption, once bound. Dimmed along with the controls.</summary>
        private MyraLabel? _caption;

        /// <summary>The caption's colour while in force, captured before the first dim overwrites it.</summary>
        private Color _captionColor;

        /// <summary>Builds a gate over one setting's controls.</summary>
        /// <param name="isOn">Whether the override starts in force.</param>
        /// <param name="controls">The controls to enable and disable.</param>
        /// <param name="onChanged">Invoked with the new state so the caller can commit it.</param>
        public GateToggle(bool isOn, Widget controls, Action<bool> onChanged)
        {
            _controls = controls;

            CheckBox = MyraCheckButton.CreateWithCallback(isOn, state =>
            {
                onChanged(state);
                Apply(state);
            });

            Apply(isOn);
        }

        /// <summary>Adopts the caption this gate governs, so it can be dimmed with the controls.</summary>
        /// <param name="caption">The row's caption.</param>
        public void Bind(MyraLabel caption)
        {
            // Captured before the first dim overwrites it, exactly as RadioGroup does.
            _caption = caption;
            _captionColor = caption.TextColor;

            Apply(CheckBox.IsChecked);
        }

        /// <summary>Brings the controls and caption in line with the gate.</summary>
        /// <param name="isOn">Whether the override is in force.</param>
        private void Apply(bool isOn)
        {
            _controls.Enabled = isOn;

            if (_caption != null)
                _caption.TextColor = isOn ? _captionColor : MyraTheme.Current.DisabledText;
        }
    }

    /// <summary>
    ///     A snapshot of everything this editor can change about a macro's button, so an unsaved session
    ///     can be undone. Holds no reference to the macro it came from.
    /// </summary>
    private readonly record struct MacroButtonAppearance(
        bool HideLabel,
        byte Scale,
        ushort Hue,
        ushort? ActiveHue,
        ushort? Graphic,
        int? ActiveGraphic
    )
    {
        /// <summary>Takes a snapshot of a macro's current button appearance.</summary>
        /// <param name="macro">The macro to read. Not retained.</param>
        /// <returns>The snapshot.</returns>
        public static MacroButtonAppearance Capture(Macro macro) => new(
            macro.HideLabel,
            macro.Scale,
            macro.Hue,
            macro.ActiveHue,
            macro.Graphic,
            macro.ActiveGraphic
        );

        /// <summary>Whether a macro's appearance is already what this snapshot holds.</summary>
        /// <param name="macro">The macro to compare against.</param>
        /// <returns>True when applying this snapshot would change nothing.</returns>
        public bool Matches(Macro macro) => this == Capture(macro);

        /// <summary>Writes this snapshot back over a macro's appearance.</summary>
        /// <param name="macro">The macro to restore.</param>
        public void ApplyTo(Macro macro)
        {
            macro.HideLabel = HideLabel;
            macro.Scale = Scale;
            macro.Hue = Hue;
            macro.ActiveHue = ActiveHue;
            macro.Graphic = Graphic;
            macro.ActiveGraphic = ActiveGraphic;
        }
    }

    #endregion
}
