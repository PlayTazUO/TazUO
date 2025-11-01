// SPDX-License-Identifier: BSD-2-Clause


using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.Network;
using ClassicUO.Resources;
using ClassicUO.Utility;
using System;
using System.Threading;
using System.Threading.Tasks;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Managers
{
    public enum CursorTarget
    {
        Invalid = -1,
        Object = 0,
        Position = 1,
        MultiPlacement = 2,
        SetTargetClientSide = 3,
        Grab,
        SetGrabBag,
        SetMount,
        HueCommandTarget,
        IgnorePlayerTarget,
        MoveItemContainer,
        Internal,
        SetFavoriteMoveBag,
        CallbackTarget
    }

    public class CursorType
    {
        public const uint Target = 6983686;
    }

    public enum TargetType
    {
        Neutral,
        Harmful,
        Beneficial,
        Cancel
    }

    public class MultiTargetInfo
    {
        public MultiTargetInfo(ushort model, ushort x, ushort y, ushort z, ushort hue)
        {
            Model = model;
            XOff = x;
            YOff = y;
            ZOff = z;
            Hue = hue;
        }

        public readonly ushort XOff, YOff, ZOff, Model, Hue;
    }

    public readonly struct Vector3Int
    {
        public readonly int X;
        public readonly int Y;
        public readonly int Z;

        public Vector3Int(int x, int y, int z)
        {
            (X, Y, Z) = (x, y, z);
        }

        public override string ToString() => $"({X}, {Y}, {Z})";
    }

    public class LastTargetInfo
    {
        public bool IsEntity => IsSet && SerialHelper.IsValid(Serial);
        public bool IsStatic => IsSet && !IsEntity && Graphic != 0 && Graphic != 0xFFFF;
        public bool IsLand => IsSet && !IsStatic;
        public Vector3Int Position => new Vector3Int(X, Y, Z);

        public ushort Graphic { get; internal set; }
        public uint Serial { get; internal set; }
        public ushort X { get; internal set; }
        public ushort Y { get; internal set; }
        public sbyte Z { get; internal set; }
        public bool IsSet { get; internal set; }

        internal void SetEntity(uint serial)
        {
            Serial = serial;
            Graphic = 0xFFFF;
            X = Y = 0xFFFF;
            Z = sbyte.MinValue;
            IsSet = true;
        }

        internal void SetStatic(ushort graphic, ushort x, ushort y, sbyte z)
        {
            Serial = 0;
            Graphic = graphic;
            X = x;
            Y = y;
            Z = z;
            IsSet = true;
        }

        internal void SetLand(ushort x, ushort y, sbyte z)
        {
            Serial = 0;
            Graphic = 0xFFFF;
            X = x;
            Y = y;
            Z = z;
            IsSet = true;
        }

        internal void Clear()
        {
            Serial = 0;
            Graphic = 0xFFFF;
            X = Y = 0xFFFF;
            Z = sbyte.MinValue;
            IsSet = false;
        }
    }

    public class AutoTargetInfo
    {
        public uint TargetSerial { get; set; }
        public TargetType ExpectedTargetType { get; set; }
        public bool IsSet => TargetSerial != 0;

        public void Set(uint serial, TargetType targetType)
        {
            TargetSerial = serial;
            ExpectedTargetType = targetType;
        }

        public void Clear()
        {
            TargetSerial = 0;
            ExpectedTargetType = TargetType.Cancel;
        }
    }

    public sealed class TargetManager
    {
        private uint _targetCursorId, _lastAttack;
        private readonly World _world;
        private readonly byte[] _lastDataBuffer = new byte[19];
        private Action<object> _targetCallback;

        public TargetManager(World world) { _world = world; }

        public uint SelectedTarget, NewTargetSystemSerial;

        public uint LastAttack
        {
            get { return _lastAttack; }
            set
            {
                _lastAttack = value;
                if (ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.OpenHealthBarForLastAttack)
                {
                    if (ProfileManager.CurrentProfile.UseOneHPBarForLastAttack)
                    {
                        if (BaseHealthBarGump.LastAttackBar != null && !BaseHealthBarGump.LastAttackBar.IsDisposed)
                        {
                            if (BaseHealthBarGump.LastAttackBar.LocalSerial != value)
                            {
                                BaseHealthBarGump.LastAttackBar.SetNewMobile(value);
                            }
                        }
                        else
                        {
                            if (ProfileManager.CurrentProfile.CustomBarsToggled)
                                UIManager.Add(BaseHealthBarGump.LastAttackBar = new HealthBarGumpCustom(_world, value) { Location = ProfileManager.CurrentProfile.LastTargetHealthBarPos, IsLastTarget = true });
                            else
                                UIManager.Add(BaseHealthBarGump.LastAttackBar = new HealthBarGump(_world, value) { Location = ProfileManager.CurrentProfile.LastTargetHealthBarPos, IsLastTarget = true });
                        }
                    }
                    else
                    {
                        if (UIManager.GetGump<BaseHealthBarGump>(value) == null)
                        {
                            if (ProfileManager.CurrentProfile.CustomBarsToggled)
                                UIManager.Add(new HealthBarGumpCustom(_world, value) { Location = ProfileManager.CurrentProfile.LastTargetHealthBarPos, IsLastTarget = true });
                            else
                                UIManager.Add(new HealthBarGump(_world, value) { Location = ProfileManager.CurrentProfile.LastTargetHealthBarPos, IsLastTarget = true });
                        }
                    }
                }
            }
        }

        public readonly LastTargetInfo LastTargetInfo = new LastTargetInfo();

        public static readonly AutoTargetInfo NextAutoTarget = new AutoTargetInfo();

        public MultiTargetInfo MultiTargetInfo { get; private set; }

        public CursorTarget TargetingState { get; private set; } = CursorTarget.Invalid;

        public bool IsTargeting { get; private set; }

        public TargetType TargetingType { get; private set; }

        private void ClearTargetingWithoutTargetCancelPacket()
        {
            if (TargetingState == CursorTarget.MultiPlacement)
            {
                MultiTargetInfo = null;
                TargetingState = 0;
                _world.HouseManager.Remove(0);
            }

            IsTargeting = false;
        }

        public void Reset()
        {
            ClearTargetingWithoutTargetCancelPacket();

            _targetCallback = null;
            TargetingState = 0;
            _targetCursorId = 0;
            MultiTargetInfo = null;
            TargetingType = 0;
        }

        public void SetTargeting(Action<object> callback, uint cursorId = CursorType.Target, TargetType cursorType = TargetType.Neutral)
        {
            _targetCallback = callback;
            SetTargeting(CursorTarget.CallbackTarget, cursorId, cursorType);
        }

        public void SetTargeting(CursorTarget targeting, uint cursorID, TargetType cursorType)
        {
            if (targeting == CursorTarget.Invalid)
            {
                return;
            }

            bool lastTargetting = IsTargeting;
            IsTargeting = cursorType < TargetType.Cancel;
            TargetingState = targeting;
            TargetingType = cursorType;

            if (IsTargeting)
            {
                //UIManager.RemoveTargetLineGump(LastTarget);
            }
            else if (lastTargetting)
            {
                CancelTarget();
            }

            // https://github.com/andreakarasho/ClassicUO/issues/1373
            // when receiving a cancellation target from the server we need
            // to send the last active cursorID, so update cursor data later

            _targetCursorId = cursorID;
        }

        public static void SetAutoTarget(uint serial, TargetType targetType) => NextAutoTarget.Set(serial, targetType);

        public void CancelTarget()
        {
            if (TargetingState == CursorTarget.MultiPlacement)
            {
                _world.HouseManager.Remove(0);

                if (_world.CustomHouseManager != null)
                {
                    _world.CustomHouseManager.Erasing = false;
                    _world.CustomHouseManager.SeekTile = false;
                    _world.CustomHouseManager.SelectedGraphic = 0;
                    _world.CustomHouseManager.CombinedStair = false;

                    UIManager.GetGump<HouseCustomizationGump>()?.Update();
                }
            }

            if (TargetingState == CursorTarget.CallbackTarget)
            {
                _targetCallback?.Invoke(null);
            }

            if (IsTargeting || TargetingType == TargetType.Cancel)
            {
                AsyncNetClient.Socket.Send_TargetCancel(TargetingState, _targetCursorId, (byte)TargetingType);
                IsTargeting = false;
            }

            Reset();
            NextAutoTarget.Clear();
        }

        public void SetTargetingMulti
        (
            uint deedSerial,
            ushort model,
            ushort x,
            ushort y,
            ushort z,
            ushort hue
        )
        {
            SetTargeting(CursorTarget.MultiPlacement, deedSerial, TargetType.Neutral);

            //if (model != 0)
            MultiTargetInfo = new MultiTargetInfo
            (
                model,
                x,
                y,
                z,
                hue
            );
        }

        public void Target(uint serial)
        {
            if (!IsTargeting)
            {
                return;
            }

            NextAutoTarget.Clear();

            // Record action for script recording
            LegionScripting.ScriptRecorder.Instance.RecordTarget(serial);

            Entity entity = _world.InGame ? _world.Get(serial) : null;

            if (entity != null)
            {
                switch (TargetingState)
                {
                    case CursorTarget.Invalid: return;

                    case CursorTarget.Internal:
                        LastTargetInfo.SetEntity(serial);
                        ClearTargetingWithoutTargetCancelPacket();
                        Mouse.CancelDoubleClick = true;
                        break;
                    case CursorTarget.MultiPlacement:
                    case CursorTarget.Position:
                    case CursorTarget.Object:
                    case CursorTarget.HueCommandTarget:
                    case CursorTarget.SetTargetClientSide:

                        if (entity != _world.Player)
                        {
                            LastTargetInfo.SetEntity(serial);
                        }

                        if (SerialHelper.IsMobile(serial) && serial != _world.Player && (_world.Player.NotorietyFlag == NotorietyFlag.Innocent || _world.Player.NotorietyFlag == NotorietyFlag.Ally))
                        {
                            var mobile = entity as Mobile;

                            if (mobile != null)
                            {
                                bool showCriminalQuery = false;

                                if (TargetingType == TargetType.Harmful && ProfileManager.CurrentProfile.EnabledCriminalActionQuery && mobile.NotorietyFlag == NotorietyFlag.Innocent)
                                {
                                    showCriminalQuery = true;
                                }
                                else if (TargetingType == TargetType.Beneficial && ProfileManager.CurrentProfile.EnabledBeneficialCriminalActionQuery && (mobile.NotorietyFlag == NotorietyFlag.Criminal || mobile.NotorietyFlag == NotorietyFlag.Murderer || mobile.NotorietyFlag == NotorietyFlag.Gray))
                                {
                                    showCriminalQuery = true;
                                }

                                if (showCriminalQuery && UIManager.GetGump<QuestionGump>() == null)
                                {
                                    var messageBox = new QuestionGump
                                    (
                                        _world,
                                        "This may flag\nyou criminal!",
                                        s =>
                                        {
                                            if (s)
                                            {
                                                AsyncNetClient.Socket.Send_TargetObject(entity,
                                                                                   entity.Graphic,
                                                                                   entity.X,
                                                                                   entity.Y,
                                                                                   entity.Z,
                                                                                   _targetCursorId,
                                                                                   (byte)TargetingType);

                                                ClearTargetingWithoutTargetCancelPacket();

                                                if (LastTargetInfo.Serial != serial)
                                                {
                                                    GameActions.RequestMobileStatus(_world, serial);
                                                }
                                            }
                                        }
                                    );

                                    UIManager.Add(messageBox);

                                    return;
                                }
                            }
                        }

                        if (TargetingState != CursorTarget.SetTargetClientSide && TargetingState != CursorTarget.Internal)
                        {
                            _lastDataBuffer[0] = 0x6C;

                            _lastDataBuffer[1] = 0x00;

                            _lastDataBuffer[2] = (byte)(_targetCursorId >> 24);
                            _lastDataBuffer[3] = (byte)(_targetCursorId >> 16);
                            _lastDataBuffer[4] = (byte)(_targetCursorId >> 8);
                            _lastDataBuffer[5] = (byte)_targetCursorId;

                            _lastDataBuffer[6] = (byte)TargetingType;

                            _lastDataBuffer[7] = (byte)(entity.Serial >> 24);
                            _lastDataBuffer[8] = (byte)(entity.Serial >> 16);
                            _lastDataBuffer[9] = (byte)(entity.Serial >> 8);
                            _lastDataBuffer[10] = (byte)entity.Serial;

                            _lastDataBuffer[11] = (byte)(entity.X >> 8);
                            _lastDataBuffer[12] = (byte)entity.X;

                            _lastDataBuffer[13] = (byte)(entity.Y >> 8);
                            _lastDataBuffer[14] = (byte)entity.Y;

                            _lastDataBuffer[15] = (byte)(entity.Z >> 8);
                            _lastDataBuffer[16] = (byte)entity.Z;

                            _lastDataBuffer[17] = (byte)(entity.Graphic >> 8);
                            _lastDataBuffer[18] = (byte)entity.Graphic;


                            AsyncNetClient.Socket.Send_TargetObject(entity,
                                                               entity.Graphic,
                                                               entity.X,
                                                               entity.Y,
                                                               entity.Z,
                                                               _targetCursorId,
                                                               (byte)TargetingType);

                            if (SerialHelper.IsMobile(serial) && LastTargetInfo.Serial != serial)
                            {
                                GameActions.RequestMobileStatus(_world, serial);
                            }
                        }

                        ClearTargetingWithoutTargetCancelPacket();

                        Mouse.CancelDoubleClick = true;

                        break;

                    case CursorTarget.Grab:

                        if (SerialHelper.IsItem(serial))
                        {
                            GameActions.GrabItem(_world, serial, ((Item)entity).Amount);
                        }

                        ClearTargetingWithoutTargetCancelPacket();

                        return;

                    case CursorTarget.SetGrabBag:

                        if (SerialHelper.IsItem(serial))
                        {
                            ProfileManager.CurrentProfile.GrabBagSerial = serial;
                            GameActions.Print(_world, string.Format(ResGeneral.GrabBagSet0, serial));
                        }

                        ClearTargetingWithoutTargetCancelPacket();

                        return;

                    case CursorTarget.SetMount:

                        if (SerialHelper.IsMobile(serial))
                        {
                            ProfileManager.CurrentProfile.SavedMountSerial = serial;
                            Entity mount = _world.Get(serial);
                            string mountName = mount?.Name ?? "mount";
                            GameActions.Print(_world, $"Mount set: {mountName} (Serial: {serial})", 48);
                        }
                        else
                        {
                            GameActions.Print(_world, "You must target a mobile/creature to set as your mount.", 32);
                        }

                        ClearTargetingWithoutTargetCancelPacket();

                        return;
                    case CursorTarget.SetFavoriteMoveBag:
                        if (SerialHelper.IsItem(serial))
                        {
                            Item item = _world.Items.Get(serial);

                            if (item != null && item.ItemData.IsContainer)
                            {
                                ProfileManager.CurrentProfile.SetFavoriteMoveBagSerial = serial;
                                GameActions.Print(_world, "Favorite move bag set.");
                            }
                            else
                            {
                                GameActions.Print(_world, "That doesn't appear to be a valid container.");
                            }
                        }
                        else
                        {
                            GameActions.Print(_world, "That is not a valid item.");
                        }

                        ClearTargetingWithoutTargetCancelPacket();
                        return;
                    case CursorTarget.IgnorePlayerTarget:
                        if (SelectedObject.Object is Entity pmEntity)
                        {
                            _world.IgnoreManager.AddIgnoredTarget(pmEntity);
                        }
                        CancelTarget();
                        return;
                    case CursorTarget.MoveItemContainer:
                        if (SerialHelper.IsItem(serial))
                        {
                            MultiItemMoveGump.OnContainerTarget(_world, serial);
                        }
                        ClearTargetingWithoutTargetCancelPacket();
                        return;
                    case CursorTarget.CallbackTarget:
                        _targetCallback?.Invoke(entity);

                        ClearTargetingWithoutTargetCancelPacket();
                        return;
                }
            }
            else
            {
                // Handle cases where entity is null but we still want to use the serial
                switch (TargetingState)
                {
                    case CursorTarget.SetMount:
                        if (SerialHelper.IsMobile(serial))
                        {
                            ProfileManager.CurrentProfile.SavedMountSerial = serial;
                            GameActions.Print(_world, $"Mount set (Serial: {serial})", 48);
                        }
                        else
                        {
                            GameActions.Print(_world, "You must target a mobile/creature to set as your mount.", 32);
                        }
                        ClearTargetingWithoutTargetCancelPacket();
                        return;
                }
            }
        }

        public void Target(ushort graphic, ushort x, ushort y, short z, bool wet = false)
        {
            if (!IsTargeting)
            {
                return;
            }

            NextAutoTarget.Clear();

            // Record action for script recording
            LegionScripting.ScriptRecorder.Instance.RecordTargetLocation(x, y, z, graphic);

            switch (TargetingState)
            {
                case CursorTarget.CallbackTarget:
                    GameObject candidate = _world.Map.GetTile(x, y);

                    while (candidate != null)
                    {
                        if (candidate.Graphic == graphic && candidate.Z == z)
                        {
                            _targetCallback?.Invoke(candidate);
                            break;
                        }
                        candidate = candidate.TNext;
                    }

                    ClearTargetingWithoutTargetCancelPacket();
                    return;
            }

            if (graphic == 0)
            {
                if (TargetingState == CursorTarget.Object)
                {
                    return;
                }
            }
            else
            {
                if (graphic >= Client.Game.UO.FileManager.TileData.StaticData.Length)
                {
                    return;
                }

                ref StaticTiles itemData = ref Client.Game.UO.FileManager.TileData.StaticData[graphic];

                if (Client.Game.UO.Version >= ClientVersion.CV_7090 && itemData.IsSurface)
                {
                    z += itemData.Height;
                }
            }

            LastTargetInfo.SetStatic(graphic, x, y, (sbyte)z);

            TargetPacket(graphic, x, y, (sbyte)z);
        }

        public void SendMultiTarget(ushort x, ushort y, sbyte z)
        {
            TargetPacket(0, x, y, z);
            MultiTargetInfo = null;
        }

        public void TargetLast()
        {
            if (!IsTargeting)
            {
                return;
            }

            NextAutoTarget.Clear();

            _lastDataBuffer[0] = 0x6C;
            _lastDataBuffer[1] = (byte)TargetingState;
            _lastDataBuffer[2] = (byte)(_targetCursorId >> 24);
            _lastDataBuffer[3] = (byte)(_targetCursorId >> 16);
            _lastDataBuffer[4] = (byte)(_targetCursorId >> 8);
            _lastDataBuffer[5] = (byte)_targetCursorId;
            _lastDataBuffer[6] = (byte)TargetingType;

            AsyncNetClient.Socket.Send(_lastDataBuffer);
            Mouse.CancelDoubleClick = true;
            ClearTargetingWithoutTargetCancelPacket();
        }

        private void TargetPacket(ushort graphic, ushort x, ushort y, sbyte z)
        {
            if (!IsTargeting)
            {
                return;
            }

            NextAutoTarget.Clear();

            _lastDataBuffer[0] = 0x6C;

            _lastDataBuffer[1] = 0x01;

            _lastDataBuffer[2] = (byte)(_targetCursorId >> 24);
            _lastDataBuffer[3] = (byte)(_targetCursorId >> 16);
            _lastDataBuffer[4] = (byte)(_targetCursorId >> 8);
            _lastDataBuffer[5] = (byte)_targetCursorId;

            _lastDataBuffer[6] = (byte)TargetingType;

            _lastDataBuffer[7] = (byte)(0 >> 24);
            _lastDataBuffer[8] = (byte)(0 >> 16);
            _lastDataBuffer[9] = (byte)(0 >> 8);
            _lastDataBuffer[10] = (byte)0;

            _lastDataBuffer[11] = (byte)(x >> 8);
            _lastDataBuffer[12] = (byte)x;

            _lastDataBuffer[13] = (byte)(y >> 8);
            _lastDataBuffer[14] = (byte)y;

            _lastDataBuffer[15] = (byte)(z >> 8);
            _lastDataBuffer[16] = (byte)z;

            _lastDataBuffer[17] = (byte)(graphic >> 8);
            _lastDataBuffer[18] = (byte)graphic;



            AsyncNetClient.Socket.Send_TargetXYZ(graphic,
                                            x,
                                            y,
                                            z,
                                            _targetCursorId,
                                            (byte)TargetingType);


            Mouse.CancelDoubleClick = true;
            ClearTargetingWithoutTargetCancelPacket();
        }
    }

}
