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
using System;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.UI
{
    public class Tooltip
    {
        /// <summary>
        /// Clearance between the cursor hotspot and the nearest background edge, held equal in both
        /// vertical directions. Sized to clear the cursor sprite, which hangs below the hotspot.
        /// </summary>
        private const int CURSOR_GAP = 22;

        /// <summary>
        /// Pixels the background extends past the text origin on its top edge. The two offsets below
        /// have to correct for it, because it shifts the background the same way whichever direction
        /// the box grows: downward it eats into the clearance, upward it adds to it.
        /// </summary>
        private const int BACKGROUND_TOP_PADDING = 2;

        /// <summary>Anchor-to-text offset when the box grows downward, leaving <see cref="CURSOR_GAP"/> of clearance.</summary>
        private const int CURSOR_OFFSET_BELOW = CURSOR_GAP + BACKGROUND_TOP_PADDING;

        /// <summary>Anchor-to-box offset when the box is flipped upward, leaving <see cref="CURSOR_GAP"/> of clearance.</summary>
        private const int CURSOR_OFFSET_ABOVE = CURSOR_GAP - BACKGROUND_TOP_PADDING;

        private uint _hash;
        private uint _lastHoverTime;
        private TextBox _textBox;
        private string _textHTML;
        private readonly World _world;
        private Item _item;

        public Tooltip(World world)
        {
            _world = world;
        }

        private bool _dirty = false;

        // Border hue requested by a matched tooltip override (-1 = default border).
        private int _borderHueOverride = -1;

        public static bool IsEnabled = false;

        public static int X, Y;
        public static int Width, Height;

        public string Text { get; protected set; }

        /// <summary>Optional line drawn above the tooltip body, e.g. the hovered counter cell's keybind. Set by the drawer each frame.</summary>
        public string Prefix { get; set; }

        public bool IsEmpty => Text == null;

        public uint Serial
        {
            get => field;
            private set
            {
                field = value;

                _item = null;

                if(SerialHelper.IsItem(field))
                    _item = _world.Items.Get(field);
            }
        }

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

                string finalString = Managers.ToolTipOverrideData.ResolveTooltipText(_world, Serial, _textHTML, out _borderHueOverride);

                if (!string.IsNullOrEmpty(Prefix))
                    finalString = Prefix + "\n" + finalString;

                if (_item?.CustomName.NotNullNotEmpty() == true) //Add custom item name
                    finalString = $"[{_item.CustomName}]\n" + finalString;

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

            // Tooltip dimensions stay in logical UI space: the whole UI is drawn to a render
            // target that the global RenderScale maps onto the screen at blit time, so multiplying
            // here would double-count the scale (background scales with RenderScale^2 while the
            // text scales with RenderScale once). See ScaleHelper's "never multiply by RenderScale".
            int z_width = _textBox.Width + 8;
            int z_height = _textBox.Height + 8;

            int bgWidth = (int)(z_width * zoom);
            int bgHeight = (int)(z_height * zoom);

            // A flip anchors the box's far edge, so placement must bound whatever reaches furthest:
            // the background scales with zoom and the text does not, so either can be the larger.
            int placementWidth = Math.Max(bgWidth, _textBox.Width);
            int placementHeight = Math.Max(bgHeight, _textBox.Height);

            x = ResolveAxis(x, 0, 0, placementWidth, ScaleHelper.LogicalWindowWidth);
            y = ResolveAxis(y, CURSOR_OFFSET_BELOW, CURSOR_OFFSET_ABOVE, placementHeight, ScaleHelper.LogicalWindowHeight);

            int bgX = x - 4;
            int bgY = y - BACKGROUND_TOP_PADDING;

            X = bgX;
            Y = bgY;
            Width = bgWidth + 1;
            Height = bgHeight + 1;

            Vector3 hue_vec = ShaderHueTranslator.GetHueVector(1, false, alpha);

            if (ProfileManager.CurrentProfile != null)
                hue_vec.X = ProfileManager.CurrentProfile.ToolTipBGHue;

            batcher.Draw
            (
                SolidColorTextureCache.GetTexture(Color.White),
                new Rectangle(bgX, bgY, bgWidth, bgHeight),
                hue_vec
            );

            Texture2D borderTexture = SolidColorTextureCache.GetTexture(Color.Gray);

            // A matched tooltip override draws a colored accent border on the left and top edges only.
            if (_borderHueOverride >= 0)
            {
                hue_vec = ShaderHueTranslator.GetHueVector(_borderHueOverride, false, alpha);
                borderTexture = SolidColorTextureCache.GetTexture(Color.White);

                const int leftWidth = 2;
                int topHeight = Managers.ToolTipOverrideData.BorderWidth;

                // Both edges sit just outside the background so they don't cover the tooltip text.
                // Top edge spans the width plus the top-left corner.
                batcher.Draw(borderTexture, new Rectangle(bgX - leftWidth, bgY - topHeight, bgWidth + leftWidth, topHeight), hue_vec);
                // Left edge.
                batcher.Draw(borderTexture, new Rectangle(bgX - leftWidth, bgY, leftWidth, bgHeight), hue_vec);
            }
            else
            {
                hue_vec = ShaderHueTranslator.GetHueVector(0, false, alpha);
                batcher.DrawRectangle(borderTexture, bgX, bgY, bgWidth, bgHeight, hue_vec);
            }

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
            _borderHueOverride = -1;
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


        /// <summary>
        /// Places the box along one axis, growing away from the anchor in the forward direction and
        /// flipping to the opposite side when that would run off screen.
        /// </summary>
        /// <param name="anchor">Cursor coordinate the tooltip hangs off.</param>
        /// <param name="forwardOffset">Gap between the anchor and the box when growing forward.</param>
        /// <param name="flipOffset">Gap between the anchor and the box when flipped back.</param>
        /// <param name="size">Box extent along this axis, padding included.</param>
        /// <param name="screenSize">Logical screen extent along this axis.</param>
        /// <returns>The box's start coordinate, clamped on screen when neither direction fits.</returns>
        private static int ResolveAxis(int anchor, int forwardOffset, int flipOffset, int size, int screenSize)
        {
            int forward = anchor + forwardOffset;

            // Preferred direction, taken whenever the far edge stays on screen.
            if (forward + size <= screenSize)
                return Math.Clamp(forward, 0, Math.Max(0, screenSize - size));

            // Overflowed, so grow back past the anchor instead; the box ends where the cursor begins.
            int flipped = anchor - size - flipOffset;

            // Flipping only helps if the near edge clears 0; otherwise the box outgrows the screen
            // on both sides, so fall back to the preferred direction pinned against the far edge.
            return flipped >= 0
                ? flipped
                : Math.Clamp(forward, 0, Math.Max(0, screenSize - size));
        }

        private string ReadProperties(uint serial, out string htmltext)
        {
            bool hasStartColor = false;

            string result = null;
            htmltext = string.Empty;

            if (SerialHelper.IsValid(serial) && _world.OPL.TryGetNameAndData(serial, out string name, out string data))
            {
                var sbHTML = new ValueStringBuilder();
                var sb = new ValueStringBuilder();

                if (!string.IsNullOrEmpty(name))
                {
                    if (SerialHelper.IsItem(serial))
                    {
                        sbHTML.Append("/c[yellow]");
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
                        sbHTML.Append("/c[#ffffff]");
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
            return string.IsNullOrEmpty(result) ? null : result;
        }

        public void SetText(string text)
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
