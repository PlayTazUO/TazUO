using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Renderer.Effects
{
    /// <summary>
    /// Wraps ScreenOverlay.fx. Every <see cref="EffectParameter"/> is resolved once in the
    /// constructor; <see cref="Apply"/> only ever sets values, never looks parameters up by name.
    /// </summary>
    public class ScreenOverlayEffect : Effect
    {
        public ScreenOverlayEffect(GraphicsDevice graphicsDevice) : base(graphicsDevice, Resources.GetScreenOverlayShader().ToArray())
        {
            MatrixTransform = Parameters["MatrixTransform"];

            Center = Parameters["Center"];
            AspectScale = Parameters["AspectScale"];
            Radius = Parameters["Radius"];
            Feather = Parameters["Feather"];
            EdgeBlend = Parameters["EdgeBlend"];
            FocusDir = Parameters["FocusDir"];
            FocusPower = Parameters["FocusPower"];
            FocusAmount = Parameters["FocusAmount"];

            Time = Parameters["Time"];
            Scale0 = Parameters["Scale0"];
            Scale1 = Parameters["Scale1"];
            Scroll0 = Parameters["Scroll0"];
            Scroll1 = Parameters["Scroll1"];
            Channel0 = Parameters["Channel0"];
            Channel1 = Parameters["Channel1"];
            WarpStrength = Parameters["WarpStrength"];
            RidgeAmount = Parameters["RidgeAmount"];
            Threshold = Parameters["Threshold"];
            Softness = Parameters["Softness"];
            NoiseAmount = Parameters["NoiseAmount"];

            Tint = Parameters["Tint"];
            Opacity = Parameters["Opacity"];
            Intensity = Parameters["Intensity"];
            PulseFreq = Parameters["PulseFreq"];
            PulseAmp = Parameters["PulseAmp"];
        }

        public EffectParameter MatrixTransform { get; }

        public EffectParameter Center { get; }
        public EffectParameter AspectScale { get; }
        public EffectParameter Radius { get; }
        public EffectParameter Feather { get; }
        public EffectParameter EdgeBlend { get; }
        public EffectParameter FocusDir { get; }
        public EffectParameter FocusPower { get; }
        public EffectParameter FocusAmount { get; }

        public EffectParameter Time { get; }
        public EffectParameter Scale0 { get; }
        public EffectParameter Scale1 { get; }
        public EffectParameter Scroll0 { get; }
        public EffectParameter Scroll1 { get; }
        public EffectParameter Channel0 { get; }
        public EffectParameter Channel1 { get; }
        public EffectParameter WarpStrength { get; }
        public EffectParameter RidgeAmount { get; }
        public EffectParameter Threshold { get; }
        public EffectParameter Softness { get; }
        public EffectParameter NoiseAmount { get; }

        public EffectParameter Tint { get; }
        public EffectParameter Opacity { get; }
        public EffectParameter Intensity { get; }
        public EffectParameter PulseFreq { get; }
        public EffectParameter PulseAmp { get; }

        /// <summary>
        /// Uploads one overlay's parameters. <paramref name="p"/> must already be clamped
        /// (<see cref="OverlayParams.Clamp"/>) and have <paramref name="globalIntensity"/> folded in
        /// by the caller.
        /// </summary>
        public void Apply(in OverlayParams p, float time, Vector2 screenSize, float globalIntensity)
        {
            Center.SetValue(p.Shape.Center);
            AspectScale.SetValue(new Vector2(1f, screenSize.Y / screenSize.X));
            Radius.SetValue(p.Shape.Radius);
            Feather.SetValue(p.Shape.Feather);
            EdgeBlend.SetValue(p.Shape.EdgeBlend);
            FocusDir.SetValue(p.Shape.FocusDir);
            FocusPower.SetValue(p.Shape.FocusPower);
            FocusAmount.SetValue(p.Shape.FocusAmount);

            Time.SetValue(time);
            Scale0.SetValue(p.Noise.Scale0);
            Scale1.SetValue(p.Noise.Scale1);
            Scroll0.SetValue(p.Noise.Scroll0);
            Scroll1.SetValue(p.Noise.Scroll1);
            Channel0.SetValue(p.Noise.Channel0.ToSelector());
            Channel1.SetValue(p.Noise.Channel1.ToSelector());
            WarpStrength.SetValue(p.Noise.WarpStrength);
            RidgeAmount.SetValue(p.Noise.RidgeAmount);
            Threshold.SetValue(p.Noise.Threshold);
            Softness.SetValue(p.Noise.Softness);
            NoiseAmount.SetValue(p.Noise.Amount);

            Tint.SetValue(p.Appearance.Tint.ToVector3());
            Opacity.SetValue(p.Appearance.Opacity);
            Intensity.SetValue(p.Appearance.Intensity * globalIntensity);
            PulseFreq.SetValue(p.Appearance.PulseFreq);
            PulseAmp.SetValue(p.Appearance.PulseAmp);
        }
    }
}
