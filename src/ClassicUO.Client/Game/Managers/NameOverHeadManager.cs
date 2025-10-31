// SPDX-License-Identifier: BSD-2-Clause


using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.Utility.Logging;
using SDL3;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace ClassicUO.Game.Managers
{
    [Flags]
    public enum NameOverheadOptions
    {
        None = 0,

        // Items
        Containers = 1 << 0,
        Gold = 1 << 1,
        Stackable = 1 << 2,
        LockedDown = 1 << 3,
        Other = 1 << 4,

        // Corpses
        MonsterCorpses = 1 << 5,
        HumanoidCorpses = 1 << 6,
        OwnCorpses = 1 << 7,

        // Mobiles (type)
        Humanoid = 1 << 8,
        Monster = 1 << 9,
        OwnFollowers = 1 << 10,
        Self = 1 << 11,
        ExcludeSelf = 1 << 12,

        // Mobiles (notoriety)
        Innocent = 1 << 13,
        Ally = 1 << 14,
        Gray = 1 << 15,
        Criminal = 1 << 16,
        Enemy = 1 << 17,
        Murderer = 1 << 18,
        Invulnerable = 1 << 19,

        // Items cont.
        Moveable = 1 << 20,
        Immoveable = 1 << 21,

        AllItems = Containers | Gold | Stackable | LockedDown | Moveable | Immoveable | Other,
        AllMobiles = Humanoid | Monster | OwnFollowers | Self,
        MobilesAndCorpses = AllMobiles | MonsterCorpses | HumanoidCorpses,
    }

    public sealed class NameOverHeadManager
    {
        private NameOverHeadHandlerGump _gump;
        private static SDL.SDL_Keycode _lastKeySym = SDL.SDL_Keycode.SDLK_UNKNOWN;
        private static SDL.SDL_Keymod _lastKeyMod = SDL.SDL_Keymod.SDL_KMOD_NONE;
        private readonly World _world;

        public NameOverHeadManager(World world) { _world = world; }

        public static string LastActiveNameOverheadOption
        {
            get => ProfileManager.CurrentProfile.LastActiveNameOverheadOption;
            set => ProfileManager.CurrentProfile.LastActiveNameOverheadOption = value;
        }

        public static NameOverheadOptions ActiveOverheadOptions { get; set; }

        public static bool IsPermaToggled
        {
            get => ProfileManager.CurrentProfile.NameOverheadToggled;
            private set => ProfileManager.CurrentProfile.NameOverheadToggled = value;
        }

        public static string Search { get; set; } = string.Empty;

        public static bool IsTemporarilyShowing { get; private set; }
        public static bool IsShowing => IsPermaToggled || IsTemporarilyShowing || Keyboard.Ctrl && Keyboard.Shift;

        private static List<NameOverheadOption> Options { get; set; } = new List<NameOverheadOption>();

        public bool IsAllowed(Entity serial)
        {
            if (serial == null)
                return false;

            if (SerialHelper.IsItem(serial))
                return HandleItemOverhead(serial);

            if (SerialHelper.IsMobile(serial))
                return HandleMobileOverhead(serial);

            return false;
        }

        private bool HandleMobileOverhead(Entity serial)
        {
            var mobile = serial as Mobile;

            if (mobile == null)
                return false;

            if (mobile.Equals(_world.Player) && ActiveOverheadOptions.HasFlag(NameOverheadOptions.ExcludeSelf))
                return false;

            // Mobile types
            if (ActiveOverheadOptions.HasFlag(NameOverheadOptions.Humanoid) && mobile.IsHuman)
                return true;

            if (ActiveOverheadOptions.HasFlag(NameOverheadOptions.Monster) && !mobile.IsHuman)
                return true;

            if (ActiveOverheadOptions.HasFlag(NameOverheadOptions.OwnFollowers) && mobile.IsRenamable && mobile.NotorietyFlag != NotorietyFlag.Invulnerable && mobile.NotorietyFlag != NotorietyFlag.Enemy)
                return true;

            // Mobile notorieties
            if (ActiveOverheadOptions.HasFlag(NameOverheadOptions.Innocent) && mobile.NotorietyFlag == NotorietyFlag.Innocent)
                return true;

            if (ActiveOverheadOptions.HasFlag(NameOverheadOptions.Ally) && mobile.NotorietyFlag == NotorietyFlag.Ally)
                return true;

            if (ActiveOverheadOptions.HasFlag(NameOverheadOptions.Gray) && mobile.NotorietyFlag == NotorietyFlag.Gray)
                return true;

            if (ActiveOverheadOptions.HasFlag(NameOverheadOptions.Criminal) && mobile.NotorietyFlag == NotorietyFlag.Criminal)
                return true;

            if (ActiveOverheadOptions.HasFlag(NameOverheadOptions.Enemy) && mobile.NotorietyFlag == NotorietyFlag.Enemy)
                return true;

            if (ActiveOverheadOptions.HasFlag(NameOverheadOptions.Murderer) && mobile.NotorietyFlag == NotorietyFlag.Murderer)
                return true;

            if (ActiveOverheadOptions.HasFlag(NameOverheadOptions.Invulnerable) && mobile.NotorietyFlag == NotorietyFlag.Invulnerable)
                return true;

            if (ActiveOverheadOptions.HasFlag(NameOverheadOptions.Self) && mobile.Equals(_world.Player))
                return true;

            return false;
        }

        private static bool HandleItemOverhead(Entity serial)
        {
            var item = serial as Item;

            if (item == null)
                return false;

            if (item.IsCorpse)
            {
                return HandleCorpseOverhead(item);
            }

            if (item.ItemData.IsContainer && ActiveOverheadOptions.HasFlag(NameOverheadOptions.Containers))
                return true;

            if (item.IsCoin && ActiveOverheadOptions.HasFlag(NameOverheadOptions.Gold))
                return true;

            if (item.ItemData.IsStackable && ActiveOverheadOptions.HasFlag(NameOverheadOptions.Stackable))
                return true;

            if (item.IsLocked && ActiveOverheadOptions.HasFlag(NameOverheadOptions.LockedDown))
                return true;

            if (item.IsMovable && ActiveOverheadOptions.HasFlag(NameOverheadOptions.Moveable))
                return true;

            if (!item.IsMovable && ActiveOverheadOptions.HasFlag(NameOverheadOptions.Immoveable))
                return true;

            if (ActiveOverheadOptions.HasFlag(NameOverheadOptions.Other))
                return true;

            return false;
        }

        private static bool HandleCorpseOverhead(Item item)
        {
            bool isHumanCorpse = item.IsHumanCorpse;

            if (isHumanCorpse && ActiveOverheadOptions.HasFlag(NameOverheadOptions.HumanoidCorpses))
                return true;

            if (!isHumanCorpse && ActiveOverheadOptions.HasFlag(NameOverheadOptions.MonsterCorpses))
                return true;

            // TODO: Add support for IsOwnCorpse, which was coded by Dyru
            return false;
        }

        public void Open()
        {
            if (_gump == null || _gump.IsDisposed)
            {
                _gump = new NameOverHeadHandlerGump(_world);
                UIManager.Add(_gump);
            }

            _gump.IsEnabled = true;
            _gump.IsVisible = true;
        }

        public void Close()
        {
            if (_gump == null)
            { //Required in case nameplates are active when closing and reopening the client
                _gump = new NameOverHeadHandlerGump(_world);
                UIManager.Add(_gump);
            }


            _gump.IsEnabled = false;
            _gump.IsVisible = false;
        }

        public void ToggleOverheads() => SetOverheadToggled(!IsPermaToggled);

        public void SetOverheadToggled(bool toggled)
        {
            if (IsPermaToggled == toggled)
                return;

            IsPermaToggled = toggled;
            _gump?.UpdateCheckboxes();
        }

        public static void Load()
        {
            string path = Path.Combine(ProfileManager.ProfilePath, "nameoverhead.xml");

            if (!File.Exists(path))
            {
                Log.Trace("No nameoverhead.xml file. Creating a default file.");


                Options.Clear();
                CreateDefaultEntries();
                Save();

                return;
            }

            Options.Clear();
            XmlDocument doc = new();

            try
            {
                doc.Load(path);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());

                return;
            }


            XmlElement root = doc["nameoverhead"];

            if (root != null)
            {
                foreach (XmlElement xml in root.GetElementsByTagName("nameoverheadoption"))
                {
                    var option = new NameOverheadOption(xml.GetAttribute("name"));
                    option.Load(xml);
                    Options.Add(option);
                }
            }

            // Ensure at least one option exists after loading
            if (Options.Count == 0)
            {
                Log.Trace("No nameoverhead options loaded. Creating default entries.");
                CreateDefaultEntries();
                Save();
            }
        }

        public static void Save()
        {
            List<NameOverheadOption> list = Options;

            string path = Path.Combine(ProfileManager.ProfilePath, "nameoverhead.xml");
            string tempPath = path + ".tmp";

            try
            {
                using (XmlTextWriter xml = new(tempPath, Encoding.UTF8)
                {
                    Formatting = Formatting.Indented,
                    IndentChar = '\t',
                    Indentation = 1
                })
                {
                    xml.WriteStartDocument(true);
                    xml.WriteStartElement("nameoverhead");

                    foreach (NameOverheadOption option in list)
                    {
                        option.Save(xml);
                    }

                    xml.WriteEndElement();
                    xml.WriteEndDocument();
                }

                // Atomic move: replace the original file with the temp file
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                File.Move(tempPath, path);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to save nameoverhead.xml: {ex}");

                // Clean up temp file if it exists
                if (File.Exists(tempPath))
                {
                    try
                    {
                        File.Delete(tempPath);
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                }
                throw;
            }
        }

        private static void CreateDefaultEntries() => Options.AddRange
            (
                new[]
                {
                    new NameOverheadOption("All", int.MaxValue),
                    new NameOverheadOption("Mobiles only", (int)NameOverheadOptions.AllMobiles),
                    new NameOverheadOption("Items only", (int)NameOverheadOptions.AllItems),
                    new NameOverheadOption("Mobiles & Corpses only", (int)NameOverheadOptions.MobilesAndCorpses),
                }
            );

        public static NameOverheadOption FindOption(string name) => Options.Find(o => o.Name == name);

        public void AddOption(NameOverheadOption option)
        {
            Options.Add(option);
            _gump?.RedrawOverheadOptions();
        }

        public void RemoveOption(NameOverheadOption option)
        {
            Options.Remove(option);
            _gump?.RedrawOverheadOptions();
        }

        public static NameOverheadOption FindOptionByHotkey(SDL.SDL_Keycode key, bool alt, bool ctrl, bool shift) => Options.FirstOrDefault(o => o.Key == key && o.Alt == alt && o.Ctrl == ctrl && o.Shift == shift);

        public static List<NameOverheadOption> GetAllOptions() => Options;

        public void RegisterKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
        {
            if (_lastKeySym == key && _lastKeyMod == mod)
                return;

            _lastKeySym = key;
            _lastKeyMod = mod;

            bool shift = (mod & SDL.SDL_Keymod.SDL_KMOD_SHIFT) != SDL.SDL_Keymod.SDL_KMOD_NONE;
            bool alt = (mod & SDL.SDL_Keymod.SDL_KMOD_ALT) != SDL.SDL_Keymod.SDL_KMOD_NONE;
            bool ctrl = (mod & SDL.SDL_Keymod.SDL_KMOD_CTRL) != SDL.SDL_Keymod.SDL_KMOD_NONE;

            NameOverheadOption option = FindOptionByHotkey(key, alt, ctrl, shift);

            if (option == null)
                return;

            SetActiveOption(option);

            IsTemporarilyShowing = true;
        }

        public static void RegisterKeyUp(SDL.SDL_Keycode key)
        {
            if (key != _lastKeySym)
                return;

            _lastKeySym = SDL.SDL_Keycode.SDLK_UNKNOWN;

            IsTemporarilyShowing = false;
        }

        public void SetActiveOption(NameOverheadOption option)
        {
            if (option == null)
            {
                ActiveOverheadOptions = NameOverheadOptions.None;
                LastActiveNameOverheadOption = string.Empty;
            }
            else
            {
                ActiveOverheadOptions = (NameOverheadOptions)option.NameOverheadOptionFlags;
                LastActiveNameOverheadOption = option.Name;
                _gump?.UpdateCheckboxes();
            }
        }
    }

    public class NameOverheadOption
    {
        public NameOverheadOption(string name, SDL.SDL_Keycode key, bool alt, bool ctrl, bool shift, int optionflagscode) : this(name)
        {
            Key = key;
            Alt = alt;
            Ctrl = ctrl;
            Shift = shift;
            NameOverheadOptionFlags = optionflagscode;
        }

        public NameOverheadOption(string name)
        {
            Name = name;
        }

        public NameOverheadOption(string name, int optionflagcode)
        {
            Name = name;
            NameOverheadOptionFlags = optionflagcode;
        }

        public string Name { get; }

        public SDL.SDL_Keycode Key { get; set; }
        public bool Alt { get; set; }
        public bool Ctrl { get; set; }
        public bool Shift { get; set; }
        public int NameOverheadOptionFlags { get; set; }

        public bool Equals(NameOverheadOption other)
        {
            if (other == null)
            {
                return false;
            }

            return Key == other.Key && Alt == other.Alt && Ctrl == other.Ctrl && Shift == other.Shift && Name == other.Name;
        }

        public void Save(XmlTextWriter writer)
        {
            writer.WriteStartElement("nameoverheadoption");
            writer.WriteAttributeString("name", Name);
            writer.WriteAttributeString("key", ((int)Key).ToString());
            writer.WriteAttributeString("alt", Alt.ToString());
            writer.WriteAttributeString("ctrl", Ctrl.ToString());
            writer.WriteAttributeString("shift", Shift.ToString());
            writer.WriteAttributeString("optionflagscode", NameOverheadOptionFlags.ToString());

            writer.WriteEndElement();
        }

        public void Load(XmlElement xml)
        {
            if (xml == null)
            {
                return;
            }

            Key = (SDL.SDL_Keycode)int.Parse(xml.GetAttribute("key"));
            Alt = bool.Parse(xml.GetAttribute("alt"));
            Ctrl = bool.Parse(xml.GetAttribute("ctrl"));
            Shift = bool.Parse(xml.GetAttribute("shift"));
            NameOverheadOptionFlags = int.Parse(xml.GetAttribute("optionflagscode"));
        }
    }
}
