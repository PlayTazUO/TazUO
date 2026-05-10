using System;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.IO;
using ClassicUO.Resources;

namespace ClassicUO.Network.PacketHandlers;

internal static class UpdateSkills
{
    public static void Receive(World world, ref StackDataReader p)
    {
        if (!world.InGame)
            return;

        byte type = p.ReadUInt8();
        bool haveCap = (type != 0u && type <= 0x03) || type == 0xDF;
        bool isSingleUpdate = type == 0xFF || type == 0xDF;

        if (type == 0xFE)
        {
            int count = p.ReadUInt16BE();

            Client.Game.UO.FileManager.Skills.Skills.Clear();
            Client.Game.UO.FileManager.Skills.SortedSkills.Clear();

            for (int i = 0; i < count; i++)
            {
                bool haveButton = p.ReadBool();
                int nameLength = p.ReadUInt8();

                Client.Game.UO.FileManager.Skills.Skills.Add(
                    new SkillEntry(i, p.ReadASCII(nameLength), haveButton)
                );
            }

            Client.Game.UO.FileManager.Skills.SortedSkills.AddRange(Client.Game.UO.FileManager.Skills.Skills);

            Client.Game.UO.FileManager.Skills.SortedSkills.Sort((a, b) =>
                string.Compare(a.Name, b.Name, StringComparison.InvariantCulture)
            );
        }
        else
        {
            StandardSkillsGump standard = null;
            SkillGumpAdvanced advanced = null;

            if (ProfileManager.CurrentProfile.StandardSkillsGump)
                standard = UIManager.GetGump<StandardSkillsGump>();
            else
                advanced = UIManager.GetGump<SkillGumpAdvanced>();

            if (!isSingleUpdate && (type == 1 || type == 3 || world.SkillsRequested))
            {
                world.SkillsRequested = false;

                // TODO: make a base class for this gump
                if (ProfileManager.CurrentProfile.StandardSkillsGump)
                {
                    if (standard == null)
                        UIManager.Add(standard = new StandardSkillsGump(world) { X = 100, Y = 100 });
                }
                else
                {
                    if (advanced == null)
                        UIManager.Add(advanced = new SkillGumpAdvanced(world) { X = 100, Y = 100 });
                }
            }

            while (p.Position < p.Length)
            {
                ushort id = p.ReadUInt16BE();

                if (p.Position >= p.Length)
                    break;

                if (id == 0 && type == 0)
                    break;

                if (type == 0 || type == 0x02)
                    id--;

                ushort realVal = p.ReadUInt16BE();
                ushort baseVal = p.ReadUInt16BE();
                var locked = (Lock)p.ReadUInt8();
                ushort cap = 1000;

                if (haveCap)
                    cap = p.ReadUInt16BE();

                if (id < world.Player.Skills.Length)
                {
                    Skill skill = world.Player.Skills[id];

                    if (skill != null)
                    {
                        if (isSingleUpdate)
                        {
                            float change = realVal / 10.0f - skill.Value;
                            int deltaThreshold = ProfileManager.CurrentProfile?.ShowSkillsChangedDeltaValue ?? 0;

                            if (
                                change != 0.0f
                                && !float.IsNaN(change)
                                && ProfileManager.CurrentProfile != null
                                && ProfileManager.CurrentProfile.ShowSkillsChangedMessage
                                && (
                                    deltaThreshold <= 0
                                    || skill.ValueFixed / deltaThreshold != realVal / deltaThreshold
                                )
                            )
                                GameActions.Print(
                                    world,
                                    string.Format(
                                        ResGeneral.YourSkillIn0Has1By2ItIsNow3,
                                        skill.Name,
                                        change < 0
                                            ? ResGeneral.Decreased
                                            : ResGeneral.Increased,
                                        Math.Abs(change),
                                        skill.Value + change
                                    ),
                                    0x58,
                                    MessageType.System,
                                    3,
                                    false
                                );
                        }

                        ushort lastBase = skill.BaseFixed;
                        ushort lastValue = skill.ValueFixed;
                        ushort lastCap = skill.CapFixed;

                        skill.BaseFixed = baseVal;
                        skill.ValueFixed = realVal;
                        skill.CapFixed = cap;
                        skill.Lock = locked;

                        if (!isSingleUpdate && !skill.HasLoginBaseline)
                        {
                            skill.BaseFixedAtLogin = baseVal;
                            skill.HasLoginBaseline = true;
                        }

                        if (isSingleUpdate)
                        {
                            if (lastBase != skill.BaseFixed)
                                EventSink.InvokeSkillBaseChanged(id);
                            if (lastValue != skill.ValueFixed)
                                EventSink.InvokeSkillValueChanged(id);
                            if (lastCap != skill.CapFixed)
                                EventSink.InvokeSkillCapChanged(id);
                        }

                        standard?.Update(id);
                        advanced?.ForceUpdate();
                    }
                }

                if (isSingleUpdate)
                    break;
            }
        }
    }
}
