using System;
using System.Collections.Generic;
using System.Xml;
using ClassicUO.Assets;
using ClassicUO.Configuration;
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

        /// <summary>
        /// Serial of the mobile whose durability this icon tracks. Zero means the local player.
        /// </summary>
        public uint WatchSerial { get; set; }

        private uint ResolvedWatchSerial => WatchSerial != 0 ? WatchSerial : (World.Player?.Serial ?? 0);

        public DurabilityGumpMinimized(World world) : base(world, 0, 0)
        {
            SetTooltip(TazLang.Get("durability_minimize_tooltip", "Open Equipment Durability Tracker"));

            WantUpdateSize = true;
            AcceptMouseInput = true;
            Width = 30;
            Height = 30;
        }

        public override bool AcceptMouseInput => World.DurabilityManager.HasDurabilityData(ResolvedWatchSerial);

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            ref readonly SpriteInfo texture = ref Client.Game.UO.Gumps.GetGump(Graphic);

            if (texture.Texture != null && World.DurabilityManager.HasDurabilityData(ResolvedWatchSerial))
            {
                var rect = new Rectangle(x, y, Width, Height);
                batcher.Draw(texture.Texture, rect, texture.UV, ShaderHueTranslator.GetHueVector(0));
            }

            return base.Draw(batcher, x, y);
        }

        public override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left && World.DurabilityManager.HasDurabilityData(ResolvedWatchSerial))
            {
                UIManager.GetGump<DurabilitysGump>()?.Dispose();
                UIManager.Add(new DurabilitysGump(World, ResolvedWatchSerial));
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
        private Label _titleLabel;
        public override GumpType GumpType => GumpType.DurabilityGump;

        /// <summary>
        /// Serial of the mobile whose equipment this tracker displays. Zero means the local player.
        /// </summary>
        private uint ResolvedWatchSerial => LocalSerial != 0 ? LocalSerial : (World.Player?.Serial ?? 0);

        public DurabilitysGump(World world, uint mobileSerial = 0) : base(world, lastX, lastY, lastWidth, lastHeight, ModernUIConstants.ModernUIPanel, ModernUIConstants.ModernUIPanel_BorderSize, true, 200, 200)
        {
            LayerOrder = UILayer.Default;
            CanCloseWithRightClick = true;
            CanMove = true;

            LocalSerial = mobileSerial;

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
            _titleLabel = new Label(TazLang.Get("durability_title", "Equipment Durability"), true, 0xFF);
            _titleLabel.X = (Width >> 1) - (_titleLabel.Width >> 1);
            _titleLabel.Y = (_titleLabel.Height >> 1) >> 1;

            Add(_titleLabel);
        }

        /// <summary>
        /// Prefixes the header with the watched mobile's name when it is not the local player, so a
        /// tracker opened for another mobile is self-explanatory.
        /// </summary>
        private void UpdateHeader(uint watchSerial)
        {
            string title = TazLang.Get("durability_title", "Equipment Durability");

            if (World.Player == null || watchSerial != World.Player.Serial)
            {
                Mobile mobile = World.Mobiles.Get(watchSerial);

                if (mobile != null && !string.IsNullOrWhiteSpace(mobile.Name))
                    title = $"{mobile.Name} - {title}";
            }

            if (_titleLabel.Text != title)
            {
                _titleLabel.Text = title;
                _titleLabel.X = (Width >> 1) - (_titleLabel.Width >> 1);
            }
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

            uint watchSerial = ResolvedWatchSerial;
            UpdateHeader(watchSerial);

            List<DurabilityInfo> items = World.DurabilityManager?.GetDurabilities(watchSerial) ?? new List<DurabilityInfo>();

            foreach (DurabilityInfo durability in items)
            {
                if (durability.Max <= 0)
                {
                    continue;
                }

                Item item = World.Items.Get(durability.Serial);

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

                int durWidth = Client.Game.UO.FileManager.Fonts.GetWidthUnicode(0, $"{durability.Current} / {durability.Max}");

                a.Add
                (
                    new Label($"{durability.Current} / {durability.Max}", true, 0xFFFF)
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
