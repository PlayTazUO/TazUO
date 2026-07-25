using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Managers.Hotkeys;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.MyraWindows;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Xml;
using ClassicUO.Game.UI.Gumps.GridHighLight;
using ClassicUO.Utility;
using ClassicUO.Game.UI.Gumps.GridContainers;

namespace ClassicUO.Game.UI.Gumps
{
public partial class GridContainer : ResizableGump
    {
        #region CONSTANTS
        public const int X_SPACING = 1, Y_SPACING = 1;
        private const int TOP_BAR_HEIGHT = 20;
        private const int LABEL_HEIGHT = 20;
        #endregion

        #region private static vars
        private static int _lastX = 100, _lastY = 100, _lastCorpseX = 100, _lastCorpseY = 100;
        public static int GridItemSize => (int)Math.Round(50 * (ProfileManager.CurrentProfile.GridContainersScale / 100f));
        private const int DEFAULT_BORDER_WIDTH = 4;
        // Per-instance so containers can't stomp each other's border width; the static size
        // helpers derive the current width from the profile via GetCurrentBorderWidth().
        private int _borderWidth = DEFAULT_BORDER_WIDTH;

        private static readonly Dictionary<BorderStyle, (int graphic, int borderSize)> _borderStyleConfig = new()
        {
            { BorderStyle.Style1, (3500, 26) },
            { BorderStyle.Style2, (5054, 12) },
            { BorderStyle.Style3, (5120, 10) },
            { BorderStyle.Style4, (9200, 7) },
            { BorderStyle.Style5, (9270, 10) },
            { BorderStyle.Style6, (9300, 4) },
            { BorderStyle.Style7, (9260, 17) }
        };
        #endregion

        #region private readonly vars
        // These UI controls are created once (in the Build* helpers invoked from the constructor)
        // and never reassigned; they are non-readonly only because they are assigned from helper
        // methods rather than inline in the constructor body.
        private AlphaBlendControl _background;
        private AlphaBlendControl _searchBoxBackground;
        private Label _containerNameLabel;
        private StbTextBox _searchBox;
        private GumpPic _openRegularGump, _sortContents;
        private ResizableStaticPic _quickDropBackpack;
        private GumpPicTiled _backgroundTexture;
        private NiceButton _setLootBag, _searchClearButton;
        private readonly bool _isCorpse;
        #endregion

        #region hotkeys
        // Cached refs to the centrally-registered grid hotkeys (registered in HotKeyRegistrar) so the
        // per-frame IsPressed checks skip the registry dictionary lookup.
        private HotKeyEntry _hkMultiMove;
        private HotKeyEntry _hkAutoLoot;
        private HotKeyEntry _hkLockSlot;
        private HotKeyEntry _hkCompare;

        public bool IsMultiMove => (_hkMultiMove ??= HotKeys.Get(HotKeyRegistrar.GridMultiMoveId))?.IsPressed() ?? false;
        public bool IsAutoLoot => (_hkAutoLoot ??= HotKeys.Get(HotKeyRegistrar.GridAutoLootId))?.IsPressed() ?? false;
        public bool IsLockSlot => (_hkLockSlot ??= HotKeys.Get(HotKeyRegistrar.GridLockSlotId))?.IsPressed() ?? false;
        public bool IsCompare => (_hkCompare ??= HotKeys.Get(HotKeyRegistrar.GridCompareId))?.IsPressed() ?? false;
        public bool QuickLootThisContainer => _quickLootThisContainer;
        #endregion

        #region private vars
        private Item Container => World.Items.Get(LocalSerial);
        private int _lastGridItemSize = GridItemSize;
        private int _lastWidth = GetWidth(), _lastHeight = GetHeight();
        private bool _quickLootThisContainer;
        public bool? UseOldContainerStyle;
        private bool _autoSortContainer;
        private GridSortMode _sortMode = GridSortMode.GraphicAndHue;

        private readonly bool _skipSave;
        private readonly ushort _originalContainerItemGraphic;

        private GridScrollArea _scrollArea;
        private bool _isMinimized;
        private int _heightBeforeMinimize;
        #endregion

        #region private tooltip vars
        private static string QuickLootStatus => GetEnabledDisabledText(ProfileManager.CurrentProfile.CorpseSingleClickLoot);
        private string QuickLootTooltip
        {
            get
            {
                if (_isCorpse)
                    return TazLang.Get("gridcontainer_quickloot_corpse_tooltip", new string[] { QuickLootStatus });
                return TazLang.Get("gridcontainer_quickloot_container_tooltip", new string[] { GetEnabledDisabledText(_quickLootThisContainer) });
            }

        }
        private string SortButtonTooltip
        {
            get
            {
                string status = GetEnabledDisabledText(_autoSortContainer);
                string sortModeText = _sortMode switch
                {
                    GridSortMode.Name => TazLang.Get("gridcontainer_sortmode_name", "Name"),
                    GridSortMode.Layer => TazLang.Get("gridcontainer_sortmode_layer", "Layer"),
                    _ => TazLang.Get("gridcontainer_sortmode_graphichue", "Graphic + Hue")
                };
                return TazLang.Get("gridcontainer_sort_tooltip", new string[] { sortModeText, status });
            }
        }

        private readonly GridContainerEntry _gridContainerEntry;
        #endregion

        #region public vars
        public GridContainerEntry GridContainerEntry => _gridContainerEntry;
        public readonly bool IsPlayerBackpack;
        public bool StackNonStackableItems;
        public bool AutoSortContainer => _autoSortContainer;
        public GridSortMode SortMode => _sortMode;
        public readonly GridSlotManager SlotManager;
        public bool IsCorpse => _isCorpse;

        public bool IsMinimized
        {
            get => _isMinimized;
            set
            {
                if (_isMinimized != value)
                {
                    _isMinimized = value;
                    UpdateMinimizedState();
                }
            }
        }

        public int HeightBeforeMinimize => _heightBeforeMinimize;
        #endregion

