using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using ClassicUO.Assets;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    public class DurabilityGumpMinimized : Gump
    {
        public uint Graphic { get; set; } = 5587;

        public DurabilityGumpMinimized(World world) : base(world, 0, 0)
        {
            SetTooltip("Open Equipment Durability Tracker");

            WantUpdateSize = true;
            AcceptMouseInput = true;
            Width = 30;
            Height = 30;
        }

        public override bool AcceptMouseInput => DurabilityManager.HasDurabilityData;

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            ref readonly SpriteInfo texture = ref Client.Game.UO.Gumps.GetGump(Graphic);

            if (texture.Texture != null && DurabilityManager.HasDurabilityData)
            {
                var rect = new Rectangle(x, y, Width, Height);
                batcher.Draw(texture.Texture, rect, texture.UV, ShaderHueTranslator.GetHueVector(0));
            }

            return base.Draw(batcher, x, y);
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left && DurabilityManager.HasDurabilityData)
            {
                UIManager.GetGump<DurabilitysGump>()?.Dispose();
                UIManager.Add(new DurabilitysGump(World));
            }
        }
    }

    internal class DurabilitysGump : NineSliceGump
    {
        private static int lastWidth = 300, lastHeight = 400;
        private static int lastX, lastY;

        private enum DurabilityColors
        {
            RED = 0x0805,
            BLUE = 0x0806,
            GREEN = 0x0808,
            YELLOW = 0x0809
        }

        private readonly Dictionary<string, ContextMenuItemEntry> _menuItems = new Dictionary<string, ContextMenuItemEntry>();
        private VBoxContainer _dataBox;
        public override GumpType GumpType => GumpType.DurabilityGump;

        public DurabilitysGump(World world) : base(world, lastX, lastY, lastWidth, lastHeight, ModernUIConstants.ModernUIPanel, ModernUIConstants.ModernUIPanel_BoderSize, true, 200, 200)
        {
            LayerOrder = UILayer.Default;
            CanCloseWithRightClick = true;
            CanMove = true;

            Width = lastWidth;
            Height = lastHeight;

            X = lastX;
            Y = lastY;

            if (lastX == 0 || lastY == 0)
            {
                X = lastX = (Client.Game.Scene.Camera.Bounds.Width - Width) / 2;
                Y = lastY = Client.Game.Scene.Camera.Bounds.Y + 20;
            }

            Build();
        }

        private void Build()
        {
            Clear();

            BuildHeader();

            var area = new ScrollArea(10, 30, Width - 20, Height - 50, true)
            {
                ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways
            };

            Add(area);

            _dataBox = new VBoxContainer(Width - 40);
            area.Add(_dataBox);

            RequestUpdateContents();
        }


        private void BuildHeader()
        {
            Label l = new ("Equipment Durability", true, 0xFF);
            l.X = (Width >> 1) - (l.Width >> 1);
            l.Y = (l.Height >> 1) >> 1;

            Add(l);
        }

        public override void Dispose()
        {
            base.Dispose();
            lastX = X;
            lastY = Y;
        }

        protected override void UpdateContents()
        {
            _dataBox.Clear();
            Rectangle barBounds = Client.Game.UO.Gumps.GetGump((uint)DurabilityColors.RED).UV;

            List<DurabiltyProp> items = World.DurabilityManager?.Durabilities ?? new List<DurabiltyProp>();

            foreach (DurabiltyProp durability in items.OrderBy(d => d.Percentage))
            {
                if (durability.MaxDurabilty <= 0)
                {
                    continue;
                }

                Item item = World.Items.Get((uint)durability.Serial);

                if (item == null)
                {
                    continue;
                }

                var a = new Area();
                a.AcceptMouseInput = true;
                a.WantUpdateSize = false;
                a.CanMove = true;
                a.Height = 44;
                a.Width = Width - (a.X * 2) - 40;

                Label name;
                a.Add(name = new Label($"{(string.IsNullOrWhiteSpace(item.Name) ? item.Layer : item.Name)}", true, 0xFFFF, ishtml: true));
                GumpPic red;
                a.Add(red = new GumpPic(0, name.Y + name.Height + 5, (ushort)DurabilityColors.RED, 0));

                DurabilityColors statusGump = DurabilityColors.GREEN;

                if (durability.Percentage < 0.7)
                {
                    statusGump = DurabilityColors.YELLOW;
                }
                else if (durability.Percentage < 0.95)
                {
                    statusGump = DurabilityColors.BLUE;
                }

                if (durability.Percentage > 0)
                {
                    a.Add(new GumpPicTiled(0, red.Y, (int)Math.Floor(barBounds.Width * durability.Percentage), barBounds.Height, (ushort)statusGump));
                }

                int durWidth = Client.Game.UO.FileManager.Fonts.GetWidthUnicode(0, $"{durability.Durabilty} / {durability.MaxDurabilty}");

                a.Add
                (
                    new Label($"{durability.Durabilty} / {durability.MaxDurabilty}", true, 0xFFFF)
                    {
                        Y = red.Y - 2,
                        X = Width - 38 - durWidth
                    }
                );

                a.MouseUp += (s, e) =>
                {
                    if (e.Button == MouseButtonType.Left && World.TargetManager.IsTargeting)
                    {
                        World.TargetManager.Target(item);
                    }
                };

                _dataBox.Add(a);
            }
        }

        protected override void OnResize(int oldWidth, int oldHeight, int newWidth, int newHeight)
        {
            base.OnResize(oldWidth, oldHeight, newWidth, newHeight);
            Build();
            lastWidth = newWidth;
            lastHeight = newHeight;
        }

        public override void Save(XmlTextWriter writer)
        {
            base.Save(writer);

            writer.WriteAttributeString("lastX", X.ToString());
            writer.WriteAttributeString("lastY", Y.ToString());
            writer.WriteAttributeString("lastWidth", lastWidth.ToString());
            writer.WriteAttributeString("lastHeight", lastHeight.ToString());
        }

        public override void Restore(XmlElement xml)
        {
            base.Restore(xml);

            int.TryParse(xml.GetAttribute("lastX"), out X);
            int.TryParse(xml.GetAttribute("lastY"), out Y);
            int.TryParse(xml.GetAttribute("lastWidth"), out Width);
            int.TryParse(xml.GetAttribute("lastHeight"), out Height);
            Build();
        }
    }
}
