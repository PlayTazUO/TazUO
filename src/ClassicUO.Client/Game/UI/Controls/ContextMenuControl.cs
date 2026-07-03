// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace ClassicUO.Game.UI.Controls
{
    public class ContextMenuControl
    {
        private readonly List<ContextMenuItemEntry> _items;
        private readonly Gump _gump;

        public ContextMenuControl(Gump gump)
        {
            _items = new List<ContextMenuItemEntry>();
            _gump = gump;
        }

        public void Add(string text, Action action, bool canBeSelected = false, bool defaultValue = false) => _items.Add(new ContextMenuItemEntry(text, action, canBeSelected, defaultValue));

        public void Add(ContextMenuItemEntry entry) => _items.Add(entry);

        public void Add(string text, List<ContextMenuItemEntry> entries) => _items.Add
            (
                new ContextMenuItemEntry(text)
                {
                    Items = entries
                }
            );

        public void Show()
        {
            UIManager.ShowContextMenu(null);

            if (_items.Count == 0)
            {
                return;
            }

            UIManager.ShowContextMenu
            (
                new ContextMenuShowMenu(_gump.World, _items)
            );
        }

        public void Dispose()
        {
            UIManager.ShowContextMenu(null);
            _items.Clear();
        }
    }

    public class ContextMenuItemEntry
    {
        public ContextMenuItemEntry(string text, Action action = null, bool canBeSelected = false, bool defaultValue = false)
        {
            Text = text;
            Action = action;
            CanBeSelected = canBeSelected;
            IsSelected = defaultValue;
        }

        public Action Action;
        public readonly bool CanBeSelected;
        public bool IsSelected;
        public List<ContextMenuItemEntry> Items = new List<ContextMenuItemEntry>();
        public string Text;

        public void Add(ContextMenuItemEntry subEntry) => Items.Add(subEntry);
    }


    public class ContextMenuShowMenu : Gump
    {
        private readonly AlphaBlendControl _background;
        private readonly ScrollArea _scroll;
        private List<ContextMenuShowMenu> _subMenus;
        private readonly double _scale;

        internal int MenuWidth => _background.Width;
        internal int MenuHeight => _background.Height;


        public ContextMenuShowMenu(World world, List<ContextMenuItemEntry> list) : base(world, 0, 0)
        {
            WantUpdateSize = true;
            ModalClickOutsideAreaClosesThisControl = true;
            IsModal = true;
            LayerOrder = UILayer.Over;

            CanMove = false;
            AcceptMouseInput = true;

            _scale = ProfileManager.CurrentProfile?.ContextMenuScale ?? 1.0;


            _background = new AlphaBlendControl(0.7f);
            Add(_background);

            _scroll = new ScrollArea(0, 0, 0, 0, true);
            Add(_scroll);

            int y = 0;

            for (int i = 0; i < list.Count; i++)
            {
                var item = new ContextMenuItem(this, list[i], _scale);

                if (i > 0)
                {
                    item.Y = y;
                }

                if (_background.Width < item.Width)
                {
                    _background.Width = item.Width;
                }

                _background.Height += item.Height;

                _scroll.Add(item);

                y += item.Height;
            }

            // The window's client bounds are in physical pixels, but the menu's
            // coordinates and size (and Mouse.Position) live in logical UI space,
            // which the global RenderScale maps onto the screen. The context menu
            // scale is already baked into _background.Width/Height. Convert the
            // window bounds into that same logical space so the menu stays on
            // screen regardless of global or context menu scaling.
            int windowWidth = (int)(Client.Game.Window.ClientBounds.Width / Client.Game.RenderScale);
            int windowHeight = (int)(Client.Game.Window.ClientBounds.Height / Client.Game.RenderScale);

            if (y >= windowHeight >> 1)
            {
                y = windowHeight >> 1;
            }

            _scroll.Height = Height = _background.Height = y;
            _scroll.Width = _background.Width;

            X = Mouse.Position.X + 5;
            Y = Mouse.Position.Y - 20;

            if (X + _background.Width > windowWidth)
            {
                X = windowWidth - _background.Width;
            }

            if (X < 0)
            {
                X = 0;
            }

            if (Y + _background.Height > windowHeight)
            {
                Y = windowHeight - _background.Height;
            }

            if (Y < 0)
            {
                Y = 0;
            }

            foreach (ContextMenuItem mitem in _scroll.FindControls<ContextMenuItem>())
            {
                if (mitem.Width < _background.Width)
                {
                    mitem.Width = _background.Width;
                }
            }
        }


        // The height cap applied in the constructor is frozen against the window
        // size at that moment. If the game scale (RenderScale) or the window is
        // changed while the menu is open, that stale cap can be taller than the
        // current logical window and the menu spills off screen. Re-clamp against
        // the live logical window height so it can only ever shrink to fit; the
        // context menu scale is already baked into these heights, so both scales
        // are accounted for. Only shrinks, never grows past the constructor cap.
        internal void ClampHeightToWindow(int windowHeight)
        {
            if (windowHeight < 0)
            {
                windowHeight = 0;
            }

            if (_background.Height > windowHeight)
            {
                _scroll.Height = Height = _background.Height = windowHeight;
            }
        }

        public override void Update()
        {
            base.Update();
            WantUpdateSize = true;

            // Submenus that have no room to the right open to the left of this menu and
            // therefore live at a negative X (and, near the bottom of the screen, can sit
            // above it at a negative Y). base.Update only ever grows the bounds rectangle
            // right/down, and Control.HitTest gates on that rectangle before descending
            // into children, so those left/up regions are unreachable: moving the mouse
            // onto a left-opening submenu is treated as leaving the menu and closes it.
            // Extend the hit-test bounds to cover any negative-offset children without
            // moving anything that is drawn (this gump is drawn at its raw X/Y, so the
            // offset only affects hit testing).
            int left = 0, top = 0;

            for (int i = 0; i < Children.Count; i++)
            {
                IGui c = Children[i];

                if (c == null || c.IsDisposed || !c.IsVisible)
                {
                    continue;
                }

                if (c.X < left)
                {
                    left = c.X;
                }

                if (c.Y < top)
                {
                    top = c.Y;
                }
            }

            SetHitTestOffset(left, top);

            // Keep the right/bottom edges where base.Update put them while pushing the
            // left/top edges out to include the negative-offset children.
            if (left < 0)
            {
                Width -= left;
            }

            if (top < 0)
            {
                Height -= top;
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);

            batcher.DrawRectangle
            (
                SolidColorTextureCache.GetTexture(Color.Gray),
                x - 1,
                y - 1,
                _background.Width + 1,
                _background.Height + 1,
                hueVector
            );

            return base.Draw(batcher, x, y);
        }

        public override bool Contains(int x, int y)
        {
            if (_background.Bounds.Contains(x, y))
            {
                return true;
            }

            if (_subMenus != null)
            {
                foreach (ContextMenuShowMenu menu in _subMenus)
                {
                    if (menu.Contains(x - menu.X, y - menu.Y))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private class ContextMenuItem : Control
        {
            private static readonly RenderedText _moreMenuLabel = RenderedText.Create(">", 0xFFFF, isunicode: true, style: FontStyle.BlackBorder);
            private readonly ContextMenuItemEntry _entry;
            private readonly Label _label;
            private readonly GumpPic _selectedPic;
            private readonly ContextMenuShowMenu _subMenu;
            private readonly ContextMenuShowMenu _gump;
            private readonly double _scale;


            public ContextMenuItem(ContextMenuShowMenu parent, ContextMenuItemEntry entry, double scale = 1.0)
            {
                _gump = parent;
                _scale = scale;
                CanCloseWithRightClick = false;
                _entry = entry;

                _label = new Label
                (
                    entry.Text,
                    true,
                    0xFFFF,
                    0,
                    style: FontStyle.BlackBorder
                )
                {
                    X = 25
                };

                _label.ApplyScale(_scale);

                Add(_label);


                _selectedPic = new GumpPic(3, 0, 0x838, 0)
                {
                    IsVisible = entry.IsSelected,
                    IsEnabled = false
                };

                _selectedPic.ApplyScale(_scale);

                Add(_selectedPic);

                Height = (int)(25 * _scale);


                _label.Y = (Height >> 1) - (_label.Height >> 1);

                if (_selectedPic != null)
                {
                    //_label.X = _selectedPic.X + _selectedPic.Width + 6;
                    _selectedPic.Y = (Height >> 1) - (_selectedPic.Height >> 1);
                }

                Width = _label.X + _label.Width + (int)(25 * _scale);

                if (Width < (int)(100 * _scale))
                {
                    Width = (int)(100 * _scale);
                }

                // The parent ScrollArea always clips the right edge by its scrollbar
                // gutter, so reserve that space here; otherwise the submenu arrow (and
                // any right-aligned content) gets clipped away - which is especially
                // visible once global scaling forces the menu to scroll.
                Width += ScrollArea.SCROLLBAR_WIDTH;

                // it is a bit tricky, but works :D
                if (_entry.Items != null && _entry.Items.Count != 0)
                {
                    _subMenu = new ContextMenuShowMenu(_gump.World, _entry.Items);
                    parent.Add(_subMenu);

                    if (parent._subMenus == null)
                    {
                        parent._subMenus = new List<ContextMenuShowMenu>();
                    }

                    parent._subMenus.Add(_subMenu);
                }

                WantUpdateSize = false;
            }


            public override void Update()
            {
                base.Update();

                if (Width > _label.Width)
                {
                    _label.Width = Width;
                }

                if (_selectedPic != null)
                {
                    _selectedPic.IsVisible = _entry.IsSelected;
                }

                if (_subMenu != null)
                {
                    // Bounds are in physical pixels; submenu coordinates live in the
                    // same logical UI space as the parent, so scale by RenderScale.
                    int windowWidth = (int)(Client.Game.Window.ClientBounds.Width / Client.Game.RenderScale);
                    int windowHeight = (int)(Client.Game.Window.ClientBounds.Height / Client.Game.RenderScale);

                    // The submenu is a child of _gump, so its X/Y are relative to _gump.
                    // Clamp against the parent menu's absolute on-screen position instead
                    // of its raw X/Y: for the root menu these are identical, but for a
                    // nested submenu (a submenu opened from another submenu) _gump.X/Y are
                    // only relative to that submenu's own parent, so using them would
                    // under-count the real offset and let deep submenus spill off the
                    // window edges. ScreenCoordinate* accumulates every ancestor offset.
                    int parentScreenX = _gump.ScreenCoordinateX;
                    int parentScreenY = _gump.ScreenCoordinateY;

                    // Keep the submenu no taller than the current logical window so a
                    // game-scale or window-size change after it was built cannot leave
                    // it overflowing. MenuHeight below then reflects the clamped size.
                    _subMenu.ClampHeightToWindow(windowHeight);

                    // Open to the right by default, but flip to the left of the parent
                    // menu if the submenu would run off the right edge of the window.
                    if (parentScreenX + Width + _subMenu.MenuWidth > windowWidth && parentScreenX - _subMenu.MenuWidth >= 0)
                    {
                        _subMenu.X = -_subMenu.MenuWidth;
                    }
                    else
                    {
                        _subMenu.X = Width;
                    }

                    // Keep the submenu inside the window vertically.
                    int subMenuY = Y;

                    if (parentScreenY + subMenuY + _subMenu.MenuHeight > windowHeight)
                    {
                        subMenuY = windowHeight - _subMenu.MenuHeight - parentScreenY;
                    }

                    if (parentScreenY + subMenuY < 0)
                    {
                        subMenuY = -parentScreenY;
                    }

                    _subMenu.Y = subMenuY;

                    if (MouseIsOver)
                    {
                        _subMenu.IsVisible = true;
                    }
                    else
                    {
                        IGui p = UIManager.MouseOverControl?.Parent;

                        while (p != null)
                        {
                            if (p == _subMenu)
                            {
                                break;
                            }

                            p = p.Parent;
                        }

                        _subMenu.IsVisible = p != null;
                    }
                }
            }

            public override void OnMouseUp(int x, int y, MouseButtonType button)
            {
                if (button == MouseButtonType.Left)
                {
                    _entry.Action?.Invoke();

                    RootParent?.Dispose();

                    if (_entry.CanBeSelected)
                    {
                        _entry.IsSelected = !_entry.IsSelected;
                        _selectedPic.IsVisible = _entry.IsSelected;
                    }

                    Mouse.CancelDoubleClick = true;
                    Mouse.LastLeftButtonClickTime = 0;
                    base.OnMouseUp(x, y, button);
                }
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                if (!string.IsNullOrWhiteSpace(_label.Text) && MouseIsOver)
                {
                    Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);

                    batcher.Draw
                    (
                        SolidColorTextureCache.GetTexture(Color.Gray),
                        new Rectangle
                        (
                            x + 2,
                            y + 5,
                            Width - 4,
                            Height - 10
                        ),
                        hueVector
                    );
                }

                base.Draw(batcher, x, y);

                if (_entry.Items != null && _entry.Items.Count != 0)
                {
                    // Keep the arrow left of the ScrollArea's clipped scrollbar gutter
                    // so it stays visible at any global/context menu scale.
                    _moreMenuLabel.Draw(batcher, x + Width - ScrollArea.SCROLLBAR_WIDTH - (int)((_moreMenuLabel.Width + 5) * _scale), y + (Height >> 1) - ((int)(_moreMenuLabel.Height * _scale) >> 1) - 1, _scale);
                }

                return true;
            }
        }
    }
}
