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
/// Conflicts across both systems are surfaced in the Conflicts column (hover for details).
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
        public bool CheckConflicts = true;
    }

    private static HotkeyCapture _capture;

    /// <summary>Stop any in-progress capture; called from AssistantWindow.Dispose so static input
    /// subscriptions don't outlive the window.</summary>
    public static void Cleanup() => _capture?.Stop();

    public static Widget Build()
    {
        var root = new VerticalStackPanel { Spacing = 6 };
        root.Widgets.Add(new MyraLabel("Hotkeys", MyraLabel.TextStyle.H2));

        var listPanel = new VerticalStackPanel { Spacing = 1 };

        _capture = new HotkeyCapture();
        object? capturingSource = null;

        void StopCapture()
        {
            _capture.Stop();
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

            var grid = new MyraGrid { MaxWidth = 800 };
            grid.SetupWithHeaders(
                GridColumnInfo.Auto("Category"),
                GridColumnInfo.Auto("Name"),
                GridColumnInfo.Auto("Binding"),
                GridColumnInfo.Auto("Conflicts"),
                GridColumnInfo.Auto(""),
                GridColumnInfo.Auto(""),
                GridColumnInfo.Auto("On")
            );

            int row = 1;
            foreach ((HotkeyRow r, HotkeyBinding binding) in snapshots)
            {
                HotkeyRow localRow = r;
                bool isCapturing = capturingSource != null && ReferenceEquals(r.Source, capturingSource);

                List<string> conflicts = (binding.IsEmpty || !r.CheckConflicts)
                    ? new List<string>()
                    : snapshots
                        .Where(s => !ReferenceEquals(s.Row, r) && s.Row.CheckConflicts && binding.Matches(s.Binding))
                        .Select(s => s.Row.Name)
                        .ToList();

                grid.AddWidget(new MyraLabel(r.Category ?? string.Empty, MyraLabel.TextStyle.P), row, 0);
                grid.AddWidget(new MyraLabel(r.Name, MyraLabel.TextStyle.P), row, 1);

                string bindingText = isCapturing
                    ? "Listening… press key / modifier / mouse / wheel / controller (Esc to cancel)"
                    : binding.Describe();
                grid.AddWidget(new MyraLabel(bindingText, MyraLabel.TextStyle.P), row, 2);

                var conflictLabel = new MyraLabel(conflicts.Count > 0 ? "Yes" : string.Empty, MyraLabel.TextStyle.P);
                if (conflicts.Count > 0)
                    conflictLabel.Tooltip = "Conflicts with: " + string.Join(", ", conflicts);
                grid.AddWidget(conflictLabel, row, 3);

                if (isCapturing)
                    grid.AddWidget(new MyraButton("Cancel", () => { StopCapture(); BuildList(); }), row, 4);
                else
                    grid.AddWidget(new MyraButton("Set", () => StartCapture(localRow)), row, 4);

                MyraButton menuButton = null!;
                menuButton = new MyraButton("...", () =>
                {
                    var items = new List<(string Label, Action Action)>
                    {
                        ("Clear", () => { StopCapture(); localRow.ClearBinding(); BuildList(); })
                    };
                    if (localRow.ResetBinding != null)
                    {
                        Action reset = localRow.ResetBinding;
                        items.Add(("Reset", () => { StopCapture(); reset(); BuildList(); }));
                    }
                    ShowRowMenu(menuButton, items.ToArray());
                });
                grid.AddWidget(menuButton, row, 5);

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

            _capture.Start(
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

        var topBar = new HorizontalStackPanel { Spacing = 4 };
        topBar.Widgets.Add(new MyraButton("Reset all", () =>
        {
            StopCapture();
            foreach (HotKeyEntry e in HotKeys.AllRegistered())
                e.ResetToDefault();
            BuildList();
        }));
        root.Widgets.Add(topBar);

        root.Widgets.Add(new ScrollViewer { Height = 360, Content = listPanel });

        return root;
    }

    private static void ShowRowMenu(Widget anchor, params (string Label, Action Action)[] items)
    {
        var menu = new VerticalMenu();
        foreach ((string label, Action action) in items)
        {
            var item = new MenuItem { Text = label };
            if (action != null)
            {
                Action captured = action;
                item.Selected += (_, _) => captured();
            }
            menu.Items.Add(item);
        }
        anchor.Desktop?.ShowContextMenu(menu, Mouse.Position);
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
                CheckConflicts = e.CheckConflicts,
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
