// SPDX-License-Identifier: BSD-2-Clause

using System.Text.RegularExpressions;
using ClassicUO.Game.Data;

namespace ClassicUO.Game.Managers;

/// <summary>Classifies server messages for journal filtering without changing their network handling.</summary>
internal static partial class JournalMessageClassifier
{
    /// <summary>Maps a timestamped system chat line to the existing global chat message type when enabled.</summary>
    /// <param name="messageType">The message type supplied by the server.</param>
    /// <param name="text">The message text to inspect.</param>
    /// <param name="classifySystemChat">Whether timestamped system chat classification is enabled.</param>
    /// <returns>The message type to store in the journal.</returns>
    public static MessageType Classify(MessageType messageType, string text, bool classifySystemChat)
    {
        if (!classifySystemChat || messageType != MessageType.System || string.IsNullOrEmpty(text))
        {
            return messageType;
        }

        return TimestampedSystemChatPattern().IsMatch(text) ? MessageType.ChatSystem : messageType;
    }

    /// <summary>Gets the compiled pattern for complete timestamped shard chat lines.</summary>
    /// <returns>The generated regular expression used for classification.</returns>
    [GeneratedRegex(
        @"^\[(?:[01][0-9]|2[0-3]):[0-5][0-9]\] [^:\s\r\n](?:[^:\r\n]*[^:\s\r\n])?: \S(?:[^\r\n]*\S)?$",
        RegexOptions.CultureInvariant
    )]
    private static partial Regex TimestampedSystemChatPattern();
}
