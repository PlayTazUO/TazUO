// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Managers.Hotkeys;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps.SpellBar;
using ClassicUO.Game.UI.MyraWindows;
using ClassicUO.Input;
using ClassicUO.LegionScripting;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.UI.Gumps
{
    /// <summary>
    /// The owning gump of a set of <see cref="BarCell"/>s: supplies the world, the cell's position and
    /// its hotkey binding, so one cell implementation can serve the counter bar and every action bar.
    /// </summary>
    public interface IBarCellHost
    {
        /// <summary>The owning gump (used as the context-menu owner and world source).</summary>
        Gump OwnerGump { get; }

        /// <summary>Position of <paramref name="cell"/> among the gump's cells, matching save order and hotkey ids.</summary>
        int IndexOf(BarCell cell);

        /// <summary>Current binding for the cell at <paramref name="index"/>, or an empty binding when unset.</summary>
        HotkeyBinding GetCellHotkey(int index);

        /// <summary>Registers (or clears) the hotkey binding for the cell at <paramref name="index"/>.</summary>
        void SetCellHotkey(int index, HotkeyBinding binding);
    }

    /// <summary>
    /// A single slot shared by the counter bar and the action bars: shows an item counter or an action
    /// icon, and handles use, drag-drop, its context menu and its hotkey. The owning gump supplies the
    /// world/hotkey plumbing through <see cref="IBarCellHost"/> and may add its own context-menu items.
    /// </summary>
    public class BarCell : Control
    {
        private int _amount;
        private readonly ImageWithText _image;
        private uint _time;
        private const uint HIGHLIGHT_DURATION = 1000;
        private const uint HOTKEY_FLASH_DURATION = 500;
        private const ushort SCRIPT_RUNNING_HUE = 0x0044;
        private uint _endHighlight;
        private uint _hotkeyFlashEnd;
        private bool _highlight;
        private readonly Gump _gump;
        private readonly IBarCellHost _host;
        private readonly Label _hotkeyLabel;

        private CounterBarSlot _slot = CounterBarSlot.Empty();
        private ContextMenuItemEntry _macroMenu;
        private ContextMenuItemEntry _scriptMenu;
        private ContextMenuItemEntry _dressAgentMenu;

        public BarCell(IBarCellHost host, int x, int y, int w, int h)
        {
            _host = host;
            _gump = host.OwnerGump;

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
            ContextMenu.Add(TazLang.Get("use_object"), Use);
            ContextMenu.Add(TazLang.Get("remove"), RemoveItem);
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

            _dressAgentMenu = new ContextMenuItemEntry(TazLang.Get("counterbar_setdressagent", "Set dress agent"));
            GenDressAgentList(_dressAgentMenu);
            ContextMenu.Add(_dressAgentMenu);

            ContextMenu.Add(new ContextMenuItemEntry(TazLang.Get("counterbar_sethotkey"), SetHotkey));
        }

        public ushort Graphic { get; private set; }

        public ushort Hue { get; private set; }

        /// <summary>The action assigned to this cell, or an empty slot for a plain item counter.</summary>
        public CounterBarSlot Slot => _slot;

        /// <summary>True when this cell holds an action instead of an item to count.</summary>
        public bool HasAction => _slot != null && !_slot.IsEmpty;

        public void SetGraphic(ushort graphic, ushort hue, bool isGumpIcon = false)
        {
            _image.ChangeGraphic(graphic, hue, isGumpIcon);

            if (graphic == 0)
                return;

            Graphic = graphic;
            Hue = hue;
        }

        /// <summary>Assigns an action to this cell (or clears it when the slot is empty) and refreshes its icon/label/tooltip.</summary>
        public void SetSlot(CounterBarSlot slot)
        {
            _slot = slot ?? CounterBarSlot.Empty();

            // An action slot replaces any item-counting graphic on this cell.
            _amount = 0;
            Graphic = 0;
            Hue = 0;

            RefreshSlotVisual();
        }

        /// <summary>Re-applies the cell's visual after its size changed, preserving action or item icon.</summary>
        public void OnCellResized()
        {
            if (HasAction)
                RefreshSlotVisual();
            else
                SetGraphic(Graphic, Hue);
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
                // Icon-less actions (and graphic-less macros) fall back to a short text label.
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
                return;

            Item backpack = _gump.World.Player.Backpack;

            if (backpack == null)
                return;

            Item item = backpack.FindItem(Graphic, Hue);

            if (item != null)
                GameActions.DoubleClick(_gump.World, item);
        }

        /// <summary>Opens the shared hotkey capture window to bind (or clear) a hotkey that triggers this cell.</summary>
        private void SetHotkey()
        {
            int index = _host.IndexOf(this);
            if (index < 0)
                return;

            // The universal capture window adds itself to the UI and commits on Save; the binding is
            // registered with the central hotkey system and persisted with the cell on the next gump save.
            _ = new HotkeyCaptureWindow(
                prompt: TazLang.Get("counterbar_slot", new[] { (index + 1).ToString() }),
                existing: _host.GetCellHotkey(index),
                onSaved: binding =>
                {
                    _host.SetCellHotkey(index, binding);
                    UpdateHotkeyLabel();
                });
        }

        /// <summary>Refreshes the optional keybind label from the cell's current binding and the profile toggle.</summary>
        public void UpdateHotkeyLabel()
        {
            if (_hotkeyLabel == null)
                return;

            int index = _host.IndexOf(this);
            HotkeyBinding binding = index >= 0 ? _host.GetCellHotkey(index) : null;
            bool hasBinding = binding is { IsEmpty: false };

            // The keybind heads the cell's tooltip regardless of the on-cell label toggle.
            TooltipPrefix = hasBinding ? $"[ {binding.Describe()} ]" : null;

            if (ProfileManager.CurrentProfile.CounterBarShowHotkeys && hasBinding)
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

            void AddSchool(string label, IEnumerable<SpellDefinition> spells)
            {
                var entry = new ContextMenuItemEntry(label);
                foreach (SpellDefinition spell in spells)
                    entry.Add(new ContextMenuItemEntry(spell.GetLocalizedName(), () => SetSlot(CounterBarSlot.FromSpell(spell))));
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

        private void GenDressAgentList(ContextMenuItemEntry parent)
        {
            if (parent == null)
                return;

            parent.Items.Clear();

            foreach (DressConfig config in DressAgentManager.Instance.CurrentPlayerConfigs)
            {
                if (config == null)
                    continue;

                DressConfig selectedConfig = config;
                var configMenu = new ContextMenuItemEntry(selectedConfig.Name ?? string.Empty);
                configMenu.Add(new ContextMenuItemEntry(
                    TazLang.Get("dressagent_dress", "Dress"),
                    () => SetSlot(CounterBarSlot.FromDressAgent(selectedConfig, false))));
                configMenu.Add(new ContextMenuItemEntry(
                    TazLang.Get("dressagent_undress", "Undress"),
                    () => SetSlot(CounterBarSlot.FromDressAgent(selectedConfig, true))));
                parent.Add(configMenu);
            }
        }

        /// <summary>Ctrl+Alt+click toggles the owning gump's lock. Overridable so a host can also persist the state.</summary>
        protected virtual void ToggleLock(Gump gump) => gump.IsLocked = !gump.IsLocked;

        public override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Right)
            {
                // Refresh the dynamic lists so newly added macros, scripts, and dress configs appear.
                GenMacroList(_macroMenu);
                GenScriptList(_scriptMenu);
                GenDressAgentList(_dressAgentMenu);
            }

            if (button == MouseButtonType.Left)
            {
                if (Keyboard.Alt && Keyboard.Ctrl && Parent is Gump pg)
                    ToggleLock(pg);

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
                else if (ProfileManager.GlobalSettings.SingleClickIconUse)
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
            if (button == MouseButtonType.Left && !ProfileManager.GlobalSettings.SingleClickIconUse)
                Use();

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
                        if (_amount >= ProfileManager.CurrentProfile.CounterBarAbbreviatedAmount)
                        {
                            _image.SetAmount(StringHelper.IntToAbbreviatedString(_amount));
                            return;
                        }
                    }

                    if (ProfileManager.CurrentProfile.CounterBarHighlightOnUse)
                    {
                        if (int.TryParse(_image.GetText(), out int cAmt) && cAmt > _amount)
                        {
                            _highlight = true;
                            _endHighlight = Time.Ticks + HIGHLIGHT_DURATION;
                        }
                    }

                    _image.SetAmount(_amount.ToString());
                }
            }
        }

        private void GetAmount(Item parent, ushort graphic, ushort hue, ref int amount)
        {
            if (parent == null)
                return;

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
                _highlight = false;

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

                    if (artInfo.Texture == null)
                        return base.Draw(batcher, x, y);

                    Rectangle rect = _isGumpGraphic ? artInfo.UV : Client.Game.UO.Arts.GetRealArtBounds(_graphic);

                    Vector3 hueVector = ShaderHueTranslator.GetHueVector(_hue, _partial, 1f, _isGumpGraphic);

                    // Scale the icon to fill the cell while preserving its aspect ratio, then center it.
                    // Scaling can be disabled per graphic kind (spell/ability icons vs item counters).
                    var originalSize = new Point(Width, Height);
                    var point = new Point();

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
                            new Rectangle(artInfo.UV.X, artInfo.UV.Y, rect.Width, rect.Height),
                            hueVector
                        );
                    else
                        batcher.Draw(
                            artInfo.Texture,
                            new Rectangle(x + point.X, y + point.Y, originalSize.X, originalSize.Y),
                            new Rectangle(artInfo.UV.X + rect.X, artInfo.UV.Y + rect.Y, rect.Width, rect.Height),
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
