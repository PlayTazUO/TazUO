#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Configuration;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Effects;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Profiles;
using ClassicUO.Game.ScreenDecorations.Manager;
using ClassicUO.Game.ScreenDecorations.Overlays;
using ClassicUO.Game.UI.MyraWindows.Options.Tabs;
using ClassicUO.Game.UI.MyraWindows.Theme;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.Renderer.Effects;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.WrapPanel;

namespace ClassicUO.Game.UI.MyraWindows.Options.Editors.Overlays;

/// <summary>
/// Composes one <see cref="EffectProfile"/>: its scope, fade timing and shake, and its layer stack
/// one layer at a time - each through a <see cref="StyledPropertyGrid"/> over the concrete
/// <see cref="LayerEffect"/>.
/// <para>
/// Reflecting over the effect rather than hand-listing its parameters is deliberate: the shader has
/// around thirty five of them and they change as it is tuned. Because the effect types are narrow,
/// the grid shows exactly the knobs the chosen technique reads - there is no radius on a chromatic
/// layer to mis-set or to explain away.
/// </para>
/// </summary>
internal sealed class OverlayProfileEditor : Widget
{
    #region Private members

    private const int INPUT_WIDTH = 80;
    private const int NAME_INPUT_WIDTH = 160;
    private const float MAX_FADE_SECONDS = 10f;

    /// <summary>Padlock, U+1F512. Present in Noto Sans Symbols 2, absent from the body font.</summary>
    private const string READ_ONLY_GLYPH = "🔒";

    private const int BANNER_GLYPH_SIZE = 26;

    /// <summary>Gap under the shake group's caption, so it sits with the group rather than reading
    /// as the first of the fields it qualifies.</summary>
    private const int NOTE_GAP = 8;

    /// <summary>
    /// Reset targets, one per technique. Built on demand and kept, because every lookup reads one
    /// through reflection and a technique's defaults never change within a session.
    /// </summary>
    private static readonly Dictionary<Type, LayerEffect> _defaultsByTechnique = [];

    private readonly EffectProfile _profile;
    private readonly Action _onChanged;

    /// <summary>Built-in profiles are shown so they can be read and copied, never edited.</summary>
    private readonly bool _readOnly;

    /// <summary>Input reused across rebuilds, so the caret survives one. Mirrors the profile
    /// editor's own rename box.</summary>
    private readonly MyraInputBox _renameInput = new() { Width = NAME_INPUT_WIDTH };

    private int _selectedLayer;

    /// <summary>Whether the layer toolbar is showing its rename controls instead of the picker.</summary>
    private bool _isRenamingLayer;

    #endregion

    #region Ctor

    /// <summary>
    /// Builds the composer for one profile.
    /// </summary>
    /// <param name="profile">The profile to edit.</param>
    /// <param name="onChanged">Invoked after every change that should be persisted.</param>
    /// <exception cref="ArgumentNullException">Either argument is null.</exception>
    public OverlayProfileEditor(EffectProfile profile, Action onChanged)
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

    #endregion

    #region Private methods

    private void Rebuild()
    {
        _selectedLayer = Math.Clamp(_selectedLayer, 0, Math.Max(_profile.Layers.Count - 1, 0));

        Children.Clear();

        if (_readOnly)
            Children.Add(BuildReadOnlyBanner());

        // The switches and timings that describe the whole look, on one wrapping line. Three
        // one-control rows stacked read as a list of unrelated settings; together they read as the
        // header they are, and the block below is what the eye drops to.
        Children.Add(BuildHeaderRow());

        Children.Add(OptionTabCommons.StyledHorizontalSeparator());
        Children.Add(BuildShakeRow());
        Children.Add(OptionTabCommons.StyledHorizontalSeparator());
        Children.Add(BuildLayerToolbar());
        Children.Add(BuildLayerIdentityRow());
        Children.Add(OptionTabCommons.StyledHorizontalSeparator());
        Children.Add(BuildLayerGrid());
    }

