using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers.Structs;
using ClassicUO.Utility.Logging;
using Lock = System.Threading.Lock;

namespace ClassicUO.Game.Managers;

public sealed partial class AutoUnequipActionManager : IDisposable
{
    private struct Armament(uint serial, Layer layer)
    {
        public readonly uint Serial = serial;
        public readonly Layer Layer = layer;
    }

    public static AutoUnequipActionManager Instance { get; private set; }

    private readonly World _world;
    private readonly CancellationTokenSource _cTokenSource = new();
    private readonly Task _interceptConsumerCompletion;

    private readonly Channel<Action> _flushChannel = Channel.CreateUnbounded<Action>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = false }
    );

    #region Dispose

    private readonly Lock _disposalLock = new();
    private bool _disposed;

    #endregion

    #region Accessors

    /// <summary>
    ///     Checks whether the manager is in a valid state and can intercept calls.
    ///     Note that this method considers profile settings.
    /// </summary>
    /// <returns>True if the manager is ready to intercept, false otherwise</returns>
    private bool CanIntercept => !_disposed && ProfileManager.CurrentProfile?.AutoUnequipForActions == true && IsPlayerBackpackAvailable;

    /// <summary>
    ///     Determines whether the player's backpack is available
    /// </summary>
    private bool IsPlayerBackpackAvailable => _world?.Player?.Backpack != null;

    #endregion

    public AutoUnequipActionManager(World world)
    {
        _world = world;
        Instance = this;
        _interceptConsumerCompletion = Task.Run(ActionConsumer).ContinueWith(result =>
        {
            if (result.IsCanceled || result.Exception == null)
                return;

            Log.Warn("Auto-Unequip manager task processor faulted:");
            Log.Warn(result.Exception.ToString());
            // We could restart but honestly if it faulted, we're probably better off leaving it disabled.
            MainThreadQueue.InvokeOnMainThread(() =>
            {
                if (_world == null)
                    return;
                try
                {
                    GameActions.Print(_world, TazLang.Get("auto_unequip_failed_stopped"), Constants.HUE_ERROR);
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to notify user of auto-equip agent failure: {e}");
                }
            });
        });
    }

    #region Public Methods

    /// <summary>
    ///     Attempts to intercept a spell cast
    /// </summary>
    /// <param name="spellIndex">The spell being intercepted</param>
    /// <returns>True if the spell was intercepted, false otherwise</returns>
    public bool TryInterceptSpellCast(int spellIndex) =>
        ShouldInterceptCast(spellIndex) && _flushChannel.Writer.TryWrite(() => GameActions.CastSpellDirect(spellIndex));

    /// <summary>
    ///     Attempts to intercept a double click
    /// </summary>
    /// <param name="itemSerial">The serial of the item being double-clicked</param>
    /// <param name="sendDoubleClickDelegate">The original 'send double click' function</param>
    /// <returns>True if the click was intercepted, false otherwise</returns>
    public bool TryInterceptDoubleClick(uint itemSerial, Action<uint> sendDoubleClickDelegate) =>
        ShouldInterceptDblClick(itemSerial, sendDoubleClickDelegate) && _flushChannel.Writer.TryWrite(() => sendDoubleClickDelegate(itemSerial));

    /// <summary>
    ///     Disposes the manager instance.
    ///     Note this method may take a few milliseconds to return.
    /// </summary>
    public void Dispose()
    {
        lock (_disposalLock)
        {
            if (_disposed)
                return;

            // First, issue a cancel. The token propagates to the wait for the main thread as well
            _cTokenSource.Cancel();

            // Then, close the producers
            _flushChannel.Writer.Complete();

            // Wait for the consumer to return
            Task.WaitAll(_interceptConsumerCompletion);
            // Finally, dispose of the rest
            _cTokenSource.Dispose();
            Instance = null;

            _disposed = true;
        }
    }

    #endregion

    #region PrivateMethods

    /// <summary>
    ///     Determines whether a spell cast should be intercepted
    /// </summary>
    /// <param name="spellIndex">The intercepted spell's index</param>
    /// <returns>True if the spell should be intercepted, false otherwise</returns>
    private bool ShouldInterceptCast(int spellIndex)
    {
        if (!CanIntercept)
            return false;

        if (spellIndex is >= 100 and <= 678 or >= 700)
            return false;

        List<Armament> arms = GetArmingState();
        if (arms.Count <= 0)
            return false;

        foreach (Armament arm in arms)
        {
            if (!_world.OPL.TryGetNameAndData(arm.Serial, out string _, out string data))
                return true; // If missing from OPL, err on the side of caution and intercept

            if (string.IsNullOrWhiteSpace(data) || !IsSpellChannelling().IsMatch(data))
                return true;
        }

        return false;
    }

    /// <summary>
    ///     Determines whether a Double-Click should be intercepted
    /// </summary>
    /// <param name="serial">The clicked target's serial</param>
    /// <param name="sendDoubleClickDelegate">The original 'send double click' function</param>
    /// <returns>True if the event should be intercepted, false otherwise</returns>
    private bool ShouldInterceptDblClick(uint serial, Action<uint> sendDoubleClickDelegate)
    {
        if (sendDoubleClickDelegate == null || !CanIntercept)
            return false;

        return IsDrinkablePotionItem(serial) && GetArmingState().Count > 0;
    }

    /// <summary>
    ///     Gets a snapshot of the player's current arming state, that is, what weapons/shields they have equipped
    /// </summary>
    /// <returns>A list of equipped weapons/shields</returns>
    private List<Armament> GetArmingState()
    {
        // Check if player has weapons equipped
        Item oneHanded = _world.Player.FindItemByLayer(Layer.OneHanded);
        Item twoHanded = _world.Player.FindItemByLayer(Layer.TwoHanded);

        var arms = new List<Armament>();
        if (oneHanded?.Serial != null)
            arms.Add(new Armament(oneHanded.Serial, Layer.OneHanded));

        if (twoHanded?.Serial != null)
            arms.Add(new Armament(twoHanded.Serial, Layer.TwoHanded));

        return arms;
    }

    /// <summary>
    ///     Heuristically determines whether an item, given by serial, is a player-drinkable potion
    /// </summary>
    /// <param name="serial">The item's serial</param>
    /// <returns></returns>
    private bool IsDrinkablePotionItem(uint serial)
    {
        Item item = _world.Items.Get(serial);
        if (item == null)
            return false;

        // Check if item is 0xF06-0xF09 OR 0xF0B-0xF0C - these are the 'drinkable' potion graphics
        if (item.Graphic is (< 0xF06 or > 0xF09) and (< 0xF0B or > 0xF0C))
            return false;

        // Get the un-localized item name. We perform an extra check here as graphics may be shared by unrelated items
        if (_world.OPL.TryGetNameAndData(item.Serial, out string name, out _))
            // Use a simple regular expression to heuristically determine if something is a potion.
            // To be expanded on with configuration.
            return name != null && IsPotionRegex().IsMatch(name);

        // Data doesn't exist in OPL cache - stay conservative and assume this is *not* a potion
        return false;
    }

    /// <summary>
    ///     The task channel's consumer, responsible for actually performing any disarming/rearming
    /// </summary>
    private async Task ActionConsumer()
    {
        try
        {
            // Wait for interception requests/cancellation
            while (await _flushChannel.Reader.WaitToReadAsync(_cTokenSource.Token))
            {
                // A micro-delay to let producers settle, in case of a series of actions
                await Task.Delay(50, _cTokenSource.Token);

                // Gather-up whatever tasks we've collected until now
                var tasks = new List<Action>();
                while (_flushChannel.Reader.TryRead(out Action task))
                    tasks.Add(task);

                ExecuteBatchedTasks(tasks);

                // A short delay to avoid excessive spam
                await Task.Delay(200, _cTokenSource.Token);
            }
        }
        catch (OperationCanceledException)
        {
            Log.Info("Auto-Unequip action consumer has been interrupted by a cancellation request");
        }
    }

    /// <summary>
    ///     Executes the given tasks, after ensuring player has been disarmed.
    ///     Re-arms the player, afterward.
    /// </summary>
    /// <param name="tasks">The tasks to execute. These could be "cast a spell" or "drink a potion"</param>
    private void ExecuteBatchedTasks(List<Action> tasks)
    {
        if (_disposed)
            return;

        // Dispatch to MT to avoid any potential sync issues.
        //
        // We need to fetch a 'fresh' state - no point issuing an unequip command if we're not currently armed.
        //
        // Note that there's a slight edge case here - if the equipped armaments were changed
        // after enqueuing a task but before we got here, there may be a change in the 'spell channeling' status.
        //
        // Theoretically, then, a disarm may no longer be necessary.
        // This is, however, a minor and mostly harmless quirk.
        List<Armament> arms = MainThreadQueue.BubblingInvokeOnMainThread(() => IsPlayerBackpackAvailable ? GetArmingState() : null, _cTokenSource.Token);
        if (arms == null) // null means world/player/backpack are gone and we should stop.
            return;

        _cTokenSource.Token.ThrowIfCancellationRequested();

        // Enqueue a disarm if necessary
        if (arms.Count > 0)
            EnqueueUnequip(arms);
        _cTokenSource.Token.ThrowIfCancellationRequested();

        // Enqueue all batched actions
        foreach (Action task in tasks)
            ObjectActionQueue.Instance.Enqueue(new ObjectActionQueueItem(task), ActionPriority.EquipItem);
        _cTokenSource.Token.ThrowIfCancellationRequested();

        // Enqueue a re-arm if necessary
        if (arms.Count > 0)
            EnqueueReEquip(arms);
    }

    /// <summary>
    ///     Enqueues un-equip actions, if the player is currently armed
    /// </summary>
    /// <returns></returns>
    private void EnqueueUnequip(IList<Armament> arms)
    {
        // Issue an unequip for each equipped armament.
        // Future compatability with Cephalopod based players.
        foreach (Armament arm in arms ?? [])
            ObjectActionQueue.Instance.Enqueue(
                CreateUnequipQueueItem(arm.Serial, arm.Layer),
                ActionPriority.EquipItem
            );
    }

    /// <summary>
    ///     Create an un-equip queue item, to be used with the object queue
    /// </summary>
    /// <param name="serial">The item to un-equip</param>
    /// <param name="layer">The item's layer, used for validation</param>
    /// <returns></returns>
    private ObjectActionQueueItem CreateUnequipQueueItem(uint serial, Layer layer) =>
        new(() =>
        {
            if (!IsPlayerBackpackAvailable)
                return;

            Item item = _world.Items?.Get(serial);
            if (item == null || item.Container != _world.Player.Serial || item.Layer != layer)
                return;

            new MoveRequest(serial, _world.Player.Backpack.Serial, item.Amount).Execute();
        });

    /// <summary>
    ///     Enqueues the given armaments for re-equipment
    /// </summary>
    /// <param name="arms">The armaments to re-equip</param>
    private void EnqueueReEquip(IList<Armament> arms)
    {
        foreach (Armament arm in arms)
            ObjectActionQueue.Instance.Enqueue(new ObjectActionQueueItem(() =>
            {
                Item item = _world?.Items?.Get(arm.Serial);
                Item backpackItem = _world?.Player?.Backpack;

                if (item != null && backpackItem != null && item.Container == backpackItem.Serial)
                    MoveRequest.EquipItem(arm.Serial, arm.Layer)?.Execute();
            }), ActionPriority.EquipItem);
    }

    #endregion

    #region Regular Expressions

    /// <summary>
    ///     A regex used to match an item's `Spell Channeling` property
    /// </summary>
    /// <returns></returns>
    [GeneratedRegex(@"^\s*Spell Channeling\s*$", RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex IsSpellChannelling();

    /// <summary>
    ///     A regex used to match standard 'drinkable' potion names
    /// </summary>
    /// <returns></returns>
    [GeneratedRegex(@"(Strength|Agility|Heal|Cure|Nightsight|Refresh(ment)?)\s+Potion", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex IsPotionRegex();

    #endregion
}
