using System;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using FontStashSharp.RichText;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Unix.Native;

namespace ClassicUO.Game.UI.Gumps
{
    /// <summary>
    /// A horizontal, modern-styled status gump that reports the player's stats using the
    /// <c>MStatusGumpHorizontal.png</c> background. Labels refresh when the player's stats are
    /// updated by the server via <see cref="EventSink.PlayerStatsUpdated"/>.
    /// </summary>
    public class StatusGumpModernHorizontalBars : StatusGumpBase
    {
        private const float FONT_SIZE = 16;
        private const float NAME_FONT_SIZE = 22;
        private const int HEADER_HUE = 54;
        private const int LABEL_HUE = 1153;
        private const int VALUE_HUE = 1281;
        private const ushort BUFF_BUTTON_NORMAL = 0x7538;
        private const ushort BUFF_BUTTON_PRESSED = 0x7539;
        private static readonly Color BAR_BACKGROUND = new Color(0.3f, 0.3f, 0.3f, 0.7f);

        private readonly TextBox[] _textLabels = new TextBox[(int)MobileStats.NumStats];
        private readonly string[] _formats = new string[(int)MobileStats.NumStats];
        private readonly FlatProgressBar[] _bars = new FlatProgressBar[(int)MobileStats.NumStats];
        private readonly (Func<int> Current, Func<int> Max)[] _barSources = new (Func<int> Current, Func<int> Max)[(int)MobileStats.NumStats];

