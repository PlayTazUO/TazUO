using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Network;
using ClassicUO.Utility;
using System;
using System.Collections.Generic;
using System.Threading;

namespace ClassicUO.Game.Managers
{
    internal class BandageManager : IDisposable
    {
        public static BandageManager Instance { get; private set; } = new();

        private long nextBandageTime = 0;
        private readonly object queueLock = new object();
        private readonly Dictionary<uint, long> pendingHeals = new Dictionary<uint, long>();
        private Timer retryTimer;
        private const int RETRY_INTERVAL_MS = 500;

        private bool isEnabled => ProfileManager.CurrentProfile?.EnableBandageAgent ?? false;
        private bool friendBandagingEnabled => ProfileManager.CurrentProfile?.BandageAgentBandageFriends ?? false;
        private int healDelayMs => ProfileManager.CurrentProfile?.BandageAgentDelay ?? 3000;
        private bool checkForBuff => ProfileManager.CurrentProfile?.BandageAgentCheckForBuff ?? false;
        private ushort bandageGraphic => ProfileManager.CurrentProfile?.BandageAgentGraphic ?? 0x0E21;
        private bool useNewBandagePacket => ProfileManager.CurrentProfile?.BandageAgentUseNewPacket ?? true;
        private int hpPercentageThreshold => ProfileManager.CurrentProfile?.BandageAgentHPPercentage ?? 80;
        public bool UseOnPoisoned => ProfileManager.CurrentProfile?.BandageAgentCheckPoisoned ?? false;
        public bool CheckHidden => ProfileManager.CurrentProfile?.BandageAgentCheckHidden ?? false;
        public bool CheckInvul => ProfileManager.CurrentProfile?.BandageAgentCheckInvul ?? false;
        public bool HasBandagingBuff { get; set; } = false;

        private BandageManager()
        {
            EventSink.OnBuffAdded += OnBuffAdded;
            EventSink.OnBuffRemoved += OnBuffRemoved;
        }

        private void OnBuffAdded(object sender, BuffEventArgs e)
        {
            if (e.Buff.Type == BuffIconType.Healing)
            {
                HasBandagingBuff = true;
            }
        }

        private void OnBuffRemoved(object sender, BuffEventArgs e)
        {
            if (e.Buff.Type == BuffIconType.Healing)
            {
                HasBandagingBuff = false;
            }
        }

        /// <summary>
        /// Called from packet handlers when mobile HP changes
        /// </summary>
        public void OnMobileHpChanged(Mobile mobile, int oldHp, int newHp)
        {
            if (!isEnabled || mobile == null)
                return;

            // Check if we should heal this mobile
            if (ShouldAttemptHeal(mobile))
            {
                AttemptHealMobile(mobile);
            }
        }

        /// <summary>
        /// Schedules a retry for a specific mobile after 500ms
        /// </summary>
        private void ScheduleRetry(uint mobileSerial)
        {
            lock (queueLock)
            {
                // Schedule or reschedule this mobile for retry in 500ms
                pendingHeals[mobileSerial] = Time.Ticks + RETRY_INTERVAL_MS;

                // Start timer if this is the first pending heal
                if (retryTimer == null)
                {
                    retryTimer = new Timer(ProcessRetryQueue, null, RETRY_INTERVAL_MS, RETRY_INTERVAL_MS);
                }
            }
        }

