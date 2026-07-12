using System;
using System.Collections.Generic;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Utility;

namespace ClassicUO.Game.Managers;

/// <summary>
/// Auto skinning support. When enabled, double clicking a knife/dagger whose graphic is in the
/// configured list prompts for a corpse target and then skins it through the object action queue.
/// </summary>
public sealed class AutoSkinningManager
{
    public static AutoSkinningManager Instance { get; } = new();

    // Set while the manager performs the actual skinning double click so the interception in
    // GameActions.DoubleClick does not re-trigger and loop.
    private bool _performingSkin;

    private string _cachedRaw;
    private readonly HashSet<ushort> _cachedGraphics = new();

    private AutoSkinningManager() { }

    public bool IsEnabled => ProfileManager.CurrentProfile?.EnableAutoSkinning ?? false;

    /// <summary>
    /// Returns true if a double click on <paramref name="serial"/> should be intercepted and turned
    /// into a skinning action instead of a normal use.
    /// </summary>
    public bool ShouldIntercept(uint serial)
    {
        if (_performingSkin || !IsEnabled)
            return false;

        if (!SerialHelper.IsItem(serial))
            return false;

        Item item = World.Instance?.Items.Get(serial);
        if (item == null)
            return false;

        return GetKnifeGraphics().Contains(item.Graphic);
    }

    /// <summary>
    /// Begins the skinning flow: prompts the user to target a corpse, then queues the skinning action.
    /// </summary>
    public void RequestSkinning(uint knifeSerial)
    {
        World world = World.Instance;
        if (world == null)
            return;

        GameActions.Print(world, TazLang.Get("autoskinning_targetcorpse", "Target a corpse to skin."));

        world.TargetManager.SetTargeting(targeted =>
        {
            if (targeted is Item corpse)
                EnqueueSkin(knifeSerial, corpse.Serial);
        }, CursorType.Target, TargetType.Neutral);
    }

    private void EnqueueSkin(uint knifeSerial, uint corpseSerial)
    {
        ObjectActionQueue.Instance.Enqueue(new ObjectActionQueueItem(() =>
        {
            World world = World.Instance;
            if (world == null)
                return;

            // Queue the corpse serial to answer the server target request produced by using the knife.
            TargetManager.SetAutoTarget(corpseSerial, TargetType.Neutral);

            _performingSkin = true;
            try
            {
                // ignoreQueue: this is already running inside the action queue, don't re-enqueue.
                GameActions.DoubleClick(world, knifeSerial, false, true);
            }
            finally
            {
                _performingSkin = false;
            }
        }), ActionPriority.UseItem);
    }

    private HashSet<ushort> GetKnifeGraphics()
    {
        string raw = ProfileManager.CurrentProfile?.AutoSkinningKnifeGraphics ?? string.Empty;

        if (raw == _cachedRaw)
            return _cachedGraphics;

        _cachedRaw = raw;
        _cachedGraphics.Clear();

        foreach (string part in raw.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (StringHelper.TryParseInt(part, out int graphic) && graphic is > 0 and <= ushort.MaxValue)
                _cachedGraphics.Add((ushort)graphic);
        }

        return _cachedGraphics;
    }
}
