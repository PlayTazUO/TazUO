// SPDX-License-Identifier: BSD-2-Clause

using System;
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
        private const string DEBUG_STRING_1 = "- Mobiles: {0}   Items: {1}   Statics: {2}   Multi: {3}   Lands: {4}   Effects: {5}\n";

        private static Point _last_position = new Point(-1, -1);

        private uint _timeToUpdate;
        private readonly AlphaBlendControl _alphaBlendControl;
        private string _cacheText = string.Empty;

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

        public override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
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
                _timeToUpdate = Time.Ticks + 100;

                GameScene scene = Client.Game.GetScene<GameScene>();
                Span<char> span = stackalloc char[256];
                var sb = new ValueStringBuilder(span);

                if (IsMinimized && scene != null)
                {
                    sb.Append
                    (string.Format(
                         TazLang.Get("debuggump_fps_fmt", "- FPS: {0} (Min={1}, Max={2}), Zoom: {3:0.00}, Total Objs: {4}\n"),
                         CUOEnviroment.CurrentRefreshRate,
                         0,
                         0,
                         !World.InGame ? 1f : scene.Camera.Zoom,
                         scene.RenderedObjectsCount
                         )
                     );

                    sb.Append(string.Format(
                        TazLang.Get("debuggump_version_fmt", "- CUO version: {0}, Client version: {1}\n"),
                        CUOEnviroment.Version,
                        Settings.GlobalSettings.ClientVersion));

                    //_sb.AppendFormat(DEBUG_STRING_1, Engine.DebugInfo.MobilesRendered, Engine.DebugInfo.ItemsRendered, Engine.DebugInfo.StaticsRendered, Engine.DebugInfo.MultiRendered, Engine.DebugInfo.LandsRendered, Engine.DebugInfo.EffectsRendered);
                    sb.Append(string.Format(
                        TazLang.Get("debuggump_charpos_fmt", "- CharPos: {0}\n- Mouse: {1}\n- InGamePos: {2}\n"),
                        World.InGame ? $"{World.Player.X}, {World.Player.Y}, {World.Player.Z}" : "0xFFFF, 0xFFFF, 0",
                        Mouse.Position,
                        SelectedObject.Object is GameObject gobj ? $"{gobj.X}, {gobj.Y}, {gobj.Z}" : "0xFFFF, 0xFFFF, 0"));

                    sb.Append(string.Format(
                        TazLang.Get("debuggump_selected_fmt", "- Selected: {0}"),
                        ReadObject(SelectedObject.Object)));

                    if (Profiler.Enabled)
                    {
                        double timeTotal = Profiler.TrackedTime;

                        foreach (Profiler.ProfileData pd in Profiler.AllFrameData)
                        {
                            sb.Append(string.Format(
                                TazLang.Get("debuggump_profiler_fmt", "\n[{0}] [Last: {1:0.0}ms] [Total %: {2:0.00}]"),
                                pd.Context[pd.Context.Length - 1],
                                pd.LastTime,
                                100d * (pd.TimeInContext / timeTotal)));
                        }
                    }
                }
                else
                {
                    int cameraZoomCount = (int)((scene.Camera.ZoomMax - scene.Camera.ZoomMin) / scene.Camera.ZoomStep);
                    int cameraZoomIndex = cameraZoomCount - (int)((scene.Camera.ZoomMax - scene.Camera.Zoom) / scene.Camera.ZoomStep);

                    if (scene != null && cameraZoomIndex != 5)
                    {
                        sb.Append(string.Format(
                            TazLang.Get("debuggump_small_fmt", "FPS: {0}\nZoom: {1:0.00}"),
                            CUOEnviroment.CurrentRefreshRate,
                            !World.InGame ? 1f : scene.Camera.Zoom));
                    }
                    else
                    {
                        sb.Append(string.Format(
                            TazLang.Get("debuggump_small_nozoom_fmt", "FPS: {0}"),
                            CUOEnviroment.CurrentRefreshRate));
                    }
                }


                _cacheText = sb.ToString();

                sb.Dispose();

                Vector2 size = Fonts.Bold.MeasureString(_cacheText);

                _alphaBlendControl.Width = Width = (int) (size.X + 20);
                _alphaBlendControl.Height = Height = (int) (size.Y + 20);

                WantUpdateSize = true;
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
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

            return true;
        }

        private string ReadObject(BaseGameObject obj)
        {
            if (obj != null && IsMinimized)
            {
                switch (obj)
                {
                    case Mobile mob: return string.Format(
                        TazLang.Get("debuggump_readobj_mobile_fmt", "Mobile (0x{0:X8})  graphic: 0x{1:X4}  flags: {2}  noto: {3}"),
                        mob.Serial, mob.Graphic, mob.Flags, mob.NotorietyFlag);

                    case Item item: return string.Format(
                        TazLang.Get("debuggump_readobj_item_fmt", "Item (0x{0:X8})  graphic: 0x{1:X4}  flags: {2}  amount: {3} itemdata: {4}"),
                        item.Serial, item.Graphic, item.Flags, item.Amount, item.ItemData.Flags);

                    case Static st: return string.Format(
                        TazLang.Get("debuggump_readobj_static_fmt", "Static (0x{0:X4})  height: {1}  flags: {2}  Alpha: {3}"),
                        st.Graphic, st.ItemData.Height, st.ItemData.Flags, st.AlphaHue);

                    case Multi multi: return string.Format(
                        TazLang.Get("debuggump_readobj_multi_fmt", "Multi (0x{0:X4})  height: {1}  flags: {2}"),
                        multi.Graphic, multi.ItemData.Height, multi.ItemData.Flags);

                    case GameEffect effect: return TazLang.Get("debuggump_readobj_effect", "GameEffect");

                    case TextObject overhead: return string.Format(
                        TazLang.Get("debuggump_readobj_textoverhead_fmt", "TextOverhead type: {0}  hue: 0x{1:X4}"),
                        overhead.Type, overhead.Hue);

                    case Land land: return string.Format(
                        TazLang.Get("debuggump_readobj_land_fmt", "Land (0x{0:X4})  flags: {1} stretched: {2}  avgZ: {3} minZ: {4}"),
                        land.Graphic, land.TileData.Flags, land.IsStretched, land.AverageZ, land.MinZ);
                }
            }

            return string.Empty;
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
