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

namespace ClassicUO.Game.Scenes
{
    public partial class GameScene : Scene
    {
        public static GameScene Instance { get; private set; }

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

        private uint _time_cleanup = Time.Ticks + 5000;
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
        private bool _use_render_target = false;
        private int _max_texture_size = 8192;
        private static string _filterMode = "linear"; // "point" | "linear" | "anisotropic" | "xbr"
        private string _currentFilter;
        private Effect _postFx;
        private SamplerState _postSampler = SamplerState.PointClamp;
        private int _rtWCache = -1, _rtHCache = -1;
        private UseItemQueue _useItemQueue;
        private MoveItemQueue _moveItemQueue;
        private bool _useObjectHandles;
        private RenderTarget2D _world_render_target,
            _light_render_target;
        private AnimatedStaticsManager _animatedStaticsManager;

        private readonly World _world;

        public GameScene(World world)
        {
            _world = world;
            _useItemQueue = new UseItemQueue(world);
            _moveItemQueue = new MoveItemQueue(_world);

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
                Client.Game.MaximizeWindow();
            }
            else if (Settings.GlobalSettings.WindowSize.HasValue)
            {
                int w = Settings.GlobalSettings.WindowSize.Value.X;
                int h = Settings.GlobalSettings.WindowSize.Value.Y;

                w = Math.Max(640, w);
                h = Math.Max(480, h);

                Client.Game.SetWindowSize(w, h);
            }

            SetPostProcessingSettings();

            Instance = this;
        }

        public void SetPostProcessingSettings()
        {
            _use_render_target = ProfileManager.CurrentProfile.EnablePostProcessingEffects;
            switch (ProfileManager.CurrentProfile.PostProcessingType)
            {
                case 1:
                    _filterMode = "linear";
                    break;
                case 2:
                    _filterMode = "anisotropic";
                    break;
                case 3:
                    _filterMode = "xbr";
                    break;
                case 0:
                default:
                    _filterMode = "point";
                    break;
            }
            _currentFilter = null;
            _postFx = null;
        }
        private long _nextProfileSave = Time.Ticks + 1000*60*60;

        public MoveItemQueue MoveItemQueue => _moveItemQueue;
        public bool UpdateDrawPosition { get; set; }
        public bool DisconnectionRequested { get; set; }
        public bool UseLights =>
            ProfileManager.CurrentProfile != null
            && ProfileManager.CurrentProfile.UseCustomLightLevel
                ? _world.Light.Personal < _world.Light.Overall
                : _world.Light.RealPersonal < _world.Light.RealOverall;
        public bool UseAltLights =>
            ProfileManager.CurrentProfile != null
            && ProfileManager.CurrentProfile.UseAlternativeLights;

        private bool _followingMode
        {
            get { return ProfileManager.CurrentProfile.FollowingMode; }
            set { ProfileManager.CurrentProfile.FollowingMode = value; }
        }
        private uint _followingTarget
        {
            get { return ProfileManager.CurrentProfile.FollowingTarget; }
            set { ProfileManager.CurrentProfile.FollowingTarget = value; }
        }

        private uint _lastResync = Time.Ticks;

        public GameScene()
        {
        }

        public void DoubleClickDelayed(uint serial) => _useItemQueue.Add(serial);

