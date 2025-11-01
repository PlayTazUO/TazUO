using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using Discord.Sdk;
using Microsoft.Xna.Framework;
using DClient = Discord.Sdk.Client;

namespace ClassicUO.Game.Managers;

public class DiscordManager
{
    public static DiscordManager Instance { get; private set; }

    private const string TUOLOBBY = "TazUODiscordSocialSDKLobby";
    private const ulong CLIENT_ID = 1255990139499577377;
    //Commited on purpose, this is a public discord bot that for some reason the social sdk requires, even though it connects to the player's account :thinking:
    private const int MAX_MSG_HISTORY = 75;

    private static Dictionary<string, string> _tuometa = new Dictionary<string, string>()
    {
        { "name", "TazUO - Global" }
    };

    public string StatusText => _statusText;
    public bool Connected => _connected;
    public Dictionary<ulong, List<MessageHandle>> MessageHistory => _messageHistory;
    public ulong UserId { get; private set; }
    public MessageHandle LastPrivateMessage { get; private set; }

    public static DiscordSettings DiscordSettings { get; private set; }

    #region Events

    public event EmptyEventHandler OnConnected;
    public event EmptyEventHandler OnStatusTextUpdated;

    /// <summary>
    /// object is MessageHandle
    /// </summary>
    public event SimpleEventHandler OnMessageReceived;

    public event EmptyEventHandler OnUserUpdated;

    /// <summary>
    /// object is LobbyHandle, may be null
    /// </summary>
    public event SimpleEventHandler OnLobbyCreated;

    /// <summary>
    /// object is ulong Lobby ID
    /// </summary>
    public event SimpleEventHandler OnLobbyDeleted;

    #endregion

    private DClient _client;
    private string _codeVerifier;
    private bool _authBegan, _connected, _noreconnect;
    private string _statusText = "Ready to connect...";

    private Dictionary<ulong, List<MessageHandle>> _messageHistory = new();
    private static Dictionary<ulong, Color> _userHueMemory = new();
    private Dictionary<ulong, LobbyHandle> _currentLobbies = new();
    private World _world;
    private Timer _richPresenceTimer;

    private int _disconnectAttempts = 0;
    int _pendingDisconnectLeaves;

    internal DiscordManager(World world)
    {
        Instance = this;
        _world = world;
        LoadDiscordSettings();

        _client = new DClient();

        _client.AddLogCallback(OnLog, LoggingSeverity.Error);
        _client.SetStatusChangedCallback(OnStatusChanged);
        _client.SetMessageCreatedCallback(OnMessageCreated);
        _client.SetUserUpdatedCallback(OnUserUpdatedCallback);
        _client.SetLobbyCreatedCallback(OnLobbyCreatedCallback);
        _client.SetLobbyDeletedCallback(OnLobbyDeletedCallback);
        EventSink.OnConnected += OnUOConnected;
        EventSink.OnPlayerCreated += OnPlayerCreated;
        EventSink.OnDisconnected += OnUODisconnected;
    }

    public void Update()
    {
        if (_authBegan)
        {
            Discord.Sdk.NativeMethods.Discord_RunCallbacks();
        }
    }

    public void BeginDisconnect()
    {
        if (!_connected)
            return;

        _richPresenceTimer?.Dispose();
        Log.Debug("Discord disconnecting..");

        if (_noreconnect)
            return;

        _noreconnect = true;

        _pendingDisconnectLeaves = _currentLobbies.Count;

        if (_pendingDisconnectLeaves == 0)
        {
            _client.Disconnect();

            return;
        }

        foreach (ulong lobbyId in _currentLobbies.Keys.ToList())
        {
            _client.LeaveLobby
            (
                lobbyId, result =>
                {
                    _pendingDisconnectLeaves--;

                    if (!result.Successful())
                        Log.Error($"Failed to leave lobby {lobbyId}: {result.Error()}");
                    else
                        Log.Debug($"Left lobby {lobbyId}");

                    if (_pendingDisconnectLeaves == 0)
                    {
                        Log.Debug("Final discord disconnect.");
                        _client.Disconnect();
                    }
                }
            );
        }
    }

