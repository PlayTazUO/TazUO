using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Renderer.Effects;

/// <summary>
///     Wraps ScreenOverlay.fx. Every <see cref="EffectParameter" /> is resolved once in the
///     constructor; <see cref="Apply" /> only ever sets values, never looks parameters up by name.
/// </summary>
public class ScreenOverlayEffect : Effect
{
    public ScreenOverlayEffect(GraphicsDevice graphicsDevice) : base(graphicsDevice, Resources.GetScreenOverlayShader().ToArray())
    {
        MatrixTransform = Parameters["MatrixTransform"];

        Center = Parameters["Center"];
        WobbleFreq = Parameters["WobbleFreq"];
        WobbleAmp = Parameters["WobbleAmp"];
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
        NoiseOffset = Parameters["NoiseOffset"];
        WarpStrength = Parameters["WarpStrength"];
        RidgeAmount = Parameters["RidgeAmount"];
        Threshold = Parameters["Threshold"];
        Softness = Parameters["Softness"];
        FlatFloor = Parameters["FlatFloor"];

        SceneOffset = Parameters["SceneOffset"];
        SceneScale = Parameters["SceneScale"];
        SampleRadius = Parameters["SampleRadius"];
        SampleZoom = Parameters["SampleZoom"];
        SampleAberration = Parameters["SampleAberration"];

        Tint = Parameters["Tint"];
        Opacity = Parameters["Opacity"];
        Intensity = Parameters["Intensity"];
        PulseFreq = Parameters["PulseFreq"];
        PulseAmp = Parameters["PulseAmp"];

        _tintTechnique = Techniques["T0"];
        _chromaticTechnique = Techniques["Chromatic"];

        _blurTechniques = TechniquesPerTapCount("Blur");
        _radialTechniques = TechniquesPerTapCount("Radial");
    }

    /// <summary>
    ///     Resolves the one technique per <see cref="OverlaySampleTaps" /> value, each named for its
    ///     count, into a table indexed by that count.
    /// </summary>
    /// <param name="prefix">Technique name without the count, e.g. "Blur".</param>
    /// <returns>Techniques indexed by tap count; entries between the defined counts are null.</returns>
    private EffectTechnique[] TechniquesPerTapCount(string prefix)
    {
        OverlaySampleTaps[] counts = Enum.GetValues<OverlaySampleTaps>();
        var techniques = new EffectTechnique[(int)counts[^1] + 1];

        foreach (OverlaySampleTaps taps in counts)
            techniques[(int)taps] = Techniques[$"{prefix}{(int)taps}"];

        return techniques;
    }

    public EffectParameter MatrixTransform { get; }

    public EffectParameter Center { get; }
    public EffectParameter WobbleFreq { get; }
    public EffectParameter WobbleAmp { get; }
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
    public EffectParameter NoiseOffset { get; }
    public EffectParameter WarpStrength { get; }
    public EffectParameter RidgeAmount { get; }
    public EffectParameter Threshold { get; }
    public EffectParameter Softness { get; }
    public EffectParameter FlatFloor { get; }

    public EffectParameter SceneOffset { get; }
    public EffectParameter SceneScale { get; }
    public EffectParameter SampleRadius { get; }
    public EffectParameter SampleZoom { get; }
    public EffectParameter SampleAberration { get; }

    public EffectParameter Tint { get; }
    public EffectParameter Opacity { get; }
    public EffectParameter Intensity { get; }
    public EffectParameter PulseFreq { get; }
    public EffectParameter PulseAmp { get; }

    private readonly EffectTechnique _tintTechnique;
    private readonly EffectTechnique _chromaticTechnique;

    /// <summary>Blur techniques indexed by tap count, so selection is one array read.</summary>
    private readonly EffectTechnique[] _blurTechniques;

    /// <summary>Radial techniques, indexed the same way as <see cref="_blurTechniques" />.</summary>
    private readonly EffectTechnique[] _radialTechniques;

