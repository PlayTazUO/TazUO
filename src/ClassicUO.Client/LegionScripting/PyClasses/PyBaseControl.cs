using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;

namespace ClassicUO.LegionScripting.PyClasses;

public class PyBaseControl(Control control)
{
    internal Control Control => control;

    /// <summary>
    /// Weather this control/gump can be moved by dragging this control
    /// </summary>
    public bool CanMove
    {
        get
        {
            return VerifyIntegrity() && control.CanMove;
        }
        set
        {
            if (!VerifyIntegrity()) return;

            control.CanMove = value;
        }
    }

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
    /// Check if this control has been disposed(delete/removed/etc)
    /// </summary>
    public bool IsDisposed => VerifyIntegrity() && control.IsDisposed;

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
    public PyBaseControl SetX(int x)
    {
        if (VerifyIntegrity())
            MainThreadQueue.EnqueueAction(() => control.X = x);

        return this;
    }

    /// <summary>
    /// Sets the control's Y position.
    /// Used in python API
    /// </summary>
    /// <param name="y">The new Y coordinate</param>
    public PyBaseControl SetY(int y)
    {
        if (VerifyIntegrity())
            MainThreadQueue.EnqueueAction(() => control.Y = y);

        return this;
    }

    /// <summary>
    /// Sets the control's X and Y positions.
    /// Used in python API
    /// </summary>
    /// <param name="x">The new X coordinate</param>
    /// <param name="y">The new Y coordinate</param>
    public PyBaseControl SetPos(int x, int y)
    {
        if (VerifyIntegrity())
            MainThreadQueue.EnqueueAction(() =>
            {
                control.X = x;
                control.Y = y;
            });

        return this;
    }

    public int GetWidth()
    {
        if (!VerifyIntegrity()) return 0;

        return MainThreadQueue.InvokeOnMainThread(() => control.Width);
    }

    public int GetHeight()
    {
        if (!VerifyIntegrity()) return 0;

        return MainThreadQueue.InvokeOnMainThread(() => control.Height);
    }

    /// <summary>
    /// Sets the control's width.
    /// Used in python API
    /// </summary>
    /// <param name="width">The new width in pixels</param>
    public PyBaseControl SetWidth(int width)
    {
        if (VerifyIntegrity())
            MainThreadQueue.EnqueueAction(() => control.Width = width);

        return this;
    }

    /// <summary>
    /// Sets the control's height.
    /// Used in python API
    /// </summary>
    /// <param name="height">The new height in pixels</param>
    public PyBaseControl SetHeight(int height)
    {
        if (VerifyIntegrity())
            MainThreadQueue.EnqueueAction(() => control.Height = height);

        return this;
    }

    /// <summary>
    /// Sets the control's position and size in one operation.
    /// Used in python API
    /// </summary>
    /// <param name="x">The new X coordinate</param>
    /// <param name="y">The new Y coordinate</param>
    /// <param name="width">The new width in pixels</param>
    /// <param name="height">The new height in pixels</param>
    public PyBaseControl SetRect(int x, int y, int width, int height)
    {
        if (VerifyIntegrity())
            MainThreadQueue.EnqueueAction(() =>
            {
                control.X = x;
                control.Y = y;
                control.Width = width;
                control.Height = height;
            });

        return this;
    }

    /// <summary>
    /// Centers a GUMP horizontally in the viewport. Only works on Gump instances.
    /// Used in python API
    /// </summary>
    public PyBaseControl CenterXInViewPort()
    {
        if (VerifyIntegrity() && control is Gump g)
            MainThreadQueue.EnqueueAction(() => g.CenterXInViewPort());

        return this;
    }

    /// <summary>
    /// Centers a GUMP vertically in the viewport. Only works on Gump instances.
    /// Used in python API
    /// </summary>
    public PyBaseControl CenterYInViewPort()
    {
        if (VerifyIntegrity() && control is Gump g)
            MainThreadQueue.EnqueueAction(() => g.CenterYInViewPort());

        return this;
    }

    /// <summary>
    /// Returns the control's Alpha value.
    /// Used in python API
    /// </summary>
    /// <returns>The Alpha value of the control</returns>
    public float GetAlpha()
    {
        if (!VerifyIntegrity())
            return 0;

        return MainThreadQueue.InvokeOnMainThread(() => control.Alpha);
    }

    /// <summary>
    /// Sets the control's Alpha value.
    /// Used in python API
    /// </summary>
    /// <param name="alpha">The new Alpha value</param>
    public PyBaseControl SetAlpha(float alpha)
    {
        if (VerifyIntegrity())
            MainThreadQueue.EnqueueAction(() => control.Alpha = alpha);

        return this;
    }

    /// <summary>
    /// Clears all child controls from this control.
    /// Used in python API
    /// </summary>
    public PyBaseControl Clear()
    {
        if (VerifyIntegrity())
            MainThreadQueue.EnqueueAction(() => control?.Clear());

        return this;
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
