// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Utility;

namespace ClassicUO.Game.UI.Gumps
{
    public class PopupMenuGump : Gump
    {
        public static uint CloseNext = uint.MaxValue;

        private ushort _selectedItem;
        private readonly PopupMenuData _data;

        public PopupMenuData Data => _data;

        public PopupMenuGump(World world, PopupMenuData data) : base(world, 0, 0)
        {
            if (CloseNext != uint.MaxValue && data.Serial == CloseNext)
            {
                Dispose();
                CloseNext = uint.MaxValue;
                return;
            }

            CanMove = false;
            CanCloseWithRightClick = true;
            _data = data;

            double scale = ProfileManager.CurrentProfile?.ContextMenuScale ?? 1.0;

            var pic = new ResizePic(0x0A3C)
            {
                Alpha = 0.75f
            };

            Add(pic);
            int offsetY = ScaleHelper.Scaled(10, scale);
            bool arrowAdded = false;
            int width = 0, height = ScaleHelper.Scaled(20, scale);

            for (int i = 0; i < data.Items.Length; i++)
            {
                ref PopupMenuItem item = ref data.Items[i];

                string text = Client.Game.UO.FileManager.Clilocs.GetString(item.Cliloc);

                ushort hue = item.Hue;

                if (item.ReplacedHue != 0)
                {
                    uint h = (HuesHelper.Color16To32(item.ReplacedHue) << 8) | 0xFF;

                    Client.Game.UO.FileManager.Fonts.SetUseHTML(true, h);
                }

                var label = new Label(text, true, hue, font: 1);
                label.ApplyScale(scale, scalePosition: false);
                label.X = ScaleHelper.Scaled(10, scale);
                label.Y = offsetY;

                Client.Game.UO.FileManager.Fonts.SetUseHTML(false);

                var box = new HitBox(ScaleHelper.Scaled(10, scale), offsetY, label.Width, label.Height)
                {
                    Tag = item.Index
                };

                box.MouseEnter += (sender, e) =>
                {
                    _selectedItem = (ushort)(sender as HitBox).Tag;
                };

                Add(box);
                Add(label);

                if ((item.Flags & 0x02) != 0 && !arrowAdded)
                {
                    arrowAdded = true;

                    // TODO: wat?
                    var arrow = new Button(0, 0x15E6, 0x15E2, 0x15E2)
                    {
                        X = ScaleHelper.Scaled(20, scale),
                        Y = offsetY
                    };
                    arrow.ApplyScale(scale, scalePosition: false);

                    Add(arrow);

                    height += ScaleHelper.Scaled(20, scale);
                }

                offsetY += label.Height;

                if (!arrowAdded)
                {
                    height += label.Height;

                    if (width < label.Width)
                    {
                        width = label.Width;
                    }
                }
            }

            width += ScaleHelper.Scaled(20, scale);

            if (height <= ScaleHelper.Scaled(10, scale) || width <= ScaleHelper.Scaled(20, scale))
            {
                Dispose();
            }
            else
            {
                pic.Width = width;
                pic.Height = height;

                foreach (HitBox box in FindControls<HitBox>())
                {
                    box.Width = width - ScaleHelper.Scaled(20, scale);
                }
            }
        }

        public override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                if (CUOEnviroment.Debug)
                    GameActions.Print(World, $"Popup menu [{_data.Serial}] response: {_selectedItem}");

                GameActions.ResponsePopupMenu(_data.Serial, _selectedItem);
                Dispose();
            }
        }
    }
}