        public override void Load()
        {
            base.Load();
            Game.UI.ImGuiManager.Initialize(Client.Game);

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

            GridContainerSaveData.Instance.Save();
            GridContainerSaveData.Reset();
            JournalFilterManager.Instance.Save();

            SpellBarManager.Unload();
            _moveItemQueue.Clear();
            GlobalPriorityQueue.Instance.Clear();

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
            catch { }

            EventSink.InvokeOnDisconnected(null);

            _world.TargetManager.Reset();

            // special case for wmap. this allow us to save settings
            UIManager.GetGump<WorldMapGump>()?.SaveSettings();

            ProfileManager.CurrentProfile?.Save(_world, ProfileManager.ProfilePath);
            ImGuiManager.Dispose();
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
            AsyncNetClient.Socket.Disconnect();
            _light_render_target?.Dispose();
            _world_render_target?.Dispose();
            _xbr?.Dispose();
            _xbr = null;

            _world.CommandManager.UnRegisterAll();
            _world.Weather.Reset();
            SkillProgressBar.QueManager.Reset();
            UIManager.Clear();
            _world.Clear();
            _world.ChatManager.Clear();
            _world.DelayedObjectClickManager.Clear();

            _useItemQueue?.Clear();
            GlobalPriorityQueue.Instance.Clear();
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
                    light.ID = 2;
                }
                else
                {
                    if (obj == lightObject && obj is Item item)
                    {
                        light.ID = item.LightID;
                    }
                    else if (lightObject is Item it)
                    {
                        light.ID = (byte)it.ItemData.LightIndex;

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
                        light.ID = 1;
                    }
                    else
                    {
                        ref StaticTiles data = ref Client.Game.UO.FileManager.TileData.StaticData[obj.Graphic];
                        light.ID = data.Layer;
                    }
                }

                light.Color = 0;
                light.IsHue = false;

                if (ProfileManager.CurrentProfile.UseColoredLights)
                {
                    if (light.ID > 200)
                    {
                        light.Color = (ushort)(light.ID - 200);
                        light.ID = 1;
                    }

                    if (LightColors.GetHue(graphic, out ushort color, out bool ishue))
                    {
                        light.Color = color;
                        light.IsHue = ishue;
                    }
                }

                if (light.ID >= LightsLoader.MAX_LIGHTS_DATA_INDEX_COUNT)
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
            bool use_handles = _useObjectHandles;
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

                    Chunk chunk = ASyncMapLoading ? map.PreloadChunk2(chunkX, chunkY) : map.GetChunk2(chunkX, chunkY, true);

                    if (chunk?.IsDestroyed != false)
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

                        AddTileToRenderList(firstObj, use_handles, 150, maxCotZ, ref playerPos);
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

            if (_time_cleanup < Time.Ticks)
            {
                _world.Map?.ClearUnusedBlocks();
                _time_cleanup = Time.Ticks + 500;
            }

