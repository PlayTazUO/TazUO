using System;
using System.Collections.Generic;
using ClassicUO.Game.Managers;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Myra.Events;
using Myra.Graphics2D.UI;
using SDL3;

namespace ClassicUO.Game.UI.Controls;

/// <summary>
/// While we inherit many interface methods from Gump controls, many of them do not apply to Myra controls.
/// It's a long process to be able to support two different types of windows/gumps in the same UIManager
/// </summary>
public class MyraControl : IGui
{
    protected Desktop _desktop = new();
    protected Window _rootWindow;

    private bool isFocused
    {
        get;
        set
        {
            field = value;
            if (value) BringOnTop();
        }
    }

    public MyraControl(string title)
    {
        _rootWindow = new Window { Title = title };
        _rootWindow.Closed += OnRootWindowOnClosed;
        _desktop.Root = _rootWindow;

        _desktop.WidgetGotKeyboardFocus += DesktopOnWidgetGotKeyboardFocus;
        _rootWindow.TouchDown += DesktopOnTouchDown;
        _rootWindow.TouchUp += DesktopOnTouchUp;
        _rootWindow.LocationChanged += DesktopWindowOnLocationChanged;
        _rootWindow.SizeChanged += RootWindowOnSizeChanged;
    }

    private void OnRootWindowOnClosed(object s, EventArgs a)
    {
        if (IsDisposed) return;

        _disposeRequested = true;
    }

    private void RootWindowOnSizeChanged(object sender, EventArgs e)
    {
        _rootWindow.UpdateArrange();
        Point mSize = _rootWindow.Measure(new Point(2000, 2000));

        Bounds.Width = mSize.X;
        Bounds.Height = mSize.Y;
        Bounds.X = _rootWindow.Left;
        Bounds.Y = _rootWindow.Top;
    }

    private void DesktopWindowOnLocationChanged(object sender, EventArgs e)
    {
        Bounds.X = _rootWindow.Left;
        Bounds.Y = _rootWindow.Top;
    }

    private void DesktopOnTouchUp(object sender, EventArgs e) => OnMouseUp(Mouse.Position.X, Mouse.Position.Y, MouseButtonType.Left);

    private void DesktopOnTouchDown(object sender, EventArgs e)
    {
        if (Bounds.Contains(Mouse.Position))
            BringOnTop();
        OnMouseDown(Mouse.Position.X, Mouse.Position.Y, MouseButtonType.Left);
    }

    private void DesktopOnWidgetGotKeyboardFocus(object sender, GenericEventArgs<Widget> e) => SetKeyboardFocus();

    public bool AcceptKeyboardInput { get; set; } = true;
    public bool AcceptMouseInput { get; set; } = true;
    public bool HandlesKeyboardFocus { get; set; }
    public bool IsFocused { get; set; }
    public bool IsDisposed { get; private set; } = false;
    public bool IsVisible { get; set; } = true;
    public bool IsEnabled { get; set; } = true;
    public IGui RootParent { get; } = null;
    public IGui Parent { get; set; }

    protected Rectangle _bounds = new();
    public ref Rectangle Bounds => ref _bounds;

    public object Tooltip { get; set; }
    public bool HasTooltip  => Tooltip != null;
    public bool CanMove { get; set; } = true;
    public bool IsEditable { get; set; }
    public uint ServerSerial { get; set; }
    public uint LocalSerial { get; set; }

    public ref int X => ref Bounds.Y;

    public ref int Y => ref Bounds.X;
    public int ScreenCoordinateX => X;
    public int ScreenCoordinateY => Y;

    public ref int Height => ref Bounds.Height;

    public ref int Width => ref Bounds.Width;

    public int ParentX { get; } = 0;
    public int ParentY { get; } = 0;
    public int Page { get; set; }
    public int ActivePage { get; set; }
    public List<IGui> Children { get; } = new();
    public ClickPriority Priority { get; set; }
    public bool CanCloseWithRightClick { get; } = true;
    public bool IsModal { get; } = false;
    public float Alpha { get; set; }
    public bool WantUpdateSize { get; set; }
    public UILayer LayerOrder { get; set; } = UILayer.Default;
    public bool IsFromServer { get; set; }
    public Point Location { get; set; } = Point.Zero;
    public bool HasKeyboardFocus => UIManager.KeyboardFocusControl == this;
    public bool ModalClickOutsideAreaClosesThisControl { get; } = true;

    private bool _disposeRequested = false;

    protected void SetRootContent(Widget widget)
    {
        _rootWindow.Content = widget;
        RootWindowOnSizeChanged(null, null);
    }

    public virtual void Update()
    {
        if (IsDisposed) return;

        if(_disposeRequested) Dispose();
    }

    public virtual void PreDraw() { }

    public virtual bool Draw(UltimaBatcher2D batcher, int x, int y)
    {
        if (IsDisposed || !IsVisible || _desktop == null || _desktop.Root == null) return false;

        batcher.FlushBatch(); //Required to draw myra on top of already drawn gumps
        _desktop.Render();
        return true;
    }

