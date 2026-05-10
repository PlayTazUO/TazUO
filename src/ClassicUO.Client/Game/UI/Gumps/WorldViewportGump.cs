// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Timers;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.LegionScripting;
using ClassicUO.Network;
using ClassicUO.Renderer;
using ClassicUO.Resources;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.UI.Gumps
{
    public class WorldViewportGump : Gump
    {
        public static WorldViewportGump Instance { get; private set; } //Special gump, only one will ever exist

        public const int BORDER_WIDTH = 5;
        private readonly BorderControl _borderControl;
        private readonly Button _button;
        private bool _clicked;
        private Point _lastSize,
            _savedSize;
        private readonly GameScene _scene;
        private readonly SystemChatControl _systemChatControl;
        private List<(string, ushort)>? _userNotifications = null;

        private static Texture2D damageWindowOutline = SolidColorTextureCache.GetTexture(Color.White);
        public static Vector3 DamageWindowOutlineHue = ShaderHueTranslator.GetHueVector(32);


        public WorldViewportGump(World world, GameScene scene) : base(world, 0, 0)
        {
            Instance = this;
            _scene = scene;
            AcceptMouseInput = false;
            CanMove = !ProfileManager.CurrentProfile.GameWindowLock;
            CanCloseWithEsc = false;
            CanCloseWithRightClick = false;
            LayerOrder = UILayer.Under;

            // Check if we're in full-size mode for initial positioning
            bool isFullSize = ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.GameWindowFullSize;
            int borderSize = isFullSize ? 0 : BORDER_WIDTH;
            int borderOffset = isFullSize ? 0 : BORDER_WIDTH * 2;

            X = scene.Camera.Bounds.X - borderSize;
            Y = scene.Camera.Bounds.Y - borderSize;
            _savedSize = _lastSize = new Point(
                scene.Camera.Bounds.Width,
                scene.Camera.Bounds.Height
            );

            _button = new Button(0, 0x837, 0x838, 0x838);

            _button.MouseDown += (sender, e) =>
            {
                if (!ProfileManager.CurrentProfile.GameWindowLock)
                {
                    _clicked = true;
                }
            };

            _button.MouseUp += (sender, e) =>
            {
                if (!ProfileManager.CurrentProfile.GameWindowLock)
                {
                    Point n = ResizeGameWindow(_lastSize);

                    // if (Client.Game.UO.Version >= Utility.ClientVersion.CV_200)
                    // {
                    //     NetClient.Socket.Send_GameWindowSize((uint)n.X, (uint)n.Y);
                    // }

                    _clicked = false;
                }
            };

            _button.MouseEnter += (sender, e) => _button.Alpha = 1f;
            _button.MouseExit += (sender, e) => _button.Alpha = 0.3f;
            _button.Alpha = 0.3f;

            _button.SetTooltip(ResGumps.ResizeGameWindow);
            Width = scene.Camera.Bounds.Width + borderOffset;
            Height = scene.Camera.Bounds.Height + borderOffset;

            _borderControl = new BorderControl(0, 0, Width, Height, 4);

            UIManager.SystemChat = _systemChatControl = new SystemChatControl(
                this,
                borderSize,
                borderSize,
                scene.Camera.Bounds.Width,
                scene.Camera.Bounds.Height
            );

            Add(_borderControl);
            Add(_systemChatControl);
            Add(_button);
            Resize();

            if (ProfileManager.CurrentProfile.LastVersionHistoryShown != CUOEnviroment.Version.ToString())
            {
                UIManager.Add(new VersionHistory(world));
                ProfileManager.CurrentProfile.LastVersionHistoryShown = CUOEnviroment.Version.ToString();

                LegionScripting.LegionScripting.DownloadApiPy();
            }

            if (Settings.GlobalSettings.FPS < GameController.SupportedRefreshRate)
            {
                _userNotifications ??= new();
                _userNotifications.Add(($"Your monitor supports {GameController.SupportedRefreshRate} fps, but you currently have your fps limited to {Settings.GlobalSettings.FPS}. " +
                                        $"To update this type -syncfps", Constants.HUE_ERROR));
            }

            if (Settings.GlobalSettings.UltimaOnlineDirectory.StartsWith(CUOEnviroment.ExecutablePath))
            {
                _userNotifications ??= new();
                _userNotifications.Add(("Warning: It looks like your UO folder is stored inside TazUO, this is discouraged as you may accidentally have your UO files deleted.", Constants.HUE_ERROR));
            }

            if (_userNotifications != null) //Why is this here? This ensures the user is in-game and can see the world viewport before sending them messages
            {
                var timer = new Timer(TimeSpan.FromSeconds(5));
                timer.Elapsed += (sender, args) =>
                {
                    if (World.Instance != null)
                        _userNotifications.ForEach((s) =>
                        {
                            (string item1, ushort item2) = s;
                            GameActions.Print(item1, item2);
                        });

                    _userNotifications.Clear();
                    _userNotifications = null;
                    timer?.Stop();
                };
                timer.Start();
            }
        }

        public override void Update()
        {
            base.Update();

            if (IsDisposed)
            {
                return;
            }

            if (Mouse.IsDragging)
            {
                Point offset = Mouse.LDragOffset;

                _lastSize = _savedSize;

                if (_clicked && offset != Point.Zero)
                {
                    int w = _lastSize.X + offset.X;
                    int h = _lastSize.Y + offset.Y;

                    // Enforce minimum size
                    if (w < 640)
                    {
                        w = 640;
                    }

                    if (h < 480)
                    {
                        h = 480;
                    }

                    // Enforce maximum size based on current position
                    int maxW = Client.Game.Window.ClientBounds.Width - _scene.Camera.Bounds.X - BORDER_WIDTH;
                    int maxH = Client.Game.Window.ClientBounds.Height - _scene.Camera.Bounds.Y - BORDER_WIDTH;

                    if (w > maxW)
                    {
                        w = maxW;
                    }

                    if (h > maxH)
                    {
                        h = maxH;
                    }

                    _lastSize.X = w;
                    _lastSize.Y = h;
                }

                if (
                    _scene.Camera.Bounds.Width != _lastSize.X
                    || _scene.Camera.Bounds.Height != _lastSize.Y
                )
                {
                    Width = _lastSize.X + BORDER_WIDTH * 2;
                    Height = _lastSize.Y + BORDER_WIDTH * 2;
                    _scene.Camera.Bounds.Width = _lastSize.X;
                    _scene.Camera.Bounds.Height = _lastSize.Y;

                    Resize();
                }
            }
        }

        protected override void OnDragEnd(int x, int y)
        {
            base.OnDragEnd(x, y);

            // Clamp to keep entire viewport inside window bounds
            ClampViewportToWindowBounds();

            UpdateGameWindowPos();
        }

        protected override void OnMove(int x, int y)
        {
            base.OnMove(x, y);

            // Check if we're in full-size mode
            bool isFullSize = ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.GameWindowFullSize;
            int borderSize = isFullSize ? 0 : BORDER_WIDTH;

            _scene.Camera.Bounds.X = ScreenCoordinateX + borderSize;
            _scene.Camera.Bounds.Y = ScreenCoordinateY + borderSize;

            // Clamp during move to prevent going outside
            ClampViewportToWindowBounds();

            UpdateGameWindowPos();
        }

        private void UpdateGameWindowPos()
        {
            if (_scene != null)
            {
                _scene.UpdateDrawPosition = true;
            }
        }

        private void ClampViewportToWindowBounds()
        {
            int windowWidth = Client.Game.Window.ClientBounds.Width;
            int windowHeight = Client.Game.Window.ClientBounds.Height;

            // Check if we're in full-size mode
            bool isFullSize = ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.GameWindowFullSize;
            int borderSize = isFullSize ? 0 : BORDER_WIDTH;
            int borderOffset = isFullSize ? 0 : BORDER_WIDTH * 2;

            // Calculate available space for viewport (accounting for borders)
            int maxWidth = windowWidth - borderOffset;
            int maxHeight = windowHeight - borderOffset;

            // Ensure viewport size fits within window
            if (_scene.Camera.Bounds.Width > maxWidth)
            {
                _scene.Camera.Bounds.Width = Math.Max(640, maxWidth);
            }
            if (_scene.Camera.Bounds.Height > maxHeight)
            {
                _scene.Camera.Bounds.Height = Math.Max(480, maxHeight);
            }

            // Ensure viewport position keeps it fully inside window
            int maxX = windowWidth - _scene.Camera.Bounds.Width - borderSize;
            int maxY = windowHeight - _scene.Camera.Bounds.Height - borderSize;

            if (_scene.Camera.Bounds.X < borderSize)
            {
                _scene.Camera.Bounds.X = borderSize;
            }
            else if (_scene.Camera.Bounds.X > maxX)
            {
                _scene.Camera.Bounds.X = Math.Max(borderSize, maxX);
            }

            if (_scene.Camera.Bounds.Y < borderSize)
            {
                _scene.Camera.Bounds.Y = borderSize;
            }
            else if (_scene.Camera.Bounds.Y > maxY)
            {
                _scene.Camera.Bounds.Y = Math.Max(borderSize, maxY);
            }

            // Update gump position to match clamped camera bounds
            X = _scene.Camera.Bounds.X - borderSize;
            Y = _scene.Camera.Bounds.Y - borderSize;
            Width = _scene.Camera.Bounds.Width + borderOffset;
            Height = _scene.Camera.Bounds.Height + borderOffset;

            // Update border and button visibility
            _borderControl.IsVisible = !isFullSize;
            _button.IsVisible = !isFullSize;
        }

        private void Resize()
        {
            // Check if we're in full-size mode
            bool isFullSize = ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.GameWindowFullSize;
            int borderSize = isFullSize ? 0 : BORDER_WIDTH;
            int borderOffset = isFullSize ? 0 : BORDER_WIDTH * 2;

            _borderControl.Width = _scene.Camera.Bounds.Width + borderOffset;
            _borderControl.Height = _scene.Camera.Bounds.Height + borderOffset;
            _borderControl.IsVisible = !isFullSize;  // Hide border in full-size mode

            _button.X = Width - (_button.Width);
            _button.Y = Height - (_button.Height);
            _button.IsVisible = !isFullSize;  // Hide resize button in full-size mode

            // Update system chat control position and size
            // Note: Width/Height already includes borderOffset from ClampViewportToWindowBounds
            _systemChatControl.X = borderSize;
            _systemChatControl.Y = borderSize;
            _systemChatControl.Width = _scene.Camera.Bounds.Width;
            _systemChatControl.Height = _scene.Camera.Bounds.Height;
            _systemChatControl.Resize();
            //WantUpdateSize = true;

            UpdateGameWindowPos();
        }

        public void SetGameWindowPosition(Point pos)
        {
            Location = pos;

            // Check if we're in full-size mode
            bool isFullSize = ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.GameWindowFullSize;
            int borderSize = isFullSize ? 0 : BORDER_WIDTH;

            _scene.Camera.Bounds.X = ScreenCoordinateX + borderSize;
            _scene.Camera.Bounds.Y = ScreenCoordinateY + borderSize;

            UpdateGameWindowPos();
        }

        public Point ResizeGameWindow(Point newSize)
        {
            int windowWidth = Client.Game.Window.ClientBounds.Width;
            int windowHeight = Client.Game.Window.ClientBounds.Height;

            // Enforce minimum size
            if (newSize.X < 640)
            {
                newSize.X = 640;
            }

            if (newSize.Y < 480)
            {
                newSize.Y = 480;
            }

            // Check if we're in full-size mode
            bool isFullSize = ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.GameWindowFullSize;
            int borderOffset = isFullSize ? 0 : BORDER_WIDTH * 2;

            // Enforce maximum size (window bounds minus borders if not full-size)
            int maxWidth = windowWidth - borderOffset;
            int maxHeight = windowHeight - borderOffset;

            if (newSize.X > maxWidth)
            {
                newSize.X = maxWidth;
            }

            if (newSize.Y > maxHeight)
            {
                newSize.Y = maxHeight;
            }

            _lastSize = _savedSize = newSize;

            if (
                _scene.Camera.Bounds.Width != _lastSize.X
                || _scene.Camera.Bounds.Height != _lastSize.Y
            )
            {
                _scene.Camera.Bounds.Width = _lastSize.X;
                _scene.Camera.Bounds.Height = _lastSize.Y;
                Width = _scene.Camera.Bounds.Width + borderOffset;
                Height = _scene.Camera.Bounds.Height + borderOffset;

                Resize();

                // Ensure viewport stays in bounds after resize
                //ClampViewportToWindowBounds();
            }

            return newSize;
        }

        public void OnWindowResized()
        {
            // Clamp viewport to new window bounds
            //ClampViewportToWindowBounds(); //Disabled, too many people complained =/

            // Update internal state
            _lastSize.X = _scene.Camera.Bounds.Width;
            _lastSize.Y = _scene.Camera.Bounds.Height;
            _savedSize = _lastSize;

            Resize();
        }

        public override bool Contains(int x, int y)
        {
            // Check if we're in full-size mode
            bool isFullSize = ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.GameWindowFullSize;
            int borderSize = isFullSize ? 0 : BORDER_WIDTH;
            int borderOffset = isFullSize ? 0 : BORDER_WIDTH * 2;

            if (
                x >= borderSize
                && x < Width - borderOffset
                && y >= borderSize
                && y
                    < Height
                        - borderOffset
                        - (
                            _systemChatControl?.TextBoxControl != null
                            && _systemChatControl.IsActive
                                ? _systemChatControl.TextBoxControl.Height
                                : 0
                        )
            )
            {
                return false;
            }

            return base.Contains(x, y);
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (World.InGame && ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.EnableHealthIndicator)
            {
                float hpPercent = (float)World.Player.Hits / (float)World.Player.HitsMax;
                if (hpPercent <= ProfileManager.CurrentProfile.ShowHealthIndicatorBelow)
                {
                    int size = ProfileManager.CurrentProfile.HealthIndicatorWidth;
                    DamageWindowOutlineHue.Z = 1f - hpPercent;
                    batcher.Draw( //Top bar
                        damageWindowOutline,
                        new Rectangle(x + BORDER_WIDTH, y + BORDER_WIDTH, Width - (BORDER_WIDTH * 3), size),
                        DamageWindowOutlineHue
                        );

                    batcher.Draw( //Left Bar
                        damageWindowOutline,
                        new Rectangle(x + BORDER_WIDTH, y + BORDER_WIDTH + size, size, Height - (BORDER_WIDTH * 3) - (size * 2)),
                        DamageWindowOutlineHue
                        );

                    batcher.Draw( //Right Bar
                        damageWindowOutline,
                        new Rectangle(x + Width - (BORDER_WIDTH * 2) - size, y + BORDER_WIDTH + size, size, Height - (BORDER_WIDTH * 3) - (size * 2)),
                        DamageWindowOutlineHue
                        );

                    batcher.Draw( //Bottom bar
                        damageWindowOutline,
                        new Rectangle(x + BORDER_WIDTH, y + Height - (BORDER_WIDTH * 2) - size, Width - (BORDER_WIDTH * 3), size),
                        DamageWindowOutlineHue
                        );
                }
            }

            return base.Draw(batcher, x, y);;
        }
    }

    public class BorderControl : Control
    {
        private int _borderSize;

        private ushort h_border = 0x0A8C;
        private ushort v_border = 0x0A8D;
        private ushort h_bottom_border = 0x0A8C;
        private ushort v_right_border = 0x0A8D;
        private ushort t_left = 0xffff, t_right = 0xffff, b_left = 0xffff, b_right = 0xffff;

        public int BorderSize { get { return _borderSize; } set { _borderSize = value; } }
        public ushort H_Border { get { return h_border; } set { h_border = value; } }
        public ushort V_Border { get { return v_border; } set { v_border = value; } }
        public ushort V_Right_Border { get { return v_right_border; } set { v_right_border = value; } }
        public ushort H_Bottom_Border { get { return h_bottom_border; } set { h_bottom_border = value; } }
        public ushort T_Left { get { return t_left; } set { t_left = value; } }
        public ushort T_Right { get { return t_right; } set { t_right = value; } }
        public ushort B_Left { get { return b_left; } set { b_left = value; } }
        public ushort B_Right { get { return b_right; } set { b_right = value; } }

        public BorderControl(int x, int y, int w, int h, int borderSize)
        {
            X = x;
            Y = y;
            Width = w;
            Height = h;
            _borderSize = borderSize;
            CanMove = true;
            AcceptMouseInput = true;
        }

        public ushort Hue { get; set; }

        public void DefaultGraphics()
        {
            h_border = 0x0A8C;
            v_border = 0x0A8D;
            h_bottom_border = 0x0A8C;
            v_right_border = 0x0A8D;
            t_left = 0xffff; t_right = 0xffff; b_left = 0xffff; b_right = 0xffff;
            _borderSize = 4;
        }

        private Texture2D GetGumpTexture(uint g, out Rectangle bounds)
        {
            ref readonly SpriteInfo texture = ref Client.Game.UO.Gumps.GetGump(g);
            bounds = texture.UV;
            return texture.Texture;
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);
            Rectangle pos;

            if (Hue != 0)
            {
                hueVector.X = Hue;
                hueVector.Y = 1;
            }
            hueVector.Z = Alpha;

            Texture2D texture = GetGumpTexture(h_border, out Rectangle bounds);
            if (texture != null)
            {
                pos = new Rectangle
                (
                    x,
                    y,
                    Width,
                    _borderSize
                );
                if (t_left != 0xffff)
                {
                    pos.X += _borderSize;
                    pos.Width -= _borderSize;
                }
                if (t_right != 0xffff)
                    pos.Width -= _borderSize;
                // sopra
                batcher.DrawTiled
                (
                    texture,
                    pos,
                    bounds,
                    hueVector
                );
            }

            texture = GetGumpTexture(h_bottom_border, out bounds);
            if (texture != null)
            {
                pos = new Rectangle
                (
                    x,
                    y + Height - _borderSize,
                    Width,
                    _borderSize
                );
                if (b_left != 0xffff)
                {
                    pos.X += _borderSize;
                    pos.Width -= _borderSize;
                }
                if (b_right != 0xffff)
                    pos.Width -= _borderSize;
                // sotto
                batcher.DrawTiled
                (
                    texture,
                    pos,
                    bounds,
                    hueVector
                );
            }

            texture = GetGumpTexture(v_border, out bounds);
            if (texture != null)
            {
                pos = new Rectangle
                (
                    x,
                    y,
                    _borderSize,
                    Height
                );
                if (t_left != 0xffff)
                {
                    pos.Y += _borderSize;
                    pos.Height -= _borderSize;
                }
                if (b_left != 0xffff)
                    pos.Height -= _borderSize;
                //sx
                batcher.DrawTiled
                (
                    texture,
                    pos,
                    bounds,
                    hueVector
                );
            }

            texture = GetGumpTexture(v_right_border, out bounds);
            if (texture != null)
            {
                pos = new Rectangle
                (
                    x + Width - _borderSize,
                    y,
                    _borderSize,
                    Height
                );
                if (t_right != 0xffff)
                {
                    pos.Y += _borderSize;
                    pos.Height -= _borderSize;
                }
                if (b_right != 0xffff)
                    pos.Height -= _borderSize;
                //dx
                batcher.DrawTiled
                (
                    texture,
                    pos,
                    bounds,
                    hueVector
                );
            }

            if (t_left != 0xffff)
            {
                texture = GetGumpTexture(t_left, out bounds);
                if (texture != null)
                    batcher.Draw(
                        texture,
                        new Rectangle(x, y, bounds.Width, bounds.Height),
                        bounds,
                        hueVector
                        );
            }
            if (t_right != 0xffff)
            {
                texture = GetGumpTexture(t_right, out bounds);
                if (texture != null)
                    batcher.Draw(
                    texture,
                    new Rectangle(x + Width - _borderSize, y, bounds.Width, bounds.Height),
                    bounds,
                    hueVector
                    );
            }
            if (b_left != 0xffff)
            {
                texture = GetGumpTexture(b_left, out bounds);
                if (texture != null)
                    batcher.Draw(
                    texture,
                    new Rectangle(x, y + Height - _borderSize, bounds.Width, bounds.Height),
                    bounds,
                    hueVector
                    );
            }
            if (b_right != 0xffff)
            {
                texture = GetGumpTexture(b_right, out bounds);
                if (texture != null)
                    batcher.Draw(
                    texture,
                    new Rectangle(x + Width - _borderSize, y + Height - _borderSize, bounds.Width, bounds.Height),
                    bounds,
                    hueVector
                    );
            }

            return base.Draw(batcher, x, y);
        }
    }
}
