# Enhanced Packets - Server Side Examples
This file contains C# examples for servers (ModernUO/ServUO style) that want
to support TazUO enhanced packets. The client protocol is documented in
`EnhancedPackets.md`.

All numbers in enhanced packets use **big endian**.

## Registering the packet
The client sends every enhanced packet wrapped in the standard `0xCE`
packet. Register a single handler for `0xCE` and dispatch on the enhanced
packet id inside.

```csharp
public static void Configure()
{
    IncomingPackets.Register(new PacketHandler(0xCE, 0, false, &OnTazUOEnhancedPacket));
}

public static void OnTazUOEnhancedPacket(NetState state, SpanReader reader)
{
    // Skip the standard packet id + length that the networking layer already handled.
    ushort id = reader.ReadUInt16();

    switch (id)
    {
        case 1: // EnableEnhancedPacket
            OnEnableEnhancedPackets(state, reader);
            break;
        case 2: // TazUO_Identifier
            OnTazUOIdentifier(state, reader);
            break;
        default:
            Console.WriteLine($"Got an unknown enhanced packet from TazUO: {id}(0x{id:X2})");
            break;
    }
}
```

## Detecting a TazUO client
The client sends its version as an ASCII string once connected (and after the
server enables enhanced packets).

```csharp
public static void OnTazUOIdentifier(NetState state, SpanReader reader)
{
    string version = reader.ReadAscii();

    Console.WriteLine($"TazUO connected! Version {version}");
    state.IsTazUO = true; // Use elsewhere to branch on the client type.
}
```

## Enabling enhanced packets
Respond to the `EnableEnhancedPacket` client packet (id 1) by sending back a
list of the server-to-client packet ids the client should accept. This also
confirms the client may send its enhanced packets.

```csharp
public static void OnEnableEnhancedPackets(NetState state, SpanReader reader)
{
    // (Optional) Read the packet ids the client already supports:
    // ushort count = reader.ReadUInt16();
    // for (int i = 0; i < count; i++) reader.ReadUInt16();

    // Tell the client which server->client enhanced packets it can receive.
    using PacketWriter writer = new(0xCE);
    writer.Write((ushort)3); // DisableFeatures
    writer.Write((ushort)0); // Version

    writer.Write((ushort)1); // Count of features to disable
    writer.Write((ushort)0); // TreeToStumps (see EnhancedPacketDisabledFeaturesEnum)

    writer.Send(state);
}
```

## Sending the DisableFeatures packet
The server can disable specific client features at any time. The client only
honors feature ids defined in `EnhancedPacketDisabledFeaturesEnum`; unknown
ids are ignored.

```csharp
public static void SendDisableFeatures(NetState state, IEnumerable<ushort> features)
{
    using PacketWriter writer = new(0xCE);
    writer.Write((ushort)3); // DisableFeatures
    writer.Write((ushort)0); // Version

    var list = features as IList<ushort> ?? features.ToList();
    writer.Write((ushort)list.Count);

    foreach (ushort feature in list)
    {
        writer.Write(feature);
    }

    writer.Send(state);
}
```
