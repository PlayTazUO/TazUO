// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Managers.Hotkeys;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.Renderer;
using ClassicUO.Resources;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SDL3;
using System.Text.Json.Serialization;
using static ClassicUO.Game.UI.Gumps.WorldMapGump;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Png;
using ClassicUO.Network.Encryption;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;
using ClassicUO.Assets;

namespace ClassicUO.Game.UI.Gumps;

public enum WorldMapDoubleClickAction
{
    ToggleLock,
    ToggleFullscreen
}

[JsonSourceGenerationOptions(WriteIndented = true, GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(ZonesFile), GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(ZonesFileZoneData), GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(List<ZonesFileZoneData>), GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(List<int>), GenerationMode = JsonSourceGenerationMode.Metadata)]
sealed partial class ZonesJsonContext : JsonSerializerContext { }

public class WorldMapGump : ResizableGump
{
    public const string USER_MARKERS_FILE = "userMarkers";

    private static readonly string[] _mapFilesPath = [Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Client"), Path.Combine(Settings.GlobalSettings.UltimaOnlineDirectory, "MapMarkers"), Path.Combine(CUOEnviroment.ExecutablePath, "Data", FileSystemHelper.RemoveInvalidChars(World.Instance.ServerName), "MapMarkers")];
    private static readonly string[] _mapIconsPath = [Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Client", "MapIcons"), Path.Combine(Settings.GlobalSettings.UltimaOnlineDirectory, "MapIcons"), Path.Combine(CUOEnviroment.ExecutablePath, "Data", FileSystemHelper.RemoveInvalidChars(World.Instance.ServerName), "MapIcons")];

    private static readonly string _mapsCachePath = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Client", "MapsCache");
    private static readonly string UserMarkersFilePath = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Client", $"{USER_MARKERS_FILE}.usr");
    public static readonly List<WMapMarkerFile> _markerFiles = new List<WMapMarkerFile>();
    public static readonly Dictionary<string, Texture2D> _markerIcons = new Dictionary<string, Texture2D>();
    // Maps a marker icon name (file name without extension, lowercased) to the full path of the
    // source icon file on disk. Used by the web map so it can serve the original icon file by its
    // path instead of streaming rendered GPU textures.
    public static readonly Dictionary<string, string> _markerIconPaths = new Dictionary<string, string>();
    private static readonly float[] _zooms = new float[10] { 0.125f, 0.25f, 0.5f, 0.75f, 1f, 1.5f, 2f, 4f, 6f, 8f };
    private static readonly Color _semiTransparentWhiteForGrid = new Color(255, 255, 255, 56);
    private static Point _last_position = new Point(100, 100);
    private static Texture2D _mapTexture;
    private static string _mapPngFilePath;
    private Map.Map _map = null;

    private Point _center, _lastScroll, _scroll;
    private Point? _lastMousePosition = null;
    private bool _flipMap = true;
    private bool _freeView;
    private List<string> _hiddenMarkerFiles;
    private bool _isScrolling;
    private bool _isTopMost;
    private bool _mapMarkersLoaded;
    private List<string> _hiddenZoneFiles;
    private ZoneSets _zoneSets = new ZoneSets();

    private static Mobile following;

    public Texture2D MapTexture => _mapTexture;

    private Renderer.SpriteFont _markerFont = Fonts.Map1;
    private int _markerFontIndex = 1;

    // When a TTF font is selected all names/markers are rendered through cached TextBox
    // instances instead of the sprite-font DrawString methods. An empty name means the
    // sprite-font styles (_markerFont) are used instead.
    private string _ttfFont = string.Empty;
    private int _ttfFontSize = 20;
    private const int TTF_FONT_SIZE_MIN = 6;
    private const int TTF_FONT_SIZE_MAX = 60;
    // TextBoxes are cached by their text and reused across frames. Entries that have not been
    // drawn for a while are disposed in Update; everything is disposed when the gump closes.
    private readonly Dictionary<string, TextBox> _ttfTextBoxes = new Dictionary<string, TextBox>();
    private readonly Dictionary<string, long> _ttfTextBoxLastUse = new Dictionary<string, long>();
    private const long TTF_TEXTBOX_TTL = 10000;
    private bool UseTtfFont => !string.IsNullOrEmpty(_ttfFont);
    private readonly Dictionary<string, ContextMenuItemEntry> _options = new Dictionary<string, ContextMenuItemEntry>();
    private bool _showCoordinates;
    private bool _showSextantCoordinates;
    private bool _showMouseCoordinates;
    private bool _showGroupBar = true;
    private bool _showGroupName = true;
    private bool _showMarkerIcons = true;
    private bool _showMarkerNames = true;
    private bool _showMarkers = true;
    private bool _showCorpse = true;
    private bool _showMobiles = true;
    private bool _showMultis = true;
    private bool _showPartyMembers = true;
    private bool _showPlayerBar = true;
    private bool _showPlayerName = true;
    private int _zoomIndex = 4;
    private bool _showGridIfZoomed = true;
    private bool _allowPositionalTarget = false;
    private WorldMapDoubleClickAction _doubleClickAction = WorldMapDoubleClickAction.ToggleLock;
    private bool _isFullscreen;
    private Rectangle _preFullscreenBounds;
    private bool _preFullscreenTopMost;

    private GumpPic _northIcon;

    private WMapMarker _gotoMarker;

    private Point? _navDest;
    private long _navDestSetTime;
    private List<Point> _navPath;
    private Action<int, int> _navStepFailedHandler;

    // End of the currently planned route (world coords + Z). Shift+Ctrl right-click extends
    // the path by searching from here to the new point, so multi-segment routes chain
    // A->B->C. _navSegments tracks how many segments make up the active route.
    private Point _navPlannedEnd;
    private sbyte _navPlannedEndZ;
    private int _navSegments;

    private const int MAX_NAV_REPLANS = 3;
    private int _navReplansLeft;

    // User-configurable replan budget; falls back to the default when no profile is loaded.
    private static int MaxNavReplans => ProfileManager.CurrentProfile?.WorldMapPathfindingMaxRetries >= 0
        ? ProfileManager.CurrentProfile.WorldMapPathfindingMaxRetries
        : MAX_NAV_REPLANS;

    private static int _mapLoading;
    private Task _loadingTask;
    private World _world;

    public WorldMapGump(World world) : base
    (
        world,
        400,
        400,
        100,
        100,
        0,
        0
    )
    {
        _world = world;
        CanMove = true;
        AcceptMouseInput = true;
        CanCloseWithRightClick = false;

        if (ProfileManager.CurrentProfile != null)
        {
            _last_position = ProfileManager.CurrentProfile.WorldMapPosition;
            IsLocked = ProfileManager.CurrentProfile.WorldMapLocked;
        }

        X = _last_position.X;
        Y = _last_position.Y;

        _map = World.Map;
        LoadSettings();

        GameActions.Print(World, ResGumps.WorldMapLoading, 0x35);
        ChangeMap(World.MapIndex);
        OnResize();

        LoadMarkers();
        LoadZones();

        BuildGump();
    }

    public override GumpType GumpType => GumpType.WorldMap;
    public float Zoom => _zooms[_zoomIndex];

    public bool TopMost
    {
        get => _isTopMost;
        set => SetTopMost(value, true);
    }

    private void SetTopMost(bool value, bool save)
    {
        if (_isTopMost != value)
        {
            _isTopMost = value;

            if (save)
            {
                SaveSettings();
            }
        }

        ShowBorder = _isTopMost;

        LayerOrder = _isTopMost ? UILayer.Over : UILayer.Under;
    }

    public bool FreeView
    {
        get => _freeView;
        set
        {
            if (_freeView != value)
            {
                _freeView = value;
                SaveSettings();

                if (!_freeView)
                {
                    _isScrolling = false;
                    if (!IsLocked)
                    {
                        CanMove = true;
                    }
                }
            }
        }
    }

    public override void Restore(XmlElement xml)
    {
        base.Restore(xml);

        BuildGump();
    }

    private void LoadSettings()
    {
        Width = ProfileManager.CurrentProfile.WorldMapWidth;
        Height = ProfileManager.CurrentProfile.WorldMapHeight;

        SetFont(ProfileManager.CurrentProfile.WorldMapFont);

        _ttfFont = ProfileManager.CurrentProfile.WorldMapTtfFont ?? string.Empty;
        _ttfFontSize = Math.Clamp(ProfileManager.CurrentProfile.WorldMapTtfFontSize, TTF_FONT_SIZE_MIN, TTF_FONT_SIZE_MAX);

        ResizeWindow(new Point(Width, Height));

        _flipMap = ProfileManager.CurrentProfile.WorldMapFlipMap;
        _showPartyMembers = ProfileManager.CurrentProfile.WorldMapShowParty;

        World.WMapManager.SetEnable(_showPartyMembers);

        _zoomIndex = ProfileManager.CurrentProfile.WorldMapZoomIndex;

        _showCoordinates = ProfileManager.CurrentProfile.WorldMapShowCoordinates;
        _showSextantCoordinates = ProfileManager.CurrentProfile.WorldMapShowSextantCoordinates;
        _showMouseCoordinates = ProfileManager.CurrentProfile.WorldMapShowMouseCoordinates;
        _showMobiles = ProfileManager.CurrentProfile.WorldMapShowMobiles;
        _showCorpse = ProfileManager.CurrentProfile.WorldMapShowCorpse;


        _showPlayerName = ProfileManager.CurrentProfile.WorldMapShowPlayerName;
        _showPlayerBar = ProfileManager.CurrentProfile.WorldMapShowPlayerBar;
        _showGroupName = ProfileManager.CurrentProfile.WorldMapShowGroupName;
        _showGroupBar = ProfileManager.CurrentProfile.WorldMapShowGroupBar;
        _showMarkers = ProfileManager.CurrentProfile.WorldMapShowMarkers;
        _showMultis = ProfileManager.CurrentProfile.WorldMapShowMultis;
        _showMarkerNames = ProfileManager.CurrentProfile.WorldMapShowMarkersNames;


        _hiddenMarkerFiles = string.IsNullOrEmpty(ProfileManager.CurrentProfile.WorldMapHiddenMarkerFiles) ? new List<string>() : ProfileManager.CurrentProfile.WorldMapHiddenMarkerFiles.Split(',').ToList();
        _hiddenZoneFiles = string.IsNullOrEmpty(ProfileManager.CurrentProfile.WorldMapHiddenZoneFiles) ? new List<string>() : ProfileManager.CurrentProfile.WorldMapHiddenZoneFiles.Split(',').ToList();

        _showGridIfZoomed = ProfileManager.CurrentProfile.WorldMapShowGridIfZoomed;
        _allowPositionalTarget = ProfileManager.CurrentProfile.WorldMapAllowPositionalTarget;
        _doubleClickAction = ProfileManager.CurrentProfile.WorldMapDoubleClickAction;
        TopMost = ProfileManager.CurrentProfile.WorldMapTopMost;
        FreeView = ProfileManager.CurrentProfile.WorldMapFreeView;
    }

    public void SaveSettings()
    {
        if (ProfileManager.CurrentProfile == null)
        {
            return;
        }


        // While in fullscreen mode, persist the windowed (pre-fullscreen) bounds so the
        // map restores to its previous size/position rather than staying fullscreen.
        ProfileManager.CurrentProfile.WorldMapWidth = _isFullscreen ? _preFullscreenBounds.Width : Width;
        ProfileManager.CurrentProfile.WorldMapHeight = _isFullscreen ? _preFullscreenBounds.Height : Height;

        ProfileManager.CurrentProfile.WorldMapFlipMap = _flipMap;
        ProfileManager.CurrentProfile.WorldMapTopMost = TopMost;
        ProfileManager.CurrentProfile.WorldMapFreeView = FreeView;
        ProfileManager.CurrentProfile.WorldMapShowParty = _showPartyMembers;

        ProfileManager.CurrentProfile.WorldMapZoomIndex = _zoomIndex;

        ProfileManager.CurrentProfile.WorldMapShowCoordinates = _showCoordinates;
        ProfileManager.CurrentProfile.WorldMapShowSextantCoordinates = _showSextantCoordinates;
        ProfileManager.CurrentProfile.WorldMapShowMouseCoordinates = _showMouseCoordinates;
        ProfileManager.CurrentProfile.WorldMapShowMobiles = _showMobiles;
        ProfileManager.CurrentProfile.WorldMapShowCorpse = _showCorpse;

        ProfileManager.CurrentProfile.WorldMapShowPlayerName = _showPlayerName;
        ProfileManager.CurrentProfile.WorldMapShowPlayerBar = _showPlayerBar;
        ProfileManager.CurrentProfile.WorldMapShowGroupName = _showGroupName;
        ProfileManager.CurrentProfile.WorldMapShowGroupBar = _showGroupBar;
        ProfileManager.CurrentProfile.WorldMapShowMarkers = _showMarkers;
        ProfileManager.CurrentProfile.WorldMapShowMultis = _showMultis;
        ProfileManager.CurrentProfile.WorldMapShowMarkersNames = _showMarkerNames;

        ProfileManager.CurrentProfile.WorldMapHiddenMarkerFiles = string.Join(",", _hiddenMarkerFiles);
        ProfileManager.CurrentProfile.WorldMapHiddenZoneFiles = string.Join(",", _hiddenZoneFiles);

        ProfileManager.CurrentProfile.WorldMapFont = _markerFontIndex;
        ProfileManager.CurrentProfile.WorldMapTtfFont = _ttfFont;
        ProfileManager.CurrentProfile.WorldMapTtfFontSize = _ttfFontSize;

        ProfileManager.CurrentProfile.WorldMapShowGridIfZoomed = _showGridIfZoomed;
        ProfileManager.CurrentProfile.WorldMapPosition = _isFullscreen ? new Point(_preFullscreenBounds.X, _preFullscreenBounds.Y) : new Point(X, Y);
        ProfileManager.CurrentProfile.WorldMapAllowPositionalTarget = _allowPositionalTarget;
        ProfileManager.CurrentProfile.WorldMapDoubleClickAction = _doubleClickAction;
    }

    private bool ParseBool(string boolStr) => bool.TryParse(boolStr, out bool value) && value;

    private void BuildGump()
    {
        BuildContextMenu();
        _northIcon?.Dispose();
        _northIcon = new GumpPic(0, 0, 5021, 0) { Width = 22, Height = 25 };
        _northIcon.X = Width - _northIcon.Width - BorderControl.BorderSize;
        _northIcon.Y = !_flipMap ? Height - _northIcon.Height - BorderControl.BorderSize : BorderControl.BorderSize;
        Add(_northIcon);
    }

    public override void OnResize()
    {
        base.OnResize();
        if (_northIcon != null)
        {
            _northIcon.X = Width - _northIcon.Width - BorderControl.BorderSize;
            _northIcon.Y = !_flipMap ? Height - _northIcon.Height - BorderControl.BorderSize : BorderControl.BorderSize;
        }
    }

    private void BuildOptionDictionary()
    {
        _options.Clear();

        _options["show_all_markers"] = new ContextMenuItemEntry(ResGumps.ShowAllMarkers, () => { _showMarkers = !_showMarkers; SaveSettings(); }, true, _showMarkers);
        _options["show_marker_names"] = new ContextMenuItemEntry(ResGumps.ShowMarkerNames, () => { _showMarkerNames = !_showMarkerNames; SaveSettings(); }, true, _showMarkerNames);
        _options["show_marker_icons"] = new ContextMenuItemEntry(ResGumps.ShowMarkerIcons, () => { _showMarkerIcons = !_showMarkerIcons; SaveSettings(); }, true, _showMarkerIcons);
        _options["flip_map"] = new ContextMenuItemEntry(ResGumps.FlipMap, () =>
        {
            _flipMap = !_flipMap; SaveSettings();
            if (_northIcon != null)
            {
                _northIcon.X = Width - _northIcon.Width - BorderControl.BorderSize;
                _northIcon.Y = !_flipMap ? Height - _northIcon.Height - BorderControl.BorderSize : BorderControl.BorderSize;
            }
        }, true, _flipMap);

        _options["goto_location"] = new ContextMenuItemEntry
        (
            ResGumps.GotoLocation,
            () => UIManager.Add(new LocationGoGump(
                    World,
                    (x, y) => GoToMarker(x, y, true),
                    ClearGoToMarker,
                    _gotoMarker != null // Pass in the current marker location, if any
                        ? new Point(_gotoMarker.X, _gotoMarker.Y)
                        : null
                )
            )
        );

        _options["top_most"] = new ContextMenuItemEntry(ResGumps.TopMost, () => { TopMost = !TopMost; }, true, _isTopMost);

        _options["free_view"] = new ContextMenuItemEntry(ResGumps.FreeView, () => { FreeView = !FreeView; }, true, FreeView);

        for (int i = 0; i < MapLoader.MAPS_COUNT; i++)
        {
            int idx = i;

            _options[$"free_view_map_{idx}"] = new ContextMenuItemEntry
            (
                string.Format(ResGumps.WorldMapChangeMap0, idx), () =>
                {
                    FreeView = true;
                    ChangeMap(idx);
                }
            );
        }

        _options["show_party_members"] = new ContextMenuItemEntry
        (
            ResGumps.ShowPartyMembers,
            () =>
            {
                _showPartyMembers = !_showPartyMembers;

                World.WMapManager.SetEnable(_showPartyMembers);
                SaveSettings();
            },
            true,
            _showPartyMembers
        );
        _options["show_corpse"] = new ContextMenuItemEntry("Show my Corpse", () => { _showCorpse = !_showCorpse; SaveSettings(); }, true, _showCorpse);

        _options["show_mobiles"] = new ContextMenuItemEntry(ResGumps.ShowMobiles, () => { _showMobiles = !_showMobiles; SaveSettings(); }, true, _showMobiles);

        _options["show_multis"] = new ContextMenuItemEntry(ResGumps.ShowHousesBoats, () => { _showMultis = !_showMultis; SaveSettings(); }, true, _showMultis);

        _options["show_your_name"] = new ContextMenuItemEntry(ResGumps.ShowYourName, () => { _showPlayerName = !_showPlayerName; SaveSettings(); }, true, _showPlayerName);

        _options["show_your_healthbar"] = new ContextMenuItemEntry(ResGumps.ShowYourHealthbar, () => { _showPlayerBar = !_showPlayerBar; SaveSettings(); }, true, _showPlayerBar);

        _options["show_party_name"] = new ContextMenuItemEntry(ResGumps.ShowGroupName, () => { _showGroupName = !_showGroupName; SaveSettings(); }, true, _showGroupName);

        _options["show_party_healthbar"] = new ContextMenuItemEntry(ResGumps.ShowGroupHealthbar, () => { _showGroupBar = !_showGroupBar; SaveSettings(); }, true, _showGroupBar);

        _options["show_coordinates"] = new ContextMenuItemEntry(ResGumps.ShowYourCoordinates, () => { _showCoordinates = !_showCoordinates; SaveSettings(); }, true, _showCoordinates);

        _options["show_sextant_coordinates"] = new ContextMenuItemEntry(ResGumps.ShowSextantCoordinates, () => { _showSextantCoordinates = !_showSextantCoordinates; }, true, _showSextantCoordinates);

        _options["show_mouse_coordinates"] = new ContextMenuItemEntry(ResGumps.ShowMouseCoordinates, () => { _showMouseCoordinates = !_showMouseCoordinates; }, true, _showMouseCoordinates);

        _options["allow_positional_target"] = new ContextMenuItemEntry(
            ResGumps.AllowPositionalTargeting, () => { _allowPositionalTarget = !_allowPositionalTarget; SaveSettings(); }, true, _allowPositionalTarget
        );

        _options["markers_manager"] = new ContextMenuItemEntry(ResGumps.MarkersManager,
            () =>
            {
                var mm = new MarkersManagerGump(World);

                UIManager.Add(mm);
            }
        );

        _options["add_marker_on_player"] = new ContextMenuItemEntry(ResGumps.AddMarkerOnPlayer, () => AddMarkerOnPlayer());

        _options["open_web_map"] = new ContextMenuItemEntry("Open Web Map (Browser)", GameActions.OpenWorldMapWebWindow);

        _options["auto_start_web_map"] = new ContextMenuItemEntry("Auto start web map", () =>
        {
            ProfileManager.CurrentProfile.WebMapAutoStart = !ProfileManager.CurrentProfile.WebMapAutoStart;
            if (!MapWebServerManager.Instance.IsRunning)
                _ = MapWebServerManager.Instance.Start();

        }, true, ProfileManager.CurrentProfile.WebMapAutoStart);

        _options["saveclose"] = new ContextMenuItemEntry(ResGumps.SaveClose, Dispose);

        _options["show_grid_if_zoomed"] = new ContextMenuItemEntry(ResGumps.GridIfZoomed, () => { _showGridIfZoomed = !_showGridIfZoomed; SaveSettings(); }, true, _showGridIfZoomed);

        _options["reset_map_cache"] = new ContextMenuItemEntry(ResGumps.ResetMapsCache, () =>
        {
            if(Directory.Exists(_mapsCachePath))
                Directory.GetFiles(_mapsCachePath, "*.png").ForEach(s => File.Delete(s));
        }, false);
    }

    public void GoToMarker(int x, int y, bool isManualType)
    {
        FreeView = true;

        _gotoMarker = new WMapMarker
        {
            Color = Color.Aquamarine,
            MapId = _map.Index,
            Name = isManualType ? $"Go to: {x}, {y}" : "",
            X = x,
            Y = y,
            ZoomIndex = 1
        };

        _center.X = x;
        _center.Y = y;
    }

    /// <summary>
    /// Clears the current <em>Go-To</em> marker, if it was set
    /// </summary>
    public void ClearGoToMarker() => _gotoMarker = null;

    private void BuildContextMenuForZones(ContextMenuControl parent)
    {
        var zoneOptions = new ContextMenuItemEntry(ResGumps.MapZoneOptions);

        zoneOptions.Add(_options["show_grid_if_zoomed"]);
        zoneOptions.Add(new ContextMenuItemEntry(ResGumps.MapZoneReload, () => { LoadZones(); BuildContextMenu(); }));
        zoneOptions.Add(new ContextMenuItemEntry(""));

        if (_zoneSets.ZoneSetDict.Count < 1)
        {
            zoneOptions.Add(new ContextMenuItemEntry(ResGumps.MapZoneNone));
        }
        else
        {
            foreach (KeyValuePair<string, ZoneSet> entry in _zoneSets.ZoneSetDict)
            {
                string filename = entry.Key;
                ZoneSet zoneSet = entry.Value;

                zoneOptions.Add
                (
                    new ContextMenuItemEntry
                    (
                        String.Format(ResGumps.MapZoneFileName, zoneSet.NiceFileName),
                        () =>
                        {
                            zoneSet.Hidden = !zoneSet.Hidden;

                            if (!zoneSet.Hidden)
                            {
                                string hiddenFile = _hiddenZoneFiles.FirstOrDefault(x => x.Equals(filename));

                                if (!string.IsNullOrEmpty(hiddenFile))
                                {
                                    _hiddenZoneFiles.Remove(hiddenFile);
                                }
                            }
                            else
                            {
                                _hiddenZoneFiles.Add(filename);
                            }
                        },
                        true,
                        !entry.Value.Hidden
                    )
                );
            }
        }

        parent.Add(zoneOptions);
    }

    public override void CloseWithRightClick()
    {
        if (!Keyboard.Ctrl)
        {
            BuildContextMenu();
            ContextMenu?.Show();
        }
        return;
    }

    public static void FollowMobile(Mobile m) => following = m;

    private void BuildContextMenu()
    {
        BuildOptionDictionary();

        ContextMenu?.Dispose();
        ContextMenu = new ContextMenuControl(this);

        var follow = new ContextMenuItemEntry(TazLang.Get("map_follow"));
        follow.Add(new ContextMenuItemEntry(TazLang.Get("map_yourself"), () => { following = World.Player; }, true));
        if (World.Party != null && World.Party.Leader != 0)
        {
            foreach (PartyMember e in World.Party.Members)
            {
                if (e != null && SerialHelper.IsValid(e.Serial))
                {
                    Mobile mob = World.Mobiles.Get(e.Serial);
                    if (mob != null && mob.Serial != World.Player.Serial)
                    {
                        follow.Add(new ContextMenuItemEntry(e.Name, () => { following = mob; }, true));
                    }
                }
            }
        }
        ContextMenu.Add(follow);

        // Font style choice applies to both marker names and mobile/entity names, so it
        // lives on the main context menu (not the marker submenu). Selecting a sprite-font
        // style also turns off any active TTF font.
        var markerFontEntry = new ContextMenuItemEntry(ResGumps.FontStyle);
        markerFontEntry.Add(new ContextMenuItemEntry(string.Format(ResGumps.Style0, 1), () => { SetFont(1); SaveSettings(); }, true, !UseTtfFont && _markerFontIndex == 1));
        markerFontEntry.Add(new ContextMenuItemEntry(string.Format(ResGumps.Style0, 2), () => { SetFont(2); SaveSettings(); }, true, !UseTtfFont && _markerFontIndex == 2));
        markerFontEntry.Add(new ContextMenuItemEntry(string.Format(ResGumps.Style0, 3), () => { SetFont(3); SaveSettings(); }, true, !UseTtfFont && _markerFontIndex == 3));
        markerFontEntry.Add(new ContextMenuItemEntry(string.Format(ResGumps.Style0, 4), () => { SetFont(4); SaveSettings(); }, true, !UseTtfFont && _markerFontIndex == 4));
        markerFontEntry.Add(new ContextMenuItemEntry(string.Format(ResGumps.Style0, 5), () => { SetFont(5); SaveSettings(); }, true, !UseTtfFont && _markerFontIndex == 5));
        markerFontEntry.Add(new ContextMenuItemEntry(string.Format(ResGumps.Style0, 6), () => { SetFont(6); SaveSettings(); }, true, !UseTtfFont && _markerFontIndex == 6));
        ContextMenu.Add(markerFontEntry);

        ContextMenu.Add(BuildTtfFontMenu());

        var markersEntry = new ContextMenuItemEntry(ResGumps.MapMarkerOptions);
        markersEntry.Add(new ContextMenuItemEntry(ResGumps.ReloadMarkers, LoadMarkers));

        markersEntry.Add(_options["show_all_markers"]);
        markersEntry.Add(new ContextMenuItemEntry(""));
        markersEntry.Add(_options["show_marker_names"]);
        markersEntry.Add(_options["show_marker_icons"]);
        markersEntry.Add(new ContextMenuItemEntry(""));

        if (_markerFiles.Count > 0)
        {
            foreach (WMapMarkerFile markerFile in _markerFiles)
            {
                var entry = new ContextMenuItemEntry
                (
                    string.Format(ResGumps.ShowHide0, markerFile.Name),
                    () =>
                    {
                        markerFile.Hidden = !markerFile.Hidden;

                            if (!markerFile.Hidden)
                            {
                                string hiddenFile = _hiddenMarkerFiles.FirstOrDefault(x => x.Equals(markerFile.Name));

                            if (!string.IsNullOrEmpty(hiddenFile))
                            {
                                _hiddenMarkerFiles.Remove(hiddenFile);
                            }
                        }
                        else
                        {
                            _hiddenMarkerFiles.Add(markerFile.Name);
                        }
                    },
                    true,
                    !markerFile.Hidden
                );

                _options[$"show_marker_{markerFile.Name}"] = entry;
                markersEntry.Add(entry);
            }
        }
        else
        {
            markersEntry.Add(new ContextMenuItemEntry(ResGumps.NoMapFiles));
        }


        ContextMenu.Add(markersEntry);

        BuildContextMenuForZones(ContextMenu);

        var namesHpBarEntry = new ContextMenuItemEntry(ResGumps.NamesHealthbars);
        namesHpBarEntry.Add(_options["show_your_name"]);
        namesHpBarEntry.Add(_options["show_your_healthbar"]);
        namesHpBarEntry.Add(_options["show_party_name"]);
        namesHpBarEntry.Add(_options["show_party_healthbar"]);

        ContextMenu.Add(namesHpBarEntry);

        ContextMenu.Add("", null);
        ContextMenu.Add(_options["goto_location"]);
        ContextMenu.Add(_options["flip_map"]);
        ContextMenu.Add(_options["top_most"]);

        var doubleClickEntry = new ContextMenuItemEntry(TazLang.Get("map_doubleclick_action", "Double click action"));
        doubleClickEntry.Add(new ContextMenuItemEntry(TazLang.Get("map_doubleclick_toggle_lock", "Toggle lock state"),
            () => { SetDoubleClickAction(WorldMapDoubleClickAction.ToggleLock); }, true,
            _doubleClickAction == WorldMapDoubleClickAction.ToggleLock));
        doubleClickEntry.Add(new ContextMenuItemEntry(TazLang.Get("map_doubleclick_toggle_fullscreen", "Toggle fullscreen"),
            () => { SetDoubleClickAction(WorldMapDoubleClickAction.ToggleFullscreen); }, true,
            _doubleClickAction == WorldMapDoubleClickAction.ToggleFullscreen));
        ContextMenu.Add(doubleClickEntry);

        var freeView = new ContextMenuItemEntry(ResGumps.FreeView);
        freeView.Add(_options["free_view"]);

        for (int i = 0; i < MapLoader.MAPS_COUNT; i++)
            freeView.Add(_options[$"free_view_map_{i}"]);

        ContextMenu.Add(freeView);

        ContextMenu.Add("", null);
        ContextMenu.Add(_options["show_party_members"]);
        ContextMenu.Add(_options["show_corpse"]);
        ContextMenu.Add(_options["show_mobiles"]);
        ContextMenu.Add(_options["show_multis"]);
        ContextMenu.Add(_options["show_coordinates"]);
        ContextMenu.Add(_options["show_sextant_coordinates"]);
        ContextMenu.Add(_options["show_mouse_coordinates"]);
        ContextMenu.Add(_options["allow_positional_target"]);
        ContextMenu.Add("", null);
        ContextMenu.Add(_options["markers_manager"]);
        ContextMenu.Add(_options["add_marker_on_player"]);
        ContextMenu.Add("", null);
        ContextMenu.Add(_options["open_web_map"]);
        ContextMenu.Add(_options["auto_start_web_map"]);
        ContextMenu.Add(_options["reset_map_cache"]);
        ContextMenu.Add(_options["saveclose"]);
    }


    #region Update

    public override void Update()
    {
        base.Update();

        if (IsDisposed)
        {
            return;
        }

        if (_map.Index != World.MapIndex && !_freeView)
            ChangeMap(World.MapIndex);

        // Drop cached TTF TextBoxes that haven't been drawn recently so names/markers that
        // leave the view (or change) don't leak their layouts.
        if (_ttfTextBoxes.Count > 0)
            PurgeTtfTextBoxes();

        if (_isFullscreen)
        {
            // Keep the map filling the client window if it gets resized while fullscreen.
            int targetW = Client.Game.Window.ClientBounds.Width;
            int targetH = Client.Game.Window.ClientBounds.Height;

            if (Width != targetW || Height != targetH || X != 0 || Y != 0)
            {
                X = 0;
                Y = 0;
                ApplySize(targetW, targetH);
            }
        }

        World.WMapManager.RequestServerPartyGuildInfo();
    }

    public void ChangeMap(int index)
    {
        ClearMapCache();
        Client.Game.UO.FileManager.Maps.LoadMap(index, World.ClientFeatures.Flags.HasFlag(CharacterListFlags.CLF_UNLOCK_FELUCCA_AREAS));
        _map = new Map.Map(World, index);


        if(World.InGame)
        {
            if (_loadingTask is { Status: TaskStatus.Running })
                _loadingTask = _loadingTask.ContinueWith(_ => LoadMap(index, _map, _world));
            else
                _loadingTask = Task.Run(() => LoadMap(index, _map, _world));
        }
    }

    #endregion


    private Point RotatePoint(int x, int y, float zoom, int dist, float angle = 45f)
    {
        x = (int)(x * zoom);
        y = (int)(y * zoom);

        if (angle == 0.0f)
        {
            return new Point(x, y);
        }

        double cos = Math.Cos(dist * Math.PI / 4.0);
        double sin = Math.Sin(dist * Math.PI / 4.0);

        return new Point((int)Math.Round(cos * x - sin * y), (int)Math.Round(sin * x + cos * y));
    }

    private void AdjustPosition
    (
        int x,
        int y,
        int centerX,
        int centerY,
        out int newX,
        out int newY
    )
    {
        int offset = GetOffset(x, y, centerX, centerY);
        int currX = x;
        int currY = y;

        while (offset != 0)
        {
            if ((offset & 1) != 0)
            {
                currY = centerY;
                currX = x * currY / y;
            }
            else if ((offset & 2) != 0)
            {
                currY = -centerY;
                currX = x * currY / y;
            }
            else if ((offset & 4) != 0)
            {
                currX = centerX;
                currY = y * currX / x;
            }
            else if ((offset & 8) != 0)
            {
                currX = -centerX;
                currY = y * currX / x;
            }

            x = currX;
            y = currY;
            offset = GetOffset(x, y, centerX, centerY);
        }

        newX = x;
        newY = y;
    }

    private void CanvasToWorld
    (
        int a_x,
        int a_y,
        out int out_x,
        out int out_y
    )
    {
        // Scale width to Zoom
        float newWidth = Width / Zoom;
        float newHeight = Height / Zoom;

        // Scale mouse cords to Zoom
        float newX = a_x / Zoom;
        float newY = a_y / Zoom;

        // Rotate Cords if map fliped
        // x' = (x + y)/Sqrt(2)
        // y' = (y - x)/Sqrt(2)
        if (_flipMap)
        {
            float nw = (newWidth + newHeight) / 1.41f;
            float nh = (newHeight - newWidth) / 1.41f;
            newWidth = (int)nw;
            newHeight = (int)nh;

            float nx = (newX + newY) / 1.41f;
            float ny = (newY - newX) / 1.41f;
            newX = (int)nx;
            newY = (int)ny;
        }

        // Calulate Click cords to Map Cords
        // (x,y) = MapCenter - ScaeldMapWidth/2 + ScaledMouseCords
        out_x = _center.X - (int)(newWidth / 2) + (int)newX;
        out_y = _center.Y - (int)(newHeight / 2) + (int)newY;
    }

    private int GetOffset(int x, int y, int centerX, int centerY)
    {
        const int OFFSET = 0;

        if (y > centerY)
        {
            return 1;
        }

        if (y < -centerY)
        {
            return 2;
        }

        if (x > centerX)
        {
            return OFFSET + 4;
        }

        if (x >= -centerX)
        {
            return OFFSET;
        }

        return OFFSET + 8;
    }

    internal void HandlePositionTarget()
    {
        Point position = Mouse.Position;
        int x = position.X - X - ParentX;
        int y = position.Y - Y - ParentY;
        CanvasToWorld(x, y, out int xMap, out int yMap);
        World.TargetManager.Target
        (
            0,
            (ushort)xMap,
            (ushort)yMap,
            _map.GetTileZ(xMap, yMap)
        );
    }

    public override void Dispose()
    {
        SaveSettings();
        World.WMapManager.SetEnable(false);

        Client.Game.UO.GameCursor.IsDraggingCursorForced = false;

        // Stop any in-progress WorldMap navigation so the step-fail hook can't fire
        // against this gump after it's been disposed.
        if (_world?.Player?.Pathfinder != null)
        {
            if (_navStepFailedHandler != null)
                _world.Player.Pathfinder.OnComputedPathStepFailed -= _navStepFailedHandler;
            _world.Player.Pathfinder.StopAutoWalk();
        }
        _navStepFailedHandler = null;
        _navDest = null;
        _navPath = null;
        _navSegments = 0;

        // Dispose every cached TextBox so their layouts don't outlive the map gump.
        PurgeTtfTextBoxes(true);

        base.Dispose();
    }

    private void SetFont(int fontIndex)
    {
        _markerFontIndex = fontIndex;

        // Choosing a sprite-font style disables any active TTF font so both name and
        // marker rendering fall back to the shared DrawString path.
        if (UseTtfFont)
        {
            _ttfFont = string.Empty;
            PurgeTtfTextBoxes(true);
        }

        switch (fontIndex)
        {
            case 1:
                _markerFont = Fonts.Map1;

                break;

            case 2:
                _markerFont = Fonts.Map2;

                break;

            case 3:
                _markerFont = Fonts.Map3;

                break;

            case 4:
                _markerFont = Fonts.Map4;

                break;

            case 5:
                _markerFont = Fonts.Map5;

                break;

            case 6:
                _markerFont = Fonts.Map6;

                break;

            default:
                _markerFontIndex = 1;
                _markerFont = Fonts.Map1;

                break;
        }
    }

    /// <summary>
    /// Builds the "TTF Fonts" context menu. When a TTF font is selected every name and marker
    /// is rendered through TextBox (TrueType) instead of the sprite-font DrawString methods.
    /// The top of the menu holds a size increase/decrease submenu.
    /// </summary>
    private ContextMenuItemEntry BuildTtfFontMenu()
    {
        var ttfEntry = new ContextMenuItemEntry(TazLang.Get("map_ttf_fonts", "TTF Fonts"));

        var sizeEntry = new ContextMenuItemEntry(string.Format(TazLang.Get("map_ttf_font_size", "Font Size: {0}"), _ttfFontSize));
        sizeEntry.Add(new ContextMenuItemEntry(TazLang.Get("map_ttf_font_size_inc", "Increase (+)"), () => AdjustTtfFontSize(2)));
        sizeEntry.Add(new ContextMenuItemEntry(TazLang.Get("map_ttf_font_size_dec", "Decrease (-)"), () => AdjustTtfFontSize(-2)));
        ttfEntry.Add(sizeEntry);

        ttfEntry.Add(new ContextMenuItemEntry(""));

        // Turns TTF rendering off and returns to the sprite-font styles.
        ttfEntry.Add(new ContextMenuItemEntry(TazLang.Get("map_ttf_font_none", "None (use font style)"), () => SetTtfFont(string.Empty), true, !UseTtfFont));

        (string[] fontNames, _) = TrueTypeLoader.Instance.GetSortedFontNames();

        foreach (string fontName in fontNames)
        {
            string name = fontName;
            ttfEntry.Add(new ContextMenuItemEntry(name, () => SetTtfFont(name), true, string.Equals(_ttfFont, name, StringComparison.Ordinal)));
        }

        return ttfEntry;
    }

    private void SetTtfFont(string fontName)
    {
        fontName ??= string.Empty;

        if (string.Equals(_ttfFont, fontName, StringComparison.Ordinal))
            return;

        _ttfFont = fontName;
        PurgeTtfTextBoxes(true); // Cached boxes are baked with the old font, drop them all.
        SaveSettings();
    }

    private void AdjustTtfFontSize(int delta)
    {
        int newSize = Math.Clamp(_ttfFontSize + delta, TTF_FONT_SIZE_MIN, TTF_FONT_SIZE_MAX);

        if (newSize == _ttfFontSize)
            return;

        _ttfFontSize = newSize;
        PurgeTtfTextBoxes(true); // Size is baked into the layout, drop and rebuild on demand.
        SaveSettings();
        BuildContextMenu(); // Refresh the "Font Size: X" label.
    }

    /// <summary>
    /// Returns a cached TextBox for <paramref name="text"/>, creating one if necessary, and marks
    /// it as used this frame so it survives the next purge.
    /// </summary>
    private TextBox GetTtfTextBox(string text)
    {
        if (!_ttfTextBoxes.TryGetValue(text, out TextBox tb) || tb.IsDisposed)
        {
            tb = TextBox.GetOne
            (
                text,
                _ttfFont,
                _ttfFontSize,
                Color.White,
                new TextBox.RTLOptions { ConvertHtmlColors = false, SupportsCommands = false }
            );

            _ttfTextBoxes[text] = tb;
        }

        _ttfTextBoxLastUse[text] = Time.Ticks;

        return tb;
    }

    /// <summary>
    /// Disposes cached TextBoxes. When <paramref name="all"/> is true every box is disposed,
    /// otherwise only those not drawn within <see cref="TTF_TEXTBOX_TTL"/> milliseconds.
    /// </summary>
    private void PurgeTtfTextBoxes(bool all = false)
    {
        if (_ttfTextBoxes.Count == 0)
            return;

        List<string> toRemove = null;

        foreach (KeyValuePair<string, TextBox> kv in _ttfTextBoxes)
        {
            long lastUse = _ttfTextBoxLastUse.TryGetValue(kv.Key, out long t) ? t : 0;

            if (all || Time.Ticks - lastUse > TTF_TEXTBOX_TTL)
            {
                (toRemove ??= new List<string>()).Add(kv.Key);
            }
        }

        if (toRemove == null)
            return;

        foreach (string key in toRemove)
        {
            if (_ttfTextBoxes.TryGetValue(key, out TextBox tb))
                tb.Dispose();

            _ttfTextBoxes.Remove(key);
            _ttfTextBoxLastUse.Remove(key);
        }
    }

    private bool GetOptionValue(string key)
    {
        _options.TryGetValue(key, out ContextMenuItemEntry v);

        return v != null && v.IsSelected;
    }

    public void SetOptionValue(string key, bool v)
    {
        if (_options.TryGetValue(key, out ContextMenuItemEntry entry) && entry != null)
        {
            entry.IsSelected = v;
        }
    }


    public class WMapMarker
    {
        public string Name { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int MapId { get; set; }
        public Color Color { get; set; }
        public Texture2D MarkerIcon { get; set; }
        public string MarkerIconName { get; set; }
        public int ZoomIndex { get; set; }
        public string ColorName { get; set; }
    }

    public class WMapMarkerFile
    {
        public string Name { get; set; }
        public string FullPath { get; set; }
        public List<WMapMarker> Markers { get; set; }
        public bool Hidden { get; set; }
        public bool IsEditable { get; set; }
    }

    private class CurLoader
    {
        public static unsafe Texture2D CreateTextureFromICO_Cur(Stream stream)
        {
            byte[] buffer = System.Buffers.ArrayPool<byte>.Shared.Rent((int)stream.Length);

            try
            {
                stream.ReadExactly(buffer, 0, (int)stream.Length);

                var reader = new StackDataReader(buffer.AsSpan(0, (int)stream.Length));

                int bmp_pitch;
                int i, pad;
                SDL.SDL_Surface* surface;
                byte* bits;
                int expand_bmp;
                int max_col = 0;
                uint ico_of_s = 0;
                uint* palette = stackalloc uint[256];

                ushort bf_reserved, bf_type, bf_count;
                uint bi_size, bi_width, bi_height;
                ushort bi_planes, bi_bit_count;

                uint bi_compression, bi_size_image, bi_x_perls_per_meter, bi_y_perls_per_meter, bi_clr_used, bi_clr_important;

                bf_reserved = reader.ReadUInt16LE();
                bf_type = reader.ReadUInt16LE();
                bf_count = reader.ReadUInt16LE();

                for (i = 0; i < bf_count; i++)
                {
                    int b_width = reader.ReadUInt8();
                    int b_height = reader.ReadUInt8();
                    int b_color_count = reader.ReadUInt8();
                    byte b_reserver = reader.ReadUInt8();
                    ushort w_planes = reader.ReadUInt16LE();
                    ushort w_bit_count = reader.ReadUInt16LE();
                    uint dw_bytes_in_res = reader.ReadUInt32LE();
                    uint dw_image_offse = reader.ReadUInt32LE();

                    if (b_width == 0)
                    {
                        b_width = 256;
                    }

                    if (b_height == 0)
                    {
                        b_height = 256;
                    }

                    if (b_color_count == 0)
                    {
                        b_color_count = 256;
                    }

                    if (b_color_count > max_col)
                    {
                        max_col = b_color_count;
                        ico_of_s = dw_image_offse;
                    }
                }

                reader.Seek(ico_of_s);

                bi_size = reader.ReadUInt32LE();

                if (bi_size == 40)
                {
                    bi_width = reader.ReadUInt32LE();
                    bi_height = reader.ReadUInt32LE();
                    bi_planes = reader.ReadUInt16LE();
                    bi_bit_count = reader.ReadUInt16LE();
                    bi_compression = reader.ReadUInt32LE();
                    bi_size_image = reader.ReadUInt32LE();
                    bi_x_perls_per_meter = reader.ReadUInt32LE();
                    bi_y_perls_per_meter = reader.ReadUInt32LE();
                    bi_clr_used = reader.ReadUInt32LE();
                    bi_clr_important = reader.ReadUInt32LE();
                }
                else
                {
                    return null;
                }

                const int BI_RGB = 0;

                switch (bi_compression)
                {
                    case BI_RGB:

                        switch (bi_bit_count)
                        {
                            case 1:
                            case 4:
                                expand_bmp = bi_bit_count;
                                bi_bit_count = 8;

                                break;

                            case 8:
                                expand_bmp = 8;

                                break;

                            case 32:
                                expand_bmp = 0;

                                break;

                            default: return null;
                        }

                        break;

                    default: return null;
                }


                bi_height >>= 1;

                // surface = (SDL.SDL_Surface*)SDL.SDL_CreateRGBSurface
                // (
                //     0,
                //     (int)bi_width,
                //     (int)bi_height,
                //     32,
                //     0x00FF0000,
                //     0x0000FF00,
                //     0x000000FF,
                //     0xFF000000
                // );
                //Pretty sure its abgr8888
                surface = (SDL.SDL_Surface*)SDL.SDL_CreateSurface((int)bi_width, (int)bi_height, SDL.SDL_PixelFormat.SDL_PIXELFORMAT_ABGR8888);
                // If issues arrise later, change to this and convert back down lower in method:
                //surface = (SDL.SDL_Surface*)SDL.SDL_CreateSurface((int)bi_width, (int)bi_height, SDL.SDL_PixelFormat.SDL_PIXELFORMAT_BGRA8888);

                if (bi_bit_count <= 8)
                {
                    if (bi_clr_used == 0)
                    {
                        bi_clr_used = (uint)(1 << bi_bit_count);
                    }

                    for (i = 0; i < bi_clr_used; i++)
                    {
                        palette[i] = reader.ReadUInt32LE();
                    }
                }

                bits = (byte*)(surface->pixels + surface->h * surface->pitch);

                switch (expand_bmp)
                {
                    case 1:
                        bmp_pitch = (int)(bi_width + 7) >> 3;
                        pad = bmp_pitch % 4 != 0 ? 4 - bmp_pitch % 4 : 0;

                        break;

                    case 4:
                        bmp_pitch = (int)(bi_width + 1) >> 1;
                        pad = bmp_pitch % 4 != 0 ? 4 - bmp_pitch % 4 : 0;

                        break;

                    case 8:
                        bmp_pitch = (int)bi_width;
                        pad = bmp_pitch % 4 != 0 ? 4 - bmp_pitch % 4 : 0;

                        break;

                    default:
                        bmp_pitch = (int)bi_width * 4;
                        pad = 0;

                        break;
                }


                while (bits > (byte*)surface->pixels)
                {
                    bits -= surface->pitch;

                    switch (expand_bmp)
                    {
                        case 1:
                        case 4:
                        case 8:
                            {
                                byte pixel = 0;
                                int shift = 8 - expand_bmp;

                                for (i = 0; i < surface->w; i++)
                                {
                                    if (i % (8 / expand_bmp) == 0)
                                    {
                                        pixel = reader.ReadUInt8();
                                    }

                                    *((uint*)bits + i) = palette[pixel >> shift];
                                    pixel <<= expand_bmp;
                                }
                            }

                            break;

                        default:

                            for (int k = 0; k < surface->pitch; k++)
                            {
                                bits[k] = reader.ReadUInt8();
                            }

                            break;
                    }

                    if (pad != 0)
                    {
                        for (i = 0; i < pad; i++)
                        {
                            reader.ReadUInt8();
                        }
                    }
                }


                bits = (byte*)(surface->pixels + surface->h * surface->pitch);
                expand_bmp = 1;
                bmp_pitch = (int)(bi_width + 7) >> 3;
                pad = bmp_pitch % 4 != 0 ? 4 - bmp_pitch % 4 : 0;

                while (bits > (byte*)surface->pixels)
                {
                    byte pixel = 0;
                    int shift = 8 - expand_bmp;

                    bits -= surface->pitch;

                    for (i = 0; i < surface->w; i++)
                    {
                        if (i % (8 / expand_bmp) == 0)
                        {
                            pixel = reader.ReadUInt8();
                        }

                        *((uint*)bits + i) |= pixel >> shift != 0 ? 0 : 0xFF000000;

                        pixel <<= expand_bmp;
                    }

                    if (pad != 0)
                    {
                        for (i = 0; i < pad; i++)
                        {
                            reader.ReadUInt8();
                        }
                    }
                }

                //Since it is intially created as argb8888 I don't think we need this
                //surface = (SDL.SDL_Surface*)INTERNAL_convertSurfaceFormat((IntPtr)surface);

                int len = surface->w * surface->h * 4;
                byte* pixels = (byte*)surface->pixels;

                for (i = 0; i < len; i += 4, pixels += 4)
                {
                    if (pixels[3] == 0)
                    {
                        pixels[0] = 0;
                        pixels[1] = 0;
                        pixels[2] = 0;
                    }
                }

                var texture = new Texture2D(Client.Game.GraphicsDevice, surface->w, surface->h);
                texture.SetDataPointerEXT(0, new Rectangle(0, 0, surface->w, surface->h), surface->pixels, len);

                //SDL.SDL_FreeSurface((IntPtr)surface);
                SDL.SDL_DestroySurface((IntPtr)surface);

                reader.Release();

                return texture;
            }
            finally
            {
                System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        // private static unsafe IntPtr INTERNAL_convertSurfaceFormat(IntPtr surface)
        // {
        //     IntPtr result = surface;
        //     SDL.SDL_Surface* surPtr = (SDL.SDL_Surface*)surface;
        //     SDL.SDL_PixelFormat* pixelFormatPtr = (SDL.SDL_PixelFormat*)surPtr->format;
        //
        //     // SurfaceFormat.Color is SDL_PIXELFORMAT_ABGR8888
        //     if (pixelFormatPtr->format != SDL.SDL_PIXELFORMAT_ABGR8888)
        //     {
        //         // Create a properly formatted copy, free the old surface
        //         result = SDL.SDL_ConvertSurfaceFormat(surface, SDL.SDL_PIXELFORMAT_ABGR8888, 0);
        //         SDL.SDL_FreeSurface(surface);
        //     }
        //
        //     return result;
        // }
    }

    #region Loading

    private static void LoadMap(int mapIndex, Map.Map map, World world)
    {
        if (mapIndex < 0 || mapIndex > MapLoader.MAPS_COUNT)
            return;

        _mapLoading = 1;

        // The PNG lock only guards the CPU-side generation. GPU operations
        // (Dispose + FromStream) are dispatched to the main/render thread AFTER the
        // lock is released, to avoid deadlock: holding the lock while blocking on
        // BubblingInvokeOnMainThread could deadlock if the main thread also tries to
        // acquire the same lock. _mapLoading stays 1 until the texture is recreated so
        // UpdateWorldMapChunk does not write into a texture that is being disposed.
        try
        {
            string fileMapPath;
            lock (Map.Map.GetMapPngLock())
            {
                fileMapPath = Map.Map.GenerateMapPng(mapIndex, map, world);
            }

            if (!string.IsNullOrEmpty(fileMapPath) && File.Exists(fileMapPath))
            {
                _mapPngFilePath = fileMapPath;
                MainThreadQueue.BubblingInvokeOnMainThread(() =>
                {
                    _mapTexture?.Dispose();
                    using FileStream stream = File.OpenRead(fileMapPath);
                    _mapTexture = Texture2D.FromStream(Client.Game.GraphicsDevice, stream);
                    GameActions.Print(ResGumps.WorldMapLoaded, 0x48);
                });
            }
            else
            {
                Log.Error($"Failed to generate map PNG for map {mapIndex}");
            }
        }
        catch (ThreadInterruptedException)
        {
        }
        finally
        {
            _mapLoading = 0;
        }
    }

    public unsafe Task UpdateWorldMapChunk(int mapBlockX, int mapBlockY, uint[] bufferBlock)
    {
        if (_mapLoading == 1 || _mapTexture == null || _mapTexture.IsDisposed)
        {
            return Task.CompletedTask;
        }

        return Task.Run
        (
            () =>
            {
                const int OFFSET_PIX = 2;
                const int OFFSET_PIX_HALF = OFFSET_PIX / 2;

                // Adjust map coordinates based on the block to reload
                // Multiply by 8 to get the actual map coordinate
                int startMapX = (mapBlockX << 3) + OFFSET_PIX_HALF;
                int startMapY = (mapBlockY << 3) + OFFSET_PIX_HALF;

                int blockWidth = 8;
                int blockHeight = 8;

                // Clamp block size if near the right or bottom border
                if (startMapX + blockWidth > _mapTexture.Width)
                    blockWidth = _mapTexture.Width - startMapX;
                if (startMapY + blockHeight > _mapTexture.Height)
                    blockHeight = _mapTexture.Height - startMapY;

                if (blockWidth > 0 && blockHeight > 0)
                {
                    fixed (uint* pixels = &bufferBlock[0])
                    {
                        _mapTexture.SetDataPointerEXT(0, new Rectangle(startMapX, startMapY, blockWidth, blockHeight), (IntPtr)pixels, sizeof(uint) * blockWidth * blockHeight);
                    }
                }
            }
        );
    }

    public static void ClearMapCache() => Map.Map.ClearMapPngCache();

    public static Texture2D GetMapTextureForMap() => _mapTexture;

    public static string GetMapPngPath() => _mapPngFilePath;

    public static async Task LoadMapTextureForMap(int mapIndex)
    {
        if (mapIndex < 0 || mapIndex > MapLoader.MAPS_COUNT)
        {
            Utility.Logging.Log.Warn($"Invalid map index for texture load: {mapIndex}");
            return;
        }

        if (!World.Instance.InGame)
        {
            Utility.Logging.Log.Warn("Cannot load map texture: not in game");
            return;
        }

        try
        {
            Utility.Logging.Log.Info($"Loading/generating map texture for map {mapIndex}...");

            // Generate the PNG file on a background thread using Map.GenerateMapPng
            string generatedMapPath = await Task.Run(() =>
            {
                var map = new Map.Map(World.Instance, mapIndex);
                return Map.Map.GenerateMapPng(mapIndex, map, World.Instance);
            });

            // Now load the texture from the generated PNG on the main thread.
            // After Task.Run above, we're on a thread pool thread, so dispatch back.
            if (!string.IsNullOrEmpty(generatedMapPath) && File.Exists(generatedMapPath))
            {
                _mapPngFilePath = generatedMapPath;
                MainThreadQueue.BubblingInvokeOnMainThread(() =>
                {
                    _mapTexture?.Dispose();
                    using FileStream stream = File.OpenRead(generatedMapPath);
                    _mapTexture = Texture2D.FromStream(Client.Game.GraphicsDevice, stream);
                    Utility.Logging.Log.Info($"Map texture loaded successfully for map {mapIndex}");
                });
            }
            else
            {
                Utility.Logging.Log.Error($"Failed to generate map texture for map {mapIndex}");
            }
        }
        catch (Exception ex)
        {
            Utility.Logging.Log.Error($"Failed to load map texture for map {mapIndex}: {ex.Message}");
        }
    }

    public class ZonesFileZoneData
    {
        public string Label { get; set; }

        public string Color { get; set; }

        public List<List<int>> Polygon { get; set; }
    }

    public class ZonesFile
    {
        public int MapIndex { get; set; }
        public List<ZonesFileZoneData> Zones { get; set; }
    }

    private class Zone
    {
        public string Label;
        public Color Color;
        public Rectangle BoundingRectangle;
        public List<Point> Vertices;

        public Zone(ZonesFileZoneData data)
        {
            Label = data.Label;
            Color = _colorMap[data.Color];

            Vertices = new List<Point>();

            int xmin = int.MaxValue;
            int xmax = int.MinValue;
            int ymin = int.MaxValue;
            int ymax = int.MinValue;

            foreach (List<int> rawPoint in data.Polygon)
            {
                var p = new Point(rawPoint[0], rawPoint[1]);

                if (p.X < xmin) xmin = p.X;
                if (p.X > xmax) xmax = p.X;
                if (p.Y < ymin) ymin = p.Y;
                if (p.Y > ymax) ymax = p.Y;

                Vertices.Add(p);
            }

            BoundingRectangle = new Rectangle(xmin, ymin, xmax - xmin, ymax - ymin);
        }
    }

    private class ZoneSet
    {
        public int MapIndex;
        public List<Zone> Zones = new List<Zone>();
        public bool Hidden = false;
        public string NiceFileName;

        public ZoneSet(ZonesFile zf, string filename, bool hidden)
        {
            MapIndex = zf.MapIndex;
            foreach (ZonesFileZoneData data in zf.Zones)
            {
                Zones.Add(new Zone(data));
            }

            Hidden = hidden;
            NiceFileName = MakeNiceFileName(filename);
        }

        public static string MakeNiceFileName(string filename) =>
            // Yes, we invoke the same method twice, because our filenames have two layers of extension
            // we want to strip off (.zones.json)
            Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(filename));
    }

    private class ZoneSets
    {
        public Dictionary<string, ZoneSet> ZoneSetDict { get; } = new Dictionary<string, ZoneSet>();

        public void AddZoneSetByFileName(World world, string filename, bool hidden)
        {
            try
            {
                ZonesFile zf = System.Text.Json.JsonSerializer.Deserialize(File.ReadAllText(filename), ZonesJsonContext.Default.ZonesFile);
                ZoneSetDict[filename] = new ZoneSet(zf, filename, hidden);
                GameActions.Print(world, string.Format(ResGumps.MapZoneFileLoaded, ZoneSetDict[filename].NiceFileName), 0x3A /* yellow green */);
            }
            catch (Exception ee)
            {
                Log.Error($"{ee}");
                if (CUOEnviroment.Debug)
                {
                    GameActions.Print(world, ee.ToString());
                }
            }
        }

        public IEnumerable<Zone> GetZonesForMapIndex(int mapIndex)
        {
            foreach (KeyValuePair<string, ZoneSet> entry in ZoneSetDict)
            {
                if (entry.Value.MapIndex != mapIndex)
                    continue;
                else if (entry.Value.Hidden)
                    continue;

                foreach (Zone zone in entry.Value.Zones)
                {
                    yield return zone;
                }
            }
        }

        public void Clear() => ZoneSetDict.Clear();
    }

    private void LoadZones()
    {
        Log.Trace("LoadZones()...");

        _zoneSets.Clear();

            List<string> zonefiles = new();
            foreach (string s in _mapFilesPath)
            {
                if(Directory.Exists(s))
                    zonefiles.AddRange(Directory.GetFiles(s, "*.zones.json"));
            }

        foreach (string filename in zonefiles)
        {
            bool shouldHide = !string.IsNullOrEmpty
            (
                _hiddenZoneFiles.FirstOrDefault(x => x.Contains(filename))
            );

            _zoneSets.AddZoneSetByFileName(World, filename, shouldHide);
        }
    }

    private bool ShouldDrawGrid() => (_showGridIfZoomed && Zoom >= 4);

    private void LoadMarkers()
    {
        //return Task.Run(() =>
        {
            if (World.InGame)
            {
                _mapMarkersLoaded = false;

                GameActions.Print(World, ResGumps.LoadingWorldMapMarkers, 0x2A);

                foreach (Texture2D t in _markerIcons.Values)
                {
                    if (t != null && !t.IsDisposed)
                    {
                        t.Dispose();
                    }
                }

                if (!File.Exists(UserMarkersFilePath))
                {
                    using (File.Create(UserMarkersFilePath)) { }
                }

                _markerIcons.Clear();
                _markerIconPaths.Clear();

                    List<string> mapIconPaths = new();
                    List<string> mapIconPathsPngJpg = new();
                    foreach (string s in _mapIconsPath)
                    {
                        bool add = Directory.Exists(s);
                        if (!add)
                        {
                            try
                            {
                                Directory.CreateDirectory(s);
                                add = true;
                            } catch { }
                        }

                        if (!add) continue;

                        mapIconPaths.AddRange(Directory.GetFiles(s, "*.cur"));
                        mapIconPaths.AddRange(Directory.GetFiles(s, "*.ico"));
                        mapIconPathsPngJpg.AddRange(Directory.GetFiles(s, "*.png"));
                        mapIconPathsPngJpg.AddRange(Directory.GetFiles(s, "*.jpg"));
                    }

                    foreach (string icon in mapIconPaths)
                    {
                        var fs = new FileStream(icon, FileMode.Open, FileAccess.Read);
                        var ms = new MemoryStream();
                        fs.CopyTo(ms);
                        ms.Seek(0, SeekOrigin.Begin);

                    try
                    {
                        Texture2D texture = CurLoader.CreateTextureFromICO_Cur(ms);

                        string iconKey = Path.GetFileNameWithoutExtension(icon).ToLower();
                        _markerIcons.Add(iconKey, texture);
                        _markerIconPaths[iconKey] = icon;
                    }
                    catch (Exception ee)
                    {
                        Log.Error($"{ee}");
                    }
                    finally
                    {
                        ms.Dispose();
                        fs.Dispose();
                    }
                }

                    foreach (string icon in mapIconPathsPngJpg)
                    {
                        var fs = new FileStream(icon, FileMode.Open, FileAccess.Read);
                        var ms = new MemoryStream();
                        fs.CopyTo(ms);
                        ms.Seek(0, SeekOrigin.Begin);

                    try
                    {
                        var texture = Texture2D.FromStream(Client.Game.GraphicsDevice, ms);

                        string iconKey = Path.GetFileNameWithoutExtension(icon).ToLower();
                        _markerIcons.Add(iconKey, texture);
                        _markerIconPaths[iconKey] = icon;
                    }
                    catch (Exception ee)
                    {
                        Log.Error($"{ee}");
                    }
                    finally
                    {
                        ms.Dispose();
                        fs.Dispose();
                    }
                }

                    List<string> mapFiles = new(){UserMarkersFilePath};

                    foreach (string s in _mapFilesPath)
                    {
                        bool add = Directory.Exists(s);
                        if (!add)
                        {
                            try
                            {
                                Directory.CreateDirectory(s);
                                add = true;
                            } catch { }
                        }

                        if (!add) continue;

                        mapFiles.AddRange(Directory.GetFiles(s, "*.map"));
                        mapFiles.AddRange(Directory.GetFiles(s, "*.csv"));
                        mapFiles.AddRange(Directory.GetFiles(s, "*.xml"));
                    }

                _markerFiles.Clear();

                foreach (string mapFile in mapFiles)
                {
                    if (File.Exists(mapFile))
                    {
                        var markerFile = new WMapMarkerFile
                        {
                            Hidden = false,
                            Name = Path.GetFileNameWithoutExtension(mapFile),
                            FullPath = mapFile,
                            Markers = new List<WMapMarker>(),
                            IsEditable = false,
                        };

                        string hiddenFile = _hiddenMarkerFiles.FirstOrDefault(x => x.Contains(markerFile.Name));

                        if (!string.IsNullOrEmpty(hiddenFile))
                        {
                            markerFile.Hidden = true;
                        }

                        if (mapFile != null && Path.GetExtension(mapFile).ToLower().Equals(".xml")) // Ultima Mapper
                        {
                            using (var reader = new XmlTextReader(File.Open(mapFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
                            {
                                while (reader.Read())
                                {
                                    if (reader.Name.Equals("Marker"))
                                    {
                                        var marker = new WMapMarker
                                        {
                                            X = int.Parse(reader.GetAttribute("X")),
                                            Y = int.Parse(reader.GetAttribute("Y")),
                                            Name = reader.GetAttribute("Name"),
                                            MapId = int.Parse(reader.GetAttribute("Facet")),
                                            Color = Color.White,
                                            ZoomIndex = 3
                                        };

                                        if (_markerIcons.TryGetValue(reader.GetAttribute("Icon").ToLower(), out Texture2D value))
                                        {
                                            marker.MarkerIcon = value;

                                            marker.MarkerIconName = reader.GetAttribute("Icon").ToLower();
                                        }

                                        markerFile.Markers.Add(marker);
                                    }
                                }

                            }
                        }
                        else if (mapFile != null && Path.GetExtension(mapFile).ToLower().Equals(".map")) //UOAM
                        {
                            using (var reader = new StreamReader(File.Open(mapFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
                            {
                                while (!reader.EndOfStream)
                                {
                                    string line = reader.ReadLine();

                                    // ignore empty lines, and if UOAM, ignore the first line that always has a 3
                                    if (string.IsNullOrEmpty(line) || line.Equals("3"))
                                    {
                                        continue;
                                    }

                                    // Check for UOAM file
                                    if (line.Substring(0, 1).Equals("+") || line.Substring(0, 1).Equals("-"))
                                    {
                                        string icon = line.Substring(1, line.IndexOf(':') - 1);

                                        line = line.Substring(line.IndexOf(':') + 2);

                                        string[] splits = line.Split(' ');

                                        if (splits.Length <= 1)
                                        {
                                            continue;
                                        }

                                        var marker = new WMapMarker
                                        {
                                            X = int.Parse(splits[0]),
                                            Y = int.Parse(splits[1]),
                                            MapId = int.Parse(splits[2]),
                                            Name = string.Join(" ", splits, 3, splits.Length - 3),
                                            Color = Color.White,
                                            ZoomIndex = 3
                                        };

                                        string[] iconSplits = icon.Split(' ');

                                        marker.MarkerIconName = iconSplits[0].ToLower();

                                        if (_markerIcons.TryGetValue(iconSplits[0].ToLower(), out Texture2D value))
                                        {
                                            marker.MarkerIcon = value;
                                        }

                                        markerFile.Markers.Add(marker);
                                    }
                                }
                            }
                        }
                        else if (mapFile != null && Path.GetExtension(mapFile).ToLower().Equals(".usr"))
                        {
                            markerFile.Markers = LoadUserMarkers();
                            markerFile.IsEditable = true;
                        }
                        else if (mapFile != null) //CSV x,y,mapindex,name of marker,iconname,color,zoom
                        {
                            using (var reader = new StreamReader(File.Open(mapFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
                            {
                                while (!reader.EndOfStream)
                                {
                                    string line = reader.ReadLine();

                                    if (string.IsNullOrEmpty(line))
                                    {
                                        return;
                                    }

                                    string[] splits = line.Split(',');

                                    if (splits.Length <= 1)
                                    {
                                        continue;
                                    }

                                    var marker = new WMapMarker
                                    {
                                        X = int.Parse(splits[0]),
                                        Y = int.Parse(splits[1]),
                                        MapId = int.Parse(splits[2]),
                                        Name = splits[3],
                                        MarkerIconName = splits[4].ToLower(),
                                        Color = GetColor(splits[5]),
                                        ZoomIndex = splits.Length == 7 ? int.Parse(splits[6]) : 3
                                    };

                                    if (_markerIcons.TryGetValue(splits[4].ToLower(), out Texture2D value))
                                    {
                                        marker.MarkerIcon = value;
                                    }

                                    markerFile.Markers.Add(marker);
                                }
                            }
                        }

                        if (markerFile.Markers.Count > 0)
                        {
                            GameActions.Print(World, $"..{Path.GetFileName(mapFile)} ({markerFile.Markers.Count})", 0x2B);
                        }
                        _markerFiles.Add(markerFile);
                    }
                }

                BuildContextMenu();

                int count = 0;

                foreach (WMapMarkerFile file in _markerFiles)
                {
                    count += file.Markers.Count;
                }

                _mapMarkersLoaded = true;

                GameActions.Print(World, string.Format(ResGumps.WorldMapMarkersLoaded0, count), 0x2A);
            }
        }

        //);
    }

    private void AddMarkerOnPlayer()
    {
        if (!World.InGame)
        {
            return;
        }

        var entryDialog = new EntryDialog(World, 250, 150, ResGumps.EnterMarkerName, SaveMakerOnPlayer)
        {
            CanCloseWithRightClick = true
        };

        UIManager.Add(entryDialog);
    }

    private void SaveMakerOnPlayer(string markerName)
    {
        if (!World.InGame)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(markerName))
        {
            GameActions.Print(World, ResGumps.InvalidMarkerName, 0x2A);
        }

        string markerColor = "blue";
        string markerIcon = "";
        int markerZoomLevel = 3;

        string markerCsv = $"{World.Player.X},{World.Player.Y},{_map.Index},{markerName},{markerIcon},{markerColor},{markerZoomLevel}";

        using (FileStream fileStream = File.Open(UserMarkersFilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write))
        using (var streamWriter = new StreamWriter(fileStream))
        {
            streamWriter.BaseStream.Seek(0, SeekOrigin.End);
            streamWriter.WriteLine(markerCsv);
        }

        var mapMarker = new WMapMarker
        {
            X = World.Player.X,
            Y = World.Player.Y,
            Color = GetColor(markerColor),
            ColorName = markerColor,
            MapId = _map.Index,
            MarkerIconName = markerIcon,
            Name = markerName,
            ZoomIndex = markerZoomLevel
        };

        if (!string.IsNullOrWhiteSpace(mapMarker.MarkerIconName) && _markerIcons.TryGetValue(mapMarker.MarkerIconName, out Texture2D markerIconTexture))
        {
            mapMarker.MarkerIcon = markerIconTexture;
        }

        WMapMarkerFile mapMarkerFile = _markerFiles.FirstOrDefault(x => x.FullPath == UserMarkersFilePath);

        mapMarkerFile?.Markers.Add(mapMarker);
    }

    public void AddUserMarker(string markerName, int x, int y, int map, string color = "yellow")
    {
        if (!World.InGame)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(markerName))
        {
            GameActions.Print(_world, ResGumps.InvalidMarkerName, 0x2A);
            return;
        }

            try
            {
            string markerCsv = $"{x},{y},{map},{markerName}, ,{color},4";

                using (FileStream fileStream = File.Open(UserMarkersFilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write))
                using (var streamWriter = new StreamWriter(fileStream))
                {
                    streamWriter.BaseStream.Seek(0, SeekOrigin.End);
                    streamWriter.WriteLine(markerCsv);
                }
            }
            catch (Exception e)
            {
                Log.Error($"Error saving user marker: {e}");
                GameActions.Print(_world, "Failed to save user markers", 32);
            }

        var mapMarker = new WMapMarker
        {
            X = x,
            Y = y,
            Color = GetColor(color),
            ColorName = color,
            MapId = map,
            MarkerIconName = "",
            Name = markerName,
            ZoomIndex = 3
        };

        if (!string.IsNullOrWhiteSpace(mapMarker.MarkerIconName) && _markerIcons.TryGetValue(mapMarker.MarkerIconName, out Texture2D markerIconTexture))
        {
            mapMarker.MarkerIcon = markerIconTexture;
        }

        WMapMarkerFile mapMarkerFile = _markerFiles.FirstOrDefault(x => x.FullPath == UserMarkersFilePath);

            mapMarkerFile?.Markers.Add(mapMarker);
        }

        public void RemoveUserMarker(string markerName)
        {
            if (!World.InGame)
            {
                return;
            }

        WMapMarkerFile mapMarkerFile = _markerFiles.FirstOrDefault(x => x.FullPath == UserMarkersFilePath);

            if (mapMarkerFile == null)
                return;

            var markersToRemove = mapMarkerFile.Markers.Where(m => m.Name.Equals(markerName, StringComparison.Ordinal)).ToList();

             if (markersToRemove.Count == 0)
                 return;

             foreach (WMapMarker marker in markersToRemove)
             {
                 mapMarkerFile.Markers.Remove(marker);
             }

             try
             {
                 using (var writer = new StreamWriter(UserMarkersFilePath, false))
                 {
                     foreach (WMapMarker m in mapMarkerFile.Markers)
                     {
                    string newLine = $"{m.X},{m.Y},{m.MapId},{m.Name},{m.MarkerIconName},{m.ColorName},4";

                         writer.WriteLine(newLine);
                     }
                 }
             }
             catch (Exception e)
             {
                 Log.Error($"Error saving user marker: {e}");
                 GameActions.Print(_world, "Failed to save user markers", 32);
             }
        }

    /// <summary>
    /// Reload User Markers File after Changes
    /// </summary>
    internal static void ReloadUserMarkers()
    {
        WMapMarkerFile userFile = _markerFiles.FirstOrDefault(f => f.Name == USER_MARKERS_FILE);

        if (userFile == null)
        {
            return;
        }

        userFile.Markers = LoadUserMarkers();
    }

    /// <summary>
    /// Load User Markers to List of Markers
    /// </summary>
    /// <returns>List of loaded Markers</returns>
    internal static List<WMapMarker> LoadUserMarkers()
    {
        var tempList = new List<WMapMarker>();

        using (var reader = new StreamReader(UserMarkersFilePath))
        {
            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine();

                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }

                string[] splits = line.Split(',');

                if (splits.Length <= 1)
                {
                    continue;
                }
                tempList.Add(ParseMarker(splits));
            }
        }

        return tempList;
    }

    #endregion

    #region Draw

    public override bool Draw(UltimaBatcher2D batcher, int x, int y)
    {
        if (IsDisposed || !IsVisible || !World.InGame)
        {
            return false;
        }

        if (!_isScrolling && !_freeView)
        {
            if (following != null)
            {
                _center.X = following.X;
                _center.Y = following.Y;
            }
            else
            {
                _center.X = World.Player.X;
                _center.Y = World.Player.Y;
            }
        }


        int gX = x + 4;
        int gY = y + 4;
        int gWidth = Width - 8;
        int gHeight = Height - 8;

        int centerX = _center.X + 1;
        int centerY = _center.Y + 1;

        int size = (int)Math.Max(gWidth * 1.75f, gHeight * 1.75f);

        int size_zoom = (int)(size / Zoom);
        int size_zoom_half = size_zoom >> 1;

        int halfWidth = gWidth >> 1;
        int halfHeight = gHeight >> 1;

        Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);

        batcher.Draw
        (
            SolidColorTextureCache.GetTexture(Color.Black),
            new Rectangle
            (
                gX,
                gY,
                gWidth,
                gHeight
            ),
            hueVector
        );


        if (_mapLoading == 1)
        {
            if (batcher.ClipBegin(gX, gY, gWidth, gHeight))
            {
                ReadOnlySpan<char> str = "Please wait, I'm making the map file...".AsSpan();
                //str = str[..(str.Length - (int)_mapLoadingTime % 3)];

                //if (Time.Ticks > _mapLoadingTime)
                //    _mapLoadingTime = Time.Ticks + 1000;

                Vector2 strSize = Fonts.Bold.MeasureString(str);
                Vector2 pos = strSize * -0.5f;
                pos.X += gX + halfWidth;
                pos.Y += gY + halfHeight;
                batcher.DrawString(Fonts.Bold, str, pos, new Vector3(38, 1, 1));

                batcher.ClipEnd();
            }
        }
        else if (_mapTexture != null && !_mapTexture.IsDisposed)
        {
            if (batcher.ClipBegin(gX, gY, gWidth, gHeight))
            {
                var destRect = new Rectangle
                (
                    gX + halfWidth,
                    gY + halfHeight,
                    size,
                    size
                );

                var srcRect = new Rectangle
                (
                    centerX - size_zoom_half,
                    centerY - size_zoom_half,
                    size_zoom,
                    size_zoom
                );

                var origin = new Vector2
                (
                    srcRect.Width / 2f,
                    srcRect.Height / 2f
                );

                batcher.Draw
                (
                    _mapTexture,
                    destRect,
                    srcRect,
                    hueVector,
                    _flipMap ? Microsoft.Xna.Framework.MathHelper.ToRadians(45) : 0,
                    origin,
                    SpriteEffects.None,
                    0
                );

                DrawAll
                (
                    batcher,
                    srcRect,
                    gX,
                    gY,
                    halfWidth,
                    halfHeight
                );

                DrawNavDestination(batcher, gX, gY, halfWidth, halfHeight);

                batcher.ClipEnd();
            }
        }

        //foreach (House house in World.HouseManager.Houses)
        //{
        //    foreach (Multi multi in house.Components)
        //    {
        //        batcher.Draw2D(Textures.GetTexture())
        //    }
        //}


        return base.Draw(batcher, x, y);
    }

    private void DrawAll(UltimaBatcher2D batcher, Rectangle srcRect, int gX, int gY, int halfWidth, int halfHeight)
    {
        foreach (Zone zone in _zoneSets.GetZonesForMapIndex(_map.Index))
        {
            if (zone.BoundingRectangle.Intersects(srcRect))
            {
                DrawZone(batcher, zone, gX, gY, halfWidth, halfHeight, Zoom);
            }
        }

        if (_showMultis)
        {
            foreach (House house in World.HouseManager.Houses)
            {
                Item item = World.Items.Get(house.Serial);

                if (item != null)
                {
                    DrawMulti
                    (
                        batcher,
                        house,
                        item.X,
                        item.Y,
                        gX,
                        gY,
                        halfWidth,
                        halfHeight,
                        Zoom
                    );
                }
            }
        }

        if (_showMarkers && _mapMarkersLoaded)
        {
            WMapMarker lastMarker = null;

            foreach (WMapMarkerFile file in _markerFiles)
            {
                if (file.Hidden)
                {
                    continue;
                }

                foreach (WMapMarker marker in file.Markers)
                {
                    if (DrawMarker
                    (
                        batcher,
                        marker,
                        gX,
                        gY,
                        halfWidth,
                        halfHeight,
                        Zoom
                    ))
                    {
                        lastMarker = marker;
                    }
                }
            }

            if (lastMarker != null)
            {
                DrawMarkerString(batcher, lastMarker, gX, gY, halfWidth, halfHeight);
            }
        }

        if (_gotoMarker != null)
        {
            DrawMarker
            (
                batcher,
                _gotoMarker,
                gX,
                gY,
                halfWidth,
                halfHeight,
                Zoom
            );
            if (_gotoMarker.MapId == World.Map.Index)
            {
                Point pdrot = RotatePoint(_gotoMarker.X - _center.X, _gotoMarker.Y - _center.Y, Zoom, 1, _flipMap ? 45f : 0f);
                pdrot.X += gX + halfWidth;
                pdrot.Y += gY + halfHeight;

                Point prot = RotatePoint(World.Player.X - _center.X, World.Player.Y - _center.Y, Zoom, 1, _flipMap ? 45f : 0f);
                prot.X += gX + halfWidth;
                prot.Y += gY + halfHeight;

                batcher.DrawLine
                (
                   SolidColorTextureCache.GetTexture(Color.YellowGreen),
                   new Vector2(pdrot.X - 2, pdrot.Y - 2),
                   new Vector2(prot.X, prot.Y),
                   ShaderHueTranslator.GetHueVector(0),
                   1,
                   0f
                );
            }
        }

        if (_showMobiles)
        {
            foreach (Mobile mob in World.Mobiles.Values)
            {
                if (mob == World.Player)
                {
                    continue;
                }

                if (mob.NotorietyFlag != NotorietyFlag.Ally)
                {
                    DrawMobile
                    (
                        batcher,
                        mob,
                        gX,
                        gY,
                        halfWidth,
                        halfHeight,
                        Zoom,
                        Color.Red
                    );
                }
                else
                {
                    if (mob != null && mob.Distance <= World.ClientViewRange)
                    {
                        WMapEntity wme = World.WMapManager.GetEntity(mob);

                        if (wme != null)
                        {
                            if (string.IsNullOrEmpty(wme.Name) && !string.IsNullOrEmpty(mob.Name))
                            {
                                wme.Name = mob.Name;
                            }
                        }
                        else
                        {
                            DrawMobile
                            (
                                batcher,
                                mob,
                                gX,
                                gY,
                                halfWidth,
                                halfHeight,
                                Zoom,
                                Color.Lime,
                                true,
                                true,
                                _showGroupBar
                            );
                        }
                    }
                    else
                    {
                        WMapEntity wme = World.WMapManager.GetEntity(mob.Serial);

                        if (wme != null && wme.IsGuild)
                        {
                            DrawWMEntity
                            (
                                batcher,
                                wme,
                                gX,
                                gY,
                                halfWidth,
                                halfHeight,
                                Zoom
                            );
                        }
                    }
                }
            }
        }

        foreach (WMapEntity wme in World.WMapManager.Entities.Values)
        {
            if (wme.IsGuild && !World.Party.Contains(wme.Serial))
            {
                DrawWMEntity
                (
                    batcher,
                    wme,
                    gX,
                    gY,
                    halfWidth,
                    halfHeight,
                    Zoom
                );
            }
        }

        if (_showPartyMembers)
        {
            for (int i = 0; i < 10; i++)
            {
                PartyMember partyMember = World.Party.Members[i];

                if (partyMember != null && SerialHelper.IsValid(partyMember.Serial))
                {
                    Mobile mob = World.Mobiles.Get(partyMember.Serial);

                    if (mob != null && mob.Distance <= World.ClientViewRange)
                    {
                        WMapEntity wme = World.WMapManager.GetEntity(mob);

                        if (wme != null)
                        {
                            if (string.IsNullOrEmpty(wme.Name) && !string.IsNullOrEmpty(partyMember.Name))
                            {
                                wme.Name = partyMember.Name;
                            }
                        }

                        DrawMobile
                        (
                            batcher,
                            mob,
                            gX,
                            gY,
                            halfWidth,
                            halfHeight,
                            Zoom,
                            Color.Yellow,
                            _showGroupName,
                            true,
                            _showGroupBar
                        );
                    }
                    else
                    {
                        WMapEntity wme = World.WMapManager.GetEntity(partyMember.Serial);

                        if (wme != null && !wme.IsGuild)
                        {
                            DrawWMEntity
                            (
                                batcher,
                                wme,
                                gX,
                                gY,
                                halfWidth,
                                halfHeight,
                                Zoom
                            );
                        }
                    }
                }
            }
        }

        if (_showCorpse && World.WMapManager._corpse != null)
        {
            DrawWMEntity
                (
                    batcher,
                    World.WMapManager._corpse,
                    gX,
                    gY,
                    halfWidth,
                    halfHeight,
                    Zoom
                );
            if (World.WMapManager._corpse.Map == World.Map.Index)
            {
                Point pdrot = RotatePoint(World.WMapManager._corpse.X - _center.X, World.WMapManager._corpse.Y - _center.Y, Zoom, 1, _flipMap ? 45f : 0f);
                pdrot.X += gX + halfWidth;
                pdrot.Y += gY + halfHeight;

                Point prot = RotatePoint(World.Player.X - _center.X, World.Player.Y - _center.Y, Zoom, 1, _flipMap ? 45f : 0f);
                prot.X += gX + halfWidth;
                prot.Y += gY + halfHeight;

                batcher.DrawLine
                (
                   SolidColorTextureCache.GetTexture(Color.YellowGreen),
                   new Vector2(pdrot.X - 2, pdrot.Y - 2),
                   new Vector2(prot.X, prot.Y),
                   ShaderHueTranslator.GetHueVector(0),
                   1,
                   0f
                );
            }

        }

        if (_world.Player.Pathfinder.AutoWalking && World.Player.Pathfinder.PathSize > 0)
        {
            Point end = RotatePoint(World.Player.Pathfinder.EndPoint.X - _center.X, World.Player.Pathfinder.EndPoint.Y - _center.Y, Zoom, 1, _flipMap ? 45f : 0f);
            end.X += gX + halfWidth;
            end.Y += gY + halfHeight;
            Point start = RotatePoint(World.Player.X - _center.X, World.Player.Y - _center.Y, Zoom, 1, _flipMap ? 45f : 0f);
            start.X += gX + halfWidth;
            start.Y += gY + halfHeight;

            batcher.DrawLine(
                SolidColorTextureCache.GetTexture(Color.Green),
                new Vector2(end.X - 2, end.Y - 2),
                new Vector2(start.X, start.Y),
                ShaderHueTranslator.GetHueVector(0),
                1,
                0f
                );
        }

        DrawMobile
        (
            batcher,
            World.Player,
            gX,
            gY,
            halfWidth,
            halfHeight,
            Zoom,
            Color.White,
            _showPlayerName,
            false,
            _showPlayerBar
        );



        if (ShouldDrawGrid())
        {
            DrawGrid(batcher, srcRect, gX, gY, halfWidth, halfHeight, Zoom);
        }

        if (_showCoordinates)
        {
            string text = $"{World.Player.X}, {World.Player.Y}, {World.Player.Z} [{_zoomIndex}]";

            if (_showSextantCoordinates && Sextant.FormatString(new Point(World.Player.X, World.Player.Y), _map, out string sextantCoords))
                text += "\n" + sextantCoords;

            Vector3 hueVector = new(0f, 1f, 1f);

            batcher.DrawString(Fonts.Bold, text, gX + 6, gY + 6, hueVector);
            hueVector = ShaderHueTranslator.GetHueVector(0);
            batcher.DrawString(Fonts.Bold, text, gX + 5, gY + 5, hueVector);
        }

        if (_showMouseCoordinates && _lastMousePosition != null)
        {
            CanvasToWorld(_lastMousePosition.Value.X, _lastMousePosition.Value.Y, out int mouseWorldX, out int mouseWorldY);

            string mouseCoordinateString = $"{mouseWorldX} {mouseWorldY}";

            if (_showSextantCoordinates && Sextant.FormatString(new Point(mouseWorldX, mouseWorldY), _map, out string sextantCoords))
                mouseCoordinateString += "\n" + sextantCoords;

            Vector2 size = Fonts.Regular.MeasureString(mouseCoordinateString);
            int mx = gX + 5;
            int my = gY + Height - (int)Math.Ceiling(size.Y) - 15;

            Vector3 hueVector = new(0f, 1f, 1f);

            batcher.DrawString
            (
                Fonts.Bold,
                mouseCoordinateString,
                mx + 1,
                my + 1,
                hueVector
            );

            hueVector = ShaderHueTranslator.GetHueVector(0);

            batcher.DrawString
            (
                Fonts.Bold,
                mouseCoordinateString,
                mx,
                my,
                hueVector
            );
        }
    }

    private void DrawMobile
    (
        UltimaBatcher2D batcher,
        Mobile mobile,
        int x,
        int y,
        int width,
        int height,
        float zoom,
        Color color,
        bool drawName = false,
        bool isparty = false,
        bool drawHpBar = false
    )
    {
        Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);

        int sx = mobile.X - _center.X;
        int sy = mobile.Y - _center.Y;

        Point rot = RotatePoint
        (
            sx,
            sy,
            zoom,
            1,
            _flipMap ? 45f : 0f
        );

        AdjustPosition
        (
            rot.X,
            rot.Y,
            width - 4,
            height - 4,
            out rot.X,
            out rot.Y
        );

        rot.X += x + width;
        rot.Y += y + height;

        const int DOT_SIZE = 4;
        const int DOT_SIZE_HALF = DOT_SIZE >> 1;

        if (rot.X < x)
        {
            rot.X = x;
        }

        if (rot.X > x + Width - 8 - DOT_SIZE)
        {
            rot.X = x + Width - 8 - DOT_SIZE;
        }

        if (rot.Y < y)
        {
            rot.Y = y;
        }

        if (rot.Y > y + Height - 8 - DOT_SIZE)
        {
            rot.Y = y + Height - 8 - DOT_SIZE;
        }

        batcher.Draw
        (
            SolidColorTextureCache.GetTexture(color),
            new Rectangle
            (
                rot.X - DOT_SIZE_HALF,
                rot.Y - DOT_SIZE_HALF,
                DOT_SIZE,
                DOT_SIZE
            ),
            hueVector
        );

        if (drawName && !string.IsNullOrEmpty(mobile.Name))
        {
            TextBox ttfBox = UseTtfFont ? GetTtfTextBox(mobile.Name) : null;
            Vector2 size = ttfBox != null ? new Vector2(ttfBox.MeasuredSize.X, ttfBox.MeasuredSize.Y) : Fonts.Regular.MeasureString(mobile.Name);

            if (rot.X + size.X / 2 > x + Width - 8)
            {
                rot.X = x + Width - 8 - (int)(size.X / 2);
            }
            else if (rot.X - size.X / 2 < x)
            {
                rot.X = x + (int)(size.X / 2);
            }

            if (rot.Y + size.Y > y + Height)
            {
                rot.Y = y + Height - (int)size.Y;
            }
            else if (rot.Y - size.Y < y)
            {
                rot.Y = y + (int)size.Y;
            }

            int xx = (int)(rot.X - size.X / 2);
            int yy = (int)(rot.Y - size.Y);

            ushort nameHue = isparty ? (ushort)0x0034 : Notoriety.GetHue(mobile.NotorietyFlag);

            if (ttfBox != null)
            {
                ttfBox.Draw(batcher, xx + 1, yy + 1, Color.Black);
                ttfBox.Draw(batcher, xx, yy, TextBox.ConvertHueToColor(nameHue));
            }
            else
            {
                hueVector.X = 0;
                hueVector.Y = 1;

                batcher.DrawString
                (
                    Fonts.Regular,
                    mobile.Name,
                    xx + 1,
                    yy + 1,
                    hueVector
                );

                hueVector.X = nameHue;
                hueVector.Y = 1;
                hueVector.Z = 1;

                batcher.DrawString
                (
                    Fonts.Regular,
                    mobile.Name,
                    xx,
                    yy,
                    hueVector
                );
            }
        }

        if (drawHpBar)
        {
            int ww = mobile.HitsMax;

            if (ww > 0)
            {
                ww = mobile.Hits * 100 / ww;

                if (ww > 100)
                {
                    ww = 100;
                }
                else if (ww < 1)
                {
                    ww = 0;
                }
            }

            rot.Y += DOT_SIZE + 1;

            DrawHpBar(batcher, rot.X, rot.Y, ww);
        }
    }

    private bool DrawMarker
    (
        UltimaBatcher2D batcher,
        WMapMarker marker,
        int x,
        int y,
        int width,
        int height,
        float zoom
    )
    {
        if (marker.MapId != _map.Index)
        {
            return false;
        }

        if (_zoomIndex < marker.ZoomIndex && marker.Color == Color.Transparent)
        {
            return false;
        }

            Vector3 hueVector = ShaderHueTranslator.GetHueVector(0, false, 1f);

        int sx = marker.X - _center.X;
        int sy = marker.Y - _center.Y;

        Point rot = RotatePoint
        (
            sx,
            sy,
            zoom,
            1,
            _flipMap ? 45f : 0f
        );

        rot.X += x + width;
        rot.Y += y + height;

        const int DOT_SIZE = 4;
        const int DOT_SIZE_HALF = DOT_SIZE >> 1;

        if (rot.X < x || rot.X > x + Width - 8 - DOT_SIZE || rot.Y < y || rot.Y > y + Height - 8 - DOT_SIZE)
        {
            return false;
        }

        bool showMarkerName = _showMarkerNames && !string.IsNullOrEmpty(marker.Name) && _zoomIndex > 5;
        bool drawSingleName = false;

        if (_zoomIndex < marker.ZoomIndex || !_showMarkerIcons || marker.MarkerIcon == null)
        {
            batcher.Draw
            (
                SolidColorTextureCache.GetTexture(marker.Color),
                new Rectangle
                (
                    rot.X - DOT_SIZE_HALF,
                    rot.Y - DOT_SIZE_HALF,
                    DOT_SIZE,
                    DOT_SIZE
                ),
                hueVector
            );

            if (Mouse.Position.X >= rot.X - DOT_SIZE && Mouse.Position.X <= rot.X + DOT_SIZE_HALF &&
                Mouse.Position.Y >= rot.Y - DOT_SIZE && Mouse.Position.Y <= rot.Y + DOT_SIZE_HALF)
            {
                drawSingleName = true;
            }
        }
        else
        {
            batcher.Draw(marker.MarkerIcon, new Vector2(rot.X - (marker.MarkerIcon.Width >> 1), rot.Y - (marker.MarkerIcon.Height >> 1)), hueVector);

            if (!showMarkerName)
            {
                if (Mouse.Position.X >= rot.X - (marker.MarkerIcon.Width >> 1) &&
                    Mouse.Position.X <= rot.X + (marker.MarkerIcon.Width >> 1) &&
                    Mouse.Position.Y >= rot.Y - (marker.MarkerIcon.Height >> 1) &&
                    Mouse.Position.Y <= rot.Y + (marker.MarkerIcon.Height >> 1))
                {
                    drawSingleName = true;
                }
            }
        }

        if (showMarkerName)
        {
            DrawMarkerString(batcher, marker, x, y, width, height);

            drawSingleName = false;
        }

        return drawSingleName;
    }

    private void DrawMarkerString(UltimaBatcher2D batcher, WMapMarker marker, int x, int y, int width, int height)
    {
        if (string.IsNullOrEmpty(marker.Name))
            return;

        int sx = marker.X - _center.X;
        int sy = marker.Y - _center.Y;

        Point rot = RotatePoint
        (
            sx,
            sy,
            Zoom,
            1,
            _flipMap ? 45f : 0f
        );

        rot.X += x + width;
        rot.Y += y + height;

        TextBox ttfBox = UseTtfFont ? GetTtfTextBox(marker.Name) : null;
        Vector2 size = ttfBox != null ? new Vector2(ttfBox.MeasuredSize.X, ttfBox.MeasuredSize.Y) : _markerFont.MeasureString(marker.Name);

        if (rot.X + size.X / 2 > x + Width - 8)
        {
            rot.X = x + Width - 8 - (int)(size.X / 2);
        }
        else if (rot.X - size.X / 2 < x)
        {
            rot.X = x + (int)(size.X / 2);
        }

        if (rot.Y + size.Y > y + Height)
        {
            rot.Y = y + Height - (int)size.Y;
        }
        else if (rot.Y - size.Y < y)
        {
            rot.Y = y + (int)size.Y;
        }

        int xx = (int)(rot.X - size.X / 2);
        int yy = (int)(rot.Y - size.Y - 5);

        var hueVector = new Vector3(0f, 1f, 0.5f);

        batcher.Draw
        (
            SolidColorTextureCache.GetTexture(Color.Black),
            new Rectangle
            (
                xx - 2,
                yy - 2,
                (int)(size.X + 4),
                (int)(size.Y + 4)
            ),
            hueVector
        );

        if (ttfBox != null)
        {
            ttfBox.Draw(batcher, xx, yy, Color.White);
            return;
        }

        hueVector = new Vector3(0f, 1f, 1f);

        batcher.DrawString
        (
            _markerFont,
            marker.Name,
            xx + 1,
            yy + 1,
            hueVector
        );

        hueVector = ShaderHueTranslator.GetHueVector(0);

        batcher.DrawString
        (
            _markerFont,
            marker.Name,
            xx,
            yy,
            hueVector
        );
    }

    private void DrawNavDestination(UltimaBatcher2D batcher, int x, int y, int width, int height)
    {
        if (!_navDest.HasValue || _world.Player == null)
            return;

        long elapsed = Environment.TickCount64 - _navDestSetTime;

        // Auto-clear: player arrived within 3 tiles, after a 2-second grace period.
        if (elapsed > 2000)
        {
            int dx = _world.Player.X - _navDest.Value.X;
            int dy = _world.Player.Y - _navDest.Value.Y;

            if (dx * dx + dy * dy <= 9)
            {
                _navDest = null;
                _navPath = null;
                return;
            }

            // Also clear if pathfinding has fully stopped (cancelled or completed).
            if (!_world.Player.Pathfinder.AutoWalking)
            {
                _navDest = null;
                _navPath = null;
                return;
            }
        }

        Vector3 hueVector = ShaderHueTranslator.GetHueVector(0, false, 1f);
        var pathTex = SolidColorTextureCache.GetTexture(new Color(220, 220, 220));

        // Draw path as a series of small dots along the route (capped at ~200 dots).
        if (_navPath != null && _navPath.Count > 1)
        {
            int step = Math.Max(1, _navPath.Count / 200);
            const int DOT = 2;
            const int DOT_HALF = DOT / 2;

            for (int i = 0; i < _navPath.Count; i += step)
            {
                Point p = _navPath[i];
                int psx = p.X - _center.X;
                int psy = p.Y - _center.Y;
                Point prot = RotatePoint(psx, psy, Zoom, 1, _flipMap ? 45f : 0f);
                int pdx = prot.X + x + width;
                int pdy = prot.Y + y + height;

                if (pdx < x || pdx > x + Width - 8 - DOT || pdy < y || pdy > y + Height - 8 - DOT)
                    continue;

                batcher.Draw(pathTex, new Rectangle(pdx - DOT_HALF, pdy - DOT_HALF, DOT, DOT), hueVector);
            }
        }

        // Destination marker (small square).
        int sx = _navDest.Value.X - _center.X;
        int sy = _navDest.Value.Y - _center.Y;

        Point rot = RotatePoint(sx, sy, Zoom, 1, _flipMap ? 45f : 0f);
        int drawX = rot.X + x + width;
        int drawY = rot.Y + y + height;

        const int SIZE = 6;
        const int HALF = SIZE / 2;

        if (drawX < x || drawX > x + Width - 8 - SIZE || drawY < y || drawY > y + Height - 8 - SIZE)
            return;

        batcher.Draw(pathTex, new Rectangle(drawX - HALF, drawY - HALF, SIZE, SIZE), hueVector);
    }

    private void DrawMulti
    (
        UltimaBatcher2D batcher,
        House house,
        int multiX,
        int multiY,
        int x,
        int y,
        int width,
        int height,
        float zoom
    )
    {
        int sx = multiX - _center.X;
        int sy = multiY - _center.Y;
        int sW = Math.Abs(house.Bounds.Width - house.Bounds.X);
        int sH = Math.Abs(house.Bounds.Height - house.Bounds.Y);

        Point rot = RotatePoint
        (
            sx,
            sy,
            zoom,
            1,
            _flipMap ? 45f : 0f
        );


        rot.X += x + width;
        rot.Y += y + height;

        const int DOT_SIZE = 4;

        if (rot.X < x || rot.X > x + Width - 8 - DOT_SIZE || rot.Y < y || rot.Y > y + Height - 8 - DOT_SIZE)
        {
            return;
        }

        Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);

        Texture2D texture = SolidColorTextureCache.GetTexture(Color.DarkGray);

        batcher.Draw
        (
            texture,
            new Rectangle
            (
                rot.X,
                rot.Y,
                (int)(sW * zoom),
                (int)(sH * zoom)
            ),
            null,
            hueVector,
            _flipMap ? Microsoft.Xna.Framework.MathHelper.ToRadians(45) : 0,
            new Vector2(0.5f, 0.5f),
            SpriteEffects.None,
            0
        );
    }

    private Vector2 WorldPointToGumpPoint(int wpx, int wpy, int x, int y, int width, int height, float zoom)
    {
        int sx = wpx - _center.X;
        int sy = wpy - _center.Y;

        Point rot = RotatePoint
        (
            sx,
            sy,
            zoom,
            1,
            _flipMap ? 45f : 0f
        );

        /* N.B. You don't want AdjustPosition() here if you want to draw rects
         * that extend beyond the gump's viewport without distoring them. */

        rot.X += x + width;
        rot.Y += y + height;

        return new Vector2(rot.X, rot.Y);
    }

    private void DrawZone
    (
        UltimaBatcher2D batcher,
        Zone zone,
        int x,
        int y,
        int width,
        int height,
        float zoom
    )
    {
        Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);
        Texture2D texture = SolidColorTextureCache.GetTexture(zone.Color);

        //Vector2 topleft = new Vector2(10000, 10000), botright = Vector2.Zero;

        for (int i = 0, j = 1; i < zone.Vertices.Count; i++, j++)
        {
            if (j >= zone.Vertices.Count) j = 0;

            Vector2 start = WorldPointToGumpPoint(zone.Vertices[i].X, zone.Vertices[i].Y, x, y, width, height, zoom);
            Vector2 end = WorldPointToGumpPoint(zone.Vertices[j].X, zone.Vertices[j].Y, x, y, width, height, zoom);

            //if(start.X < topleft.X)
            //{
            //    topleft.X = start.X;
            //}

            //if(start.Y < topleft.Y)
            //{
            //    topleft.Y = start.Y;
            //}

            //if(end.X > botright.X)
            //{
            //    botright.X = end.X;
            //}
            //if (end.Y > botright.Y)
            //{
            //    botright.Y = end.Y;
            //}
            ////Handle drawing a label here

            batcher.DrawLine(texture, start, end, hueVector, 1, 0f);
        }
    }

    private void DrawGrid
    (
        UltimaBatcher2D batcher,
        Rectangle srcRect,
        int x,
        int y,
        int width,
        int height,
        float zoom
    )
    {
        const int GRID_SKIP = 8;
        Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);
        Texture2D colorTexture = SolidColorTextureCache.GetTexture(_semiTransparentWhiteForGrid);

        batcher.SetBlendState(BlendState.Additive);

        for (int worldY = (srcRect.Y / GRID_SKIP) * GRID_SKIP; worldY < srcRect.Y + srcRect.Height; worldY += GRID_SKIP)
        {
            Vector2 start = WorldPointToGumpPoint(srcRect.X, worldY, x, y, width, height, zoom);
            Vector2 end = WorldPointToGumpPoint(srcRect.X + srcRect.Width, worldY, x, y, width, height, zoom);

            batcher.DrawLine(colorTexture, start, end, hueVector, 1, 0f);
        }

        for (int worldX = (srcRect.X / GRID_SKIP) * GRID_SKIP; worldX < srcRect.X + srcRect.Width; worldX += GRID_SKIP)
        {
            Vector2 start = WorldPointToGumpPoint(worldX, srcRect.Y, x, y, width, height, zoom);
            Vector2 end = WorldPointToGumpPoint(worldX, srcRect.Y + srcRect.Height, x, y, width, height, zoom);

            batcher.DrawLine(colorTexture, start, end, hueVector, 1, 0f);
        }

        batcher.SetBlendState(null);
    }

    private void DrawWMEntity
    (
        UltimaBatcher2D batcher,
        WMapEntity entity,
        int x,
        int y,
        int width,
        int height,
        float zoom
    )
    {
        Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);

        ushort uohue;
        Color color;

        if (entity.IsGuild)
        {
            uohue = 0x0044;
            color = Color.LimeGreen;
        }
        else
        {
            uohue = 0x0034;
            color = Color.Yellow;
        }

        if (entity.Map != _map.Index)
        {
            uohue = 992;
            color = Color.DarkGray;
        }

        int sx = entity.X - _center.X;
        int sy = entity.Y - _center.Y;

        Point rot = RotatePoint
        (
            sx,
            sy,
            zoom,
            1,
            _flipMap ? 45f : 0f
        );

        AdjustPosition
        (
            rot.X,
            rot.Y,
            width - 4,
            height - 4,
            out rot.X,
            out rot.Y
        );

        rot.X += x + width;
        rot.Y += y + height;

        const int DOT_SIZE = 4;
        const int DOT_SIZE_HALF = DOT_SIZE >> 1;

        if (rot.X < x)
        {
            rot.X = x;
        }

        if (rot.X > x + Width - 8 - DOT_SIZE)
        {
            rot.X = x + Width - 8 - DOT_SIZE;
        }

        if (rot.Y < y)
        {
            rot.Y = y;
        }

        if (rot.Y > y + Height - 8 - DOT_SIZE)
        {
            rot.Y = y + Height - 8 - DOT_SIZE;
        }

        batcher.Draw
        (
            SolidColorTextureCache.GetTexture(color),
            new Rectangle
            (
                rot.X - DOT_SIZE_HALF,
                rot.Y - DOT_SIZE_HALF,
                DOT_SIZE,
                DOT_SIZE
            ),
            hueVector
        );

        if (_showGroupName)
        {
            string name = entity.Name ?? ResGumps.OutOfRange;
            TextBox ttfBox = UseTtfFont ? GetTtfTextBox(name) : null;
            Vector2 size = ttfBox != null ? new Vector2(ttfBox.MeasuredSize.X, ttfBox.MeasuredSize.Y) : Fonts.Regular.MeasureString(name);

            if (rot.X + size.X / 2 > x + Width - 8)
            {
                rot.X = x + Width - 8 - (int)(size.X / 2);
            }
            else if (rot.X - size.X / 2 < x)
            {
                rot.X = x + (int)(size.X / 2);
            }

            if (rot.Y + size.Y > y + Height)
            {
                rot.Y = y + Height - (int)size.Y;
            }
            else if (rot.Y - size.Y < y)
            {
                rot.Y = y + (int)size.Y;
            }

            int xx = (int)(rot.X - size.X / 2);
            int yy = (int)(rot.Y - size.Y);

            if (ttfBox != null)
            {
                ttfBox.Draw(batcher, xx + 1, yy + 1, Color.Black);
                ttfBox.Draw(batcher, xx, yy, TextBox.ConvertHueToColor(uohue));
            }
            else
            {
                hueVector.X = 0;
                hueVector.Y = 1;

                batcher.DrawString
                (
                    Fonts.Regular,
                    name,
                    xx + 1,
                    yy + 1,
                    hueVector
                );

                hueVector = new Vector3(uohue, 1f, 1f);

                batcher.DrawString
                (
                    Fonts.Regular,
                    name,
                    xx,
                    yy,
                    hueVector
                );
            }
        }

        if (_showGroupBar)
        {
            rot.Y += DOT_SIZE + 1;
            DrawHpBar(batcher, rot.X, rot.Y, entity.HP);
        }
    }

    private void DrawHpBar(UltimaBatcher2D batcher, int x, int y, int hp)
    {
        Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);

        const int BAR_MAX_WIDTH = 25;
        const int BAR_MAX_WIDTH_HALF = BAR_MAX_WIDTH / 2;

        const int BAR_MAX_HEIGHT = 3;
        const int BAR_MAX_HEIGHT_HALF = BAR_MAX_HEIGHT / 2;


        batcher.Draw
        (
            SolidColorTextureCache.GetTexture(Color.Black),
            new Rectangle
            (
                x - BAR_MAX_WIDTH_HALF - 1,
                y - BAR_MAX_HEIGHT_HALF - 1,
                BAR_MAX_WIDTH + 2,
                BAR_MAX_HEIGHT + 2
            ),
            hueVector
        );

        batcher.Draw
        (
            SolidColorTextureCache.GetTexture(Color.Red),
            new Rectangle
            (
                x - BAR_MAX_WIDTH_HALF,
                y - BAR_MAX_HEIGHT_HALF,
                BAR_MAX_WIDTH,
                BAR_MAX_HEIGHT
            ),
            hueVector
        );

        int max = 100;
        int current = hp;

        if (max > 0)
        {
            max = current * 100 / max;

            if (max > 100)
            {
                max = 100;
            }

            if (max > 1)
            {
                max = BAR_MAX_WIDTH * max / 100;
            }
        }

        batcher.Draw
        (
            SolidColorTextureCache.GetTexture(Color.CornflowerBlue),
            new Rectangle
            (
                x - BAR_MAX_WIDTH_HALF,
                y - BAR_MAX_HEIGHT_HALF,
                max,
                BAR_MAX_HEIGHT
            ),
            hueVector
        );
    }

    #endregion

    #region I/O

    public override void OnMouseUp(int x, int y, MouseButtonType button)
    {
        bool allowTarget = _allowPositionalTarget && World.TargetManager.IsTargeting && World.TargetManager.TargetingState == CursorTarget.Position;
        if (allowTarget && button == MouseButtonType.Left)
        {
            HandlePositionTarget();
        }

        if (button == MouseButtonType.Left && !Keyboard.Alt)
        {
            _isScrolling = false;
            if (!IsLocked)
            {
                CanMove = true;
            }
        }

        if (button == MouseButtonType.Left || button == MouseButtonType.Middle)
        {
            _lastScroll.X = _center.X;
            _lastScroll.Y = _center.Y;
        }

        if (button == MouseButtonType.Left && !Keyboard.Alt && !Keyboard.Ctrl && !Keyboard.Shift)
        {
            if (x > 10 && x < 120 && y > 10 && y < 25)
            {
                SDL.SDL_SetClipboardText($"{World.Player.X}, {World.Player.Y}, {World.Player.Z}");
                GameActions.Print("Copied player coords to clipboard.");
            }
        }

        Client.Game.UO.GameCursor.IsDraggingCursorForced = false;

        base.OnMouseUp(x, y, button);
    }

    public override void OnMouseDown(int x, int y, MouseButtonType button)
    {
        if (!Client.Game.UO.GameCursor.ItemHold.Enabled)
        {
            if (button == MouseButtonType.Left && HotKeys.IsPressed(HotKeyRegistrar.WorldMapMarkerId) && !Keyboard.Alt)
            {
                CanvasToWorld(x, y, out int wX, out int wY);

                WMapMarkerFile userFile = _markerFiles.Where(f => f.Name == USER_MARKERS_FILE).FirstOrDefault();
                if (userFile == null)
                    return;

                UserMarkersGump existingGump = UIManager.GetGump<UserMarkersGump>();
                existingGump?.Dispose();
                new UserMarkersGump(World, wX, wY, userFile.Markers, mapIndex: _map.Index);
                return;
            }

            if (button == MouseButtonType.Right && HotKeys.IsPressed(HotKeyRegistrar.WorldMapPathfindId) && !Keyboard.Alt)
            {
                CanvasToWorld(x, y, out int wX, out int wY);
                int mapIndex = _world.Map.Index;

                // The append modifier (Shift by default, rebindable) extends the active route:
                // search from the end of the current path to the new point and append the result
                // (A->B->C) instead of restarting from the player.
                bool append = HotKeys.IsPressed(HotKeyRegistrar.WorldMapPathfindAppendId)
                              && _navDest.HasValue
                              && _world.Player?.Pathfinder != null
                              && _world.Player.Pathfinder.AutoWalking;

                if (append)
                {
                    StartNavPath(mapIndex, _navPlannedEnd.X, _navPlannedEnd.Y, _navPlannedEndZ, wX, wY,
                                 firstAttempt: true, append: true);
                    return;
                }

                _navDest = new Point(wX, wY);
                _navDestSetTime = Environment.TickCount64;
                _navPath = null;
                _navPlannedEnd = new Point(wX, wY);
                _navPlannedEndZ = _world.Player.Z;
                _navSegments = 1;
                ClearGoToMarker();

                // Fresh nav session: clear any dynamic-block memory from a previous run,
                // reset the hook so a stale closure from an old session can't fire, and
                // stop any walk that may still be in progress.
                WorldMapPathfinder.ClearDynamicBlocks();
                if (_world.Player?.Pathfinder != null)
                {
                    if (_navStepFailedHandler != null)
                        _world.Player.Pathfinder.OnComputedPathStepFailed -= _navStepFailedHandler;
                    _navStepFailedHandler = null;
                    _world.Player.Pathfinder.StopAutoWalk();
                }
                _navReplansLeft = MaxNavReplans;

                StartNavPath(mapIndex, _world.Player.X, _world.Player.Y, _world.Player.Z, wX, wY,
                             firstAttempt: true, append: false);
                return;
            }

            if (button == MouseButtonType.Left && (Keyboard.Alt || _freeView) || button == MouseButtonType.Middle)
            {
                if (x > 4 && x < Width - 8 && y > 4 && y < Height - 8)
                {
                    if (button == MouseButtonType.Middle)
                    {
                        FreeView = !FreeView;
                    }

                    if (FreeView)
                    {
                        _lastScroll.X = _center.X;
                        _lastScroll.Y = _center.Y;
                        _isScrolling = true;
                        CanMove = false;

                        Client.Game.UO.GameCursor.IsDraggingCursorForced = true;
                    }
                }

            }
        }

        base.OnMouseDown(x, y, button);
    }

    /// <summary>
    /// Dispatches a WorldMapPathfinder search and hands the result to the walker.
    /// When <paramref name="append"/> is set the result is chained onto the route currently
    /// being walked (multi-segment A-&gt;B-&gt;C) instead of replacing it. For fresh (non-append)
    /// paths it registers an on-step-failed hook so dynamic obstacles get added to the
    /// dynamic-block set and the search retried up to MAX_NAV_REPLANS times.
    /// </summary>
    private void StartNavPath(int mapIndex, int startX, int startY, sbyte startZ, int destX, int destY, bool firstAttempt, bool append)
    {
        var houseMultis = BuildHouseMultiSnapshot();

        WorldMapPathfinder.FindPathAsync(mapIndex, startX, startY, startZ, destX, destY, 8, path =>
        {
            if (path == null || path.Count == 0)
            {
                if (append)
                {
                    // Couldn't extend the route — leave the existing path intact.
                    GameActions.Print("Can't extend the path there.");
                    return;
                }

                if (firstAttempt)
                    GameActions.Print("Can't find a path there.");
                _navDest = null;
                _navPath = null;
                _navSegments = 0;
                return;
            }

            var pathPoints = new List<(int X, int Y, int Z, int Direction)>(path.Count);
            var navRender = new List<Point>(path.Count);
            foreach (var p in path)
            {
                pathPoints.Add((p.X, p.Y, p.Z, p.Direction));
                navRender.Add(new Point(p.X, p.Y));
            }

            var last = path[path.Count - 1];

            // Extend the active route without restarting the walk.
            if (append && _world.Player.Pathfinder.AppendComputedPath(pathPoints))
            {
                if (_navPath == null)
                    _navPath = navRender;
                else
                    _navPath.AddRange(navRender);

                _navDest = new Point(last.X, last.Y);
                _navDestSetTime = Environment.TickCount64;
                _navPlannedEnd = new Point(last.X, last.Y);
                _navPlannedEndZ = (sbyte)last.Z;
                _navSegments++;
                return;
            }

            // Fresh path (or an append that found nothing to attach to because the previous
            // route had already finished): replace whatever was there.
            _navPath = navRender;
            _navDest = new Point(last.X, last.Y);
            _navDestSetTime = Environment.TickCount64;
            _navPlannedEnd = new Point(last.X, last.Y);
            _navPlannedEndZ = (sbyte)last.Z;
            _navSegments = 1;

            // Unsubscribe any stale handler from a previous plan before registering the new one.
            if (_navStepFailedHandler != null)
                _world.Player.Pathfinder.OnComputedPathStepFailed -= _navStepFailedHandler;

            _navStepFailedHandler = (blockX, blockY) =>
            {
                // Multi-segment routes can't be locally replanned around a block without
                // skipping waypoints, so a blocked step clears the entire route.
                if (_navSegments > 1)
                {
                    if (_navStepFailedHandler != null)
                        _world.Player.Pathfinder.OnComputedPathStepFailed -= _navStepFailedHandler;
                    _navStepFailedHandler = null;
                    _navDest = null;
                    _navPath = null;
                    _navSegments = 0;
                    return;
                }

                if (_navReplansLeft <= 0 || !_navDest.HasValue)
                {
                    _navDest = null;
                    _navPath = null;
                    _navSegments = 0;
                    return;
                }

                _navReplansLeft--;
                WorldMapPathfinder.MarkDynamicBlock(blockX, blockY);

                var dest = _navDest.Value;
                StartNavPath(_world.Map.Index, _world.Player.X, _world.Player.Y, _world.Player.Z,
                             dest.X, dest.Y, firstAttempt: false, append: false);
            };
            _world.Player.Pathfinder.OnComputedPathStepFailed += _navStepFailedHandler;

            _world.Player.Pathfinder.StartComputedPath(pathPoints, run: true);
        }, houseMultis);
    }

    /// <summary>
    /// Shallow-copies Multi component fields from every loaded player house into a flat
    /// list of plain structs. Runs on the main thread where live World data is safe to
    /// iterate, but does NO filtering and NO map-file I/O — just field reads (~20 ns each).
    /// The background thread (WorldMapPathfinder) does the flag checks and Z lookups.
    /// </summary>
    private List<WorldMapPathfinder.HouseMultiSnapshot> BuildHouseMultiSnapshot()
    {
        var houseManager = _world?.HouseManager;
        if (houseManager == null)
            return null;

        var list = new List<WorldMapPathfinder.HouseMultiSnapshot>(256);
        foreach (var house in houseManager.Houses)
        {
            foreach (var multi in house.Components)
            {
                list.Add(new WorldMapPathfinder.HouseMultiSnapshot
                {
                    X = multi.X,
                    Y = multi.Y,
                    Z = multi.Z,
                    Graphic = multi.Graphic,
                    State = (int)multi.State,
                    IsHousePreview = multi.IsHousePreview,
                    IsDestroyed = multi.IsDestroyed,
                });
            }
        }
        return list;
    }

    public override void OnMouseOver(int x, int y)
    {
        _lastMousePosition = new Point(x, y);

        Point offset = Mouse.LButtonPressed ? Mouse.LDragOffset : Mouse.MButtonPressed ? Mouse.MDragOffset : Point.Zero;

        if (_isScrolling && offset != Point.Zero)
        {
            _scroll.X = _scroll.Y = 0;

            if (Mouse.LButtonPressed)
            {
                _scroll.X = x - (Mouse.LClickPosition.X - X);
                _scroll.Y = y - (Mouse.LClickPosition.Y - Y);
            }
            else if (Mouse.MButtonPressed)
            {
                _scroll.X = x - (Mouse.MClickPosition.X - X);
                _scroll.Y = y - (Mouse.MClickPosition.Y - Y);
            }

            if (_scroll == Point.Zero)
            {
                return;
            }

            _scroll = RotatePoint
            (
                _scroll.X,
                _scroll.Y,
                1f / Zoom,
                -1,
                _flipMap ? 45f : 0f
            );

            _center.X = _lastScroll.X - _scroll.X;
            _center.Y = _lastScroll.Y - _scroll.Y;

            if (_center.X < 0)
            {
                _center.X = 0;
            }

            if (_center.Y < 0)
            {
                _center.Y = 0;
            }

            if (_center.X > Client.Game.UO.FileManager.Maps.MapsDefaultSize[_map.Index, 0])
            {
                _center.X = Client.Game.UO.FileManager.Maps.MapsDefaultSize[_map.Index, 0];
            }

            if (_center.Y > Client.Game.UO.FileManager.Maps.MapsDefaultSize[_map.Index, 1])
            {
                _center.Y = Client.Game.UO.FileManager.Maps.MapsDefaultSize[_map.Index, 1];
            }
        }
        else
        {
            base.OnMouseOver(x, y);
        }
    }

    public override void OnMouseWheel(MouseEventType delta)
    {
        if (delta == MouseEventType.WheelScrollUp)
        {
            _zoomIndex++;

            if (_zoomIndex >= _zooms.Length)
            {
                _zoomIndex = _zooms.Length - 1;
            }
        }
        else
        {
            _zoomIndex--;

            if (_zoomIndex < 0)
            {
                _zoomIndex = 0;
            }
        }


        base.OnMouseWheel(delta);
    }

    public override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
    {
        if (button != MouseButtonType.Left || _isScrolling || Keyboard.Alt)
        {
            return base.OnMouseDoubleClick(x, y, button);
        }

        switch (_doubleClickAction)
        {
            case WorldMapDoubleClickAction.ToggleFullscreen:
                ToggleFullscreen();
                break;

            case WorldMapDoubleClickAction.ToggleLock:
            default:
                SetLockStatus(!IsLocked);
                TopMost = !IsLocked;
                break;
        }

        return true;
    }

    private void SetDoubleClickAction(WorldMapDoubleClickAction action)
    {
        if (_doubleClickAction == action)
        {
            return;
        }

        _doubleClickAction = action;
        SaveSettings();
        BuildContextMenu();
    }

    private void ToggleFullscreen()
    {
        if (!_isFullscreen)
        {
            // Remember the current windowed bounds and top-most state so we can restore them later.
            _preFullscreenBounds = new Rectangle(X, Y, Width, Height);
            _preFullscreenTopMost = _isTopMost;
            _isFullscreen = true;

            SetTopMost(true, false);

            X = 0;
            Y = 0;
            ApplySize(Client.Game.Window.ClientBounds.Width, Client.Game.Window.ClientBounds.Height);
        }
        else
        {
            _isFullscreen = false;

            SetTopMost(_preFullscreenTopMost, false);

            X = _preFullscreenBounds.X;
            Y = _preFullscreenBounds.Y;
            ApplySize(_preFullscreenBounds.Width, _preFullscreenBounds.Height);
        }

        SaveSettings();
    }

    private void ApplySize(int width, int height)
    {
        Width = width;
        Height = height;
        ResizeWindow(new Point(Width, Height));
        OnResize();
    }

    protected override void OnMouseExit(int x, int y)
    {
        _lastMousePosition = null;
        base.OnMouseExit(x, y);
    }

    protected override void OnMove(int x, int y)
    {
        base.OnMove(x, y);
        _last_position.X = ScreenCoordinateX;
        _last_position.Y = ScreenCoordinateY;
    }

    #endregion

    #region Helpers


    /// <summary>
    /// Parser String to Marker
    /// </summary>
    /// <param name="splits">Array of string contain information about Marker</param>
    /// <returns>Marker</returns>
    internal static WMapMarker ParseMarker(string[] splits)
    {
        var marker = new WMapMarker
        {
            X = int.Parse(Truncate(splits[0], 4)),
            Y = int.Parse(Truncate(splits[1], 4)),
            MapId = int.Parse(splits[2]),
            Name = Truncate(splits[3], 25),
            MarkerIconName = splits[4].ToLower(),
            Color = GetColor(Truncate(splits[5], 10)),
            ColorName = Truncate(splits[5], 10),
            ZoomIndex = splits.Length == 7 ? int.Parse(splits[6]) : 3
        };

        if (_markerIcons.TryGetValue(splits[4].ToLower(), out Texture2D value))
        {
            marker.MarkerIcon = value;
        }

        return marker;
    }

    /// <summary>
    /// Truncate string to max length
    /// </summary>
    /// <param name="s">String</param>
    /// <param name="maxLen">Max Length</param>
    /// <returns>Truncated String</returns>
    private static string Truncate(string s, int maxLen) => s.Length > maxLen ? s.Remove(maxLen) : s;

    /// <summary>
    /// Map Color name to Color in XNA
    /// </summary>
    private static readonly Dictionary<string, Color> _colorMap = new Dictionary<string, Color>
        {
            { "red", Color.Red },
            { "green", Color.Green },
            { "blue", Color.Blue },
            { "purple", Color.Purple },
            { "black", Color.Black },
            { "yellow", Color.Yellow },
            { "white", Color.White },
            { "none", Color.Transparent },
        };

    /// <summary>
    /// Get Color for Texture by name
    /// </summary>
    /// <param name="name">Color name</param>
    /// <returns>Color in XNA (RGBA)</returns>
    public static Color GetColor(string name) => _colorMap.TryGetValue(name, out Color color) ? color : Color.White;

    /// <summary>
    /// Converts latitudes and longitudes to X and Y locations based on Lord British's throne is located at 1323.1624 or 0° 0'N 0° 0'E
    /// </summary>
    /// <param name="coords"></param>
    /// <param name="xAxis"></param>
    /// <param name="yAxis"></param>
    private static void ConvertCoords(string coords, ref int xAxis, ref int yAxis)
    {
        string[] coordsSplit = coords.Split(',');

        string yCoord = coordsSplit[0];
        string xCoord = coordsSplit[1];

        // Calc Y first
        string[] ySplit = yCoord.Split('°', 'o');
        double yDegree = Convert.ToDouble(ySplit[0]);
        double yMinute = Convert.ToDouble(ySplit[1].Substring(0, ySplit[1].IndexOf("'", StringComparison.Ordinal)));

        if (yCoord.Substring(yCoord.Length - 1).Equals("N"))
        {
            yAxis = (int)(1624 - (yMinute / 60) * (4096.0 / 360) - yDegree * (4096.0 / 360));
        }
        else
        {
            yAxis = (int)(1624 + (yMinute / 60) * (4096.0 / 360) + yDegree * (4096.0 / 360));
        }

        // Calc X next
        string[] xSplit = xCoord.Split('°', 'o');
        double xDegree = Convert.ToDouble(xSplit[0]);
        double xMinute = Convert.ToDouble(xSplit[1].Substring(0, xSplit[1].IndexOf("'", StringComparison.Ordinal)));

        if (xCoord.Substring(xCoord.Length - 1).Equals("W"))
        {
            xAxis = (int)(1323 - (xMinute / 60) * (5120.0 / 360) - xDegree * (5120.0 / 360));
        }
        else
        {
            xAxis = (int)(1323 + (xMinute / 60) * (5120.0 / 360) + xDegree * (5120.0 / 360));
        }

        // Normalize values outside of map range.
        if (xAxis < 0)
        {
            xAxis += 5120;
        }
        else if (xAxis > 5120)
        {
            xAxis -= 5120;
        }

        if (yAxis < 0)
        {
            yAxis += 4096;
        }
        else if (yAxis > 4096)
        {
            yAxis -= 4096;
        }
    }
}

#endregion
