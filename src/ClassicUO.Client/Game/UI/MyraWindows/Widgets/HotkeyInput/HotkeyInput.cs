#nullable enable

using System;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers.Hotkeys;
using ClassicUO.Game.UI.MyraWindows.Options.Tabs;
using Microsoft.Xna.Framework;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.HotkeyInput;

public class SelectionChangedEventArgs : EventArgs
{
    public HotkeyBinding OldValue { get; init; } = new();
    public HotkeyBinding NewValue { get; init; } = new();
}

public class HotkeyInput : Panel
{
    public event EventHandler<SelectionChangedEventArgs>? SelectionChanged;

    private readonly TextBox _input;
    private readonly Action<SelectionChangedEventArgs>? _onSelectionChanged;
    private readonly Color _defaultTextColor;

    private readonly HotkeyCapture _capturer = new() { CapturesMouseEvents = false };

    private HotkeyBinding _selection;

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

    public HotkeyInput(
        string? labelText = null,
        HotkeyBinding? existingSelection = null,
        Action<SelectionChangedEventArgs>? onSelectionChanged = null
    )
    {
        _onSelectionChanged = onSelectionChanged;

        StackPanel panel = OptionTabCommons.StyledStackPanel(Orientation.Horizontal);
        if (!string.IsNullOrEmpty(labelText))
        {
            var label = new Label { Text = labelText, VerticalAlignment = VerticalAlignment.Center };
            panel.Widgets.Add(label);
        }

        _input = new TextBox
        {
            Tooltip = Language.Instance.GetModernOptionsGumpLanguage.GetNamePlates.OptionsTab.HotkeyInputTooltip,
            Width = 150,
            Cursor = null,
            Selection = null,
            VerticalAlignment = VerticalAlignment.Center
        };

        _input.TouchDown += StartRecording;
        _defaultTextColor = _input.TextColor;

        panel.Widgets.Add(_input);
        panel.Widgets.Add(new MyraButton(Language.Instance.GetModernOptionsGumpLanguage.Kw.Clear, Clear));

        _selection = existingSelection ?? new HotkeyBinding();
        UpdateText();

        Children.Add(panel);
    }

    protected override void OnPlacedChanged() => DetachAsNecessary();

    public override void OnVisibleChanged() => DetachAsNecessary();

    private void DetachAsNecessary()
    {
        // Check if we're still being rendered
        if (Desktop != null || Visible)
            return;

        // Detach everything
        _capturer.Stop();
        _input.TouchDown -= StartRecording;
    }

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

    public void Clear()
    {
        Selection = new HotkeyBinding();
        _capturer.Stop();
    }

    private void UpdateText()
    {
        if (_capturer.IsActive)
        {
            _input.Text = Language.Instance.UiCommons.PressAnyKey;
            _input.TextColor = Color.DarkGoldenrod;
            return;
        }

        _input.TextColor = _defaultTextColor;

        if (Selection.IsEmpty)
        {
            _input.Text = Language.Instance.UiCommons.NoHotkeySet;
            return;
        }

        _input.Text = Selection.ToString();
    }
}
