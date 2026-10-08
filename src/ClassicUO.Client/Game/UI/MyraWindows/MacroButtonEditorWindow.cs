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
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.WrapPanel;

namespace ClassicUO.Game.UI.MyraWindows;

/// <summary>
///     Editor for how a macro's standalone button looks on screen — its label, scale, hue and gump
///     graphic — with a live preview of the result.
/// </summary>
/// <remarks>
///     Edits are written straight onto the live macro, which is what lets the preview show them as they
///     happen. The button already on screen does not follow along - it caches the macro's appearance
///     when it is handed one - so saving and reverting both re-hand it the macro. Closing therefore has
///     to put back what was there on open, or "close without saving" would still have changed the macro
///     for the session. Open through <see cref="Show" />, which keeps a single editor across call sites.
/// </remarks>
public sealed class MacroButtonEditorWindow : MyraControl
{
    #region Private members

    /// <summary>Scale bounds, in percent, matching what the macro itself accepts.</summary>
    private const int MIN_SCALE = 10;

    private const int MAX_SCALE = 200;

    /// <summary>Scale a button starts at, and the one <see cref="Reset" /> puts it back to.</summary>
    private const byte DEFAULT_SCALE = 100;

    /// <summary>Keeps the preview box from resizing under the controls as the button grows and shrinks.</summary>
    private const int PREVIEW_MIN_HEIGHT = 120;

    /// <summary>Widths of the graphic picker's two halves: the number field and the searchable list.</summary>
    private const int GRAPHIC_NUMBER_WIDTH = 80;

    private const int GRAPHIC_LIST_WIDTH = 130;

    private const int SLIDER_WIDTH = 180;

    /// <summary>Width of the hue index field, narrower than the graphic's since a hue is at most five digits.</summary>
    private const int HUE_INPUT_WIDTH = 60;

    /// <summary>Width of the custom label field.</summary>
    private const int LABEL_INPUT_WIDTH = 150;

    /// <summary>Opacity floor. Zero is already reachable by clearing the label, so the slider need not go there.</summary>
    private const int MIN_OPACITY = 0;

    /// <summary>Width of the opacity slider, narrower than the shared scale slider's column.</summary>
    private const int OPACITY_SLIDER_WIDTH = 140;

    /// <summary>Gap between the two state columns, wider than the gap within one so they read as a pair.</summary>
    private const int STATE_COLUMN_SPACING = 14;

    /// <summary>
    ///     Breathing room between the window frame and its contents, which the frame's own padding does
    ///     not give enough of once settings run the full width.
    /// </summary>
    private const int WINDOW_INSET = 12;

    /// <summary>Gap above the shared settings, so they do not sit against the title bar.</summary>
    private const int SHARED_TOP_MARGIN = 6;

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

    /// <summary>Rebuilt by <see cref="RebuildContent" />, so not readonly.</summary>
    private MacroButtonPreview _preview = null!;

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
    private HueSelector? _activeHueSelector;

    /// <summary>The active label hue's selector, kept in step with the resting one for the same reason.</summary>
    private HueSelector? _activeLabelHueSelector;

    /// <summary>The active label opacity's slider, kept in step with the resting one for the same reason.</summary>
    private LabeledHorizontalSlider? _activeOpacitySlider;

    /// <summary>
    ///     The active graphic's picker, held for the same reason as <see cref="_activeHueSelector" /> and
    ///     so its gate can adopt whatever it currently shows.
    /// </summary>
    private GumpGraphicPicker _activeGraphicPicker = null!;

    /// <summary>
    ///     The active label's box, kept in step with the resting one while it is set to follow, so
    ///     ticking its gate adopts the text the user is actually looking at.
    /// </summary>
    private MyraInputBox? _activeLabelInput;

    /// <summary>Set while the inactive graphic is moving the active picker, so the echo is not read as an edit.</summary>
    private bool _mirroringActiveGraphic;

    /// <summary>Names which state the preview is showing; kept in step with it, clicks included.</summary>
    private MyraLabel _previewStateLabel = null!;

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

