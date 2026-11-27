using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;

namespace ClassicUO.LegionScripting.ApiClasses;

public class ApiUiBaseControl(Control control)
{
    internal Control Control => control;

    /// <summary>
    /// Weather this control/gump can be moved by dragging this control
    /// </summary>
    public bool CanMove
    {
        get => GetProp(() => control.CanMove);
        set => SetProp(() => control.CanMove = value);
    }

    public bool IsVisible
    {
        get => GetProp(() => control.IsVisible);
        set => SetProp(() => control.IsVisible = value);
    }

    /// <summary>
    /// Check if this control has been disposed(delete/removed/etc)
    /// </summary>
    public bool IsDisposed => !VerifyIntegrity(); //Verify returns false if null or disposed

    /// <summary>
    /// Adds a child control to this control. Works with gumps too (gump.Add(control)).
    /// Used in python API
    /// </summary>
    /// <param name="childControl">The control to add as a child</param>
    public void Add(object childControl)
    {
        if (childControl == null || !VerifyIntegrity()) return;

        Control c = null;

        if (childControl is ApiUiBaseControl pyBaseControl)
            c = pyBaseControl.Control;
        else if(childControl is Control rawControl)
            c =  rawControl;

        if(c != null)
            MainThreadQueue.EnqueueAction(() => control?.Add(c));
    }

    /// <summary>
    /// The control's X position. Assigning sets it without the fluent <see cref="SetX"/> chain overhead.
    /// Used in python API
    /// </summary>
    public int X
    {
        get => GetProp(() => control.X);
        set => SetProp(() => control.X = value);
    }

    /// <summary>
    /// The control's Y position. Assigning sets it without the fluent <see cref="SetY"/> chain overhead.
    /// Used in python API
    /// </summary>
    public int Y
    {
        get => GetProp(() => control.Y);
        set => SetProp(() => control.Y = value);
    }

    /// <summary>
    /// Returns the control's X position.
    /// Used in python API
    /// </summary>
    /// <returns>The X coordinate of the control</returns>
    public int GetX() => GetProp(() => control.X);

    /// <summary>
    /// Returns the control's Y position.
    /// Used in python API
    /// </summary>
    /// <returns>The Y coordinate of the control</returns>
    public int GetY() => GetProp(() => control.Y);

    /// <summary>
    /// Sets the control's X position.
    /// Used in python API
    /// </summary>
    /// <param name="x">The new X coordinate</param>
    public ApiUiBaseControl SetX(int x) => SetPropFluent(() => control.X = x);

    /// <summary>
    /// Sets the control's Y position.
    /// Used in python API
    /// </summary>
    /// <param name="y">The new Y coordinate</param>
    public ApiUiBaseControl SetY(int y) => SetPropFluent(() => control.Y = y);

    /// <summary>
    /// Sets the control's X and Y positions.
    /// Used in python API
    /// </summary>
    /// <param name="x">The new X coordinate</param>
    /// <param name="y">The new Y coordinate</param>
    public ApiUiBaseControl SetPos(int x, int y) => SetPropFluent(() =>
    {
        control.X = x;
        control.Y = y;
    });

    /// <summary>
    /// The control's width in pixels. Assigning sets it without the fluent <see cref="SetWidth"/> chain overhead.
    /// Used in python API
    /// </summary>
    public int Width
    {
        get => GetProp(() => control.Width);
        set => SetProp(() => control.Width = value);
    }

    /// <summary>
    /// The control's height in pixels. Assigning sets it without the fluent <see cref="SetHeight"/> chain overhead.
    /// Used in python API
    /// </summary>
    public int Height
    {
        get => GetProp(() => control.Height);
        set => SetProp(() => control.Height = value);
    }

    public int GetWidth() => GetProp(() => control.Width);

    public int GetHeight() => GetProp(() => control.Height);

    /// <summary>
    /// Sets the control's width.
    /// Used in python API
    /// </summary>
    /// <param name="width">The new width in pixels</param>
    public ApiUiBaseControl SetWidth(int width) => SetPropFluent(() => control.Width = width);

    /// <summary>
    /// Sets the control's height.
    /// Used in python API
    /// </summary>
    /// <param name="height">The new height in pixels</param>
    public ApiUiBaseControl SetHeight(int height) => SetPropFluent(() => control.Height = height);

    /// <summary>
    /// Sets the control's position and size in one operation.
    /// Used in python API
    /// </summary>
    /// <param name="x">The new X coordinate</param>
    /// <param name="y">The new Y coordinate</param>
    /// <param name="width">The new width in pixels</param>
    /// <param name="height">The new height in pixels</param>
    public ApiUiBaseControl SetRect(int x, int y, int width, int height) => SetPropFluent(() =>
    {
        control.X = x;
        control.Y = y;
        control.Width = width;
        control.Height = height;
    });

    /// <summary>
    /// The control's position and size (X, Y, Width, Height) as one <see cref="ApiRect"/>.
    /// Reading reads all four in a single main-thread call; assigning applies all four in one
    /// non-blocking enqueue, instead of four separate property sets.
    /// Used in python API
    /// </summary>
    public ApiRect Bounds
    {
        get => GetProp(() => new ApiRect(control.X, control.Y, control.Width, control.Height));
        set
        {
            if (value == null) return;

            // Snapshot now: the caller may reuse or edit this same rect before the queued task runs.
            int x = value.X;
            int y = value.Y;
            int width = value.Width;
            int height = value.Height;

            EnqueueProp(() =>
            {
                control.X = x;
                control.Y = y;
                control.Width = width;
                control.Height = height;
            });
        }
    }

