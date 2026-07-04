using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI
{
    /// <summary>
    /// Single source of truth for UI scaling conversions. There are three coordinate spaces in the
    /// client UI, and this helper makes the conversions between them explicit so callers stop
    /// re-deriving the math (and stop mixing the two scales up):
    ///
    /// <list type="bullet">
    /// <item><b>Design space</b> - the raw literals authored before any scaling (e.g. a control's
    /// source size, or hard-coded margins like <c>16</c>/<c>25</c>). Multiply by a gump/context-menu
    /// scale to reach Logical space.</item>
    /// <item><b>Logical UI space</b> - what <see cref="Controls.Control"/> coordinates
    /// (<c>X/Y/Width/Height</c>, <c>ScreenCoordinateX/Y</c>) and <c>Mouse.Position</c> live in. A
    /// control's gump scale is <i>already baked into</i> these values by
    /// <see cref="Controls.Control.ApplyScale"/>; the global RenderScale is <i>never</i> applied here.</item>
    /// <item><b>Physical/screen space</b> - only <c>Client.Game.Window.ClientBounds</c> lives here.
    /// Logical space is mapped onto it by the global RenderScale at blit time.</item>
    /// </list>
    ///
    /// The one rule this helper exists to protect: <b>never multiply or divide a Control coordinate
    /// by RenderScale</b> - it is already in logical space, so doing so double-counts. RenderScale is
    /// only ever used to convert the physical window bounds <i>into</i> logical space (see
    /// <see cref="LogicalWindowWidth"/>/<see cref="LogicalWindowHeight"/>).
    /// </summary>
    public static class ScaleHelper
    {
        // --- Design space -> Logical space (the baked "gump scale" multiply) ---------------------
        // Uses (int)(value * scale) truncation to stay identical with the hand-written math it replaces.

        /// <summary>
        /// Scale a design-space value by a gump/context-menu scale to get its logical-space value.
        /// </summary>
        public static int Scaled(int value, double scale) => (int)(value * scale);

        /// <summary>
        /// Scale a design-space point by a gump/context-menu scale to get its logical-space point.
        /// </summary>
        public static Point Scaled(Point p, double scale) => new Point((int)(p.X * scale), (int)(p.Y * scale));

        /// <summary>
        /// Remove a gump/context-menu scale from a logical-space value to get its design-space value.
        /// The inverse of <see cref="Scaled(int, double)"/>.
        /// </summary>
        public static int Unscaled(int value, double scale) => scale == 0 ? value : (int)(value / scale);

        // --- Physical window -> Logical space (the live "game scale" divide) ---------------------

        /// <summary>The global game/render scale that maps logical UI space onto the physical window.</summary>
        public static float RenderScale => Client.Game.RenderScale;

        /// <summary>Convert a physical pixel measurement into logical UI space.</summary>
        public static int ToLogical(int physical) => (int)(physical / RenderScale);

        /// <summary>The game window width in logical UI space (physical width with RenderScale removed).</summary>
        public static int LogicalWindowWidth => (int)(Client.Game.Window.ClientBounds.Width / RenderScale);

        /// <summary>The game window height in logical UI space (physical height with RenderScale removed).</summary>
        public static int LogicalWindowHeight => (int)(Client.Game.Window.ClientBounds.Height / RenderScale);
    }
}
