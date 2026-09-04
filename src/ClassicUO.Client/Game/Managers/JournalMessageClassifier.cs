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
    /// <param name="message">The original message and its source context.</param>
    /// <param name="classifySystemChat">The profile-level feature switch.</param>
    /// <param name="pattern">A .NET regex; blank or malformed patterns match nothing.</param>
    /// <returns><see cref="MessageType.ChatSystem"/> on a match; otherwise the original type.</returns>
    public static MessageType Classify(
        MessageEventArgs message,
        bool classifySystemChat,
        string pattern
    )
    {
        bool isEligibleMessageType =
            message.Type == MessageType.System
            || message.Type == MessageType.Regular
                && (message.Parent == null || !SerialHelper.IsValid(message.Parent.Serial));

        if (
            !classifySystemChat
            || !isEligibleMessageType
            || string.IsNullOrEmpty(message.Text)
            || string.IsNullOrWhiteSpace(pattern)
        )
        {
            return message.Type;
        }

        try
        {
            return GetPattern(pattern)?.IsMatch(message.Text) == true
                ? MessageType.ChatSystem
                : message.Type;
        }
        catch (RegexMatchTimeoutException)
        {
            return message.Type;
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
