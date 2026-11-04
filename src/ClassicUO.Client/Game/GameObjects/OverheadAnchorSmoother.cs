#nullable enable

using System;

namespace ClassicUO.Game.GameObjects
{
    /// <summary>
    /// Holds an overhead anchor offset at the peak of the recent animation frames, so what is drawn above a mobile
    /// stays still while a walk or run cycle bobs the sprite. Larger offsets are higher.
    /// </summary>
    /// <remarks>
    /// Rises are followed at once, so overheads never sink into the sprite. Falls wait until the peak has not been
    /// reached for <see cref="HOLD_MS"/>, then glide down: a cycle that keeps reaching its peak never moves the anchor.
    /// </remarks>
    internal struct OverheadAnchorSmoother
    {
        #region Private members

        // Longer than one walk/run cycle, so each cycle touches the peak again before it is released.
        private const uint HOLD_MS = 1000;
        // A larger drop (dismount, morph, graphic swap) snaps instead of sliding across the gap.
        private const float SNAP_DISTANCE = 40f;
        // ~200ms to converge on a drop; higher speed = snappier, lower = smoother.
        private const float SPEED = 12f;

        private float _value;
        private bool _hasValue;
        private uint _lastTick;
        private uint _peakTick;

        #endregion

        #region Public methods

        /// <summary>
        /// Moves the held offset toward <paramref name="target"/>.
        /// </summary>
        /// <param name="target">Offset this frame would use unsmoothed.</param>
        /// <returns>The held offset, rounded to whole pixels.</returns>
        public int Step(int target)
        {
            uint now = Time.Ticks;

            if (!_hasValue || target >= _value || _value - target > SNAP_DISTANCE)
            {
                _value = target;
                _hasValue = true;
                _lastTick = now;
                _peakTick = now;

                return target;
            }

            float dt = (now - _lastTick) / 1000f;
            _lastTick = now;

            if (dt > 0f && now - _peakTick > HOLD_MS)
            {
                float t = 1f - MathF.Exp(-SPEED * dt);
                _value += (target - _value) * t;
            }

            return (int)MathF.Round(_value);
        }

        #endregion
    }
}
