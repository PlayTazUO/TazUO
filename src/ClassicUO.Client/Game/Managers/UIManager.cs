// SPDX-License-Identifier: BSD-2-Clause


using ClassicUO.Configuration;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using ClassicUO.Game.UI;

namespace ClassicUO.Game.Managers
{
    internal static class UIManager
    {
        private static readonly Dictionary<Type, List<IGui>> _gumpTypeList = new();
        private static readonly ConcurrentDictionary<uint, Point> _gumpPositionCache = new();
        private static readonly IGui[] _mouseDownControls = new IGui[0xFF];


        //private static readonly Dictionary<uint, TargetLineGump> _targetLineGumps = new Dictionary<uint, TargetLineGump>();
        private static Point _dragOrigin;
        private static bool _isDraggingControl;
        private static IGui _keyboardFocusControl, _lastFocus;
        private static bool _needSort;

        // Ctrl-modified drag state for axis-locking and speed reduction
        private static bool _ctrlDragAxisDetermined;
        private static bool _ctrlDragLockHorizontal;
        private static float _ctrlDragRemainderX;
        private static float _ctrlDragRemainderY;
        private static bool _wasCtrlHeldLastFrame;

        // Ctrl-modified drag constants
        private const int AXIS_LOCK_THRESHOLD_PIXELS = 10;
        private const float CTRL_DRAG_SPEED_MULTIPLIER = 0.5f;

        private static void ResetCtrlDragState()
        {
            _ctrlDragAxisDetermined = false;
            _ctrlDragLockHorizontal = false;
            _ctrlDragRemainderX = 0f;
            _ctrlDragRemainderY = 0f;
            _wasCtrlHeldLastFrame = false;
        }


        public static World World { get; set; }

        public static float ContainerScale { get; set; } = 1f;

        public static AnchorManager AnchorManager { get; } = new();

        public static LinkedList<IGui> Gumps { get; } = new();

        public static IGui MouseOverControl { get; private set; }
        public static IGui TopMostControl { get; set; }

        public static bool IsModalOpen { get; private set; }

        public static bool InGame;

        public static bool IsMouseOverWorld
        {
            get
            {
                Point mouse = Mouse.Position;
                Profile profile = ProfileManager.CurrentProfile;

                return profile != null &&
                    Client.Game.UO.GameCursor.AllowDrawSDLCursor &&
                    DraggingControl == null &&
                    MouseOverControl == null &&
                    !IsModalOpen &&
                    Client.Game.Scene.Camera.Bounds.Contains(mouse);
            }
        }

        public static Control DraggingControl { get; private set; }

        public static SystemChatControl SystemChat { get; set; }

        public static PopupMenuGump PopupMenu { get; private set; }

        public static IGui KeyboardFocusControl
        {
            get => _keyboardFocusControl;
            set
            {
                if (_keyboardFocusControl != value)
                {
                    _keyboardFocusControl?.OnFocusLost();
                    _keyboardFocusControl = value;

                    if (value != null && value.AcceptKeyboardInput)
                    {
                        if (!value.IsFocused)
                        {
                            value.OnFocusEnter();
                        }
                    }
                }
            }
        }

        public static bool IsDragging => _isDraggingControl && DraggingControl != null;

        public static ContextMenuShowMenu ContextMenu { get; private set; }

        public static bool UpdateTimerEnabled
        {
            get => updateTimerEnabled; set
            {
                updateTimerEnabled = value;
                if (value)
                {
                    UpdateTimerTotalTime = new Dictionary<Type, double>();
                    UpdateTimerCount = new Dictionary<Type, int>();
                    updateTimer = Stopwatch.StartNew();
                }
            }
        }

        public static Dictionary<Type, double> UpdateTimerTotalTime;
        public static Dictionary<Type, int> UpdateTimerCount;
        private static bool updateTimerEnabled;
        private static Stopwatch updateTimer;

        public static void ShowGamePopup(PopupMenuGump popup)
        {
            PopupMenu?.Dispose();
            PopupMenu = popup;

            if (popup == null || popup.IsDisposed)
            {
                return;
            }

            Add(PopupMenu);
        }


