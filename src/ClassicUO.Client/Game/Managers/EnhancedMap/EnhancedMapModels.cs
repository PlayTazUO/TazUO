using System;

namespace ClassicUO.Game.Managers.EnhancedMap;

/// <summary>A raw EnhancedMap frame handed from the network thread to the game thread.</summary>
/// <param name="Id">The frame's packet id.</param>
/// <param name="Payload">The frame payload with the three-byte header removed.</param>
internal readonly record struct EnhancedMapFrame(byte Id, byte[] Payload);

/// <summary>State for another player reported by the EnhancedMap server (packet 0x25).</summary>
/// <remarks>
/// Written only on the game thread, so no locking is needed for reads that also happen there
/// (rendering, snapshotting). The name is the identity key the server uses.
/// </remarks>
internal sealed class EnhancedMapRemoteUser
{
    public required string Name { get; init; }

    public int X { get; set; }
    public int Y { get; set; }
    public byte Map { get; set; }

    public ushort Hits { get; set; }
    public ushort Stamina { get; set; }
    public ushort Mana { get; set; }
    public ushort MaxHits { get; set; }
    public ushort MaxStamina { get; set; }
    public ushort MaxMana { get; set; }

    public byte Flag { get; set; }
    public bool Panic { get; set; }
    public uint Color { get; set; }

    public long LastUpdate { get; set; }

    /// <summary>True when the reported hits are zero, matching the client's death convention.</summary>
    public bool IsDead => Flag == EnhancedMapProtocol.FlagDead;
}

/// <summary>A temporary point marker raised by another player (packet 0x27).</summary>
internal sealed class EnhancedMapAlert
{
    public required string Name { get; init; }

    public int X { get; init; }
    public int Y { get; init; }

    public long ExpiresAt { get; init; }
}

/// <summary>A shared map label exchanged between clients (packet 0x31).</summary>
internal sealed class EnhancedMapLabel
{
    public required string Name { get; init; }

    public int X { get; set; }
    public int Y { get; set; }
    public byte Map { get; set; }
    public string Description { get; set; } = string.Empty;
    public long LastUpdate { get; set; }
}
