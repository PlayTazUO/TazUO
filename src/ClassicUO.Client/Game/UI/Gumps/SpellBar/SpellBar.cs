using System;
using System.Collections.Generic;
using System.Text;
using ClassicUO.Assets;
using ClassicUO.LegionScripting;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Managers.SpellVisualRange;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.MyraWindows;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps.SpellBar;

public class SpellBar : Gump
{
    public static SpellBar Instance { get; private set; }

    private SpellEntry[] spellEntries = new SpellEntry[10];
    private TextBox rowLabel;
    private AlphaBlendControl background;

    public SpellBar(World world) : base(world, 0, 0)
    {
        Instance?.Dispose();
        Instance = this;
        CanMove = true;
        CanCloseWithRightClick = false;
        CanCloseWithEsc = false;
        AcceptMouseInput = true;

        Width = 515;
        Height = 48;

        CenterXInViewPort();
        CenterYInViewPort();

        Build();

        EventSink.SpellCastBegin += EventSinkOnSpellCastBegin;
        ClassicUO.LegionScripting.LegionScripting.ScriptStarted += OnScriptStarted;
        ClassicUO.LegionScripting.LegionScripting.ScriptStopped += OnScriptStopped;
    }

    private void EventSinkOnSpellCastBegin(object sender, int e)
    {
        foreach (SpellEntry entry in spellEntries)
            if (entry.CurrentSpellID == e)
                entry.BeginTrackingCasting();
    }

    private void OnScriptStarted(object sender, ScriptFile e) => UpdateScriptHighlights(e, true);
    private void OnScriptStopped(object sender, ScriptFile e) => UpdateScriptHighlights(e, false);

    private void UpdateScriptHighlights(ScriptFile script, bool running)
    {
        if (script == null)
            return;

        foreach (SpellEntry entry in spellEntries)
            entry.SetScriptRunning(script.RelativePath, running);
    }

    public override GumpType GumpType { get; } = GumpType.SpellBar;

    public override void OnMouseWheel(MouseEventType delta)
    {
        base.OnMouseWheel(delta);

        if (delta == MouseEventType.WheelScrollDown)
            ChangeRow(true);
        else
            ChangeRow(false);
    }

    public void SetRow(int row)
    {
        SpellBarManager.CurrentRow = row;

        if (SpellBarManager.CurrentRow < 0)
            SpellBarManager.CurrentRow = SpellBarManager.SpellBarRows.Count - 1;

        if (SpellBarManager.CurrentRow >= SpellBarManager.SpellBarRows.Count)
            SpellBarManager.CurrentRow = 0;

        rowLabel.SetText(SpellBarManager.CurrentRow.ToString());

        for (int s = 0; s < spellEntries.Length; s++) spellEntries[s].SetSlot(SpellBarManager.GetSlot(SpellBarManager.CurrentRow, s), SpellBarManager.CurrentRow, s);

        background.Hue = SpellBarManager.SpellBarRows[SpellBarManager.CurrentRow].RowHue;
    }

    public void ChangeRow(bool up)
    {
        if (up)
            SetRow(SpellBarManager.CurrentRow - 1);
        else
            SetRow(SpellBarManager.CurrentRow + 1);
    }