        public static bool IsModalControlOpen()
        {
            for (LinkedListNode<IGui> last = Gumps.Last; last != null; last = last.Previous)
            {
                if (last.Value.IsModal)
                {
                    return true;
                }
            }

            return false;
        }


        public static void OnMouseDragging()
        {
            HandleMouseInput();

            if (_mouseDownControls[(int)MouseButtonType.Left] != null)
            {
                if (ProfileManager.CurrentProfile == null || !ProfileManager.CurrentProfile.HoldAltToMoveGumps || Keyboard.Alt)
                {
                    AttemptDragControl(_mouseDownControls[(int)MouseButtonType.Left]);
                }
            }

            if (_isDraggingControl)
            {
                DoDragControl();
            }
        }

        public static void OnMouseButtonDown(MouseButtonType button)
        {
            HandleMouseInput();

            if (MouseOverControl != null)
            {
                if (MouseOverControl.IsEnabled && MouseOverControl.IsVisible)
                {
                    if (_lastFocus != MouseOverControl)
                    {
                        _lastFocus?.OnFocusLost();
                        MouseOverControl.OnFocusEnter();
                        _lastFocus = MouseOverControl;
                    }
                }

                MakeTopMostGump(MouseOverControl);
                MouseOverControl.InvokeMouseDown(Mouse.Position, button);

                if (MouseOverControl.AcceptKeyboardInput)
                {
                    _keyboardFocusControl = MouseOverControl;
                }

                _mouseDownControls[(int)button] = MouseOverControl;
            }
            else
            {
                foreach (IGui s in Gumps)
                {
                    if (s.IsModal && s.ModalClickOutsideAreaClosesThisControl)
                    {
                        s.Dispose();
                        Mouse.CancelDoubleClick = true;
                    }
                }
            }

            if (PopupMenu != null && !PopupMenu.Bounds.Contains(Mouse.Position.X, Mouse.Position.Y))
            {
                ShowGamePopup(null);
            }
        }

        public static void OnMouseButtonUp(MouseButtonType button)
        {
            EndDragControl(Mouse.Position);
            HandleMouseInput();

            int index = (int)button;

            if (MouseOverControl != null)
            {
                if (_mouseDownControls[index] != null && MouseOverControl == _mouseDownControls[index] || Client.Game.UO.GameCursor.ItemHold.Enabled)
                {
                    MouseOverControl.InvokeMouseUp(Mouse.Position, button);
                }
                else if (_mouseDownControls[index] != null && MouseOverControl != _mouseDownControls[index])
                {
                    if (!_mouseDownControls[index].IsDisposed)
                    {
                        _mouseDownControls[index].InvokeMouseUp(Mouse.Position, button);
                    }
                }
            }
            else if (_mouseDownControls[index] != null && !_mouseDownControls[index].IsDisposed)
            {
                _mouseDownControls[index].InvokeMouseUp(Mouse.Position, button);
            }

            if (button == MouseButtonType.Right)
            {
                IGui mouseDownControl = _mouseDownControls[index];
                // only attempt to close the gump if the mouse is still on the gump when right click mouse up occurs
                if (mouseDownControl != null && MouseOverControl == mouseDownControl)
                {
                    mouseDownControl.InvokeMouseCloseGumpWithRClick();
                }
            }

            _mouseDownControls[index] = null;
        }

        public static bool OnMouseDoubleClick(MouseButtonType button)
        {
            HandleMouseInput();

            if (MouseOverControl != null)
            {
                if (MouseOverControl.InvokeMouseDoubleClick(Mouse.Position, button))
                {
                    if (button == MouseButtonType.Left)
                    {
                        Client.Game.UO.World.DelayedObjectClickManager.Clear();
                    }

                    return true;
                }
            }

            return false;
        }

        public static void OnMouseWheel(bool isup)
        {
            if (MouseOverControl != null && MouseOverControl.AcceptMouseInput)
            {
                MouseOverControl.InvokeMouseWheel(isup ? MouseEventType.WheelScrollUp : MouseEventType.WheelScrollDown);
            }
        }

