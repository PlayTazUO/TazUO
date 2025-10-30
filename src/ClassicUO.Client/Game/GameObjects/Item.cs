// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Assets;
using Microsoft.Xna.Framework;
using MathHelper = ClassicUO.Utility.MathHelper;

namespace ClassicUO.Game.GameObjects
{
    public partial class Item : Entity
    {
        //private static readonly QueuedPool<Item> _pool = new QueuedPool<Item>(
        //    Constants.PREDICTABLE_CHUNKS * 3,
        //    i =>
        //    {
        //        i.IsDestroyed = false;
        //        i.Graphic = 0;
        //        i.Amount = 0;
        //        i.Container = 0xFFFF_FFFF;
        //        i._isMulti = false;
        //        i.Layer = 0;
        //        i.Price = 0;
        //        i.UsedLayer = false;
        //        i._displayedGraphic = null;
        //        i.X = 0;
        //        i.Y = 0;
        //        i.Z = 0;

        //        i.LightID = 0;
        //        i.MultiDistanceBonus = 0;
        //        i.Flags = 0;
        //        i.WantUpdateMulti = true;
        //        i.MultiInfo = null;
        //        i.MultiGraphic = 0;

        //        i.AlphaHue = 0;
        //        i.Name = null;
        //        i.Direction = 0;
        //        i.AnimIndex = 0;
        //        i.Hits = 0;
        //        i.HitsMax = 0;
        //        i.LastStepTime = 0;
        //        i.LastAnimationChangeTime = 0;

        //        i.Clear();

        //        i.IsClicked = false;
        //        i.IsDamageable = false;
        //        i.Offset = Vector3.Zero;
        //        i.HitsPercentage = 0;
        //        i.Opened = false;
        //        i.TextContainer?.Clear();
        //        i.IsFlipped = false;
        //        i.FrameInfo = Rectangle.Empty;
        //        i.ObjectHandlesStatus = ObjectHandlesStatus.NONE;
        //        i.AlphaHue = 0;
        //        i.AllowedToDraw = true;
        //        i.ExecuteAnimation = true;
        //        i.HitsRequest = HitsRequestStatus.None;

        //        i.ResetOriginalGraphic();
        //        i.MatchesHighlightData = false;
        //        i.HighlightHue = 0;
        //    }
        //);

        private ushort? _displayedGraphic;
        private bool _isMulti;

        /// <summary>
        /// Use this constructor for internal usage only, otherwise use the static Create method.
        /// </summary>
        /// <param name="world"></param>
        public Item(World world) : base(world, 0)
        {
            _isLight = ItemData.IsLight;
        }

        public bool IsCoin => Graphic == 0x0EEA || Graphic == 0x0EED || Graphic == 0x0EF0;

        public bool MatchesHighlightData;
        public Color HighlightColor = Color.White;
        public string HighlightName = string.Empty;
        public ushort DisplayedGraphic
        {
            get
            {
                if (_displayedGraphic.HasValue)
                {
                    return _displayedGraphic.Value;
                }

                if (IsCoin)
                {
                    if (Amount > 5)
                    {
                        return (ushort)(Graphic + 2);
                    }

                    if (Amount > 1)
                    {
                        return (ushort)(Graphic + 1);
                    }
                }
                else if (IsMulti)
                {
                    return MultiGraphic;
                }

                return Graphic;
            }
            set => _displayedGraphic = value;
        }

        public bool IsLocked => (Flags & Flags.Movable) == 0 && ItemData.Weight > 90;

        public bool IsMovable => (Flags & Flags.Movable) != 0;

        public ushort MultiGraphic { get; private set; }

        public bool IsMulti
        {
            get => _isMulti;
            set
            {
                _isMulti = value;

                if (!value)
                {
                    MultiDistanceBonus = 0;
                    MultiInfo = null;
                }
            }
        }

        public int MultiDistanceBonus { get; private set; }

