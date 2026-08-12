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

            // Evaluated after the action runs, so items can report whether they actually did
            // work (e.g. a bandage heal that self-throttles only counts on rounds where a heal
            // was really sent). Items that did no work don't reset the shared cooldown and
            // don't stall the queue - we drain to the next item this same tick.
            if (!(item.TriggersGlobalCooldown?.Invoke() ?? true))
                continue;

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
    /// Evaluated by the queue after <see cref="Action"/> runs to decide whether this item
    /// starts the shared <see cref="GlobalActionCooldown"/>. Returning false runs the item
    /// but skips the cooldown, so self-throttled actions (e.g. a bandage heal that didn't
    /// actually fire this round) don't block the player's other queued item actions.
    /// Defaults to always true.
    /// </summary>
    public Func<bool> TriggersGlobalCooldown { get; init; } = static () => true;

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

/// <summary>
/// Warning: Values may be rearranged, don't use int values for saving as they may loose their values
/// </summary>
public enum ActionPriority
{
    Immediate,
    ManualUseItem, //Higher priority than regular useitem which may occur in scripts
    UseItem,
    UnequipItem, //Unequip item to make room for equipping - must run before EquipItem
    EquipItem,
    MoveItem,
    LootItemHigh,   //Auto-loot: High priority items (still lower than manual moves)
    LootItemMedium, //Auto-loot: Normal priority items
    LootItem,       //Auto-loot: Low priority items - lowest overall priority
    OpenCorpse,
}
