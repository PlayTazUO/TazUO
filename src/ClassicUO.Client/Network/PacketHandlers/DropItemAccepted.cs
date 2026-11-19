using ClassicUO.Game;
using ClassicUO.Game.Managers;
using ClassicUO.IO;

namespace ClassicUO.Network.PacketHandlers;

internal static class DropItemAccepted
{
    public static void Receive(World world, ref StackDataReader p)
    {
        if (!world.InGame)
            return;

        ObjectActionQueue.Instance.ClearPendingMove();

        Client.Game.UO.GameCursor.ItemHold.Enabled = false;
        Client.Game.UO.GameCursor.ItemHold.Dropped = false;
    }
}
