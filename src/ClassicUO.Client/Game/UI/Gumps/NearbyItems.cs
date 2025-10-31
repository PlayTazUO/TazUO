using System;
using System.Collections.Generic;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    public class NearbyItems : Gump
    {
        public const int SIZE = 75;
        public static NearbyItems NearbyItemGump;

        private int playerX, playerY;

        private long openedTicks = Time.Ticks;
        public NearbyItems(World world) : base(world, 0, 0)
        {
            playerX = world.Player.X;
            playerY = world.Player.Y;
            NearbyItemGump?.Dispose();
            NearbyItemGump = this;
            CanCloseWithRightClick = true;
            CanMove = false;

            BuildGump();

            X = Mouse.Position.X - (Width / 2);
            Y = Mouse.Position.Y - (Height / 2);
        }

        public override void Update()
        {
            base.Update();

            if(!IsDisposed && (playerX != World.Player.X || playerY != World.Player.Y || Time.Ticks - openedTicks > 30000 ))
            {
                Dispose();
            }
        }

        private void BuildGump()
        {
            var items = new List<NearbyItemDisplay>();

            foreach (Item i in World.Items.Values)
            {
                if (!i.OnGround) continue;

                if (i.Distance > Constants.DRAG_ITEMS_DISTANCE) continue;

                if (i.IsLocked && !i.ItemData.IsContainer) continue;

                if(!i.IsLootable) continue;
                
                if(i.IsCorpse) continue;

                items.Add(new NearbyItemDisplay(World, i));
            }

            if (items.Count == 0)
            {
                Dispose();
                return;
            }

            int gridSize = (int)Math.Ceiling(Math.Sqrt(items.Count));

            int ii = 0;
            int nx = 0;
            int ny = 0;
            for (int row = 0; row < gridSize; row++)
            {
                for (int col = 0; col < gridSize; col++)
                {
                    if (ii >= items.Count) break;

                    items[ii].X = nx;
                    items[ii].Y = ny;
                    Add(items[ii]);
                    nx += SIZE;
                    ii++;
                }
                nx = 0;
                ny += SIZE;
            }

            Width = gridSize * SIZE;
            Height = gridSize * SIZE;
        }

        public override void Dispose()
        {
            base.Dispose();
            NearbyItemGump = null;
        }
    }

    public class NearbyItemDisplay : Control
    {
        private Point originalSize;
        private float scale = (ProfileManager.CurrentProfile.GridContainersScale / 100f);
        private Vector3 hueVector;
        private Rectangle realArtRectBounds;
        private Point point = new Point();
        private readonly SpriteInfo itemSpriteInfo;
        private readonly AlphaBlendControl background;


        public NearbyItemDisplay(World world, Item item)
        {
            Width = NearbyItems.SIZE;
            Height = NearbyItems.SIZE;
            background = new AlphaBlendControl() { Width = Width, Height = Height };
            originalSize = new Point(Width, Height);
            hueVector = ShaderHueTranslator.GetHueVector(item.Hue, item.ItemData.IsPartialHue, 1f);
            realArtRectBounds = Client.Game.UO.Arts.GetRealArtBounds((uint)item.DisplayedGraphic);
            itemSpriteInfo = Client.Game.UO.Arts.GetArt((uint)(item.DisplayedGraphic));

            var loot = new HitBox(0, 0, Width, Height / 2);
            loot.Add(TextBox.GetOne("Loot", TrueTypeLoader.EMBEDDED_FONT, 16, Color.White, TextBox.RTLOptions.DefaultCentered(Width)));
            loot.MouseUp += (s, e) =>
            {
                if(e.Button != MouseButtonType.Left) return;
                Client.Game.GetScene<GameScene>()?.MoveItemQueue.EnqueueQuick(item);
                Dispose();
            };
            Add(loot);

            var use = new HitBox(0, Height / 2, Width, Height / 2);
            TextBox tb;
            use.Add(tb = TextBox.GetOne("Use", TrueTypeLoader.EMBEDDED_FONT, 16, Color.White, TextBox.RTLOptions.DefaultCentered(Width)));
            tb.Y = use.Height - tb.MeasuredSize.Y;
            use.MouseUp += (s, e) =>
            {
                if (e.Button != MouseButtonType.Left) return;
                GameActions.DoubleClick(world, item);
            };
            Add(use);


            if (realArtRectBounds.Width < Width)
            {
                if (ProfileManager.CurrentProfile.GridContainerScaleItems)
                    originalSize.X = (ushort)(realArtRectBounds.Width * scale);
                else
                    originalSize.X = realArtRectBounds.Width;

                point.X = (Width >> 1) - (originalSize.X >> 1);
            }
            else if (realArtRectBounds.Width > Width)
            {
                if (ProfileManager.CurrentProfile.GridContainerScaleItems)
                    originalSize.X = (ushort)(Width * scale);
                else
                    originalSize.X = Width;
                point.X = (Width >> 1) - (originalSize.X >> 1);
            }

            if (realArtRectBounds.Height < Height)
            {
                if (ProfileManager.CurrentProfile.GridContainerScaleItems)
                    originalSize.Y = (ushort)(realArtRectBounds.Height * scale);
                else
                    originalSize.Y = realArtRectBounds.Height;

                point.Y = (Height >> 1) - (originalSize.Y >> 1);
            }
            else if (realArtRectBounds.Height > Height)
            {
                if (ProfileManager.CurrentProfile.GridContainerScaleItems)
                    originalSize.Y = (ushort)(Height * scale);
                else
                    originalSize.Y = Height;

                point.Y = (Height >> 1) - (originalSize.Y >> 1);
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            background.Draw(batcher, x, y);

            batcher.Draw
            (
                itemSpriteInfo.Texture,
                new Rectangle
                (
                    x + point.X,
                    y + point.Y,
                    originalSize.X,
                    originalSize.Y
            ),
            new Rectangle
            (
                    itemSpriteInfo.UV.X + realArtRectBounds.X,
                    itemSpriteInfo.UV.Y + realArtRectBounds.Y,
                    realArtRectBounds.Width,
                    realArtRectBounds.Height
                ),
                hueVector
            );

            base.Draw(batcher, x, y);

            return true;
        }
    }
}
