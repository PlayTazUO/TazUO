using System;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;

namespace ClassicUO.LegionScripting.PyClasses;

public class PyNineSliceGump : PyBaseControl, IPyGump
{
    private readonly NineSliceGump _nineSliceGump;
    private readonly API _api;
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
    public PyNineSliceGump(API api, int x, int y, int width, int height, bool resizable = true, int minWidth = 50, int minHeight = 50, object onResized = null)
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

    private static NineSliceGump CreateNineSliceGump(API api, int x, int y, int width, int height, bool resizable, int minWidth, int minHeight) => new ModernNineSliceGump(api, x, y, width, height, resizable, minWidth, minHeight);

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
    private readonly API _api;
    private object _resizeCallback;

    public ModernNineSliceGump(API api, int x, int y, int width, int height, bool resizable, int minWidth, int minHeight)
        : base(Game.World.Instance, x, y, width, height, ModernUIConstants.ModernUIPanel, ModernUIConstants.ModernUIPanel_BoderSize, resizable, minWidth, minHeight)
    {
        _api = api;
    }

    public void SetResizeCallback(object callback) => _resizeCallback = callback;

    protected override void OnResize(int oldWidth, int oldHeight, int newWidth, int newHeight)
    {
        base.OnResize(oldWidth, oldHeight, newWidth, newHeight);

        if (_resizeCallback != null)
        {
            _api.ScheduleCallback
            (() =>
                {
                    try
                    {
                        _api.engine.Operations.Invoke(_resizeCallback);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Script callback error: {ex}");
                    }
                }
            );
        }
    }
}
