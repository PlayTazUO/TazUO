// SPDX-License-Identifier: BSD-2-Clause


using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Managers.Hotkeys;
using ClassicUO.Game.UI;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.Network;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SDL3;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using ClassicUO.Common;
using ClassicUO.Game.Managers.SpellVisualRange;
using ClassicUO.Game.Map;
using ClassicUO.Game.ScreenDecorations.Manager;
using ClassicUO.Game.ScreenDecorations.Overlays;
using ClassicUO.Game.UI.Gumps.GridHighLight;
using ClassicUO.LegionScripting;
using ClassicUO.Network.PacketHandlers.Helpers;

namespace ClassicUO.Game.Scenes
{
    public partial class GameScene : Scene
    {
        public static GameScene Instance { get; private set; }

        private bool _waitingForWindowResize = false;
        private Point? _expectedWindowSize = null;
        private long _windowResizeStartTime = 0;
        private const int WINDOW_RESIZE_TIMEOUT_MS = 500;
        private const int TOLERANCE = 5;
        private const float MAX_LAYER_DEPTH = GameObjects.RenderDepth.FarPlane;

        private static readonly Lazy<BlendState> _darknessBlend = new Lazy<BlendState>(() =>
        {
            var state = new BlendState();
            state.ColorSourceBlend = Blend.Zero;
            state.ColorDestinationBlend = Blend.SourceColor;
            state.ColorBlendFunction = BlendFunction.Add;

            return state;
        });

        private static readonly Lazy<BlendState> _altLightsBlend = new Lazy<BlendState>(() =>
        {
            var state = new BlendState();
            state.ColorSourceBlend = Blend.DestinationColor;
            state.ColorDestinationBlend = Blend.One;
            state.ColorBlendFunction = BlendFunction.Add;

            return state;
        });

        private static XBREffect _xbr;
        private static FSREffect _fsr;
        private bool _alphaChanged;
        private long _alphaTimer;
        private bool _forceStopScene;
        private HealthLinesManager _healthLinesManager;

        private Point3D _lastSelectedMultiPositionInHouseCustomization;
        private int _lightCount;
        private readonly LightData[] _lights = new LightData[
            LightsLoader.MAX_LIGHTS_DATA_INDEX_COUNT
        ];

        // Drawn opaque tiles that can occlude a light, bucketed by isometric column (X - Y); rebuilt each frame, queried by AddLight.
        private readonly Dictionary<int, List<LightOccluder>> _lightOccluders =
            new Dictionary<int, List<LightOccluder>>();
        private readonly Stack<List<LightOccluder>> _lightOccluderPool =
            new Stack<List<LightOccluder>>();
        private Item _multi;
        private Rectangle _rectangleObj = Rectangle.Empty,
            _rectanglePlayer;
        private long _timePing;

        private uint _timeToPlaceMultiInHouseCustomization;
        private const int MAX_TEXTURE_SIZE = 8192;

        // An occluder d tiles in front of a light must stand ~11*d z-units above it to cover it on screen; slop is the tile-thickness leniency.
        private const int LIGHT_OCCLUSION_STEP = 11;
        private const int LIGHT_OCCLUSION_SLOP = 6;
        private static PostProcessingType _filterMode = PostProcessingType.Point;
        private PostProcessingType _currentFilter;
        private Effect _postFx;
        private SamplerState _postSampler = SamplerState.PointClamp;
        private readonly AutoUnequipActionManager _autoUnequipActionManager;
        private bool _useObjectHandles;
        private RenderTarget2D _worldRenderTarget, _lightRenderTarget;
        private AnimatedStaticsManager _animatedStaticsManager;

        private readonly World _world;

        public GameScene(World world)
        {
            _world = world;
            _autoUnequipActionManager = new AutoUnequipActionManager(_world);

            SDL.SDL_SetWindowMinimumSize(Client.Game.Window.Handle, 640, 480);

            Camera.Zoom = ProfileManager.CurrentProfile.DefaultScale;
            Camera.Bounds.X = Math.Max(0, ProfileManager.CurrentProfile.GameWindowPosition.X);
            Camera.Bounds.Y = Math.Max(0, ProfileManager.CurrentProfile.GameWindowPosition.Y);
            Camera.Bounds.Width = Math.Max(640, ProfileManager.CurrentProfile.GameWindowSize.X);
            Camera.Bounds.Height = Math.Max(480, ProfileManager.CurrentProfile.GameWindowSize.Y);

            Client.Game.Window.AllowUserResizing = true;

            if (ProfileManager.CurrentProfile.WindowBorderless)
            {
                Client.Game.SetWindowBorderless(true);
            }
            else if (Settings.GlobalSettings.IsWindowMaximized && !ProfileManager.CurrentProfile.BorderlessWindow)
            {
                _waitingForWindowResize = true;
                _windowResizeStartTime = Time.Ticks;
                Client.Game.MaximizeWindow();
            }
            else
            {
                // Determine target size: prioritize saved GlobalSettings, fallback to Profile
                int w, h;

                if (Settings.GlobalSettings.WindowSize.HasValue)
                {
                    w = Settings.GlobalSettings.WindowSize.Value.X;
                    h = Settings.GlobalSettings.WindowSize.Value.Y;
                }
                else
                {
                    // FALLBACK: Use profile's game window size
                    w = Math.Max(800, ProfileManager.CurrentProfile.GameWindowSize.X);
                    h = Math.Max(600, ProfileManager.CurrentProfile.GameWindowSize.Y);
                }

                w = Math.Max(640, w);
                h = Math.Max(480, h);

                _expectedWindowSize = new Point(w, h);
                _waitingForWindowResize = true;
                _windowResizeStartTime = Time.Ticks;

                Client.Game.SetWindowSize(w, h);
            }

            // Borderless (windowed) removes the window border while keeping the normal
            // windowed size. Skip it when fullscreen-borderless is active since that
            // mode already strips the border and resizes the window to fill the display.
            if (!ProfileManager.CurrentProfile.WindowBorderless && ProfileManager.CurrentProfile.BorderlessWindow)
            {
                Client.Game.SetWindowBordered(false);
            }

            SetPostProcessingSettings();

            Instance = this;
        }

        public void SetPostProcessingSettings()
        {
            _currentFilter = PostProcessingType.Invalid;
            _postFx = null;

            if (!ProfileManager.CurrentProfile.EnablePostProcessingEffects)
            {
                _filterMode = PostProcessingType.Point;
                return;
            }

            _filterMode = (PostProcessingType)ProfileManager.CurrentProfile.PostProcessingType;
        }
        private long _nextProfileSave = Time.Ticks + 1000*60*60;

        public bool UpdateDrawPosition { get; set; }
        public bool DisconnectionRequested { get; set; }

        private bool UseLights =>
            ProfileManager.CurrentProfile != null
            && ProfileManager.CurrentProfile.UseCustomLightLevel
                ? _world.Light.Personal < _world.Light.Overall
                : _world.Light.RealPersonal < _world.Light.RealOverall;

        private bool UseAltLights =>
            ProfileManager.CurrentProfile != null
            && ProfileManager.CurrentProfile.UseAlternativeLights;

        private uint _lastResync = Time.Ticks;
        private Matrix _worldRtMatrix;

        /// <summary>The crop of <see cref="_worldRenderTarget"/> that fills the viewport this frame.
        /// Varies with the camera zoom.</summary>
        private Rectangle _worldSrcRect;

