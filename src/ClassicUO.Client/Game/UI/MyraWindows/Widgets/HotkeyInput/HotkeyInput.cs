#nullable enable

using System;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers.Hotkeys;
using ClassicUO.Game.UI.MyraWindows.Options.Tabs;
using Microsoft.Xna.Framework;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.HotkeyInput;

/// <summary>
/// Event arguments for <see cref="HotkeyInput.SelectionChanged"/>, carrying the previous
/// and new <see cref="HotkeyBinding"/> values.
/// </summary>
public class SelectionChangedEventArgs : EventArgs
{
    /// <summary>Gets the binding that was active before the change.</summary>
    public HotkeyBinding OldValue { get; init; } = new();

    /// <summary>Gets the binding that became active after the change.</summary>
    public HotkeyBinding NewValue { get; init; } = new();
}

/// <summary>
/// A Myra <see cref="Panel"/> widget that lets the user record a keyboard (and optionally mouse)
/// hotkey binding. Clicking the embedded text box enters recording mode; the next key or button
/// press is captured and stored as the current <see cref="Selection"/>. A Clear button resets the
/// binding to empty.
/// </summary>
public class HotkeyInput : Panel
{
    /// <summary>
    /// Raised after <see cref="Selection"/> changes, supplying both the old and new bindings.
    /// </summary>
    public event EventHandler<SelectionChangedEventArgs>? SelectionChanged;

    private readonly TextBox _input;
    private readonly Action<SelectionChangedEventArgs>? _onSelectionChanged;
    private readonly Color _defaultTextColor;
    private readonly bool _capturesMouseEvents;
    private readonly string? _labelText;

    private HotkeyCaptureWindow? _captureWindow;

    private HotkeyBinding _selection;

    public Func<HotkeyBinding, bool>? BindingValidator { get; set; }

    /// <summary>
    /// Gets or sets the tooltip shown on the input box. Hides <see cref="Widget.Tooltip"/> so the
    /// hint lands on the box itself rather than the surrounding panel; defaults to a generic
    /// usage hint.
    /// </summary>
    public new string Tooltip
    {
        get => _input.Tooltip;
        set => _input.Tooltip = value;
    }

    /// <summary>
    /// Gets or sets the currently recorded hotkey binding.
    /// Setting a value equal to the current one is a no-op; any change raises
    /// <see cref="SelectionChanged"/> and invokes the optional callback supplied at construction.
    /// </summary>
    public HotkeyBinding Selection
    {
        get => _selection;
        set
        {
            if (_selection.Equals(value))
                return;

            var eventArgs = new SelectionChangedEventArgs { OldValue = _selection, NewValue = value };

            _selection = value;
            UpdateText();
            _onSelectionChanged?.Invoke(eventArgs);
            SelectionChanged?.Invoke(this, eventArgs);
        }
    }

    /// <summary>
    /// Initializes a new <see cref="HotkeyInput"/> widget.
    /// </summary>
    /// <param name="labelText">Optional label rendered to the left of the input box.</param>
    /// <param name="existingSelection">Pre-populated binding; defaults to an empty binding.</param>
    /// <param name="onSelectionChanged">
    /// Callback invoked synchronously when <see cref="Selection"/> changes, in addition to the
    /// <see cref="SelectionChanged"/> event.
    /// </param>
    /// <param name="capturesMouseEvents">
    /// When <see langword="true"/> the underlying <see cref="HotkeyCapture"/> also records mouse
    /// button presses as part of the binding.
    /// </param>
    /// <param name="bindingValidator">An optional binding validator. Called when a hotkey is captured by the component. A <see langword="true"/> result indicates the hotkey is valid,
    /// a <see langword="false"/> result indicates the hotkey is invalid and should be rejected</param>
    public HotkeyInput(
        string? labelText = null,
        HotkeyBinding? existingSelection = null,
        Action<SelectionChangedEventArgs>? onSelectionChanged = null,
        bool capturesMouseEvents = true,
        Func<HotkeyBinding, bool>? bindingValidator = null
    )
    {
        _onSelectionChanged = onSelectionChanged;
        BindingValidator = bindingValidator;
        _capturesMouseEvents = capturesMouseEvents;
        _labelText = labelText;

        StackPanel panel = OptionTabCommons.StyledStackPanel(Orientation.Horizontal);
        if (!string.IsNullOrEmpty(labelText))
        {
            var label = new Label { Text = labelText, VerticalAlignment = VerticalAlignment.Center };
            panel.Widgets.Add(label);
        }

        _input = new TextBox
        {
            Tooltip = TazLang.Get("uicommons_hotkeyinputtooltip_new"),
            Width = 150,
            Cursor = null,
            Selection = null,
            VerticalAlignment = VerticalAlignment.Center,
            Readonly = true
        };

        //_input.TouchDown += StartRecording;
        _defaultTextColor = _input.TextColor;

        panel.Widgets.Add(_input);
        panel.Widgets.Add(new MyraButton(TazLang.Get("mog_kw_set"), StartRecording));
        panel.Widgets.Add(new MyraButton(TazLang.Get("mog_kw_clear"), Clear));

        _selection = existingSelection ?? new HotkeyBinding();
        UpdateText();

        Children.Add(panel);
    }

    /// <inheritdoc/>
    protected override void OnPlacedChanged() => DetachAsNecessary();

    /// <inheritdoc/>
    public override void OnVisibleChanged() => DetachAsNecessary();

    /// <summary>
    /// Closes any open capture window when the widget leaves the desktop or becomes invisible,
    /// so a capture session cannot outlive the widget that spawned it.
    /// </summary>
    private void DetachAsNecessary()
    {
        // OnPlacedChanged/OnVisibleChanged can fire before the constructor body runs, so the backing
        // field may not exist yet.
        if (_input == null)
            return;

        if (Desktop != null && Visible)
            return;

        CloseCaptureWindow();
        UpdateText();
    }

    /// <summary>
    /// Opens the shared <see cref="HotkeyCaptureWindow"/> to record a new binding. Does nothing if a
    /// capture window is already open for this input.
    /// </summary>
    private void StartRecording()
    {
        if (_captureWindow is { IsDisposed: false })
        {
            _captureWindow.BringOnTop();
            return;
        }

        _captureWindow = new HotkeyCaptureWindow(
            prompt: _labelText,
            existing: _selection,
            onSaved: newBinding => Selection = newBinding,
            capturesMouseEvents: _capturesMouseEvents,
            bindingValidator: BindingValidator
        );
    }

    private void CloseCaptureWindow()
    {
        if (_captureWindow is { IsDisposed: false })
            _captureWindow.Dispose();

        _captureWindow = null;
    }

    /// <summary>
    /// Resets <see cref="Selection"/> to an empty binding and closes any open capture window.
    /// </summary>
    public void Clear()
    {
        Selection = new HotkeyBinding();
        CloseCaptureWindow();
    }

    /// <summary>
    /// Refreshes the text box to reflect the current binding value (or a "no hotkey set" prompt).
    /// </summary>
    private void UpdateText()
    {
        _input.TextColor = _defaultTextColor;

        if (Selection.IsEmpty)
        {
            _input.Text = TazLang.Get("uicommons_nohotkeyset");
            return;
        }

        _input.Text = Selection.ToString();
    }
}
