using System.Timers;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;

namespace ClassicUO.LegionScripting.ApiClasses;

public class ApiUiNineSliceGump : ApiUiBaseControl, IApiGump
{
    private readonly NineSliceGump _nineSliceGump;
    private readonly LegionAPI _api;
    private object _onResizedCallback;
    public NineSliceGump NineSliceGump => _nineSliceGump;

    /// <summary>
    /// Creates a modern nine-slice gump using ModernUIConstants
    /// </summary>
    /// <param name="x">X position</param>
    /// <param name="y">Y position</param>
    /// <param name="width">Initial width</param>
    /// <param name="height">Initial height</param>
    /// <param name="resizable">Whether the gump can be resized by dragging corners</param>
    /// <param name="minWidth">Minimum width</param>
    /// <param name="minHeight">Minimum height</param>
    /// <param name="onResized">Optional callback function called when the gump is resized</param>
    public ApiUiNineSliceGump(LegionAPI api, int x, int y, int width, int height, bool resizable = true, int minWidth = 50, int minHeight = 50, object onResized = null)
        : base(CreateNineSliceGump(api, x, y, width, height, resizable, minWidth, minHeight))
    {
        _nineSliceGump = (NineSliceGump)Control;
        _api = api;
        _onResizedCallback = onResized;

        // Override the OnResize method if callback is provided
        if (_onResizedCallback != null)
        {
            SetupResizeCallback();
        }
    }

    private static NineSliceGump CreateNineSliceGump(LegionAPI api, int x, int y, int width, int height, bool resizable, int minWidth, int minHeight) => new ModernNineSliceGump(api, x, y, width, height, resizable, minWidth, minHeight);

    private void SetupResizeCallback()
    {
        if (_nineSliceGump is ModernNineSliceGump modernGump)
        {
            modernGump.SetResizeCallback(_onResizedCallback);
        }
    }

    /// <summary>
    /// Gets the current hue of the nine-slice gump
    /// </summary>
    /// <returns>The current hue value</returns>
    public ushort GetHue()
    {
        if (!VerifyIntegrity())
            return 0;
        return _nineSliceGump.Hue;
    }

    /// <summary>
    /// Sets the hue of the nine-slice gump
    /// </summary>
    /// <param name="hue">The hue value to set</param>
    public void SetHue(ushort hue)
    {
        if (VerifyIntegrity())
            MainThreadQueue.EnqueueAction(() => _nineSliceGump.Hue = hue);
    }

    /// <summary>
    /// Gets whether the gump is resizable
    /// </summary>
    /// <returns>True if resizable, false otherwise</returns>
    public bool GetResizable()
    {
        if (!VerifyIntegrity())
            return false;
        return _nineSliceGump.Resizable;
    }

    /// <summary>
    /// Sets whether the gump is resizable
    /// </summary>
    /// <param name="resizable">True to make resizable, false otherwise</param>
    public void SetResizable(bool resizable)
    {
        if (VerifyIntegrity())
            MainThreadQueue.EnqueueAction(() => _nineSliceGump.Resizable = resizable);
    }

    /// <summary>
    /// Gets the border size of the nine-slice
    /// </summary>
    /// <returns>The border size in pixels</returns>
    public int GetBorderSize()
    {
        if (!VerifyIntegrity())
            return 0;
        return _nineSliceGump.BorderSize;
    }

    /// <summary>
    /// Sets the border size of the nine-slice
    /// </summary>
    /// <param name="borderSize">The border size in pixels</param>
    public void SetBorderSize(int borderSize)
    {
        if (VerifyIntegrity())
            MainThreadQueue.EnqueueAction(() => _nineSliceGump.BorderSize = borderSize);
    }

    public Gump Gump => _nineSliceGump;
}

/// <summary>
/// Internal class that extends NineSliceGump to provide callback support
/// </summary>
internal class ModernNineSliceGump : NineSliceGump
{
    private const int DEBOUNCE_MS = 75;

    private readonly LegionAPI _api;
    private object _resizeCallback;
    private Timer _debounceTimer;

    private volatile bool _onResizeDispatchRequested;

    public ModernNineSliceGump(LegionAPI api, int x, int y, int width, int height, bool resizable, int minWidth, int minHeight)
        : base(Game.World.Instance, x, y, width, height, ModernUIConstants.ModernUIPanel, ModernUIConstants.ModernUIPanel_BoderSize, resizable, minWidth, minHeight)
    {
        _api = api;
    }

    /// <summary>
    /// Registers a callback to be called when the gump is resized.
    /// <remarks>
    /// Note that only one callback may be registered at a time. Subsequent calls will replace the previous callback.
    /// </remarks>
    /// </summary>
    /// <param name="callback"></param>
    public void SetResizeCallback(object callback)
    {
        _resizeCallback = callback;
        if (callback == null)
        {
            // No callback registered, stop the debounce timer, if it's running'
            if (_debounceTimer == null)
                return;

            _debounceTimer.Stop();
            _debounceTimer.Elapsed -= DispatchResizeEvent;
            _debounceTimer = null;
            return;
        }

        // A callback was registered, start the debounce timer
        _debounceTimer = new Timer { AutoReset = true, Interval = DEBOUNCE_MS };
        _debounceTimer.Elapsed += DispatchResizeEvent;
        _debounceTimer.Start();
    }

    /// <summary>
    /// Disposes the gump and releases its internal resources
    /// </summary>
    public override void Dispose()
    {
        if (_debounceTimer != null)
        {
            _debounceTimer.Elapsed -= DispatchResizeEvent;
            _debounceTimer?.Stop();
            _debounceTimer = null;
        }

        base.Dispose();
    }

    /// <summary>
    /// An handler for resize events
    /// </summary>
    /// <param name="oldWidth">The gump's old width</param>
    /// <param name="oldHeight">The gump's old height</param>
    /// <param name="newWidth">The gump's new width</param>
    /// <param name="newHeight">The gump's new height</param>
    protected override void OnResize(int oldWidth, int oldHeight, int newWidth, int newHeight)
    {
        base.OnResize(oldWidth, oldHeight, newWidth, newHeight);

        if (_resizeCallback != null)
            _onResizeDispatchRequested = true;
    }

    /// <summary>
    /// Dispatches the resize event to the registered callback
    /// </summary>
    /// <param name="sender">The sender to dispatch the event with</param>
    /// <param name="e">The event's arguments</param>
    private void DispatchResizeEvent(object sender, ElapsedEventArgs e)
    {
        // This is not a synchronized construct - this event CAN dispatch more often than
        // the debounce timer interval.
        //
        // Since we're only talking debounce and not idempotency and such, this is fine.
        // Saves on synchronization complexity and overhead.
        if (!_onResizeDispatchRequested)
            return;

        _onResizeDispatchRequested = false;

        // A bit of defensiveness to prevent null callbacks via dispose or script-based update
        if (_resizeCallback != null)
            _api.ScheduleCallback(_resizeCallback);
    }
}
