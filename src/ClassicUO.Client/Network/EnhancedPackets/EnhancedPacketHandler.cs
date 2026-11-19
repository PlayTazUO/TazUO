using System;
using System.Collections.Generic;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.IO;
using ClassicUO.Network.PacketHandlers.Helpers;
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
        Handler.Add(EnhancedPacketType.MobileScale, MobileScale);
    }

    /// <summary>
    /// Allow servers to enable specific outgoing (client -> server) enhanced packets.
    /// </summary>
    private static void EnableEnhancedPacket(World world, ref StackDataReader p, int version)
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
    private static void DisableFeatures(World world, ref StackDataReader p, int version)
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

    /// <summary>
    /// Creates or updates a mobile using the same logic as the standard mobile update packets,
    /// with an explicit render scale applied to <see cref="ClassicUO.Game.GameObjects.Mobile.Scale"/>.
    /// </summary>
    private static void MobileScale(World world, ref StackDataReader p, int version)
    {
        uint serial = p.ReadUInt32BE();
        ushort graphic = p.ReadUInt16BE();
        ushort x = p.ReadUInt16BE();
        ushort y = p.ReadUInt16BE();
        sbyte z = p.ReadInt8();
        var direction = (Direction)p.ReadUInt8();
        ushort hue = p.ReadUInt16BE();
        var flags = (Flags)p.ReadUInt8();
        float scale = BitConverter.UInt32BitsToSingle(p.ReadUInt32BE());

        if (!SerialHelper.IsMobile(serial) || !float.IsFinite(scale) || scale <= 0f)
            return;

        ObjectHelpers.UpdateGameObject(world, serial, graphic, 0, 0, x, y, z, direction, hue, flags, 0, 0, 1, scale);
    }

    private readonly Dictionary<ushort, EnhancedOnPacketBufferReader> _handlers = new();
    public delegate void EnhancedOnPacketBufferReader(World world, ref StackDataReader p, int version);
    public static EnhancedPacketHandler Handler { get; } = new();

    public static void Handle(World world, ref StackDataReader p)
    {
        ushort id = p.ReadUInt16BE();
        ushort ver = p.ReadUInt16BE();
        Handler.HandlePacket(world, id, ref p, ver);
    }

    public void Add(EnhancedPacketType type, EnhancedOnPacketBufferReader handler) => _handlers[(ushort)type] = handler;

    public void HandlePacket(World world, ushort packetID, ref StackDataReader p, int version)
    {
        if (!Enabled)
            return;
        
        if (_handlers.ContainsKey(packetID))
        {
            _handlers[packetID].Invoke(world, ref p, version);
        }
        else
        {
            Log.Error($"Received invalid enhanced packet {packetID} (0x{packetID:X}) len={p.Length}");
        }
    }
}