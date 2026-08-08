// SPDX-License-Identifier: BSD-2-Clause
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers.Hotkeys;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.Network;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using SDL3;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using ClassicUO.Common.Enums;
using ClassicUO.Game.UI.Gumps.SpellBar;
using ClassicUO.LegionScripting;
using static SDL3.SDL;
using ClassicUO.Game.UI;

namespace ClassicUO.Game.Managers
{
    public sealed class MacroManager : LinkedObject
    {
        public static readonly string[] MacroNames = Enum.GetNames(typeof(MacroType));
        private readonly uint[] _itemsInHand = new uint[2];
        private MacroObject _lastMacro;
        private MacroObject _currentMacroHead; // head node of the macro currently executing (for toggle-stop)
        private long _nextTimer;
        private readonly World _world;

        private readonly byte[] _skillTable =
        {
            1, 2, 35, 4, 6, 12,
            14, 15, 16, 19, 21, 56 /*imbuing*/,
            23, 3, 46, 9, 30, 22,
            48, 32, 33, 47, 36, 38
        };

        private readonly int[] _spellsCountTable =
        {
            Constants.SPELLBOOK_1_SPELLS_COUNT,
            Constants.SPELLBOOK_2_SPELLS_COUNT,
            Constants.SPELLBOOK_3_SPELLS_COUNT,
            Constants.SPELLBOOK_4_SPELLS_COUNT,
            Constants.SPELLBOOK_5_SPELLS_COUNT,
            Constants.SPELLBOOK_6_SPELLS_COUNT,
            Constants.SPELLBOOK_7_SPELLS_COUNT
        };


        public MacroManager(World world)
        {
            _world = world;
            EventSink.JournalEntryAdded += OnJournalEntryAdded;
        }

        private void OnJournalEntryAdded(object sender, JournalEntry e)
        {
            if (e == null || string.IsNullOrEmpty(e.Text) || _world?.Player == null)
            {
                return;
            }

            for (var macro = (Macro)Items; macro != null; macro = (Macro)macro.Next)
            {
                if (macro.HasJournalTriggers && macro.MatchesJournalTrigger(e.Text) && macro.Items is MacroObject macroObject)
                {
                    SetMacroToExecute(macroObject);
                    WaitForTargetTimer = 0;
                    break;
                }
            }
        }

        public long WaitForTargetTimer { get; set; }

        public bool WaitingBandageTarget { get; set; }

        public static MacroManager TryGetMacroManager(World world) => world.Macros;

        public void Load()
        {
            string path = Path.Combine(ProfileManager.ProfilePath, "macros.xml");

            if (!File.Exists(path))
            {
                Log.Trace("No macros.xml file. Creating a default file.");

                Clear();
                CreateDefaultMacros();
                Save();

                return;
            }

            var doc = new XmlDocument();

            try
            {
                doc.Load(path);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());

                return;
            }


            Clear();

            XmlElement root = doc["macros"];

            if (root != null)
            {
                foreach (XmlElement xml in root.GetElementsByTagName("macro"))
                {
                    var macro = new Macro(xml.GetAttribute("name"));
                    macro.Load(xml);
                    PushToBack(macro);
                }
            }
        }

        public void Save(string path = null)
        {
            List<Macro> list = GetAllMacros();

            string tempPath = Path.GetTempFileName();

            path ??= Path.Combine(ProfileManager.ProfilePath, "macros.xml");

            if (!File.Exists(tempPath))
            {
                try
                {
                    File.Create(tempPath).Close();
                }
                catch (Exception)
                {
                    Log.Error($"Warning, unable to create {path}.");
                }
            }

            try
            {
                using (var xml = new XmlTextWriter(tempPath, Encoding.UTF8) { Formatting = Formatting.Indented, IndentChar = '\t', Indentation = 1 })
                {
                    xml.WriteStartDocument(true);
                    xml.WriteStartElement("macros");

                    foreach (Macro macro in list)
                    {
                        macro.Save(xml);
                    }

                    xml.WriteEndElement();
                    xml.WriteEndDocument();
                }

                if (File.Exists(path))
                    File.Delete(path);
                File.Move(tempPath, path);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                Log.Error("Failed to save macros.");
            }
        }

#nullable enable
        public string? GetXmlExport()
        {
            try
            {
                List<Macro> macros = GetAllMacros();

                if (macros.Count == 0)
                    return null;

                var sb = new StringBuilder();
                using (var xml = new XmlTextWriter(new StringWriter(sb)) { Formatting = Formatting.Indented, IndentChar = '\t', Indentation = 1 })
                {
                    xml.WriteStartDocument(true);
                    xml.WriteStartElement("macros");

                    foreach (Macro macro in macros)
                    {
                        macro.Save(xml);
                    }

                    xml.WriteEndElement();
                    xml.WriteEndDocument();
                }
                return sb.ToString();
            }
            catch (Exception e)
            {
                Log.Error($"Error exporting macros to XML: {e}");
            }

            return null;
        }
#nullable disable