        #region Helper Methods
        /// <summary>
        /// Sets the visibility of all UI controls (used during minimize/maximize)
        /// </summary>
        /// <param name="visible">True to show controls, false to hide them</param>
        private void SetControlsVisibility(bool visible)
        {
            if (_searchBox != null) _searchBox.IsVisible = visible;
            if (_searchClearButton != null) _searchClearButton.IsVisible = visible;
            if (_openRegularGump != null) _openRegularGump.IsVisible = visible;
            if (_quickDropBackpack != null) _quickDropBackpack.IsVisible = visible;
            if (_sortContents != null) _sortContents.IsVisible = visible;
            if (_scrollArea != null) _scrollArea.IsVisible = visible;
            if (_setLootBag != null) _setLootBag.IsVisible = visible && _isCorpse;

            if (ResizeButton != null) ResizeButton.IsVisible = visible;
        }

        /// <summary>
        /// Repositions the resize handle into the bottom-right corner. Used when we bypass
        /// <see cref="ResizableGump.ResizeWindow"/> (e.g. maintaining the minimized height).
        /// </summary>
        private void RepositionResizeButton()
        {
            Button btn = ResizeButton;
            if (btn == null) return;

            btn.X = Width - btn.Width + 2;
            btn.Y = Height - btn.Height + 2;
        }

        /// <summary>
        /// Switches between minimized and maximized position states
        /// </summary>
        /// <param name="fromMinimized">True if switching from minimized to maximized, false otherwise</param>
        private void SwitchPositionState(bool fromMinimized)
        {
            if (_gridContainerEntry == null) return;

            // Save current position for current state
            _gridContainerEntry.SetPositionForState(X, Y, fromMinimized);

            // Load position for new state
            Point newPos = _gridContainerEntry.GetPositionForState(!fromMinimized);
            X = newPos.X;
            Y = newPos.Y;
        }

        /// <summary>
        /// Event handler for double-clicking to toggle minimize/maximize state
        /// </summary>
        private void OnMinimizeToggleDoubleClick(object sender, MouseDoubleClickEventArgs e)
        {
            if (e.Button == MouseButtonType.Left)
            {
                IsMinimized = !IsMinimized;
                e.Result = true;
            }
        }

        /// <summary>
        /// Generates color-coded enabled/disabled status text for tooltips
        /// </summary>
        private static string GetEnabledDisabledText(bool isEnabled) => isEnabled ? "<basefont color=\"green\">Enabled" : "<basefont color=\"red\">Disabled";

        /// <summary>
        /// Updates both background and backgroundTexture dimensions
        /// </summary>
        private void UpdateBackgroundDimensions(int width, int height)
        {
            _background.Width = _backgroundTexture.Width = width;
            _background.Height = _backgroundTexture.Height = height;
        }

        /// <summary>
        /// Updates both background and backgroundTexture style properties (hue and alpha)
        /// </summary>
        private void UpdateBackgroundStyle(ushort hue, float alpha)
        {
            _background.Hue = _backgroundTexture.Hue = hue;
            _background.Alpha = _backgroundTexture.Alpha = alpha;
        }
        #endregion

        public GridContainer(World world, uint local, ushort originalContainerGraphic, bool? useGridStyle = null) : base(world, GetWidth(), GetHeight(), GetWidth(2), GetHeight(1), local, 0)
        {
            if (Container == null || world == null || world.Player == null)
            {
                Dispose();
                return;
            }

            #region SET VARS
            _isCorpse = Container.IsCorpse || Container.Graphic == 0x0009;
            if (useGridStyle != null)
                UseOldContainerStyle = !useGridStyle;

            IsPlayerBackpack = LocalSerial == World.Player.Backpack.Serial;

            _gridContainerEntry = GridContainerSaveData.Instance.GetContainer(local);

            _autoSortContainer = _gridContainerEntry.AutoSort;
            StackNonStackableItems = _gridContainerEntry.VisuallyStackNonStackables;
            _sortMode = (GridSortMode)_gridContainerEntry.SortMode;

            // Load minimized state from save data
            bool loadMinimized = _gridContainerEntry.IsMinimized;

            Point lastPos = IsPlayerBackpack ? ProfileManager.CurrentProfile.BackpackGridPosition : _gridContainerEntry.GetPositionForState(loadMinimized);
            if (lastPos == Point.Zero || (lastPos.X == 100 && lastPos.Y == 100)) //Default positions, use last static position
            {
                lastPos.X = _lastX;
                lastPos.Y = _lastY;
            }

            Point savedSize = IsPlayerBackpack ? ProfileManager.CurrentProfile.BackpackGridSize : _gridContainerEntry.GetSize();
            if (savedSize == Point.Zero)
            {
                savedSize.X = GetWidth();
                savedSize.Y = GetHeight();
            }

            IsLocked = IsPlayerBackpack && ProfileManager.CurrentProfile.BackPackLocked;

            _lastWidth = Width = savedSize.X;
            _lastHeight = Height = savedSize.Y;

            X = _isCorpse ? _lastCorpseX : _lastX = lastPos.X;
            Y = _isCorpse ? _lastCorpseY : _lastY = lastPos.Y;

            if (_isCorpse)
            {
                World.Player.ManualOpenedCorpses.Remove(LocalSerial);

                if (World.Player.AutoOpenedCorpses.Contains(LocalSerial) && ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.SkipEmptyCorpse && Container.IsEmpty)
                {
                    IsVisible = false;
                    Dispose();
            return;
                }
            }

            AnchorType = ProfileManager.CurrentProfile.EnableGridContainerAnchor ? ANCHOR_TYPE.NONE : ANCHOR_TYPE.DISABLED;
            _originalContainerItemGraphic = originalContainerGraphic;

            CanMove = true;
            AcceptMouseInput = true;
            #endregion

            BuildBackground();
            BuildTopBar();
            BuildScrollArea();
            BuildLootBag();
            AddControls();

            SlotManager = new GridSlotManager(world, LocalSerial, this, _scrollArea); //Must come after scroll area
            InitializeListView();

            UpdateContainerNameLabel();

            if (ShouldUseOldContainerStyle())
            {
                _skipSave = true; //Avoid unsaving item slots because they have not be set up yet
                OpenOldContainer(local);
                return;
            }

            BuildBorder();
            ResizeWindow(savedSize);

            // Populate and lay out the grid immediately. Content updates are otherwise only
            // triggered when the server sends item packets (see ItemHelpers), so without this
            // an empty container would leave its empty slots stacked at (0,0), and switching
            // from the old container style back to the grid (where the items already exist in
            // the world and no packet arrives) would render an empty grid.
            RequestUpdateContents();

            // Apply minimized state after all controls are created
            if (loadMinimized)
            {
                // Store the current (full) height from savedSize before minimizing
                _heightBeforeMinimize = savedSize.Y > 0 ? savedSize.Y : Height;
                _isMinimized = true;
                // Don't call UpdateMinimizedState here - just apply the minimized dimensions directly
                // to avoid overwriting _heightBeforeMinimize
                ApplyMinimizedDimensions();
            }
        }

