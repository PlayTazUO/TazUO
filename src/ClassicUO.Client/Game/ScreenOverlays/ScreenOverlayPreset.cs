using ClassicUO.Renderer.Effects;

namespace ClassicUO.Game.ScreenOverlays
{
    public enum OverlayId
    {
        Poison,
        Bleed,
        TunnelVision,
        Fracture
    }

    /// <summary>
    /// Bakes a small, call-site-friendly set of tunables down to the full <see cref="OverlayParams"/>
    /// the shader needs. Concrete presets expose only the handful of values worth tuning per use.
    /// </summary>
    public abstract class ScreenOverlayPreset
    {
        public float FadeInSeconds { get; set; } = 0.4f;
        public float FadeOutSeconds { get; set; } = 0.8f;

        protected abstract OverlayParams Bake();

        internal OverlayParams BakeClamped()
        {
            OverlayParams p = Bake();
            p.Clamp();
            return p;
        }
    }
}
