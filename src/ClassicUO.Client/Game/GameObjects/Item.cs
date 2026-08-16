// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Globalization;
using System.Threading.Tasks;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Assets;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;
using MathHelper = ClassicUO.Utility.MathHelper;

namespace ClassicUO.Game.GameObjects
{
    public partial class Item : Entity
    {
        private ushort? _displayedGraphic;

        /// <summary>
        /// Use this constructor for internal usage only, otherwise use the static Create method.
        /// </summary>
        /// <param name="world"></param>
        public Item(World world) : base(world, 0)
        {
            if(!Client.UnitTestingActive)
                _isLight = ItemData.IsLight;
        }

        public bool IsCoin => Graphic == 0x0EEA || Graphic == 0x0EED || Graphic == 0x0EF0;

        public bool MatchesHighlightData;
        public Color HighlightColor = Color.White;
        public string HighlightName = string.Empty;
        public bool ShouldAutoLoot;
        public bool HighlightChecked;
        public string CustomName { get; set; }

        /// <summary>
        /// The OPL (Object Property List) name for this item, cached when OPL data is received.
        /// May be null if OPL data has not yet been received.
        /// </summary>
        public string OPLName { get; set; }

        /// <summary>
        /// The OPL (Object Property List) data (tooltip body) for this item, cached when OPL data is received.
        /// May be null if OPL data has not yet been received.
        /// </summary>
        public string OPLData { get; set; }

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
                    if (Amount > 5) return (ushort)(Graphic + 2);

                    if (Amount > 1) return (ushort)(Graphic + 1);
                }
                else if (IsMulti) return MultiGraphic;

                return Graphic;
            }
            set => _displayedGraphic = value;
        }

        public bool IsLocked => (Flags & Flags.Movable) == 0 && ItemData.Weight > 90;

        public bool IsMovable => (Flags & Flags.Movable) != 0;

        public ushort MultiGraphic { get; private set; }

        public bool IsMulti
        {
            get;
            set
            {
                field = value;

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
                Item last = null;
                Item item = this;
                while (SerialHelper.IsItem(item.Container))
                {
                    item = World.Items.Get(item.Container);
                    if (item == null)
                    {
                        break;
                    }
                    
                    last = item;
                }

                // Never return the item itself
                return last?.Serial ?? 0;
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

        /// <summary>
        /// The serial of the parent mobile this corpse belongs to.
        /// Set via the DisplayDeath packet when this item is a corpse.
        /// </summary>
        public uint CorpseParent;

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
            i.TryGetCustomName();

            // If OPL data was already received before this item existed, cache it now.
            if (world.OPL.TryGetNameAndData(serial, out string oplName, out string oplData))
            {
                i.OPLName = oplName;
                i.OPLData = oplData;
            }

            return i;
        }

        public async void TryGetCustomName()
        {
            string name = await ItemDatabaseManager.Instance.GetItemCustomName(Serial);
            CustomName = name ?? "";
        }

        /// <summary>
        /// Gets a display friendly name for this item, preferring the cached OPL name
        /// (received from the server) and falling back to the item data name.
        /// </summary>
        /// <param name="showAmount">
        /// When true, the stack size is prefixed to the name (e.g. "5 Gold Coins") for stacks
        /// greater than one. When false, any leading stack size is stripped so only the name remains.
        /// </param>
        /// <returns>The normalized item name, or an empty string if no name is available.</returns>
        public string GetNormalizedName(bool showAmount)
        {
            // Prefer the OPL name cached when OPL data was received (easier/faster lookup).
            string name = OPLName;

            if (string.IsNullOrEmpty(name))
            {
                // Fall back to the server-assigned entity name, then the item data name
                // (adjusted for plurality).
                name = !string.IsNullOrEmpty(Name)
                    ? Name
                    : StringHelper.CapitalizeAllWords(StringHelper.GetPluralAdjustedString(ItemData.Name, Amount > 1));
            }

            if (string.IsNullOrEmpty(name))
                return string.Empty;

            name = name.Trim();

            // OPL names (and some fallbacks) may already include a leading stack size,
            // e.g. "5 Gold Coins". Strip it so amount display is controlled consistently.
            string amountStr = $"{Amount.ToString(CultureInfo.InvariantCulture)} ";
            if (name.StartsWith(amountStr, StringComparison.Ordinal))
                name = name[amountStr.Length..];

            if (showAmount && !IsCorpse && Amount > 1)
                name = amountStr + name;

            return name;
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
                UIManager.ForEach<ContainerGump>(g => g.Dispose(), Serial);
                UIManager.ForEach<GridContainer>(g => g.Dispose(), Serial);
                UIManager.ForEach<SpellbookGump>(g => g.Dispose(), Serial);
                UIManager.ForEach<MapGump>(g => g.Dispose(), Serial);

                if (IsCorpse)
                    UIManager.ForEach<GridLootGump>(g => g.Dispose(), Serial);

                UIManager.ForEach<BulletinBoardGump>(g => g.Dispose(), Serial);
                UIManager.ForEach<SplitMenuGump>(g => g.Dispose(), Serial);

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

            UIManager.ForEach<MiniMapGump>(g => g.RequestUpdateContents());

            if (World.HouseManager.EntityIntoHouse(Serial, World.Player)) GameScene.Instance?.UpdateMaxDrawZ(true);

            World.BoatMovingManager.ClearSteps(Serial);

            // A multi (house/boat) just had its components placed on the map. If the player is
            // auto-walking a path that was computed before this geometry existed, that path may
            // now cut through the new blocker — re-pathfind from the current position so the
            // walk routes around it.
            World.Player?.Pathfinder?.RecalculatePath();
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

                // Removed Camera.WorldToScreen() - text is now transformed by worldRTMatrix during rendering

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
