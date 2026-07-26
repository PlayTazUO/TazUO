using System;
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps.GridHighLight;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using static ClassicUO.Game.UI.Gumps.GridContainer;

namespace ClassicUO.Game.UI.Gumps.GridContainers;

    public class GridSlotManager
    {
        private Dictionary<int, GridItem> _gridSlots = new Dictionary<int, GridItem>();
        private Item _container;
        private List<Item> _containerContents;
        private int _amount = 125;
        private Control _area;
        private Dictionary<int, uint> _itemPositions = new Dictionary<int, uint>();
        private HashSet<uint> _itemLocks = new HashSet<uint>();
        private World _world;
        private GridContainer _gridContainer;
        private bool _bandsActive;

        public Dictionary<int, GridItem> GridSlots => _gridSlots;
        public List<Item> ContainerContents => _containerContents ??= new List<Item>();
        public Dictionary<int, uint> ItemPositions => _itemPositions;

        /// <summary>
        /// Get the GridItem of a serial if it exists
        /// </summary>
        public Dictionary<uint, GridItem> GridItems { get; } = new();

        public GridSlotManager(World world, uint thisContainer, GridContainer gridContainer, Control controlArea)
        {
            #region VARS
            this._world = world;
            this._gridContainer = gridContainer;
            _area = controlArea;
            foreach (GridContainerSlotEntry item in gridContainer.GridContainerEntry.Slots.Values)
            {
                ItemPositions[item.Slot] = item.Serial;

                if (item.Locked)
                    if (!_itemLocks.Contains(item.Serial))
                        _itemLocks.Add(item.Serial);
            }
            _container = world.Items.Get(thisContainer);
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
            _gridContainer.GridContainerEntry.GetSlot(serial).Slot = specificSlot;

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

            foreach (GridItem slot in _gridSlots.Values)
            {
                if (slot.SlotItem?.Serial == serial)
                {
                    GridItems[serial] = slot;
                    return slot;
                }
            }

            return null;
        }

        public bool IsItemLocked(uint serial) => _itemLocks.Contains(serial);

        /// <summary>
        /// Rebuilds the container's visual layout by placing items in grid slots.
        /// </summary>
        /// <param name="filteredItems">List of items to display (may be filtered by search)</param>
        /// <param name="searchText">Search query for filtering/highlighting items</param>
        /// <param name="overrideSort">If true, only locked items maintain their positions</param>
        public void RebuildContainer(List<Item> filteredItems, string searchText = "", bool overrideSort = false)
        {
            // Ensure we have enough grid slots for all items
            SetupGridItemControls();

            // Clear all grid slots by setting them to null
            foreach (KeyValuePair<int, GridItem> slot in _gridSlots)
            {
                slot.Value.SetGridItem(null);
            }

            // Cache the band-layout state for this rebuild; SetGridPositions (and the click handlers
            // in GridItem) read it instead of re-scanning the band config on every layout pass.
            _bandsActive = BandsActive();

            if (_bandsActive)
            {
                // Band layout ignores saved slot positions and locks: items are grouped by band
                // (first matching band wins) and laid out in band order, then any unmatched items.
                AssignBands(filteredItems);
            }
            else
            {
                PlaceItemsDefault(filteredItems, overrideSort);
            }

            // Rebuild the GridItems lookup dictionary for quick serial-to-GridItem access
            GridItems.Clear();

            bool searchTextEmpty = string.IsNullOrEmpty(searchText);
            // Handle search visibility and highlighting
            foreach (KeyValuePair<int, GridItem> slot in _gridSlots)
            {
                // In band mode only slots holding an item are shown (empty cells and inter-band
                // spacing are handled by the positioning pass). Otherwise fall back to the standard
                // rule, hiding everything by default in "hide" search mode.
                slot.Value.IsVisible = _bandsActive
                    ? slot.Value.SlotItem != null
                    : (!_gridContainer.IsListView || slot.Value.SlotItem != null) && !(!searchTextEmpty && ProfileManager.CurrentProfile.GridContainerSearchMode == 0);

                if (slot.Value.SlotItem != null)
                {
                    // Keep serial lookup populated for OPL/list-name refreshes.
                    GridItems[slot.Value.SlotItem.Serial] = slot.Value;
                    if (!searchTextEmpty && SearchItemNameAndProps(searchText, slot.Value.SlotItem))
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
        /// Default (non-band) item placement: honors saved slot positions/locks and fills the rest.
        /// </summary>
        private void PlaceItemsDefault(List<Item> filteredItems, bool overrideSort = false)
        {
            // Serials of everything we're allowed to display, for O(1) membership checks.
            var filteredSerials = new HashSet<uint>(filteredItems.Count);
            foreach (Item i in filteredItems)
                filteredSerials.Add(i.Serial);

            // Tracks which items have already been placed so the second pass can skip them
            // without mutating the caller's list (which may be a shared/cached collection).
            var placed = new HashSet<uint>();

            // First pass: Place items that have saved positions (and locked items if sorting)
            // This maintains user-customized item positions unless auto-sort is overriding
            foreach (KeyValuePair<int, uint> spot in _itemPositions)
            {
                uint serial = spot.Value;

                if (spot.Key >= _gridSlots.Count || placed.Contains(serial))
                    continue;

                // Place item if it's in the filtered list AND (not sorting OR item is locked)
                if (!filteredSerials.Contains(serial) || (overrideSort && !_itemLocks.Contains(serial)))
                    continue;

                Item i = _world.Items.Get(serial);
                if (i == null)
                    continue;

                // Place the item at its saved slot position
                _gridSlots[spot.Key].SetGridItem(i);

                // Mark the slot as locked if the item is locked in place
                if (_itemLocks.Contains(serial))
                    _gridSlots[spot.Key].ItemGridLocked = true;

                placed.Add(serial);
            }

            // Second pass: Fill remaining empty slots with items that don't have saved positions
            // This includes new items or items being auto-sorted
            int nextFreeSlot = 0;
            foreach (Item i in filteredItems)
            {
                if (placed.Contains(i.Serial))
                    continue;

                // Advance to the next empty slot (slots fill front-to-back, so we never need
                // to re-scan from the beginning).
                while (nextFreeSlot < _gridSlots.Count && _gridSlots[nextFreeSlot].SlotItem != null)
                    nextFreeSlot++;

                if (nextFreeSlot >= _gridSlots.Count)
                    break;

                _gridSlots[nextFreeSlot].SetGridItem(i);
                AddItemSlot(i, nextFreeSlot);
                placed.Add(i.Serial);
            }
        }

        /// <summary>
        /// Whether band layout is currently active: enabled in the profile, the container is in grid
        /// (not list) view, and at least one enabled band is configured.
        /// </summary>
        public bool BandsActive()
        {
            if (_gridContainer.IsListView)
                return false;

            // Per-container override: bands can be turned off for this container even when enabled.
            if (_gridContainer.BandsDisabledForContainer)
                return false;

            return ActiveGroup.HasActiveBands();
        }

        /// <summary>The band group that applies to this container (corpse / backpack / other).</summary>
        private GridContainerBandGroup ActiveGroup =>
            GridContainerBandsConfig.Current.GetGroupForContainer(_gridContainer.IsCorpse, _gridContainer.IsPlayerBackpack);

        /// <summary>
        /// Groups items into bands (first matching enabled band wins) and assigns them to grid slots in
        /// band order, followed by any unmatched items. Each band's slots take the band's background
        /// color and a shared band-group index used by the positioning pass to insert spacing.
        /// </summary>
        private void AssignBands(List<Item> filteredItems)
        {
            List<GridContainerBand> bands = ActiveGroup.Bands;

            var bandItems = new List<Item>[bands.Count];
            var unmatched = new List<Item>();

            // Bucket items, preserving the incoming (already-sorted) order within each band.
            foreach (Item item in filteredItems)
            {
                int matched = -1;
                for (int b = 0; b < bands.Count; b++)
                {
                    GridContainerBand band = bands[b];
                    if (band is not { Enabled: true })
                        continue;

                    if (band.Matches(item.Graphic, item.Hue, item.ItemData.Layer))
                    {
                        matched = b;
                        break;
                    }
                }

                if (matched >= 0)
                    (bandItems[matched] ??= new List<Item>()).Add(item);
                else
                    unmatched.Add(item);
            }

            int slotIndex = 0;
            int groupIndex = 0;

            for (int b = 0; b < bands.Count; b++)
            {
                List<Item> items = bandItems[b];
                if (items == null || items.Count == 0)
                    continue;

                GridContainerBand band = bands[b];
                Color? color = band.UseBackgroundColor ? band.GetBackgroundColor() : null;

                if (!PlaceBandItems(items, groupIndex, color, ref slotIndex))
                    return;

                groupIndex++;
            }

            // Unmatched items are shown last as their own uncolored group.
            if (unmatched.Count > 0)
                PlaceBandItems(unmatched, groupIndex, null, ref slotIndex);
        }

        /// <summary>Assigns a band group's items to consecutive grid slots. Returns false when slots run out.</summary>
        private bool PlaceBandItems(List<Item> items, int groupIndex, Color? color, ref int slotIndex)
        {
            foreach (Item item in items)
            {
                if (slotIndex >= _gridSlots.Count)
                    return false;

                GridItem slot = _gridSlots[slotIndex];
                slot.SetGridItem(item);
                slot.BandGroup = groupIndex;
                slot.SetBandColor(color);
                slotIndex++;
            }

            return true;
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

            if (_gridSlots[slot].SlotItem == null)
                return;

            uint itemSerial = _gridSlots[slot].SlotItem.Serial;
            _gridSlots[slot].ItemGridLocked = locked;

            if (!locked)
            {
                // Unlock: remove from locks list
                _itemLocks.Remove(itemSerial);
            }
            else
            {
                // Lock: add to locks list AND ensure it has a position entry
                if (!_itemLocks.Contains(itemSerial))
                    _itemLocks.Add(itemSerial);

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
            if (_gridContainer.IsListView)
            {
                int rowWidth = Math.Max(0, _area.Width - GridScrollArea.SCROLLBAR_WIDTH);
                int columns = Math.Max(1, rowWidth / LIST_COLUMN_WIDTH);
                int columnWidth = columns > 1 ? rowWidth / columns : rowWidth;
                int itemWidth = Math.Max(0, columnWidth - (columns > 1 ? LIST_COLUMN_GAP : 0));
                int visibleIndex = 0;

                foreach (KeyValuePair<int, GridItem> slot in _gridSlots)
                {
                    if (!slot.Value.IsVisible || slot.Value.SlotItem == null)
                        continue;

                    int column = visibleIndex % columns;
                    int row = visibleIndex / columns;
                    slot.Value.X = column * columnWidth;
                    slot.Value.Y = row * LIST_ROW_HEIGHT;
                    slot.Value.ResizeList(itemWidth);
                    visibleIndex++;
                }

                return;
            }

            if (_bandsActive)
            {
                SetBandGridPositions();
                return;
            }

            int x = X_SPACING, y = 0;
            foreach (KeyValuePair<int, GridItem> slot in _gridSlots)
            {
                if (!slot.Value.IsVisible)
                {
                    continue;
                }
                if (x + GridItemSize >= _area.Width - GridScrollArea.SCROLLBAR_WIDTH)
                {
                    x = X_SPACING;
                    y += GridItemSize + Y_SPACING;
                }
                slot.Value.X = x;
                slot.Value.Y = y;
                slot.Value.Resize();
                x += GridItemSize + X_SPACING;
            }
        }

        /// <summary>
        /// Positions visible item slots grouped by band. Each band lays its items out in grid rows;
        /// when a new band starts the layout drops to a fresh row and adds <see cref="BAND_SPACING"/>
        /// of vertical space, leaving the remainder of the previous band's last row empty.
        /// </summary>
        private void SetBandGridPositions()
        {
            int usableWidth = Math.Max(0, _area.Width - GridScrollArea.SCROLLBAR_WIDTH);
            int columns = Math.Max(1, (usableWidth - X_SPACING) / (GridItemSize + X_SPACING));
            int bandPadding = Math.Max(0, GridContainerBandsConfig.Current.BandPadding);

            int x = X_SPACING, y = 0, col = 0;
            int prevGroup = int.MinValue;

            foreach (KeyValuePair<int, GridItem> kv in _gridSlots)
            {
                GridItem slot = kv.Value;
                if (!slot.IsVisible || slot.SlotItem == null)
                    continue;

                int group = slot.BandGroup;

                if (prevGroup == int.MinValue)
                {
                    // First item.
                    x = X_SPACING;
                    y = 0;
                    col = 0;
                }
                else if (group != prevGroup)
                {
                    // New band: drop to a fresh row and add the band gap.
                    y += GridItemSize + Y_SPACING + bandPadding;
                    x = X_SPACING;
                    col = 0;
                }
                else if (col >= columns)
                {
                    // Wrap within the current band.
                    y += GridItemSize + Y_SPACING;
                    x = X_SPACING;
                    col = 0;
                }

                slot.X = x;
                slot.Y = y;
                slot.Resize();

                x += GridItemSize + X_SPACING;
                col++;
                prevGroup = group;
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
                    foreach (Item i in _containerContents)
                    {
                        if (SearchItemNameAndProps(search, i))
                            filteredContents.Add(i);
                    }
                    return filteredContents;
                }
            }
            return _containerContents;
        }

        private bool SearchItemNameAndProps(string search, Item item)
        {
            if (item == null)
                return false;

            if (item.OPLName.ContainsIgnoreCase(search) || item.OPLData.ContainsIgnoreCase(search))
                return true;

            if (item.GetNormalizedName(false).ContainsIgnoreCase(search))
                return true;

            return false;
        }

        private void UpdateItems() => _containerContents = GetItemsInContainer(_world, _container, _gridContainer.SortMode, _gridContainer.AutoSortContainer);

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
                    return contents.OrderBy(GetItemName).ThenBy(((x) => x.Amount)).ToList();
                }
                else if (sortMode == GridSortMode.Layer) // Sort by layer
                {
                    return contents.OrderBy((x) => x.ItemData.Layer).ThenBy((x) => x.Graphic).ThenBy((x) => x.Hue).ToList();
                }
                else // Default: Sort by graphic + hue
                {
                    return contents.OrderBy((x) => x.Graphic).ThenBy((x) => x.Hue).ToList();
                }
            }

            return contents;
        }

        private static string GetItemName(Item item) => item.GetNormalizedName(false);

        private void SetupGridItemControls()
        {
            UpdateItems();
            if (_containerContents.Count > 125)
                _amount = _containerContents.Count;

            for (int i = 0; i < _amount; i++)
            {
                if (_gridSlots.ContainsKey(i)) continue;

                var gi = new GridItem(_world, 0, GridItemSize, _container, _gridContainer, i);
                _gridSlots.Add(i, gi);
                _area.Add(gi);
            }
        }
    }