        public StatusGumpModernHorizontalBars(World world) : base(world)
        {

            if (ExternalImageLoader.Instance.TryGetEmbeddedTexture("MStatusGumpHorizontal.png", out Texture2D background))
            {
                Add(new EmbeddedGumpPic(0, 0, background) { Width = 500, Height = 165 });
                Width = 500;
                Height = 165;
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

            const int BAR_WIDTH = 100;
            const int BAR_HEIGHT = 14;
            const int SPACING = 4;

            //Stats
            Control stats = AddVisualContainer([
                AddHeader("Stats"),
                LeftSideLabel(
                    "HP",
                    AddProgressBar(BAR_WIDTH, BAR_HEIGHT, MobileStats.HealthCurrent, new Color(120, 0, 0), new Color(0, 120, 0), () => World.Instance.Player?.Hits ?? 0, () => World.Instance.Player?.HitsMax ?? 0),
                    130
                ),
                LeftSideLabel(
                    "SP",
                    AddProgressBar(BAR_WIDTH, BAR_HEIGHT, MobileStats.StaminaCurrent, BAR_BACKGROUND, new Color(0, 40, 0), () => World.Instance.Player?.Stamina ?? 0, () => World.Instance.Player?.StaminaMax ?? 0),
                    130
                ),
                LeftSideLabel(
                    "MP",
                    AddProgressBar(BAR_WIDTH, BAR_HEIGHT, MobileStats.ManaCurrent, BAR_BACKGROUND, new Color(0, 0, 120), () => World.Instance.Player?.Mana ?? 0, () => World.Instance.Player?.ManaMax ?? 0),
                    130
                )
            ]);

            //Attributes
            Control attr = AddVisualContainer([
                AddHeader("Attr"),
                AddStatRow(MobileStats.Strength, "STR", TazLang.Get("strength")),
                AddStatRow(MobileStats.Dexterity, "DEX", TazLang.Get("dexterity")),
                AddStatRow(MobileStats.Intelligence, "INT", TazLang.Get("intelligence"))
            ]);

            //Load
            Control load = AddVisualContainer([
                AddHeader("Load"),
                LeftSideLabel(
                    "WT",
                    AddProgressBar(50, BAR_HEIGHT, MobileStats.WeightCurrent, BAR_BACKGROUND, new Color(160, 160, 70), () => World.Instance.Player?.Weight ?? 0, () => World.Instance.Player?.WeightMax ?? 0),
                    80
                ),
                LeftSideLabel(
                    "FR",
                    AddProgressBar(50, BAR_HEIGHT, MobileStats.Followers, BAR_BACKGROUND, new Color(160, 160, 70), () => World.Instance.Player?.Followers ?? 0, () => World.Instance.Player?.FollowersMax ?? 0),
                    80
                ),
                AddStatRow(MobileStats.Gold, "GD", TazLang.Get("gold")),
                AddStatRow(MobileStats.Luck, "LK", TazLang.Get("luck")),
                AddStatRow(MobileStats.StatCap, "MST", TazLang.Get("max_stats"))
            ]);

            //Magic
            Control magic = AddVisualContainer([
                AddHeader("Magic"),
                AddStatRow(MobileStats.SpellDamageInc, "SDI", TazLang.Get("spell_damage_increase")),
                AddStatRow(MobileStats.FasterCasting, "FC", TazLang.Get("faster_casting")),
            ]);

            Control magic2 = AddVisualContainer([
                AddStatRow(MobileStats.FasterCastRecovery, "FCR", TazLang.Get("faster_cast_recovery")),
                AddStatRow(MobileStats.LowerManaCost, "LMC", TazLang.Get("lower_mana_cost")),
                AddStatRow(MobileStats.LowerReagentCost, "LRC", TazLang.Get("lower_reagent_cost"))
            ]);

            //Res
            Control res = AddVisualContainer([
                AddHeader("Resist"),
                LeftSideLabel(
                    "PH",
                    AddProgressBar(50, BAR_HEIGHT, MobileStats.AR, BAR_BACKGROUND, new Color(160, 160, 70), () => World.Instance.Player?.PhysicalResistance ?? 0, () => World.Instance.Player?.MaxPhysicResistence ?? 0),
                    80
                ),
                LeftSideLabel(
                    "FR",
                    AddProgressBar(50, BAR_HEIGHT, MobileStats.RF, BAR_BACKGROUND, new Color(120, 0, 0), () => World.Instance.Player?.FireResistance ?? 0, () => World.Instance.Player?.MaxFireResistence ?? 0),
                    80
                ),
                LeftSideLabel(
                    "CD",
                    AddProgressBar(50, BAR_HEIGHT, MobileStats.RC, BAR_BACKGROUND, new Color(0, 0, 120), () => World.Instance.Player?.ColdResistance ?? 0, () => World.Instance.Player?.MaxColdResistence ?? 0),
                    80
                ),
                LeftSideLabel(
                    "PS",
                    AddProgressBar(50, BAR_HEIGHT, MobileStats.RP, BAR_BACKGROUND, new Color(0, 120, 0), () => World.Instance.Player?.PoisonResistance ?? 0, () => World.Instance.Player?.MaxPoisonResistence ?? 0),
                    80
                ),
                LeftSideLabel(
                    "ER",
                    AddProgressBar(50, BAR_HEIGHT, MobileStats.RE, BAR_BACKGROUND, new Color(160, 160, 70), () => World.Instance.Player?.EnergyResistance ?? 0, () => World.Instance.Player?.MaxEnergyResistence ?? 0),
                    80
                ),
            ]);

            //Phys
            Control physical = AddVisualContainer([
                AddHeader("Phys"),
                AddStatRow(MobileStats.Damage, "D", TazLang.Get("damage")),
                AddStatRow(MobileStats.DamageChanceInc, "DI", TazLang.Get("weapon_damage_increase")),
                AddStatRow(MobileStats.HitChanceInc, "HCI", TazLang.Get("hit_chance_increase")),
                AddStatRow(MobileStats.SwingSpeedInc, "SSI", TazLang.Get("swing_speed_increase")),
                AddStatRow(MobileStats.DefenseChanceInc, "DCI", TazLang.Get("defense_chance_increase")),
            ]);

            stats.X = 10;
            stats.Y = 30;

            attr.Width = 80;
            attr.X = stats.Bounds.Right + SPACING;
            attr.Y = stats.Y;

            load.Width = 90;
            load.X = attr.Bounds.Right + SPACING;
            load.Y = attr.Y;

            physical.Width = 80;
            physical.X = load.Bounds.Right + SPACING;
            physical.Y = load.Y;

            res.X = physical.Bounds.Right + SPACING;
            res.Y = physical.Y;

            int mwidth = (attr.Bounds.Right - stats.X) / 2;

            magic.Width = mwidth;
            magic.X = 10;
            magic.Y = stats.Bounds.Bottom + SPACING;

            magic2.Width = mwidth;
            magic2.X = magic.Bounds.Right;
            magic2.Y = magic.Y;

            Add(stats);
            Add(attr);
            Add(load);
            Add(magic);
            Add(magic2);
            Add(res);
            Add(physical);

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

            UpdateBars();
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

        /// <summary>
        /// Adds a <see cref="FlatProgressBar"/> keyed by <paramref name="stat"/> so the update pass can resize
        /// its fill. The fill starts empty and is driven by <see cref="UpdateBars"/>.
        /// </summary>
        /// <param name="x">X position of the bar.</param>
        /// <param name="y">Y position of the bar.</param>
        /// <param name="width">Full width of the bar.</param>
        /// <param name="height">Height of the bar.</param>
        /// <param name="stat">The <see cref="MobileStats"/> this bar reports.</param>
        /// <param name="background">Color of the unfilled area.</param>
        /// <param name="foreground">Color of the filled area.</param>
        /// <param name="current">Getter for the current value.</param>
        /// <param name="max">Getter for the maximum value.</param>
        private Control AddProgressBar(int width, int height, MobileStats stat, Color background, Color foreground, Func<int> current, Func<int> max)
        {
            _barSources[(int)stat] = (current, max);
            return _bars[(int)stat] = new FlatProgressBar(width, height, background, foreground);
        }

        private void UpdateBars()
        {
            if (World.Player == null)
            {
                return;
            }

            for (int i = 0; i < _barSources.Length; i++)
            {
                (Func<int> current, Func<int> max) source = _barSources[i];

                if (source.current == null || source.max == null)
                {
                    continue;
                }

                _bars[i].UpdateProgress(source.current(), source.max());
            }
        }

        /// <summary>
        /// Creates a container with <paramref name="label"/> on the left and <paramref name="rightSide"/>
        /// placed to its right, vertically centered against the label text.
        /// </summary>
        /// <param name="label">Text to display on the left.</param>
        /// <param name="rightSide">Control to place on the right.</param>
        private Control LeftSideLabel(string label, Control rightSide, int minWidth)
        {
            var area = new Area(false);
            var text = TextBox.GetOne(label, EmbeddedFontNames.ALAGARD, FONT_SIZE, LABEL_HUE, new TextBox.RTLOptions());
            text.AcceptMouseInput = false;
            area.Add(text);

            rightSide.X = minWidth - rightSide.Width;
            rightSide.Y = Math.Max(0, (text.Height - rightSide.Height) >> 1);
            area.Add(rightSide);

            area.ForceSizeUpdate();
            area.Width = minWidth;

            return area;
        }

        private Control AddHeader(string text, int hue = HEADER_HUE)
        {
            var label = TextBox.GetOne(text, EmbeddedFontNames.ALAGARD, FONT_SIZE, hue, new TextBox.RTLOptions());
            label.AcceptMouseInput = false;
            return label;
        }

        private Control AddStatRow(MobileStats stat, string label, string tooltip, int labelHue = LABEL_HUE, int valueHue = VALUE_HUE)
        {
            string labelColor = TextBox.ConvertHueToColor(labelHue).ToHexString();
            string valueColor = TextBox.ConvertHueToColor(valueHue).ToHexString();
            _formats[(int)stat] = $"/c[{labelColor}]{label} /c[{valueColor}]{{0}}";
            return AddLabel(_formats[(int)stat], stat, FONT_SIZE, tooltip);
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

        private Control AddLabel(string text, MobileStats stat, float size = FONT_SIZE, string tooltip = null)
        {
            var label = TextBox.GetOne(text, EmbeddedFontNames.ALAGARD, size, 0, new TextBox.RTLOptions());
            label.AcceptMouseInput = tooltip != null;
            label.CanMove = true;
            label.SetTooltip(tooltip);

            _textLabels[(int)stat] = label;

            return label;
        }

        private Control AddVisualContainer(Control[] children)
        {
            var alpha = new AlphaBlendControl(0.3f);

            int nextY = 0;

            foreach (Control c in children)
            {
                c.Y = nextY;
                nextY = c.Bounds.Bottom;
                alpha.Add(c);
            }

            alpha.ForceSizeUpdate();

            return alpha;
        }
    }
}
