// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Managers.Hotkeys;
using ClassicUO.Input;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;
using SDL3;

namespace ClassicUO.Game.UI.Gumps
{
    /// <summary>
    /// Shared XML (de)serialization for the counter bar and action bar cells, so the saved gump format
    /// lives in one place and cannot drift between the two bars.
    /// </summary>
    internal static class BarXml
    {
        /// <summary>Writes a cell's hotkey binding as attributes, writing nothing when the binding is empty.</summary>
        public static void WriteHotkey(XmlTextWriter writer, HotkeyBinding binding)
        {
            if (binding == null || binding.IsEmpty)
                return;

            writer.WriteAttributeString("hkkey", ((int)binding.Key).ToString());
            writer.WriteAttributeString("hkctrl", binding.Ctrl.ToString());
            writer.WriteAttributeString("hkshift", binding.Shift.ToString());
            writer.WriteAttributeString("hkalt", binding.Alt.ToString());
            writer.WriteAttributeString("hkmouse", ((int)binding.MouseButton).ToString());
            writer.WriteAttributeString("hkwheel", binding.WheelScroll.ToString());
            writer.WriteAttributeString("hkwheelup", binding.WheelUp.ToString());
            if (binding.ControllerButtons is { Length: > 0 } buttons)
                writer.WriteAttributeString("hkcontroller", string.Join(",", buttons.Select(b => ((int)b).ToString())));
        }

        /// <summary>Writes a cell's border color as a packed RGBA value, writing nothing when it is the default.</summary>
        public static void WriteCellColor(XmlTextWriter writer, Color color)
        {
            if (color == BarCell.DefaultCellColor)
                return;

            writer.WriteAttributeString("cellcolor", color.PackedValue.ToString());
        }

        /// <summary>Rebuilds a cell's border color from its saved attribute, defaulting to gray when absent or invalid.</summary>
        public static Color ReadCellColor(XmlElement cellXml)
        {
            return cellXml.HasAttribute("cellcolor") && uint.TryParse(cellXml.GetAttribute("cellcolor"), out uint packed)
                ? new Color { PackedValue = packed }
                : BarCell.DefaultCellColor;
        }

        /// <summary>Rebuilds a cell's <see cref="HotkeyBinding"/> from its saved attributes; tolerant of missing/partial data.</summary>
        public static HotkeyBinding ReadHotkey(XmlElement cellXml)
        {
            var binding = new HotkeyBinding
            {
                Key = (SDL.SDL_Keycode)ParseIntAttr(cellXml, "hkkey", (int)SDL.SDL_Keycode.SDLK_UNKNOWN),
                Ctrl = ParseBoolAttr(cellXml, "hkctrl"),
                Shift = ParseBoolAttr(cellXml, "hkshift"),
                Alt = ParseBoolAttr(cellXml, "hkalt"),
                MouseButton = (MouseButtonType)ParseIntAttr(cellXml, "hkmouse", (int)MouseButtonType.None),
                WheelScroll = ParseBoolAttr(cellXml, "hkwheel"),
                WheelUp = ParseBoolAttr(cellXml, "hkwheelup")
            };

            string controllers = cellXml.GetAttribute("hkcontroller");
            if (!string.IsNullOrEmpty(controllers))
            {
                var buttons = new List<SDL.SDL_GamepadButton>();
                foreach (string part in controllers.Split(','))
                    if (int.TryParse(part, out int b))
                        buttons.Add((SDL.SDL_GamepadButton)b);

                if (buttons.Count > 0)
                    binding.ControllerButtons = buttons.ToArray();
            }

            return binding;
        }

        /// <summary>Rebuilds a <see cref="CounterBarSlot"/> from a saved cell, migrating the legacy standalone "spellid" attribute.</summary>
        public static CounterBarSlot ReadSlot(XmlElement cellXml)
        {
            // A partially-written profile (e.g. a crash mid-save) can leave malformed or missing
            // per-type attributes; degrade gracefully to an empty slot instead of aborting the whole restore.
            try
            {
                if (cellXml.HasAttribute("slottype"))
                {
                    var type = (CounterBarSlotType)int.Parse(cellXml.GetAttribute("slottype"));

                    switch (type)
                    {
                        case CounterBarSlotType.Spell:
                            return CounterBarSlot.FromSpell(SpellDefinition.FullIndexGetSpell(int.Parse(cellXml.GetAttribute("spellid"))));
                        case CounterBarSlotType.Macro:
                            return new CounterBarSlot { Type = CounterBarSlotType.Macro, MacroName = cellXml.GetAttribute("macroname") };
                        case CounterBarSlotType.Script:
                            return new CounterBarSlot { Type = CounterBarSlotType.Script, ScriptId = cellXml.GetAttribute("scriptid") };
                        case CounterBarSlotType.Skill:
                            return CounterBarSlot.FromSkill(int.Parse(cellXml.GetAttribute("skillindex")));
                        case CounterBarSlotType.Ability:
                            return CounterBarSlot.FromAbility(bool.Parse(cellXml.GetAttribute("abilityprimary")));
                        case CounterBarSlotType.DressAgent:
                            return new CounterBarSlot
                            {
                                Type = CounterBarSlotType.DressAgent,
                                DressConfigName = cellXml.GetAttribute("dressconfigname"),
                                DressAgentUndress = bool.Parse(cellXml.GetAttribute("dressagentundress"))
                            };
                    }
                }

                // Legacy: pre-parity saves stored a spell as a standalone "spellid" attribute alongside the gump graphic.
                if (cellXml.HasAttribute("spellid"))
                    return CounterBarSlot.FromSpell(SpellDefinition.FullIndexGetSpell(int.Parse(cellXml.GetAttribute("spellid"))));
            }
            catch (FormatException e)
            {
                Log.Error($"Malformed bar slot data during restore; defaulting to empty. {e}");
            }

            return CounterBarSlot.Empty();
        }

        private static int ParseIntAttr(XmlElement xml, string attr, int fallback)
            => xml.HasAttribute(attr) && int.TryParse(xml.GetAttribute(attr), out int v) ? v : fallback;

        private static bool ParseBoolAttr(XmlElement xml, string attr)
            => xml.HasAttribute(attr) && bool.TryParse(xml.GetAttribute(attr), out bool v) && v;
    }
}
