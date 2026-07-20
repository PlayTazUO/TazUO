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

    private readonly HotkeyCapture _capturer;

    private HotkeyBinding _selection;

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
    public HotkeyInput(
        string? labelText = null,
        HotkeyBinding? existingSelection = null,
        Action<SelectionChangedEventArgs>? onSelectionChanged = null,
        bool capturesMouseEvents = true
    )
    {
        _onSelectionChanged = onSelectionChanged;

        _capturer = new HotkeyCapture { CapturesMouseEvents = capturesMouseEvents };

        StackPanel panel = OptionTabCommons.StyledStackPanel(Orientation.Horizontal);
        if (!string.IsNullOrEmpty(labelText))
        {
            var label = new Label { Text = labelText, VerticalAlignment = VerticalAlignment.Center };
            panel.Widgets.Add(label);
        }

        _input = new TextBox
        {
            Tooltip = TazLang.Get("mog_nameplates_optionstab_hotkeyinputtooltip"),
            Width = 150,
            Cursor = null,
            Selection = null,
            VerticalAlignment = VerticalAlignment.Center
        };

        _input.TouchDown += StartRecording;
        _defaultTextColor = _input.TextColor;

        panel.Widgets.Add(_input);
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
    /// Stops the capturer and detaches event handlers when the widget leaves the desktop or
    /// becomes invisible, preventing stale captures after the widget is no longer rendered.
    /// </summary>
    private void DetachAsNecessary()
    {
        // Check if we're still being rendered
        if (Desktop != null || Visible)
            return;

        // Detach everything
        _capturer.Stop();
        _input.TouchDown -= StartRecording;
    }

    /// <summary>
    /// Begins hotkey recording when the user clicks the input box.
    /// Does nothing if a capture session is already active.
    /// </summary>
    private void StartRecording(object? sender, EventArgs e)
    {
        if (_capturer.IsActive)
            return;

        _capturer.Start(newBinding =>
        {
            Selection = newBinding; // This auto stops upon capture
        });

        UpdateText();
    }

    /// <summary>
    /// Resets <see cref="Selection"/> to an empty binding and stops any in-progress capture.
    /// </summary>
    public void Clear()
    {
        Selection = new HotkeyBinding();
        _capturer.Stop();
    }

    /// <summary>
    /// Refreshes the text box to reflect the current capture state or binding value.
    /// Shows a "press any key" prompt while recording, or the binding string (or "no hotkey set")
    /// when idle.
    /// </summary>
    private void UpdateText()
    {
        if (_capturer.IsActive)
        {
            _input.Text = TazLang.Get("uicommons_pressanykey");
            _input.TextColor = Color.DarkGoldenrod;
            return;
        }

        _input.TextColor = _defaultTextColor;

        if (Selection.IsEmpty)
        {
            _input.Text = TazLang.Get("uicommons_nohotkeyset");
            return;
        }

        _input.Text = Selection.ToString();
    }
}
