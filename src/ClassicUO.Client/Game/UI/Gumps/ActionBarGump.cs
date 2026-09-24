// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Linq;
using System.Xml;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Managers.Hotkeys;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.MyraWindows;
using ClassicUO.Input;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.UI.Gumps
{
    /// <summary>
    /// A named, freely placeable bar of action cells. Unlike the single counter bar, any number of
    /// action bars can be open at once; each has its own name and cell grid. The whole bar (name,
    /// layout and every cell's action/hotkey) is persisted through the standard gump XML
    /// (<see cref="Save"/>/<see cref="Restore"/>), so it is written to and read from the profile's
    /// <c>gumps.xml</c> like every other saved gump with no separate save system.
    /// </summary>
    public class ActionBarGump : Gump, IBarCellHost
    {
        /// <summary>Height of the title strip that shows the bar's name above the cells.</summary>
        private const int HEADER_HEIGHT = 16;

        // Layout bounds shared by the constructor, SetLayout and Restore, so no path can build an
        // oversized grid (e.g. from malformed saved values).
        private const int MIN_CELL_SIZE = 30;
        private const int MAX_CELL_SIZE = 80;
        private const int MIN_DIMENSION = 1;
        private const int MAX_DIMENSION = 30;

        /// <summary>Prefix shared by every cell hotkey id of every action bar (the bar id follows).</summary>
        private const string HotkeyIdPrefix = "actionbar:";

        private const string HotkeyCategory = "Bar";

        private AlphaBlendControl _background;
        private Label _nameLabel;

        private int _rows,
            _columns,
            _rectSize;

        private string _name;
        private int _barId;

        /// <summary>Restore constructor: state is filled in by <see cref="Restore"/>.</summary>
        public ActionBarGump(World world) : base(world, 0, 0)
        {
            CanCloseWithRightClick = false;
        }

        public ActionBarGump(
            World world,
            int x,
            int y,
            int rectSize,
            int rows,
            int columns,
            string name
        ) : base(world, 0, 0)
        {
            X = x;
            Y = y;

            _rows = ClampDimension(rows);
            _columns = ClampDimension(columns);
            _rectSize = ClampCellSize(rectSize);
            _name = NormalizeName(name);
            _barId = GetNextBarId();

            // A reused id can still have hotkeys mirrored in hotkeys.json from a since-closed bar; drop
            // them so the new bar starts clean.
            ClearBarHotkeys();

            BuildGump();

            IsLocked = ProfileManager.CurrentProfile.CounterGumpLocked;
            CanCloseWithRightClick = false;
        }

        public override GumpType GumpType => GumpType.ActionBar;

        /// <inheritdoc />
        public Gump OwnerGump => this;

        /// <summary>User-facing name shown in the bar's header and used to label its hotkeys.</summary>
        public string Name => _name;

        /// <summary>Stable per-bar id used to namespace this bar's hotkeys across save/restore.</summary>
        private int BarId => _barId;

        private string HotkeyPrefix => $"{HotkeyIdPrefix}{_barId}:";

        private string HotkeyId(int index) => $"{HotkeyPrefix}{index}";

        /// <summary>Display name for one of this bar's cell hotkeys in the central hotkeys list.</summary>
        private string HotkeyDisplayName(int index) => $"{_name}: {index + 1}";

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

            HotKeyEntry entry = HotKeys.Register(id, HotkeyDisplayName(index), new HotkeyBinding(), HotkeyCategory, () => GetActionItem(index)?.ActivateFromHotkey());
            entry.Binding = binding.Clone();
        }

        private void ClearCellHotkey(int index) => HotKeys.Unregister(HotkeyId(index));

        /// <summary>Drops every registered hotkey for this bar (its cells all share <see cref="HotkeyPrefix"/>).</summary>
        private void ClearBarHotkeys()
        {
            foreach (HotKeyEntry entry in HotKeys.AllRegistered().ToArray())
                if (entry.Id.StartsWith(HotkeyPrefix, StringComparison.Ordinal))
                    HotKeys.Unregister(entry.Id);
        }

        /// <summary>Creates a new action bar from the profile's counter layout and adds it to the UI.</summary>
        public static ActionBarGump Create(World world, string name)
        {
            Profile profile = ProfileManager.CurrentProfile;

            // Cascade new bars so a stack of them doesn't land on the exact same pixels.
            int offset = 0;
            foreach (IGui gui in UIManager.Gumps)
                if (gui is ActionBarGump)
                    offset++;

            var bar = new ActionBarGump(world, 200 + offset * 12, 200 + offset * 12, profile.CounterBarCellSize, 1, 5, name);
            UIManager.Add(bar);
            return bar;
        }

        private static string NormalizeName(string name) =>
            string.IsNullOrWhiteSpace(name) ? TazLang.Get("actionbar_defaultname", "Action Bar") : name.Trim();

        private static int ClampCellSize(int size) => Math.Clamp(size, MIN_CELL_SIZE, MAX_CELL_SIZE);

        private static int ClampDimension(int value) => Math.Clamp(value, MIN_DIMENSION, MAX_DIMENSION);

        /// <summary>Returns an id greater than every open bar's id, so new bars never collide.</summary>
        private static int GetNextBarId()
        {
            int max = 0;

            foreach (IGui gui in UIManager.Gumps)
                if (gui is ActionBarGump bar && bar.BarId > max)
                    max = bar.BarId;

            return max + 1;
        }

        private void BuildGump()
        {
            // Preserve the saved locked state: base.Restore sets IsLocked before BuildGump runs.
            CanMove = !IsLocked;
            AcceptMouseInput = true;
            AcceptKeyboardInput = false;
            WantUpdateSize = false;
            Width = _rectSize * _columns + 1;
            Height = HEADER_HEIGHT + _rectSize * _rows + 1;

            Add(_background = new AlphaBlendControl(0.7f) { Width = Width, Height = Height });

            Add(_nameLabel = new Label(_name, true, 0x35, 0, 1, FontStyle.BlackBorder) { Y = 1 });
            CenterNameLabel();

            for (int row = 0; row < _rows; row++)
            {
                for (int col = 0; col < _columns; col++)
                {
                    Add(
                        new ActionItem(
                            this,
                            col * _rectSize + 2,
                            HEADER_HEIGHT + row * _rectSize + 2,
                            _rectSize - 4,
                            _rectSize - 4
                        )
                    );
                }
            }
        }

        private void CenterNameLabel()
        {
            if (_nameLabel != null)
                _nameLabel.X = Math.Max(0, (Width - _nameLabel.Width) / 2);
        }

        /// <summary>Renames the bar, refreshes its header and updates its hotkeys' display names.</summary>
        public void SetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return;

            _name = name.Trim();

            if (_nameLabel != null)
            {
                _nameLabel.Text = _name;
                CenterNameLabel();
            }

            // Keep the central hotkey list's labels in sync with the new name.
            foreach (HotKeyEntry entry in HotKeys.AllRegistered().ToArray())
                if (entry.Id.StartsWith(HotkeyPrefix, StringComparison.Ordinal) && int.TryParse(entry.Id.AsSpan(HotkeyPrefix.Length), out int index))
                    entry.Name = HotkeyDisplayName(index);
        }

        public void AddRow() => SetLayout(_rectSize, _rows + 1, _columns);

        public void AddColumn() => SetLayout(_rectSize, _rows, _columns + 1);

        public void RemoveRow() => SetLayout(_rectSize, _rows - 1, _columns);

        public void RemoveColumn() => SetLayout(_rectSize, _rows, _columns - 1);

        /// <summary>Current square cell size, in pixels.</summary>
        public int CellSize => _rectSize;

        /// <summary>Applies a new cell size, clamped to the supported range; grid dimensions are unchanged.</summary>
        public void SetCellSize(int size) => SetLayout(size, _rows, _columns);

        public void SetLayout(int size, int rows, int columns)
        {
            rows = ClampDimension(rows);
            columns = ClampDimension(columns);
            size = ClampCellSize(size);

            if (_rectSize == size && _rows == rows && _columns == columns)
                return;

            _rectSize = size;
            _rows = rows;
            _columns = columns;
            ApplyLayout();
        }

        private void ApplyLayout()
        {
            Width = _rectSize * _columns + 1;
            Height = HEADER_HEIGHT + _rectSize * _rows + 1;

            _background.Width = Width;
            _background.Height = Height;
            CenterNameLabel();

            int needed = _rows * _columns;
            ActionItem[] items = GetControls<ActionItem>();

            // Drop cells past the end of the new grid (and their hotkeys).
            for (int i = items.Length - 1; i >= needed; i--)
            {
                ClearCellHotkey(i);
                items[i].Parent = null;
                items[i].Dispose();
            }

            for (int index = 0; index < needed; index++)
            {
                int row = index / _columns;
                int col = index % _columns;
                int x = col * _rectSize + 2;
                int y = HEADER_HEIGHT + row * _rectSize + 2;

                if (index < items.Length)
                {
                    ActionItem c = items[index];
                    c.X = x;
                    c.Y = y;
                    c.Width = _rectSize - 4;
                    c.Height = _rectSize - 4;
                    c.OnCellResized();
                }
                else
                {
                    Add(new ActionItem(this, x, y, _rectSize - 4, _rectSize - 4));
                }
            }

            RefreshHotkeyLabels();
            SetInScreen();
        }

        public ActionItem GetActionItem(int index)
        {
            ActionItem[] items = GetControls<ActionItem>();

            if (items != null && index >= 0 && index < items.Length)
                return items[index];

            return null;
        }

        /// <summary>Position of <paramref name="item"/> among the cells, matching save order and hotkey ids. -1 when not found.</summary>
        public int IndexOf(BarCell item)
        {
            ActionItem[] items = GetControls<ActionItem>();

            for (int i = 0; i < items.Length; i++)
                if (items[i] == item)
                    return i;

            return -1;
        }

        /// <summary>Refreshes every cell's optional keybind label (after a toggle, rename or restore).</summary>
        public void RefreshHotkeyLabels()
        {
            foreach (ActionItem item in GetControls<ActionItem>())
                item.UpdateHotkeyLabel();
        }

        public override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            base.OnMouseUp(x, y, button);

            if (button == MouseButtonType.Left && Keyboard.Alt && Keyboard.Ctrl)
                IsLocked = !IsLocked;
        }

        public override void Save(XmlTextWriter writer)
        {
            base.Save(writer);

            writer.WriteAttributeString("name", _name);
            writer.WriteAttributeString("barid", _barId.ToString());
            writer.WriteAttributeString("rows", _rows.ToString());
            writer.WriteAttributeString("columns", _columns.ToString());
            writer.WriteAttributeString("rectsize", _rectSize.ToString());

            ActionItem[] controls = GetControls<ActionItem>();

            writer.WriteStartElement("controls");

            for (int index = 0; index < controls.Length; index++)
            {
                ActionItem control = controls[index];

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

                BarXml.WriteCellColor(writer, control.CellColor);
                BarXml.WriteHotkey(writer, GetCellHotkey(index));

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        public override void Restore(XmlElement xml)
        {
            base.Restore(xml);

            _name = NormalizeName(xml.GetAttribute("name"));
            _barId = int.TryParse(xml.GetAttribute("barid"), out int barId) ? barId : 0;
            if (_barId <= 0)
                _barId = GetNextBarId();

            // Drop any hotkeys loaded from hotkeys.json for this id before re-applying the XML set,
            // so a binding cleared in a previous session can't linger on the restored bar.
            ClearBarHotkeys();

            // Tolerate missing/invalid layout attributes so a malformed bar still restores (clamped)
            // instead of throwing and being dropped by Profile.ReadGumps.
            _rows = int.TryParse(xml.GetAttribute("rows"), out int rows) ? ClampDimension(rows) : MIN_DIMENSION;
            _columns = int.TryParse(xml.GetAttribute("columns"), out int columns) ? ClampDimension(columns) : MIN_DIMENSION;
            _rectSize = int.TryParse(xml.GetAttribute("rectsize"), out int rectSize) ? ClampCellSize(rectSize) : MIN_CELL_SIZE;

            BuildGump();

            XmlElement controlsXml = xml["controls"];

            if (controlsXml != null)
            {
                ActionItem[] items = GetControls<ActionItem>();
                int index = 0;

                foreach (XmlElement controlXml in controlsXml.GetElementsByTagName("control"))
                {
                    if (index < items.Length)
                    {
                        CounterBarSlot slot = BarXml.ReadSlot(controlXml);

                        if (slot != null && !slot.IsEmpty)
                        {
                            items[index]?.SetSlot(slot);
                        }
                        else if (
                            ushort.TryParse(controlXml.GetAttribute("graphic"), out ushort graphic)
                            && ushort.TryParse(controlXml.GetAttribute("hue"), out ushort hue)
                        )
                        {
                            items[index]?.SetGraphic(graphic, hue);
                        }
                        else
                        {
                            // Leave the cell empty rather than aborting the whole bar's restore.
                            Log.Error($"Malformed action bar cell at index {index}; leaving it empty.");
                        }

                        items[index]?.SetCellColor(BarXml.ReadCellColor(controlXml));

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

            RefreshHotkeyLabels();
        }

        protected override void OnLockedChanged()
        {
            base.OnLockedChanged();
            CanCloseWithRightClick = false;
        }

        public override void Dispose()
        {
            ClearBarHotkeys();
            base.Dispose();
        }

        /// <summary>An action-bar cell: a plain item counter or an action, with bar-management entries added to its context menu.</summary>
        public class ActionItem : BarCell
        {
            private readonly ActionBarGump _gump;

            public ActionItem(ActionBarGump gump, int x, int y, int w, int h) : base(gump, x, y, w, h)
            {
                _gump = gump;

                var sizeMenu = new ContextMenuItemEntry(TazLang.Get("actionbar_size", "Size"));
                sizeMenu.Add(new ContextMenuItemEntry(TazLang.Get("actionbar_addrow", "Add row"), _gump.AddRow));
                sizeMenu.Add(new ContextMenuItemEntry(TazLang.Get("actionbar_addcolumn", "Add column"), _gump.AddColumn));
                sizeMenu.Add(new ContextMenuItemEntry(TazLang.Get("actionbar_removerow", "Remove row"), _gump.RemoveRow));
                sizeMenu.Add(new ContextMenuItemEntry(TazLang.Get("actionbar_removecolumn", "Remove column"), _gump.RemoveColumn));
                sizeMenu.Add(new ContextMenuItemEntry(TazLang.Get("actionbar_setcellsize", "Set Cell Size"), SetCellSize));

                // Bar-management entries fold into the shared Options submenu, ahead of Set hotkey.
                OptionsMenu.Items.Insert(0, sizeMenu);
                OptionsMenu.Add(new ContextMenuItemEntry(TazLang.Get("actionbar_rename", "Rename action bar"), Rename));
                OptionsMenu.Add(new ContextMenuItemEntry(TazLang.Get("actionbar_delete", "Delete action bar"), () => _gump.Dispose()));
            }

            private void SetCellSize()
            {
                new PromptPopupWindow(
                    TazLang.Get("actionbar_setcellsize", "Set Cell Size"),
                    TazLang.Get("actionbar_setcellsizeprompt", "New cell size:"),
                    OnCellSizeEntered,
                    TazLang.Get("spellbar_save", "Save"),
                    TazLang.Get("uicommons_cancel", "Cancel"),
                    null,
                    _gump.CellSize.ToString()
                );
            }

            private void OnCellSizeEntered(string text)
            {
                if (int.TryParse(text, out int size))
                    _gump.SetCellSize(size);
            }

            private void Rename()
            {
                new PromptPopupWindow(
                    TazLang.Get("actionbar_rename", "Rename action bar"),
                    TazLang.Get("actionbar_renameprompt", "New name:"),
                    _gump.SetName,
                    TazLang.Get("spellbar_save", "Save"),
                    TazLang.Get("uicommons_cancel", "Cancel"),
                    null,
                    _gump.Name
                );
            }
        }
    }
}
