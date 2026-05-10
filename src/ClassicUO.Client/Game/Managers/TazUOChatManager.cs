using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Async.IRC;
using ClassicUO.Configuration;
using ClassicUO.Utility.Logging;
using FontStashSharp.RichText;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Managers;

public class TazUOChatManager
{
    public static TazUOChatManager Instance { get; private set; } = new();

    public string CurrentNick => _client?.Nickname ?? "User";

    private IrcClient _client;

    private readonly Lock _messagesLock = new();
    private readonly Lock _usersLock = new();

    /// <summary>Messages received, keyed by source (nick/channel).</summary>
    private Dictionary<string, List<string>> ReceivedMessages { get; } = [];

    /// <summary>Users present in each channel, keyed by channel name.</summary>
    private Dictionary<string, HashSet<string>> ChannelUsers { get; } = [];

    /// <summary>Incremented each time a message is stored. Used by the UI to detect new messages.</summary>
    public volatile int TotalMessageCount = 0;

    /// <summary>Incremented each time a channel is joined/left. Used by the UI to detect new channels.</summary>
    public volatile int TotalChannelCount = 0;

    /// <summary>Incremented each time the user list for any channel changes. Used by the UI to detect user list updates.</summary>
    public volatile int TotalUserUpdates = 0;

    public bool IsConnected => _client != null && _client.IsConnected;

    private TazUOChatManager(){}

