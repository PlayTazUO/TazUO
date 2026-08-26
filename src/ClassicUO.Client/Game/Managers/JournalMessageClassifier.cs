// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Text.RegularExpressions;
using ClassicUO.Game.Data;

namespace ClassicUO.Game.Managers;

/// <summary>Classifies server messages for journal filtering without changing their network handling.</summary>
internal static class JournalMessageClassifier
{
    private static readonly object _patternLock = new();
    private static string _cachedPattern;
    private static Regex _cachedRegex;

    /// <summary>Maps a matching system chat line to the existing global chat message type when enabled.</summary>
    /// <param name="messageType">The message type supplied by the server.</param>
    /// <param name="text">The message text to inspect.</param>
    /// <param name="classifySystemChat">Whether system chat classification is enabled.</param>
    /// <param name="pattern">The server-specific regular expression used to identify global chat.</param>
    /// <returns>The message type to store in the journal.</returns>
    public static MessageType Classify(
        MessageType messageType,
        string text,
        bool classifySystemChat,
        string pattern
    )
    {
        if (
            !classifySystemChat
            || messageType != MessageType.System
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

    /// <summary>Returns the compiled form of the current profile pattern, or null when invalid.</summary>
    private static Regex GetPattern(string pattern)
    {
        lock (_patternLock)
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
}
