// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Assets;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Managers.Hotkeys;
using ClassicUO.LegionScripting;
using ClassicUO.Game.UI.Gumps.SpellBar;
using ClassicUO.Game.UI.MyraWindows;
using ClassicUO.Renderer;
using ClassicUO.Resources;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SDL3;

namespace ClassicUO.Game.UI.Gumps
{
    public class CounterBarGump : Gump
    {
        private AlphaBlendControl _background;

        public static CounterBarGump CurrentCounterBarGump { get; private set; }

        private int _rows,
            _columns,
            _rectSize;

        //private bool _isVertical;

        public CounterBarGump(World world) : base(world, 0, 0)
        {
            CurrentCounterBarGump = this;
        }

        public CounterBarGump(
            World world,
            int x,
            int y,
            int rectSize = 30,
            int rows = 1,
            int columns = 1 /*, bool vertical = false*/
        ) : base(world, 0, 0)
        {
            X = x;
            Y = y;

            if (rectSize < 30)
            {
                rectSize = 30;
            }
            else if (rectSize > 80)
            {
                rectSize = 80;
            }

            if (rows < 1)
            {
                rows = 1;
            }

            if (columns < 1)
            {
                columns = 1;
            }

            _rows = rows;
            _columns = columns;
            _rectSize = rectSize;
            //_isVertical = vertical;

            BuildGump();

            CurrentCounterBarGump = this;
            IsLocked = ProfileManager.CurrentProfile.CounterGumpLocked;
            CanCloseWithRightClick = false;
        }

        public override GumpType GumpType => GumpType.CounterBar;

        private void BuildGump()
        {
            CanMove = true;
            AcceptMouseInput = true;
            AcceptKeyboardInput = false;
            WantUpdateSize = false;
            Width = _rectSize * _columns + 1;
            Height = _rectSize * _rows + 1;

            Add(_background = new AlphaBlendControl(0.7f) { Width = Width, Height = Height });

            for (int row = 0; row < _rows; row++)
            {
                for (int col = 0; col < _columns; col++)
                {
                    Add(
                        new CounterItem(
                            this,
                            col * _rectSize + 2,
                            row * _rectSize + 2,
                            _rectSize - 4,
                            _rectSize - 4
                        )
                    );
                }
            }
        }

        public void SetLayout(int size, int rows, int columns)
        {
            bool ok = false;

            //if (_isVertical != isvertical)
            //{
            //    _isVertical = isvertical;
            //    int temp = _rows;
            //    _rows = _columns;
            //    _columns = temp;
            //    ok = true;
            //}

            if (rows > 30)
            {
                rows = 30;
            }

            if (columns > 30)
            {
                columns = 30;
            }

            if (size < 30)
            {
                size = 30;
            }
            else if (size > 80)
            {
                size = 80;
            }

            if (_rectSize != size)
            {
                ok = true;
                _rectSize = size;
            }

            if (rows < 1)
            {
                rows = 1;
            }

            if (_rows != rows)
            {
                ok = true;
                _rows = rows;
            }

            if (columns < 1)
            {
                columns = 1;
            }

            if (_columns != columns)
            {
                ok = true;
                _columns = columns;
            }

            if (ok)
            {
                ApplyLayout();
            }
        }

        private void ApplyLayout()
        {
            Width = _rectSize * _columns + 1;
            Height = _rectSize * _rows + 1;

            _background.Width = Width;
            _background.Height = Height;

            CounterItem[] items = GetControls<CounterItem>();

            int[] indices = new int[items.Length];

            for (int row = 0; row < _rows; row++)
            {
                for (int col = 0; col < _columns; col++)
                {
                    int index = /*_isVertical ? col * _rows + row :*/
                        row * _columns + col;

                    if (index < items.Length)
                    {
                        CounterItem c = items[index];

                        c.X = col * _rectSize + 2;
                        c.Y = row * _rectSize + 2;
                        c.Width = _rectSize - 4;
                        c.Height = _rectSize - 4;

                        c.SetGraphic(c.Graphic, c.Hue);

                        indices[index] = -1;
                    }
                    else
                    {
                        Add(
                            new CounterItem(
                                this,
                                col * _rectSize + 2,
                                row * _rectSize + 2,
                                _rectSize - 4,
                                _rectSize - 4
                            )
                        );
                    }
                }
            }

            for (int i = 0; i < indices.Length; i++)
            {
                int index = indices[i];

                if (index >= 0 && index < items.Length)
                {
                    items[i].Parent = null;

                    items[i].Dispose();
                }
            }

            // Drop hotkeys bound to cells that no longer exist after a shrink.
            CounterBarHotkeysManager.PruneFrom(_rows * _columns);

            // Re-center keybind labels for the new cell size.
            RefreshHotkeyLabels();

            SetInScreen();
        }

