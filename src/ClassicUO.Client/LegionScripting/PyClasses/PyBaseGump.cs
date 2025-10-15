using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;

namespace ClassicUO.LegionScripting.PyClasses;

/// <summary>
/// Python API wrapper for Gump (game window) objects in TazUO.
/// Provides safe, thread-marshaled access to gump properties and methods from Python scripts.
/// Inherits all control manipulation methods from PyBaseControl.
/// Used in python API
/// </summary>
public class PyBaseGump(Gump gump) : PyBaseControl(gump), IPyGump
{
    /// <summary>
    /// Gets whether the gump has been disposed and is no longer valid.
    /// Returns true if the gump is disposed or no longer exists.
    /// Used in python API
    /// </summary>
    public bool IsDisposed
    {
        get
        {
            if (!VerifyIntegrity())
                return true;

            return MainThreadQueue.InvokeOnMainThread(() => Gump.IsDisposed);
        }
    }

    /// <summary>
    /// Gets the original packet text that was used to create this gump.
    /// This contains the gump layout and content data sent from the server.
    /// Used in python API
    /// </summary>
    public string PacketGumpText
    {
        get
        {
            if (!VerifyIntegrity()) return string.Empty;

            return MainThreadQueue.InvokeOnMainThread(() => Gump.PacketGumpText);
        }
    }

    /// <summary>
    /// Gets or Sets the ability to close the gump with a right click
    /// </summary>
    public bool CanCloseWithRightClick
    {
        get
        {
            if (!VerifyIntegrity())
                return false;

            return MainThreadQueue.InvokeOnMainThread(() => Gump.CanCloseWithRightClick);
        }
        set
        {
            if (!VerifyIntegrity())
                return;

            MainThreadQueue.EnqueueAction(() => Gump.CanCloseWithRightClick = value);
        }
    }

    /// <summary>
    /// Gets the underlying Gump instance that this wrapper represents.
    /// Used internally by the scripting system to access the actual game object.
    /// </summary>
    public Gump Gump { get; } = gump;

    /// <summary>
    /// Ensures the gump is fully visible within the screen boundaries.
    /// Adjusts the gump's position if it extends beyond the screen edges.
    /// Used in python API
    /// </summary>
    public void SetInScreen()
    {
        if (!VerifyIntegrity())
            return;

        MainThreadQueue.InvokeOnMainThread(Gump.SetInScreen);
    }

    /// <summary>
    /// Centers the gump vertically within the entire screen.
    /// This accounts for the full screen dimensions, including all UI elements.
    /// Used in python API
    /// </summary>
    public void CenterYInScreen()
    {
        if (!VerifyIntegrity())
            return;

        MainThreadQueue.InvokeOnMainThread(Gump.CenterYInScreen);
    }

    /// <summary>
    /// Centers the gump horizontally within the entire screen.
    /// This accounts for the full screen dimensions, including all UI elements.
    /// Used in python API
    /// </summary>
    public void CenterXInScreen()
    {
        if (!VerifyIntegrity())
            return;

        MainThreadQueue.InvokeOnMainThread(Gump.CenterXInScreen);
    }

    /// <summary>
    /// Verifies that the gump reference is still valid and not disposed.
    /// Used internally to check if the gump can be safely accessed.
    /// </summary>
    /// <returns>True if the gump is valid and not disposed, false otherwise</returns>
    private new bool VerifyIntegrity()
    {
        if (Gump == null)
            return false;

        return !Gump.IsDisposed;
    }
}