        CreatePreview();
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
        // The hue swatches open a classic gump that is not parented to this window, so it outlives the
        // close unless shut here - and a pick landing after the revert would write an edit back onto the
        // macro with nothing left to undo it.
        UIManager.GetGump<ModernColorPicker>()?.Dispose();

        Revert();
        _preview.Dispose();
        base.Dispose();
    }

    #endregion

    #region Private methods

    /// <summary>Builds the preview and the caption naming the state it shows, replacing any already built.</summary>
    private void CreatePreview()
    {
        _preview?.Dispose();

        _preview = new MacroButtonPreview(_macro);
        _preview.ShowActiveStateChanged += (_, _) => SyncPreviewStateLabel();

        // Rebuilt alongside the preview rather than reused: a Myra widget belongs to one parent, and a
        // rebuild hands both of these to a new one.
        _previewStateLabel = new MyraLabel(string.Empty, MyraLabel.TextStyle.P) { HorizontalAlignment = HorizontalAlignment.Center };

        SyncPreviewStateLabel();
    }

    /// <summary>Assembles the window: the settings, the preview, and the save/close row.</summary>
    private void Build()
    {
        var root = new VerticalStackPanel { Spacing = SECTION_SPACING, MinWidth = 350, Padding = new Thickness(WINDOW_INSET, 0, WINDOW_INSET, WINDOW_INSET) };

        root.Widgets.Add(BuildSharedSection());
        root.Widgets.Add(new HorizontalSeparator());
        root.Widgets.Add(BuildStateColumns());
        root.Widgets.Add(new HorizontalSeparator());
        root.Widgets.Add(BuildPreviewSection());
        root.Widgets.Add(BuildButtonRow());

        SetRootContent(root);
    }

    /// <summary>Settings that apply to the button in both run states.</summary>
    /// <returns>The shared strip.</returns>
    private Widget BuildSharedSection()
    {
        var rows = new Grid
        {
            ColumnSpacing = COLUMN_SPACING,
            RowSpacing = ROW_SPACING,
            Margin = new Thickness(0, SHARED_TOP_MARGIN, 0, 0),
            ColumnsProportions = { Auto(), Auto(), Auto() }
        };

        var scale = LabeledHorizontalSlider.CreateSliderWithCallback(
            MIN_SCALE,
            MAX_SCALE,
            _macro.Scale,
            value =>
            {
                _macro.Scale = (byte)value;

                // Scale alone never invalidates the hue bake, so stay off the re-baking path: this
                // fires on every pixel of a slider drag.
                _preview.RefreshScale();
            }
        );

        scale.Width = SLIDER_WIDTH;

        int row = NewRow(rows);
        Place(rows, new MyraLabel(TazLang.Get("macrobtneditor_scale", "Scale"), MyraLabel.TextStyle.P), row, 1);
        Place(rows, scale, row, 2);

        // The size warning belongs here rather than on a graphic: the button's size is the inactive
        // graphic's, times this.
        Place(rows, new WarningChip(TazLang.Get("macrobtneditor_sizenotice",
                "This and the inactive graphic together set the button's size.\nThe active graphic is stretched to fit, so the button never resizes while the macro runs")),
            row, 3);

        rows.ColumnsProportions.Add(Auto());

        return rows;
    }

    /// <summary>
    ///     Builds the two mirrored state columns, each holding the same three settings.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Grids rather than stacks, so captions and controls line up down each column whatever the
    ///         rows contain. The two are separate grids deliberately: sharing one would tie the left
    ///         column's caption width to the right's, and they hold different words.
    ///     </para>
    ///     <para>
    ///         Wrapped rather than stacked side by side, so narrowing the window drops the running state
    ///         under the resting one instead of clipping it. That rules out a rule between them - a
    ///         vertical line reads as a divider only while they are actually side by side - so the column
    ///         headings carry the separation instead.
    ///     </para>
    /// </remarks>
    /// <returns>The pair of columns.</returns>
    private Widget BuildStateColumns()
    {
        var columns = new WrapPanel { Orientation = Orientation.Horizontal, HorizontalSpacing = STATE_COLUMN_SPACING, VerticalSpacing = STATE_COLUMN_SPACING };

        columns.Widgets.Add(BuildStateColumn(false));
        columns.Widgets.Add(BuildStateColumn(true));

        return columns;
    }

    /// <summary>
    ///     Builds one state's column of settings.
    /// </summary>
    /// <remarks>
    ///     Every row reads the same way: an optional gate, a caption, the controls. A gate off means
    ///     "inherit from the level above" - the resting column inherits the macro's own name, and the
    ///     running column inherits the resting column.
    /// </remarks>
    /// <param name="isActive">Which state this column edits.</param>
    /// <returns>The column.</returns>
    private Widget BuildStateColumn(bool isActive)
    {
        var grid = new Grid { ColumnSpacing = COLUMN_SPACING, RowSpacing = ROW_SPACING, ColumnsProportions = { Auto(), Auto(), Auto() } };

        var header = new MyraLabel(
            isActive
                ? TazLang.Get("macrobtneditor_state_active", "Active")
                : TazLang.Get("macrobtneditor_state_inactive", "Inactive / Default"),
            MyraLabel.TextStyle.H5
        );

        int headerRow = NewRow(grid);
        Place(grid, header, headerRow, 0);
        Grid.SetColumnSpan(header, 3);

        AddSetting(grid, BuildLabelRow(isActive));
        AddSetting(grid, BuildHueRow(isActive, HueTarget.Label));
        AddSetting(grid, BuildOpacityRow(isActive));
        AddSetting(grid, BuildHueRow(isActive, HueTarget.Button));
        AddSetting(grid, BuildGraphicRow(isActive));

        return grid;
    }

    /// <summary>Places one setting's gate, caption and controls across a column's three cells.</summary>
    /// <param name="grid">The column.</param>
    /// <param name="setting">The setting to place.</param>
    private static void AddSetting(Grid grid, SettingRow setting)
    {
        int row = NewRow(grid);

        if (setting.Gate != null)
        {
            Place(grid, setting.Gate.CheckBox, row, 0);
            setting.Gate.Bind(setting.Caption);
        }

        Place(grid, setting.Caption, row, 1);
        Place(grid, setting.Controls, row, 2);
    }

    /// <summary>
    ///     The button's text for one state. Unticked inherits - the macro's name on the left, the
    ///     resting text on the right - and an empty box hides the label.
    /// </summary>
    /// <param name="isActive">Which state this row edits.</param>
    /// <returns>The row.</returns>
    private SettingRow BuildLabelRow(bool isActive)
    {
        string tooltip = isActive
            ? TazLang.Get("macrobtneditor_activelabel_tooltip",
                "Text the button shows while the macro is running.\nUnticked it keeps the inactive text; ticked and empty hides it")
            : TazLang.Get("macrobtneditor_inactivelabel_tooltip",
                "Text the button shows at rest.\nUnticked it follows the macro's name; ticked and empty hides it");

        var input = new MyraInputBox { Text = _macro.LabelFor(isActive), Width = LABEL_INPUT_WIDTH, Tooltip = tooltip };

        if (isActive)
            _activeLabelInput = input;

        input.TextChangedByUser += (_, _) =>
        {
            SetLabel(isActive, input.Text ?? string.Empty);
            MirrorInheritedLabel();
            _preview.RefreshSurface();
        };

        // Ticking adopts the text inherited from the level above, which the mirroring keeps this box
        // showing while the gate is off - so the button never takes a label the user has not seen.
        var gate = new GateToggle(CustomLabel(isActive) != null, input, isOn =>
        {
            SetLabel(isActive, isOn ? _macro.LabelFor(isActive) : null);
            input.Text = _macro.LabelFor(isActive);
            MirrorInheritedLabel();
            _preview.RefreshSurface();
        });

        gate.CheckBox.Tooltip = tooltip;

        return new SettingRow(
            gate,
            new MyraLabel(TazLang.Get("macrobtneditor_label", "Custom label"), MyraLabel.TextStyle.P) { Tooltip = tooltip },
            input
        );
    }

    /// <summary>A hue setting for one state: the button's own, or its label's.</summary>
    /// <param name="isActive">Which state this row edits.</param>
    /// <param name="target">Which of the macro's two hues this row edits.</param>
    /// <returns>The row.</returns>
    private SettingRow BuildHueRow(bool isActive, HueTarget target)
    {
        bool isLabel = target == HueTarget.Label;

        string tooltip = TooltipFor(
            isActive,
            isLabel ? "macrobtneditor_labelhue" : "macrobtneditor_buttonhue",
            isLabel
                ? "Hue the button's text is drawn in"
                : "Hue of the button itself"
        );

        var selector = new HueSelector(
            isLabel ? _macro.LabelHueFor(isActive) : _macro.HueFor(isActive),
            inputWidth: HUE_INPUT_WIDTH
        );

        // Only the button's hue is baked into the preview's graphic; the label's is drawn straight, so
        // it takes the path that does not re-bake. Both of these fire per keystroke in the index field.
        Action refresh = isLabel ? _preview.RefreshSurface : _preview.Refresh;

        selector.HueChanged += (_, hue) =>
        {
            SetHue(target, isActive, hue);
            refresh();
        };

        MyraLabel caption = Caption(
            isLabel ? "macrobtneditor_labelhue" : "macrobtneditor_buttonhue",
            isLabel ? "Label hue" : "Button hue",
            tooltip
        );

        if (!isActive)
            return new SettingRow(null, caption, selector);

        if (isLabel)
            _activeLabelHueSelector = selector;
        else
            _activeHueSelector = selector;

        // Ticking adopts what the selector already shows - the inherited hue - so the button never
        // jumps to a colour the user has not seen.
        var gate = new GateToggle(HasHueOverride(target), selector, isOn =>
        {
            SetActiveHue(target, isOn ? selector.Hue : null);
            refresh();
        });

        gate.CheckBox.Tooltip = tooltip;

        return new SettingRow(gate, caption, selector);
    }

    /// <summary>The label's opacity for one state.</summary>
    /// <param name="isActive">Which state this row edits.</param>
    /// <returns>The row.</returns>
    private SettingRow BuildOpacityRow(bool isActive)
    {
        string tooltip = TooltipFor(isActive, "macrobtneditor_labelopacity", "How solid the button's text is drawn");

        var slider = LabeledHorizontalSlider.CreateSliderWithCallback(
            MIN_OPACITY,
            Macro.FULL_OPACITY,
            _macro.LabelOpacityFor(isActive),
            value =>
            {
                if (isActive)
                    _macro.ActiveLabelOpacity = (byte)value;
                else
                {
                    _macro.LabelOpacity = (byte)value;

                    // Null only while the resting column is still being built, which is before any of
                    // this can fire.
                    if (!_macro.ActiveLabelOpacity.HasValue && _activeOpacitySlider != null)
                        _activeOpacitySlider.Value = value;
                }

                // Opacity is applied to the label as it is drawn, so the graphic's bake still stands -
                // and this fires on every pixel of a slider drag.
                _preview.RefreshSurface();
            }
        );

        slider.Width = OPACITY_SLIDER_WIDTH;

        MyraLabel caption = Caption("macrobtneditor_labelopacity", "Label opacity", tooltip);

        if (!isActive)
            return new SettingRow(null, caption, slider);

        _activeOpacitySlider = slider;

        var gate = new GateToggle(_macro.ActiveLabelOpacity.HasValue, slider, isOn =>
        {
            _macro.ActiveLabelOpacity = isOn ? (byte)slider.Value : null;
            _preview.RefreshSurface();
        });

        gate.CheckBox.Tooltip = tooltip;

        return new SettingRow(gate, caption, slider);
    }

    /// <summary>
    ///     Builds a row's tooltip, appending what unticking its gate falls back to on the running side.
    /// </summary>
    /// <param name="isActive">Which state the row edits.</param>
    /// <param name="key">Base localization key; the running side reads its "_active" sibling.</param>
    /// <param name="fallback">Text to use when the key is missing.</param>
    /// <returns>The tooltip.</returns>
    private static string TooltipFor(bool isActive, string key, string fallback) =>
        isActive
            ? TazLang.Get($"{key}_activetooltip", $"{fallback} while the macro is running.\nUnticked, it keeps the inactive value")
            : TazLang.Get($"{key}_tooltip", $"{fallback} at rest");

    /// <summary>Writes one of the macro's hues.</summary>
    /// <param name="target">Which hue to write.</param>
    /// <param name="isActive">Which state to write it for.</param>
    /// <param name="hue">The hue.</param>
    private void SetHue(HueTarget target, bool isActive, ushort hue)
    {
        if (isActive)
        {
            SetActiveHue(target, hue);

            return;
        }

        if (target == HueTarget.Label)
            _macro.LabelHue = hue;
        else
            _macro.Hue = hue;

        // Mirrored rather than left stale: a disabled selector still reads as the active hue, and
        // showing a value the button no longer uses is what the gate exists to clarify. Null only
        // while the resting column is still being built, which is before any of this can fire.
        if (!HasHueOverride(target) && ActiveSelector(target) is { } active)
            active.Hue = hue;
    }

    /// <summary>Writes one of the macro's running-state hues, or clears it back to inheriting.</summary>
    /// <param name="target">Which hue to write.</param>
    /// <param name="hue">The hue, or null to inherit.</param>
    private void SetActiveHue(HueTarget target, ushort? hue)
    {
        if (target == HueTarget.Label)
            _macro.ActiveLabelHue = hue;
        else
            _macro.ActiveHue = hue;
    }

    /// <summary>Whether a hue is overridden for the running state rather than inherited.</summary>
    /// <param name="target">Which hue to test.</param>
    /// <returns>True when the running state has its own value.</returns>
    private bool HasHueOverride(HueTarget target) =>
        target == HueTarget.Label ? _macro.ActiveLabelHue.HasValue : _macro.ActiveHue.HasValue;

    /// <summary>The running state's selector for a hue, held so the resting side can keep it in step.</summary>
    /// <param name="target">Which hue's selector.</param>
    /// <returns>The selector.</returns>
    private HueSelector? ActiveSelector(HueTarget target) =>
        target == HueTarget.Label ? _activeLabelHueSelector : _activeHueSelector;

    /// <summary>The button's gump graphic for one state.</summary>
    /// <remarks>
    ///     The active picker's Default entry means "draw nothing while running", which is not what its
    ///     gate means; see <see cref="Macro.ACTIVE_GRAPHIC_NONE" />.
    /// </remarks>
    /// <param name="isActive">Which state this row edits.</param>
    /// <returns>The row.</returns>
    private SettingRow BuildGraphicRow(bool isActive)
    {
        string tooltip = isActive
            ? TazLang.Get("macrobtneditor_activegraphic_gatetooltip",
                "Gump graphic shown while the macro is running.\nUnticked, it keeps the inactive graphic")
            : TazLang.Get("macrobtneditor_inactivegraphic_gatetooltip",
                "Gump graphic of the button at rest. Default draws no graphic");

        GumpGraphicPicker picker = BuildGraphicPicker(_macro.GraphicFor(isActive), tooltip);

        if (!isActive)
        {
            picker.GraphicChanged += (_, graphic) =>
            {
                _macro.Graphic = graphic;

                if (!_macro.ActiveGraphic.HasValue)
                    MirrorToActiveGraphic(graphic);

                _preview.Refresh();
            };

            return new SettingRow(null, Caption("macrobtneditor_graphic", "Graphic", tooltip), picker);
        }

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

        var gate = new GateToggle(_macro.ActiveGraphic.HasValue, picker, isOn =>
        {
            _macro.ActiveGraphic = isOn ? ToActiveGraphic(picker.Graphic) : null;
            _preview.Refresh();
        });

        gate.CheckBox.Tooltip = tooltip;

        return new SettingRow(gate, Caption("macrobtneditor_graphic", "Graphic", tooltip), picker);
    }

    /// <summary>The custom label a state has been given, or null where it inherits.</summary>
    /// <param name="isActive">Which state to read.</param>
    /// <returns>The stored text, or null.</returns>
    private string? CustomLabel(bool isActive) => isActive ? _macro.ActiveLabel : _macro.Label;

    /// <summary>
    ///     Shows the resting label in the running state's box while that state inherits it.
    /// </summary>
    /// <remarks>
    ///     A disabled box still reads as the running label, so leaving it stale would both misreport the
    ///     button and give its gate the wrong text to adopt. Programmatic, so it raises no
    ///     <c>TextChangedByUser</c> and cannot be mistaken for an edit.
    /// </remarks>
    private void MirrorInheritedLabel()
    {
        if (_macro.ActiveLabel == null && _activeLabelInput != null)
            _activeLabelInput.Text = _macro.LabelFor(true);
    }

    /// <summary>Stores one state's label text.</summary>
    /// <param name="isActive">Which state to write.</param>
    /// <param name="text">The text, empty to hide the label, or null to go back to inheriting.</param>
    private void SetLabel(bool isActive, string? text)
    {
        if (isActive)
            _macro.ActiveLabel = text;
        else
            _macro.Label = text;
    }

    /// <summary>Builds a row caption.</summary>
    /// <param name="key">Localization key.</param>
    /// <param name="fallback">Text to use when the key is missing.</param>
    /// <param name="tooltip">Tooltip shared with the row's controls.</param>
    /// <returns>The caption.</returns>
    private static MyraLabel Caption(string key, string fallback, string tooltip) =>
        new(TazLang.Get(key, fallback), MyraLabel.TextStyle.P) { Tooltip = tooltip };

    /// <summary>Appends an auto-sized row to a grid.</summary>
    /// <param name="grid">The grid to extend.</param>
    /// <returns>The new row's index.</returns>
    private static int NewRow(Grid grid)
    {
        grid.RowsProportions.Add(Auto());

        return grid.RowsProportions.Count - 1;
    }

    /// <summary>A row or column sized to whatever it holds, which is every one of them here.</summary>
    /// <returns>The proportion.</returns>
    private static Proportion Auto() => new(ProportionType.Auto);

    /// <summary>Puts a widget in one grid cell, centred so mixed-height rows sit on a common line.</summary>
    /// <param name="grid">The grid.</param>
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

    /// <summary>Builds a gump-graphic picker sized for a state column.</summary>
    /// <param name="graphic">The graphic to start on.</param>
    /// <param name="tooltip">Tooltip for the number field.</param>
    /// <returns>The picker.</returns>
    private static GumpGraphicPicker BuildGraphicPicker(ushort? graphic, string tooltip) =>
        new(graphic)
        {
            NumberInput = { Width = GRAPHIC_NUMBER_WIDTH, HintText = TazLang.Get("macrobtneditor_graphic_hint", "Graphic # or 0x.."), Tooltip = tooltip },
            NameList = { Width = GRAPHIC_LIST_WIDTH, SearchHintText = TazLang.Get("macrobtneditor_graphic_searchhint", "Search for a gump..") }
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

    /// <summary>
    ///     Builds the save/close row. Both leave; the pair differ in what they do with the edits, so
    ///     neither is a cancel of the other.
    /// </summary>
    /// <returns>The row, right-aligned.</returns>
    private HorizontalStackPanel BuildButtonRow()
    {
        var row = new HorizontalStackPanel { Spacing = MyraStyle.STANDARD_SPACING, HorizontalAlignment = HorizontalAlignment.Right };

        row.Widgets.Add(new MyraButton(TazLang.Get("macrobtneditor_reset", "Reset"), Reset)
        {
            Tooltip = TazLang.Get("macrobtneditor_reset_tooltip", "Put every setting back to its default.\nClose still discards it, like any other change")
        });

        row.Widgets.Add(new MyraButton(TazLang.Get("macrobtneditor_save", "Save"), Save)
        {
            Tooltip = TazLang.Get("macrobtneditor_save_closetooltip", "Keep these changes, write them to disk and close")
        });

        // Dispose() rather than the base's flag, so the revert runs now instead of next frame.
        row.Widgets.Add(new MyraButton(TazLang.Get("uicommons_close", "Close"), Dispose)
        {
            Tooltip = TazLang.Get("macrobtneditor_close_tooltip", "Discard any changes made since the last save")
        });

        return row;
    }

    /// <summary>
    ///     Commits the current appearance, persists the macro list, re-reads the macro into any button
    ///     already on screen - which caches the appearance rather than reading it per frame - and closes.
    /// </summary>
    /// <remarks>
    ///     Closing is the confirmation. Left open, saving looked like nothing had happened: the preview
    ///     already showed the edits, and the button on screen is only reached through the refresh below.
    ///     Disposing is safe straight after committing, since the revert it runs finds nothing to undo.
    /// </remarks>
    private void Save()
    {
        _committed = MacroButtonAppearance.Capture(_macro);

        World.Instance.Macros.Save();
        RefreshLiveButtons();
        Dispose();
    }

    /// <summary>
    ///     Puts the button back to how an untouched macro looks. An edit like any other: it is not
    ///     written to disk until <see cref="Save" />, and <see cref="Revert" /> undoes it.
    /// </summary>
    private void Reset()
    {
        MacroButtonAppearance.Defaults.ApplyTo(_macro);
        RebuildContent();
    }

    /// <summary>
    ///     Throws the window's controls away and builds them again from the macro.
    /// </summary>
    /// <remarks>
    ///     Cheaper than a setter per control, and the only way the gates' dimming and the inherited
    ///     values the pickers show stay consistent after the macro changes wholesale. The cached
    ///     running-state widgets are dropped first; <see cref="Build" /> hands back new ones.
    /// </remarks>
    private void RebuildContent()
    {
        _activeHueSelector = null;
        _activeLabelHueSelector = null;
        _activeOpacitySlider = null;
        _activeLabelInput = null;

        CreatePreview();
        Build();
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

    /// <summary>Which of a macro's two hues a row edits.</summary>
    private enum HueTarget
    {
        /// <summary>The plate and graphic.</summary>
        Button,

        /// <summary>The text drawn over them.</summary>
        Label
    }

    /// <summary>One setting's three cells, before they are placed into a column.</summary>
    /// <param name="Gate">The override toggle, or null for a setting that is always in force.</param>
    /// <param name="Caption">The row's caption.</param>
    /// <param name="Controls">The setting's controls.</param>
    private sealed record SettingRow(GateToggle? Gate, MyraLabel Caption, Widget Controls);

    /// <summary>A rule across the window, separating one band of settings from the next.</summary>
    private sealed class HorizontalSeparator : Panel
    {
        public HorizontalSeparator()
        {
            Height = 1;
            HorizontalAlignment = HorizontalAlignment.Stretch;
            Background = new SolidBrush(MyraStyle.GridBorderColor);
        }
    }


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
        string? Label,
        string? ActiveLabel,
        ushort LabelHue,
        ushort? ActiveLabelHue,
        byte LabelOpacity,
        byte? ActiveLabelOpacity,
        byte Scale,
        ushort Hue,
        ushort? ActiveHue,
        ushort? Graphic,
        int? ActiveGraphic
    )
    {
        /// <summary>
        ///     How a macro that has never been through this editor looks: its own name as the label, at
        ///     the default hue and full opacity, over a bare plate, with no separate running appearance.
        /// </summary>
        public static readonly MacroButtonAppearance Defaults = new(
            null,
            null,
            Macro.DEFAULT_LABEL_HUE,
            null,
            Macro.FULL_OPACITY,
            null,
            DEFAULT_SCALE,
            0,
            null,
            null,
            null
        );

        /// <summary>Takes a snapshot of a macro's current button appearance.</summary>
        /// <param name="macro">The macro to read. Not retained.</param>
        /// <returns>The snapshot.</returns>
        public static MacroButtonAppearance Capture(Macro macro) => new(
            macro.Label,
            macro.ActiveLabel,
            macro.LabelHue,
            macro.ActiveLabelHue,
            macro.LabelOpacity,
            macro.ActiveLabelOpacity,
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
            macro.Label = Label;
            macro.ActiveLabel = ActiveLabel;
            macro.LabelHue = LabelHue;
            macro.ActiveLabelHue = ActiveLabelHue;
            macro.LabelOpacity = LabelOpacity;
            macro.ActiveLabelOpacity = ActiveLabelOpacity;
            macro.Scale = Scale;
            macro.Hue = Hue;
            macro.ActiveHue = ActiveHue;
            macro.Graphic = Graphic;
            macro.ActiveGraphic = ActiveGraphic;
        }
    }

    #endregion
}
