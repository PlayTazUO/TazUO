// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Game.Managers;
using ClassicUO.Input;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SDL3;
using Keyboard = ClassicUO.Input.Keyboard;

namespace ClassicUO.Game.UI.Controls
{
    public abstract class Control : IGui
    {
    internal static int _StepsDone = 1;
    internal static int _StepChanger = 1;

    private bool _acceptKeyboardInput;
    private Rectangle _bounds;
    private Point _offset;
    private float alpha = 1.0f;

    protected Control(IGui parent = null)
    {
        Parent = parent;
        Children = new();
        AllowedToDraw = true;
        AcceptMouseInput = true;
        Page = 0;

        IsDisposed = false;
        IsEnabled = true;
    }

    public virtual ClickPriority Priority { get; set; } = ClickPriority.Default;

    /// <summary>
    /// Do not set this manually, should only be set by UIManager
    /// </summary>
    public bool IsTopMost { get; set; }

    public uint ServerSerial { get; set; }

    public uint LocalSerial { get; set; }

    public bool IsFromServer { get; set; }

    /// <summary>
    /// This is not implemented in all controls, this is for use in custom control's that want to have a scale setting
    /// </summary>
    public double Scale { get; set; } = 1.0f;

    /// <summary>
    /// This is intended for scaling mouse positions and other non-visual uses
    /// </summary>
    public double InternalScale { get; set; } = 1.0f;

    public int Page { get; set; }

    public Point Location
    {
        get => _bounds.Location;
        set
        {
            X = value.X;
            Y = value.Y;
            _bounds.Location = value;
        }
    }

    public ref Rectangle Bounds => ref _bounds;

    public Point Offset => _offset;

    public bool IsDisposed { get; private set; }

    public bool IsVisible { get; set; } = true;

    public bool IsEnabled { get; set; }

    public bool HasKeyboardFocus => UIManager.KeyboardFocusControl == this;

    public bool MouseIsOver => UIManager.MouseOverControl == this;

    public bool CanMove { get; set; }

    public bool CanCloseWithRightClick { get; set; } = true;

    public bool CanCloseWithEsc { get; set; }

    public bool IsEditable { get; set; }

    public bool IsFocused { get; set; }

    public float Alpha
    {
        get => alpha;
        set
        {
            float old = alpha;
            alpha = value;
            AlphaChanged(old, value);
        }
    }

    public List<IGui> Children { get; }

    public object Tag { get; set; }

    public object Tooltip { get; private set; }

    public bool HasTooltip => /*World.ClientFlags.TooltipsEnabled &&*/ Tooltip != null;

    public virtual bool AcceptKeyboardInput
    {
        get => IsEnabled && !IsDisposed && IsVisible && _acceptKeyboardInput;
        set => _acceptKeyboardInput = value;
    }

    public virtual bool AcceptMouseInput
    {
        get => IsEnabled && !IsDisposed && field && IsVisible;
        set;
    }

    public ref int X => ref _bounds.X;

    public ref int Y => ref _bounds.Y;

    public ref int Width => ref _bounds.Width;

    public ref int Height => ref _bounds.Height;

    public int ParentX => Parent != null ? Parent.X + Parent.ParentX : 0;

    public int ParentY => Parent != null ? Parent.Y + Parent.ParentY : 0;

    public int ScreenCoordinateX => ParentX + X;

    public int ScreenCoordinateY => ParentY + Y;

    public ContextMenuControl ContextMenu { get; set; }

    public IGui Parent
    {
        get;
        set
        {
            if (value == null)
            {
                field?.Children.Remove(this);
            }
            else
            {
                field?.Children.Remove(this);
                value.Children.Add(this);
            }

            field = value;
        }
    }

    public IGui RootParent
    {
        get
        {
            if (Parent == null)
            {
                return null;
            }

            IGui p = Parent;

            while (p.Parent != null)
            {
                p = p.Parent;
            }

            return p;
        }
    }

    public UILayer LayerOrder { get; set; } = UILayer.Default;
    public bool IsModal { get; set; }
    public bool ModalClickOutsideAreaClosesThisControl { get; set; }


    public virtual bool HandlesKeyboardFocus
    {
        get
        {
            if (!IsEnabled || IsDisposed || !IsVisible)
            {
                return false;
            }

            if (field)
            {
                return true;
            }

            if (Children == null)
            {
                return false;
            }

            foreach (IGui c in Children)
            {
                if (c.HandlesKeyboardFocus)
                {
                    return true;
                }
            }

            return false;
        }
        set;
    }

