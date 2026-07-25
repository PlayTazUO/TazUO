#nullable enable

using System;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Managers.Hotkeys;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows;

/// <summary>
/// The single, shared hotkey capture window used by every place in the client that assigns a
/// hotkey. While open it listens continuously (via <see cref="HotkeyCapture"/> in non-auto-stop
/// mode) to keyboard, mouse (side buttons only — left and right are ignored), wheel and controller
/// input, disables global hotkeys so nothing fires during capture, and only commits or discards the
/// pending binding when the user presses one of its buttons:
/// <list type="bullet">
///   <item><description><b>Save</b> — commit the pending binding and close.</description></item>
///   <item><description><b>Reset</b> — revert the pending binding to what it was when the window opened.</description></item>
///   <item><description><b>Clear</b> — set the pending binding to empty (no hotkey).</description></item>
///   <item><description><b>Cancel</b> — discard changes and close.</description></item>
/// </list>
/// Listening never stops automatically after a single key; it stops only on Save, Cancel or when the
/// window is disposed.
/// </summary>
public sealed class HotkeyCaptureWindow : MyraControl
{
    private readonly HotkeyCapture _capturer;
    private readonly HotkeyBinding _original;
    private readonly Action<HotkeyBinding> _onSaved;
    private readonly Func<HotkeyBinding, bool>? _validator;
    private readonly Myra.Graphics2D.UI.TextBox _display;
    private readonly Color _defaultTextColor;

    private HotkeyBinding _pending;

    /// <param name="prompt">Optional descriptive line shown above the capture box.</param>
    /// <param name="existing">The binding to seed the window with; a clone is taken.</param>
    /// <param name="onSaved">Invoked with the pending binding when the user presses Save.</param>
    /// <param name="capturesMouseEvents">When <see langword="true"/> side mouse buttons and wheel are captured too.</param>
    /// <param name="bindingValidator">
    /// Optional validator run on Save. A <see langword="false"/> result rejects the binding and keeps
    /// the window open so the user can try again.
    /// </param>
    public HotkeyCaptureWindow(
        string? prompt,
        HotkeyBinding? existing,
        Action<HotkeyBinding> onSaved,
        bool capturesMouseEvents = true,
        Func<HotkeyBinding, bool>? bindingValidator = null
    ) : base(TazLang.Get("hotkeycapture_title", "Set Hotkey"))
    {
        _original = (existing ?? new HotkeyBinding()).Clone();
        _pending = _original.Clone();
        _onSaved = onSaved;
        _validator = bindingValidator;

        _capturer = new HotkeyCapture { CapturesMouseEvents = capturesMouseEvents, AutoStop = false };

        var layout = new VerticalStackPanel { Spacing = 8, Padding = new Thickness(8) };

        if (!string.IsNullOrEmpty(prompt))
            layout.Widgets.Add(new MyraLabel(prompt, MyraLabel.TextStyle.P));

        layout.Widgets.Add(new MyraLabel(
            TazLang.Get("hotkeycapture_instructions", "Press a key, side mouse button, wheel or controller button."),
            MyraLabel.TextStyle.P)
        { TextColor = Color.Gray });

        _display = new Myra.Graphics2D.UI.TextBox
        {
            Width = 240,
            Readonly = true,
            Cursor = null,
            Selection = null,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        _defaultTextColor = _display.TextColor;
        layout.Widgets.Add(_display);

        var btnRow = new HorizontalStackPanel { Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right };
        btnRow.Widgets.Add(new MyraButton(TazLang.Get("hotkeycapture_save", "Save"), OnSave));
        btnRow.Widgets.Add(new MyraButton(TazLang.Get("hotkeycapture_reset", "Reset"), OnReset));
        btnRow.Widgets.Add(new MyraButton(TazLang.Get("hotkeycapture_clear", "Clear"), OnClear));
        btnRow.Widgets.Add(MyraStyle.ApplyButtonDangerStyle(new MyraButton(TazLang.Get("hotkeycapture_cancel", "Cancel"), OnCancel)));
        layout.Widgets.Add(btnRow);

        // Closing via the title-bar button (or any path that only flags disposal) tears the window
        // down through the base ExecuteDispose, which does not route through our Dispose override, so
        // stop listening here too. Stop is idempotent, so overlapping stop paths are harmless.
        _rootWindow.Closed += (_, _) => _capturer.Stop();

        SetRootContent(layout);
        CenterInViewPort();
        UIManager.Add(this);
        BringOnTop();

        UpdateText();

        // Start listening immediately; the capture stays live for the entire lifetime of the window.
        _capturer.Start(OnCaptured);
    }

    private void OnCaptured(HotkeyBinding binding)
    {
        _pending = binding;
        UpdateText();
    }

    private void OnSave()
    {
        // Reject invalid bindings but keep the window open so the user can pick another.
        if (_validator?.Invoke(_pending) == false)
            return;

        _capturer.Stop();
        _onSaved(_pending);
        _disposeRequested = true;
    }

    private void OnReset()
    {
        _pending = _original.Clone();
        UpdateText();
    }

    private void OnClear()
    {
        _pending = new HotkeyBinding();
        UpdateText();
    }

    private void OnCancel()
    {
        _capturer.Stop();
        _disposeRequested = true;
    }

    public override void Dispose()
    {
        // Disposing (e.g. the title-bar close button or a right-click) must also stop listening.
        _capturer.Stop();
        base.Dispose();
    }

    private void UpdateText()
    {
        _display.TextColor = _defaultTextColor;
        _display.Text = _pending.IsEmpty
            ? TazLang.Get("uicommons_nohotkeyset", "No hotkey set")
            : _pending.ToString();
    }
}
