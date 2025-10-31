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
        private readonly HashSet<uint> _enqueuedInGlobalQueue = new();
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
        public bool UseDexFormula => ProfileManager.CurrentProfile?.BandageAgentUseDexFormula ?? false;
        private bool DisableSelfHeal => ProfileManager.CurrentProfile?.BandageAgentDisableSelfHeal ?? false;

        private BandageManager()
        {
            EventSink.OnBuffAdded += OnBuffAdded;
            EventSink.OnBuffRemoved += OnBuffRemoved;
        }

        public void SetPoisoned(uint serial, bool status)
        {
            if (!IsEnabled || !status) return;

            Mobile mobile = World.Instance?.Mobiles?.Get(serial);

            if (ShouldAttemptHeal(mobile))
            {
                AttemptHealMobile(mobile);
            }
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
            if (!IsEnabled) return;

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
            if (!IsEnabled || _pendingHeals.Count == 0)
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
            PlayerMobile player = World.Instance.Player;
            if (player == null || mobile == null)
                return false;

            if (mobile.IsDead)
                return false;

            // Check if this is the player or a friend
            bool isPlayer = mobile == player;
            bool isFriend = !isPlayer && FriendBandagingEnabled && FriendsListManager.Instance.IsFriend(mobile.Serial);
            if (!isPlayer && !isFriend)
                return false;

            // Check if self-healing is disabled
            if (isPlayer && DisableSelfHeal)
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

            int currentHpPercentage = (int)((double)mobile.Hits / mobile.HitsMax * 100);

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

            // Only enqueue if not already in the global priority queue
            if (_enqueuedInGlobalQueue.Add(mobile.Serial))
            {
                GlobalPriorityQueue.Instance.Enqueue(() => ExecuteHealMobile(mobile));
            }
        }

        private void ExecuteHealMobile(Mobile mobile)
        {
            // Remove from tracking set now that we're executing
            _enqueuedInGlobalQueue.Remove(mobile.Serial);

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
            {
                // Use the same pattern as BandageSelf but target the mobile
                AsyncNetClient.Socket.Send_TargetSelectedObject(bandage.Serial, mobile.Serial);
            }
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
            _pendingHeals.Clear();
            _enqueuedInGlobalQueue.Clear();
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
