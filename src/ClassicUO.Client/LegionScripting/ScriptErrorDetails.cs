using System.Collections.Generic;

namespace ClassicUO.LegionScripting;

public struct ScriptErrorLocation(string fileName, string filePath, int lineNumber, string lineContent)
{
    public string FileName    { get; } = fileName;
    public string FilePath    { get; } = filePath;
    public int    LineNumber  { get; } = lineNumber;
    public string LineContent { get; } = lineContent;
}

public struct ScriptErrorDetails(string errorMsg, List<ScriptErrorLocation> locations, ScriptFile script)
{
    public string                    ErrorMsg  { get; } = errorMsg;
    public List<ScriptErrorLocation> Locations { get; } = locations;
    public ScriptFile                Script    { get; } = script;
}
