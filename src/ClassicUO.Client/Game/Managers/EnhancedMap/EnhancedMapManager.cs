using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.MyraWindows;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Managers.EnhancedMap;

/// <summary>
/// Orchestrates the EnhancedMap integration: owns the protocol client, keeps the state the server
/// reports about other players, and drives the in-world overlay.
/// </summary>
/// <remarks>
/// Started from <c>GameScene.Load</c> and ticked from <c>GameScene.Update</c>, so every mutation of
/// the state below happens on the game thread. The client's network thread only enqueues raw frames
/// onto <see cref="ConcurrentQueue{T}"/>; <see cref="Update"/> drains and applies them. This keeps
/// the render path lock-free.
/// </remarks>
internal sealed class EnhancedMapManager
{
    private const long SendIntervalMs = 1000;
    private const long ReconnectDelayMs = 10_000;
    private const long SettingsPromptDelayMs = 2000;
    private const long AlertLifetimeMs = 8000;
    private const long UserTimeoutMs = 6000;

    private static EnhancedMapManager _instance;

    private readonly EnhancedMapClient _client;
    private readonly ConcurrentQueue<EnhancedMapFrame> _frames = new();
    private readonly Dictionary<string, EnhancedMapRemoteUser> _users = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, EnhancedMapAlert> _alerts = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, EnhancedMapLabel> _labels = new(StringComparer.OrdinalIgnoreCase);

    private bool _started;
    private long _lastSend;
    private long _lastConnect;
    private long _lastSweep;
    private long _settingsPromptQueuedAt;
    private bool _settingsPromptQueued;

    private EnhancedMapOverlayGump _overlayGump;

    private EnhancedMapManager()
    {
        _client = new EnhancedMapClient
        {
            OnFrame = EnqueueFrame,
            OnDisconnected = OnClientDisconnected
        };
    }

    public static EnhancedMapManager Instance => _instance ??= new EnhancedMapManager();

    /// <summary>The active profile's EnhancedMap settings, or null when no profile is loaded.</summary>
    public static EnhancedMapConfig Config => ProfileManager.CurrentProfile?.EnhancedMap;

    /// <summary>Remote users keyed by their reported name. Read on the game thread.</summary>
    public IReadOnlyDictionary<string, EnhancedMapRemoteUser> Users => _users;

    /// <summary>Active point markers raised by other players. Read on the game thread.</summary>
    public IReadOnlyDictionary<string, EnhancedMapAlert> Alerts => _alerts;

    /// <summary>Shared labels exchanged with other players. Read on the game thread.</summary>
    public IReadOnlyDictionary<string, EnhancedMapLabel> Labels => _labels;

    /// <summary>True while the socket is open and the server has accepted our login.</summary>
    public bool IsConnected => _client.IsAlive && _client.IsAccepted;

    /// <summary>True when an alert can actually be delivered right now.</summary>
    public bool CanSendAlert => IsConnected;

    /// <summary>
    /// The name attached to the most recently sent alert, kept so the UI can offer it for quick
    /// reuse. Not sent on the wire: the server stamps the sender's account name onto the alert.
    /// </summary>
    public string LastAlertName { get; set; } = string.Empty;

    /// <summary>
    /// Sends a one-shot alert marker at a world coordinate. Coordinates are clamped to the
    /// protocol's ushort range; the server does not validate them.
    /// </summary>
    /// <param name="x">World X.</param>
    /// <param name="y">World Y.</param>
    public void SendAlert(int x, int y)
    {
        if (!CanSendAlert)
            return;

        _client.SendAlert(
            (ushort)Math.Clamp(x, 0, ushort.MaxValue),
            (ushort)Math.Clamp(y, 0, ushort.MaxValue));
    }

    /// <summary>
    /// Adds a transient alert marker locally. The server excludes the sender from its broadcast,
    /// so this is what lets the sender see their own alert.
    /// </summary>
    /// <param name="x">World X.</param>
    /// <param name="y">World Y.</param>
    /// <param name="name">Label shown on the marker.</param>
    public void AddLocalAlert(int x, int y, string name)
    {
        if (string.IsNullOrEmpty(name))
            return;

        _alerts[name] = new EnhancedMapAlert
        {
            Name = name,
            X = x,
            Y = y,
            ExpiresAt = Environment.TickCount64 + AlertLifetimeMs
        };
    }