    public void FinalizeDisconnect()
    {
        SaveDiscordSettings();

        if (!_connected)
            return;

        //Yes we're going to freeze the game for a bit, this is called after everything is unloaded already.
        //This would not work in a task, so this is our last resort
        while (_pendingDisconnectLeaves > 0)
        {
            if (_disconnectAttempts > 200) //~2 seconds
                return;

            Discord.Sdk.NativeMethods.Discord_RunCallbacks();
            Thread.Sleep(10);
            _disconnectAttempts++;
        }
    }

    public IEnumerable<LobbyHandle> GetLobbies()
    {
        ulong[] lobbies = _client.GetLobbyIds();

        List<LobbyHandle> handles = new();

        foreach (ulong lobby in lobbies)
        {
            LobbyHandle h = _client.GetLobbyHandle(lobby);

            if (h != null)
                handles.Add(h);
        }

        return handles;
    }

    public string GetLobbyName(LobbyHandle handle)
    {
        Dictionary<string, string> meta = handle.Metadata();

        if (meta.ContainsKey("name"))
        {
            return meta["name"];
        }

        return "Lobby";
    }

    public IEnumerable<RelationshipHandle> GetFriends()
    {
        if (!_connected)
            return null;

        return _client.GetRelationships();
    }

    public ChannelHandle GetChannel(ulong channelId) => _client.GetChannelHandle(channelId);

    public LobbyHandle GetLobby(ulong lobbyId) => _client.GetLobbyHandle(lobbyId);

    public UserHandle GetUser(ulong userId) => _client.GetUser(userId);

    public void SendDm(ulong id, string message)
    {
        if (string.IsNullOrEmpty(message))
            return;

        _client.SendUserMessage(id, message, SendUserMessageCallback);
    }

    public void SendChannelMsg(ulong channelId, string message)
    {
        if (string.IsNullOrEmpty(message))
            return;

        _client.SendLobbyMessage(channelId, message, SendUserMessageCallback);
    }

    public void SendChannelItem(ulong channelId, Item item, bool isDm)
    {
        if (item == null)
            return;

        Dictionary<string, string> metadata = new();

        metadata["graphic"] = item.Graphic.ToString();
        metadata["hue"] = item.Hue.ToString();

        if (_world.OPL.TryGetNameAndData(item.Serial, out string name, out string data))
        {
            metadata["name"] = name;
            metadata["data"] = data;
        }
        else
        {
            metadata["name"] = item.Name;
        }

        if(isDm)
            _client.SendUserMessageWithMetadata(channelId, string.IsNullOrEmpty(metadata["name"]) ? "Checkout this item!" : metadata["name"], metadata, SendUserMessageCallback);
        else
            _client.SendLobbyMessageWithMetadata(channelId, string.IsNullOrEmpty(metadata["name"]) ? "Checkout this item!" : metadata["name"], metadata, SendUserMessageCallback);
    }

    public void StartCall(ulong channel) => _client.StartCall(channel);

    public void EndCall(ulong channel) => _client.EndCall(channel, EndVoiceCallCallback);

    public Call GetCall(ulong channelId) => _client.GetCall(channelId);

    private void AddMsgHistory(ulong id, MessageHandle msg)
    {
        if (!_messageHistory.ContainsKey(id))
            _messageHistory.Add(id, new List<MessageHandle>());

        List<MessageHandle> list = _messageHistory[id];
        list.Add(msg);

        int excess = list.Count - MAX_MSG_HISTORY;

        if (excess > 0)
            list.RemoveRange(0, excess);
    }

    private void OnUOConnected(object sender, EventArgs e) => RunLater(JoinGameLobby);

    private void OnPlayerCreated(object sender, EventArgs e) => RunLater(() => UpdateRichPresence(true));

    private void OnUODisconnected(object sender, EventArgs e) => _client.UpdateRichPresence(new Activity(), OnUpdateRichPresence); //Reset presence

