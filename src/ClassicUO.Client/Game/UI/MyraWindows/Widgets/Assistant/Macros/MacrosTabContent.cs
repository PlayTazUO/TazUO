#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.Utility;
using Myra.Graphics2D.UI;
using SDL3;
using TextBox = Myra.Graphics2D.UI.TextBox;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Assistant.Macros;

public static class MacrosTabContent
{
    private static readonly HashSet<MacroType> _filteredMacroTypes = new() { MacroType.INVALID };
    private static readonly string[] _sortedMacroTypeNames;
    private static readonly MacroType[] _sortedMacroTypeValues;
    private static readonly Dictionary<MacroType, int> _macroTypeToDisplayIndex;

    private static Action? _cleanupAction;
    /// <summary>Call when the owning window closes to unsubscribe any active hotkey-capture handler and re-enable hotkeys.</summary>
    public static void Cleanup() => _cleanupAction?.Invoke();

    static MacrosTabContent()
    {
        MacroType[] macroTypes = Enum.GetValues<MacroType>()
            .Where(t => !_filteredMacroTypes.Contains(t))
            .OrderBy(t => t == MacroType.None ? "" : t.ToString(), StringComparer.OrdinalIgnoreCase)
            .ToArray();

        _sortedMacroTypeNames = macroTypes
            .Select(t => StringHelper.AddSpaceBeforeCapital(t.ToString()))
            .ToArray();
        _sortedMacroTypeValues = macroTypes;

        _macroTypeToDisplayIndex = new Dictionary<MacroType, int>();
        for (int i = 0; i < macroTypes.Length; i++)
            _macroTypeToDisplayIndex[macroTypes[i]] = i;
    }