        /// <summary>
        /// Timer callback to process the retry queue
        /// </summary>
        private void ProcessRetryQueue(object state)
        {
            List<uint> mobilesToProcess = null;
            var currentTime = Time.Ticks;

            // Find mobiles ready for retry
            lock (queueLock)
            {
                if (pendingHeals.Count > 0)
                {
                    mobilesToProcess = new List<uint>();
                    var toRemove = new List<uint>();

                    foreach (var kvp in pendingHeals)
                    {
                        if (kvp.Value <= currentTime)
                        {
                            mobilesToProcess.Add(kvp.Key);
                            toRemove.Add(kvp.Key);
                        }
                    }

                    // Remove processed items
                    foreach (var serial in toRemove)
                    {
                        pendingHeals.Remove(serial);
                    }

                    // Stop timer if queue is now empty
                    if (pendingHeals.Count == 0)
                    {
                        retryTimer?.Dispose();
                        retryTimer = null;
                    }
                }
            }

            // Process retries outside the lock
            if (mobilesToProcess != null)
            {
                foreach (var mobileSerial in mobilesToProcess)
                {
                    var mobile = World.Instance?.Mobiles?.Get(mobileSerial);
                    if (mobile != null && ShouldAttemptHeal(mobile))
                    {
                        AttemptHealMobile(mobile);
                    }
                }
            }
        }

        private bool ShouldAttemptHeal(Mobile mobile)
        {
            var player = World.Instance.Player;
            if (player == null || mobile == null)
                return false;

            // Check if this is the player or a friend
            bool isPlayer = mobile == player;
            bool isFriend = !isPlayer && friendBandagingEnabled && FriendsListManager.Instance.IsFriend(mobile.Serial);

            if (!isPlayer && !isFriend)
                return false;

            // Check distance for friends (within 3 tiles)
            if (isFriend && mobile.Distance > 3)
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

            var currentHpPercentage = (int)((double)mobile.Hits / mobile.HitsMax * 100);

            // Check for poison status or HP threshold
            if ((!UseOnPoisoned || !mobile.IsPoisoned) &&
                currentHpPercentage >= hpPercentageThreshold)
                return false;

            return true;
        }

        private void AttemptHealMobile(Mobile mobile)
        {
            // If using buff checking, only prevent healing if buff is present
            if (checkForBuff && HasBandagingBuff)
            {
                // Schedule retry in 0.5 seconds
                ScheduleRetry(mobile.Serial);
                return;
            }

            // If using delay checking (not buff checking), check time delay
            if (!checkForBuff && Time.Ticks < nextBandageTime)
            {
                // Schedule retry in 0.5 seconds
                ScheduleRetry(mobile.Serial);
                return;
            }

            // Enqueue the healing action into the global priority queue
            GlobalPriorityQueue.Instance.Enqueue(() => ExecuteHealMobile(mobile));
        }

        private void ExecuteHealMobile(Mobile mobile)
        {
            if (World.Instance.Player == null || mobile == null)
                return;

            Item bandage = FindBandage();
            if (bandage == null)
                return;

            if (useNewBandagePacket)
            {
                // Use the same pattern as BandageSelf but target the mobile
                AsyncNetClient.Socket.Send_TargetSelectedObject(bandage.Serial, mobile.Serial);
                nextBandageTime = Time.Ticks + healDelayMs;
            }
            else
            {
                // Set up auto-target before double-clicking
                TargetManager.SetAutoTarget(mobile.Serial, TargetType.Beneficial, CursorTarget.Object);

                GameActions.DoubleClick(World.Instance, bandage.Serial);
                nextBandageTime = Time.Ticks + healDelayMs;
            }
        }

        private Item FindBandage()
        {
            if (World.Instance.Player?.FindItemByGraphic(bandageGraphic) is Item bandage)
                return bandage;

            return World.Instance.Player?.FindBandage();
        }

        /// <summary>
        /// Clears all pending healing requests
        /// </summary>
        public void ClearAllPendingHeals()
        {
            lock (queueLock)
            {
                pendingHeals.Clear();
                retryTimer?.Dispose();
                retryTimer = null;
            }
        }

        public void Dispose()
        {
            retryTimer?.Dispose();
            retryTimer = null;
            ClearAllPendingHeals();
            EventSink.OnBuffAdded -= OnBuffAdded;
            EventSink.OnBuffRemoved -= OnBuffRemoved;
        }
    }
}