    /// <summary>
    ///     Points <see cref="Effect.CurrentTechnique" /> at the pass implementing
    ///     <paramref name="sampling" />.
    ///     <para>
    ///         The tap count selects a technique rather than being uploaded as a uniform: it is the
    ///         bound of an unrolled loop and has to be known when the shader is compiled. See
    ///         SampleDisk in ScreenOverlay.fx for what a uniform bound costs.
    ///     </para>
    /// </summary>
    /// <param name="sampling">The layer's distortion settings, already clamped.</param>
    public void SetTechnique(in OverlaySampling sampling)
    {
        EffectTechnique technique = sampling.Mode switch
        {
            OverlaySampleMode.Blur => _blurTechniques[(int)sampling.Taps],
            OverlaySampleMode.Radial => _radialTechniques[(int)sampling.Taps],
            OverlaySampleMode.Chromatic => _chromaticTechnique,
            _ => _tintTechnique
        };

        // Assigning this crosses into FNA3D, so it is worth not repeating for a run of layers
        // that all want the same pass - which is every tint-only overlay.
        if (!ReferenceEquals(technique, CurrentTechnique))
            CurrentTechnique = technique;
    }

    /// <summary>
    ///     Uploads one overlay's parameters. <paramref name="p" /> must already be clamped
    ///     (<see cref="OverlayParams.Clamp" />) and have <paramref name="globalIntensity" /> folded in
    ///     by the caller.
    /// </summary>
    /// <param name="p">The layer's clamped parameters.</param>
    /// <param name="time">Animation time in seconds, wrapped by the caller.</param>
    /// <param name="screenSize">Pixel size of the quad being filled.</param>
    /// <param name="globalIntensity">The user's overall overlay strength.</param>
    /// <param name="scene">Where that quad sits inside the scene texture. Ignored by tint layers.</param>
    public void Apply(
        in OverlayParams p,
        float time,
        Vector2 screenSize,
        float globalIntensity,
        OverlaySceneMap scene
    )
    {
        Center.SetValue(p.Shape.Center);
        WobbleFreq.SetValue(p.Shape.WobbleFreq);
        WobbleAmp.SetValue(p.Shape.WobbleAmp);
        // Guarded because the full-screen pass is handed the raw viewport, which is 0x0 while the
        // window is minimized. Square is the neutral answer: nothing is visible at that size anyway,
        // and an Inf uploaded here would still be resident on the frame the window comes back.
        AspectScale.SetValue(new Vector2(1f, screenSize.X > 0f ? screenSize.Y / screenSize.X : 1f));
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
        NoiseOffset.SetValue(p.Noise.Offset);
        WarpStrength.SetValue(p.Noise.WarpStrength);
        RidgeAmount.SetValue(p.Noise.RidgeAmount);
        Threshold.SetValue(p.Noise.Threshold);
        Softness.SetValue(p.Noise.Softness);
        FlatFloor.SetValue(p.Noise.FlatFloor);

        if (p.Sampling.ReadsScene)
            ApplySampling(p.Sampling, screenSize, scene);

        Tint.SetValue(p.Appearance.Tint.ToVector3());
        Opacity.SetValue(p.Appearance.Opacity);
        Intensity.SetValue(p.Appearance.Intensity * globalIntensity);
        PulseFreq.SetValue(p.Appearance.PulseFreq);
        PulseAmp.SetValue(p.Appearance.PulseAmp);
    }

    /// <summary>
    ///     Converts the sampling knobs into scene-texture uv, so the shader never has to know where
    ///     the quad sits or what shape the screen is.
    /// </summary>
    private void ApplySampling(in OverlaySampling sampling, Vector2 screenSize, OverlaySceneMap scene)
    {
        SceneOffset.SetValue(scene.Offset);
        SceneScale.SetValue(scene.Scale);

        // Radius is a fraction of width. Equal pixel counts vertically need the width-to-height
        // ratio folded in, or the disk comes out as an ellipse on any non-square quad.
        float aspect = screenSize.Y > 0f ? screenSize.X / screenSize.Y : 1f;

        SampleRadius.SetValue(
            new Vector2(sampling.Radius * scene.Scale.X, sampling.Radius * aspect * scene.Scale.Y)
        );

        SampleZoom.SetValue(sampling.Zoom);

        // Scales the centre ray, which already carries SceneScale, so it needs no mapping of its
        // own - and must stay isotropic or the split stops following the ray.
        SampleAberration.SetValue(new Vector2(sampling.Aberration, sampling.Aberration));
    }
}