        public bool IsCorpse => /*MathHelper.InRange(Graphic, 0x0ECA, 0x0ED2) ||*/
            Graphic == 0x2006;

        public bool IsHumanCorpse => IsCorpse &&
            MathHelper.InRange(Amount, 0x0190, 0x0193) ||
            MathHelper.InRange(Amount, 0x00B7, 0x00BA) ||
            MathHelper.InRange(Amount, 0x025D, 0x0260) ||
            MathHelper.InRange(Amount, 0x029A, 0x029B) ||
            MathHelper.InRange(Amount, 0x02B6, 0x02B7) ||
            Amount == 0x03DB ||
            Amount == 0x03DF ||
            Amount == 0x03E2 ||
            Amount == 0x02E8 ||
            Amount == 0x02E9;

        public bool OnGround => !SerialHelper.IsValid(Container);

        public uint RootContainer
        {
            get
            {
                Item item = this;

                while (SerialHelper.IsItem(item.Container))
                {
                    item = World.Items.Get(item.Container);

                    if (item == null)
                    {
                        return 0;
                    }
                }

                return SerialHelper.IsMobile(item.Container) ? item.Container : item;
            }
        }

        public uint BackpackOrRootContainer
        {
            get
            {
                Item last;
                Item item = last = this;

                while (SerialHelper.IsItem(item.Container))
                {
                    last = item;
                    item = World.Items.Get(item.Container);

                    if (item == null)
                    {
                        return 0;
                    }
                }

                return last;
            }
        }

        public ref StaticTiles ItemData =>
            ref Client.Game.UO.FileManager.TileData.StaticData[IsMulti ? MultiGraphic : Graphic];

        public bool IsLootable =>
            ItemData.Layer != (int)Layer.Hair
            && ItemData.Layer != (int)Layer.Beard
            && ItemData.Layer != (int)Layer.Face
            && Graphic != 0;

        public ushort Amount;
        public uint Container = 0xFFFF_FFFF;

        public bool IsDamageable;
        public Layer Layer;
        public byte LightID;

        public Rectangle? MultiInfo;
        public bool Opened;

        public uint Price;
        public bool UsedLayer;
        public bool WantUpdateMulti = true;

        private bool _isLight;
        private bool _wasCorpse; // Track if this item was previously a corpse

        public static Item Create(World world, uint serial)
        {
            var i = new Item(world); // _pool.GetOne();
            i.Serial = serial;

            return i;
        }

        public override void OnGraphicSet(ushort newGraphic)
        {
            base.OnGraphicSet(newGraphic);

            // Check if this item became a corpse or stopped being a corpse
            bool isNowCorpse = newGraphic == 0x2006;

            if (isNowCorpse && !_wasCorpse)
            {
                // Item became a corpse, add to corpse collection
                World.AddCorpse(this);
                _wasCorpse = true;
            }
            else if (!isNowCorpse && _wasCorpse)
            {
                // Item is no longer a corpse, remove from collection
                World.RemoveCorpse(this);
                _wasCorpse = false;
            }
        }

        public override void Destroy()
        {
            if (IsDestroyed)
            {
                return;
            }

            // Remove from corpse collection if this was a corpse
            if (_wasCorpse)
            {
                World.RemoveCorpse(this);
                _wasCorpse = false;
            }

            if (Opened)
            {
                UIManager.GetGump<ContainerGump>(Serial)?.Dispose();
                #region GridContainer
                UIManager.GetGump<GridContainer>(Serial)?.Dispose();
                #endregion
                UIManager.GetGump<SpellbookGump>(Serial)?.Dispose();
                UIManager.GetGump<MapGump>(Serial)?.Dispose();

                if (IsCorpse)
                {
                    UIManager.GetGump<GridLootGump>(Serial)?.Dispose();
                }

                UIManager.GetGump<BulletinBoardGump>(Serial)?.Dispose();
                UIManager.GetGump<SplitMenuGump>(Serial)?.Dispose();

                Opened = false;
            }

            base.Destroy();

            //_pool.ReturnOne(this);
        }