    public void Dispose()
    {
        if(IsDisposed) return;

        _disposeRequested = false;
        IsDisposed = true;

        if (_desktop is null) return;

        _desktop.WidgetGotKeyboardFocus -= DesktopOnWidgetGotKeyboardFocus;

        if(_rootWindow is not null)
        {
            _rootWindow.Closed -= OnRootWindowOnClosed;
            _rootWindow.TouchDown -= DesktopOnTouchDown;
            _rootWindow.TouchUp -= DesktopOnTouchUp;
            _rootWindow.LocationChanged -= DesktopWindowOnLocationChanged;
            _rootWindow.SizeChanged -= RootWindowOnSizeChanged;
        }

        _desktop.Widgets.Clear();
        _desktop.Dispose();
    }

    public void OnFocusEnter() { }

    public void OnFocusLost() { }

    public void SetKeyboardFocus()
    {
        if (AcceptKeyboardInput && !HasKeyboardFocus)
        {
            UIManager.KeyboardFocusControl = this;
        }
    }

    public void InvokeKeyUp(SDL.SDL_Keycode key, SDL.SDL_Keymod mod) { }

    public void InvokeKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod) { }

    public void InvokeTextInput(string c) { }

    public void InvokeControllerButtonUp(SDL.SDL_GamepadButton button) { }

    public void InvokeControllerButtonDown(SDL.SDL_GamepadButton button) { }

    public void InvokeMouseDown(Point position, MouseButtonType button) { }

    public void InvokeMouseUp(Point position, MouseButtonType button) { }

    public void InvokeMouseOver(Point position) { }

    public void InvokeMouseEnter(Point position) { }

    public void InvokeMouseExit(Point position) { }

    public bool InvokeMouseDoubleClick(Point position, MouseButtonType button) => true;

    public void InvokeMouseWheel(MouseEventType delta) { }

    public void InvokeMouseCloseGumpWithRClick() { }

    public void InvokeDragBegin(Point position) { }

    public void InvokeDragEnd(Point position) { }

    public void HitTest(Point position, ref IGui res)
    {
        if (!IsVisible || !IsEnabled || IsDisposed) return;

        if (Bounds.Contains(position.X, position.Y))
            if (AcceptMouseInput)
            {
                res = this;
                OnHitTestSuccess(position.X, position.Y, ref res);
            }
    }

    public void HitTest(int x, int y, ref IGui res) => HitTest(new Point(x, y), ref res);

    public void OnHitTestSuccess(int x, int y, ref IGui res) { }

    public void OnMouseUp(int x, int y, MouseButtonType button) { }

    public void OnMouseDown(int x, int y, MouseButtonType button) { }

    public void OnMouseWheel(MouseEventType delta) { }

    public void OnMouseOver(int x, int y) { }

    public bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
    {
        if (Contains(x + ParentX, y + ParentY))
            return true;

        return false;
    }

    public void OnKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod) { }

    public void OnKeyUp(SDL.SDL_Keycode key, SDL.SDL_Keymod mod) { }

    public void OnButtonClick(int buttonID) { }

    public void OnKeyboardReturn(int textID, string text) { }

    public void ChangePage(int pageIndex) { }

    public void CloseWithRightClick() => Dispose();

    public bool Contains(int x, int y)
    {
        if(_desktop == null) return false;

        return _desktop.BoundsFetcher.Invoke().Contains(x + ParentX, y + ParentY);
    }

    public IEnumerable<T> FindControls<T>() where T : IGui => Array.Empty<T>();

    public void KeyboardTabToNextFocus(IGui c) { }

    public void UpdateOffset(int x, int y) { }

    public T Add<T>(T c, int page = 0) where T : IGui => c;

    public void Remove(IGui c) => Children.Remove(c);

    public void SetTooltip(string text, int maxWidth = 0) //TODO: Remove maxWidth param
    {
        ClearTooltip();

        if (!string.IsNullOrEmpty(text)) Tooltip = text;
    }

    public void SetTooltip(uint entity)
    {
        ClearTooltip();
        Tooltip = entity;
    }

    public void SetTooltip(IGui c)
    {
        ClearTooltip();
        Tooltip = c;
    }

    public void ClearTooltip() => Tooltip = null;

    public virtual void OnPageChanged() { }

    public virtual void ForceSizeUpdate(bool onlyIfLarger = true)
    {
        if (_desktop == null) return;

        Bounds = _desktop.BoundsFetcher.Invoke();
    }

    public IGui ApplyScale(double scale, bool scalePosition = true, bool scaleSize = true, bool force = false) => this;

    public IGui SetInternalScale(double scale) => this;

    public IGui GetFirstControlAcceptKeyboardInput() => null;

    public void Insert(int index, IGui c, int page = 0) { }

    public void BringOnTop() => UIManager.MakeTopMostGump(this);
}
