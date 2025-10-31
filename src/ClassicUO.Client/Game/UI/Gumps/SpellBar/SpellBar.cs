using System;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Managers.SpellVisualRange;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.ImGuiControls;
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
        EventSink.SpellRecoveryBegin += EventSinkOnSpellRecoveryBegin;
    }

    private void EventSinkOnSpellCastBegin(object sender, int e)
    {
        foreach (SpellEntry entry in spellEntries)
        {
            if (entry.CurrentSpellID == e)
            {
                entry.BeginTrackingCasting();
            }
        }
    }

    private void EventSinkOnSpellRecoveryBegin(object sender, int e)
    {
        foreach (SpellEntry entry in spellEntries)
        {
            if (entry.CurrentSpellID == e)
            {
                entry.BeginTrackingRecovery();
            }
        }
    }

    public override GumpType GumpType { get; } = GumpType.SpellBar;

    protected override void OnMouseWheel(MouseEventType delta)
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

        for (int s = 0; s < spellEntries.Length; s++)
        {
            spellEntries[s].SetSpell(SpellBarManager.GetSpell(SpellBarManager.CurrentRow, s), SpellBarManager.CurrentRow, s);
        }

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
            Add(spellEntries[i] = new SpellEntry(World, this).SetSpell(SpellBarManager.GetSpell(SpellBarManager.CurrentRow, i), SpellBarManager.CurrentRow, i));
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

        ContextMenuItemEntry import = new("Import preset");

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
        menu.ContextMenu.Add(new ContextMenuItemEntry("Save preset", () =>
        {
            Gump g;
            UIManager.Add(g = new InputRequest(World, "Preset name", "Save", "Cancel", (r, n) =>
            {
                if (r == InputRequest.Result.BUTTON1)
                    SpellBarManager.SaveCurrentRowPreset(n);
            }));
            g.CenterXInViewPort();
            g.CenterYInViewPort();
        }));
        menu.ContextMenu.Add(import);
        menu.ContextMenu.Add(new ContextMenuItemEntry("Lock/Unlock spellbar movement", (() =>
        {
            IsLocked = !IsLocked;
        })));
        menu.ContextMenu.Add(new ContextMenuItemEntry("Add row", () =>
        {
            SpellBarManager.SpellBarRows.Add(new SpellBarRow());
            SpellBarManager.CurrentRow = SpellBarManager.SpellBarRows.Count - 1;
            Build();
        }));
        menu.ContextMenu.Add(new ContextMenuItemEntry("Delete row", () =>
        {
            if (SpellBarManager.SpellBarRows.Count > 1)
            {
                SpellBarManager.SpellBarRows.RemoveAt(SpellBarManager.CurrentRow);
                SpellBarManager.CurrentRow = Math.Max(0, SpellBarManager.CurrentRow - 1);
                Build();
            }
        }));
        menu.ContextMenu.Add(new ContextMenuItemEntry("Set row color", () =>
        {
            UIManager.Add(new ModernColorPicker(World, (h) =>
            {
                SpellBarManager.SpellBarRows[SpellBarManager.CurrentRow].RowHue = h;
                Build();
            }));
        }));
        menu.ContextMenu.Add(new ContextMenuItemEntry("More options", AssistantWindow.Show));

        Add(menu);
    }

    private static void GenAvailablePresets(ContextMenuItemEntry par)
    {
        if (par == null)
            return;

        foreach (string preset in SpellBarManager.ListPresets())
        {
            par.Add(new ContextMenuItemEntry(preset, () =>
            {
                SpellBarManager.ImportPreset(preset);
            }));
        }
    }

    protected override void OnMouseUp(int x, int y, MouseButtonType button)
    {
        base.OnMouseUp(x, y, button);

        if (button == MouseButtonType.Left && Keyboard.Alt && UIManager.MouseOverControl != null && (UIManager.MouseOverControl == this || UIManager.MouseOverControl.RootParent == this))
        {
            ref readonly SpriteInfo texture = ref Client.Game.UO.Gumps.GetGump(0x82C);
            if (texture.Texture != null)
            {
                if (x >= 0 && x <= texture.UV.Width && y >= 0 && y <= texture.UV.Height)
                {
                    IsLocked = !IsLocked;
                }
            }
        }
    }

    public void SetupHotkeyLabels()
    {
        foreach (SpellEntry entry in spellEntries)
        {
            entry.BuildHotkeyLabel();
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        EventSink.SpellCastBegin -= EventSinkOnSpellCastBegin;
        EventSink.SpellRecoveryBegin -= EventSinkOnSpellRecoveryBegin;
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
        public int CurrentSpellID => spell?.ID ?? -1;

        private GumpPic icon;
        private SpellDefinition spell;
        private AlphaBlendControl background;
        private int row, col;
        private bool trackCasting;
        private bool trackRecovery;
        private World World;
        private Gump parentGump;
        private TextBox hotkeyLabel;
        private Microsoft.Xna.Framework.Graphics.Texture2D castingTexture = SolidColorTextureCache.GetTexture(Color.Black);
        private Microsoft.Xna.Framework.Graphics.Texture2D recoveryTexture = SolidColorTextureCache.GetTexture(Color.Black);
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

        public SpellEntry SetSpell(SpellDefinition spell, int row, int col)
        {
            this.spell = spell;
            this.row = row;
            this.col = col;
            background.Hue = SpellBarManager.SpellBarRows[row].RowHue;
            SpellBarManager.SpellBarRows[row].SpellSlot[col] = spell;
            if (spell != null && spell != SpellDefinition.EmptySpell)
            {
                icon.Graphic = (ushort)spell.GumpIconSmallID;
                icon.IsVisible = true;

                int cliloc = GetSpellTooltip(spell.ID);

                if (cliloc != 0)
                    SetTooltip(Client.Game.UO.FileManager.Clilocs.GetString(cliloc), 80);
                else
                    SetTooltip(string.Empty);
            }
            else
            {
                SetTooltip("Right click to set spell");
                icon.IsVisible = false;
            }

            SetHotkeyText(col);

            return this;
        }

        private void SetHotkeyText(int slot)
        {
            if (!ProfileManager.CurrentProfile.SpellBar_ShowHotkeys) return;
            if (hotkeyLabel == null) return;
            if (spell == null || spell == SpellDefinition.EmptySpell)
            {
                hotkeyLabel.SetText(string.Empty);
                return;
            }

            string keys = SpellBarManager.GetKetNames(slot);
            if (string.IsNullOrEmpty(keys))
                keys = SpellBarManager.GetControllerButtonsName(slot);

            hotkeyLabel.SetText(keys);
        }

        /// <summary>
        /// Only call this when you're sure it's being casted.
        /// </summary>
        public void BeginTrackingCasting()
        {
            savedStateTime = DateTime.Now;
            trackCasting = true;
        }

        /// <summary>
        /// Only call this when you're sure to be in a recovery phase.
        /// </summary>
        public void BeginTrackingRecovery()
        {
            savedStateTime = DateTime.Now;
            trackCasting = false;
            trackRecovery = true;
        }

        public void Cast()
        {
            if (spell != null && spell != SpellDefinition.EmptySpell)
            {
                GameActions.CastSpell(spell.ID);
            }
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            base.OnMouseUp(x, y, button);
            if (button == MouseButtonType.Right)
                ContextMenu?.Show();

            if (button == MouseButtonType.Left && !Keyboard.Alt && !Keyboard.Ctrl)
            {
                Cast();
            }
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
            BuildHotkeyLabel();

            ContextMenu = new(parentGump);
            ContextMenu.Add(new ContextMenuItemEntry("Set spell", () =>
            {
                UIManager.Add
                (
                    new SpellQuickSearch
                    (World,
                        ScreenCoordinateX - 20, ScreenCoordinateY - 90, (s) =>
                        {
                            SetSpell(s, row, col);
                        }, true
                    )
                );
            }));
            ContextMenu.Add(new ContextMenuItemEntry("Clear", () =>
            {
                SetSpell(SpellDefinition.EmptySpell, row, col);
            }));
        }

        private void resetStates()
        {
            trackCasting = false;
            trackRecovery = false;
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (!base.Draw(batcher, x, y))
                return false;

            if (!trackCasting && !trackRecovery)
                return true;

            if (trackCasting && !SpellVisualRangeManager.Instance.IsCastingWithoutTarget())
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

            double castTime = trackCasting ? i.GetEffectiveCastTime() : i.GetEffectiveRecoveryTime();
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
                batcher.Draw(trackCasting ? castingTexture : recoveryTexture, rect, new Vector3(0, 0, 0.65f));
            }
            else
            {
                resetStates();
            }


            return true;
        }

        private static int GetSpellTooltip(int id)
        {
            if (id >= 1 && id <= 64) // Magery
            {
                return 3002011 + (id - 1);
            }

            if (id >= 101 && id <= 117) // necro
            {
                return 1060509 + (id - 101);
            }

            if (id >= 201 && id <= 210)
            {
                return 1060585 + (id - 201);
            }

            if (id >= 401 && id <= 406)
            {
                return 1060595 + (id - 401);
            }

            if (id >= 501 && id <= 508)
            {
                return 1060610 + (id - 501);
            }

            if (id >= 601 && id <= 616)
            {
                return 1071026 + (id - 601);
            }

            if (id >= 678 && id <= 693)
            {
                return 1031678 + (id - 678);
            }

            if (id >= 701 && id <= 745)
            {
                if (id <= 706)
                {
                    return 1115612 + (id - 701);
                }

                if (id <= 745)
                {
                    return 1155896 + (id - 707);
                }
            }

            return 0;
        }
    }
}
