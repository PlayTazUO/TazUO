#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Renderer;
using ClassicUO.Utility.Logging;
using FontStashSharp.RichText;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ClassicUO.Utility;

namespace ClassicUO.Game.UI.Controls
{
    public class TextBox : Control
    {
        private static Queue<TextBox> _pool = new();
        private RichTextLayout _rtl;
        private string _font;
        private float _size;
        private Color _color;
        private bool _dirty = false;

        private int getStrokeSize
        {
            get
            {
                if (ProfileManager.CurrentProfile != null)
                    return ProfileManager.CurrentProfile.TextBorderSize;
                return 1;
            }
        }

        public bool MultiLine { get { return Options.MultiLine; } set { Options.MultiLine = value; } }

        public static TextBox CreateNew(string text, string font, float size, int hue, RTLOptions options) => new TextBox(text, font, size, ConvertHueToColor(hue), options);

        public static TextBox GetOne(string text, string font, float size, int hue, RTLOptions options) => GetOne(text, font, size, ConvertHueToColor(hue), options);

        public static TextBox GetOne(string text, string font, float size, Color hue, RTLOptions options) =>
            // if (_pool.Count > 0)
            // {
            //     TextBox tb = _pool.Dequeue();
            //
            //     while (!tb.IsDisposed && tb.Parent != null) //In case a text entry was added to the pool but is still in use somewhere
            //     {
            //         if(_pool.Count > 0)
            //             tb = _pool.Dequeue();
            //         else
            //             return new TextBox(text, font, size, hue, options);
            //     }
            //
            //     tb.SetDisposed(false);
            //     tb._font = font;
            //     tb._size = size;
            //     tb._color = hue;
            //     tb.Options = options;
            //     tb.AcceptMouseInput = options.AcceptMouseInput;
            //     tb.CreateRichTextLayout(text);
            //
            //     return tb;
            // }

            new TextBox(text, font, size, hue, options);

        private TextBox(string text, string font, float size, Color hue, RTLOptions options)
        {
            _font = font;
            _size = size;
            _color = hue;
            Options = options;
            AcceptMouseInput = options.AcceptMouseInput;
            CreateRichTextLayout(text);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="text"></param>
        /// <param name="width">Leave null to make width fit the text.</param>
        /// <param name="applyTextFormatting">True will add a stroke, and convert html colors if those are true. Set to false to keep text as is.</param>
        private void CreateRichTextLayout(string text)
        {
            text ??= string.Empty;  //Prevent null ref error while still updating everything else
            _dirty = false;         //Reset these because we're creating a new text object
            WantUpdateSize = false; //Not resetting them causes this to happen twice from the constructor setting _font and what not.

            if (Options == null)
            {
                Log.Error("Options was null when creating rich text layout(TextBox.cs)"); //Avoid a bad textbox object by using default options
                Options = RTLOptions.Default();
            }

            if (Options.ConvertHtmlColors)
                text = ConvertHTMLColorsToFSS(text);

            if (Options.StrokeEffect && !text.StartsWith("/es"))
                text = $"/es[{getStrokeSize}]" + text;

            if (_rtl == null || _rtl.Text != text || _rtl.Width != Options.Width)
                _rtl = new RichTextLayout
                {
                    Font = TrueTypeLoader.Instance.GetFont(_font, _size),
                    Text = text,
                    IgnoreColorCommand = Options.IgnoreColorCommands,
                    SupportsCommands = Options.SupportsCommands,
                    CalculateGlyphs = Options.CalculateGlyphs,
                    Width = Options.Width
                };

            base.Width = Options.Width ?? _rtl.Size.X;
            base.Height = Height;
        }

        public static Color ConvertHueToColor(int hue)
        {
            if (hue == 0xFFFF || hue == ushort.MaxValue)
            {
                return Color.White;
            }

            if (hue == 0)
                hue = 946; //Change black text to standard gray

            return new Color() { PackedValue = Client.Game.UO.FileManager.Hues.GetHueColorRgba8888(31, (ushort)hue) };
        }

        public bool PixelCheck(int x, int y)
        {
            if (!AcceptMouseInput || string.IsNullOrWhiteSpace(Text))
            {
                return false;
            }

            if (x < 0 || x >= Width)
            {
                return false;
            }

            if (y < 0 || y >= Height)
            {
                return false;
            }

            return true;
        }

        public new int Width
        {
            get
            {
                if (base.Width > 0)
                    return base.Width;

                if (Options != null && Options.Width.HasValue)
                    return Options.Width.Value;

                if (_rtl != null && _rtl.Size != null)
                    return _rtl.Size.X;

                return 0;
            }

            set
            {
                Options ??= new RTLOptions();

                Options.Width = base.Width = value;
                _dirty = true;
            }
        }
        public new int Height
        {
            get
            {
                if (_rtl == null)
                    return base.Height;

                return _rtl.Size.Y;
            }

            set
            {
                base.Height = value;
            }
        }
        public Point MeasuredSize
        {
            get
            {
                if (_rtl == null)
                    return Point.Zero;
                return _rtl.Size;
            }
        }
        public string Text
        {
            get => _rtl.Text;
            set
            {
                if (_rtl.Text != value)
                {
                    if (Options.ConvertHtmlColors)
                    {
                        _rtl.Text = ConvertHTMLColorsToFSS(value);
                    }
                    else
                    {
                        _rtl.Text = value;
                    }

                    _dirty = true;
                }
            }
        }
        public int Hue
        {
            get => (int)_color.PackedValue;
            set
            {
                uint newVal = Client.Game.UO.FileManager.Hues.GetHueColorRgba8888(31, (ushort)value);
                if (_color.PackedValue != newVal)
                {
                    _color.PackedValue = newVal;
                    _dirty = true;
                }
            }
        }
        public Color FontColor
        {
            get => _color;
            set
            {
                _color = value;
                _dirty = true;
            }
        }
        public RTLOptions Options { get; set; }
        public RichTextLayout RTL => _rtl;
        public string Font
        {
            get => _font;
            set
            {
                _font = value;
                _dirty = true;
            }
        }
        public float FontSize
        {
            get => _size;
            set
            {
                _size = value;
                _dirty = true;
            }
        }

        /// <summary>
        /// Added in for Python API
        /// </summary>
        /// <param name="text"></param>
        public void SetText(string text) => Text = text;

        public void Reset()
        {
            X = 0;
            Y = 0;
            _font = string.Empty;
            _color = Color.White;
            _size = 0;
            base.Width = 0;
            base.Height = 0;
            _rtl = null;
            Options = null;
            Alpha = 1f;
            _dirty = false;
            WantUpdateSize = false;
        }

        private static readonly Regex _baseFontColorRegex = RegexHelper.GetRegex("<basefont color=\"?'?(?<color>.*?)\"?'?>", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        private static readonly Regex _bodyTextColorRegex = RegexHelper.GetRegex("<bodytextcolor\"?'?(?<color>.*?)\"?'?>", RegexOptions.Multiline | RegexOptions.IgnoreCase);

        public static string ConvertHTMLColorsToFSS(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            string finalString = _baseFontColorRegex.Replace(text, " /c[${color}]");
            finalString = _bodyTextColorRegex.Replace(finalString, " /c[${color}]");
            finalString = finalString.Replace("</basefont>", "/cd").Replace("</BASEFONT>", "/cd").Replace("\n", "\n/cd").Replace("<BASEFONT>", "");

            return finalString;
        }

        public static string ConvertHtmlToFontStashSharpCommand(string text, bool converthtmlcolors = true)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            string finalString = text;

            if (converthtmlcolors)
                finalString = ConvertHTMLColorsToFSS(text);

            string[] replacements = new string[]
            {
                "<br>", "\n",
                "<BR>", "\n",
                "<left>", string.Empty,
                "</left>", string.Empty,
                "<b>", string.Empty,
                "</b>", string.Empty,
                "</font>", string.Empty,
                "<font>", string.Empty,
                "<h2>", string.Empty,
                "<BODY>", string.Empty,
                "<body>", string.Empty,
                "</BODY>", string.Empty,
                "</body>", string.Empty,
                "</p>", string.Empty,
                "<p>", string.Empty,
                "</BIG>", string.Empty,
                "<BIG>", string.Empty,
                "</big>", string.Empty,
                "<big>", string.Empty,
                "<basefont>", string.Empty,
                "<BASEFONT>", string.Empty
            };

            var sb = new StringBuilder(finalString);
            for (int i = 0; i < replacements.Length; i += 2)
                sb.Replace(replacements[i], replacements[i + 1]);

            return sb.ToString();
        }

        public override void Update()
        {
            if (_dirty || WantUpdateSize)
            {
                string text = _rtl?.Text ?? string.Empty;

                if (WantUpdateSize && Options != null)
                    Options.Width = null;

                CreateRichTextLayout(text);

                WantUpdateSize = false;
                _dirty = false;
            }
        }

        public override void Dispose() => base.Dispose();// #if DEBUG//             if (CUOEnviroment.Debug)//                 Log.Debug($"Returned to pool: [{Text}]");// #endif//             Reset();//             _pool.Enqueue(this);

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            base.Draw(batcher, x, y);

            return Draw(batcher, x, y, _color);
        }

