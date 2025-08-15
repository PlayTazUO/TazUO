using System;
using System.Collections.Concurrent;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps.GridHighLight;
using ClassicUO.Input;
using ClassicUO.Renderer;
using ClassicUO.Utility;

namespace ClassicUO.Game.UI.Gumps
{
    internal class MultiItemMoveGump : NineSliceGump
    {
        private const int WIDTH = 230;
        private const int HEIGHT = 150;

        public static int PreferredWidth => UIManager.GetGump<MultiItemMoveGump>()?.Width ?? WIDTH;
        public static int PreferredHeight => UIManager.GetGump<MultiItemMoveGump>()?.Height ?? HEIGHT;

        public static void ShowNextTo(Control anchor, int padding = -2)
        {
            int w = PreferredWidth;
            int screenH = Client.Game.Window.ClientBounds.Height;

            int x = anchor.X >= w + padding
                ? anchor.X - (w + padding)           // left of anchor
                : anchor.X + anchor.Width + padding; // right of anchor

            int y = Math.Max(0, Math.Min(anchor.Y, screenH - PreferredHeight));

            var g = UIManager.GetGump<MultiItemMoveGump>();
            if (g == null || g.IsDisposed)
            {
                AddMultiItemMoveGumpToUI(x, y);
                g = UIManager.GetGump<MultiItemMoveGump>();
            }
            else
            {
                g.X = x;
                g.Y = y;
            }
            g?.SetInScreen();
        }

        // ===== Selection + queue =====
        public static readonly ConcurrentQueue<Item> MoveItems = new ConcurrentQueue<Item>();
        private static readonly ConcurrentDictionary<uint, byte> _selected = new ConcurrentDictionary<uint, byte>();
        private static int SelectedCount => _selected.Count;

        // ===== Processing state =====
        public static int ObjDelay = 1000;
        private static bool processing = false;
        private static ProcessType processType = ProcessType.None;
        private static long nextMove;
        private static uint tradeId, containerId;
        private static int groundX, groundY, groundZ;

        // ===== UI =====
        private Label _header;
        private InputField _delayInput;

        public static bool IsSelected(uint serial) => _selected.ContainsKey(serial);

        public static void HideIfNoSelection()
        {
            if (SelectedCount == 0)
            {
                processing = false;
                ResetDestination();
                UIManager.GetGump<MultiItemMoveGump>()?.Dispose();
            }
        }

        public MultiItemMoveGump(int x, int y)
            // resizable = true, with sensible minimums
            : base(x, y, WIDTH, HEIGHT, ModernUIConstants.ModernUIPanel,
                   ModernUIConstants.ModernUIPanel_BoderSize, true, WIDTH, HEIGHT)
        {
            CanMove = true;
            AcceptMouseInput = true;
            CanCloseWithRightClick = false;

            ObjDelay = ProfileManager.CurrentProfile.MoveMultiObjectDelay;

            Build();
        }

        protected override void OnResize(int oldWidth, int oldHeight, int newWidth, int newHeight)
        {
            base.OnResize(oldWidth, oldHeight, newWidth, newHeight);
            Build();
        }