        public override void Load()
        {
            base.Load();
            GridContainerSaveData.Instance.Load();
            ScreenOverlayManager.Instance.Start();

            Client.Game.UO.GameCursor.ItemHold.Clear();

            NameOverHeadManager.Load();

            _world.Macros.Clear();
            _world.Macros.Load();
            _animatedStaticsManager = new AnimatedStaticsManager();
            _animatedStaticsManager.Initialize();
            _world.InfoBars.Load();
            _healthLinesManager = new HealthLinesManager(_world);

            _world.CommandManager.Initialize();
            ItemDatabaseManager.Instance.Initialize();

            var viewport = new WorldViewportGump(_world, this);
            UIManager.Add(viewport, false);

            if (!ProfileManager.CurrentProfile.TopbarGumpIsDisabled)
            {
                TopBarGump.Create(_world);
            }

            AsyncNetClient.Socket.Disconnected += SocketOnDisconnected;
            EventSink.MessageReceived += ChatOnMessageReceived;
            UIManager.ContainerScale = ProfileManager.CurrentProfile.ContainersScale / 100f;

            Plugin.OnConnected();
            EventSink.InvokeOnConnected(null);
            GameController.UpdateBackgroundHueShader();
            SpellDefinition.LoadCustomSpells(_world);
            SpellVisualRangeManager.Instance.OnSceneLoad();
            AutoLootManager.Instance.OnSceneLoad();
            AutoSkinningManager.Instance.OnSceneLoad();
            ScavengerManager.Instance.OnSceneLoad();
            DressAgentManager.Instance.Load();
            FriendsListManager.Instance.OnSceneLoad();

            foreach (string xml in ProfileManager.CurrentProfile.AutoOpenXmlGumps)
            {
                XmlGumpHandler.TryAutoOpenByName(_world, xml);
            }

            PersistentVars.Load();
            LegionScripting.LegionScripting.Init(_world);
            BuySellAgent.Load();
            OrganizerAgent.Load();
            GraphicsReplacement.Load();
            HotKeys.Load();
            HotKeyRegistrar.RegisterAll();
            ScriptHotkeysManager.RegisterAll();
            SpellBarManager.Load();
            SelfHealManager.Load();
            if(ProfileManager.CurrentProfile.EnableCaveBorder)
                StaticFilters.ApplyCaveTileBorder();

            if (ProfileManager.CurrentProfile.VoiceRecognitionEnabled)
                VoiceRecognitionManager.Instance.InitializeAsync(ProfileManager.CurrentProfile.VoiceModelPath, startListeningAfter: true);
        }

        private void ChatOnMessageReceived(object sender, MessageEventArgs e)
        {
            if (e.Type == MessageType.Command)
            {
                return;
            }

            string name;
            string text;

            ushort hue = e.Hue;

            switch (e.Type)
            {
                case MessageType.ChatSystem:
                    name = e.Name;
                    text = e.Text;
                    break;
                case MessageType.Regular:
                case MessageType.Limit3Spell:

                    if (e.Parent == null || !SerialHelper.IsValid(e.Parent.Serial))
                        name = ProfileManager.CurrentProfile?.HideJournalSystemPrefix == true ? null : TazLang.Get("system");
                    else
                        name = e.Name;

                    text = e.Text;
                    break;

                case MessageType.System:
                    if (string.IsNullOrEmpty(e.Name) || string.Equals(e.Name, "system", StringComparison.InvariantCultureIgnoreCase))
                        name = ProfileManager.CurrentProfile?.HideJournalSystemPrefix == true ? null : TazLang.Get("system");
                    else
                        name = e.Name;

                    text = e.Text;

                    break;

                case MessageType.Emote:
                    name = e.Name;
                    text = $"{e.Text}";

                    if (e.Hue == 0)
                    {
                        hue = ProfileManager.CurrentProfile.EmoteHue;
                    }

                    break;

                case MessageType.Label:

                    if (e.Parent == null || !SerialHelper.IsValid(e.Parent.Serial))
                    {
                        name = string.Empty;
                    }
                    else if (string.IsNullOrEmpty(e.Name))
                    {
                        name = TazLang.Get("you_see");
                    }
                    else
                    {
                        name = e.Name;
                    }

                    text = e.Text;

                    break;

                case MessageType.Spell:
                    name = e.Name;
                    text = e.Text;

                    break;

                case MessageType.Party:
                    text = e.Text;
                    name = string.Format(TazLang.Get("party0"), e.Name);
                    hue = ProfileManager.CurrentProfile.PartyMessageHue;

                    break;

                case MessageType.Alliance:
                    text = e.Text;
                    name = string.Format(TazLang.Get("alliance0"), e.Name);
                    hue = ProfileManager.CurrentProfile.AllyMessageHue;

                    break;

                case MessageType.Guild:
                    text = e.Text;
                    name = string.Format(TazLang.Get("guild0"), e.Name);
                    hue = ProfileManager.CurrentProfile.GuildMessageHue;

                    break;

                default:
                    text = e.Text;
                    name = e.Name;
                    hue = e.Hue;

                    Log.Warn($"Unhandled text type {e.Type}  -  text: '{e.Text}'");

                    break;
            }

            if (!string.IsNullOrEmpty(text))
            {
                MessageType journalMessageType = JournalMessageClassifier.Classify(
                    e,
                    ProfileManager.CurrentProfile?.ClassifySystemMessagesAsGlobalChat == true,
                    ProfileManager.CurrentProfile?.SystemMessageGlobalChatRegex
                );

                _world.Journal.Add(text, hue, name, e.TextType, e.IsUnicode, journalMessageType);
            }
        }

        public override void Unload()
        {
            if (IsDestroyed)
            {
                if(Instance == this)
                    Instance = null;

                return;
            }

            Instance = null;

            GridHighlightsConfig.Unload();
            CooldownBarsConfig.Unload();
            TooltipOverridesConfig.Unload();
            GridContainerSaveData.Instance.Save();
            GridContainerSaveData.Reset();
            JournalFilterManager.Instance.Save();

            ScreenOverlayManager.Instance.Reset();
            SpellBarManager.Unload();
            SelfHealManager.Unload();
            _autoUnequipActionManager?.Dispose();
            ObjectActionQueue.Instance.Clear();

            GraphicsReplacement.Save();
            BuySellAgent.Unload();
            OrganizerAgent.Unload();

            PersistentVars.Unload();
            LegionScripting.LegionScripting.Unload();
            BandageManager.Instance.Dispose();
            DressAgentManager.Instance.Unload();

            ProfileManager.CurrentProfile.GameWindowPosition = new Point(
                Camera.Bounds.X,
                Camera.Bounds.Y
            );
            ProfileManager.CurrentProfile.GameWindowSize = new Point(
                Camera.Bounds.Width,
                Camera.Bounds.Height
            );
            ProfileManager.CurrentProfile.DefaultScale = Camera.Zoom;

            Client.Game.Audio?.StopMusic();
            Client.Game.Audio?.StopSounds();

            Client.Game.SetWindowTitle(string.Empty);
            Client.Game.UO.GameCursor.ItemHold.Clear();

            try
            {
                Plugin.OnDisconnected();
            }
            catch (Exception e)
            {
                Log.ErrorDebug(e.ToString());
            }

            EventSink.InvokeOnDisconnected(null);

            _world.TargetManager.Reset();

            // special case for wmap. this allow us to save settings
            UIManager.GetGump<WorldMapGump>()?.SaveSettings();

            ProfileManager.CurrentProfile?.Save(_world, ProfileManager.ProfilePath);
            MapWebServerManager.Instance.Stop();
            TileMarkerManager.Instance.Save();
            SpellVisualRangeManager.Instance.Save();
            SpellVisualRangeManager.Instance.OnSceneUnload();
            AutoLootManager.Instance.OnSceneUnload();
            GridHighlightData.Unload();
            AutoSkinningManager.Instance.OnSceneUnload();
            ScavengerManager.Instance.OnSceneUnload();
            FriendsListManager.Instance.OnSceneUnload();

            NameOverHeadManager.Save();

            _world.Macros.Save();
            _world.Macros.Clear();
            HotKeys.Save();
            _world.InfoBars.Save();
            ProfileManager.UnLoadProfile();

            StaticFilters.CleanTreeTextures();

            AsyncNetClient.Socket.Disconnected -= SocketOnDisconnected;
            _ = AsyncNetClient.Socket.Disconnect();
            _lightRenderTarget?.Dispose();
            _worldRenderTarget?.Dispose();
            _xbr?.Dispose();
            _xbr = null;
            _fsr?.Dispose();
            _fsr = null;

            _world.CommandManager.UnRegisterAll();
            _world.Weather.Reset();
            SkillProgressBar.QueManager.Reset();
            UIManager.Clear();
            _world.Clear();
            _world.ChatManager.Clear();
            _world.DelayedObjectClickManager.Clear();

            EventSink.MessageReceived -= ChatOnMessageReceived;

            Settings.GlobalSettings.WindowSize = new Point(
                Client.Game.Window.ClientBounds.Width,
                Client.Game.Window.ClientBounds.Height
            );

            Settings.GlobalSettings.IsWindowMaximized = Client.Game.IsWindowMaximized();
            Client.Game.SetWindowBorderless(false);
            Client.Game.SetWindowBordered(true);

            base.Unload();
        }

