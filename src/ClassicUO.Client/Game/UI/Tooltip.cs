// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI
{
    public class Tooltip
    {
        private uint _hash;
        private uint _lastHoverTime;
        private TextBox _textBox;
        private string _textHTML;
        private readonly World _world;

        public Tooltip(World world)
        {
            _world = world;
        }

        private bool _dirty = false;

        public static bool IsEnabled = false;

        public static int X, Y;
        public static int Width, Height;

        public string Text { get; protected set; }

        public bool IsEmpty => Text == null;

        public uint Serial { get; private set; }

        public bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (SerialHelper.IsValid(Serial) && _world.OPL.TryGetRevision(Serial, out uint revision) && _hash != revision)
            {
                _hash = revision;
                Text = ReadProperties(Serial, out _textHTML);
            }

            if (string.IsNullOrEmpty(Text))
            {
                return false;
            }

            if (_lastHoverTime > Time.Ticks)
            {
                return false;
            }

            float alpha = 0.7f;
            ushort hue = 0xFFFF;
            float zoom = 1;

            if (ProfileManager.CurrentProfile != null)
            {
                alpha = ProfileManager.CurrentProfile.TooltipBackgroundOpacity / 100f;

                if (float.IsNaN(alpha))
                {
                    alpha = 0f;
                }

                hue = ProfileManager.CurrentProfile.TooltipTextHue;
                zoom = ProfileManager.CurrentProfile.TooltipDisplayZoom / 100f;
            }


            if (_textBox == null || _dirty)
            {
                FontStashSharp.RichText.TextHorizontalAlignment align = FontStashSharp.RichText.TextHorizontalAlignment.Center;
                if (ProfileManager.CurrentProfile != null)
                {
                    if (ProfileManager.CurrentProfile.LeftAlignToolTips)
                        align = FontStashSharp.RichText.TextHorizontalAlignment.Left;
                    if (SerialHelper.IsMobile(Serial) && ProfileManager.CurrentProfile.ForceCenterAlignTooltipMobiles)
                        align = FontStashSharp.RichText.TextHorizontalAlignment.Center;
                }

                string finalString = _textHTML;
                if (SerialHelper.IsItem(Serial))
                {
                    finalString = Managers.ToolTipOverrideData.ProcessTooltipText(_world, Serial);
                    finalString ??= _textHTML;
                }

                if (string.IsNullOrEmpty(finalString) && !string.IsNullOrEmpty(_textHTML)) //Fix for vendor search
                    finalString = Managers.ToolTipOverrideData.ProcessTooltipText(_textHTML);

                if (_textBox == null || _textBox.IsDisposed)
                {
                    string font = TrueTypeLoader.EMBEDDED_FONT;
                    int fontSize = 15;

                    if (ProfileManager.CurrentProfile != null)
                    {
                        font = ProfileManager.CurrentProfile.SelectedToolTipFont;
                        fontSize = ProfileManager.CurrentProfile.SelectedToolTipFontSize;
                    }
                    TextBox.RTLOptions tooltipOptions = new() { Align = align, StrokeEffect = true };
                    _textBox = TextBox.GetOne(TextBox.ConvertHtmlToFontStashSharpCommand(finalString).Trim(), font, fontSize, hue, tooltipOptions);

                    //_textBox.Width = _textBox.MeasuredSize.X + 10;
                }
                else
                {
                    _textBox.Text = TextBox.ConvertHtmlToFontStashSharpCommand(finalString).Trim();
                    _textBox.Update(); //For recreating the text to check size below
                }

                if (_textBox.Width > 600)
                {
                    _textBox.Width = 600;
                    _textBox.Update();
                }

                IsEnabled = true;
            }

            if (_textBox == null || _textBox.IsDisposed)
            {
                Log.Warn("Textbox should not be null/disposed, but it is.");
                return false;
            }

            int z_width = _textBox.Width + 8;
            int z_height = _textBox.Height + 8;

            if (x < 0)
            {
                x = 0;
            }
            else if (x > Client.Game.Window.ClientBounds.Width - z_width)
            {
                x = Client.Game.Window.ClientBounds.Width - z_width;
            }

            if (y < 0)
            {
                y = 0;
            }
            else if (y > Client.Game.Window.ClientBounds.Height - z_height)
            {
                y = Client.Game.Window.ClientBounds.Height - z_height;
            }

            X = x - 4;
            Y = y - 2;
            Width = (int)(z_width * zoom) + 1;
            Height = (int)(z_height * zoom) + 1;

            Vector3 hue_vec = ShaderHueTranslator.GetHueVector(1, false, alpha);

            if (ProfileManager.CurrentProfile != null)
                hue_vec.X = ProfileManager.CurrentProfile.ToolTipBGHue;

            batcher.Draw
            (
                SolidColorTextureCache.GetTexture(Color.White),
                new Rectangle
                (
                    x - 4,
                    y - 2,
                    (int)(z_width * zoom),
                    (int)(z_height * zoom)
                ),
                hue_vec
            );

            hue_vec = ShaderHueTranslator.GetHueVector(0, false, alpha);

            batcher.DrawRectangle
            (
                SolidColorTextureCache.GetTexture(Color.Gray),
                x - 4,
                y - 2,
                (int)(z_width * zoom),
                (int)(z_height * zoom),
                hue_vec
            );

            _textBox.Draw(batcher, x, y);

            return true;
        }

        public void Clear()
        {
            Serial = 0;
            _hash = 0;
            _textHTML = Text = null;
            _textBox?.Dispose();
            _textBox = null;
            IsEnabled = false;
        }

        public void SetGameObject(uint serial)
        {
            if (Serial == 0 || serial != Serial)
            {
                uint revision2 = 0;

                if (Serial == 0 || Serial != serial || _world.OPL.TryGetRevision(Serial, out uint revision) && _world.OPL.TryGetRevision(serial, out revision2) && revision != revision2)
                {
                    Serial = serial;
                    _hash = revision2;
                    Text = ReadProperties(serial, out _textHTML);
                    _textBox?.Dispose();
                    _textBox = null;
                    _dirty = true;

                    _lastHoverTime = (uint)(Time.Ticks + (ProfileManager.CurrentProfile != null ? ProfileManager.CurrentProfile.TooltipDelayBeforeDisplay : 250));
                }
            }
        }


        private string ReadProperties(uint serial, out string htmltext)
        {
            bool hasStartColor = false;

            string result = null;
            htmltext = string.Empty;

            if (SerialHelper.IsValid(serial) && _world.OPL.TryGetNameAndData(serial, out string name, out string data))
            {
                var sbHTML = new ValueStringBuilder();
                {
                    var sb = new ValueStringBuilder();
                    {
                        if (!string.IsNullOrEmpty(name))
                        {
                            if (SerialHelper.IsItem(serial))
                            {
                                sbHTML.Append("<basefont color=\"yellow\">");
                                hasStartColor = true;
                            }
                            else
                            {
                                Mobile mob = _world.Mobiles.Get(serial);

                                if (mob != null)
                                {
                                    sbHTML.Append(Notoriety.GetHTMLHue(mob.NotorietyFlag));
                                    hasStartColor = true;
                                }
                            }

                            sb.Append(name);
                            sbHTML.Append(name);

                            if (hasStartColor)
                            {
                                sbHTML.Append("<basefont color=\"#FFFFFFFF\">");
                            }
                        }

                        if (!string.IsNullOrEmpty(data))
                        {
                            sb.Append('\n');
                            sb.Append(data);
                            sbHTML.Append('\n');
                            sbHTML.Append(data);
                        }

                        htmltext = sbHTML.ToString();
                        result = sb.ToString();

                        sb.Dispose();
                        sbHTML.Dispose();
                    }
                }
            }
            return string.IsNullOrEmpty(result) ? null : result;
        }

        public void SetText(string text, int maxWidth = 0)
        {
            if (ProfileManager.CurrentProfile != null && !ProfileManager.CurrentProfile.UseTooltip)
            {
                return;
            }

            Serial = 0;

            Text = _textHTML = text;

            _dirty = true;


            _textBox?.Dispose();
            _textBox = null;

            _lastHoverTime = (uint)(Time.Ticks + (ProfileManager.CurrentProfile != null ? ProfileManager.CurrentProfile.TooltipDelayBeforeDisplay : 250));

        }
    }
}
