using ClassicUO.Game;
using ClassicUO.Game.Managers;
using ClassicUO.IO;

namespace ClassicUO.Network.PacketHandlers;

internal static class PlaySoundEffect
{
    public static void Receive(World world, ref StackDataReader p)
    {
        if (world.Player == null)
            return;

        p.Skip(1);

        ushort index = p.ReadUInt16BE();
        ushort audio = p.ReadUInt16BE();
        ushort x = p.ReadUInt16BE();
        ushort y = p.ReadUInt16BE();
        short z = (short)p.ReadUInt16BE();

        Client.Game.Audio.PlaySoundWithDistance(world, index, x, y);
        EventSink.InvokeSoundPlayed(new SoundEventArgs(index, x, y));
    }
}