        private void SocketOnDisconnected(object sender, SocketError e) =>
            // Disconnected is raised from the background network/receive tasks (see AsyncNetClient),
            // but this handler tears down the scene and adds gumps, which mutates UIManager state
            // (_gumpTypeList, the Gumps list, etc.). Touching that off the main thread races the
            // game loop and corrupts those collections. Marshal onto the main thread so all the UI
            // work runs there; InvokeOnMainThread runs inline when already on the main thread.
            MainThreadQueue.InvokeOnMainThread(() =>
            {
                // The callback can be drained a frame later, by which point this scene may already
                // have been unloaded/replaced; skip the stale teardown then.
                if (IsDestroyed || Instance != this)
                    return;

                if (DisconnectionRequested)
                {
                    Client.Game.SetScene(new LoginScene(_world));

                    return;
                }
                if (Settings.GlobalSettings.Reconnect)
                {
                    LoginHandshake.Reconnect = true;
                    _forceStopScene = true;
                }
                else
                {
                    UIManager.Add(
                        new MessageBoxGump(
                            _world,
                            200,
                            200,
                            string.Format(
                                TazLang.Get("connection_lost0"),
                                StringHelper.AddSpaceBeforeCapital(e.ToString())
                            ),
                            s =>
                            {
                                if (s)
                                {
                                    Client.Game.SetScene(new LoginScene(_world));
                                }
                            }
                        )
                    );
                }
            });

        public void RequestQuitGame() => UIManager.Add(
                new QuestionGump(
                    _world,
                    Client.Game.UO.FileManager.Clilocs.GetString(3000000),
                    s =>
                    {
                        if (s)
                        {
                            GameActions.Logout(_world);
                        }
                    }
                )
            );

        public void AddLight(GameObject obj, GameObject lightObject, int x, int y)
        {
            if (
                _lightCount >= LightsLoader.MAX_LIGHTS_DATA_INDEX_COUNT
                || !UseLights && !UseAltLights
                || obj == null
            )
            {
                return;
            }

            bool canBeAdded = true;

            // Occluded if a tall enough tile in the light's own column (X - Y) sits in front of it toward the camera.
            if (_lightOccluders.TryGetValue(obj.X - obj.Y, out List<LightOccluder> occluders))
            {
                int lightX = obj.X;
                int lightZ = obj.Z;

                for (int i = 0; i < occluders.Count; i++)
                {
                    LightOccluder o = occluders[i];
                    int d = o.X - lightX;

                    // Only tiles in front of the light (nearer the camera) can hide it.
                    if (d <= 0)
                    {
                        continue;
                    }

                    if (o.Z < _maxZ && o.Z - lightZ >= LIGHT_OCCLUSION_STEP * d - LIGHT_OCCLUSION_SLOP)
                    {
                        canBeAdded = false;

                        break;
                    }
                }
            }

            if (canBeAdded)
            {
                ref LightData light = ref _lights[_lightCount];

                ushort graphic = lightObject.Graphic;

                if (
                    graphic >= 0x3E02 && graphic <= 0x3E0B
                    || graphic >= 0x3914 && graphic <= 0x3929
                    || graphic == 0x0B1D
                )
                {
                    light.Id = 2;
                }
                else
                {
                    if (obj == lightObject && obj is Item item)
                    {
                        light.Id = item.LightID;
                    }
                    else if (lightObject is Item it)
                    {
                        light.Id = (byte)it.ItemData.LightIndex;

                        if (obj is Mobile mob)
                        {
                            switch (mob.Direction)
                            {
                                case Direction.Right:
                                    y += 33;
                                    x += 22;

                                    break;

                                case Direction.Left:
                                    y += 33;
                                    x -= 22;

                                    break;

                                case Direction.East:
                                    x += 22;
                                    y += 55;

                                    break;

                                case Direction.Down:
                                    y += 55;

                                    break;

                                case Direction.South:
                                    x -= 22;
                                    y += 55;

                                    break;
                            }
                        }
                    }
                    else if (obj is Mobile _)
                    {
                        light.Id = 1;
                    }
                    else
                    {
                        ref StaticTiles data = ref Client.Game.UO.FileManager.TileData.StaticData[obj.Graphic];
                        light.Id = data.Layer;
                    }
                }

                light.Color = 0;
                light.IsHue = false;

                if (ProfileManager.CurrentProfile.UseColoredLights)
                {
                    if (light.Id > 200)
                    {
                        light.Color = (ushort)(light.Id - 200);
                        light.Id = 1;
                    }

                    if (LightColors.GetHue(graphic, out ushort color, out bool ishue))
                    {
                        light.Color = color;
                        light.IsHue = ishue;
                    }
                }

                if (light.Id >= LightsLoader.MAX_LIGHTS_DATA_INDEX_COUNT)
                {
                    return;
                }

                if (light.Color != 0)
                {
                    light.Color++;
                }

                light.DrawX = x;
                light.DrawY = y;
                light.WorldX = obj.X;
                light.WorldY = obj.Y;
                light.WorldZ = obj.Z;
                _lightCount++;
            }
        }

        public bool ASyncMapLoading = ProfileManager.CurrentProfile.EnableASyncMapLoading;

