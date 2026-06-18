using Microsoft.Xna.Framework;

namespace ClassicUO.LegionScripting.ApiClasses;

/// <summary>
/// Result metadata for screenshot captures created through the Legion API.
/// </summary>
public class ApiScreenshotResult
{
    public bool Success { get; set; }
    public string Path { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public string Mode { get; set; } = "full";
    public uint GumpId { get; set; }
    public string Error { get; set; } = string.Empty;

    internal static ApiScreenshotResult FromCaptureResult(GameController.ScreenshotCaptureResult result, string mode, uint gumpId = 0)
    {
        Rectangle region = result.Region;

        return new ApiScreenshotResult
        {
            Success = result.Success,
            Path = result.Path,
            Width = result.Width,
            Height = result.Height,
            X = region.X,
            Y = region.Y,
            Mode = mode,
            GumpId = gumpId,
            Error = result.Error
        };
    }
}