            PacketHandlers.SendMegaClilocRequests(_world);

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
                _useItemQueue.ClearCorpses();
            }

            // Process priority queue first (for bandages and other high-priority actions)
            GlobalPriorityQueue.Instance.Update();

            _useItemQueue.Update();

            AutoLootManager.Instance.Update();
            _moveItemQueue.ProcessQueue();
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
                        z = o.Z;
                    }
                    else
                    {
                        x = gobj.X;
                        y = gobj.Y;
                        z = gobj.Z;
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

        private float GetActiveScale()
        {
            float factor = ProfileManager.CurrentProfile.GlobalScaling
                ? ProfileManager.CurrentProfile.GlobalScale
                : Camera.Zoom;

            return Math.Max(0.0001f, factor);
        }

        public override bool Draw(UltimaBatcher2D batcher)
        {
            if (!_world.InGame)
            {
                return false;
            }

            if (CheckDeathScreen(batcher))
            {
                return true;
            }

            Profile profile = ProfileManager.CurrentProfile;
            GraphicsDevice gd = batcher.GraphicsDevice;

            Viewport r_viewport = gd.Viewport;
            Viewport camera_viewport = Camera.GetViewport();

            bool can_draw_lights = false;
            var hue = new Vector3(0, 0, 1);

            EnsureRenderTargets(gd);

            if (_use_render_target)
            {
                Profiler.EnterContext("DrawWorldRenderTarget");
                can_draw_lights = DrawWorldRenderTarget(batcher, gd, camera_viewport);
                Profiler.ExitContext("DrawWorldRenderTarget");
            }
            else
            {
                Profiler.EnterContext("DrawWorldDirect");
                can_draw_lights = DrawWorldDirect(batcher, gd, camera_viewport);
                Profiler.ExitContext("DrawWorldDirect");
            }

            // draw lights
            if (can_draw_lights)
            {
                if (profile.GlobalScaling)
                    batcher.Begin(null, Matrix.CreateScale(profile.GlobalScale));
                else
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
                    _light_render_target,
                    new Rectangle(0, 0, Camera.Bounds.Width, Camera.Bounds.Height),
                    hue
                );

                batcher.SetBlendState(null);
                batcher.End();

                hue.Z = 1f;
            }

            if (profile.GlobalScaling)
                batcher.Begin(null, Matrix.CreateScale(profile.GlobalScale));
            else
                batcher.Begin();

            DrawOverheads(batcher);
            DrawSelection(batcher);

            batcher.End();

            gd.Viewport = r_viewport;

            if (can_draw_lights || _use_render_target)
            {
                gd.Clear(ClearOptions.Stencil, Color.Transparent, 0f, 0);
            }
            return base.Draw(batcher);
        }

        private bool DrawWorldDirect(UltimaBatcher2D batcher, GraphicsDevice gd, Viewport camera_viewport)
        {
            Profile profile = ProfileManager.CurrentProfile;
            Matrix  matrix = Camera.ViewTransformMatrix;

            if (profile.GlobalScaling)
            {
                Camera.Zoom = 1f; // oScale + profile.GlobalScale;
                float scale = profile.GlobalScale;
                matrix = Matrix.CreateScale(scale);
                camera_viewport.Bounds = new Rectangle(
                    (int)(camera_viewport.Bounds.X * scale),
                    (int)(camera_viewport.Bounds.Y * scale),
                    (int)(camera_viewport.Bounds.Width * scale),
                    (int)(camera_viewport.Bounds.Height * scale)
                );
            }

            bool can_draw_lights = PrepareLightsRendering(batcher, ref matrix);
            gd.Viewport = camera_viewport;

            DrawWorld(batcher, ref matrix, false);

            return can_draw_lights;
        }

        private bool DrawWorldRenderTarget(UltimaBatcher2D batcher, GraphicsDevice gd, Viewport camera_viewport)
        {
            Profile profile = ProfileManager.CurrentProfile;
            float scale = GetActiveScale();
            bool can_draw_lights = false;
            Rectangle srcRect;
            Rectangle destRect;

            int rtW = _world_render_target?.Width ?? Camera.Bounds.Width;
            int rtH = _world_render_target?.Height ?? Camera.Bounds.Height;
            int vpW = Camera.Bounds.Width;
            int vpH = Camera.Bounds.Height;

            var vpCenter = new Vector2(vpW * 0.5f, vpH * 0.5f);
            Vector2 camOffset = Camera.Offset;
            var rtCenter = new Vector2(rtW * 0.5f, rtH * 0.5f);

            Matrix.CreateTranslation(-vpCenter.X, -vpCenter.Y, 0f, out Matrix matTrans1);
            Matrix.CreateTranslation(-camOffset.X, -camOffset.Y, 0f, out Matrix matTrans2);
            Matrix.CreateTranslation(rtCenter.X, rtCenter.Y, 0f, out Matrix matTrans3);
            Matrix.Multiply(ref matTrans1, ref matTrans2, out Matrix temp1);
            Matrix.Multiply(ref temp1, ref matTrans3, out Matrix worldRTMatrix);

            if (profile.GlobalScaling)
            {
                Camera.Zoom = 1f;

                camera_viewport.Bounds = new Rectangle(
                    (int)(camera_viewport.Bounds.X * profile.GlobalScale),
                    (int)(camera_viewport.Bounds.Y * profile.GlobalScale),
                    (int)(camera_viewport.Bounds.Width * profile.GlobalScale),
                    (int)(camera_viewport.Bounds.Height * profile.GlobalScale)
                );

                DrawWorld(batcher, ref worldRTMatrix, true);

                can_draw_lights = PrepareLightsRendering(batcher, ref worldRTMatrix);
                gd.Viewport = camera_viewport;

                srcRect = new Rectangle(0, 0, rtW, rtH);
                destRect = new Rectangle(0, 0, (int)Math.Floor(vpW * scale), (int)Math.Floor(vpH * scale));
            }
            else
            {
                DrawWorld(batcher, ref worldRTMatrix, true);

                can_draw_lights = PrepareLightsRendering(batcher, ref worldRTMatrix);
                gd.Viewport = camera_viewport;

                int srcW = (int)Math.Floor(vpW * scale);
                int srcH = (int)Math.Floor(vpH * scale);
                int srcX = (rtW - srcW) / 2;
                int srcY = (rtH - srcH) / 2;
                srcRect = new Rectangle(srcX, srcY, srcW, srcH);
                destRect = new Rectangle(0, 0, vpW, vpH);
            }

            UpdatePostProcessState(gd);

            if (_postFx == _xbr && _xbr != null)
            {
                BindXbrParams(gd);
            }
            batcher.Begin(_postFx, Matrix.Identity);
            try { batcher.SetSampler(_postSampler ?? SamplerState.PointClamp); } catch { batcher.SetSampler(SamplerState.PointClamp); }
            batcher.Draw(_world_render_target, destRect, srcRect, new Vector3(0, 0, 1));
            batcher.End();
            batcher.SetSampler(null);
            batcher.SetBlendState(null);

            return can_draw_lights;
        }

        private void DrawWorld(UltimaBatcher2D batcher, ref Matrix matrix, bool use_render_target)
        {
            SelectedObject.Object = null;
            Profiler.EnterContext("FillObjectList");
            FillGameObjectList();
            Profiler.ExitContext("FillObjectList");

            if (use_render_target)
            {
                batcher.GraphicsDevice.SetRenderTarget(_world_render_target);
                batcher.GraphicsDevice.Clear(ClearOptions.Target, Color.Black, 1f, 0);
            }

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
            _world.Weather.Draw(batcher, 0, 0); // TODO: fix the depth

            batcher.End();

            if (use_render_target)
            {
                batcher.GraphicsDevice.SetRenderTarget(null);
            }

            //batcher.Begin();
            //hueVec.X = 0;
            //hueVec.Y = 1;
            //hueVec.Z = 1;
            //string s = $"Flushes: {batcher.FlushesDone}\nSwitches: {batcher.TextureSwitches}\nArt texture count: {TextureAtlas.Shared.TexturesCount}\nMaxZ: {_maxZ}\nMaxGround: {_maxGroundZ}";
            //batcher.DrawString(Fonts.Bold, s, 200, 200, ref hueVec);
            //hueVec = Vector3.Zero;
            //batcher.DrawString(Fonts.Bold, s, 200 + 1, 200 - 1, ref hueVec);
            //batcher.End();
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
            if (_light_render_target == null) return false;

            batcher.GraphicsDevice.SetRenderTarget(_light_render_target);
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
                ref readonly SpriteInfo lightInfo = ref Client.Game.UO.Lights.GetLight(l.ID);

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

            batcher.GraphicsDevice.SetRenderTarget(null);
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
            _world.WorldTextManager.Draw(batcher, Camera.Bounds.X, Camera.Bounds.Y);
        }

        public void DrawSelection(UltimaBatcher2D batcher)
        {
            if (_isSelectionActive)
            {
                var selectionHue = new Vector3();
                selectionHue.Z = 0.7f;

                int minX = Math.Min(_selectionStart.X, Mouse.Position.X);
                int maxX = Math.Max(_selectionStart.X, Mouse.Position.X);
                int minY = Math.Min(_selectionStart.Y, Mouse.Position.Y);
                int maxY = Math.Max(_selectionStart.Y, Mouse.Position.Y);

                var selectionRect = new Rectangle(
                    minX - Camera.Bounds.X,
                    minY - Camera.Bounds.Y,
                    maxX - minX,
                    maxY - minY
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
        }

        private void EnsureRenderTargets(GraphicsDevice gd)
        {
            Viewport vp = Camera.GetViewport();
            Profile profile = ProfileManager.CurrentProfile;
            float scale = GetActiveScale();

            int vw = Math.Max(1, Camera.Bounds.Width);
            int vh = Math.Max(1, Camera.Bounds.Height);
            int rtWidth = Math.Min(profile.GlobalScaling ? vw : (int)Math.Floor(vw * scale), _max_texture_size);
            int rtHeight = Math.Min(profile.GlobalScaling ? vh : (int)Math.Floor(vh * scale), _max_texture_size);

            if (_use_render_target
                && (_world_render_target == null
                    || _world_render_target.IsDisposed
                    || _world_render_target.Width != rtWidth
                    || _world_render_target.Height != rtHeight
                 ))
            {
                _world_render_target?.Dispose();
                PresentationParameters pp = gd.PresentationParameters;
                _world_render_target = new RenderTarget2D(
                    gd, rtWidth, rtHeight, false,
                    pp.BackBufferFormat, pp.DepthStencilFormat, pp.MultiSampleCount, pp.RenderTargetUsage);
            }

            int ltWidth = _use_render_target ? rtWidth : vw;
            int ltHeight = _use_render_target ? rtHeight : vh;

            if (_light_render_target == null
                || _light_render_target.IsDisposed
                || _light_render_target.Width != ltWidth
                || _light_render_target.Height != ltHeight)
            {
                _light_render_target?.Dispose();
                PresentationParameters pp = gd.PresentationParameters;
                _light_render_target = new RenderTarget2D(
                    gd, ltWidth, ltHeight, false,
                    pp.BackBufferFormat, pp.DepthStencilFormat, pp.MultiSampleCount, pp.RenderTargetUsage);
            }
        }

        private void UpdatePostProcessState(GraphicsDevice gd)
        {
            string mode = (_filterMode ?? "point").ToLowerInvariant();
            Profile profile = ProfileManager.CurrentProfile;
            float scale = GetActiveScale();

            if (
                (mode == "xbr" && scale >= 1.0f && !profile.GlobalScaling) ||
                (mode == "xbr" && scale <= 1.0f && profile.GlobalScaling))
            {
                _postFx = null;
                _postSampler = SamplerState.LinearClamp;
                _currentFilter = "linear";
                return;
            }

            if (_currentFilter == mode &&
                ((_postFx == null && mode != "xbr") || (_postFx != null && (mode != "xbr" || ReferenceEquals(_postFx, _xbr)))))
                return;

            _currentFilter = mode;

            switch (mode)
            {
                case "xbr":
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

                case "anisotropic":
                    _postFx = null;
                    _postSampler = SamplerState.AnisotropicClamp;
                    break;

                case "linear":
                    _postFx = null;
                    _postSampler = SamplerState.LinearClamp;
                    break;

                case "point":
                default:
                    _postFx = null;
                    _postSampler = SamplerState.PointClamp;
                    break;
            }
        }

        private void BindXbrParams(GraphicsDevice gd)
        {
            if (_xbr == null || _world_render_target == null) return;

            try
            {
                if (_xbr.Techniques?["T0"] != null)
                    _xbr.CurrentTechnique = _xbr.Techniques["T0"];
            }
            catch { }

            float w = _world_render_target.Width;
            float h = _world_render_target.Height;

            Viewport vp = gd.Viewport;
            var ortho = Matrix.CreateOrthographicOffCenter(0, vp.Width, vp.Height, 0, 0, 1);
            _xbr.MatrixTransform?.SetValue(ortho);
            _xbr.TextureSize?.SetValue(new Vector2(w, h));
            _xbr.Parameters?["invTextureSize"]?.SetValue(new Vector2(1f / w, 1f / h));
            _xbr.Parameters?["TextureSizeInv"]?.SetValue(new Vector2(1f / w, 1f / h));
            _xbr.Parameters?["decal"]?.SetValue(_world_render_target);
        }

        private static readonly RenderedText _youAreDeadText = RenderedText.Create(
            ResGeneral.YouAreDead,
            0xFFFF,
            3,
            false,
            FontStyle.BlackBorder,
            TEXT_ALIGN_TYPE.TS_LEFT
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
            public byte ID;
            public ushort Color;
            public bool IsHue;
            public int DrawX,
                DrawY;
        }
    }
}
