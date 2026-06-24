#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Managers.Hotkeys;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.Input;
using Myra.Graphics2D.UI;
using SDL3;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Assistant;

/// <summary>
/// Central place to view and edit every hotkey in one table. Registered hotkeys persist to
/// hotkeys.json; macro hotkeys are shown in the same table and, when edited here, are saved back
/// to the macro itself (macros.xml) so the macro system stays the owner of its bindings.
/// Conflicts across both systems are flagged inline.
/// </summary>
public static class HotkeysTabContent
{
    /// <summary>
    /// A unified editable row over either a registered <see cref="HotKeyEntry"/> or a <see cref="Macro"/>.
    /// <see cref="Source"/> is the stable backing object, used to keep "currently capturing" identity
    /// across table rebuilds.
    /// </summary>
    private sealed class HotkeyRow
    {
        public string Name = string.Empty;
        public string Category = string.Empty;
        public object Source = null!;
        public Func<HotkeyBinding> GetBinding = () => new HotkeyBinding();
        public Action<HotkeyBinding> Apply = _ => { };
        public Action ClearBinding = () => { };
        public Action? ResetBinding;
        public bool SupportsEnable;
        public Func<bool> GetEnabled = () => true;
        public Action<bool> SetEnabled = _ => { };
    }

    public static Widget Build()
    {
        var root = new VerticalStackPanel { Spacing = 6 };
        root.Widgets.Add(new MyraLabel("Hotkeys", MyraLabel.TextStyle.H2));
        root.Widgets.Add(new MyraLabel(
            "Every hotkey is listed here. Macro hotkeys are editable too — changes to a macro's "
            + "hotkey are saved back to the macro itself.",
            MyraLabel.TextStyle.P));

        var listPanel = new VerticalStackPanel { Spacing = 1 };

        var capture = new HotkeyCapture();
        object? capturingSource = null;

        void StopCapture()
        {
            capture.Stop();
            capturingSource = null;
        }

        void BuildList()
        {
            listPanel.Widgets.Clear();

            List<HotkeyRow> rows = BuildRows();
            if (rows.Count == 0)
            {
                listPanel.Widgets.Add(new MyraLabel("No hotkeys registered.", MyraLabel.TextStyle.P));
                return;
            }

            // Snapshot every binding up front so conflicts are detected across the whole table
            // (registered hotkeys and macros alike), not just within one system.
            List<(HotkeyRow Row, HotkeyBinding Binding)> snapshots =
                rows.Select(r => (Row: r, Binding: r.GetBinding())).ToList();

            var grid = new MyraGrid();
            grid.SetupWithHeaders(
                GridColumnInfo.Fill("Name", 2),
                GridColumnInfo.Auto("Category"),
                GridColumnInfo.Fill("Binding", 3),
                GridColumnInfo.Auto(""),
                GridColumnInfo.Auto(""),
                GridColumnInfo.Auto(""),
                GridColumnInfo.Auto("On")
            );

            int row = 1;
            foreach ((HotkeyRow r, HotkeyBinding binding) in snapshots)
            {
                HotkeyRow localRow = r;
                bool isCapturing = capturingSource != null && ReferenceEquals(r.Source, capturingSource);

                List<string> conflicts = binding.IsEmpty
                    ? new List<string>()
                    : snapshots
                        .Where(s => !ReferenceEquals(s.Row, r) && binding.Matches(s.Binding))
                        .Select(s => s.Row.Name)
                        .ToList();

                grid.AddWidget(new MyraLabel(r.Name, MyraLabel.TextStyle.P), row, 0);
                grid.AddWidget(new MyraLabel(r.Category ?? string.Empty, MyraLabel.TextStyle.P), row, 1);

                string bindingText = isCapturing
                    ? "Listening… press key / mouse / wheel / controller (Esc to cancel)"
                    : binding.Describe();
                if (conflicts.Count > 0)
                    bindingText += "  ⚠ conflicts: " + string.Join(", ", conflicts);
                grid.AddWidget(new MyraLabel(bindingText, MyraLabel.TextStyle.P), row, 2);

                if (isCapturing)
                    grid.AddWidget(new MyraButton("Cancel", () => { StopCapture(); BuildList(); }), row, 3);
                else
                    grid.AddWidget(new MyraButton("Set", () => StartCapture(localRow)), row, 3);

                grid.AddWidget(new MyraButton("Clear", () =>
                {
                    StopCapture();
                    localRow.ClearBinding();
                    BuildList();
                }), row, 4);

                if (localRow.ResetBinding != null)
                {
                    grid.AddWidget(new MyraButton("Reset", () =>
                    {
                        StopCapture();
                        localRow.ResetBinding();
                        BuildList();
                    }), row, 5);
                }
                else
                {
                    grid.AddWidget(new MyraLabel(string.Empty, MyraLabel.TextStyle.P), row, 5);
                }

                if (localRow.SupportsEnable)
                {
                    grid.AddWidget(MyraCheckButton.CreateWithCallback(
                        localRow.GetEnabled(),
                        b => localRow.SetEnabled(b),
                        null,
                        "Enable or disable this hotkey"), row, 6);
                }
                else
                {
                    grid.AddWidget(new MyraLabel(string.Empty, MyraLabel.TextStyle.P), row, 6);
                }

                row++;
            }

            listPanel.Widgets.Add(grid);
        }

        void StartCapture(HotkeyRow row)
        {
            capturingSource = row.Source;
            BuildList();

            capture.Start(
                binding =>
                {
                    row.Apply(binding);
                    capturingSource = null;
                    BuildList();
                },
                () =>
                {
                    capturingSource = null;
                    BuildList();
                });
        }

        BuildList();

        root.Widgets.Add(new MyraLabel(
            "Capturing records keyboard keys, mouse buttons (middle/extra), mouse wheel, or "
            + "controller buttons, together with any held modifiers. Left/right mouse buttons "
            + "can't be bound.",
            MyraLabel.TextStyle.P));
        root.Widgets.Add(new ScrollViewer { Height = 360, Content = listPanel });

        return root;
    }

