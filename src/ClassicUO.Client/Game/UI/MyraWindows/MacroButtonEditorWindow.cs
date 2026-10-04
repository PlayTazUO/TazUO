#nullable enable
using System;
using System.Linq;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
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
    private const int GRAPHIC_LIST_WIDTH = 180;

    private const int SLIDER_WIDTH = 180;

    /// <summary>Inset of the setting rows within their section, kept equal on both sides so the
    /// right-pinned controls do not sit flush against the border.</summary>
    private const int ROW_INSET = 20;

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

    /// <summary>Names which state the preview is showing; kept in step with it, clicks included.</summary>
    private readonly MyraLabel _previewStateLabel = new(string.Empty, MyraLabel.TextStyle.P)
    {
        HorizontalAlignment = HorizontalAlignment.Center
    };

    #endregion

    #region Ctor

    private MacroButtonEditorWindow(Macro macro)
        : base(TazLang.Get("macrobtneditor_title", "Macro Button Editor"))
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
        {
            if (gump is MacroButtonEditorWindow existing && !existing.IsDisposed)
                existing.Dispose();
        }

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

    private void Build()
    {
        var root = new VerticalStackPanel { Spacing = MyraStyle.STANDARD_SPACING, MinWidth = 350 };

        root.Widgets.Add(new MyraLabel(
            TazLang.GetEx("macrobtneditor_header", "Editor for {0}", [_macro.Name]),
            MyraLabel.TextStyle.H3));

        root.Widgets.Add(BuildAppearanceSection());
        root.Widgets.Add(BuildPreviewSection());
        root.Widgets.Add(BuildButtonRow());

        SetRootContent(root);
    }

    /// <remarks>
    ///     A StackPanel, not a WrapPanel: the rows are a fixed vertical sequence, and they stretch so
    ///     every control lines up on the right edge. A vertical WrapPanel would answer an over-tall
    ///     child by starting a second column instead.
    /// </remarks>
    private VisualContainer BuildAppearanceSection()
    {
        var rows = new VerticalStackPanel
        {
            Spacing = MyraStyle.STANDARD_SPACING,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Padding = new Thickness(ROW_INSET, 0, ROW_INSET, 0)
        };

        // Built before the inactive row, whose handler reaches for this one's selector.
        Widget activeHue = BuildActiveHueRow();

        rows.Widgets.Add(BuildHideLabelToggle());
        rows.Widgets.Add(BuildScaleSlider());
        rows.Widgets.Add(BuildInactiveHueRow());
        rows.Widgets.Add(activeHue);
        rows.Widgets.Add(BuildInactiveGraphicPicker());
        rows.Widgets.Add(BuildActiveGraphicPicker());

        return new VisualContainer(
            new VisualContainerProps { LabelText = TazLang.Get("macrobtneditor_appearance", "Appearance") },
            rows
        )
        {
            Margin = new Thickness(0, 10, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
    }

    private MyraCheckButton BuildHideLabelToggle() =>
        MyraCheckButton.CreateWithCallback(
            _macro.HideLabel,
            hidden =>
            {
                _macro.HideLabel = hidden;
                _preview.Refresh();
            },
            TazLang.Get("macrobtneditor_hidelabel", "Hide Label"),
            TazLang.Get("macrobtneditor_hidelabel_tooltip", "Hide the macro's name on the button.")
        );

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

        return Row(TazLang.Get("macrobtneditor_scale", "Scale"), slider, tooltip: null);
    }

    /// <summary>Pairs a row label with its control, the control pinned to the section's right edge.</summary>
    private static Widget Row(string label, Widget control, string? tooltip) =>
        new SpaceBetweenRow(
            new MyraLabel(label, MyraLabel.TextStyle.P) { Tooltip = tooltip },
            control
        );

    private Widget BuildInactiveHueRow()
    {
        var selector = new HueSelector(_macro.Hue);

        selector.HueChanged += (_, hue) =>
        {
            _macro.Hue = hue;

            // Mirrored rather than left stale: a disabled selector still reads as the active hue, and
            // showing the old value there is exactly the ambiguity the radio exists to remove.
            if (!_macro.ActiveHue.HasValue)
                _activeHueSelector.Hue = hue;

            _preview.Refresh();
        };

        return Row(
            TazLang.Get("macrobtneditor_inactivehue", "Inactive hue"),
            selector,
            TazLang.Get("macrobtneditor_inactivehue_tooltip", "Hue of the button at rest.")
        );
    }

    /// <summary>
    ///     Active hue as an explicit choice between following the inactive hue and setting its own.
    /// </summary>
    /// <remarks>
    ///     A bare swatch could not express this field: null means "follow the inactive hue", and a
    ///     swatch showing the followed value is indistinguishable from one deliberately set to it -
    ///     worse still at hue 0, where "unset" and "no hue" look identical. The radio says which it is,
    ///     and clicking a swatch can only ever set a value, never clear one back to following.
    /// </remarks>
    private Widget BuildActiveHueRow()
    {
        var selector = new HueSelector(_macro.HueFor(true));
        _activeHueSelector = selector;

        selector.HueChanged += (_, hue) =>
        {
            _macro.ActiveHue = hue;
            _preview.Refresh();
        };

        var group = new RadioGroup<bool>(
            TazLang.Get("macrobtneditor_activehue", "Active hue"),
            _macro.ActiveHue.HasValue,
            new RadioOption<bool>(
                false,
                TazLang.Get("macrobtneditor_activehue_inherit", "Same as inactive"),
                TazLang.Get("macrobtneditor_activehue_inherit_tooltip",
                    "The button keeps its inactive hue while the macro runs.")
            ),
            new RadioOption<bool>(
                true,
                TazLang.Get("macrobtneditor_activehue_custom", "Custom"),
                TazLang.Get("macrobtneditor_activehue_custom_tooltip",
                    "Give the button its own hue while the macro runs."),
                Row(TazLang.Get("macrobtneditor_activehue_hue", "Hue"), selector, tooltip: null)
            )
        );

        group.SelectionChanged += (_, custom) =>
        {
            // The selector keeps showing the followed hue while on "same as inactive", so switching to
            // Custom adopts what is already on screen rather than snapping to something unseen.
            _macro.ActiveHue = custom ? selector.Hue : null;
            _preview.Refresh();
        };

        return group;
    }

    /// <summary>
    ///     Gump graphic of the button at rest. "(None)" stores a null graphic, which the button draws
    ///     as a bare plate.
    /// </summary>
    private Widget BuildInactiveGraphicPicker() =>
        BuildGraphicRow(
            TazLang.Get("macrobtneditor_inactivegraphic", "Inactive graphic"),
            _macro.Graphic,
            TazLang.Get("macrobtneditor_inactivegraphic_tooltip",
                "Gump graphic of the button at rest. Select (None) for no graphic."),
            graphic =>
            {
                _macro.Graphic = graphic;
                _preview.Refresh();
            },
            noneLabel: null
        );

    /// <summary>
    ///     Gump graphic shown while the macro runs. "(None)" leaves it matching the resting graphic, so
    ///     the button does not change while running.
    /// </summary>
    private Widget BuildActiveGraphicPicker() =>
        BuildGraphicRow(
            TazLang.Get("macrobtneditor_activegraphic", "Active graphic"),
            _macro.ActiveGraphic,
            TazLang.Get("macrobtneditor_activegraphic_tooltip",
                "Gump graphic shown while the macro is running.\nSelect (Same as inactive) to keep the inactive graphic."),
            graphic =>
            {
                _macro.ActiveGraphic = graphic;
                _preview.Refresh();
            },
            TazLang.Get("macrobtneditor_activegraphic_same", "(Same as inactive)")
        );

    /// <param name="noneLabel">What this field's null entry is called; null for the picker's default.</param>
    private Widget BuildGraphicRow(
        string label,
        ushort? graphic,
        string tooltip,
        Action<ushort?> onChanged,
        string? noneLabel
    )
    {
        var picker = new GumpGraphicPicker(graphic, noneLabel)
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

        picker.GraphicChanged += (_, value) => onChanged(value);

        return Row(label, picker, tooltip);
    }

    private Widget BuildPreviewSection() =>
        new VisualContainer(
            new VisualContainerProps
            {
                LabelText = TazLang.Get("macrobtneditor_preview", "Preview"),
                LabelHorizontalAlignment = HorizontalAlignment.Center
            },
            new Panel
            {
                MinHeight = PREVIEW_MIN_HEIGHT,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Widgets = { _preview }
            },
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

    private Widget BuildButtonRow()
    {
        var row = new HorizontalStackPanel
        {
            Spacing = MyraStyle.STANDARD_SPACING,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        row.Widgets.Add(new MyraButton(TazLang.Get("macrobtneditor_save", "Save"), Save)
        {
            Tooltip = TazLang.Get("macrobtneditor_save_tooltip", "Keep these changes and write them to disk.")
        });

        // Dispose() rather than the base's flag, so the revert runs.
        row.Widgets.Add(new MyraButton(TazLang.Get("uicommons_close", "Close"), Dispose)
        {
            Tooltip = TazLang.Get("macrobtneditor_close_tooltip", "Discard any changes made since the last save.")
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
        {
            if (button.TheMacro == _macro)
                button.TheMacro = _macro;
        }
    }

    #endregion

    #region Nested types

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
        ushort? ActiveGraphic
    )
    {
        public static MacroButtonAppearance Capture(Macro macro) => new(
            macro.HideLabel,
            macro.Scale,
            macro.Hue,
            macro.ActiveHue,
            macro.Graphic,
            macro.ActiveGraphic
        );

        public bool Matches(Macro macro) => this == Capture(macro);

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
