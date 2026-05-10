using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.Utility;
using SDL3;

namespace ClassicUO.Game.UI.ImGuiControls
{
    public class MacrosTabContent : TabContent
    {
        private readonly Profile _profile = ProfileManager.CurrentProfile;

        // UI State
        private Macro _selectedMacro = null;
        private int _selectedMacroIndex = -1;
        private bool _showDeleteMacroDialog = false;
        private string _filterText = "";

        // Save debouncing
        private float _saveTimer = 0;
        private const float SAVE_DELAY = 0.5f;

        // Hotkey capture state
        private bool _isListeningForHotkey = false;
        private SDL.SDL_Keycode _capturedKey = SDL.SDL_Keycode.SDLK_UNKNOWN;
        private SDL.SDL_Keymod _capturedMod = SDL.SDL_Keymod.SDL_KMOD_NONE;
        private SDL.SDL_GamepadButton[] _capturedButtons = null;
        private MouseButtonType _capturedMouseButton = MouseButtonType.None;
        private List<Macro> _cachedMacroList;
        private List<Macro> _filteredMacroList;

        // Sorted/filtered action type mappings
        private static readonly string[] _sortedMacroTypeNames;
        private static readonly MacroType[] _sortedMacroTypeValues;
        private static readonly Dictionary<MacroType, int> _macroTypeToDisplayIndex;
        private static Vector2 _macroSize = new(650, 500);

        private static readonly HashSet<MacroType> _filteredMacroTypes = new()
        {
            MacroType.INVALID
        };

        static MacrosTabContent()
        {
            // Build sorted/filtered MacroType mapping
            MacroType[] macroTypes = Enum.GetValues<MacroType>()
                .Where(t => !_filteredMacroTypes.Contains(t))
                .OrderBy(t => t == MacroType.None ? "" : t.ToString(), StringComparer.OrdinalIgnoreCase)
                .ToArray();

            _sortedMacroTypeNames = macroTypes.Select(t => StringHelper.AddSpaceBeforeCapital(t.ToString())).ToArray();
            _sortedMacroTypeValues = macroTypes;
            _macroTypeToDisplayIndex = new Dictionary<MacroType, int>();
            for (int i = 0; i < macroTypes.Length; i++)
            {
                _macroTypeToDisplayIndex[macroTypes[i]] = i;
            }
        }

        public MacrosTabContent()
        {
        }

        public override void DrawContent()
        {
            if (_profile == null)
            {
                ImGui.Text("Profile not loaded");
                return;
            }

            if (ImGui.BeginChild("macros_children_area", _macroSize))
            {
                // Handle hotkey capture during draw
                if (_isListeningForHotkey) CaptureCurrentInput();

                DrawToolbar();
                ImGui.Separator();
                ImGui.Spacing();

                // Two-panel layout
                if (ImGui.BeginTable("MacrosSplit", 2, ImGuiTableFlags.Resizable))
                {
                    ImGui.TableSetupColumn("MacroList", ImGuiTableColumnFlags.WidthFixed, 300);
                    ImGui.TableSetupColumn("MacroEditor", ImGuiTableColumnFlags.WidthStretch, 300);

                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    DrawMacroList();

                    ImGui.TableSetColumnIndex(1);
                    DrawMacroEditor();

                    ImGui.EndTable();
                }

                DrawDialogs();

                ImGui.EndChild();
            }
        }

        public override void Update()
        {
            base.Update();

            // Handle debounced save
            if (_saveTimer > 0)
            {
                _saveTimer -= (float)Time.Delta;
                if (_saveTimer <= 0)
                {
                    World.Instance.Macros.Save();
                }
            }
        }

