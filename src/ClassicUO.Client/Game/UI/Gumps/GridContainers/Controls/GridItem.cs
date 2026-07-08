using System;
using System.Collections.Generic;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.UI.Controls;

public class GridItem : Control
{
    private bool _mousePressedWhenEntered;
    private readonly Item _container;
    private Item _item;
    private readonly GridContainer _gridContainer;
    private readonly int _slot;
    private GridContainerPreview _preview;
    private Label _count;
    private readonly AlphaBlendControl _background;
    private readonly Label _listLabel;
    private CustomToolTip _toolTipThis, _toolTipitem1, _toolTipitem2;
    private readonly List<SimpleTimedTextGump> _timedTexts = new();
    private readonly World _world;
    private static readonly HashSet<uint> _toggledThisAltDrag = new HashSet<uint>();
    private static bool _altDragActive;
    private bool _selectHighlight;
    private bool _isListLayout;

    public bool ItemGridLocked { get; set; }
    public bool Highlight { get; set; }
    public static int GridItemSize => (int)Math.Round(50 * (ProfileManager.CurrentProfile.GridContainersScale / 100f));

    public Item SlotItem
    {
        get => _item;
        set
        {
            _item = value;
            LocalSerial = value?.Serial ?? 0;
        }
    }

    private readonly int[] _spellbooks = [0x0EFA, 0x2253, 0x2252, 0x238C, 0x23A0, 0x2D50, 0x2D9D, 0x225A];

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

        _background = new AlphaBlendControl(0.25f)
        {
            Width = size,
            Height = size
        };

        Width = Height = size;

        Add(_background);

        _listLabel = new Label(string.Empty, true, 43, ishtml: true)
        {
            X = GridContainer.LIST_ICON_SIZE + 4,
            AcceptMouseInput = false,
            IsVisible = false
        };

        Add(_listLabel);

