using System;
using System.Text.RegularExpressions;
using ClassicUO.Game.Data;

namespace ClassicUO.Game.Managers;

/// <summary>
/// Applies the profile's Global Chat pattern to system-like messages. Invalid or slow patterns fail
/// closed so user configuration cannot interrupt journal processing.
/// </summary>
internal static class JournalMessageClassifier
{
    private static string _cachedPattern;
    private static Regex _cachedRegex;

    /// <summary>Reclassifies eligible journal text without changing unrelated message types.</summary>
    /// <param name="messageType">The original server message type.</param>
    /// <param name="text">The final journal text, matched without preprocessing.</param>
    /// <param name="classifySystemChat">The profile-level feature switch.</param>
    /// <param name="pattern">A .NET regex; blank or malformed patterns match nothing.</param>
    /// <param name="isSystemLikeRegularMessage">
    /// Whether a <see cref="MessageType.Regular"/> message is displayed as a system message because
    /// it has no valid parent.
    /// </param>
    /// <returns><see cref="MessageType.ChatSystem"/> on a match; otherwise the original type.</returns>
    public static MessageType Classify(
        MessageType messageType,
        string text,
        bool classifySystemChat,
        string pattern,
        bool isSystemLikeRegularMessage = false
    )
    {
        bool isEligibleMessageType =
            messageType == MessageType.System
            || messageType == MessageType.Regular && isSystemLikeRegularMessage;

        if (
            !classifySystemChat
            || !isEligibleMessageType
            || string.IsNullOrEmpty(text)
            || string.IsNullOrWhiteSpace(pattern)
        )
        {
            return messageType;
        }

        try
        {
            return GetPattern(pattern)?.IsMatch(text) == true ? MessageType.ChatSystem : messageType;
        }
        catch (RegexMatchTimeoutException)
        {
            return messageType;
        }
    }

    private static Regex GetPattern(string pattern)
    {
        if (pattern == _cachedPattern)
        {
            return _cachedRegex;
        }

        _cachedPattern = pattern;

        try
        {
            _cachedRegex = new Regex(
                pattern,
                RegexOptions.Compiled | RegexOptions.CultureInvariant,
                TimeSpan.FromMilliseconds(100)
            );
        }
        catch (ArgumentException)
        {
            _cachedRegex = null;
        }

        return _cachedRegex;
    }
}
