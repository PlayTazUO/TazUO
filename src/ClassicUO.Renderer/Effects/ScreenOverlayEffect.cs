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
        SampleSwirl = Parameters["SampleSwirl"];
        SampleAberration = Parameters["SampleAberration"];

        // Both carry a suffix the technique reading them does not: uniforms and techniques share one
        // identifier table in fx_2_0, which rejects the collision as a redefinition.
        Tint = Parameters["TintColour"];
        SaturationAmount = Parameters["SaturationAmount"];
        Opacity = Parameters["Opacity"];
        Intensity = Parameters["Intensity"];
        PulseFreq = Parameters["PulseFreq"];
        PulseAmp = Parameters["PulseAmp"];

        _tintTechnique = Techniques["Tint"];
        _tintFlatTechnique = Techniques["TintFlat"];
        _chromaticTechnique = Techniques["Chromatic"];
        _chromaticFlatTechnique = Techniques["ChromaticFlat"];
        _saturationTechnique = Techniques["Saturation"];
        _saturationFlatTechnique = Techniques["SaturationFlat"];

        _blurTechniques = TechniquesPerTapCount("Blur");
        _radialTechniques = TechniquesPerTapCount("Radial");
        _radialSwirlTechniques = TechniquesPerTapCount("RadialSwirl");
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
    public EffectParameter SampleSwirl { get; }
    public EffectParameter SampleAberration { get; }

    public EffectParameter Tint { get; }
    public EffectParameter SaturationAmount { get; }
    public EffectParameter Opacity { get; }
    public EffectParameter Intensity { get; }
    public EffectParameter PulseFreq { get; }
    public EffectParameter PulseAmp { get; }

    private readonly EffectTechnique _tintTechnique;
    private readonly EffectTechnique _chromaticTechnique;
    private readonly EffectTechnique _saturationTechnique;

    /// <summary>
    ///     The same three passes with <c>OverlayAlphaFlat</c> behind them, picked when
    ///     <see cref="OverlayParams.HasFlatAlpha" /> says the noise machinery cannot change the
    ///     layer's alpha. Every technique's alpha costs three noise fetches, which a vignette's
    ///     <c>clip()</c> hides and a layer at full reach pays on every pixel - and these are the
    ///     passes where that is most of the cost: tint 91 slots/3 fetches against 49/0, chromatic
    ///     100/6 against 59/3, saturation 96/4 against 54/1.
    ///     <para>
    ///         The blur and radial families are deliberately absent. The saving is the same three
    ///         fetches but a few percent of an already-expensive march, and collecting it would need
    ///         one variant per tap count.
    ///     </para>
    /// </summary>
    private readonly EffectTechnique _tintFlatTechnique;

    /// <inheritdoc cref="_tintFlatTechnique" />
    private readonly EffectTechnique _chromaticFlatTechnique;

    /// <inheritdoc cref="_tintFlatTechnique" />
    private readonly EffectTechnique _saturationFlatTechnique;

    /// <summary>Blur techniques indexed by tap count, so selection is one array read.</summary>
    private readonly EffectTechnique[] _blurTechniques;

    /// <summary>Radial techniques, indexed the same way as <see cref="_blurTechniques" />.</summary>
    private readonly EffectTechnique[] _radialTechniques;

    /// <summary>
    ///     Radial again, with the swirling march. A separate family because the swirl angle is a
    ///     uniform: the compiler cannot fold a zero away, so a shared pass would charge every
    ///     ordinary radial layer for rotation it does not use.
    /// </summary>
    private readonly EffectTechnique[] _radialSwirlTechniques;

    /// <summary>
    ///     Points <see cref="Effect.CurrentTechnique" /> at the pass implementing
    ///     <paramref name="p" />.
    ///     <para>
    ///         The tap count selects a technique rather than being uploaded as a uniform: it is the
    ///         bound of an unrolled loop and has to be known when the shader is compiled. See
    ///         SampleDisk in ScreenOverlay.fx for what a uniform bound costs.
    ///     </para>
    ///     <para>
    ///         <see cref="OverlayParams.HasFlatAlpha" /> picks between a mode's full and flat passes
    ///         where it has both - see <see cref="_tintFlatTechnique" />.
    ///     </para>
    /// </summary>
    /// <param name="p">The layer's parameters, already clamped.</param>
    public void SetTechnique(in OverlayParams p)
    {
        bool flat = p.HasFlatAlpha;

        EffectTechnique technique = p.Sampling.Mode switch
        {
            OverlaySampleMode.Blur => _blurTechniques[(int)p.Sampling.Taps],
            OverlaySampleMode.Radial => RadialTechnique(p.Sampling),
            OverlaySampleMode.Chromatic => flat ? _chromaticFlatTechnique : _chromaticTechnique,
            OverlaySampleMode.Saturation => flat ? _saturationFlatTechnique : _saturationTechnique,
            _ => flat ? _tintFlatTechnique : _tintTechnique
        };

        // Assigning this crosses into FNA3D, so it is worth not repeating for a run of layers
        // that all want the same pass - which is every tint-only overlay.
        if (!ReferenceEquals(technique, CurrentTechnique))
            CurrentTechnique = technique;
    }

    /// <summary>
    ///     Straight or swirling march. Exact zero rather than an epsilon: a swirl too small to see is
    ///     still one the author typed, not an invitation to substitute the cheaper pass.
    /// </summary>
    /// <param name="sampling">The layer's distortion settings, already clamped.</param>
    /// <returns>The radial technique for this layer's tap count.</returns>
    private EffectTechnique RadialTechnique(in OverlaySampling sampling) =>
        sampling.Swirl == 0f
            ? _radialTechniques[(int)sampling.Taps]
            : _radialSwirlTechniques[(int)sampling.Taps];

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
        //
        // Both axes, not just the divisor: SampleRadialSwirl divides by this, where a zero y is an
        // Inf of its own and then a NaN against an on-centre pixel's zero offset.
        bool degenerateViewport = screenSize.X <= 0f || screenSize.Y <= 0f;

        AspectScale.SetValue(new Vector2(1f, degenerateViewport ? 1f : screenSize.Y / screenSize.X));
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
        SampleSwirl.SetValue(sampling.Swirl);

        // Scales the centre ray, which already carries SceneScale, so it needs no mapping of its
        // own - and must stay isotropic or the split stops following the ray.
        SampleAberration.SetValue(new Vector2(sampling.Aberration, sampling.Aberration));

        SaturationAmount.SetValue(sampling.Saturation);
    }
}