    public void Init()
    {
        return; //System disabled

        if (IsConnected) return;

        if (_client != null)
            Dispose();

        _client = new();
        _client.Connected += OnConnected;
        _client.ChannelJoined += ChannelJoined;
        _client.ChannelParted += ChannelParted;
        _client.UserQuit += UserQuit;
        _client.NamesReceived += NamesReceived;
        _client.ChannelMessageReceived += ChannelMessageReceived;
        _client.PrivateMessageReceived += PrivateMessageReceived;
        _client.Disconnected += OnDisconnected;
        _client.ConnectionFailed += OnConnectionFailed;

        string nick = new(ProfileManager.CurrentProfile.TazUOChatNick.Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-').ToArray());
        if (string.IsNullOrEmpty(nick)) nick = "User";

        _ = _client.ConnectAsync("irc.tazuo.org", 6697, nick, useSsl: true);
        Log.TraceDebug($"Connecting to TazUO chat...");
    }

    public string[] GetMessages(string channel)
    {
        lock (_messagesLock)
        {
            if (ReceivedMessages.TryGetValue(channel, out List<string> msgs))
                return msgs.ToArray();
            return Array.Empty<string>();
        }
    }

    public string[] GetChannels()
    {
        lock (_messagesLock)
            return ReceivedMessages.Keys.ToArray();
    }

    public string[] GetUsers(string channel)
    {
        lock (_usersLock)
        {
            if (ChannelUsers.TryGetValue(channel, out HashSet<string> users))
                return users.ToArray();
            return Array.Empty<string>();
        }
    }

    private void OnConnectionFailed(object sender, IrcConnectionFailedEventArgs e) => Log.TraceDebug($"TazUO chat connection failed: {e.Exception.Message}");

    private void PrivateMessageReceived(object sender, IrcMessageEventArgs e)
    {
        string formatted = FormatMessage(e.Source, e.Message);
        Log.TraceDebug($"{e.Source}: {e.Message}");
        StoreMessage(e.Source, formatted);
    }

    private void ChannelMessageReceived(object sender, IrcMessageEventArgs e)
    {
        string formatted = FormatMessage(e.Source, e.Message);
        Log.TraceDebug($"{e.Target} | {e.Source}: {e.Message} => [{formatted}]");
        StoreMessage(e.Target, formatted);
    }

    private static string FormatMessage(string nick, string message)
    {
        // CTCP ACTION with delimiters: \x01ACTION text\x01
        if (message.StartsWith("\u0001ACTION ", StringComparison.Ordinal))
        {
            int textStart = 8; // past \x01ACTION + space
            int end = message.LastIndexOf('\u0001');
            if (end <= 0) end = message.Length;
            string action = message.Length > textStart ? message[textStart..end] : string.Empty;
            return $"* {nick} {action}";
        }

        return $"{FormatNickname(nick)}: {message}";
    }

    private void StoreMessage(string source, string message)
    {
        lock (_messagesLock)
        {
            if (!ReceivedMessages.TryGetValue(source, out List<string> list))
            {
                list = [];
                ReceivedMessages[source] = list;
            }
            list.Add(message);

            Interlocked.Increment(ref TotalMessageCount);

            while (list.Count > 200)
                list.RemoveAt(0);
        }
    }

    public void JoinChannel(string channel)
    {
        if (_client == null || string.IsNullOrEmpty(channel)) return;
        _ = _client.JoinChannelAsync(channel);
    }

    public void LeaveChannel(string channel)
    {
        if (_client == null || string.IsNullOrEmpty(channel)) return;
        _ = _client.LeaveChannelAsync(channel);
    }

    public void SendMessage(string target, string message)
    {
        if (_client == null || string.IsNullOrEmpty(target) || string.IsNullOrEmpty(message)) return;

        if (message.StartsWith('/') && message.Length > 1)
        {
            string[] args = message.Substring(1).Split(' ');

            if(args.Length > 0)
            {
                string cmd = args[0]?.ToLower();

                if (cmd == "nick" && args.Length > 1)
                {
                    _ = _client.SetNicknameAsync(args[1]);
                    StoreMessage(target, $"*** Changing nick to {args[1]} ***");
                    ProfileManager.CurrentProfile.TazUOChatNick = args[1];
                    return;
                }
            }
        }

        _ = _client.SendMessageAsync(target, message);
        StoreMessage(target, $"/c[green]{_client.Nickname}/cd: {message}");
    }

    private void ChannelJoined(object sender, IrcChannelJoinedEventArgs e)
    {
        Log.TraceDebug($"Joined channel: {e.Channel}");
        lock (_messagesLock)
        {
            if (!ReceivedMessages.ContainsKey(e.Channel))
                ReceivedMessages[e.Channel] = [];
        }

        lock (_usersLock)
            GetOrCreateUsers(e.Channel).Add(e.Nick);

        StoreMessage(e.Channel, $"*** {e.Nick} has joined {e.Channel}");

        Interlocked.Increment(ref TotalChannelCount);
        Interlocked.Increment(ref TotalUserUpdates);
    }

    private void ChannelParted(object sender, IrcChannelPartedEventArgs e)
    {
        lock (_usersLock)
            GetOrCreateUsers(e.Channel).Remove(e.Nick);

        if (string.Equals(e.Nick, _client?.Nickname, StringComparison.OrdinalIgnoreCase))
        {
            lock (_messagesLock)
                ReceivedMessages.Remove(e.Channel);
        }
        else
            StoreMessage(e.Channel, $"*** {e.Nick} has left {e.Channel}");

        Interlocked.Decrement(ref TotalChannelCount);
        Interlocked.Increment(ref TotalUserUpdates);
    }

    private void UserQuit(object sender, IrcUserQuitEventArgs e)
    {
        // Collect affected channels under the users lock, then store messages outside it
        // to avoid nesting _usersLock → _messagesLock.
        List<string> affectedChannels = null;
        lock (_usersLock)
        {
            foreach (KeyValuePair<string, HashSet<string>> kvp in ChannelUsers)
            {
                if (kvp.Value.Remove(e.Nick))
                    (affectedChannels ??= []).Add(kvp.Key);
            }
        }

        if (affectedChannels != null)
        {
            string msg = string.IsNullOrEmpty(e.Reason)
                ? $"*** {e.Nick} has quit"
                : $"*** {e.Nick} has quit ({e.Reason})";
            foreach (string channel in affectedChannels)
                StoreMessage(channel, msg);
            Interlocked.Increment(ref TotalUserUpdates);
        }
    }

    private void NamesReceived(object sender, IrcNamesEventArgs e)
    {
        lock (_usersLock)
        {
            HashSet<string> users = GetOrCreateUsers(e.Channel);
            foreach (string nick in e.Nicks)
            {
                // Strip mode prefixes (@, +, %, ~, &)
                string clean = nick.TrimStart('@', '+', '%', '~', '&');
                if (!string.IsNullOrEmpty(clean) && !users.Contains(clean))
                    users.Add(FormatNickname(clean));
            }
        }
        Interlocked.Increment(ref TotalUserUpdates);
    }

    private HashSet<string> GetOrCreateUsers(string channel)
    {
        if (!ChannelUsers.TryGetValue(channel, out HashSet<string> users))
        {
            users = [];
            ChannelUsers[channel] = users;
        }
        return users;
    }

    private void OnConnected(object sender, EventArgs e)
    {
        Log.TraceDebug("Connected!");
        _ = _client.JoinChannelAsync("#tazuo");
    }

    private void OnDisconnected(object sender, EventArgs e)
    {
        Log.TraceDebug("Disconnected");
        UnSubEvents();
        ClearMessages();
        _client = null;
    }

    private void UnSubEvents()
    {
        if (_client is null) return;

        _client.Connected -= OnConnected;
        _client.ChannelJoined -= ChannelJoined;
        _client.ChannelParted -= ChannelParted;
        _client.UserQuit -= UserQuit;
        _client.NamesReceived -= NamesReceived;
        _client.ChannelMessageReceived -= ChannelMessageReceived;
        _client.PrivateMessageReceived -= PrivateMessageReceived;
        _client.Disconnected -= OnDisconnected;
        _client.ConnectionFailed -= OnConnectionFailed;
    }

    private void ClearMessages()
    {
        lock (_messagesLock)
        {
            ReceivedMessages.Clear();
            Interlocked.Exchange(ref TotalMessageCount, 0);
            Interlocked.Exchange(ref TotalChannelCount, 0);
        }
        lock (_usersLock)
            ChannelUsers.Clear();
    }

    public void Dispose()
    {
        if (_client == null) return;

        UnSubEvents();
        _ = _client.DisposeAsync();
        ClearMessages();
        _client = null;
    }

    private static Dictionary<string, string> _nickFormats = new();
    private static string FormatNickname(string nick)
    {
        if(_nickFormats.Count > 1000)
            _nickFormats.Clear();

        if(_nickFormats.TryGetValue(nick, out string nickFormat))
            return nickFormat;

        string htmlColor = new Color(Random.Shared.Next(254),  Random.Shared.Next(254), Random.Shared.Next(254)).ToHexString();

        string formattedNick = $"/c[{htmlColor}]{nick}/cd";
        _nickFormats[nick] = formattedNick;

        return formattedNick;
    }

    /// <summary>
    /// Generates a random name by combining syllables.
    /// </summary>
    /// <param name="minSyllables">Minimum length of the name.</param>
    /// <param name="maxSyllables">Maximum length of the name.</param>
    public static string GenerateFantasyName(int minSyllables, int maxSyllables)
    {
        string[] consonants = { "b", "c", "d", "f", "g", "h", "j", "k", "l", "m", "n", "p", "r", "s", "t", "v", "z", "th", "sh" };
        string[] vowels = { "a", "e", "i", "o", "u", "ae", "ou" };

        int length = Random.Shared.Next(minSyllables, maxSyllables + 1);
        string name = "";

        for (int i = 0; i < length; i++)
        {
            // Build a syllable: Consonant + Vowel
            string syllable = consonants[Random.Shared.Next(consonants.Length)] + vowels[Random.Shared.Next(vowels.Length)];
            name += syllable;
        }

        // Capitalize the first letter and return
        return char.ToUpper(name[0]) + name.Substring(1);
    }
}
