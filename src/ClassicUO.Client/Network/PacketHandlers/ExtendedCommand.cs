using System;
using System.Collections.Generic;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.IO;
using ClassicUO.Network.PacketHandlers.Helpers;
using ClassicUO.Resources;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Network.PacketHandlers;

internal static class ExtendedCommand
{
    public static void Receive(World world, ref StackDataReader p)
    {
        ushort cmd = p.ReadUInt16BE();

        switch (cmd)
        {
            case 0:
                break;

            //===========================================================================================
            //===========================================================================================
            case 1: // fast walk prevention
                for (int i = 0; i < 6; i++)
                    world.Player.Walker.FastWalkStack.SetValue(i, p.ReadUInt32BE());

                break;

            //===========================================================================================
            //===========================================================================================
            case 2: // add key to fast walk stack
                world.Player.Walker.FastWalkStack.AddValue(p.ReadUInt32BE());

                break;

            //===========================================================================================
            //===========================================================================================
            case 4: // close generic gump
                uint ser = p.ReadUInt32BE();
                int button = (int)p.ReadUInt32BE();

                LinkedListNode<Gump> first = UIManager.Gumps.First;

                while (first != null)
                {
                    LinkedListNode<Gump> nextGump = first.Next;

                    if (first.Value.ServerSerial == ser && first.Value.IsFromServer)
                    {
                        if (button != 0)
                            first.Value?.OnButtonClick(button);
                        else
                        {
                            if (first.Value.CanMove)
                                UIManager.SavePosition(ser, first.Value.Location);
                            else
                                UIManager.RemovePosition(ser);
                        }

                        first.Value.Dispose();
                    }

                    first = nextGump;
                }

                break;

            //===========================================================================================
            //===========================================================================================
            case 6: //party
                world.Party.ParsePacket(ref p);

                break;

            //===========================================================================================
            //===========================================================================================
            case 8: // map change
                world.MapIndex = p.ReadUInt8();

                break;

            //===========================================================================================
            //===========================================================================================
            case 0x0C: // close statusbar gump
                UIManager.GetGump<HealthBarGump>(p.ReadUInt32BE())?.Dispose();

                break;

            //===========================================================================================
            //===========================================================================================
            case 0x10: // display equip info
                Item item = world.Items.Get(p.ReadUInt32BE());

                if (item == null)
                    return;

                uint cliloc = p.ReadUInt32BE();
                string str = string.Empty;

                if (cliloc > 0)
                {
                    str = Client.Game.UO.FileManager.Clilocs.GetString((int)cliloc, true);

                    if (!string.IsNullOrEmpty(str))
                        item.Name = str;

                    world.MessageManager.HandleMessage(
                        item,
                        str,
                        item.Name,
                        0x3B2,
                        MessageType.Regular,
                        3,
                        TextType.OBJECT,
                        true
                    );
                }

                str = string.Empty;
                ushort crafterNameLen = 0;
                uint next = p.ReadUInt32BE();

                Span<char> span = stackalloc char[256];
                var strBuffer = new ValueStringBuilder(span);
                if (next == 0xFFFFFFFD)
                {
                    crafterNameLen = p.ReadUInt16BE();

                    if (crafterNameLen > 0)
                    {
                        strBuffer.Append(ResGeneral.CraftedBy);
                        strBuffer.Append(p.ReadASCII(crafterNameLen));
                    }
                }

                if (crafterNameLen != 0)
                    next = p.ReadUInt32BE();

                if (next == 0xFFFFFFFC)
                    strBuffer.Append("[Unidentified");

                byte count = 0;

                while (p.Position < p.Length - 4)
                {
                    if (count != 0 || next == 0xFFFFFFFD || next == 0xFFFFFFFC)
                        next = p.ReadUInt32BE();

                    short charges = (short)p.ReadUInt16BE();
                    string attr = Client.Game.UO.FileManager.Clilocs.GetString((int)next);

                    if (attr != null)
                    {
                        if (charges == -1)
                        {
                            if (count > 0)
                            {
                                strBuffer.Append("/");
                                strBuffer.Append(attr);
                            }
                            else
                            {
                                strBuffer.Append(" [");
                                strBuffer.Append(attr);
                            }
                        }
                        else
                        {
                            strBuffer.Append("\n[");
                            strBuffer.Append(attr);
                            strBuffer.Append(" : ");
                            strBuffer.Append(charges.ToString());
                            strBuffer.Append("]");
                            count += 20;
                        }
                    }

                    count++;
                }

                if ((count < 20 && count > 0) || (next == 0xFFFFFFFC && count == 0))
                    strBuffer.Append(']');

                if (strBuffer.Length != 0)
                    world.MessageManager.HandleMessage(
                        item,
                        strBuffer.ToString(),
                        item.Name,
                        0x3B2,
                        MessageType.Regular,
                        3,
                        TextType.OBJECT,
                        true
                    );

                strBuffer.Dispose();

                AsyncNetClient.Socket.Send_MegaClilocRequest_Old(item);

                break;

            //===========================================================================================
            //===========================================================================================
            case 0x11:
                break;

            //===========================================================================================
            //===========================================================================================
            case 0x14: // display popup/context menu
                UIManager.ShowGamePopup(
                    new PopupMenuGump(world, PopupMenuData.Parse(ref p))
                    {
                        X = world.DelayedObjectClickManager.LastMouseX,
                        Y = world.DelayedObjectClickManager.LastMouseY
                    }
                );

                break;

            //===========================================================================================
            //===========================================================================================
            case 0x16: // close user interface windows
                uint id = p.ReadUInt32BE();
                uint serial = p.ReadUInt32BE();

                switch (id)
                {
                    case 1: // paperdoll
                        UIManager.GetGump<PaperDollGump>(serial)?.Dispose();
                        UIManager.GetGump<ModernPaperdoll>(serial)?.Dispose();

                        break;

                    case 2: //statusbar
                        UIManager.GetGump<HealthBarGump>(serial)?.Dispose();

                        if (serial == world.Player.Serial)
                            StatusGumpBase.GetStatusGump()?.Dispose();

                        break;

                    case 8: // char profile
                        UIManager.GetGump<ProfileGump>()?.Dispose();

                        break;

                    case 0x0C: //container
                        UIManager.GetGump<ContainerGump>(serial)?.Dispose();

                        break;
                }

                break;

            //===========================================================================================
            //===========================================================================================
            case 0x18: // enable map patches

                if (Client.Game.UO.FileManager.Maps.ApplyPatches(ref p))
                {
                    //List<GameObject> list = new List<GameObject>();

                    //foreach (int i in World.Map.GetUsedChunks())
                    //{
                    //    Chunk chunk = World.Map.Chunks[i];

                    //    for (int xx = 0; xx < 8; xx++)
                    //    {
                    //        for (int yy = 0; yy < 8; yy++)
                    //        {
                    //            Tile tile = chunk.Tiles[xx, yy];

                    //            for (GameObject obj = tile.FirstNode; obj != null; obj = obj.Right)
                    //            {
                    //                if (!(obj is Static) && !(obj is Land))
                    //                {
                    //                    list.Add(obj);
                    //                }
                    //            }
                    //        }
                    //    }
                    //}


                    int map = world.MapIndex;
                    world.MapIndex = -1;
                    world.MapIndex = map;

                    Log.Trace("Map Patches applied.");
                }

                break;

            //===========================================================================================
            //===========================================================================================
            case 0x19: //extened stats
                byte version = p.ReadUInt8();
                serial = p.ReadUInt32BE();

                switch (version)
                {
                    case 0:
                        Mobile bonded = world.Mobiles.Get(serial);

                        if (bonded == null)
                            break;

                        bool dead = p.ReadBool();
                        bonded.IsDead = dead;

                        break;

                    case 2:

                        if (serial == world.Player)
                        {
                            byte updategump = p.ReadUInt8();
                            byte state = p.ReadUInt8();

                            world.Player.StrLock = (Lock)((state >> 4) & 3);
                            world.Player.DexLock = (Lock)((state >> 2) & 3);
                            world.Player.IntLock = (Lock)(state & 3);

                            StatusGumpBase.GetStatusGump()?.RequestUpdateContents();
                        }

                        break;

                    case 5:

                        int pos = p.Position;
                        byte zero = p.ReadUInt8();
                        byte type2 = p.ReadUInt8();

                        if (type2 == 0xFF)
                        {
                            byte status = p.ReadUInt8();
                            ushort animation = p.ReadUInt16BE();
                            ushort frame = p.ReadUInt16BE();

                            if (status == 0 && animation == 0 && frame == 0)
                            {
                                p.Seek(pos);
                                goto case 0;
                            }

                            Mobile mobile = world.Mobiles.Get(serial);

                            if (mobile != null)
                            {
                                mobile.SetAnimation(
                                    Mobile.GetReplacedObjectAnimation(mobile.Graphic, animation)
                                );
                                mobile.ExecuteAnimation = false;
                                mobile.AnimIndex = (byte)frame;
                            }
                        }
                        else if (world.Player != null && serial == world.Player)
                        {
                            p.Seek(pos);
                            goto case 2;
                        }

                        break;
                }

                break;

            //===========================================================================================
            //===========================================================================================
            case 0x1B: // new spellbook content
                p.Skip(2);
                Item spellbook = world.GetOrCreateItem(p.ReadUInt32BE());
                spellbook.Graphic = p.ReadUInt16BE();
                spellbook.Clear();
                ushort type = p.ReadUInt16BE();

                for (int j = 0; j < 2; j++)
                {
                    uint spells = 0;

                    for (int i = 0; i < 4; i++)
                        spells |= (uint)(p.ReadUInt8() << (i * 8));

                    for (int i = 0; i < 32; i++)
                        if ((spells & (1 << i)) != 0)
                        {
                            ushort cc = (ushort)(j * 32 + i + 1);
                            // FIXME: should i call Item.Create ?
                            var spellItem = Item.Create(world, cc); // new Item()
                            spellItem.Serial = cc;
                            spellItem.Graphic = 0x1F2E;
                            spellItem.Amount = cc;
                            spellItem.Container = spellbook;
                            spellbook.PushToBack(spellItem);
                        }
                }

                UIManager.GetGump<SpellbookGump>(spellbook)?.RequestUpdateContents();

                break;

            //===========================================================================================
            //===========================================================================================
            case 0x1D: // house revision state
                serial = p.ReadUInt32BE();
                uint revision = p.ReadUInt32BE();

                Item multi = world.Items.Get(serial);

                if (multi == null)
                    world.HouseManager.Remove(serial);

                if (
                    !world.HouseManager.TryGetHouse(serial, out House house)
                    || !house.IsCustom
                    || house.Revision != revision
                )
                    SharedStore.AddCustomHouseRequest(serial);
                else
                {
                    house.Generate();
                    world.BoatMovingManager.ClearSteps(serial);

                    UIManager.GetGump<MiniMapGump>()?.RequestUpdateContents();

                    if (world.HouseManager.EntityIntoHouse(serial, world.Player))
                        Client.Game.GetScene<GameScene>()?.UpdateMaxDrawZ(true);
                }

                break;

            //===========================================================================================
            //===========================================================================================
            case 0x20:
                serial = p.ReadUInt32BE();
                type = p.ReadUInt8();
                ushort graphic = p.ReadUInt16BE();
                ushort x = p.ReadUInt16BE();
                ushort y = p.ReadUInt16BE();
                sbyte z = p.ReadInt8();

                switch (type)
                {
                    case 1: // update
                        break;

                    case 2: // remove
                        break;

                    case 3: // update multi pos
                        break;

                    case 4: // begin
                        HouseCustomizationGump gump = UIManager.GetGump<HouseCustomizationGump>();

                        if (gump != null)
                            break;

                        gump = new HouseCustomizationGump(world, serial, 50, 50);
                        UIManager.Add(gump);

                        break;

                    case 5: // end
                        UIManager.GetGump<HouseCustomizationGump>(serial)?.Dispose();

                        break;
                }

                break;

            //===========================================================================================
            //===========================================================================================
            case 0x21:

                for (int i = 0; i < 2; i++)
                    world.Player.Abilities[i] &= (Ability)0x7F;

                break;

            //===========================================================================================
            //===========================================================================================
            case 0x22:
                p.Skip(1);

                Entity en = world.Get(p.ReadUInt32BE());

                if (en != null)
                {
                    byte damage = p.ReadUInt8();

                    if (damage > 0)
                        world.WorldTextManager.AddDamage(en, damage);
                }

                break;

            case 0x25:

                ushort spell = p.ReadUInt16BE();
                bool active = p.ReadBool();

                for (LinkedListNode<Gump> last = UIManager.Gumps.Last; last != null; last = last.Previous)
                {
                    Control c = last.Value;

                    if (c.IsDisposed || !c.IsVisible) continue;

                    if (c is not UseSpellButtonGump spellButton || spellButton.SpellID != spell) continue;

                    if (active)
                    {
                        spellButton.Hue = 38;
                        world.ActiveSpellIcons.Add(spell);
                    }
                    else
                    {
                        spellButton.Hue = 0;
                        world.ActiveSpellIcons.Remove(spell);
                    }

                    break;
                }

                break;

            //===========================================================================================
            //===========================================================================================
            case 0x26:
                byte val = p.ReadUInt8();

                if (val > (int)CharacterSpeedType.FastUnmountAndCantRun)
                    val = 0;

                if (world.Player == null) break;

                world.Player.SpeedMode = (CharacterSpeedType)val;

                break;

            case 0x2A:
                bool isfemale = p.ReadBool();
                byte race = p.ReadUInt8();

                UIManager.GetGump<RaceChangeGump>()?.Dispose();
                UIManager.Add(new RaceChangeGump(world, isfemale, race));
                break;

            case 0x2B:
                serial = p.ReadUInt16BE();
                byte animID = p.ReadUInt8();
                byte frameCount = p.ReadUInt8();

                foreach (Mobile m in world.Mobiles.Values)
                    if ((m.Serial & 0xFFFF) == serial)
                    {
                        m.SetAnimation(animID);
                        m.AnimIndex = frameCount;
                        m.ExecuteAnimation = false;

                        break;
                    }

                break;

            case 0xBEEF: // ClassicUO commands

                type = p.ReadUInt16BE();

                break;

            case 0x39:
                OnSpellbookCacheValid(world, ref p);
                break;

            case 0x3A:
                OnSpellbookFullData(world, ref p);
                break;

            case 0x3B:
                OnInvalidateSpellbookCache(world, ref p);
                break;

            case 0x3D:
                OnRegisterSpellbookSerial(world, ref p);
                break;

            default:
                Log.Warn($"Unhandled 0xBF - sub: {cmd.ToHex()}");

                break;
        }
    }

