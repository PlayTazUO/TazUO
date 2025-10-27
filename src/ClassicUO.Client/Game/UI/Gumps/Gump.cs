// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    public class Gump : Control
    {
        private bool isLocked = false;

        public Gump(World world, uint local, uint server)
        {
            World = world;
            LocalSerial = local;
            ServerSerial = server;
            AcceptMouseInput = false;
            AcceptKeyboardInput = false;
        }

        public string PacketGumpText { get; set; } = string.Empty;

        public World World { get; }
        
        public virtual bool ShouldBeSaved => true;

        public bool CanBeSaved => ShouldBeSaved && (GumpType != Gumps.GumpType.None || ServerSerial != 0);

        public virtual GumpType GumpType { get; }

        public bool InvalidateContents { get; set; }

        public uint MasterGumpSerial { get; set; }

        public float AlphaOffset = 0;

        protected override void OnMouseWheel(MouseEventType delta)
        {
            base.OnMouseWheel(delta);

            if (Keyboard.Alt && ProfileManager.CurrentProfile.EnableAlphaScrollingOnGumps)
            {
                if (delta == MouseEventType.WheelScrollUp && Alpha < 0.99)
                {
                    AlphaOffset += 0.02f;
                    Alpha += 0.02f;
                    foreach (Control c in Children)
                    {
                        c.Alpha += 0.02f;
                        if (c.Alpha > 1) c.Alpha = 1;
                    }
                }
                else if (Alpha > 0.1)
                {
                    AlphaOffset -= 0.02f;
                    Alpha -= 0.02f;
                    foreach (Control c in Children)
                        c.Alpha -= 0.02f;
                }
            }
        }

        public virtual bool IsLocked
        {
            get { return isLocked; }
            set
            {
                isLocked = value;
                if (isLocked)
                {
                    CanMove = false;
                    CanCloseWithRightClick = false;
                }
                else
                {
                    CanMove = true;
                    CanCloseWithRightClick = true;
                }
                OnLockedChanged();
            }
        }

        public bool CanBeLocked { get; set; } = true;

        public override void Update()
        {
            if (InvalidateContents)
            {
                UpdateContents();
                InvalidateContents = false;
            }

            if (ActivePage == 0)
            {
                ActivePage = 1;
            }

            base.Update();
        }

        public override void Dispose()
        {
            Item it = World.Items.Get(LocalSerial);

            if (it != null && it.Opened)
            {
                it.Opened = false;
            }

            base.Dispose();
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            base.OnMouseUp(x, y, button);
            if (CanBeLocked && ((Keyboard.Ctrl && Keyboard.Alt) || Controller.Button_LeftTrigger) && UIManager.MouseOverControl != null && (UIManager.MouseOverControl == this || UIManager.MouseOverControl.RootParent == this))
            {
                IsLocked ^= true;
            }
        }

        public virtual void Save(XmlTextWriter writer)
        {
            writer.WriteAttributeString("type", ((int)GumpType).ToString());
            writer.WriteAttributeString("x", X.ToString());
            writer.WriteAttributeString("y", Y.ToString());
            writer.WriteAttributeString("serial", LocalSerial.ToString());
            writer.WriteAttributeString("serverSerial", ServerSerial.ToString());
            writer.WriteAttributeString("isLocked", isLocked.ToString());
            writer.WriteAttributeString("alphaOffset", AlphaOffset.ToString());
        }

        public void CenterXInScreen()
        {
            Rectangle windowBounds = Client.Game.Window.ClientBounds;
            if (ProfileManager.CurrentProfile.GlobalScaling)
            {
                float scale = ProfileManager.CurrentProfile.GlobalScale;
                // Convert physical width to unscaled (logical) width
                float logicalWidth = windowBounds.Width / scale;
                // Center in logical coordinates
                X = (int)((logicalWidth - Width) / 2);
            }
            else
            {
                X = (windowBounds.Width - Width) / 2;
            }
        }

        public void CenterYInScreen()
        {
            Rectangle windowBounds = Client.Game.Window.ClientBounds;
            if (ProfileManager.CurrentProfile.GlobalScaling)
            {
                float scale = ProfileManager.CurrentProfile.GlobalScale;
                float logicalHeight = windowBounds.Height / scale;
                Y = (int)((logicalHeight - Height) / 2);
            }
            else
            {
                Y = (windowBounds.Height - Height) / 2;
            }
        }

        public void CenterXInViewPort()
        {
            Camera camera = Client.Game.Scene.Camera;
            if (ProfileManager.CurrentProfile.GlobalScaling)
            {
                float scale = ProfileManager.CurrentProfile.GlobalScale;
                // Compute the camera's physical center, then convert to logical coordinates.
                float logicalCenterX = (camera.Bounds.X + camera.Bounds.Width / 2f);
                // Set element X so that its center aligns with the camera's logical center.
                X = (int)(logicalCenterX - ((Width / scale) / 2f));
            }
            else
            {
                X = camera.Bounds.X + ((camera.Bounds.Width - Width) / 2);
            }
        }

        public void CenterYInViewPort()
        {
            Camera camera = Client.Game.Scene.Camera;
            if (ProfileManager.CurrentProfile.GlobalScaling)
            {
                float scale = ProfileManager.CurrentProfile.GlobalScale;
                float logicalCenterY = (camera.Bounds.Y + camera.Bounds.Height / 2f);
                Y = (int)(logicalCenterY - ((Height / scale) / 2f));
            }
            else
            {
                Y = camera.Bounds.Y + ((camera.Bounds.Height - Height) / 2);
            }
        }

        public void SetInScreen()
        {
            Rectangle windowBounds = Client.Game.Window.ClientBounds;

            int halfWidth = Width / 2;
            int halfHeight = Height / 2;

            int newX = (int)MathHelper.Clamp(X, -halfWidth, windowBounds.Width - halfWidth);
            int newY = (int)MathHelper.Clamp(Y, -halfHeight, windowBounds.Height - halfHeight);

            X = newX;
            Y = newY;
        }

        public virtual void Restore(XmlElement xml)
        {
            if (bool.TryParse(xml.GetAttribute("isLocked"), out bool lockedStatus))
                IsLocked = lockedStatus;
            if (float.TryParse(xml.GetAttribute("alphaOffset"), out float alpha))
            {
                AlphaOffset = alpha;
                Alpha += alpha;
                foreach (Control c in Children)
                    c.Alpha += alpha;
            }
        }

        public void RequestUpdateContents() => InvalidateContents = true;

        protected virtual void UpdateContents()
        {
        }

        protected override void OnDragEnd(int x, int y)
        {
            Point position = Location;
            int halfWidth = Width - (Width >> 2);
            int halfHeight = Height - (Height >> 2);

            if (X < -halfWidth)
            {
                position.X = -halfWidth;
            }

            if (Y < -halfHeight)
            {
                position.Y = -halfHeight;
            }

            if (X > Client.Game.Window.ClientBounds.Width - (Width - halfWidth))
            {
                position.X = Client.Game.Window.ClientBounds.Width - (Width - halfWidth);
            }

            if (Y > Client.Game.Window.ClientBounds.Height - (Height - halfHeight))
            {
                position.Y = Client.Game.Window.ClientBounds.Height - (Height - halfHeight);
            }

            Location = position;
        }

        protected override void OnMove(int x, int y)
        {
            base.OnMove(x, y);

            SetInScreen();
        }

        protected virtual void OnLockedChanged()
        {
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y) => IsVisible && base.Draw(batcher, x, y);

        public override void OnButtonClick(int buttonID)
        {
            if (!IsDisposed && LocalSerial != 0)
            {
                var switches = new List<uint>();
                var entries = new List<Tuple<ushort, string>>();

                foreach (Control control in Children)
                {
                    switch (control)
                    {
                        case Checkbox checkbox when checkbox.IsChecked:
                            switches.Add(control.LocalSerial);

                            break;

                        case StbTextBox textBox:
                            entries.Add(new Tuple<ushort, string>((ushort)textBox.LocalSerial, textBox.Text));

                            break;
                    }
                }

                GameActions.ReplyGump
                (
                    World,
                    LocalSerial,
                    // Seems like MasterGump serial does not work as expected.
                    /*MasterGumpSerial != 0 ? MasterGumpSerial :*/ ServerSerial,
                    buttonID,
                    switches.ToArray(),
                    entries.ToArray()
                );

                if (CanMove)
                {
                    UIManager.SavePosition(ServerSerial, Location);
                }
                else
                {
                    UIManager.RemovePosition(ServerSerial);
                }

                Dispose();
            }
        }

        protected override void CloseWithRightClick()
        {
            if (!CanCloseWithRightClick)
            {
                return;
            }

            if (ServerSerial != 0)
            {
                OnButtonClick(0);
            }

            base.CloseWithRightClick();
        }

        public override void ChangePage(int pageIndex) =>
            // For a gump, Page is the page that is drawing.
            ActivePage = pageIndex;
    }
}
