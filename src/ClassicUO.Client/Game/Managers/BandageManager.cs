using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Utility;
using System;

namespace ClassicUO.Game.Managers
{
    internal class BandageManager
    {
        public static BandageManager Instance { get; private set; } = new();

        private long nextBandageTime = 0;
        private bool isEnabled => ProfileManager.CurrentProfile?.EnableBandageAgent ?? false;
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
            EventSink.OnPlayerStatChange += OnPlayerStatChanged;
            EventSink.OnBuffAdded += OnBuffAdded;
            EventSink.OnBuffRemoved += OnBuffRemoved;
        }

        private void OnPlayerStatChanged(object sender, PlayerStatChangedArgs e)
        {
            if (!isEnabled || e.Stat != PlayerStatChangedArgs.PlayerStat.Hits)
                return;

            OnHpChanged(e.OldValue, e.NewValue);
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

        private void OnHpChanged(int oldHp, int newHp)
        {
            var player = World.Instance.Player;
            if (player == null || !isEnabled)
                return;

            // Guard against divide-by-zero and invul
            if (player.HitsMax <= 0 || (CheckInvul && player.IsYellowHits))
                return;

            var currentHpPercentage = (int)((double)newHp / player.HitsMax * 100);

            // Check for hidden status
            if (CheckHidden && player.IsHidden)
                return;

            // Check for poison status #thanks taz
            if ((!UseOnPoisoned || !player.IsPoisoned) &&
                currentHpPercentage >= hpPercentageThreshold)
                return;

            // If using buff checking, only prevent healing if buff is present
            if (checkForBuff && HasBandagingBuff)
                return;

            // If using delay checking (not buff checking), check time delay
            if (!checkForBuff && Time.Ticks < nextBandageTime)
                return;

            AttemptHeal();
        }

        private void AttemptHeal()
        {
            // Enqueue the healing action into the global priority queue
            GlobalPriorityQueue.Instance.Enqueue(() => ExecuteHeal());
        }

        private void ExecuteHeal()
        {
            if (World.Instance.Player == null)
                return;

            if (useNewBandagePacket)
            {
                if (GameActions.BandageSelf(World.Instance))
                {
                    nextBandageTime = Time.Ticks + healDelayMs;
                }
            }
            else
            {
                Item bandage = FindBandage();
                if (bandage == null)
                    return;

                // Set up auto-target before double-clicking
                TargetManager.SetAutoTarget(World.Instance.Player.Serial, TargetType.Beneficial, CursorTarget.Object);

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
    }
}