    /// <summary>Begins the integration for the current profile. Called when the world scene loads.</summary>
    public void Start()
    {
        _started = true;

        EnhancedMapConfig config = Config;

        if (config == null)
            return;

        if (config.Enabled && !config.IsConfigured)
        {
            _settingsPromptQueued = true;
            _settingsPromptQueuedAt = Environment.TickCount64;
        }

        SyncOverlayGump(config);

        if (config.IsConfigured)
            Connect(config);
    }

    /// <summary>Stops the integration and clears all remote state. Called when the world scene unloads.</summary>
    public void Stop()
    {
        _started = false;
        _settingsPromptQueued = false;
        _client.Disconnect();
        ClearState();

        _overlayGump?.Dispose();
        _overlayGump = null;
    }

    /// <summary>Ticks the manager. Call once per frame while in-world.</summary>
    public void Update()
    {
        if (!_started)
            return;

        DrainFrames();

        long now = Environment.TickCount64;

        ExpireState(now);

        EnhancedMapConfig config = Config;

        if (config == null)
            return;

        SyncOverlayGump(config);

        if (_settingsPromptQueued && now - _settingsPromptQueuedAt >= SettingsPromptDelayMs)
        {
            _settingsPromptQueued = false;
            OpenSettingsWindow();
        }

        if (!config.Enabled || !config.IsConfigured)
        {
            if (_client.IsAlive)
                _client.Disconnect();

            return;
        }

        if (!_client.IsAlive)
        {
            if (now - _lastConnect >= ReconnectDelayMs)
                Connect(config);
        }
        else if (!_client.IsAccepted && now - _lastConnect >= ReconnectDelayMs)
        {
            // Alive but still not accepted: the login stalled past the reconnect window, so
            // drop it. The next tick's reconnect check will start a fresh attempt.
            _client.Disconnect();
        }

        if (!_client.IsAccepted || !config.SendLocation)
            return;

        if (now - _lastSend >= SendIntervalMs)
        {
            _lastSend = now;
            SendLocalPlayer();
        }
    }

    /// <summary>Drops the current connection and immediately reconnects using the current settings.</summary>
    public void Reconnect()
    {
        EnhancedMapConfig config = Config;

        if (config == null)
            return;

        _client.Disconnect();
        ClearState();

        if (config.IsConfigured)
            Connect(config);
    }

    /// <summary>Opens the EnhancedMap settings window, focusing an existing instance if present.</summary>
    public static void OpenSettingsWindow()
    {
        string title = TazLang.Get("enhancedmap_settings_title", "EnhancedMap");

        QuickOptionsWindow existing = QuickOptionsWindow.GetExisting(title);

        if (existing != null)
        {
            existing.CenterInScreen();
            existing.BringOnTop();
            return;
        }

        EnhancedMapConfig config = Config;

        if (config == null)
            return;

        var window = new QuickOptionsWindow(title);

        window.AddLabel(TazLang.Get("enhancedmap_settings_desc", "Connect to an EnhancedMap server to share your position and see other players."));

        window.AddCheckbox(
            TazLang.Get("enhancedmap_enabled", "Enable EnhancedMap"),
            config.Enabled,
            value => config.Enabled = value
        );

        window.AddInput(
            TazLang.Get("enhancedmap_address", "Server address"),
            config.Address,
            value => config.Address = value.Trim()
        );

        window.AddInput(
            TazLang.Get("enhancedmap_port", "Port"),
            config.Port.ToString(),
            value =>
            {
                if (int.TryParse(value, out int port) && port is > 0 and <= 65535)
                    config.Port = port;
            },
            100,
            inputFilter: MyraInputBox.DigitInputFilter
        );

        window.AddInput(
            TazLang.Get("enhancedmap_username", "Username"),
            config.Username,
            value => config.Username = value
        );

        window.AddInput(
            TazLang.Get("enhancedmap_password", "Password"),
            config.Password,
            value => config.Password = value,
            tooltip: TazLang.Get("enhancedmap_password_tooltip", "Stored unencrypted in your profile."),
            isPassword: true
        );

        window.AddCheckbox(
            TazLang.Get("enhancedmap_send_location", "Share my location"),
            config.SendLocation,
            value => config.SendLocation = value
        );

        window.AddCheckbox(
            TazLang.Get("enhancedmap_show_overlay", "Show in-world overlay"),
            config.ShowOverlay,
            value => config.ShowOverlay = value
        );

        window.AddCheckbox(
            TazLang.Get("enhancedmap_show_world_map_bars", "Show HP/stamina/mana bars on world map"),
            config.ShowWorldMapBars,
            value => config.ShowWorldMapBars = value
        );

        window.AddButton(TazLang.Get("enhancedmap_reconnect", "Reconnect"), Instance.Reconnect);
    }