    /// <summary>
    /// Says outright that nothing below can be changed. Myra greys a disabled widget only where its
    /// style defines a disabled brush, so several of these controls look editable until clicked -
    /// the banner is what makes the state legible regardless.
    /// </summary>
    private static Widget BuildReadOnlyBanner()
    {
        MyraPalette palette = MyraTheme.Current;

        var banner = new HorizontalStackPanel
        {
            Spacing = MyraStyle.STANDARD_SPACING,
            Padding = new Thickness(8, 6),
            Background = new SolidBrush(palette.PanelFill),

            // The banner's border is the notice tint held well back, so it frames without shouting.
            Border = new SolidBrush(palette.Notice * palette.NoticeBorderAlpha),
            BorderThickness = new Thickness(1),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center
        };

        banner.Widgets.Add(MyraLabel.Symbol(READ_ONLY_GLYPH, BANNER_GLYPH_SIZE, palette.Notice));

        banner.Widgets.Add(
            new MyraLabel(
                TazLang.Get(
                    "visualeffects_builtinreadonly",
                    "Built-in effect - read only. Copy the profile to edit."
                ),
                MyraLabel.TextStyle.P
            )
            {
                TextColor = palette.Notice,
                VerticalAlignment = VerticalAlignment.Center
            }
        );

        return banner;
    }

    /// <summary>
    /// What the look is, before what it draws: whether it can be seen right now, what it covers, and
    /// how it arrives and leaves.
    /// <para>
    /// Preview is a session setting and the rest belong to the profile, so on a shipped look the
    /// preview switch is the only one of them that still works. <see cref="Editable"/> carries that
    /// per control, alongside the banner at the top of the panel - the row does not have to.
    /// </para>
    /// </summary>
    private WrapPanel BuildHeaderRow() =>
        Row(
            PreviewToggle(),
            ScopeToggle(),
            Editable(
                LabelledFloat(
                    TazLang.Get("visualeffects_fadein", "Fade in (s)"),
                    _profile.Fade.InSeconds,
                    MAX_FADE_SECONDS,
                    value => _profile.Fade.InSeconds = value
                )
            ),
            Editable(
                LabelledFloat(
                    TazLang.Get("visualeffects_fadeout", "Fade out (s)"),
                    _profile.Fade.OutSeconds,
                    MAX_FADE_SECONDS,
                    value => _profile.Fade.OutSeconds = value
                )
            )
        );

    /// <summary>
    /// The switch that shows the look on demand. Preview ignores every rule and the player's state:
    /// the usual reason to look at an effect is to decide whether to wire one up at all.
    /// </summary>
    private Widget PreviewToggle()
    {
        var preview = MyraCheckButton.CreateWithCallback(
            ScreenOverlayManager.Instance.IsPreviewing(_profile.Id),
            on => ScreenOverlayManager.Instance.SetPreview(_profile.Id, on)
        );

        preview.Tooltip = TazLang.Get(
            "visualeffects_preview_tooltip",
            "Shows this effect regardless of your character's state,\n"
            + "shake included. Still subject to the switches on the General tab,\n"
            + "and ends when the options are closed."
        );

        return Labelled(TazLang.Get("visualeffects_preview", "Preview this effect"), preview);
    }

    /// <summary>What the look covers. Stored on the profile, so read-only on a shipped one.</summary>
    private Widget ScopeToggle()
    {
        var fullScreen = MyraCheckButton.CreateWithCallback(
            _profile.FullScreen,
            on =>
            {
                _profile.FullScreen = on;
                _onChanged();
            }
        );

        fullScreen.Tooltip = TazLang.Get(
            "visualeffects_fullscreen_tooltip",
            "Covers the gumps and cursor as well as the world,\n"
            + "and shakes the whole window rather than only the viewport."
        );

        // The label goes with it. Disabling the box alone leaves its caption at full strength, which
        // is what made these read as editable.
        return Editable(
            Labelled(TazLang.Get("visualeffects_fullscreen", "Draw over the whole window"), fullScreen)
        );
    }

