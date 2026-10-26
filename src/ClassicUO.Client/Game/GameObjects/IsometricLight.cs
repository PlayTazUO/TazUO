// SPDX-License-Identifier: BSD-2-Clause

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.GameObjects
{
    public sealed class IsometricLight
    {
        private float _height = -0.75f;
        private int _overall = 9, _realOveall = 9;
        private int _personal = 9, _realPersonal = 9;
        // Overlapping flashes are counted so one ending doesn't cut another short.
        private int _flashes;

        public int Personal
        {
            get => _personal;
            set
            {
                _personal = value;
                Recalculate();
            }
        }

        public int Overall
        {
            get => _overall;
            set
            {
                _overall = value;
                Recalculate();
            }
        }

        public float Height
        {
            get => _height;
            set
            {
                _height = value;
                Recalculate();
            }
        }

        public int RealPersonal
        {
            get => _realPersonal;
            set
            {
                _realPersonal = value;
                Recalculate();
            }
        }

        public int RealOverall
        {
            get => _realOveall;
            set
            {
                _realOveall = value;
                Recalculate();
            }
        }

        public float IsometricLevel { get; private set; }

        public Vector3 IsometricDirection { get; } = new Vector3(-1.0f, -1.0f, .5f);

        /// <summary>
        /// Starts a full-bright flash (e.g. lightning). Light levels are left untouched, so server updates and custom
        /// light settings still apply once every flash has ended. Each call must be paired with <see cref="EndFlash"/>.
        /// </summary>
        public void BeginFlash()
        {
            _flashes++;
            Recalculate();
        }

        /// <summary>Ends a flash started with <see cref="BeginFlash"/>.</summary>
        public void EndFlash()
        {
            if (_flashes > 0)
            {
                _flashes--;
                Recalculate();
            }
        }

        private void Recalculate()
        {
            if (_flashes > 0)
            {
                IsometricLevel = 1f;

                return;
            }

            int reverted = 32 - Overall; //if overall is 0, we have MAXIMUM light, if 30, we have the MINIMUM light, so 30 is the max, but we must have some remainder for visibility

            float current = Personal > reverted ? Personal : reverted;
            IsometricLevel = current * 0.03125f;
        }
    }
}