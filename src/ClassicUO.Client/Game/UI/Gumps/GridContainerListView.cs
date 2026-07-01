using System;
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.UI.Gumps
{
    public partial class GridContainer
    {
        private const int LIST_ROW_HEIGHT = 40;
        private const int LIST_ICON_SIZE = 40;
        private const int LIST_NAME_MAX_CHARS = 20;
        private const int LIST_COLUMN_WIDTH = 200;
        private const int LIST_COLUMN_GAP = 8;
        private readonly List<GridListItem> _listItems = new();

        public enum GridContainerViewMode
        {
            Grid = 0,
            List = 1
        }

        private GridContainerViewMode EffectiveViewMode
        {
            get
            {
                int mode = _gridContainerEntry?.ViewModeOverride
                    ?? ProfileManager.CurrentProfile?.GridContainerViewMode
                    ?? (int)GridContainerViewMode.Grid;

                return mode == (int)GridContainerViewMode.List ? GridContainerViewMode.List : GridContainerViewMode.Grid;
            }
        }

        private bool IsListView => EffectiveViewMode == GridContainerViewMode.List;

        private void InitializeListView() => EventSink.OPLOnReceive += OnListViewOplReceived;

        private void DisposeListView() => EventSink.OPLOnReceive -= OnListViewOplReceived;

        private void OnListViewOplReceived(object sender, OPLEventArgs e)
        {
            if (!IsListView)
                return;

            FindListItem(e.Serial)?.RefreshName();
        }

        private void SetContainerViewModeOverride(GridContainerViewMode? mode)
        {
            _gridContainerEntry.ViewModeOverride = mode.HasValue ? (int)mode.Value : null;
            _openRegularGump.ContextMenu = GenContextMenu();
            _gridContainerEntry.UpdateSaveDataEntry(this);
            RequestUpdateContents();
        }

        private int GetContainerViewModeOverrideIndex()
        {
            if (_gridContainerEntry.ViewModeOverride == (int)GridContainerViewMode.Grid)
                return 1;

            if (_gridContainerEntry.ViewModeOverride == (int)GridContainerViewMode.List)
                return 2;

            return 0;
        }

        private void SetContainerViewModeOverrideIndex(int index)
        {
            switch (index)
            {
                case 1:
                    SetContainerViewModeOverride(GridContainerViewMode.Grid);
                    break;
                case 2:
                    SetContainerViewModeOverride(GridContainerViewMode.List);
                    break;
                default:
                    SetContainerViewModeOverride(null);
                    break;
            }
        }

        private void RebuildListContainer(List<Item> sortedContents, string searchText)
        {
            SlotManager.HideGridSlots();

            int rowWidth = Math.Max(0, _scrollArea.Width - 14);
            int columns = Math.Max(1, rowWidth / LIST_COLUMN_WIDTH);
            int columnWidth = columns > 1 ? rowWidth / columns : rowWidth;
            int itemWidth = Math.Max(0, columnWidth - (columns > 1 ? LIST_COLUMN_GAP : 0));
            bool hideSearch = !string.IsNullOrEmpty(searchText)
                              && (ProfileManager.CurrentProfile?.GridContainerSearchMode ?? 0) == 0;
            bool highlightSearch = !string.IsNullOrEmpty(searchText)
                                   && ProfileManager.CurrentProfile?.GridContainerSearchMode == 1;

            if (!hideSearch)
                SlotManager.SetContainerContents(sortedContents);

            List<Item> displayContents = SlotManager.ApplyLockedPositions(sortedContents);

            for (int i = 0; i < displayContents.Count; i++)
            {
                GridListItem row;

                if (i < _listItems.Count)
                {
                    row = _listItems[i];
                }
                else
                {
                    row = new GridListItem(World, Container, this);
                    _listItems.Add(row);
                    _scrollArea.Add(row);
                }

                Item item = displayContents[i];
                int column = i % columns;
                int rowIndex = i / columns;
                row.X = column * columnWidth;
                row.Y = rowIndex * LIST_ROW_HEIGHT;
                row.Resize(itemWidth, LIST_NAME_MAX_CHARS);
                row.SetItem(item, highlightSearch && SlotManager.MatchesSearch(searchText, item), i);
                row.IsVisible = true;
            }

            for (int i = displayContents.Count; i < _listItems.Count; i++)
                _listItems[i].IsVisible = false;
        }

        private void HideListRows()
        {
            foreach (GridListItem row in _listItems)
                row.IsVisible = false;
        }

        private GridListItem FindListItem(uint serial)
        {
            foreach (GridListItem row in _listItems)
            {
                if (row.IsVisible && row.LocalSerial == serial)
                    return row;
            }

            return null;
        }

        private sealed class GridListItem : Control
        {
            private readonly World _world;
            private readonly Item _container;
            private readonly GridContainer _gridContainer;
            private readonly AlphaBlendControl _background;
            private readonly ResizableStaticPic _icon;
            private readonly Label _label;
            private readonly List<SimpleTimedTextGump> _timedTexts = new();
            private readonly Profile _profile = ProfileManager.CurrentProfile;
            private readonly int[] _spellbooks = [0x0EFA, 0x2253, 0x2252, 0x238C, 0x23A0, 0x2D50, 0x2D9D, 0x225A];

            private Item _item;
            private bool _mousePressedWhenEntered;
            private bool _selectHighlight;
            private int _listIndex;
            private int _maxNameChars = LIST_NAME_MAX_CHARS;

            public bool Highlight { get; private set; }

            public GridListItem(World world, Item container, GridContainer gridContainer)
            {
                _world = world;
                _container = container;
                _gridContainer = gridContainer;

                Height = LIST_ROW_HEIGHT;
                AcceptMouseInput = true;
                WantUpdateSize = false;

                _background = new AlphaBlendControl(0.25f)
                {
                    Width = Width,
                    Height = Height
                };

                _icon = new ResizableStaticPic(0, LIST_ICON_SIZE, LIST_ICON_SIZE)
                {
                    X = 0,
                    Y = 0
                };

                _label = new Label(string.Empty, true, 43, ishtml: true)
                {
                    X = LIST_ICON_SIZE + 4
                };

                Add(_background);
                Add(_icon);
                Add(_label);
            }

            public void Resize(int width, int maxNameChars = LIST_NAME_MAX_CHARS)
            {
                Width = width;
                Height = LIST_ROW_HEIGHT;
                _maxNameChars = maxNameChars;
                _background.Width = width;
                _background.Height = Height;
                _label.X = LIST_ICON_SIZE + 4;
                _label.Y = Math.Max(0, (Height - _label.Height) >> 1);
            }

            public void SetItem(Item item, bool highlight, int listIndex)
            {
                _item = item;
                Highlight = highlight;
                _listIndex = listIndex;
                LocalSerial = item?.Serial ?? 0;

                if (item == null)
                {
                    IsVisible = false;
                    ClearTooltip();
                    return;
                }

                _icon.Graphic = item.DisplayedGraphic;
                _icon.Hue = item.Hue;

                _world.OPL.Contains(item);
                RefreshName();
                SetTooltip(item);
            }

            public void RefreshName()
            {
                if (_item == null)
                    return;

                string name = GetDisplayName(_world, _item);
                int widthChars = Math.Max(8, (Width - LIST_ICON_SIZE - 8) / 7);
                _label.Text = name.Truncate(Math.Min(_maxNameChars, widthChars));
                _label.Y = Math.Max(0, (Height - _label.Height) >> 1);
            }

            private static string GetDisplayName(World world, Item item)
            {
                bool showAmount = item.ItemData.IsStackable && item.Amount > 1;

                if (world.OPL.TryGetNameAndData(item.Serial, out string oplName, out string _))
                {
                    string tooltipName = oplName?.Trim();

                    if (!string.IsNullOrWhiteSpace(tooltipName))
                        return tooltipName;
                }

                string name = NormalizeFallbackDisplayName(item.Name, item, showAmount);

                if (string.IsNullOrWhiteSpace(name))
                {
                    name = StringHelper.CapitalizeAllWords(
                        StringHelper.GetPluralAdjustedString(item.ItemData.Name, showAmount)
                    );
                }

                if (showAmount && !HasAmountPrefix(name, item.Amount))
                    return $"{item.Amount} {name}";

                return name;
            }

            private static string NormalizeFallbackDisplayName(string name, Item item, bool showAmount)
            {
                if (string.IsNullOrWhiteSpace(name))
                    return name;

                name = name.Trim();

                if (showAmount)
                    return StripAmountPrefix(name, item.Amount);

                return item.ItemData.IsStackable ? name : StripLeadingNumberPrefix(name);
            }

            private static string StripAmountPrefix(string name, int amount)
            {
                if (string.IsNullOrWhiteSpace(name) || amount <= 1)
                    return name;

                string amountPrefix = $"{amount} ";
                return HasAmountPrefix(name, amount) ? name[amountPrefix.Length..] : name;
            }

            private static bool HasAmountPrefix(string name, int amount)
            {
                if (string.IsNullOrWhiteSpace(name) || amount <= 1)
                    return false;

                return name.StartsWith($"{amount} ", StringComparison.Ordinal);
            }

            private static string StripLeadingNumberPrefix(string name)
            {
                int separatorIndex = name.IndexOf(' ');

                if (separatorIndex <= 0 || separatorIndex >= name.Length - 1)
                    return name;

                for (int i = 0; i < separatorIndex; i++)
                {
                    if (!char.IsDigit(name[i]))
                        return name;
                }

                return name[(separatorIndex + 1)..];
            }

            public void AddText(string text, ushort hue)
            {
                var timedText = new SimpleTimedTextGump(_world, text, hue, TimeSpan.FromSeconds(2), 200)
                {
                    X = ScreenCoordinateX,
                    Y = ScreenCoordinateY
                };

                _timedTexts.RemoveAll(tt => tt == null || tt.IsDisposed);

                foreach (SimpleTimedTextGump tt in _timedTexts)
                    tt.Y -= timedText.Height + 5;

                _timedTexts.Add(timedText);
                UIManager.Add(timedText);
            }

            public override bool OnMouseDoubleClick(int x, int y, MouseButtonType e)
            {
                base.OnMouseDoubleClick(x, y, e);

                if (e != MouseButtonType.Left || _world.TargetManager.IsTargeting || _item == null)
                    return false;

                if (!_gridContainer.IsLockSlot
                    && !_gridContainer.IsMultiMove
                    && _profile.DoubleClickToLootInsideContainers
                    && _gridContainer._isCorpse
                    && !_item.IsDestroyed
                    && !_item.ItemData.IsContainer
                    && _container != _world.Player.Backpack
                    && !_item.IsLocked
                    && _item.IsLootable)
                {
                    GameActions.GrabItem(_world, _item, _item.Amount);
                }
                else if (_gridContainer.IsMultiMove)
                {
                    SelectMatchingListRows();
                    MultiItemMoveGump.ShowNextTo(_gridContainer);
                }
                else
                {
                    GameActions.DoubleClick(_world, LocalSerial);
                }

                return true;
            }

            public override void OnMouseUp(int x, int y, MouseButtonType e)
            {
                base.OnMouseUp(x, y, e);

                if (e != MouseButtonType.Left)
                    return;

                if (Client.Game.UO.GameCursor.ItemHold.Enabled)
                {
                    DropHeldItem();
                    Mouse.CancelDoubleClick = true;
                    _mousePressedWhenEntered = false;
                    return;
                }

                if (_world.TargetManager.IsTargeting)
                {
                    _world.TargetManager.Target(_item ?? _container);

                    if (_item != null && _world.TargetManager.TargetingState == CursorTarget.SetTargetClientSide)
                        UIManager.Add(new InspectorGump(_world, _item));

                    Mouse.CancelDoubleClick = true;
                    return;
                }

            if (_gridContainer.IsLockSlot)
            {
                if (_item != null)
                    _gridContainer.SlotManager.ToggleItemLock(_item, _listIndex);

                Mouse.CancelDoubleClick = true;
                return;
            }

                if (_gridContainer.IsMultiMove && _item != null)
                {
                    _selectHighlight = MultiItemMoveGump.ToggleItem(_item);

                    if (_selectHighlight)
                        MultiItemMoveGump.ShowNextTo(_gridContainer);

                    Mouse.CancelDoubleClick = true;
                    return;
                }

                if (_gridContainer.IsAutoLoot
                    && _item != null
                    && _profile.EnableAutoLoot
                    && !_profile.HoldShiftForContext
                    && !_profile.HoldShiftToSplitStack)
                {
                    AutoLootManager.Instance.AddAutoLootEntry(_item.Graphic, _item.Hue, _item.Name);
                    GameActions.Print(_world, "Added this item to auto loot.");
                    return;
                }

                if (_item == null)
                    return;

                Point offset = Mouse.LDragOffset;

                if (Math.Abs(offset.X) >= Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS
                    || Math.Abs(offset.Y) >= Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS)
                {
                    return;
                }

                if ((_gridContainer._isCorpse && _profile.CorpseSingleClickLoot) || _gridContainer._quickLootThisContainer)
                {
                    ObjectActionQueue.Instance.Enqueue(ObjectActionQueueItem.QuickLoot(_item), ActionPriority.MoveItem);
                    Mouse.CancelDoubleClick = true;
                }
                else if (_world.ClientFeatures.TooltipsEnabled)
                {
                    _world.DelayedObjectClickManager.Set(_item.Serial, _gridContainer.X, _gridContainer.Y - 80, Time.Ticks + Mouse.MOUSE_DELAY_DOUBLE_CLICK);
                }
                else
                {
                    GameActions.SingleClick(_world, _item.Serial);
                }
            }

            protected override void OnMouseEnter(int x, int y)
            {
                base.OnMouseEnter(x, y);

                SelectedObject.Object = _world.Get(LocalSerial);
                _mousePressedWhenEntered = Mouse.LButtonPressed;

                if (_item?.ItemData.IsContainer == true
                    && _item.Items != null
                    && _profile.GridEnableContPreview
                    && !_spellbooks.Contains(_item.Graphic))
                {
                    UIManager.Add(new GridContainerPreview(_world, _item, Mouse.Position.X, Mouse.Position.Y));
                }

                if (_item != null && !HasTooltip)
                    SetTooltip(_item);
            }

            protected override void OnMouseExit(int x, int y)
            {
                base.OnMouseExit(x, y);

                if (Mouse.LButtonPressed && !_mousePressedWhenEntered)
                {
                    Point offset = Mouse.LDragOffset;

                    if ((Math.Abs(offset.X) >= Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS
                         || Math.Abs(offset.Y) >= Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS)
                        && _item != null
                        && !_gridContainer.IsMultiMove)
                    {
                        GameActions.PickUp(_world, _item, x, y);
                    }
                }

                GridContainerPreview g;

                while ((g = UIManager.GetGump<GridContainerPreview>()) != null)
                    g.Dispose();

                _mousePressedWhenEntered = false;
            }

            public override void Update()
            {
                base.Update();

                ushort hue = 0;

                if (_item != null && AutoLootManager.Instance.IsBeingLooted(_item.Serial))
                    hue = 32;
                else if (_item != null && _gridContainer.SlotManager.IsItemLocked(_item.Serial))
                    hue = 2;
                else if (_selectHighlight || Highlight || MouseIsOver)
                    hue = 53;

                if (_background.Hue != hue)
                    _background.Hue = hue;
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                bool result = base.Draw(batcher, x, y);

                if (_item?.MatchesHighlightData == true)
                {
                    Texture2D borderTexture = SolidColorTextureCache.GetTexture(_item.HighlightColor);
                    Vector3 hueVector = new(1, 0, 1);
                    int bx = x + 6;
                    int by = y + 6;
                    int size = LIST_ICON_SIZE - 12;

                    batcher.Draw(borderTexture, new Rectangle(bx, by, size, 1), hueVector);
                    batcher.Draw(borderTexture, new Rectangle(bx, by + 1, 1, size - 2), hueVector);
                    batcher.Draw(borderTexture, new Rectangle(bx + size - 1, by + 1, 1, size - 2), hueVector);
                    batcher.Draw(borderTexture, new Rectangle(bx, by + size - 1, size, 1), hueVector);
                }

                return result;
            }

            private void DropHeldItem()
            {
                if (_item?.ItemData.IsContainer == true)
                {
                    GameActions.DropItem(Client.Game.UO.GameCursor.ItemHold.Serial, 0xFFFF, 0xFFFF, 0, _item.Serial);
                }
                else if (_item != null
                         && _item.ItemData.IsStackable
                         && _item.Graphic == Client.Game.UO.GameCursor.ItemHold.Graphic)
                {
                    GameActions.DropItem(Client.Game.UO.GameCursor.ItemHold.Serial, _item.X, _item.Y, 0, _item.Serial);
                }
                else
                {
                    GameActions.DropItem(Client.Game.UO.GameCursor.ItemHold.Serial, 0xFFFF, 0xFFFF, 0, _container.Serial);
                }
            }

            private void SelectMatchingListRows()
            {
                if (_item == null)
                    return;

                if (MultiItemMoveGump.TrySelect(_item))
                    _selectHighlight = true;

                ushort graphic = _item.Graphic;
                ushort hue = _item.Hue;

                foreach (GridListItem row in _gridContainer._listItems)
                {
                    Item item = row._item;

                    if (!row.IsVisible
                        || item == null
                        || graphic != item.Graphic
                        || hue != item.Hue
                        || MultiItemMoveGump.IsSelected(item.Serial))
                    {
                        continue;
                    }

                    if (MultiItemMoveGump.TrySelect(item))
                        row._selectHighlight = true;
                }
            }
        }
    }
}
