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
/// Central place to view and edit every registered hotkey, plus a read-only view of macro
/// hotkeys so conflicts across the two systems are visible in one spot.
/// </summary>
public static class HotkeysTabContent
{
    public static Widget Build()
    {
        var root = new VerticalStackPanel { Spacing = 6 };
        root.Widgets.Add(new MyraLabel("Hotkeys", MyraLabel.TextStyle.H2));
        root.Widgets.Add(new MyraLabel(
            "All registered hotkeys are listed here. Macro hotkeys are shown read-only for reference; edit those in the Macros tab.",
            MyraLabel.TextStyle.P));

        var listPanel = new VerticalStackPanel { Spacing = 1 };

        Action? unsubscribe = null;
        HotKeyEntry? capturingEntry = null;

        void StopCapture()
        {
            unsubscribe?.Invoke();
            unsubscribe = null;
            capturingEntry = null;
        }

        void BuildList()
        {
            listPanel.Widgets.Clear();

            List<HotKeyEntry> registered = HotKeys.AllRegistered()
                .OrderBy(e => e.Category ?? string.Empty)
                .ThenBy(e => e.Name)
                .ToList();

            if (registered.Count == 0)
            {
                listPanel.Widgets.Add(new MyraLabel("No hotkeys registered.", MyraLabel.TextStyle.P));
                return;
            }

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
            foreach (HotKeyEntry entry in registered)
            {
                HotKeyEntry localEntry = entry;
                List<HotKeyEntry> conflicts = HotKeys.FindConflicts(entry);

                grid.AddWidget(new MyraLabel(entry.Name, MyraLabel.TextStyle.P), row, 0);
                grid.AddWidget(new MyraLabel(entry.Category ?? string.Empty, MyraLabel.TextStyle.P), row, 1);

                string bindingText = capturingEntry == entry ? "Press a key (Esc to cancel)..." : entry.Binding.Describe();
                if (conflicts.Count > 0)
                    bindingText += "  ⚠ conflicts: " + string.Join(", ", conflicts.Select(c => c.Name));
                grid.AddWidget(new MyraLabel(bindingText, MyraLabel.TextStyle.P), row, 2);

                if (capturingEntry == entry)
                {
                    grid.AddWidget(new MyraButton("Cancel", () => { StopCapture(); BuildList(); }), row, 3);
                }
                else
                {
                    grid.AddWidget(new MyraButton("Set", () => StartCapture(localEntry)), row, 3);
                }

                grid.AddWidget(new MyraButton("Clear", () =>
                {
                    StopCapture();
                    localEntry.Binding.Clear();
                    BuildList();
                }), row, 4);

                grid.AddWidget(new MyraButton("Reset", () =>
                {
                    StopCapture();
                    localEntry.ResetToDefault();
                    BuildList();
                }), row, 5);

                grid.AddWidget(MyraCheckButton.CreateWithCallback(
                    entry.Enabled,
                    b => localEntry.Enabled = b,
                    null,
                    "Enable or disable this hotkey"), row, 6);

                row++;
            }

            listPanel.Widgets.Add(grid);
        }

        void StartCapture(HotKeyEntry entry)
        {
            StopCapture();
            capturingEntry = entry;
            BuildList();

            void Handler(string hotkey)
            {
                (SDL.SDL_Keycode key, SDL.SDL_Keymod mod) = HotkeyUtil.ParseHotKeyString(hotkey);

                if (key == SDL.SDL_Keycode.SDLK_ESCAPE)
                {
                    StopCapture();
                    BuildList();
                    return;
                }

                if (key != SDL.SDL_Keycode.SDLK_UNKNOWN)
                {
                    entry.Binding = new HotkeyBinding(key, mod);
                    StopCapture();
                    BuildList();
                }
            }

            Keyboard.KeyDownEvent += Handler;
            unsubscribe = () => Keyboard.KeyDownEvent -= Handler;
        }

        BuildList();

        root.Widgets.Add(new MyraLabel(
            "Capturing a binding records keyboard keys + modifiers. Mouse, wheel and controller bindings are shown but are currently set from the system that owns them.",
            MyraLabel.TextStyle.P));
        root.Widgets.Add(new ScrollViewer { Height = 260, Content = listPanel });

        root.Widgets.Add(new MyraLabel("Macro hotkeys (read-only)", MyraLabel.TextStyle.H3));
        root.Widgets.Add(new ScrollViewer { Height = 160, Content = BuildMacroList() });

        return root;
    }

    private static Widget BuildMacroList()
    {
        var panel = new VerticalStackPanel { Spacing = 1 };

        List<(string Name, HotkeyBinding Binding)> macros = new();
        MacroManager? manager = World.Instance?.Macros;
        if (manager != null)
        {
            foreach (Macro m in manager.GetAllMacros())
            {
                var binding = MacroBinding(m);
                if (!binding.IsEmpty)
                    macros.Add((m.Name, binding));
            }
        }

        if (macros.Count == 0)
        {
            panel.Widgets.Add(new MyraLabel("No macros with hotkeys.", MyraLabel.TextStyle.P));
            return panel;
        }

        var grid = new MyraGrid();
        grid.SetupWithHeaders(
            GridColumnInfo.Fill("Macro", 2),
            GridColumnInfo.Fill("Binding", 3)
        );

        int row = 1;
        foreach ((string name, HotkeyBinding binding) in macros)
        {
            List<HotKeyEntry> conflicts = HotKeys.FindConflicts(binding);

            string bindingText = binding.Describe();
            if (conflicts.Count > 0)
                bindingText += "  ⚠ conflicts: " + string.Join(", ", conflicts.Select(c => c.Name));

            grid.AddWidget(new MyraLabel(name, MyraLabel.TextStyle.P), row, 0);
            grid.AddWidget(new MyraLabel(bindingText, MyraLabel.TextStyle.P), row, 1);
            row++;
        }

        panel.Widgets.Add(grid);
        return panel;
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
