namespace ClassicUO.LegionScripting.ApiClasses;

/// <summary>
/// Lightweight metadata for an open gump on screen.
/// </summary>
public class ApiOpenGumpInfo
{
    public uint ServerSerial { get; set; }
    public uint LocalSerial { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string Name { get; set; } = string.Empty;
}
