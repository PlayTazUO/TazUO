using ImGuiNET;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using System.Numerics;
using System.Collections.Generic;
using System;
using Microsoft.Xna.Framework.Input;
using SDL3;

namespace ClassicUO.Game.UI.ImGuiControls
{
    public class SpellBarWindow : SingletonImGuiWindow<SpellBarWindow>
    {
        private Profile profile;
        private bool enableSpellBar;
        private bool showHotkeys;
        private bool[] showHotkeyEdit = new bool[10];
        private string[] hotkeyLabels = new string[10];
        private bool[] isListeningForHotkey = new bool[10];
        private string newPresetName = "";
        private bool showPresetSaveDialog = false;
        private bool showPresetLoadDialog = false;
        private int listeningSlot = -1;
        private SDL.SDL_Keycode capturedKey = SDL.SDL_Keycode.SDLK_UNKNOWN;
        private SDL.SDL_Keymod capturedMod = SDL.SDL_Keymod.SDL_KMOD_NONE;
        private List<SDL.SDL_GamepadButton> capturedButtons = new List<SDL.SDL_GamepadButton>();

        private SpellBarWindow() : base("Spell Bar")
        {
            WindowFlags = ImGuiWindowFlags.AlwaysAutoResize;
            profile = ProfileManager.CurrentProfile;

            enableSpellBar = SpellBarManager.IsEnabled();
            showHotkeys = profile.SpellBar_ShowHotkeys;

            UpdateHotkeyLabels();
        }

        private void UpdateHotkeyLabels()
        {
            SDL.SDL_GamepadButton[][] controllerHotkeys = SpellBarManager.GetControllerButtons();
            SDL.SDL_Keycode[] hotkeys = SpellBarManager.GetHotKeys();
            SDL.SDL_Keymod[] keymods = SpellBarManager.GetModKeys();

            for (int i = 0; i < 10; i++)
            {
                string hotkeyDisplay = "";
                if (i < hotkeys.Length && i < keymods.Length)
                {
                    hotkeyDisplay = KeysTranslator.TryGetKey(hotkeys[i], keymods[i]);
                }
                if (i < controllerHotkeys.Length)
                {
                    string controllerDisplay = Controller.GetButtonNames(controllerHotkeys[i]);
                    if (!string.IsNullOrEmpty(controllerDisplay))
                    {
                        if (!string.IsNullOrEmpty(hotkeyDisplay))
                            hotkeyDisplay += " / ";
                        hotkeyDisplay += controllerDisplay;
                    }
                }

                hotkeyLabels[i] = string.IsNullOrEmpty(hotkeyDisplay) ? "None" : hotkeyDisplay;
            }
        }

        private void StartListeningForHotkey(int slot)
        {
            // Reset all listening states
            for (int i = 0; i < 10; i++)
            {
                isListeningForHotkey[i] = false;
            }

            isListeningForHotkey[slot] = true;
            listeningSlot = slot;
            capturedKey = SDL.SDL_Keycode.SDLK_UNKNOWN;
            capturedMod = SDL.SDL_Keymod.SDL_KMOD_NONE;
            capturedButtons.Clear();
        }

        private void StopListeningForHotkey()
        {
            for (int i = 0; i < 10; i++)
            {
                isListeningForHotkey[i] = false;
            }
            listeningSlot = -1;
        }

        private static Dictionary<ImGuiKey, SDL.SDL_Keycode> keyMap = new()
        {
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

        // Mapping for keypad number keys (checked separately to support both regular and keypad variants)
        private static Dictionary<ImGuiKey, SDL.SDL_Keycode> keypadMap = new()
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
            if (listeningSlot < 0) return;

            // Capture modifier keys from ImGui
            capturedMod = SDL.SDL_Keymod.SDL_KMOD_NONE;
            if (ImGui.GetIO().KeyCtrl)
                capturedMod |= SDL.SDL_Keymod.SDL_KMOD_CTRL;
            if (ImGui.GetIO().KeyAlt)
                capturedMod |= SDL.SDL_Keymod.SDL_KMOD_ALT;
            if (ImGui.GetIO().KeyShift)
                capturedMod |= SDL.SDL_Keymod.SDL_KMOD_SHIFT;

            // Capture keyboard input from regular keys
            foreach (KeyValuePair<ImGuiKey, SDL.SDL_Keycode> kvp in keyMap)
            {
                if (ImGui.IsKeyPressed(kvp.Key))
                {
                    capturedKey = kvp.Value;
                    break;
                }
            }

            // Also check keypad keys separately to support both regular and keypad number keys
            if (capturedKey == SDL.SDL_Keycode.SDLK_UNKNOWN)
            {
                foreach (KeyValuePair<ImGuiKey, SDL.SDL_Keycode> kvp in keypadMap)
                {
                    if (ImGui.IsKeyPressed(kvp.Key))
                    {
                        capturedKey = kvp.Value;
                        break;
                    }
                }
            }

            // Capture gamepad button input using TazUO's existing controller system
            SDL.SDL_GamepadButton[] pressedButtons = Controller.PressedButtons();
            if (pressedButtons.Length > 0)
            {
                capturedButtons.Clear();
                capturedButtons.AddRange(pressedButtons);
            }
        }

