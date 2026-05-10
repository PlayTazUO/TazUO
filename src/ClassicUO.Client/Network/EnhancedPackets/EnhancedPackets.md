# Enhanced Packet Support
TazUO has begun implementing enhanced packet support for servers to utilize.

This is not quite ready yet, more details to come.

## Details
- Supports up to `65534` packets.
- Servers can enable specific packets
- Players can disable enhanced packets if desired.

## Packet ID
- Currently using `0xCE` for the enhanced packet id. (This is needed to incorporate with the current packet system)

# Packets
## Packet Headers
- This is sent at the beginning of every packet.
### Outgoing -> Server
```cs
byte   ENHANCEDPACKETID //Used to incorporate with current packet system, this is the same on all enhanced packets.
ushort LENGTH //Length of the packet sent
ushort PACKETID //Enhanced packet id, ranging from 1-65535
```
### Incoming -> Client
```cs
ushort ID       // 1
ushort VERSION  // 0
```

## EnhancedPacketEnabler : 1
### Client -> Server
```cs
HEADER
```

### Server -> Client
```cs
HEADER
ushort COUNT  // Enhanced Packets Support Count
for x int count
    ushort ID //Enhanced packet id to enable
```

## TazUO Identifier : 2
### Client -> Server
```cs
HEADER
string TazUO Version
```
