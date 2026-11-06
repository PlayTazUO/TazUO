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

    private ItemMoveSource? _pendingMove;

    private ObjectActionQueue() { }

    /// <summary>
    /// Cancel every queued action that moves <paramref name="serial"/>.
    /// </summary>
    /// <remarks>
    /// Called when the server rejects a move (packet 0x27) so the rest of the queue does not keep
    /// trying to move an item the server refused to hand over.
    /// </remarks>
    public void CancelMovesFor(uint serial)
    {
        if (serial == 0) return;

        RemoveWhere(item => item.Serial == serial);
    }

    /// <summary>
    /// The location of the most recent item move sent to the server that has not been confirmed yet.
    /// </summary>
    /// <remarks>
    /// Queued moves send the pickup/drop packets without populating <see cref="ItemHold"/> (unlike a
    /// manual drag), so this snapshot is what lets the deny handler put a rejected item back.
    /// </remarks>
    public bool TryGetPendingMove(out ItemMoveSource source)
    {
        if (_pendingMove.HasValue)
        {
            source = _pendingMove.Value;
            return true;
        }

        source = default;
        return false;
    }

    /// <summary>
    /// Forget the pending move. When <paramref name="serial"/> is non-zero the pending move is only
    /// cleared if it belongs to that item.
    /// </summary>
    public void ClearPendingMove(uint serial = 0)
    {
        if (_pendingMove.HasValue && (serial == 0 || _pendingMove.Value.Serial == serial))
            _pendingMove = null;
    }

    internal void OnMoveStarted(MoveRequest moveRequest)
    {
        Item item = World.Instance.Items.Get(moveRequest.Serial);
        _pendingMove = item != null ? ItemMoveSource.Capture(item) : null;
    }

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
    /// Item serial this action moves, or zero when the action does not target a specific item.
    /// Used to cancel queued moves for an item the server refused.
    /// </summary>
    public uint Serial { get; init; }

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
        })
        {
            Serial = moveRequest.Serial
        };

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
