using System;
using System.Collections.Generic;
using System.Xml;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls.ResizableComponents;
using ClassicUO.Game.UI.MyraWindows;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Myra.Events;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;
using SDL3;

namespace ClassicUO.Game.UI.Controls;

/// <summary>
/// While we inherit many interface methods from Gump controls, many of them do not apply to Myra controls.
/// It's a long process to be able to support two different types of windows/gumps in the same UIManager
/// </summary>
public class MyraControl : IGui
{
    #region Internal Controls

    protected readonly Desktop _desktop = new();
    protected readonly ResizableWindow _rootWindow;

    #endregion

    public MyraControl(string title)
    {
        _rootWindow = new ResizableWindow(
            new ResizableWindowProps
            {
                Resize = { Placements = ResizeEdges.Bottom | ResizeEdges.Left | ResizeEdges.Right }
            }
        ) { Title = title };

        _rootWindow.Closed += OnRootWindowOnClosed;
        _rootWindow.TitlePanel.Background = new SolidBrush(new Color(0, 0, 0, 75));
        _rootWindow.TitlePanel.Border = new SolidBrush(new Color(0, 0, 0, MyraStyle.STANDARD_BORDER_ALPHA));
        _rootWindow.TitlePanel.BorderThickness = new Thickness(1);

        MyraStyle.ApplyButtonDangerStyle(_rootWindow.CloseButton);

        _desktop.Root = _rootWindow;

        _desktop.WidgetGotKeyboardFocus += DesktopOnWidgetGotKeyboardFocus;
        _rootWindow.TouchDown += DesktopOnTouchDown;
        _rootWindow.TouchUp += DesktopOnTouchUp;
        _rootWindow.LocationChanged += DesktopWindowOnLocationChanged;
        _rootWindow.SizeChanged += RootWindowOnSizeChanged;
        _rootWindow.ArrangeUpdated += RootWindowOnSizeChanged;

        _rootWindow.CloseKey = null;

        UIManager.TopMostChanged += UIManagerOnTopMostChanged;
    }

    #region Event Handlers
    private void UIManagerOnTopMostChanged(object sender, EventArgs e) => _desktop.Opacity = UIManager.TopMostControl == this ? 1f : 0.8f;

    private void OnRootWindowOnClosed(object s, EventArgs a)
    {
        if (IsDisposed)
            return;

        _disposeRequested = true;
    }

    private void RootWindowOnSizeChanged(object sender = null, EventArgs e = null) => UpdateBoundsToContents(false);

