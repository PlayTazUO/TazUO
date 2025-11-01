using ClassicUO.LegionScripting;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;

namespace ClassicUO.Game.UI.ImGuiControls.Legion;

public class ScriptConstantsEditorWindow : ImGuiWindow
{
    private readonly ScriptFile _script;
    private Dictionary<string, ConstantEntry> _constants = new Dictionary<string, ConstantEntry>();
    private string _filterText = "";
    private bool _hasUnsavedChanges = false;
    private string _saveStatus = "";
    private float _saveStatusTimer = 0;

    public ScriptConstantsEditorWindow(ScriptFile script) : base(script.FileName + " constants")
    {
        _script = script;
        ParseConstants();
    }

    private void ParseConstants()
    {
        _constants.Clear();

        if (_script.FileContents == null || _script.FileContents.Length == 0)
            return;

        var constantPattern = new Regex(@"^([A-Z][A-Z0-9_]*)\s*=\s*(.+?)(?:\s*#.*)?$", RegexOptions.Compiled);

        for (int i = 0; i < _script.FileContents.Length; i++)
        {
            string line = _script.FileContents[i].TrimEnd();

            // Skip empty lines and comments
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                continue;

            // Skip indented lines (not top-level)
            if (line.Length > 0 && char.IsWhiteSpace(line[0]))
                continue;

            Match match = constantPattern.Match(line);
            if (match.Success)
            {
                string name = match.Groups[1].Value;
                string value = match.Groups[2].Value.Trim();

                _constants[name] = new ConstantEntry
                {
                    Name = name,
                    OriginalValue = value,
                    EditValue = value,
                    LineNumber = i,
                    FullLine = line
                };
            }
        }
    }

    public override void DrawContent()
    {
        DrawToolbar();
        ImGui.Separator();
        DrawConstantsTable();
    }

    private void DrawToolbar()
    {
        ImGui.SetNextItemWidth(175);
        ImGui.InputTextWithHint("##filter", "Filter constants...", ref _filterText, 256);

        ImGui.SameLine();
        if (ImGui.Button("Refresh"))
        {
            RefreshConstants();
        }

        ImGui.SameLine();

        if (ImGui.Button(_hasUnsavedChanges ? "Save Changes *" : "Save"))
            SaveConstants();

        // Show save status message
        if (_saveStatusTimer > 0)
        {
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(0.2f, 0.8f, 0.2f, 1.0f), _saveStatus);
            _saveStatusTimer -= ImGui.GetIO().DeltaTime;
        }

        ImGui.SameLine();
        ImGui.Text($"({_constants.Count} constant{(_constants.Count != 1 ? "s" : "")})");
    }

    private void DrawConstantsTable()
    {
        IEnumerable<ConstantEntry> filteredConstants = _constants.Values.AsEnumerable();

        // Apply filter
        if (!string.IsNullOrWhiteSpace(_filterText))
        {
            filteredConstants = filteredConstants.Where(c =>
                c.Name.Contains(_filterText, StringComparison.OrdinalIgnoreCase) ||
                c.EditValue.Contains(_filterText, StringComparison.OrdinalIgnoreCase)
            );
        }

        var constantsList = filteredConstants.OrderBy(c => c.LineNumber).ToList();

        if (constantsList.Count == 0)
        {
            if (string.IsNullOrWhiteSpace(_filterText))
            {
                ImGui.Text("No constants found in script.");
                ImGui.Text("Constants must be top-level assignments with UPPERCASE names.");
                ImGui.Spacing();
                ImGui.Text("Example: MAX_DISTANCE = 10");
            }
            else
            {
                ImGui.Text("No constants match the filter.");
            }
            return;
        }

        // Table with columns: Constant Name, Value, Line
        if (ImGui.BeginTable("ConstantsTable", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable | ImGuiTableFlags.ScrollY))
        {
            ImGui.TableSetupColumn("Constant", ImGuiTableColumnFlags.WidthFixed, 200);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Line", ImGuiTableColumnFlags.WidthFixed, 60);
            ImGui.TableSetupScrollFreeze(0, 1);
            ImGui.TableHeadersRow();

            foreach (ConstantEntry constant in constantsList)
            {
                ImGui.TableNextRow();
                ImGui.PushID(constant.Name);

                // Constant name column
                ImGui.TableSetColumnIndex(0);
                ImGui.Text(constant.Name);

                // Value column
                ImGui.TableSetColumnIndex(1);
                ImGui.SetNextItemWidth(-1);

                string editValue = constant.EditValue;
                if (ImGui.InputText("##value", ref editValue, 1024))
                {
                    constant.EditValue = editValue;
                    CheckForChanges();
                }

                // Show tooltip with original value if changed
                if (ImGui.IsItemHovered() && constant.OriginalValue != constant.EditValue)
                {
                    ImGui.SetTooltip($"Original: {constant.OriginalValue}");
                }

                // Line number column
                ImGui.TableSetColumnIndex(2);
                ImGui.Text($"{constant.LineNumber + 1}");

                ImGui.PopID();
            }

            ImGui.EndTable();
        }
    }

    private void CheckForChanges() => _hasUnsavedChanges = _constants.Values.Any(c => c.OriginalValue != c.EditValue);

    private void RefreshConstants()
    {
        // Reload file contents
        if (File.Exists(_script.FullPath))
        {
            _script.FileContents = File.ReadAllLines(_script.FullPath);
            _script.FileContentsJoined = string.Join("\n", _script.FileContents);
            ParseConstants();
            _hasUnsavedChanges = false;
            _saveStatus = "Refreshed from file";
            _saveStatusTimer = 3.0f;
        }
    }

    private void SaveConstants()
    {
        try
        {
            if (!_hasUnsavedChanges)
            {
                _saveStatus = "No changes to save";
                _saveStatusTimer = 2.0f;
                return;
            }

            // Create a copy of file contents
            string[] updatedLines = new string[_script.FileContents.Length];
            Array.Copy(_script.FileContents, updatedLines, _script.FileContents.Length);

            // Update each changed constant
            foreach (ConstantEntry constant in _constants.Values.Where(c => c.OriginalValue != c.EditValue))
            {
                // Reconstruct the line with the new value
                string updatedLine = $"{constant.Name} = {constant.EditValue}";
                updatedLines[constant.LineNumber] = updatedLine;
            }

            // Write to file
            File.WriteAllLines(_script.FullPath, updatedLines);

            // Update script object
            _script.FileContents = updatedLines;
            _script.FileContentsJoined = string.Join("\n", updatedLines);

            // Update our constants to reflect the new state
            foreach (ConstantEntry constant in _constants.Values)
            {
                constant.OriginalValue = constant.EditValue;
                constant.FullLine = updatedLines[constant.LineNumber];
            }

            _hasUnsavedChanges = false;
            _saveStatus = "Saved successfully!";
            _saveStatusTimer = 3.0f;
        }
        catch (Exception ex)
        {
            _saveStatus = $"Error: {ex.Message}";
            _saveStatusTimer = 5.0f;
        }
    }

    private class ConstantEntry
    {
        public string Name { get; set; }
        public string OriginalValue { get; set; }
        public string EditValue { get; set; }
        public int LineNumber { get; set; }
        public string FullLine { get; set; }
    }
}
