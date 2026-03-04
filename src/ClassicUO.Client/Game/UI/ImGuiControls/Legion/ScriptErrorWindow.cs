using System.Collections.Generic;
using System.Numerics;
using ClassicUO.Game.UI.MyraWindows;
using ClassicUO.LegionScripting;
using ClassicUO.Utility;
using ImGuiNET;

namespace ClassicUO.Game.UI.ImGuiControls.Legion;

public class ScriptErrorWindow : ImGuiWindow
{
    private readonly ScriptErrorDetails _errorDetails;
    private static int id = 1;

    public ScriptErrorWindow(ScriptErrorDetails errorDetails) : base("Script Error " + id++)
    {
        _errorDetails = errorDetails;
        _errorMsg = errorDetails.ErrorMsg;
        _lineContents = new string[errorDetails.Locations.Count];
        for (int i = 0; i < errorDetails.Locations.Count; i++)
            _lineContents[i] = errorDetails.Locations[i].LineContent;
        WindowFlags = ImGuiWindowFlags.AlwaysAutoResize;
    }

    private string _errorMsg;
    private readonly string[] _lineContents;

    public override void DrawContent()
    {
        ImGui.Text("Your script encountered an error, here's what we know:");

        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 0.2f, 0.2f, 1));
        ImGui.Text(_errorMsg);
        ClipboardOnClick(_errorMsg);
        ImGui.PopStyleColor();
        ImGuiComponents.Tooltip("Click this error to copy it to your clipboard.");

        ImGui.Separator();

        for (int i = _errorDetails.Locations.Count - 1; i >= 0; i--)
        {
            ScriptErrorLocation loc = _errorDetails.Locations[i];
            ImGui.Text($"File: {loc.FileName}  |  Line: {loc.LineNumber}");

            if (!string.IsNullOrEmpty(_lineContents[i]))
            {
                ImGui.InputTextMultiline($"###ErrorLineContent{i}", ref _lineContents[i], uint.MaxValue, new Vector2(480, 80));
            }

            if (i > 0)
                ImGui.Separator();
        }

        if (ImGui.Button("Edit")) new ScriptEditorWindow(_errorDetails.Script);

        ImGui.SameLine();

        if (ImGui.Button("Edit Externally")) FileSystemHelper.OpenFileWithDefaultApp(_errorDetails.Script.FullPath);
    }
}

public struct ScriptErrorLocation(string fileName, string filePath, int lineNumber, string lineContent)
{
    public string FileName { get; } = fileName;
    public string FilePath { get; } = filePath;
    public int LineNumber { get; } = lineNumber;
    public string LineContent { get; } = lineContent;
}

public struct ScriptErrorDetails(string errorMsg, List<ScriptErrorLocation> locations, ScriptFile script)
{
    public string ErrorMsg { get; } = errorMsg;
    public List<ScriptErrorLocation> Locations { get; } = locations;
    public ScriptFile Script { get; } = script;
}