    private void Connect(EnhancedMapConfig config)
    {
        _lastConnect = Environment.TickCount64;
        ClearState();
        _client.Connect(config.Address, config.Port, config.Username, config.Password);
    }

    #region Incoming

    private void EnqueueFrame(byte id, byte[] payload) => _frames.Enqueue(new EnhancedMapFrame(id, payload));

    private void OnClientDisconnected(bool graceful)
    {
        if (!graceful)
            Log.Info("EnhancedMap: connection lost, will retry with the next reconnect window.");
    }

    private void DrainFrames()
    {
        while (_frames.TryDequeue(out EnhancedMapFrame frame))
            ApplyFrame(frame.Id, frame.Payload);
    }

    private void ApplyFrame(byte id, byte[] payload)
    {
        switch (id)
        {
            case EnhancedMapProtocol.UserData:
                ApplyUserData(payload);
                break;

            case EnhancedMapProtocol.UserConnection:
                ApplyUserConnection(payload);
                break;

            case EnhancedMapProtocol.AlertAdvice:
                ApplyAlert(payload);
                break;

            case EnhancedMapProtocol.SharedLabel:
                ApplySharedLabel(payload);
                break;

            case EnhancedMapProtocol.ChatMessage:
                ApplyChatMessage(payload);
                break;

            case EnhancedMapProtocol.ServerMessage:
                ApplyServerMessage(payload);
                break;

            case EnhancedMapProtocol.ServerResponse:
                ApplyServerResponse(payload);
                break;
        }
    }

    private void ApplyUserData(byte[] payload)
    {
        var reader = new EmPacketReader(payload);

        int x = reader.U16();
        int y = reader.U16();
        byte map = reader.U8();

        ushort hits = reader.U16();
        ushort stamina = reader.U16();
        ushort mana = reader.U16();
        ushort maxHits = reader.U16();
        ushort maxStamina = reader.U16();
        ushort maxMana = reader.U16();

        byte flag = reader.U8();
        reader.Skip(1); // legacy death byte
        bool panic = reader.U8() != 0;
        uint color = reader.U32();

        byte fontNameLen = reader.U8();
        reader.Ascii(fontNameLen);
        reader.Skip(4); // font size (float)
        reader.U8();    // font style

        string name = reader.Ascii(reader.U8());

        if (reader.Failed || string.IsNullOrEmpty(name))
            return;

        if (!_users.TryGetValue(name, out EnhancedMapRemoteUser user))
        {
            user = new EnhancedMapRemoteUser { Name = name };
            _users[name] = user;
        }

        user.X = x;
        user.Y = y;
        user.Map = map;
        user.Hits = hits;
        user.Stamina = stamina;
        user.Mana = mana;
        user.MaxHits = maxHits;
        user.MaxStamina = maxStamina;
        user.MaxMana = maxMana;
        user.Flag = flag;
        user.Panic = panic;
        user.Color = color;
        user.LastUpdate = Environment.TickCount64;
    }

    private void ApplyUserConnection(byte[] payload)
    {
        var reader = new EmPacketReader(payload);

        bool connected = reader.U8() != 0;
        string name = reader.Ascii(reader.U8());

        if (reader.Failed || string.IsNullOrEmpty(name))
            return;

        if (connected)
            return;

        _users.Remove(name);
        _labels.Remove(name);
    }

    private void ApplyAlert(byte[] payload)
    {
        var reader = new EmPacketReader(payload);

        int x = reader.U16();
        int y = reader.U16();
        string name = reader.Ascii(reader.U8());

        if (reader.Failed || string.IsNullOrEmpty(name))
            return;

        _alerts[name] = new EnhancedMapAlert
        {
            Name = name,
            X = x,
            Y = y,
            ExpiresAt = Environment.TickCount64 + AlertLifetimeMs
        };
    }