    /// <summary>
    /// Returns the control's position and size (X, Y, Width, Height) as one <see cref="ApiRect"/>.
    /// Reads all four in a single main-thread call, the read counterpart of <see cref="SetRect"/>.
    /// Used in python API
    /// </summary>
    /// <returns>The control's bounds.</returns>
    public ApiRect GetBounds() => Bounds;

    /// <summary>
    /// Centers a GUMP horizontally in the viewport. Only works on Gump instances.
    /// Used in python API
    /// </summary>
    public ApiUiBaseControl CenterXInViewPort()
    {
        if (VerifyIntegrity() && control is Gump g)
            MainThreadQueue.EnqueueAction(() => g.CenterXInViewPort());

        return this;
    }

    /// <summary>
    /// Centers a GUMP vertically in the viewport. Only works on Gump instances.
    /// Used in python API
    /// </summary>
    public ApiUiBaseControl CenterYInViewPort()
    {
        if (VerifyIntegrity() && control is Gump g)
            MainThreadQueue.EnqueueAction(() => g.CenterYInViewPort());

        return this;
    }

    /// <summary>
    /// The control's alpha (0-1). Assigning sets it without the fluent <see cref="SetAlpha"/> chain overhead.
    /// Used in python API
    /// </summary>
    public float Alpha
    {
        get => GetProp(() => control.Alpha);
        set => SetProp(() => control.Alpha = value);
    }

    /// <summary>
    /// Returns the control's Alpha value.
    /// Used in python API
    /// </summary>
    /// <returns>The Alpha value of the control</returns>
    public float GetAlpha() => GetProp(() => control.Alpha);

    /// <summary>
    /// Sets the control's Alpha value.
    /// Used in python API
    /// </summary>
    /// <param name="alpha">The new Alpha value</param>
    public ApiUiBaseControl SetAlpha(float alpha) => SetPropFluent(() => control.Alpha = alpha);

    /// <summary>
    /// Write-only plain text tooltip shown when hovering this control. Assigning enables mouse input
    /// and sets the tooltip without the fluent <see cref="SetTooltip"/> chain overhead.
    /// Used in python API
    /// </summary>
    public string Tooltip
    {
        set => SetTooltip(value);
    }

    /// <summary>
    /// Sets a plain text tooltip that is shown when hovering this control.
    /// Automatically enables mouse input so the tooltip can be triggered by hovering.
    /// Used in python API
    /// </summary>
    /// <param name="text">The tooltip text to display. Pass an empty string to clear the tooltip.</param>
    public ApiUiBaseControl SetTooltip(string text) => SetPropFluent(() =>
    {
        if (control == null) return;
        control.AcceptMouseInput = true;
        control.SetTooltip(text);
    });

    /// <summary>
    /// Write-only entity tooltip. Assigning shows that item/entity's properties on hover, as if hovering the item.
    /// Used in python API
    /// </summary>
    public uint EntityTooltip
    {
        set => SetEntityTooltip(value);
    }

    /// <summary>
    /// Sets the tooltip of this control to display the properties of an item/entity, as if hovering that item.
    /// Automatically enables mouse input so the tooltip can be triggered by hovering.
    /// Used in python API
    /// </summary>
    /// <param name="serial">The serial of the item/entity whose tooltip should be shown.</param>
    public ApiUiBaseControl SetEntityTooltip(uint serial) => SetPropFluent(() =>
    {
        if (control == null) return;
        control.AcceptMouseInput = true;
        control.SetTooltip(serial);
    });

    /// <summary>
    /// Whether this control accepts mouse input. Must be enabled for hover features such as tooltips.
    /// Used in python API
    /// </summary>
    public bool AcceptMouseInput
    {
        get => GetProp(() => control.AcceptMouseInput);
        set => SetAcceptMouseInput(value);
    }

    /// <summary>
    /// Sets whether this control accepts mouse input. Mouse input must be enabled for
    /// hover-based features such as tooltips to work.
    /// Used in python API
    /// </summary>
    /// <param name="enabled">True to accept mouse input, false to ignore it.</param>
    public ApiUiBaseControl SetAcceptMouseInput(bool enabled) => SetPropFluent(() =>
    {
        if (control != null)
            control.AcceptMouseInput = enabled;
    });

    /// <summary>
    /// Clears the tooltip from this control.
    /// Used in python API
    /// </summary>
    public ApiUiBaseControl ClearTooltip() => SetPropFluent(() => control?.ClearTooltip());

    /// <summary>
    /// Clears all child controls from this control.
    /// Used in python API
    /// </summary>
    public ApiUiBaseControl Clear() => SetPropFluent(() => control?.Clear());

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

    /// <summary>
    /// Reads a control property on the main thread, returning <paramref name="fallback"/>
    /// when the control is disposed or missing.
    /// </summary>
    protected T GetProp<T>(System.Func<T> getter, T fallback = default)
        => VerifyIntegrity() ? MainThreadQueue.InvokeOnMainThread(getter) : fallback;

    /// <summary>
    /// Applies a control property change on the main thread, no-op when the control is disposed or missing.
    /// </summary>
    protected void SetProp(System.Action setter)
    {
        if (VerifyIntegrity())
            MainThreadQueue.InvokeOnMainThread(setter);
    }

    /// <summary>
    /// Enqueues a control mutation on the main thread without blocking the caller.
    /// No-op when the control is disposed or missing.
    /// </summary>
    protected void EnqueueProp(System.Action setter)
    {
        if (VerifyIntegrity())
            MainThreadQueue.EnqueueAction(setter);
    }

    /// <summary>
    /// Enqueues a control mutation on the main thread and returns this wrapper for fluent chaining.
    /// </summary>
    protected ApiUiBaseControl SetPropFluent(System.Action setter)
    {
        EnqueueProp(setter);

        return this;
    }
}
