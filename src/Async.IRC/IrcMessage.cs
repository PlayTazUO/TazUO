using System;
using System.Collections.Generic;
using System.Linq;

namespace Async.IRC;

/// <summary>
/// Represents a parsed IRC message conforming to RFC 1459.
/// Format: [:prefix] COMMAND [params...] [:trailing]
/// </summary>
public readonly struct IrcMessage
{
    public string? Prefix { get; }
    public string Command { get; }
    public string[] Params { get; }
    public string? Trailing { get; }

    private IrcMessage(string? prefix, string command, string[] parameters, string? trailing)
    {
        Prefix = prefix;
        Command = command;
        Params = parameters;
        Trailing = trailing;
    }

    /// <summary>Extracts just the nickname portion from a "nick!user@host" prefix.</summary>
    public string? NickFromPrefix()
    {
        if (Prefix == null) return null;
        int bang = Prefix.IndexOf('!');
        return bang >= 0 ? Prefix[..bang] : Prefix;
    }

    public static IrcMessage Parse(string line)
    {
        if (string.IsNullOrEmpty(line))
            return new IrcMessage(null, string.Empty, [], null);

        ReadOnlySpan<char> span = line.AsSpan();
        string? prefix = null;
        string? trailing = null;
        int pos = 0;

        // Optional prefix starting with ':'
        if (span[0] == ':')
        {
            int spaceIdx = span.IndexOf(' ');
            if (spaceIdx < 0)
                return new IrcMessage(null, string.Empty, [], null);
            prefix = span[1..spaceIdx].ToString();
            pos = spaceIdx + 1;
        }

        // Skip extra spaces
        while (pos < span.Length && span[pos] == ' ') pos++;

        ReadOnlySpan<char> remaining = span[pos..];

        // Find trailing parameter after " :"
        int trailingIdx = -1;
        for (int i = 0; i < remaining.Length - 1; i++)
        {
            if (remaining[i] == ' ' && remaining[i + 1] == ':')
            {
                trailingIdx = i;
                break;
            }
        }

        ReadOnlySpan<char> mainPart;
        if (trailingIdx >= 0)
        {
            trailing = remaining[(trailingIdx + 2)..].ToString();
            mainPart = remaining[..trailingIdx];
        }
        else
        {
            mainPart = remaining;
        }

        // Split remaining on spaces: first token is command, rest are params
        var parts = new List<string>();
        int start = 0;
        for (int i = 0; i <= mainPart.Length; i++)
        {
            if (i == mainPart.Length || mainPart[i] == ' ')
            {
                if (i > start)
                    parts.Add(mainPart[start..i].ToString());
                start = i + 1;
            }
        }

        if (parts.Count == 0)
            return new IrcMessage(prefix, string.Empty, [], trailing);

        string command = parts[0];
        string[] parameters = parts.Count > 1 ? [.. parts.Skip(1)] : [];

        return new IrcMessage(prefix, command, parameters, trailing);
    }
}