    public int ActivePage
    {
        get;
        set
        {
            field = value;

            OnPageChanged();
        }
    }

    public void SetDisposed(bool status)
    {
        if (status)
            Dispose();
        else
            IsDisposed = false;
    }

    public bool WantUpdateSize { get; set; } = true;

    public bool AllowedToDraw { get; set; }

    public void UpdateOffset(int x, int y)
    {
        if (_offset.X != x || _offset.Y != y)
        {
            _offset.X = x;
            _offset.Y = y;

            foreach (IGui c in Children)
            {
                c.UpdateOffset(x, y);
            }
        }
    }

    public virtual bool Draw(UltimaBatcher2D batcher, int x, int y)
    {
        if (IsDisposed)
        {
            return false;
        }

        for (int i = 0; i < Children.Count; i++)
        {
            if (Children.Count <= i)
                break;

            IGui c = Children[i];

            if (c != null && (c.Page == 0 || c.Page == ActivePage))
            {
                if (c.IsVisible)
                {
                    c.Draw(batcher, c.X + x, c.Y + y);
                }
            }
        }

        DrawDebug(batcher, x, y);

        return true;
    }

    /// <summary>
    /// Update is called as often as possible.
    /// </summary>
    public virtual void Update()
    {
        if (IsDisposed || Children.Count == 0)
            return;

        int w = 0, h = 0, count = Children.Count;

        for (int i = 0; i < count; i++)
        {
            if (i >= Children.Count)
                break;

            IGui c = Children[i];

            if (c == null || c.IsDisposed)
                continue;

            c.Update();

            if (WantUpdateSize)
            {
                if ((c.Page == 0 || c.Page == ActivePage) && c.IsVisible)
                {
                    if (w < c.Bounds.Right)
                    {
                        w = c.Bounds.Right;
                    }

                    if (h < c.Bounds.Bottom)
                    {
                        h = c.Bounds.Bottom;
                    }
                }
            }
        }

        if (WantUpdateSize && IsVisible)
        {
            if (w != Width)
            {
                Width = w;
            }

            if (h != Height)
            {
                Height = h;
            }

            WantUpdateSize = false;
        }
    }

    /// <summary>
    /// Intended for any ui changes that only need to occur just before drawing to the screen.
    /// </summary>
    public virtual void PreDraw()
    {
        if (IsDisposed) return;

        if (Children.Count == 0) return;

        int count = Children.Count;

        for (int i = 0; i < count; i++)
        {
            if (i >= Children.Count)
                continue;

            IGui c = Children[i];

            if (c == null || c.IsDisposed)
                continue;

            c.PreDraw();
        }

        CleanUpDisposedChildren();
    }

    /// <summary>
    /// Scale the width and height of this control. Width/Height * Scale
    /// </summary>
    /// <param name="scale"></param>
    /// <returns>This control</returns>
    public virtual IGui ScaleWidthAndHeight(double scale)
    {
        if (scale != 1f)
        {
            Width = (int)(Width * scale);
            Height = (int)(Height * scale);
        }

        return this;
    }

    /// <summary>
    /// Scale the x/y position of this control. x/y * Scale
    /// </summary>
    /// <param name="scale"></param>
    /// <returns>This control</returns>
    public virtual IGui ScaleXAndY(double scale)
    {
        if (scale != 1f)
        {
            X = (int)(X * scale);
            Y = (int)(Y * scale);
        }

        return this;
    }

    /// <summary>
    /// Set the internal scale used for mouse interactions or other non visual scaling
    /// </summary>
    /// <param name="scale"></param>
    /// <returns>This control</returns>
    public virtual IGui SetInternalScale(double scale)
    {
        InternalScale = scale;
        return this;
    }

    /// <summary>
    /// Apply scaling to this control in one call. Scales position, size, and sets internal scale.
    /// Prevents double-scaling by tracking if control has already been scaled.
    /// NOTE: This method is NOT thread-safe. Only call from the UI thread.
    /// </summary>
    /// <param name="scale">The scale factor to apply</param>
    /// <param name="scalePosition">Whether to scale X/Y position (default: true)</param>
    /// <param name="scaleSize">Whether to scale Width/Height (default: true)</param>
    /// <param name="force">Force scaling even if already scaled (default: false)</param>
    /// <returns>This control for method chaining</returns>
    public virtual IGui ApplyScale(double scale, bool scalePosition = true, bool scaleSize = true,
        bool force = false)
    {
        const double epsilon = 0.0001;
        if (!force && Math.Abs(scale - InternalScale) < epsilon)
        {
            return this;
        }

        if (scaleSize)
            ScaleWidthAndHeight(scale);
        if (scalePosition)
            ScaleXAndY(scale);
        // Don't set Scale property - it's used for rendering transforms and would cause double-scaling
        // Only set InternalScale for mouse interactions
        SetInternalScale(scale);

        return this;
    }

