namespace ClassicUO.Game.GameObjects
{
    /// <summary>
    /// Centralized depth (Z) constants for the isometric world renderer.
    ///
    /// The world is drawn into a hardware depth buffer. Every sprite gets a float depth written to
    /// <c>Position.Z</c>; higher depth values render <b>in front</b>. The base value for an object is
    /// <see cref="GameObject.CalculateDepthZ"/>, whose integer part is the isometric diagonal row
    /// (<c>X + Y</c>) and whose fractional part is a height tie-breaker. The constants below are the
    /// <b>sub-tile bands</b> added on top of that base value to control the layering of things that
    /// share a tile cell (shadows, land, statics, mobiles, effects, ...).
    ///
    /// Two families of bands exist:
    /// <list type="bullet">
    /// <item><description><b>Surface bands</b> live in the range [0, 1) so they only reorder objects
    /// <i>within the same tile cell</i> and never override the coarse <c>X + Y</c> row ordering.</description></item>
    /// <item><description><b>Forward-projected bands</b> (&gt;= 1.0) intentionally push an object toward the
    /// viewer by one or more tile rows so it can correctly occlude taller objects standing in front of
    /// it (e.g. a mobile in front of a wall).</description></item>
    /// </list>
    ///
    /// To add a "between-layer" effect, pick the appropriate band. For example an effect that must sit
    /// on top of statics but below mobiles uses <see cref="SurfaceOverlay"/>.
    /// </summary>
    public static class RenderDepth
    {
        // ----- Projection depth range (see UltimaBatcher2D.SetProjectionDepthRange) -----

        /// <summary>
        /// Upper bound of the world depth range. World sprite depths are expected to fall in
        /// [0, <see cref="FarPlane"/>], which the projection maps across the full depth buffer.
        /// It must exceed the largest possible <c>X + Y</c> diagonal of any supported map
        /// (Felucca, the largest standard map, tops out around 11264) plus every band below,
        /// while staying small enough to keep good Z precision. 0x8000 fills the whole 24-bit
        /// buffer, roughly doubling the usable precision versus the legacy short range.
        /// </summary>
        public const float FarPlane = 0x8000; // 32768

        /// <summary>
        /// Depth used by things that must always draw over the entire world (e.g. weather).
        /// </summary>
        public const float Top = FarPlane - 1f;

        // ----- Surface bands: within a single tile cell, kept in [0, 1) -----

        /// <summary>Drop shadows for statics, foliage, trees and rocks. Below everything else on the tile.</summary>
        public const float ObjectShadow = 0.25f;

        /// <summary>Land / terrain tile surface.</summary>
        public const float Land = 0.49f;

        /// <summary>Base layer of the animated-water effect, drawn just under <see cref="Surface"/> to avoid z-fighting with the animated top layer.</summary>
        public const float SurfaceWetBase = 0.49f;

        /// <summary>Statics, ground items, multis and gump-art tiles. The default "surface" layer.</summary>
        public const float Surface = 0.50f;

        /// <summary>Outline pass, drawn a hair under <see cref="Surface"/> so the silhouette stays behind its sprite.</summary>
        public const float SurfaceOutline = Surface - 0.001f;

        /// <summary>
        /// NEW band: on top of statics but below mobiles. Use for effects that should hug a wall or
        /// static (mist, glowing runes, wall overlays) yet still be walked in front of by a mobile.
        /// </summary>
        public const float SurfaceOverlay = 0.60f;

        // ----- Forward-projected bands: reach into the next tile row (>= 1.0) -----

        /// <summary>
        /// Mobile body, aura and health bar. Pushed a full tile forward so a character correctly
        /// occludes statics on the tile it is standing on. Tall sprites are additionally sliced
        /// (see <c>MobileDepthSliceStep</c>) to occlude walls one or more rows behind them.
        /// </summary>
        public const float Mobile = 1.0f;

        /// <summary>
        /// NEW band: locked above mobiles. Also the effective band of source-attached game effects
        /// (which use <see cref="Mobile"/> plus the <see cref="Surface"/> offset applied while drawing).
        /// </summary>
        public const float MobileOverlay = 1.5f;
    }
}