    /// <summary>
    /// Update this <see cref="Bounds"/> to fit to the content of the window.
    /// </summary>
    private void UpdateBoundsToContents(bool arrange = true)
    {
        if (arrange)
            _rootWindow.UpdateArrange();

        Point mSize = _rootWindow.Measure(new Point(2000, 2000));

        if (_rootWindow.Left < 0)
            _rootWindow.Left = 0;

        if (_rootWindow.Top < 0)
            _rootWindow.Top = 0;

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

    private void DesktopOnTouchUp(object sender, EventArgs e) =>
        OnMouseUp(Mouse.Position.X, Mouse.Position.Y, MouseButtonType.Left);

    private void DesktopOnTouchDown(object sender, EventArgs e) =>
        OnMouseDown(Mouse.Position.X, Mouse.Position.Y, MouseButtonType.Left);

    private void DesktopOnWidgetGotKeyboardFocus(object sender, GenericEventArgs<Widget> e)
    {
        if (e.Data.AcceptsKeyboardFocus && e.Data is Myra.Graphics2D.UI.TextBox)
            SetKeyboardFocus();
        else
            UIManager.KeyboardFocusControl = null;
    }

    #endregion

    #region Fields
    protected Rectangle _bounds = new();
    protected bool _disposeRequested = false;
    #endregion

    #region Properties
    public bool IsFocused { get; set; }
    public bool CanBeSaved { get; set; } = false;
    public bool AcceptKeyboardInput { get; set; } = true;
    public bool AcceptMouseInput { get; set; } = true;
    public bool HandlesKeyboardFocus { get; set; }
    public bool IsDisposed { get; private set; } = false;
    public bool IsVisible { get; set; } = true;
    public bool IsEnabled { get; set; } = true;
    public IGui RootParent { get; } = null;
    public IGui Parent { get; set; }
    public ref Rectangle Bounds => ref _bounds;
    public object Tooltip { get; set; }
    public bool HasTooltip => Tooltip != null;
    public bool CanMove { get; set; } = true;
    public bool IsEditable { get; set; }
    public uint ServerSerial { get; set; }
    public uint LocalSerial { get; set; }

    /// <summary> Setting this does not affect position of this window, use SetPosition() instead </summary>
    public ref int X => ref Bounds.X;

    /// <summary> Setting this does not affect position of this window, use SetPosition() instead </summary>
    public ref int Y => ref Bounds.Y;
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

    /// <summary>
    /// Do not set this manually, should only be set by UIManager
    /// </summary>
    public bool IsTopMost { get; set; }

    #endregion

    protected void SetRootContent(Widget widget)
    {
        _rootWindow.Content = widget;
        UpdateBoundsToContents();
    }

    public void SetKeyboardFocus()
    {
        if (AcceptKeyboardInput && !HasKeyboardFocus)
            UIManager.KeyboardFocusControl = this;
    }

    public MyraControl CenterInViewPort()
    {
        Camera camera = Client.Game.Scene.Camera;
        X = camera.Bounds.X + (camera.Bounds.Width - Width) / 2;
        Y = camera.Bounds.Y + (camera.Bounds.Height - Height) / 2;

        if (X < 0)
            X = 0;

        SetPosition(X, Y);

        return this;
    }

    public void SetPosition(int x, int y)
    {
        _rootWindow.Left = x;
        _rootWindow.Top = y;
        UpdateBoundsToContents();
    }

    public virtual void Update()
    {
        if (IsDisposed)
            return;

        if (_disposeRequested)
            ExecuteDispose();
    }

    public virtual void PreDraw()
    {
        if (IsDisposed)
            return;

        if (_disposeRequested)
            ExecuteDispose();
    }

    public virtual bool Draw(UltimaBatcher2D batcher, int x, int y)
    {
        if (IsDisposed || !IsVisible || _desktop == null || _desktop.Root == null)
            return false;

        batcher.FlushBatch(); //Required to draw myra on top of already drawn gumps

        if (IsTopMost)
        {
            _desktop.Render();
        }
        else
        {
            _desktop.UpdateLayout();
            _desktop.RenderVisual();
        }

        DrawDebug(batcher, x, y);
        return true;
    }

    private void DrawDebug(UltimaBatcher2D batcher, int x, int y)
    {
        if (CUOEnviroment.Debug)
        {
            Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);

            batcher.DrawRectangle(SolidColorTextureCache.GetTexture(Color.Green), x, y, Width, Height, hueVector);
        }
    }

    public virtual void Save(XmlTextWriter xml)
    {
        xml.WriteAttributeString("type", GetType().FullName);
        xml.WriteAttributeString("x", X.ToString());
        xml.WriteAttributeString("y", Y.ToString());
    }

    public virtual void Load(XmlElement xml)
    {
        if (xml.GetAttribute("x") is { } x && xml.GetAttribute("y") is { } y)
            if (int.TryParse(xml.GetAttribute("x"), out int xInt) && xInt > 0)
                if (int.TryParse(xml.GetAttribute("y"), out int yInt))
                    SetPosition(xInt, yInt);
    }

    public virtual void Dispose()
    {
        if (IsDisposed)
            return;
        _disposeRequested = true;
    }