        private unsafe void LoadMulti()
        {
            WantUpdateMulti = false;

            short minX = 0;
            short minY = 0;
            short maxX = 0;
            short maxY = 0;

            if (!World.HouseManager.TryGetHouse(Serial, out House house))
            {
                house = new House(World, Serial, 0, false);
                World.HouseManager.Add(Serial, house);
            }
            else
            {
                house.ClearComponents();
            }

            bool movable = false;
            System.Collections.Generic.List<MultiInfo> multis = Client.Game.UO.FileManager.Multis.GetMultis(Graphic);

            for (int i = 0; i < multis.Count; ++i)
            {
                MultiInfo block = multis[i];

                if (block.X < minX)
                {
                    minX = block.X;
                }

                if (block.X > maxX)
                {
                    maxX = block.X;
                }

                if (block.Y < minY)
                {
                    minY = block.Y;
                }

                if (block.Y > maxY)
                {
                    maxY = block.Y;
                }

                if (block.IsVisible)
                {
                    var m = Multi.Create(World, block.ID);
                    m.MultiOffsetX = block.X;
                    m.MultiOffsetY = block.Y;
                    m.MultiOffsetZ = block.Z;
                    m.Hue = Hue;
                    m.AlphaHue = 255;
                    m.IsCustom = false;
                    m.State = CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_DONT_REMOVE;
                    m.IsMovable = ItemData.IsMultiMovable;

                    m.SetInWorldTile(
                        (ushort)(X + block.X),
                        (ushort)(Y + block.Y),
                        (sbyte)(Z + block.Z)
                    );

                    house.Components.Add(m);

                    if (m.ItemData.IsMultiMovable)
                    {
                        movable = true;
                    }
                }
                else if (i == 0)
                {
                    MultiGraphic = block.ID;
                }
            }

            MultiInfo = new Rectangle
            {
                X = minX,
                Y = minY,
                Width = maxX,
                Height = maxY
            };

            // hack to make baots movable.
            // Mast is not the main center in bigger boats, so if we got a movable multi --> makes all multi movable
            if (movable)
            {
                foreach (Multi m in house.Components)
                {
                    m.IsMovable = movable;
                }
            }

            MultiDistanceBonus = Math.Max(
                Math.Max(Math.Abs(minX), maxX),
                Math.Max(Math.Abs(minY), maxY)
            );

            house.Bounds = MultiInfo.Value;

            UIManager.GetGump<MiniMapGump>()?.RequestUpdateContents();

            if (World.HouseManager.EntityIntoHouse(Serial, World.Player))
            {
                Client.Game.GetScene<GameScene>()?.UpdateMaxDrawZ(true);
            }

            World.BoatMovingManager.ClearSteps(Serial);
        }

        public override void CheckGraphicChange(byte animIndex = 0)
        {
            if (!IsMulti)
            {
                if (!IsCorpse)
                {
                    AllowedToDraw = CanBeDrawn(World, Graphic);
                }
                else
                {
                    AnimIndex = 99;

                    if ((Direction & Direction.Running) != 0)
                    {
                        UsedLayer = true;
                        Direction &= (Direction)0x7F;
                    }
                    else
                    {
                        UsedLayer = false;
                    }

                    Layer = (Layer)Direction;
                    AllowedToDraw = true;
                }
            }
            else if (WantUpdateMulti)
            {
                if (
                    MultiDistanceBonus == 0
                    || World.HouseManager.IsHouseInRange(Serial, World.ClientViewRange)
                )
                {
                    LoadMulti();
                    AllowedToDraw = MultiGraphic > 2;
                }
            }

            _isLight = ItemData.IsLight;
        }

