using ImGuiNET;
using System;
using System.IO;
using System.Linq;
using System.Numerics;

namespace ClassicUO.Game.UI.ImGuiControls
{
    public class ImGuiThemeEditorWindow : SingletonImGuiWindow<ImGuiThemeEditorWindow>
    {
        private ImGuiTheme.ThemeColors _editingColors;
        private string _importExportText = string.Empty;
        private string _statusMessage = string.Empty;
        private float _statusMessageTimer = 0f;

        private ImGuiThemeEditorWindow() : base("Custom Theme Editor")
        {
            WindowFlags = ImGuiWindowFlags.AlwaysAutoResize;
            _editingColors = CloneCustomTheme();
        }

        private ImGuiTheme.ThemeColors CloneCustomTheme()
        {
            var customTheme = ImGuiTheme.GetTheme("Custom");
            if (customTheme == null)
            {
                ImGuiTheme.LoadCustomThemeFromSettings();
                customTheme = ImGuiTheme.GetTheme("Custom");
            }

            return CloneThemeColors(customTheme.Colors);
        }

        private ImGuiTheme.ThemeColors CloneFromCurrent() => CloneThemeColors(ImGuiTheme.Current);

        private ImGuiTheme.ThemeColors CloneThemeColors(ImGuiTheme.ThemeColors colors) =>
            new()
            {
                Base100 = colors.Base100,
                Base200 = colors.Base200,
                Base300 = colors.Base300,
                BaseContent = colors.BaseContent,
                Primary = colors.Primary,
                PrimaryContent = colors.PrimaryContent,
                Secondary = colors.Secondary,
                SecondaryContent = colors.SecondaryContent,
                Accent = colors.Accent,
                AccentContent = colors.AccentContent,
                Neutral = colors.Neutral,
                NeutralContent = colors.NeutralContent,
                Info = colors.Info,
                InfoContent = colors.InfoContent,
                Success = colors.Success,
                SuccessContent = colors.SuccessContent,
                Warning = colors.Warning,
                WarningContent = colors.WarningContent,
                Error = colors.Error,
                ErrorContent = colors.ErrorContent,
                Border = colors.Border,
                BorderShadow = colors.BorderShadow,
                ScrollbarBg = colors.ScrollbarBg,
                ScrollbarGrab = colors.ScrollbarGrab,
                ScrollbarGrabHovered = colors.ScrollbarGrabHovered,
                ScrollbarGrabActive = colors.ScrollbarGrabActive,
            };

        private void ApplyEditingTheme()
        {
            // Update the Custom theme with edited colors
            var customTheme = ImGuiTheme.GetTheme("Custom");
            if (customTheme != null)
            {
                customTheme.Colors.Base100 = _editingColors.Base100;
                customTheme.Colors.Base200 = _editingColors.Base200;
                customTheme.Colors.Base300 = _editingColors.Base300;
                customTheme.Colors.BaseContent = _editingColors.BaseContent;
                customTheme.Colors.Primary = _editingColors.Primary;
                customTheme.Colors.PrimaryContent = _editingColors.PrimaryContent;
                customTheme.Colors.Secondary = _editingColors.Secondary;
                customTheme.Colors.SecondaryContent = _editingColors.SecondaryContent;
                customTheme.Colors.Accent = _editingColors.Accent;
                customTheme.Colors.AccentContent = _editingColors.AccentContent;
                customTheme.Colors.Neutral = _editingColors.Neutral;
                customTheme.Colors.NeutralContent = _editingColors.NeutralContent;
                customTheme.Colors.Info = _editingColors.Info;
                customTheme.Colors.InfoContent = _editingColors.InfoContent;
                customTheme.Colors.Success = _editingColors.Success;
                customTheme.Colors.SuccessContent = _editingColors.SuccessContent;
                customTheme.Colors.Warning = _editingColors.Warning;
                customTheme.Colors.WarningContent = _editingColors.WarningContent;
                customTheme.Colors.Error = _editingColors.Error;
                customTheme.Colors.ErrorContent = _editingColors.ErrorContent;
                customTheme.Colors.Border = _editingColors.Border;
                customTheme.Colors.BorderShadow = _editingColors.BorderShadow;
                customTheme.Colors.ScrollbarBg = _editingColors.ScrollbarBg;
                customTheme.Colors.ScrollbarGrab = _editingColors.ScrollbarGrab;
                customTheme.Colors.ScrollbarGrabHovered = _editingColors.ScrollbarGrabHovered;
                customTheme.Colors.ScrollbarGrabActive = _editingColors.ScrollbarGrabActive;

                // Save to settings
                ImGuiTheme.SaveCustomThemeToSettings();

                // Switch to Custom theme
                if (ImGuiTheme.SetTheme("Custom"))
                {
                    _ = Client.Settings?.SetAsync(SettingsScope.Global, Constants.SqlSettings.IMGUI_THEME, "Custom");
                    ImGuiManager.UpdateTheme();
                }
            }
        }