    private static void OnSpellbookCacheValid(World world, ref StackDataReader p)
    {
        uint serial = p.ReadUInt32BE();
        byte type = p.ReadUInt8();
        uint version = p.ReadUInt32BE();
        ulong spellBitmask = p.ReadUInt64BE();

        SpellbookCacheManager.Instance.OnCacheValid(type, version, spellBitmask);

        UIManager.GetGump<SpellbookGump>(serial)?.OnCacheValidated(spellBitmask);
    }

    private static void OnSpellbookFullData(World world, ref StackDataReader p)
    {
        var entry = ReadSpellbookEntry(ref p, out uint serial, out uint ttl);
        SpellbookCacheManager.Instance.UpdateCache(entry, ttl);

        var gump = UIManager.GetGump<SpellbookGump>(serial);
        if (gump != null)
            gump.OnSpellbookDataReceived();
        else
            UIManager.Add(new SpellbookGump(world, serial));
    }

    private static void OnInvalidateSpellbookCache(World world, ref StackDataReader p)
    {
        byte type = p.ReadUInt8();
        _ = p.ReadUInt32BE();
        byte flags = p.ReadUInt8();
        bool forceReload = (flags & 0x01) != 0;

        if (type == 0xFF)
            SpellbookCacheManager.Instance.InvalidateAll();
        else
            SpellbookCacheManager.Instance.InvalidateCache(type);

        if (!forceReload)
            return;

        LinkedListNode<Gump> first = UIManager.Gumps.First;
        while (first != null)
        {
            LinkedListNode<Gump> next = first.Next;
            if (first.Value is SpellbookGump sbGump && ((byte)sbGump.SpellBookType == type || type == 0xFF))
            {
                uint sbSerial = sbGump.LocalSerial;
                sbGump.Dispose();
                AsyncNetClient.Socket.Send_SpellbookRequest(sbSerial, type, 0, true);
            }
            first = next;
        }
    }

