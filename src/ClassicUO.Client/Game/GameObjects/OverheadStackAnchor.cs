#nullable enable

namespace ClassicUO.Game.GameObjects
{
    /// <summary>
    /// How far a mobile's overhead stack sits above its draw anchor: the tallest sprite seen in its own movement and
    /// standing animations, so the stack stays still through walk cycles, wing flaps and turns.
    /// </summary>
    /// <remarks>
    /// Only grows while the look (body and mount) stays the same, and starts over when it changes. Server actions and
    /// idle fidgets (emotes, casting, attacks) are not sampled, so they never move the stack.
    /// </remarks>
    internal struct OverheadStackAnchor
    {
        #region Private members

        private int _rise;
        private uint _lookKey;
        private bool _hasRise;
        private bool _sampled;

        #endregion

        #region Public methods

        /// <summary>
        /// Feeds this frame's sprite rise and returns the rise the stack should use.
        /// </summary>
        /// <param name="spriteRise">Pixels the drawn sprite reaches above the draw anchor this frame.</param>
        /// <param name="lookKey">Identifies the body and mount; a change discards the measured rise.</param>
        /// <param name="isMovementFrame">True for walk, run and standing frames; only those are measured.</param>
        /// <returns>The rise to stack the overheads on.</returns>
        public int Update(int spriteRise, uint lookKey, bool isMovementFrame)
        {
            if (!_hasRise || lookKey != _lookKey)
            {
                _rise = spriteRise;
                _lookKey = lookKey;
                _hasRise = true;
                _sampled = isMovementFrame;

                return _rise;
            }

            if (isMovementFrame)
            {
                // A rise first taken from an action frame is replaced by the first real measurement.
                if (!_sampled || spriteRise > _rise)
                {
                    _rise = spriteRise;
                }

                _sampled = true;
            }

            return _rise;
        }

        #endregion
    }
}