        public bool Draw(UltimaBatcher2D batcher, int x, int y, Color color)
        {
            if (IsDisposed)
            {
                return false;
            }

            if (Options.Align == TextHorizontalAlignment.Center)
            {
                x += Width / 2;
            }
            else if (Options.Align == TextHorizontalAlignment.Right)
            {
                x += Width;
            }

            _rtl.Draw(batcher, new Vector2(x, y), color * Alpha, horizontalAlignment: Options.Align);

            return true;
        }

        public class RTLOptions
        {
            public static RTLOptions Default(int? width = null) => new RTLOptions() { Width = width };
            public static RTLOptions DefaultCentered(int? width = null) => new RTLOptions() { Align = TextHorizontalAlignment.Center, Width = width };
            public static RTLOptions DefaultRightAligned(int? width = null) => new RTLOptions() { Align = TextHorizontalAlignment.Right, Width = width };
            public static RTLOptions DefaultCenterStroked(int? width = null) => new RTLOptions() { Align = TextHorizontalAlignment.Center, StrokeEffect = true, Width = width };
            public bool IgnoreColorCommands { get; set; }
            public bool SupportsCommands { get; set; } = true;
            public bool CalculateGlyphs { get; set; }
            public bool StrokeEffect { get; set; }
            public bool ConvertHtmlColors { get; set; } = true;
            public TextHorizontalAlignment Align { get; set; } = TextHorizontalAlignment.Left;
            public int? Width { get; set; } = null;
            public bool MultiLine { get; set; }
            public bool AcceptMouseInput { get; set; }

            public RTLOptions DisableCommands()
            {
                SupportsCommands = false;
                return this;
            }
            public RTLOptions IgnoreColors()
            {
                IgnoreColorCommands = true;
                return this;
            }
            public RTLOptions EnableGlyphCalculation()
            {
                CalculateGlyphs = true;
                return this;
            }
            public RTLOptions Alignment(TextHorizontalAlignment align)
            {
                Align = align;
                return this;
            }
            public RTLOptions MouseInput(bool accept = true)
            {
                AcceptMouseInput = accept;
                return this;
            }
        }
    }
}
