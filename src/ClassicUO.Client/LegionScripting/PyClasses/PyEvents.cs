using Microsoft.Scripting.Hosting;

namespace ClassicUO.LegionScripting.PyClasses;

public partial class PyEvents
{
    private readonly ScriptEngine _engine;
    private readonly API _api;

    internal PyEvents(ScriptEngine engine, API api)
    {
        _engine = engine;
        _api = api;
    }

    /// <summary>
    /// Subscribe to player hits changed event. Callback receives the new hits value as an integer.
    /// Example:
    /// ```py
    /// def on_hits_changed(new_hits):
    ///   API.SysMsg(f"Player hits changed to: {new_hits}")
    /// API.Events.OnPlayerHitsChanged(on_hits_changed)
    /// while not API.StopRequested:
    ///   API.ProcessCallbacks()
    ///   API.Pause(0.25)
    /// ```
    /// </summary>
    /// <param name="callback">Python function to call when player hits change</param>
    [GenApiEvent("OnPlayerHitsChanged")]
    public partial void OnPlayerHitsChanged(object callback);

    /// <summary>
    /// Called when a buff is added to your char. Callback receives a Buff object.
    /// </summary>
    /// <param name="callback"></param>
    [GenApiEvent("PyOnBuffAdded")]
    public partial void OnBuffAdded(object callback);

    /// <summary>
    /// Called when a buff is removed from your char. Callback receives a Buff object.
    /// </summary>
    /// <param name="callback"></param>
    [GenApiEvent("PyOnBuffRemoved")]
    public partial void OnBuffRemoved(object callback);

    /// <summary>
    /// Called when the player dies. Callback receives your characters serial.
    /// </summary>
    /// <param name="callback"></param>
    [GenApiEvent("OnPlayerDeath")]
    public partial void OnPlayerDeath(object callback);

    /// <summary>
    /// Called when a container is opened. Callback receives the container serial.
    /// </summary>
    /// <param name="callback"></param>
    [GenApiEvent("OnOpenContainer")]
    public partial void OnOpenContainer(object callback);

    /// <summary>
    /// Called when the player moves. Callback receives a PositionChangedArgs object with .NewLocation available in the object.
    /// </summary>
    /// <param name="callback"></param>
    [GenApiEvent("OnPositionChanged")]
    public partial void OnPlayerMoved(object callback);

    /// <summary>
    /// Called when a new item is created. Callback receives the item serial.
    /// </summary>
    [GenApiEvent("PyOnItemCreated")]
    public partial void OnItemCreated(object callback);
}