    public void ForceSizeUpdate(bool onlyIfLarger = true)
    {
        int h = onlyIfLarger ? Height : 0, w = onlyIfLarger ? Width : 0;
        for (int i = 0; i < Children.Count; i++)
        {
            IGui c = Children[i];
            if ((c.Page == 0 || c.Page == ActivePage) && c.IsVisible && !c.IsDisposed)
            {
                if (w < c.Bounds.Right)
                {
                    w = c.Bounds.Right;
                }

                if (h < c.Bounds.Bottom)
                {
                    h = c.Bounds.Bottom;
                }
            }
        }

        if (w != Width)
        {
            Width = w;
        }

        if (h != Height)
        {
            Height = h;
        }

        WantUpdateSize = false;
    }

    public virtual void OnPageChanged()
    {
        //Update size as pages may vary in size.
        if (ServerSerial != 0)
        {
            WantUpdateSize = true;
        }
    }

    internal void CleanUpDisposedChildren()
    {
        bool childRemoved = false;
        for (int i = Children.Count - 1; i >= 0; i--)
        {
            if (i < Children.Count && Children[i]?.IsDisposed == true)
            {
                Children.RemoveAt(i);
                childRemoved = true;
            }
        }

        if (childRemoved)
            OnChildRemoved();
    }

    private void DrawDebug(UltimaBatcher2D batcher, int x, int y)
    {
        if (IsVisible && CUOEnviroment.Debug)
        {
            Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);

            batcher.DrawRectangle
            (
                SolidColorTextureCache.GetTexture(Color.Green),
                x,
                y,
                Width,
                Height,
                hueVector
            );
        }
    }

    public void BringOnTop() => UIManager.MakeTopMostGump(this);

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

    public void SetKeyboardFocus()
    {
        if (AcceptKeyboardInput && !HasKeyboardFocus)
        {
            UIManager.KeyboardFocusControl = this;
        }
    }

    internal event EventHandler<MouseEventArgs>
        MouseDown, MouseUp, MouseOver, MouseEnter, MouseExit, DragBegin, DragEnd;

    internal event EventHandler<MouseWheelEventArgs> MouseWheel;

    internal event EventHandler<MouseDoubleClickEventArgs> MouseDoubleClick;

    internal event EventHandler FocusEnter, FocusLost, Disposed;

    internal event EventHandler<KeyboardEventArgs> KeyDown, KeyUp;

    internal event EventHandler<SDL.SDL_GamepadButton> ControllerButtonUp, ControllerButtonDown;


    public void HitTest(int x, int y, ref IGui res)
    {
        if (!IsVisible || !IsEnabled || IsDisposed)
        {
            return;
        }

        int parentX = ParentX;
        int parentY = ParentY;

        if (Bounds.Contains(x - parentX - _offset.X, y - parentY - _offset.Y))
        {
            if (Contains(x - X - parentX, y - Y - parentY))
            {
                if (AcceptMouseInput)
                    if (res == null || res.Priority >= Priority)
                    {
                        res = this;
                        OnHitTestSuccess(x, y, ref res);
                    }

                for (int i = 0; i < Children.Count; ++i)
                {
                    IGui c = Children[i];

                    if (c == null) continue;

                    if (c.Page == 0 || c.Page == ActivePage) c.HitTest(x, y, ref res);
                }
            }
        }
    }

    public void HitTest(Point position, ref IGui res) => HitTest(position.X, position.Y, ref res);

    public virtual void OnHitTestSuccess(int x, int y, ref IGui res)
    {
    }


    /// <summary>
    /// Invoked when alpha is changed on a control
    /// </summary>
    /// <param name="oldValue"></param>
    /// <param name="newValue"></param>
    public virtual void AlphaChanged(float oldValue, float newValue) { }

    public IGui GetFirstControlAcceptKeyboardInput()
    {
        if (_acceptKeyboardInput)
        {
            return this;
        }

        if (Children == null || Children.Count == 0)
        {
            return null;
        }

        foreach (IGui c in Children.ToArray())
        {
            IGui a = c.GetFirstControlAcceptKeyboardInput();

            if (a != null)
            {
                return a;
            }
        }

        return null;
    }

