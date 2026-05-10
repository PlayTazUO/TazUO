using System.Collections.Generic;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using SDL3;

namespace ClassicUO.Game.UI;

public interface IGui
{
    bool IsTopMost { get; set; }
    bool AcceptKeyboardInput { get; set; }
    bool AcceptMouseInput { get; set; }
    bool HandlesKeyboardFocus { get; set; }
    bool IsFocused { get; set; }
    bool IsDisposed { get; }
    bool IsVisible { get; set; }
    bool IsEnabled { get; set; }
    IGui RootParent { get; }
    IGui Parent { get; set; }
    ref Rectangle Bounds { get; }
    object Tooltip { get; }
    bool HasTooltip { get; }
    bool CanMove { get; set; }
    /// <summary>
    /// Is this control something the user can edit like an input field? (Affects mouse cursor graphic)
    /// </summary>
    bool IsEditable { get; set; }
    uint ServerSerial { get; set; }
    uint LocalSerial { get; set; }
    ref int X { get; }
    ref int Y { get; }
    int ScreenCoordinateX { get; }
    int ScreenCoordinateY { get; }
    ref int Height { get; }
    ref int Width { get; }
    int ParentX { get; }
    int ParentY { get; }
    int Page { get; set; }
    int ActivePage { get; set; }
    List<IGui> Children { get; }
    ClickPriority Priority { get; set; }
    bool CanCloseWithRightClick { get; }
    bool IsModal { get; }
    float Alpha { get; set;  }
    bool WantUpdateSize { get; set; }
    UILayer LayerOrder { get; set; }
    bool IsFromServer { get; set; }
    Point Location { get; set; }
    bool HasKeyboardFocus { get; }
    bool ModalClickOutsideAreaClosesThisControl { get; }

    void Update();
    void PreDraw();
    bool Draw(UltimaBatcher2D batcher, int x, int y);
    void Dispose();
    void OnFocusEnter();
    void OnFocusLost();
    void SetKeyboardFocus();
    void InvokeKeyUp(SDL.SDL_Keycode key, SDL.SDL_Keymod mod);
    void InvokeKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod);
    void InvokeTextInput(string c);
    void InvokeControllerButtonUp(SDL.SDL_GamepadButton button);
    void InvokeControllerButtonDown(SDL.SDL_GamepadButton button);
    void InvokeMouseDown(Point position, MouseButtonType button);
    void InvokeMouseUp(Point position, MouseButtonType button);
    void InvokeMouseOver(Point position);
    void InvokeMouseEnter(Point position);
    void InvokeMouseExit(Point position);
    bool InvokeMouseDoubleClick(Point position, MouseButtonType button);
    void InvokeMouseWheel(MouseEventType delta);
    void InvokeMouseCloseGumpWithRClick();
    void InvokeDragBegin(Point position);
    void InvokeDragEnd(Point position);
    void HitTest(Point position, ref IGui res);
    void HitTest(int x, int y, ref IGui res);
    void OnHitTestSuccess(int x, int y, ref IGui res);
    void OnMouseUp(int x, int y, MouseButtonType button);
    void OnMouseDown(int x, int y, MouseButtonType button);
    void OnMouseWheel(MouseEventType delta);
    void OnMouseOver(int x, int y);
    bool OnMouseDoubleClick(int x, int y, MouseButtonType button);
    void OnKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod);
    void OnKeyUp(SDL.SDL_Keycode key, SDL.SDL_Keymod mod);
    void OnButtonClick(int buttonID);
    void OnKeyboardReturn(int textID, string text);
    void ChangePage(int pageIndex);
    void CloseWithRightClick();
    bool Contains(int x, int y);
    IEnumerable<T> FindControls<T>() where T : IGui;
    void KeyboardTabToNextFocus(IGui c);
    void UpdateOffset(int x, int y);
    T Add<T>(T c, int page = 0) where T : IGui;
    void Remove(IGui c);
    void SetTooltip(string text, int maxWidth = 0);
    void SetTooltip(IGui c);
    void SetTooltip(uint entity);
    void OnPageChanged();
    void ForceSizeUpdate(bool onlyIfLarger = true);
    IGui ApplyScale(double scale, bool scalePosition = true, bool scaleSize = true, bool force = false);
    IGui SetInternalScale(double scale);
    IGui GetFirstControlAcceptKeyboardInput();
    void Insert(int index, IGui c, int page = 0);
    void BringOnTop();
}