        public static IGui LastControlMouseDown(MouseButtonType button) => _mouseDownControls[(int)button];

        public static void SavePosition(uint serverSerial, Point point) => _gumpPositionCache[serverSerial] = point;

        public static bool RemovePosition(uint serverSerial) => _gumpPositionCache.Remove(serverSerial, out _);

        public static bool GetGumpCachePosition(uint id, out Point pos) => _gumpPositionCache.TryGetValue(id, out pos);

        public static void ShowContextMenu(ContextMenuShowMenu menu)
        {
            ContextMenu?.Dispose();

            ContextMenu = menu;

            if (ContextMenu == null || menu.IsDisposed)
            {
                return;
            }

            Add(ContextMenu);
        }

        public static T GetGump<T>(uint? serial = null) where T : Gump
        {
            // if (serial.HasValue)
            // {
            //     for (LinkedListNode<IGui> last = Gumps.Last; last != null; last = last.Previous)
            //     {
            //         Control c = last.Value;
            //
            //         if (!c.IsDisposed && c.LocalSerial == serial.Value && c is T t)
            //         {
            //             return t;
            //         }
            //     }
            // }
            // else
            // {
            //     for (LinkedListNode<IGui> first = Gumps.First; first != null; first = first.Next)
            //     {
            //         Control c = first.Value;
            //
            //         if (!c.IsDisposed && c is T t)
            //         {
            //             return t;
            //         }
            //     }
            // }

            if (!_gumpTypeList.TryGetValue(typeof(T), out List<IGui> list))
                return null;

            list.RemoveAll(i => i.IsDisposed);

            if (list.Count <= 0) return null;

            if(!serial.HasValue)
                return list[0] as T;

            foreach(IGui gump in list)
                if (gump.LocalSerial == serial.Value)
                    return gump as T;

            return null;
        }

        public static Gump GetGump(uint serial)
        {
            for (LinkedListNode<IGui> last = Gumps.Last; last != null; last = last.Previous)
            {
                IGui c = last.Value;

                if (!c.IsDisposed && c.LocalSerial == serial && c is Gump gump)
                {
                    return gump;
                }
            }

            return null;
        }

        public static Gump GetGumpServer(uint serial)
        {
            for (LinkedListNode<IGui> last = Gumps.Last; last != null; last = last.Previous)
            {
                IGui c = last.Value;

                if (!c.IsDisposed && c.ServerSerial == serial && c is Gump g)
                {
                    return g;
                }
            }

            return null;
        }

        public static TradingGump GetTradingGump(uint serial)
        {
            for (LinkedListNode<IGui> g = Gumps.Last; g != null; g = g.Previous)
            {
                if (g.Value != null && !g.Value.IsDisposed && g.Value is TradingGump trading && (trading.ID1 == serial || trading.ID2 == serial || trading.LocalSerial == serial))
                {
                    return trading;
                }
            }

            return null;
        }

        public static void Update()
        {
            SortControlsByInfo();

            LinkedListNode<IGui> first = Gumps.First;

            while (first != null)
            {
                LinkedListNode<IGui> next = first.Next;

                IGui g = first.Value;
                if (updateTimerEnabled)
                {
                    updateTimer.Restart();
                    g.Update();
                    updateTimer.Stop();

                    if (!UpdateTimerTotalTime.ContainsKey(g.GetType()))
                    {
                        UpdateTimerTotalTime[g.GetType()] = 0;
                        UpdateTimerCount[g.GetType()] = 0;
                    }

                    UpdateTimerTotalTime[g.GetType()] += updateTimer.Elapsed.TotalMilliseconds;
                    UpdateTimerCount[g.GetType()]++;
                }
                else
                {
                    g.Update();
                }
                if (g.IsDisposed)
                {
                    Gumps.Remove(first);
                    UnregisterGump(g);
                }

                first = next;
            }

            HandleKeyboardInput();
            HandleMouseInput();
        }

