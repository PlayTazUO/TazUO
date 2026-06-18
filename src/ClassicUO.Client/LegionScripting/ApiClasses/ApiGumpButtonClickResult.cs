namespace ClassicUO.LegionScripting.ApiClasses;

/// <summary>
/// Result returned when clicking a gump button through Legion API or MCP.
/// </summary>
public class ApiGumpButtonClickResult
{
    public bool Success { get; set; }
    public string Kind { get; set; } = string.Empty;
    public uint RequestedGumpId { get; set; }
    public uint ServerSerial { get; set; }
    public uint LocalSerial { get; set; }
    public string GumpName { get; set; } = string.Empty;
    public int Button { get; set; }
    public string ControlType { get; set; } = string.Empty;
    public int ControlIndex { get; set; }
    public int MatchCount { get; set; }
    public string Text { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
}