        private void FillGameObjectList()
        {
            _renderListStatics.Clear();
            _renderListAnimations.Clear();
            _renderListEffects.Clear();
            _renderListTransparentObjects.Clear();

            // Recycle this frame's column buckets to the pool so the map rebuilds without allocating.
            foreach (List<LightOccluder> bucket in _lightOccluders.Values)
            {
                bucket.Clear();
                _lightOccluderPool.Push(bucket);
            }

            _lightOccluders.Clear();

            _foliageCount = 0;

            if (!_world.InGame)
            {
                return;
            }

            _alphaChanged = _alphaTimer < Time.Ticks;

            if (_alphaChanged)
            {
                _alphaTimer = Time.Ticks + Constants.ALPHA_TIME;
            }

            FoliageIndex++;

            if (FoliageIndex >= 100)
            {
                FoliageIndex = 1;
            }

            GetViewPort();

            // The per-object selection pass (CheckMouseSelection -> pixel-picker RLE walks) runs for
            // every drawn object and dominates list-building cost. Skip it entirely while neither the
            // cursor nor the view transform moved; reuse last frame's selection, re-testing only the
            // cached object so one that moved out from under a stationary cursor still deselects.
            // When the cursor is over a gump the result is thrown away by Update/Draw anyway, so
            // don't pay for the pass at all.
            bool mouseOverWorld = UIManager.IsMouseOverWorld;
            _runWorldSelection =
                mouseOverWorld
                && (
                    _selectionDirty
                    || SelectedObject.TranslatedMousePositionByViewport != _lastSelectionMousePosition
                );
            _selectionDirty = false;

            if (_runWorldSelection)
            {
                _lastSelectionMousePosition = SelectedObject.TranslatedMousePositionByViewport;
                SelectedObject.Object = null;
            }
            else
            {
                // Temporarily clear so gump-set selections (from Update's OnMouseOver) don't leak
                // into the cached world result; re-assert it only if it's still under the cursor.
                SelectedObject.Object = null;

                if (
                    mouseOverWorld
                    && _lastWorldSelectionObject is GameObject cached
                    && IsWorldSelectionValid(cached)
                )
                {
                    SelectedObject.Object = cached;
                }
                else
                {
                    _lastWorldSelectionObject = null;
                }
            }

            bool useObjectHandles = NameOverHeadManager.IsShowing;
            if (useObjectHandles != _useObjectHandles)
            {
                _useObjectHandles = useObjectHandles;
                if (_useObjectHandles)
                {
                    _world.NameOverHeadManager.Open();
                }
                else
                {
                    _world.NameOverHeadManager.Close();
                }
            }

            _rectanglePlayer.X = (int)(
                _world.Player.RealScreenPosition.X
                - _world.Player.FrameInfo.X
                + 22
                + _world.Player.Offset.X
            );
            _rectanglePlayer.Y = (int)(
                _world.Player.RealScreenPosition.Y
                - _world.Player.FrameInfo.Y
                + 22
                + (_world.Player.Offset.Y - _world.Player.Offset.Z)
            );
            _rectanglePlayer.Width = _world.Player.FrameInfo.Width;
            _rectanglePlayer.Height = _world.Player.FrameInfo.Height;

            int minX = _minTile.X;
            int minY = _minTile.Y;
            int maxX = _maxTile.X;
            int maxY = _maxTile.Y;
            Map.Map map = _world.Map;
            bool useHandles = _useObjectHandles;
            int maxCotZ = _world.Player.Z + 5;
            Vector2 playerPos = _world.Player.GetScreenPosition();


            (int minChunkX, int minChunkY) = (minX >> 3, minY >> 3);
            (int maxChunkX, int maxChunkY) = (maxX >> 3, maxY >> 3);

            int totalChunksX = maxChunkX - minChunkX + 1;
            int totalChunksY = maxChunkY - minChunkY + 1;

            for (int chunkXIdx = 0; chunkXIdx < totalChunksX; chunkXIdx++)
            {
                int chunkX = minChunkX + chunkXIdx;
                for (int chunkYIdx = 0; chunkYIdx < totalChunksY; chunkYIdx++)
                {
                    int chunkY = minChunkY + chunkYIdx;

                    Chunk chunk = ASyncMapLoading ? map.PreloadChunk2(chunkX, chunkY) : map.GetChunk2(chunkX, chunkY);

                    if(chunk == null || chunk.IsDestroyed || chunk.IsLoading)
                        continue;

                    // Access tiles directly instead of calling GetHeadObject 64 times
                    GameObject[,] tiles = chunk.Tiles;
                    for (int tileIdx = 0; tileIdx < 64; tileIdx++) // 8x8 = 64
                    {
                        int x = tileIdx & 7;        // tileIdx % 8
                        int y = tileIdx >> 3;       // tileIdx / 8

                        // Inline GetHeadObject logic for better performance
                        GameObject firstObj = tiles[x, y];
                        while (firstObj?.TPrevious != null)
                        {
                            firstObj = firstObj.TPrevious;
                        }

                        if (firstObj?.IsDestroyed != false)
                            continue;

                        AddTileToRenderList(firstObj, useHandles, 150, maxCotZ, ref playerPos);
                    }
                }
            }



            //for (var x = minX; x <= maxX; x++)
            //    for (var y = minY; y <= maxY; y++)
            //    {
            //        AddTileToRenderList(
            //            map.GetTile(x, y),
            //            use_handles,
            //            150,
            //            maxCotZ,
            //            ref playerPos
            //        );
            //    }

            if (_alphaChanged)
            {
                for (int i = 0; i < _foliageCount; i++)
                {
                    GameObject f = _foliages[i];

                    if (f.FoliageIndex == FoliageIndex)
                    {
                        CalculateAlpha(ref f.AlphaHue, Constants.FOLIAGE_ALPHA);
                    }
                    else if (f.Z < _maxZ)
                    {
                        CalculateAlpha(ref f.AlphaHue, 0xFF);
                    }
                }
            }

            UpdateTextServerEntities(_world.Mobiles.Values, true);
            UpdateTextServerEntities(_world.Items.Values, false);

            if (_runWorldSelection)
            {
                _lastWorldSelectionObject = SelectedObject.Object as GameObject;
            }

            UpdateDrawPosition = false;
        }

        private void UpdateTextServerEntities<T>(IEnumerable<T> entities, bool force)
            where T : Entity
        {
            foreach (T e in entities)
            {
                if (
                    e.TextContainer != null
                    && !e.TextContainer.IsEmpty
                    && (force || e.Graphic == 0x2006)
                )
                {
                    e.UpdateRealScreenPosition(_offset.X, _offset.Y);
                }
            }
        }

