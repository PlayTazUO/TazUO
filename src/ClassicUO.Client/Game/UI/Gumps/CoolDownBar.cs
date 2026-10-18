using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using System;

namespace ClassicUO.Game.UI.Gumps
{
    public class CoolDownBar : Gump
    {
        public const int COOL_DOWN_WIDTH = 180, COOL_DOWN_HEIGHT = 30;
        public static int DEFAULT_X => ProfileManager.CurrentProfile.CoolDownX;
        public static int DEFAULT_Y => ProfileManager.CurrentProfile.CoolDownY;

        private AlphaBlendControl background, foreground;
        private readonly Control nameText, timerText;
        private DateTime expire;
        private TimeSpan duration;
        private int startX, startY;
        public string Name { get; }
        private readonly bool isBuffBar;

        private GumpPic gumpPic;

        public BuffIconType buffIconType;

        public CoolDownBar(World world, TimeSpan _duration, string _name, ushort _hue, int x, int y, ushort graphic = ushort.MaxValue, BuffIconType type = BuffIconType.Unknown2, bool isBuffBar = false) : base(world, 0, 0)
        {
            #region VARS
            Width = COOL_DOWN_WIDTH;
            Height = COOL_DOWN_HEIGHT;
            X = x;
            startX = x;
            Y = y;
            startY = y;
            expire = DateTime.Now + _duration;
            duration = _duration;
            CanCloseWithRightClick = true;
            CanMove = true;
            AcceptMouseInput = true;
            buffIconType = type;
            this.isBuffBar = isBuffBar;
            #endregion

            #region BACK/FORE GROUND
            background = new AlphaBlendControl(0.3f);
            background.Width = COOL_DOWN_WIDTH;
            background.Height = COOL_DOWN_HEIGHT;
            background.Hue = _hue;

            foreground = new AlphaBlendControl(0.8f);
            foreground.Width = COOL_DOWN_WIDTH;
            foreground.Height = COOL_DOWN_HEIGHT;
            foreground.Hue = _hue;
            #endregion

            if (graphic != ushort.MaxValue)
            {
                gumpPic = new GumpPic(0, 2, graphic, 0);
                background.X = gumpPic.Width;
                background.Width = COOL_DOWN_WIDTH - gumpPic.Width;

                foreground.X = gumpPic.Width;
                foreground.Width = COOL_DOWN_WIDTH - gumpPic.Width;
            }

            #region LABELS
            Name = _name;
            if (_name.Length > 17)
            {
                _name = _name.Substring(0, 16) + "..";
            }

            if (isBuffBar)
            {
                nameText = CreateText(_name, background.Width, _hue);
                timerText = CreateText("------", background.Width, _hue);
            }
            else
            {
                nameText = new Label(_name, true, _hue, background.Width, style: FontStyle.BlackBorder, align: Assets.TEXT_ALIGN_TYPE.TS_CENTER);
                timerText = new Label("------", true, _hue, background.Width, style: FontStyle.BlackBorder, align: Assets.TEXT_ALIGN_TYPE.TS_CENTER);
            }

            nameText.X = background.X;
            timerText.X = background.X;
            timerText.Y = COOL_DOWN_HEIGHT - timerText.Height - 2;
            SetText(timerText, string.Empty);
            #endregion

            #region ADD CONTROLS
            if (graphic != ushort.MaxValue)
                Add(gumpPic);
            Add(background);
            Add(foreground);
            Add(nameText);
            Add(timerText);
            #endregion
        }

        private static TextBox CreateText(string text, int width, ushort hue) =>
            TextBox.GetOne(text, GetBuffBarFont(), GetBuffBarFontSize(), hue, TextBox.RTLOptions.DefaultCenterStroked(width));

        private static string GetBuffBarFont()
        {
            GlobalSettingsSave settings = ProfileManager.GlobalSettings;
            return string.IsNullOrWhiteSpace(settings?.BuffBarFont) ? EmbeddedFontNames.AVADONIAN : settings.BuffBarFont;
        }

        private static float GetBuffBarFontSize()
        {
            GlobalSettingsSave settings = ProfileManager.GlobalSettings;
            return settings == null || settings.BuffBarFontSize <= 0 ? 14 : settings.BuffBarFontSize;
        }

        private static void SetText(Control control, string text)
        {
            switch (control)
            {
                case Label label:
                    label.Text = text;
                    break;
                case TextBox textBox:
                    textBox.Text = text;
                    break;
            }
        }

        /// <summary>
        /// Applies the machine-wide buff bar font to live bars so option changes take effect without
        /// re-adding buffs.
        /// </summary>
        private void SyncBuffBarFont()
        {
            if (nameText is not TextBox nameBox || timerText is not TextBox timerBox)
                return;

            string font = GetBuffBarFont();
            float size = GetBuffBarFontSize();

            if (nameBox.Font != font)
            {
                nameBox.Font = font;
                timerBox.Font = font;
            }

            if (nameBox.FontSize != size)
            {
                nameBox.FontSize = size;
                timerBox.FontSize = size;
            }

            timerText.Y = COOL_DOWN_HEIGHT - timerText.Height - 2;
        }

        public TimeSpan Remaining => expire - DateTime.Now;

        public void Update(TimeSpan? maxValue = null, TimeSpan? currentValue = null)
        {
            if (maxValue.HasValue)
                duration = maxValue.Value;

            if (currentValue.HasValue)
                expire = DateTime.Now + currentValue.Value;
        }

        public void Restart()
        {
            expire = DateTime.Now + duration;
        }

        public override void Update()
        {
            base.Update();

            if (isBuffBar)
                SyncBuffBarFont();

            if (
                !isBuffBar &&
                (ProfileManager.CurrentProfile?.UseLastMovedCooldownPosition ?? false) &&
                (X != startX || Y != startY)
                )
            {
                ProfileManager.CurrentProfile.CoolDownX = X;
                ProfileManager.CurrentProfile.CoolDownY = Y;
                startX = X;
                startY = Y;
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsDisposed)
                return false;

            if (DateTime.Now >= expire)
            {
                Dispose();
                return false;
            }

            TimeSpan remaing = expire - DateTime.Now;

            if (remaing < TimeSpan.FromMinutes(60))
            {
                int offset = 0;
                if (gumpPic != null)
                    offset = gumpPic.Width;
                foreground.Width = Math.Max(0, Math.Min(COOL_DOWN_WIDTH - offset, (int)((remaing.TotalSeconds / duration.TotalSeconds) * (COOL_DOWN_WIDTH - offset))));
                SetText(timerText, ((int)remaing.TotalSeconds).ToString());
            }

            base.Draw(batcher, x, y);

            batcher.DrawRectangle(
                    SolidColorTextureCache.GetTexture(Color.Black),
                    x, y,
                    COOL_DOWN_WIDTH,
                    COOL_DOWN_HEIGHT,
                    ShaderHueTranslator.GetHueVector(background.Hue, false, 1f)
                );
            batcher.DrawRectangle(
                SolidColorTextureCache.GetTexture(Color.Black),
                x + 1, y + 1,
                COOL_DOWN_WIDTH - 2,
                COOL_DOWN_HEIGHT - 2,
                ShaderHueTranslator.GetHueVector(background.Hue, false, 1f)
            );

            return true;
        }
    }
}
