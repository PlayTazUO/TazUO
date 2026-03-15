// SPDX-License-Identifier: BSD-2-Clause


using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.Network;
using ClassicUO.Renderer;
using ClassicUO.Resources;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SDL3;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using ClassicUO.Game.Map;
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
        private bool _alphaChanged;
        private long _alphaTimer;
        private bool _forceStopScene;
        private HealthLinesManager _healthLinesManager;

        private Point _lastSelectedMultiPositionInHouseCustomization;
        private int _lightCount;
        private readonly LightData[] _lights = new LightData[
            LightsLoader.MAX_LIGHTS_DATA_INDEX_COUNT
        ];
        private Item _multi;
        private Rectangle _rectangleObj = Rectangle.Empty,
            _rectanglePlayer;
        private long _timePing;

        private uint _timeToPlaceMultiInHouseCustomization;
        private const int MAX_TEXTURE_SIZE = 8192;
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
            else if (Settings.GlobalSettings.IsWindowMaximized)
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

        public override void Load()
        {
            base.Load();
            GridContainerSaveData.Instance.Load();

            Client.Game.UO.GameCursor.ItemHold.Clear();

            NameOverHeadManager.Load();

            _world.Macros.Clear();
            _world.Macros.Load();
            _animatedStaticsManager = new AnimatedStaticsManager();
            _animatedStaticsManager.Initialize();
            _world.InfoBars.Load();
            _healthLinesManager = new HealthLinesManager(_world);

            _world.CommandManager.Initialize();
            WalkableManager.Instance.Initialize();
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
            SpellBarManager.Load();
            if(ProfileManager.CurrentProfile.EnableCaveBorder)
                StaticFilters.ApplyCaveTileBorder();

            if(!ProfileManager.CurrentProfile.DisableConnectToIrcOnLogin)
                TazUOChatManager.Instance.Init();

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
                    {
                        if (ProfileManager.CurrentProfile.HideJournalSystemPrefix)
                        {
                            name = null;
                        }
                        else
                        {
                            name = ResGeneral.System;
                        }
                    }
                    else
                    {
                        name = e.Name;
                    }

                    text = e.Text;

                    break;

                case MessageType.System:
                    if (string.IsNullOrEmpty(e.Name) || string.Equals(e.Name, "system", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (ProfileManager.CurrentProfile.HideJournalSystemPrefix)
                        {
                            name = null;
                        }
                        else
                        {
                            name = ResGeneral.System;
                        }
                    }
                    else
                    {
                        name = e.Name;
                    }

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
                        name = ResGeneral.YouSee;
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
                    name = string.Format(ResGeneral.Party0, e.Name);
                    hue = ProfileManager.CurrentProfile.PartyMessageHue;

                    break;

                case MessageType.Alliance:
                    text = e.Text;
                    name = string.Format(ResGeneral.Alliance0, e.Name);
                    hue = ProfileManager.CurrentProfile.AllyMessageHue;

                    break;

                case MessageType.Guild:
                    text = e.Text;
                    name = string.Format(ResGeneral.Guild0, e.Name);
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
                _world.Journal.Add(text, hue, name, e.TextType, e.IsUnicode, e.Type);
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

            TazUOChatManager.Instance.Dispose();

            LongDistancePathfinder.Dispose();
            WalkableManager.Instance.Shutdown();

            GridContainerSaveData.Instance.Save();
            GridContainerSaveData.Reset();
            JournalFilterManager.Instance.Save();

            SpellBarManager.Unload();
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
            FriendsListManager.Instance.OnSceneUnload();

            NameOverHeadManager.Save();

            _world.Macros.Save();
            _world.Macros.Clear();
            _world.InfoBars.Save();
            ProfileManager.UnLoadProfile();

            StaticFilters.CleanTreeTextures();

            AsyncNetClient.Socket.Disconnected -= SocketOnDisconnected;
            _ = AsyncNetClient.Socket.Disconnect();
            _lightRenderTarget?.Dispose();
            _worldRenderTarget?.Dispose();
            _xbr?.Dispose();
            _xbr = null;

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

            base.Unload();
        }

        private void SocketOnDisconnected(object sender, SocketError e)
        {
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
                            ResGeneral.ConnectionLost0,
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
        }

        public void RequestQuitGame() => UIManager.Add(
                new QuestionGump(
                    _world,
                    ResGeneral.QuitPrompt,
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

            int testX = obj.X + 1;
            int testY = obj.Y + 1;

            GameObject tile = _world.Map.GetTile(testX, testY);

            if (tile != null)
            {
                sbyte z5 = (sbyte)(obj.Z + 5);

                for (GameObject o = tile; o != null; o = o.TNext)
                {
                    if (
                        (!(o is Static s) || s.ItemData.IsTransparent)
                            && (!(o is Multi m) || m.ItemData.IsTransparent)
                        || !o.AllowedToDraw
                    )
                    {
                        continue;
                    }

                    if (o.Z < _maxZ && o.Z >= z5)
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

            Profiler.EnterContext("MapChunkLoop");
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

            Profiler.ExitContext("MapChunkLoop");


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
            Profile currentProfile = ProfileManager.CurrentProfile;

            SelectedObject.TranslatedMousePositionByViewport = Camera.MouseToWorldPosition();

            base.Update();

            // Check if we're waiting for window resize to complete
            if (_waitingForWindowResize)
            {
                if (_expectedWindowSize.HasValue)
                {
                    // We're waiting for a specific window size
                    Point expected = _expectedWindowSize.Value;
                    int actualWidth = Client.Game.Window.ClientBounds.Width;
                    int actualHeight = Client.Game.Window.ClientBounds.Height;

                    bool widthMatch = Math.Abs(actualWidth - expected.X) <= TOLERANCE;
                    bool heightMatch = Math.Abs(actualHeight - expected.Y) <= TOLERANCE;

                    if (widthMatch && heightMatch)
                    {
                        // Resize succeeded
                        _waitingForWindowResize = false;
                        _expectedWindowSize = null;
                    }
                    else if (Time.Ticks - _windowResizeStartTime > WINDOW_RESIZE_TIMEOUT_MS)
                    {
                        // Timeout reached, retry resize once
                        Log.Trace($"Window resize timeout, retrying. Expected: {expected.X}x{expected.Y}, Actual: {actualWidth}x{actualHeight}");
                        Client.Game.SetWindowSize(expected.X, expected.Y);

                        // Reset timer for one more attempt
                        _windowResizeStartTime = Time.Ticks;
                        _waitingForWindowResize = false;
                        _expectedWindowSize = null;
                    }
                }
                else
                {
                    // We're waiting for maximize operation to complete
                    if (Client.Game.IsWindowMaximized())
                    {
                        // Maximize succeeded
                        _waitingForWindowResize = false;
                    }
                    else if (Time.Ticks - _windowResizeStartTime > WINDOW_RESIZE_TIMEOUT_MS)
                    {
                        // Timeout reached, retry maximize once
                        Log.Trace("Window maximize timeout, retrying.");
                        Client.Game.MaximizeWindow();

                        // Reset timer for one more attempt
                        _windowResizeStartTime = Time.Ticks;
                        _waitingForWindowResize = false;
                    }
                }
            }

            // Temporary to see if memory usage get's too high or not. This will keep map chunks loaded
            // for better performance at the cost of more ram.
            // if (_time_cleanup < Time.Ticks)
            // {
            //     _world.Map?.ClearUnusedBlocks();
            //     _time_cleanup = Time.Ticks + 500;
            // }

            // Update WalkableManager for chunk generation
            WalkableManager.Instance.Update();

            // Update LongDistancePathfinder
            LongDistancePathfinder.Update();

            SharedStore.SendMegaCliLocRequests(_world);

            if (_forceStopScene)
            {
                var loginScene = new LoginScene(_world);
                Client.Game.SetScene(loginScene);
                loginScene.Reconnect = true;

                return;
            }

            if (!_world.InGame)
            {
                return;
            }

            if (Time.Ticks > _timePing)
            {
                AsyncNetClient.Socket.Statistics.SendPing();
                _timePing = (long)Time.Ticks + 1000;
            }

            if (currentProfile.ForceResyncOnHang && Time.Ticks - AsyncNetClient.Socket.Statistics.LastPingReceived > 5000 && Time.Ticks - _lastResync > 5000)
            {
                //Last ping > ~5 seconds
                AsyncNetClient.Socket.Send_Resync();
                _lastResync = Time.Ticks;
                GameActions.Print(_world, "Possible connection hang, resync attempted", 32, MessageType.System);
            }

            _world.Update();
            _animatedStaticsManager.Process();
            _world.BoatMovingManager.Update();
            _world.Player.Pathfinder.ProcessAutoWalk();
            _world.DelayedObjectClickManager.Update();


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
            GridHighlightData.ProcessQueue(_world);

            if (!MoveCharacterByMouseInput() && !currentProfile.DisableArrowBtn && !MoveCharByController())
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
                            StopFollowing(); //Can't get there
                        }
                    }
                }
                else
                {
                    StopFollowing();
                }
            }

            _world.Macros.Update();

            if (Time.Ticks > _nextProfileSave)
            {
                ProfileManager.CurrentProfile.Save(_world, ProfileManager.ProfilePath);
                _nextProfileSave = Time.Ticks + 1000*60*60; //1 Hour
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
            {
                if (
                    _world.CustomHouseManager != null
                    && _world.CustomHouseManager.SelectedGraphic != 0
                    && !_world.CustomHouseManager.SeekTile
                    && !_world.CustomHouseManager.Erasing
                    && Time.Ticks > _timeToPlaceMultiInHouseCustomization
                )
                {
                    if (
                        SelectedObject.Object is GameObject obj
                        && (
                            obj.X != _lastSelectedMultiPositionInHouseCustomization.X
                            || obj.Y != _lastSelectedMultiPositionInHouseCustomization.Y
                        )
                    )
                    {
                        _world.CustomHouseManager.OnTargetWorld(obj);
                        _timeToPlaceMultiInHouseCustomization = Time.Ticks + 50;
                        _lastSelectedMultiPositionInHouseCustomization.X = obj.X;
                        _lastSelectedMultiPositionInHouseCustomization.Y = obj.Y;
                    }
                }
                else if (Time.Ticks - _holdMouse2secOverItemTime >= 1000)
                {
                    if (SelectedObject.Object is Item it && GameActions.PickUp(_world, it.Serial, 0, 0))
                    {
                        _isMouseLeftDown = false;
                        _holdMouse2secOverItemTime = 0;
                    }
                }
            }
        }

        private float GetActiveScale() => Math.Max(0.0001f, Camera.Zoom);

        public override bool Draw(UltimaBatcher2D batcher)
        {
            if (!_world.InGame) return false;

            if (CheckDeathScreen(batcher)) return true;

            GraphicsDevice gd = batcher.GraphicsDevice;

            Viewport rViewport = gd.Viewport;
            Viewport cameraViewport = Camera.GetViewport();

            var hue = new Vector3(0, 0, 1);

            EnsureRenderTargets(gd);

            // Always use render target for consistent scaling
            Profiler.EnterContext("DrawWorldRenderTarget");
            bool canDrawLights = DrawWorldRenderTarget(batcher, gd, cameraViewport);
            Profiler.ExitContext("DrawWorldRenderTarget");

            // draw lights
            if (canDrawLights)
            {
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
            }

            // Draw overheads and selection after lighting, on the backbuffer
            batcher.Begin(null, Camera.ViewTransformMatrix);
            DrawOverheads(batcher);
            DrawSelection(batcher);
            batcher.End();

            gd.Viewport = rViewport;

            // Always clear stencil buffer to prevent dirty state affecting UI rendering
            gd.Clear(ClearOptions.Stencil, Color.Transparent, 0f, 0);

            return base.Draw(batcher);
        }

        private bool DrawWorldRenderTarget(UltimaBatcher2D batcher, GraphicsDevice gd, Viewport cameraViewport)
        {
            float scale = GetActiveScale();

            int rtW = _worldRenderTarget?.Width ?? Camera.Bounds.Width;
            int rtH = _worldRenderTarget?.Height ?? Camera.Bounds.Height;
            int vpW = Camera.Bounds.Width;
            int vpH = Camera.Bounds.Height;

            EnsureWorldMatrix(rtW, rtH, vpW, vpH);

            DrawWorld(batcher, ref _worldRtMatrix);

            bool canDrawLights = PrepareLightsRendering(batcher, ref _worldRtMatrix);
            gd.Viewport = cameraViewport;

            int srcW = (int)Math.Floor(vpW * scale);
            int srcH = (int)Math.Floor(vpH * scale);
            int srcX = (rtW - srcW) / 2;
            int srcY = (rtH - srcH) / 2;
            var srcRect = new Rectangle(srcX, srcY, srcW, srcH);
            var destRect = new Rectangle(0, 0, vpW, vpH);

            UpdatePostProcessState(gd);

            if (_postFx == _xbr && _xbr != null)
            {
                BindXbrParams(gd);
            }
            batcher.Begin(_postFx, Matrix.Identity);
            try { batcher.SetSampler(_postSampler ?? SamplerState.PointClamp); } catch { batcher.SetSampler(SamplerState.PointClamp); }
            batcher.Draw(_worldRenderTarget, destRect, srcRect, new Vector3(0, 0, 1));
            batcher.End();
            batcher.SetSampler(null);
            batcher.SetBlendState(null);

            return canDrawLights;
        }

        private void DrawWorld(UltimaBatcher2D batcher, ref Matrix matrix)
        {
            SelectedObject.Object = null;
            Profiler.EnterContext("FillObjectList");
            FillGameObjectList();
            Profiler.ExitContext("FillObjectList");

            // Always use render target for consistent scaling
            RenderTargetBinding[] previousRenderTargets = batcher.GraphicsDevice.GetRenderTargets();
            batcher.GraphicsDevice.SetRenderTarget(_worldRenderTarget);
            batcher.GraphicsDevice.Clear(ClearOptions.Target, Color.Black, 1f, 0);

            batcher.SetSampler(SamplerState.PointClamp);

            batcher.Begin(null, matrix);
            batcher.SetBrightlight(ProfileManager.CurrentProfile.TerrainShadowsLevel * 0.1f);
            batcher.SetStencil(DepthStencilState.Default);

            Profiler.EnterContext("DrawObjects");
            RenderedObjectsCount = 0;
            Profiler.EnterContext("Statics");
            RenderedObjectsCount += DrawRenderList(
                batcher,
                _renderListStatics
            );
            Profiler.ExitContext("Statics");
            Profiler.EnterContext("Animations");
            RenderedObjectsCount += DrawRenderList(
                batcher,
                _renderListAnimations
            );
            Profiler.ExitContext("Animations");
            Profiler.EnterContext("Effects");
            RenderedObjectsCount += DrawRenderList(
                batcher,
                _renderListEffects
            );
            Profiler.ExitContext("Effects");

            if (_renderListTransparentObjects.Count > 0)
            {
                Profiler.EnterContext("Transparency");
                batcher.SetStencil(DepthStencilState.DepthRead);
                RenderedObjectsCount += DrawRenderList(
                    batcher,
                    _renderListTransparentObjects
                );
                Profiler.ExitContext("Transparency");
            }
            Profiler.ExitContext("DrawObjects");

            batcher.SetStencil(null);

            if (
                _multi != null
                && _world.TargetManager.IsTargeting
                && _world.TargetManager.TargetingState == CursorTarget.MultiPlacement
            )
            {
                Profiler.EnterContext("DrawMulti");
                _multi.Draw(
                    batcher,
                    _multi.RealScreenPosition.X,
                    _multi.RealScreenPosition.Y,
                    _multi.CalculateDepthZ()
                );
                Profiler.ExitContext("DrawMulti");
            }

            batcher.SetSampler(null);
            batcher.SetStencil(null);

            // draw weather
            if (!ProfileManager.CurrentProfile.DisableWeather)
            {
                _world.Weather.Draw(batcher, 0, 0); // TODO: fix the depth
            }

            //GameController.DrawFlushCounts(batcher, 200, 200);

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
                    Profiler.EnterContext("Calculate depth");
                    float depth = obj.CalculateDepthZ();
                    Profiler.ExitContext("Calculate depth");

                    Profiler.EnterContext("Draw");
                    if (
                        obj.Draw(batcher, obj.RealScreenPosition.X, obj.RealScreenPosition.Y, depth)
                    )
                    {
                        ++done;
                    }
                    Profiler.ExitContext("Draw");
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

            for (int i = 0; i < _lightCount; i++)
            {
                ref LightData l = ref _lights[i];
                ref readonly SpriteInfo lightInfo = ref Client.Game.UO.Lights.GetLight(l.Id);

                if (lightInfo.Texture == null)
                {
                    continue;
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

            var selStart = new Point(
                Math.Min(_selectionStart.X, Mouse.Position.X),
                Math.Min(_selectionStart.Y, Mouse.Position.Y)
            );
            var selEnd = new Point(
                Math.Max(_selectionStart.X, Mouse.Position.X),
                Math.Max(_selectionStart.Y, Mouse.Position.Y)
            );

            // Convert to viewport-relative coordinates
            selStart.X -= Camera.Bounds.X;
            selStart.Y -= Camera.Bounds.Y;
            selEnd.X -= Camera.Bounds.X;
            selEnd.Y -= Camera.Bounds.Y;

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

            int rtWidth = Math.Min((int)Math.Floor(vw * scale), MAX_TEXTURE_SIZE);
            int rtHeight = Math.Min((int)Math.Floor(vh * scale), MAX_TEXTURE_SIZE);

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
            if (_currentFilter == _filterMode &&
                ((_postFx == null && _filterMode != PostProcessingType.Xbr) || (_postFx != null && (_filterMode != PostProcessingType.Xbr || ReferenceEquals(_postFx, _xbr)))))
                return;

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

        private static readonly RenderedText _youAreDeadText = RenderedText.Create(
            ResGeneral.YouAreDead,
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
                    ResGeneral.StoppedFollowing,
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
        }
    }

    public enum PostProcessingType
    {
        Point,
        Linear,
        Anisotropic,
        Xbr,
        Invalid
    }
}