    public virtual T Add<T>(T c, int page = 0) where T : IGui
    {
        c.Page = page;
        c.Parent = this;
        OnChildAdded();

        return c;
    }

    public void Insert(int index, IGui c, int page = 0)
    {
        c.Page = 0;

        c.Parent?.Children.Remove(c);

        c.Parent = this;

        Children.Insert(index, c);

        OnChildAdded();
    }

    public virtual void Remove(IGui c)
    {
        if (c == null)
        {
            return;
        }

        c.Parent = null;
        Children.Remove(c);
        OnChildRemoved();
    }

    public virtual void Clear()
    {
        foreach (IGui c in Children)
        {
            c.Dispose();
        }
    }

    public T[] GetControls<T>() where T : IGui => Children.OfType<T>().Where(s => !s.IsDisposed).ToArray();

    public IEnumerable<T> FindControls<T>() where T : IGui => Children.OfType<T>().Where(s => !s.IsDisposed);


    public void InvokeMouseDown(Point position, MouseButtonType button)
    {
        int x = position.X - X - ParentX;
        int y = position.Y - Y - ParentY;
        OnMouseDown(x, y, button);
        MouseDown.Raise(new MouseEventArgs(x, y, button, ButtonState.Pressed), this);
    }

    public void InvokeMouseUp(Point position, MouseButtonType button)
    {
        int x = position.X - X - ParentX;
        int y = position.Y - Y - ParentY;
        OnMouseUp(x, y, button);
        MouseUp.Raise(new MouseEventArgs(x, y, button), this);
    }

    public void InvokeMouseCloseGumpWithRClick()
    {
        if (CanCloseWithRightClick)
        {
            CloseWithRightClick();
        }
    }

    public void InvokeMouseOver(Point position)
    {
        int x = position.X - X - ParentX;
        int y = position.Y - Y - ParentY;
        OnMouseOver(x, y);
        MouseOver.Raise(new MouseEventArgs(x, y), this);
    }

    public void InvokeMouseEnter(Point position)
    {
        int x = position.X - X - ParentX;
        int y = position.Y - Y - ParentY;
        OnMouseEnter(x, y);
        MouseEnter.Raise(new MouseEventArgs(x, y), this);
    }

    public void InvokeMouseExit(Point position)
    {
        int x = position.X - X - ParentX;
        int y = position.Y - Y - ParentY;
        OnMouseExit(x, y);
        MouseExit.Raise(new MouseEventArgs(x, y), this);
    }

    public bool InvokeMouseDoubleClick(Point position, MouseButtonType button)
    {
        int x = position.X - X - ParentX;
        int y = position.Y - Y - ParentY;
        bool result = OnMouseDoubleClick(x, y, button);

        var arg = new MouseDoubleClickEventArgs(x, y, button);
        MouseDoubleClick.Raise(arg, this);
        result |= arg.Result;

        return result;
    }

    public void InvokeTextInput(string c) => OnTextInput(c);

    public void InvokeKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
    {
        OnKeyDown(key, mod);
        var arg = new KeyboardEventArgs(key, mod, KeyboardEventType.Down);
        KeyDown?.Raise(arg);
    }