        public override void Update()
        {
            Profiler.EnterContext("GameSceneUpdate");

            Profile currentProfile = ProfileManager.CurrentProfile;

            Point cameraMouse = Camera.MousePosition;
            Rectangle cameraBounds = Camera.Bounds;
            ulong cameraVersion = Camera.TransformVersion;

            // MouseToWorldPosition is a pure function of the mouse position and the camera transform;
            // skip the transform while both are unchanged (or no matrix rebuild is pending).
            if (
                Camera.IsMatrixDirty
                || cameraMouse != _lastTranslatedMouseMouse
                || cameraBounds != _lastTranslatedMouseBounds
                || cameraVersion != _lastTranslatedCameraVersion
            )
            {
                SelectedObject.TranslatedMousePositionByViewport = Camera.MouseToWorldPosition();
                _lastTranslatedMouseMouse = Camera.MousePosition;
                _lastTranslatedMouseBounds = Camera.Bounds;
                _lastTranslatedCameraVersion = Camera.TransformVersion;
            }

            base.Update();
            SelfHealManager.Update();
            ScreenOverlayManager.Instance.Tick();

            if (_waitingForWindowResize)
            {
                if (_expectedWindowSize.HasValue)
                {
                    Point expected = _expectedWindowSize.Value;
                    int actualWidth = Client.Game.Window.ClientBounds.Width;
                    int actualHeight = Client.Game.Window.ClientBounds.Height;

                    bool widthMatch = Math.Abs(actualWidth - expected.X) <= TOLERANCE;
                    bool heightMatch = Math.Abs(actualHeight - expected.Y) <= TOLERANCE;

                    if (widthMatch && heightMatch)
                    {
                        _waitingForWindowResize = false;
                        _expectedWindowSize = null;
                    }
                    else if (Time.Ticks - _windowResizeStartTime > WINDOW_RESIZE_TIMEOUT_MS)
                    {
                        Log.Trace($"Window resize timeout, retrying. Expected: {expected.X}x{expected.Y}, Actual: {actualWidth}x{actualHeight}");
                        Client.Game.SetWindowSize(expected.X, expected.Y);

                        _windowResizeStartTime = Time.Ticks;
                        _waitingForWindowResize = false;
                        _expectedWindowSize = null;
                    }
                }
                else
                {
                    if (Client.Game.IsWindowMaximized())
                    {
                        _waitingForWindowResize = false;
                    }
                    else if (Time.Ticks - _windowResizeStartTime > WINDOW_RESIZE_TIMEOUT_MS)
                    {
                        Log.Trace("Window maximize timeout, retrying.");
                        Client.Game.MaximizeWindow();

                        _windowResizeStartTime = Time.Ticks;
                        _waitingForWindowResize = false;
                    }
                }
            }

            SharedStore.SendMegaCliLocRequests(_world);

            if (_forceStopScene)
            {
                var loginScene = new LoginScene(_world);
                Client.Game.SetScene(loginScene);
                loginScene.Reconnect = true;

                Profiler.ExitContext("GameSceneUpdate");
                return;
            }

            if (!_world.InGame)
            {
                Profiler.ExitContext("GameSceneUpdate");
                return;
            }

            if (Time.Ticks > _timePing)
            {
                AsyncNetClient.Socket.Statistics.SendPing();
                _timePing = (long)Time.Ticks + 1000;
            }

            if (currentProfile.ForceResyncOnHang && Time.Ticks - AsyncNetClient.Socket.Statistics.LastPingReceived > 5000 && Time.Ticks - _lastResync > 5000)
            {
                AsyncNetClient.Socket.Send_Resync();
                _lastResync = Time.Ticks;
                GameActions.Print(_world, "Possible connection hang, resync attempted", 32, MessageType.System);
            }

            Profiler.EnterContext("WorldUpdate");
            _world.Update();
            _world.Weather.UpdateAudio();
            _animatedStaticsManager.Process();
            _world.BoatMovingManager.Update();
            _world.Player.Pathfinder.ProcessAutoWalk();
            _world.DelayedObjectClickManager.Update();
            Profiler.ExitContext("WorldUpdate");

            Profiler.EnterContext("Actions");
            if (
                (currentProfile.CorpseOpenOptions == 1 || currentProfile.CorpseOpenOptions == 3)
                    && _world.TargetManager.IsTargeting
                || (currentProfile.CorpseOpenOptions == 2 || currentProfile.CorpseOpenOptions == 3)
                    && _world.Player.IsHidden
            )
            {
                ObjectActionQueue.Instance.ClearByPriority(ActionPriority.OpenCorpse);
            }

            ObjectActionQueue.Instance.Update();
            AutoLootManager.Instance.Update();
            ScavengerManager.Instance.Update();
            BandageManager.Instance.Update();
            GridHighlightData.ProcessQueue(_world);
            Profiler.ExitContext("Actions");

            Profiler.EnterContext("Movement");
            bool useWASD = ProfileManager.GlobalSettings.UseWASDInsteadArrowKeys;

            if (!MoveCharacterByMouseInput() && (!currentProfile.DisableArrowBtn || useWASD) && !MoveCharByController())
            {
                Direction dir = DirectionHelper.DirectionFromKeyboardArrows(
                    _flags[0],
                    _flags[2],
                    _flags[1],
                    _flags[3]
                );

                if (_world.InGame && !_world.Player.Pathfinder.AutoWalking && dir != Direction.NONE)
                {
                    _world.Player.Walk(dir, currentProfile.AlwaysRun);
                }
            }

            if (currentProfile.FollowingMode && SerialHelper.IsMobile(currentProfile.FollowingTarget) && !_world.Player.Pathfinder.AutoWalking)
            {
                Mobile follow = _world.Mobiles.Get(currentProfile.FollowingTarget);

                if (follow != null)
                {
                    int distance = follow.Distance;

                    if (distance > _world.ClientViewRange)
                    {
                        StopFollowing();
                    }
                    else if (distance > currentProfile.AutoFollowDistance)
                    {
                        if (!_world.Player.Pathfinder.WalkTo(follow.X, follow.Y, follow.Z, currentProfile.AutoFollowDistance) && !_world.Player.IsParalyzed)
                        {
                            StopFollowing();
                        }
                    }
                }
                else
                {
                    StopFollowing();
                }
            }
            Profiler.ExitContext("Movement");

            _world.Macros.Update();

            if (Time.Ticks > _nextProfileSave)
            {
                ProfileManager.CurrentProfile.Save(_world, ProfileManager.ProfilePath);
                _nextProfileSave = Time.Ticks + 1000*60*60;
            }

            if (!UIManager.IsMouseOverWorld)
            {
                SelectedObject.Object = null;
            }

            if (
                _world.TargetManager.IsTargeting
                && _world.TargetManager.TargetingState == CursorTarget.MultiPlacement
                && _world.CustomHouseManager == null
                && _world.TargetManager.MultiTargetInfo != null
            )
            {
                if (_multi == null)
                {
                    _multi = Item.Create(_world, 0);
                    _multi.Graphic = _world.TargetManager.MultiTargetInfo.Model;
                    _multi.Hue = _world.TargetManager.MultiTargetInfo.Hue;
                    _multi.IsMulti = true;
                }

                if (SelectedObject.Object is GameObject gobj)
                {
                    ushort x,
                        y;
                    sbyte z;

                    int cellX = gobj.X % 8;
                    int cellY = gobj.Y % 8;

                    GameObject o = _world.Map.GetChunk(gobj.X, gobj.Y)?.Tiles[cellX, cellY];

                    if (o != null)
                    {
                        x = o.X;
                        y = o.Y;
                    }
                    else
                    {
                        x = gobj.X;
                        y = gobj.Y;
                    }

                    _world.Map.GetMapZ(x, y, out sbyte groundZ, out sbyte _);

                    if (gobj is Static st && st.ItemData.IsWet)
                    {
                        groundZ = gobj.Z;
                    }

                    x = (ushort)(x - _world.TargetManager.MultiTargetInfo.XOff);
                    y = (ushort)(y - _world.TargetManager.MultiTargetInfo.YOff);
                    z = (sbyte)(groundZ - _world.TargetManager.MultiTargetInfo.ZOff);

                    _multi.SetInWorldTile(x, y, z);
                    _multi.CheckGraphicChange();

                    _world.HouseManager.TryGetHouse(_multi.Serial, out House house);

                    foreach (Multi s in house.Components)
                    {
                        s.IsHousePreview = true;
                        s.SetInWorldTile(
                            (ushort)(_multi.X + s.MultiOffsetX),
                            (ushort)(_multi.Y + s.MultiOffsetY),
                            (sbyte)(_multi.Z + s.MultiOffsetZ)
                        );
                    }
                }
            }
            else if (_multi != null)
            {
                _world.HouseManager.RemoveMultiTargetHouse();
                _multi.Destroy();
                _multi = null;
            }

            if (_isMouseLeftDown && !Client.Game.UO.GameCursor.ItemHold.Enabled)
                UpdateWhenLeftClickHeld();

            Profiler.ExitContext("GameSceneUpdate");
        }

        /// <summary>
        /// Handles updates performed when the left mouse button is held.
        /// This method manages interactions with game objects while the left mouse button is being held down,
        /// including custom house modifications and item pickup behaviors.
        /// </summary>
        /// <remarks>
        /// - In the context of custom house building, it processes object selection and placement in real-time.
        /// - Prevents repeated interactions on the same object or position to avoid duplicate actions.
        /// - Implements a delay mechanism for placing objects in house customization for better user experience.
        /// - Handles item pickup if an item is selected under specific conditions.
        /// </remarks>
        private void UpdateWhenLeftClickHeld()
        {
            if (
                _world.CustomHouseManager != null
                && _world.CustomHouseManager.SelectedGraphic != 0
                && !_world.CustomHouseManager.SeekTile
                && !_world.CustomHouseManager.Erasing
                && Time.Ticks > _timeToPlaceMultiInHouseCustomization
            )
            {
                // Null/Type guard
                if (SelectedObject.Object is not GameObject obj)
                    return;

                // Guard against multiple clicks to on the exact same entity
                if (obj.X == _lastSelectedMultiPositionInHouseCustomization.X &&
                    obj.Y == _lastSelectedMultiPositionInHouseCustomization.Y &&
                    obj.Z == _lastSelectedMultiPositionInHouseCustomization.Z
                   )
                    return;

                _world.CustomHouseManager.OnTargetWorld(obj);
                _timeToPlaceMultiInHouseCustomization = Time.Ticks + 50;
                _lastSelectedMultiPositionInHouseCustomization.X = obj.X;
                _lastSelectedMultiPositionInHouseCustomization.Y = obj.Y;
                return;
            }

            if (Time.Ticks - _holdMouse2secOverItemTime < 1000)
                return;

            if (SelectedObject.Object is Item it && GameActions.PickUp(_world, it.Serial, 0, 0))
            {
                _isMouseLeftDown = false;
                _holdMouse2secOverItemTime = 0;
            }
        }

        private float GetActiveScale() => Math.Max(0.0001f, Camera.Zoom);

        public override bool DrawsOwnBackground => true;

