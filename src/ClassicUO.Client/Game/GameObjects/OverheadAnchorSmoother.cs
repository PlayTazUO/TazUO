#nullable enable

using System;

namespace ClassicUO.Game.GameObjects
{
    /// <summary>
    /// Frame-rate independent exponential smoothing of an overhead anchor offset, so what is drawn above a mobile
    /// glides instead of bouncing with every animation frame. Advances at most once per game tick.
    /// </summary>
    internal struct OverheadAnchorSmoother
    {
        #region Private members

        // A larger jump (mount/dismount, morph, graphic swap) snaps instead of sliding across the gap.
        private const float SNAP_DISTANCE = 40f;
        // ~200ms to converge; higher speed = snappier, lower = smoother.
        private const float SPEED = 12f;

        private float _value;
        private bool _hasValue;
        private uint _lastTick;

        #endregion

        #region Public methods

        /// <summary>
        /// Moves the smoothed offset toward <paramref name="target"/>.
        /// </summary>
        /// <param name="target">Offset this frame would use unsmoothed.</param>
        /// <returns>The smoothed offset, rounded to whole pixels.</returns>
        public int Step(int target)
        {
            uint now = Time.Ticks;

            if (!_hasValue || Math.Abs(target - _value) > SNAP_DISTANCE)
            {
                _value = target;
                _hasValue = true;
                _lastTick = now;

                return target;
            }

            float dt = (now - _lastTick) / 1000f;

            if (dt > 0f)
            {
                float t = 1f - MathF.Exp(-SPEED * dt);
                _value += (target - _value) * t;
                _lastTick = now;
            }

            return (int)MathF.Round(_value);
        }

        #endregion
    }
}