    /// <summary>
    /// Shake belongs to the look rather than to the trigger: how hard a thing hits is part of what
    /// it feels like, and the occurrence's own strength scales it.
    /// <para>
    /// The fields are always present and merely disabled while the switch is off. Hiding them would
    /// reflow every row beneath on each click, and a control that vanishes teaches nothing about
    /// what turning it on would offer.
    /// </para>
    /// <para>
    /// Framed off from the rows above it, and captioned as running on its own clock. Shake timing
    /// and fade timing are independent - a shake starts when the effect is raised and ends after its
    /// own duration, whatever the fade is doing - and sat in the same flat column as the fade fields
    /// the two read as one set of timings that ought to agree.
    /// </para>
    /// </summary>
    private Widget BuildShakeRow()
    {
        ShakeSpec? shake = _profile.Shake;

        var enabled = MyraCheckButton.CreateWithCallback(
            shake != null,
            on =>
            {
                _profile.Shake = on ? new ShakeSpec() : null;
                _onChanged();
                Rebuild();
            }
        );

        enabled.Tooltip = TazLang.Get(
            "visualeffects_shake_tooltip",
            "Fired once as the effect arrives, scaled by how strong\n"
            + "the occurrence is. Which rectangle it displaces follows\n"
            + "the scope switch above."
        );

        // Bound to a throwaway spec while shake is off, so the grid can render its defaults without
        // the row having to special-case a null. Disabled, so those edits go nowhere.
        var grid = new StyledPropertyGrid(static () => new ShakeSpec())
        {
            Object = shake ?? new ShakeSpec(), Enabled = shake != null && !_readOnly
        };

        MyraPalette palette = MyraTheme.Current;

        var stack = new VerticalStackPanel
        {
            Spacing = MyraStyle.STANDARD_SPACING,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Padding = new Thickness(8, 6),
            Background = new SolidBrush(palette.PanelFill),
            Border = new SolidBrush(palette.PanelBorder),
            BorderThickness = new Thickness(1)
        };

        stack.Widgets.Add(
            Row(
                Editable(
                    Labelled(TazLang.Get("visualeffects_shake", "Shakes the screen"), enabled)
                )
            )
        );

        // A notch smaller than the controls it qualifies, so it explains the group rather than
        // competing with the switch above it. The gap beneath separates it from the fields it
        // qualifies; on the row spacing alone it reads as another of them.
        stack.Widgets.Add(
            new MyraLabel(
                TazLang.Get(
                    "visualeffects_shake_independent",
                    "Shake is unaffected by the fade in/out settings"
                ),
                MyraLabel.TextStyle.H6
            )
            {
                Wrap = true,
                Margin = new Thickness(0, 0, 0, NOTE_GAP)
            }
        );

        stack.Widgets.Add(grid);

        grid.PropertyChanged += (_, _) =>
        {
            if (_readOnly || _profile.Shake == null)
                return;

            _onChanged();
        };

        return stack;
    }

    private Widget LabelledFloat(string label, float value, float maximum, Action<float> commit)
    {
        var input = new FloatInputBox
        {
            MinValue = 0f,
            MaxValue = maximum,
            Width = INPUT_WIDTH,
            Enabled = !_readOnly,
            Value = value,
            VerticalAlignment = VerticalAlignment.Center
        };

        // Committing on focus loss rather than per keystroke; the value is persisted, and a save on
        // every character typed is both wasteful and jumpy. Nothing is rebuilt here - see the
        // rename flow for why a rebuild on focus loss is the wrong shape.
        input.KeyboardFocusChanged += (_, _) =>
        {
            if (input.IsKeyboardFocused)
                return;

            commit(input.Value);
            _onChanged();
        };

        return Labelled(label, input);
    }

    /// <summary>
    /// Which layer is being edited, how it is named, and the two ways to change how many there are.
    /// <para>
    /// Renaming swaps the picker for an input and a pair of buttons, exactly as the profile editor
    /// above it does. An inline box committing on focus loss was tried first and is the wrong shape:
    /// committing means rebuilding the row to re-label the picker, which tears the widget tree down
    /// under whatever the user clicked next - so the click that moved focus never lands.
    /// </para>
    /// </summary>
    private Widget BuildLayerToolbar() => _isRenamingLayer ? BuildRenameToolbar() : BuildPickerToolbar();