    public void Build()
    {
        Clear();

        Add(background = new AlphaBlendControl() { Width = Width, Height = Height });

        int x = 2;

        if (SpellBarManager.CurrentRow > SpellBarManager.SpellBarRows.Count - 1)
            SpellBarManager.CurrentRow = SpellBarManager.SpellBarRows.Count - 1;

        background.Hue = SpellBarManager.SpellBarRows[SpellBarManager.CurrentRow].RowHue;

        for (int i = 0; i < spellEntries.Length; i++)
        {
            Add(spellEntries[i] = new SpellEntry(World, this).SetSlot(SpellBarManager.GetSlot(SpellBarManager.CurrentRow, i), SpellBarManager.CurrentRow, i));
            spellEntries[i].X = x;
            spellEntries[i].Y = 1;
            x += 46 + 2;
        }

        rowLabel = TextBox.GetOne(SpellBarManager.CurrentRow.ToString(), TrueTypeLoader.EMBEDDED_FONT, 12, Color.White, TextBox.RTLOptions.DefaultCentered(16));
        rowLabel.X = 482;
        rowLabel.Y = (Height - rowLabel.Height) >> 1;
        Add(rowLabel);

        PNGLoader.Instance.TryGetEmbeddedTexture("upicon.png", out Microsoft.Xna.Framework.Graphics.Texture2D upTexture);
        var up = new EmbeddedGumpPic(Width - 31, 0, upTexture, 148);
        up.MouseUp += (sender, e) => { ChangeRow(false); };
        PNGLoader.Instance.TryGetEmbeddedTexture("downicon.png", out Microsoft.Xna.Framework.Graphics.Texture2D downTexture);
        var down = new EmbeddedGumpPic(Width - 31, Height - 16, downTexture, 148);
        down.MouseUp += (sender, e) => { ChangeRow(true); };

        Add(up);
        Add(down);

        NiceButton menu = new(Width - 15, 0, 15, Height, ButtonAction.Default, "+");

        ContextMenuItemEntry import = new(TazLang.Get("spellbar_importpreset"));

        menu.MouseUp += (sender, e) =>
        {
            if (e.Button == MouseButtonType.Left)
            {
                import.Items.Clear();
                GenAvailablePresets(import);
                menu.ContextMenu?.Show();
            }
        };

        menu.ContextMenu = new ContextMenuControl(this);
        menu.ContextMenu.Add(new ContextMenuItemEntry(TazLang.Get("spellbar_savepreset"), () =>
        {
            new PromptPopupWindow(TazLang.Get("spellbar_savepreset_title"), TazLang.Get("spellbar_savepreset_name"), n => SpellBarManager.SaveCurrentRowPreset(n), TazLang.Get("spellbar_save"), TazLang.Get("spellbar_cancel"));
        }));
        menu.ContextMenu.Add(import);
        menu.ContextMenu.Add(new ContextMenuItemEntry(TazLang.Get("spellbar_lockmovement"), (() =>
        {
            IsLocked = !IsLocked;
        })));
        menu.ContextMenu.Add(new ContextMenuItemEntry(TazLang.Get("spellbar_addrow"), () =>
        {
            SpellBarManager.SpellBarRows.Add(new SpellBarRow());
            SpellBarManager.CurrentRow = SpellBarManager.SpellBarRows.Count - 1;
            Build();
        }));
        menu.ContextMenu.Add(new ContextMenuItemEntry(TazLang.Get("spellbar_deleterow"), () =>
        {
            if (SpellBarManager.SpellBarRows.Count > 1)
            {
                SpellBarManager.SpellBarRows.RemoveAt(SpellBarManager.CurrentRow);
                SpellBarManager.CurrentRow = Math.Max(0, SpellBarManager.CurrentRow - 1);
                Build();
            }
        }));
        menu.ContextMenu.Add(new ContextMenuItemEntry(TazLang.Get("spellbar_setrowcolor"), () =>
        {
            UIManager.Add(new ModernColorPicker(World, (h) =>
            {
                SpellBarManager.SpellBarRows[SpellBarManager.CurrentRow].RowHue = h;
                Build();
            }));
        }));
        menu.ContextMenu.Add(new ContextMenuItemEntry(TazLang.Get("spellbar_moreoptions"), AssistantWindow.Show));

        Add(menu);
    }

    private static void GenAvailablePresets(ContextMenuItemEntry par)
    {
        if (par == null)
            return;

        foreach (string preset in SpellBarManager.ListPresets())
            par.Add(new ContextMenuItemEntry(preset, () =>
            {
                SpellBarManager.ImportPreset(preset);
            }));
    }

    public override void OnMouseUp(int x, int y, MouseButtonType button)
    {
        base.OnMouseUp(x, y, button);

        if (button == MouseButtonType.Left && Keyboard.Alt && UIManager.MouseOverControl != null && (UIManager.MouseOverControl == this || UIManager.MouseOverControl.RootParent == this))
        {
            ref readonly SpriteInfo texture = ref Client.Game.UO.Gumps.GetGump(0x82C);
            if (texture.Texture != null)
                if (x >= 0 && x <= texture.UV.Width && y >= 0 && y <= texture.UV.Height)
                    IsLocked = !IsLocked;
        }
    }