    private static void OnRegisterSpellbookSerial(World world, ref StackDataReader p)
    {
        uint serial = p.ReadUInt32BE();
        var bookType = (SpellBookType)p.ReadUInt8();

        SpellbookTypeRegistry.RegisterSerial(serial, bookType);
        DynamicSpellbookRegistry.RegisterDynamic(bookType);
    }

    private static SpellbookCacheEntry ReadSpellbookEntry(ref StackDataReader p, out uint serial, out uint ttl)
    {
        serial = p.ReadUInt32BE();

        var entry = new SpellbookCacheEntry
        {
            SpellbookType = p.ReadUInt8(),
            Version = p.ReadUInt32BE(),
            SpellBitmask = p.ReadUInt64BE(),
            BookGraphic = p.ReadUInt16BE(),
            MinimizedGraphic = p.ReadUInt16BE()
        };

        byte spellCount = p.ReadUInt8();
        ttl = p.ReadUInt32BE();
        entry.SpellsPerPageSide = p.ReadUInt8();
        entry.MaxDictionaryPages = p.ReadUInt8();
        byte pageNameCount = p.ReadUInt8();

        byte displayFlags = p.ReadUInt8();
        entry.DisplayManaCost = (displayFlags & 0x01) != 0;
        entry.DisplayMinSkill = (displayFlags & 0x02) != 0;
        entry.DisplayPowerWords = (displayFlags & 0x04) != 0;

        entry.BookHue = p.ReadUInt16BE();
        entry.TextColor = p.ReadUInt16BE();
        entry.SpellNameColor = p.ReadUInt16BE();
        entry.ContentOffsetX = (short)p.ReadUInt16BE();
        entry.ContentOffsetY = (short)p.ReadUInt16BE();
        entry.PageTurnLeftGraphic = p.ReadUInt16BE();
        entry.PageTurnRightGraphic = p.ReadUInt16BE();
        entry.PageTurnLeftX = (short)p.ReadUInt16BE();
        entry.PageTurnLeftY = (short)p.ReadUInt16BE();
        entry.PageTurnRightX = (short)p.ReadUInt16BE();
        entry.PageTurnRightY = (short)p.ReadUInt16BE();

        byte overlayCount = p.ReadUInt8();
        var overlays = new ushort[overlayCount];
        for (int i = 0; i < overlayCount; i++)
            overlays[i] = p.ReadUInt16BE();
        entry.OverlayGraphics = overlays;

        entry.CustomPropertyTitle = ReadOptionalAscii8(ref p);
        entry.CustomPropertyLabel = ReadOptionalAscii8(ref p);
        entry.CustomPropertyName = ReadOptionalAscii8(ref p);

        var pageNames = new string[pageNameCount];
        for (int i = 0; i < pageNameCount; i++)
            pageNames[i] = ReadOptionalAscii8(ref p) ?? string.Empty;
        entry.PageNames = pageNames;

        var spells = new List<DynamicSpellDefinition>(spellCount);
        for (int i = 0; i < spellCount; i++)
            spells.Add(ReadSpellDefinition(ref p));

        if (p.Remaining > 0)
        {
            entry.ManaCostLabel = ReadOptionalAscii8(ref p);
            entry.MinSkillLabel = ReadOptionalAscii8(ref p);
            entry.TitleColor = p.ReadUInt16BE();

            byte infoPageCount = p.ReadUInt8();
            var infoPages = new List<SpellbookInfoPage>(infoPageCount);
            for (int i = 0; i < infoPageCount; i++)
            {
                infoPages.Add(new SpellbookInfoPage
                {
                    Title = ReadOptionalAscii16(ref p),
                    Body = ReadOptionalAscii16(ref p)
                });
            }
            entry.InfoPages = infoPages;

            if (p.Remaining > 0 && p.ReadUInt8() != 0)
            {
                entry.Bookmark = new SpellbookBookmarkInfo
                {
                    Graphic = p.ReadUInt16BE(),
                    PressedGraphic = p.ReadUInt16BE(),
                    X = (short)p.ReadUInt16BE(),
                    Y = (short)p.ReadUInt16BE(),
                    Hue = p.ReadUInt16BE(),
                    DisplayPage = p.ReadUInt8(),
                    ActionType = p.ReadUInt8(),
                    Action = p.ReadUInt32BE(),
                    Tooltip = ReadOptionalAscii8(ref p)
                };
            }
        }

        entry.Spells = spells;
        return entry;
    }