        public static void PreDraw()
        {
            SortControlsByInfo();

            LinkedListNode<IGui> first = Gumps.First;

            while (first != null)
            {
                LinkedListNode<IGui> next = first.Next;

                IGui g = first.Value;

                g.PreDraw();

                if (g.IsDisposed)
                {
                    Gumps.Remove(first);
                    UnregisterGump(g);
                }

                first = next;
            }
        }

        public static void Draw(UltimaBatcher2D batcher)
        {
            SortControlsByInfo();
            batcher.Begin();

            for (LinkedListNode<IGui> last = Gumps.Last; last != null; last = last.Previous)
            {
                IGui g = last.Value;
                g.Draw(batcher, g.X, g.Y);
            }

            batcher.End();
        }

        public static void Add(IGui gump, bool front = true)
        {
            if (!gump.IsDisposed)
            {
                if (front)
                {
                    Gumps.AddFirst(gump);
                }
                else
                {
                    Gumps.AddLast(gump);
                }

                _needSort = Gumps.Count > 1;

                RegisterGump(gump);
            }
        }

        public static void Clear()
        {
            foreach (IGui s in Gumps)
            {
                s.Dispose();
            }

            _gumpTypeList.Clear();
        }

        /// <summary>
        /// Register gump to it's correct list(s) via Type
        /// </summary>
        /// <param name="item"></param>
        private static void RegisterGump(IGui item)
        {
            Type t = item.GetType();

            while (t != null)
            {
                if (t == typeof(Control)) break; //break early at control ( XX <- Gump <- Control -< Object )

                if (!_gumpTypeList.TryGetValue(t, out List<IGui> list))
                {
                    list = new List<IGui>();
                    _gumpTypeList[t] = list;
                }
                list.Add(item);

                t = t.BaseType;
            }
        }

        /// <summary>
        /// Remove a gump from it's correct Type list(s)
        /// </summary>
        /// <param name="item"></param>
        private static void UnregisterGump(IGui item)
        {
            Type t = item.GetType();

            while (t != null)
            {
                if (t == typeof(Control)) break;

                if (_gumpTypeList.TryGetValue(t, out List<IGui> list))
                    list.Remove(item);

                t = t.BaseType;
            }
        }

        /// <summary>
        /// Iterate through a snapshot of all live instances of the given type.
        /// Disposed instances are pruned automatically before this iteration.
        /// </summary>
        /// <returns>True if any instances existed</returns>
        public static bool ForEach<T>(Action<T> action, uint? serial = null) where T : IGui
        {
            IGui[] snapshot;
            int count;

            if (!_gumpTypeList.TryGetValue(typeof(T), out List<IGui> list))
                return false;

            list.RemoveAll(i => i.IsDisposed);
            count = list.Count;
            if (count == 0) return false;

            snapshot = ArrayPool<IGui>.Shared.Rent(count);
            list.CopyTo(snapshot, 0);

            int c = 0;

            try
            {
                for (int i = 0; i < count; i++)
                {
                    if(!serial.HasValue || serial.Value == snapshot[i].LocalSerial)
                    {
                        action((T)snapshot[i]);
                        c++;
                    }
                }
            }
            finally
            {
                Array.Clear(snapshot, 0, count);
                ArrayPool<IGui>.Shared.Return(snapshot);
            }

            return c != 0;
        }

        private static void HandleKeyboardInput()
        {
            if (_keyboardFocusControl != null && _keyboardFocusControl.IsDisposed)
            {
                _keyboardFocusControl = null;
            }

            if (_keyboardFocusControl == null)
            {
                if (SystemChat is { IsDisposed: false })
                {
                    _keyboardFocusControl = SystemChat.TextBoxControl;
                    _keyboardFocusControl.OnFocusEnter();
                }
                else
                {
                    for (LinkedListNode<IGui> first = Gumps.First; first != null; first = first.Next)
                    {
                        IGui c = first.Value;

                        if (!c.IsDisposed && c.IsVisible && c.IsEnabled)
                        {
                            _keyboardFocusControl = c.GetFirstControlAcceptKeyboardInput();

                            if (_keyboardFocusControl != null)
                            {
                                _keyboardFocusControl.OnFocusEnter();

                                break;
                            }
                        }
                    }
                }
            }
        }

