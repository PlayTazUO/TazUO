using ClassicUO.Assets;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.IO;

namespace ClassicUO.Network.PacketHandlers;

internal static class EnableLockedFeatures
{
    public static void Receive(World world, ref StackDataReader p)
    {
        LockedFeatureFlags flags = 0;

        if (Client.Game.UO.Version >= Utility.ClientVersion.CV_60142 && !AsyncNetClient.PacketsTable.LegacyLoginFormat)
            flags = (LockedFeatureFlags)p.ReadUInt32BE();
        else
            flags = (LockedFeatureFlags)p.ReadUInt16BE();

        world.ClientLockedFeatures.SetFlags(flags);

        world.ChatManager.ChatIsEnabled = world.ClientLockedFeatures.Flags.HasFlag(
            LockedFeatureFlags.T2A
        )
            ? ChatStatus.Enabled
            : 0;

        BodyConvFlags bcFlags = 0;
        if (flags.HasFlag(LockedFeatureFlags.UOR))
            bcFlags |= BodyConvFlags.Anim1 | BodyConvFlags.Anim2;
        if (flags.HasFlag(LockedFeatureFlags.LBR))
            bcFlags |= BodyConvFlags.Anim1;
        if (flags.HasFlag(LockedFeatureFlags.AOS))
            bcFlags |= BodyConvFlags.Anim2;
        if (flags.HasFlag(LockedFeatureFlags.SE))
            bcFlags |= BodyConvFlags.Anim3;
        if (flags.HasFlag(LockedFeatureFlags.ML))
            bcFlags |= BodyConvFlags.Anim4;

        Client.Game.UO.Animations.UpdateAnimationTable(bcFlags);
    }
}
