// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    public class Label : Control
    {
        private readonly RenderedText _gText;

        public Label
        (
            string text,
            bool isunicode,
            ushort hue,
            int maxwidth = 0,
            byte font = 0xFF,
            FontStyle style = FontStyle.None,
            TEXT_ALIGN_TYPE align = TEXT_ALIGN_TYPE.TS_LEFT,
            bool ishtml = false
        )
        {
            _gText = RenderedText.Create
            (
                text,
                hue,
                font,
                isunicode,
                style,
                align,
                maxwidth,
                isHTML: ishtml
            );

            AcceptMouseInput = false;
            Width = _gText.Width;
            Height = _gText.Height;
        }

        public Label(List<string> parts, string[] lines) : this
        (
            int.TryParse(parts[4], out int lineIndex) && lineIndex >= 0 && lineIndex < lines.Length ? lines[lineIndex] : string.Empty,
            true,
            (ushort) (UInt16Converter.Parse(parts[3]) + 1),
            0,
            style: FontStyle.BlackBorder
        )
        {
            X = int.Parse(parts[1]);
            Y = int.Parse(parts[2]);
            IsFromServer = true;
        }

        public string Text
        {
            get => _gText.Text;
            set
            {
                _gText.Text = value;
                Width = _gText.Width;
                Height = _gText.Height;
            }
        }


        public ushort Hue
        {
            get => _gText.Hue;
            set
            {
                if (_gText.Hue != value)
                {
                    _gText.Hue = value;
                    _gText.CreateTexture();
                }
            }
        }


        public byte Font => _gText.Font;

        public bool Unicode => _gText.IsUnicode;

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsDisposed)
            {
                return false;
            }

            _gText.Draw(batcher, x, y, Alpha);

            return base.Draw(batcher, x, y);
        }

        public override void Dispose()
        {
            base.Dispose();
            _gText.Destroy();
        }

        public static bool IsCJKLanguage =>
            string.Equals(Settings.GlobalSettings?.Language, "CHS", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Settings.GlobalSettings?.Language, "CHT", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Settings.GlobalSettings?.Language, "JPN", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Settings.GlobalSettings?.Language, "KOR", StringComparison.OrdinalIgnoreCase);

        public static bool ContainsCJK(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            foreach (char c in text)
                if (c >= 0x4E00 && c <= 0x9FFF)
                    return true;

            return false;
        }

        public static Control CreateCJK(
            string text, bool isunicode, ushort hue,
            int maxwidth = 0, byte font = 0xFF,
            FontStyle style = FontStyle.None,
            TEXT_ALIGN_TYPE align = TEXT_ALIGN_TYPE.TS_LEFT,
            bool ishtml = false)
        {
            if (IsCJKLanguage && ContainsCJK(text))
            {
                string fontName = TrueTypeLoader.Instance.Fonts.Contains("simhei")
                    ? "simhei" : TrueTypeLoader.EMBEDDED_FONT;

                float fontSize = font switch
                {
                    1 => 14, 2 => 15, 3 => 16, 6 => 14, 9 => 12, _ => 14
                };

                var options = TextBox.RTLOptions.Default();
                if (maxwidth > 0)
                    options.Width = maxwidth;

                var color = TextBox.ConvertHueToColor(hue);
                return TextBox.GetOne(text, fontName, fontSize, color, options);
            }

            return new Label(text, isunicode, hue, maxwidth, font, style, align, ishtml);
        }
    }
}