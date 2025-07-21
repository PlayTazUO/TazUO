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
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using Discord.Sdk;
using Microsoft.Xna.Framework;
using DClient = Discord.Sdk.Client;

namespace ClassicUO.Game.Managers;

public class DiscordManager
{
    public static DiscordManager Instance { get; } = new DiscordManager();

    private const string TUOLOBBY = "TazUODiscordSocialSDKLobby";
    private const ulong CLIENT_ID = 1255990139499577377;
    //Commited on purpose, this is a public discord bot that for some reason the social sdk requires, even though it connects to the player's account :thinking:
    private const int MAX_MSG_HISTORY = 75;

    private static Dictionary<string, string> TUOMETA = new Dictionary<string, string>()
    {
        { "name", "TazUO - Global" }
    };

    public string StatusText => statusText;
    public bool Connected => connected;
    public Dictionary<ulong, List<MessageHandle>> MessageHistory => messageHistory;
    public ulong UserId { get; private set; }

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

    private DClient client;
    private string codeVerifier;
    private bool authBegan, connected, noreconnect;
    private string statusText = "Ready to connect...";

    private Dictionary<ulong, List<MessageHandle>> messageHistory = new();
    private static Dictionary<ulong, Color> userHueMemory = new();
    private Dictionary<ulong, LobbyHandle> currentLobbies = new();
    private Timer richPresenceTimer;

    private int disconnectAttempts = 0;
    int pendingDisconnectLeaves;

    private DiscordManager()
    {
        LoadDiscordSettings();

        client = new DClient();

        client.AddLogCallback(OnLog, LoggingSeverity.Error);
        client.SetStatusChangedCallback(OnStatusChanged);
        client.SetMessageCreatedCallback(OnMessageCreated);
        client.SetUserUpdatedCallback(OnUserUpdatedCallback);
        client.SetLobbyCreatedCallback(OnLobbyCreatedCallback);
        client.SetLobbyDeletedCallback(OnLobbyDeletedCallback);
        EventSink.OnConnected += OnUOConnected;
        EventSink.OnPlayerCreated += OnPlayerCreated;
        EventSink.OnDisconnected += OnUODisconnected;
    }

    public void Update()
    {
        if (authBegan)
        {
            Discord.Sdk.NativeMethods.Discord_RunCallbacks();
        }
    }

    public void BeginDisconnect()
    {
        if (!connected)
            return;

        richPresenceTimer?.Dispose();
        Log.Debug("Discord disconnecting..");

        if (noreconnect)
            return;

        noreconnect = true;

        pendingDisconnectLeaves = currentLobbies.Count;

        if (pendingDisconnectLeaves == 0)
        {
            client.Disconnect();

            return;
        }

        foreach (var lobbyId in currentLobbies.Keys.ToList())
        {
            client.LeaveLobby
            (
                lobbyId, result =>
                {
                    pendingDisconnectLeaves--;

                    if (!result.Successful())
                        Log.Error($"Failed to leave lobby {lobbyId}: {result.Error()}");
                    else
                        Log.Debug($"Left lobby {lobbyId}");

                    if (pendingDisconnectLeaves == 0)
                    {
                        Log.Debug("Final discord disconnect.");
                        client.Disconnect();
                    }
                }
            );
        }
    }

    public void FinalizeDisconnect()
    {
        SaveDiscordSettings();

        if (!connected)
            return;

        //Yes we're going to freeze the game for a bit, this is called after everything is unloaded already.
        //This would not work in a task, so this is our last resort
        while (pendingDisconnectLeaves > 0)
        {
            if (disconnectAttempts > 200) //~2 seconds
                return;

            Discord.Sdk.NativeMethods.Discord_RunCallbacks();
            Thread.Sleep(10);
            disconnectAttempts++;
        }
    }

    public IEnumerable<LobbyHandle> GetLobbies()
    {
        var lobbies = client.GetLobbyIds();

        List<LobbyHandle> handles = new();

        foreach (var lobby in lobbies)
        {
            var h = client.GetLobbyHandle(lobby);

            if (h != null)
                handles.Add(h);
        }

        return handles;
    }

    public string GetLobbyName(LobbyHandle handle)
    {
        var meta = handle.Metadata();

        if (meta.ContainsKey("name"))
        {
            return meta["name"];
        }

        return "Lobby";
    }

    public IEnumerable<RelationshipHandle> GetFriends()
    {
        if (!connected)
            return null;

        return client.GetRelationships();
    }

    public ChannelHandle GetChannel(ulong channelId) => client.GetChannelHandle(channelId);

    public LobbyHandle GetLobby(ulong lobbyId) => client.GetLobbyHandle(lobbyId);

