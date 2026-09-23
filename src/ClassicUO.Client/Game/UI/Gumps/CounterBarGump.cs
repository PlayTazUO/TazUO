// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Xml;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Managers.Hotkeys;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.UI.Gumps
{
    public class CounterBarGump : Gump, IBarCellHost
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

        /// <inheritdoc />
        public Gump OwnerGump => this;

        /// <summary>Current number of cell rows.</summary>
        public int Rows => _rows;

        /// <summary>Current number of cell columns.</summary>
        public int Columns => _columns;

        /// <summary>Current cell edge length in pixels.</summary>
        public int RectSize => _rectSize;

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

                        c.OnCellResized();

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
                    // The cell is being removed by the shrink, so drop its hotkey too.
                    ClearCellHotkey(i);

                    items[i].Parent = null;

                    items[i].Dispose();
                }
            }

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
        public int IndexOf(BarCell item)
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

        /// <summary>Display name for a cell's hotkey in the central hotkeys list.</summary>
        private static string HotkeyDisplayName(int index) => TazLang.Get("counterbar_slot", new[] { (index + 1).ToString() });

        private const string HotkeyIdPrefix = "counterbar:";
        private const string HotkeyCategory = "Bar";

        private static string HotkeyId(int index) => HotkeyIdPrefix + index;

        /// <summary>Current binding for a cell, or an empty binding when unset.</summary>
        public HotkeyBinding GetCellHotkey(int index) => HotKeys.Get(HotkeyId(index))?.Binding?.Clone() ?? new HotkeyBinding();

        /// <summary>
        /// Registers (or clears) a cell's hotkey with the central <see cref="HotKeys"/> system. The
        /// binding is persisted with the cell on the next gump save; unbindable bindings clear it.
        /// </summary>
        public void SetCellHotkey(int index, HotkeyBinding binding)
        {
            string id = HotkeyId(index);

            if (binding?.IsTriggerable != true)
            {
                HotKeys.Unregister(id);
                return;
            }

            HotKeyEntry entry = HotKeys.Register(id, HotkeyDisplayName(index), new HotkeyBinding(), HotkeyCategory, () =>
            {
                // The gump is kept around while counters are toggled off, so only fire when visible.
                if (!IsDisposed && IsEnabled && IsVisible)
                    GetCounterItem(index)?.ActivateFromHotkey();
            });
            entry.Binding = binding.Clone();
        }

        private static void ClearCellHotkey(int index) => HotKeys.Unregister(HotkeyId(index));

        private static void ClearCellHotkeys(int count)
        {
            for (int i = 0; i < count; i++)
                HotKeys.Unregister(HotkeyId(i));
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
                        case CounterBarSlotType.DressAgent:
                            writer.WriteAttributeString("dressconfigname", slot.DressConfigName ?? string.Empty);
                            writer.WriteAttributeString("dressagentundress", slot.DressAgentUndress.ToString());
                            break;
                    }
                }

                BarXml.WriteHotkey(writer, GetCellHotkey(index));

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
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
                        CounterBarSlot slot = BarXml.ReadSlot(controlXml);

                        if (slot != null && !slot.IsEmpty)
                        {
                            // Spell/macro/ability/script/skill/dress-agent action; resolves its own icon/label.
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
                        HotkeyBinding hotkey = BarXml.ReadHotkey(controlXml);
                        if (hotkey != null && !hotkey.IsEmpty)
                            SetCellHotkey(index, hotkey);

                        index++;
                    }
                    else
                    {
                        Log.Error(TazLang.Get("index_out_ofbounds"));
                    }
                }
            }

            IsEnabled = IsVisible = ProfileManager.CurrentProfile.CounterBarEnabled;
            IsLocked = ProfileManager.CurrentProfile.CounterGumpLocked;

            RefreshHotkeyLabels();
        }

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
                ClearCellHotkeys(_rows * _columns);
                CurrentCounterBarGump = null;
            }
            base.Dispose();
        }

        /// <summary>A counter-bar cell: a plain item counter or a counter action.</summary>
        public class CounterItem : BarCell
        {
            public CounterItem(CounterBarGump gump, int x, int y, int w, int h) : base(gump, x, y, w, h) { }

            /// <inheritdoc />
            protected override void ToggleLock(Gump gump) =>
                gump.IsLocked = ProfileManager.CurrentProfile.CounterGumpLocked = !gump.IsLocked;
        }
    }
}
