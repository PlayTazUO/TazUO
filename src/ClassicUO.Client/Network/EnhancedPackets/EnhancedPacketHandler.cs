using System;
using System.Collections.Generic;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.IO;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Network;

public class EnhancedPacketHandler
{
    //Make sure all enhanced packets use BigEndian for numbers. Avoid LittleEndian.
    
    public const byte EPID = 0xCE;
    
    public static bool Enabled => ProfileManager.ServerSettings?.EnableEnhancedPackets ?? true;

    static EnhancedPacketHandler()
    {
        if (!Enabled)
            return;
        
        Handler.Add(EnhancedPacketType.EnableEnhancedPacket, EnableEnhancedPacket);
        Handler.Add(EnhancedPacketType.DisableFeatures, DisableFeatures);
    }

    /// <summary>
    /// Allow servers to enable specific outgoing (client -> server) enhanced packets.
    /// </summary>
    private static void EnableEnhancedPacket(ref StackDataReader p, int version)
    { 
        EnhancedOutgoingPackets.EnabledPackets.Add(EnhancedPacketType.EnableEnhancedPacket);

        if (version >= 0)
        {
            ushort count = p.ReadUInt16BE(); //Number of enhanced packets to follow.

            for (int i = 0; i < count; i++)
            {
                ushort id = p.ReadUInt16BE();

                if (Enum.IsDefined(typeof(EnhancedPacketType), id))
                {
                    EnhancedOutgoingPackets.EnabledPackets.Add((EnhancedPacketType)id);
                }
            }
        }
        
        AsyncNetClient.Socket.SendEnhancedPacket(); //Confirm we are ready to receive enhanced packets.
    }

    /// <summary>
    /// Servers can disable specific features using this packet
    /// </summary>
    private static void DisableFeatures(ref StackDataReader p, int version)
    {
        if (version >= 0)
        {
            ushort count = p.ReadUInt16BE(); //Number of settings sent.

            for (int i = 0; i < count; i++)
            {
                ushort id = p.ReadUInt16BE();

                if (Enum.IsDefined(typeof(EnhancedPacketDisabledFeaturesEnum), id))
                {
                    World.DisabledFeatures.Add((EnhancedPacketDisabledFeaturesEnum)id);
                }
            }
        }
    }

    private readonly Dictionary<ushort, EnhancedOnPacketBufferReader> _handlers = new();
    public delegate void EnhancedOnPacketBufferReader(ref StackDataReader p, int version);
    public static EnhancedPacketHandler Handler { get; } = new();

    public static void Handle(World world, ref StackDataReader p)
    {
        ushort id = p.ReadUInt16BE();
        ushort ver = p.ReadUInt16BE();
        Handler.HandlePacket(id, ref p, ver);
    }

    public void Add(EnhancedPacketType type, EnhancedOnPacketBufferReader handler) => _handlers[(ushort)type] = handler;

    public void HandlePacket(ushort packetID, ref StackDataReader p, int version)
    {
        if (!Enabled)
            return;
        
        if (_handlers.ContainsKey(packetID))
        {
            _handlers[packetID].Invoke(ref p, version);
        }
        else
        {
            Log.Error($"Received invalid enhanced packet {packetID} (0x{packetID:X}) len={p.Length}");
        }
    }
}