    private static DynamicSpellDefinition ReadSpellDefinition(ref StackDataReader p)
    {
        var spell = new DynamicSpellDefinition
        {
            SpellID = p.ReadUInt16BE(),
            IconGraphic = p.ReadUInt16BE(),
            NameCliloc = p.ReadInt32BE(),
            Name = ReadOptionalAscii8(ref p),
            PowerWords = ReadOptionalAscii8(ref p),
            Description = ReadOptionalAscii8(ref p),
            ManaCost = p.ReadUInt8(),
            MinSkill = p.ReadUInt8(),
            TargetType = p.ReadUInt8(),
            Reagents = p.ReadUInt16BE()
        };

        byte customReagentCount = p.ReadUInt8();
        if (customReagentCount > 0)
        {
            spell.CustomReagents = new string[customReagentCount];
            for (int r = 0; r < customReagentCount; r++)
                spell.CustomReagents[r] = ReadOptionalAscii8(ref p) ?? string.Empty;
        }

        spell.Cooldown = p.ReadUInt16BE();
        spell.Page = p.ReadUInt8();

        return spell;
    }

    private static string ReadOptionalAscii8(ref StackDataReader p)
    {
        byte len = p.ReadUInt8();
        return len > 0 ? p.ReadASCII(len) : null;
    }

    private static string ReadOptionalAscii16(ref StackDataReader p)
    {
        ushort len = p.ReadUInt16BE();
        return len > 0 ? p.ReadASCII(len) : null;
    }
}
