#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Gumps.GridHighLight;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.UI.Gumps
{
    public class GridContainer : ResizableGump
    {
        #region CONSTANTS
        private const int X_SPACING = 1, Y_SPACING = 1;
        private const int TOP_BAR_HEIGHT = 20;
        private const int LABEL_HEIGHT = 20;
        #endregion

        #region private static vars
        private static int lastX = 100, lastY = 100, lastCorpseX = 100, lastCorpseY = 100;
        private static int gridItemSize => (int)Math.Round(50 * (ProfileManager.CurrentProfile.GridContainersScale / 100f));
        private static int borderWidth = 4;

        private static readonly Dictionary<BorderStyle, (int graphic, int borderSize)> BorderStyleConfig = new Dictionary<BorderStyle, (int graphic, int borderSize)>
        {
            { BorderStyle.Style1, (3500, 26) },
            { BorderStyle.Style2, (5054, 12) },
            { BorderStyle.Style3, (5120, 10) },
            { BorderStyle.Style4, (9200, 7) },
            { BorderStyle.Style5, (9270, 10) },
            { BorderStyle.Style6, (9300, 4) },
            { BorderStyle.Style7, (9260, 17) },
            { BorderStyle.Style8, (40303, 16) }
        };
        #endregion

        #region private readonly vars
        private readonly AlphaBlendControl background;
        private readonly AlphaBlendControl searchBoxBackground;
        private readonly Label containerNameLabel;
        private readonly StbTextBox searchBox;
        private readonly GumpPic openRegularGump, sortContents;
        private readonly ResizableStaticPic quickDropBackpack;
        private readonly GumpPicTiled backgroundTexture;
        private readonly NiceButton setLootBag, searchClearButton;
        private readonly bool isCorpse = false;
        #endregion

        #region private vars
        private Item container => World.Items.Get(LocalSerial);
        private float lastGridItemScale = (ProfileManager.CurrentProfile.GridContainersScale / 100f);
        private int lastWidth = GetWidth(), lastHeight = GetHeight();
        private bool quickLootThisContainer = false;
        public bool? UseOldContainerStyle = null;
        private bool autoSortContainer = false;
        private GridSortMode sortMode = GridSortMode.GraphicAndHue;

        private bool skipSave = false;
        private readonly ushort originalContainerItemGraphic;

        private GridScrollArea scrollArea;
        private bool _isMinimized;
        private int _heightBeforeMinimize;
        #endregion

        #region private tooltip vars
        private string quickLootStatus => GetEnabledDisabledText(ProfileManager.CurrentProfile.CorpseSingleClickLoot);
        private string quickLootTooltip
        {
            get
            {
                if (isCorpse)
                    return $"Drop an item here to send it to your backpack.<br><br>Click this icon to enable/disable single-click looting for corpses.<br>   Currently {quickLootStatus}";
                else
                    return $"Drop an item here to send it to your backpack.<br><br>Click this icon to enable/disable single-click loot for this container while it remains open.<br>   Currently {GetEnabledDisabledText(quickLootThisContainer)}";
            }

        }
        private string sortButtonTooltip
        {
            get
            {
                string status = GetEnabledDisabledText(autoSortContainer);
                string sortModeText = sortMode == GridSortMode.Name ? "Name" : "Graphic + Hue";
                return $"Sort this container.<br>Left click to show sort options<br>Alt + Click to enable auto sort<br>Current sort: {sortModeText}<br>Auto sort currently {status}";
            }
        }

        private GridContainerEntry gridContainerEntry;
        #endregion

        #region public vars
        public GridContainerEntry GridContainerEntry => gridContainerEntry;
        public readonly bool IsPlayerBackpack = false;
        public bool StackNonStackableItems = false;
        public bool AutoSortContainer => autoSortContainer;
        public GridSortMode SortMode => sortMode;
        public GridSlotManager SlotManager;

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
            if (searchBox != null) searchBox.IsVisible = visible;
            if (searchClearButton != null) searchClearButton.IsVisible = visible;
            if (openRegularGump != null) openRegularGump.IsVisible = visible;
            if (quickDropBackpack != null) quickDropBackpack.IsVisible = visible;
            if (sortContents != null) sortContents.IsVisible = visible;
            if (scrollArea != null) scrollArea.IsVisible = visible;
            if (setLootBag != null) setLootBag.IsVisible = visible && isCorpse;

            // Find and toggle resize button
            foreach (Control child in Children)
            {
                if (child is Button)
                {
                    child.IsVisible = visible;
                    break;
                }
            }
        }

        /// <summary>
        /// Switches between minimized and maximized position states
        /// </summary>
        /// <param name="fromMinimized">True if switching from minimized to maximized, false otherwise</param>
        private void SwitchPositionState(bool fromMinimized)
        {
            if (gridContainerEntry == null) return;

            // Save current position for current state
            gridContainerEntry.SetPositionForState(X, Y, fromMinimized);

            // Load position for new state
            Point newPos = gridContainerEntry.GetPositionForState(!fromMinimized);
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
            background.Width = backgroundTexture.Width = width;
            background.Height = backgroundTexture.Height = height;
        }

        /// <summary>
        /// Updates both background and backgroundTexture style properties (hue and alpha)
        /// </summary>
        private void UpdateBackgroundStyle(ushort hue, float alpha)
        {
            background.Hue = backgroundTexture.Hue = hue;
            background.Alpha = backgroundTexture.Alpha = alpha;
        }
        #endregion

        public GridContainer(World world, uint local, ushort originalContainerGraphic, bool? useGridStyle = null) : base(world, GetWidth(), GetHeight(), GetWidth(2), GetHeight(1), local, 0)
        {
            if (container == null || world == null || world.Player == null)
            {
                Dispose();
                return;
            }

            #region SET VARS
            isCorpse = container.IsCorpse || container.Graphic == 0x0009;
            if (useGridStyle != null)
                UseOldContainerStyle = !useGridStyle;

            IsPlayerBackpack = LocalSerial == World.Player.Backpack.Serial;

            gridContainerEntry = GridContainerSaveData.Instance.GetContainer(local);

            autoSortContainer = gridContainerEntry.AutoSort;
            StackNonStackableItems = gridContainerEntry.VisuallyStackNonStackables;
            sortMode = (GridSortMode)gridContainerEntry.SortMode;

            // Load minimized state from save data
            bool loadMinimized = gridContainerEntry.IsMinimized;

            Point lastPos = IsPlayerBackpack ? ProfileManager.CurrentProfile.BackpackGridPosition : gridContainerEntry.GetPositionForState(loadMinimized);
            if (lastPos == Point.Zero || (lastPos.X == 100 && lastPos.Y == 100)) //Default positions, use last static position
            {
                lastPos.X = lastX;
                lastPos.Y = lastY;
            }

            Point savedSize = IsPlayerBackpack ? ProfileManager.CurrentProfile.BackpackGridSize : gridContainerEntry.GetSize();
            if (savedSize == Point.Zero)
            {
                savedSize.X = GetWidth();
                savedSize.Y = GetHeight();
            }

            IsLocked = IsPlayerBackpack && ProfileManager.CurrentProfile.BackPackLocked;

            lastWidth = Width = savedSize.X;
            lastHeight = Height = savedSize.Y;

            X = isCorpse ? lastCorpseX : lastX = lastPos.X;
            Y = isCorpse ? lastCorpseY : lastY = lastPos.Y;

            if (isCorpse)
            {
                World.Player.ManualOpenedCorpses.Remove(LocalSerial);

                if (World.Player.AutoOpenedCorpses.Contains(LocalSerial) && ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.SkipEmptyCorpse && container.IsEmpty)
                {
                    IsVisible = false;
                    Dispose();
                }
            }

            AnchorType = ProfileManager.CurrentProfile.EnableGridContainerAnchor ? ANCHOR_TYPE.NONE : ANCHOR_TYPE.DISABLED;
            originalContainerItemGraphic = originalContainerGraphic;

            CanMove = true;
            AcceptMouseInput = true;
            #endregion

            #region background
            background = new AlphaBlendControl()
            {
                Width = Width - (borderWidth * 2),
                Height = Height - (borderWidth * 2),
                X = borderWidth,
                Y = borderWidth,
                Alpha = (float)ProfileManager.CurrentProfile.ContainerOpacity / 100,
                Hue = ProfileManager.CurrentProfile.Grid_UseContainerHue ? container.Hue : ProfileManager.CurrentProfile.AltGridContainerBackgroundHue,
                AcceptMouseInput = true,
                CanMove = true
            };
            background.MouseDoubleClick += OnMinimizeToggleDoubleClick;

            backgroundTexture = new GumpPicTiled(0);
            backgroundTexture.CanMove = true;
            backgroundTexture.MouseDoubleClick += OnMinimizeToggleDoubleClick;
            #endregion

            #region TOP BAR AREA
            containerNameLabel = new Label(GetContainerName(), true, 0x0481, 150, ishtml: true)
            {
                X = borderWidth,
                Y = borderWidth,
                AcceptMouseInput = true,
                CanMove = true
            };
            containerNameLabel.MouseDoubleClick += OnMinimizeToggleDoubleClick;

            searchBox = new StbTextBox(0xFF, 20, 0, true, FontStyle.None, 0x0481)
            {
                X = borderWidth,
                Y = borderWidth + LABEL_HEIGHT,
                Multiline = false,
                Width = background.Width - 18,
                Height = 20
            };
            searchBox.PlaceHolderText = "Search...";
            searchBox.TextChanged += (sender, e) => { UpdateItems(); };

            searchClearButton = new NiceButton(borderWidth + background.Width - 16, borderWidth + LABEL_HEIGHT, 16, searchBox.Height, ButtonAction.Default, "X");
            searchClearButton.MouseUp += (sender, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    searchBox.ClearText();
                    UIManager.SystemChat?.SetFocus();
                }
            };
            searchClearButton.SetTooltip("Clear search");

            Texture2D regularGumpIcon = Client.Game.UO.Gumps.GetGump(5839).Texture;
            openRegularGump = new GumpPic(background.Width - 25 - borderWidth, borderWidth, regularGumpIcon == null ? (ushort)1209 : (ushort)5839, 0);
            openRegularGump.ContextMenu = GenContextMenu();

            openRegularGump.MouseUp += (sender, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    openRegularGump.ContextMenu?.Show();
                }
            };
            openRegularGump.MouseEnter += (sender, e) => { openRegularGump.Graphic = regularGumpIcon == null ? (ushort)1210 : (ushort)5840; };
            openRegularGump.MouseExit += (sender, e) => { openRegularGump.Graphic = regularGumpIcon == null ? (ushort)1209 : (ushort)5839; };
            openRegularGump.SetTooltip(
                "/c[orange]Grid Container Controls:/cd\n" +
                "Ctrl + Click to lock an item in place\n" +
                "Alt + Click to toggle selection for multi-move\n" +
                "Alt + Double Click to select all similar items\n" +
                "Shift + Click to add an item to your auto loot list\n" +
                "Sort and single click looting can be enabled with the icons on the right side");
            quickDropBackpack = new ResizableStaticPic(World.Player.Backpack.DisplayedGraphic, 20, 20)
            {
                X = Width - openRegularGump.Width - 20 - borderWidth,
                Y = borderWidth
            };
            quickDropBackpack.MouseUp += (sender, e) =>
            {
                if (e.Button == MouseButtonType.Left && quickDropBackpack.MouseIsOver)
                {
                    if (Client.Game.UO.GameCursor.ItemHold.Enabled)
                    {
                        GameActions.DropItem(Client.Game.UO.GameCursor.ItemHold.Serial, 0xFFFF, 0xFFFF, 0, World.Player.Backpack);
                    }
                    else if (isCorpse)
                    {
                        ProfileManager.CurrentProfile.CorpseSingleClickLoot ^= true;
                        quickDropBackpack.SetTooltip(quickLootTooltip);
                    }
                    else
                    {
                        quickLootThisContainer ^= true;
                        quickDropBackpack.SetTooltip(quickLootTooltip);
                    }
                }
            };
            quickDropBackpack.MouseEnter += (sender, e) => { quickDropBackpack.Hue = 0x34; };
            quickDropBackpack.MouseExit += (sender, e) => { quickDropBackpack.Hue = 0; };
            quickDropBackpack.SetTooltip(quickLootTooltip);

            sortContents = new GumpPic(quickDropBackpack.X - 20, borderWidth, 1210, 0);
            sortContents.ContextMenu = GenSortContextMenu();
            sortContents.MouseUp += (sender, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    if (Keyboard.Alt)
                    {
                        autoSortContainer ^= true;
                        gridContainerEntry.AutoSort = autoSortContainer;
                        sortContents.SetTooltip(sortButtonTooltip);
                    }
                    else
                    {
                        sortContents.ContextMenu?.Show();
                    }
                }
            };
            sortContents.MouseEnter += (sender, e) => { sortContents.Graphic = 1209; };
            sortContents.MouseExit += (sender, e) => { sortContents.Graphic = 1210; };
            sortContents.SetTooltip(sortButtonTooltip);
            #endregion

            #region Scroll Area
            scrollArea = new GridScrollArea(
                background.X,
                LABEL_HEIGHT + TOP_BAR_HEIGHT + background.Y,
                background.Width,
                background.Height - LABEL_HEIGHT - TOP_BAR_HEIGHT
                );

            scrollArea.MouseUp += ScrollArea_MouseUp;
            scrollArea.MouseDoubleClick += (sender, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    // Only toggle if clicking on empty space (not on grid items)
                    Control clickedControl = UIManager.MouseOverControl;
                    if (clickedControl == scrollArea)
                    {
                        OnMinimizeToggleDoubleClick(sender, e);
                    }
                }
            };
            #endregion

            #region Set loot bag
            setLootBag = new NiceButton(0, Height - 20, 100, 20, ButtonAction.Default, "Set loot bag") { IsSelectable = false };
            setLootBag.IsVisible = isCorpse;
            setLootBag.SetTooltip("For double click looting only");
            setLootBag.MouseUp += (s, e) =>
            {
                GameActions.Print(world, Resources.ResGumps.TargetContainerToGrabItemsInto);
                world.TargetManager.SetTargeting(CursorTarget.SetGrabBag, 0, TargetType.Neutral);
            };
            #endregion

            #region Add controls
            Add(background);
            Add(backgroundTexture);
            Add(containerNameLabel);
            searchBoxBackground = new AlphaBlendControl(0.5f)
            {
                Hue = 0x0481,
                Width = searchBox.Width,
                Height = searchBox.Height
            };
            searchBox.Add(searchBoxBackground);
            Add(searchBox);
            Add(searchClearButton);
            Add(openRegularGump);
            Add(quickDropBackpack);
            Add(sortContents);
            Add(scrollArea);
            Add(setLootBag);
            #endregion

            SlotManager = new GridSlotManager(world, LocalSerial, this, scrollArea); //Must come after scroll area

            if (ShouldUseOldContainerStyle())
            {
                skipSave = true; //Avoid unsaving item slots because they have not be set up yet
                OpenOldContainer(local);
                return;
            }

            BuildBorder();
            ResizeWindow(savedSize);

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
            if (gridContainerEntry.UseOriginalContainer == null && UseOldContainerStyle == null)
                return ProfileManager.CurrentProfile.GridContainersDefaultToOldStyleView;

            // Next, if the open request was made with a specific mode (i.e., useGridStyle != nul), use that,
            // otherwise, fallback to the stored preference (which, if we got here is *not* null, or we'd fall into the default case above)
            return UseOldContainerStyle ?? gridContainerEntry.UseOriginalContainer.Value;
        }

        public override GumpType GumpType => GumpType.GridContainer;

        private void UpdateMinimizedState()
        {
            if (_isMinimized)
            {
                // Store current height before minimizing
                _heightBeforeMinimize = Height;

                // Save current maximized position and load minimized position
                SwitchPositionState(false);

                // Hide all controls except container name, background, and border
                SetControlsVisibility(false);

                // Resize to minimal height (just the label area + border)
                int minimizedHeight = LABEL_HEIGHT + (borderWidth * 2);
                ResizeWindow(new Point(Width, minimizedHeight));
                Height = minimizedHeight;

                // Update border and background dimensions
                if (background != null) background.Height = LABEL_HEIGHT;
                if (backgroundTexture != null) backgroundTexture.Height = LABEL_HEIGHT;

                // Update the border control to match new dimensions
                OnResize();
            }
            else
            {
                // Save current minimized position and load maximized position
                SwitchPositionState(true);

                // Restore all controls
                SetControlsVisibility(true);

                // Restore original height (fallback to default if not set)
                int restoredHeight;
                if (_heightBeforeMinimize > 0)
                {
                    restoredHeight = _heightBeforeMinimize;
                }
                else
                {
                    // Fallback to a reasonable default height
                    restoredHeight = GetHeight();
                }

                ResizeWindow(new Point(Width, restoredHeight));
                Height = restoredHeight;

                // Restore border and background dimensions
                if (background != null) background.Height = Height - (borderWidth * 2);
                if (backgroundTexture != null) backgroundTexture.Height = Height - (borderWidth * 2);

                // Update the border control to match restored dimensions
                OnResize();
            }

            WantUpdateSize = true;

            // Save the minimized state
            gridContainerEntry?.UpdateSaveDataEntry(this);
        }

        /// <summary>
        /// Applies minimized dimensions without storing height (used on initial load)
        /// </summary>
        private void ApplyMinimizedDimensions()
        {
            // Hide all controls except container name, background, and border
            SetControlsVisibility(false);

            // Resize to minimal height (just the label area + border)
            int minimizedHeight = LABEL_HEIGHT + (borderWidth * 2);
            ResizeWindow(new Point(Width, minimizedHeight));
            Height = minimizedHeight;

            // Update border and background dimensions
            if (background != null) background.Height = LABEL_HEIGHT;
            if (backgroundTexture != null) backgroundTexture.Height = LABEL_HEIGHT;

            // Update the border control to match new dimensions
            OnResize();

            WantUpdateSize = true;
        }

        private ContextMenuControl GenContextMenu()
        {
            var control = new ContextMenuControl(this);
            control.Add(new ContextMenuItemEntry("Open Original View", () =>
            {
                UseOldContainerStyle = true;
                OpenOldContainer(LocalSerial);
            }));

            control.Add(new ContextMenuItemEntry
            (
                "Open New Containers in the Original View", () =>
                {
                    ProfileManager.CurrentProfile.GridContainersDefaultToOldStyleView = !ProfileManager.CurrentProfile.GridContainersDefaultToOldStyleView;
                    openRegularGump.ContextMenu = GenContextMenu();
                }, true, ProfileManager.CurrentProfile.GridContainersDefaultToOldStyleView
            ));

            control.Add(new ContextMenuItemEntry("Stack Similar Items in the Original View", () =>
            {
                StackNonStackableItems = !StackNonStackableItems;
                openRegularGump.ContextMenu = GenContextMenu();
            }, true, StackNonStackableItems));

            control.Add(new ContextMenuItemEntry("Open Grid View Highlight Settings", () =>
            {
                GridHighlightMenu.Open(World);
            }));

            if (container != World.Player.Backpack)
            {
                control.Add(new ContextMenuItemEntry("Autoloot this container", () =>
                {
                    AutoLootManager.Instance.ForceLootContainer(LocalSerial);
                }));
            }

            // Re-applies highlight rules and colors; useful if item highlights desync after SOS loot or container refresh.
            control.Add(new ContextMenuItemEntry("Refresh item highlights", () =>
            {
                GridHighlightData.RecheckMatchStatus();
            }));

            return control;
        }

        private ContextMenuControl GenSortContextMenu()
        {
            var control = new ContextMenuControl(this);

            control.Add(new ContextMenuItemEntry("Sort by Graphic + Hue", () =>
            {
                sortMode = GridSortMode.GraphicAndHue;
                sortContents.ContextMenu = GenSortContextMenu();
                sortContents.SetTooltip(sortButtonTooltip);
                UpdateItems(true);
                gridContainerEntry.UpdateSaveDataEntry(this);
            }, true, sortMode == GridSortMode.GraphicAndHue));

            control.Add(new ContextMenuItemEntry("Sort by Name", () =>
            {
                sortMode = GridSortMode.Name;
                sortContents.ContextMenu = GenSortContextMenu();
                sortContents.SetTooltip(sortButtonTooltip);
                UpdateItems(true);
                gridContainerEntry.UpdateSaveDataEntry(this);
            }, true, sortMode == GridSortMode.Name));

            return control;
        }
        private static int GetWidth(int columns = -1)
        {
            // Use default columns if none are specified
            if (columns < 0)
                columns = ProfileManager.CurrentProfile.Grid_DefaultColumns;

            // Calculate the total width of the grid container
            return (borderWidth * 2)           // Borders on the left and right
                    + 15                       // Width of the scroll bar
                    + (gridItemSize * columns) // Total width of grid items
                    + (X_SPACING * columns);   // Spacing between grid items
        }
        private static int GetHeight(int rows = -1)
        {
            // Use default rows if none are specified
            if (rows < 0)
                rows = ProfileManager.CurrentProfile.Grid_DefaultRows;

            // Calculate the total height of the grid container
            return LABEL_HEIGHT                 // Height of the container name label
                   + TOP_BAR_HEIGHT             // Height of the top bar
                   + (borderWidth * 2)          // Borders on the top and bottom
                   + ((gridItemSize + Y_SPACING) * rows); // Total height of grid items with spacing
        }

        public override void Save(XmlTextWriter writer)
        {
            base.Save(writer);

            if (!skipSave)
            {
                gridContainerEntry.UpdateSaveDataEntry(this);
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

            writer.WriteAttributeString("ogContainer", originalContainerItemGraphic.ToString());
        }
        public override void Restore(XmlElement xml)
        {
            base.Restore(xml);
            GameActions.DoubleClickQueued(LocalSerial);
        }

        private void ScrollArea_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtonType.Left && scrollArea.MouseIsOver)
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

            ushort graphic = originalContainerItemGraphic;

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

            var container = new ContainerGump(World, this.container.Serial, graphic, true, true)
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
            if (container == null)
            {
                Dispose();
                return;
            }

            containerNameLabel.Text = GetContainerName();

            if (autoSortContainer)
                overrideSort = true;

            List<Item> sortedContents = (ProfileManager.CurrentProfile is null || ProfileManager.CurrentProfile.GridContainerSearchMode == 0) && !string.IsNullOrEmpty(searchBox.Text)
                ? SlotManager.SearchResults(searchBox.Text)
                : GridSlotManager.GetItemsInContainer(World, container, sortMode, overrideSort);

            SlotManager.RebuildContainer(sortedContents, searchBox.Text, overrideSort);
            InvalidateContents = false;
        }

        protected override void UpdateContents()
        {
            if (InvalidateContents && !IsDisposed)
                UpdateItems();
        }

        protected override void OnMouseExit(int x, int y)
        {
            if (isCorpse && container != null && container == SelectedObject.CorpseObject)
                SelectedObject.CorpseObject = null;
        }

        public override bool Contains(int x, int y)
        {
            if (_isMinimized)
            {
                // When minimized, only accept mouse input within the minimized bounds
                return x >= 0 && x < Width && y >= 0 && y < (LABEL_HEIGHT + (borderWidth * 2));
            }
            return base.Contains(x, y);
        }

        protected override void OnMove(int x, int y)
        {
            base.OnMove(x, y);

            if (gridContainerEntry != null)
            {
                gridContainerEntry.SetPositionForState(X, Y, IsMinimized);
            }

            // Backpack special handling
            if (IsPlayerBackpack)
            {
                ProfileManager.CurrentProfile.BackpackGridPosition = new Point(X, Y);
            }
        }

        public override void Dispose()
        {
            if (isCorpse)
            {
                lastCorpseX = X;
                lastCorpseY = Y;
            }
            else
            {
                lastX = X;
                lastY = Y;
            }

            Item currentContainer = container;

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

            if (SlotManager != null && !skipSave && SlotManager.ItemPositions.Count > 0 && !isCorpse)
                gridContainerEntry.UpdateSaveDataEntry(this);

            base.Dispose();
        }

        public override void PreDraw()
        {
            base.PreDraw();

            if (IsDisposed)
                return;

            Item item = container;

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
            if (_isMinimized && Height != LABEL_HEIGHT + (borderWidth * 2))
            {
                Height = LABEL_HEIGHT + (borderWidth * 2);
                background.Height = LABEL_HEIGHT;
                backgroundTexture.Height = LABEL_HEIGHT;
                BorderControl.Width = Width;
                BorderControl.Height = Height;

                // Manually reposition the resize button since we're bypassing ResizeWindow's min height
                foreach (Control child in Children)
                {
                    if (child is Button btn)
                    {
                        btn.X = Width - (btn.Width >> 0) + 2;
                        btn.Y = Height - (btn.Height >> 0) + 2;
                        break;
                    }
                }

                WantUpdateSize = true;
            }

            if (!_isMinimized && (lastWidth != Width || lastHeight != Height || lastGridItemScale != gridItemSize))
            {
                lastGridItemScale = gridItemSize;
                int adjustedWidth = Width - (borderWidth * 2);
                int adjustedHeight = Height - (borderWidth * 2);
                UpdateBackgroundDimensions(adjustedWidth, adjustedHeight);
                scrollArea.Width = adjustedWidth;
                scrollArea.Height = adjustedHeight - LABEL_HEIGHT - TOP_BAR_HEIGHT;
                openRegularGump.X = Width - openRegularGump.Width - borderWidth;
                quickDropBackpack.X = openRegularGump.X - quickDropBackpack.Width;
                sortContents.X = quickDropBackpack.X - sortContents.Width;
                lastHeight = Height;
                lastWidth = Width;
                searchBox.Width = adjustedWidth - 18;
                searchBoxBackground.Width = searchBox.Width;
                searchClearButton.X = borderWidth + adjustedWidth - 16;
                UpdateBackgroundStyle(background.Hue, background.Alpha);
                setLootBag.Y = Height - 20;

                if (IsPlayerBackpack)
                    ProfileManager.CurrentProfile.BackpackGridSize = new Point(Width, Height);
                else
                    gridContainerEntry?.UpdateSaveDataEntry(this); // Save size for non-backpack containers

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

        private string GetContainerName()
        {
            string containerName = !string.IsNullOrEmpty(container.Name) ? container.Name : "a container";

            if (SlotManager != null)
            {
                containerName += $" ({SlotManager.ContainerContents.Count})";
            }

            return containerName;
        }

        public void OptionsUpdated()
        {
            float newAlpha = ProfileManager.CurrentProfile.ContainerOpacity / 100f;
            ushort newHue = ProfileManager.CurrentProfile.Grid_UseContainerHue
                ? container.Hue
                : ProfileManager.CurrentProfile.AltGridContainerBackgroundHue;

            UpdateBackgroundStyle(newHue, newAlpha);
            BorderControl.Hue = newHue;
            BorderControl.Alpha = newAlpha;

            AnchorType = ProfileManager.CurrentProfile.EnableGridContainerAnchor
                ? ANCHOR_TYPE.NONE
                : ANCHOR_TYPE.DISABLED;

            BuildBorder();
        }

        public static void UpdateAllGridContainers()
        {
            foreach (GridContainer _ in UIManager.Gumps.OfType<GridContainer>())
                _.OptionsUpdated();
        }

        public void HandleObjectMessage(Entity parent, string text, ushort hue)
        {
            if (parent != null)
                SlotManager.FindItem(parent.Serial)?.AddText(text, hue);
        }

        public void BuildBorder()
        {
            int graphic = 0, borderSize = 0;
            var currentStyle = (BorderStyle)ProfileManager.CurrentProfile.Grid_BorderStyle;

            if (currentStyle == BorderStyle.Default)
            {
                BorderControl.DefaultGraphics();
                backgroundTexture.IsVisible = false;
                background.IsVisible = true;
                borderWidth = 4;
            }
            else if (BorderStyleConfig.TryGetValue(currentStyle, out (int graphic, int borderSize) config))
            {
                graphic = config.graphic;
                borderSize = config.borderSize;

                // Special handling for Style8 fallback
                if (currentStyle == BorderStyle.Style8 && Client.Game.UO.Gumps.GetGump(40303).Texture == null)
                {
                    graphic = 83;
                }

                // Apply border graphics for non-default styles
                BorderControl.T_Left = (ushort)graphic;
                BorderControl.H_Border = (ushort)(graphic + 1);
                BorderControl.T_Right = (ushort)(graphic + 2);
                BorderControl.V_Border = (ushort)(graphic + 3);

                backgroundTexture.Graphic = (ushort)(graphic + 4);
                backgroundTexture.IsVisible = true;
                backgroundTexture.Hue = background.Hue;
                BorderControl.Hue = background.Hue;
                BorderControl.Alpha = background.Alpha;
                background.IsVisible = false;

                BorderControl.V_Right_Border = (ushort)(graphic + 5);
                BorderControl.B_Left = (ushort)(graphic + 6);
                BorderControl.H_Bottom_Border = (ushort)(graphic + 7);
                BorderControl.B_Right = (ushort)(graphic + 8);
                BorderControl.BorderSize = borderSize;
                borderWidth = borderSize;
            }
            UpdateUIPositions();
            OnResize();

            BorderControl.IsVisible = !ProfileManager.CurrentProfile.Grid_HideBorder;
        }

        private void UpdateUIPositions()
        {
            background.X = background.Y = borderWidth;
            scrollArea.X = background.X;
            scrollArea.Y = LABEL_HEIGHT + TOP_BAR_HEIGHT + background.Y;
            searchBox.X = borderWidth;
            searchBox.Y = borderWidth + LABEL_HEIGHT;
            quickDropBackpack.Y = sortContents.Y = openRegularGump.Y = borderWidth;
            backgroundTexture.X = background.X;
            backgroundTexture.Y = background.Y;

            int adjustedWidth = Width - (borderWidth * 2);
            int adjustedHeight = Height - (borderWidth * 2);

            UpdateBackgroundDimensions(adjustedWidth, adjustedHeight);

            searchBox.Width = adjustedWidth - 18;
            searchBoxBackground.Width = searchBox.Width;
            searchClearButton.X = borderWidth + adjustedWidth - 16;
            searchClearButton.Y = borderWidth + LABEL_HEIGHT;

            scrollArea.Width = adjustedWidth;
            scrollArea.Height = adjustedHeight - LABEL_HEIGHT - TOP_BAR_HEIGHT;
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (CUOEnviroment.Debug)
                batcher.DrawString(Renderer.Fonts.Bold, LocalSerial.ToString(), x, y - 40, ShaderHueTranslator.GetHueVector(32));
            return base.Draw(batcher, x, y);
        }

        public enum GridSortMode
        {
            GraphicAndHue = 0,
            Name = 1
        }

        public enum BorderStyle
        {
            Default,
            Style1,
            Style2,
            Style3,
            Style4,
            Style5,
            Style6,
            Style7,
            Style8
        }

        public class GridItem : Control
        {
            private bool _mousePressedWhenEntered;
            private readonly Item _container;
            private Item _item;
            private readonly GridContainer _gridContainer;
            private readonly int _slot;
            private GridContainerPreview _preview;
            private Label _count;
            private readonly AlphaBlendControl background;
            private CustomToolTip _toolTipThis, _toolTipitem1, _toolTipitem2;
            private readonly List<SimpleTimedTextGump> _timedTexts = new();
            private readonly World _world;
            private static readonly HashSet<uint> _toggledThisAltDrag = new HashSet<uint>();
            private static bool _altDragActive;
            private bool _selectHighlight;

            public bool ItemGridLocked { get; set; }
            public bool Highlight { get; set; }
            public Item SlotItem
            {
                get => _item;
                set
                {
                    _item = value;
                    LocalSerial = value?.Serial ?? 0;
                }
            }

            private readonly int[] spellbooks = [0x0EFA, 0x2253, 0x2252, 0x238C, 0x23A0, 0x2D50, 0x2D9D, 0x225A];

            public GridItem(World world, uint serial, int size, Item container, GridContainer gridContainer, int count)
            {
                _world = world;
                _slot = count;
                _container = container;
                _gridContainer = gridContainer;
                LocalSerial = serial;
                _item = world.Items.Get(serial);
                CanMove = false;
                AcceptMouseInput = true;
                WantUpdateSize = false;

                StaticGridContainerSettingUpdated();

                background = new AlphaBlendControl(0.25f)
                {
                    Width = size,
                    Height = size
                };

                Width = Height = size;

                Add(background);

                SetGridItem(_item);
            }

            public void AddText(string text, ushort hue)
            {
                var timedText = new SimpleTimedTextGump(_world, text, (uint)hue, TimeSpan.FromSeconds(2), 200)
                {
                    X = ScreenCoordinateX,
                    Y = ScreenCoordinateY
                };

                // Remove disposed timed texts
                _timedTexts.RemoveAll(tt => tt == null || tt.IsDisposed);

                // Adjust the Y position of existing timed texts
                foreach (SimpleTimedTextGump tt in _timedTexts)
                    tt.Y -= timedText.Height + 5;

                _timedTexts.Add(timedText);
                UIManager.Add(timedText);
            }

            public void Resize()
            {
                Width = gridItemSize;
                Height = gridItemSize;
                background.Width = gridItemSize;
                background.Height = gridItemSize;
            }

            /// <summary>
            /// Set this grid slot's item. Set to null for empty slot.
            /// </summary>
            /// <param name="item"></param>
            public void SetGridItem(Item item)
            {
                if (item == null)
                {
                    _item = null;
                    LocalSerial = 0;
                    ClearTooltip();
                    Highlight = false;
                    _count?.Dispose();
                    _count = null;
                    ItemGridLocked = false;
                    CanMove = true;
                    _hasItem = false;
                    _shouldDraw = !_gridContainer.isCorpse;
                    return;
                }

                _hasItem = true;
                CanMove = false;
                _item = item;
                ref readonly SpriteInfo text = ref Client.Game.UO.Arts.GetArt((uint)_item.DisplayedGraphic);
                _texture = text.Texture;
                _bounds = text.UV;
                _rect = Client.Game.UO.Arts.GetRealArtBounds(_item.DisplayedGraphic);
                _shouldDraw = _texture != null;

                LocalSerial = item.Serial;
                int itemAmt = _item.ItemData.IsStackable ? _item.Amount : 1;

                if (itemAmt > 1)
                {
                    _count?.Dispose();
                    _count = new Label(itemAmt.ToString(), true, 0x0481, align: TEXT_ALIGN_TYPE.TS_LEFT)
                    {
                        X = 1
                    };
                    Y = Height - _count.Height;
                }

                SetTooltip(_item);
            }

            /// <summary>
            /// Called when various cached settings like border hue and alpha are updated.
            /// </summary>
            public static void StaticGridContainerSettingUpdated() => _borderHueVec = ShaderHueTranslator.GetHueVector(ProfileManager.CurrentProfile.GridBorderHue, false, (float)ProfileManager.CurrentProfile.GridBorderAlpha / 100);

            protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType e)
            {
                base.OnMouseDoubleClick(x, y, e);

                if (e != MouseButtonType.Left || _world.TargetManager.IsTargeting || _item == null)
                    return false;

                if (!Keyboard.Ctrl &&
                    !Keyboard.Alt &&
                    _profile.DoubleClickToLootInsideContainers &&
                    _gridContainer.isCorpse &&
                    !_item.IsDestroyed &&
                    !_item.ItemData.IsContainer &&
                    _container != _world.Player.Backpack &&
                    !_item.IsLocked &&
                    _item.IsLootable)
                {
                    GameActions.GrabItem(_world, _item, _item.Amount);
                }
                else if (Keyboard.Alt && _item != null)
                {
                    if (MultiItemMoveGump.TrySelect(_item))
                        _selectHighlight = true;
                    ushort graphic = _item.Graphic;
                    ushort hue = _item.Hue;
                    foreach (GridItem gridItem in _gridContainer.SlotManager.GridSlots.Values)
                    {
                        Item item = gridItem?._item;
                        if (item is null ||
                            graphic != item.Graphic ||
                            hue != item.Hue ||
                            MultiItemMoveGump.IsSelected(item.Serial))
                        {
                            continue;
                        }

                        if (MultiItemMoveGump.TrySelect(item))
                            gridItem._selectHighlight = true;
                    }

                    MultiItemMoveGump.ShowNextTo(_gridContainer);
                }
                else
                {
                    GameActions.DoubleClick(_world, LocalSerial);
                }

                return true;
            }

            protected override void OnMouseUp(int x, int y, MouseButtonType e)
            {
                base.OnMouseUp(x, y, e);

                if (e == MouseButtonType.Left)
                {
                    if (Client.Game.UO.GameCursor.ItemHold.Enabled)
                    {
                        if (_item != null && _item.ItemData.IsContainer)
                        {
                            GameActions.DropItem(Client.Game.UO.GameCursor.ItemHold.Serial, 0xFFFF, 0xFFFF, 0, _item.Serial);
                            Mouse.CancelDoubleClick = true;
                            _mousePressedWhenEntered = false; //Fix for not needing to move mouse out of grid box to re-drag item
                        }
                        else if (_item != null && _item.ItemData.IsStackable && _item.Graphic == Client.Game.UO.GameCursor.ItemHold.Graphic)
                        {
                            GameActions.DropItem(Client.Game.UO.GameCursor.ItemHold.Serial, _item.X, _item.Y, 0, _item.Serial);
                            Mouse.CancelDoubleClick = true;
                            _mousePressedWhenEntered = false; //Fix for not needing to move mouse out of grid box to re-drag item
                        }
                        else
                        {
                            Rectangle containerBounds = _world.ContainerManager.Get(_container.Graphic).Bounds;
                            _gridContainer.SlotManager.AddItemSlot(Client.Game.UO.GameCursor.ItemHold.Serial, _slot);
                            (int X, int Y) pos = GetBoxPosition(_slot, Client.Game.UO.GameCursor.ItemHold.Graphic, containerBounds.Width, containerBounds.Height);
                            GameActions.DropItem(Client.Game.UO.GameCursor.ItemHold.Serial, pos.X, pos.Y, 0, _container.Serial);
                            Mouse.CancelDoubleClick = true;
                            _mousePressedWhenEntered = false; //Fix for not needing to move mouse out of grid box to re-drag item
                        }
                    }
                    else if (_world.TargetManager.IsTargeting)
                    {
                        if (_item != null)
                        {
                            _world.TargetManager.Target(_item);
                            if (_world.TargetManager.TargetingState == CursorTarget.SetTargetClientSide)
                            {
                                UIManager.Add(new InspectorGump(_world, _item));
                            }
                        }
                        else if (!_profile.DisableTargetingGridContainers)
                            _world.TargetManager.Target(_container);
                        Mouse.CancelDoubleClick = true;
                    }
                    else if (Keyboard.Ctrl)
                    {
                        if (_item != null)
                            _gridContainer.SlotManager.SetLockedSlot(_slot, !ItemGridLocked, _gridContainer.gridContainerEntry.GetSlot(_item.Serial));
                        Mouse.CancelDoubleClick = true;
                    }
                    else if (Keyboard.Alt && _item != null)
                    {
                        // If no drag occurred, toggle on click to prevent missed quick taps.
                        if (!_altDragActive)
                        {
                            _selectHighlight = MultiItemMoveGump.ToggleItem(_item);
                        }
                        else
                        {
                            _selectHighlight = MultiItemMoveGump.IsSelected(_item.Serial);
                        }

                        if (_selectHighlight)
                            MultiItemMoveGump.ShowNextTo(_gridContainer);

                        Mouse.CancelDoubleClick = true;
                    }
                    else if (Keyboard.Shift && _item != null && _profile.EnableAutoLoot && !_profile.HoldShiftForContext && !_profile.HoldShiftToSplitStack)
                    {
                        AutoLootManager.Instance.AddAutoLootEntry(_item.Graphic, _item.Hue, _item.Name);
                        GameActions.Print(_world, $"Added this item to auto loot.");
                    }
                    else if (_item != null)
                    {
                        Point offset = Mouse.LDragOffset;
                        if (Math.Abs(offset.X) < Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS && Math.Abs(offset.Y) < Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS)
                        {
                            if ((_gridContainer.isCorpse && _profile.CorpseSingleClickLoot) || _gridContainer.quickLootThisContainer)
                            {
                                ObjectActionQueue.Instance.Enqueue(ObjectActionQueueItem.QuickLoot(_item), ActionPriority.MoveItem);
                                Mouse.CancelDoubleClick = true;
                            }
                            else
                            {
                                if (_world.ClientFeatures.TooltipsEnabled)
                                    _world.DelayedObjectClickManager.Set(_item.Serial, _gridContainer.X, _gridContainer.Y - 80, Time.Ticks + Mouse.MOUSE_DELAY_DOUBLE_CLICK);
                                else
                                {
                                    GameActions.SingleClick(_world, _item.Serial);
                                }
                            }
                        }
                    }
                }
            }

            private (int X, int Y) GetBoxPosition(int boxIndex, uint itemGraphic, int width, int height)
            {
                if (_gridContainer.StackNonStackableItems)
                    foreach (GridItem gridSlot in _gridContainer.SlotManager.GridSlots.Values)
                    {
                        if (gridSlot._item != null && gridSlot._item.Graphic == itemGraphic)
                        {
                            return (gridSlot._item.X, gridSlot._item.Y);
                        }
                    }

                int gridSize = (int)Math.Ceiling(Math.Sqrt(_gridContainer.SlotManager.GridSlots.Count));

                int row = boxIndex / gridSize;
                int col = boxIndex % gridSize;

                float cellWidth = width / gridSize;
                float cellHeight = height / gridSize;

                float x = col * cellWidth + cellWidth / 2;
                float y = row * cellHeight + cellHeight / 2;

                return ((int)x, (int)y);
            }

            protected override void OnMouseExit(int x, int y)
            {
                base.OnMouseExit(x, y);

                if (Mouse.LButtonPressed && !_mousePressedWhenEntered)
                {
                    Point offset = Mouse.LDragOffset;
                    if (Math.Abs(offset.X) >= Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS || Math.Abs(offset.Y) >= Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS)
                    {
                        if (_item != null)
                        {
                            if (!Keyboard.Alt)
                                GameActions.PickUp(_world, _item, x, y);
                        }
                    }
                }

                GridContainerPreview g;
                while ((g = UIManager.GetGump<GridContainerPreview>()) != null)
                {
                    g.Dispose();
                }

                _mousePressedWhenEntered = false;
            }

            protected override void OnMouseEnter(int x, int y)
            {
                base.OnMouseEnter(x, y);

                SelectedObject.Object = _world.Get(LocalSerial);
                _mousePressedWhenEntered = Mouse.LButtonPressed;

                if (_item != null)
                {
                    if (_item.ItemData.IsContainer && _item.Items != null &&
                        _profile.GridEnableContPreview && !spellbooks.Contains(_item.Graphic))
                    {
                        _preview = new GridContainerPreview(_world, _item, Mouse.Position.X, Mouse.Position.Y);
                        UIManager.Add(_preview);
                    }

                    if (!HasTooltip)
                        SetTooltip(_item);
                }
            }

            private Texture2D _texture;
            private Rectangle _rect = Rectangle.Empty;
            private Rectangle _bounds;
            private readonly Profile _profile = ProfileManager.CurrentProfile;
            private readonly Texture2D _whiteTexture = SolidColorTextureCache.GetTexture(Color.White);
            private bool _hasItem;
            private static readonly Vector3 _highLightHue = ShaderHueTranslator.GetHueVector(0x34, false, 1);
            private static Vector3 _borderHueVec;
            private bool _shouldDraw;

            /// <summary>
            /// Calculates the centered dimension and offset for rendering an item within a grid cell
            /// </summary>
            private (int size, int offset) CalculateCenteredDimension(int rectDimension, int cellDimension, bool scaleItems, float scale)
            {
                int originalSize = cellDimension;
                int offset = 0;

                if (rectDimension < cellDimension)
                {
                    originalSize = scaleItems ? (int)(rectDimension * scale) : rectDimension;
                    offset = (cellDimension >> 1) - (originalSize >> 1);
                }
                else if (rectDimension > cellDimension)
                {
                    originalSize = scaleItems ? (int)(cellDimension * scale) : cellDimension;
                    offset = (cellDimension >> 1) - (originalSize >> 1);
                }

                return (originalSize, offset);
            }

            /// <summary>
            /// Gets the weapon comparison items for tooltip comparison (handles one-handed and two-handed weapons)
            /// </summary>
            private (Item primary, Item secondary) GetWeaponComparisonItems(Layer itemLayer)
            {
                if (itemLayer != Layer.OneHanded && itemLayer != Layer.TwoHanded)
                    return (null, null);

                Layer primaryLayer = itemLayer;
                Layer secondaryLayer = itemLayer == Layer.OneHanded ? Layer.TwoHanded : Layer.OneHanded;

                Item primary = _world.Player.FindItemByLayer(primaryLayer);
                Item secondary = _world.Player.FindItemByLayer(secondaryLayer);

                // If no item in primary layer, swap with secondary
                if (primary == null && secondary != null)
                {
                    primary = secondary;
                    secondary = null;
                }

                return (primary, secondary);
            }

            /// <summary>
            /// Draws a highlighted border around the grid item
            /// </summary>
            private void DrawHighlightBorder(UltimaBatcher2D batcher, int x, int y, Texture2D borderTexture, Vector3 borderHueVec)
            {
                int bsize = _profile.GridHighlightSize;
                int bx = x + 6;
                int by = y + 6;
                int innerWidth = Width - 12;
                int innerHeight = Height - 12;

                // Top border
                batcher.Draw(borderTexture, new Rectangle(bx, by, innerWidth, bsize), borderHueVec);

                // Left border
                batcher.Draw(borderTexture, new Rectangle(bx, by + bsize, bsize, innerHeight - (bsize * 2)), borderHueVec);

                // Right border
                batcher.Draw(borderTexture, new Rectangle(bx + innerWidth - bsize, by + bsize, bsize, innerHeight - (bsize * 2)), borderHueVec);

                // Bottom border
                batcher.Draw(borderTexture, new Rectangle(bx, by + innerHeight - bsize, innerWidth, bsize), borderHueVec);
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                if (!_shouldDraw || IsDisposed) return false;

                if (_hasItem && Keyboard.Ctrl && _item.ItemData.Layer > 0 && MouseIsOver && (_toolTipThis == null || _toolTipThis.IsDisposed) && (_toolTipitem1 == null || _toolTipitem1.IsDisposed) && (_toolTipitem2 == null || _toolTipitem2.IsDisposed))
                {
                    var itemLayer = (Layer)_item.ItemData.Layer;
                    Item compItem = _world.Player.FindItemByLayer(itemLayer);
                    Item compItem2 = null;

                    // For weapons, get both possible comparison items (one-handed and two-handed)
                    (Item weaponPrimary, Item weaponSecondary) = GetWeaponComparisonItems(itemLayer);
                    if (weaponPrimary != null)
                    {
                        compItem = weaponPrimary;
                        compItem2 = weaponSecondary;
                    }

                    if (compItem != null && itemLayer != Layer.Backpack)
                    {
                        ClearTooltip();
                        var toolTipList = new List<CustomToolTip>();
                        _toolTipThis = new CustomToolTip(_world, _item, Mouse.Position.X + 5, Mouse.Position.Y + 5, this, compareTo: compItem);
                        toolTipList.Add(_toolTipThis);
                        _toolTipitem1 = new CustomToolTip(_world, compItem, _toolTipThis.X + _toolTipThis.Width + 10, _toolTipThis.Y, this, "<basefont color=\"orange\">Equipped Item<br>");
                        toolTipList.Add(_toolTipitem1);

                        if (CUOEnviroment.Debug)
                        {
                            var i1 = new ItemPropertiesData(_world, _item);
                            var i2 = new ItemPropertiesData(_world, compItem);

                            if (i1.GenerateComparisonTooltip(i2, out string compileToolTip))
                                GameActions.Print(_world, compileToolTip);
                        }

                        // Add second weapon comparison if both hands have weapons
                        if (compItem2 != null)
                        {
                            _toolTipitem2 = new CustomToolTip(_world, compItem2, _toolTipitem1.X + _toolTipitem1.Width + 10, _toolTipitem1.Y, this, "<basefont color=\"orange\">Equipped Item<br>");
                            toolTipList.Add(_toolTipitem2);
                        }

                        var multipleToolTipGump = new MultipleToolTipGump(_world, Mouse.Position.X + 10, Mouse.Position.Y + 10, toolTipList.ToArray(), this);
                        UIManager.Add(multipleToolTipGump);
                    }
                }

                if (_selectHighlight && _hasItem)
                    if (!MultiItemMoveGump.IsSelected(_item.Serial))
                        _selectHighlight = false;

                base.Draw(batcher, x, y);

                Vector3 hueVector = _borderHueVec;

                if (_hasItem)
                {
                    if (ItemGridLocked)
                        hueVector = ShaderHueTranslator.GetHueVector(0x2, false, (float)_profile.GridBorderAlpha / 100);

                    if (Highlight || _selectHighlight)
                        hueVector = _highLightHue;
                }

                batcher.DrawRectangle
                (
                    _whiteTexture,
                    x,
                    y,
                    Width,
                    Height,
                    hueVector
                );

                if (!_hasItem) return true;

                if (_item.MatchesHighlightData)
                {
                    Texture2D borderTexture = SolidColorTextureCache.GetTexture(_item.HighlightColor);
                    var borderHueVec = new Vector3(1, 0, 1);

                    DrawHighlightBorder(batcher, x, y, borderTexture, borderHueVec);
                }

                if (MouseIsOver)
                {
                    hueVector.Z = 0.3f;

                    batcher.Draw
                    (
                        _whiteTexture,
                        new Rectangle
                        (
                            x + 1,
                            y,
                            Width - 1,
                            Height
                        ),
                        hueVector
                    );
                }

                if (_texture == null) return true;

                hueVector = ShaderHueTranslator.GetHueVector(_item.Hue, _item.ItemData.IsPartialHue, 1f);

                Point originalSize = new(Width, Height);
                Point point = new();
                float scale = (_profile.GridContainersScale / 100f);
                bool scaleItems = _profile.GridContainerScaleItems;

                // Calculate centered X dimension
                (originalSize.X, point.X) = CalculateCenteredDimension(_rect.Width, Width, scaleItems, scale);

                // Calculate centered Y dimension
                (originalSize.Y, point.Y) = CalculateCenteredDimension(_rect.Height, Height, scaleItems, scale);

                batcher.Draw
                (
                    _texture,
                    new Rectangle
                    (
                        x + point.X,
                        y + point.Y,
                        originalSize.X,
                        originalSize.Y
                    ),
                    new Rectangle
                    (
                        _bounds.X + _rect.X,
                        _bounds.Y + _rect.Y,
                        _rect.Width,
                        _rect.Height
                    ),
                    hueVector
                );

                _count?.Draw(batcher, x + _count.X, y + _count.Y);

                return true;
            }

            public override void PreDraw()
            {
                base.PreDraw();

                bool comboActive = Keyboard.Alt && Mouse.LButtonPressed
                   && !Client.Game.UO.GameCursor.ItemHold.Enabled
                   && !_world.TargetManager.IsTargeting;

                if (comboActive)
                {
                    // Gesture just started: reset guard
                    if (!_altDragActive)
                    {
                        _altDragActive = true;
                        _toggledThisAltDrag.Clear();
                    }

                    // Toggle immediately for the item currently under the cursor
                    if (_item != null && MouseIsOver && _toggledThisAltDrag.Add(_item.Serial))
                    {
                        _selectHighlight = MultiItemMoveGump.ToggleItem(_item);

                        if (_selectHighlight)
                            MultiItemMoveGump.ShowNextTo(_gridContainer);
                    }
                }
                else if (_altDragActive)
                {
                    // Gesture ended: clean up
                    _altDragActive = false;
                    _toggledThisAltDrag.Clear();
                }
            }
        }

        public class GridSlotManager
        {
            private Dictionary<int, GridItem> gridSlots = new Dictionary<int, GridItem>();
            private Item container;
            private List<Item> containerContents;
            private int amount = 125;
            private Control area;
            private Dictionary<int, uint> itemPositions = new Dictionary<int, uint>();
            private List<uint> itemLocks = new List<uint>();
            private World world;
            private GridContainer gridContainer;

            public Dictionary<int, GridItem> GridSlots => gridSlots;
            public List<Item> ContainerContents => containerContents;
            public Dictionary<int, uint> ItemPositions => itemPositions;

            /// <summary>
            /// Get the GridItem of a serial if it exists
            /// </summary>
            public Dictionary<uint, GridItem> GridItems { get; } = new();

            public GridSlotManager(World world, uint thisContainer, GridContainer gridContainer, Control controlArea)
            {
                #region VARS
                this.world = world;
                this.gridContainer = gridContainer;
                area = controlArea;
                foreach (GridContainerSlotEntry item in gridContainer.gridContainerEntry.Slots.Values)
                {
                    ItemPositions[item.Slot] = item.Serial;

                    if (item.Locked)
                        if (!itemLocks.Contains(item.Serial))
                            itemLocks.Add(item.Serial);
                }
                container = world.Items.Get(thisContainer);
                #endregion

                SetupGridItemControls();
            }

            /// <summary>
            /// Sets an item's position in a specific slot without locking it (unlike Ctrl + Click).
            /// This is used when dragging items to slots or when auto-arranging items.
            /// </summary>
            /// <param name="serial">The serial of the item to position</param>
            /// <param name="specificSlot">The slot index where the item should be placed</param>
            public void AddItemSlot(uint serial, int specificSlot)
            {
                // Update the save data with the new slot position
                gridContainer.gridContainerEntry.GetSlot(serial).Slot = specificSlot;

                // If this item already has a saved position elsewhere, remove it to avoid duplicates
                // Single-pass lookup: find the slot that currently contains this item
                int? oldSlot = null;
                foreach (KeyValuePair<int, uint> kvp in ItemPositions)
                {
                    if (kvp.Value == serial)
                    {
                        oldSlot = kvp.Key;
                        break;
                    }
                }

                if (oldSlot.HasValue)
                {
                    ItemPositions.Remove(oldSlot.Value);
                }

                // Remove any item currently in the target slot (it will be repositioned elsewhere)
                ItemPositions.Remove(specificSlot);

                // Place the item in the specified slot
                ItemPositions[specificSlot] = serial;
            }

            public GridItem FindItem(uint serial)
            {
                if (GridItems.TryGetValue(serial, out GridItem item))
                    return item;

                return null;
            }

            /// <summary>
            /// Rebuilds the container's visual layout by placing items in grid slots
            /// </summary>
            /// <param name="filteredItems">List of items to display (may be filtered by search)</param>
            /// <param name="searchText">Search query for filtering/highlighting items</param>
            /// <param name="overrideSort">If true, only locked items maintain their positions</param>
            public void RebuildContainer(List<Item> filteredItems, string searchText = "", bool overrideSort = false)
            {
                // Ensure we have enough grid slots for all items
                SetupGridItemControls();

                // Clear all grid slots by setting them to null
                foreach (KeyValuePair<int, GridItem> slot in gridSlots)
                {
                    slot.Value.SetGridItem(null);
                }

                // First pass: Place items that have saved positions (and locked items if sorting)
                // This maintains user-customized item positions unless auto-sort is overriding
                foreach (KeyValuePair<int, uint> spot in itemPositions)
                {
                    Item i = world.Items.Get(spot.Value);
                    if (i != null)
                        // Place item if it's in the filtered list AND (not sorting OR item is locked)
                        if (filteredItems.Contains(i) && (!overrideSort || itemLocks.Contains(spot.Value)))
                        {
                            if (spot.Key < gridSlots.Count)
                            {
                                // Place the item at its saved slot position
                                gridSlots[spot.Key].SetGridItem(i);

                                // Mark the slot as locked if the item is locked in place
                                if (itemLocks.Contains(spot.Value))
                                    gridSlots[spot.Key].ItemGridLocked = true;

                                // Remove from the list so it won't be placed again
                                filteredItems.Remove(i);
                            }
                        }
                }

                // Second pass: Fill remaining empty slots with items that don't have saved positions
                // This includes new items or items being auto-sorted
                foreach (Item i in filteredItems)
                {
                    foreach (KeyValuePair<int, GridItem> slot in gridSlots)
                    {
                        // Skip slots that already have items
                        if (slot.Value.SlotItem != null)
                            continue;
                        // Place item in first available empty slot
                        slot.Value.SetGridItem(i);
                        AddItemSlot(i, slot.Key);
                        break;
                    }
                }

                // Rebuild the GridItems lookup dictionary for quick serial-to-GridItem access
                GridItems.Clear();

                bool searchTextEmpty = string.IsNullOrEmpty(searchText);
                // Third pass: Handle search visibility and highlighting
                foreach (KeyValuePair<int, GridItem> slot in gridSlots)
                {
                    // In "hide" search mode, hide all slots by default (they'll be shown if they match)
                    slot.Value.IsVisible = !(!searchTextEmpty && ProfileManager.CurrentProfile.GridContainerSearchMode == 0);
                    if (slot.Value.SlotItem != null && !searchTextEmpty)
                    {
                        // Add to GridItems lookup for items that need search processing
                        GridItems[slot.Value.SlotItem.Serial] = slot.Value;
                        if (SearchItemNameAndProps(searchText, slot.Value.SlotItem))
                        {
                            // In "highlight" mode (1), highlight matching items. In "hide" mode (0), show them
                            slot.Value.Highlight = ProfileManager.CurrentProfile.GridContainerSearchMode == 1;
                            slot.Value.IsVisible = true;
                        }
                    }
                }

                // Position all visible slots on screen based on grid layout
                SetGridPositions();
            }

            /// <summary>
            /// Intended for actively locking an item in place with Ctrl click
            /// </summary>
            /// <param name="slot"></param>
            /// <param name="locked"></param>
            /// <param name="saveEntry"></param>
            public void SetLockedSlot(int slot, bool locked, GridContainerSlotEntry saveEntry)
            {
                saveEntry.Locked = locked;

                if (gridSlots[slot].SlotItem == null)
                    return;

                uint itemSerial = gridSlots[slot].SlotItem.Serial;
                gridSlots[slot].ItemGridLocked = locked;

                if (!locked)
                {
                    // Unlock: remove from locks list
                    itemLocks.Remove(itemSerial);
                }
                else
                {
                    // Lock: add to locks list AND ensure it has a position entry
                    if (!itemLocks.Contains(itemSerial))
                        itemLocks.Add(itemSerial);

                    // Ensure the item is in ItemPositions so it maintains its position during rebuilds
                    // Without this, locked items get repositioned because they're not found in the first pass
                    AddItemSlot(itemSerial, slot);
                }
            }

            /// <summary>
            /// Set the visual grid items to the current GridSlots dict
            /// </summary>
            public void SetGridPositions()
            {
                int x = X_SPACING, y = 0;
                foreach (KeyValuePair<int, GridItem> slot in gridSlots)
                {
                    if (!slot.Value.IsVisible)
                    {
                        continue;
                    }
                    if (x + gridItemSize >= area.Width - 14) //14 is the scroll bar width
                    {
                        x = X_SPACING;
                        y += gridItemSize + Y_SPACING;
                    }
                    slot.Value.X = x;
                    slot.Value.Y = y;
                    slot.Value.Resize();
                    x += gridItemSize + X_SPACING;
                }
            }

            /// <summary>
            ///
            /// </summary>
            /// <param name="search"></param>
            /// <returns>List of items matching the search result, or all items if search is blank/profile does has hide search mode disabled</returns>
            public List<Item> SearchResults(string search)
            {
                UpdateItems(); //Why is this here? Because the server sends the container before it sends the data with it so sometimes we get empty containers without reloading the contents
                if (search != "")
                {
                    if (ProfileManager.CurrentProfile.GridContainerSearchMode == 0) //Hide search mode
                    {
                        var filteredContents = new List<Item>();
                        foreach (Item i in containerContents)
                        {
                            if (SearchItemNameAndProps(search, i))
                                filteredContents.Add(i);
                        }
                        return filteredContents;
                    }
                }
                return containerContents;
            }

            private static bool ContainsIgnoreCase(string source, string searchLower) => source != null && source.ToLower().Contains(searchLower);

            private bool SearchItemNameAndProps(string search, Item item)
            {
                if (item == null)
                    return false;

                string searchLower = search.ToLower();

                if (world.OPL.TryGetNameAndData(item.Serial, out string name, out string data))
                {
                    if (ContainsIgnoreCase(name, searchLower) || ContainsIgnoreCase(data, searchLower))
                        return true;
                }
                else
                {
                    if (ContainsIgnoreCase(item.Name, searchLower) || ContainsIgnoreCase(item.ItemData.Name, searchLower))
                        return true;
                }

                return false;
            }

            private void UpdateItems() => containerContents = GetItemsInContainer(world, container, gridContainer.SortMode, gridContainer.AutoSortContainer);

            public static List<Item> GetItemsInContainer(World world, Item container, GridSortMode sortMode = GridSortMode.GraphicAndHue, bool shouldSort = true)
            {
                var contents = new List<Item>();
                for (LinkedObject i = container.Items; i != null; i = i.Next)
                {
                    var item = (Item)i;
                    var layer = (Layer)item.ItemData.Layer;

                    if (container.IsCorpse && item.Layer > 0 && !Constants.BAD_CONTAINER_LAYERS[(int)layer])
                        continue;

                    if (item.ItemData.IsWearable && (layer == Layer.Face || layer == Layer.Beard || layer == Layer.Hair))
                        continue;

                    if (item.IsDestroyed)
                        continue;

                    GridHighlightData.ProcessItemOpl(world, item);

                    world.OPL.Contains(item); //Request tooltip data

                    contents.Add(item);
                }

                if (shouldSort)
                {
                    if (sortMode == GridSortMode.Name) // Sort by name
                    {
                        return contents.OrderBy(item => GetItemName(item)).ThenBy((x) => x.Graphic).ThenBy((x) => x.Hue).ToList();
                    }
                    else // Default: Sort by graphic + hue
                    {
                        return contents.OrderBy((x) => x.Graphic).ThenBy((x) => x.Hue).ToList();
                    }
                }

                return contents;
            }

            private static string GetItemName(Item item)
            {
                if (World.Instance?.OPL?.TryGetNameAndData(item.Serial, out string name, out string data) != true)
                    return !string.IsNullOrEmpty(item.Name) ? item.Name : item.ItemData.Name;

                if (string.IsNullOrEmpty(name))
                    return item.ItemData.Name;

                string itemAmountStr = item.Amount.ToString();
                return name.StartsWith(itemAmountStr)
                    ? name[itemAmountStr.Length..]
                    : name;
            }

            private void SetupGridItemControls()
            {
                UpdateItems();
                if (containerContents.Count > 125)
                    amount = containerContents.Count;

                for (int i = 0; i < amount; i++)
                {
                    if (gridSlots.ContainsKey(i)) continue;

                    var GI = new GridItem(world, 0, gridItemSize, container, gridContainer, i);
                    gridSlots.Add(i, GI);
                    area.Add(GI);
                }
            }
        }

        private class GridScrollArea : Control
        {
            private readonly ScrollBarBase _scrollBar;
            private int _lastWidth;
            private int _lastHeight;

            public GridScrollArea
            (
                int x,
                int y,
                int w,
                int h,
                int scroll_max_height = -1
            )
            {
                X = x;
                Y = y;
                Width = w;
                Height = h;
                _lastWidth = w;
                _lastHeight = h;

                _scrollBar = new ScrollBar(Width - 14, 0, Height);


                ScrollMaxHeight = scroll_max_height;

                _scrollBar.MinValue = 0;
                _scrollBar.MaxValue = scroll_max_height >= 0 ? scroll_max_height : Height;
                _scrollBar.Parent = this;

                AcceptMouseInput = true;
                WantUpdateSize = false;
                CanMove = true;
                ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways;
            }


            public int ScrollMaxHeight { get; set; } = -1;
            public ScrollbarBehaviour ScrollbarBehaviour { get; set; }
            public int ScrollValue => _scrollBar.Value;
            public int ScrollMinValue => _scrollBar.MinValue;
            public int ScrollMaxValue => _scrollBar.MaxValue;

            public Rectangle ScissorRectangle;

            public override void Update()
            {
                base.Update();

                CalculateScrollBarMaxValue();

                if (Width != _lastWidth || Height != _lastHeight)
                {
                    _scrollBar.X = Width - 14;
                    _scrollBar.Height = Height;
                    _lastWidth = Width;
                    _lastHeight = Height;
                }

                if (ScrollbarBehaviour == ScrollbarBehaviour.ShowAlways)
                {
                    _scrollBar.IsVisible = true;
                }
                else if (ScrollbarBehaviour == ScrollbarBehaviour.ShowWhenDataExceedFromView)
                {
                    _scrollBar.IsVisible = _scrollBar.MaxValue > _scrollBar.MinValue;
                }
            }

            public void Scroll(bool isup)
            {
                if (isup)
                {
                    _scrollBar.Value -= _scrollBar.ScrollStep;
                }
                else
                {
                    _scrollBar.Value += _scrollBar.ScrollStep;
                }
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                _scrollBar.Draw(batcher, x + _scrollBar.X, y + _scrollBar.Y);

                if (batcher.ClipBegin(x + ScissorRectangle.X, y + ScissorRectangle.Y, Width - 14 + ScissorRectangle.Width, Height + ScissorRectangle.Height))
                {
                    for (int i = 1; i < Children.Count; i++)
                    {
                        Control child = Children[i];

                        if (!child.IsVisible)
                        {
                            continue;
                        }

                        int finalY = y + child.Y - _scrollBar.Value + ScissorRectangle.Y;

                        child.Draw(batcher, x + child.X, finalY);
                    }

                    batcher.ClipEnd();
                }

                return true;
            }

            protected override void OnMouseWheel(MouseEventType delta)
            {
                switch (delta)
                {
                    case MouseEventType.WheelScrollUp:
                        _scrollBar.Value -= _scrollBar.ScrollStep;

                        break;

                    case MouseEventType.WheelScrollDown:
                        _scrollBar.Value += _scrollBar.ScrollStep;

                        break;
                }
            }

            public override void Clear()
            {
                for (int i = 1; i < Children.Count; i++)
                {
                    Children[i].Dispose();
                }
            }

            private void CalculateScrollBarMaxValue()
            {
                _scrollBar.Height = ScrollMaxHeight >= 0 ? ScrollMaxHeight : Height;
                bool maxValue = _scrollBar.Value == _scrollBar.MaxValue && _scrollBar.MaxValue != 0;

                int startX = 0, startY = 0, endX = 0, endY = 0;

                for (int i = 1; i < Children.Count; i++)
                {
                    Control c = Children[i];

                    if (c.IsVisible && !c.IsDisposed)
                    {
                        if (c.X < startX)
                        {
                            startX = c.X;
                        }

                        if (c.Y < startY)
                        {
                            startY = c.Y;
                        }

                        if (c.Bounds.Right > endX)
                        {
                            endX = c.Bounds.Right;
                        }

                        if (c.Bounds.Bottom > endY)
                        {
                            endY = c.Bounds.Bottom;
                        }
                    }
                }

                int width = Math.Abs(startX) + Math.Abs(endX);
                int height = Math.Abs(startY) + Math.Abs(endY) - _scrollBar.Height;
                height = Math.Max(0, height - (-ScissorRectangle.Y + ScissorRectangle.Height));

                if (height > 0)
                {
                    _scrollBar.MaxValue = height;

                    if (maxValue)
                    {
                        _scrollBar.Value = _scrollBar.MaxValue;
                    }
                }
                else
                {
                    _scrollBar.Value = _scrollBar.MaxValue = 0;
                }

                _scrollBar.UpdateOffset(0, Offset.Y);

                for (int i = 1; i < Children.Count; i++)
                {
                    Children[i].UpdateOffset(0, -_scrollBar.Value + ScissorRectangle.Y);
                }
            }
        }

        private class GridContainerPreview : Gump
        {
            private readonly AlphaBlendControl _background;
            private readonly Item _container;

            private const int WIDTH = 170;
            private const int HEIGHT = 150;
            private const int GRIDSIZE = 50;

            public GridContainerPreview(World world, uint serial, int x, int y) : base(world, serial, 0)
            {
                _container = World.Items.Get(serial);
                if (_container == null)
                {
                    Dispose();
                    return;
                }

                X = x - WIDTH - 20;
                Y = y - HEIGHT - 20;
                _background = new AlphaBlendControl();
                _background.Width = WIDTH;
                _background.Height = HEIGHT;

                CanCloseWithRightClick = true;
                Add(_background);
                InvalidateContents = true;
            }

            protected override void UpdateContents()
            {
                base.UpdateContents();
                if (InvalidateContents && !IsDisposed && IsVisible)
                {
                    if (_container != null && _container.Items != null)
                    {
                        int currentCount = 0, lastX = 0, lastY = 0;
                        for (LinkedObject i = _container.Items; i != null; i = i.Next)
                        {

                            var item = (Item)i;
                            if (item == null)
                                continue;

                            if (currentCount > 8)
                                break;

                            var gridItem = new StaticPic(item.DisplayedGraphic, item.Hue);
                            gridItem.X = lastX;
                            if (gridItem.X + GRIDSIZE > WIDTH)
                            {
                                gridItem.X = 0;
                                lastX = 0;
                                lastY += GRIDSIZE;

                            }
                            lastX += GRIDSIZE;
                            gridItem.Y = lastY;
                            //gridItem.Width = GRIDSIZE;
                            //gridItem.Height = GRIDSIZE;
                            Add(gridItem);

                            currentCount++;


                        }
                    }
                }
            }

            public override void Update()
            {
                if (IsDisposed)
                {
                    return;
                }

                if (_container == null || _container.IsDestroyed || _container.OnGround && _container.Distance > 3)
                {
                    Dispose();

                    return;
                }

                base.Update();
            }
        }
    }
}