        public override void Update()
        {
            if (IsDestroyed)
            {
                return;
            }

            base.Update();

            ProcessAnimation();
        }
        public override ushort GetGraphicForAnimation()
        {
            ushort graphic = Graphic;

            if (Layer == Layer.Mount)
            {
                // ethereal unicorn
                if (graphic == 0x3E9B || graphic == 0x3E9D)
                {
                    return 0x00C0;
                }

                // ethereal kirin
                if (graphic == 0x3E9C)
                {
                    return 0x00BF;
                }

                if (Mounts.TryGet(graphic, out MountInfo mountInfo))
                {
                    graphic = mountInfo.Graphic;
                }

                if (ItemData.AnimID != 0)
                {
                    graphic = ItemData.AnimID;
                }
            }
            else if (IsCorpse)
            {
                return Amount;
            }

            return graphic;
        }

        public override void UpdateTextCoordsV()
        {
            if (TextContainer == null)
            {
                return;
            }

            var last = (TextObject)TextContainer.Items;

            while (last?.Next != null)
            {
                last = (TextObject)last.Next;
            }

            if (last == null)
            {
                return;
            }

            int offY = 0;

            if (OnGround)
            {
                Point p = RealScreenPosition;

                Rectangle bounds = Client.Game.UO.Arts.GetRealArtBounds(Graphic);
                p.Y -= bounds.Height >> 1;

                p.X += (int)Offset.X + 22;
                p.Y += (int)(Offset.Y - Offset.Z) + 22;

                p = Client.Game.Scene.Camera.WorldToScreen(p);

                for (; last != null; last = (TextObject)last.Previous)
                {
                    if (last.TextBox != null && !last.TextBox.IsDisposed)
                    {
                        if (offY == 0 && last.Time < Time.Ticks)
                        {
                            continue;
                        }

                        last.OffsetY = offY;
                        offY += last.TextBox.Height;

                        last.RealScreenPosition.X = p.X - (last.TextBox.Width >> 1);
                        last.RealScreenPosition.Y = p.Y - offY;
                    }
                }

                FixTextCoordinatesInScreen();
            }
            else
            {
                for (; last != null; last = (TextObject)last.Previous)
                {
                    if (last.TextBox != null && !last.TextBox.IsDisposed)
                    {
                        if (offY == 0 && last.Time < Time.Ticks)
                        {
                            continue;
                        }

                        last.OffsetY = offY;
                        offY += last.TextBox.Height;

                        last.RealScreenPosition.X = last.X - (last.TextBox.Width >> 1);
                        last.RealScreenPosition.Y = last.Y - offY;
                    }
                }
            }
        }

        public override void ProcessAnimation(bool evalutate = false)
        {
            if (!IsCorpse)
            {
                return;
            }

            byte dir = (byte)Layer;

            if (LastAnimationChangeTime < Time.Ticks)
            {
                byte frameIndex = (byte)(AnimIndex + (ExecuteAnimation ? 1 : 0));
                ushort id = GetGraphicForAnimation();

                bool mirror = false;

                Renderer.Animations.Animations animations = Client.Game.UO.Animations;
                animations.GetAnimDirection(ref dir, ref mirror);

                if (id < animations.MaxAnimationCount && dir < 5)
                {
                    animations.ConvertBodyIfNeeded(ref id);
                    AnimationGroupsType animGroup = animations.GetAnimType(id);
                    AnimationFlags animFlags = animations.GetAnimFlags(id);
                    byte action = Client.Game.UO.FileManager.Animations.GetDeathAction(
                        id,
                        animFlags,
                        animGroup,
                        UsedLayer
                    );
                    Span<Renderer.SpriteInfo> frames = animations.GetAnimationFrames(
                        id,
                        action,
                        dir,
                        out _,
                        out _,
                        isCorpse: true
                    );

                    if (frames.Length > 0)
                    {
                        // when the animation is done, stop to animate the corpse
                        if (frameIndex >= frames.Length)
                        {
                            frameIndex = (byte)(frames.Length - 1);
                        }

                        AnimIndex = (byte)(frameIndex % frames.Length);
                    }
                }

                LastAnimationChangeTime = Time.Ticks + Constants.CHARACTER_ANIMATION_DELAY;
            }
        }
    }
}
