using System;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using FontStashSharp.RichText;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.UI.Gumps
{
    /// <summary>
    /// A horizontal, modern-styled status gump that reports the player's stats using the
    /// <c>MStatusGumpHorizontal.png</c> background. Labels refresh when the player's stats are
    /// updated by the server via <see cref="EventSink.PlayerStatsUpdated"/>.
    /// </summary>
    public class StatusGumpModernHorizontal : StatusGumpBase
    {
        private const float FONT_SIZE = 16;
        private const float NAME_FONT_SIZE = 22;
        private const int HEADER_HUE = 54;
        private const int LABEL_HUE = 1153;
        private const int VALUE_HUE = 1281;
        private const ushort BUFF_BUTTON_NORMAL = 0x7538;
        private const ushort BUFF_BUTTON_PRESSED = 0x7539;

        private readonly TextBox[] _textLabels = new TextBox[(int)MobileStats.NumStats];
        private readonly string[] _formats = new string[(int)MobileStats.NumStats];

        public StatusGumpModernHorizontal(World world) : base(world)
        {
            if (ExternalImageLoader.Instance.TryGetEmbeddedTexture("MStatusGumpHorizontal.png", out Texture2D background))
            {
                Add(new EmbeddedGumpPic(0, 0, background) { Width = 482, Height = 150 });
                Width = 482;
                Height = 150;
                WantUpdateSize = false;
            }

            Add
            (
                new Button((int)ButtonType.BuffIcon, BUFF_BUTTON_NORMAL, BUFF_BUTTON_PRESSED, BUFF_BUTTON_PRESSED)
                {
                    X = 5,
                    Y = 5,
                    ButtonAction = ButtonAction.Activate
                }
            );

            _formats[(int)MobileStats.Name] = "{0}";
            AddCenteredLabel(0, 5, MobileStats.Name, NAME_FONT_SIZE, LABEL_HUE, Width);

            int hw = Width << 2;
            Add(new Line(hw << 2, 16, hw, 1, Color.Gray.PackedValue) { AcceptMouseInput = false });

            int y = 30;
            int ydiff = 16;
            int x = 15;
            AddHeader(x, y, "Stats");
            AddStatRow(x, y+=ydiff, MobileStats.Strength, "STR", TazLang.Get("strength"));
            AddStatRow(x, y+=ydiff, MobileStats.Dexterity, "DEX", TazLang.Get("dexterity"));
            AddStatRow(x, y+=ydiff, MobileStats.Intelligence, "INT", TazLang.Get("intelligence"));
            AddStatRow(x, y+=ydiff, MobileStats.HealthCurrent, "HP", TazLang.Get("hit_points"));
            AddStatRow(x, y+=ydiff, MobileStats.ManaCurrent, "MP", TazLang.Get("mana"));
            AddStatRow(x, y+=ydiff, MobileStats.StaminaCurrent, "SP", TazLang.Get("stamina"));

            y = 30;
            x = 120;

            AddHeader(x,  y, "Physical");
            AddStatRow(x, y+=ydiff, MobileStats.Damage, "DI", TazLang.Get("damage"));
            AddStatRow(x, y+=ydiff, MobileStats.HitChanceInc, "HCI", TazLang.Get("hit_chance_increase"));
            AddStatRow(x, y+=ydiff, MobileStats.DefenseChanceInc, "DCI", TazLang.Get("defense_chance_increase"));
            AddStatRow(x, y+=ydiff, MobileStats.SwingSpeedInc, "SSI", TazLang.Get("swing_speed_increase"));
            AddStatRow(x, y+=ydiff, MobileStats.DamageChanceInc, "DI", TazLang.Get("weapon_damage_increase"));

            y = 30;
            x = 200;

            AddHeader(x, y, "Magical");
            AddStatRow(x, y+=ydiff, MobileStats.SpellDamageInc, "SDI", TazLang.Get("spell_damage_increase"));
            AddStatRow(x, y+=ydiff, MobileStats.FasterCasting, "FC", TazLang.Get("faster_casting"));
            AddStatRow(x, y+=ydiff, MobileStats.FasterCastRecovery, "FCR", TazLang.Get("faster_cast_recovery"));
            AddStatRow(x, y+=ydiff, MobileStats.LowerManaCost, "LMC", TazLang.Get("lower_mana_cost"));
            AddStatRow(x, y+=ydiff, MobileStats.LowerReagentCost, "LRC", TazLang.Get("lower_reagent_cost"));

            y = 30;
            x = 280;

            AddHeader(x, y, "Resistances");
            AddStatRow(x, y+=ydiff, MobileStats.AR, "PH", TazLang.Get("physical_resistance"), valueHue: 114);
            AddStatRow(x, y+=ydiff, MobileStats.RF, "FR", TazLang.Get("fire_resistance"), valueHue: 40);
            AddStatRow(x, y+=ydiff, MobileStats.RC, "CD", TazLang.Get("cold_resistance"), valueHue: 93);
            AddStatRow(x, y+=ydiff, MobileStats.RP, "PS", TazLang.Get("poison_resistance"), valueHue: 172);
            AddStatRow(x, y+=ydiff, MobileStats.RE, "EN", TazLang.Get("energy_resistance"));

            y = 30;
            x = 380;

            AddHeader(x, y, "Other");
            AddStatRow(x, y+=ydiff, MobileStats.StatCap, "MST", TazLang.Get("max_stats"));
            AddStatRow(x, y+=ydiff, MobileStats.Luck, "LK", TazLang.Get("luck"));
            AddStatRow(x, y+=ydiff, MobileStats.WeightCurrent, "WT", TazLang.Get("weight"));
            AddStatRow(x, y+=ydiff, MobileStats.Gold, "GD", TazLang.Get("gold"));
            AddStatRow(x, y+=ydiff, MobileStats.Followers, "FR", TazLang.Get("followers"));

            UpdateLabels();
            EventSink.PlayerStatsUpdated += OnPlayerStatsUpdated;
        }

        public override void Dispose()
        {
            EventSink.PlayerStatsUpdated -= OnPlayerStatsUpdated;
            base.Dispose();
        }

        public override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            Parent?.OnMouseUp(X + x, Y + y, button);

            if (button == MouseButtonType.Left && World.TargetManager.IsTargeting)
            {
                World.TargetManager.Target(World.Player);
                Mouse.LastLeftButtonClickTime = 0;
            }
        }

        private void OnPlayerStatsUpdated(object sender, EventArgs e) => UpdateLabels();

        private void UpdateLabels()
        {
            if (World.Player == null)
            {
                return;
            }

            SetLabel(MobileStats.Name, !string.IsNullOrEmpty(World.Player.Name) ? World.Player.Name : string.Empty);
            SetLabel(MobileStats.Strength, World.Player.Strength.ToString());
            SetLabel(MobileStats.Dexterity, World.Player.Dexterity.ToString());
            SetLabel(MobileStats.Intelligence, World.Player.Intelligence.ToString());
            SetLabel(MobileStats.HealthCurrent, $"{World.Player.Hits}/{World.Player.HitsMax}");
            SetLabel(MobileStats.ManaCurrent, $"{World.Player.Mana}/{World.Player.ManaMax}");
            SetLabel(MobileStats.StaminaCurrent, $"{World.Player.Stamina}/{World.Player.StaminaMax}");
            SetLabel(MobileStats.Damage, $"{World.Player.DamageMin}-{World.Player.DamageMax}");
            SetLabel(MobileStats.HitChanceInc, World.Player.HitChanceIncrease.ToString());
            SetLabel(MobileStats.DefenseChanceInc, World.Player.DefenseChanceIncrease.ToString());
            SetLabel(MobileStats.SwingSpeedInc, World.Player.SwingSpeedIncrease.ToString());
            SetLabel(MobileStats.DamageChanceInc, World.Player.DamageIncrease.ToString());
            SetLabel(MobileStats.SpellDamageInc, World.Player.SpellDamageIncrease.ToString());
            SetLabel(MobileStats.FasterCasting, World.Player.FasterCasting.ToString());
            SetLabel(MobileStats.FasterCastRecovery, World.Player.FasterCastRecovery.ToString());
            SetLabel(MobileStats.LowerManaCost, World.Player.LowerManaCost.ToString());
            SetLabel(MobileStats.LowerReagentCost, World.Player.LowerReagentCost.ToString());
            SetLabel(MobileStats.AR, $"{World.Player.PhysicalResistance}/{World.Player.MaxPhysicResistence}");
            SetLabel(MobileStats.RF, $"{World.Player.FireResistance}/{World.Player.MaxFireResistence}");
            SetLabel(MobileStats.RC, $"{World.Player.ColdResistance}/{World.Player.MaxColdResistence}");
            SetLabel(MobileStats.RP, $"{World.Player.PoisonResistance}/{World.Player.MaxPoisonResistence}");
            SetLabel(MobileStats.RE, $"{World.Player.EnergyResistance}/{World.Player.MaxEnergyResistence}");
            SetLabel(MobileStats.StatCap, World.Player.StatsCap.ToString());
            SetLabel(MobileStats.Luck, World.Player.Luck.ToString());
            SetLabel(MobileStats.WeightCurrent, $"{World.Player.Weight}/{World.Player.WeightMax}");
            SetLabel(MobileStats.Gold, World.Player.Gold.ToString());
            SetLabel(MobileStats.Followers, $"{World.Player.Followers}/{World.Player.FollowersMax}");
        }

        private void SetLabel(MobileStats stat, string value)
        {
            TextBox label = _textLabels[(int)stat];

            if (label == null || label.IsDisposed)
            {
                return;
            }

            string text = string.Format(_formats[(int)stat], value);

            if (label.Text != text)
            {
                label.Text = text;
            }
        }

        private void AddHeader(int x, int y, string text, int hue = HEADER_HUE)
        {
            var label = TextBox.GetOne(text, EmbeddedFontNames.ALAGARD, FONT_SIZE, hue, new TextBox.RTLOptions());
            label.X = x;
            label.Y = y;
            label.AcceptMouseInput = false;
            Add(label);
        }

        private void AddStatRow(int x, int y, MobileStats stat, string label, string tooltip, int labelHue = LABEL_HUE, int valueHue = VALUE_HUE)
        {
            string labelColor = TextBox.ConvertHueToColor(labelHue).ToHexString();
            string valueColor = TextBox.ConvertHueToColor(valueHue).ToHexString();
            _formats[(int)stat] = $"/c[{labelColor}]{label} /c[{valueColor}]{{0}}";
            AddLabel(x, y, _formats[(int)stat], stat, FONT_SIZE, tooltip);
        }

        private void AddCenteredLabel(int x, int y, MobileStats stat, float size, int hue, int width)
        {
            var options = new TextBox.RTLOptions { Width = width, Align = TextHorizontalAlignment.Center };
            var label = TextBox.GetOne(_formats[(int)stat], EmbeddedFontNames.ALAGARD, size, hue, options);
            label.X = x;
            label.Y = y;
            label.AcceptMouseInput = false;
            Add(label);

            _textLabels[(int)stat] = label;
        }

        private void AddLabel(int x, int y, string text, MobileStats stat, float size = FONT_SIZE, string tooltip = null)
        {
            var label = TextBox.GetOne(text, EmbeddedFontNames.ALAGARD, size, 0, new TextBox.RTLOptions());
            label.X = x;
            label.Y = y;
            label.AcceptMouseInput = tooltip != null;
            label.SetTooltip(tooltip);
            Add(label);

            _textLabels[(int)stat] = label;
        }
    }
}
