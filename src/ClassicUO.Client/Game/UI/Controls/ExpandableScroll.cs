// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Input;
using Microsoft.Xna.Framework;
using System;

namespace ClassicUO.Game.UI.Controls
{
    public class ExpandableScroll : Control
    {
        private const int c_ExpandableScrollHeight_Min = 274;
        private const int c_ExpandableScrollHeight_Max = 800;
        private const int c_GumplingExpanderY_Offset = 2; // this is the gap between the pixels of the btm Control texture and the height of the btm Control texture.
        private const int c_GumplingExpander_ButtonID = 0x7FBEEF;
        private readonly GumpPic _gumpBottom;
        private Button _gumpExpander;
        private GumpPic _gumplingTitle;
        private int _gumplingTitleGumpID;
        private bool _gumplingTitleGumpIDDelta;
        private readonly GumpPicTiled _gumpMiddle;
        private readonly GumpPicTiled _gumpRight;
        private readonly GumpPic _gumpTop;
        private bool _isExpanding;
        private readonly bool _isResizable = true;
        private Point _lastExpanderPosition;

        // Gump scale baked into the scroll's graphics/layout. SpecialHeight is always kept in design
        // (unscaled) space so persisted heights stay valid regardless of the current scale.
        private readonly double _scale = 1.0;
        private int S(int v) => (int)(v * _scale);

        public event EventHandler SizeChanged;

        public ExpandableScroll(int x, int y, int height, ushort graphic, bool isResizable = true, double scale = 1.0)
        {
            X = x;
            Y = y;
            SpecialHeight = height;
            _isResizable = isResizable;
            _scale = scale <= 0 ? 1.0 : scale;
            CanMove = true;
            AcceptMouseInput = true;

            int width = 0;

            int w0 = 0,
                w1 = 0,
                w3 = 0;

            for (int i = 0; i < 4; i++)
            {
                ref readonly Renderer.SpriteInfo gumpInfo = ref Client.Game.UO.Gumps.GetGump((ushort)(graphic + i));

                if (gumpInfo.Texture == null)
                {
                    Dispose();

                    return;
                }

                if (gumpInfo.UV.Width > width)
                {
                    width = gumpInfo.UV.Width;
                }

                if (i == 0)
                {
                    w0 = gumpInfo.UV.Width;
                }
                else if (i == 1)
                {
                    w1 = gumpInfo.UV.Width;
                }
                else if (i == 3)
                {
                    w3 = gumpInfo.UV.Width;
                }
            }

            Add(_gumpTop = new GumpPic(0, 0, graphic, 0));

            Add(_gumpRight = new GumpPicTiled(0, 0, 0, 0, (ushort)(graphic + 1)));

            Add(_gumpMiddle = new GumpPicTiled(0, 0, 0, 0, (ushort)(graphic + 2)));

            Add(_gumpBottom = new GumpPic(0, 0, (ushort)(graphic + 3), 0));

            if (_isResizable)
            {
                Add(
                    _gumpExpander = new Button(c_GumplingExpander_ButtonID, 0x082E, 0x82F)
                    {
                        ButtonAction = ButtonAction.Activate,
                        X = 0,
                        Y = 0
                    }
                );

                _gumpExpander.MouseDown += expander_OnMouseDown;
                _gumpExpander.MouseUp += expander_OnMouseUp;
            }

            // Bake the gump scale into every part's size (positions are (re)computed below / in
            // RepositionElements). Guarded so the default scale of 1.0 leaves behaviour untouched.
            if (_scale != 1.0)
            {
                _gumpTop.ApplyScale(_scale, scalePosition: false);
                _gumpRight.ApplyScale(_scale, scalePosition: false);
                _gumpMiddle.ApplyScale(_scale, scalePosition: false);
                _gumpBottom.ApplyScale(_scale, scalePosition: false);
                _gumpExpander?.ApplyScale(_scale, scalePosition: false);

                // The side/body pieces are tiled; scale the tile so their baked-in edges land on the
                // scaled width instead of repeating at the native width.
                _gumpRight.ScaleTiledTexture = true;
                _gumpMiddle.ScaleTiledTexture = true;
            }

            int off = w0 - w3;

            if (_scale == 1.0)
            {
                _gumpRight.X = _gumpMiddle.X = (width - w1) / 2;
            }
            else
            {
                // Center each tiled piece by its own (scaled) width. The original shares the w1-based
                // offset for both pieces; when the body width differs from w1 that tiny off-center error
                // gets multiplied by the scale and becomes visible, so re-center from each real width.
                _gumpRight.X = (S(width) - _gumpRight.Width) / 2;
                _gumpMiddle.X = (S(width) - _gumpMiddle.Width) / 2;
            }

            _gumpRight.Y = _gumpMiddle.Y = _gumplingMidY;
            _gumpRight.Height = _gumpMiddle.Height = _gumplingMidHeight;
            _gumpRight.WantUpdateSize = _gumpMiddle.WantUpdateSize = true;
            _gumpBottom.X = S((off / 2) + (off / 4));

            Width = S(width);

            RepositionElements();

            WantUpdateSize = true;
        }