        public override bool Draw(UltimaBatcher2D batcher)
        {
            if (!_world.InGame)
            {
                // No world/light targets are rendered on this path, so nothing discards the screen
                // target - draw the background directly (GameController skips it for this scene).
                Client.Game.DrawWindowBackground(batcher);
                return false;
            }

            if (CheckDeathScreen(batcher))
            {
                return true;
            }

            Profiler.EnterContext("GameSceneDraw");

            GraphicsDevice gd = batcher.GraphicsDevice;

            Viewport rViewport = gd.Viewport;
            Viewport cameraViewport = Camera.GetViewport();

            var hue = new Vector3(0, 0, 1);

            Profiler.EnterContext("EnsureRenderTargets");
            EnsureRenderTargets(gd);
            Profiler.ExitContext("EnsureRenderTargets");

            Profiler.EnterContext("DrawWorld");
            bool canDrawLights = DrawWorldRenderTarget(batcher, gd, cameraViewport);
            Profiler.ExitContext("DrawWorld");

            if (canDrawLights)
            {
                Profiler.EnterContext("DrawLights");

                batcher.Begin();

                if (UseAltLights)
                {
                    hue.Z = .5f;
                    batcher.SetBlendState(_altLightsBlend.Value);
                }
                else
                {
                    batcher.SetBlendState(_darknessBlend.Value);
                }

                batcher.Draw(
                    _lightRenderTarget,
                    new Rectangle(0, 0, Camera.Bounds.Width, Camera.Bounds.Height),
                    hue
                );

                batcher.SetBlendState(null);
                batcher.End();

                hue.Z = 1f;

                Profiler.ExitContext("DrawLights");
            }

            Profiler.EnterContext("DrawOverlays");

            // Overheads (names, health bars, overhead text) can optionally be drawn in screen
            // space (identity transform) so they keep a constant on-screen size regardless of the
            // camera zoom. Their world anchors are converted to screen space per-object so they
            // still follow the zoomed world. When scaling is enabled they are drawn under the
            // camera transform exactly as before. This only affects the camera zoom scale, not any
            // other (global) game scaling.
            bool overheadsScaleWithZoom = ProfileManager.CurrentProfile?.OverheadsScaleWithZoom ?? true;

            batcher.Begin(null, overheadsScaleWithZoom ? Camera.ViewTransformMatrix : Matrix.Identity);
            DrawOverheads(batcher);
            batcher.End();

            // The selection rectangle always tracks the world, so keep it under the camera transform.
            batcher.Begin(null, Camera.ViewTransformMatrix);
            DrawSelection(batcher);
            batcher.End();

            gd.Viewport = rViewport;

            gd.Clear(ClearOptions.Stencil, Color.Transparent, 0f, 0);
            Profiler.ExitContext("DrawOverlays");

            // Here rather than at the end of the frame so gumps draw over them: viewport-scoped
            // decorations are meant to colour the world, not the UI sitting on it. Drawn in screen
            // space - the camera viewport is restored above - so Camera.Bounds is the target as-is.
            //
            // The world target is handed over as the scene source for layers that distort the frame.
            // It is already a separate texture, so nothing has to be copied - at the cost of holding
            // the world before lights and overheads were composited over it.
            Profiler.EnterContext("ScreenDecorations");

            var scene = new ScreenOverlaySource(_worldRenderTarget, _worldSrcRect);
            ScreenOverlayManager.DrawViewportOverlays(batcher, Camera.Bounds, scene);

            Profiler.ExitContext("ScreenDecorations");

            Profiler.ExitContext("GameSceneDraw");

            return base.Draw(batcher);
        }

        private bool DrawWorldRenderTarget(UltimaBatcher2D batcher, GraphicsDevice gd, Viewport cameraViewport)
        {
            float scale = GetActiveScale();

            int rtW = _worldRenderTarget?.Width ?? Camera.Bounds.Width;
            int rtH = _worldRenderTarget?.Height ?? Camera.Bounds.Height;
            int vpW = Camera.Bounds.Width;
            int vpH = Camera.Bounds.Height;

            Profiler.EnterContext("EnsureWorldMatrix");
            EnsureWorldMatrix(rtW, rtH, vpW, vpH);
            Profiler.ExitContext("EnsureWorldMatrix");

            Profiler.EnterContext("RenderWorld");
            DrawWorld(batcher, ref _worldRtMatrix);
            Profiler.ExitContext("RenderWorld");

            Profiler.EnterContext("PrepareLights");
            bool canDrawLights = PrepareLightsRendering(batcher, ref _worldRtMatrix);
            Profiler.ExitContext("PrepareLights");

            // The world/light render-target swaps above rebind (and discard) the screen target,
            // wiping any background drawn before the scene. Redraw it now - behind the world
            // composite below and all gumps drawn afterwards.
            Client.Game.DrawWindowBackground(batcher);

            gd.Viewport = cameraViewport;

            Profiler.EnterContext("PostProcess");
            int srcW = (int)Math.Floor(vpW * scale);
            int srcH = (int)Math.Floor(vpH * scale);

            // Viewport-scoped shake moves the crop taken from the render target rather than where
            // that crop is drawn: destRect stays pinned to the viewport, so the shake reveals real
            // pixels EnsureRenderTargets padded the target with instead of exposing empty texture at
            // the edge. Clamped to the crop's own margin - zero when the target isn't padded (shake
            // off), and never past it even at full shake intensity.
            // Margin floored at 0: the target is capped at MAX_TEXTURE_SIZE, the crop isn't, and
            // Math.Clamp throws when its bounds cross.
            Point shake = ScreenOverlayManager.Instance.ViewportShakeOffset();
            int marginX = Math.Max(0, rtW - srcW);
            int marginY = Math.Max(0, rtH - srcH);
            int srcX = Math.Clamp(marginX / 2 + shake.X, 0, marginX);
            int srcY = Math.Clamp(marginY / 2 + shake.Y, 0, marginY);

            var srcRect = new Rectangle(srcX, srcY, srcW, srcH);
            var destRect = new Rectangle(0, 0, vpW, vpH);

            // Kept for the overlay pass at the end of the frame: the crop is what maps the viewport
            // back onto the world target, and the zoom that determines it is recomputed here.
            _worldSrcRect = srcRect;

            UpdatePostProcessState(gd);

            if (_postFx == _xbr && _xbr != null)
            {
                BindXbrParams(gd);
            }
            else if (_postFx == _fsr && _fsr != null)
            {
                BindFsrParams(gd);
            }

            batcher.Begin(_postFx, Matrix.Identity);
            try { batcher.SetSampler(_postSampler ?? SamplerState.PointClamp); } catch { batcher.SetSampler(SamplerState.PointClamp); }
            batcher.Draw(_worldRenderTarget, destRect, srcRect, new Vector3(0, 0, 1));
            batcher.End();
            batcher.SetSampler(null);
            batcher.SetBlendState(null);
            Profiler.ExitContext("PostProcess");

            return canDrawLights;
        }

        private void DrawWorld(UltimaBatcher2D batcher, ref Matrix matrix)
        {
            Profiler.EnterContext("WorldSelection");
            FillGameObjectList();
            Profiler.ExitContext("WorldSelection");

            // Always use render target for consistent scaling
            RenderTargetBinding[] previousRenderTargets = batcher.GraphicsDevice.GetRenderTargets();
            batcher.GraphicsDevice.SetRenderTarget(_worldRenderTarget);
            batcher.GraphicsDevice.Clear(ClearOptions.Target, Color.Black, 1f, 0);

            batcher.SetSampler(SamplerState.PointClamp);

            batcher.Begin(null, matrix);
            batcher.SetBrightlight(ProfileManager.CurrentProfile.TerrainShadowsLevel * 0.1f);
            batcher.SetStencil(DepthStencilState.Default);

            // Map world depth [0, FarPlane] across the entire depth buffer so tile depth values use
            // the full 24-bit precision instead of only a fraction of it. Higher depth = nearer.
            batcher.SetProjectionDepthRange(-GameObjects.RenderDepth.FarPlane, 0f);

            RenderedObjectsCount = 0;
            Profiler.EnterContext("Statics");
            RenderedObjectsCount += DrawRenderList(batcher, _renderListStatics);
            Profiler.ExitContext("Statics");
            Profiler.EnterContext("Animation");
            RenderedObjectsCount += DrawRenderList(batcher, _renderListAnimations);
            Profiler.ExitContext("Animation");
            Profiler.EnterContext("Effects");
            RenderedObjectsCount += DrawRenderList(batcher, _renderListEffects);
            Profiler.ExitContext("Effects");

            if (_renderListTransparentObjects.Count > 0)
            {
                batcher.SetStencil(DepthStencilState.DepthRead);
                RenderedObjectsCount += DrawRenderList(batcher, _renderListTransparentObjects);
            }

            batcher.SetStencil(null);

            if (
                _multi != null
                && _world.TargetManager.IsTargeting
                && _world.TargetManager.TargetingState == CursorTarget.MultiPlacement
            )
            {
                _multi.Draw(
                    batcher,
                    _multi.RealScreenPosition.X,
                    _multi.RealScreenPosition.Y,
                    _multi.CalculateDepthZ()
                );
            }

            DrawDragItemPreview(batcher);

            batcher.SetSampler(null);
            batcher.SetStencil(null);

            // draw weather (DisableWeather also checked inside Weather.Draw)
            if (ProfileManager.CurrentProfile?.DisableWeather != true)
            {
                Profiler.EnterContext("Weather");
                _world.Weather.Draw(batcher, 0, 0, MAX_LAYER_DEPTH - 1);
                Profiler.ExitContext("Weather");
            }

            //GameController.DrawFlushCounts(batcher, 200, 200);

            // Restore the default projection depth range for all other (non-world) batcher passes.
            batcher.ResetProjectionDepthRange();

            batcher.End();

            // Restore previous render target
            if (previousRenderTargets != null && previousRenderTargets.Length > 0)
            {
                batcher.GraphicsDevice.SetRenderTargets(previousRenderTargets);
            }
            else
            {
                batcher.GraphicsDevice.SetRenderTarget(null);
            }
        }

