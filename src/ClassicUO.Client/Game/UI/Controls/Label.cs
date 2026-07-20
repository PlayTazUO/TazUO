// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using ClassicUO.Utility;

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

        public override IGui ApplyScale(double scale, bool scalePosition = true, bool scaleSize = true, bool force = false)
        {
            // Width/Height are derived from the text, so let the base set InternalScale and scale the
            // position, then recompute the size from the (now known) scale instead of compounding it.
            base.ApplyScale(scale, scalePosition, false, force);

            if (scaleSize)
            {
                Width = (int)(_gText.Width * InternalScale);
                Height = (int)(_gText.Height * InternalScale);
            }

            return this;
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
                Width = (int)(_gText.Width * InternalScale);
                Height = (int)(_gText.Height * InternalScale);
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

            _gText.Draw(batcher, x, y, InternalScale, Alpha);

            return base.Draw(batcher, x, y);
        }

        public override void Dispose()
        {
            base.Dispose();
            _gText.Destroy();
        }
    }
}