    public UserHandle GetUser(ulong userId) => client.GetUser(userId);

    public void SendDM(ulong id, string message)
    {
        if (string.IsNullOrEmpty(message))
            return;

        client.SendUserMessage(id, message, SendUserMessageCallback);
    }

    public void SendChannelMsg(ulong channelId, string message)
    {
        if (string.IsNullOrEmpty(message))
            return;

        client.SendLobbyMessage(channelId, message, SendUserMessageCallback);
    }

    public void StartCall(ulong channel)
    {
        client.StartCall(channel);
    }

    public void EndCall(ulong channel)
    {
        client.EndCall(channel, EndVoiceCallCallback);
    }

    public Call GetCall(ulong channelId)
    {
        return client.GetCall(channelId);
    }

    private void AddMsgHistory(ulong id, MessageHandle msg)
    {
        if (!messageHistory.ContainsKey(id))
            messageHistory.Add(id, new List<MessageHandle>());

        var list = messageHistory[id];
        list.Add(msg);

        var excess = list.Count - MAX_MSG_HISTORY;

        if (excess > 0)
            list.RemoveRange(0, excess);
    }

    private void OnUOConnected(object sender, EventArgs e)
    {
        RunLater(JoinGameLobby);
    }

    private void OnPlayerCreated(object sender, EventArgs e)
    {
        RunLater(()=>UpdateRichPresence(true));
    }

    private void OnUODisconnected(object sender, EventArgs e)
    {
        client.UpdateRichPresence(new Activity(), OnUpdateRichPresence); //Reset presence
    }

    private void ClientReady()
    {
        UserId = client.GetCurrentUser().Id();

        connected = true;
        OnConnected?.Invoke();

        RunLater(JoinGlobalLobby);

        richPresenceTimer = new Timer(_=>PeriodicChecks(), null, TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(30));
    }

    private string GenServerSecret()
    {
        return World.ServerName + Settings.GlobalSettings.IP;
    }

    private Dictionary<string, string> GenServerMeta()
    {
        return new Dictionary<string, string>()
        {
            { "name", World.ServerName }
        };
    }

    private void PeriodicChecks()
    {
        RunLater(JoinGlobalLobby);
        RunLater(JoinGameLobby);
        RunLater(()=>UpdateRichPresence());
    }

    private static long furthestAction;

    private static async void RunLater(Action action)
    {
        long now = Time.Ticks;

        // Schedule at least 1s after the last one, or now if no pending delay
        if (now > furthestAction)
            furthestAction = now;

        furthestAction += 2000; // push it forward by 1 second per action

        int delayMs = (int)(furthestAction - now);
        await Task.Delay(delayMs);

        action();
    }

    private static ulong unixStart = (ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    private void UpdateRichPresence(bool includeParty = true)
    {
        Log.Debug("Updating rich presence.");
        Activity activity = new Activity();
        activity.SetName("Ultima Online");
        activity.SetType(ActivityTypes.Playing);

        if (includeParty && World.InGame)
        {
            var party = new ActivityParty();
            party.SetPrivacy(ActivityPartyPrivacy.Public);
            party.SetCurrentSize(1);
            party.SetMaxSize(1);

            party.SetId
                (new ServerInfo(Settings.GlobalSettings.IP, Settings.GlobalSettings.Port.ToString(), World.ServerName, World.Player == null ? 0 : World.Player.Serial).ToJson());

            activity.SetParty(party);
        }

        var ts = new ActivityTimestamps();
        ts.SetStart(unixStart);
        activity.SetTimestamps(ts);

        client.UpdateRichPresence(activity, OnUpdateRichPresence);
    }

    private void SetStatusText(string text)
    {
        statusText = text;
        OnStatusTextUpdated?.Invoke();
    }

    #region Utilities

    private void LoadDiscordSettings()
    {
        var path = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "DiscordSettings.json");

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
        var path = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "DiscordSettings.json");

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
        if (!userHueMemory.ContainsKey(id))
            userHueMemory.Add(id, GetColorFromId(id));

        return userHueMemory[id];
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
        if (!currentLobbies.ContainsKey(lobbyId))
            currentLobbies.Add(lobbyId, client.GetLobbyHandle(lobbyId));