        private static void HandleMouseInput()
        {
            IGui gump = GetMouseOverControl(Mouse.Position);

            if (MouseOverControl != null && gump != MouseOverControl)
            {
                MouseOverControl.InvokeMouseExit(Mouse.Position);

                if (MouseOverControl.RootParent != null)
                {
                    if (gump == null || gump.RootParent != MouseOverControl.RootParent)
                    {
                        MouseOverControl.RootParent.InvokeMouseExit(Mouse.Position);
                    }
                }
            }

            if (gump != null)
            {
                if (gump != MouseOverControl)
                {
                    gump.InvokeMouseEnter(Mouse.Position);

                    if (gump.RootParent != null)
                    {
                        if (MouseOverControl == null || gump.RootParent != MouseOverControl.RootParent)
                        {
                            gump.RootParent.InvokeMouseEnter(Mouse.Position);
                        }
                    }
                }

                gump.InvokeMouseOver(Mouse.Position);
            }

            MouseOverControl = gump;

            //for (int i = 0; i < (int) MouseButtonType.Size; i++)
            //{
            //    if (_mouseDownControls[i] != null && _mouseDownControls[i] != gump)
            //    {
            //        _mouseDownControls[i].InvokeMouseOver(Mouse.Position);
            //    }
            //}
        }

        private static IGui GetMouseOverControl(Point position)
        {
            if (_isDraggingControl)
            {
                return DraggingControl;
            }

            IGui control = null;

            IsModalOpen = IsModalControlOpen();

            for (LinkedListNode<IGui> first = Gumps.First; first != null; first = first.Next)
            {
                IGui c = first.Value;

                if (IsModalOpen && !c.IsModal || !c.IsVisible || !c.IsEnabled)
                {
                    continue;
                }

                c.HitTest(position, ref control);

                if (control != null)
                {
                    return control;
                }
            }

            return null;
        }

        public static void MakeTopMostGump(IGui gump)
        {
            if (gump != null && gump?.RootParent != null)
            {
                gump = gump.RootParent;
            }

            if (gump == null) return;

            for (LinkedListNode<IGui> start = Gumps.First; start != null; start = start.Next)
            {
                if (start.Value == gump)
                {
                    if (gump.LayerOrder == UILayer.Under)
                    {
                        if (start != Gumps.Last)
                        {
                            Gumps.Remove(gump);
                            Gumps.AddBefore(Gumps.Last, start);
                        }
                    }
                    else
                    {
                        Gumps.Remove(gump);
                        Gumps.AddFirst(start);
                    }

                    break;
                }
            }

            TopMostControl = gump;

            _needSort = Gumps.Count > 1;
        }

        private static void SortControlsByInfo()
        {
            if (_needSort)
            {
                for (LinkedListNode<IGui> el = Gumps.First; el != null; el = el.Next)
                {
                    IGui c = el.Value;

                    if (c.LayerOrder == UILayer.Default)
                    {
                        continue;
                    }

                    if (c.LayerOrder == UILayer.Under)
                    {
                        for (LinkedListNode<IGui> first = Gumps.First; first != null; first = first.Next)
                        {
                            if (first.Value == c)
                            {
                                if (c != Gumps.Last.Value)
                                {
                                    Gumps.Remove(first);
                                    Gumps.AddBefore(Gumps.Last, first);
                                }
                            }
                        }
                    }
                    else if (c.LayerOrder == UILayer.Over)
                    {
                        for (LinkedListNode<IGui> first = Gumps.First; first != null; first = first.Next)
                        {
                            if (first.Value == c)
                            {
                                Gumps.Remove(first);
                                Gumps.AddFirst(c);
                            }
                        }
                    }
                }

                _needSort = false;
            }
        }