        #region Control construction
        private void BuildBackground()
        {
            _background = new AlphaBlendControl()
            {
                Width = Width - (_borderWidth * 2),
                Height = Height - (_borderWidth * 2),
                X = _borderWidth,
                Y = _borderWidth,
                Alpha = (float)ProfileManager.CurrentProfile.ContainerOpacity / 100,
                Hue = ProfileManager.CurrentProfile.Grid_UseContainerHue ? Container.Hue : ProfileManager.CurrentProfile.AltGridContainerBackgroundHue,
                AcceptMouseInput = true,
                CanMove = true
            };
            _background.MouseDoubleClick += OnMinimizeToggleDoubleClick;
            _background.MouseUp += OnBackgroundMouseUp;

            _backgroundTexture = new GumpPicTiled(0) { CanMove = true };
            _backgroundTexture.MouseDoubleClick += OnMinimizeToggleDoubleClick;
        }

        private void BuildTopBar()
        {
            _containerNameLabel = new Label(GetContainerName(), true, 0x0481, 0, ishtml: true)
            {
                X = _borderWidth,
                Y = _borderWidth,
                AcceptMouseInput = true,
                CanMove = true
            };
            _containerNameLabel.MouseDoubleClick += OnMinimizeToggleDoubleClick;
            _containerNameLabel.MouseUp += OnBackgroundMouseUp;

            _searchBox = new StbTextBox(0xFF, 20, 0, true, FontStyle.None, 0x0481)
            {
                X = _borderWidth,
                Y = _borderWidth + LABEL_HEIGHT,
                Multiline = false,
                Width = _background.Width - 18,
                Height = 20
            };
            _searchBox.PlaceHolderText = TazLang.Get("gridcontainer_search", "Search...");
            _searchBox.TextChanged += (sender, e) => { UpdateItems(); };

            _searchClearButton = new NiceButton(_borderWidth + _background.Width - 16, _borderWidth + LABEL_HEIGHT, 16, _searchBox.Height, ButtonAction.Default, TazLang.Get("gridcontainer_clearsearch", "X"));
            _searchClearButton.MouseUp += (sender, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    _searchBox.ClearText();
                    UIManager.SystemChat?.SetFocus();
                }
            };
            _searchClearButton.SetTooltip(TazLang.Get("gridcontainer_clearsearch_tooltip", "Clear search"));

            Texture2D regularGumpIcon = Client.Game.UO.Gumps.GetGump(5839).Texture;
            _openRegularGump = new GumpPic(_background.Width - 25 - _borderWidth, _borderWidth, regularGumpIcon == null ? (ushort)1209 : (ushort)5839, 0);
            _openRegularGump.ContextMenu = GenContextMenu();

            _openRegularGump.MouseUp += (sender, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    _openRegularGump.ContextMenu?.Show();
                }
            };
            _openRegularGump.MouseEnter += (sender, e) => { _openRegularGump.Graphic = regularGumpIcon == null ? (ushort)1210 : (ushort)5840; };
            _openRegularGump.MouseExit += (sender, e) => { _openRegularGump.Graphic = regularGumpIcon == null ? (ushort)1209 : (ushort)5839; };
            _openRegularGump.SetTooltip(TazLang.Get("gridcontainer_controls_tooltip",
                "/c[orange]Grid Container Controls:/cd\n" +
                "Ctrl + Click to lock an item in place\n" +
                "Alt + Click to toggle selection for multi-move\n" +
                "Alt + Double Click to select all similar items\n" +
                "Shift + Click to add an item to your auto loot list\n" +
                "Sort and single click looting can be enabled with the icons on the right side"));
            _quickDropBackpack = new ResizableStaticPic(World.Player.Backpack.DisplayedGraphic, 20, 20)
            {
                X = Width - _openRegularGump.Width - 20 - _borderWidth,
                Y = _borderWidth
            };
            _quickDropBackpack.MouseUp += (sender, e) =>
            {
                if (e.Button == MouseButtonType.Left && _quickDropBackpack.MouseIsOver)
                {
                    if (Client.Game.UO.GameCursor.ItemHold.Enabled)
                    {
                        GameActions.DropItem(Client.Game.UO.GameCursor.ItemHold.Serial, 0xFFFF, 0xFFFF, 0, World.Player.Backpack);
                    }
                    else if (_isCorpse)
                    {
                        ProfileManager.CurrentProfile.CorpseSingleClickLoot ^= true;
                        _quickDropBackpack.SetTooltip(QuickLootTooltip);
                    }
                    else
                    {
                        _quickLootThisContainer ^= true;
                        _quickDropBackpack.SetTooltip(QuickLootTooltip);
                    }
                }
            };
            _quickDropBackpack.MouseEnter += (sender, e) => { _quickDropBackpack.Hue = 0x34; };
            _quickDropBackpack.MouseExit += (sender, e) => { _quickDropBackpack.Hue = 0; };
            _quickDropBackpack.SetTooltip(QuickLootTooltip);

