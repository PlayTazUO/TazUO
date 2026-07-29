using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Network;
using System;
using System.Collections.Generic;
using System.Threading;
using ClassicUO.Utility.Logging;
using Lock = System.Threading.Lock;

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
        private readonly LinkedList<uint> _pendingHeals = new();
        private readonly HashSet<uint> _enqueuedInGlobalQueue = new();
        private readonly Dictionary<uint, long> _retryDeadlines = new();
        private readonly Lock _queueLock = new();
        private Timer _retryTimer;
        private const int RETRY_INTERVAL_MS = 100;

        // Upper bound on how long a single mobile is kept in the retry queue while it
        // can't actually be healed (e.g. permanently out of range and HP not changing),
        // so we don't spin the retry timer forever. Reset whenever a heal is attempted.
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
                    VerifyTimer();
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
        /// Schedules a retry
        /// </summary>
        private void ScheduleRetry(uint mobileSerial)
        {
            if (!IsEnabled || mobileSerial == 0) return;

            uint playerSerial = World.Instance?.Player?.Serial ?? 0;

            lock (_queueLock)
            {
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

            VerifyTimer();
        }

        private void VerifyTimer()
        {
            lock (_queueLock)
            {
                if (!IsEnabled || _pendingHeals.Count == 0)
                {
                    DestroyTimer();
                    return;
                }
                _retryTimer ??= new Timer(ProcessRetryQueue, null, RETRY_INTERVAL_MS, RETRY_INTERVAL_MS);
            }
        }

        private void DestroyTimer()
        {
            _retryTimer?.Dispose();
            _retryTimer = null;
        }

        /// <summary>
        /// Timer callback to process the retry queue. The timer fires on a threadpool
        /// thread, but healing reads and mutates game state, so the actual work is
        /// marshaled onto the main thread.
        /// </summary>
        private void ProcessRetryQueue(object state)
        {
            MainThreadQueue.EnqueueAction(ProcessRetryQueueMainThread);
        }

        /// <summary>
        /// Processes a single pending heal on the main thread.
        /// </summary>
        private void ProcessRetryQueueMainThread()
        {
            try
            {
                if (!IsEnabled)
                {
                    ClearAllPendingHeals();
                    return;
                }

                PlayerMobile player = World.Instance?.Player;
                if (player == null)
                    return; // Not in game yet (login/logout/world load); keep the queue and retry later.

                // Drop anything that has been un-healable for too long so the timer
                // can eventually stop spinning (e.g. no bandages, or a stuck target).
                PruneExpiredRetries();

                if (FindBandage() == null)
                    return; // Return early if we don't have bandages..

                uint serial;

                // Safely get and remove the first item from the queue
                lock (_queueLock)
                {
                    if (_pendingHeals.Count == 0) return;

                    serial = _pendingHeals.First.Value;
                    _pendingHeals.RemoveFirst();
                }

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
                    lock (_queueLock) _retryDeadlines.Remove(serial);
                }
            }
            catch (Exception e)
            {
                Log.Error($"BandageManager failed while processing the heal retry queue: {e}");
            }
            finally
            {
                VerifyTimer();
            }
        }

        /// <summary>
        /// Whether the retry window for a mobile has elapsed without a successful heal attempt.
        /// </summary>
        private bool IsRetryExpired(uint serial)
        {
            lock (_queueLock)
                return _retryDeadlines.TryGetValue(serial, out long deadline) && Time.Ticks >= deadline;
        }

        /// <summary>
        /// Removes queued heals whose retry window has elapsed so the retry timer
        /// doesn't spin indefinitely for targets that can never be healed.
        /// </summary>
        private void PruneExpiredRetries()
        {
            lock (_queueLock)
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
            lock (_queueLock) _retryDeadlines[mobile.Serial] = Time.Ticks + MAX_RETRY_DURATION_MS;

            // Only enqueue if not already in the global priority queue
            bool shouldEnqueue;
            lock (_queueLock) shouldEnqueue = _enqueuedInGlobalQueue.Add(mobile.Serial);

            if (shouldEnqueue) ObjectActionQueue.Instance.Enqueue(new ObjectActionQueueItem(() => ExecuteHealMobile(mobile)), ActionPriority.Immediate);
        }

        private void ExecuteHealMobile(Mobile mobile)
        {
            // Remove from tracking set now that we're executing
            lock (_queueLock) _enqueuedInGlobalQueue.Remove(mobile.Serial);

            if (World.Instance == null || World.Instance.Player == null || mobile == null)
                return;

            Item bandage = FindBandage();
            if (bandage == null)
            {
                // No bandage found, schedule retry to check again later
                ScheduleRetry(mobile.Serial);
                return;
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
            lock (_queueLock)
            {
                _pendingHeals.Clear();
                _enqueuedInGlobalQueue.Clear();
                _retryDeadlines.Clear();
            }
            DestroyTimer();
        }

        public void Dispose()
        {
            DestroyTimer();
            ClearAllPendingHeals();
            EventSink.OnBuffAddedInternal -= OnBuffAdded;
            EventSink.OnBuffRemovedInternal -= OnBuffRemoved;
            EventSink.JournalEntryAdded -= OnJournalEntryAdded;
            Instance = null;
        }
    }
}
