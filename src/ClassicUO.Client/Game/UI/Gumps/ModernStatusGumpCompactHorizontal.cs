using System;
using System.Collections.Generic;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using FontStashSharp.RichText;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.UI.Gumps
{
    /// <summary>
    /// A compact status gump drawn on a plain <see cref="AlphaBlendControl"/> background. All
    /// player stats are listed in columns without a character name or section headers; each row
    /// is placed directly below the previous one using its measured height so the gump stays as
    /// small as possible. Labels refresh when the player's stats are updated by the server via
    /// <see cref="EventSink.PlayerStatsUpdated"/>.
    /// </summary>
    public class StatusGumpCompactHorizontal : StatusGumpBase
    {
        private const float FONT_SIZE = 16;
        private const int LABEL_HUE = 1153;
        private const int VALUE_HUE = 1281;
        private const ushort BUFF_BUTTON_NORMAL = 0x7538;
        private const ushort BUFF_BUTTON_PRESSED = 0x7539;
        private const int PADDING = 8;
        private const int COLUMN_GAP = 18;
        private const int ROW_GAP = 2;

        private readonly TextBox[] _textLabels = new TextBox[(int)MobileStats.NumStats];
        private readonly string[] _formats = new string[(int)MobileStats.NumStats];

        public StatusGumpCompactHorizontal(World world) : base(world)
        {
            var background = new AlphaBlendControl(0.7f)
            {
                AcceptMouseInput = true,
                CanMove = true
            };
            Add(background);

            var buffButton = new Button((int)ButtonType.BuffIcon, BUFF_BUTTON_NORMAL, BUFF_BUTTON_PRESSED, BUFF_BUTTON_PRESSED)
            {
                X = PADDING,
                Y = PADDING,
                ButtonAction = ButtonAction.Activate
            };
            Add(buffButton);

            (MobileStats, string, string, int)[][] columns = new[]
            {
                new[]
                {
                    (MobileStats.Strength, "STR", "strength", VALUE_HUE),
                    (MobileStats.Dexterity, "DEX", "dexterity", VALUE_HUE),
                    (MobileStats.Intelligence, "INT", "intelligence", VALUE_HUE),
                },
                new[]
                {
                    (MobileStats.HealthCurrent, "HP", "hit_points", VALUE_HUE),
                    (MobileStats.ManaCurrent, "MP", "mana", VALUE_HUE),
                    (MobileStats.StaminaCurrent, "SP", "stamina", VALUE_HUE),
                },
                new[]
                {
                    (MobileStats.Damage, "D", "damage", VALUE_HUE),
                    (MobileStats.DamageChanceInc, "DI", "weapon_damage_increase", VALUE_HUE),
                    (MobileStats.HitChanceInc, "HCI", "hit_chance_increase", VALUE_HUE),
                    (MobileStats.SwingSpeedInc, "SSI", "swing_speed_increase", VALUE_HUE),
                    (MobileStats.DefenseChanceInc, "DCI", "defense_chance_increase", VALUE_HUE),
                },
                new[]
                {
                    (MobileStats.SpellDamageInc, "SDI", "spell_damage_increase", VALUE_HUE),
                    (MobileStats.FasterCasting, "FC", "faster_casting", VALUE_HUE),
                    (MobileStats.FasterCastRecovery, "FCR", "faster_cast_recovery", VALUE_HUE),
                    (MobileStats.LowerManaCost, "LMC", "lower_mana_cost", VALUE_HUE),
                    (MobileStats.LowerReagentCost, "LRC", "lower_reagent_cost", VALUE_HUE),
                },
                new[]
                {      
                    (MobileStats.AR, "PH", "physical_resistance", 114),
                    (MobileStats.RF, "FR", "fire_resistance", 40),
                    (MobileStats.RC, "CD", "cold_resistance", 93),
                    (MobileStats.RP, "PS", "poison_resistance", 172),
                    (MobileStats.RE, "EN", "energy_resistance", VALUE_HUE),
                },
                new[]
                {
                    (MobileStats.StatCap, "MST", "max_stats", VALUE_HUE),
                    (MobileStats.Luck, "LK", "luck", VALUE_HUE),
                    (MobileStats.WeightCurrent, "WT", "weight", VALUE_HUE),
                    (MobileStats.Gold, "GD", "gold", VALUE_HUE),
                    (MobileStats.Followers, "FR", "followers", VALUE_HUE)
                }
            };

            int top = PADDING + buffButton.Height + ROW_GAP;
            var columnLabels = new List<TextBox>[columns.Length];
            int tallestColumn = top;

            for (int i = 0; i < columns.Length; i++)
            {
                int y = top;
                columnLabels[i] = new List<TextBox>(columns[i].Length);

                foreach ((MobileStats stat, string label, string tooltip, int valueHue) in columns[i])
                {
                    TextBox row = AddStatRow(stat, label, TazLang.Get(tooltip), valueHue);
                    row.Y = y;
                    columnLabels[i].Add(row);
                    y += row.Height + ROW_GAP;
                }

                tallestColumn = Math.Max(tallestColumn, y);
            }

            UpdateLabels();

            int x = PADDING;
            foreach (List<TextBox> labels in columnLabels)
            {
                int columnWidth = 0;

                foreach (TextBox row in labels)
                {
                    row.X = x;
                    columnWidth = Math.Max(columnWidth, row.MeasuredSize.X);
                }

                x += columnWidth + COLUMN_GAP;
            }

            Width = x - COLUMN_GAP + PADDING;
            Height = tallestColumn + PADDING;
            background.Width = Width;
            background.Height = Height;
            WantUpdateSize = false;

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

        private TextBox AddStatRow(MobileStats stat, string label, string tooltip, int valueHue)
        {
            string labelColor = TextBox.ConvertHueToColor(LABEL_HUE).ToHexString();
            string valueColor = TextBox.ConvertHueToColor(valueHue).ToHexString();
            _formats[(int)stat] = $"/c[{labelColor}]{label} /c[{valueColor}]{{0}}";

            var row = TextBox.GetOne(_formats[(int)stat], EmbeddedFontNames.ALAGARD, FONT_SIZE, 0, new TextBox.RTLOptions());
            row.AcceptMouseInput = tooltip != null;
            row.CanMove = true;
            row.SetTooltip(tooltip);
            Add(row);

            _textLabels[(int)stat] = row;
            return row;
        }
    }
}
