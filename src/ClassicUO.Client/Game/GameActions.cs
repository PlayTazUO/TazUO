// SPDX-License-Identifier: BSD-2-Clause
using System;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Game.UI.ImGuiControls;
using ClassicUO.Input;
using ClassicUO.LegionScripting;
using ClassicUO.Network;
using ClassicUO.Resources;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using static ClassicUO.Network.AsyncNetClient;

namespace ClassicUO.Game;

internal static class GameActions
{
    public static int LastSpellIndex { get; set; } = 1;
    public static int LastSkillIndex { get; set; } = 1;


    internal static void ToggleWarMode(PlayerMobile player) => RequestWarMode(player, !player.InWarMode);

    internal static void RequestWarMode(PlayerMobile player, bool war)
    {
        if (!player.IsDead)
        {
            if (war && ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.EnableMusic)
            {
                Client.Game.Audio.PlayMusic((RandomHelper.GetValue(0, 3) % 3) + 38, true);
            }
            else if (!war)
            {
                Client.Game.Audio.StopWarMusic();
            }
        }

        Socket.Send_ChangeWarMode(war);
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>False if no durability gump was open</returns>
    internal static bool CloseDurabilityGump()
    {
        Gump g = UIManager.GetGump<DurabilitysGump>();

        if (g != null)
        {
            g.Dispose();
            return true;
        }

        return false;
    }

    internal static void OpenDurabilityGump(World world) => UIManager.Add(new DurabilitysGump(world));

    internal static void OpenLegionScriptingGump(World world) => ScriptManagerWindow.Show();

    /// <summary>
    ///
    /// </summary>
    /// <returns>False if no script manager window was open</returns>
    internal static bool CloseLegionScriptingGump()
    {
        var window = ScriptManagerWindow.Instance;

        if (window != null && window.IsVisible)
        {
            window.IsVisible = false;
            return true;
        }

        return false;
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>False if no nearby loot gump was open</returns>
    internal static bool CloseNearbyLootGump()
    {
        Gump g = UIManager.GetGump<NearbyLootGump>();

        if (g != null)
        {
            g.Dispose();
            return true;
        }

        return false;
    }

    internal static void OpenNearbyLootGump(World world) => UIManager.Add(new NearbyLootGump(world));

    internal static void OpenMacroGump(World world, string name)
    {
        MacroGump macroGump = UIManager.GetGump<MacroGump>();

        macroGump?.Dispose();
        UIManager.Add(new MacroGump(world, name));
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="serial"></param>
    /// <returns>False if no paperdoll is open</returns>
    internal static bool ClosePaperdoll(World world, uint? serial = null)
    {
        serial ??= world.Player.Serial;
        Gump g;
        if (ProfileManager.CurrentProfile.UseModernPaperdoll)
            g = UIManager.GetGump<ModernPaperdoll>(serial);
        else
            g = UIManager.GetGump<PaperDollGump>(serial);

        if (g != null)
        {
            g.Dispose();
            return true;
        }
        return false;
    }

    internal static void OpenPaperdoll(World world, uint serial)
    {
        if (ProfileManager.CurrentProfile.UseModernPaperdoll && serial == world.Player.Serial)
        {
            ModernPaperdoll modernPaperdoll = UIManager.GetGump<ModernPaperdoll>(serial);
            if (modernPaperdoll == null)
                UIManager.Add(new ModernPaperdoll(world, serial));
            else
            {
                modernPaperdoll.SetInScreen();
                modernPaperdoll.BringOnTop();
            }
        }
        else
        {
            PaperDollGump paperDollGump = UIManager.GetGump<PaperDollGump>(serial);

            if (paperDollGump == null)
            {
                    // Bitwish ORing 0x8000_0000 signals to the server to send the
                    // OpenPaperdoll packet for the player specifically.
                DoubleClickQueued(serial | 0x8000_0000);
            }
            else
            {
                if (paperDollGump.IsMinimized)
                {
                    paperDollGump.IsMinimized = false;
                }

                paperDollGump.SetInScreen();
                paperDollGump.BringOnTop();
            }
        }
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>False if no settings are open</returns>
    internal static bool CloseSettings()
    {
        Gump g = UIManager.GetGump<ModernOptionsGump>();

        if (g != null)
        {
            g.Dispose();
            return true;
        }

        return false;
    }

    internal static void OpenSettings(World world, int page = 0)
    {
        ModernOptionsGump opt = UIManager.GetGump<ModernOptionsGump>();

        if (opt == null)
        {
            var optionsGump = new ModernOptionsGump(world);

            UIManager.Add(optionsGump);
            optionsGump.ChangePage(page);
            optionsGump.SetInScreen();
        }
        else
        {
            opt.SetInScreen();
            opt.BringOnTop();
        }
    }

    internal static void OpenStatusBar(World world)
    {
        Client.Game.Audio.StopWarMusic();

        if (StatusGumpBase.GetStatusGump() == null)
        {
            UIManager.Add(StatusGumpBase.AddStatusGump(world, ProfileManager.CurrentProfile.StatusGumpPosition.X, ProfileManager.CurrentProfile.StatusGumpPosition.Y));
        }
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>False if no status gump open</returns>
    internal static bool CloseStatusBar()
    {
        Gump g = StatusGumpBase.GetStatusGump();
        if (g != null)
        {
            g.Dispose();
            return true;
        }

        return false;
    }

    internal static void OpenJournal(World world) => UIManager.Add(new ResizableJournal(world));

    /// <summary>
    ///
    /// </summary>
    /// <returns>False if no journals were open</returns>
    internal static bool CloseAllJournals()
    {
        Gump g = UIManager.GetGump<ResizableJournal>();

        bool status = g != null;

        while (g != null)
        {
            g.Dispose();
            g = UIManager.GetGump<ResizableJournal>();
        }

        return status;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="type"></param>
    /// <returns>False if no spell books of that type were open</returns>
    internal static bool CloseSpellBook(SpellBookType type)
    {
        SpellbookGump g = UIManager.GetGump<SpellbookGump>();

        while (g != null)
        {
            if (g.SpellBookType == type)
            {
                g.Dispose();
                return true;
            }
            g = UIManager.GetGump<SpellbookGump>();
        }

        return false;
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>False if no skill gumps were open</returns>
    internal static bool CloseSkills()
    {
        Gump g;
        if (ProfileManager.CurrentProfile.StandardSkillsGump)

            g = UIManager.GetGump<StandardSkillsGump>();
        else
            g = UIManager.GetGump<SkillGumpAdvanced>();

        if (g != null)
        {
            g.Dispose();
            return true;
        }

        return false;
    }

    internal static void OpenSkills(World world)
    {
        if (ProfileManager.CurrentProfile.StandardSkillsGump)
        {
            StandardSkillsGump skillsGump = UIManager.GetGump<StandardSkillsGump>();

            if (skillsGump != null && skillsGump.IsMinimized)
            {
                skillsGump.IsMinimized = false;
            }
            else
            {
                world.SkillsRequested = true;
                Socket.Send_SkillsRequest(world.Player.Serial);
            }
        }
        else
        {
            SkillGumpAdvanced skillsGump = UIManager.GetGump<SkillGumpAdvanced>();

            if (skillsGump == null)
            {
                world.SkillsRequested = true;
                Socket.Send_SkillsRequest(world.Player.Serial);
            }
        }
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>False if no mini map open</returns>
    internal static bool CloseMiniMap()
    {
        Gump g = UIManager.GetGump<MiniMapGump>();

        if (g != null)
        {
            g.Dispose();
            return true;
        }

        return false;
    }

    internal static void OpenMiniMap(World world)
    {
        MiniMapGump miniMapGump = UIManager.GetGump<MiniMapGump>();

        if (miniMapGump == null)
        {
            UIManager.Add(new MiniMapGump(world));
        }
        else
        {
            miniMapGump.ToggleSize();
            miniMapGump.SetInScreen();
            miniMapGump.BringOnTop();
        }
    }

    internal static bool BandageSelf(World world)
    {
        Item bandage = world.Player.FindBandage();
        if (bandage != null)
        {
                // Record action for script recording
                ClassicUO.LegionScripting.ScriptRecorder.Instance.RecordBandageSelf();

            Socket.Send_TargetSelectedObject(bandage.Serial, world.Player.Serial);
            return true;
        }
        return false;
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>False if no world map is open</returns>
    internal static bool CloseWorldMap()
    {
        Gump g = UIManager.GetGump<WorldMapGump>();

        if (g != null)
        {
            g.Dispose();
            return true;
        }

        return false;
    }


    internal static void OpenWorldMap(World world)
    {
        WorldMapGump worldMap = UIManager.GetGump<WorldMapGump>();

        if (worldMap == null || worldMap.IsDisposed)
        {
            worldMap = new WorldMapGump(world);
            UIManager.Add(worldMap);
        }
        else
        {
            worldMap.BringOnTop();
            worldMap.SetInScreen();
        }
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>False if no chat was open</returns>
    internal static bool CloseChat()
    {
        Gump g = UIManager.GetGump<ChatGump>();
        if (g != null)
        {
            g.Dispose();
            return true;
        }
        return false;
    }

    internal static void OpenChat(World world)
    {
        if (world.ChatManager.ChatIsEnabled == ChatStatus.Enabled)
        {
            ChatGump chatGump = UIManager.GetGump<ChatGump>();

            if (chatGump == null)
            {
                UIManager.Add(new ChatGump(world));
            }
            else
            {
                chatGump.SetInScreen();
                chatGump.BringOnTop();
            }
        }
        else if (world.ChatManager.ChatIsEnabled == ChatStatus.EnabledUserRequest)
        {
            ChatGumpChooseName chatGump = UIManager.GetGump<ChatGumpChooseName>();

            if (chatGump == null)
            {
                UIManager.Add(new ChatGumpChooseName(world));
            }
            else
            {
                chatGump.SetInScreen();
                chatGump.BringOnTop();
            }
        }
    }

    internal static bool OpenCorpse(World world, uint serial)
    {
        if (!SerialHelper.IsItem(serial))
        {
            return false;
        }

        Item item = world.Items.Get(serial);

        if (item == null || !item.IsCorpse || item.IsDestroyed)
        {
            return false;
        }

        world.Player.ManualOpenedCorpses.Add(serial);
        DoubleClick(world, serial);

        return true;
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>False if no backpack was opened</returns>
    internal static bool CloseBackpack(World world)
    {
        Gump g;

        Item backpack = world.Player.Backpack;

        if (backpack == null)
        {
            return false;
        }

            // Record action for script recording
            ClassicUO.LegionScripting.ScriptRecorder.Instance.RecordCloseContainer(backpack.Serial, "backpack");

        g = UIManager.GetGump<ContainerGump>(backpack);
        g ??= UIManager.GetGump<GridContainer>(backpack);

        if (g != null)
        {
            g.Dispose();
            return true;
        }

        return false;
    }

    internal static bool OpenBackpack(World world)
    {
        Item backpack = world.Player.Backpack;

        if (backpack == null)
        {
            return false;
        }

        Gump backpackGump = UIManager.GetGump<ContainerGump>(backpack);
        if (backpackGump == null)
        {
            backpackGump = UIManager.GetGump<GridContainer>(backpack);
            if (backpackGump == null)
            {
                DoubleClick(world, backpack);
                return true;
            }
            else
            {
                backpackGump.RequestUpdateContents();
                backpackGump.SetInScreen();
                backpackGump.BringOnTop();
            }
        }
        else
        {
            ((ContainerGump)backpackGump).IsMinimized = false;
            backpackGump.SetInScreen();
            backpackGump.BringOnTop();
        }
        return true;
    }

    internal static void Attack(World world, uint serial)
    {
        if (ProfileManager.CurrentProfile.EnabledCriminalActionQuery)
        {
            Mobile m = world.Mobiles.Get(serial);

            if (m != null && (world.Player.NotorietyFlag == NotorietyFlag.Innocent || world.Player.NotorietyFlag == NotorietyFlag.Ally) && m.NotorietyFlag == NotorietyFlag.Innocent && m != world.Player)
            {
                var messageBox = new QuestionGump
                (
                    world,
                    ResGeneral.ThisMayFlagYouCriminal,
                    s =>
                    {
                        if (s)
                        {
                            Socket.Send_AttackRequest(serial);
                        }
                    }
                );

                UIManager.Add(messageBox);
                return;
            }
        }

            // Record action for script recording
            ClassicUO.LegionScripting.ScriptRecorder.Instance.RecordAttack(serial);
            ScriptingInfoGump.AddOrUpdateInfo("Last Attacked", $"0x{serial:X}");

        world.TargetManager.NewTargetSystemSerial = serial;
        world.TargetManager.LastAttack = serial;
        Socket.Send_AttackRequest(serial);
    }

    internal static void DoubleClickQueued(uint serial) => Client.Game.GetScene<GameScene>()?.DoubleClickDelayed(serial);

    internal static void DoubleClick(World world, uint serial)
    {
        // Record action for script recording (only for items)
        if (SerialHelper.IsItem(serial))
            ClassicUO.LegionScripting.ScriptRecorder.Instance.RecordUseItem(serial);

        ScriptingInfoGump.AddOrUpdateInfo("Last Object", $"0x{serial:X}");
        Entity obj = World.Instance.Get(serial);
        if (obj != null)
            ScriptingInfoGump.AddOrUpdateInfo("Last Object Graphic", $"0x{obj.Graphic:X}");

        if (serial != world.Player && SerialHelper.IsMobile(serial) && world.Player.InWarMode)
        {
            RequestMobileStatus(world, serial);
            Attack(world, serial);
        }
        else
        {
            if (SerialHelper.IsItem(serial))
            {
                Gump g = UIManager.GetGump<GridContainer>(serial);
                if (g != null)
                {
                    g.SetInScreen();
                    g.BringOnTop();
                }
                Socket.Send_DoubleClick(serial);
            }
            else
                Socket.Send_DoubleClick(serial);
        }

        if (SerialHelper.IsItem(serial) || (SerialHelper.IsMobile(serial) && (world.Mobiles.Get(serial)?.IsHuman ?? false)))
        {
            world.LastObject = serial;
        }
        else
        {
            world.LastObject = 0;
        }
    }

    internal static void SingleClick(World world, uint serial)
    {
        // add  request context menu
        Socket.Send_ClickRequest(serial);

        Entity entity = world.Get(serial);

        if (entity != null)
        {
            entity.IsClicked = true;
        }
    }

    internal static void Say(string message, ushort hue = 0xFFFF, MessageType type = MessageType.Regular, byte font = 3)
    {
            // Record action for script recording (only for regular speech)
            switch (type)
            {
                case MessageType.Regular:
                    ClassicUO.LegionScripting.ScriptRecorder.Instance.RecordSay(message);
                    break;
                case MessageType.Emote:
                    ClassicUO.LegionScripting.ScriptRecorder.Instance.RecordEmoteMsg(message);
                    break;
                case MessageType.Whisper:
                    ClassicUO.LegionScripting.ScriptRecorder.Instance.RecordWhisperMsg(message);
                    break;
                case MessageType.Yell:
                    ClassicUO.LegionScripting.ScriptRecorder.Instance.RecordYellMsg(message);
                    break;
                case MessageType.Guild:
                    ClassicUO.LegionScripting.ScriptRecorder.Instance.RecordGuildMsg(message);
                    break;
                case MessageType.Alliance:
                    ClassicUO.LegionScripting.ScriptRecorder.Instance.RecordAllyMsg(message);
                    break;
                case MessageType.Party:
                    ClassicUO.LegionScripting.ScriptRecorder.Instance.RecordPartyMsg(message);
                    break;
            }

        if (hue == 0xFFFF)
        {
            hue = ProfileManager.CurrentProfile.SpeechHue;
        }

        // TODO: identify what means 'older client' that uses ASCIISpeechRquest [0x03]
        //
        // Fix -> #1267
        if (Client.Game.UO.Version >= ClientVersion.CV_200)
        {
            Socket.Send_UnicodeSpeechRequest(message,
                                             type,
                                             font,
                                             hue,
                                             Settings.GlobalSettings.Language);
        }
        else
        {
            Socket.Send_ASCIISpeechRequest(message, type, font, hue);
        }
    }


    internal static void Print(string message, ushort hue = 946, MessageType type = MessageType.Regular, byte font = 3, bool unicode = true) => Print(World.Instance, message, hue, type, font, unicode);

    internal static void Print(World world, string message, ushort hue = 946, MessageType type = MessageType.Regular, byte font = 3, bool unicode = true)
    {
        if (type == MessageType.ChatSystem)
        {
            world.MessageManager.HandleMessage
            (
                null,
                message,
                "Chat",
                hue,
                type,
                font,
                TextType.OBJECT,
                unicode,
                Settings.GlobalSettings.Language
            );
            return;
        }

        Print
        (
            world,
            null,
            message,
            hue,
            type,
            font,
            unicode
        );
    }

    internal static void Print
    (
        World world,
        Entity entity,
        string message,
        ushort hue = 946,
        MessageType type = MessageType.Regular,
        byte font = 3,
        bool unicode = true
    ) => world.MessageManager.HandleMessage
        (
            entity,
            message,
            entity != null ? entity.Name : "System",
            hue,
            type,
            font,
            entity == null ? TextType.SYSTEM : TextType.OBJECT,
            unicode,
            Settings.GlobalSettings.Language
        );

    internal static void SayParty(string message, uint serial = 0)
    {
            // Record action for script recording
            ClassicUO.LegionScripting.ScriptRecorder.Instance.RecordPartyMsg(message);
        Socket.Send_PartyMessage(message, serial);
    }

    internal static void RequestPartyAccept(uint serial)
    {
        Socket.Send_PartyAccept(serial);

        UIManager.GetGump<PartyInviteGump>()?.Dispose();
    }

    internal static void RequestPartyRemoveMemberByTarget() => Socket.Send_PartyRemoveRequest(0x00);

    internal static void RequestPartyRemoveMember(uint serial) => Socket.Send_PartyRemoveRequest(serial);

    internal static void RequestPartyQuit(PlayerMobile player) => Socket.Send_PartyRemoveRequest(player.Serial);

    internal static void RequestPartyInviteByTarget() => Socket.Send_PartyInviteRequest();

    internal static void RequestPartyLootState(bool isLootable) => Socket.Send_PartyChangeLootTypeRequest(isLootable);

    internal static bool PickUp
    (
        World world,
        uint serial,
        int x,
        int y,
        int amount = -1,
        Point? offset = null,
        bool is_gump = false
    )
    {
        if (world.Player.IsDead || Client.Game.UO.GameCursor.ItemHold.Enabled)
        {
            return false;
        }

        Item item = world.Items.Get(serial);

        if (item == null || item.IsDestroyed || item.IsMulti || item.OnGround && (item.IsLocked || item.Distance > Constants.DRAG_ITEMS_DISTANCE))
        {
            return false;
        }

        if (amount <= -1 && item.Amount > 1 && item.ItemData.IsStackable)
        {
            if (ProfileManager.CurrentProfile.HoldShiftToSplitStack == Keyboard.Shift)
            {
                SplitMenuGump gump = UIManager.GetGump<SplitMenuGump>(item);

                if (gump != null)
                {
                    return false;
                }

                gump = new SplitMenuGump(world, item, new Point(x, y))
                {
                    X = Mouse.Position.X - 80,
                    Y = Mouse.Position.Y - 40
                };

                UIManager.Add(gump);
                UIManager.AttemptDragControl(gump, true);

                return true;
            }
        }

        if (amount <= 0)
        {
            amount = item.Amount;
        }

        Client.Game.UO.GameCursor.ItemHold.Clear();
        Client.Game.UO.GameCursor.ItemHold.Set(item, (ushort)amount, offset);
        Client.Game.UO.GameCursor.ItemHold.IsGumpTexture = is_gump;
        Socket.Send_PickUpRequest(item, (ushort)amount);
        ScriptingInfoGump.AddOrUpdateInfo("Last Picked Up Item", $"0x{item.Serial:X}");
        ScriptingInfoGump.AddOrUpdateInfo("Last Object Graphic", $"0x{item.Graphic:X}");

        if (item.OnGround)
        {
            item.RemoveFromTile();
        }

        item.TextContainer?.Clear();

        world.ObjectToRemove = item.Serial;

        return true;
    }

    internal static void DropItem(uint serial, int x, int y, int z, uint container, bool force = false)
    {
        if (force || (Client.Game.UO.GameCursor.ItemHold.Enabled && !Client.Game.UO.GameCursor.ItemHold.IsFixedPosition && (Client.Game.UO.GameCursor.ItemHold.Serial != container || Client.Game.UO.GameCursor.ItemHold.ItemData.IsStackable)))
        {
            // Record action for script recording
            uint sourceSerial = Client.Game.UO.GameCursor.ItemHold.Enabled ? Client.Game.UO.GameCursor.ItemHold.Serial : serial;
                int amount = Client.Game.UO.GameCursor.ItemHold.Enabled ? Client.Game.UO.GameCursor.ItemHold.Amount : -1;
                ClassicUO.LegionScripting.ScriptRecorder.Instance.RecordDragDrop(sourceSerial, container, amount, x, y);
            if (Client.Game.UO.Version >= ClientVersion.CV_6017)
            {
                Socket.Send_DropRequest(serial,
                                        (ushort)x,
                                        (ushort)y,
                                        (sbyte)z,
                                        0,
                                        container);
            }
            else
            {
                Socket.Send_DropRequest_Old(serial,
                                            (ushort)x,
                                            (ushort)y,
                                            (sbyte)z,
                                            container);
            }

            Client.Game.UO.GameCursor.ItemHold.Enabled = false;
            Client.Game.UO.GameCursor.ItemHold.Dropped = true;
        }
    }

    internal static void Equip(World world, uint container = 0)
    {
        if (Client.Game.UO.GameCursor.ItemHold.Enabled && !Client.Game.UO.GameCursor.ItemHold.IsFixedPosition && Client.Game.UO.GameCursor.ItemHold.ItemData.IsWearable)
        {
            if (!SerialHelper.IsValid(container))
            {
                container = world.Player.Serial;
            }

                // Record action for script recording
                ClassicUO.LegionScripting.ScriptRecorder.Instance.RecordEquipItem(Client.Game.UO.GameCursor.ItemHold.Serial, (Layer)Client.Game.UO.GameCursor.ItemHold.ItemData.Layer);

            Socket.Send_EquipRequest(Client.Game.UO.GameCursor.ItemHold.Serial, (Layer)Client.Game.UO.GameCursor.ItemHold.ItemData.Layer, container);

            Client.Game.UO.GameCursor.ItemHold.Enabled = false;
            Client.Game.UO.GameCursor.ItemHold.Dropped = true;
            Client.Game.UO.GameCursor.ItemHold.Clear();
        }
    }

    internal static void ReplyGump(World world, uint local, uint server, int button, uint[] switches = null, Tuple<ushort, string>[] entries = null)
    {
            // Record action for script recording
            ClassicUO.LegionScripting.ScriptRecorder.Instance.RecordReplyGump(server, button, switches, entries);
            ScriptingInfoGump.AddOrUpdateInfo("Last Gump Response", button);

        Socket.Send_GumpResponse(world, local,
                                 server,
                                 button,
                                 switches,
                                 entries);
        if (CUOEnviroment.Debug)
            GameActions.Print(world, $"Gump Button: {button} for gump: {server}");
    }

    internal static void RequestHelp() => Socket.Send_HelpRequest();

    internal static void RequestQuestMenu(World world) => Socket.Send_QuestMenuRequest(world);

    internal static void RequestProfile(uint serial) => Socket.Send_ProfileRequest(serial);

    internal static void ChangeSkillLockStatus(ushort skillindex, byte lockstate) => Socket.Send_SkillStatusChangeRequest(skillindex, lockstate);

    internal static void RequestMobileStatus(World world, uint serial, bool force = false)
    {
        if (world.InGame)
        {
            Entity ent = world.Get(serial);

            if (ent != null)
            {
                if (force)
                {
                    if (ent.HitsRequest >= HitsRequestStatus.Pending)
                    {
                        SendCloseStatus(world, serial);
                    }
                }

                if (ent.HitsRequest < HitsRequestStatus.Received)
                {
                    ent.HitsRequest = HitsRequestStatus.Pending;
                    force = true;
                }
            }

            if (force && SerialHelper.IsValid(serial))
            {
                //ent = ent ?? World.Player;
                //ent.AddMessage(MessageType.Regular, $"PACKET SENT: 0x{serial:X8}", 3, 0x34, true, TextType.OBJECT);
                Socket.Send_StatusRequest(serial);
            }
        }
    }

    internal static void SendCloseStatus(World world, uint serial, bool force = false)
    {
        if (Client.Game.UO.Version >= ClientVersion.CV_200 && world.InGame)
        {
            Entity ent = world.Get(serial);

            if (ent != null && ent.HitsRequest >= HitsRequestStatus.Pending)
            {
                ent.HitsRequest = HitsRequestStatus.None;
                force = true;
            }

            if (force && SerialHelper.IsValid(serial))
            {
                //ent = ent ?? World.Player;
                //ent.AddMessage(MessageType.Regular, $"PACKET REMOVED SENT: 0x{serial:X8}", 3, 0x34 + 10, true, TextType.OBJECT);
                Socket.Send_CloseStatusBarGump(serial);
            }
        }
    }

    internal static void CastSpellFromBook(int index, uint bookSerial)
    {
        if (index >= 0)
        {
            LastSpellIndex = index;
            SpellVisualRangeManager.Instance.ClearCasting();
            Socket.Send_CastSpellFromBook(index, bookSerial);
        }
    }

    internal static void CastSpell(int index)
    {
        if (index >= 0)
        {
            LastSpellIndex = index;
            SpellVisualRangeManager.Instance.ClearCasting();
            Socket.Send_CastSpell(index);

            // Record action for script recording
            string name = SpellDefinition.FullIndexGetSpell(index).Name;
            ScriptRecorder.Instance.RecordCastSpell(name);
            ScriptingInfoGump.AddOrUpdateInfo("Last Spell", name);
        }
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="name">Can be a partial match</param>
    internal static bool CastSpellByName(string name)
    {
        name = name.Trim();

        if (!string.IsNullOrEmpty(name) && SpellDefinition.TryGetSpellFromName(name, out SpellDefinition spellDef))
        {
                // Record action for script recording
                ScriptRecorder.Instance.RecordCastSpell(name);
                ScriptingInfoGump.AddOrUpdateInfo("Last Spell", name);

            CastSpell(spellDef.ID);
            return true;
        }

        return false;
    }

    internal static void OpenGuildGump(World world) => Socket.Send_GuildMenuRequest(world);

    internal static void ChangeStatLock(byte stat, Lock state) => Socket.Send_StatLockStateRequest(stat, state);

    internal static void Rename(uint serial, string name) => Socket.Send_RenameRequest(serial, name);

    public static void Logout(World world)
    {
        LoginHandshake.Reconnect = false;
        WorldMapGump.ClearMapCache();

        if ((world.ClientFeatures.Flags & CharacterListFlags.CLF_OWERWRITE_CONFIGURATION_BUTTON) != 0)
        {
            Client.Game.GetScene<GameScene>().DisconnectionRequested = true;
            Socket.Send_LogoutNotification();
        }
        else
        {
            Client.Game.GetScene<GameScene>().DisconnectionRequested = true;
            Client.Game.SetScene(new LoginScene(world));

            Socket?.Disconnect();
            Socket = new AsyncNetClient();
        }
    }

    internal static void UseSkill(int index)
    {
        if (index >= 0)
        {
                // Record action for script recording
                string skillName = "";
                if (index < World.Instance.Player.Skills.Length)
                    skillName = World.Instance.Player.Skills[index].Name;
                ClassicUO.LegionScripting.ScriptRecorder.Instance.RecordUseSkill(skillName);
                ScriptingInfoGump.AddOrUpdateInfo("Last Skill", skillName);

            LastSkillIndex = index;
            Socket.Send_UseSkill(index);
        }
    }

    internal static void OpenPopupMenu(uint serial, bool shift = false)
    {
        shift = shift || Keyboard.Shift;

        if (ProfileManager.CurrentProfile.HoldShiftForContext && !shift)
        {
            return;
        }

        Socket.Send_RequestPopupMenu(serial);
    }

    internal static void ResponsePopupMenu(uint serial, ushort index)
    {
            // Record action for script recording
            ClassicUO.LegionScripting.ScriptRecorder.Instance.RecordContextMenu(serial, index);
            ScriptingInfoGump.AddOrUpdateInfo("Last Context Menu response", index);

        Socket.Send_PopupMenuSelection(serial, index);
    }

    internal static void MessageOverhead(World world, string message, uint entity) => Print(world, world.Get(entity), message);

    internal static void MessageOverhead(World world, string message, ushort hue, uint entity) => Print(world, world.Get(entity), message, hue);

    internal static void AcceptTrade(uint serial, bool accepted) => Socket.Send_TradeResponse(serial, 2, accepted);

    internal static void CancelTrade(uint serial) => Socket.Send_TradeResponse(serial, 1, false);

    internal static void AllNames(World world)
    {
        foreach (Mobile mobile in world.Mobiles.Values)
        {
            if (mobile != world.Player)
            {
                Socket.Send_ClickRequest(mobile.Serial);
            }
        }

        foreach (Item item in world.Items.Values)
        {
            if (item.IsCorpse)
            {
                Socket.Send_ClickRequest(item.Serial);
            }
        }
    }

    internal static void OpenDoor() => Socket.Send_OpenDoor();

    internal static void EmoteAction(string action) => Socket.Send_EmoteAction(action);

    internal static void OpenAbilitiesBook(World world)
    {
        if (UIManager.GetGump<CombatBookGump>() == null)
        {
            UIManager.Add(new CombatBookGump(world, 100, 100));
        }
    }



    private static void SendAbility(World world, byte idx, bool primary)
    {
        if ((world.ClientLockedFeatures.Flags & LockedFeatureFlags.AOS) == 0)
        {
            if (primary)
                Socket.Send_StunRequest();
            else
                Socket.Send_DisarmRequest();
        }
        else
        {
            Socket.Send_UseCombatAbility(world, idx);
        }
    }

    internal static void UsePrimaryAbility(World world)
    {
        ref Ability ability = ref world.Player.Abilities[0];

        if (((byte)ability & 0x80) == 0)
        {
            for (int i = 0; i < 2; i++)
            {
                world.Player.Abilities[i] &= (Ability)0x7F;
            }

            SendAbility(world, (byte)ability, true);
        }
        else
        {
            SendAbility(world, 0, true);
        }

            ClassicUO.LegionScripting.ScriptRecorder.Instance.RecordAbility("primary");

        ability ^= (Ability)0x80;
    }

    internal static void UseSecondaryAbility(World world)
    {
        ref Ability ability = ref world.Player.Abilities[1];

        if (((byte)ability & 0x80) == 0)
        {
            for (int i = 0; i < 2; i++)
            {
                world.Player.Abilities[i] &= (Ability)0x7F;
            }

            SendAbility(world, (byte)ability, false);
        }
        else
        {
            SendAbility(world, 0, true);
        }

        ClassicUO.LegionScripting.ScriptRecorder.Instance.RecordAbility("secondary");

        ability ^= (Ability)0x80;
    }

    // ===================================================
    [Obsolete("temporary workaround to not break assistants")]
    internal static void UsePrimaryAbility() => UsePrimaryAbility(ClassicUO.Client.Game.UO.World);

    [Obsolete("temporary workaround to not break assistants")]
    internal static void UseSecondaryAbility() => UseSecondaryAbility(ClassicUO.Client.Game.UO.World);
    // ===================================================

    internal static void QuestArrow(bool rightClick) => Socket.Send_ClickQuestArrow(rightClick);

    internal static void GrabItem(World world, uint serial, ushort amount, uint bag = 0, bool stack = true)
    {
        //Socket.Send(new PPickUpRequest(serial, amount));

        Item backpack = world.Player.Backpack;

        if (backpack == null)
        {
            return;
        }

        if (bag == 0)
        {
            bag = ProfileManager.CurrentProfile.GrabBagSerial == 0 ? backpack.Serial : ProfileManager.CurrentProfile.GrabBagSerial;
        }

        if (!world.Items.Contains(bag))
        {
            Print(world, ResGeneral.GrabBagNotFound);
            ProfileManager.CurrentProfile.GrabBagSerial = 0;
            bag = backpack.Serial;
        }

        PickUp(world, serial, 0, 0, amount);

            if (stack)
                DropItem
                (
                    serial,
                    0xFFFF,
                    0xFFFF,
                    0,
                    bag
                );
            else
                DropItem
                (
                    serial,
                    0,
                    0,
                    0,
                    bag
                );
        }

    public static void RequestEquippedOPL(World world)
    {
        foreach (Layer layer in Enum.GetValues(typeof(Data.Layer)))
        {
            Item item = world.Player.FindItemByLayer(layer);
            if(item == null) continue;

            world.OPL.Contains(item); //Requests data if we don't have it
        }
    }

    internal static bool Mount()
    {
        if (World.Instance == null) return false;

        if (ProfileManager.CurrentProfile.SavedMountSerial == 0) return false;

        Entity mount = World.Instance.Get(ProfileManager.CurrentProfile.SavedMountSerial);
        if (mount != null)
        {
            DoubleClickQueued(ProfileManager.CurrentProfile.SavedMountSerial);
            ScriptRecorder.Instance.RecordMount(mount);
            return true;
        }

        return false;
    }
}
