using System;
using System.Collections.Generic;
using System.Linq;

namespace ClassicUO.Game.Managers;

public static class TextHistoryManager
{
    private const int MAX_HISTORY_SIZE = 200;
    private static readonly List<string> commandHistory = new();

    /// <summary>
    /// Adds text to history
    /// </summary>
    /// <param name="input">The input text to check and potentially add</param>
    public static void AddToHistoryIfCommand(string input)
    {
        if (string.IsNullOrEmpty(input)) return;

        // Don't add duplicates - remove existing and add to end for most recent
        if (commandHistory.Contains(input))
        {
            commandHistory.Remove(input);
        }

        commandHistory.Add(input);

        while(commandHistory.Count > MAX_HISTORY_SIZE)
            commandHistory.RemoveAt(0);
    }

    /// <summary>
    /// Gets autocomplete suggestions for the given partial input
    /// </summary>
    /// <param name="partialInput">The partial text to complete</param>
    /// <returns>List of matching text from history</returns>
    public static List<string> GetAutocompleteSuggestions(string partialInput, int max = Int32.MaxValue)
    {
        if (string.IsNullOrEmpty(partialInput)) return new List<string>();

        // Get text that start with the partial input (case-insensitive)
        var matches = commandHistory
            .Where(cmd => cmd.StartsWith(partialInput, StringComparison.OrdinalIgnoreCase))
            .Distinct()
            .OrderBy(cmd => cmd.Length)  // Shortest first (closest match)
            .ThenBy(cmd => cmd, StringComparer.OrdinalIgnoreCase)  // Then alphabetically
            .ToList();

        return matches.GetRange(0, Math.Min(max, matches.Count));
    }

    /// <summary>
    /// Gets the best autocomplete match for the given partial input
    /// </summary>
    /// <param name="partialInput">The partial text to complete</param>
    /// <returns>The best matching text or null if no matches</returns>
    public static string GetBestAutocompletion(string partialInput)
    {
        List<string> suggestions = GetAutocompleteSuggestions(partialInput);
        return suggestions.FirstOrDefault();
    }

    /// <summary>
    /// Gets all text in history (most recent first)
    /// </summary>
    /// <returns>List of all text in reverse chronological order</returns>
    public static List<string> GetAllHistory()
    {
        var result = new List<string>(commandHistory);
        result.Reverse();
        return result;
    }

    /// <summary>
    /// Clears the history
    /// </summary>
    public static void ClearHistory() => commandHistory.Clear();
}