        public CounterItem GetCounterItem(int index)
        {
            CounterItem[] items = GetControls<CounterItem>();

            if (items == null)
            {
                return null;
            }

            if (index >= 0 && items.Length > index)
            {
                return items[index];
            }

            return null;
        }

        /// <summary>Position of <paramref name="item"/> among the counter cells, matching the order used by save/restore and hotkey ids. -1 when not found.</summary>
        public int IndexOf(CounterItem item)
        {
            CounterItem[] items = GetControls<CounterItem>();

            for (int i = 0; i < items.Length; i++)
                if (items[i] == item)
                    return i;

            return -1;
        }

        /// <summary>Refreshes every cell's optional keybind label (e.g. after toggling the option or restoring).</summary>
        public void RefreshHotkeyLabels()
        {
            foreach (CounterItem item in GetControls<CounterItem>())
                item.UpdateHotkeyLabel();
        }

        public override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            base.OnMouseUp(x, y, button);

            if (button == MouseButtonType.Left)
            {
                if (Keyboard.Alt && Keyboard.Ctrl)
                {
                    IsLocked = ProfileManager.CurrentProfile.CounterGumpLocked = !IsLocked;
                }
            }
        }

        public override void Save(XmlTextWriter writer)
        {
            base.Save(writer);

            writer.WriteAttributeString("rows", _rows.ToString());
            writer.WriteAttributeString("columns", _columns.ToString());
            writer.WriteAttributeString("rectsize", _rectSize.ToString());

            CounterItem[] controls = GetControls<CounterItem>();

            writer.WriteStartElement("controls");

            for (int index = 0; index < controls.Length; index++)
            {
                CounterItem control = controls[index];

                writer.WriteStartElement("control");
                writer.WriteAttributeString("graphic", control.Graphic.ToString());
                writer.WriteAttributeString("hue", control.Hue.ToString());

                CounterBarSlot slot = control.Slot;
                if (slot != null && !slot.IsEmpty)
                {
                    writer.WriteAttributeString("slottype", ((int)slot.Type).ToString());

                    switch (slot.Type)
                    {
                        case CounterBarSlotType.Spell:
                            writer.WriteAttributeString("spellid", slot.SpellId.ToString());
                            break;
                        case CounterBarSlotType.Macro:
                            writer.WriteAttributeString("macroname", slot.MacroName ?? string.Empty);
                            break;
                        case CounterBarSlotType.Script:
                            writer.WriteAttributeString("scriptid", slot.ScriptId ?? string.Empty);
                            break;
                        case CounterBarSlotType.Skill:
                            writer.WriteAttributeString("skillindex", slot.SkillIndex.ToString());
                            break;
                        case CounterBarSlotType.Ability:
                            writer.WriteAttributeString("abilityprimary", slot.AbilityPrimary.ToString());
                            break;
                    }
                }

                WriteHotkey(writer, CounterBarHotkeysManager.GetBinding(index));

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        /// <summary>Serializes a cell's hotkey binding as attributes, writing nothing when the binding is empty.</summary>
        private static void WriteHotkey(XmlTextWriter writer, HotkeyBinding binding)
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

        public override void Restore(XmlElement xml)
        {
            base.Restore(xml);

            _rows = int.Parse(xml.GetAttribute("rows"));
            _columns = int.Parse(xml.GetAttribute("columns"));
            _rectSize = int.Parse(xml.GetAttribute("rectsize"));

            BuildGump();

            XmlElement controlsXml = xml["controls"];

            if (controlsXml != null)
            {
                CounterItem[] items = GetControls<CounterItem>();
                int index = 0;

                foreach (XmlElement controlXml in controlsXml.GetElementsByTagName("control"))
                {
                    if (index < items.Length)
                    {
                        CounterBarSlot slot = RestoreSlot(controlXml);

                        if (slot != null && !slot.IsEmpty)
                        {
                            // Spell/macro/ability/script/skill action; resolves its own icon/label.
                            items[index]?.SetSlot(slot);
                        }
                        else
                        {
                            // Plain item counter.
                            items[index]?.SetGraphic(
                                ushort.Parse(controlXml.GetAttribute("graphic")),
                                ushort.Parse(controlXml.GetAttribute("hue"))
                            );
                        }

                        // Re-register the saved hotkey with the central system (XML is the source of truth).
                        HotkeyBinding hotkey = RestoreHotkey(controlXml);
                        if (hotkey != null && !hotkey.IsEmpty)
                            CounterBarHotkeysManager.SetBinding(index, hotkey);

                        index++;
                    }
                    else
                    {
                        Log.Error(ResGumps.IndexOutOfbounds);
                    }
                }
            }

            IsEnabled = IsVisible = ProfileManager.CurrentProfile.CounterBarEnabled;
            IsLocked = ProfileManager.CurrentProfile.CounterGumpLocked;

            RefreshHotkeyLabels();
        }

        /// <summary>Rebuilds a <see cref="CounterBarSlot"/> from a saved counter cell, migrating the legacy standalone "spellid" attribute.</summary>
        private static CounterBarSlot RestoreSlot(XmlElement controlXml)
        {
            // A partially-written profile (e.g. a crash mid-save) can leave malformed or missing
            // per-type attributes; degrade gracefully to an empty slot instead of aborting the whole restore.
            try
            {
                if (controlXml.HasAttribute("slottype"))
                {
                    var type = (CounterBarSlotType)int.Parse(controlXml.GetAttribute("slottype"));

                    switch (type)
                    {
                        case CounterBarSlotType.Spell:
                            return CounterBarSlot.FromSpell(SpellDefinition.FullIndexGetSpell(int.Parse(controlXml.GetAttribute("spellid"))));
                        case CounterBarSlotType.Macro:
                            return new CounterBarSlot { Type = CounterBarSlotType.Macro, MacroName = controlXml.GetAttribute("macroname") };
                        case CounterBarSlotType.Script:
                            return new CounterBarSlot { Type = CounterBarSlotType.Script, ScriptId = controlXml.GetAttribute("scriptid") };
                        case CounterBarSlotType.Skill:
                            return CounterBarSlot.FromSkill(int.Parse(controlXml.GetAttribute("skillindex")));
                        case CounterBarSlotType.Ability:
                            return CounterBarSlot.FromAbility(bool.Parse(controlXml.GetAttribute("abilityprimary")));
                    }
                }

                // Legacy: pre-parity saves stored a spell as a standalone "spellid" attribute alongside the gump graphic.
                if (controlXml.HasAttribute("spellid"))
                    return CounterBarSlot.FromSpell(SpellDefinition.FullIndexGetSpell(int.Parse(controlXml.GetAttribute("spellid"))));
            }
            catch (FormatException e)
            {
                Log.Error($"Malformed CounterBarSlot data during restore; defaulting to empty. {e}");
            }

            return CounterBarSlot.Empty();
        }

        /// <summary>Rebuilds a cell's <see cref="HotkeyBinding"/> from its saved attributes; tolerant of missing/partial data.</summary>
        private static HotkeyBinding RestoreHotkey(XmlElement controlXml)
        {
            var binding = new HotkeyBinding
            {
                Key = (SDL.SDL_Keycode)ParseIntAttr(controlXml, "hkkey", (int)SDL.SDL_Keycode.SDLK_UNKNOWN),
                Ctrl = ParseBoolAttr(controlXml, "hkctrl"),
                Shift = ParseBoolAttr(controlXml, "hkshift"),
                Alt = ParseBoolAttr(controlXml, "hkalt"),
                MouseButton = (MouseButtonType)ParseIntAttr(controlXml, "hkmouse", (int)MouseButtonType.None),
                WheelScroll = ParseBoolAttr(controlXml, "hkwheel"),
                WheelUp = ParseBoolAttr(controlXml, "hkwheelup")
            };

            string controllers = controlXml.GetAttribute("hkcontroller");
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

        private static int ParseIntAttr(XmlElement xml, string attr, int fallback)
            => xml.HasAttribute(attr) && int.TryParse(xml.GetAttribute(attr), out int v) ? v : fallback;

        private static bool ParseBoolAttr(XmlElement xml, string attr)
            => xml.HasAttribute(attr) && bool.TryParse(xml.GetAttribute(attr), out bool v) && v;

        protected override void OnLockedChanged()
        {
            base.OnLockedChanged();
            CanCloseWithRightClick = false;
        }

        public override void Dispose()
        {
            if (CurrentCounterBarGump == this)
            {
                // Drop this bar's cell hotkeys from the central registry so they don't linger or fire
                // after the gump is gone (e.g. across a profile switch).
                CounterBarHotkeysManager.PruneFrom(0);
                CurrentCounterBarGump = null;
            }
            base.Dispose();
        }

        public class CounterItem : Control
        {
            private int _amount;
            private readonly ImageWithText _image;
            private uint _time;
            private const uint HIGHLIGHT_DURATION = 1000;
            private const uint HOTKEY_FLASH_DURATION = 500; // warn-colored flash when the cell's hotkey fires
            private const ushort SCRIPT_RUNNING_HUE = 0x0044; // green tint while a script slot runs
            private uint _endHighlight;
            private uint _hotkeyFlashEnd;
            private bool _highlight;
            private readonly CounterBarGump _gump;
            private readonly Label _hotkeyLabel;

            private CounterBarSlot _slot = CounterBarSlot.Empty();
            private ContextMenuItemEntry _macroMenu;
            private ContextMenuItemEntry _scriptMenu;

            public CounterItem(CounterBarGump gump, int x, int y, int w, int h)
            {
                _gump = gump;
                AcceptMouseInput = true;
                WantUpdateSize = false;
                CanMove = true;
                CanCloseWithRightClick = false;

                X = x;
                Y = y;
                Width = w;
                Height = h;

                _image = new ImageWithText();
                Add(_image);

                // Optional keybind label shown at the top of the cell (hidden until a hotkey is set and the option is on).
                Add(_hotkeyLabel = new Label(string.Empty, true, 0x35, 0, 1, FontStyle.BlackBorder)
                {
                    X = 2,
                    Y = 1,
                    AcceptMouseInput = false,
                    IsVisible = false
                });

                ContextMenu = new ContextMenuControl(_gump);
                ContextMenu.Add(ResGumps.UseObject, Use);
                ContextMenu.Add(ResGumps.Remove, RemoveItem);
                ContextMenu.Add(TazLang.Get("spellbar_setspell"), GenSpellList());
                ContextMenu.Add(new ContextMenuItemEntry(TazLang.Get("spellbar_quicksetspell"), QuickSetSpell));

                _macroMenu = new ContextMenuItemEntry(TazLang.Get("spellbar_setmacro"));
                GenMacroList(_macroMenu);
                ContextMenu.Add(_macroMenu);

                var abilityMenu = new ContextMenuItemEntry(TazLang.Get("spellbar_setability"));
                abilityMenu.Add(new ContextMenuItemEntry(TazLang.Get("spellbar_ability_primary"), () => SetSlot(CounterBarSlot.FromAbility(true))));
                abilityMenu.Add(new ContextMenuItemEntry(TazLang.Get("spellbar_ability_secondary"), () => SetSlot(CounterBarSlot.FromAbility(false))));
                ContextMenu.Add(abilityMenu);

                _scriptMenu = new ContextMenuItemEntry(TazLang.Get("spellbar_setscript"));
                GenScriptList(_scriptMenu);
                ContextMenu.Add(_scriptMenu);

                var skillMenu = new ContextMenuItemEntry(TazLang.Get("spellbar_setskill"));
                GenSkillList(skillMenu);
                ContextMenu.Add(skillMenu);

                ContextMenu.Add(new ContextMenuItemEntry(TazLang.Get("counterbar_sethotkey"), SetHotkey));
            }

            public ushort Graphic { get; private set; }

            public ushort Hue { get; private set; }

            /// <summary>The spell/macro/ability/script/skill action assigned to this cell, or an empty slot for a plain item counter.</summary>
            public CounterBarSlot Slot => _slot;

            /// <summary>True when this cell holds a spell bar action instead of an item to count.</summary>
            public bool HasAction => _slot != null && !_slot.IsEmpty;

            public void SetGraphic(ushort graphic, ushort hue, bool isGumpIcon = false)
            {
                _image.ChangeGraphic(graphic, hue, isGumpIcon);

                if (graphic == 0)
                {
                    return;
                }

                Graphic = graphic;
                Hue = hue;
            }

            /// <summary>Assigns a spell bar action to this cell (or clears it when the slot is empty) and refreshes its icon/label/tooltip.</summary>
            public void SetSlot(CounterBarSlot slot)
            {
                _slot = slot ?? CounterBarSlot.Empty();

                // An action slot replaces any item-counting graphic on this cell.
                _amount = 0;
                Graphic = 0;
                Hue = 0;

                RefreshSlotVisual();
            }

            /// <summary>Resolves the icon/label/tooltip for the currently assigned action slot.</summary>
            private void RefreshSlotVisual()
            {
                if (!HasAction)
                {
                    _image.ChangeGraphic(0, 0);
                    ClearTooltip();
                    return;
                }

                ushort graphic = _slot.GetIconGraphic(_gump.World);
                if (graphic != 0)
                {
                    // Spells, macros and abilities resolve to a gump icon; tint it red while the action
                    // is active (a toggled-on weapon ability or a toggle-move spell), like the spell bar.
                    _image.ChangeGraphic(graphic, _slot.GetActiveHue(_gump.World), true);
                }
                else
                {
                    // Scripts and skills (and graphic-less macros) fall back to a short text label.
                    _image.ChangeGraphic(0, 0, true);
                    _image.SetAmount(StringHelper.AbbreviateToInitials(_slot.SlotLabel));
                }

                if (_slot.TryGetTooltip(_gump.World, out string tip) && !string.IsNullOrEmpty(tip))
                    SetTooltip(tip);
                else
                    ClearTooltip();
            }

            public void RemoveItem()
            {
                _image?.ChangeGraphic(0, 0);
                _image?.SetAmount(string.Empty); // clear any lingering script/skill text label
                _amount = 0;
                Graphic = 0;
                Hue = 0;
                _slot = CounterBarSlot.Empty();
                ClearTooltip();
            }

            public void Use()
            {
                if (HasAction)
                {
                    _slot.Activate(_gump.World);
                    return;
                }

                if (Graphic == 0)
                {
                    return;
                }

                Item backpack = _gump.World.Player.Backpack;

                if (backpack == null)
                {
                    return;
                }

                Item item = backpack.FindItem(Graphic, Hue);

                if (item != null)
                {
                    GameActions.DoubleClick(_gump.World, item);
                }
            }

            // public override void OnMouseOver(int x, int y)
            // {
            //     base.OnMouseOver(x, y);

            //     if (_gump.World.Player.Backpack?.FindItem(Graphic, Hue) is {} item)
            //         SetTooltip(item);
            // }

            // protected override void OnMouseExit(int x, int y)
            // {
            //     base.OnMouseExit(x, y);
            //     ClearTooltip();
            // }

            /// <summary>Opens the shared hotkey capture window to bind (or clear) a hotkey that triggers this cell.</summary>
            private void SetHotkey()
            {
                int index = _gump.IndexOf(this);
                if (index < 0)
                    return;

                // The universal capture window adds itself to the UI and commits on Save; the binding is
                // registered with the central hotkey system and persisted with the cell on the next gump save.
                _ = new HotkeyCaptureWindow(
                    prompt: TazLang.Get("counterbar_slot", new[] { (index + 1).ToString() }),
                    existing: CounterBarHotkeysManager.GetBinding(index),
                    onSaved: binding =>
                    {
                        CounterBarHotkeysManager.SetBinding(index, binding);
                        UpdateHotkeyLabel();
                    });
            }

            /// <summary>Refreshes the optional keybind label from the cell's current binding and the profile toggle.</summary>
            public void UpdateHotkeyLabel()
            {
                if (_hotkeyLabel == null)
                    return;

                int index = _gump.IndexOf(this);
                HotkeyBinding binding = index >= 0 ? CounterBarHotkeysManager.GetBinding(index) : null;

                if (ProfileManager.CurrentProfile.CounterBarShowHotkeys && binding is { IsEmpty: false })
                {
                    _hotkeyLabel.Text = binding.Describe();
                    _hotkeyLabel.X = Math.Max(0, (Width - _hotkeyLabel.Width) / 2);
                    _hotkeyLabel.Y = 1;
                    _hotkeyLabel.IsVisible = true;
                }
                else
                {
                    _hotkeyLabel.IsVisible = false;
                }
            }

            /// <summary>Triggers the cell from its hotkey: shows a brief warn-colored flash, then performs the action.</summary>
            public void ActivateFromHotkey()
            {
                _hotkeyFlashEnd = Time.Ticks + HOTKEY_FLASH_DURATION;
                Use();
            }

            private void QuickSetSpell() =>
                UIManager.Add
                (
                    new SpellQuickSearch
                    (World.Instance,
                        ScreenCoordinateX - 20, ScreenCoordinateY - 90, (s) =>
                        {
                            SetSlot(CounterBarSlot.FromSpell(s));
                        }, true
                    )
                );

            private List<ContextMenuItemEntry> GenSpellList()
            {
                var list = new List<ContextMenuItemEntry>();

                void AddSchool(string label, System.Collections.Generic.IEnumerable<SpellDefinition> spells)
                {
                    var entry = new ContextMenuItemEntry(label);
                    foreach (SpellDefinition spell in spells)
                        entry.Add(new ContextMenuItemEntry(spell.Name, () => SetSlot(CounterBarSlot.FromSpell(spell))));
                    list.Add(entry);
                }

                AddSchool(TazLang.Get("spellschool_magery"), SpellsMagery.GetAllSpells.Values);
                AddSchool(TazLang.Get("spellschool_necromancy"), SpellsNecromancy.GetAllSpells.Values);
                AddSchool(TazLang.Get("spellschool_chivalry"), SpellsChivalry.GetAllSpells.Values);
                AddSchool(TazLang.Get("spellschool_bushido"), SpellsBushido.GetAllSpells.Values);
                AddSchool(TazLang.Get("spellschool_ninjitsu"), SpellsNinjitsu.GetAllSpells.Values);
                AddSchool(TazLang.Get("spellschool_spellweaving"), SpellsSpellweaving.GetAllSpells.Values);
                AddSchool(TazLang.Get("spellschool_mysticism"), SpellsMysticism.GetAllSpells.Values);
                AddSchool(TazLang.Get("spellschool_mastery"), SpellsMastery.GetAllSpells.Values);

                return list;
            }

            private void GenMacroList(ContextMenuItemEntry parent)
            {
                if (parent == null)
                    return;

                parent.Items.Clear();

                foreach (Macro macro in _gump.World.Macros.GetAllMacros())
                    parent.Add(new ContextMenuItemEntry(macro.Name, () => SetSlot(CounterBarSlot.FromMacro(macro))));
            }

            private void GenScriptList(ContextMenuItemEntry parent)
            {
                if (parent == null)
                    return;

                parent.Items.Clear();

                foreach (ScriptFile s in ClassicUO.LegionScripting.LegionScripting.LoadedScripts)
                {
                    ScriptFile script = s;
                    // RelativePath (e.g. "group/loot.py") so same-named scripts in different groups stay distinguishable.
                    parent.Add(new ContextMenuItemEntry(script.RelativePath, () => SetSlot(CounterBarSlot.FromScript(script))));
                }
            }

            private void GenSkillList(ContextMenuItemEntry parent)
            {
                if (parent == null)
                    return;

                parent.Items.Clear();

                // Only skills that have a usable action can be invoked.
                foreach (var skill in Client.Game.UO.FileManager.Skills.SortedSkills)
                {
                    if (!skill.HasAction)
                        continue;

                    int index = skill.Index;
                    parent.Add(new ContextMenuItemEntry(skill.Name, () => SetSlot(CounterBarSlot.FromSkill(index))));
                }
            }

            public override void OnMouseUp(int x, int y, MouseButtonType button)
            {
                if (button == MouseButtonType.Right)
                {
                    // Refresh the dynamic lists so newly added macros/scripts appear.
                    GenMacroList(_macroMenu);
                    GenScriptList(_scriptMenu);
                }

                if (button == MouseButtonType.Left)
                {
                    if (Keyboard.Alt && Keyboard.Ctrl)
                        if(Parent is Gump pg)
                        {
                            Log.Trace(pg.GetType().ToString());
                            pg.IsLocked = ProfileManager.CurrentProfile.CounterGumpLocked = !pg.IsLocked;
                        }
                    if (Client.Game.UO.GameCursor.ItemHold.Enabled)
                    {
                        // Dropping an item onto the cell turns it back into a plain item counter.
                        _slot = CounterBarSlot.Empty();
                        ClearTooltip();

                        SetGraphic(
                            Client.Game.UO.GameCursor.ItemHold.Graphic,
                            Client.Game.UO.GameCursor.ItemHold.Hue
                        );

                        GameActions.DropItem(
                            Client.Game.UO.GameCursor.ItemHold.Serial,
                            Client.Game.UO.GameCursor.ItemHold.X,
                            Client.Game.UO.GameCursor.ItemHold.Y,
                            0,
                            Client.Game.UO.GameCursor.ItemHold.Container
                        );
                    }
                    else if (ProfileManager.CurrentProfile.CastSpellsByOneClick)
                    {
                        Use();
                        return;
                    }
                }
                else if (button == MouseButtonType.Right && Keyboard.Alt && (Graphic != 0 || HasAction))
                {
                    RemoveItem();

                    return;
                }

                base.OnMouseUp(x, y, button);
            }

            public override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
            {
                if (
                    button == MouseButtonType.Left
                    && !ProfileManager.CurrentProfile.CastSpellsByOneClick
                )
                {
                    Use();
                }

                return true;
            }

            public override void Update()
            {
                base.Update();

                if (Parent != null && Parent.IsEnabled && _time < Time.Ticks)
                {
                    _time = Time.Ticks + 100;
                    if (HasAction)
                    {
                        // Ability slots follow the equipped weapon, so keep the icon/label/tooltip current.
                        RefreshSlotVisual();
                        return;
                    }

                    if (Graphic == 0)
                    {
                        _image.SetAmount(string.Empty);
                    }
                    else
                    {
                        _amount = 0;

                        for (
                            var item = (Item)_gump.World.Player.Items;
                            item != null;
                            item = (Item)item.Next
                        )
                        {
                            if (
                                item.ItemData.IsContainer
                                && !item.IsEmpty
                                && item.Layer >= Layer.OneHanded
                                && item.Layer <= Layer.Legs
                            )
                            {
                                GetAmount(item, Graphic, Hue, ref _amount);
                            }
                        }

                        if (ProfileManager.CurrentProfile.CounterBarDisplayAbbreviatedAmount)
                        {
                            if (
                                _amount >= ProfileManager.CurrentProfile.CounterBarAbbreviatedAmount
                            )
                            {
                                _image.SetAmount(StringHelper.IntToAbbreviatedString(_amount));

                                return;
                            }
                        }

                        if (ProfileManager.CurrentProfile.CounterBarHighlightOnUse)
                        {
                            if (int.TryParse(_image.GetText(), out int cAmt))
                            {
                                if (cAmt > _amount)
                                {
                                    _highlight = true;
                                    _endHighlight = Time.Ticks + HIGHLIGHT_DURATION;
                                }
                            }
                        }
                        _image.SetAmount(_amount.ToString());
                    }
                }
            }

            private void GetAmount(Item parent, ushort graphic, ushort hue, ref int amount)
            {
                if (parent == null)
                {
                    return;
                }

                for (LinkedObject i = parent.Items; i != null; i = i.Next)
                {
                    var item = (Item)i;

                    GetAmount(item, graphic, hue, ref amount);

                    if (item.Graphic == graphic && item.Hue == hue && item.Exists)
                    {
                        amount += item.Amount;
                        SetTooltip(item);
                    }
                }
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                // Tint the cell green while a script slot is running, matching the spell bar.
                if (_slot != null && _slot.IsScriptRunning)
                {
                    Vector3 runningHue = ShaderHueTranslator.GetHueVector(SCRIPT_RUNNING_HUE, false, 0.5f);
                    batcher.Draw(SolidColorTextureCache.GetTexture(Color.Green), new Rectangle(x, y, Width, Height), runningHue);
                }

                base.Draw(batcher, x, y);

                Texture2D color = SolidColorTextureCache.GetTexture(
                    MouseIsOver
                        ? Color.Yellow
                        : ProfileManager.CurrentProfile.CounterBarHighlightOnAmount
                        && _amount < ProfileManager.CurrentProfile.CounterBarHighlightAmount
                        && Graphic != 0
                            ? Color.Red
                            : Color.Gray
                );

                Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);

                if (_highlight && Time.Ticks > _endHighlight)
                {
                    _highlight = false;
                }

                if (_highlight)
                {
                    hueVector.Z = ((float)_endHighlight - (float)Time.Ticks) / (float)HIGHLIGHT_DURATION;
                    batcher.Draw(SolidColorTextureCache.GetTexture(Color.Yellow), new Rectangle(x, y, Width, Height), hueVector);
                }

                // Brief warn-colored flash when this cell's hotkey fires, fading out over its duration.
                if (Time.Ticks < _hotkeyFlashEnd)
                {
                    Vector3 flashHue = ShaderHueTranslator.GetHueVector(0);
                    flashHue.Z = (float)(_hotkeyFlashEnd - Time.Ticks) / HOTKEY_FLASH_DURATION;
                    batcher.Draw(SolidColorTextureCache.GetTexture(Constants.Warn), new Rectangle(x, y, Width, Height), flashHue);
                }

                hueVector.Z = 1;

                batcher.DrawRectangle(color, x, y, Width, Height, hueVector);

                return true;
            }

            private class ImageWithText : Control
            {
                private readonly Label _label;
                private ushort _graphic;
                private ushort _hue;
                private bool _partial;
                private bool _isGumpGraphic;

                public ImageWithText()
                {
                    CanMove = true;
                    WantUpdateSize = true;
                    AcceptMouseInput = false;

                    _label = new Label("", true, 0x35, 0, 1, FontStyle.BlackBorder)
                    {
                        X = 2,
                        Y = Height - 15,
                        AcceptMouseInput = false
                    };

                    Add(_label);
                }

                public void ChangeGraphic(ushort graphic, ushort hue, bool isGumpGraphic = false)
                {
                    _isGumpGraphic = isGumpGraphic;
                    if (graphic != 0)
                    {
                        _graphic = graphic;
                        _hue = hue;
                        _partial = isGumpGraphic ? false : Client.Game.UO.FileManager.TileData.StaticData[graphic].IsPartialHue;
                        _label.Y = Parent.Height - 15;
                    }
                    else
                    {
                        _graphic = 0;
                    }
                    if (_isGumpGraphic)
                        _label.Text = string.Empty;
                }

                public override void Update()
                {
                    base.Update();

                    if (Parent != null)
                    {
                        Width = Parent.Width;
                        Height = Parent.Height;

                        // Center the label horizontally and anchor it to the bottom of the cell (used for
                        // amounts and for icon-less action labels such as scripts/skills).
                        if (_label != null)
                        {
                            _label.X = Math.Max(0, (Width - _label.Width) / 2);
                            _label.Y = Height - 15;
                        }
                    }
                }

                public override bool Draw(UltimaBatcher2D batcher, int x, int y)
                {
                    if (_graphic != 0)
                    {
                        ref readonly SpriteInfo artInfo = ref Client.Game.UO.Arts.GetArt(_graphic);
                        if (_isGumpGraphic)
                            artInfo = ref Client.Game.UO.Gumps.GetGump(_graphic);

                        Rectangle rect = _isGumpGraphic ? artInfo.UV : Client.Game.UO.Arts.GetRealArtBounds(_graphic);

                        Vector3 hueVector = ShaderHueTranslator.GetHueVector(_hue, _partial, 1f, _isGumpGraphic);

                        // Scale the icon to fill the cell while preserving its aspect ratio, then center it.
                        // This scales small icons up and large icons down so they always match the slot size.
                        var originalSize = new Point(Width, Height);
                        var point = new Point();

                        // Scaling can be disabled per graphic kind: spell/ability icons resolve to gump
                        // graphics, plain item counters to art. When disabled we draw the graphic at its
                        // native size (still centered) instead of stretching it to the cell.
                        bool disableScaling = _isGumpGraphic
                            ? ProfileManager.CurrentProfile.CounterBarDisableIconScaling
                            : ProfileManager.CurrentProfile.CounterBarDisableItemScaling;

                        if (rect.Width > 0 && rect.Height > 0)
                        {
                            if (disableScaling)
                            {
                                originalSize.X = rect.Width;
                                originalSize.Y = rect.Height;
                            }
                            else
                            {
                                float scale = Math.Min((float)Width / rect.Width, (float)Height / rect.Height);

                                originalSize.X = Math.Max(1, (int)(rect.Width * scale));
                                originalSize.Y = Math.Max(1, (int)(rect.Height * scale));
                            }

                            point.X = (Width - originalSize.X) >> 1;
                            point.Y = (Height - originalSize.Y) >> 1;
                        }

                        if (_isGumpGraphic)
                            batcher.Draw(
                                artInfo.Texture,
                                new Rectangle(x + point.X, y + point.Y, originalSize.X, originalSize.Y),
                                new Rectangle(
                                    artInfo.UV.X,
                                    artInfo.UV.Y,
                                    rect.Width,
                                    rect.Height
                                ),
                                hueVector
                            );
                        else
                            batcher.Draw(
                                artInfo.Texture,
                                new Rectangle(x + point.X, y + point.Y, originalSize.X, originalSize.Y),
                                new Rectangle(
                                    artInfo.UV.X + rect.X,
                                    artInfo.UV.Y + rect.Y,
                                    rect.Width,
                                    rect.Height
                                ),
                                hueVector
                            );
                    }

                    return base.Draw(batcher, x, y);
                }

                public void SetAmount(string amount) => _label.Text = amount;

                public string GetText() => _label?.Text ?? "";
            }
        }
    }
}