    public static Widget Build()
    {
        Profile? profile = ProfileManager.CurrentProfile;
        if (profile == null)
            return new MyraLabel("Profile not loaded", MyraLabel.Style.P);

        // ── State ────────────────────────────────────────────────────────────
        Macro? selectedMacro = null;
        string filterText = "";

        bool isListening = false;
        SDL.SDL_Keycode capturedKey = SDL.SDL_Keycode.SDLK_UNKNOWN;
        SDL.SDL_Keymod capturedMod = SDL.SDL_Keymod.SDL_KMOD_NONE;
        Action<string>? captureHandler = null;

        // ── Panel references ─────────────────────────────────────────────────
        var macroListPanel = new VerticalStackPanel { Spacing = 1 };
        var editorPanel    = new VerticalStackPanel { Spacing = 4 };
        var hotkeyRow      = new HorizontalStackPanel { Spacing = 4 };
        var actionsPanel   = new VerticalStackPanel { Spacing = 2 };

        // ── Helpers ──────────────────────────────────────────────────────────
        void MarkDirty() => World.Instance?.Macros?.Save();

        string GetHotkeyString(Macro macro)
        {
            if (macro.ControllerButtons is { Length: > 0 })
                return Controller.GetButtonNames(macro.ControllerButtons);

            SDL.SDL_Keymod mod = SDL.SDL_Keymod.SDL_KMOD_NONE;
            if (macro.Alt)   mod |= SDL.SDL_Keymod.SDL_KMOD_ALT;
            if (macro.Ctrl)  mod |= SDL.SDL_Keymod.SDL_KMOD_CTRL;
            if (macro.Shift) mod |= SDL.SDL_Keymod.SDL_KMOD_SHIFT;

            if (macro.Key != SDL.SDL_Keycode.SDLK_UNKNOWN)
                return KeysTranslator.TryGetKey(macro.Key, mod);
            if (macro.MouseButton != MouseButtonType.None)
                return KeysTranslator.GetMouseButton(macro.MouseButton, mod);
            if (macro.WheelScroll)
                return KeysTranslator.GetMouseWheel(macro.WheelUp, mod);

            return "None";
        }

        void CancelCapture()
        {
            isListening = false;
            capturedKey = SDL.SDL_Keycode.SDLK_UNKNOWN;
            capturedMod = SDL.SDL_Keymod.SDL_KMOD_NONE;
            if (captureHandler != null)
            {
                Keyboard.KeyDownEvent -= captureHandler;
                captureHandler = null;
            }
            if (profile != null) profile.DisableHotkeys = false;
        }

        void ApplyCapturedHotkey()
        {
            if (selectedMacro == null || capturedKey == SDL.SDL_Keycode.SDLK_UNKNOWN) return;

            bool ctrl  = (capturedMod & SDL.SDL_Keymod.SDL_KMOD_CTRL)  != 0;
            bool alt   = (capturedMod & SDL.SDL_Keymod.SDL_KMOD_ALT)   != 0;
            bool shift = (capturedMod & SDL.SDL_Keymod.SDL_KMOD_SHIFT) != 0;

            Macro? existing = World.Instance?.Macros?.FindMacro(capturedKey, alt, ctrl, shift);
            if (existing != null && existing != selectedMacro)
            {
                GameActions.Print(World.Instance, $"Hotkey already used by macro: {existing.Name}", 32);
                CancelCapture();
                return;
            }

            selectedMacro.Key              = capturedKey;
            selectedMacro.Alt              = alt;
            selectedMacro.Ctrl             = ctrl;
            selectedMacro.Shift            = shift;
            selectedMacro.MouseButton      = MouseButtonType.None;
            selectedMacro.WheelScroll      = false;
            selectedMacro.ControllerButtons = null;

            CancelCapture();
            MarkDirty();
        }

        // ── BuildMacroList ────────────────────────────────────────────────────
        void BuildMacroList()
        {
            macroListPanel.Widgets.Clear();

            List<Macro> allMacros = World.Instance?.Macros?.GetAllMacros() ?? new List<Macro>();

            List<Macro> display = string.IsNullOrWhiteSpace(filterText)
                ? allMacros
                : allMacros.Where(m =>
                    m.Name.Contains(filterText, StringComparison.OrdinalIgnoreCase) ||
                    GetHotkeyString(m).Contains(filterText, StringComparison.OrdinalIgnoreCase))
                    .ToList();

            if (display.Count == 0)
            {
                macroListPanel.Widgets.Add(new MyraLabel("No macros.", MyraLabel.Style.P));
                return;
            }

            var grid = new MyraGrid();
            grid.AddColumn(new Proportion(ProportionType.Fill)); // Name
            grid.AddColumn(new Proportion(ProportionType.Auto)); // Hotkey
            grid.AddColumn(new Proportion(ProportionType.Auto)); // Edit
            MyraStyle.ApplyStandardGridStyling(grid);

            grid.AddWidget(new MyraLabel("Name",   MyraLabel.Style.H3), 0, 0);
            grid.AddWidget(new MyraLabel("Hotkey", MyraLabel.Style.H3), 0, 1);
            grid.AddWidget(new MyraLabel("Edit",   MyraLabel.Style.H3), 0, 2);

            int dataRow = 1;
            foreach (Macro macro in display)
            {
                Macro captured = macro;

                grid.AddWidget(new MyraLabel(macro.Name, MyraLabel.Style.P), dataRow, 0);
                grid.AddWidget(new MyraLabel(GetHotkeyString(macro), MyraLabel.Style.P), dataRow, 1);
                grid.AddWidget(new MyraButton("Edit", () =>
                {
                    selectedMacro = captured;
                    BuildMacroList();
                    BuildEditor();
                }), dataRow, 2);
                dataRow++;
            }

            macroListPanel.Widgets.Add(grid);
        }

        // ── BuildEditor ───────────────────────────────────────────────────────
        void BuildEditor()
        {
            CancelCapture();
            editorPanel.Widgets.Clear();

            if (selectedMacro == null)
            {
                editorPanel.Widgets.Add(new MyraLabel("Select a macro to edit.", MyraLabel.Style.P));
                return;
            }

            Macro macro = selectedMacro;

            // Name row
            var nameRow = new HorizontalStackPanel { Spacing = 4 };
            nameRow.Widgets.Add(new MyraLabel("Macro Name:", MyraLabel.Style.P));
            var nameBox = new TextBox { Text = macro.Name, Width = 200 };
            nameBox.TextChangedByUser += (_, _) =>
            {
                macro.Name = nameBox.Text ?? "";
                MarkDirty();
            };
            nameRow.Widgets.Add(nameBox);
            editorPanel.Widgets.Add(nameRow);

            // Hotkey row (rebuilt dynamically)
            BuildHotkeyRow();
            editorPanel.Widgets.Add(hotkeyRow);

            // Create Macro Button
            editorPanel.Widgets.Add(new MyraButton("Create Macro Button", () =>
            {
                foreach (IGui? gump in UIManager.Gumps)
                    if (gump is MacroButtonGump mbg && mbg.TheMacro == macro)
                    {
                        mbg.Dispose();
                        break;
                    }
                var macroButtonGump = new MacroButtonGump(World.Instance, macro, 0, 0);
                macroButtonGump.CenterXInViewPort();
                macroButtonGump.CenterYInViewPort();
                UIManager.Add(macroButtonGump);
            }) { Tooltip = "Create a draggable macro button for this macro" });

            editorPanel.Widgets.Add(new MyraLabel("Actions:", MyraLabel.Style.H3));

            BuildActionsPanel();
            editorPanel.Widgets.Add(new ScrollViewer { MaxHeight = 250, Content = actionsPanel });

            var bottomRow = new HorizontalStackPanel { Spacing = 4 };
            bottomRow.Widgets.Add(new MyraButton("Add Action", () =>
            {
                MacroObject newAction = Macro.Create(MacroType.Say);
                var scanAction = (MacroObject)macro.Items;
                MacroObject? lastAction = null;
                while (scanAction != null && scanAction.Code != MacroType.None)
                {
                    lastAction = scanAction;
                    scanAction = (MacroObject)scanAction.Next;
                }
                if (lastAction != null)
                    macro.Insert(lastAction, newAction);
                else
                {
                    macro.Items = newAction;
                    newAction.Next = new MacroObject(MacroType.None, MacroSubType.MSC_NONE);
                }
                MarkDirty();
                BuildActionsPanel();
            }));

            bottomRow.Widgets.Add(MyraStyle.ApplyButtonDangerStyle(new MyraButton("Delete Macro", () =>
            {
                new MyraDialog($"Delete '{macro.Name}'?",
                    new MyraLabel($"Are you sure you want to delete '{macro.Name}'?", MyraLabel.Style.P),
                    ok =>
                    {
                        if (!ok) return;
                        World.Instance?.Macros?.Remove(macro);
                        selectedMacro = null;
                        MarkDirty();
                        BuildMacroList();
                        BuildEditor();
                    });
            }) { Tooltip = "Permanently delete this macro" }));

            editorPanel.Widgets.Add(bottomRow);
        }

        // ── BuildHotkeyRow ────────────────────────────────────────────────────
        void BuildHotkeyRow()
        {
            hotkeyRow.Widgets.Clear();
            hotkeyRow.Widgets.Add(new MyraLabel("Hotkey:", MyraLabel.Style.P));

            if (selectedMacro == null) return;
            Macro macro = selectedMacro;

            if (isListening)
            {
                string captureDisplay = capturedKey != SDL.SDL_Keycode.SDLK_UNKNOWN
                    ? KeysTranslator.TryGetKey(capturedKey, capturedMod)
                    : "Listening...";
                hotkeyRow.Widgets.Add(new MyraLabel(captureDisplay, MyraLabel.Style.P));

                if (capturedKey != SDL.SDL_Keycode.SDLK_UNKNOWN)
                    hotkeyRow.Widgets.Add(new MyraButton("Apply", () =>
                    {
                        ApplyCapturedHotkey();
                        BuildHotkeyRow();
                        BuildMacroList();
                    }));

                hotkeyRow.Widgets.Add(new MyraButton("Cancel", () =>
                {
                    CancelCapture();
                    BuildHotkeyRow();
                }));
            }
            else
            {
                hotkeyRow.Widgets.Add(new MyraLabel(GetHotkeyString(macro), MyraLabel.Style.P));

                hotkeyRow.Widgets.Add(new MyraButton("Capture", () =>
                {
                    isListening = true;
                    capturedKey = SDL.SDL_Keycode.SDLK_UNKNOWN;
                    capturedMod = SDL.SDL_Keymod.SDL_KMOD_NONE;
                    if (profile != null) profile.DisableHotkeys = true;

                    captureHandler = hotkeyStr =>
                    {
                        // Parse: "CTRL+SHIFT+SDLK_F1" → keycode + mod
                        SDL.SDL_Keycode key = SDL.SDL_Keycode.SDLK_UNKNOWN;
                        SDL.SDL_Keymod mod  = SDL.SDL_Keymod.SDL_KMOD_NONE;
                        foreach (string part in hotkeyStr.Split('+'))
                        {
                            switch (part)
                            {
                                case "CTRL":  mod |= SDL.SDL_Keymod.SDL_KMOD_CTRL;  break;
                                case "SHIFT": mod |= SDL.SDL_Keymod.SDL_KMOD_SHIFT; break;
                                case "ALT":   mod |= SDL.SDL_Keymod.SDL_KMOD_ALT;   break;
                                default:
                                    Enum.TryParse<SDL.SDL_Keycode>(part, true, out key);
                                    break;
                            }
                        }

                        if (key == SDL.SDL_Keycode.SDLK_ESCAPE)
                        {
                            CancelCapture();
                            BuildHotkeyRow();
                            return;
                        }

                        capturedKey = key;
                        capturedMod = mod;
                        BuildHotkeyRow();
                    };

                    Keyboard.KeyDownEvent += captureHandler;
                    BuildHotkeyRow();
                }) { Tooltip = "Click then press a key to assign as hotkey" });

                hotkeyRow.Widgets.Add(new MyraButton("Clear", () =>
                {
                    macro.Key               = SDL.SDL_Keycode.SDLK_UNKNOWN;
                    macro.MouseButton       = MouseButtonType.None;
                    macro.WheelScroll       = false;
                    macro.ControllerButtons = null;
                    macro.Alt = macro.Ctrl = macro.Shift = false;
                    MarkDirty();
                    BuildHotkeyRow();
                    BuildMacroList();
                }) { Tooltip = "Remove the hotkey from this macro" });
            }
        }

        // ── BuildActionsPanel ─────────────────────────────────────────────────
        void BuildActionsPanel()
        {
            actionsPanel.Widgets.Clear();

            if (selectedMacro == null) return;
            Macro macro = selectedMacro;

            var action = (MacroObject)macro.Items;
            int actionIndex = 0;

            while (action != null && action.Code != MacroType.None)
            {
                MacroObject capturedAction = action;
                int capturedIndex = actionIndex;

                var actionRow = new HorizontalStackPanel { Spacing = 4 };
                actionRow.Widgets.Add(new MyraLabel($"{capturedIndex + 1}.", MyraLabel.Style.P));

                // Action type ComboBox
#pragma warning disable CS0612, CS0618
                var typeCombo = new ComboBox { Width = 160 };
                foreach (string typeName in _sortedMacroTypeNames)
                    typeCombo.Items.Add(new ListItem(typeName));

                int displayIdx = _macroTypeToDisplayIndex.TryGetValue(capturedAction.Code, out int di) ? di : 0;
                typeCombo.SelectedIndex = displayIdx;
                typeCombo.SelectedIndexChanged += (_, _) =>
                {
                    if (typeCombo.SelectedIndex == null) return;
                    MacroType newType = _sortedMacroTypeValues[typeCombo.SelectedIndex.Value];
                    if (newType == capturedAction.Code) return;

                    MacroObject newAction = Macro.Create(newType);
                    macro.Insert(capturedAction, newAction);
                    macro.Remove(capturedAction);
                    MarkDirty();
                    BuildActionsPanel();
                };
#pragma warning restore CS0612, CS0618
                actionRow.Widgets.Add(typeCombo);

                // Sub-type input
                if (capturedAction.SubMenuType == 1)
                {
                    // Dropdown sub-type
                    int subCount = 0, subOffset = 0;
                    Macro.GetBoundByCode(capturedAction.Code, ref subCount, ref subOffset);

                    string[] subNames = new string[subCount];
                    for (int si = 0; si < subCount; si++)
                        subNames[si] = ((MacroSubType)(si + subOffset)).ToString();

                    int curSubIdx = (int)capturedAction.SubCode - subOffset;
                    if (curSubIdx < 0 || curSubIdx >= subCount) curSubIdx = 0;

#pragma warning disable CS0612, CS0618
                    var subCombo = new ComboBox { Width = 160 };
                    foreach (string subName in subNames)
                        subCombo.Items.Add(new ListItem(subName));
                    subCombo.SelectedIndex = curSubIdx;
                    subCombo.SelectedIndexChanged += (_, _) =>
                    {
                        if (subCombo.SelectedIndex == null) return;
                        capturedAction.SubCode = (MacroSubType)(subCombo.SelectedIndex.Value + subOffset);
                        MarkDirty();
                    };
#pragma warning restore CS0612, CS0618
                    actionRow.Widgets.Add(subCombo);
                }
                else if (capturedAction.SubMenuType == 2)
                {
                    // Text input
                    string currentText = capturedAction.HasString()
                        ? ((MacroObjectString)capturedAction).Text
                        : "";
                    var textBox = new TextBox { Text = currentText, Width = 180 };
                    textBox.TextChangedByUser += (_, _) =>
                    {
                        string newText = textBox.Text ?? "";
                        if (capturedAction.HasString())
                        {
                            ((MacroObjectString)capturedAction).Text = newText;
                        }
                        else
                        {
                            // Replace with string version
                            var strAction = new MacroObjectString(capturedAction.Code, capturedAction.SubCode, newText);
                            strAction.Next = capturedAction.Next;
                            var scan = (MacroObject)macro.Items;
                            MacroObject? prev = null;
                            while (scan != null && scan != capturedAction)
                            {
                                prev = scan;
                                scan = (MacroObject)scan.Next;
                            }
                            if (prev != null) prev.Next = strAction;
                            else macro.Items = strAction;
                        }
                        MarkDirty();
                    };
                    actionRow.Widgets.Add(textBox);
                }

                // Remove button
                actionRow.Widgets.Add(MyraStyle.ApplyButtonDangerStyle(new MyraButton("X", () =>
                {
                    macro.Remove(capturedAction);
                    MarkDirty();
                    BuildActionsPanel();
                }) { Tooltip = "Remove this action" }));

                actionsPanel.Widgets.Add(actionRow);

                action = (MacroObject)action.Next;
                actionIndex++;
            }

            if (actionIndex == 0)
                actionsPanel.Widgets.Add(new MyraLabel("No actions. Click 'Add Action' to add one.", MyraLabel.Style.P));
        }

        // ── Toolbar ───────────────────────────────────────────────────────────
        var toolbar = new HorizontalStackPanel { Spacing = 4 };

        toolbar.Widgets.Add(new MyraButton("Add", () =>
        {
            string baseName = "New Macro";
            string macroName = baseName;
            int counter = 1;
            while (World.Instance?.Macros?.GetAllMacros().Any(m => m.Name == macroName) == true)
                macroName = $"{baseName} {counter++}";

            var newMacro = new Macro(macroName);
            newMacro.Items = new MacroObject(MacroType.None, MacroSubType.MSC_NONE);
            World.Instance?.Macros?.PushToBack(newMacro);
            selectedMacro = newMacro;
            MarkDirty();
            BuildMacroList();
            BuildEditor();
        }));

        toolbar.Widgets.Add(new MyraButton("Move Up", () =>
        {
            if (selectedMacro == null) return;
            World.Instance?.Macros?.MoveMacroUp(selectedMacro);
            MarkDirty();
            BuildMacroList();
        }));

        toolbar.Widgets.Add(new MyraButton("Move Down", () =>
        {
            if (selectedMacro == null) return;
            World.Instance?.Macros?.MoveMacroDown(selectedMacro);
            MarkDirty();
            BuildMacroList();
        }));

        toolbar.Widgets.Add(new MyraButton("Import", () =>
        {
            string? xml = Utility.Clipboard.GetClipboardText();
            if (xml.NotNullNotEmpty() && World.Instance?.Macros?.ImportFromXml(xml) == true)
            {
                BuildMacroList();
                return;
            }
            GameActions.Print("Your clipboard does not have a valid macro export copied.", Constants.HUE_ERROR);
        }) { Tooltip = "Import macros from clipboard (must have a valid export)" });

        toolbar.Widgets.Add(new MyraButton("Export", () =>
        {
            World.Instance?.Macros?.GetXmlExport()?.CopyToClipboard();
            int cnt = World.Instance?.Macros?.GetAllMacros().Count ?? 0;
            GameActions.Print($"Exported {cnt} macro(s) to your clipboard!", Constants.HUE_SUCCESS);
        }) { Tooltip = "Export all macros to clipboard" });

        var filterBox = new TextBox { HintText = "Filter...", Width = 150 };
        filterBox.TextChangedByUser += (_, _) =>
        {
            filterText = filterBox.Text ?? "";
            BuildMacroList();
        };
        toolbar.Widgets.Add(filterBox);

        // ── Main layout ───────────────────────────────────────────────────────
        var mainArea = new HorizontalStackPanel { Spacing = 8 };

        var listScroll = new ScrollViewer { MaxHeight = 450, Width = 270, Content = macroListPanel };
        mainArea.Widgets.Add(listScroll);
        mainArea.Widgets.Add(editorPanel);

        BuildMacroList();
        BuildEditor();

        _cleanupAction = CancelCapture;

        var root = new VerticalStackPanel { Spacing = 4 };
        root.Widgets.Add(toolbar);
        root.Widgets.Add(mainArea);
        return root;
    }
}