        private void DrawToolbar()
        {
            // Add button
            if (ImGui.Button("Add"))
            {
                string baseName = "New Macro";
                string macroName = baseName;
                int counter = 1;

                // Auto-increment if duplicate
                while (World.Instance.Macros.GetAllMacros().Any(m => m.Name == macroName))
                {
                    macroName = $"{baseName} {counter++}";
                }

                var newMacro = new Macro(macroName);
                newMacro.Items = new MacroObject(MacroType.None, MacroSubType.MSC_NONE);
                World.Instance.Macros.PushToBack(newMacro);
                _selectedMacro = newMacro;
                InvalidateMacroCache();
                MarkDirty();
            }

            ImGui.SameLine();

            // Move Up button
            if (ImGui.Button("Move Up"))
            {
                if (_selectedMacro != null)
                {
                    World.Instance.Macros.MoveMacroUp(_selectedMacro);
                    InvalidateMacroCache();
                    MarkDirty();
                }
            }

            ImGui.SameLine();

            // Move Down button
            if (ImGui.Button("Move Down"))
            {
                if (_selectedMacro != null)
                {
                    World.Instance.Macros.MoveMacroDown(_selectedMacro);
                    InvalidateMacroCache();
                    MarkDirty();
                }
            }

            ImGui.SameLine();

            // Import button
            if (ImGui.Button("Import"))
            {
                string xml = Utility.Clipboard.GetClipboardText();

                if(xml.NotNullNotEmpty() && World.Instance.Macros.ImportFromXml(xml))
                {
                    InvalidateMacroCache();
                    return;
                }

                GameActions.Print("Your clipboard does not have a valid macro export copied.", Constants.HUE_ERROR);
            }
            ImGuiComponents.Tooltip("Import macros from your clipboard, must have a valid export copied.");

            List<Macro> allMacros = World.Instance.Macros.GetAllMacros();
            if (allMacros.Count > 0)
            {
                ImGui.SameLine();

                // Export button
                if (ImGui.Button("Export"))
                {
                    World.Instance.Macros.GetXmlExport()?.CopyToClipboard();
                    GameActions.Print($"Exported {allMacros.Count} macro(s) to your clipboard!", Constants.HUE_SUCCESS);
                }
                ImGuiComponents.Tooltip("Export all macros to your clipboard.");
            }

            ImGui.SameLine();

            ImGui.SameLine();
            ImGui.SetNextItemWidth(150);
            if (ImGui.InputTextWithHint("##Filter", "Filter...", ref _filterText, 256))
            {
                _filteredMacroList = null;
            }
        }

        private void DrawMacroList()
        {
            _cachedMacroList ??= World.Instance.Macros.GetAllMacros();

            // Apply filter
            List<Macro> displayList;
            if (!string.IsNullOrWhiteSpace(_filterText))
            {
                _filteredMacroList ??= _cachedMacroList.Where(m =>
                    m.Name.Contains(_filterText, StringComparison.OrdinalIgnoreCase) ||
                    GetHotkeyString(m).Contains(_filterText, StringComparison.OrdinalIgnoreCase)
                ).ToList();
                displayList = _filteredMacroList;
            }
            else
            {
                _filteredMacroList = null;
                displayList = _cachedMacroList;
            }

            if (ImGui.BeginChild("MacroListChild", new Vector2(0, 0), ImGuiChildFlags.Borders))
            {
                if (ImGui.BeginTable("MacrosTable", 2,
                    ImGuiTableFlags.Borders |
                    ImGuiTableFlags.RowBg |
                    ImGuiTableFlags.ScrollY))
                {
                    ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableSetupColumn("Hotkey", ImGuiTableColumnFlags.WidthFixed, 120);
                    ImGui.TableSetupScrollFreeze(0, 1);
                    ImGui.TableHeadersRow();

                    for (int i = 0; i < displayList.Count; i++)
                    {
                        Macro macro = displayList[i];

                        bool isSelected = _selectedMacro == macro;

                        ImGui.TableNextRow();
                        ImGui.PushID(i);

                        ImGui.TableSetColumnIndex(0);

                        if (ImGui.Selectable(macro.Name, isSelected,
                            ImGuiSelectableFlags.SpanAllColumns))
                        {
                            _selectedMacro = macro;
                            _selectedMacroIndex = i;
                        }

                        ImGui.TableSetColumnIndex(1);
                        ImGui.TextDisabled(GetHotkeyString(macro));

                        ImGui.PopID();
                    }

                    ImGui.EndTable();
                }
            }
            ImGui.EndChild();
        }

