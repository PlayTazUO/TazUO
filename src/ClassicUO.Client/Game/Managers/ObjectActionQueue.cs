using System;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers.Structs;
using ClassicUO.Utility;

namespace ClassicUO.Game.Managers;

public class ObjectActionQueue : ConcurrentPriorityQueue<ObjectActionQueueItem, ActionPriority>
{
    public static ObjectActionQueue Instance { get; } = new();

    public int GetCurrentQueuedCount => _queue.Count;

    private ObjectActionQueue() { }

    public void Update()
    {
        if (IsEmpty || GlobalActionCooldown.IsOnCooldown) return; //Quick bool return if empty to avoid checking the queue when unnecessary

        while (TryDequeue(out ObjectActionQueueItem item, out ActionPriority priority, out long sequence))
        {
            if (item.Canceled)
            {
                item.AfterInvoked?.Invoke(item);
                continue;
            }

            if (priority >= ActionPriority.UnequipItem && Client.Game.UO.GameCursor.ItemHold.Enabled)
            {
                Enqueue(item,  priority, sequence); //Return to queue to retry again when not holding an item
                return;
            }

            item.Action?.Invoke();
            item.AfterInvoked?.Invoke(item);

            // Some items (e.g. bandage heals) throttle themselves and shouldn't reset the
            // shared cooldown - otherwise they'd stall the player's own queued actions. Skip
            // the cooldown for those but still process at most one item per tick.
            if (item.TriggersGlobalCooldown)
                GlobalActionCooldown.BeginCooldown();
            break;
        }
    }
}

/// <summary>
///
/// </summary>
/// <param name="action">The action to perform</param>
/// <param name="afterInvoked">Called after the action was performed, will be called weather it was canceled or not.</param>
public class ObjectActionQueueItem(Action action, Action<ObjectActionQueueItem> afterInvoked = null)
{
    public Action Action { get; } = action;
    public Action<ObjectActionQueueItem> AfterInvoked { get; } = afterInvoked;
    public bool Canceled { get; private set; }

    /// <summary>
    /// When false, the queue runs this item but does not start the shared
    /// <see cref="GlobalActionCooldown"/> afterwards. Use for self-throttled actions that
    /// must not block the player's other queued item actions. Defaults to true.
    /// </summary>
    public bool TriggersGlobalCooldown { get; init; } = true;

    public void SetCanceled(bool canceled = true) => Canceled = canceled;

    private static ObjectActionQueueItem FromMoveRequest(MoveRequest moveRequest) =>
        new(() =>
        {
            moveRequest.Execute();
        });

    public static ObjectActionQueueItem QuickLoot(uint serial) => World.Instance.Items.TryGetValue(serial, out Item item) ? QuickLoot(item) : null;

    public static ObjectActionQueueItem QuickLoot(Item item)
    {
        if (item == null) return null;
        MoveRequest? moveRequest = item.ToLootBag();

        if(moveRequest.HasValue)
            return FromMoveRequest(moveRequest.Value);

        return null;
    }

    public static ObjectActionQueueItem EquipItem(uint serial, Layer layer)
    {
        MoveRequest? moveRequest = MoveRequest.EquipItem(serial, layer);

        if(moveRequest.HasValue)
            return FromMoveRequest(moveRequest.Value);

        return null;
    }

    public static ObjectActionQueueItem DoubleClick(uint serial, bool ignoreWarMode = false)
    {
        if(serial == 0) return null;

        return new ObjectActionQueueItem(() => GameActions.DoubleClick(World.Instance, serial, ignoreWarMode, true));
    }
}

public enum ActionPriority
{
    Immediate,
    ManualUseItem, //Higher priority than regular useitem which may occur in scripts
    UseItem,
    OpenCorpse,
    UnequipItem, //Unequip item to make room for equipping - must run before EquipItem
    EquipItem,
    MoveItem,
    LootItemHigh,   //Auto-loot: High priority items (still lower than manual moves)
    LootItemMedium, //Auto-loot: Normal priority items
    LootItem,       //Auto-loot: Low priority items - lowest overall priority
}
