#nullable enable

using System;
using ClassicUO.Game.UI.MyraWindows.Options.Tabs;
using ClassicUO.Input;
using Microsoft.Xna.Framework;
using Myra.Events;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using SDL3;
using Keyboard = ClassicUO.Input.Keyboard;
using Mouse = ClassicUO.Input.Mouse;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.HotkeyInput;

public class SelectionChangedEventArgs : EventArgs
{
    public HotkeySelection OldValue { get; init; } = new();
    public HotkeySelection NewValue { get; init; } = new();
}

public class HotkeyInput : Panel
{
    public event EventHandler<SelectionChangedEventArgs>? SelectionChanged;

    private readonly TextBox _input;
    private readonly Action<SelectionChangedEventArgs>? _onSelectionChanged;
    private readonly Color _defaultTextColor;

    private bool _isRecording;

    // Baseline mouse-button states captured when recording starts, so the same
    // click that opened the textbox (TouchDown fires for L/R/M alike in Myra)
    // isn't immediately re-captured as the bound button.
    private bool _baseLPressed;
    private bool _baseRPressed;
    private bool _baseMPressed;
    private bool _baseXPressed;

    private HotkeySelection _selection;

    public HotkeySelection Selection
    {
        get => _selection;
        set
        {
            if (_selection.Equals(value))
                return;

            var eventArgs = new SelectionChangedEventArgs { OldValue = _selection, NewValue = value };

            _selection = value;
            _onSelectionChanged?.Invoke(eventArgs);
            SelectionChanged?.Invoke(this, eventArgs);
        }
    }

    public HotkeyInput(
        string? labelText = null,
        HotkeySelection? existingSelection = null,
        Action<SelectionChangedEventArgs>? onSelectionChanged = null
    )
    {
        _onSelectionChanged = onSelectionChanged;
        _selection = existingSelection ?? new HotkeySelection();

        StackPanel panel = OptionTabCommons.StyledStackPanel(Orientation.Horizontal);
        if (!string.IsNullOrEmpty(labelText))
        {
            var label = new Label { Text = labelText, VerticalAlignment = VerticalAlignment.Center };
            panel.Widgets.Add(label);
        }

        _input = new TextBox
        {
            Width = 150,
            Text = !_selection.IsEmpty ? _selection.ToString() : "No hotkey set",
            Cursor = null,
            Selection = null,
            VerticalAlignment = VerticalAlignment.Center
        };

        _input.TouchDown += (_, _) => StartRecording();
        _defaultTextColor = _input.TextColor;

        panel.Widgets.Add(_input);
        panel.Widgets.Add(new MyraButton("Clear", Clear));

        Children.Add(panel);

        // Subscribes on this Panel (not _input) because AcceptsMouseWheel is overridden below.
        MouseWheelChanged += OnMouseWheelChanged;
    }

    // Myra only exposes wheel events per-widget when this is true; left/right/middle
    // button identity isn't exposed by Myra at all (TouchDown fires for any of them),
    // so those are polled from the raw ClassicUO.Input.Mouse state in InternalRender.
    protected override bool AcceptsMouseWheel => _isRecording;

    public override void InternalRender(RenderContext context)
    {
        if (_isRecording)
            PollMouseButtons();

        base.InternalRender(context);
    }

    private void PollMouseButtons()
    {
        if (TryCaptureButton(Mouse.LButtonPressed, ref _baseLPressed, MouseButtonType.Left)) return;
        if (TryCaptureButton(Mouse.RButtonPressed, ref _baseRPressed, MouseButtonType.Right)) return;
        if (TryCaptureButton(Mouse.MButtonPressed, ref _baseMPressed, MouseButtonType.Middle)) return;
        // Mouse.XButtonPressed doesn't distinguish XButton1 from XButton2, so this
        // always binds XButton1; there's no way to tell them apart with the current
        // ClassicUO.Input.Mouse state without changes outside this file.
        TryCaptureButton(Mouse.XButtonPressed, ref _baseXPressed, MouseButtonType.XButton1);
    }

    private bool TryCaptureButton(bool pressed, ref bool basePressed, MouseButtonType button)
    {
        if (!pressed)
        {
            basePressed = false;
            return false;
        }

        if (basePressed)
            return false;

        basePressed = true;
        CaptureSelection(new HotkeySelection(mouseButton: button, modifiers: CurrentModifiers()));
        return true;
    }

    private void OnMouseWheelChanged(object? sender, GenericEventArgs<float> e)
    {
        if (!_isRecording || e.Data == 0)
            return;

        MouseWheelEvent wheel = e.Data > 0 ? MouseWheelEvent.ScrollUp : MouseWheelEvent.ScrollDown;
        CaptureSelection(new HotkeySelection(wheel: wheel, modifiers: CurrentModifiers()));
    }

    // Keyboard tracks live Ctrl/Shift/Alt state from SDL key events; mouse button/wheel
    // captures don't come with their own modifier info, so read it from there.
    private static SDL.SDL_Keymod CurrentModifiers()
    {
        SDL.SDL_Keymod mod = SDL.SDL_Keymod.SDL_KMOD_NONE;
        if (Keyboard.Ctrl) mod |= SDL.SDL_Keymod.SDL_KMOD_CTRL;
        if (Keyboard.Shift) mod |= SDL.SDL_Keymod.SDL_KMOD_SHIFT;
        if (Keyboard.Alt) mod |= SDL.SDL_Keymod.SDL_KMOD_ALT;
        return mod;
    }

    protected override void OnPlacedChanged() => DetachAsNecessary();

    public override void OnVisibleChanged() => DetachAsNecessary();

    private void DetachAsNecessary()
    {
        if (_isRecording && (Desktop == null || !Visible))
            StopRecording();
    }

    private void StartRecording()
    {
        if (_isRecording)
            return;
        _isRecording = true;

        // Skip whichever button is already down from the click that opened recording
        // (Myra's TouchDown can't tell which button caused it, and it may still be
        // held here) so it isn't immediately re-captured as the bound button.
        _baseLPressed = Mouse.LButtonPressed;
        _baseRPressed = Mouse.RButtonPressed;
        _baseMPressed = Mouse.MButtonPressed;
        _baseXPressed = Mouse.XButtonPressed;

        _input.Text = "Press a key...";
        _input.TextColor = Color.DarkGoldenrod;
        Keyboard.KeyDownEvent += OnGlobalKeyDown;
    }

    public void Clear()
    {
        _input.Text = "No hotkey set";
        Selection = new HotkeySelection();
        StopRecording();
        _input.TextColor = _defaultTextColor;
    }

    private void OnGlobalKeyDown(string key) => CaptureSelection(HotkeySelection.FromString(key));

    private void CaptureSelection(HotkeySelection selection)
    {
        if (!_isRecording)
            return;

        StopRecording();

        Selection = selection;
        _input.Text = selection.ToString();
        _input.TextColor = Color.White;
    }

    private void StopRecording()
    {
        _isRecording = false;
        Keyboard.KeyDownEvent -= OnGlobalKeyDown;
    }
}