        private int DrawRenderList(UltimaBatcher2D batcher, List<GameObject> renderList)
        {
            int done = 0;

            foreach (GameObject obj in renderList)
            {
                if (obj.Z <= _maxGroundZ)
                {
                    float depth = obj.CalculateDepthZ();
                    if (obj.Draw(batcher, obj.RealScreenPosition.X, obj.RealScreenPosition.Y, depth))
                    {
                        ++done;
                    }
                }
            }

            return done;
        }

        private bool PrepareLightsRendering(UltimaBatcher2D batcher, ref Matrix matrix)
        {
            if (!UseLights && !UseAltLights) return false;
            if (_world.Player.IsDead && ProfileManager.CurrentProfile.EnableBlackWhiteEffect) return false;
            if (_lightRenderTarget == null) return false;

            RenderTargetBinding[] previousRenderTargets = batcher.GraphicsDevice.GetRenderTargets();

            batcher.GraphicsDevice.SetRenderTarget(_lightRenderTarget);
            batcher.GraphicsDevice.Clear(ClearOptions.Target, Color.Black, 0f, 0);

            if (!UseAltLights)
            {
                float lightColor = _world.Light.IsometricLevel;

                if (ProfileManager.CurrentProfile.UseDarkNights)
                {
                    lightColor -= 0.04f;
                }

                batcher.GraphicsDevice.Clear(
                    ClearOptions.Target,
                    new Vector4(lightColor, lightColor, lightColor, 1),
                    0f,
                    0
                );
            }

            batcher.Begin(null, matrix);
            batcher.SetBlendState(BlendState.Additive);

            Vector3 hue = Vector3.Zero;

            hue.Z = 1f;

            bool candleFlicker = ProfileManager.CurrentProfile.CandleFlickerLights;
            // Time in seconds, used as the phase base for the flicker oscillation.
            float flickerTime = Time.Ticks / 1000f;

            for (int i = 0; i < _lightCount; i++)
            {
                ref LightData l = ref _lights[i];
                ref readonly SpriteInfo lightInfo = ref Client.Game.UO.Lights.GetLight(l.Id);

                if (lightInfo.Texture == null)
                {
                    continue;
                }

                // Gently modulate each light's intensity so it ebbs and flows like a
                // candle. A per-light phase seed derived from its position keeps nearby
                // lights out of sync, and blending two frequencies avoids an obvious
                // pulse. The amplitude is intentionally small for a subtle effect.
                if (candleFlicker)
                {
                    // Seed the phase from the light's world position so it stays constant
                    // while the camera scrolls, keeping the flicker at a steady speed.
                    float seed = l.WorldX * 0.73f + l.WorldY * 1.31f + l.WorldZ * 0.57f;

                    hue.Z = 1f
                        + 0.06f * (float)Math.Sin(flickerTime * 3.1f + seed)
                        + 0.03f * (float)Math.Sin(flickerTime * 7.7f + seed * 1.7f);
                }
                else
                {
                    hue.Z = 1f;
                }

                hue.X = l.Color;
                hue.Y =
                    hue.X > 1.0f
                        ? l.IsHue
                            ? ShaderHueTranslator.SHADER_HUED
                            : ShaderHueTranslator.SHADER_LIGHTS
                        : ShaderHueTranslator.SHADER_NONE;

                batcher.Draw(
                    lightInfo.Texture,
                    new Vector2(
                        l.DrawX - lightInfo.UV.Width * 0.5f,
                        l.DrawY - lightInfo.UV.Height * 0.5f
                    ),
                    lightInfo.UV,
                    hue
                );
            }

            _lightCount = 0;

            batcher.SetBlendState(null);
            batcher.End();

            // Restore previous render target
            if (previousRenderTargets != null && previousRenderTargets.Length > 0)
            {
                batcher.GraphicsDevice.SetRenderTargets(previousRenderTargets);
            }
            else
            {
                batcher.GraphicsDevice.SetRenderTarget(null);
            }
            return true;
        }

        public void DrawOverheads(UltimaBatcher2D batcher)
        {
            _healthLinesManager.Draw(batcher);

            if (!UIManager.IsMouseOverWorld)
            {
                SelectedObject.Object = null;
            }

            _world.WorldTextManager.ProcessWorldText(true);
            // Always drawing to render target, use 0,0 offset since render target has no offset
            _world.WorldTextManager.Draw(batcher, 0, 0);
        }

        public void DrawSelection(UltimaBatcher2D batcher)
        {
            if (!_isSelectionActive) return;

            var selectionHue = new Vector3 { Z = 0.7f };

            // Convert to viewport-relative then to game space so the rectangle
            // renders at the correct position after Camera.ViewTransformMatrix is applied.
            Point selStart = Camera.ScreenToWorld(new Point(
                Math.Min(_selectionStart.X, Mouse.Position.X) - Camera.Bounds.X,
                Math.Min(_selectionStart.Y, Mouse.Position.Y) - Camera.Bounds.Y
            ));
            Point selEnd = Camera.ScreenToWorld(new Point(
                Math.Max(_selectionStart.X, Mouse.Position.X) - Camera.Bounds.X,
                Math.Max(_selectionStart.Y, Mouse.Position.Y) - Camera.Bounds.Y
            ));

            var selectionRect = new Rectangle(
                selStart.X,
                selStart.Y,
                selEnd.X - selStart.X,
                selEnd.Y - selStart.Y
            );

            batcher.Draw(
                SolidColorTextureCache.GetTexture(Color.Black),
                selectionRect,
                selectionHue
            );

            selectionHue.Z = 0.3f;

            batcher.DrawRectangle(
                SolidColorTextureCache.GetTexture(Color.DeepSkyBlue),
                selectionRect.X,
                selectionRect.Y,
                selectionRect.Width,
                selectionRect.Height,
                selectionHue
            );
        }

