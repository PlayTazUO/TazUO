using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Network;
using System;
using System.Collections.Generic;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Managers
{
    internal class BandageManager : IDisposable
    {
        public static BandageManager Instance
        {
            get
            {
                if (field == null)
                    field = new();
                return field;
            }
            private set;
        }

        private long _nextBandageTime = 0;
        private long _bandagingBuffSetTime = 0;
        private long _nextRetryTime = 0;
        private readonly LinkedList<uint> _pendingHeals = new();
        private readonly HashSet<uint> _enqueuedInGlobalQueue = new();
        private readonly Dictionary<uint, long> _retryDeadlines = new();

        // How often the pending heal queue is re-checked from Update(). Update() runs
        // every frame, so this throttles the re-check cadence to keep it lightweight.
        private const int RETRY_INTERVAL_MS = 100;

        // Upper bound on how long a single mobile is kept in the retry queue while it
        // can't actually be healed (e.g. permanently out of range and HP not changing),
        // so we don't re-queue it forever. Reset whenever a heal is attempted.
        private const long MAX_RETRY_DURATION_MS = 30_000;

        // Safety net in case a buff-removed event is ever missed: treat the bandaging
        // buff as expired after this long so the agent can't get stuck never healing.
        private const long MAX_BANDAGE_BUFF_AGE_MS = 15_000;

        public int PendingHealCount => _pendingHeals.Count;
        public int PendingInGlobalQueueCount => _enqueuedInGlobalQueue.Count;

        private bool IsEnabled => ProfileManager.CurrentProfile?.EnableBandageAgent ?? false;
        private bool FriendBandagingEnabled => ProfileManager.CurrentProfile?.BandageAgentBandageFriends ?? false;
        private bool AllyBandagingEnabled => ProfileManager.CurrentProfile?.BandageAgentBandageAllies ?? false;
        private bool PetBandagingEnabled => ProfileManager.CurrentProfile?.BandageAgentBandagePets ?? false;
        private int HealDelayMs => ProfileManager.CurrentProfile?.BandageAgentDelay ?? 3000;
        private bool CheckForBuff => ProfileManager.CurrentProfile?.BandageAgentCheckForBuff ?? false;
        private ushort BandageGraphic => ProfileManager.CurrentProfile?.BandageAgentGraphic ?? 0x0E21;
        private bool UseNewBandagePacket => ProfileManager.CurrentProfile?.BandageAgentUseNewPacket ?? true;
        private int HpPercentageThreshold => ProfileManager.CurrentProfile?.BandageAgentHPPercentage ?? 80;
        private bool UseOnPoisoned => ProfileManager.CurrentProfile?.BandageAgentCheckPoisoned ?? false;
        private bool CheckHidden => ProfileManager.CurrentProfile?.BandageAgentCheckHidden ?? false;
        private bool CheckInvul => ProfileManager.CurrentProfile?.BandageAgentCheckInvul ?? false;
        private bool HasBandagingBuff { get; set; } = false;
        private bool UseDexFormula => ProfileManager.CurrentProfile?.BandageAgentUseDexFormula ?? false;
        private bool DisableSelfHeal => ProfileManager.CurrentProfile?.BandageAgentDisableSelfHeal ?? false;
        private bool UseJournalTrigger => ProfileManager.CurrentProfile?.BandageAgentUseJournalTrigger ?? false;
        private string JournalMessages => ProfileManager.CurrentProfile?.BandageAgentJournalMessages ?? "";

        private BandageManager()
        {
            EventSink.OnBuffAddedInternal += OnBuffAdded;
            EventSink.OnBuffRemovedInternal += OnBuffRemoved;
            EventSink.JournalEntryAdded += OnJournalEntryAdded;
        }

        public void SetPoisoned(uint serial, bool status)
        {
            if (!IsEnabled || !status) return;

            Mobile mobile = World.Instance?.Mobiles?.Get(serial);

            if (ShouldAttemptHeal(mobile)) AttemptHealMobile(mobile);
        }

        private void OnBuffAdded(object sender, BuffEventArgs e)
        {
            if (!IsEnabled) return;

            if (e.Buff.Type == BuffIconType.Healing || e.Buff.Type == BuffIconType.Veterinary)
            {
                HasBandagingBuff = true;
                _bandagingBuffSetTime = Time.Ticks;
            }
        }

        /// <summary>
        /// Whether the bandaging buff is currently considered active. Includes a maximum
        /// age so a missed buff-removed event can't permanently disable healing.
        /// </summary>
        private bool IsBandagingBuffActive => HasBandagingBuff && (Time.Ticks - _bandagingBuffSetTime) < MAX_BANDAGE_BUFF_AGE_MS;

        private void OnJournalEntryAdded(object sender, JournalEntry e)
        {
            if (!IsEnabled || !UseJournalTrigger || e == null) return;
            if (string.IsNullOrEmpty(e.Text)) return;

            string messages = JournalMessages;
            if (string.IsNullOrWhiteSpace(messages)) return;

            string[] triggers = messages.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (string trigger in triggers)
            {
                if (!string.IsNullOrEmpty(trigger) && e.Text.Contains(trigger, StringComparison.OrdinalIgnoreCase))
                {
                    _nextBandageTime = 0;
                    HasBandagingBuff = false;
                    return;
                }
            }
        }

        private void OnBuffRemoved(object sender, BuffEventArgs e)
        {
            if (e.Buff.Type == BuffIconType.Healing)
            {
                HasBandagingBuff = false;
                if(CheckForBuff && Time.Ticks >= _nextBandageTime) //Add small delay after healing buff is removed
                    _nextBandageTime = Time.Ticks + AsyncNetClient.Socket.Statistics.Ping;
            }
            else if (e.Buff.Type == BuffIconType.Veterinary)
            {
                HasBandagingBuff = false;
                if(CheckForBuff && Time.Ticks >= _nextBandageTime) //Add small delay after healing buff is removed
                    _nextBandageTime = Time.Ticks + AsyncNetClient.Socket.Statistics.Ping;
            }
        }

        /// <summary>
        /// Called from packet handlers when mobile HP changes
        /// </summary>
        public void OnMobileHpChanged(Mobile mobile, int oldHp, int newHp)
        {
            if (!IsEnabled || mobile == null)
                return;

            // Check if we should heal this mobile
            if (ShouldAttemptHeal(mobile)) AttemptHealMobile(mobile);
        }

        /// <summary>
        /// Queues a mobile to be re-checked for healing on a later Update().
        /// </summary>
        private void ScheduleRetry(uint mobileSerial)
        {
            if (!IsEnabled || mobileSerial == 0) return;

            uint playerSerial = World.Instance?.Player?.Serial ?? 0;

            if (!_pendingHeals.Contains(mobileSerial))
            {
                if (mobileSerial == playerSerial)
                    _pendingHeals.AddFirst(mobileSerial);
                else
                    _pendingHeals.AddLast(mobileSerial);
            }

            if (!_retryDeadlines.ContainsKey(mobileSerial))
                _retryDeadlines[mobileSerial] = Time.Ticks + MAX_RETRY_DURATION_MS;
        }

        /// <summary>
        /// Driven from GameScene.Update(). Runs on the main thread, so no locking or
        /// thread marshaling is needed. Re-checks a single pending heal per interval so
        /// mobiles that couldn't be healed immediately (distance, timing, no bandages)
        /// are retried without a background timer.
        /// </summary>
        public void Update()
        {
            if (!IsEnabled)
            {
                if (_pendingHeals.Count > 0 || _enqueuedInGlobalQueue.Count > 0 || _retryDeadlines.Count > 0)
                    ClearAllPendingHeals();
                return;
            }

            if (_pendingHeals.Count == 0)
                return;

            if (Time.Ticks < _nextRetryTime)
                return;

            _nextRetryTime = Time.Ticks + RETRY_INTERVAL_MS;

            ProcessPendingHeals();
        }

        /// <summary>
        /// Processes a single pending heal. Always called on the main thread from Update().
        /// </summary>
        private void ProcessPendingHeals()
        {
            try
            {
                PlayerMobile player = World.Instance?.Player;
                if (player == null)
                    return; // Not in game yet (login/logout/world load); keep the queue and retry later.

                // Drop anything that has been un-healable for too long (e.g. no bandages,
                // or a stuck target) so the queue doesn't keep re-checking it forever.
                PruneExpiredRetries();

                if (FindBandage() == null)
                    return; // Return early if we don't have bandages..

                if (_pendingHeals.Count == 0) return;

                uint serial = _pendingHeals.First.Value;
                _pendingHeals.RemoveFirst();

                Mobile mobile = World.Instance?.Mobiles?.Get(serial);
                if (ShouldAttemptHeal(mobile))
                {
                    AttemptHealMobile(mobile);
                }
                else if (IsHealCandidate(mobile) && !IsRetryExpired(serial))
                {
                    // Conditions temporarily not met (e.g., distance, hidden, invul) but
                    // mobile still needs healing - keep retrying so we don't lose track
                    ScheduleRetry(serial);
                }
                else
                {
                    // Recovered, no longer a valid target, or retry window elapsed - stop tracking.
                    _retryDeadlines.Remove(serial);
                }
            }
            catch (Exception e)
            {
                Log.Error($"BandageManager failed while processing the heal retry queue: {e}");
            }
        }

        /// <summary>
        /// Whether the retry window for a mobile has elapsed without a successful heal attempt.
        /// </summary>
        private bool IsRetryExpired(uint serial)
        {
            return _retryDeadlines.TryGetValue(serial, out long deadline) && Time.Ticks >= deadline;
        }

        /// <summary>
        /// Removes queued heals whose retry window has elapsed so we don't keep
        /// re-checking targets that can never be healed.
        /// </summary>
        private void PruneExpiredRetries()
        {
            if (_pendingHeals.Count == 0) return;

            long now = Time.Ticks;
            LinkedListNode<uint> node = _pendingHeals.First;
            while (node != null)
            {
                LinkedListNode<uint> next = node.Next;
                if (_retryDeadlines.TryGetValue(node.Value, out long deadline) && now >= deadline)
                {
                    _pendingHeals.Remove(node);
                    _retryDeadlines.Remove(node.Value);
                }
                node = next;
            }
        }

        /// <summary>
        /// Checks whether a mobile is still a valid candidate for healing, ignoring
        /// temp conditions like distance/hidden/invul. Used to decide whether to
        /// keep retrying when ShouldAttemptHeal returns false.
        /// </summary>
        private bool IsHealCandidate(Mobile mobile)
        {
            PlayerMobile player = World.Instance?.Player;

            if (player == null || mobile == null || mobile.IsDead)
                return false;

            bool isPlayer = mobile == player;
            bool isFriend = !isPlayer && FriendBandagingEnabled && FriendsListManager.Instance.IsFriend(mobile);
            bool isAlly = !isPlayer && AllyBandagingEnabled && mobile.NotorietyFlag == NotorietyFlag.Ally;
            bool isPet = !isPlayer && PetBandagingEnabled && mobile.IsRenamable;

            if (!isPlayer && !isFriend && !isAlly && !isPet)
                return false;

            if (isPlayer && DisableSelfHeal)
                return false;

            if (mobile.HitsMax <= 0)
                return false;

            int currentHpPercentage = (int)((double)mobile.Hits / mobile.HitsMax * 100);
            return currentHpPercentage < HpPercentageThreshold || (UseOnPoisoned && mobile.IsPoisoned);
        }

        private bool ShouldAttemptHeal(Mobile mobile)
        {
            PlayerMobile player = World.Instance.Player;
            if (player == null || mobile == null)
                return false;

            if (mobile.IsDead)
                return false;

            // Check if this is the player or a friend/ally
            bool isPlayer = mobile == player;
            bool isFriend = !isPlayer && FriendBandagingEnabled && FriendsListManager.Instance.IsFriend(mobile.Serial);
            bool isAlly = !isPlayer && AllyBandagingEnabled && mobile.NotorietyFlag == NotorietyFlag.Ally;
            bool isPet = !isPlayer && PetBandagingEnabled && mobile.IsRenamable;

            if (!isPlayer && !isFriend && !isAlly && !isPet)
                return false;

            // Check if self-healing is disabled
            if (isPlayer && DisableSelfHeal)
                return false;

            // Check distance for friends/allies (within 3 tiles)
            if ((isFriend || isAlly) && mobile.Distance > 3)
                return false;

            // Guard against divide-by-zero and invul
            if (mobile.HitsMax <= 0)
                return false;

            // Check for invul if enabled
            if (CheckInvul && mobile.IsYellowHits)
                return false;

            // Check for hidden status if enabled
            if (CheckHidden && mobile.IsHidden)
                return false;

            int currentHpPercentage = (int)((double)mobile.Hits / mobile.HitsMax * 100);

            // Check for poison status or HP threshold
            if ((!UseOnPoisoned || !mobile.IsPoisoned) &&
                currentHpPercentage >= HpPercentageThreshold)
                return false;

            return true;
        }

        private void AttemptHealMobile(Mobile mobile)
        {
            if (mobile == null) return;

            // If using buff checking, only prevent healing while the bandaging buff is present
            if (CheckForBuff && IsBandagingBuffActive)
            {
                ScheduleRetry(mobile.Serial);
                return;
            }

            // Always honor the minimum time before the next bandage. In buff mode this
            // covers the short window between sending a heal and the buff packet arriving,
            // preventing a duplicate bandage from being applied.
            if (Time.Ticks < _nextBandageTime)
            {
                ScheduleRetry(mobile.Serial);
                return;
            }

            // A heal is being attempted, so refresh the retry deadline for this mobile.
            _retryDeadlines[mobile.Serial] = Time.Ticks + MAX_RETRY_DURATION_MS;

            // Only enqueue if not already in the global priority queue. The heal re-validates
            // when it runs and may not actually fire (mobile recovered, still on the bandage
            // timer, no bandage, etc.). Only a heal that really went out should reset the shared
            // action cooldown - otherwise a no-op heal round would stall the player's own queued
            // loot/move/equip actions. TriggersGlobalCooldown reads the executed result, which
            // the queue evaluates after the action runs.
            if (_enqueuedInGlobalQueue.Add(mobile.Serial))
            {
                bool healExecuted = false;
                ObjectActionQueue.Instance.Enqueue(
                    new ObjectActionQueueItem(() => healExecuted = ExecuteHealMobile(mobile))
                    {
                        TriggersGlobalCooldown = () => healExecuted
                    },
                    ActionPriority.Immediate);
            }

            // Keep the mobile queued so we re-check until IsHealCandidate is false, even if no HP-change packet arrives.
            ScheduleRetry(mobile.Serial);
        }

        /// <summary>
        /// Sends a heal at execution time if it is still warranted.
        /// </summary>
        /// <returns>True if a bandage was actually sent this round; false if the heal was skipped
        /// (recovered, still throttled, no bandage, etc.). The queue uses this to decide whether
        /// the heal should reset the shared action cooldown.</returns>
        private bool ExecuteHealMobile(Mobile mobile)
        {
            // Remove from tracking set now that we're executing
            _enqueuedInGlobalQueue.Remove(mobile.Serial);

            if (World.Instance == null || World.Instance.Player == null || mobile == null)
                return false;

            // Re-validate at execution time. The item may have waited in the queue while the
            // mobile recovered, another heal completed, or the buff/timer state changed. Without
            // this a bandage could be wasted on a mobile that no longer needs it, and the per-heal
            // throttle could be bypassed when several mobiles are enqueued in the same frame.
            if ((CheckForBuff && IsBandagingBuffActive) || Time.Ticks < _nextBandageTime || !ShouldAttemptHeal(mobile))
            {
                ScheduleRetry(mobile.Serial);
                return false;
            }

            Item bandage = FindBandage();
            if (bandage == null)
            {
                // No bandage found, schedule retry to check again later
                ScheduleRetry(mobile.Serial);
                return false;
            }

            if (UseNewBandagePacket)
                // Use the same pattern as BandageSelf but target the mobile
                AsyncNetClient.Socket.Send_TargetSelectedObject(bandage.Serial, mobile.Serial);
            else
            {
                // Set up auto-target before double-clicking
                TargetManager.SetAutoTarget(mobile.Serial, TargetType.Beneficial);

                GameActions.DoubleClick(World.Instance, bandage.Serial);
            }

            if (UseDexFormula)
                _nextBandageTime = Time.Ticks + GetDexHealingTime(mobile.Serial == World.Instance.Player);
            else
                _nextBandageTime = Time.Ticks + (CheckForBuff ? AsyncNetClient.Socket.Statistics.Ping + 10 : HealDelayMs);

            Log.Debug("Tried to heal someone");

            // Schedule recheck in case heal failed and hp stayed the same
            ScheduleRetry(mobile.Serial);
            return true;
        }

        private Item FindBandage()
        {
            if (World.Instance.Player?.FindItemByGraphic(BandageGraphic) is { } bandage)
                return bandage;

            return World.Instance.Player?.FindBandage(BandageGraphic);
        }

        /// <summary>
        /// This includes your last ping to be on the safe side
        /// </summary>
        /// <returns></returns>
        private int GetDexHealingTime(bool self)
        {
            if (!IsEnabled) return 0;

            int diff = self ? World.Instance.Player.Dexterity / 20 : World.Instance.Player.Dexterity / 60;
            int init = self ? 11 : 4;

            return (int)(((init - diff) * 1000) + AsyncNetClient.Socket.Statistics.Ping + 10);
        }

        /// <summary>
        /// Clears all pending healing requests
        /// </summary>
        private void ClearAllPendingHeals()
        {
            _pendingHeals.Clear();
            _enqueuedInGlobalQueue.Clear();
            _retryDeadlines.Clear();
        }

        public void Dispose()
        {
            ClearAllPendingHeals();
            EventSink.OnBuffAddedInternal -= OnBuffAdded;
            EventSink.OnBuffRemovedInternal -= OnBuffRemoved;
            EventSink.JournalEntryAdded -= OnJournalEntryAdded;
            Instance = null;
        }
    }
}