        private void Build()
        {
            Clear();

            // Content area inside the modern border
            int cx = BorderSize;
            int cy = BorderSize;
            int cw = Width - (BorderSize * 2);
            int ch = Height - (BorderSize * 2);
            int contentBottom = cy + ch;

            // Header
            Add(_header = new Label(TextForHeader(), true, 0xFFFF, cw, align: TEXT_ALIGN_TYPE.TS_CENTER)
            {
                X = cx,
                Y = cy
            });

            // "Object delay" + numeric input (right-aligned)
            int delayRowY = cy + _header.Height + 5;

            Add(new Label("Object delay:", true, 0xFFFF, 150)
            {
                X = cx,
                Y = delayRowY
            });

            Add(_delayInput = new InputField(0x0BB8, 0xFF, 0xFFFF, true, 56, 20)
            {
                X = cx + (cw - 56), // right edge of content
                Y = delayRowY,
                NumbersOnly = true
            });
            _delayInput.SetText(ObjDelay.ToString());
            _delayInput.TextChanged += (s, e) =>
            {
                if (int.TryParse(_delayInput.Text, out int newDelay))
                {
                    newDelay = Math.Max(0, Math.Min(5000, newDelay));
                    ObjDelay = newDelay;
                    ProfileManager.CurrentProfile.MoveMultiObjectDelay = newDelay;
                    if (_delayInput.Text != newDelay.ToString())
                        _delayInput.SetText(newDelay.ToString());
                }
            };

            // --- Buttons: position from the content bottom so spacing stays correct when resizing ---
            const int GAP = 6;                  // small gap between left/right buttons
            int halfW = (cw - GAP) / 2;

            int rowY1 = contentBottom - 72;     // Move to backpack (full width)
            int rowY2 = contentBottom - 44;     // Set favorite / To favorite
            int rowY3 = contentBottom - 20;     // Cancel / Move to

            NiceButton b;

            // Move to backpack (full width)
            Add(b = new NiceButton(cx, rowY1, cw, 20, ButtonAction.Activate, "Move to backpack", align: TEXT_ALIGN_TYPE.TS_CENTER));
            b.SetTooltip("Move selected items to your backpack.");
            b.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    var bp = World.Player.FindItemByLayer(Layer.Backpack);
                    if (bp != null) ProcessItemMoves(bp);
                }
            };