    public void SetupHotkeyLabels()
    {
        foreach (SpellEntry entry in spellEntries) entry.BuildHotkeyLabel();
    }

    public override void Dispose()
    {
        base.Dispose();
        EventSink.SpellCastBegin -= EventSinkOnSpellCastBegin;
        ClassicUO.LegionScripting.LegionScripting.ScriptStarted -= OnScriptStarted;
        ClassicUO.LegionScripting.LegionScripting.ScriptStopped -= OnScriptStopped;
    }

    public override bool Draw(UltimaBatcher2D batcher, int x, int y)
    {
        if (!base.Draw(batcher, x, y))
            return false;

        if (Keyboard.Alt)
        {
            Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);

            ref readonly SpriteInfo texture = ref Client.Game.UO.Gumps.GetGump(0x82C);

            if (texture.Texture != null)
            {
                if (IsLocked)
                {
                    hueVector.X = 34;
                    hueVector.Y = 1;
                }
                batcher.Draw
                (
                    texture.Texture,
                    new Vector2(x, y),
                    texture.UV,
                    hueVector
                );
            }
        }

        return true;
    }

    public class SpellEntry : Control
    {
        /// <summary>Spell id of this entry's slot for spell slots, or -1 for empty/macro/ability slots.</summary>
        public int CurrentSpellID => slot?.CurrentSpellID ?? -1;

        private GumpPic icon;
        private SpellBarSlot slot;
        private AlphaBlendControl background;
        private int row, col;
        private bool trackCasting;
        private bool scriptRunning;
        private World World;
        private Gump parentGump;
        private TextBox hotkeyLabel;
        private TextBox macroLabel;
        private ContextMenuItemEntry macroMenu;
        private ContextMenuItemEntry scriptMenu;
        private Microsoft.Xna.Framework.Graphics.Texture2D castingTexture = SolidColorTextureCache.GetTexture(Color.Black);
        private DateTime savedStateTime;

        public SpellEntry(World world, Gump parent)
        {
            CanMove = true;
            CanCloseWithRightClick = false;
            CanCloseWithEsc = false;
            AcceptMouseInput = true;
            Width = 46;
            Height = 46;
            World = world;
            parentGump = parent;
            Build();
        }

        /// <summary>Assigns the given slot to this entry at the given row/column and refreshes its icon, tooltip, and hotkey label.</summary>
        public SpellEntry SetSlot(SpellBarSlot slot, int row, int col)
        {
            this.slot = slot ?? SpellBarSlot.Empty();
            this.row = row;
            this.col = col;
            background.Hue = SpellBarManager.SpellBarRows[row].RowHue;
            SpellBarManager.SpellBarRows[row].Slots[col] = this.slot;

            icon.Hue = 0; // Draw() re-applies the active highlight for ability slots
            SetMacroLabel();

            // Seed the script running-highlight once at assignment (events keep it updated thereafter).
            scriptRunning = this.slot.IsScriptRunning;
            ApplyScriptHighlight();

            if (!this.slot.IsEmpty)
            {
                ushort graphic = this.slot.GetIconGraphic(World);
                if (graphic != 0)
                {
                    icon.Graphic = graphic;
                    icon.IsVisible = true;
                }
                else
                {
                    icon.IsVisible = false;
                }

                if (this.slot.TryGetTooltip(World, out string tip))
                    SetTooltip(tip, 80);
                else
                    SetTooltip(string.Empty);
            }
            else
            {
                SetTooltip(TazLang.Get("spellbar_rightclicktoset"));
                icon.IsVisible = false;
            }

            SetHotkeyText(col);

            return this;
        }

        /// <summary>Shows an abbreviation (capital letters) of the macro or script name inside the slot for macro/script slots, hidden otherwise.</summary>
        private void SetMacroLabel()
        {
            if (macroLabel == null)
                return;

            string name =
                slot != null && slot.Type == SpellBarSlotType.Macro ? slot.MacroName :
                slot != null && slot.Type == SpellBarSlotType.Script ? slot.ScriptDisplayName :
                slot != null && slot.Type == SpellBarSlotType.Skill ? slot.SkillDisplayName :
                null;

            if (!string.IsNullOrEmpty(name))
            {
                macroLabel.SetText(AbbreviateMacroName(name));
                macroLabel.Y = (Height - macroLabel.Height) >> 1;
                macroLabel.IsVisible = true;
            }
            else
            {
                macroLabel.SetText(string.Empty);
                macroLabel.IsVisible = false;
            }
        }

        /// <summary>Builds a short label from a macro name using its capital letters (e.g. "Last Object Macro" -> "LOM").</summary>
        private static string AbbreviateMacroName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return string.Empty;

            var sb = new StringBuilder();
            foreach (char c in name)
                if (char.IsUpper(c))
                    sb.Append(c);

            // Fall back to the first letter of each word when the name has no capitals.
            if (sb.Length == 0)
                foreach (string part in name.Split(new[] { ' ', '_', '-' }, StringSplitOptions.RemoveEmptyEntries))
                    sb.Append(char.ToUpperInvariant(part[0]));

            return sb.Length > 0 ? sb.ToString() : name.ToUpperInvariant();
        }

        private void SetHotkeyText(int slotIndex)
        {
            if (!ProfileManager.CurrentProfile.SpellBar_ShowHotkeys) return;
            if (hotkeyLabel == null) return;
            if (slot == null || slot.IsEmpty)
            {
                hotkeyLabel.SetText(string.Empty);
                return;
            }

            string keys = SpellBarManager.GetKetNames(slotIndex);
            if (string.IsNullOrEmpty(keys))
                keys = SpellBarManager.GetControllerButtonsName(slotIndex);

            hotkeyLabel.SetText(keys);
        }

        /// <summary>
        /// Only call this when you're sure it's being cast.
        /// </summary>
        public void BeginTrackingCasting()
        {
            savedStateTime = DateTime.Now;
            trackCasting = true;
        }

        /// <summary>Triggers this entry's slot action (cast spell, run macro, or use ability) when it is not empty.</summary>
        public void Activate()
        {
            if (slot != null && !slot.IsEmpty) slot.Activate(World);
        }

        public override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            base.OnMouseUp(x, y, button);
            if (button == MouseButtonType.Right)
            {
                GenMacroList(macroMenu);
                GenScriptList(scriptMenu);
                ContextMenu?.Show();
            }

            if (button == MouseButtonType.Left && !Keyboard.Alt && !Keyboard.Ctrl) Activate();
        }

        public void BuildHotkeyLabel()
        {
            hotkeyLabel?.Dispose();
            if (ProfileManager.CurrentProfile.SpellBar_ShowHotkeys)
            {
                Add(hotkeyLabel = TextBox.GetOne(string.Empty, "uo-unicode-1", 18, Color.White, TextBox.RTLOptions.DefaultCenterStroked(44))); ;
                hotkeyLabel.Y = 46;
                SetHotkeyText(col);
            }
        }

        private void Build()
        {
            Add(background = new AlphaBlendControl() { Width = 44, Height = 44, X = 1, Y = 1 });
            Add(icon = new GumpPic(1, 1, 0x5000, 0) { IsVisible = false, AcceptMouseInput = false });
            Add(macroLabel = TextBox.GetOne(string.Empty, "uo-unicode-1", 18, Color.White, TextBox.RTLOptions.DefaultCenterStroked(44)));
            macroLabel.AcceptMouseInput = false;
            macroLabel.IsVisible = false;
            BuildHotkeyLabel();

            ContextMenu = new(parentGump);
            ContextMenu.Add(TazLang.Get("spellbar_setspell"), GenSpellList());
            ContextMenu.Add(new ContextMenuItemEntry(TazLang.Get("spellbar_quicksetspell"), () =>
            {
                UIManager.Add
                (
                    new SpellQuickSearch
                    (World,
                        ScreenCoordinateX - 20, ScreenCoordinateY - 90, (s) =>
                        {
                            SetSlot(SpellBarSlot.FromSpell(s), row, col);
                        }, true
                    )
                );
            }));

            macroMenu = new ContextMenuItemEntry(TazLang.Get("spellbar_setmacro"));
            GenMacroList(macroMenu);
            ContextMenu.Add(macroMenu);

            var abilityMenu = new ContextMenuItemEntry(TazLang.Get("spellbar_setability"));
            abilityMenu.Add(new ContextMenuItemEntry(TazLang.Get("spellbar_ability_primary"), () =>
            {
                SetSlot(SpellBarSlot.FromAbility(true), row, col);
            }));
            abilityMenu.Add(new ContextMenuItemEntry(TazLang.Get("spellbar_ability_secondary"), () =>
            {
                SetSlot(SpellBarSlot.FromAbility(false), row, col);
            }));
            ContextMenu.Add(abilityMenu);

            scriptMenu = new ContextMenuItemEntry(TazLang.Get("spellbar_setscript"));
            GenScriptList(scriptMenu);
            ContextMenu.Add(scriptMenu);

            var skillMenu = new ContextMenuItemEntry(TazLang.Get("spellbar_setskill"));
            GenSkillList(skillMenu);
            ContextMenu.Add(skillMenu);

            ContextMenu.Add(new ContextMenuItemEntry(TazLang.Get("spellbar_clear"), () =>
            {
                SetSlot(SpellBarSlot.Empty(), row, col);
            }));
        }

        private void GenMacroList(ContextMenuItemEntry parent)
        {
            if (parent == null)
                return;

            parent.Items.Clear();

            foreach (Macro macro in World.Macros.GetAllMacros())
                parent.Add(new ContextMenuItemEntry(macro.Name, () =>
                {
                    SetSlot(SpellBarSlot.FromMacro(macro), row, col);
                }));
        }

        private void GenScriptList(ContextMenuItemEntry parent)
        {
            if (parent == null)
                return;

            parent.Items.Clear();

            foreach (ScriptFile s in ClassicUO.LegionScripting.LegionScripting.LoadedScripts)
            {
                ScriptFile script = s;
                // RelativePath (e.g. "group/loot.py") so same-named scripts in different groups are distinguishable.
                parent.Add(new ContextMenuItemEntry(script.RelativePath, () =>
                {
                    SetSlot(SpellBarSlot.FromScript(script), row, col);
                }));
            }
        }

        private void GenSkillList(ContextMenuItemEntry parent)
        {
            if (parent == null)
                return;

            parent.Items.Clear();

            // Only skills that have a usable action (HasAction) can be invoked from the spell bar.
            foreach (var skill in Client.Game.UO.FileManager.Skills.SortedSkills)
            {
                if (!skill.HasAction)
                    continue;

                int index = skill.Index;
                parent.Add(new ContextMenuItemEntry(skill.Name, () =>
                {
                    SetSlot(SpellBarSlot.FromSkill(index), row, col);
                }));
            }
        }

        private List<ContextMenuItemEntry> GenSpellList()
            {
                var list = new List<ContextMenuItemEntry>();

                var entry = new ContextMenuItemEntry(TazLang.Get("spellschool_magery"));
                foreach (SpellDefinition spell in SpellsMagery.GetAllSpells.Values)
                    entry.Add(new ContextMenuItemEntry(spell.Name, () =>
                    {
                        SetSlot(SpellBarSlot.FromSpell(spell), row, col);
                    }));
                list.Add(entry);


                entry = new ContextMenuItemEntry(TazLang.Get("spellschool_necromancy"));
                foreach (SpellDefinition spell in SpellsNecromancy.GetAllSpells.Values)
                    entry.Add(new ContextMenuItemEntry(spell.Name, () =>
                    {
                        SetSlot(SpellBarSlot.FromSpell(spell), row, col);
                    }));
                list.Add(entry);


                entry = new ContextMenuItemEntry(TazLang.Get("spellschool_chivalry"));
                foreach (SpellDefinition spell in SpellsChivalry.GetAllSpells.Values)
                    entry.Add(new ContextMenuItemEntry(spell.Name, () =>
                    {
                        SetSlot(SpellBarSlot.FromSpell(spell), row, col);
                    }));
                list.Add(entry);


                entry = new ContextMenuItemEntry(TazLang.Get("spellschool_bushido"));
                foreach (SpellDefinition spell in SpellsBushido.GetAllSpells.Values)
                    entry.Add(new ContextMenuItemEntry(spell.Name, () =>
                    {
                        SetSlot(SpellBarSlot.FromSpell(spell), row, col);
                    }));
                list.Add(entry);


                entry = new ContextMenuItemEntry(TazLang.Get("spellschool_ninjitsu"));
                foreach (SpellDefinition spell in SpellsNinjitsu.GetAllSpells.Values)
                    entry.Add(new ContextMenuItemEntry(spell.Name, () =>
                    {
                        SetSlot(SpellBarSlot.FromSpell(spell), row, col);
                    }));
                list.Add(entry);


                entry = new ContextMenuItemEntry(TazLang.Get("spellschool_spellweaving"));
                foreach (SpellDefinition spell in SpellsSpellweaving.GetAllSpells.Values)
                    entry.Add(new ContextMenuItemEntry(spell.Name, () =>
                    {
                        SetSlot(SpellBarSlot.FromSpell(spell), row, col);
                    }));
                list.Add(entry);


                entry = new ContextMenuItemEntry(TazLang.Get("spellschool_mysticism"));
                foreach (SpellDefinition spell in SpellsMysticism.GetAllSpells.Values)
                    entry.Add(new ContextMenuItemEntry(spell.Name, () =>
                    {
                        SetSlot(SpellBarSlot.FromSpell(spell), row, col);
                    }));
                list.Add(entry);


                entry = new ContextMenuItemEntry(TazLang.Get("spellschool_mastery"));
                foreach (SpellDefinition spell in SpellsMastery.GetAllSpells.Values)
                    entry.Add(new ContextMenuItemEntry(spell.Name, () =>
                    {
                        SetSlot(SpellBarSlot.FromSpell(spell), row, col);
                    }));
                list.Add(entry);
                return list;
            }

        private void resetStates() => trackCasting = false;

        /// <summary>
        /// Updates this slot's running highlight when its script starts/stops. Driven by
        /// LegionScripting's ScriptStarted/ScriptStopped events, so there is no per-frame scan.
        /// </summary>
        public void SetScriptRunning(string scriptId, bool running)
        {
            if (slot == null || slot.Type != SpellBarSlotType.Script || slot.ScriptId != scriptId)
                return;

            scriptRunning = running;
            ApplyScriptHighlight();
        }

        private void ApplyScriptHighlight()
        {
            const ushort runningHue = 0x0044; // green tint while the script runs
            background.Hue = scriptRunning ? runningHue : SpellBarManager.SpellBarRows[row].RowHue;
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (slot != null && slot.Type == SpellBarSlotType.Ability)
            {
                // The active primary/secondary ability follows the equipped weapon, so keep icon/hue/tooltip in sync.
                ushort graphic = slot.GetIconGraphic(World);
                if (graphic != 0)
                {
                    if (icon.Graphic != graphic)
                    {
                        icon.Graphic = graphic;
                        if (slot.TryGetTooltip(World, out string tip))
                            SetTooltip(tip, 80);
                        else
                            SetTooltip(string.Empty);
                    }
                    icon.IsVisible = true;

                    bool active = ((byte)World.Player.Abilities[slot.AbilityPrimary ? 0 : 1] & 0x80) != 0;
                    ushort wanted = (ushort)(active ? 38 : 0);
                    if (icon.Hue != wanted)
                        icon.Hue = wanted;
                }
                else if (icon.IsVisible)
                {
                    icon.IsVisible = false;
                    SetTooltip(string.Empty);
                }
            }

            if (!base.Draw(batcher, x, y))
                return false;

            if (!trackCasting)
                return true;

            if (!SpellVisualRangeManager.Instance.IsCastingWithoutTarget())
            {
                resetStates();
                return true;
            }

            SpellRangeInfo i = SpellVisualRangeManager.Instance.GetCurrentSpell();
            if (i == null)
            {
                resetStates();
                return true;
            }

            double castTime = i.GetEffectiveCastTime();
            if (castTime > 0)
            {
                double percent = (DateTime.Now - savedStateTime).TotalSeconds / castTime;
                if (percent < 0)
                    percent = 0;

                if (percent > 1)
                    percent = 1;

                int filledHeight = (int)(Height * percent);
                int yb = Height - filledHeight; // This shifts the rect up as it grows

                Rectangle rect = new(x, y + yb, Width, filledHeight);
                batcher.Draw(castingTexture, rect, new Vector3(0, 0, 0.65f));
            }
            else
                resetStates();


            return true;
        }
    }
}