    private void ClientReady()
    {
        UserId = _client.GetCurrentUser().Id();

        _connected = true;
        OnConnected?.Invoke();

        RunLater(JoinGlobalLobby);

        _richPresenceTimer = new Timer(_=>PeriodicChecks(), null, TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(30));
    }

    private string GenServerSecret() => _world.ServerName + Settings.GlobalSettings.IP;

    private Dictionary<string, string> GenServerMeta() => new Dictionary<string, string>()
        {
            { "name", _world.ServerName }
        };

    private void PeriodicChecks()
    {
        RunLater(JoinGlobalLobby);
        RunLater(JoinGameLobby);
        RunLater(()=>UpdateRichPresence());
    }

    private static long _furthestAction;

    private static async void RunLater(Action action, long minDuration = 2000)
    {
        long now = Time.Ticks;

        // Schedule at least 1s after the last one, or now if no pending delay
        if (now > _furthestAction)
            _furthestAction = now;

        _furthestAction += minDuration;

        int delayMs = (int)(_furthestAction - now);
        await Task.Delay(delayMs);

        action();
    }

    private static ulong _unixStart = (ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    private void UpdateRichPresence(bool includeParty = true)
    {
        if (!_connected) return;

        Log.Debug("Updating rich presence.");
        var activity = new Activity();
        activity.SetName("Ultima Online");
        activity.SetType(ActivityTypes.Playing);

        if (includeParty && _world.InGame)
        {
            var party = new ActivityParty();
            party.SetPrivacy(ActivityPartyPrivacy.Public);
            party.SetCurrentSize(1);
            party.SetMaxSize(1);

            party.SetId
                (new ServerInfo(Settings.GlobalSettings.IP, Settings.GlobalSettings.Port.ToString(), _world.ServerName, _world.Player == null ? 0 : _world.Player.Serial).ToJson());

            activity.SetParty(party);
        }

        var ts = new ActivityTimestamps();
        ts.SetStart(_unixStart);
        activity.SetTimestamps(ts);

        RunLater(() => _client.UpdateRichPresence(activity, OnUpdateRichPresence));
    }

    private void SetStatusText(string text)
    {
        _statusText = text;
        OnStatusTextUpdated?.Invoke();
    }

    #region Utilities

    private void LoadDiscordSettings()
    {
        string path = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "DiscordSettings.json");

        if (!File.Exists(path))
        {
            DiscordSettings = new DiscordSettings();

            SaveDiscordSettings();

            return;
        }

        try
        {
            DiscordSettings = JsonSerializer.Deserialize(File.ReadAllText(path), DiscordSettingsJsonContext.Default.DiscordSettings);
        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
        }
    }

    public void SaveDiscordSettings()
    {
        string path = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "DiscordSettings.json");

