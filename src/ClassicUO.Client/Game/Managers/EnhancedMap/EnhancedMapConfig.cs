using System.Text.Json.Serialization;

namespace ClassicUO.Game.Managers.EnhancedMap;

/// <summary>
/// Per-profile settings for the EnhancedMap shared-map service.
/// </summary>
/// <remarks>
/// Held on <see cref="ClassicUO.Configuration.Profile"/> and serialized as part of
/// <c>profile.json</c>, so it rides along with profile load/save rather than a side file. Public
/// get/set properties only: the profile's serializer is not configured for fields.
/// </remarks>
public sealed class EnhancedMapConfig
{
    /// <summary>Master toggle. While off, no connection is attempted and no player data is sent.</summary>
    public bool Enabled { get; set; }

    /// <summary>Host name or IP of the EnhancedMap server.</summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>TCP port of the EnhancedMap server; 8887 is the service default.</summary>
    public int Port { get; set; } = 8887;

    /// <summary>Login account name.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Login password. Stored in plain text in profile.json, same as server credentials.</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>Whether this client broadcasts its own position to the server.</summary>
    public bool SendLocation { get; set; } = true;

    /// <summary>Whether remote alerts are drawn on an in-world overlay.</summary>
    public bool ShowOverlay { get; set; } = true;

    /// <summary>Whether each remote user's HP, stamina and mana bars are drawn under their name on the world map.</summary>
    public bool ShowWorldMapBars { get; set; } = true;

    /// <summary>
    /// True when the connection settings are complete enough to attempt a connection. The master
    /// toggle must be on and a valid host plus port present.
    /// </summary>
    [JsonIgnore]
    public bool IsConfigured => Enabled && !string.IsNullOrWhiteSpace(Address) && Port is > 0 and <= 65535;
}