    public void InvokeKeyUp(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
    {
        OnKeyUp(key, mod);
        var arg = new KeyboardEventArgs(key, mod, KeyboardEventType.Up);
        KeyUp?.Raise(arg);
    }

    public void InvokeControllerButtonUp(SDL.SDL_GamepadButton button)
    {
        OnControllerButtonUp(button);
        ControllerButtonUp?.Raise(button);
    }

    public void InvokeControllerButtonDown(SDL.SDL_GamepadButton button)
    {
        OnControllerButtonDown(button);
        ControllerButtonDown?.Raise(button);
    }

    public void InvokeMouseWheel(MouseEventType delta)
    {
        OnMouseWheel(delta);
        MouseWheel.Raise(new MouseWheelEventArgs(delta), this);
    }

    public void InvokeDragBegin(Point position)
    {
        int x = position.X - X - ParentX;
        int y = position.Y - Y - ParentY;
        OnDragBegin(x, y);
        DragBegin.Raise(new MouseEventArgs(x, y, MouseButtonType.Left, ButtonState.Pressed), this);
    }

    public void InvokeDragEnd(Point position)
    {
        int x = position.X - X - ParentX;
        int y = position.Y - Y - ParentY;
        OnDragEnd(x, y);
        DragEnd.Raise(new MouseEventArgs(x, y, MouseButtonType.Left), this);
    }

    public void InvokeMove(int x, int y)
    {
        x = x - X - ParentX;
        y = y - Y - ParentY;
        OnMove(x, y);
    }

    public virtual void OnMouseDown(int x, int y, MouseButtonType button) =>
        Parent?.OnMouseDown(X + x, Y + y, button);

    public virtual void OnMouseUp(int x, int y, MouseButtonType button)
    {
        Parent?.OnMouseUp(X + x, Y + y, button);

        if (button == MouseButtonType.Right && !IsDisposed && !CanCloseWithRightClick && !Keyboard.Alt &&
            !Keyboard.Shift && !Keyboard.Ctrl)
        {
            ContextMenu?.Show();
        }
    }

    public virtual void OnMouseWheel(MouseEventType delta) => Parent?.OnMouseWheel(delta);

    public virtual void OnMouseOver(int x, int y) => Parent?.OnMouseOver(X + x, Y + y);

    protected virtual void OnMouseEnter(int x, int y)
    {
    }

    protected virtual void OnMouseExit(int x, int y)
    {
    }

    public virtual bool OnMouseDoubleClick(int x, int y, MouseButtonType button) =>
        Parent?.OnMouseDoubleClick(X + x, Y + y, button) ?? false;

    protected virtual void OnDragBegin(int x, int y)
    {
    }

    protected virtual void OnDragEnd(int x, int y)
    {
    }

    protected virtual void OnTextInput(string c)
    {
    }

    public virtual void OnKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod) => Parent?.OnKeyDown(key, mod);

    public virtual void OnKeyUp(SDL.SDL_Keycode key, SDL.SDL_Keymod mod) => Parent?.OnKeyUp(key, mod);

    protected virtual void OnControllerButtonUp(SDL.SDL_GamepadButton button) { }

    protected virtual void OnControllerButtonDown(SDL.SDL_GamepadButton button) { }

    public virtual bool Contains(int x, int y) => !IsDisposed;

    protected virtual void OnMove(int x, int y)
    {
    }

    public virtual void OnFocusEnter()
    {
        if (!IsFocused)
        {
            IsFocused = true;
            FocusEnter.Raise(this);
            //Parent?.OnFocusEnter();
        }
    }

    public virtual void OnFocusLost()
    {
        if (IsFocused)
        {
            IsFocused = false;
            FocusLost.Raise(this);
            //Parent?.OnFocusLeft();
        }
    }

    protected virtual void OnChildAdded()
    {
    }

    protected virtual void OnChildRemoved()
    {
    }

    public virtual void CloseWithRightClick()
    {
        if (!CanCloseWithRightClick)
        {
            return;
        }

        IGui parent = Parent;

        while (parent != null)
        {
            if (!parent.CanCloseWithRightClick)
            {
                return;
            }

            parent = parent.Parent;
        }

        if (Parent == null)
        {
            Dispose();
        }
        else
        {
            Parent.CloseWithRightClick();
        }
    }

    public void KeyboardTabToNextFocus(IGui c)
    {
        int startIndex = Children.IndexOf(c);

        for (int i = startIndex + 1; i < Children.Count; i++)
        {
            if (Children[i].AcceptKeyboardInput)
            {
                Children[i].SetKeyboardFocus();

                return;
            }
        }

        for (int i = 0; i < startIndex; i++)
        {
            if (Children[i].AcceptKeyboardInput)
            {
                Children[i].SetKeyboardFocus();

                return;
            }
        }
    }

    public virtual void OnButtonClick(int buttonID) => Parent?.OnButtonClick(buttonID);

    public virtual void OnKeyboardReturn(int textID, string text) => Parent?.OnKeyboardReturn(textID, text);

    public virtual void ChangePage(int pageIndex) => Parent?.ChangePage(pageIndex);

    public virtual void Dispose()
    {
        if (IsDisposed)
        {
            return;
        }

        if (Children != null)
        {
            foreach (Control c in Children)
            {
                c?.Dispose();
            }

            Children.Clear();
        }

        IsDisposed = true;
        AfterDispose();
        Disposed?.Invoke(null, null);
    }

    /// <summary>
    /// Called after the control has been disposed.
    /// </summary>
    public virtual void AfterDispose() { }
    }
}