        try
        {
            File.WriteAllText(path, JsonSerializer.Serialize(DiscordSettings, DiscordSettingsJsonContext.Default.DiscordSettings));
        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
        }
    }

    public static Color GetUserhue(ulong id)
    {
        if (!_userHueMemory.ContainsKey(id))
            _userHueMemory.Add(id, GetColorFromId(id));

        return _userHueMemory[id];
    }

    private static Color GetColorFromId(ulong id)
    {
        // Convert ulong to bytes
        byte[] bytes = BitConverter.GetBytes(id);

        // Mix the bytes to get more color variation
        byte r = (byte)(bytes[0] ^ bytes[4]);
        byte g = (byte)(bytes[1] ^ bytes[5]);
        byte b = (byte)(bytes[2] ^ bytes[6]);

        return new Color(r, g, b);
    }

    private static ushort GetHueFromId(ulong id)
    {
        // Fold the 64-bit ID into 32 bits for some mixing
        uint folded = (uint)(id ^ (id >> 32));

        // Further mix the bits
        folded ^= (folded >> 16);
        folded ^= (folded >> 8);

        // Return value in range 0–999
        return (ushort)(folded % 1000);
    }

    #endregion

    #region Callbacks

    private void EndVoiceCallCallback()
    {
        //Do nothing
    }

    private void SendUserMessageCallback(ClientResult result, ulong messageId)
    {
        if (!result.Successful())
        {
            Log.Debug("Failed to send message...");
        }
    }

    private void OnLobbyCreatedCallback(ulong lobbyId)
    {
        if (!_currentLobbies.ContainsKey(lobbyId))
            _currentLobbies.Add(lobbyId, _client.GetLobbyHandle(lobbyId));

        OnLobbyCreated?.Invoke(_currentLobbies[lobbyId]);
    }

    private void JoinGlobalLobby() => _client.CreateOrJoinLobbyWithMetadata(TUOLOBBY, _tuometa, new Dictionary<string, string>(), GameGameJoinCallback);
    private void JoinGameLobby()
    {
        if (_world.InGame)
            _client.CreateOrJoinLobbyWithMetadata(GenServerSecret(), GenServerMeta(), new Dictionary<string, string>(), GameLobbyJoinCallback);
    }

    private void GameGameJoinCallback(ClientResult result, ulong lobbyId)
    {
        if (!result.Successful())
        {
            RunLater(JoinGameLobby, 500 + (long)(result.RetryAfter()*1000));
        }
    }
    private void GameLobbyJoinCallback(ClientResult result, ulong lobbyId)
    {
        if (!result.Successful())
        {
            RunLater(JoinGameLobby, 500 + (long)(result.RetryAfter()*1000));
        }
    }

    private void OnLobbyDeletedCallback(ulong lobbyId)
    {
        _currentLobbies.Remove(lobbyId);

        if(!_noreconnect)
        {
            if(_connected)
            {
                RunLater(JoinGlobalLobby);
                RunLater(JoinGameLobby);
            }
        }

        OnLobbyDeleted?.Invoke(lobbyId);
    }

    private void OnUserUpdatedCallback(ulong userId) => OnUserUpdated?.Invoke();

    private void OnMessageCreated(ulong messageId)
    {
        MessageHandle msg = _client.GetMessageHandle(messageId);

        if (msg == null)
            return;

        ulong id = msg.ChannelId();
        ChannelHandle channel = msg.Channel(); //This msg may be a lobby, which is not a Channel.
        LobbyHandle lobby = msg.Lobby();
        UserHandle author = msg.Author();
        bool isdm = false;

        if (channel?.Type() == ChannelType.Dm)
        {
            isdm = true;

            id = msg.AuthorId();

            if (id == UserId)           //Message was sent by us
                id = msg.RecipientId(); //Put this into the msg history for this user
            else
                LastPrivateMessage = msg;
        }

        AddMsgHistory(id, msg);

        if (author == null)
            return;

        OnMessageReceived?.Invoke(msg);
        string chan = "Discord";

        if (!isdm)
            chan = channel != null ? channel.Name() : ((lobby != null) ? GetLobbyName(lobby) : "Discord");

        if ((isdm && DiscordSettings.ShowDMInGame) || (!isdm && DiscordSettings.ShowChatInGame))
            _world.MessageManager.HandleMessage(null, $"{msg.Content()}", $"[{chan}] {author.DisplayName()}", GetHueFromId(author.Id()), MessageType.ChatSystem, 255, TextType.SYSTEM);
    }

    private static void OnLog(string message, LoggingSeverity severity) => Log.Debug($"Log: {severity} - {message}");

    private void OnStatusChanged(DClient.Status status, DClient.Error error, int errorCode)
    {
        Log.Debug($"Status changed: {status}");
        SetStatusText(status.ToString());

        if (error != DClient.Error.None)
        {
            Log.Error($"Error: {error}, code: {errorCode}");
        }

        switch (status)
        {
            case DClient.Status.Disconnecting:
            case DClient.Status.Disconnected:
                _connected = false;

                if (_noreconnect)
                    break;

                Log.Debug("Discord disconnected, reconnecting...");
                _client.Connect();

                break;

            case DClient.Status.Ready: ClientReady(); break;
        }
    }

    private void OnUpdateRichPresence(ClientResult result)
    {
        //Do nothing
    }

    #endregion

    #region AuthFlow

    public void StartOAuthFlow()
    {
        Log.Debug("Starting Discord OAuth handshakes");
        SetStatusText("Attempting to connect...");
        AuthorizationCodeVerifier authorizationVerifier = _client.CreateAuthorizationCodeVerifier();
        _codeVerifier = authorizationVerifier.Verifier();

        var args = new AuthorizationArgs();
        args.SetClientId(CLIENT_ID);
        args.SetScopes(DClient.GetDefaultCommunicationScopes());
        args.SetCodeChallenge(authorizationVerifier.Challenge());
        _client.Authorize(args, OnAuthorizeResult);
        _authBegan = true;
    }

    public void FromSavedToken()
    {
        string rpath = Path.Combine(CUOEnviroment.ExecutablePath, "Data", ".dratoken");

        if (!File.Exists(rpath))
            return;

        SetStatusText("Attempting to reconnect...");

        try
        {
            string rtoken = Crypter.Decrypt(File.ReadAllText(rpath));

            _client.RefreshToken(CLIENT_ID, rtoken, OnTokenExchangeCallback);
            _authBegan = true;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private void OnTokenExchangeCallback(ClientResult result, string token, string refreshToken, AuthorizationTokenType tokenType, int expiresIn, string scopes)
    {
        if (!result.Successful())
        {
            OnRetrieveTokenFailed();

            return;
        }

        OnReceivedToken(token, refreshToken);
    }

    private void OnAuthorizeResult(ClientResult result, string code, string redirectUri)
    {
        Log.Debug($"Authorization result: [{result.Error()}] [{code}] [{redirectUri}]");
        SetStatusText("Handshake in progress...");

        if (!result.Successful())
        {
            OnRetrieveTokenFailed();

            return;
        }

        GetTokenFromCode(code, redirectUri);
    }

    private void GetTokenFromCode(string code, string redirectUri) => _client.GetToken(CLIENT_ID, code, _codeVerifier, redirectUri, TokenExchangeCallback);

    private void TokenExchangeCallback(ClientResult result, string token, string refreshToken, AuthorizationTokenType tokenType, int expiresIn, string scopes)
    {
        //TODO: Handle token expirations
        if (token != "")
        {
            OnReceivedToken(token, refreshToken);
        }
        else
        {
            OnRetrieveTokenFailed();
        }
    }

    private void OnReceivedToken(string token, string refresh)
    {
        Log.Debug("Token received");
        SetStatusText("Almost done...");

        try
        {
            File.WriteAllText(Path.Combine(CUOEnviroment.ExecutablePath, "Data", ".dratoken"), Crypter.Encrypt(refresh));
        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
        }

        _client.UpdateToken(AuthorizationTokenType.Bearer, token, (ClientResult result) => { _client.Connect(); });
    }

    private void OnRetrieveTokenFailed() => SetStatusText("Failed to retrieve token");

    #endregion
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(DiscordSettings))]
public partial class DiscordSettingsJsonContext : JsonSerializerContext
{
}

public class DiscordSettings
{
    public bool ShowDMInGame { get; set; } = true;
    public bool ShowChatInGame { get; set; } = true;
}

public delegate void SimpleEventHandler(object sender);
public delegate void EmptyEventHandler();

public struct ServerInfo(string ip, string port, string name, uint playerSerial)
{
    public string Ip { get; set; } = ip;
    public string Port { get; set; } = port;
    public string Name { get; set; } = name;
    public uint PlayerSerial { get; set; } = playerSerial;

    public string ToJson() => JsonSerializer.Serialize(new ServerInfo(Ip, Port, Name, PlayerSerial), ServerInfoJsonContext.Default.ServerInfo);

    public static ServerInfo FromJson(string json) => JsonSerializer.Deserialize(json, ServerInfoJsonContext.Default.ServerInfo);
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(ServerInfo))]
public partial class ServerInfoJsonContext : JsonSerializerContext
{
}
