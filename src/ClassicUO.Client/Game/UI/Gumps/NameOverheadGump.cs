// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
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
        private static int currentHeight = 22;
        private static readonly int COLLISION_SPACING = 8;

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

                _text.Text = t;

                Width = _background.Width = Math.Max(60, _text.Width) + 4;
                Height = _background.Height = CurrentHeight = Math.Max(Constants.OBJECT_HANDLES_GUMP_HEIGHT, _text.Height) + 4;
                _textDrawOffset.X = (Width - _text.Width - 4) >> 1;
                _textDrawOffset.Y = (Height - _text.Height) >> 1;
                WantUpdateSize = false;

                return true;
            }

            if (!string.IsNullOrEmpty(entity.Name))
            {
                if (_text == null)
                    return false;

                _text.Text = entity.Name;

                Width = _background.Width = Math.Max(60, _text.Width) + 4;
                Height = _background.Height = Math.Max(Constants.OBJECT_HANDLES_GUMP_HEIGHT, _text.Height) + 4;
                _textDrawOffset.X = (Width - _text.Width - 4) >> 1;
                _textDrawOffset.Y = (Height - _text.Height) >> 1;
                WantUpdateSize = false;

                return true;
            }

            return false;
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

        protected override void CloseWithRightClick()
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

        protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
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

        protected override void OnMouseDown(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                _lastLeftMousePositionDown = Mouse.Position;
                _leftMouseIsDown = true;
            }

            base.OnMouseDown(x, y, button);
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
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

        protected override void OnMouseOver(int x, int y)
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

                _lockedPosition.X = (int)(m.RealScreenPosition.X + m.Offset.X + 22 + 5);

                _lockedPosition.Y = (int)(
                    m.RealScreenPosition.Y
                    + (m.Offset.Y - m.Offset.Z)
                    - (height + centerY + 15)
                    + (
                        m.IsGargoyle && m.IsFlying
                            ? -22
                            : !m.IsMounted
                                ? 22
                                : 0
                    )
                );
            }

            base.OnMouseOver(x, y);
        }

        protected override void OnMouseExit(int x, int y)
        {
            _positionLocked = false;
            base.OnMouseExit(x, y);
        }

        private static List<NameOverheadGump> GetAllVisibleNameOverheads()
        {
            var result = new List<NameOverheadGump>();

            for (LinkedListNode<Gump> node = UIManager.Gumps.First; node != null; node = node.Next)
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

            bool _isMobile = false;
            double _hpPercent = 1;
            IsVisible = true;
            if (SerialHelper.IsMobile(LocalSerial))
            {
                Mobile m = World.Mobiles.Get(LocalSerial);

                if (m == null)
                {
                    Dispose();

                    return false;
                }

                if (!string.IsNullOrEmpty(NameOverHeadManager.Search))
                {
                    string sText = NameOverHeadManager.Search.ToLower();
                    if (m.Name == null || !m.Name.ToLower().Contains(sText))
                    {
                        if (World.OPL.TryGetNameAndData(m.Serial, out string name, out string data))
                        {
                            if (/*(data != null && !data.ToLower().Contains(sText)) && */(name != null && !name.ToLower().Contains(sText)))
                            {
                                IsVisible = false;
                                return true;
                            }
                        }
                        else
                        {
                            IsVisible = false;
                            return true;
                        }
                    }
                }

                _isMobile = true;
                _hpPercent = (double)m.Hits / (double)m.HitsMax;

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

                    x = (int)(m.RealScreenPosition.X + m.Offset.X + 22 + 5);
                    y = (int)(
                        m.RealScreenPosition.Y
                        + (m.Offset.Y - m.Offset.Z)
                        - (height + centerY + 15)
                        + (
                            m.IsGargoyle && m.IsFlying
                                ? -22
                                : !m.IsMounted
                                    ? 22
                                    : 0
                        )
                    );
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

                if (!string.IsNullOrEmpty(NameOverHeadManager.Search))
                {
                    string sText = NameOverHeadManager.Search.ToLower();
                    if (item.Name == null || !item.Name.ToLower().Contains(sText))// && (!item.ItemData.Name?.ToLower().Contains(sText)))
                    {
                        if (World.OPL.TryGetNameAndData(item.Serial, out string name, out string data))
                        {
                            if ((data != null && !data.ToLower().Contains(sText)) && (name != null && !name.ToLower().Contains(sText)))
                            {
                                IsVisible = false;
                                return true;
                            }
                        }
                        else
                        {
                            IsVisible = false;
                            return true;
                        }
                    }
                }

                if (!string.IsNullOrEmpty(NameOverHeadManager.Search))
                {
                    string sText = NameOverHeadManager.Search.ToLower();
                    if (item.Name == null || !item.Name.ToLower().Contains(sText))// && (!item.ItemData.Name?.ToLower().Contains(sText)))
                    {
                        if (World.OPL.TryGetNameAndData(item.Serial, out string name, out string data))
                        {
                            if ((data != null && !data.ToLower().Contains(sText)) && (name != null && !name.ToLower().Contains(sText)))
                            {
                                IsVisible = false;
                                return true;
                            }
                        }
                        else
                        {
                            IsVisible = false;
                            return true;
                        }
                    }
                }

                Rectangle bounds = Client.Game.UO.Arts.GetRealArtBounds(item.Graphic);

                x = item.RealScreenPosition.X + (int)item.Offset.X + 22 + 5;
                y =
                    item.RealScreenPosition.Y
                    + (int)(item.Offset.Y - item.Offset.Z)
                    + (bounds.Height >> 1);
            }

            Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);

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

            hueVector.Z = ProfileManager.CurrentProfile.NamePlateBorderOpacity / 100f;

            batcher.DrawRectangle
            (
                _borderColor,
                x,
                y,
                Width,
                Height,
                hueVector
            );

            base.Draw(batcher, x, y);

            if (ProfileManager.CurrentProfile.NamePlateHealthBar && _isMobile)
            {
                Mobile m = World.Mobiles.Get(LocalSerial);
                bool isPlayer = m is PlayerMobile;
                bool isInParty = World.Party.Contains(m.Serial);

                float _alpha = ProfileManager.CurrentProfile.NamePlateHealthBarOpacity / 100f;
                DrawResourceBar(batcher, m, x, y, Height / (isPlayer || isInParty ? 3 : 1), m =>
                {
                    double hpPercent = (double)m.Hits / (double)m.HitsMax;
                    int _baseHue = hpPercent switch
                    {
                        1 => (m is PlayerMobile || World.Party.Contains(m.Serial)) ? 0x0058 : Notoriety.GetHue(m.NotorietyFlag),
                        > .8 => 0x0058,
                        > .4 => 0x0030,
                        _ => 0x0021
                    };
                    Vector3 hueVec = ShaderHueTranslator.GetHueVector(_baseHue, false, _alpha);

                    if (m.IsPoisoned)
                    {
                        hueVec = ShaderHueTranslator.GetHueVector(63, false, _alpha);
                    }
                    else if (m.IsYellowHits || m.IsParalyzed)
                    {
                        hueVec = ShaderHueTranslator.GetHueVector(353, false, _alpha);
                    }
                    return (hueVec, hpPercent);
                }, out int nY);

                if (m is PlayerMobile || isInParty)
                {
                    DrawResourceBar(batcher, m, x, nY, Height / 3, m =>
                    {
                        double mpPercent = (double)m.Mana / (double)m.ManaMax;
                        int _baseHue = mpPercent switch
                        {
                            > .6 => 0x0058,
                            > .2 => 0x0030,
                            _ => 0x0021
                        };
                        Vector3 hueVec = ShaderHueTranslator.GetHueVector(_baseHue, false, _alpha);
                        return (hueVec, mpPercent);
                    }, out nY);

                    DrawResourceBar(batcher, m, x, nY, Height / 3, m =>
                    {
                        double spPercent = (double)m.Stamina / (double)m.StaminaMax;
                        int _baseHue = spPercent switch
                        {
                            > .8 => 0x0058,
                            > .5 => 0x0030,
                            _ => 0x0021
                        };
                        Vector3 hueVec = ShaderHueTranslator.GetHueVector(_baseHue, false, _alpha);
                        return (hueVec, spPercent);
                    }, out nY);
                    y += 20;
                }
            }

            return _text.Draw(batcher, (int)(x + 2 + _textDrawOffset.X), (int)(y + 2 + _textDrawOffset.Y));
        }

        private void DrawResourceBar(UltimaBatcher2D batcher, Mobile m, int x, int y, int height, Func<Mobile, (Vector3, double)> getHueVector, out int nY)
        {
            (Vector3, double) data = getHueVector == null ? (ShaderHueTranslator.GetHueVector(0x0058), 0) : getHueVector(m);
            batcher.DrawRectangle
            (
                _borderColor,
                x,
                y,
                Width,
                height,
                ShaderHueTranslator.GetHueVector(0)
            );
            batcher.Draw
            (
                SolidColorTextureCache.GetTexture(Color.White),
                new Vector2(x + 1, y + 1),
                new Rectangle(x, y, Math.Min((int)((Width - 1) * data.Item2), Width - 1), height),
                data.Item1
            );
            nY = y + height;
        }

        public override void Dispose()
        {
            _text?.Dispose();
            base.Dispose();
        }
    }
}