        public override void Update()
        {
            base.Update();

            if (_statusMessageTimer > 0f)
            {
                _statusMessageTimer -= 0.016f; // Approximate delta time
                if (_statusMessageTimer <= 0f)
                {
                    _statusMessage = string.Empty;
                }
            }
        }

        private void ShowStatus(string message, float duration = 3f)
        {
            _statusMessage = message;
            _statusMessageTimer = duration;
        }

        public override void DrawContent()
        {
            ImGui.Text("Edit your custom theme colors");
            ImGui.Separator();

            if (ImGui.Button("Reset to Saved Custom Theme"))
            {
                _editingColors = CloneCustomTheme();
                ShowStatus("Reset to saved custom theme");
            }
            ImGuiComponents.Tooltip("Discard unsaved changes and reload the last saved custom theme.");

            ImGui.SameLine();

            if (ImGui.Button("Copy from Current Theme"))
            {
                _editingColors = CloneFromCurrent();
                ShowStatus($"Copied colors from {ImGuiTheme.CurrentThemeName} theme");
            }
            ImGuiComponents.Tooltip("Copy colors from the currently active theme to start customizing.");

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            // Status message
            if (!string.IsNullOrEmpty(_statusMessage))
            {
                ImGui.TextColored(new Vector4(0.4f, 0.8f, 0.4f, 1.0f), _statusMessage);
                ImGui.Spacing();
            }

            // Color editors in tabs
            if (ImGui.BeginTabBar("##ThemeColorTabs"))
            {
                if (ImGui.BeginTabItem("Base Colors"))
                {
                    ImGui.Text("Bases");
                    ImGui.Spacing();
                    DrawColorEdit("Base100", () => _editingColors.Base100, v => _editingColors.Base100 = v);
                    DrawColorEdit("Base200", () => _editingColors.Base200, v => _editingColors.Base200 = v);
                    DrawColorEdit("Base300", () => _editingColors.Base300, v => _editingColors.Base300 = v);
                    DrawColorEdit("BaseContent", () => _editingColors.BaseContent, v => _editingColors.BaseContent = v);
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Principal"))
                {
                    ImGui.Text("Primary");
                    ImGui.Spacing();
                    DrawColorEdit("Primary", () => _editingColors.Primary, v => _editingColors.Primary = v);
                    DrawColorEdit("PrimaryContent", () => _editingColors.PrimaryContent, v => _editingColors.PrimaryContent = v);

                    ImGui.Spacing();
                    ImGui.Text("Secondary");
                    ImGui.Spacing();
                    DrawColorEdit("Secondary", () => _editingColors.Secondary, v => _editingColors.Secondary = v);
                    DrawColorEdit("SecondaryContent", () => _editingColors.SecondaryContent, v => _editingColors.SecondaryContent = v);

                    ImGui.Spacing();
                    ImGui.Text("Accent");
                    ImGui.Spacing();
                    DrawColorEdit("Accent", () => _editingColors.Accent, v => _editingColors.Accent = v);
                    DrawColorEdit("AccentContent", () => _editingColors.AccentContent, v => _editingColors.AccentContent = v);
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Neutral"))
                {
                    ImGui.Text("Neutral");
                    ImGui.Spacing();
                    DrawColorEdit("Neutral", () => _editingColors.Neutral, v => _editingColors.Neutral = v);
                    DrawColorEdit("NeutralContent", () => _editingColors.NeutralContent, v => _editingColors.NeutralContent = v);
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("States"))
                {
                    ImGui.Text("Info");
                    ImGui.Spacing();
                    DrawColorEdit("Info", () => _editingColors.Info, v => _editingColors.Info = v);
                    DrawColorEdit("InfoContent", () => _editingColors.InfoContent, v => _editingColors.InfoContent = v);

                    ImGui.Spacing();
                    ImGui.Text("Success");
                    ImGui.Spacing();
                    DrawColorEdit("Success", () => _editingColors.Success, v => _editingColors.Success = v);
                    DrawColorEdit("SuccessContent", () => _editingColors.SuccessContent, v => _editingColors.SuccessContent = v);

                    ImGui.Spacing();
                    ImGui.Text("Warning");
                    ImGui.Spacing();
                    DrawColorEdit("Warning", () => _editingColors.Warning, v => _editingColors.Warning = v);
                    DrawColorEdit("WarningContent", () => _editingColors.WarningContent, v => _editingColors.WarningContent = v);

                    ImGui.Spacing();
                    ImGui.Text("Error");
                    ImGui.Spacing();
                    DrawColorEdit("Error", () => _editingColors.Error, v => _editingColors.Error = v);
                    DrawColorEdit("ErrorContent", () => _editingColors.ErrorContent, v => _editingColors.ErrorContent = v);
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("UI Elements"))
                {
                    ImGui.Text("Borders");
                    ImGui.Spacing();
                    DrawColorEdit("Border", () => _editingColors.Border, v => _editingColors.Border = v);
                    DrawColorEdit("BorderShadow", () => _editingColors.BorderShadow, v => _editingColors.BorderShadow = v);

                    ImGui.Spacing();
                    ImGui.Text("Scrollbar");
                    ImGui.Spacing();
                    DrawColorEdit("ScrollbarBg", () => _editingColors.ScrollbarBg, v => _editingColors.ScrollbarBg = v);
                    DrawColorEdit("ScrollbarGrab", () => _editingColors.ScrollbarGrab, v => _editingColors.ScrollbarGrab = v);
                    DrawColorEdit("ScrollbarGrabHovered", () => _editingColors.ScrollbarGrabHovered, v => _editingColors.ScrollbarGrabHovered = v);
                    DrawColorEdit("ScrollbarGrabActive", () => _editingColors.ScrollbarGrabActive, v => _editingColors.ScrollbarGrabActive = v);
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Import/Export"))
                {
                    DrawImportExportTab();
                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            // Apply button
            if (ImGui.Button("Save and Apply Custom Theme", new Vector2(200, 30)))
            {
                ApplyEditingTheme();
                ShowStatus("Custom theme saved and applied!");
            }
            ImGuiComponents.Tooltip("Save your custom theme to settings and activate it.");
        }

        private void DrawColorEdit(string name, Func<Vector4> getter, Action<Vector4> setter)
        {
            Vector4 color = getter();
            if (ImGui.ColorEdit4($"##{name}", ref color, ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.DisplayRGB))
            {
                setter(color);
            }
            ImGui.SameLine();
            ImGui.Text(name);
        }

        private void DrawImportExportTab()
        {
            ImGui.Text("Export or import your custom theme as JSON");
            ImGui.Spacing();

            if (ImGui.Button("Export to Clipboard"))
            {
                try
                {
                    _importExportText = _editingColors.ToJson();
                    SDL3.SDL.SDL_SetClipboardText(_importExportText);
                    ShowStatus("Custom theme exported to clipboard!");
                }
                catch (Exception ex)
                {
                    ShowStatus($"Export failed: {ex.Message}");
                }
            }

            ImGui.SameLine();

            if (ImGui.Button("Export to File"))
            {
                try
                {
                    string fileName = $"custom_theme_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                    string filePath = Path.Combine(Environment.CurrentDirectory, fileName);
                    File.WriteAllText(filePath, _editingColors.ToJson());
                    ShowStatus($"Exported to {fileName}");
                }
                catch (Exception ex)
                {
                    ShowStatus($"File export failed: {ex.Message}");
                }
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            ImGui.Text("Paste JSON below to import:");
            ImGui.InputTextMultiline("##importText", ref _importExportText, 10000, new Vector2(500, 300));

            ImGui.Spacing();

            if (ImGui.Button("Import from JSON"))
            {
                try
                {
                    var imported = new ImGuiTheme.ThemeColors(_importExportText);
                    _editingColors = imported;
                    ShowStatus("Theme imported successfully!");
                }
                catch (Exception ex)
                {
                    ShowStatus($"Import failed: {ex.Message}");
                }
            }

            ImGui.SameLine();

            if (ImGui.Button("Import from Clipboard"))
            {
                try
                {
                    _importExportText = SDL3.SDL.SDL_GetClipboardText();
                    var imported = new ImGuiTheme.ThemeColors(_importExportText);
                    _editingColors = imported;
                    ShowStatus("Theme imported from clipboard!");
                }
                catch (Exception ex)
                {
                    ShowStatus($"Clipboard import failed: {ex.Message}");
                }
            }
        }
    }
}