        private void ApplyCapturedHotkey()
        {
            if (listeningSlot < 0) return;

            // Apply if we have either a keyboard key or controller buttons
            if (capturedKey != SDL.SDL_Keycode.SDLK_UNKNOWN || capturedButtons.Count > 0)
            {
                // Apply the captured hotkey
                SpellBarManager.SetButtons(listeningSlot, capturedMod, capturedKey, capturedButtons.ToArray());
                UpdateHotkeyLabels();

                // Refresh the actual SpellBar gump instances to update their hotkey labels
                Game.UI.Gumps.SpellBar.SpellBar.Instance?.SetupHotkeyLabels();

                StopListeningForHotkey();
            }
        }

        public override void DrawContent()
        {
            if (profile == null)
            {
                ImGui.Text("Profile not loaded");
                return;
            }

            // Handle key capture if we're listening for hotkeys
            if (listeningSlot >= 0)
            {
                CaptureCurrentInput();
            }

            ImGui.Spacing();

            // Main enable checkbox
            if (ImGui.Checkbox("Enable spellbar", ref enableSpellBar))
            {
                if (SpellBarManager.ToggleEnabled())
                {
                    UIManager.Add(new Game.UI.Gumps.SpellBar.SpellBar(Client.Game.UO.World));
                }
                else
                {
                    Game.UI.Gumps.SpellBar.SpellBar.Instance?.Dispose();
                }
            }
            ImGuiComponents.Tooltip("Enable or disable the spell bar feature");

            ImGui.Spacing();

            // Show hotkeys checkbox
            if (ImGui.Checkbox("Display hotkeys on spellbar", ref showHotkeys))
            {
                profile.SpellBar_ShowHotkeys = showHotkeys;
                Game.UI.Gumps.SpellBar.SpellBar.Instance?.SetupHotkeyLabels();
            }
            ImGuiComponents.Tooltip("Show hotkey assignments on the spell bar buttons");

            ImGui.Spacing();
            ImGui.SeparatorText("Row Management:");

            // Row management buttons
            if (ImGui.Button("Add Row"))
            {
                SpellBarManager.SpellBarRows.Add(new SpellBarRow());
                Game.UI.Gumps.SpellBar.SpellBar.Instance?.Build();
            }
            ImGuiComponents.Tooltip("Add a new spell bar row");

            ImGui.SameLine();

            if (ImGui.Button("Remove Row"))
            {
                if (SpellBarManager.SpellBarRows.Count > 1) // Make sure to always leave one row
                    SpellBarManager.SpellBarRows.RemoveAt(SpellBarManager.SpellBarRows.Count - 1);
                Game.UI.Gumps.SpellBar.SpellBar.Instance?.Build();
            }
            ImGuiComponents.Tooltip("Remove the last row. If you have 5 rows, row 5 will be removed.");

            ImGui.Spacing();
            ImGui.SeparatorText("Hotkey Configuration:");

            // Create a table for better layout
            if (ImGui.BeginTable("HotkeyTable", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn("Slot", ImGuiTableColumnFlags.WidthFixed, 60);
                ImGui.TableSetupColumn("Current Hotkey", ImGuiTableColumnFlags.WidthFixed, 150);
                ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthFixed, 120);
                ImGui.TableHeadersRow();

                for (int i = 0; i < 10; i++)
                {
                    ImGui.TableNextRow();

                    ImGui.TableNextColumn();
                    ImGui.Text($"Slot {i}");

                    ImGui.TableNextColumn();
                    if (isListeningForHotkey[i])
                    {
                        // Show what we've captured so far
                        string captureText = "Press a key or controller button...";
                        if (capturedKey != SDL.SDL_Keycode.SDLK_UNKNOWN || capturedButtons.Count > 0)
                        {
                            string parts = "";

                            // Add keyboard part
                            if (capturedKey != SDL.SDL_Keycode.SDLK_UNKNOWN)
                            {
                                string modText = "";
                                if ((capturedMod & SDL.SDL_Keymod.SDL_KMOD_CTRL) != 0) modText += "Ctrl+";
                                if ((capturedMod & SDL.SDL_Keymod.SDL_KMOD_ALT) != 0) modText += "Alt+";
                                if ((capturedMod & SDL.SDL_Keymod.SDL_KMOD_SHIFT) != 0) modText += "Shift+";
                                parts = $"{modText}{capturedKey}";
                            }

                            // Add controller part
                            if (capturedButtons.Count > 0)
                            {
                                string buttonNames = Controller.GetButtonNames(capturedButtons.ToArray());
                                if (!string.IsNullOrEmpty(parts)) parts += " / ";
                                parts += buttonNames;
                            }

                            captureText = $"Captured: {parts}";
                        }

                        ImGui.TextColored(new Vector4(1, 1, 0, 1), captureText);

                        // Escape cancels
                        if (ImGui.IsKeyPressed(ImGuiKey.Escape))
                        {
                            StopListeningForHotkey();
                        }
                    }
                    else
                    {
                        ImGui.Text(hotkeyLabels[i]);
                    }

                    ImGui.TableNextColumn();
                    if (isListeningForHotkey[i])
                    {
                        if (ImGui.Button($"Apply##slot{i}"))
                        {
                            ApplyCapturedHotkey();
                        }
                        ImGui.SameLine();
                        if (ImGui.Button($"Cancel##slot{i}"))
                        {
                            StopListeningForHotkey();
                        }
                    }
                    else
                    {
                        if (ImGui.Button($"Set##slot{i}"))
                        {
                            StartListeningForHotkey(i);
                        }
                        ImGui.SameLine();
                        if (ImGui.Button($"Clear##slot{i}"))
                        {
                            // Clear the hotkey assignment (pass empty array instead of null to clear controller bindings)
                            SpellBarManager.SetButtons(i, SDL.SDL_Keymod.SDL_KMOD_NONE, SDL.SDL_Keycode.SDLK_UNKNOWN, new SDL.SDL_GamepadButton[0]);
                            UpdateHotkeyLabels();

                            // Refresh the actual SpellBar gump instances to update their hotkey labels
                            Game.UI.Gumps.SpellBar.SpellBar.Instance?.SetupHotkeyLabels();
                        }
                    }
                }

                ImGui.EndTable();
            }

            ImGui.Spacing();
            ImGui.SeparatorText("Preset Management:");

            // Preset management
            if (ImGui.Button("Save Current Row as Preset"))
            {
                showPresetSaveDialog = true;
            }
            ImGuiComponents.Tooltip("Save the current spell bar row as a preset");

            ImGui.SameLine();

            if (ImGui.Button("Load Preset"))
            {
                showPresetLoadDialog = true;
            }
            ImGuiComponents.Tooltip("Load a saved preset");

            // Save preset dialog
            if (showPresetSaveDialog)
            {
                ImGui.OpenPopup("Save Preset");
                showPresetSaveDialog = false;
            }

            if (ImGui.BeginPopupModal("Save Preset", ref showPresetSaveDialog))
            {
                ImGui.Text("Enter preset name:");
                ImGui.InputText("##PresetName", ref newPresetName, 50);

                if (ImGui.Button("Save"))
                {
                    if (!string.IsNullOrEmpty(newPresetName))
                    {
                        SpellBarManager.SaveCurrentRowPreset(newPresetName);
                        newPresetName = "";
                        ImGui.CloseCurrentPopup();
                    }
                }
                ImGui.SameLine();
                if (ImGui.Button("Cancel"))
                {
                    newPresetName = "";
                    ImGui.CloseCurrentPopup();
                }

                ImGui.EndPopup();
            }

            // Load preset dialog
            if (showPresetLoadDialog)
            {
                ImGui.OpenPopup("Load Preset");
                showPresetLoadDialog = false;
            }

            if (ImGui.BeginPopupModal("Load Preset", ref showPresetLoadDialog))
            {
                string[] presets = SpellBarManager.ListPresets();

                if (presets.Length == 0)
                {
                    ImGui.Text("No presets available.");
                }
                else
                {
                    ImGui.Text("Select a preset to load:");
                    ImGui.Separator();

                    foreach (string preset in presets)
                    {
                        if (ImGui.Button(preset))
                        {
                            SpellBarManager.ImportPreset(preset);
                            ImGui.CloseCurrentPopup();
                        }
                    }
                }

                ImGui.Separator();
                if (ImGui.Button("Cancel"))
                {
                    ImGui.CloseCurrentPopup();
                }

                ImGui.EndPopup();
            }

            ImGui.Spacing();

            // Wiki link
            if (ImGui.Button("SpellBar Wiki"))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://github.com/PlayTazUO/TazUO/wiki/TazUO.SpellBar",
                    UseShellExecute = true
                });
            }
            ImGuiComponents.Tooltip("Open the SpellBar wiki page in your browser");
        }
    }
}