            // Set favorite (left)
            Add(b = new NiceButton(cx, rowY2, halfW, 20, ButtonAction.Activate, "Set favorite bag", align: TEXT_ALIGN_TYPE.TS_CENTER));
            b.SetTooltip("Set your preferred destination container for future item moves.");
            b.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    GameActions.Print("Target a container to set as your favorite.");
                    TargetManager.SetTargeting(CursorTarget.SetFavoriteMoveBag, CursorType.Target, TargetType.Neutral);
                }
            };

            // To favorite (right)
            Add(b = new NiceButton(cx + halfW + GAP, rowY2, halfW, 20, ButtonAction.Activate, "To favorite", align: TEXT_ALIGN_TYPE.TS_CENTER));
            b.SetTooltip("Move selected items to your favorite container.");
            b.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    uint fav = ProfileManager.CurrentProfile.SetFavoriteMoveBagSerial;
                    if (fav == 0)
                    {
                        GameActions.Print("No favorite container set. Please target one.");
                        TargetManager.SetTargeting(CursorTarget.SetFavoriteMoveBag, CursorType.Target, TargetType.Neutral);
                        return;
                    }

                    Item cont = World.Items.Get(fav);
                    if (cont != null) ProcessItemMoves(cont);
                    else GameActions.Print("Favorite container is not available.");
                }
            };

            // Cancel (left)
            Add(b = new NiceButton(cx, rowY3, halfW, 20, ButtonAction.Activate, "Cancel", align: TEXT_ALIGN_TYPE.TS_CENTER));
            b.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    ClearAll();
                    Dispose();
                }
            };

            // Move to (right)
            Add(b = new NiceButton(cx + halfW + GAP, rowY3, halfW, 20, ButtonAction.Activate, "Move to", align: TEXT_ALIGN_TYPE.TS_CENTER));
            b.SetTooltip("Select a container or a ground tile to move these items to.");
            b.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    GameActions.Print("Where should we move these items?");
                    TargetManager.SetTargeting(CursorTarget.MoveItemContainer, CursorType.Target, TargetType.Neutral);
                }
            };
        }

        // ===== Selection API used by GridContainer =====

        public static bool TrySelect(Item item)
        {
            if (item == null) return false;
            if (!_selected.TryAdd(item.Serial, 1)) return false; // already selected
            MoveItems.Enqueue(item);
            return true;
        }

        /// <summary>
        /// Toggle selection state of an item. Returns true if now selected; false if deselected.
        /// </summary>
        public static bool ToggleItem(Item item)
        {
            if (item == null) return false;

            if (_selected.TryRemove(item.Serial, out _))
            {
                // deselected
                return false;
            }

            _selected[item.Serial] = 1;
            MoveItems.Enqueue(item);
            return true;
        }

        public static void AddMultiItemMoveGumpToUI(int x, int y)
        {
            if (SelectedCount > 0)
            {
                var g = UIManager.GetGump<MultiItemMoveGump>();
                if (g == null || g.IsDisposed)
                    UIManager.Add(new MultiItemMoveGump(x, y));
            }
        }

        // ===== Target entry points =====

        public static void OnContainerTarget(uint serial)
        {
            if (SerialHelper.IsItem(serial))
            {
                Item moveToContainer = World.Items.Get(serial);
                if (moveToContainer == null || !moveToContainer.ItemData.IsContainer)
                {
                    GameActions.Print("That does not appear to be a container...");
                    return;
                }
                GameActions.Print("Moving items to the selected container..");
                ProcessItemMoves(moveToContainer);
            }
        }

        public static void OnContainerTarget(int x, int y, int z)
        {
            processItemMoves(x, y, z);
        }

        public static void OnTradeWindowTarget(uint tradeID)
        {
            processItemMoves(tradeID);
        }

        // ===== Processing impl =====

        private static void ProcessItemMoves(Item container)
        {
            if (container != null)
            {
                containerId = container.Serial;
                processType = ProcessType.Container;
                processing = true;
            }
        }

        private static void processItemMoves(int x, int y, int z)
        {
            processType = ProcessType.Ground;
            groundX = x;
            groundY = y;
            groundZ = z;
            processing = true;
        }

        private static void processItemMoves(uint tradeID)
        {
            tradeId = tradeID;
            processType = ProcessType.TradeWindow;
            processing = true;
        }

        public override void Update()
        {
            base.Update();

            // live header
            if (_header != null)
                _header.Text = TextForHeader();

            if (!processing)
                return;

            if (Time.Ticks < nextMove)
                return;

            if (Client.Game.GameCursor.ItemHold.Enabled)
                return;

            if (MoveItems.TryDequeue(out Item moveItem))
            {
                if (_selected.ContainsKey(moveItem.Serial))
                {
                    switch (processType)
                    {
                        case ProcessType.Ground:
                            var itemData = TileDataLoader.Instance.StaticData[moveItem.Graphic];
                            MoveItemQueue.Instance.Enqueue(
                                moveItem.Serial,
                                0,
                                moveItem.Amount,
                                groundX,
                                groundY,
                                groundZ + (sbyte)(itemData.Height == 0xFF ? 0 : itemData.Height));
                            break;

                        case ProcessType.Container:
                            MoveItemQueue.Instance.Enqueue(moveItem.Serial, containerId, moveItem.Amount);
                            break;

                        case ProcessType.TradeWindow:
                            MoveItemQueue.Instance.Enqueue(
                                moveItem.Serial,
                                tradeId,
                                moveItem.Amount,
                                RandomHelper.GetValue(0, 20),
                                RandomHelper.GetValue(0, 20),
                                0);
                            break;

                        case ProcessType.None:
                        default:
                            processing = false;
                            ResetDestination();
                            break;
                    }

                    _selected.TryRemove(moveItem.Serial, out _);
                    nextMove = Time.Ticks + ObjDelay;
                }
                // else: was deselected after enqueue -> skip
            }

            if (MoveItems.IsEmpty && SelectedCount == 0)
            {
                processing = false;
                ResetDestination();
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            // auto-close if nothing to do
            if (SelectedCount < 1 && MoveItems.IsEmpty)
            {
                processing = false;
                ResetDestination();
                Dispose();
                return false;
            }

            return base.Draw(batcher, x, y);
        }

        private static string TextForHeader()
        {
            var count = SelectedCount;
            return processing ? $"Moving {count} items." : $"Selected {count} items.";
        }

        private static void ClearAll()
        {
            _selected.Clear();
            while (MoveItems.TryDequeue(out _)) { }
            processing = false;
            ResetDestination();
        }

        private static void ResetDestination()
        {
            processType = ProcessType.None;
            containerId = 0;
            tradeId = 0;
            groundX = groundY = groundZ = 0;
        }

        protected enum ProcessType
        {
            None = 0,
            Container,
            Ground,
            TradeWindow
        }
    }
}