    private static List<HotkeyRow> BuildRows()
    {
        var rows = new List<HotkeyRow>();

        foreach (HotKeyEntry entry in HotKeys.AllRegistered()
                     .OrderBy(e => e.Category ?? string.Empty)
                     .ThenBy(e => e.Name))
        {
            HotKeyEntry e = entry;
            rows.Add(new HotkeyRow
            {
                Name = e.Name,
                Category = e.Category ?? string.Empty,
                Source = e,
                GetBinding = () => e.Binding ?? new HotkeyBinding(),
                Apply = b => e.Binding = b,
                ClearBinding = () => e.Binding?.Clear(),
                ResetBinding = e.ResetToDefault,
                SupportsEnable = true,
                GetEnabled = () => e.Enabled,
                SetEnabled = v => e.Enabled = v,
            });
        }

        MacroManager? manager = World.Instance?.Macros;
        if (manager != null)
        {
            MacroManager macros = manager;
            foreach (Macro m in macros.GetAllMacros().OrderBy(x => x.Name))
            {
                Macro macro = m;
                rows.Add(new HotkeyRow
                {
                    Name = macro.Name,
                    Category = "Macro",
                    Source = macro,
                    GetBinding = () => MacroBinding(macro),
                    Apply = b =>
                    {
                        ApplyBindingToMacro(macro, b);
                        macros.Save();
                    },
                    ClearBinding = () =>
                    {
                        ClearMacroBinding(macro);
                        macros.Save();
                    },
                    // Macros have no registered default to reset to; Clear covers removal.
                    ResetBinding = null,
                    SupportsEnable = false,
                });
            }
        }

        return rows;
    }

    private static void ApplyBindingToMacro(Macro macro, HotkeyBinding b)
    {
        // A macro holds a single binding; a captured binding has exactly one input kind set, so
        // copying every field cleanly replaces whatever was bound before.
        macro.Key = b.Key;
        macro.Ctrl = b.Ctrl;
        macro.Shift = b.Shift;
        macro.Alt = b.Alt;
        macro.MouseButton = b.MouseButton;
        macro.WheelScroll = b.WheelScroll;
        macro.WheelUp = b.WheelUp;
        macro.ControllerButtons = b.ControllerButtons;
    }

    private static void ClearMacroBinding(Macro macro)
    {
        macro.Key = SDL.SDL_Keycode.SDLK_UNKNOWN;
        macro.Ctrl = macro.Shift = macro.Alt = false;
        macro.MouseButton = MouseButtonType.None;
        macro.WheelScroll = false;
        macro.WheelUp = false;
        macro.ControllerButtons = null;
    }

    private static HotkeyBinding MacroBinding(Macro m) => new()
    {
        Key = m.Key,
        Ctrl = m.Ctrl,
        Shift = m.Shift,
        Alt = m.Alt,
        MouseButton = m.MouseButton,
        WheelScroll = m.WheelScroll,
        WheelUp = m.WheelUp,
        ControllerButtons = m.ControllerButtons
    };
}
