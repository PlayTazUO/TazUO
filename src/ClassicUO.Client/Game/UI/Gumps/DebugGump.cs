// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Diagnostics;
using System.Xml;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    public class DebugGump : Gump
    {
        private const string DEBUG_STRING_0 = "- FPS: {0} (Min={1}, Max={2}), Zoom: {3:0.00}, Total Objs: {4}\n";
        private const string DEBUG_STRING_1 = "- Mobiles: {0}   Items: {1}   Statics: {2}   Multi: {3}   Lands: {4}   Effects: {5}\n";
        private const string DEBUG_STRING_2 = "- CharPos: {0}\n- Mouse: {1}\n- InGamePos: {2}\n";
        private const string DEBUG_STRING_3 = "- Selected: {0}";
        private const string DEBUG_STRING_4 = "\n- MoveGap: last {0}ms max {1}ms  Δ({2},{3},{4})";
        private const string DEBUG_STRING_5 = "\n- Walker: steps {0} unacked {1} failed {2} lastReq {3}ms";
        private const string DEBUG_STRING_6 = "\n- CamOff: {0:0.0},{1:0.0}  Δ {2:0.0}";
        private const string DEBUG_STRING_7 = "\n- Frame: last {0}ms max {1}ms  Update {2}ms  Draw {3}ms";
        private const string DEBUG_STRING_8 = "\n- GC: gen0 {0} gen1 {1} gen2 {2}  mem {3:0.0}MB";

        private const string DEBUG_STRING_SMALL = "FPS: {0}\nZoom: {1:0.00}";
        private const string DEBUG_STRING_SMALL_NO_ZOOM = "FPS: {0}";
        private static Point _last_position = new Point(-1, -1);

        private uint _timeToUpdate;
        private readonly AlphaBlendControl _alphaBlendControl;
        private string _cacheText = string.Empty;
        private bool _hasLastPlayerPos;
        private int _lastPlayerX;
        private int _lastPlayerY;
        private int _lastPlayerZ;
        private long _lastPlayerMoveTick;
        private int _lastMoveGapMs;
        private int _maxMoveGapMs;
        private Vector2 _lastCameraOffset;
        private long _lastFrameTick;
        private int _lastFrameMs;
        private int _maxFrameMs;
        private long _frameWindowStart;
        private int _lastUpdateMs;
        private int _lastDrawMs;

        public DebugGump(World world, int x, int y) : base(world, 0, 0)
        {
            CanMove = true;
            CanCloseWithEsc = false;
            CanCloseWithRightClick = true;
            AcceptMouseInput = true;
            AcceptKeyboardInput = false;

            Width = 100;
            Height = 50;
            X = _last_position.X <= 0 ? x : _last_position.X;
            Y = _last_position.Y <= 0 ? y : _last_position.Y;

            Add
            (
                _alphaBlendControl = new AlphaBlendControl(.7f)
                {
                    Width = Width, Height = Height
                }
            );

            LayerOrder = UILayer.Over;

            WantUpdateSize = true;
        }

        public bool IsMinimized { get; set; }

        public override GumpType GumpType => GumpType.Debug;

        protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                IsMinimized = !IsMinimized;

                return true;
            }

            return false;
        }

        public override void Update()
        {
            base.Update();

            if (IsDisposed)
            {
                return;
            }

            if (Time.Ticks > _timeToUpdate)
            {
                long updateStart = Stopwatch.GetTimestamp();
                _timeToUpdate = Time.Ticks + 100;
                UpdateFrameTiming();

                GameScene scene = Client.Game.GetScene<GameScene>();
                Span<char> span = stackalloc char[256];
                var sb = new ValueStringBuilder(span);

                if (IsMinimized && scene != null)
                {
                    UpdateMovementDiagnostics(scene);

                    sb.Append
                    (string.Format(
                         DEBUG_STRING_0,
                         CUOEnviroment.CurrentRefreshRate,
                         0,
                         0,
                         !World.InGame ? 1f : scene.Camera.Zoom,
                         scene.RenderedObjectsCount
                         )
                     );

                    sb.Append($"- CUO version: {CUOEnviroment.Version}, Client version: {Settings.GlobalSettings.ClientVersion}\n");

                    //_sb.AppendFormat(DEBUG_STRING_1, Engine.DebugInfo.MobilesRendered, Engine.DebugInfo.ItemsRendered, Engine.DebugInfo.StaticsRendered, Engine.DebugInfo.MultiRendered, Engine.DebugInfo.LandsRendered, Engine.DebugInfo.EffectsRendered);
                    sb.Append(string.Format(DEBUG_STRING_2, World.InGame ? $"{World.Player.X}, {World.Player.Y}, {World.Player.Z}" : "0xFFFF, 0xFFFF, 0", Mouse.Position, SelectedObject.Object is GameObject gobj ? $"{gobj.X}, {gobj.Y}, {gobj.Z}" : "0xFFFF, 0xFFFF, 0"));
                    sb.Append(string.Format(
                        DEBUG_STRING_4,
                        _lastMoveGapMs,
                        _maxMoveGapMs,
                        World.InGame ? World.Player.X - _lastPlayerX : 0,
                        World.InGame ? World.Player.Y - _lastPlayerY : 0,
                        World.InGame ? World.Player.Z - _lastPlayerZ : 0
                    ));
                    if (World.InGame)
                    {
                        long lastReqDelta = World.Player.Walker.LastStepRequestTime > 0 ? (Time.Ticks - World.Player.Walker.LastStepRequestTime) : 0;
                        sb.Append(string.Format(
                            DEBUG_STRING_5,
                            World.Player.Walker.StepsCount,
                            World.Player.Walker.UnacceptedPacketsCount,
                            World.Player.Walker.WalkingFailed,
                            lastReqDelta
                        ));
                    }
                    sb.Append(string.Format(
                        DEBUG_STRING_6,
                        scene.Camera.Offset.X,
                        scene.Camera.Offset.Y,
                        Vector2.Distance(scene.Camera.Offset, _lastCameraOffset)
                    ));
                    sb.Append(string.Format(
                        DEBUG_STRING_7,
                        _lastFrameMs,
                        _maxFrameMs,
                        _lastUpdateMs,
                        _lastDrawMs
                    ));
                    sb.Append(string.Format(
                        DEBUG_STRING_8,
                        GC.CollectionCount(0),
                        GC.CollectionCount(1),
                        GC.CollectionCount(2),
                        GC.GetTotalMemory(false) / 1024f / 1024f
                    ));

                    sb.Append(string.Format(DEBUG_STRING_3, ReadObject(SelectedObject.Object)));

                    if (Profiler.Enabled)
                    {
                        double timeTotal = Profiler.TrackedTime;
                        
                        foreach (Profiler.ProfileData pd in Profiler.AllFrameData)
                        {
                            sb.Append($"\n[{pd.Context[pd.Context.Length - 1]}] [Last: {pd.LastTime:0.0}ms] [Total %: {100d * (pd.TimeInContext / timeTotal):0.00}]");
                        }
                    }
                }
                else
                {
                    if (scene != null)
                    {
                        UpdateMovementDiagnostics(scene);
                    }

                    int cameraZoomCount = (int)((scene.Camera.ZoomMax - scene.Camera.ZoomMin) / scene.Camera.ZoomStep);
                    int cameraZoomIndex = cameraZoomCount - (int)((scene.Camera.ZoomMax - scene.Camera.Zoom) / scene.Camera.ZoomStep);

                    if (scene != null && cameraZoomIndex != 5)
                    {
                        sb.Append(string.Format(DEBUG_STRING_SMALL, CUOEnviroment.CurrentRefreshRate, !World.InGame ? 1f : scene.Camera.Zoom));
                    }
                    else
                    {
                        sb.Append(string.Format(DEBUG_STRING_SMALL_NO_ZOOM, CUOEnviroment.CurrentRefreshRate));
                    }

                    if (scene != null && World.InGame)
                    {
                        sb.Append(string.Format(DEBUG_STRING_4, _lastMoveGapMs, _maxMoveGapMs, World.Player.X - _lastPlayerX, World.Player.Y - _lastPlayerY, World.Player.Z - _lastPlayerZ));
                        long lastReqDelta = World.Player.Walker.LastStepRequestTime > 0 ? (Time.Ticks - World.Player.Walker.LastStepRequestTime) : 0;
                        sb.Append(string.Format(DEBUG_STRING_5, World.Player.Walker.StepsCount, World.Player.Walker.UnacceptedPacketsCount, World.Player.Walker.WalkingFailed, lastReqDelta));
                        sb.Append(string.Format(DEBUG_STRING_6, scene.Camera.Offset.X, scene.Camera.Offset.Y, Vector2.Distance(scene.Camera.Offset, _lastCameraOffset)));
                        sb.Append(string.Format(DEBUG_STRING_7, _lastFrameMs, _maxFrameMs, _lastUpdateMs, _lastDrawMs));
                        sb.Append(string.Format(
                            DEBUG_STRING_8,
                            GC.CollectionCount(0),
                            GC.CollectionCount(1),
                            GC.CollectionCount(2),
                            GC.GetTotalMemory(false) / 1024f / 1024f
                        ));
                    }
                }


                _cacheText = sb.ToString();

                sb.Dispose();

                Vector2 size = Fonts.Bold.MeasureString(_cacheText);

                _alphaBlendControl.Width = Width = (int) (size.X + 20);
                _alphaBlendControl.Height = Height = (int) (size.Y + 20);

                WantUpdateSize = true;
                _lastUpdateMs = ElapsedMs(updateStart);
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            long drawStart = Stopwatch.GetTimestamp();
            if (!base.Draw(batcher, x, y))
            {
                return false;
            }

            Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);

            batcher.DrawString
            (
                Fonts.Bold,
                _cacheText,
                x + 10,
                y + 10,
                hueVector
            );

            _lastDrawMs = ElapsedMs(drawStart);
            return true;
        }

        private string ReadObject(BaseGameObject obj)
        {
            if (obj != null && IsMinimized)
            {
                switch (obj)
                {
                    case Mobile mob: return $"Mobile (0x{mob.Serial:X8})  graphic: 0x{mob.Graphic:X4}  flags: {mob.Flags}  noto: {mob.NotorietyFlag}";

                    case Item item: return $"Item (0x{item.Serial:X8})  graphic: 0x{item.Graphic:X4}  flags: {item.Flags}  amount: {item.Amount} itemdata: {item.ItemData.Flags}";

                    case Static st: return $"Static (0x{st.Graphic:X4})  height: {st.ItemData.Height}  flags: {st.ItemData.Flags}  Alpha: {st.AlphaHue}";

                    case Multi multi: return $"Multi (0x{multi.Graphic:X4})  height: {multi.ItemData.Height}  flags: {multi.ItemData.Flags}";

                    case GameEffect effect: return "GameEffect";

                    case TextObject overhead: return $"TextOverhead type: {overhead.Type}  hue: 0x{overhead.Hue:X4}";

                    case Land land: return $"Land (0x{land.Graphic:X4})  flags: {land.TileData.Flags} stretched: {land.IsStretched}  avgZ: {land.AverageZ} minZ: {land.MinZ}";
                }
            }

            return string.Empty;
        }

        private void UpdateMovementDiagnostics(GameScene scene)
        {
            if (!World.InGame || World.Player == null)
            {
                _hasLastPlayerPos = false;
                _lastMoveGapMs = 0;
                _maxMoveGapMs = 0;
                _lastCameraOffset = scene.Camera.Offset;
                return;
            }

            if (!_hasLastPlayerPos)
            {
                _hasLastPlayerPos = true;
                _lastPlayerX = World.Player.X;
                _lastPlayerY = World.Player.Y;
                _lastPlayerZ = World.Player.Z;
                _lastPlayerMoveTick = Time.Ticks;
                _lastMoveGapMs = 0;
                _maxMoveGapMs = 0;
                _lastCameraOffset = scene.Camera.Offset;
                return;
            }

            if (_lastPlayerX != World.Player.X || _lastPlayerY != World.Player.Y || _lastPlayerZ != World.Player.Z)
            {
                long now = Time.Ticks;
                _lastMoveGapMs = (int)Math.Max(0, now - _lastPlayerMoveTick);
                if (_lastMoveGapMs > _maxMoveGapMs)
                {
                    _maxMoveGapMs = _lastMoveGapMs;
                }

                _lastPlayerX = World.Player.X;
                _lastPlayerY = World.Player.Y;
                _lastPlayerZ = World.Player.Z;
                _lastPlayerMoveTick = now;
            }

            _lastCameraOffset = scene.Camera.Offset;
        }

        private void UpdateFrameTiming()
        {
            long now = Time.Ticks;
            if (_lastFrameTick == 0)
            {
                _lastFrameTick = now;
                _frameWindowStart = now;
                _lastFrameMs = 0;
                _maxFrameMs = 0;
                return;
            }

            _lastFrameMs = (int)Math.Max(0, now - _lastFrameTick);
            _lastFrameTick = now;

            if (now - _frameWindowStart > 1000)
            {
                _frameWindowStart = now;
                _maxFrameMs = _lastFrameMs;
            }
            else if (_lastFrameMs > _maxFrameMs)
            {
                _maxFrameMs = _lastFrameMs;
            }
        }

        private static int ElapsedMs(long startTimestamp)
        {
            long ticks = Stopwatch.GetTimestamp() - startTimestamp;
            return (int)((ticks * 1000) / Stopwatch.Frequency);
        }


        public override void Save(XmlTextWriter writer)
        {
            base.Save(writer);

            writer.WriteAttributeString("minimized", IsMinimized.ToString());
        }

        public override void Restore(XmlElement xml)
        {
            base.Restore(xml);

            bool.TryParse(xml.GetAttribute("minimized"), out bool b);
            IsMinimized = b;
        }

        protected override void OnDragEnd(int x, int y)
        {
            base.OnDragEnd(x, y);
            _last_position.X = ScreenCoordinateX;
            _last_position.Y = ScreenCoordinateY;
        }

        protected override void OnMove(int x, int y)
        {
            base.OnMove(x, y);

            _last_position.X = ScreenCoordinateX;
            _last_position.Y = ScreenCoordinateY;
        }
    }
}
