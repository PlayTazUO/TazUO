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
            if (World.Player == null || !isEnabled)
                return;

            // Guard against divide-by-zero
            if (World.Player.HitsMax <= 0)
                return;

            var currentHpPercentage = (int)((double)newHp / World.Player.HitsMax * 100);

            if (currentHpPercentage >= hpPercentageThreshold)
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
            if (World.Player == null)
                return;

            if (useNewBandagePacket)
            {
                if (GameActions.BandageSelf())
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
                TargetManager.SetAutoTarget(World.Player.Serial, TargetType.Beneficial, CursorTarget.Object);
                
                GameActions.DoubleClick(bandage.Serial);
                nextBandageTime = Time.Ticks + healDelayMs;
            }
        }

        private Item FindBandage()
        {
            if (World.Player?.FindItemByGraphic(bandageGraphic) is Item bandage)
                return bandage;

            return World.Player?.FindBandage();
        }
    }
}
