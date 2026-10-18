using System;
using System.Collections.Generic;
using System.Text;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.UI
{
    /// <summary>
    /// Debug-only overlay that follows the cursor and reports the world object or UI control under it,
    /// with a sprite preview where the hovered thing has one. Active only while
    /// <see cref="CUOEnviroment.Debug"/> is set (the in-game <c>-debug</c> command).
    /// </summary>
    internal sealed class DebugHoverTooltip
    {
        private const int PADDING = 6;
        private const int MOUSE_OFFSET = 18;
        private const int MAX_PREVIEW_SIZE = 72;
        private const int MAX_TEXT_LENGTH = 220;
        private const int MAX_LINE_LENGTH = 100;
        private const uint REFRESH_INTERVAL = 100;

        private readonly World _world;

        private string _text = string.Empty;
        private Preview _preview;
        private object _target;
        private uint _nextRefresh;

        /// <param name="world">World used to resolve entity names from the OPL for the hovered object.</param>
        public DebugHoverTooltip(World world)
        {
            _world = world;
        }

        /// <summary>
        /// Draws the hover panel beside the cursor. No-op unless <see cref="CUOEnviroment.Debug"/> is set.
        /// </summary>
        /// <param name="batcher">Batcher the cursor is already drawing through.</param>
        public void Draw(UltimaBatcher2D batcher)
        {
            if (!CUOEnviroment.Debug)
            {
                return;
            }

            object target = (object)UIManager.MouseOverControl ?? (object)FindGumpUnderMouse() ?? SelectedObject.Object;

            if (!ReferenceEquals(target, _target) || Time.Ticks >= _nextRefresh)
            {
                _target = target;
                _nextRefresh = Time.Ticks + REFRESH_INTERVAL;
                Build(target);
            }

            if (string.IsNullOrEmpty(_text))
            {
                return;
            }

            DrawPanel(batcher);
        }

        private void Build(object target)
        {
            _text = string.Empty;
            _preview = default;

            switch (target)
            {
                case IGui control when !control.IsDisposed:
                    _text = WrapText(BuildControl(control));

                    break;

                case BaseGameObject obj:
                    _text = WrapText(BuildWorldObject(obj));

                    break;
            }
        }

        private void DrawPanel(UltimaBatcher2D batcher)
        {
            Vector2 textSize = Fonts.Bold.MeasureString(_text);

            int previewBox = _preview.HasTexture ? MAX_PREVIEW_SIZE : 0;
            int textWidth = (int)Math.Ceiling(textSize.X);
            int textHeight = (int)Math.Ceiling(textSize.Y);

            int contentWidth = textWidth + (previewBox > 0 ? previewBox + PADDING : 0);
            int contentHeight = Math.Max(textHeight, previewBox);

            int panelWidth = contentWidth + (PADDING * 2);
            int panelHeight = contentHeight + (PADDING * 2);

            Point mouse = Mouse.Position;

            int panelX = mouse.X + MOUSE_OFFSET;
            int panelY = mouse.Y + MOUSE_OFFSET;

            if (panelX + panelWidth > ScaleHelper.LogicalWindowWidth)
            {
                panelX = mouse.X - panelWidth - MOUSE_OFFSET;
            }

            if (panelY + panelHeight > ScaleHelper.LogicalWindowHeight)
            {
                panelY = mouse.Y - panelHeight - MOUSE_OFFSET;
            }

            panelX = Math.Max(0, panelX);
            panelY = Math.Max(0, panelY);

            batcher.Draw
            (
                SolidColorTextureCache.GetTexture(Color.Black),
                new Rectangle(panelX, panelY, panelWidth, panelHeight),
                ShaderHueTranslator.GetHueVector(0, false, 0.85f)
            );

            batcher.DrawRectangle
            (
                SolidColorTextureCache.GetTexture(Color.Gray),
                panelX,
                panelY,
                panelWidth,
                panelHeight,
                ShaderHueTranslator.GetHueVector(0)
            );

            int textX = panelX + PADDING;

            if (_preview.HasTexture)
            {
                DrawPreview(batcher, panelX + PADDING, panelY + PADDING);
                textX += previewBox + PADDING;
            }

            batcher.DrawString(Fonts.Bold, _text, textX, panelY + PADDING, ShaderHueTranslator.GetHueVector(0));
        }

        private void DrawPreview(UltimaBatcher2D batcher, int x, int y)
        {
            Rectangle uv = _preview.UV;

            if (uv.Width <= 0 || uv.Height <= 0)
            {
                return;
            }

            float scale = Math.Min((float)MAX_PREVIEW_SIZE / uv.Width, (float)MAX_PREVIEW_SIZE / uv.Height);
            scale = Math.Min(scale, 1.5f);

            int width = Math.Max(1, (int)(uv.Width * scale));
            int height = Math.Max(1, (int)(uv.Height * scale));

            int px = x + ((MAX_PREVIEW_SIZE - width) >> 1);
            int py = y + ((MAX_PREVIEW_SIZE - height) >> 1);

            batcher.Draw(_preview.Texture, new Rectangle(px, py, width, height), uv, _preview.Hue);
        }

        private string BuildWorldObject(BaseGameObject obj)
        {
            var sb = new StringBuilder(256);

            switch (obj)
            {
                case Mobile mobile:
                    sb.Append($"Mobile  0x{mobile.Serial:X8}\n");
                    AppendName(sb, mobile.Serial);
                    sb.Append($"Graphic: 0x{mobile.Graphic:X4}  Anim: 0x{mobile.GetGraphicForAnimation():X4}\n");
                    sb.Append($"Notoriety: {mobile.NotorietyFlag}  Flags: {mobile.Flags}\n");
                    sb.Append($"HP: {mobile.Hits}/{mobile.HitsMax}  Mana: {mobile.Mana}/{mobile.ManaMax}  Stam: {mobile.Stamina}/{mobile.StaminaMax}\n");
                    sb.Append($"Dir: {mobile.Direction}  Mounted: {mobile.IsMounted}  Dead: {mobile.IsDead}\n");
                    AppendGameObjectCommon(sb, mobile);
                    SetPreview(MobilePreview(mobile));

                    break;

                case Item item:
                    sb.Append($"Item  0x{item.Serial:X8}\n");
                    AppendName(sb, item.Serial);
                    sb.Append($"Graphic: 0x{item.Graphic:X4}  Hue: 0x{item.Hue:X4}  Amount: {item.Amount}\n");
                    sb.Append($"Layer: {item.Layer}  Container: 0x{item.Container:X8}\n");
                    sb.Append($"Flags: {item.Flags}  HP: {item.Hits}/{item.HitsMax}\n");
                    AppendCustomName(sb, item.CustomName);

                    if (item.IsMulti)
                    {
                        sb.Append($"Multi: 0x{item.MultiGraphic:X4}  Dir: {item.Direction}\n");
                    }

                    AppendGameObjectCommon(sb, item);
                    SetPreview(ArtPreview(item.Graphic, item.Hue));

                    break;

                case Static staticObj:
                    sb.Append($"Static  0x{staticObj.Graphic:X4}\n");
                    sb.Append($"Height: {staticObj.ItemData.Height}  Flags: {staticObj.ItemData.Flags}  Hue: 0x{staticObj.Hue:X4}\n");
                    sb.Append($"Wall: {staticObj.ItemData.IsWall}  Impassable: {staticObj.ItemData.IsImpassable}  Vegetation: {staticObj.IsVegetation}\n");
                    AppendGameObjectCommon(sb, staticObj);
                    SetPreview(ArtPreview(staticObj.Graphic, staticObj.Hue));

                    break;

                case Multi multi:
                    sb.Append($"Multi  0x{multi.Graphic:X4}\n");
                    sb.Append($"Height: {multi.ItemData.Height}  State: {multi.State}  Movable: {multi.IsMovable}\n");
                    sb.Append($"Wall: {multi.ItemData.IsWall}  Impassable: {multi.ItemData.IsImpassable}\n");
                    AppendGameObjectCommon(sb, multi);
                    SetPreview(ArtPreview(multi.Graphic, multi.Hue));

                    break;

                case Land land:
                    sb.Append($"Land  0x{land.Graphic:X4}\n");
                    sb.Append($"Flags: {land.TileData.Flags}  Stretched: {land.IsStretched}  Impassable: {land.TileData.IsImpassable}\n");
                    sb.Append($"Z: {land.Z}  MinZ: {land.MinZ}  AvgZ: {land.AverageZ}  Pos: {land.X},{land.Y}\n");
                    SetPreview(LandPreview(land.Graphic));

                    break;

                case GameEffect effect:
                    sb.Append($"Effect  0x{effect.Graphic:X4}  Anim: 0x{effect.AnimationGraphic:X4}  Blend: {effect.Blend}\n");
                    AppendGameObjectCommon(sb, effect);
                    SetPreview(ArtPreview(effect.Graphic, effect.Hue));

                    break;

                case TextObject text:
                    sb.Append($"OverheadText  Type: {text.Type}  Hue: 0x{text.Hue:X4}\n");
                    AppendText(sb, text.TextBox?.Text);

                    break;
            }

            return sb.ToString();
        }

        private string BuildControl(IGui control)
        {
            var sb = new StringBuilder(256);

            Gump gump = control as Gump ?? control.RootParent as Gump;

            if (gump != null)
            {
                sb.Append("Gump: ").Append(gump.GumpType);

                if (gump.IsFromServer)
                {
                    sb.Append(" [server]");
                }

                sb.Append('\n');
                sb.Append($"GumpID: 0x{gump.ServerSerial:X8}  Local: 0x{gump.LocalSerial:X8}\n");

                if (gump.MasterGumpSerial != 0)
                {
                    sb.Append($"MasterGump: 0x{gump.MasterGumpSerial:X8}\n");
                }
            }

            sb.Append("Control: ").Append(control.GetType().Name).Append('\n');

            if (control.LocalSerial != 0 || control.ServerSerial != 0)
            {
                sb.Append($"Serial: 0x{control.ServerSerial:X8}  Local: 0x{control.LocalSerial:X8}\n");
            }

            sb.Append($"Page: {control.Page}  Active: {control.ActivePage}  Size: {control.Width}x{control.Height} @ {control.ScreenCoordinateX},{control.ScreenCoordinateY}\n");

            switch (control)
            {
                case Button button:
                    sb.Append($"ButtonID: {button.ButtonID}  Action: {button.ButtonAction}  ToPage: {button.ToPage}\n");
                    sb.Append($"Graphics N: 0x{button.ButtonGraphicNormal:X4}  P: 0x{button.ButtonGraphicPressed:X4}  O: 0x{button.ButtonGraphicOver:X4}\n");
                    SetPreview(GumpPreview(button.ButtonGraphicNormal, button.Hue));

                    break;

                case GumpPicBase pic:
                    sb.Append($"Graphic: 0x{pic.Graphic:X4}  Hue: 0x{pic.Hue:X4}\n");
                    SetPreview(GumpPreview(pic.Graphic, pic.Hue));

                    break;

                case GumpPicTiled tiled:
                    sb.Append($"Graphic: 0x{tiled.Graphic:X4}  Hue: 0x{tiled.Hue:X4}\n");
                    SetPreview(GumpPreview(tiled.Graphic, tiled.Hue));

                    break;

                case ResizePic resize:
                    sb.Append($"Graphic: 0x{resize.Graphic:X4}\n");
                    SetPreview(GumpPreview(resize.Graphic, 0));

                    break;

                case StaticPic staticPic:
                    sb.Append($"Graphic: 0x{staticPic.Graphic:X4}  Hue: 0x{staticPic.Hue:X4}\n");
                    SetPreview(ArtPreview(staticPic.Graphic, staticPic.Hue));

                    break;

                case RadioButton radio:
                    sb.Append($"Group: {radio.GroupIndex}  Checked: {radio.IsChecked}\n");
                    AppendText(sb, radio.Text);

                    break;

                case Checkbox checkbox:
                    sb.Append($"Checked: {checkbox.IsChecked}\n");
                    AppendText(sb, checkbox.Text);

                    break;

                case StbTextBox stbTextBox:
                    AppendText(sb, stbTextBox.Text);

                    break;

                case TextBox textBox:
                    AppendText(sb, textBox.Text);

                    break;

                case HtmlControl html:
                    AppendText(sb, html.Text);

                    break;

                case Label label:
                    AppendText(sb, label.Text);

                    break;
            }

            if (control.HasTooltip && control.Tooltip != null)
            {
                string tooltip = control.Tooltip switch
                {
                    string text => text,
                    uint serial => $"0x{serial:X8}",
                    Control custom => custom.GetType().Name,
                    _ => control.Tooltip.ToString()
                };

                AppendText(sb, tooltip, "Tooltip");
            }

            return sb.ToString();
        }

        private void AppendName(StringBuilder sb, uint serial)
        {
            if (_world.OPL.TryGetNameAndData(serial, out string name, out _) && !string.IsNullOrEmpty(name))
            {
                sb.Append($"Name: {name}\n");
            }
        }

        private static void AppendCustomName(StringBuilder sb, string customName)
        {
            if (!string.IsNullOrEmpty(customName))
            {
                sb.Append($"Custom: {customName}\n");
            }
        }

        private static void AppendGameObjectCommon(StringBuilder sb, GameObject obj)
        {
            sb.Append($"Pos: {obj.X},{obj.Y},{obj.Z}  Distance: {obj.Distance}\n");
            sb.Append($"Hue: 0x{obj.Hue:X4}  AlphaHue: {obj.AlphaHue}  Draw: {obj.AllowedToDraw}\n");
        }

        private static void AppendText(StringBuilder sb, string text, string label = "Text")
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            text = text.Replace('\r', ' ').Replace('\n', ' ');

            if (text.Length > MAX_TEXT_LENGTH)
            {
                text = string.Concat(text.AsSpan(0, MAX_TEXT_LENGTH), "…");
            }

            sb.Append(label).Append(": ").Append(text).Append('\n');
        }

        /// <summary>
        /// Hard-wraps lines longer than <see cref="MAX_LINE_LENGTH"/> at the last space so a single long
        /// value (a gump's text, a tooltip) cannot stretch the panel past the window edge.
        /// </summary>
        private static string WrapText(string text)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= MAX_LINE_LENGTH)
            {
                return text;
            }

            var sb = new StringBuilder(text.Length + 32);
            int lineLength = 0;
            int lastSpace = -1;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                if (c == '\n')
                {
                    sb.Append(c);
                    lineLength = 0;
                    lastSpace = -1;

                    continue;
                }

                sb.Append(c);

                if (c == ' ')
                {
                    lastSpace = sb.Length - 1;
                }

                if (++lineLength < MAX_LINE_LENGTH)
                {
                    continue;
                }

                if (lastSpace >= 0)
                {
                    sb[lastSpace] = '\n';
                    lineLength = sb.Length - lastSpace - 1;
                }
                else
                {
                    sb.Append('\n');
                    lineLength = 0;
                }

                lastSpace = -1;
            }

            return sb.ToString();
        }

        private void SetPreview(Preview preview)
        {
            _preview = preview.HasTexture ? preview : default;
        }

        private static Preview ArtPreview(ushort graphic, ushort hue)
        {
            ref readonly SpriteInfo info = ref Client.Game.UO.Arts.GetArt(graphic);

            return new Preview(info.Texture, info.UV, ShaderHueTranslator.GetHueVector(hue, false, 1f));
        }

        private static Preview LandPreview(ushort graphic)
        {
            ref readonly SpriteInfo info = ref Client.Game.UO.Arts.GetLand(graphic);

            return new Preview(info.Texture, info.UV, ShaderHueTranslator.GetHueVector(0));
        }

        private static Preview GumpPreview(ushort graphic, int hue)
        {
            ref readonly SpriteInfo info = ref Client.Game.UO.Gumps.GetGump(graphic);

            return new Preview(info.Texture, info.UV, ShaderHueTranslator.GetHueVector(hue, false, 1f, gump: true));
        }

        private static Preview MobilePreview(Mobile mobile)
        {
            ushort id = mobile.GetGraphicForAnimation();
            var animations = Client.Game.UO.Animations;

            if (id >= animations.MaxAnimationCount)
            {
                return default;
            }

            byte group = Mobile.GetGroupForAnimation(mobile, isParent: true);
            byte direction = (byte)mobile.GetDirectionForAnimation();

            Span<SpriteInfo> frames = animations.GetAnimationFrames(id, group, direction, out ushort hue, out _);

            if (frames.Length == 0)
            {
                return default;
            }

            SpriteInfo frame = frames[mobile.AnimIndex % frames.Length];

            return new Preview(frame.Texture, frame.UV, ShaderHueTranslator.GetHueVector(hue, false, 1f));
        }

        private static IGui FindGumpUnderMouse()
        {
            Point position = Mouse.Position;

            for (LinkedListNode<IGui> node = UIManager.Gumps.First; node != null; node = node.Next)
            {
                IGui gump = node.Value;

                if (gump == null || gump.IsDisposed || !gump.IsVisible || !gump.IsEnabled)
                {
                    continue;
                }

                // The world viewport spans the whole game window; treat the world beneath it as world,
                // not as a gump to inspect.
                if (gump is WorldViewportGump)
                {
                    continue;
                }

                bool underMouse = gump is Control control
                    ? control.Bounds.Contains(position.X - control.ParentX - control.Offset.X, position.Y - control.ParentY - control.Offset.Y)
                    : gump.Bounds.Contains(position.X, position.Y);

                if (underMouse)
                {
                    return gump;
                }
            }

            return null;
        }

        private readonly struct Preview
        {
            public readonly Texture2D Texture;
            public readonly Rectangle UV;
            public readonly Vector3 Hue;

            public Preview(Texture2D texture, Rectangle uv, Vector3 hue)
            {
                Texture = texture;
                UV = uv;
                Hue = hue;
            }

            public bool HasTexture => Texture != null && !Texture.IsDisposed;
        }
    }
}
