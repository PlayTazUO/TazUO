// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Managers.Hotkeys;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ClassicUO.Game.Scenes;
using System.Collections.Generic;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.UI.Gumps
{
    public class NameOverheadGump : Gump
    {
        private AlphaBlendControl _background;
        private Point _lockedPosition,
            _lastLeftMousePositionDown;
        private bool _positionLocked,
            _leftMouseIsDown,
            _isLastTarget,
            _needsNameUpdate;
        private TextBox _text;
        private Texture2D _borderColor = SolidColorTextureCache.GetTexture(Color.Black);
        private Vector2 _textDrawOffset = Vector2.Zero;
        private Rectangle _wordOfDeathIconBounds = Rectangle.Empty;
        private int _lastLayoutSignature;
        private int _namePlateWidth;
        private int _healthBarWidth;
        private int _nameBandHeight;
        private int _resourceBarHeight;
        private int _resourceBarCount;
        private bool _useSplitLayout;
        private static int currentHeight = 22;
        private static readonly int COLLISION_SPACING = 8;
        private static readonly Dictionary<long, int[]> RoundedRectangleInsetCache = new();
        private const int MIN_NAMEPLATE_WIDTH = 60;
        private const int NAMEPLATE_HORIZONTAL_PADDING = 4;
        private const int NAMEPLATE_VERTICAL_PADDING = 4;
        private const int WORD_OF_DEATH_GUMP_ID = 0x59E5;
        private const int WORD_OF_DEATH_SPELL_ID = 614;
        private const double WORD_OF_DEATH_HEALTH_THRESHOLD = 0.30d;
        private const int WORD_OF_DEATH_ICON_PADDING = 3;
        private static readonly Color MissingHealthBackgroundColor = new Color(14, 14, 14);

        public static void InvalidateAllLayouts()
        {
            foreach (var gump in UIManager.Gumps)
            {
                if (gump is NameOverheadGump nameOverhead)
                {
                    nameOverhead._needsNameUpdate = true;
                }
            }
        }

        public static int CurrentHeight
        {
            get
            {
                if (NameOverHeadManager.IsShowing)
                {
                    return currentHeight;
                }

                return 0;
            }
            private set
            {
                currentHeight = value;
            }
        }

        public new UILayer LayerOrder {
            get
            {
                if (IsFocused)
                    return UILayer.Default;

                return UILayer.Under;
            }
            set { }
        }

        public NameOverheadGump(World world, uint serial) : base(world, serial, 0)
        {
            CanMove = false;
            AcceptMouseInput = true;
            CanCloseWithRightClick = true;

            Entity entity = World.Get(serial);

            if (entity == null)
            {
                Dispose();
                return;
            }

            EnsureTextBox(entity);

            SetTooltip(entity);

            BuildGump();
            SetName();
        }

        private void EnsureTextBox(Entity entity) => _text = TextBox.GetOne(string.Empty, ProfileManager.CurrentProfile.NamePlateFont, ProfileManager.CurrentProfile.NamePlateFontSize, entity is Mobile m ? Notoriety.GetHue(m.NotorietyFlag) : (ushort)0x0481, TextBox.RTLOptions.DefaultCenterStroked());

        public bool SetName()
        {
            if (World == null) return false;

            Entity entity = World.Get(LocalSerial);

            if (entity == null)
                return false;

            if (_text == null || _text.IsDisposed)
            {
                EnsureTextBox(entity);

                if (_text == null)
                {
                    Log.Error("Failed to create textbox in NameOverheadGump.SetName");
                    return false;
                }
            }

            if (entity is Item item)
            {
                if (!World.OPL.TryGetNameAndData(item, out string t, out _))
                {
                    t = string.Empty;
                    _needsNameUpdate = true;
                    if (!item.IsCorpse && item.Amount > 1)
                    {
                        t = item.Amount.ToString() + ' ';
                    }

                    if (string.IsNullOrEmpty(item.ItemData.Name))
                    {
                        t += Client.Game.UO.FileManager.Clilocs.GetString(1020000 + item.Graphic, true, t);
                    }
                    else
                    {
                        t += StringHelper.CapitalizeAllWords(
                            StringHelper.GetPluralAdjustedString(
                                item.ItemData.Name,
                                item.Amount > 1
                            )
                        );
                    }
                }
                else
                {
                    _needsNameUpdate = false;
                }

                if (string.IsNullOrEmpty(t))
                {
                    return false;
                }

                ApplyNameLayout(entity, t);

                return true;
            }

            if (!string.IsNullOrEmpty(entity.Name))
            {
                if (_text == null)
                    return false;

                ApplyNameLayout(entity, entity.Name);

                return true;
            }

            return false;
        }

        private void ApplyNameLayout(Entity entity, string name)
        {
            Profile profile = ProfileManager.CurrentProfile;
            _text.Font = profile.NamePlateFont;
            _text.FontSize = profile.NamePlateFontSize;
            SetMeasuredText(name);
            int nameWidth;

            if (profile.NamePlateUseFixedWidth)
            {
                nameWidth = Math.Clamp(profile.NamePlateFixedWidth, 60, 300);
                SetFittedText(name, Math.Max(1, nameWidth - NAMEPLATE_HORIZONTAL_PADDING));
            }
            else
            {
                nameWidth = Math.Max(MIN_NAMEPLATE_WIDTH, _text.Width) + NAMEPLATE_HORIZONTAL_PADDING;
            }

            int healthBarWidth = GetHealthBarWidth(profile, nameWidth, entity);
            int width = Math.Max(nameWidth, healthBarWidth);

            int minimumNameBandHeight = Math.Max(Constants.OBJECT_HANDLES_GUMP_HEIGHT, _text.Height) + NAMEPLATE_VERTICAL_PADDING;
            _namePlateWidth = nameWidth;
            _healthBarWidth = healthBarWidth;
            _nameBandHeight = minimumNameBandHeight;
            _resourceBarCount = 0;
            _resourceBarHeight = 0;
            _resourceBarCount = GetSplitResourceBarCount(profile, entity);
            _useSplitLayout = _resourceBarCount > 0;

            if (_useSplitLayout)
            {
                _resourceBarHeight = Math.Max(4, Math.Min(8, _nameBandHeight / 4));
            }

            int automaticHeight = _nameBandHeight + (_useSplitLayout ? _resourceBarHeight * _resourceBarCount + 2 : 0);
            int height = profile.NamePlateHeight > 0 ? Math.Max(profile.NamePlateHeight, minimumNameBandHeight) : automaticHeight;

            if (_useSplitLayout && profile.NamePlateHeight > 0)
            {
                int minimumBarHeight = Math.Max(3, _resourceBarCount);
                height = Math.Max(height, minimumNameBandHeight + minimumBarHeight + 2);
                int barAreaHeight = Math.Max(minimumBarHeight, height - minimumNameBandHeight - 2);
                _resourceBarHeight = Math.Max(1, barAreaHeight / Math.Max(1, _resourceBarCount));
                _nameBandHeight = height - _resourceBarHeight * _resourceBarCount - 2;
            }

            Width = _background.Width = width;
            Height = _background.Height = CurrentHeight = height;
            int textAreaWidth = Math.Max(1, _namePlateWidth - NAMEPLATE_HORIZONTAL_PADDING);
            _textDrawOffset.X = Math.Max(0, (textAreaWidth - _text.Width) >> 1);
            _textDrawOffset.Y = Math.Max(0, ((_useSplitLayout ? _nameBandHeight : Height) - _text.Height) >> 1);
            _lastLayoutSignature = GetLayoutSignature(profile, entity);
            WantUpdateSize = false;
        }

        private int GetLayoutSignature(Profile profile, Entity entity)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + profile.NamePlateUseFixedWidth.GetHashCode();
                hash = hash * 31 + profile.NamePlateFixedWidth;
                hash = hash * 31 + profile.NamePlateUseFixedHealthBarWidth.GetHashCode();
                hash = hash * 31 + profile.NamePlateHealthBarFixedWidth;
                hash = hash * 31 + profile.NamePlateHeight;
                hash = hash * 31 + profile.NamePlateSplitHealthBar.GetHashCode();
                hash = hash * 31 + profile.NamePlateHealthBar.GetHashCode();
                hash = hash * 31 + profile.NamePlateCornerRadius;
                hash = hash * 31 + profile.NamePlateFontSize;
                hash = hash * 31 + (profile.NamePlateFont?.GetHashCode() ?? 0);
                hash = hash * 31 + GetSplitResourceBarCount(profile, entity);
                return hash;
            }
        }

        private int GetSplitResourceBarCount(Profile profile, Entity entity)
        {
            if (!profile.NamePlateSplitHealthBar || !profile.NamePlateHealthBar || entity is not Mobile mobile)
            {
                return 0;
            }

            return mobile is PlayerMobile || World.Party.Contains(mobile.Serial) ? 3 : 1;
        }

        private static int GetHealthBarWidth(Profile profile, int nameWidth, Entity entity)
        {
            if (!profile.NamePlateHealthBar || entity is not Mobile || !profile.NamePlateUseFixedHealthBarWidth)
            {
                return nameWidth;
            }

            return Math.Clamp(profile.NamePlateHealthBarFixedWidth, 60, 300);
        }

        private void SetFittedText(string text, int maxWidth)
        {
            text ??= string.Empty;
            SetMeasuredText(text);

            if (_text.Width <= maxWidth)
            {
                return;
            }

            const string ellipsis = "...";
            SetMeasuredText(ellipsis);

            if (_text.Width > maxWidth || text.Length == 0)
            {
                return;
            }

            int low = 0;
            int high = text.Length;

            while (low < high)
            {
                int mid = (low + high + 1) >> 1;
                SetMeasuredText(text.Substring(0, mid) + ellipsis);

                if (_text.Width <= maxWidth)
                {
                    low = mid;
                }
                else
                {
                    high = mid - 1;
                }
            }

            SetMeasuredText(text.Substring(0, low) + ellipsis);
        }

        private void SetMeasuredText(string text)
        {
            _text.Text = text;
            _text.Update();
        }

        private void BuildGump()
        {
            Entity entity = World.Get(LocalSerial);

            if (entity == null)
            {
                Dispose();

                return;
            }

            Add
            (
                _background = new AlphaBlendControl(ProfileManager.CurrentProfile.NamePlateOpacity / 100f)
                {
                    WantUpdateSize = false,
                    Hue = entity is Mobile m ? Notoriety.GetHue(m.NotorietyFlag) : Notoriety.GetHue(NotorietyFlag.Gray)
                }
            );
        }

        public override void CloseWithRightClick()
        {
            Entity entity = World.Get(LocalSerial);

            if (entity != null)
            {
                entity.ObjectHandlesStatus = ObjectHandlesStatus.CLOSED;
            }

            base.CloseWithRightClick();
        }

        private void DoDrag()
        {
            Point delta = Mouse.Position - _lastLeftMousePositionDown;

            if (
                Math.Abs(delta.X) <= Constants.MIN_GUMP_DRAG_DISTANCE
                && Math.Abs(delta.Y) <= Constants.MIN_GUMP_DRAG_DISTANCE
            )
            {
                return;
            }

            _leftMouseIsDown = false;
            _positionLocked = false;

            Entity entity = World.Get(LocalSerial);

            if (entity is Mobile || entity is Item it && it.IsDamageable)
            {
                if (UIManager.IsDragging)
                {
                    return;
                }

                BaseHealthBarGump gump = UIManager.GetGump<BaseHealthBarGump>(LocalSerial);
                gump?.Dispose();

                if (ProfileManager.CurrentProfile.CustomBarsToggled)
                {
                    var rect = new Rectangle(
                        0,
                        0,
                        HealthBarGumpCustom.HPB_WIDTH,
                        HealthBarGumpCustom.HPB_HEIGHT_SINGLELINE
                    );

                    UIManager.Add(
                        gump = new HealthBarGumpCustom(World, entity)
                        {
                            X = Mouse.Position.X - (rect.Width >> 1),
                            Y = Mouse.Position.Y - (rect.Height >> 1)
                        }
                    );
                }
                else
                {
                    ref readonly SpriteInfo gumpInfo = ref Client.Game.UO.Gumps.GetGump(0x0804);

                    UIManager.Add(
                        gump = new HealthBarGump(World, entity)
                        {
                            X = Mouse.LClickPosition.X - (gumpInfo.UV.Width >> 1),
                            Y = Mouse.LClickPosition.Y - (gumpInfo.UV.Height >> 1)
                        }
                    );
                }

                UIManager.AttemptDragControl(gump, true);
            }
            else if (entity != null)
            {
                GameActions.PickUp(World, LocalSerial, 0, 0);

                //if (entity.Texture != null)
                //    GameActions.PickUp(LocalSerial, entity.Texture.Width >> 1, entity.Texture.Height >> 1);
                //else
                //    GameActions.PickUp(LocalSerial, 0, 0);
            }
        }

        public override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                if (SerialHelper.IsMobile(LocalSerial))
                {
                    if (World.Player.InWarMode)
                    {
                        GameActions.Attack(World, LocalSerial);
                    }
                    else
                    {
                        GameActions.DoubleClick(World, LocalSerial);
                    }
                }
                else
                {
                    if (!GameActions.OpenCorpse(World, LocalSerial))
                    {
                        GameActions.DoubleClick(World, LocalSerial);
                    }
                }

                return true;
            }

            return false;
        }

        public override void OnMouseDown(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                _lastLeftMousePositionDown = Mouse.Position;
                _leftMouseIsDown = true;

                if (ProfileManager.CurrentProfile.SingleClickMobileSetsLastTarget)
                    World.Instance.TargetManager.LastTargetInfo.SetEntity(LocalSerial);
            }

            base.OnMouseDown(x, y, button);
        }

        public override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                _leftMouseIsDown = false;

                if (!Client.Game.UO.GameCursor.ItemHold.Enabled)
                {
                    if (
                        UIManager.IsDragging
                        || Math.Max(Math.Abs(Mouse.LDragOffset.X), Math.Abs(Mouse.LDragOffset.Y))
                            >= 1
                    )
                    {
                        _positionLocked = false;

                        return;
                    }

                    if (TryCastWordOfDeathFromIcon(x, y))
                    {
                        Mouse.LastLeftButtonClickTime = 0;
                        Mouse.CancelDoubleClick = true;
                        return;
                    }
                }

                if (World.TargetManager.IsTargeting)
                {
                    switch (World.TargetManager.TargetingState)
                    {
                        case CursorTarget.Internal:
                        case CursorTarget.Position:
                        case CursorTarget.Object:
                        case CursorTarget.Grab:
                        case CursorTarget.SetGrabBag:
                        case CursorTarget.MoveItemContainer:
                        case CursorTarget.CallbackTarget:
                            World.TargetManager.Target(LocalSerial);
                            Mouse.LastLeftButtonClickTime = 0;

                            break;

                        case CursorTarget.SetTargetClientSide:
                            World.TargetManager.Target(LocalSerial);
                            Mouse.LastLeftButtonClickTime = 0;
                            UIManager.Add(new InspectorGump(World, World.Get(LocalSerial)));

                            break;

                        case CursorTarget.HueCommandTarget:
                            World.CommandManager.OnHueTarget(World.Get(LocalSerial));

                            break;
                    }
                }
                else
                {
                    if (
                        Client.Game.UO.GameCursor.ItemHold.Enabled
                        && !Client.Game.UO.GameCursor.ItemHold.IsFixedPosition
                    )
                    {
                        uint drop_container = 0xFFFF_FFFF;
                        bool can_drop = false;
                        ushort dropX = 0;
                        ushort dropY = 0;
                        sbyte dropZ = 0;

                        Entity obj = World.Get(LocalSerial);

                        if (obj != null)
                        {
                            can_drop = obj.Distance <= Constants.DRAG_ITEMS_DISTANCE;

                            if (can_drop)
                            {
                                if (obj is Item it && it.ItemData.IsContainer || obj is Mobile)
                                {
                                    dropX = 0xFFFF;
                                    dropY = 0xFFFF;
                                    dropZ = 0;
                                    drop_container = obj.Serial;
                                }
                                else if (
                                    obj is Item it2
                                    && (
                                        it2.ItemData.IsSurface
                                        || it2.ItemData.IsStackable
                                            && it2.DisplayedGraphic
                                                == Client.Game.UO.GameCursor.ItemHold.DisplayedGraphic
                                    )
                                )
                                {
                                    dropX = obj.X;
                                    dropY = obj.Y;
                                    dropZ = obj.Z;

                                    if (it2.ItemData.IsSurface)
                                    {
                                        dropZ += (sbyte)(
                                            it2.ItemData.Height == 0xFF ? 0 : it2.ItemData.Height
                                        );
                                    }
                                    else
                                    {
                                        drop_container = obj.Serial;
                                    }
                                }
                            }
                            else
                            {
                                Client.Game.Audio.PlaySound(0x0051);
                            }

                            if (can_drop)
                            {
                                if (drop_container == 0xFFFF_FFFF && dropX == 0 && dropY == 0)
                                {
                                    can_drop = false;
                                }

                                if (can_drop)
                                {
                                    GameActions.DropItem(
                                        Client.Game.UO.GameCursor.ItemHold.Serial,
                                        dropX,
                                        dropY,
                                        dropZ,
                                        drop_container
                                    );
                                }
                            }
                        }
                    }
                    else if (
                        HotKeys.IsPressed(HotKeyRegistrar.FollowMobileId)
                        && !ProfileManager.CurrentProfile.DisableAutoFollowAlt
                        && SerialHelper.IsMobile(LocalSerial)
                    )
                    {
                        if (World.Get(LocalSerial) is Mobile followTarget)
                        {
                            followTarget.Follow();
                        }
                    }
                    else if (!World.DelayedObjectClickManager.IsEnabled)
                    {
                        World.DelayedObjectClickManager.Set(
                            LocalSerial,
                            Mouse.Position.X,
                            Mouse.Position.Y,
                            Time.Ticks + Mouse.MOUSE_DELAY_DOUBLE_CLICK
                        );
                    }
                }
            }

            base.OnMouseUp(x, y, button);
        }

        public override void OnMouseOver(int x, int y)
        {
            if (_leftMouseIsDown)
            {
                DoDrag();
            }

            if (!_positionLocked && SerialHelper.IsMobile(LocalSerial))
            {
                Mobile m = World.Mobiles.Get(LocalSerial);

                if (m == null)
                {
                    Dispose();

                    return;
                }

                _positionLocked = true;

                Client.Game.UO.Animations.GetAnimationDimensions(
                    m.AnimIndex,
                    m.GetGraphicForAnimation(),
                    /*(byte) m.GetDirectionForAnimation()*/
                    0,
                    /*Mobile.GetGroupForAnimation(m, isParent:true)*/
                    0,
                    m.IsMounted,
                    /*(byte) m.AnimIndex*/
                    0,
                    out int centerX,
                    out int centerY,
                    out int width,
                    out int height
                );

                _lockedPosition = GetPosition(m, height, centerY);
            }

            base.OnMouseOver(x, y);
        }

        private Point GetPosition(Mobile m, int height, int centerY)
        {
            Point p = new(
                (int)(m.RealScreenPosition.X + m.Offset.X + 22 + 5),
                (int)(m.RealScreenPosition.Y
                      + (m.Offset.Y - m.Offset.Z)
                      - (height + centerY + 15) * m.Scale
                      + (
                          m.IsGargoyle && m.IsFlyingAnimationEnabled
                              ? -22
                              : !m.IsMounted
                                  ? 22
                                  : 0
                      )
                    ));

            return p;
        }

        protected override void OnMouseExit(int x, int y)
        {
            _positionLocked = false;
            base.OnMouseExit(x, y);
        }

        private static List<NameOverheadGump> GetAllVisibleNameOverheads()
        {
            var result = new List<NameOverheadGump>();

            for (LinkedListNode<IGui> node = UIManager.Gumps.First; node != null; node = node.Next)
            {
                if (node.Value is NameOverheadGump nameGump &&
                    !nameGump.IsDisposed &&
                    nameGump.IsVisible)
                {
                    result.Add(nameGump);
                }
            }

            return result;
        }

        private Rectangle GetBounds(int x, int y, int width, int height) => new Rectangle(x, y, width, height);

        private Point AdjustPositionToAvoidOverlap(int originalX, int originalY)
        {
            if (!ProfileManager.CurrentProfile.NamePlateAvoidOverlap)
            {
                return new Point(originalX, originalY);
            }

            List<NameOverheadGump> allNameOverheads = GetAllVisibleNameOverheads();
            int adjustedX = originalX;
            int adjustedY = originalY;
            int maxIterations = 10;
            int iterations = 0;

            while (iterations < maxIterations)
            {
                Rectangle proposedBounds = GetBounds(adjustedX, adjustedY, Width, Height);
                bool hasCollision = false;

                foreach (NameOverheadGump other in allNameOverheads)
                {
                    if (other == this || other.LocalSerial == this.LocalSerial)
                        continue;

                    Rectangle otherBounds = GetBounds(other.X, other.Y, other.Width, other.Height);

                    if (proposedBounds.Intersects(otherBounds))
                    {
                        adjustedY = otherBounds.Bottom + COLLISION_SPACING;
                        hasCollision = true;
                        break;
                    }
                }

                if (!hasCollision)
                    break;

                iterations++;
            }

            return new Point(adjustedX, adjustedY);
        }

        public override void Update()
        {
            base.Update();

            Entity entity = World.Get(LocalSerial);

            if (
                entity == null
                || entity.IsDestroyed
                || entity.ObjectHandlesStatus == ObjectHandlesStatus.NONE
                || entity.ObjectHandlesStatus == ObjectHandlesStatus.CLOSED
            )
            {
                Dispose();
            }
            else
            {
                if (entity == World.TargetManager.LastTargetInfo.Serial)
                {
                    if (!_isLastTarget) //Only set this if it was not already last target
                    {
                        _borderColor = SolidColorTextureCache.GetTexture(Color.Red);
                        _background.Hue = (ushort)(_text.Hue = entity is Mobile m
                            ? Notoriety.GetHue(m.NotorietyFlag)
                            : (ushort)0x0481);
                        _isLastTarget = true;
                    }
                }
                else
                {
                    if (_isLastTarget)//If we make it here, it is no longer the last target so we update colors and set this to false.
                    {
                        _borderColor = SolidColorTextureCache.GetTexture(Color.Black);
                        _background.Hue = (ushort)(_text.Hue = entity is Mobile m
                            ? Notoriety.GetHue(m.NotorietyFlag)
                            : (ushort)0x0481);
                        _isLastTarget = false;
                    }
                }

                if (_lastLayoutSignature != GetLayoutSignature(ProfileManager.CurrentProfile, entity))
                {
                    _needsNameUpdate = true;
                }

                if (_needsNameUpdate)
                {
                    SetName();
                }
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsDisposed)
            {
                return false;
            }

            _wordOfDeathIconBounds = Rectangle.Empty;

            bool _isMobile = false;
            IsVisible = true;
            Entity nameplateEntity = null;

            if (SerialHelper.IsMobile(LocalSerial))
            {
                Mobile m = World.Mobiles.Get(LocalSerial);

                if (m == null)
                {
                    Dispose();

                    return false;
                }

                if (NameOverHeadManager.HasSearchFilter)
                {
                    string oplName = null;
                    if (World.OPL.TryGetNameAndData(m.Serial, out string name, out string _))
                        oplName = name;

                    if (NameOverHeadManager.MatchesNegativeSearch(m.Name, oplName)
                        || !NameOverHeadManager.MatchesSearch(m.Name, oplName))
                    {
                        IsVisible = false;
                        return true;
                    }
                }

                _isMobile = true;
                nameplateEntity = m;
                double _hpPercent = GetResourcePercent(m.Hits, m.HitsMax);

                IsVisible = true;
                if (ProfileManager.CurrentProfile.NamePlateHideAtFullHealth && _hpPercent >= 1)
                {
                    if (ProfileManager.CurrentProfile.NamePlateHideAtFullHealthInWarmode)
                    {
                        if (World.Player.InWarMode)
                        {
                            IsVisible = false;
                            return false;
                        }

                    }
                    else
                    {
                        IsVisible = false;
                        return false;
                    }

                }

                if (_positionLocked)
                {
                    x = _lockedPosition.X;
                    y = _lockedPosition.Y;
                }
                else
                {
                    Client.Game.UO.Animations.GetAnimationDimensions(
                        m.AnimIndex,
                        m.GetGraphicForAnimation(),
                        /*(byte) m.GetDirectionForAnimation()*/
                        0,
                        /*Mobile.GetGroupForAnimation(m, isParent:true)*/
                        0,
                        m.IsMounted,
                        /*(byte) m.AnimIndex*/
                        0,
                        out int centerX,
                        out int centerY,
                        out int width,
                        out int height
                    );

                    Point pos = GetPosition(m, height, centerY);

                    x = pos.X;
                    y = pos.Y - m.NameOverheadTextExtraHeight;
                }
            }
            else if (SerialHelper.IsItem(LocalSerial))
            {
                Item item = World.Items.Get(LocalSerial);

                if (item == null)
                {
                    Dispose();
                    return false;
                }

                nameplateEntity = item;

                if (NameOverHeadManager.HasSearchFilter)
                {
                    string oplName = null, oplData = null;
                    if (World.OPL.TryGetNameAndData(item.Serial, out string name, out string data))
                    {
                        oplName = name;
                        oplData = data;
                    }

                    if (NameOverHeadManager.MatchesNegativeSearch(item.Name, oplName, oplData)
                        || !NameOverHeadManager.MatchesSearch(item.Name, oplName, oplData))
                    {
                        IsVisible = false;
                        return true;
                    }
                }

                Rectangle bounds = Client.Game.UO.Arts.GetRealArtBounds(item.Graphic);

                x = item.RealScreenPosition.X + (int)item.Offset.X + 22 + 5;
                y =
                    item.RealScreenPosition.Y
                    + (int)(item.Offset.Y - item.Offset.Z)
                    + (bounds.Height >> 1);
            }

            Point p = Client.Game.Scene.Camera.WorldToScreen(new Point(x, y));
            x = p.X - (Width >> 1);
            y = p.Y - (Height);// >> 1);

            Camera camera = Client.Game.Scene.Camera;
            x += camera.Bounds.X;
            y += camera.Bounds.Y;

            if (x < camera.Bounds.X || x + Width > camera.Bounds.Right)
            {
                return false;
            }

            if (y < camera.Bounds.Y || y + Height > camera.Bounds.Bottom)
            {
                return false;
            }

            Point adjustedPos = AdjustPositionToAvoidOverlap(x, y);
            x = adjustedPos.X;
            y = adjustedPos.Y;

            X = x;
            Y = y;

            Rectangle nameBounds = GetNamePlateBackgroundBounds(x, y);
            int cornerRadius = GetCornerRadius(nameBounds.Width, nameBounds.Height);
            DrawNamePlateBackground(batcher, nameplateEntity, nameBounds, cornerRadius);

            int textY = y;

            if (ProfileManager.CurrentProfile.NamePlateHealthBar && _isMobile)
            {
                Mobile m = World.Mobiles.Get(LocalSerial);

                if (m != null)
                {
                    bool isPlayer = m is PlayerMobile;
                    bool isInParty = World.Party.Contains(m.Serial);
                    bool showAllResources = isPlayer || isInParty;
                    int barCount = showAllResources ? 3 : 1;
                    float _alpha = ProfileManager.CurrentProfile.NamePlateHealthBarOpacity / 100f;

                    double hpPercent = GetResourcePercent(m.Hits, m.HitsMax);
                    Color fillColor = GetHealthFillColor(m, hpPercent);
                    DrawResourceBar(
                        batcher,
                        GetResourceBarBounds(x, y, 0, barCount),
                        SolidColorTextureCache.GetTexture(fillColor),
                        ShaderHueTranslator.GetHueVector(0, false, _alpha),
                        hpPercent,
                        _alpha
                    );

                    if (showAllResources)
                    {
                        double mpPercent = GetResourcePercent(m.Mana, m.ManaMax);
                        DrawResourceBar(
                            batcher,
                            GetResourceBarBounds(x, y, 1, barCount),
                            SolidColorTextureCache.GetTexture(Color.White),
                            ShaderHueTranslator.GetHueVector(GetResourceThresholdHue(mpPercent, .6d, .2d), false, _alpha),
                            mpPercent,
                            _alpha
                        );

                        double spPercent = GetResourcePercent(m.Stamina, m.StaminaMax);
                        DrawResourceBar(
                            batcher,
                            GetResourceBarBounds(x, y, 2, barCount),
                            SolidColorTextureCache.GetTexture(Color.White),
                            ShaderHueTranslator.GetHueVector(GetResourceThresholdHue(spPercent, .8d, .5d), false, _alpha),
                            spPercent,
                            _alpha
                        );

                        if (!_useSplitLayout)
                        {
                            textY += 20;
                        }
                    }
                }
            }

            Mobile textMobile = _isMobile ? World.Mobiles.Get(LocalSerial) : null;
            Color textColor = GetContrastingTextColor(GetTextSurfaceColor(nameplateEntity, textMobile));
            int textX = (int)(nameBounds.X + 2 + _textDrawOffset.X);
            int textDrawY = (int)(textY + 2 + _textDrawOffset.Y);

            bool result = _text.Draw(batcher, textX, textDrawY, textColor);
            DrawWordOfDeathIcon(batcher, textMobile, nameBounds, textDrawY);
            return result;
        }

        private Rectangle GetResourceBarBounds(int x, int y, int barIndex, int barCount)
        {
            int barWidth = _healthBarWidth > 0 ? _healthBarWidth : Width;
            int barX = GetCenteredX(x, barWidth);

            if (_useSplitLayout)
            {
                return new Rectangle(barX, y + _nameBandHeight + 1 + barIndex * _resourceBarHeight, Math.Max(1, barWidth), _resourceBarHeight);
            }

            int barHeight = Math.Max(1, Height / Math.Max(1, barCount));
            int barY = y + barIndex * barHeight;
            int height = barIndex == barCount - 1 ? Math.Max(1, Height - barHeight * barIndex) : barHeight;

            return new Rectangle(barX, barY, Math.Max(1, barWidth), height);
        }

        private Rectangle GetNamePlateBackgroundBounds(int x, int y)
        {
            int nameWidth = _namePlateWidth > 0 ? _namePlateWidth : Width;
            int nameHeight = Math.Max(1, _useSplitLayout ? _nameBandHeight : Height);

            return new Rectangle(GetCenteredX(x, nameWidth), y, Math.Max(1, nameWidth), nameHeight);
        }

        private int GetCenteredX(int x, int width)
        {
            return x + Math.Max(0, (Width - width) >> 1);
        }

        private void DrawNamePlateBackground(UltimaBatcher2D batcher, Entity entity, Rectangle bounds, int radius)
        {
            Profile profile = ProfileManager.CurrentProfile;
            float borderAlpha = profile.NamePlateBorderOpacity / 100f;
            Vector3 borderHue = ShaderHueTranslator.GetHueVector(0, false, borderAlpha);

            DrawRoundedRectangle(batcher, _borderColor, bounds, borderHue, radius);

            Rectangle innerBounds = new Rectangle(bounds.X + 1, bounds.Y + 1, Math.Max(0, bounds.Width - 2), Math.Max(0, bounds.Height - 2));

            if (innerBounds.Width <= 0 || innerBounds.Height <= 0)
            {
                return;
            }

            Texture2D backgroundTexture;
            Vector3 backgroundHue;
            float backgroundAlpha = profile.NamePlateOpacity / 100f;

            if (profile.NamePlateBackgroundMode == NamePlateBackgroundMode.NotorietyColor && entity is Mobile mobile)
            {
                backgroundTexture = SolidColorTextureCache.GetTexture(Color.Black);
                backgroundHue = ShaderHueTranslator.GetHueVector(GetNamePlateNotorietyHue(mobile), false, backgroundAlpha);
            }
            else
            {
                backgroundTexture = SolidColorTextureCache.GetTexture(new Color(profile.NamePlateBackgroundR, profile.NamePlateBackgroundG, profile.NamePlateBackgroundB));
                backgroundHue = ShaderHueTranslator.GetHueVector(0, false, backgroundAlpha);
            }

            DrawRoundedRectangle(batcher, backgroundTexture, innerBounds, backgroundHue, Math.Max(0, radius - 1));
        }

        private Color GetTextSurfaceColor(Entity entity, Mobile mobile)
        {
            Profile profile = ProfileManager.CurrentProfile;

            if (!_useSplitLayout && mobile != null && ProfileManager.CurrentProfile.NamePlateHealthBar)
            {
                double hpPercent = GetResourcePercent(mobile.Hits, mobile.HitsMax);
                Color surface = hpPercent >= 0.5d ? GetHealthFillColor(mobile, hpPercent) : MissingHealthBackgroundColor;
                return ApplyAlphaOverBlack(surface, profile.NamePlateHealthBarOpacity / 100f);
            }

            return ApplyAlphaOverBlack(GetNamePlateBackgroundColor(entity), profile.NamePlateOpacity / 100f);
        }

        private Color GetNamePlateBackgroundColor(Entity entity)
        {
            Profile profile = ProfileManager.CurrentProfile;

            if (profile.NamePlateBackgroundMode == NamePlateBackgroundMode.NotorietyColor && entity is Mobile mobile)
            {
                return TextBox.ConvertHueToColor(GetNamePlateNotorietyHue(mobile));
            }

            return new Color(profile.NamePlateBackgroundR, profile.NamePlateBackgroundG, profile.NamePlateBackgroundB);
        }

        private Color GetHealthFillColor(Mobile mobile, double hpPercent)
        {
            switch (ProfileManager.CurrentProfile.NamePlateHealthBarMode)
            {
                case NamePlateHealthBarMode.Green:
                    return Color.Green;
                case NamePlateHealthBarMode.Blue:
                    return Color.Blue;
                case NamePlateHealthBarMode.Red:
                    return Color.Red;
                case NamePlateHealthBarMode.Cyan:
                    return Color.Cyan;
                case NamePlateHealthBarMode.Yellow:
                    return Color.Yellow;
                case NamePlateHealthBarMode.Orange:
                    return Color.Orange;
                case NamePlateHealthBarMode.Purple:
                    return Color.Purple;
                case NamePlateHealthBarMode.White:
                    return Color.White;
                case NamePlateHealthBarMode.Gray:
                    return Color.Gray;
                case NamePlateHealthBarMode.Black:
                    return Color.Black;
                default:
                    return TextBox.ConvertHueToColor(GetStatusHealthHue(mobile, hpPercent));
            }
        }

        private ushort GetStatusHealthHue(Mobile mobile, double hpPercent)
        {
            bool friendly = mobile is PlayerMobile || mobile.Serial == World.Player?.Serial || World.Party.Contains(mobile.Serial);
            ushort baseHue = hpPercent switch
            {
                >= 1d => friendly ? (ushort)0x0058 : Notoriety.GetHue(mobile.NotorietyFlag),
                > .8d => 0x0058,
                > .4d => 0x0030,
                _ => 0x0021
            };

            if (mobile.IsPoisoned)
            {
                baseHue = 63;
            }
            else if (mobile.IsYellowHits || mobile.IsParalyzed)
            {
                baseHue = 353;
            }

            return baseHue;
        }

        private static int GetResourceThresholdHue(double percent, double highThreshold, double midThreshold)
        {
            if (percent > highThreshold)
            {
                return 0x0058;
            }

            return percent > midThreshold ? 0x0030 : 0x0021;
        }

        private static Color GetContrastingTextColor(Color background)
        {
            double luminance =
                (0.2126d * background.R +
                 0.7152d * background.G +
                 0.0722d * background.B) / 255d;

            return luminance > 0.72d ? Color.Black : Color.White;
        }

        private static Color ApplyAlphaOverBlack(Color color, float alpha)
        {
            alpha = Math.Clamp(alpha, 0f, 1f);

            return new Color(
                (byte)Math.Clamp((int)Math.Round(color.R * alpha), 0, 255),
                (byte)Math.Clamp((int)Math.Round(color.G * alpha), 0, 255),
                (byte)Math.Clamp((int)Math.Round(color.B * alpha), 0, 255)
            );
        }

        private ushort GetNamePlateNotorietyHue(Mobile mobile)
        {
            if (mobile.Serial == World.Player?.Serial)
            {
                return Notoriety.GetHue(NotorietyFlag.Innocent);
            }

            if (World.Party.Contains(mobile.Serial))
            {
                return Notoriety.GetHue(NotorietyFlag.Ally);
            }

            return Notoriety.GetHue(mobile.NotorietyFlag);
        }

        private void DrawWordOfDeathIcon(UltimaBatcher2D batcher, Mobile mobile, Rectangle nameBounds, int textY)
        {
            _wordOfDeathIconBounds = Rectangle.Empty;

            if (!ShouldDrawWordOfDeathIcon(mobile) || nameBounds.Width <= WORD_OF_DEATH_ICON_PADDING * 2)
            {
                return;
            }

            ref readonly SpriteInfo iconInfo = ref Client.Game.UO.Gumps.GetGump(WORD_OF_DEATH_GUMP_ID);

            if (iconInfo.Texture == null)
            {
                return;
            }

            int iconSize = GetWordOfDeathIconSize(nameBounds);

            if (iconSize <= 0)
            {
                return;
            }

            int iconX = nameBounds.Right - iconSize - WORD_OF_DEATH_ICON_PADDING;
            int iconY = textY + Math.Max(0, (_text.Height - iconSize) >> 1);
            _wordOfDeathIconBounds = new Rectangle(iconX, iconY, iconSize, iconSize);
            batcher.Draw(iconInfo.Texture, _wordOfDeathIconBounds, iconInfo.UV, ShaderHueTranslator.GetHueVector(0));
        }

        private bool ShouldDrawWordOfDeathIcon(Mobile mobile)
        {
            return mobile != null
                   && ProfileManager.CurrentProfile.NamePlateShowWordOfDeathIcon
                   && mobile.HitsMax > 0
                   && GetResourcePercent(mobile.Hits, mobile.HitsMax) <= WORD_OF_DEATH_HEALTH_THRESHOLD;
        }

        private bool TryCastWordOfDeathFromIcon(int x, int y)
        {
            if (World.TargetManager.IsTargeting || _wordOfDeathIconBounds.Width <= 0 || _wordOfDeathIconBounds.Height <= 0 || !SerialHelper.IsMobile(LocalSerial))
            {
                return false;
            }

            Mobile mobile = World.Mobiles.Get(LocalSerial);

            if (!ShouldDrawWordOfDeathIcon(mobile))
            {
                return false;
            }

            Rectangle localIconBounds = new Rectangle(
                _wordOfDeathIconBounds.X - X,
                _wordOfDeathIconBounds.Y - Y,
                _wordOfDeathIconBounds.Width,
                _wordOfDeathIconBounds.Height
            );

            if (!localIconBounds.Contains(x, y))
            {
                return false;
            }

            TargetManager.SetAutoTarget(LocalSerial, TargetType.Harmful);
            GameActions.CastSpell(WORD_OF_DEATH_SPELL_ID);

            return true;
        }

        private int GetWordOfDeathIconSize(Rectangle nameBounds)
        {
            int maxSize = Math.Min(nameBounds.Height - WORD_OF_DEATH_ICON_PADDING * 2, _text.Height + 2);

            return Math.Clamp(maxSize, 0, Math.Min(18, nameBounds.Width - WORD_OF_DEATH_ICON_PADDING * 2));
        }

        private void DrawResourceBar(UltimaBatcher2D batcher, Rectangle bounds, Texture2D fillTexture, Vector3 fillHue, double percent, float alpha)
        {
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                return;
            }

            int radius = GetCornerRadius(bounds.Width, bounds.Height);
            percent = double.IsNaN(percent) || double.IsInfinity(percent) ? 0 : Math.Clamp(percent, 0, 1);

            DrawRoundedRectangle(batcher, _borderColor, bounds, ShaderHueTranslator.GetHueVector(0), radius);

            Rectangle fillBounds = new Rectangle(bounds.X + 1, bounds.Y + 1, Math.Max(0, bounds.Width - 2), Math.Max(0, bounds.Height - 2));

            if (fillBounds.Width > 0 && fillBounds.Height > 0)
            {
                DrawRoundedRectangle(
                        batcher,
                        SolidColorTextureCache.GetTexture(MissingHealthBackgroundColor),
                        fillBounds,
                        ShaderHueTranslator.GetHueVector(0, false, alpha),
                        Math.Max(0, radius - 1)
                    );

                int fillWidth = Math.Min(fillBounds.Width, (int)Math.Round(fillBounds.Width * percent));

                if (fillWidth > 0)
                {
                    DrawRoundedRectangleClipped(batcher, fillTexture, fillBounds, fillHue, Math.Max(0, radius - 1), fillWidth);
                }
            }
        }

        private static double GetResourcePercent(int current, int max)
        {
            return max <= 0 ? 0 : Math.Clamp((double)current / max, 0, 1);
        }

        private static int GetCornerRadius(int width, int height)
        {
            return Math.Clamp(ProfileManager.CurrentProfile.NamePlateCornerRadius, 0, Math.Min(width, height) >> 1);
        }

        // Renderer-only rounded fill: uses UltimaBatcher2D/XNA textures, not System.Drawing/GDI.
        private static void DrawRoundedRectangle(UltimaBatcher2D batcher, Texture2D texture, Rectangle bounds, Vector3 hueVector, int radius)
        {
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                return;
            }

            radius = Math.Clamp(radius, 0, Math.Min(bounds.Width, bounds.Height) >> 1);

            if (radius <= 0)
            {
                batcher.Draw(texture, bounds, hueVector);
                return;
            }

            int[] insets = GetRoundedRectangleInsets(bounds.Height, radius);

            for (int row = 0; row < bounds.Height; row++)
            {
                int inset = insets[row];
                int width = bounds.Width - inset * 2;

                if (width > 0)
                {
                    batcher.Draw(texture, new Rectangle(bounds.X + inset, bounds.Y + row, width, 1), hueVector);
                }
            }
        }

        private static void DrawRoundedRectangleClipped(UltimaBatcher2D batcher, Texture2D texture, Rectangle bounds, Vector3 hueVector, int radius, int clipWidth)
        {
            if (bounds.Width <= 0 || bounds.Height <= 0 || clipWidth <= 0)
            {
                return;
            }

            radius = Math.Clamp(radius, 0, Math.Min(bounds.Width, bounds.Height) >> 1);
            int clippedWidth = Math.Min(bounds.Width, clipWidth);

            if (radius <= 0)
            {
                batcher.Draw(texture, new Rectangle(bounds.X, bounds.Y, clippedWidth, bounds.Height), hueVector);
                return;
            }

            int clipRight = bounds.X + clippedWidth;
            int[] insets = GetRoundedRectangleInsets(bounds.Height, radius);

            for (int row = 0; row < bounds.Height; row++)
            {
                int inset = insets[row];
                int rowLeft = bounds.X + inset;
                int rowRight = Math.Min(bounds.Right - inset, clipRight);
                int width = rowRight - rowLeft;

                if (width > 0)
                {
                    batcher.Draw(texture, new Rectangle(rowLeft, bounds.Y + row, width, 1), hueVector);
                }
            }
        }

        private static int[] GetRoundedRectangleInsets(int height, int radius)
        {
            long cacheKey = ((long)height << 32) | (uint)radius;

            if (RoundedRectangleInsetCache.TryGetValue(cacheKey, out int[] insets))
            {
                return insets;
            }

            insets = new int[height];

            for (int row = 0; row < height; row++)
            {
                if (row < radius)
                {
                    double dy = radius - row - 0.5d;
                    insets[row] = Math.Max(0, radius - (int)Math.Sqrt(radius * radius - dy * dy));
                }
                else if (row >= height - radius)
                {
                    double dy = row - (height - radius) + 0.5d;
                    insets[row] = Math.Max(0, radius - (int)Math.Sqrt(radius * radius - dy * dy));
                }
            }

            RoundedRectangleInsetCache[cacheKey] = insets;
            return insets;
        }

        public override void Dispose()
        {
            _text?.Dispose();
            base.Dispose();
        }
    }
}