    private void ExecuteDispose()
    {
        if (IsDisposed)
            return;

        _disposeRequested = false;
        IsDisposed = true;

        if (_desktop is null)
            return;

        _desktop.WidgetGotKeyboardFocus -= DesktopOnWidgetGotKeyboardFocus;
        UIManager.TopMostChanged -= UIManagerOnTopMostChanged;

        if (_rootWindow is not null)
        {
            _rootWindow.Closed -= OnRootWindowOnClosed;
            _rootWindow.TouchDown -= DesktopOnTouchDown;
            _rootWindow.TouchUp -= DesktopOnTouchUp;
            _rootWindow.LocationChanged -= DesktopWindowOnLocationChanged;
            _rootWindow.SizeChanged -= RootWindowOnSizeChanged;
            _rootWindow.ArrangeUpdated -= RootWindowOnSizeChanged;
        }

        _desktop.Widgets.Clear();
        _desktop.Dispose();
    }

    public virtual void OnFocusEnter() => IsFocused = true;

    public virtual void OnFocusLost()
    {
        IsFocused = false;
        _desktop.FocusedKeyboardWidget = null;
    }

    #region Invokations
    /// <summary>This is not in use here. Use _rootWindow events instead.</summary>
    public void InvokeKeyUp(SDL.SDL_Keycode key, SDL.SDL_Keymod mod) { }