        public static void AttemptDragControl(IGui control, bool attemptAlwaysSuccessful = false)
        {
            if ((_isDraggingControl && !attemptAlwaysSuccessful) || Client.Game.UO.GameCursor.ItemHold.Enabled && !Client.Game.UO.GameCursor.ItemHold.IsFixedPosition)
            {
                return;
            }

            if (control is not Control dragTarget) return;


            if (!dragTarget.CanMove)
            {
                return;
            }

            while (dragTarget?.Parent != null)
            {
                dragTarget = dragTarget.Parent as Control;
            }


            if (dragTarget?.CanMove == true)
            {
                Point delta = Mouse.LDragOffset;

                bool doDrag = (ProfileManager.CurrentProfile == null || Math.Abs(delta.X) >= ProfileManager.CurrentProfile.MinGumpMoveDistance || Math.Abs(delta.Y) >= ProfileManager.CurrentProfile.MinGumpMoveDistance) || attemptAlwaysSuccessful;
                if (doDrag && (!_isDraggingControl || attemptAlwaysSuccessful))
                {
                    DraggingControl = dragTarget;
                    _dragOrigin = Mouse.LClickPosition;

                    // Reset Ctrl drag state for new drag operation
                    ResetCtrlDragState();

                    for (int i = 0; i < (int)MouseButtonType.Size; i++)
                    {
                        _mouseDownControls[i] = null;
                    }
                }

                if (doDrag)
                {
                    _isDraggingControl = true;
                    dragTarget.InvokeDragBegin(delta);
                }
            }
        }

        private static void DoDragControl()
        {
            if (DraggingControl == null)
            {
                return;
            }

            Point delta = Mouse.Position - _dragOrigin;

            // Apply Ctrl modifier: axis-locking and speed reduction
            bool ctrlCurrentlyHeld = Keyboard.Ctrl;
            bool ctrlJustPressed = ctrlCurrentlyHeld && !_wasCtrlHeldLastFrame;

            if (ctrlJustPressed)
            {
                // Reset Ctrl drag state when Ctrl is newly pressed mid-drag
                ResetCtrlDragState();
            }

            if (ctrlCurrentlyHeld)
            {
                // Determine axis lock direction on first significant movement
                if (!_ctrlDragAxisDetermined)
                {
                    // Require threshold movement to determine direction
                    if (Math.Abs(delta.X) >= AXIS_LOCK_THRESHOLD_PIXELS ||
                        Math.Abs(delta.Y) >= AXIS_LOCK_THRESHOLD_PIXELS)
                    {
                        _ctrlDragLockHorizontal = Math.Abs(delta.X) >= Math.Abs(delta.Y);
                        _ctrlDragAxisDetermined = true;
                    }
                }

                // Apply axis lock if determined
                if (_ctrlDragAxisDetermined)
                {
                    if (_ctrlDragLockHorizontal)
                    {
                        delta.Y = 0;
                    }
                    else
                    {
                        delta.X = 0;
                    }
                }

                // Apply speed reduction with fractional tracking to prevent cumulative truncation error
                float scaledX = delta.X * CTRL_DRAG_SPEED_MULTIPLIER + _ctrlDragRemainderX;
                float scaledY = delta.Y * CTRL_DRAG_SPEED_MULTIPLIER + _ctrlDragRemainderY;

                delta.X = (int)scaledX;
                delta.Y = (int)scaledY;

                // Track fractional remainders for next frame
                _ctrlDragRemainderX = scaledX - delta.X;
                _ctrlDragRemainderY = scaledY - delta.Y;
            }
            else
            {
                // Clear remainders when not in Ctrl mode
                _ctrlDragRemainderX = 0f;
                _ctrlDragRemainderY = 0f;
            }

            _wasCtrlHeldLastFrame = ctrlCurrentlyHeld;

            DraggingControl.X += delta.X;
            DraggingControl.Y += delta.Y;
            DraggingControl.InvokeMove(delta.X, delta.Y);
            _dragOrigin = Mouse.Position;
        }

        private static void EndDragControl(Point mousePosition)
        {
            if (_isDraggingControl)
            {
                DoDragControl();
            }

            DraggingControl?.InvokeDragEnd(mousePosition);
            DraggingControl = null;
            _isDraggingControl = false;

            // Reset Ctrl drag state when drag ends
            ResetCtrlDragState();
        }
    }
}
