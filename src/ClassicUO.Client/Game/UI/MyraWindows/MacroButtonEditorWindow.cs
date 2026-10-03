#nullable enable
using System;
using System.Linq;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.Game.UI.MyraWindows.Widgets.ArtTexture;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows;

/// <summary>
///     Editor for how a macro's standalone button looks on screen — its label, scale, hue and gump
///     graphic — with a live preview of the result.
/// </summary>
/// <remarks>
///     Edits are written straight onto the macro so the preview can show them, but only reach disk when
///     the user saves. Open through <see cref="Show" />, which keeps a single editor across call sites.
/// </remarks>
public sealed class MacroButtonEditorWindow : MyraControl
{
    #region Private members

    /// <summary>Hue swatch graphic. The dye tub every hue picker in the UI uses.</summary>
    private const ushort SWATCH_GRAPHIC = 0x0FAB;

    private const int SWATCH_SIZE = 20;

    /// <summary>Scale bounds, in percent, matching what the macro itself accepts.</summary>
    private const int MIN_SCALE = 10;
    private const int MAX_SCALE = 200;

    /// <summary>Keeps the preview box from resizing under the controls as the button grows and shrinks.</summary>
    private const int PREVIEW_MIN_HEIGHT = 120;

    /// <summary>Widths of the graphic picker's two halves: the number field and the searchable list.</summary>
    private const int GRAPHIC_NUMBER_WIDTH = 110;
    private const int GRAPHIC_LIST_WIDTH = 180;

    private const int SLIDER_WIDTH = 180;

    /// <summary>Indent of the setting rows under their section heading.</summary>
    private const int ROW_INDENT = 20;

    private readonly Macro _macro;
    private readonly MacroButtonPreview _preview;

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
        _preview = new MacroButtonPreview(macro);
        _preview.ShowActiveStateChanged += (_, _) => SyncPreviewStateLabel();
        SyncPreviewStateLabel();

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
    public override void Dispose()
    {
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
            Padding = new Thickness(ROW_INDENT, 0, 0, 0)
        };

        rows.Widgets.Add(BuildHideLabelToggle());
        rows.Widgets.Add(BuildScaleSlider());
        rows.Widgets.Add(BuildInactiveHueRow());
        rows.Widgets.Add(BuildActiveHueRow());
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
        LabeledHorizontalSlider slider = LabeledHorizontalSlider.CreateSliderWithCallback(
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

    private Widget BuildInactiveHueRow() =>
        BuildHueRow(
            TazLang.Get("macrobtneditor_inactivehue", "Inactive hue"),
            TazLang.Get("macrobtneditor_inactivehue_tooltip", "Hue of the button at rest."),
            () => _macro.Hue,
            hue => _macro.Hue = hue,
            inherit: null
        );

    /// <remarks>
    ///     Carries a reset, which the resting row does not need: this hue is nullable, and clicking a
    ///     swatch can only ever set a value, never clear one back to "inherit the resting hue".
    /// </remarks>
    private Widget BuildActiveHueRow() =>
        BuildHueRow(
            TazLang.Get("macrobtneditor_activehue", "Active hue"),
            TazLang.Get("macrobtneditor_activehue_tooltip",
                "Hue of the button while the macro is running."),
            () => _macro.HueFor(true),
            hue => _macro.ActiveHue = hue,
            inherit: () => _macro.ActiveHue = null
        );

    /// <summary>
    ///     Swatch that opens the shared color picker, mirroring the hue rows in the options panels.
    /// </summary>
    /// <param name="read">Reads the hue to display; re-read after <paramref name="inherit" /> runs.</param>
    /// <param name="write">Commits a picked hue.</param>
    /// <param name="inherit">
    ///     Clears the field back to following another hue, or null for a field that is never unset.
    /// </param>
    private Widget BuildHueRow(
        string label,
        string tooltip,
        Func<ushort> read,
        Action<ushort> write,
        Action? inherit
    )
    {
        var swatch = new MyraArtTexture(SWATCH_GRAPHIC, read(), SWATCH_SIZE)
        {
            Tooltip = HueTooltip(tooltip, read())
        };

        swatch.TouchUp += (_, _) =>
        {
            if (!swatch.Enabled)
                return;

            UIManager.GetGump<ModernColorPicker>()?.Dispose();
            UIManager.Add(new ModernColorPicker(World.Instance, hue =>
            {
                write(hue);
                swatch.SetColorByHue(hue);
                swatch.Tooltip = HueTooltip(tooltip, hue);
                _preview.Refresh();
            }, isClickable: true));
        };

        if (inherit == null)
            return Row(label, swatch, tooltip);

        var controls = new HorizontalStackPanel
        {
            Spacing = MyraStyle.STANDARD_SPACING,
            VerticalAlignment = VerticalAlignment.Center
        };

        controls.Widgets.Add(swatch);
        controls.Widgets.Add(new MyraButton(TazLang.Get("macrobtneditor_hue_inherit", "Same"), () =>
        {
            inherit();
            swatch.SetColorByHue(read());
            swatch.Tooltip = HueTooltip(tooltip, read());
            _preview.Refresh();
        })
        {
            Tooltip = TazLang.Get("macrobtneditor_hue_inherit_tooltip", "Use the inactive hue.")
        });

        return Row(label, controls, tooltip);
    }

    private static string HueTooltip(string tooltip, ushort hue) =>
        $"{tooltip}\n{TazLang.GetEx("macrobtneditor_hue_current", "Current hue: {0}", [hue.ToString()])}";

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

        row.Widgets.Add(new MyraButton(TazLang.Get("macrobtneditor_save", "Save"), Save));
        row.Widgets.Add(new MyraButton(TazLang.Get("uicommons_close", "Close"), () => _disposeRequested = true));

        return row;
    }

    /// <summary>
    ///     Persists the macro list and re-reads the macro into any button already on screen, which caches
    ///     the appearance fields rather than reading them per frame.
    /// </summary>
    private void Save()
    {
        World.Instance.Macros.Save();

        foreach (MacroButtonGump button in UIManager.Gumps.OfType<MacroButtonGump>().ToList())
        {
            if (button.TheMacro == _macro)
                button.TheMacro = _macro;
        }
    }

    #endregion
}