    /// <summary>This is not in use here. Use _rootWindow events instead.</summary>
    public void InvokeKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod) { }

    /// <summary>This is not in use here. Use _rootWindow events instead.</summary>
    public void InvokeTextInput(string c) { }

    /// <summary>This is not in use here. Use _rootWindow events instead.</summary>
    public void InvokeControllerButtonUp(SDL.SDL_GamepadButton button) { }

    /// <summary>This is not in use here. Use _rootWindow events instead.</summary>
    public void InvokeControllerButtonDown(SDL.SDL_GamepadButton button) { }

    /// <summary>This is not in use here. Use _rootWindow events instead.</summary>
    public void InvokeMouseDown(Point position, MouseButtonType button) { }

    /// <summary>This is not in use here. Use _rootWindow events instead.</summary>
    public void InvokeMouseUp(Point position, MouseButtonType button) { }

    public void InvokeMouseOver(Point position) => OnMouseOver(position.X, position.Y);

    /// <summary>This is not in use here. Use _rootWindow events instead.</summary>
    public void InvokeMouseEnter(Point position) { }

    /// <summary>This is not in use here. Use _rootWindow events instead.</summary>
    public void InvokeMouseExit(Point position) { }

    public bool InvokeMouseDoubleClick(Point position, MouseButtonType button) =>
        OnMouseDoubleClick(position.X, position.Y, button);

    /// <summary>This is not in use here. Use _rootWindow events instead.</summary>
    public void InvokeMouseWheel(MouseEventType delta) { }

    /// <summary>This is not in use here. Use _rootWindow events instead.</summary>
    public void InvokeMouseCloseGumpWithRClick() { }

    /// <summary>This is not in use here. Use _rootWindow events instead.</summary>
    public void InvokeDragBegin(Point position) { }

    /// <summary>This is not in use here. Use _rootWindow events instead.</summary>
    public void InvokeDragEnd(Point position) { }
    #endregion

    public virtual void HitTest(Point position, ref IGui res)
    {
        if (!IsVisible || !IsEnabled || IsDisposed || !AcceptMouseInput)
            return;

        if (Bounds.Contains(position.X, position.Y) || Contains(position.X, position.Y))
        {
            res = this;
            OnHitTestSuccess(position.X, position.Y, ref res);
        }
    }

    public void HitTest(int x, int y, ref IGui res) => HitTest(new Point(x, y), ref res);

    /// <summary>This is not in use here. Use _rootWindow events instead.</summary>
    public void ChangePage(int pageIndex) { }

    public void CloseWithRightClick() => Dispose();

    public bool Contains(int x, int y)
    {
        if (_desktop == null)
            return false;

        if (Bounds.Contains(x + ParentX, y + ParentY))
            return true;

        if (_desktop.ContextMenu is { Visible: true } contextMenu)
        {
            var realBounds = new Rectangle(
                contextMenu.Left,
                contextMenu.Top,
                contextMenu.Bounds.Width,
                contextMenu.Bounds.Height
            );
            if (realBounds.Contains(x + ParentX, y + ParentY))
                return true;
        }

        return false;
    }

    #region OnEventOccured
    public virtual void OnHitTestSuccess(int x, int y, ref IGui res) { }

    public virtual void OnMouseUp(int x, int y, MouseButtonType button) { }

    public virtual void OnMouseDown(int x, int y, MouseButtonType button) { }

    /// <summary>This is not in use here. Use _rootWindow events instead.</summary>
    public void OnMouseWheel(MouseEventType delta) { }

    public virtual void OnMouseOver(int x, int y) { }

    public virtual bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
    {
        if (Contains(x + ParentX, y + ParentY))
            return true;

        return false;
    }

    /// <summary>This is not in use here. Use _rootWindow events instead.</summary>
    public void OnKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod) { }

    /// <summary>This is not in use here. Use _rootWindow events instead.</summary>
    public void OnKeyUp(SDL.SDL_Keycode key, SDL.SDL_Keymod mod) { }

    /// <summary>This is not in use here. Use _rootWindow events instead.</summary>
    public void OnButtonClick(int buttonID) { }

    /// <summary>This is not in use here. Use _rootWindow events instead.</summary>
    public void OnKeyboardReturn(int textID, string text) { }

    /// <summary>This is not in use here. Use _rootWindow events instead.</summary>
    public void OnPageChanged() { }
    #endregion

    /// <summary>This is not in use here. Use _rootWindow events instead.</summary>
    public IEnumerable<T> FindControls<T>()
        where T : IGui => Array.Empty<T>();

    /// <summary>This is not in use here. Use _rootWindow events instead.</summary>
    public void KeyboardTabToNextFocus(IGui c) { }

    /// <summary>This is not in use here. Use _rootWindow events instead.</summary>
    public void UpdateOffset(int x, int y) { }

    /// <summary>This is not in use here. Use _rootWindow events instead.</summary>
    public T Add<T>(T c, int page = 0)
        where T : IGui => c;

    /// <summary>This is not in use here. Use _rootWindow events instead.</summary>
    public void Remove(IGui c) => Children.Remove(c);

    public void SetTooltip(string text, int maxWidth = 0) //TODO: Remove maxWidth param
    {
        ClearTooltip();

        if (!string.IsNullOrEmpty(text))
        {
            Tooltip = text;
            _rootWindow?.Tooltip = text;
        }
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

    public virtual void ForceSizeUpdate(bool onlyIfLarger = true)
    {
        if (_desktop == null)
            return;

        UpdateBoundsToContents();
    }

    /// <summary>This is not in use here. Use _rootWindow events instead.</summary>
    public IGui ApplyScale(double scale, bool scalePosition = true, bool scaleSize = true, bool force = false) => this;

    /// <summary>This is not in use here. Use _rootWindow events instead.</summary>
    public IGui SetInternalScale(double scale) => this;

    /// <summary>This is not in use here. Use _rootWindow events instead.</summary>
    public IGui GetFirstControlAcceptKeyboardInput() => null;

    /// <summary>This is not in use here. Use _rootWindow events instead.</summary>
    public void Insert(int index, IGui c, int page = 0) { }

    public void BringOnTop() => UIManager.MakeTopMostGump(this);

    public void ShowContextMenu(params (string Label, Action Action)[] items)
    {
        var menu = new VerticalMenu();
        foreach (var (label, action) in items)
        {
            var item = new MenuItem { Text = label };
            if (action != null)
            {
                Action captured = action;
                item.Selected += (_, _) => captured();
            }
            menu.Items.Add(item);
        }
        _desktop.ShowContextMenu(menu, Mouse.Position);
    }

    /// <summary>Returns a formatted status string for context menu labels.</summary>
    /// <param name="status">Whether the option is enabled (true) or disabled (false).</param>
    /// <param name="label">The label text to display after the status indicator.</param>
    /// <returns>A colored status tag followed by the label text.</returns>
    public string ContextMenuLabelToggle(bool status, string label) =>
        $"{(status ? "[/c[green]Enabled/cd]" : "[/c[red]Disabled/cd]")} {label}";
}
