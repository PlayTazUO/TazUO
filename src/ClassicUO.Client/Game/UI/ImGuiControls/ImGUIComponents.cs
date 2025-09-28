using ImGuiNET;

/// <summary>
/// Provides reusable ImGui UI components with consistent styling.
/// </summary>
public static class ImGuiComponents
{
    /// <summary>
    /// Displays a "(?)" help icon that shows a tooltip with the specified text when hovered.
    /// </summary>
    /// <param name="text">The tooltip text to display.</param>
    public static void Tooltip(string text)
    {
        ImGui.SameLine();
        ImGui.TextDisabled("(?)");
        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.PushTextWrapPos(400); // Limita el ancho del texto
            ImGui.TextUnformatted(text);
            ImGui.PopTextWrapPos();
            ImGui.EndTooltip();
        }
    }
}
