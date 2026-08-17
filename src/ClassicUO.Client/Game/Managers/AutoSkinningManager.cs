using System;
using System.Collections.Generic;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Utility;

namespace ClassicUO.Game.Managers;

/// <summary>
/// Auto skinning support. When enabled, opening a corpse automatically uses a knife/dagger
/// (from the configured graphic list) on that corpse through the object action queue -
/// i.e. it double clicks the knife and targets the corpse for you.
/// </summary>
public sealed class AutoSkinningManager
{
    public static AutoSkinningManager Instance
    {
        get
        {
            if (field == null)
                field = new();
            return field;
        }
        private set => field = value;
    }

    // Bounded history of corpses we've already tried to skin so we don't spam the same corpse
    // when it re-fires open/creation events.
    private const int MaxSkinnedHistory = 200;
    private readonly HashSet<uint> _skinnedCorpses = new();
    private readonly Queue<uint> _skinnedOrder = new();

    private string _cachedRaw;
    private readonly List<ushort> _cachedGraphics = new();

    private AutoSkinningManager() { }

    public bool IsEnabled => ProfileManager.CurrentProfile?.EnableAutoSkinning ?? false;

    public void OnSceneLoad() => EventSink.OnOpenContainer += OnOpenContainer;

    public void OnSceneUnload()
    {
        EventSink.OnOpenContainer -= OnOpenContainer;
        _skinnedCorpses.Clear();
        _skinnedOrder.Clear();
        Instance = null;
    }

    private void OnOpenContainer(object sender, uint serial)
    {
        if (!IsEnabled)
            return;

        if (sender is Item corpse && corpse.IsCorpse)
            TrySkin(corpse);
    }

    /// <summary>
    /// Attempts to skin the given corpse if a configured knife is available. Safe to call multiple
    /// times for the same corpse - it will only be attempted once.
    /// </summary>
    public void TrySkin(Item corpse)
    {
        if (!IsEnabled || corpse is not { IsCorpse: true })
            return;

        if (corpse.IsHumanCorpse && !(ProfileManager.CurrentProfile?.AutoSkinningHumanCorpses ?? false))
            return;

        if (!MarkSkinned(corpse.Serial))
            return;

        Item knife = FindKnife();
        if (knife == null)
            return;

        EnqueueSkin(knife.Serial, corpse.Serial);
    }

    private Item FindKnife()
    {
        PlayerMobile player = World.Instance?.Player;
        if (player == null)
            return null;

        foreach (ushort graphic in GetKnifeGraphics())
        {
            Item knife = player.FindItemByGraphic(graphic);
            if (knife != null)
                return knife;
        }

        return null;
    }

    private void EnqueueSkin(uint knifeSerial, uint corpseSerial) => ObjectActionQueue.Instance.Enqueue(new ObjectActionQueueItem(() =>
                                                                          {
                                                                              World world = World.Instance;
                                                                              if (world == null)
                                                                                  return;

                                                                              // Queue the corpse serial to answer the server target request produced by using the knife.
                                                                              // Match any target cursor type since some servers send a non-neutral cursor for skinning.
                                                                              TargetManager.SetAutoTarget(corpseSerial, TargetType.Neutral, true);

                                                                              // ignoreQueue: this is already running inside the action queue, don't re-enqueue.
                                                                              GameActions.DoubleClick(world, knifeSerial, false, true);
                                                                          }), ActionPriority.UseItem);

    private bool MarkSkinned(uint serial)
    {
        if (serial == 0 || !_skinnedCorpses.Add(serial))
            return false;

        _skinnedOrder.Enqueue(serial);

        while (_skinnedOrder.Count > MaxSkinnedHistory && _skinnedOrder.TryDequeue(out uint oldest))
            _skinnedCorpses.Remove(oldest);

        return true;
    }

    private List<ushort> GetKnifeGraphics()
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