        private void EnsureRenderTargets(GraphicsDevice gd)
        {
            // Cache Camera.Bounds to avoid repeated property access
            Rectangle cameraBounds = Camera.GetViewport().Bounds;
            int vw = Math.Max(1, cameraBounds.Width);
            int vh = Math.Max(1, cameraBounds.Height);

            // Cache PresentationParameters to avoid struct copying
            PresentationParameters pp = gd.PresentationParameters;
            float scale = GetActiveScale();

            // Extra canvas viewport-scope shake can crop into instead of exposing the target's
            // unrendered edge. Cached on ScreenOverlayManager, refreshed only when the shake settings
            // change - this runs every frame, and re-deriving it from settings here would mean a
            // dereference chain per frame for a value that almost never moves.
            int shakeMargin = ScreenOverlayManager.Instance.ViewportShakeMarginPixels;

            int rtWidth = Math.Min((int)Math.Floor(vw * scale) + shakeMargin, MAX_TEXTURE_SIZE);
            int rtHeight = Math.Min((int)Math.Floor(vh * scale) + shakeMargin, MAX_TEXTURE_SIZE);

            // Create/recreate world render target if needed
            if (_worldRenderTarget == null
                || _worldRenderTarget.IsDisposed
                || _worldRenderTarget.Width != rtWidth
                || _worldRenderTarget.Height != rtHeight)
            {
                _worldRenderTarget?.Dispose();
                _worldRenderTarget = new RenderTarget2D(
                    gd, rtWidth, rtHeight, false,
                    pp.BackBufferFormat, pp.DepthStencilFormat, pp.MultiSampleCount, pp.RenderTargetUsage);
            }

            // Light render target matches world render target dimensions
            if (_lightRenderTarget == null
                || _lightRenderTarget.IsDisposed
                || _lightRenderTarget.Width != rtWidth
                || _lightRenderTarget.Height != rtHeight)
            {
                _lightRenderTarget?.Dispose();
                _lightRenderTarget = new RenderTarget2D(
                    gd, rtWidth, rtHeight, false,
                    pp.BackBufferFormat, pp.DepthStencilFormat, pp.MultiSampleCount, pp.RenderTargetUsage);
            }
        }

        private void EnsureWorldMatrix(int rtW, int rtH, int vpW, int vpH)
        {
            var vpCenter = new Vector2(vpW * 0.5f, vpH * 0.5f);
            Vector2 camOffset = Camera.Offset;
            var rtCenter = new Vector2(rtW * 0.5f, rtH * 0.5f);

            Matrix.CreateTranslation(-vpCenter.X, -vpCenter.Y, 0f, out Matrix matTrans1);
            Matrix.CreateTranslation(-camOffset.X, -camOffset.Y, 0f, out Matrix matTrans2);
            Matrix.CreateTranslation(rtCenter.X, rtCenter.Y, 0f, out Matrix matTrans3);
            Matrix.Multiply(ref matTrans1, ref matTrans2, out Matrix temp1);
            Matrix.Multiply(ref temp1, ref matTrans3, out _worldRtMatrix);
        }

        private void UpdatePostProcessState(GraphicsDevice gd)
        {
            if (_currentFilter == _filterMode)
            {
                switch (_filterMode)
                {
                    case PostProcessingType.Xbr:
                        if (ReferenceEquals(_postFx, _xbr) && _xbr != null) return;
                        break;
                    case PostProcessingType.Fsr:
                        if (ReferenceEquals(_postFx, _fsr) && _fsr != null) return;
                        break;
                    default:
                        if (_postFx == null) return;
                        break;
                }
            }

            _currentFilter = _filterMode;

            switch (_filterMode)
            {
                case PostProcessingType.Xbr:
                    if (_xbr == null)
                    {
                        _xbr = new XBREffect(gd);
                        EffectTechnique tech = _xbr.Techniques?["T0"] ??
                                   (_xbr.Techniques?.Count > 0 ? _xbr.Techniques[0] : null);
                        if (tech != null) _xbr.CurrentTechnique = tech;
                        else { _xbr = null; _postFx = null; _postSampler = SamplerState.PointClamp; break; }
                    }
                    _postFx = _xbr;
                    _postSampler = SamplerState.PointClamp;
                    break;

                case PostProcessingType.Fsr:
                    if (_fsr == null)
                    {
                        _fsr = new FSREffect(gd);
                        EffectTechnique tech = _fsr.Techniques?["T0"] ??
                                   (_fsr.Techniques?.Count > 0 ? _fsr.Techniques[0] : null);
                        if (tech != null) _fsr.CurrentTechnique = tech;
                        else { _fsr = null; _postFx = null; _postSampler = SamplerState.PointClamp; break; }
                    }
                    _postFx = _fsr;
                    _postSampler = SamplerState.PointClamp;
                    break;

                case PostProcessingType.Anisotropic:
                    _postFx = null;
                    _postSampler = SamplerState.AnisotropicClamp;
                    break;

                case PostProcessingType.Linear:
                    _postFx = null;
                    _postSampler = SamplerState.LinearClamp;
                    break;

                default:
                    _postFx = null;
                    _postSampler = SamplerState.PointClamp;
                    break;
            }
        }

        private void BindXbrParams(GraphicsDevice gd)
        {
            if (_xbr == null || _worldRenderTarget == null) return;

            try
            {
                if (_xbr.Techniques?["T0"] != null)
                    _xbr.CurrentTechnique = _xbr.Techniques["T0"];
            }
            catch (Exception e)
            {
                Log.ErrorDebug(e.ToString());
            }

            float w = _worldRenderTarget.Width;
            float h = _worldRenderTarget.Height;

            Viewport vp = gd.Viewport;
            var ortho = Matrix.CreateOrthographicOffCenter(0, vp.Width, vp.Height, 0, 0, 1);
            _xbr.MatrixTransform?.SetValue(ortho);
            _xbr.TextureSize?.SetValue(new Vector2(w, h));
            _xbr.Parameters?["invTextureSize"]?.SetValue(new Vector2(1f / w, 1f / h));
            _xbr.Parameters?["TextureSizeInv"]?.SetValue(new Vector2(1f / w, 1f / h));
            _xbr.Parameters?["decal"]?.SetValue(_worldRenderTarget);
        }

        private void BindFsrParams(GraphicsDevice gd)
        {
            if (_fsr == null || _worldRenderTarget == null) return;

            try
            {
                if (_fsr.Techniques?["T0"] != null)
                    _fsr.CurrentTechnique = _fsr.Techniques["T0"];
            }
            catch (Exception e)
            {
                Log.ErrorDebug(e.ToString());
            }

            float w = _worldRenderTarget.Width;
            float h = _worldRenderTarget.Height;

            Viewport vp = gd.Viewport;
            var ortho = Matrix.CreateOrthographicOffCenter(0, vp.Width, vp.Height, 0, 0, 1);
            _fsr.MatrixTransform?.SetValue(ortho);
            _fsr.TextureSize?.SetValue(new Vector2(w, h));
            _fsr.Parameters?["decal"]?.SetValue(_worldRenderTarget);
        }

        private static readonly RenderedText _youAreDeadText = RenderedText.Create(
            TazLang.Get("you_are_dead"),
            0xFFFF,
            3,
            false,
            FontStyle.BlackBorder
        );

        private bool CheckDeathScreen(UltimaBatcher2D batcher)
        {
            if (ProfileManager.CurrentProfile == null || !ProfileManager.CurrentProfile.EnableDeathScreen)
            {
                return false;
            }

            if (!_world.Player.IsDead || _world.Player.DeathScreenTimer <= Time.Ticks)
            {
                return false;
            }

            // Death-splash path returns early without rendering the world, so nothing discards the
            // screen target here - draw the background so the splash sits on it like every other path.
            Client.Game.DrawWindowBackground(batcher);

            batcher.Begin();
            _youAreDeadText.Draw(
                batcher,
                Camera.Bounds.X + (Camera.Bounds.Width / 2 - _youAreDeadText.Width / 2),
                Camera.Bounds.Bottom / 2
            );
            batcher.End();

            return true;

        }

        private void StopFollowing()
        {
            if (ProfileManager.CurrentProfile.FollowingMode)
            {
                ProfileManager.CurrentProfile.FollowingMode = false;
                ProfileManager.CurrentProfile.FollowingTarget = 0;
                _world.Player.Pathfinder.StopAutoWalk();

                _world.MessageManager.HandleMessage(
                    _world.Player,
                    TazLang.Get("stopped_following"),
                    string.Empty,
                    0,
                    MessageType.Regular,
                    3,
                    TextType.CLIENT
                );
            }
        }

        private struct LightData
        {
            public byte Id;
            public ushort Color;
            public bool IsHue;
            public int DrawX,
                DrawY;
            public ushort WorldX,
                WorldY;
            public sbyte WorldZ;
        }

        private struct LightOccluder
        {
            public int X;
            public int Z;
        }
    }

    public enum PostProcessingType
    {
        Point,
        Linear,
        Anisotropic,
        Xbr,
        Fsr,
        Invalid
    }
}
