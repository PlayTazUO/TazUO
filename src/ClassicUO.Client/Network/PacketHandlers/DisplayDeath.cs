using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.GameObjects;
using ClassicUO.IO;
using ClassicUO.Renderer.Animations;

namespace ClassicUO.Network.PacketHandlers;

internal static class DisplayDeath
{
    public static void Receive(World world, ref StackDataReader p)
    {
        if (!world.InGame)
            return;

        uint serial = p.ReadUInt32BE();
        uint corpseSerial = p.ReadUInt32BE();
        uint running = p.ReadUInt32BE();

        Mobile owner = world.Mobiles.Get(serial);

        if (owner == null || serial == world.Player)
            return;

        serial |= 0x80000000;

        if (world.Mobiles.Remove(owner.Serial))
        {
            for (LinkedObject i = owner.Items; i != null; i = i.Next)
            {
                var it = (Item)i;
                it.Container = serial;
            }

            world.Mobiles[serial] = owner;
            owner.Serial = serial;
        }

        if (SerialHelper.IsValid(corpseSerial))
        {
            world.CorpseManager.Add(corpseSerial, serial, owner.Direction, running != 0);

            Item corpse = world.Items.Get(corpseSerial);

            if (corpse != null)
                corpse.CorpseParent = serial;
        }

        Animations animations = Client.Game.UO.Animations;
        ushort gfx = owner.Graphic;
        animations.ConvertBodyIfNeeded(ref gfx);
        AnimationGroupsType animGroup = animations.GetAnimType(gfx);
        AnimationFlags animFlags = animations.GetAnimFlags(gfx);
        byte group = Client.Game.UO.FileManager.Animations.GetDeathAction(
            gfx,
            animFlags,
            animGroup,
            running != 0,
            true
        );
        owner.SetAnimation(group, 0, 5, 1);
        owner.AnimIndex = 0;

        if (ProfileManager.CurrentProfile.AutoOpenCorpses)
            world.Player.TryOpenCorpses();
    }
}