        private void DrawMacroEditor()
        {
            if (_selectedMacro == null)
            {
                ImGui.TextDisabled("Select a macro to edit");
                return;
            }

            Macro macro = _selectedMacro;

            // Macro Name
            ImGui.Text("Macro Name:");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(200);
            string macroName = macro.Name;
            if (ImGui.InputText("##MacroName", ref macroName, 256))
            {
                macro.Name = macroName;
                MarkDirty();
            }

            ImGui.Spacing();

            // Hotkey Capture
            DrawHotkeyCapture(macro);

            // Create Macro Button
            ImGui.SameLine();
            if (ImGui.Button("Create Macro Button"))
            {
                // Dispose any existing MacroButtonGump for this macro
                foreach (Gump gump in UIManager.Gumps)
                {
                    if (gump is MacroButtonGump mbg && mbg.TheMacro == _selectedMacro)
                    {
                        mbg.Dispose();
                        break;
                    }
                }

                var macroButtonGump = new MacroButtonGump(World.Instance, _selectedMacro, 0, 0);
                macroButtonGump.CenterXInViewPort();
                macroButtonGump.CenterYInViewPort();
                UIManager.Add(macroButtonGump);
            }
            ImGuiComponents.Tooltip("Create a draggable macro button for the selected macro.");

            ImGui.Separator();
            ImGui.Spacing();

            // Actions List
            DrawActionsList(macro);

            // Delete button
            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.8f, 0.2f, 0.2f, 1.0f));
            if (ImGui.Button("Delete Macro"))
            {
                _showDeleteMacroDialog = true;
            }
            ImGui.PopStyleColor();
            ImGuiComponents.Tooltip("Delete this macro");
        }

        private void DrawHotkeyCapture(Macro macro)
        {
            ImGui.Text("Hotkey:");
            ImGui.SameLine();

            string hotkeyDisplay = _isListeningForHotkey
                ? GetCaptureDisplayString()
                : GetHotkeyString(macro);

            ImGui.SetNextItemWidth(250);
            if (_isListeningForHotkey)
            {
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(1.0f, 1.0f, 0.0f, 0.4f));
                if (ImGui.Button(hotkeyDisplay + "##HotkeyButton"))
                {
                    // Still listening
                }
                ImGui.PopStyleColor();
            }
            else
            {
                if (ImGui.Button(hotkeyDisplay + "##HotkeyButton"))
                {
                    _isListeningForHotkey = true;
                    _capturedKey = SDL.SDL_Keycode.SDLK_UNKNOWN;
                    _capturedMod = SDL.SDL_Keymod.SDL_KMOD_NONE;
                    _capturedButtons = null;
                    _capturedMouseButton = MouseButtonType.None;
                    ProfileManager.CurrentProfile.DisableHotkeys = true;
                }
            }

            if (_isListeningForHotkey)
            {
                ImGui.SameLine();
                if (ImGui.Button("Cancel##HotkeyCancel"))
                {
                    _isListeningForHotkey = false;
                    ProfileManager.CurrentProfile.DisableHotkeys = false;
                }

                // Show captured key if any
                if (_capturedKey != SDL.SDL_Keycode.SDLK_UNKNOWN || (_capturedButtons != null && _capturedButtons.Length > 0) || _capturedMouseButton != MouseButtonType.None)
                {
                    ImGui.SameLine();
                    if (ImGui.Button("Apply##HotkeyApply"))
                    {
                        ApplyCapturedHotkey();
                        _isListeningForHotkey = false;
                        ProfileManager.CurrentProfile.DisableHotkeys = false;
                    }
                }
            }
            else
            {
                ImGui.SameLine();
                if (ImGui.Button("Clear##HotkeyClear"))
                {
                    macro.Key = SDL.SDL_Keycode.SDLK_UNKNOWN;
                    macro.MouseButton = MouseButtonType.None;
                    macro.WheelScroll = false;
                    macro.ControllerButtons = null;
                    macro.Alt = macro.Ctrl = macro.Shift = false;
                    MarkDirty();
                }
            }
        }

        private void DrawActionsList(Macro macro)
        {
            ImGui.Text("Actions:");

            if (ImGui.BeginChild("ActionsListScrollable",
                new Vector2(450, -30),
                ImGuiChildFlags.Borders))
            {
                var action = (MacroObject)macro.Items;
                int actionIndex = 0;

                while (action != null && action.Code != MacroType.None)
                {
                    ImGui.PushID(actionIndex);

                    // Draw inline editable action
                    DrawActionRow(macro, action, actionIndex);

                    ImGui.PopID();

                    action = (MacroObject)action.Next;
                    actionIndex++;
                }
            }
            ImGui.EndChild();

            // Action management buttons
            if (ImGui.Button("Add Action"))
            {
                // Add a new None action that can be edited inline
                var newAction = new MacroObject(MacroType.Say, MacroSubType.MSC_NONE);

                // Find the last action (before None terminator)
                var scanAction = (MacroObject)macro.Items;
                MacroObject lastAction = null;

                while (scanAction != null && scanAction.Code != MacroType.None)
                {
                    lastAction = scanAction;
                    scanAction = (MacroObject)scanAction.Next;
                }

                // Insert new action before the None terminator
                if (lastAction != null)
                {
                    macro.Insert(lastAction, newAction);
                }
                else
                {
                    // First action - replace the None placeholder
                    macro.Items = newAction;
                    newAction.Next = new MacroObject(MacroType.None, MacroSubType.MSC_NONE);
                }

                MarkDirty();
            }
        }

        private void DrawActionRow(Macro macro, MacroObject action, int index)
        {
            ImGui.Text($"{index + 1}.");
            ImGui.SameLine();

            // Main action type combo - use sorted/filtered mapping
            ImGui.SetNextItemWidth(150);
            int displayIndex = _macroTypeToDisplayIndex.TryGetValue(action.Code, out int idx) ? idx : 0;
            if (ImGui.Combo($"##ActionType{index}", ref displayIndex, _sortedMacroTypeNames, _sortedMacroTypeNames.Length))
            {
                MacroType newType = _sortedMacroTypeValues[displayIndex];
                if (newType != action.Code)
                {
                    // Create a new MacroObject with proper SubMenuType via Macro.Create()
                    MacroObject newAction = Macro.Create(newType);
                    macro.Insert(action, newAction);
                    macro.Remove(action);
                    MarkDirty();
                    // Don't continue drawing - the action has been replaced
                    return;
                }
            }

            ImGui.SameLine();

            // SubMenuType 1: Dropdown for sub-options
            if (action.SubMenuType == 1)
            {
                int count = 0;
                int offset = 0;
                string[] subNames;
                int[] customSpellSubCodes = null;
                int currentSubIndex;

                if (action.Code == MacroType.CastCustomSpell)
                {
                    Macro.BuildCastCustomSpellPicker(action.SubCode, out subNames, out customSpellSubCodes, out currentSubIndex);
                }
                else
                {
                    Macro.GetBoundByCode(action.Code, ref count, ref offset);
                    subNames = new string[count];
                    for (int i = 0; i < count; i++)
                    {
                        subNames[i] = ((MacroSubType)(i + offset)).ToString();
                    }
                    currentSubIndex = (int)action.SubCode - offset;
                    if (currentSubIndex < 0 || currentSubIndex >= count)
                        currentSubIndex = 0;
                }

                ImGui.SetNextItemWidth(150);
                if (ImGui.Combo($"##ActionSubType{index}", ref currentSubIndex, subNames, subNames.Length))
                {
                    if (customSpellSubCodes != null)
                    {
                        if (currentSubIndex >= 0 && currentSubIndex < customSpellSubCodes.Length)
                            action.SubCode = (MacroSubType)customSpellSubCodes[currentSubIndex];
                    }
                    else
                    {
                        action.SubCode = (MacroSubType)(currentSubIndex + offset);
                    }
                    MarkDirty();
                }
                ImGui.SameLine();
            }
            // SubMenuType 2: Text input
            else if (action.SubMenuType == 2)
            {
                ImGui.SetNextItemWidth(200);
                string text = action.HasString() ? ((MacroObjectString)action).Text : "";
                if (ImGui.InputText($"##ActionText{index}", ref text, 256))
                {
                    // Need to replace with MacroObjectString if it isn't already
                    if (!action.HasString())
                    {
                        // Replace this action with a MacroObjectString version
                        var newAction = new MacroObjectString(action.Code, action.SubCode, text);
                        newAction.Next = action.Next;

                        // Find previous action to update its Next pointer
                        var scanAction = (MacroObject)macro.Items;
                        MacroObject prevAction = null;
                        while (scanAction != null && scanAction != action)
                        {
                            prevAction = scanAction;
                            scanAction = (MacroObject)scanAction.Next;
                        }

                        if (prevAction != null)
                        {
                            prevAction.Next = newAction;
                        }
                        else
                        {
                            macro.Items = newAction;
                        }
                    }
                    else
                    {
                        ((MacroObjectString)action).Text = text;
                    }
                    MarkDirty();
                }
                ImGui.SameLine();
            }
            else
            {
                ImGui.SameLine();
            }

            // Remove button
            if (ImGui.Button($"X##Remove{index}"))
            {
                macro.Remove(action);
                MarkDirty();
            }
        }

        private void DrawDialogs()
        {
            // Delete Confirmation Dialog
            if (_showDeleteMacroDialog)
            {
                ImGui.OpenPopup("Delete Macro?");
                _showDeleteMacroDialog = false;
            }

            bool showDeleteModal = true;
            if (ImGui.BeginPopupModal("Delete Macro?", ref showDeleteModal,
                ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.Text($"Are you sure you want to delete '{_selectedMacro?.Name}'?");
                ImGui.Separator();

                if (ImGui.Button("Delete", new Vector2(120, 0)))
                {
                    World.Instance.Macros.Remove(_selectedMacro);
                    _selectedMacro = null;
                    _selectedMacroIndex = -1;
                    InvalidateMacroCache();
                    MarkDirty();
                    ImGui.CloseCurrentPopup();
                }

                ImGui.SameLine();
                if (ImGui.Button("Cancel", new Vector2(120, 0)))
                {
                    ImGui.CloseCurrentPopup();
                }

                ImGui.EndPopup();
            }
        }

        public override void Dispose()
        {
            if (_isListeningForHotkey)
                ProfileManager.CurrentProfile.DisableHotkeys = false;
            base.Dispose();
        }

        #region Helper Methods

        private string GetHotkeyString(Macro macro)
        {
            if (macro.ControllerButtons != null && macro.ControllerButtons.Length > 0)
                return Controller.GetButtonNames(macro.ControllerButtons);

            if (macro.Key != SDL.SDL_Keycode.SDLK_UNKNOWN)
            {
                SDL.SDL_Keymod mod = SDL.SDL_Keymod.SDL_KMOD_NONE;
                if (macro.Alt) mod |= SDL.SDL_Keymod.SDL_KMOD_ALT;
                if (macro.Ctrl) mod |= SDL.SDL_Keymod.SDL_KMOD_CTRL;
                if (macro.Shift) mod |= SDL.SDL_Keymod.SDL_KMOD_SHIFT;
                return KeysTranslator.TryGetKey(macro.Key, mod);
            }

            if (macro.MouseButton != MouseButtonType.None)
            {
                SDL.SDL_Keymod mod = SDL.SDL_Keymod.SDL_KMOD_NONE;
                if (macro.Alt) mod |= SDL.SDL_Keymod.SDL_KMOD_ALT;
                if (macro.Ctrl) mod |= SDL.SDL_Keymod.SDL_KMOD_CTRL;
                if (macro.Shift) mod |= SDL.SDL_Keymod.SDL_KMOD_SHIFT;
                return KeysTranslator.GetMouseButton(macro.MouseButton, mod);
            }

            if (macro.WheelScroll)
            {
                SDL.SDL_Keymod mod = SDL.SDL_Keymod.SDL_KMOD_NONE;
                if (macro.Alt) mod |= SDL.SDL_Keymod.SDL_KMOD_ALT;
                if (macro.Ctrl) mod |= SDL.SDL_Keymod.SDL_KMOD_CTRL;
                if (macro.Shift) mod |= SDL.SDL_Keymod.SDL_KMOD_SHIFT;
                return KeysTranslator.GetMouseWheel(macro.WheelUp, mod);
            }

            return "None";
        }

        private string GetCaptureDisplayString()
        {
            var parts = new List<string>();

            // Add modifiers
            if ((_capturedMod & SDL.SDL_Keymod.SDL_KMOD_CTRL) != 0)
                parts.Add("Ctrl");
            if ((_capturedMod & SDL.SDL_Keymod.SDL_KMOD_ALT) != 0)
                parts.Add("Alt");
            if ((_capturedMod & SDL.SDL_Keymod.SDL_KMOD_SHIFT) != 0)
                parts.Add("Shift");

            // Add key if captured
            if (_capturedKey != SDL.SDL_Keycode.SDLK_UNKNOWN)
            {
                parts.Add(KeysTranslator.TryGetKey(_capturedKey, SDL.SDL_Keymod.SDL_KMOD_NONE));
            }

            // Add controller buttons if captured
            if (_capturedButtons != null && _capturedButtons.Length > 0)
            {
                parts.Add(Controller.GetButtonNames(_capturedButtons));
            }

            // Add mouse button if captured
            if (_capturedMouseButton != MouseButtonType.None)
            {
                parts.Add(_capturedMouseButton.ToString());
            }

            // Return formatted string or "Listening..." if nothing captured yet
            return parts.Count > 0 ? string.Join(" + ", parts) : "Listening...";
        }

        private void MarkDirty() => _saveTimer = SAVE_DELAY;

        private void InvalidateMacroCache()
        {
            _cachedMacroList = null;
            _filteredMacroList = null;
        }

        #endregion

        #region Hotkey Capture

        private static Dictionary<ImGuiKey, SDL.SDL_Keycode> _keyMap = new()
        {
            { ImGuiKey.GraveAccent, SDL.SDL_Keycode.SDLK_GRAVE },
            { ImGuiKey.F1, SDL.SDL_Keycode.SDLK_F1 },
            { ImGuiKey.F2, SDL.SDL_Keycode.SDLK_F2 },
            { ImGuiKey.F3, SDL.SDL_Keycode.SDLK_F3 },
            { ImGuiKey.F4, SDL.SDL_Keycode.SDLK_F4 },
            { ImGuiKey.F5, SDL.SDL_Keycode.SDLK_F5 },
            { ImGuiKey.F6, SDL.SDL_Keycode.SDLK_F6 },
            { ImGuiKey.F7, SDL.SDL_Keycode.SDLK_F7 },
            { ImGuiKey.F8, SDL.SDL_Keycode.SDLK_F8 },
            { ImGuiKey.F9, SDL.SDL_Keycode.SDLK_F9 },
            { ImGuiKey.F10, SDL.SDL_Keycode.SDLK_F10 },
            { ImGuiKey.F11, SDL.SDL_Keycode.SDLK_F11 },
            { ImGuiKey.F12, SDL.SDL_Keycode.SDLK_F12 },
            { ImGuiKey.A, SDL.SDL_Keycode.SDLK_A },
            { ImGuiKey.B, SDL.SDL_Keycode.SDLK_B },
            { ImGuiKey.C, SDL.SDL_Keycode.SDLK_C },
            { ImGuiKey.D, SDL.SDL_Keycode.SDLK_D },
            { ImGuiKey.E, SDL.SDL_Keycode.SDLK_E },
            { ImGuiKey.F, SDL.SDL_Keycode.SDLK_F },
            { ImGuiKey.G, SDL.SDL_Keycode.SDLK_G },
            { ImGuiKey.H, SDL.SDL_Keycode.SDLK_H },
            { ImGuiKey.I, SDL.SDL_Keycode.SDLK_I },
            { ImGuiKey.J, SDL.SDL_Keycode.SDLK_J },
            { ImGuiKey.K, SDL.SDL_Keycode.SDLK_K },
            { ImGuiKey.L, SDL.SDL_Keycode.SDLK_L },
            { ImGuiKey.M, SDL.SDL_Keycode.SDLK_M },
            { ImGuiKey.N, SDL.SDL_Keycode.SDLK_N },
            { ImGuiKey.O, SDL.SDL_Keycode.SDLK_O },
            { ImGuiKey.P, SDL.SDL_Keycode.SDLK_P },
            { ImGuiKey.Q, SDL.SDL_Keycode.SDLK_Q },
            { ImGuiKey.R, SDL.SDL_Keycode.SDLK_R },
            { ImGuiKey.S, SDL.SDL_Keycode.SDLK_S },
            { ImGuiKey.T, SDL.SDL_Keycode.SDLK_T },
            { ImGuiKey.U, SDL.SDL_Keycode.SDLK_U },
            { ImGuiKey.V, SDL.SDL_Keycode.SDLK_V },
            { ImGuiKey.W, SDL.SDL_Keycode.SDLK_W },
            { ImGuiKey.X, SDL.SDL_Keycode.SDLK_X },
            { ImGuiKey.Y, SDL.SDL_Keycode.SDLK_Y },
            { ImGuiKey.Z, SDL.SDL_Keycode.SDLK_Z },
            { ImGuiKey._1, SDL.SDL_Keycode.SDLK_1 },
            { ImGuiKey._2, SDL.SDL_Keycode.SDLK_2 },
            { ImGuiKey._3, SDL.SDL_Keycode.SDLK_3 },
            { ImGuiKey._4, SDL.SDL_Keycode.SDLK_4 },
            { ImGuiKey._5, SDL.SDL_Keycode.SDLK_5 },
            { ImGuiKey._6, SDL.SDL_Keycode.SDLK_6 },
            { ImGuiKey._7, SDL.SDL_Keycode.SDLK_7 },
            { ImGuiKey._8, SDL.SDL_Keycode.SDLK_8 },
            { ImGuiKey._9, SDL.SDL_Keycode.SDLK_9 },
            { ImGuiKey._0, SDL.SDL_Keycode.SDLK_0 }
        };

        private static Dictionary<ImGuiKey, SDL.SDL_Keycode> _keypadMap = new()
        {
            { ImGuiKey.Keypad1, SDL.SDL_Keycode.SDLK_KP_1 },
            { ImGuiKey.Keypad2, SDL.SDL_Keycode.SDLK_KP_2 },
            { ImGuiKey.Keypad3, SDL.SDL_Keycode.SDLK_KP_3 },
            { ImGuiKey.Keypad4, SDL.SDL_Keycode.SDLK_KP_4 },
            { ImGuiKey.Keypad5, SDL.SDL_Keycode.SDLK_KP_5 },
            { ImGuiKey.Keypad6, SDL.SDL_Keycode.SDLK_KP_6 },
            { ImGuiKey.Keypad7, SDL.SDL_Keycode.SDLK_KP_7 },
            { ImGuiKey.Keypad8, SDL.SDL_Keycode.SDLK_KP_8 },
            { ImGuiKey.Keypad9, SDL.SDL_Keycode.SDLK_KP_9 },
            { ImGuiKey.Keypad0, SDL.SDL_Keycode.SDLK_KP_0 }
        };

        private void CaptureCurrentInput()
        {
            // Capture modifier keys from static Keyboard class
            _capturedMod = SDL.SDL_Keymod.SDL_KMOD_NONE;
            if (Keyboard.Ctrl)
                _capturedMod |= SDL.SDL_Keymod.SDL_KMOD_CTRL;
            if (Keyboard.Alt)
                _capturedMod |= SDL.SDL_Keymod.SDL_KMOD_ALT;
            if (Keyboard.Shift)
                _capturedMod |= SDL.SDL_Keymod.SDL_KMOD_SHIFT;

            // Capture keyboard input from regular keys
            foreach (KeyValuePair<ImGuiKey, SDL.SDL_Keycode> kvp in _keyMap)
            {
                if (ImGui.IsKeyPressed(kvp.Key))
                {
                    _capturedKey = kvp.Value;
                    break;
                }
            }

            // Also check keypad keys separately
            if (_capturedKey == SDL.SDL_Keycode.SDLK_UNKNOWN)
            {
                foreach (KeyValuePair<ImGuiKey, SDL.SDL_Keycode> kvp in _keypadMap)
                {
                    if (ImGui.IsKeyPressed(kvp.Key))
                    {
                        _capturedKey = kvp.Value;
                        break;
                    }
                }
            }

            // Capture gamepad button input
            SDL.SDL_GamepadButton[] pressedButtons = Controller.PressedButtons();
            if (pressedButtons != null && pressedButtons.Length > 0)
            {
                _capturedButtons = pressedButtons;
            }

            // Capture mouse button input (Middle, Right, XButton1, XButton2)
            // Left button is excluded as it's used for UI interaction
            // Use ImGui's mouse detection to capture clicks within the ImGui window
            if (ImGui.IsMouseClicked(ImGuiMouseButton.Middle))
            {
                _capturedMouseButton = MouseButtonType.Middle;
            }
            else if (ImGui.IsMouseClicked(ImGuiMouseButton.Right))
            {
                _capturedMouseButton = MouseButtonType.Right;
            }
            // Also check the Mouse class for game window clicks and XButtons (ImGui doesn't expose XButton)
            else if (Mouse.MButtonPressed && _capturedMouseButton == MouseButtonType.None)
            {
                _capturedMouseButton = MouseButtonType.Middle;
            }
            else if (Mouse.RButtonPressed && _capturedMouseButton == MouseButtonType.None)
            {
                _capturedMouseButton = MouseButtonType.Right;
            }
            else if (Mouse.XButtonPressed)
            {
                // Note: XButton1 and XButton2 share the same pressed state in the Mouse class
                // Default to XButton1, but users can manually differentiate if needed
                _capturedMouseButton = MouseButtonType.XButton1;
            }

            // Check for Escape to cancel
            if (ImGui.IsKeyPressed(ImGuiKey.Escape))
            {
                _isListeningForHotkey = false;
                _capturedKey = SDL.SDL_Keycode.SDLK_UNKNOWN;
                _capturedButtons = null;
                _capturedMouseButton = MouseButtonType.None;
            }
        }

        private void ApplyCapturedHotkey()
        {
            if (_selectedMacro == null) return;

            bool ctrl = (_capturedMod & SDL.SDL_Keymod.SDL_KMOD_CTRL) != 0;
            bool alt = (_capturedMod & SDL.SDL_Keymod.SDL_KMOD_ALT) != 0;
            bool shift = (_capturedMod & SDL.SDL_Keymod.SDL_KMOD_SHIFT) != 0;

            // Check for duplicates based on what was captured
            if (_capturedMouseButton != MouseButtonType.None)
            {
                // Mouse button was captured - check for mouse button duplicates
                Macro existing = World.Instance.Macros.FindMacro(_capturedMouseButton, alt, ctrl, shift);
                if (existing != null && existing != _selectedMacro)
                {
                    GameActions.Print(World.Instance,
                        $"Hotkey already used by macro: {existing.Name}", 32);
                    return;
                }

                // Apply mouse button to macro
                _selectedMacro.Key = SDL.SDL_Keycode.SDLK_UNKNOWN;
                _selectedMacro.MouseButton = _capturedMouseButton;
                _selectedMacro.WheelScroll = false;
                _selectedMacro.ControllerButtons = null;
            }
            else if (_capturedKey != SDL.SDL_Keycode.SDLK_UNKNOWN)
            {
                // Keyboard key was captured - check for key duplicates
                Macro existing = World.Instance.Macros.FindMacro(_capturedKey, alt, ctrl, shift);
                if (existing != null && existing != _selectedMacro)
                {
                    GameActions.Print(World.Instance,
                        $"Hotkey already used by macro: {existing.Name}", 32);
                    return;
                }

                // Apply keyboard key to macro
                _selectedMacro.Key = _capturedKey;
                _selectedMacro.MouseButton = MouseButtonType.None;
                _selectedMacro.WheelScroll = false;
                _selectedMacro.ControllerButtons = _capturedButtons;
            }
            else if (_capturedButtons != null && _capturedButtons.Length > 0)
            {
                // Controller buttons were captured
                _selectedMacro.Key = SDL.SDL_Keycode.SDLK_UNKNOWN;
                _selectedMacro.MouseButton = MouseButtonType.None;
                _selectedMacro.WheelScroll = false;
                _selectedMacro.ControllerButtons = _capturedButtons;
            }

            _selectedMacro.Ctrl = ctrl;
            _selectedMacro.Alt = alt;
            _selectedMacro.Shift = shift;

            MarkDirty();
        }

        #endregion
    }
}
