using System;

namespace Async.IRC;

public sealed class IrcMessageEventArgs : EventArgs
{
    /// <summary>The nickname (or server name) that sent the message.</summary>
    public string Source { get; }

    /// <summary>The channel or nick the message was addressed to.</summary>
    public string Target { get; }

    public string Message { get; }

    internal IrcMessageEventArgs(string source, string target, string message)
    {
        Source = source;
        Target = target;
        Message = message;
    }
}

public sealed class IrcChannelJoinedEventArgs : EventArgs
{
    public string Channel { get; }

    /// <summary>The nickname that joined the channel.</summary>
    public string Nick { get; }

    internal IrcChannelJoinedEventArgs(string channel, string nick)
    {
        Channel = channel;
        Nick = nick;
    }
}

public sealed class IrcChannelPartedEventArgs : EventArgs
{
    public string Channel { get; }
    public string Nick { get; }
    public string Reason { get; }

    internal IrcChannelPartedEventArgs(string channel, string nick, string reason)
    {
        Channel = channel;
        Nick = nick;
        Reason = reason;
    }
}

public sealed class IrcUserQuitEventArgs : EventArgs
{
    public string Nick { get; }
    public string Reason { get; }

    internal IrcUserQuitEventArgs(string nick, string reason)
    {
        Nick = nick;
        Reason = reason;
    }
}

public sealed class IrcNamesEventArgs : EventArgs
{
    public string Channel { get; }

    /// <summary>Nicks as sent by the server, may include mode prefixes (@, +).</summary>
    public string[] Nicks { get; }

    internal IrcNamesEventArgs(string channel, string[] nicks)
    {
        Channel = channel;
        Nicks = nicks;
    }
}

public sealed class IrcConnectionFailedEventArgs : EventArgs
{
    public Exception Exception { get; }

    internal IrcConnectionFailedEventArgs(Exception exception)
    {
        Exception = exception;
    }
}

public sealed class IrcRawLineEventArgs : EventArgs
{
    public string Line { get; }

    /// <summary>True if this line was received from the server; false if sent by the client.</summary>
    public bool IsIncoming { get; }

    internal IrcRawLineEventArgs(string line, bool isIncoming)
    {
        Line = line;
        IsIncoming = isIncoming;
    }
}