    private void ApplySharedLabel(byte[] payload)
    {
        var reader = new EmPacketReader(payload);

        bool remove = reader.U8() != 0;
        int x = reader.U16();
        int y = reader.U16();
        byte map = reader.U8();

        if (remove)
        {
            string name = reader.Ascii(reader.U8());

            if (!reader.Failed && !string.IsNullOrEmpty(name))
                _labels.Remove(name);

            return;
        }

        string description = reader.Ascii(reader.U8());
        string labelName = reader.Ascii(reader.U8());

        if (reader.Failed || string.IsNullOrEmpty(labelName))
            return;

        _labels[labelName] = new EnhancedMapLabel
        {
            Name = labelName,
            X = x,
            Y = y,
            Map = map,
            Description = description,
            LastUpdate = Environment.TickCount64
        };
    }

    private void ApplyChatMessage(byte[] payload)
    {
        var reader = new EmPacketReader(payload);

        ushort messageLength = reader.U16();
        reader.U32(); // color (ARGB), not representable as a UO hue
        string message = reader.Ascii(messageLength);
        string name = reader.Ascii(reader.U8());

        if (reader.Failed || string.IsNullOrEmpty(message))
            return;

        GameActions.Print(string.IsNullOrEmpty(name) ? $"[EnhancedMap] {message}" : $"[EnhancedMap] {name}: {message}");
    }

    private void ApplyServerMessage(byte[] payload)
    {
        var reader = new EmPacketReader(payload);

        string message = reader.Ascii(reader.U16());
        reader.U32(); // color

        if (reader.Failed || string.IsNullOrEmpty(message))
            return;

        GameActions.Print($"[EnhancedMap] {message}");
    }

    private void ApplyServerResponse(byte[] payload)
    {
        var reader = new EmPacketReader(payload);

        reader.U8(); // response type
        string message = reader.Ascii(reader.U16());

        if (reader.Failed || string.IsNullOrEmpty(message))
            return;

        GameActions.Print($"[EnhancedMap] {message}");
    }

    #endregion

    #region Outgoing

    private void SendLocalPlayer()
    {
        World world = World.Instance;
        PlayerMobile player = world?.Player;

        if (player == null)
            return;

        byte flag = EnhancedMapProtocol.FlagNormal;

        if (player.IsDead)
            flag = EnhancedMapProtocol.FlagDead;
        else if (player.IsHidden)
            flag = EnhancedMapProtocol.FlagHidden;
        else if (player.IsPoisoned)
            flag = EnhancedMapProtocol.FlagPoisoned;

        byte map = (byte)Math.Max(0, world.MapIndex);

        _client.SendPlayerData(
            player.X,
            player.Y,
            map,
            player.Hits, player.Stamina, player.Mana,
            player.HitsMax, player.StaminaMax, player.ManaMax,
            flag,
            false,
            0xFFFFFFFF,
            "Segoe UI",
            12f,
            0
        );
    }

    #endregion

    #region Overlay

    private void SyncOverlayGump(EnhancedMapConfig config)
    {
        if (config.Enabled && config.ShowOverlay)
        {
            if (_overlayGump == null || _overlayGump.IsDisposed)
            {
                _overlayGump = new EnhancedMapOverlayGump(World.Instance);

                // Back of the gump stack: over the world, but under real UI windows.
                UIManager.Add(_overlayGump, false);
            }
        }
        else if (_overlayGump != null)
        {
            _overlayGump.Dispose();
            _overlayGump = null;
        }
    }

    #endregion

    #region State lifetime

    private void ExpireState(long now)
    {
        if (now - _lastSweep < 1000)
            return;

        _lastSweep = now;

        RemoveStale(_users, now, UserTimeoutMs);

        if (_alerts.Count > 0)
        {
            List<string> expired = null;

            foreach (EnhancedMapAlert alert in _alerts.Values)
            {
                if (alert.ExpiresAt <= now)
                    (expired ??= new List<string>()).Add(alert.Name);
            }

            if (expired != null)
            {
                foreach (string name in expired)
                    _alerts.Remove(name);
            }
        }
    }

    private static void RemoveStale(Dictionary<string, EnhancedMapRemoteUser> users, long now, long timeout)
    {
        List<string> stale = null;

        foreach (EnhancedMapRemoteUser user in users.Values)
        {
            if (now - user.LastUpdate > timeout)
                (stale ??= new List<string>()).Add(user.Name);
        }

        if (stale != null)
        {
            foreach (string name in stale)
                users.Remove(name);
        }
    }

    private void ClearState()
    {
        _users.Clear();
        _alerts.Clear();
        _labels.Clear();
    }

    #endregion
}
