using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;

namespace ClassicUO.LegionScripting.PyClasses;

public class PyBaseControl(Control control)
{
    internal Control Control => control;

    public bool IsVisible
    {
        get
        {
            return VerifyIntegrity() && control.IsVisible;
        }
        set
        {
            if (!VerifyIntegrity()) return;

            control.IsVisible = value;
        }
    }

    /// <summary>
    /// Adds a child control to this control. Works with gumps too (gump.Add(control)).
    /// Used in python API
    /// </summary>
    /// <param name="childControl">The control to add as a child</param>
    public void Add(object childControl)
    {
        if (childControl == null || !VerifyIntegrity()) return;

        Control c = null;

        if (childControl is PyBaseControl pyBaseControl)
            c = pyBaseControl.Control;
        else if(childControl is Control rawControl)
            c =  rawControl;

        if(c != null)
            MainThreadQueue.EnqueueAction(() => control?.Add(c));
    }

    /// <summary>
    /// Returns the control's X position.
    /// Used in python API
    /// </summary>
    /// <returns>The X coordinate of the control</returns>
    public int GetX()
    {
        if (!VerifyIntegrity())
            return 0;
        return control.X;
    }

    /// <summary>
    /// Returns the control's Y position.
    /// Used in python API
    /// </summary>
    /// <returns>The Y coordinate of the control</returns>
    public int GetY()
    {
        if (!VerifyIntegrity())
            return 0;
        return control.Y;
    }

    /// <summary>
    /// Sets the control's X position.
    /// Used in python API
    /// </summary>
    /// <param name="x">The new X coordinate</param>
    public void SetX(int x)
    {
        if (VerifyIntegrity())
            MainThreadQueue.EnqueueAction(() => control.X = x);
    }

    /// <summary>
    /// Sets the control's Y position.
    /// Used in python API
    /// </summary>
    /// <param name="y">The new Y coordinate</param>
    public void SetY(int y)
    {
        if (VerifyIntegrity())
            MainThreadQueue.EnqueueAction(() => control.Y = y);
    }

    /// <summary>
    /// Sets the control's X and Y positions.
    /// Used in python API
    /// </summary>
    /// <param name="x">The new X coordinate</param>
    /// <param name="y">The new Y coordinate</param>
    public void SetPos(int x, int y)
    {
        if (VerifyIntegrity())
            MainThreadQueue.EnqueueAction(() =>
            {
                control.X = x;
                control.Y = y;
            });
    }

    /// <summary>
    /// Sets the control's width.
    /// Used in python API
    /// </summary>
    /// <param name="width">The new width in pixels</param>
    public void SetWidth(int width)
    {
        if (VerifyIntegrity())
            MainThreadQueue.EnqueueAction(() => control.Width = width);
    }

    /// <summary>
    /// Sets the control's height.
    /// Used in python API
    /// </summary>
    /// <param name="height">The new height in pixels</param>
    public void SetHeight(int height)
    {
        if (VerifyIntegrity())
            MainThreadQueue.EnqueueAction(() => control.Height = height);
    }

    /// <summary>
    /// Sets the control's position and size in one operation.
    /// Used in python API
    /// </summary>
    /// <param name="x">The new X coordinate</param>
    /// <param name="y">The new Y coordinate</param>
    /// <param name="width">The new width in pixels</param>
    /// <param name="height">The new height in pixels</param>
    public void SetRect(int x, int y, int width, int height)
    {
        if (VerifyIntegrity())
            MainThreadQueue.EnqueueAction(() =>
            {
                control.X = x;
                control.Y = y;
                control.Width = width;
                control.Height = height;
            });
    }

    /// <summary>
    /// Centers a GUMP horizontally in the viewport. Only works on Gump instances.
    /// Used in python API
    /// </summary>
    public void CenterXInViewPort()
    {
        if (VerifyIntegrity() && control is Gump g)
            MainThreadQueue.EnqueueAction(() => g.CenterXInViewPort());
    }

    /// <summary>
    /// Centers a GUMP vertically in the viewport. Only works on Gump instances.
    /// Used in python API
    /// </summary>
    public void CenterYInViewPort()
    {
        if (VerifyIntegrity() && control is Gump g)
            MainThreadQueue.EnqueueAction(() => g.CenterYInViewPort());
    }

    /// <summary>
    /// Clears all child controls from this control.
    /// Used in python API
    /// </summary>
    public void Clear()
    {
        if (VerifyIntegrity())
            MainThreadQueue.EnqueueAction(() => control?.Clear());
    }

    /// <summary>
    /// Close/Destroy the control
    /// </summary>
    public void Dispose()
    {
        if (VerifyIntegrity())
            MainThreadQueue.EnqueueAction(() => control?.Dispose());
    }

    protected bool VerifyIntegrity()
    {
        if (control == null)
            return false;

        return !control.IsDisposed;
    }
}