        public bool ImportFromXml(string xml)
        {
            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(xml);

                XmlElement root = doc["macros"];

                if (root != null)
                {
                    int addedCount = 0;

                    foreach (XmlElement xmlMacro in root.GetElementsByTagName("macro"))
                    {
                        string macroName = xmlMacro.GetAttribute("name");

                        // Make name unique if it already exists
                        string uniqueName = macroName;
                        int counter = 1;
                        while (GetAllMacros().Any(m => m.Name == uniqueName))
                        {
                            uniqueName = $"{macroName} ({counter++})";
                        }

                        var macro = new Macro(uniqueName);
                        macro.Load(xmlMacro);
                        PushToBack(macro);
                        addedCount++;
                    }

                    if (addedCount > 0)
                    {
                        Save();
                        GameActions.Print($"Imported {addedCount} macro(s) from clipboard!", Constants.HUE_SUCCESS);
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error($"Error importing macros from XML: {e}");
            }

            return false;
        }

        private void CreateDefaultMacros()
        {
            if (LoadFromDefaultMacroSave()) return;

            PushToBack
            (
                new Macro
                (
                    TazLang.Get("paperdoll"),
                    (SDL_Keycode)112,
                    true,
                    false,
                    false
                )
                {
                    Items = new MacroObject((MacroType)8, (MacroSubType)10)
                    {
                        SubMenuType = 1
                    }
                }
            );

            PushToBack
            (
                new Macro
                (
                    TazLang.Get("options"),
                    (SDL_Keycode)111,
                    true,
                    false,
                    false
                )
                {
                    Items = new MacroObject((MacroType)8, (MacroSubType)9)
                    {
                        SubMenuType = 1
                    }
                }
            );

            PushToBack
            (
                new Macro
                (
                    TazLang.Get("journal"),
                    (SDL_Keycode)106,
                    true,
                    false,
                    false
                )
                {
                    Items = new MacroObject((MacroType)8, (MacroSubType)12)
                    {
                        SubMenuType = 1
                    }
                }
            );

            PushToBack
            (
                new Macro
                (
                    TazLang.Get("backpack"),
                    (SDL_Keycode)105,
                    true,
                    false,
                    false
                )
                {
                    Items = new MacroObject((MacroType)8, (MacroSubType)16)
                    {
                        SubMenuType = 1
                    }
                }
            );

            PushToBack
            (
                new Macro
                (
                    "Use last object",
                    SDL_Keycode.SDLK_F5,
                    false,
                    false,
                    false
                )
                {
                    Items = new MacroObject(MacroType.LastObject, MacroSubType.Overview)
                }
            );

            PushToBack
            (
                new Macro
                (
                    "Last target",
                    SDL_Keycode.SDLK_F6,
                    false,
                    false,
                    false
                )
                {
                    Items = new MacroObject(MacroType.LastTarget, MacroSubType.Overview)
                }
            );
        }

        private bool LoadFromDefaultMacroSave()
        {
            string defaultMacroXmlPath = Path.Combine(ProfileManager.RootPath, "macros.xml");

            if (!Path.Exists(defaultMacroXmlPath)) return false;

            var doc = new XmlDocument();

            try
            {
                doc.Load(defaultMacroXmlPath);
                Clear();

                XmlElement root = doc["macros"];

                if (root != null)
                {
                    foreach (XmlElement xml in root.GetElementsByTagName("macro"))
                    {
                        var macro = new Macro(xml.GetAttribute("name"));
                        macro.Load(xml);
                        PushToBack(macro);
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }

            return false;
        }

        public List<Macro> GetAllMacros()
        {
            var m = (Macro)Items;

            while (m?.Previous != null)
            {
                m = (Macro)m.Previous;
            }

            var macros = new List<Macro>();

            while (true)
            {
                if (m != null)
                {
                    macros.Add(m);
                }
                else
                {
                    break;
                }

                m = (Macro)m.Next;
            }

            return macros;
        }

        public bool MoveMacroUp(Macro macro)
        {
            if (macro == null || macro.Previous == null)
            {
                return false;
            }

            var prev = (Macro)macro.Previous;

            Unlink(macro);

            if (prev.Previous != null)
            {
                Insert(prev.Previous, macro);
            }
            else
            {
                macro.Next = prev;
                macro.Previous = null;
                prev.Previous = macro;
                Items = macro;
            }

            return true;
        }

        public bool MoveMacroDown(Macro macro)
        {
            if (macro == null || macro.Next == null)
            {
                return false;
            }

            var next = (Macro)macro.Next;

            Unlink(macro);
            Insert(next, macro);

            return true;
        }

        public Macro FindMacro(SDL_GamepadButton button)
        {
            var obj = (Macro)Items;

            while (obj != null)
            {
                if (obj.ControllerButtons != null)
                {
                    if (obj.ControllerButtons.Length > 0)
                    {
                        if (Controller.AreButtonsPressed(obj.ControllerButtons))
                        {
                            break;
                        }
                    }
                }

                obj = (Macro)obj.Next;
            }

            return obj;
        }

        public Macro FindMacro(SDL_Keycode key, bool alt, bool ctrl, bool shift)
        {
            var obj = (Macro)Items;

            while (obj != null)
            {
                if (obj.Key == key && obj.Alt == alt && obj.Ctrl == ctrl && obj.Shift == shift)
                {
                    break;
                }

                obj = (Macro)obj.Next;
            }

            return obj;
        }

        public Macro FindMacro(MouseButtonType button, bool alt, bool ctrl, bool shift)
        {
            var obj = (Macro)Items;

            while (obj != null)
            {
                if (obj.MouseButton == button && obj.Alt == alt && obj.Ctrl == ctrl && obj.Shift == shift)
                {
                    break;
                }

                obj = (Macro)obj.Next;
            }

            return obj;
        }

        public Macro FindMacro(bool wheelUp, bool alt, bool ctrl, bool shift)
        {
            var obj = (Macro)Items;

            while (obj != null)
            {
                if (obj.WheelScroll == true && obj.WheelUp == wheelUp && obj.Alt == alt && obj.Ctrl == ctrl && obj.Shift == shift)
                {
                    break;
                }

                obj = (Macro)obj.Next;
            }

            return obj;
        }

        public Macro FindMacro(string name)
        {
            var obj = (Macro)Items;

            while (obj != null)
            {
                if (obj.Name == name)
                {
                    break;
                }

                obj = (Macro)obj.Next;
            }

            return obj;
        }

        public void SetMacroToExecute(MacroObject macro)
        {
            // If this exact macro is already running and it contains a loop,
            // treat triggering it again as a "stop" request.
            if (_lastMacro != null && _currentMacroHead == macro && MacroContainsLoop(macro))
            {
                StopExecution();
                return;
            }

            // Reset any loop containers in this macro's action chain so a fresh
            // invocation always starts loops from iteration 0, even if a previous
            // run was interrupted mid-loop.
            for (MacroObject node = macro; node != null; node = (MacroObject)node.Next)
            {
                if (node is MacroLoopContainer loop)
                {
                    loop.Reset();
                }
            }

            _lastMacro = macro;
            _currentMacroHead = macro;
        }

        private static bool MacroContainsLoop(MacroObject macro)
        {
            for (MacroObject node = macro; node != null; node = (MacroObject)node.Next)
            {
                if (node is MacroLoopContainer)
                {
                    return true;
                }
            }

            return false;
        }

        public void StopExecution()
        {
            // Reset any loop containers so a future run starts clean.
            if (_currentMacroHead != null)
            {
                for (MacroObject node = _currentMacroHead; node != null; node = (MacroObject)node.Next)
                {
                    if (node is MacroLoopContainer loop)
                    {
                        loop.Reset();
                    }
                }
            }

            _lastMacro = null;
            _currentMacroHead = null;
        }

        public void Update()
        {
            while (_lastMacro != null)
            {
                switch (Process())
                {
                    case 2:
                        _lastMacro = null;
                        _currentMacroHead = null;

                        break;

                    case 1: return;

                    case 0:
                        _lastMacro = (MacroObject)_lastMacro?.Next;

                        if (_lastMacro == null)
                        {
                            _currentMacroHead = null;
                        }

                        break;
                }
            }
        }

        private int Process()
        {
            int result;

            if (_lastMacro == null) // MRC_STOP
            {
                result = 2;
            }
            else if (_nextTimer <= Time.Ticks)
            {
                result = Process(_lastMacro);
            }
            else // MRC_BREAK_PARSER
            {
                result = 1;
            }

            return result;
        }

        private int Process(MacroObject macro)
        {
            if (macro == null)
            {
                return 0;
            }

            if (macro is MacroLoopContainer loopContainer)
            {
                return ProcessLoopContainer(loopContainer);
            }

            int result = 0;

            switch (macro.Code)
            {
                case MacroType.Say:
                case MacroType.Emote:
                case MacroType.Whisper:
                case MacroType.Yell:
                case MacroType.RazorMacro:

                    if (macro is MacroObjectString { Text: { } text })
                    {
                        MessageType type = MessageType.Regular;
                        ushort hue = ProfileManager.CurrentProfile.SpeechHue;

                        switch (macro.Code)
                        {
                            case MacroType.Emote:
                                text = TazLang.Get("emote_char") + text + TazLang.Get("emote_char");
                                type = MessageType.Emote;
                                hue = ProfileManager.CurrentProfile.EmoteHue;

                                break;

                            case MacroType.Whisper:
                                type = MessageType.Whisper;
                                hue = ProfileManager.CurrentProfile.WhisperHue;

                                break;

                            case MacroType.Yell:
                                type = MessageType.Yell;

                                break;

                            case MacroType.RazorMacro:
                                text = ">macro " + text;

                                break;
                        }

                        GameActions.Say(text, hue, type);
                    }

                    break;

                case MacroType.PrivateSay:

                    if (macro is MacroObjectString { Text: { } msg })
                    {
                        _world.MessageManager.HandleMessage
                        (
                            _world.Player,
                            msg,
                            _world.Player.Name ?? "Me",
                            ProfileManager.CurrentProfile.SpeechHue,
                            MessageType.Regular,
                            3,
                            TextType.OBJECT,
                            true,
                            Settings.GlobalSettings.Language,
                            true
                        );
                    }

                    break;

                case MacroType.Walk:
                    byte dt = (byte)Direction.Up;

                    if (macro.SubCode != MacroSubType.NW)
                    {
                        dt = (byte)(macro.SubCode - 2);

                        if (dt > 7)
                        {
                            dt = 0;
                        }
                    }

                    if (!_world.Player.Pathfinder.AutoWalking)
                    {
                        _world.Player.Walk((Direction)dt, false);
                    }

                    break;

                case MacroType.WarPeace:
                    GameActions.ToggleWarMode(_world.Player);

                    break;

                case MacroType.Paste:
                    string txt = StringHelper.GetClipboardText(true);

                    if (txt != null)
                    {
                        UIManager.SystemChat.TextBoxControl.AppendText(txt);
                    }

                    break;

                case MacroType.Open:
                case MacroType.Close:
                case MacroType.Minimize:
                case MacroType.Maximize:
                case MacroType.ToggleGump:

                    switch (macro.Code)
                    {
                        case MacroType.Open:

                            switch (macro.SubCode)
                            {
                                case MacroSubType.Configuration:
                                    GameActions.OpenSettings(_world);

                                    break;

                                case MacroSubType.Paperdoll:
                                    GameActions.OpenPaperdoll(_world, _world.Player);

                                    break;

                                case MacroSubType.Status:
                                    GameActions.OpenStatusBar(_world);

                                    break;

                                case MacroSubType.Journal:
                                    GameActions.OpenJournal(_world);

                                    break;

                                case MacroSubType.Skills:
                                    GameActions.OpenSkills(_world);

                                    break;

                                case MacroSubType.MageSpellbook:
                                case MacroSubType.NecroSpellbook:
                                case MacroSubType.PaladinSpellbook:
                                case MacroSubType.BushidoSpellbook:
                                case MacroSubType.NinjitsuSpellbook:
                                case MacroSubType.SpellWeavingSpellbook:
                                case MacroSubType.MysticismSpellbook:

                                    SpellBookType type = SpellBookType.Magery;

                                    switch (macro.SubCode)
                                    {
                                        case MacroSubType.NecroSpellbook:
                                            type = SpellBookType.Necromancy;

                                            break;

                                        case MacroSubType.PaladinSpellbook:
                                            type = SpellBookType.Chivalry;

                                            break;

                                        case MacroSubType.BushidoSpellbook:
                                            type = SpellBookType.Bushido;

                                            break;

                                        case MacroSubType.NinjitsuSpellbook:
                                            type = SpellBookType.Ninjitsu;

                                            break;

                                        case MacroSubType.SpellWeavingSpellbook:
                                            type = SpellBookType.Spellweaving;

                                            break;

                                        case MacroSubType.MysticismSpellbook:
                                            type = SpellBookType.Mysticism;

                                            break;

                                        case MacroSubType.BardSpellbook:
                                            type = SpellBookType.Mastery;

                                            break;
                                    }

                                    AsyncNetClient.Socket.Send_OpenSpellBook((byte)type);

                                    break;

                                case MacroSubType.Chat:
                                    GameActions.OpenChat(_world);

                                    break;

                                case MacroSubType.Backpack:
                                    GameActions.OpenBackpack(_world);

                                    break;

                                case MacroSubType.Overview:
                                    GameActions.OpenMiniMap(_world);

                                    break;

                                case MacroSubType.WorldMap:
                                    GameActions.OpenWorldMap(_world);

                                    break;

                                case MacroSubType.Mail:
                                case MacroSubType.PartyManifest:
                                    PartyGump party = UIManager.GetGump<PartyGump>();

                                    if (party == null)
                                    {
                                        int x = Client.Game.Window.ClientBounds.Width / 2 - 272;
                                        int y = Client.Game.Window.ClientBounds.Height / 2 - 240;
                                        UIManager.Add(new PartyGump(_world, x, y, _world.Party.CanLoot));
                                    }
                                    else
                                    {
                                        party.BringOnTop();
                                    }

                                    break;

                                case MacroSubType.Guild:
                                    GameActions.OpenGuildGump(_world);

                                    break;

                                case MacroSubType.QuestLog:
                                    GameActions.RequestQuestMenu(_world);

                                    break;

                                case MacroSubType.PartyChat:
                                case MacroSubType.CombatBook:
                                case MacroSubType.RacialAbilitiesBook:
                                case MacroSubType.BardSpellbook:
                                    Log.Warn($"Macro '{macro.SubCode}' not implemented");

                                    break;
                            }

                            break;

                        case MacroType.Close:
                        case MacroType.Minimize:
                        case MacroType.Maximize:

                            switch (macro.SubCode)
                            {
                                case MacroSubType.WorldMap:

                                    if (macro.Code == MacroType.Close)
                                    {
                                        UIManager.GetGump<MiniMapGump>()?.Dispose();
                                    }

                                    break;

                                case MacroSubType.Configuration:

                                    if (macro.Code == MacroType.Close)
                                    {
                                        UIManager.GetGump<ModernOptionsGump>()?.Dispose();
                                    }

                                    break;

                                case MacroSubType.Paperdoll:

                                    PaperDollGump paperdoll = UIManager.GetGump<PaperDollGump>(_world.Player.Serial);

                                    if (paperdoll != null)
                                    {
                                        if (macro.Code == MacroType.Close)
                                        {
                                            paperdoll.Dispose();
                                        }
                                        else if (macro.Code == MacroType.Minimize)
                                        {
                                            paperdoll.IsMinimized = true;
                                        }
                                        else if (macro.Code == MacroType.Maximize)
                                        {
                                            paperdoll.IsMinimized = false;
                                        }
                                    }

                                    break;

                                case MacroSubType.Status:

                                    var status = StatusGumpBase.GetStatusGump();

                                    if (macro.Code == MacroType.Close)
                                    {
                                        if (status != null)
                                        {
                                            status.Dispose();
                                        }
                                        else
                                        {
                                            UIManager.GetGump<BaseHealthBarGump>(_world.Player)?.Dispose();
                                        }
                                    }
                                    else if (macro.Code == MacroType.Minimize)
                                    {
                                        if (status != null)
                                        {
                                            if (ProfileManager.CurrentProfile.StatusGumpBarMutuallyExclusive)
                                                status.Dispose();

                                            if (ProfileManager.CurrentProfile.CustomBarsToggled)
                                            {
                                                UIManager.Add(new HealthBarGumpCustom(_world, _world.Player) { X = status.ScreenCoordinateX, Y = status.ScreenCoordinateY });
                                            }
                                            else
                                            {
                                                UIManager.Add(new HealthBarGump(_world, _world.Player) { X = status.ScreenCoordinateX, Y = status.ScreenCoordinateY });
                                            }
                                        }
                                        else
                                        {
                                            UIManager.GetGump<BaseHealthBarGump>(_world.Player)?.BringOnTop();
                                        }
                                    }
                                    else if (macro.Code == MacroType.Maximize)
                                    {
                                        if (status != null)
                                        {
                                            status.BringOnTop();
                                        }
                                        else
                                        {
                                            BaseHealthBarGump healthbar = UIManager.GetGump<BaseHealthBarGump>(_world.Player);

                                            if (healthbar != null)
                                            {
                                                UIManager.Add(StatusGumpBase.AddStatusGump(_world, healthbar.ScreenCoordinateX, healthbar.ScreenCoordinateY));
                                            }
                                        }
                                    }

                                    break;

                                case MacroSubType.Journal:
                                    ResizableJournal rjournal = UIManager.GetGump<ResizableJournal>();
                                    if (macro.Code == MacroType.Close)
                                    {
                                        rjournal?.Dispose();
                                    }
                                    break;

                                case MacroSubType.Skills:

                                    if (ProfileManager.CurrentProfile.StandardSkillsGump)
                                    {
                                        StandardSkillsGump skillgump = UIManager.GetGump<StandardSkillsGump>();

                                        if (skillgump != null)
                                        {
                                            if (macro.Code == MacroType.Close)
                                            {
                                                skillgump.Dispose();
                                            }
                                            else if (macro.Code == MacroType.Minimize)
                                            {
                                                skillgump.IsMinimized = true;
                                            }
                                            else if (macro.Code == MacroType.Maximize)
                                            {
                                                skillgump.IsMinimized = false;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (macro.Code == MacroType.Close)
                                        {
                                            UIManager.GetGump<SkillGumpAdvanced>()?.Dispose();
                                        }
                                    }

                                    break;

                                case MacroSubType.MageSpellbook:
                                case MacroSubType.NecroSpellbook:
                                case MacroSubType.PaladinSpellbook:
                                case MacroSubType.BushidoSpellbook:
                                case MacroSubType.NinjitsuSpellbook:
                                case MacroSubType.SpellWeavingSpellbook:
                                case MacroSubType.MysticismSpellbook:

                                    SpellbookGump spellbook = UIManager.GetGump<SpellbookGump>();

                                    if (spellbook != null)
                                    {
                                        if (macro.Code == MacroType.Close)
                                        {
                                            spellbook.Dispose();
                                        }
                                        else if (macro.Code == MacroType.Minimize)
                                        {
                                            spellbook.IsMinimized = true;
                                        }
                                        else if (macro.Code == MacroType.Maximize)
                                        {
                                            spellbook.IsMinimized = false;
                                        }
                                    }

                                    break;

                                case MacroSubType.Overview:

                                    if (macro.Code == MacroType.Close)
                                    {
                                        UIManager.GetGump<MiniMapGump>()?.Dispose();
                                    }
                                    else if (macro.Code == MacroType.Minimize)
                                    {
                                        UIManager.GetGump<MiniMapGump>()?.ToggleSize(false);
                                    }
                                    else if (macro.Code == MacroType.Maximize)
                                    {
                                        UIManager.GetGump<MiniMapGump>()?.ToggleSize(true);
                                    }

                                    break;

                                case MacroSubType.Backpack:

                                    Item backpack = _world.Player.Backpack;

                                    if (backpack != null)
                                    {
                                        ContainerGump backpackGump = UIManager.GetGump<ContainerGump>(backpack.Serial);

                                        if (backpackGump != null)
                                        {
                                            if (macro.Code == MacroType.Close)
                                            {
                                                backpackGump.Dispose();
                                            }
                                            else if (macro.Code == MacroType.Minimize)
                                            {
                                                backpackGump.IsMinimized = true;
                                            }
                                            else if (macro.Code == MacroType.Maximize)
                                            {
                                                backpackGump.IsMinimized = false;
                                            }
                                        }
                                    }

                                    break;

                                case MacroSubType.Mail:
                                    Log.Warn($"Macro '{macro.SubCode}' not implemented");

                                    break;

                                case MacroSubType.PartyManifest:

                                    if (macro.Code == MacroType.Close)
                                    {
                                        UIManager.GetGump<PartyGump>()?.Dispose();
                                    }

                                    break;

                                case MacroSubType.PartyChat:
                                case MacroSubType.CombatBook:
                                case MacroSubType.RacialAbilitiesBook:
                                case MacroSubType.BardSpellbook:
                                    Log.Warn($"Macro '{macro.SubCode}' not implemented");

                                    break;
                            }

                            break;

                        case MacroType.ToggleGump:
                            switch (macro.SubCode)
                            {
                                case MacroSubType.Configuration:
                                    if (!GameActions.CloseSettings())
                                        GameActions.OpenSettings(_world);
                                    break;

                                case MacroSubType.Paperdoll:
                                    if (!GameActions.ClosePaperdoll(_world))
                                        GameActions.OpenPaperdoll(_world, _world.Player);

                                    break;

                                case MacroSubType.Status:
                                    if (!GameActions.CloseStatusBar())
                                        GameActions.OpenStatusBar(_world);

                                    break;

                                case MacroSubType.Journal:
                                    if (!GameActions.CloseAllJournals())
                                        GameActions.OpenJournal(_world);

                                    break;

                                case MacroSubType.Skills:
                                    if (!GameActions.CloseSkills())
                                        GameActions.OpenSkills(_world);

                                    break;

                                case MacroSubType.MageSpellbook:
                                case MacroSubType.NecroSpellbook:
                                case MacroSubType.PaladinSpellbook:
                                case MacroSubType.BushidoSpellbook:
                                case MacroSubType.NinjitsuSpellbook:
                                case MacroSubType.SpellWeavingSpellbook:
                                case MacroSubType.MysticismSpellbook:

                                    SpellBookType type = SpellBookType.Magery;

                                    switch (macro.SubCode)
                                    {
                                        case MacroSubType.NecroSpellbook:
                                            type = SpellBookType.Necromancy;

                                            break;

                                        case MacroSubType.PaladinSpellbook:
                                            type = SpellBookType.Chivalry;

                                            break;

                                        case MacroSubType.BushidoSpellbook:
                                            type = SpellBookType.Bushido;

                                            break;

                                        case MacroSubType.NinjitsuSpellbook:
                                            type = SpellBookType.Ninjitsu;

                                            break;

                                        case MacroSubType.SpellWeavingSpellbook:
                                            type = SpellBookType.Spellweaving;

                                            break;

                                        case MacroSubType.MysticismSpellbook:
                                            type = SpellBookType.Mysticism;

                                            break;

                                        case MacroSubType.BardSpellbook:
                                            type = SpellBookType.Mastery;

                                            break;
                                    }

                                    if (!GameActions.CloseSpellBook(type))
                                        AsyncNetClient.Socket.Send_OpenSpellBook((byte)type);

                                    break;

                                case MacroSubType.Chat:
                                    if (!GameActions.CloseChat())
                                        GameActions.OpenChat(_world);

                                    break;

                                case MacroSubType.Backpack:
                                    if (!GameActions.CloseBackpack(_world))
                                        GameActions.OpenBackpack(_world);

                                    break;

                                case MacroSubType.Overview:
                                    if (!GameActions.CloseMiniMap())
                                        GameActions.OpenMiniMap(_world);

                                    break;

                                case MacroSubType.WorldMap:
                                    if (!GameActions.CloseWorldMap())
                                        GameActions.OpenWorldMap(_world);

                                    break;

                                case MacroSubType.Mail:
                                case MacroSubType.PartyManifest:
                                    PartyGump party = UIManager.GetGump<PartyGump>();

                                    if (party == null)
                                    {
                                        int x = Client.Game.Window.ClientBounds.Width / 2 - 272;
                                        int y = Client.Game.Window.ClientBounds.Height / 2 - 240;
                                        UIManager.Add(new PartyGump(_world, x, y, _world.Party.CanLoot));
                                    }
                                    else
                                    {
                                        party.Dispose();
                                    }

                                    break;

                                case MacroSubType.Guild:
                                    //Guild gump is server-side, no way of knowing if one is open
                                    break;

                                case MacroSubType.QuestLog:
                                    //Server side gump, unknown if it is open

                                    break;

                                case MacroSubType.PartyChat:
                                case MacroSubType.CombatBook:
                                case MacroSubType.RacialAbilitiesBook:
                                case MacroSubType.BardSpellbook:
                                    Log.Warn($"Macro '{macro.SubCode}' not implemented");

                                    break;
                            }
                            break;
                    }

                    break;
                case MacroType.ToggleDurabilityGump:
                    if (!GameActions.CloseDurabilityGump())
                        GameActions.OpenDurabilityGump(_world);

                    break;

                case MacroType.ToggleNearbyLootGump:
                    if (!GameActions.CloseNearbyLootGump())
                        GameActions.OpenNearbyLootGump(_world);

                    break;

                case MacroType.ToggleLegionScripting:
                    if (!GameActions.CloseLegionScriptingGump())
                        GameActions.OpenLegionScriptingGump(_world);

                    break;

                case MacroType.SpellBarRowUp:
                    SpellBar.Instance?.ChangeRow(true);

                    break;

                case MacroType.SpellBarRowDown:
                    SpellBar.Instance?.ChangeRow(false);

                    break;

                case MacroType.SetSpellBarRow:
                    string spellRow = ((MacroObjectString)macro).Text;

                    if (int.TryParse(spellRow, out int row))
                    {
                        SpellBar.Instance?.SetRow(row);
                    }
                    else
                    {
                        GameActions.Print(_world, "That is not a valid row.", Constants.HUE_ERROR);
                    }
                    break;

                case MacroType.Dismount:
                    Item m = _world.Player.FindItemByLayer(Layer.Mount);
                    if (m != null)
                    {
                        GameActions.DoubleClickQueued(_world.Player, true);
                        ScriptRecorder.Instance.RecordDismount();
                    }
                    break;

                case MacroType.Mount:
                    if (!GameActions.Mount())
                    {
                        GameActions.Print(_world, "Saved mount not found.", Constants.HUE_ERROR);
                        goto case MacroType.SetMount;
                    }
                    break;

                case MacroType.SetMount:
                    GameActions.Print(_world, "Target a mount to save it for the Mount macro.", 48);
                    _world.TargetManager.SetTargeting(CursorTarget.SetMount, 0, TargetType.Neutral);
                    break;

                case MacroType.ToggleMount:
                    if (_world.Player.FindItemByLayer(Layer.Mount) != null)
                    {
                        // Player is mounted, dismount
                        GameActions.DoubleClickQueued(_world.Player);
                        ScriptRecorder.Instance.RecordDismount();
                    }
                    else
                    {
                        // Player is not mounted, try to mount
                        if (!GameActions.Mount())
                        {
                            GameActions.Print(_world, "Saved mount not found.", Constants.HUE_ERROR);
                            goto case MacroType.SetMount;
                        }
                    }
                    break;

                case MacroType.AddFriend:
                    GameActions.Print(_world, "Target a player to add as a friend.", 62);
                    _world.TargetManager.SetTargeting(targeted =>
                    {
                        if (targeted != null && targeted is Mobile mobile && mobile.Serial != _world.Player.Serial)
                        {
                            if (FriendsListManager.Instance.AddFriend(mobile))
                            {
                                GameActions.Print(_world, $"Added {mobile.Name} to friends list", 62);
                            }
                            else
                            {
                                GameActions.Print(_world, $"Could not add {mobile.Name} - already in friends list", Constants.HUE_ERROR);
                            }
                        }
                        else
                        {
                            if (targeted is Entity entity && entity.Serial == _world.Player.Serial)
                            {
                                GameActions.Print(_world, "You cannot add yourself as a friend", Constants.HUE_ERROR);
                            }
                            else
                            {
                                GameActions.Print(_world, "Invalid target - must be a player", Constants.HUE_ERROR);
                            }
                        }
                    });
                    break;

                case MacroType.RemoveFriend:
                    GameActions.Print(_world, "Target a friend to remove from your friend list.", Constants.HUE_ERROR);
                    _world.TargetManager.SetTargeting(targeted =>
                    {
                        if (targeted != null && targeted is Mobile mobile)
                        {
                            if (FriendsListManager.Instance.RemoveFriend(mobile))
                            {
                                GameActions.Print(_world, $"Removed {mobile.Name} from friends list", Constants.HUE_ERROR);
                            }
                            else
                            {
                                GameActions.Print(_world, $"Could not remove {mobile.Name} - not in friends list", Constants.HUE_ERROR);
                            }
                        }
                    });
                    break;

                case MacroType.ClearHands:
                    var layersToClear = new List<Layer>();
                    Item mainHand = _world.Player.FindItemByLayer(Layer.OneHanded);
                    Item offHand = _world.Player.FindItemByLayer(Layer.TwoHanded);

                    if (mainHand != null)
                    {
                        ProfileManager.CurrentProfile.SavedMainHandSerial = mainHand.Serial;
                        layersToClear.Add(Layer.OneHanded);
                    }
                    else
                    {
                        ProfileManager.CurrentProfile.SavedMainHandSerial = 0;
                    }

                    if (offHand != null)
                    {
                        ProfileManager.CurrentProfile.SavedOffHandSerial = offHand.Serial;
                        layersToClear.Add(Layer.TwoHanded);
                    }
                    else
                    {
                        ProfileManager.CurrentProfile.SavedOffHandSerial = 0;
                    }

                    if (layersToClear.Count > 0)
                    {
                        AsyncNetClient.Socket.Send_UnequipMacroKR(layersToClear.ToArray().AsSpan());
                    }

                    break;

                case MacroType.EquipHands:
                    var itemsToEquip = new List<uint>();

                    if (ProfileManager.CurrentProfile.SavedMainHandSerial != 0)
                    {
                        Item mainHandItem = _world.Items.Get(ProfileManager.CurrentProfile.SavedMainHandSerial);
                        if (mainHandItem != null && mainHandItem.Container != _world.Player?.Serial)
                        {
                            itemsToEquip.Add(mainHandItem.Serial);
                        }
                    }

                    if (ProfileManager.CurrentProfile.SavedOffHandSerial != 0)
                    {
                        Item offHandItem = _world.Items.Get(ProfileManager.CurrentProfile.SavedOffHandSerial);
                        if (offHandItem != null && offHandItem.Container != _world.Player?.Serial)
                        {
                            itemsToEquip.Add(offHandItem.Serial);
                        }
                    }

                    if (itemsToEquip.Count > 0)
                    {
                        AsyncNetClient.Socket.Send_EquipMacroKR(itemsToEquip.ToArray().AsSpan());
                    }

                    break;

                case MacroType.OpenDoor:
                    GameActions.OpenDoor();

                    break;

                case MacroType.UseSkill:
                    int skill = macro.SubCode - MacroSubType.Anatomy;

                    if (skill >= 0 && skill < 24)
                    {
                        skill = _skillTable[skill];

                        if (skill != 0xFF)
                        {
                            GameActions.UseSkill(skill);
                        }
                    }

                    break;

                case MacroType.LastSkill:
                    GameActions.UseSkill(GameActions.LastSkillIndex);

                    break;

                case MacroType.CastSpell:
                    int spell = macro.SubCode - MacroSubType.Clumsy + 1;

                    if (spell > 0 && spell <= 151)
                    {
                        int totalCount = 0;
                        int spellType;

                        for (spellType = 0; spellType < 8; spellType++)
                        {
                            totalCount += _spellsCountTable[spellType];

                            if (spell <= totalCount)
                            {
                                break;
                            }
                        }

                        if (spellType < 7)
                        {
                            spell -= totalCount - _spellsCountTable[spellType];
                            spell += spellType * 100;

                            if (spellType > 2)
                            {
                                spell += 100;

                                // fix offset for mysticism
                                if (spellType == 6)
                                {
                                    spell -= 23;
                                }
                            }

                            GameActions.CastSpell(spell);
                        }
                    }

                    break;

                case MacroType.LastSpell:
                    GameActions.CastSpell(GameActions.LastSpellIndex);

                    break;

                case MacroType.Bow:
                case MacroType.Salute:
                    int index = macro.Code - MacroType.Bow;

                    const string BOW = "bow";
                    const string SALUTE = "salute";

                    GameActions.EmoteAction(index == 0 ? BOW : SALUTE);

                    break;

                case MacroType.QuitGame:
                    Client.Game.GetScene<GameScene>()?.RequestQuitGame();

                    break;

                case MacroType.AllNames:
                    GameActions.AllNames(_world);

                    break;

                case MacroType.ToggleVoiceRecognition:
                    {
                        VoiceRecognitionManager vm = VoiceRecognitionManager.Instance;
                        if (vm.IsInitializing)
                        {
                            GameActions.Print(_world, "[Voice] Model is still loading...");
                        }
                        else if (!vm.IsInitialized)
                        {
                            Configuration.Profile profile = ProfileManager.CurrentProfile;
                            if (profile != null && !string.IsNullOrEmpty(profile.VoiceModelPath))
                            {
                                GameActions.Print(_world, "[Voice] Loading model...");
                                vm.InitializeAsync(profile.VoiceModelPath, startListeningAfter: true);
                            }
                            else
                            {
                                GameActions.Print(_world, "[Voice] No model path set - configure in Options > Sound");
                            }
                        }
                        else
                        {
                            vm.ToggleListening();
                            if (!vm.IsListening)
                                GameActions.Print(_world, "[Voice] Off");
                            // "[Voice] Listening..." is printed by VoiceRecognitionManager.StatusMessage when recording actually starts
                        }

                        break;
                    }

                case MacroType.LastObject:

                    if (_world.Get(_world.LastObject) != null)
                    {
                        GameActions.DoubleClick(_world, _world.LastObject);
                    }

                    break;

                case MacroType.UseItemInHand:
                    Item itemInLeftHand = _world.Player.FindItemByLayer(Layer.OneHanded);

                    if (itemInLeftHand != null)
                    {
                        GameActions.DoubleClick(_world, itemInLeftHand.Serial);
                    }
                    else
                    {
                        Item itemInRightHand = _world.Player.FindItemByLayer(Layer.TwoHanded);

                        if (itemInRightHand != null)
                        {
                            GameActions.DoubleClick(_world, itemInRightHand.Serial);
                        }
                    }

                    break;

                case MacroType.LastTarget:

                    //if (WaitForTargetTimer == 0)
                    //    WaitForTargetTimer = Time.Ticks + Constants.WAIT_FOR_TARGET_DELAY;

                    if (_world.TargetManager.IsTargeting)
                    {
                        //if (TargetManager.TargetingState != TargetType.Object)
                        //{
                        //    TargetManager.TargetGameObject(TargetManager.LastGameObject);
                        //}
                        //else

                        if (_world.TargetManager.TargetingState != CursorTarget.Object && !_world.TargetManager.LastTargetInfo.IsEntity)
                        {
                            _world.TargetManager.TargetLast();
                        }
                        else if (_world.TargetManager.LastTargetInfo.IsEntity)
                        {
                            _world.TargetManager.Target(_world.TargetManager.LastTargetInfo.Serial);
                        }
                        else
                        {
                            _world.TargetManager.Target(_world.TargetManager.LastTargetInfo.Graphic, _world.TargetManager.LastTargetInfo.X, _world.TargetManager.LastTargetInfo.Y, _world.TargetManager.LastTargetInfo.Z);
                        }

                        WaitForTargetTimer = 0;
                    }
                    else if (WaitForTargetTimer < Time.Ticks)
                    {
                        WaitForTargetTimer = 0;
                    }
                    else
                    {
                        result = 1;
                    }

                    break;

                case MacroType.TargetSelf:

                    //if (WaitForTargetTimer == 0)
                    //    WaitForTargetTimer = Time.Ticks + Constants.WAIT_FOR_TARGET_DELAY;

                    if (_world.TargetManager.IsTargeting)
                    {
                        _world.TargetManager.Target(_world.Player);
                        WaitForTargetTimer = 0;
                    }
                    else if (WaitForTargetTimer < Time.Ticks)
                    {
                        WaitForTargetTimer = 0;
                    }
                    else
                    {
                        result = 1;
                    }

                    break;

                case MacroType.ArmDisarm:
                    int handIndex = 1 - (macro.SubCode - MacroSubType.LeftHand);
                    GameScene gs = Client.Game.GetScene<GameScene>();

                    if (handIndex < 0 || handIndex > 1 || Client.Game.UO.GameCursor.ItemHold.Enabled)
                    {
                        break;
                    }

                    if (_itemsInHand[handIndex] != 0)
                    {
                        if (_world.Items.TryGetValue(_itemsInHand[handIndex], out Item item))
                        {
                            ObjectActionQueue.Instance.Enqueue(ObjectActionQueueItem.EquipItem(item, (Layer)item.ItemData.Layer), ActionPriority.EquipItem);

                            _itemsInHand[handIndex] = 0;
                            _nextTimer = Time.Ticks + 1000;
                        }
                    }
                    else
                    {
                        Item backpack = _world.Player.Backpack;

                        if (backpack == null)
                        {
                            break;
                        }

                        Item item = _world.Player.FindItemByLayer(Layer.OneHanded + (byte)handIndex);

                        if (item != null)
                        {
                            _itemsInHand[handIndex] = item.Serial;

                            GameActions.PickUp(_world, item, 0, 0, 1);

                            GameActions.DropItem
                            (
                                Client.Game.UO.GameCursor.ItemHold.Serial,
                                0xFFFF,
                                0xFFFF,
                                0,
                                backpack.Serial
                            );

                            _nextTimer = Time.Ticks + 1000;
                        }
                    }

                    break;

                case MacroType.WaitForTarget:

                    if (WaitForTargetTimer == 0)
                    {
                        WaitForTargetTimer = Time.Ticks + Constants.WAIT_FOR_TARGET_DELAY;
                    }

                    if (_world.TargetManager.IsTargeting || WaitForTargetTimer < Time.Ticks)
                    {
                        WaitForTargetTimer = 0;
                    }
                    else
                    {
                        result = 1;
                    }

                    break;

                case MacroType.TargetNext:

                    uint sel_obj = _world.FindNext(ScanTypeObject.Mobiles, _world.TargetManager.LastTargetInfo.Serial, false);

                    if (SerialHelper.IsValid(sel_obj))
                    {
                        _world.TargetManager.LastTargetInfo.SetEntity(sel_obj);
                        _world.TargetManager.LastAttack = sel_obj;
                    }

                    break;

                case MacroType.AttackLast:
                    if (_world.TargetManager.LastTargetInfo.IsEntity)
                    {
                        GameActions.Attack(_world, _world.TargetManager.LastTargetInfo.Serial);
                    }

                    break;

                case MacroType.Delay:
                    var mosss = (MacroObjectString)macro;
                    string str = mosss.Text;

                    if (!string.IsNullOrEmpty(str) && int.TryParse(str, out int rr))
                    {
                        _nextTimer = Time.Ticks + rr;
                    }

                    break;

                case MacroType.CircleTrans:
                    ProfileManager.GlobalSettings.UseCircleOfTransparency = !ProfileManager.GlobalSettings.UseCircleOfTransparency;

                    break;

                case MacroType.ToggleHouses:
                    ProfileManager.CurrentProfile.ForceHouseTransparency = !ProfileManager.CurrentProfile.ForceHouseTransparency;

                    break;

                case MacroType.CloseGump:

                    UIManager.Gumps.Where(s => !(s is TopBarGump) && !(s is BuffGump) && !(s is ImprovedBuffGump) && !(s is WorldViewportGump)).ToList().ForEach(s => s.Dispose());

                    break;

                case MacroType.AlwaysRun:
                    ProfileManager.CurrentProfile.AlwaysRun = !ProfileManager.CurrentProfile.AlwaysRun;

                    GameActions.Print(_world, ProfileManager.CurrentProfile.AlwaysRun ? TazLang.Get("always_run_is_now_on") : TazLang.Get("always_run_is_now_off"));

                    break;

                case MacroType.SaveDesktop:
                    ProfileManager.CurrentProfile?.Save(_world, ProfileManager.ProfilePath);

                    break;

                case MacroType.EnableRangeColor:
                    ProfileManager.CurrentProfile.NoColorObjectsOutOfRange = true;

                    break;

                case MacroType.DisableRangeColor:
                    ProfileManager.CurrentProfile.NoColorObjectsOutOfRange = false;

                    break;

                case MacroType.ToggleRangeColor:
                    ProfileManager.CurrentProfile.NoColorObjectsOutOfRange = !ProfileManager.CurrentProfile.NoColorObjectsOutOfRange;

                    break;

                case MacroType.AttackSelectedTarget:

                    if (SerialHelper.IsMobile(_world.TargetManager.SelectedTarget))
                    {
                        GameActions.Attack(_world, _world.TargetManager.SelectedTarget);
                    }

                    break;

                case MacroType.UseSelectedTarget:
                    if (SerialHelper.IsValid(_world.TargetManager.SelectedTarget))
                    {
                        GameActions.DoubleClick(_world, _world.TargetManager.SelectedTarget);
                    }

                    break;

                case MacroType.CurrentTarget:

                    if (_world.TargetManager.SelectedTarget != 0)
                    {
                        if (WaitForTargetTimer == 0)
                        {
                            WaitForTargetTimer = Time.Ticks + Constants.WAIT_FOR_TARGET_DELAY;
                        }

                        if (_world.TargetManager.IsTargeting)
                        {
                            _world.TargetManager.Target(_world.TargetManager.SelectedTarget);
                            WaitForTargetTimer = 0;
                        }
                        else if (WaitForTargetTimer < Time.Ticks)
                        {
                            WaitForTargetTimer = 0;
                        }
                        else
                        {
                            result = 1;
                        }
                    }

                    break;

                case MacroType.TargetSystemOnOff:

                    if (ProfileManager.CurrentProfile.UseNewTargetSystem)
                    {
                        ProfileManager.CurrentProfile.UseNewTargetSystem = false;
                        GameActions.Print(_world, "Target System: Off");
                    }
                    else
                    {
                        ProfileManager.CurrentProfile.UseNewTargetSystem = true;
                        GameActions.Print(_world, "Target System: On");
                    }

                    break;

                case MacroType.BandageSelf:
                case MacroType.BandageTarget:

                    if (Client.Game.UO.Version < ClientVersion.CV_5020 || ProfileManager.CurrentProfile.BandageSelfOld)
                    {
                        if (WaitingBandageTarget)
                        {
                            if (WaitForTargetTimer == 0)
                            {
                                WaitForTargetTimer = Time.Ticks + Constants.WAIT_FOR_TARGET_DELAY;
                            }

                            if (_world.TargetManager.IsTargeting)
                            {
                                if (macro.Code == MacroType.BandageSelf)
                                {
                                    _world.TargetManager.Target(_world.Player);
                                }
                                else if (_world.TargetManager.LastTargetInfo.IsEntity)
                                {
                                    _world.TargetManager.Target(_world.TargetManager.LastTargetInfo.Serial);
                                }

                                WaitingBandageTarget = false;
                                WaitForTargetTimer = 0;
                            }
                            else if (WaitForTargetTimer < Time.Ticks)
                            {
                                WaitingBandageTarget = false;
                                WaitForTargetTimer = 0;
                            }
                            else
                            {
                                result = 1;
                            }
                        }
                        else
                        {
                            Item bandage = _world.Player.FindBandage();

                            if (bandage != null)
                            {
                                WaitingBandageTarget = true;
                                GameActions.DoubleClick(_world, bandage);
                                result = 1;
                            }
                        }
                    }
                    else
                    {
                        Item bandage = _world.Player.FindBandage();

                        if (bandage != null)
                        {
                            if (macro.Code == MacroType.BandageSelf)
                            {
                                AsyncNetClient.Socket.Send_TargetSelectedObject(bandage.Serial, _world.Player.Serial);
                            }
                            else if (SerialHelper.IsMobile(_world.TargetManager.SelectedTarget))
                            {
                                AsyncNetClient.Socket.Send_TargetSelectedObject(bandage.Serial, _world.TargetManager.SelectedTarget);
                            }
                        }
                    }

                    break;

                case MacroType.SetUpdateRange:
                case MacroType.ModifyUpdateRange:

                    if (macro is MacroObjectString moss && !string.IsNullOrEmpty(moss.Text) && byte.TryParse(moss.Text, out byte res))
                    {
                        if (res < Constants.MIN_VIEW_RANGE)
                        {
                            res = Constants.MIN_VIEW_RANGE;
                        }
                        else if (res > Constants.MAX_VIEW_RANGE)
                        {
                            res = Constants.MAX_VIEW_RANGE;
                        }

                        _world.ClientViewRange = res;

                        GameActions.Print(_world, string.Format(TazLang.Get("client_view_range_is_now0"), res));
                    }

                    break;

                case MacroType.IncreaseUpdateRange:
                    _world.ClientViewRange++;

                    if (_world.ClientViewRange > Constants.MAX_VIEW_RANGE)
                    {
                        _world.ClientViewRange = Constants.MAX_VIEW_RANGE;
                    }

                    GameActions.Print(_world, string.Format(TazLang.Get("client_view_range_is_now0"), _world.ClientViewRange));

                    break;

                case MacroType.DecreaseUpdateRange:
                    _world.ClientViewRange--;

                    if (_world.ClientViewRange < Constants.MIN_VIEW_RANGE)
                    {
                        _world.ClientViewRange = Constants.MIN_VIEW_RANGE;
                    }

                    GameActions.Print(_world, string.Format(TazLang.Get("client_view_range_is_now0"), _world.ClientViewRange));

                    break;

                case MacroType.MaxUpdateRange:
                    _world.ClientViewRange = Constants.MAX_VIEW_RANGE;
                    GameActions.Print(_world, string.Format(TazLang.Get("client_view_range_is_now0"), _world.ClientViewRange));

                    break;

                case MacroType.MinUpdateRange:
                    _world.ClientViewRange = Constants.MIN_VIEW_RANGE;
                    GameActions.Print(_world, string.Format(TazLang.Get("client_view_range_is_now0"), _world.ClientViewRange));

                    break;

                case MacroType.DefaultUpdateRange:
                    _world.ClientViewRange = Constants.MAX_VIEW_RANGE;
                    GameActions.Print(_world, string.Format(TazLang.Get("client_view_range_is_now0"), _world.ClientViewRange));

                    break;

                case MacroType.SelectNext:
                case MacroType.SelectPrevious:
                case MacroType.SelectNearest:
                    // scanRange:
                    // 0 - SelectNext
                    // 1 - SelectPrevious
                    // 2 - SelectNearest
                    var scanRange = (ScanModeObject)(macro.Code - MacroType.SelectNext);

                    // scantype:
                    // 0 - Hostile (only hostile mobiles: gray, criminal, enemy, murderer)
                    // 1 - Party (only party members)
                    // 2 - Follower (only your followers)
                    // 3 - Object (???)
                    // 4 - Mobile (any mobiles)
                    // 5 - Friend (only mobiles on your friends list)
                    var scantype = (ScanTypeObject)(macro.SubCode - MacroSubType.Hostile);

                    if (scanRange == ScanModeObject.Nearest)
                    {
                        SetLastTarget(_world.FindNearest(scantype));
                    }
                    else
                    {
                        SetLastTarget(_world.FindNext(scantype, _world.TargetManager.SelectedTarget, scanRange == ScanModeObject.Previous));
                    }

                    break;

                case MacroType.ToggleBuffIconGump:
                    if (ProfileManager.CurrentProfile.UseImprovedBuffBar)
                    {
                        ImprovedBuffGump buff = UIManager.GetGump<ImprovedBuffGump>();
                        if (buff != null)
                        {
                            buff.Dispose();
                        }
                        else
                        {
                            UIManager.Add(new ImprovedBuffGump(_world));
                        }
                    }
                    else
                    {
                        BuffGump buff = UIManager.GetGump<BuffGump>();

                        if (buff != null)
                        {
                            buff.Dispose();
                        }
                        else
                        {
                            UIManager.Add(new BuffGump(_world, 100, 100));
                        }
                    }

                    break;

                case MacroType.InvokeVirtue:
                    byte id = (byte)(macro.SubCode - MacroSubType.Honor + 1);
                    AsyncNetClient.Socket.Send_InvokeVirtueRequest(id);

                    break;

                case MacroType.PrimaryAbility:
                    GameActions.UsePrimaryAbility(_world);

                    break;

                case MacroType.SecondaryAbility:
                    GameActions.UseSecondaryAbility(_world);

                    break;

                case MacroType.ToggleGargoyleFly:

                    if (_world.Player.Race == RaceType.GARGOYLE)
                    {
                        AsyncNetClient.Socket.Send_ToggleGargoyleFlying();
                    }

                    break;

                case MacroType.EquipLastWeapon:
                    AsyncNetClient.Socket.Send_EquipLastWeapon(_world);

                    break;

                case MacroType.KillGumpOpen:
                    // TODO:
                    break;

                case MacroType.Zoom:

                    switch (macro.SubCode)
                    {
                        case MacroSubType.MSC_NONE:
                        case MacroSubType.DefaultZoom:
                            Client.Game.Scene.Camera.Zoom = ProfileManager.CurrentProfile.DefaultScale;

                            break;

                        case MacroSubType.ZoomIn:
                            Client.Game.Scene.Camera.ZoomIn();

                            break;

                        case MacroSubType.ZoomOut:
                            Client.Game.Scene.Camera.ZoomOut();

                            break;
                    }

                    break;

                case MacroType.SetZoomLevel:
                    if (macro is MacroObjectString zoomStr && float.TryParse(zoomStr.Text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float zoomLevel))
                    {
                        Client.Game.Scene.Camera.Zoom = zoomLevel;
                    }

                    break;

                case MacroType.ToggleChatVisibility:
                    UIManager.SystemChat?.ToggleChatVisibility();

                    break;

                case MacroType.Aura:
                    // hold to draw
                    break;

                case MacroType.AuraOnOff:
                    _world.AuraManager.ToggleVisibility();

                    break;

                case MacroType.Grab:
                    GameActions.Print(_world, TazLang.Get("target_an_item_to_grab_it"));
                    _world.TargetManager.SetTargeting(CursorTarget.Grab, 0, TargetType.Neutral);

                    break;

                case MacroType.LootHoveredItem:
                {
                    Item lootItem = GetHoveredItem();

                    if (lootItem != null && !lootItem.IsDestroyed && lootItem.IsLootable)
                        GameActions.GrabItem(_world, lootItem.Serial, lootItem.Amount);

                    break;
                }

                case MacroType.SetGrabBag:
                    GameActions.Print(_world, TazLang.Get("target_container_to_grab_items_into"));
                    _world.TargetManager.SetTargeting(CursorTarget.SetGrabBag, 0, TargetType.Neutral);

                    break;

                case MacroType.NamesOnOff:
                    _world.NameOverHeadManager.ToggleOverheads();

                    break;

                case MacroType.UsePotion:
                    scantype = (ScanTypeObject)(macro.SubCode - MacroSubType.ConfusionBlastPotion);

                    ushort start = (ushort)(0x0F06 + scantype);

                    Item potion = _world.Player.FindItemByGraphic(start);

                    if (potion != null)
                    {
                        GameActions.DoubleClick(_world, potion);
                    }

                    break;

                case MacroType.UseObject:
                    Item obj;

                    switch (macro.SubCode)
                    {
                        case MacroSubType.BestHealPotion:
                            Span<int> healpotion_clilocs = stackalloc int[3] { 1041330, 1041329, 1041328 };

                            obj = _world.Player.FindPreferredItemByCliloc(healpotion_clilocs);

                            if (obj != null)
                            {
                                GameActions.DoubleClick(_world, obj);
                            }

                            break;

                        case MacroSubType.BestCurePotion:
                            Span<int> curepotion_clilocs = stackalloc int[3] { 1041317, 1041316, 1041315 };

                            obj = _world.Player.FindPreferredItemByCliloc(curepotion_clilocs);

                            if (obj != null)
                            {
                                GameActions.DoubleClick(_world, obj);
                            }

                            break;

                        case MacroSubType.BestRefreshPotion:
                            Span<int> refreshpotion_clilocs = stackalloc int[2] { 1041327, 1041326 };

                            obj = _world.Player.FindPreferredItemByCliloc(refreshpotion_clilocs);

                            if (obj != null)
                            {
                                GameActions.DoubleClick(_world, obj);
                            }

                            break;

                        case MacroSubType.BestStrengthPotion:
                            Span<int> strpotion_clilocs = stackalloc int[2] { 1041321, 1041320 };

                            obj = _world.Player.FindPreferredItemByCliloc(strpotion_clilocs);

                            if (obj != null)
                            {
                                GameActions.DoubleClick(_world, obj);
                            }

                            break;

                        case MacroSubType.BestAgiPotion:
                            Span<int> agipotion_clilocs = stackalloc int[2] { 1041319, 1041318 };

                            obj = _world.Player.FindPreferredItemByCliloc(agipotion_clilocs);

                            if (obj != null)
                            {
                                GameActions.DoubleClick(_world, obj);
                            }

                            break;

                        case MacroSubType.BestExplosionPotion:
                            Span<int> explopotion_clilocs = stackalloc int[3] { 1041333, 1041332, 1041331 };

                            obj = _world.Player.FindPreferredItemByCliloc(explopotion_clilocs);

                            if (obj != null)
                            {
                                GameActions.DoubleClick(_world, obj);
                            }

                            break;

                        case MacroSubType.BestConflagPotion:
                            Span<int> conflagpotion_clilocs = stackalloc int[2] { 1072098, 1072095 };

                            obj = _world.Player.FindPreferredItemByCliloc(conflagpotion_clilocs);

                            if (obj != null)
                            {
                                GameActions.DoubleClick(_world, obj);
                            }

                            break;

                        case MacroSubType.HealStone:
                            obj = _world.Player.FindItemByCliloc(1095376);

                            if (obj != null)
                            {
                                GameActions.DoubleClick(_world, obj);
                            }

                            break;

                        case MacroSubType.SpellStone:
                            obj = _world.Player.FindItemByCliloc(1095377);

                            if (obj != null)
                            {
                                GameActions.DoubleClick(_world, obj);
                            }

                            break;

                        case MacroSubType.EnchantedApple:
                            obj = _world.Player.FindItemByCliloc(1032248);

                            if (obj != null)
                            {
                                GameActions.DoubleClick(_world, obj);
                            }

                            break;

                        case MacroSubType.PetalsOfTrinsic:
                            obj = _world.Player.FindItemByCliloc(1062926);

                            if (obj != null)
                            {
                                GameActions.DoubleClick(_world, obj);
                            }

                            break;

                        case MacroSubType.OrangePetals:
                            obj = _world.Player.FindItemByCliloc(1053122);

                            if (obj != null)
                            {
                                GameActions.DoubleClick(_world, obj);
                            }

                            break;

                        case MacroSubType.SmokeBomb:
                            obj = _world.Player.FindItemByGraphic(0x2808);

                            if (obj != null)
                            {
                                GameActions.DoubleClick(_world, obj);
                            }

                            break;

                        case MacroSubType.TrappedBox:
                            Span<int> trapbox_clilocs = stackalloc int[7] { 1015093, 1022473, 1044309, 1022474, 1023709, 1027808, 1027809 };

                            obj = _world.Player.FindPreferredItemByCliloc(trapbox_clilocs);

                            if (obj != null)
                            {
                                GameActions.DoubleClick(_world, obj);
                            }

                            break;
                    }

                    break;

                case MacroType.UseType:
                    var useTypeString = (MacroObjectString)macro;
                    string typePattern = useTypeString.Text;

                    if (!string.IsNullOrEmpty(typePattern))
                    {
                        // Parse pattern: format is "graphic hue" or just "graphic"
                        // Example: "0x0F0E 0" for graphic 0x0F0E with hue 0
                        // Example: "0x0F0E" for graphic 0x0F0E with any hue
                        string[] parts = typePattern.Split(' ');
                        ushort graphic = 0;
                        ushort? hue = null;

                        int parsed = 0;

                        if (parts.Length >= 1)
                            if (!StringHelper.TryParseInt(parts[0], out parsed))
                                break; // Invalid graphic, exit

                        graphic = (ushort)parsed;

                        if (parts.Length == 2)
                            // Hue is optional
                            if (StringHelper.TryParseInt(parts[1], out int h))
                                hue = (ushort)h;

                        Item foundItem = _world.Player.FindItemByGraphicAndHue(graphic, hue);

                        if (foundItem != null)
                        {
                            GameActions.DoubleClick(_world, foundItem);
                            ScriptRecorder.Instance.RecordUseItem(foundItem);
                        }
                    }

                    break;

                case MacroType.CloseAllHealthBars:

                    //Includes HealthBarGump/HealthBarGumpCustom

                    UIManager.ForEach<BaseHealthBarGump>(g =>
                    {
                        if (UIManager.AnchorManager[g] == null && g.LocalSerial != _world.Player)
                        {
                            g.Dispose();
                        }
                    });

                    break;

                case MacroType.CloseInactiveHealthBars:
                    UIManager.ForEach<BaseHealthBarGump>(g =>
                    {
                        if (g.IsInactive && g.LocalSerial != _world.Player)
                        {
                            if (UIManager.AnchorManager[g] != null)
                            {
                                UIManager.AnchorManager[g].DetachControl(g);
                            }

                            g.Dispose();
                        }
                    });
                    break;

                case MacroType.CloseCorpses:
                    CorpseContainerStyle corpseStyle = ProfileManager.CurrentProfile?.CorpseContainerStyle ?? CorpseContainerStyle.Grid;

                    if (corpseStyle is CorpseContainerStyle.Original or CorpseContainerStyle.OldGridLootAndContainer)
                        UIManager.ForEach<ContainerGump>(g =>
                        {
                            if (g.Graphic == ContainerGump.CORPSES_GUMP)
                                g.Dispose();
                        });

                    if (corpseStyle is CorpseContainerStyle.OldGridLoot or CorpseContainerStyle.OldGridLootAndContainer)
                        UIManager.ForEach<GridLootGump>(g =>
                        {
                            g.Dispose();
                        });

                    UIManager.ForEach<GridContainer>(g =>
                    {
                        Item item = _world.Items.Get(g.LocalSerial);
                        if (item != null && item.IsCorpse) g.Dispose();
                    });
                    break;

                case MacroType.ToggleDrawRoofs:
                    ProfileManager.CurrentProfile.DrawRoofs = !ProfileManager.CurrentProfile.DrawRoofs;

                    break;

                case MacroType.ToggleTreeStumps:
                    StaticFilters.CleanTreeTextures();
                    ProfileManager.CurrentProfile.TreeToStumps = !ProfileManager.CurrentProfile.TreeToStumps;

                    break;

                case MacroType.ToggleVegetation:
                    ProfileManager.CurrentProfile.HideVegetation = !ProfileManager.CurrentProfile.HideVegetation;

                    break;

                case MacroType.BorderCaveTiles:
                    ProfileManager.CurrentProfile.EnableCaveBorder = !ProfileManager.CurrentProfile.EnableCaveBorder;
                    if (ProfileManager.CurrentProfile.EnableCaveBorder)
                        StaticFilters.ApplyCaveTileBorder();

                    break;

                case MacroType.LookAtMouse:
                    // handle in gamesceneinput
                    break;

                case MacroType.UseCounterBar:
                    string counterIndex = ((MacroObjectString)macro).Text;

                    if (!string.IsNullOrEmpty(counterIndex) && int.TryParse(counterIndex, out int cIndex))
                    {
                        CounterBarGump.CurrentCounterBarGump?.GetCounterItem(cIndex)?.Use();
                    }
                    break;
                case MacroType.ClientCommand:
                    string command = ((MacroObjectString)macro).Text;

                    if (!string.IsNullOrEmpty(command))
                    {
                        string[] parts = command.Split(' ');
                        _world.CommandManager.Execute(parts[0], parts);
                    }
                    break;
                case MacroType.DisarmAbility:
                    AsyncNetClient.Socket.Send_DisarmRequest();

                    break;

                case MacroType.StunAbility:
                    AsyncNetClient.Socket.Send_StunRequest();

                    break;

                case MacroType.ShowNearbyItems:
                    UIManager.Add(new NearbyItems(_world));
                    break;

                case MacroType.ToggleHudVisible:
                    HideHudManager.ToggleHidden(ProfileManager.CurrentProfile.HideHudGumpFlags);
                    break;

                case MacroType.Resync:
                    AsyncNetClient.Socket.Send_Resync();
                    break;

                case MacroType.ToggleHotkeys:
                    HotKeyRegistrar.ToggleHotkeysEnabled();
                    break;


                case MacroType.CastMasterySpell:
                    int mspell = (int)macro.SubCode + 459; //Inspire is enum #242 for backwards compat, we need to add 459 because 242 + 459 = 701 which is the spell index

                    GameActions.CastSpell(mspell);
                    break;

                case MacroType.ToggleAutoLoot:
                    ProfileManager.CurrentProfile.EnableAutoLoot = !ProfileManager.CurrentProfile.EnableAutoLoot;
                    if (!ProfileManager.CurrentProfile.EnableAutoLoot) AutoLootManager.Instance.ClearActiveLootQueue();
                    break;

                case MacroType.SetLastTarget:
                    GameActions.Print("Who would you like to set as last target?");
                    _world.TargetManager.SetTargeting((o) =>
                    {
                        if (o is Entity e)
                            SetLastTarget(e);
                    });
                    break;

                case MacroType.ToggleAutoWalk:
                    GameScene.Instance.ToggleAutoWalk(null);
                    break;

                case MacroType.ToggleBandageAgent:
                    bool newStatus = !ProfileManager.CurrentProfile.EnableBandageAgent;
                    ProfileManager.CurrentProfile.EnableBandageAgent = newStatus;
                    GameActions.Print($"Bandage agent {(newStatus ? "enabled" : "disabled")}.", newStatus ? Constants.HUE_SUCCESS : Constants.HUE_ERROR);
                    break;

                case MacroType.ToggleBuyAgent:
                    bool newBuyStatus = !ProfileManager.CurrentProfile.BuyAgentEnabled;
                    ProfileManager.CurrentProfile.BuyAgentEnabled = newBuyStatus;
                    GameActions.Print($"Buy agent {(newBuyStatus ? "enabled" : "disabled")}.", newBuyStatus ? Constants.HUE_SUCCESS : Constants.HUE_ERROR);
                    break;

                case MacroType.ToggleSellAgent:
                    bool newSellStatus = !ProfileManager.CurrentProfile.SellAgentEnabled;
                    ProfileManager.CurrentProfile.SellAgentEnabled = newSellStatus;
                    GameActions.Print($"Sell agent {(newSellStatus ? "enabled" : "disabled")}.", newSellStatus ? Constants.HUE_SUCCESS : Constants.HUE_ERROR);
                    break;

                case MacroType.SetOrganizerSource:
                    if (macro is MacroObjectString { Text: { } organizerName } && !string.IsNullOrWhiteSpace(organizerName))
                    {
                        OrganizerAgent.Instance.SetSourceContainerViaTarget(organizerName);
                    }
                    else
                    {
                        GameActions.Print(_world, "Set the organizer name (or index) in the macro to target a source container for it.", Constants.HUE_ERROR);
                    }
                    break;
            }

            return result;
        }

        /// <summary>
        /// Resolves the item currently under the mouse for the LootHoveredItem macro.
        /// Prefers the UI control directly under the cursor (grid containers, regular
        /// containers, paperdoll and modern paperdoll all expose their item via
        /// LocalSerial), stopping at the gump boundary so empty space inside a container
        /// does not resolve to the container itself. Falls back to the world object under
        /// the cursor for items on the ground and item nameplates.
        /// </summary>
        private Item GetHoveredItem()
        {
            for (IGui control = UIManager.MouseOverControl; control != null && control is not Gump; control = control.Parent)
            {
                if (!SerialHelper.IsItem(control.LocalSerial))
                    continue;

                Item item = _world.Items.Get(control.LocalSerial);

                if (item != null)
                    return item;
            }

            BaseGameObject obj = SelectedObject.Object;

            return obj as Item ?? (obj as TextObject)?.Owner as Item;
        }

        private void SetLastTarget(uint serial)
        {
            if (SerialHelper.IsValid(serial))
            {
                Entity ent = _world.Get(serial);

                if (SerialHelper.IsMobile(serial))
                {
                    if (ent != null)
                    {
                        if (!ProfileManager.CurrentProfile.HideMacroTargetMessage)
                            GameActions.MessageOverhead(_world, string.Format(TazLang.Get("target0"), ent.Name), Notoriety.GetHue(((Mobile)ent).NotorietyFlag), _world.Player);

                        _world.TargetManager.NewTargetSystemSerial = serial;
                        _world.TargetManager.SelectedTarget = serial;
                        _world.TargetManager.LastTargetInfo.SetEntity(serial);

                        return;
                    }
                }
                else
                {
                    if (ent != null)
                    {
                        if (!ProfileManager.CurrentProfile.HideMacroTargetMessage)
                            GameActions.MessageOverhead(_world, string.Format(TazLang.Get("target0"), ent.Name), 992, _world.Player);
                        _world.TargetManager.SelectedTarget = serial;
                        _world.TargetManager.LastTargetInfo.SetEntity(serial);

                        return;
                    }
                }
            }

            GameActions.Print(_world, TazLang.Get("entity_not_found"));
        }

        private int ProcessLoopContainer(MacroLoopContainer container)
        {
            // First time touching this container
            if (container.CurrentIteration == -1)
            {
                container.CurrentIteration = 0;
                container.CurrentItemIndex = 0;
            }

            // All iterations done -> container finished, advance to next outer item
            if (container.CurrentIteration >= container.LoopCount)
            {
                container.Reset();
                return 0;
            }

            // Reached end of items for this iteration
            if (container.CurrentItemIndex < 0 || container.CurrentItemIndex >= container.Items.Count)
            {
                container.CurrentIteration++;

                if (container.CurrentIteration < container.LoopCount)
                {
                    container.CurrentItemIndex = 0;

                    if (container.DelayBetweenIterations > 0)
                    {
                        _nextTimer = Time.Ticks + container.DelayBetweenIterations;
                    }

                    // Stay on this container; don't let Update() advance _lastMacro
                    return 1;
                }
                else
                {
                    container.Reset();
                    return 0; // whole loop finished, move to next outer item
                }
            }

            // Execute current item
            MacroObject currentItem = container.Items.ElementAt(container.CurrentItemIndex);
            int result = Process(currentItem);

            if (result == 0)
            {
                container.CurrentItemIndex++;

                // More items remain (or more iterations) -> keep Update() on this container
                // so it processes the next item on the next call, rather than advancing past it.
                return 1;
            }

            // result == 1 (waiting/break) or 2 (stop) -> propagate as-is
            return result;
        }

    }


    public class Macro : LinkedObject, IEquatable<Macro>
    {
        public Macro(string name, SDL_Keycode key, bool alt, bool ctrl, bool shift) : this(name)
        {
            Key = key;
            Alt = alt;
            Ctrl = ctrl;
            Shift = shift;
        }

        public Macro(string name, MouseButtonType button, bool alt, bool ctrl, bool shift) : this(name)
        {
            MouseButton = button;
            Alt = alt;
            Ctrl = ctrl;
            Shift = shift;
        }

        public Macro(string name, bool wheelUp, bool alt, bool ctrl, bool shift) : this(name)
        {
            WheelScroll = true;
            WheelUp = wheelUp;
            Alt = alt;
            Ctrl = ctrl;
            Shift = shift;
        }

        public Macro(string name)
        {
            Name = name;
        }

        public string Name { get; set; }

        public SDL_GamepadButton[] ControllerButtons { get; set; }
        public SDL_Keycode Key { get; set; }
        public MouseButtonType MouseButton { get; set; }
        public bool WheelScroll { get; set; }
        public bool WheelUp { get; set; }
        public bool Alt { get; set; }
        public bool Ctrl { get; set; }
        public bool Shift { get; set; }

        private string _journalTriggers = string.Empty;

        /// <summary>
        /// Semicolon (;) separated list of journal messages that will trigger this macro when received.
        /// A macro is triggered when a journal entry contains any one of these substrings (case-insensitive).
        /// </summary>
        public string JournalTriggers
        {
            get => _journalTriggers;
            set
            {
                _journalTriggers = value ?? string.Empty;
                HasJournalTriggers = !string.IsNullOrWhiteSpace(_journalTriggers);
            }
        }

        /// <summary>
        /// Cached flag indicating this macro has at least one journal trigger configured.
        /// Used as a cheap pre-check so journal processing can skip macros without triggers.
        /// </summary>
        public bool HasJournalTriggers { get; private set; }

        /// <summary>
        /// Returns true when the given journal text matches one of this macro's configured journal triggers.
        /// </summary>
        public bool MatchesJournalTrigger(string text)
        {
            if (!HasJournalTriggers || string.IsNullOrEmpty(text))
            {
                return false;
            }

            string[] triggers = _journalTriggers.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (string trigger in triggers)
            {
                if (!string.IsNullOrEmpty(trigger) && text.Contains(trigger, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public HotkeyBinding GetBinding() => new()
        {
            Key = Key,
            Ctrl = Ctrl,
            Shift = Shift,
            Alt = Alt,
            MouseButton = MouseButton,
            WheelScroll = WheelScroll,
            WheelUp = WheelUp,
            ControllerButtons = ControllerButtons
        };

        public void ApplyBinding(HotkeyBinding binding)
        {
            Key = binding.Key;
            Ctrl = binding.Ctrl;
            Shift = binding.Shift;
            Alt = binding.Alt;
            MouseButton = binding.MouseButton;
            WheelScroll = binding.WheelScroll;
            WheelUp = binding.WheelUp;
            ControllerButtons = binding.ControllerButtons;
        }

        public bool HideLabel = false;
        public ushort Hue = 0x00;
        public ushort? Graphic = null;
        private byte _scale = 100;
        public byte Scale
        {
            get { return _scale; }
            set
            {
                if (value <= 10) _scale = 10;
                else _scale = value;
            }
        }

        public bool Equals(Macro other)
        {
            if (other == null)
            {
                return false;
            }

            return Key == other.Key && Alt == other.Alt && Ctrl == other.Ctrl && Shift == other.Shift && Name == other.Name;
        }

        //public Macro Left { get; set; }
        //public Macro Right { get; set; }

        //private void AppendMacro(MacroObject item)
        //{
        //    if (FirstNode == null)
        //    {
        //        FirstNode = item;
        //    }
        //    else
        //    {
        //        MacroObject o = FirstNode;

        //        while (o.Right != null)
        //        {
        //            o = o.Right;
        //        }

        //        o.Right = item;
        //        item.Left = o;
        //    }
        //}


        public void Save(XmlTextWriter writer)
        {
            writer.WriteStartElement("macro");
            writer.WriteAttributeString("name", Name);
            writer.WriteAttributeString("key", ((int)Key).ToString());
            writer.WriteAttributeString("mousebutton", ((int)MouseButton).ToString());
            writer.WriteAttributeString("wheelscroll", WheelScroll.ToString());
            writer.WriteAttributeString("wheelup", WheelUp.ToString());
            writer.WriteAttributeString("alt", Alt.ToString());
            writer.WriteAttributeString("ctrl", Ctrl.ToString());
            writer.WriteAttributeString("shift", Shift.ToString());
            writer.WriteAttributeString("hidelabel", HideLabel.ToString());
            writer.WriteAttributeString("hue", Hue.ToString());
            writer.WriteAttributeString("graphic", Graphic.HasValue ? Graphic.ToString() : string.Empty);
            writer.WriteAttributeString("scale", Scale.ToString());
            writer.WriteAttributeString("journaltriggers", JournalTriggers ?? string.Empty);

            writer.WriteStartElement("actions");

            for (var action = (MacroObject)Items; action != null; action = (MacroObject)action.Next)
            {
                if (action is MacroLoopContainer loopContainer)
                {
                    loopContainer.Save(writer);
                }
                else
                {
                    writer.WriteStartElement("action");
                    writer.WriteAttributeString("code", ((int)action.Code).ToString());
                    writer.WriteAttributeString("subcode", ((int)action.SubCode).ToString());
                    writer.WriteAttributeString("submenutype", action.SubMenuType.ToString());

                    if (action.HasString())
                    {
                        writer.WriteAttributeString("text", ((MacroObjectString)action).Text);
                    }

                    writer.WriteEndElement();
                }
            }

            writer.WriteEndElement();

            if (ControllerButtons != null)
            {
                writer.WriteStartElement("controllerbuttons");
                foreach (SDL_GamepadButton b in ControllerButtons)
                {
                    writer.WriteElementString("button", ((int)b).ToString());
                }
                writer.WriteEndElement();
            }



            writer.WriteEndElement();
        }

        public void Load(XmlElement xml)
        {
            if (xml == null)
            {
                return;
            }

            if (!Enum.TryParse(xml.GetAttribute("key"), out SDL_Keycode mainKey))
                mainKey = (int)SDL_Keycode.SDLK_UNKNOWN;
            


            Key = mainKey;
            Alt = bool.Parse(xml.GetAttribute("alt"));
            Ctrl = bool.Parse(xml.GetAttribute("ctrl"));
            Shift = bool.Parse(xml.GetAttribute("shift"));
            bool.TryParse(xml.GetAttribute("hidelabel"), out HideLabel);
            ushort.TryParse(xml.GetAttribute("hue"), out Hue);
            if (byte.TryParse(xml.GetAttribute("scale"), out byte savedScale))
            {
                Scale = savedScale;
            }
            if (ushort.TryParse(xml.GetAttribute("graphic"), out ushort graphic))
            {
                Graphic = graphic;
            }

            if (xml.HasAttribute("journaltriggers"))
            {
                JournalTriggers = xml.GetAttribute("journaltriggers");
            }

            if (xml.HasAttribute("mousebutton"))
            {
                MouseButton = (MouseButtonType)int.Parse(xml.GetAttribute("mousebutton"));
            }

            if (xml.HasAttribute("wheelscroll"))
            {
                WheelScroll = bool.Parse(xml.GetAttribute("wheelscroll"));
            }

            if (xml.HasAttribute("wheelup"))
            {
                WheelUp = bool.Parse(xml.GetAttribute("wheelup"));
            }

            XmlElement actions = xml["actions"];

            if (actions != null)
            {
                foreach (XmlNode node in actions.ChildNodes)
                {
                    if (node is XmlElement xmlElement)
                    {
                        if (xmlElement.Name == "loop")
                        {
                            var container = MacroLoopContainer.Load(xmlElement);
                            PushToBack(container);
                        }
                        else if (xmlElement.Name == "action")
                        {
                            var code = (MacroType)int.Parse(xmlElement.GetAttribute("code"));
                            var sub = (MacroSubType)int.Parse(xmlElement.GetAttribute("subcode"));

                            // ########### PATCH ###########
                            // FIXME: path to remove the MovePlayer macro. This macro is not needed. We have Walk.
                            if ((int)code == 61 /*MacroType.MovePlayer*/)
                            {
                                code = MacroType.Walk;

                                switch ((int)sub)
                                {
                                    case 211: // top
                                        sub = MacroSubType.NW;
                                        break;

                                    case 214: // left
                                        sub = MacroSubType.SW;
                                        break;

                                    case 213: // down
                                        sub = MacroSubType.SE;
                                        break;

                                    case 212: // right
                                        sub = MacroSubType.NE;
                                        break;
                                }
                            }
                            // ########### END PATCH ###########

                            sbyte subMenuType = sbyte.Parse(xmlElement.GetAttribute("submenutype"));

                            MacroObject m;

                            if (xmlElement.HasAttribute("text"))
                            {
                                m = new MacroObjectString(code, sub, xmlElement.GetAttribute("text"));
                            }
                            else
                            {
                                m = new MacroObject(code, sub);
                            }

                            m.SubMenuType = subMenuType;

                            PushToBack(m);
                        }
                    }
                }
            }

            XmlElement buttons = xml["controllerbuttons"];

            if (buttons != null)
            {
                List<SDL_GamepadButton> savedButtons = new();
                foreach (XmlElement buttonNum in buttons.GetElementsByTagName("button"))
                {
                    if (int.TryParse(buttonNum.InnerText, out int b))
                    {
                        if (Enum.IsDefined(typeof(SDL_GamepadButton), b))
                        {
                            savedButtons.Add((SDL_GamepadButton)b);
                        }
                    }
                }
                ControllerButtons = savedButtons.ToArray();
            }
        }


        public static MacroObject Create(MacroType code)
        {
            MacroObject obj;

            switch (code)
            {
                case MacroType.Say:
                case MacroType.Emote:
                case MacroType.Whisper:
                case MacroType.Yell:
                case MacroType.Delay:
                case MacroType.SetUpdateRange:
                case MacroType.ModifyUpdateRange:
                case MacroType.RazorMacro:
                case MacroType.UseCounterBar:
                case MacroType.SetSpellBarRow:
                case MacroType.ClientCommand:
                case MacroType.UseType:
                case MacroType.SetOrganizerSource:
                case MacroType.SetZoomLevel:
                case MacroType.PrivateSay:
                    obj = new MacroObjectString(code, MacroSubType.MSC_NONE);

                    break;

                default:
                    obj = new MacroObject(code, MacroSubType.MSC_NONE);

                    break;
            }

            return obj;
        }

        public static MacroLoopContainer CreateLoopContainer(int loopCount = 1, int delayBetweenIterations = 0)
        {
            var container = new MacroLoopContainer
            {
                LoopCount = loopCount,
                DelayBetweenIterations = delayBetweenIterations
            };
            return container;
        }

        public static Macro CreateEmptyMacro(string name)
        {
            var macro = new Macro
            (
                name,
                (SDL_Keycode)0,
                false,
                false,
                false
            );

            var item = new MacroObject(MacroType.None, MacroSubType.MSC_NONE);

            macro.PushToBack(item);

            return macro;
        }

        public static Macro CreateFastMacro(string name, MacroType type, MacroSubType sub)
        {
            var macro = new Macro
              (
                  name,
                  (SDL_Keycode)0,
                  false,
                  false,
                  false
              );

            var item = new MacroObject(type, sub);

            macro.PushToBack(item);

            return macro;
        }

        public static void GetBoundByCode(MacroType code, ref int count, ref int offset)
        {
            switch (code)
            {
                case MacroType.Walk:
                    offset = (int)MacroSubType.NW;
                    count = MacroSubType.Configuration - MacroSubType.NW;

                    break;

                case MacroType.Open:
                case MacroType.Close:
                case MacroType.Minimize:
                case MacroType.Maximize:
                case MacroType.ToggleGump:
                    offset = (int)MacroSubType.Configuration;
                    count = MacroSubType.Anatomy - MacroSubType.Configuration;

                    break;

                case MacroType.UseSkill:
                    offset = (int)MacroSubType.Anatomy;
                    count = MacroSubType.LeftHand - MacroSubType.Anatomy;

                    break;

                case MacroType.ArmDisarm:
                    offset = (int)MacroSubType.LeftHand;
                    count = MacroSubType.Honor - MacroSubType.LeftHand;

                    break;

                case MacroType.InvokeVirtue:
                    offset = (int)MacroSubType.Honor;
                    count = MacroSubType.Clumsy - MacroSubType.Honor;

                    break;

                case MacroType.CastSpell:
                    offset = (int)MacroSubType.Clumsy;
                    int countInitial = MacroSubType.Hostile - MacroSubType.Clumsy;
                    //var countFinal = MacroSubType.DeathRay - MacroSubType.Boarding;
                    count = countInitial;// + 33 + 43;
                    break;

                case MacroType.SelectNext:
                case MacroType.SelectPrevious:
                case MacroType.SelectNearest:
                    offset = (int)MacroSubType.Hostile;
                    count = MacroSubType.MscTotalCount - MacroSubType.Hostile;

                    break;

                case MacroType.UsePotion:
                    offset = (int)MacroSubType.ConfusionBlastPotion;
                    count = MacroSubType.DefaultZoom - MacroSubType.ConfusionBlastPotion;

                    break;

                case MacroType.Zoom:
                    offset = (int)MacroSubType.DefaultZoom;
                    count = 1 + MacroSubType.ZoomOut - MacroSubType.DefaultZoom;

                    break;

                case MacroType.UseObject:
                    offset = (int)MacroSubType.BestHealPotion;
                    count = 1 + MacroSubType.SpellStone - MacroSubType.BestHealPotion;

                    break;

                case MacroType.LookAtMouse:
                    offset = (int)MacroSubType.LookForwards;
                    count = 1 + MacroSubType.LookBackwards - MacroSubType.LookForwards;

                    break;
                case MacroType.CastMasterySpell:
                    offset = (int)MacroSubType.Inspire;
                    count = 1 + (int)MacroSubType.Boarding - (int)MacroSubType.Inspire;
                    break;
            }
        }
    }


    public class MacroObject : LinkedObject
    {
        public MacroObject(MacroType code, MacroSubType sub)
        {
            Code = code;
            SubCode = sub;

            switch (code)
            {
                case MacroType.Walk:
                case MacroType.Open:
                case MacroType.Close:
                case MacroType.Minimize:
                case MacroType.Maximize:
                case MacroType.ToggleGump:
                case MacroType.UseSkill:
                case MacroType.ArmDisarm:
                case MacroType.InvokeVirtue:
                case MacroType.CastSpell:
                case MacroType.SelectNext:
                case MacroType.SelectPrevious:
                case MacroType.SelectNearest:
                case MacroType.UsePotion:
                case MacroType.Zoom:
                case MacroType.UseObject:
                case MacroType.LookAtMouse:
                case MacroType.CastMasterySpell:

                    if (sub == MacroSubType.MSC_NONE)
                    {
                        int count = 0;
                        int offset = 0;
                        Macro.GetBoundByCode(code, ref count, ref offset);
                        SubCode = (MacroSubType)offset;
                    }

                    SubMenuType = 1;

                    break;

                case MacroType.Say:
                case MacroType.Emote:
                case MacroType.Whisper:
                case MacroType.Yell:
                case MacroType.Delay:
                case MacroType.SetUpdateRange:
                case MacroType.ModifyUpdateRange:
                case MacroType.RazorMacro:
                case MacroType.UseCounterBar:
                case MacroType.SetSpellBarRow:
                case MacroType.ClientCommand:
                case MacroType.UseType:
                case MacroType.SetOrganizerSource:
                case MacroType.SetZoomLevel:
                case MacroType.PrivateSay:
                    SubMenuType = 2;

                    break;

                default:
                    SubMenuType = 0;

                    break;
            }
        }

        public MacroType Code { get; set; }
        public MacroSubType SubCode { get; set; }
        public sbyte SubMenuType { get; set; }

        public virtual bool HasString() => false;
    }

    public class MacroObjectString : MacroObject
    {
        public MacroObjectString(MacroType code, MacroSubType sub, string str = "") : base(code, sub)
        {
            Text = str;
        }

        public string Text { get; set; }

        public override bool HasString() => true;
    }

    public class MacroLoopContainer : MacroObject
    {
        public int LoopCount { get; set; } = 1;
        public int DelayBetweenIterations { get; set; } = 0;
        public int CurrentIteration { get; set; } = -1;
        public int CurrentItemIndex { get; set; } = -1;
        public LinkedList<MacroObject> Items { get; } = new();

        public MacroLoopContainer() : base((MacroType)MacroType.LoopContainer, MacroSubType.MSC_NONE)
        {
            Code = (MacroType)MacroType.LoopContainer;
            SubMenuType = 0;
        }

        public void Save(XmlTextWriter writer)
        {
            writer.WriteStartElement("loop");
            writer.WriteAttributeString("loopcount", LoopCount.ToString());
            writer.WriteAttributeString("delaybetween", DelayBetweenIterations.ToString());

            foreach (MacroObject action in Items)
            {
                writer.WriteStartElement("action");
                writer.WriteAttributeString("code", ((int)action.Code).ToString());
                writer.WriteAttributeString("subcode", ((int)action.SubCode).ToString());
                writer.WriteAttributeString("submenutype", action.SubMenuType.ToString());

                if (action.HasString())
                    writer.WriteAttributeString("text", ((MacroObjectString)action).Text);

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        public static MacroLoopContainer Load(XmlElement xmlLoop)
        {
            var container = new MacroLoopContainer();

            if (int.TryParse(xmlLoop.GetAttribute("loopcount"), out int loopCount))
                container.LoopCount = loopCount;

            if (int.TryParse(xmlLoop.GetAttribute("delaybetween"), out int delay))
                container.DelayBetweenIterations = delay;

            foreach (XmlElement xmlAction in xmlLoop.GetElementsByTagName("action"))
            {
                var code = (MacroType)int.Parse(xmlAction.GetAttribute("code"));
                var sub = (MacroSubType)int.Parse(xmlAction.GetAttribute("subcode"));
                sbyte subMenuType = sbyte.Parse(xmlAction.GetAttribute("submenutype"));

                MacroObject m;
                if (xmlAction.HasAttribute("text"))
                    m = new MacroObjectString(code, sub, xmlAction.GetAttribute("text"));
                else
                    m = new MacroObject(code, sub);

                m.SubMenuType = subMenuType;
                container.Items.AddLast(m);
            }

            return container;
        }

        public void Reset()
        {
            CurrentIteration = -1;
            CurrentItemIndex = -1;
        }
    }
}