        OnLobbyCreated?.Invoke(currentLobbies[lobbyId]);
    }

    private void JoinGlobalLobby()
    {
        client.CreateOrJoinLobbyWithMetadata(TUOLOBBY, TUOMETA, new Dictionary<string, string>(), GameGameJoinCallback);
    }
    private void JoinGameLobby()
    {
        if (World.InGame)
            client.CreateOrJoinLobbyWithMetadata(GenServerSecret(), GenServerMeta(), new Dictionary<string, string>(), GameLobbyJoinCallback);
    }

    private void GameGameJoinCallback(ClientResult result, ulong lobbyId)
    {
        if (!result.Successful())
        {
            long now = Time.Ticks;
            furthestAction = Math.Max(furthestAction, now);
            furthestAction += 500 + (long)(result.RetryAfter()*1000);
            RunLater(JoinGameLobby);
        }
    }
    private void GameLobbyJoinCallback(ClientResult result, ulong lobbyId)
    {
        if (!result.Successful())
        {
            long now = Time.Ticks;
            furthestAction = Math.Max(furthestAction, now);
            furthestAction += 500 + (long)(result.RetryAfter()*1000);
            RunLater(JoinGameLobby);
        }
    }

    private void OnLobbyDeletedCallback(ulong lobbyId)
    {
        currentLobbies.Remove(lobbyId);

        if(!noreconnect)
        {
            if(connected)
            {
                RunLater(JoinGlobalLobby);
                RunLater(JoinGameLobby);
            }
        }

        OnLobbyDeleted?.Invoke(lobbyId);
    }

    private void OnUserUpdatedCallback(ulong userId)
    {
        OnUserUpdated?.Invoke();
    }

    private void OnMessageCreated(ulong messageId)
    {
        var msg = client.GetMessageHandle(messageId);

        if (msg == null)
            return;

        var id = msg.ChannelId();
        var channel = msg.Channel(); //This msg may be a lobby, which is not a Channel.
        var lobby = msg.Lobby();
        var author = msg.Author();
        bool isdm = false;

        if (channel?.Type() == ChannelType.Dm)
        {
            isdm = true;

            id = msg.AuthorId();

            if (id == UserId)           //Message was sent by us
                id = msg.RecipientId(); //Put this into the msg history for this user
        }

        AddMsgHistory(id, msg);

        if (author == null)
            return;

        OnMessageReceived?.Invoke(msg);
        string chan = "Discord";

        if (!isdm)
            chan = channel != null ? channel.Name() : ((lobby != null) ? GetLobbyName(lobby) : "Discord");

        if ((isdm && DiscordSettings.ShowDMInGame) || (!isdm && DiscordSettings.ShowChatInGame))
            MessageManager.HandleMessage(null, $"{msg.Content()}", $"[{chan}] {author.DisplayName()}", GetHueFromId(author.Id()), MessageType.ChatSystem, 255, TextType.SYSTEM);
    }

    private static void OnLog(string message, LoggingSeverity severity)
    {
        Log.Debug($"Log: {severity} - {message}");
    }

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
                connected = false;

                if (noreconnect)
                    break;

                Log.Debug("Discord disconnected, reconnecting...");
                client.Connect();

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
        var authorizationVerifier = client.CreateAuthorizationCodeVerifier();
        codeVerifier = authorizationVerifier.Verifier();

        var args = new AuthorizationArgs();
        args.SetClientId(CLIENT_ID);
        args.SetScopes(DClient.GetDefaultCommunicationScopes());
        args.SetCodeChallenge(authorizationVerifier.Challenge());
        client.Authorize(args, OnAuthorizeResult);
        authBegan = true;
    }

    public void FromSavedToken()
    {
        var rpath = Path.Combine(CUOEnviroment.ExecutablePath, "Data", ".dratoken");

        if (!File.Exists(rpath))
            return;

        SetStatusText("Attempting to reconnect...");

        try
        {
            var rtoken = Crypter.Decrypt(File.ReadAllText(rpath));

            client.RefreshToken(CLIENT_ID, rtoken, OnTokenExchangeCallback);
            authBegan = true;
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

    private void GetTokenFromCode(string code, string redirectUri)
    {
        client.GetToken(CLIENT_ID, code, codeVerifier, redirectUri, TokenExchangeCallback);
    }

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

        client.UpdateToken(AuthorizationTokenType.Bearer, token, (ClientResult result) => { client.Connect(); });
    }

    private void OnRetrieveTokenFailed()
    {
        SetStatusText("Failed to retrieve token");
    }

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

    public string ToJson()
    {
        return JsonSerializer.Serialize(new ServerInfo(Ip, Port, Name, PlayerSerial), ServerInfoJsonContext.Default.ServerInfo);
    }

    public static ServerInfo FromJson(string json)
    {
        return JsonSerializer.Deserialize(json, ServerInfoJsonContext.Default.ServerInfo);
    }
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(ServerInfo))]
public partial class ServerInfoJsonContext : JsonSerializerContext
{
}
