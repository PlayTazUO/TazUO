// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Configuration;
using ClassicUO.Game.Managers;

namespace ClassicUO.Game.GameObjects
{
    public sealed partial class LightningEffect : GameEffect
    {
        private bool _isFlashing;

        public LightningEffect(World world, EffectManager manager, uint src, ushort x, ushort y, sbyte z, ushort hue)
            : base(world, manager, 0x4E20, hue, 400, 0)
        {
            IsEnabled = true;
            AnimIndex = 0;

            Entity source = World.Get(src);

            if (SerialHelper.IsValid(src) && source != null)
            {
                SetSource(source);
            }
            else
            {
                SetSource(x, y, z);
            }
        }

        public override void Update()
        {
            if (!IsDestroyed)
            {
                if (AnimIndex >= 10 || (Duration < Time.Ticks && Duration >= 0))
                {
                    Destroy();
                }
                else
                {
                    AnimationGraphic = (ushort) (Graphic + AnimIndex);

                    if (NextChangeFrameTime < Time.Ticks)
                    {
                        AnimIndex++;
                        NextChangeFrameTime = (long)Time.Ticks + IntervalInMs;
                    }

                    (ushort x, ushort y, sbyte z) = GetSource();

                    if (X != x || Y != y || Z != z)
                    {
                        SetInWorldTile(x, y, z);
                    }

                    SetFlash(ProfileManager.CurrentProfile.LightningFlash && IsFlashFrame(AnimIndex));
                }
            }
        }

        public override void Destroy()
        {
            SetFlash(false);
            base.Destroy();
        }

        // Double flash: lit on frames 0-1 and 4-5.
        private static bool IsFlashFrame(int animIndex) => animIndex < 2 || animIndex == 4 || animIndex == 5;

        private void SetFlash(bool flashing)
        {
            if (flashing == _isFlashing)
                return;

            _isFlashing = flashing;

            if (flashing)
                World.Light.BeginFlash();
            else
                World.Light.EndFlash();
        }
    }
}