            _sortContents = new GumpPic(_quickDropBackpack.X - 20, _borderWidth, 1210, 0);
            _sortContents.ContextMenu = GenSortContextMenu();
            _sortContents.MouseUp += (sender, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    if (Keyboard.Alt)
                    {
                        _autoSortContainer ^= true;
                        _gridContainerEntry.AutoSort = _autoSortContainer;
                        _sortContents.SetTooltip(SortButtonTooltip);
                    }
                    else
                    {
                        _sortContents.ContextMenu?.Show();
                    }
                }
            };
            _sortContents.MouseEnter += (sender, e) => { _sortContents.Graphic = 1209; };
            _sortContents.MouseExit += (sender, e) => { _sortContents.Graphic = 1210; };
            _sortContents.SetTooltip(SortButtonTooltip);
        }

        private void BuildScrollArea()
        {
            _scrollArea = new GridScrollArea(
                _background.X,
                LABEL_HEIGHT + TOP_BAR_HEIGHT + _background.Y,
                _background.Width,
                _background.Height - LABEL_HEIGHT - TOP_BAR_HEIGHT
                );

            _scrollArea.MouseUp += ScrollArea_MouseUp;
            _scrollArea.MouseDoubleClick += (sender, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    // Only toggle if clicking on empty space (not on grid items)
                    IGui clickedControl = UIManager.MouseOverControl;
                    if (clickedControl == _scrollArea)
                    {
                        OnMinimizeToggleDoubleClick(sender, e);
                    }
                }
            };
        }

        private void BuildLootBag()
        {
            _setLootBag = new NiceButton(0, Height - 20, 100, 20, ButtonAction.Default, TazLang.Get("gridcontainer_setlootbag", "Set loot bag")) { IsSelectable = false };
            _setLootBag.IsVisible = _isCorpse;
            _setLootBag.SetTooltip(TazLang.Get("gridcontainer_setlootbag_tooltip", "For double click looting only"));
            _setLootBag.MouseUp += (s, e) =>
            {
                GameActions.Print(World, Resources.ResGumps.TargetContainerToGrabItemsInto);
                World.TargetManager.SetTargeting(CursorTarget.SetGrabBag, 0, TargetType.Neutral);
            };
        }

        private void AddControls()
        {
            Add(_background);
            Add(_backgroundTexture);
            Add(_containerNameLabel);
            _searchBoxBackground = new AlphaBlendControl(0.5f)
            {
                Hue = 0x0481,
                Width = _searchBox.Width,
                Height = _searchBox.Height
            };
            _searchBox.Add(_searchBoxBackground);
            Add(_searchBox);
            Add(_searchClearButton);
            Add(_openRegularGump);
            Add(_quickDropBackpack);
            Add(_sortContents);
            Add(_scrollArea);
            Add(_setLootBag);
        }
        #endregion

        /// <summary>
        ///     Checks whether the container should be opened in the 'old' style.
        ///     <br />
        ///     Container style is determined, in order, by:
        ///     <list type="number">
        ///         <item>Explicit open request (such as when clicking the `Return to grid container view` button</item>
        ///         <item>The value of the container's <see cref="GridContainerEntry.UseOriginalContainer" /> (if exists)</item>
        ///         <item>The global preference in <see cref="Profile.GridContainersDefaultToOldStyleView" /></item>
        ///     </list>
        /// </summary>
        /// <returns></returns>
        private bool ShouldUseOldContainerStyle()
        {
            // If the container has no stored preference and was not opened in a specific mode, use the global default
            if (_gridContainerEntry.UseOriginalContainer == null && UseOldContainerStyle == null)
            {
                // Corpses can override the global "open all containers in original style" preference
                // via their own dedicated setting.
                if (_isCorpse)
                {
                    switch (ProfileManager.CurrentProfile.CorpseContainerStyle)
                    {
                        case CorpseContainerStyle.Grid: return false;
                        case CorpseContainerStyle.Original: return true;
                        // CorpseContainerStyle.OldGridLoot falls through to the global default
                    }
                }

                return ProfileManager.CurrentProfile.GridContainersDefaultToOldStyleView;
            }

            // Next, if the open request was made with a specific mode (i.e., useGridStyle != nul), use that,
            // otherwise, fallback to the stored preference (which, if we got here is *not* null, or we'd fall into the default case above)
            return UseOldContainerStyle ?? _gridContainerEntry.UseOriginalContainer.Value;
        }

        public override GumpType GumpType => GumpType.GridContainer;

        /// <summary>Height of the container when minimized (just the label area plus borders).</summary>
        private int MinimizedHeight => LABEL_HEIGHT + (_borderWidth * 2);

        /// <summary>
        /// Collapses the window to <see cref="MinimizedHeight"/> and shrinks the background to the
        /// label strip. Shared by the runtime minimize toggle and the initial-load path.
        /// </summary>
        private void ApplyMinimizedHeight()
        {
            ResizeWindow(new Point(Width, MinimizedHeight));
            Height = MinimizedHeight;

            if (_background != null) _background.Height = LABEL_HEIGHT;
            if (_backgroundTexture != null) _backgroundTexture.Height = LABEL_HEIGHT;

            OnResize();
        }

        private void UpdateMinimizedState()
        {
            if (_isMinimized)
            {
                // Store current height before minimizing
                _heightBeforeMinimize = Height;

                SwitchPositionState(false);
                SetControlsVisibility(false);
                ApplyMinimizedHeight();
            }
            else
            {
                SwitchPositionState(true);
                SetControlsVisibility(true);

                // Restore original height (fallback to a reasonable default if not set)
                int restoredHeight = _heightBeforeMinimize > 0 ? _heightBeforeMinimize : GetHeight();

                ResizeWindow(new Point(Width, restoredHeight));
                Height = restoredHeight;

                if (_background != null) _background.Height = Height - (_borderWidth * 2);
                if (_backgroundTexture != null) _backgroundTexture.Height = Height - (_borderWidth * 2);

                OnResize();
            }

            WantUpdateSize = true;

            _gridContainerEntry?.UpdateSaveDataEntry(this);
        }

        /// <summary>
        /// Applies minimized dimensions without storing height (used on initial load)
        /// </summary>
        private void ApplyMinimizedDimensions()
        {
            // Hide all controls except container name, background, and border
            SetControlsVisibility(false);
            ApplyMinimizedHeight();
            WantUpdateSize = true;
        }

        private ContextMenuControl GenContextMenu()
        {
            var control = new ContextMenuControl(this);
            control.Add(new ContextMenuItemEntry(TazLang.Get("gridcontainer_openoriginal", "Open Original View"), () =>
            {
                UseOldContainerStyle = true;
                OpenOldContainer(LocalSerial);
            }));

            control.Add(new ContextMenuItemEntry
            (
                TazLang.Get("gridcontainer_defaultoriginal", "Open New Containers in the Original View"), () =>
                {
                    ProfileManager.CurrentProfile.GridContainersDefaultToOldStyleView = !ProfileManager.CurrentProfile.GridContainersDefaultToOldStyleView;
                    _openRegularGump.ContextMenu = GenContextMenu();
                }, true, ProfileManager.CurrentProfile.GridContainersDefaultToOldStyleView
            ));

            control.Add(new ContextMenuItemEntry(
                TazLang.Get("gridcontainer_view_label", "View:"),
                new[]
                {
                    TazLang.Get("gridcontainer_view_default", "Default"),
                    TazLang.Get("gridcontainer_view_grid_short", "Grid"),
                    TazLang.Get("gridcontainer_view_list_short", "List")
                },
                GetContainerViewModeOverrideIndex(),
                SetContainerViewModeOverrideIndex
            ));

            control.Add(new ContextMenuItemEntry(TazLang.Get("gridcontainer_stacksimilar", "Stack Similar Items in the Original View"), () =>
            {
                StackNonStackableItems = !StackNonStackableItems;
                _openRegularGump.ContextMenu = GenContextMenu();
            }, true, StackNonStackableItems));

            control.Add(new ContextMenuItemEntry(TazLang.Get("gridcontainer_openhighlightsettings", "Open Grid View Highlight Settings"), () =>
            {
                GridHighlightMenu.Open(World);
            }));

            if (Container != World.Player.Backpack)
            {
                control.Add(new ContextMenuItemEntry(TazLang.Get("gridcontainer_autolootthis", "Autoloot this container"), () =>
                {
                    AutoLootManager.Instance.ForceLootContainer(LocalSerial);
                }));
            }

            // Re-applies highlight rules and colors; useful if item highlights desync after SOS loot or container refresh.
            control.Add(new ContextMenuItemEntry(TazLang.Get("gridcontainer_refreshhighlights", "Refresh item highlights"), GridHighlightData.RecheckMatchStatus));

            control.Add(new ContextMenuItemEntry(TazLang.Get("gridcontainer_renamecontainer", "Rename container"), () =>
            {
                new PromptPopupWindow(TazLang.Get("gridcontainer_rename_title", "Rename Container"), TazLang.Get("gridcontainer_rename_desc", "Type in a custom name for this container."), s =>
                {
                    _gridContainerEntry?.CustomName = s;
                    UpdateContainerNameLabel();
                }, TazLang.Get("gridcontainer_save", "Save"), TazLang.Get("gridcontainer_reset", "Reset"), () =>
                {
                    _gridContainerEntry?.CustomName = null;
                    UpdateContainerNameLabel();
                }, GetContainerName(true));
            }));

            return control;
        }

        private ContextMenuControl GenSortContextMenu()
        {
            var control = new ContextMenuControl(this);

            control.Add(new ContextMenuItemEntry(TazLang.Get("gridcontainer_sortbygraphichue", "Sort by Graphic + Hue"), () =>
            {
                _sortMode = GridSortMode.GraphicAndHue;
                _sortContents.ContextMenu = GenSortContextMenu();
                _sortContents.SetTooltip(SortButtonTooltip);
                UpdateItems(true);
                _gridContainerEntry.UpdateSaveDataEntry(this);
            }, true, _sortMode == GridSortMode.GraphicAndHue));

            control.Add(new ContextMenuItemEntry(TazLang.Get("gridcontainer_sortbyname", "Sort by Name"), () =>
            {
                _sortMode = GridSortMode.Name;
                _sortContents.ContextMenu = GenSortContextMenu();
                _sortContents.SetTooltip(SortButtonTooltip);
                UpdateItems(true);
                _gridContainerEntry.UpdateSaveDataEntry(this);
            }, true, _sortMode == GridSortMode.Name));

            control.Add(new ContextMenuItemEntry(TazLang.Get("gridcontainer_sortbylayer", "Sort by Layer"), () =>
            {
                _sortMode = GridSortMode.Layer;
                _sortContents.ContextMenu = GenSortContextMenu();
                _sortContents.SetTooltip(SortButtonTooltip);
                UpdateItems(true);
                _gridContainerEntry.UpdateSaveDataEntry(this);
            }, true, _sortMode == GridSortMode.Layer));

            return control;
        }
        /// <summary>
        /// Border width implied by the profile's current border style. Used by the static size
        /// helpers, which run before an instance exists and so can't read <see cref="_borderWidth"/>.
        /// </summary>
        private static int GetCurrentBorderWidth()
        {
            var style = (BorderStyle)ProfileManager.CurrentProfile.Grid_BorderStyle;

            if (style != BorderStyle.Default && _borderStyleConfig.TryGetValue(style, out (int graphic, int borderSize) config))
                return config.borderSize;

            return DEFAULT_BORDER_WIDTH;
        }

        private static int GetWidth(int columns = -1)
        {
            // Use default columns if none are specified
            if (columns < 0)
                columns = ProfileManager.CurrentProfile.Grid_DefaultColumns;

            // Calculate the total width of the grid container.
            // The layout (see GridSlotManager.SetGridPositions) starts each row at x = X_SPACING
            // and advances by GridItemSize + X_SPACING per column, so N columns need a leading
            // gap plus one trailing gap per column: X_SPACING * (columns + 1). Budgeting only
            // X_SPACING * columns leaves the last column landing exactly on the wrap boundary,
            // pushing it to the next row (e.g. asking for 5 columns only fits 4).
            return (GetCurrentBorderWidth() * 2)          // Borders on the left and right
                    + GridScrollArea.SCROLLBAR_WIDTH      // Width of the scroll bar
                    + (GridItemSize * columns)            // Total width of grid items
                    + (X_SPACING * (columns + 1));        // Leading gap + spacing after each column
        }
        private static int GetHeight(int rows = -1)
        {
            // Use default rows if none are specified
            if (rows < 0)
                rows = ProfileManager.CurrentProfile.Grid_DefaultRows;

            // Calculate the total height of the grid container
            return LABEL_HEIGHT                 // Height of the container name label
                   + TOP_BAR_HEIGHT             // Height of the top bar
                   + (GetCurrentBorderWidth() * 2)  // Borders on the top and bottom
                   + ((GridItemSize + Y_SPACING) * rows); // Total height of grid items with spacing
        }

        public override void Save(XmlTextWriter writer)
        {
            base.Save(writer);

            if (!_skipSave)
            {
                _gridContainerEntry.UpdateSaveDataEntry(this);
            }

            if (IsPlayerBackpack && ProfileManager.CurrentProfile != null)
            {
                ProfileManager.CurrentProfile.BackpackGridPosition = Location;
                ProfileManager.CurrentProfile.BackpackGridSize = new Point(Width, Height);
            }

            Item item = World.Items.Get(LocalSerial);
            if (item is not null)
            {
                writer.WriteAttributeString("parent", item.Container.ToString());
            }

            writer.WriteAttributeString("ogContainer", _originalContainerItemGraphic.ToString());
        }
        public override void Restore(XmlElement xml)
        {
            base.Restore(xml);
            GameActions.DoubleClickQueued(LocalSerial);
        }

        private void ScrollArea_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtonType.Left && _scrollArea.MouseIsOver)
            {
                if (Client.Game.UO.GameCursor.ItemHold.Enabled)
                    GameActions.DropItem(Client.Game.UO.GameCursor.ItemHold.Serial, 0xFFFF, 0xFFFF, 0, LocalSerial);
                else if (World.TargetManager.IsTargeting && !ProfileManager.CurrentProfile.DisableTargetingGridContainers)
                    World.TargetManager.Target(LocalSerial);
            }
            else if (e.Button == MouseButtonType.Right)
            {
                InvokeMouseCloseGumpWithRClick();
            }
        }

        private void OpenOldContainer(uint serial)
        {
            UIManager.GetGump<ContainerGump>(serial)?.Dispose();

            ushort graphic = _originalContainerItemGraphic;

            if (Client.Game.UO.Version >= Utility.ClientVersion.CV_706000 &&
                ProfileManager.CurrentProfile?.UseLargeContainerGumps == true)
            {
                switch (graphic)
                {
                    case 0x0048 when Client.Game.UO.Gumps.GetGump(0x06E8).Texture != null:
                        graphic = 0x06E8;
                        break;
                    case 0x0049 when Client.Game.UO.Gumps.GetGump(0x9CDF).Texture != null:
                        graphic = 0x9CDF;
                        break;
                    case 0x0051 when Client.Game.UO.Gumps.GetGump(0x06E7).Texture != null:
                        graphic = 0x06E7;
                        break;
                    case 0x003E when Client.Game.UO.Gumps.GetGump(0x06E9).Texture != null:
                        graphic = 0x06E9;
                        break;
                    case 0x004D when Client.Game.UO.Gumps.GetGump(0x06EA).Texture != null:
                        graphic = 0x06EA;
                        break;
                    case 0x004E when Client.Game.UO.Gumps.GetGump(0x06E6).Texture != null:
                        graphic = 0x06E6;
                        break;
                    case 0x004F when Client.Game.UO.Gumps.GetGump(0x06E5).Texture != null:
                        graphic = 0x06E5;
                        break;
                    case 0x004A when Client.Game.UO.Gumps.GetGump(0x9CDD).Texture != null:
                        graphic = 0x9CDD;
                        break;
                    case 0x0044 when Client.Game.UO.Gumps.GetGump(0x9CE3).Texture != null:
                        graphic = 0x9CE3;
                        break;
                }
            }

            World.ContainerManager.CalculateContainerPosition(serial, graphic);

            var container = new ContainerGump(World, this.Container.Serial, graphic, true, true)
            {
                X = World.ContainerManager.X,
                Y = World.ContainerManager.Y,
                InvalidateContents = true
            };

            UIManager.Add(container);
            Dispose();
        }

        private void UpdateItems(bool overrideSort = false)
        {
            if (Container == null)
            {
                Dispose();
                return;
            }

            if (_autoSortContainer)
                overrideSort = true;

            List<Item> sortedContents = (ProfileManager.CurrentProfile is null || ProfileManager.CurrentProfile.GridContainerSearchMode == 0) && !string.IsNullOrEmpty(_searchBox.Text)
                ? SlotManager.SearchResults(_searchBox.Text)
                : GridSlotManager.GetItemsInContainer(World, Container, _sortMode, overrideSort);

            SlotManager.RebuildContainer(sortedContents, _searchBox.Text, overrideSort);

            // Update name AFTER slot manager rebuild, or we get stale data
            UpdateContainerNameLabel();
            InvalidateContents = false;
        }

        protected override void UpdateContents()
        {
            if (InvalidateContents && !IsDisposed)
                UpdateItems();
        }

        /// <summary>
        /// Occurs when a mouse up even is issued on the background pane.
        /// The background panel is the one holding the container's name and is
        /// the only visible part when minimized.
        /// </summary>
        /// <param name="sender">The event's sender. May be the background, title label, etc.</param>
        /// <param name="e">The mouse event's arguments</param>
        private void OnBackgroundMouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtonType.Left)
                return;

            // Verify the sender is actually what we expect it to be
            if (sender is not Control { MouseIsOver: true })
                return;

            if (Client.Game.UO.GameCursor.ItemHold.Enabled)
            {
                // Issue a direct drop item command and let the underlying `UpdateContainerItem`
                // mechanisms take care of actual placement
                GameActions.DropItem(
                    Client.Game.UO.GameCursor.ItemHold.Serial,
                    0xFFFF,
                    0xFFFF,
                    0,
                    LocalSerial
                );
            }
            else if (World.TargetManager.IsTargeting && !ProfileManager.CurrentProfile.DisableTargetingGridContainers)
            {
                // Let a target cursor pick the bag itself, matching how an empty grid slot behaves
                World.TargetManager.Target(LocalSerial);
            }
        }

        protected override void OnMouseExit(int x, int y)
        {
            if (_isCorpse && Container != null && Container == SelectedObject.CorpseObject)
                SelectedObject.CorpseObject = null;
        }

        public override bool Contains(int x, int y)
        {
            if (_isMinimized)
            {
                // When minimized, only accept mouse input within the minimized bounds
                return x >= 0 && x < Width && y >= 0 && y < (LABEL_HEIGHT + (_borderWidth * 2));
            }
            return base.Contains(x, y);
        }

        protected override void OnMove(int x, int y)
        {
            base.OnMove(x, y);

            if (_gridContainerEntry != null)
            {
                _gridContainerEntry.SetPositionForState(X, Y, IsMinimized);
            }

            // Backpack special handling
            if (IsPlayerBackpack)
            {
                ProfileManager.CurrentProfile.BackpackGridPosition = new Point(X, Y);
            }
        }

        public override void Dispose()
        {
            if (_isCorpse)
            {
                _lastCorpseX = X;
                _lastCorpseY = Y;
            }
            else
            {
                _lastX = X;
                _lastY = Y;
            }

            Item currentContainer = Container;

            if (currentContainer != null)
            {
                if (currentContainer == SelectedObject.CorpseObject)
                    SelectedObject.CorpseObject = null;

                Item bank = World.Player.FindItemByLayer(Layer.Bank);

                if (bank != null && (currentContainer.Serial == bank.Serial || currentContainer.Container == bank.Serial))
                {
                    for (LinkedObject i = currentContainer.Items; i != null; i = i.Next)
                    {
                        var child = (Item)i;

                        if (child.Container == currentContainer)
                        {
                            UIManager.GetGump<GridContainer>(child)?.Dispose();
                            UIManager.GetGump<ContainerGump>(child)?.Dispose();
                        }
                    }
                }
            }

            if (SlotManager != null && !_skipSave && SlotManager.ItemPositions.Count > 0 && !_isCorpse)
                _gridContainerEntry.UpdateSaveDataEntry(this);

            // Dispose of the event handlers
            if (_background != null)
            {
                _background.MouseDoubleClick -= OnMinimizeToggleDoubleClick;
                _background.MouseUp -= OnBackgroundMouseUp;
            }

            if (_backgroundTexture != null)
                _backgroundTexture.MouseDoubleClick -= OnMinimizeToggleDoubleClick;

            if (_containerNameLabel != null)
                _containerNameLabel.MouseUp -= OnBackgroundMouseUp;

            DisposeListView();
            base.Dispose();
        }

        public override void PreDraw()
        {
            base.PreDraw();

            if (IsDisposed)
                return;

            Item item = Container;

            if (item == null || item.IsDestroyed)
            {
                Dispose();
                return;
            }

            if (item.IsCorpse && item.OnGround && item.Distance > 3)
            {
                Dispose();
                return;
            }

            // Maintain minimized height when minimized
            if (_isMinimized && Height != MinimizedHeight)
            {
                Height = MinimizedHeight;
                _background.Height = LABEL_HEIGHT;
                _backgroundTexture.Height = LABEL_HEIGHT;
                BorderControl.Width = Width;
                BorderControl.Height = Height;

                // Manually reposition the resize button since we're bypassing ResizeWindow's min height
                RepositionResizeButton();

                WantUpdateSize = true;
            }

            if (!_isMinimized && (_lastWidth != Width || _lastHeight != Height || _lastGridItemSize != GridItemSize))
            {
                _lastGridItemSize = GridItemSize;
                LayoutControls();
                _lastHeight = Height;
                _lastWidth = Width;

                if (IsPlayerBackpack)
                    ProfileManager.CurrentProfile.BackpackGridSize = new Point(Width, Height);
                else
                    _gridContainerEntry?.UpdateSaveDataEntry(this); // Save size for non-backpack containers

                RequestUpdateContents();
            }

            if (IsPlayerBackpack && Location != ProfileManager.CurrentProfile.BackpackGridPosition)
                ProfileManager.CurrentProfile.BackpackGridPosition = Location;

            if (UIManager.MouseOverControl != null &&
                (UIManager.MouseOverControl == this || UIManager.MouseOverControl.RootParent == this))
            {
                SelectedObject.Object = item;
                if (item.IsCorpse)
                    SelectedObject.CorpseObject = item;
            }
        }

        /// <summary>
        /// Resolves the container's display name: custom name if set, otherwise the item's name,
        /// falling back to "a container".
        /// </summary>
        private string ResolveRawName() =>
            GridContainerEntry?.CustomName.NotNullNotEmpty() == true ? GridContainerEntry.CustomName :
            !string.IsNullOrEmpty(Container.Name) ? Container.Name : "a container";

        private void UpdateContainerNameLabel()
        {
            string rawName = ResolveRawName();

            string countSuffix = SlotManager != null ? $" ({SlotManager.ContainerContents.Count})" : "";

            // Available width = from left border to the sort button, minus a small padding
            int availableWidth = _sortContents.X - _borderWidth - 2;

            // Start with the standard 21-char truncation
            string displayName = rawName.Truncate(21);
            _containerNameLabel.Text = displayName + countSuffix;

            // If the rendered text is too wide, trim the name char by char until it fits
            if (_containerNameLabel.Width > availableWidth)
            {
                string baseName = displayName.EndsWith("...") ? displayName[..^3] : displayName;

                while (_containerNameLabel.Width > availableWidth && baseName.Length > 0)
                {
                    baseName = baseName[..^1];
                    _containerNameLabel.Text = (baseName.Length > 0 ? baseName + "..." : "") + countSuffix;
                }
            }

            _containerNameLabel.SetTooltip(rawName);
        }

        private string GetContainerName(bool skipCount = false, bool truncate = true)
        {
            string containerName = ResolveRawName();

            if (truncate)
                containerName = containerName.Truncate(21);

            if (!skipCount && SlotManager != null)
            {
                containerName += $" ({SlotManager.ContainerContents.Count})";
            }

            return containerName;
        }

        public void OptionsUpdated()
        {
            float newAlpha = ProfileManager.CurrentProfile.ContainerOpacity / 100f;
            ushort newHue = ProfileManager.CurrentProfile.Grid_UseContainerHue
                ? Container.Hue
                : ProfileManager.CurrentProfile.AltGridContainerBackgroundHue;

            UpdateBackgroundStyle(newHue, newAlpha);
            BorderControl.Hue = newHue;
            BorderControl.Alpha = newAlpha;

            AnchorType = ProfileManager.CurrentProfile.EnableGridContainerAnchor
                ? ANCHOR_TYPE.NONE
                : ANCHOR_TYPE.DISABLED;

            BuildBorder();
            RequestUpdateContents();
        }

        public static void UpdateAllGridContainers() => UIManager.ForEach<GridContainer>(c => c.OptionsUpdated());

        public void HandleObjectMessage(Entity parent, string text, ushort hue)
        {
            if (parent != null)
            {
                SlotManager.FindItem(parent.Serial)?.AddText(text, hue);
            }
        }

        public void BuildBorder()
        {
            int graphic = 0, borderSize = 0;
            var currentStyle = (BorderStyle)ProfileManager.CurrentProfile.Grid_BorderStyle;

            if (currentStyle == BorderStyle.Default)
            {
                BorderControl.DefaultGraphics();
                _backgroundTexture.IsVisible = false;
                _background.IsVisible = true;
                _borderWidth = 4;
            }
            else if (_borderStyleConfig.TryGetValue(currentStyle, out (int graphic, int borderSize) config))
            {
                graphic = config.graphic;
                borderSize = config.borderSize;

                // Apply border graphics for non-default styles
                BorderControl.T_Left = (ushort)graphic;
                BorderControl.H_Border = (ushort)(graphic + 1);
                BorderControl.T_Right = (ushort)(graphic + 2);
                BorderControl.V_Border = (ushort)(graphic + 3);

                _backgroundTexture.Graphic = (ushort)(graphic + 4);
                _backgroundTexture.IsVisible = true;
                _backgroundTexture.Hue = _background.Hue;
                BorderControl.Hue = _background.Hue;
                BorderControl.Alpha = _background.Alpha;
                _background.IsVisible = false;

                BorderControl.V_Right_Border = (ushort)(graphic + 5);
                BorderControl.B_Left = (ushort)(graphic + 6);
                BorderControl.H_Bottom_Border = (ushort)(graphic + 7);
                BorderControl.B_Right = (ushort)(graphic + 8);
                BorderControl.BorderSize = borderSize;
                _borderWidth = borderSize;
            }
            LayoutControls();
            OnResize();

            BorderControl.IsVisible = !ProfileManager.CurrentProfile.Grid_HideBorder;
        }

        /// <summary>
        /// Positions and sizes every child control from the current <see cref="Control.Width"/>,
        /// <see cref="Control.Height"/>, and <see cref="_borderWidth"/>. Shared by initial layout
        /// (via <see cref="BuildBorder"/>) and the resize path in <see cref="PreDraw"/>; every
        /// assignment is idempotent so calling it repeatedly is safe.
        /// </summary>
        private void LayoutControls()
        {
            int adjustedWidth = Width - (_borderWidth * 2);
            int adjustedHeight = Height - (_borderWidth * 2);

            // Background + tiled texture
            _background.X = _background.Y = _borderWidth;
            _backgroundTexture.X = _background.X;
            _backgroundTexture.Y = _background.Y;
            UpdateBackgroundDimensions(adjustedWidth, adjustedHeight);
            UpdateBackgroundStyle(_background.Hue, _background.Alpha);

            // Top-bar buttons (right-aligned, in from the right edge)
            _openRegularGump.X = Width - _openRegularGump.Width - _borderWidth;
            _quickDropBackpack.X = _openRegularGump.X - _quickDropBackpack.Width;
            _sortContents.X = _quickDropBackpack.X - _sortContents.Width;
            _quickDropBackpack.Y = _sortContents.Y = _openRegularGump.Y = _borderWidth;

            // Search box + clear button
            _searchBox.X = _borderWidth;
            _searchBox.Y = _borderWidth + LABEL_HEIGHT;
            _searchBox.Width = adjustedWidth - 18;
            _searchBoxBackground.Width = _searchBox.Width;
            _searchClearButton.X = _borderWidth + adjustedWidth - 16;
            _searchClearButton.Y = _borderWidth + LABEL_HEIGHT;

            // Scroll area (below the top bar)
            _scrollArea.X = _background.X;
            _scrollArea.Y = LABEL_HEIGHT + TOP_BAR_HEIGHT + _background.Y;
            _scrollArea.Width = adjustedWidth;
            _scrollArea.Height = adjustedHeight - LABEL_HEIGHT - TOP_BAR_HEIGHT;

            // Set-loot-bag button (corpses only, pinned to the bottom)
            _setLootBag.Y = Height - 20;
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (CUOEnviroment.Debug)
                batcher.DrawString(Renderer.Fonts.Bold, LocalSerial.ToString(), x, y - 40, ShaderHueTranslator.GetHueVector(32));
            return base.Draw(batcher, x, y);
        }

        public static void OpenOrUpdate(uint serial, ushort graphic)
        {
            GridContainer gridContainer = UIManager.GetGump<GridContainer>(serial);
            if (gridContainer != null)
            {
                if (gridContainer.IsMinimized)
                    gridContainer.IsMinimized = false;
                gridContainer.BringOnTop();
                gridContainer.RequestUpdateContents();
            }
            else
                UIManager.Add(new GridContainer(World.Instance, serial, graphic));
        }
   }
}