        private int _gumplingMidY => _gumpTop.Height;

        private int _gumplingMidHeight =>
            S(SpecialHeight) - _gumpTop.Height - _gumpBottom.Height - (_gumpExpander?.Height ?? 0);

        private int _gumplingBottomY =>
            S(SpecialHeight) - _gumpBottom.Height - (_gumpExpander?.Height ?? 0);

        private int _gumplingExpanderX => (Width - (_gumpExpander?.Width ?? 0)) >> 1;

        private int _gumplingExpanderY =>
            S(SpecialHeight) - (_gumpExpander?.Height ?? 0) - S(c_GumplingExpanderY_Offset);

        public int TitleGumpID
        {
            set
            {
                _gumplingTitleGumpID = value;
                _gumplingTitleGumpIDDelta = true;
            }
        }

        public int SpecialHeight { get; set; }

        public ushort Hue
        {
            get => _gumpTop.Hue;
            set => _gumpTop.Hue = _gumpBottom.Hue = _gumpMiddle.Hue = _gumpRight.Hue = value;
        }

        public override void Dispose()
        {
            if (_gumpExpander != null)
            {
                _gumpExpander.MouseDown -= expander_OnMouseDown;
                _gumpExpander.MouseUp -= expander_OnMouseUp;
                _gumpExpander.Dispose();
                _gumpExpander = null;
            }

            base.Dispose();
        }

        public override bool Contains(int x, int y)
        {
            x += ScreenCoordinateX;
            y += ScreenCoordinateY;

            IGui c = null;

            _gumpTop.HitTest(x, y, ref c);

            if (c != null)
            {
                return true;
            }

            _gumpMiddle.HitTest(x, y, ref c);

            if (c != null)
            {
                return true;
            }

            _gumpRight.HitTest(x, y, ref c);

            if (c != null)
            {
                return true;
            }

            _gumpBottom.HitTest(x, y, ref c);

            if (c != null)
            {
                return true;
            }

            _gumpExpander.HitTest(x, y, ref c);

            if (c != null)
            {
                return true;
            }

            return false;
        }

        public override void Update()
        {
            if (Mouse.LButtonPressed && _isExpanding)
            {
                // Mouse movement is in scaled (logical) space; SpecialHeight is in design space.
                SpecialHeight += (int)((Mouse.Position.Y - _lastExpanderPosition.Y) / _scale);
                _lastExpanderPosition = Mouse.Position;

                RepositionElements();
                WantUpdateSize = true;
                Parent?.OnPageChanged();
                SizeChanged?.Invoke(this, null);
            }

            if (_gumplingTitleGumpIDDelta)
            {
                _gumplingTitleGumpIDDelta = false;

                _gumplingTitle?.Dispose();
                Add(_gumplingTitle = new GumpPic(0, 0, (ushort)_gumplingTitleGumpID, 0));

                if (_scale != 1.0)
                    _gumplingTitle.ApplyScale(_scale, scalePosition: false);

                RepositionElements();
            }

            base.Update();
        }

        private void RepositionElements()
        {
            if (SpecialHeight < c_ExpandableScrollHeight_Min)
            {
                _lastExpanderPosition.Y += c_ExpandableScrollHeight_Min - SpecialHeight;
                SpecialHeight = c_ExpandableScrollHeight_Min;
            }

            if (SpecialHeight > c_ExpandableScrollHeight_Max)
            {
                _lastExpanderPosition.Y -= SpecialHeight - c_ExpandableScrollHeight_Max;
                SpecialHeight = c_ExpandableScrollHeight_Max;
            }

            //TOP
            _gumpTop.X = 0;
            _gumpTop.Y = 0;
            _gumpTop.WantUpdateSize = true;
            //MIDDLE
            _gumpRight.Y = _gumpMiddle.Y = _gumplingMidY;
            _gumpRight.Height = _gumpMiddle.Height = _gumplingMidHeight;
            _gumpRight.WantUpdateSize = _gumpMiddle.WantUpdateSize = true;
            //BOTTOM
            _gumpBottom.Y = _gumplingBottomY;
            _gumpBottom.WantUpdateSize = true;

            if (_isResizable)
            {
                _gumpExpander.X = _gumplingExpanderX;
                _gumpExpander.Y = _gumplingExpanderY;
                _gumpExpander.WantUpdateSize = true;
            }

            if (_gumplingTitle != null)
            {
                _gumplingTitle.X = (_gumpTop.Width - _gumplingTitle.Width) >> 1;
                _gumplingTitle.Y = (_gumpTop.Height - _gumplingTitle.Height) >> 1;
                _gumplingTitle.WantUpdateSize = true;
            }
        }

        private void expander_OnMouseDown(object sender, MouseEventArgs args)
        {
            if (args.Button == MouseButtonType.Left)
            {
                _isExpanding = true;
                _lastExpanderPosition = Mouse.Position;
            }
        }

        private void expander_OnMouseUp(object sender, MouseEventArgs args)
        {
            _isExpanding = false;
            RepositionElements();
            SizeChanged?.Invoke(this, null);
        }
    }
}
