using ClassicUO.Game;
using ClassicUO.Game.Managers;
using ClassicUO.IO;

namespace ClassicUO.Network.PacketHandlers;

internal static class TargetCursor
{
    public static void Receive(World world, ref StackDataReader p)
    {
        var cursorTarget = (CursorTarget)p.ReadUInt8();
        uint cursorId = p.ReadUInt32BE();
        var targetType = (TargetType)p.ReadUInt8();

        world.TargetManager.SetTargeting(cursorTarget, cursorId, targetType);

        if (world.Party.PartyHealTimer < Time.Ticks && world.Party.PartyHealTarget != 0)
        {
            world.TargetManager.Target(world.Party.PartyHealTarget);
            world.Party.PartyHealTimer = 0;
            world.Party.PartyHealTarget = 0;
        }
        else if (TargetManager.NextAutoTarget.IsSet && TargetManager.NextAutoTarget.Matches(targetType))
            world.TargetManager.Target(TargetManager.NextAutoTarget.TargetSerial);

        // Always clear after any target cursor (no queuing)
        TargetManager.NextAutoTarget.Clear();
    }
}