        SetGridItem(_item);
    }

    public void AddText(string text, ushort hue)
    {
        // Offset accounts for the scroll area's scroll position; ScreenCoordinate alone
        // ignores it, which pushes the text far off screen once the grid is scrolled
        // (which happens readily when the container is scaled up and cells overflow the view).
        var timedText = new SimpleTimedTextGump(_world, text, (uint)hue, TimeSpan.FromSeconds(2), 200)
        {
            X = ScreenCoordinateX + Offset.X,
            Y = ScreenCoordinateY + Offset.Y
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
        _isListLayout = false;
        Width = GridItemSize;
        Height = GridItemSize;
        _background.Width = GridItemSize;
        _background.Height = GridItemSize;
        _listLabel.IsVisible = false;
        RepositionCount();
    }

    public void ResizeList(int width)
    {
        _isListLayout = true;
        Width = width;
        Height = GridContainer.LIST_ROW_HEIGHT;
        _background.Width = width;
        _background.Height = Height;
        _listLabel.X = GridContainer.LIST_ICON_SIZE + 4;
        _listLabel.IsVisible = _item != null;
        RefreshListName();
        RepositionCount();
    }

    private void RepositionCount()
    {
        if (_count != null)
            _count.Y = Height - _count.Height;
    }

    public void RefreshListName()
    {
        if (!_isListLayout || _item == null)
            return;

        string name = _item.GetNormalizedName(_item.ItemData.IsStackable && _item.Amount > 1);
        int widthChars = Math.Max(8, (Width - GridContainer.LIST_ICON_SIZE - 8) / 7);
        _listLabel.Text = name.Truncate(Math.Min(GridContainer.LIST_NAME_MAX_CHARS, widthChars));
        _listLabel.Y = Math.Max(0, (Height - _listLabel.Height) >> 1);
        _listLabel.IsVisible = true;
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
            _listLabel.Text = string.Empty;
            _listLabel.IsVisible = false;
            ItemGridLocked = false;
            CanMove = true;
            _hasItem = false;
            _shouldDraw = !_gridContainer.IsCorpse;
            _hasLastLowContrastCacheKey = false;
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
        _hasLastLowContrastCacheKey = false;

        LocalSerial = item.Serial;
        int itemAmt = _item.ItemData.IsStackable ? _item.Amount : 1;

        if (itemAmt > 1)
        {
            _count?.Dispose();
            _count = new Label(itemAmt.ToString(), true, 0x0481, align: TEXT_ALIGN_TYPE.TS_LEFT)
            {
                X = 1
            };
            RepositionCount();
        }

        RefreshListName();
        SetTooltip(_item);
    }

    /// <summary>
    /// Called when various cached settings like border hue and alpha are updated.
    /// </summary>
    public static void StaticGridContainerSettingUpdated() => _borderHueVec = ShaderHueTranslator.GetHueVector(ProfileManager.CurrentProfile.GridBorderHue, false, (float)ProfileManager.CurrentProfile.GridBorderAlpha / 100);

    public override bool OnMouseDoubleClick(int x, int y, MouseButtonType e)
    {
        base.OnMouseDoubleClick(x, y, e);

        if (e != MouseButtonType.Left || _world.TargetManager.IsTargeting || _item == null)
            return false;

        if (!_gridContainer.IsLockSlot &&
            !_gridContainer.IsMultiMove &&
            _profile.DoubleClickToLootInsideContainers &&
            _gridContainer.IsCorpse &&
            !_item.IsDestroyed &&
            !_item.ItemData.IsContainer &&
            _container != _world.Player.Backpack &&
            !_item.IsLocked &&
            _item.IsLootable)
        {
            GameActions.GrabItem(_world, _item, _item.Amount);
        }
        else if (_gridContainer.IsMultiMove && _item != null)
        {
            // The second click of this double-click is still a held button press, so the
            // combo-drag logic in PreDraw would otherwise treat it as a fresh gesture and
            // toggle the double-clicked item back off. Mark the gesture active and record
            // this serial as already handled so it stays selected along with its matches.
            _altDragActive = true;
            _toggledThisAltDrag.Add(_item.Serial);

            MultiItemMoveGump.TrySelect(_item);
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

    public override void OnMouseUp(int x, int y, MouseButtonType e)
    {
        base.OnMouseUp(x, y, e);

        if (e != MouseButtonType.Left)
            return;

        // Modes are mutually exclusive and checked in priority order.
        if (Client.Game.UO.GameCursor.ItemHold.Enabled)
            DropHeldItem();
        else if (_world.TargetManager.IsTargeting)
            HandleTargetingClick();
        else if (_gridContainer.IsLockSlot)
            HandleLockSlotClick();
        else if (_gridContainer.IsMultiMove && _item != null)
            HandleMultiMoveClick();
        else if (_gridContainer.IsAutoLoot && _item != null && _profile.EnableAutoLoot && !_profile.HoldShiftForContext && !_profile.HoldShiftToSplitStack)
            AddToAutoLoot();
        else if (_item != null)
            HandlePlainClick();
    }

    // Drops the item currently held by the cursor: into this slot's item if it's a container,
    // stacked onto a matching stackable, or into this grid slot otherwise.
    private void DropHeldItem()
    {
        if (_item != null && _item.ItemData.IsContainer)
        {
            GameActions.DropItem(Client.Game.UO.GameCursor.ItemHold.Serial, 0xFFFF, 0xFFFF, 0, _item.Serial);
        }
        else if (_item != null && _item.ItemData.IsStackable && _item.Graphic == Client.Game.UO.GameCursor.ItemHold.Graphic)
        {
            GameActions.DropItem(Client.Game.UO.GameCursor.ItemHold.Serial, _item.X, _item.Y, 0, _item.Serial);
        }
        else
        {
            Rectangle containerBounds = _world.ContainerManager.Get(_container.Graphic).Bounds;
            _gridContainer.SlotManager.AddItemSlot(Client.Game.UO.GameCursor.ItemHold.Serial, _slot);
            (int X, int Y) pos = GetBoxPosition(_slot, Client.Game.UO.GameCursor.ItemHold.Graphic, containerBounds.Width, containerBounds.Height);
            GameActions.DropItem(Client.Game.UO.GameCursor.ItemHold.Serial, pos.X, pos.Y, 0, _container.Serial);
        }

        Mouse.CancelDoubleClick = true;
        _mousePressedWhenEntered = false; //Fix for not needing to move mouse out of grid box to re-drag item
    }

    private void HandleTargetingClick()
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

    private void HandleLockSlotClick()
    {
        if (_item != null)
            _gridContainer.SlotManager.SetLockedSlot(_slot, !ItemGridLocked, _gridContainer.GridContainerEntry.GetSlot(_item.Serial));

        Mouse.CancelDoubleClick = true;
    }

    private void HandleMultiMoveClick()
    {
        // If no drag occurred, toggle on click to prevent missed quick taps.
        _selectHighlight = !_altDragActive
            ? MultiItemMoveGump.ToggleItem(_item)
            : MultiItemMoveGump.IsSelected(_item.Serial);

        if (_selectHighlight)
            MultiItemMoveGump.ShowNextTo(_gridContainer);

        Mouse.CancelDoubleClick = true;
    }

    private void AddToAutoLoot()
    {
        AutoLootManager.Instance.AddAutoLootEntry(_item.Graphic, _item.Hue, _item.Name);
        GameActions.Print(_world, $"Added this item to auto loot.");
    }

    // A plain left click on an item: quick-loot it if enabled, otherwise show its tooltip/single-click.
    private void HandlePlainClick()
    {
        Point offset = Mouse.LDragOffset;
        if (Math.Abs(offset.X) >= Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS || Math.Abs(offset.Y) >= Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS)
            return;

        if ((_gridContainer.IsCorpse && _profile.CorpseSingleClickLoot) || _gridContainer.QuickLootThisContainer)
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
                    if (!_gridContainer.IsMultiMove)
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
                _profile.GridEnableContPreview && !_spellbooks.Contains(_item.Graphic))
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
    private static readonly Dictionary<LowContrastCacheKey, bool> _lowContrastCache = new();
    private static readonly Queue<LowContrastCacheKey> _lowContrastCacheOrder = new();
    private static readonly Vector3 _lowContrastOutlineHue = ShaderHueTranslator.GetOutlineHueVector(1f);
    private static readonly Vector3 _whiteOutlineColor = new(1f, 1f, 1f);
    private static readonly Vector3 _lowContrastSpotlightOuterHue = ShaderHueTranslator.GetHueVector(0, false, 0.16f);
    private static readonly Vector3 _lowContrastSpotlightInnerHue = ShaderHueTranslator.GetHueVector(0, false, 0.28f);
    private bool _hasItem;
    private static readonly Vector3 _highLightHue = ShaderHueTranslator.GetHueVector(0x34, false, 1);
    private static Vector3 _borderHueVec;
    private bool _shouldDraw;
    private LowContrastCacheKey _lastLowContrastCacheKey;
    private bool _lastLowContrastResult;
    private bool _hasLastLowContrastCacheKey;
    private const ushort PARTIAL_HUE_FLAG = 0x8000;
    private const ushort SPECTRAL_HUE_FLAG = 0x4000;
    private const int LOW_CONTRAST_OUTLINE_PADDING = 1;
    private const int LOW_CONTRAST_CACHE_MAX_ENTRIES = 8192;
    private const double LOW_CONTRAST_AVERAGE_RATIO_THRESHOLD = 1.45d;
    private const double LOW_CONTRAST_PIXEL_RATIO_THRESHOLD = 1.35d;
    private const double LOW_CONTRAST_PIXEL_FRACTION_THRESHOLD = 0.75d;
    private static readonly double[] _srgbToLinearLut = CreateSrgbToLinearLut();

    private readonly record struct LowContrastCacheKey(
        ushort Graphic,
        ushort Hue,
        bool PartialHue,
        bool SpectralHue,
        ushort BackgroundHue,
        byte BackgroundAlpha
    );

    private readonly record struct LowContrastSample(
        double AverageLuminance,
        double LowContrastPixelFraction
    );

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
    private void DrawHighlightBorder(UltimaBatcher2D batcher, Rectangle cellBounds, Texture2D borderTexture, Vector3 borderHueVec)
    {
        int bsize = _profile.GridHighlightSize;
        int bx = cellBounds.X + 6;
        int by = cellBounds.Y + 6;
        int innerWidth = cellBounds.Width - 12;
        int innerHeight = cellBounds.Height - 12;

        // Top border
        batcher.Draw(borderTexture, new Rectangle(bx, by, innerWidth, bsize), borderHueVec);

        // Left border
        batcher.Draw(borderTexture, new Rectangle(bx, by + bsize, bsize, innerHeight - (bsize * 2)), borderHueVec);

        // Right border
        batcher.Draw(borderTexture, new Rectangle(bx + innerWidth - bsize, by + bsize, bsize, innerHeight - (bsize * 2)), borderHueVec);

        // Bottom border
        batcher.Draw(borderTexture, new Rectangle(bx, by + innerHeight - bsize, innerWidth, bsize), borderHueVec);
    }

    private LowContrastCacheKey CreateLowContrastCacheKey()
    {
        ushort backgroundHue = _profile.Grid_UseContainerHue && _container != null
            ? _container.Hue
            : _profile.AltGridContainerBackgroundHue;
        ushort itemHue = _item?.Hue ?? 0;
        bool partialHue = _item?.ItemData.IsPartialHue ?? false;
        bool spectralHue = NormalizeItemHueForRendering(ref itemHue, ref partialHue);

        return new LowContrastCacheKey(
            _item?.DisplayedGraphic ?? 0,
            itemHue,
            partialHue,
            spectralHue,
            backgroundHue,
            _profile.ContainerOpacity
        );
    }

    private static bool NormalizeItemHueForRendering(ref ushort hue, ref bool partialHue)
    {
        if ((hue & PARTIAL_HUE_FLAG) != 0)
        {
            partialHue = true;
            hue &= 0x7FFF;
        }

        if (hue == 0)
            partialHue = false;

        if ((hue & SPECTRAL_HUE_FLAG) == 0)
            return false;

        hue = 0;
        partialHue = false;
        return true;
    }

    private bool IsLowContrastItem()
    {
        if (!_hasItem || _item == null || _texture == null || _texture.IsDisposed || _rect.Width <= 0 || _rect.Height <= 0)
            return false;

        LowContrastCacheKey key = CreateLowContrastCacheKey();

        if (_hasLastLowContrastCacheKey && key.Equals(_lastLowContrastCacheKey))
            return _lastLowContrastResult;

        if (!_lowContrastCache.TryGetValue(key, out bool result))
        {
            double backgroundLuminance = GetBackgroundLuminance(key.BackgroundHue, key.BackgroundAlpha);
            ArtInfo artInfo = Client.Game.UO.Arts.GetArtPixels(key.Graphic);
            LowContrastSample sample = GetItemContrastSample(
                artInfo.Pixels,
                artInfo.Width,
                artInfo.Height,
                _rect,
                key.Hue,
                key.PartialHue,
                key.SpectralHue,
                backgroundLuminance
            );

            double contrastRatio = GetContrastRatio(sample.AverageLuminance, backgroundLuminance);

            result = contrastRatio < LOW_CONTRAST_AVERAGE_RATIO_THRESHOLD
                && sample.LowContrastPixelFraction >= LOW_CONTRAST_PIXEL_FRACTION_THRESHOLD;
            AddLowContrastCacheResult(key, result);
        }

        _lastLowContrastCacheKey = key;
        _lastLowContrastResult = result;
        _hasLastLowContrastCacheKey = true;

        return result;
    }

    private static void AddLowContrastCacheResult(LowContrastCacheKey key, bool result)
    {
        if (_lowContrastCache.Count >= LOW_CONTRAST_CACHE_MAX_ENTRIES)
        {
            while (_lowContrastCache.Count >= LOW_CONTRAST_CACHE_MAX_ENTRIES && _lowContrastCacheOrder.Count > 0)
                _lowContrastCache.Remove(_lowContrastCacheOrder.Dequeue());

            if (_lowContrastCache.Count >= LOW_CONTRAST_CACHE_MAX_ENTRIES)
            {
                _lowContrastCache.Clear();
                _lowContrastCacheOrder.Clear();
            }
        }

        _lowContrastCache[key] = result;
        _lowContrastCacheOrder.Enqueue(key);
    }

    private static LowContrastSample GetItemContrastSample(
        ReadOnlySpan<uint> pixels,
        int pixelWidth,
        int pixelHeight,
        Rectangle sourceRectangle,
        ushort hue,
        bool partialHue,
        bool spectralHue,
        double backgroundLuminance
    )
    {
        if (pixels.IsEmpty || pixelWidth <= 0 || pixelHeight <= 0 || pixels.Length < pixelWidth * pixelHeight)
            return new LowContrastSample(1d, 0d);

        sourceRectangle = ClampRectangleToPixelBounds(sourceRectangle, pixelWidth, pixelHeight);

        if (sourceRectangle.Width <= 0 || sourceRectangle.Height <= 0)
            return new LowContrastSample(1d, 0d);

        Span<uint> hueColors = stackalloc uint[32];
        bool hasHueColors = TryFillHueColors(hue, hueColors);
        double total = 0d;
        int count = 0;
        int lowContrastPixels = 0;

        for (int y = sourceRectangle.Top; y < sourceRectangle.Bottom; y++)
        {
            int rowOffset = y * pixelWidth;

            for (int x = sourceRectangle.Left; x < sourceRectangle.Right; x++)
            {
                uint pixel = pixels[rowOffset + x];

                if ((pixel & 0xFF000000u) == 0)
                    continue;

                uint rendered = ApplyItemHue(pixel, hue, partialHue, spectralHue, hasHueColors, hueColors);
                double luminance = GetRelativeLuminance(rendered);

                total += luminance;
                if (GetContrastRatio(luminance, backgroundLuminance) < LOW_CONTRAST_PIXEL_RATIO_THRESHOLD)
                    lowContrastPixels++;

                count++;
            }
        }

        return count == 0
            ? new LowContrastSample(1d, 0d)
            : new LowContrastSample(total / count, lowContrastPixels / (double)count);
    }

    private static Rectangle ClampRectangleToPixelBounds(Rectangle rectangle, int width, int height)
    {
        int left = Math.Clamp(rectangle.Left, 0, width);
        int top = Math.Clamp(rectangle.Top, 0, height);
        int right = Math.Clamp(rectangle.Right, left, width);
        int bottom = Math.Clamp(rectangle.Bottom, top, height);

        return new Rectangle(left, top, right - left, bottom - top);
    }

    private static bool TryFillHueColors(ushort hue, Span<uint> hueColors)
    {
        if (hue == 0 || hue >= Client.Game.UO.FileManager.Hues.HuesCount)
            return false;

        hue -= 1;
        int group = hue >> 3;
        int entry = hue % 8;

        for (int i = 0; i < hueColors.Length; i++)
            hueColors[i] = HuesHelper.Color16To32(Client.Game.UO.FileManager.Hues.HuesRange[group].Entries[entry].ColorTable[i]);

        return true;
    }

    private static uint ApplyItemHue(uint color, ushort hue, bool partialHue, bool spectralHue, bool hasHueColors, ReadOnlySpan<uint> hueColors)
    {
        if (spectralHue)
            return color & 0xFF000000u;

        if (hue == 0)
            return color;

        if (!hasHueColors)
        {
            ushort color16 = HuesHelper.Color32To16(color);
            return partialHue
                ? HuesHelper.Color16To32(color16)
                : HuesHelper.Color16To32(hue);
        }

        int red5 = (int)(color & 0xFF) >> 3;
        int green5 = (int)((color >> 8) & 0xFF) >> 3;
        int blue5 = (int)((color >> 16) & 0xFF) >> 3;

        if (partialHue && (red5 != green5 || red5 != blue5))
            return HuesHelper.Color16To32(HuesHelper.Color32To16(color));

        return hueColors[blue5];
    }

    private static double GetBackgroundLuminance(ushort backgroundHue, byte backgroundAlpha)
    {
        Color background = Color.Black;

        if (backgroundHue != 0)
            background = ColorFromUOPacked(Client.Game.UO.FileManager.Hues.GetHueColorRgba8888(0, backgroundHue));

        double alpha = Math.Clamp(backgroundAlpha / 100d, 0d, 1d);
        return GetRelativeLuminance(background.R, background.G, background.B) * alpha;
    }

    private static Color ColorFromUOPacked(uint packed) =>
        new(
            (byte)(packed & 0xFF),
            (byte)((packed >> 8) & 0xFF),
            (byte)((packed >> 16) & 0xFF),
            (byte)((packed >> 24) & 0xFF)
        );

    private static double GetRelativeLuminance(uint packed) =>
        GetRelativeLuminance(
            (byte)(packed & 0xFF),
            (byte)((packed >> 8) & 0xFF),
            (byte)((packed >> 16) & 0xFF)
        );

    private static double GetRelativeLuminance(byte r, byte g, byte b) =>
        0.2126d * _srgbToLinearLut[r] + 0.7152d * _srgbToLinearLut[g] + 0.0722d * _srgbToLinearLut[b];

    private static double GetContrastRatio(double luminanceA, double luminanceB)
    {
        double lighter = Math.Max(luminanceA, luminanceB);
        double darker = Math.Min(luminanceA, luminanceB);

        return (lighter + 0.05d) / (darker + 0.05d);
    }

    private static double ToLinear(byte value)
    {
        double channel = value / 255d;
        return channel <= 0.03928d
            ? channel / 12.92d
            : Math.Pow((channel + 0.055d) / 1.055d, 2.4d);
    }

    private static double[] CreateSrgbToLinearLut()
    {
        double[] lut = new double[256];

        for (int i = 0; i < lut.Length; i++)
            lut[i] = ToLinear((byte)i);

        return lut;
    }

    private void DrawLowContrastSpriteBorder(UltimaBatcher2D batcher, Rectangle destination, Rectangle source)
    {
        if (source.Width <= 0 || source.Height <= 0 || destination.Width <= 0 || destination.Height <= 0)
            return;

        Rectangle outlineSource = InflateRectangleWithinBounds(
            source,
            LOW_CONTRAST_OUTLINE_PADDING,
            0,
            LOW_CONTRAST_OUTLINE_PADDING,
            LOW_CONTRAST_OUTLINE_PADDING,
            _bounds
        );
        Vector2 scale = new(
            destination.Width / (float)source.Width,
            destination.Height / (float)source.Height
        );
        Vector2 position = new(
            destination.X - ((source.X - outlineSource.X) * scale.X),
            destination.Y - ((source.Y - outlineSource.Y) * scale.Y)
        );

        batcher.DrawOutlined(
            _texture,
            position,
            outlineSource,
            _lowContrastOutlineHue,
            _whiteOutlineColor,
            0f,
            Vector2.Zero,
            scale,
            SpriteEffects.None,
            0f
        );
    }

    private void DrawLowContrastSpotlight(UltimaBatcher2D batcher, Rectangle destination, Rectangle cellBounds)
    {
        DrawSpotlightRectangle(batcher, InflateRectangle(destination, 4), cellBounds, _lowContrastSpotlightOuterHue);
        DrawSpotlightRectangle(batcher, InflateRectangle(destination, 2), cellBounds, _lowContrastSpotlightInnerHue);
    }

    private void DrawSpotlightRectangle(UltimaBatcher2D batcher, Rectangle rectangle, Rectangle cellBounds, Vector3 hueVector)
    {
        int left = Math.Max(rectangle.Left, cellBounds.Left);
        int top = Math.Max(rectangle.Top, cellBounds.Top);
        int right = Math.Min(rectangle.Right, cellBounds.Right);
        int bottom = Math.Min(rectangle.Bottom, cellBounds.Bottom);

        if (right <= left || bottom <= top)
            return;

        batcher.Draw(_whiteTexture, new Rectangle(left, top, right - left, bottom - top), hueVector);
    }

    private static Rectangle InflateRectangle(Rectangle rectangle, int padding) =>
        new(
            rectangle.X - padding,
            rectangle.Y - padding,
            rectangle.Width + (padding * 2),
            rectangle.Height + (padding * 2)
        );

    private static Rectangle InflateRectangleWithinBounds(Rectangle rectangle, int leftPadding, int topPadding, int rightPadding, int bottomPadding, Rectangle bounds)
    {
        leftPadding = Math.Max(0, Math.Min(leftPadding, rectangle.Left - bounds.Left));
        topPadding = Math.Max(0, Math.Min(topPadding, rectangle.Top - bounds.Top));
        rightPadding = Math.Max(0, Math.Min(rightPadding, bounds.Right - rectangle.Right));
        bottomPadding = Math.Max(0, Math.Min(bottomPadding, bounds.Bottom - rectangle.Bottom));

        return new Rectangle(
            rectangle.X - leftPadding,
            rectangle.Y - topPadding,
            rectangle.Width + leftPadding + rightPadding,
            rectangle.Height + topPadding + bottomPadding
        );
    }

    /// <summary>
    /// When compare mode is active and the cursor is over an equippable item, builds the
    /// side-by-side comparison tooltip against the currently equipped item(s).
    /// </summary>
    private void TryShowComparisonTooltip()
    {
        if (!_hasItem || !_gridContainer.IsCompare || _item.ItemData.Layer <= 0 || !MouseIsOver)
            return;

        // Bail if a comparison tooltip is already showing.
        if ((_toolTipThis != null && !_toolTipThis.IsDisposed) ||
            (_toolTipitem1 != null && !_toolTipitem1.IsDisposed) ||
            (_toolTipitem2 != null && !_toolTipitem2.IsDisposed))
            return;

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

        if (compItem == null || itemLayer == Layer.Backpack)
            return;

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

    public override bool Draw(UltimaBatcher2D batcher, int x, int y)
    {
        if (!_shouldDraw || IsDisposed) return false;

        TryShowComparisonTooltip();

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

        int itemCellWidth = _isListLayout ? GridContainer.LIST_ICON_SIZE : Width;
        int itemCellHeight = _isListLayout ? GridContainer.LIST_ICON_SIZE : Height;
        Rectangle itemCellBounds = new(x, y, itemCellWidth, itemCellHeight);

        if (_item.MatchesHighlightData)
        {
            Texture2D borderTexture = SolidColorTextureCache.GetTexture(_item.HighlightColor);
            var borderHueVec = new Vector3(1, 0, 1);

            DrawHighlightBorder(batcher, itemCellBounds, borderTexture, borderHueVec);
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

        Point originalSize = new(itemCellWidth, itemCellHeight);
        Point point = new();
        float scale = (_profile.GridContainersScale / 100f);
        bool scaleItems = !_isListLayout && _profile.GridContainerScaleItems;

        // Calculate centered X dimension
        (originalSize.X, point.X) = CalculateCenteredDimension(_rect.Width, itemCellWidth, scaleItems, scale);

        // Calculate centered Y dimension
        (originalSize.Y, point.Y) = CalculateCenteredDimension(_rect.Height, itemCellHeight, scaleItems, scale);

        Rectangle destination = new(
            itemCellBounds.X + point.X,
            itemCellBounds.Y + point.Y,
            originalSize.X,
            originalSize.Y
        );

        Rectangle source = new(
            _bounds.X + _rect.X,
            _bounds.Y + _rect.Y,
            _rect.Width,
            _rect.Height
        );

        if (_profile.GridHighlightLowContrastItems && IsLowContrastItem())
        {
            if ((LowContrastHighlightStyle)_profile.GridHighlightLowContrastItemsStyle == LowContrastHighlightStyle.Spotlight)
                DrawLowContrastSpotlight(batcher, destination, itemCellBounds);
            else
                DrawLowContrastSpriteBorder(batcher, destination, source);
        }

        batcher.Draw(_texture, destination, source, hueVector);

        _count?.Draw(batcher, x + _count.X, y + _count.Y);

        return true;
    }

    public override void PreDraw()
    {
        base.PreDraw();

        bool comboActive = _gridContainer.IsMultiMove && Mouse.LButtonPressed
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