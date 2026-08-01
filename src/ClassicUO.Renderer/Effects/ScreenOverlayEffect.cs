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
            Reach = Parameters["Reach"];
            Feather = Parameters["Feather"];
            EdgeBlend = Parameters["EdgeBlend"];
            CornerBias = Parameters["CornerBias"];
            JitterReach = Parameters["JitterReach"];
            JitterFeather = Parameters["JitterFeather"];
            JitterScale = Parameters["JitterScale"];
            JitterScroll = Parameters["JitterScroll"];
            JitterChannel = Parameters["JitterChannel"];
            FocusDir = Parameters["FocusDir"];
            FocusPower = Parameters["FocusPower"];
            FocusAmount = Parameters["FocusAmount"];

            Time = Parameters["Time"];
            BaseScale = Parameters["BaseScale"];
            DetailScale = Parameters["DetailScale"];
            BaseScroll = Parameters["BaseScroll"];
            DetailScroll = Parameters["DetailScroll"];
            BaseChannel = Parameters["BaseChannel"];
            DetailChannel = Parameters["DetailChannel"];
            WarpStrength = Parameters["WarpStrength"];
            RidgeAmount = Parameters["RidgeAmount"];
            Threshold = Parameters["Threshold"];
            Softness = Parameters["Softness"];
            FlatFloor = Parameters["FlatFloor"];

            Tint = Parameters["Tint"];
            Opacity = Parameters["Opacity"];
            Intensity = Parameters["Intensity"];
            PulseFreq = Parameters["PulseFreq"];
            PulseAmp = Parameters["PulseAmp"];
        }

        public EffectParameter MatrixTransform { get; }

        public EffectParameter Center { get; }
        public EffectParameter AspectScale { get; }
        public EffectParameter Reach { get; }
        public EffectParameter Feather { get; }
        public EffectParameter EdgeBlend { get; }
        public EffectParameter CornerBias { get; }
        public EffectParameter JitterReach { get; }
        public EffectParameter JitterFeather { get; }
        public EffectParameter JitterScale { get; }
        public EffectParameter JitterScroll { get; }
        public EffectParameter JitterChannel { get; }
        public EffectParameter FocusDir { get; }
        public EffectParameter FocusPower { get; }
        public EffectParameter FocusAmount { get; }

        public EffectParameter Time { get; }
        public EffectParameter BaseScale { get; }
        public EffectParameter DetailScale { get; }
        public EffectParameter BaseScroll { get; }
        public EffectParameter DetailScroll { get; }
        public EffectParameter BaseChannel { get; }
        public EffectParameter DetailChannel { get; }
        public EffectParameter WarpStrength { get; }
        public EffectParameter RidgeAmount { get; }
        public EffectParameter Threshold { get; }
        public EffectParameter Softness { get; }
        public EffectParameter FlatFloor { get; }

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
            Reach.SetValue(p.Shape.Reach);
            Feather.SetValue(p.Shape.Feather);
            EdgeBlend.SetValue(p.Shape.EdgeBlend);
            CornerBias.SetValue(p.Shape.CornerBias);
            JitterReach.SetValue(p.Shape.Jitter.ReachAmount);
            JitterFeather.SetValue(p.Shape.Jitter.FeatherAmount);
            JitterScale.SetValue(p.Shape.Jitter.Scale);
            JitterScroll.SetValue(p.Shape.Jitter.Scroll);
            JitterChannel.SetValue(p.Shape.Jitter.Channel.ToSelector());
            FocusDir.SetValue(p.Shape.FocusDir);
            FocusPower.SetValue(p.Shape.FocusPower);
            FocusAmount.SetValue(p.Shape.FocusAmount);

            Time.SetValue(time);
            BaseScale.SetValue(p.Noise.BaseScale);
            DetailScale.SetValue(p.Noise.DetailScale);
            BaseScroll.SetValue(p.Noise.BaseScroll);
            DetailScroll.SetValue(p.Noise.DetailScroll);
            BaseChannel.SetValue(p.Noise.BaseChannel.ToSelector());
            DetailChannel.SetValue(p.Noise.DetailChannel.ToSelector());
            WarpStrength.SetValue(p.Noise.WarpStrength);
            RidgeAmount.SetValue(p.Noise.RidgeAmount);
            Threshold.SetValue(p.Noise.Threshold);
            Softness.SetValue(p.Noise.Softness);
            FlatFloor.SetValue(p.Noise.FlatFloor);

            Tint.SetValue(p.Appearance.Tint.ToVector3());
            Opacity.SetValue(p.Appearance.Opacity);
            Intensity.SetValue(p.Appearance.Intensity * globalIntensity);
            PulseFreq.SetValue(p.Appearance.PulseFreq);
            PulseAmp.SetValue(p.Appearance.PulseAmp);
        }
    }
}
