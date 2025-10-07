using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Network;
using System;
using System.Collections.Generic;
using System.Threading;
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
            private set => field = value;
        }

        private long _nextBandageTime = 0;
        private readonly LinkedList<uint> _pendingHeals = new();
        private Timer _retryTimer;
        private const int RETRY_INTERVAL_MS = 100;

        private bool IsEnabled => ProfileManager.CurrentProfile?.EnableBandageAgent ?? false;
        private bool FriendBandagingEnabled => ProfileManager.CurrentProfile?.BandageAgentBandageFriends ?? false;
        private int HealDelayMs => ProfileManager.CurrentProfile?.BandageAgentDelay ?? 3000;
        private bool CheckForBuff => ProfileManager.CurrentProfile?.BandageAgentCheckForBuff ?? false;
        private ushort BandageGraphic => ProfileManager.CurrentProfile?.BandageAgentGraphic ?? 0x0E21;
        private bool UseNewBandagePacket => ProfileManager.CurrentProfile?.BandageAgentUseNewPacket ?? true;
        private int HpPercentageThreshold => ProfileManager.CurrentProfile?.BandageAgentHPPercentage ?? 80;
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
            if (!IsEnabled || mobile == null)
                return;

            // Check if we should heal this mobile
            if (ShouldAttemptHeal(mobile))
            {
                AttemptHealMobile(mobile);
            }
        }

        /// <summary>
        /// Schedules a retry
        /// </summary>
        private void ScheduleRetry(uint mobileSerial = 0)
        {
            if(!_pendingHeals.Contains(mobileSerial))
            {
                if (mobileSerial == World.Instance.Player)
                    _pendingHeals.AddFirst(mobileSerial);
                else
                    _pendingHeals.AddLast(mobileSerial);
            }

            VerifyTimer();
        }

        private void VerifyTimer()
        {
            if (_pendingHeals.Count == 0)
            {
                DestroyTimer();
                return;
            }
            _retryTimer ??= new Timer(ProcessRetryQueue, null, RETRY_INTERVAL_MS, RETRY_INTERVAL_MS);
        }

        private void DestroyTimer()
        {
            _retryTimer?.Dispose();
            _retryTimer = null;
        }

        /// <summary>
        /// Timer callback to process the retry queue
        /// </summary>
        private void ProcessRetryQueue(object state)
        {
            if (_pendingHeals.Count == 0) return;

            if (_pendingHeals.First == null)
            {
                _pendingHeals.RemoveFirst();
                ProcessRetryQueue(state);
                return;
            }

            uint serial = _pendingHeals.First.Value;
            _pendingHeals.RemoveFirst();
            Mobile mobile = World.Instance?.Mobiles?.Get(serial);
            if (ShouldAttemptHeal(mobile))
            {
                AttemptHealMobile(mobile);
            }

            VerifyTimer();
        }

        private bool ShouldAttemptHeal(Mobile mobile)
        {
            var player = World.Instance.Player;
            if (player == null || mobile == null)
                return false;

            // Check if this is the player or a friend
            bool isPlayer = mobile == player;
            bool isFriend = !isPlayer && FriendBandagingEnabled && FriendsListManager.Instance.IsFriend(mobile.Serial);
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
                currentHpPercentage >= HpPercentageThreshold)
                return false;

            return true;
        }

        private void AttemptHealMobile(Mobile mobile)
        {
            // If using buff checking, only prevent healing if buff is present
            if (CheckForBuff && HasBandagingBuff)
            {
                ScheduleRetry(mobile.Serial);
                return;
            }

            // If using delay checking (not buff checking), check time delay
            if (!CheckForBuff && Time.Ticks < _nextBandageTime)
            {
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

            if (UseNewBandagePacket)
            {
                // Use the same pattern as BandageSelf but target the mobile
                AsyncNetClient.Socket.Send_TargetSelectedObject(bandage.Serial, mobile.Serial);
                _nextBandageTime = Time.Ticks + (CheckForBuff ? AsyncNetClient.Socket.Statistics.Ping + 10 : HealDelayMs);
            }
            else
            {
                // Set up auto-target before double-clicking
                TargetManager.SetAutoTarget(mobile.Serial, TargetType.Beneficial, CursorTarget.Object);

                GameActions.DoubleClick(World.Instance, bandage.Serial);
                _nextBandageTime = Time.Ticks + (CheckForBuff ? AsyncNetClient.Socket.Statistics.Ping + 10 : HealDelayMs);
            }

            Log.Debug("Tried to heal someone");

            ScheduleRetry(mobile.Serial); // Schedule recheck in case heal failed and hp stayed the same
        }

        private Item FindBandage()
        {
            if (World.Instance.Player?.FindItemByGraphic(BandageGraphic) is { } bandage)
                return bandage;

            return World.Instance.Player?.FindBandage();
        }

        /// <summary>
        /// Clears all pending healing requests
        /// </summary>
        private void ClearAllPendingHeals()
        {
            _pendingHeals.Clear();
            DestroyTimer();
        }

        public void Dispose()
        {
            DestroyTimer();
            ClearAllPendingHeals();
            EventSink.OnBuffAdded -= OnBuffAdded;
            EventSink.OnBuffRemoved -= OnBuffRemoved;
            Instance = null;
        }
    }
}
