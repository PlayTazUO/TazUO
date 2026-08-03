using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.IO;

namespace ClassicUO.Network.PacketHandlers;

internal static class CharacterStatus
{
    public static void Receive(World world, ref StackDataReader p)
    {
        if (world.Player == null)
            return;

        uint serial = p.ReadUInt32BE();
        Entity entity = world.Get(serial);

        if (entity == null)
            return;

        string oldName = entity.Name;
        ushort oldHits = entity.Hits;
        entity.Name = p.ReadASCII(30);
        entity.Hits = p.ReadUInt16BE();
        entity.HitsMax = p.ReadUInt16BE();

        if (entity.HitsRequest == HitsRequestStatus.Pending)
            entity.HitsRequest = HitsRequestStatus.Received;

        if (SerialHelper.IsMobile(serial))
        {
            var mobile = entity as Mobile;

            if (mobile == null)
                return;

            mobile.IsRenamable = p.ReadBool();
            byte type = p.ReadUInt8();

            if (type > 0 && p.Position + 1 <= p.Length)
            {
                mobile.IsFemale = p.ReadBool();

                if (mobile == world.Player)
                {
                    if (
                        !string.IsNullOrEmpty(world.Player.Name) && oldName != world.Player.Name
                    )
                    {
                        Client.Game.SetWindowTitle(world.Player.Name);
                        if (ProfileManager.CurrentProfile?.EnableTitleBarStats == true)
                            TitleBarStatsManager.ForceUpdate();
                    }

                    ushort str = p.ReadUInt16BE();
                    ushort dex = p.ReadUInt16BE();
                    ushort intell = p.ReadUInt16BE();
                    world.Player.Stamina = p.ReadUInt16BE();
                    world.Player.StaminaMax = p.ReadUInt16BE();
                    world.Player.Mana = p.ReadUInt16BE();
                    world.Player.ManaMax = p.ReadUInt16BE();
                    world.Player.Gold = p.ReadUInt32BE();
                    world.Player.PhysicalResistance = (short)p.ReadUInt16BE();
                    world.Player.Weight = p.ReadUInt16BE();

                    if (
                        world.Player.Strength != 0
                        && ProfileManager.CurrentProfile != null
                        && ProfileManager.CurrentProfile.ShowStatsChangedMessage
                    )
                    {
                        ushort currentStr = world.Player.Strength;
                        ushort currentDex = world.Player.Dexterity;
                        ushort currentInt = world.Player.Intelligence;

                        int deltaStr = str - currentStr;
                        int deltaDex = dex - currentDex;
                        int deltaInt = intell - currentInt;

                        if (deltaStr != 0)
                            GameActions.Print(
                                world,
                                string.Format(
                                    TazLang.Get("your0_has_changed_by1_it_is_now2"),
                                    TazLang.Get("strength_lower"),
                                    deltaStr,
                                    str
                                ),
                                0x0170,
                                MessageType.System,
                                3,
                                false
                            );

                        if (deltaDex != 0)
                            GameActions.Print(
                                world,
                                string.Format(
                                    TazLang.Get("your0_has_changed_by1_it_is_now2"),
                                    TazLang.Get("dexterity_lower"),
                                    deltaDex,
                                    dex
                                ),
                                0x0170,
                                MessageType.System,
                                3,
                                false
                            );

                        if (deltaInt != 0)
                            GameActions.Print(
                                world,
                                string.Format(
                                    TazLang.Get("your0_has_changed_by1_it_is_now2"),
                                    TazLang.Get("intelligence_lower"),
                                    deltaInt,
                                    intell
                                ),
                                0x0170,
                                MessageType.System,
                                3,
                                false
                            );
                    }

                    world.Player.Strength = str;
                    world.Player.Dexterity = dex;
                    world.Player.Intelligence = intell;

                    AutoStatLockManager.Instance.OnStatsUpdated(world);

                    if (type >= 5) //ML
                    {
                        world.Player.WeightMax = p.ReadUInt16BE();
                        byte race = p.ReadUInt8();

                        if (race == 0)
                            race = 1;

                        world.Player.Race = (RaceType)race;
                    }
                    else
                    {
                        if (Client.Game.UO.Version >= Utility.ClientVersion.CV_500A)
                            world.Player.WeightMax = (ushort)(
                                7 * (world.Player.Strength >> 1) + 40
                            );
                        else
                            world.Player.WeightMax = (ushort)(world.Player.Strength * 4 + 25);
                    }

                    if (type >= 3) //Renaissance
                    {
                        world.Player.StatsCap = (short)p.ReadUInt16BE();
                        world.Player.Followers = p.ReadUInt8();
                        world.Player.FollowersMax = p.ReadUInt8();
                    }

                    if (type >= 4) //AOS
                    {
                        world.Player.FireResistance = (short)p.ReadUInt16BE();
                        world.Player.ColdResistance = (short)p.ReadUInt16BE();
                        world.Player.PoisonResistance = (short)p.ReadUInt16BE();
                        world.Player.EnergyResistance = (short)p.ReadUInt16BE();
                        world.Player.Luck = p.ReadUInt16BE();
                        world.Player.DamageMin = (short)p.ReadUInt16BE();
                        world.Player.DamageMax = (short)p.ReadUInt16BE();
                        world.Player.TithingPoints = p.ReadUInt32BE();
                    }

                    if (type >= 6)
                    {
                        world.Player.MaxPhysicResistence =
                            p.Position + 2 > p.Length ? (short)0 : (short)p.ReadUInt16BE();
                        world.Player.MaxFireResistence =
                            p.Position + 2 > p.Length ? (short)0 : (short)p.ReadUInt16BE();
                        world.Player.MaxColdResistence =
                            p.Position + 2 > p.Length ? (short)0 : (short)p.ReadUInt16BE();
                        world.Player.MaxPoisonResistence =
                            p.Position + 2 > p.Length ? (short)0 : (short)p.ReadUInt16BE();
                        world.Player.MaxEnergyResistence =
                            p.Position + 2 > p.Length ? (short)0 : (short)p.ReadUInt16BE();
                        world.Player.DefenseChanceIncrease =
                            p.Position + 2 > p.Length ? (short)0 : (short)p.ReadUInt16BE();
                        world.Player.MaxDefenseChanceIncrease =
                            p.Position + 2 > p.Length ? (short)0 : (short)p.ReadUInt16BE();
                        world.Player.HitChanceIncrease =
                            p.Position + 2 > p.Length ? (short)0 : (short)p.ReadUInt16BE();
                        world.Player.SwingSpeedIncrease =
                            p.Position + 2 > p.Length ? (short)0 : (short)p.ReadUInt16BE();
                        world.Player.DamageIncrease =
                            p.Position + 2 > p.Length ? (short)0 : (short)p.ReadUInt16BE();
                        world.Player.LowerReagentCost =
                            p.Position + 2 > p.Length ? (short)0 : (short)p.ReadUInt16BE();
                        world.Player.SpellDamageIncrease =
                            p.Position + 2 > p.Length ? (short)0 : (short)p.ReadUInt16BE();
                        world.Player.FasterCastRecovery =
                            p.Position + 2 > p.Length ? (short)0 : (short)p.ReadUInt16BE();
                        world.Player.FasterCasting =
                            p.Position + 2 > p.Length ? (short)0 : (short)p.ReadUInt16BE();
                        world.Player.LowerManaCost =
                            p.Position + 2 > p.Length ? (short)0 : (short)p.ReadUInt16BE();
                    }
                }
            }

            if (mobile == world.Player) TitleBarStatsManager.UpdateTitleBar();

            // Check for bandage healing
            if (oldHits != mobile.Hits) BandageManager.Instance.OnMobileHpChanged(mobile, oldHits, mobile.Hits);

            if (mobile.IsRenamable && ProfileManager.CurrentProfile.EnablePetScaling)
                mobile.Scale = 0.6f; //Customizable later
        }
    }
}