    private Widget BuildPickerToolbar()
    {
        bool hasLayer = SelectedLayer() != null;
        bool canAdd = !_readOnly && _profile.Layers.Count < OverlayLayerStack.MaxLayers;
        bool canEdit = !_readOnly && hasLayer;

        string[] names = LayerNames();

        Widget layers = OptionTabCommons.CreateOptionsComboBox(
            TazLang.Get("visualeffects_layer", "Layer"),
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

        layers.Margin = new Thickness(0, 0, 20, 0);

        return Row(
            layers,
            new MyraButton(TazLang.Get("visualeffects_renamelayer", "Rename"), BeginRenameLayer) { Enabled = canEdit },
            new MyraButton(TazLang.Get("visualeffects_addlayer", "Add layer"), AddLayer) { Enabled = canAdd },
            MyraStyle.ApplyButtonDangerStyle(
                new MyraButton(TazLang.Get("visualeffects_removelayer", "Remove layer"), RemoveLayer) { Enabled = canEdit }
            )
        );
    }

    private Widget BuildRenameToolbar()
    {
        _renameInput.Text = SelectedLayer()?.Name ?? string.Empty;
        _renameInput.VerticalAlignment = VerticalAlignment.Center;
        _renameInput.Tooltip = TazLang.Get("visualeffects_layername_tooltip", "Leave blank to name the layer after its technique.");

        return Row(
            Labelled(TazLang.Get("visualeffects_layername", "Name"), _renameInput),
            new MyraButton(TazLang.Get("profileeditor_save", "Save"), CommitRenameLayer),
            new MyraButton(TazLang.Get("profileeditor_cancel", "Cancel"), CancelRenameLayer)
        );
    }

    private void BeginRenameLayer()
    {
        _isRenamingLayer = true;
        Rebuild();

        // Only after the rebuild: SetKeyboardFocus goes through the Desktop, which the input box
        // does not have until it has been placed in the tree.
        if (_renameInput.Desktop == null)
            return;

        _renameInput.SetKeyboardFocus();
        _renameInput.CursorPosition = _renameInput.Text?.Length ?? 0;
    }

    private void CommitRenameLayer()
    {
        ProfileLayer? layer = SelectedLayer();
        _isRenamingLayer = false;

        if (layer == null)
        {
            Rebuild();
            return;
        }

        string? name = string.IsNullOrWhiteSpace(_renameInput.Text) ? null : _renameInput.Text.Trim();

        if (name != layer.Name)
        {
            layer.Name = name;
            _onChanged();
        }

        Rebuild();
    }

    private void CancelRenameLayer()
    {
        _isRenamingLayer = false;
        Rebuild();
    }

    /// <summary>
    /// What technique the selected layer draws with, and how it composites. Both belong to the stack
    /// rather than to the look, which is why neither is in the grid below.
    /// </summary>
    private Widget BuildLayerIdentityRow()
    {
        ProfileLayer? layer = SelectedLayer();

        if (layer == null)
            return new Panel();

        return Row(TechniqueCombo(layer), BlendCombo(layer));
    }

    /// <summary>
    /// The technique this layer draws with. Changing it swaps the effect for a fresh one of the
    /// chosen type, carrying across everything the two share - the knobs that disappear are the ones
    /// the new technique has no use for.
    /// </summary>
    private Widget TechniqueCombo(ProfileLayer layer)
    {
        IReadOnlyList<LayerEffect> techniques = LayerEffectFactory.CreateAll();
        string[] names = [.. techniques.Select(technique => technique.TechniqueName)];

        Widget combo = OptionTabCommons.CreateOptionsComboBox(
            TazLang.Get("visualeffects_technique", "Technique"),
            layer.Effect?.TechniqueName ?? string.Empty,
            names,
            selected =>
            {
                LayerEffect? replacement = techniques.FirstOrDefault(entry => entry.TechniqueName == selected);

                if (layer.Effect == null || replacement == null || replacement.TechniqueName == layer.Effect.TechniqueName)
                    return;

                layer.Effect = LayerEffectFactory.ChangeTechnique(layer.Effect, replacement);

                _onChanged();
                Rebuild();
            },
            TazLang.Get(
                "visualeffects_technique_tooltip",
                "What the layer draws: a colour painted over the scene,\n"
                + "or one of the three ways of distorting it. A distortion must sit\n"
                + "below anything it is meant to affect."
            )
        );

        combo.Enabled = !_readOnly;
        combo.Margin = new Thickness(0, 0, 20, 0);

        return combo;
    }

    /// <summary>
    /// How the selected layer combines with what is beneath it. On the stack rather than in the
    /// parameter grid because it is a property of the composition, not of the look.
    /// </summary>
    private Widget BlendCombo(ProfileLayer layer)
    {
        Widget combo = OptionTabCommons.CreateOptionsComboBox(
            TazLang.Get("visualeffects_blend", "Blend"),
            layer.Blend.ToString(),
            Enum.GetNames<OverlayBlend>(),
            selected =>
            {
                if (!Enum.TryParse(selected, out OverlayBlend blend) || layer.Blend == blend)
                    return;

                layer.Blend = blend;
                _onChanged();
            }
        );

        combo.Enabled = !_readOnly;

        return combo;
    }

    /// <summary>Layers are named by their technique and position unless the user named them, so the
    /// list says what each one is rather than merely how many there are.</summary>
    private string[] LayerNames() =>
        [
            .. _profile.Layers.Select(
                (layer, index) =>
                    $"{index + 1}. {layer.Name ?? layer.Effect?.TechniqueName ?? TazLang.Get("visualeffects_emptylayer", "Empty")}"
            )
        ];

    /// <summary>
    /// The selected layer's own parameters. The grid writes straight through to the effect, which is
    /// a reference type, so nothing has to be copied back.
    /// </summary>
    private Widget BuildLayerGrid()
    {
        if (SelectedLayer()?.Effect is not { } effect)
            return new MyraLabel(TazLang.Get("visualeffects_nolayers", "This profile has no layers."), MyraLabel.TextStyle.P);

        var grid = new StyledPropertyGrid(() => DefaultFor(effect.GetType())) {
            // Assigning it builds the rows, and the sub-grids copy the settings above from their parent
            // as they are created.
            Object = effect, // After the rows exist: Enabled is pushed to the children present at the time it is set, so
            // disabling an empty grid would leave everything built afterwards editable.
            Enabled = !_readOnly };

        grid.PropertyChanged += (_, _) =>
        {
            if (_readOnly)
                return;

            _onChanged();
        };

        return grid;
    }

    private ProfileLayer? SelectedLayer() =>
        _selectedLayer >= 0 && _selectedLayer < _profile.Layers.Count ? _profile.Layers[_selectedLayer] : null;

    /// <summary>Marks a control as belonging to the profile rather than to the session, so a shipped
    /// look cannot be edited through it.</summary>
    private T Editable<T>(T widget) where T : Widget
    {
        widget.Enabled = !_readOnly;

        return widget;
    }

    private static LayerEffect? DefaultFor(Type technique)
    {
        if (_defaultsByTechnique.TryGetValue(technique, out LayerEffect? pristine))
            return pristine;

        pristine = Activator.CreateInstance(technique) as LayerEffect;

        if (pristine != null)
            _defaultsByTechnique[technique] = pristine;

        return pristine;
    }

    /// <summary>
    /// Appends a layer, copying the shape and noise of the one on screen where there is one: a new
    /// layer is nearly always a variation of the one being tuned, and a pristine one shares no
    /// scale, channel or reach with it. Its technique is picked afterwards, in the row above the
    /// grid, since changing one keeps everything the two techniques have in common.
    /// </summary>
    private void AddLayer()
    {
        if (_readOnly || _profile.Layers.Count >= OverlayLayerStack.MaxLayers)
            return;

        var added = new TintEffect();

        if (SelectedLayer()?.Effect is { } seed)
            LayerEffectFactory.ChangeTechnique(seed, added);

        _profile.Layers.Add(new ProfileLayer { Effect = added });
        _selectedLayer = _profile.Layers.Count - 1;

        _onChanged();
        Rebuild();
    }

    private void RemoveLayer()
    {
        if (_readOnly || _profile.Layers.Count == 0)
            return;

        _profile.Layers.RemoveAt(_selectedLayer);
        _selectedLayer = Math.Min(_selectedLayer, Math.Max(_profile.Layers.Count - 1, 0));

        _onChanged();
        Rebuild();
    }

    /// <summary>
    /// One row of the composer. Everything in it is centred on the row's own axis, because these
    /// rows mix combo boxes, buttons and inputs of different heights and a top-aligned button beside
    /// a combo reads as a mistake.
    /// <para>
    /// <see cref="WrapPanel.Aligned"/> is what makes the centring take effect: unaligned, the panel
    /// arranges each child into a rectangle of exactly its own measured height, so a child's vertical
    /// alignment has no room to resolve against and everything seats at the top of the line.
    /// </para>
    /// </summary>
    /// <param name="content">The row's widgets, in order.</param>
    /// <returns>The row.</returns>
    private static WrapPanel Row(params Widget[] content)
    {
        foreach (Widget widget in content)
            widget.VerticalAlignment = VerticalAlignment.Center;

        WrapPanel row = OptionTabCommons.StyledHorizontalWrapPanel(content);
        row.Aligned = true;

        return row;
    }

    private static Widget Labelled(string label, Widget content)
    {
        content.VerticalAlignment = VerticalAlignment.Center;

        StackPanel panel = OptionTabCommons.StyledStackPanel(
            Orientation.Horizontal,
            new MyraLabel(label, MyraLabel.TextStyle.P) { VerticalAlignment = VerticalAlignment.Center },
            content
        );

        panel.VerticalAlignment = VerticalAlignment.Center;

        return panel;
    }

    #endregion
}
