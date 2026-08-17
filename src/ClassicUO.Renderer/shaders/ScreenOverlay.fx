float4x4 MatrixTransform;

sampler NoiseSampler : register(s0);

// The frame as it stood before this overlay pass. Bound only for sampling layers; the tint
// technique never reads it. Slot 3 because slots 1 and 2 hold the hue lookup tables, which are
// bound once at startup for the whole session.
sampler SceneSampler : register(s3);

// ---- Shape: where on screen the effect lives -------------------------------
float2 Center;        // vignette centre in screen uv; (0.5, 0.5) is the middle
float2 WobbleFreq;     // rate Center drifts at, in Hz per axis; 0 holds that axis still
float  WobbleAmp;      // peak drift of Center, in screen uv
float2 AspectScale;   // (1, height/width); keeps the radial falloff circular
float  Reach;         // how far in from the edge the effect extends; larger = thicker
float  Feather;       // width of the falloff band behind the boundary
float  EdgeBlend;     // 0 = radial vignette, 1 = border trim
float  CornerBias;    // border trim only: 0 = square corners, 1 = strongly corner-weighted
float  JitterReach;   // noise displacement of the boundary; 0 = straight, unbroken iso-line
float  JitterFeather; // noise modulation of the falloff width; 0 = same gradient length everywhere
float2 JitterScale;   // frequency of both; low values = long, gradual variation
float2 JitterScroll;
float4 JitterChannel;
float2 FocusDir;      // unit vector biasing the effect toward one side or corner
float  FocusPower;    // higher = tighter lobe
float  FocusAmount;   // 0 = uniform ring, 1 = fully directional

// ---- Noise: how it moves ---------------------------------------------------
float  Time;          // seconds; wrapped by the CPU
float2 BaseScale;     // frequency of the primary field; x/y ratio is the streak anisotropy
float2 BaseScroll;
float4 BaseChannel;   // selects which packed noise channel the primary field reads
float2 DetailScale;   // frequency of the secondary field, warped by the primary
float2 DetailScroll;
float4 DetailChannel;
float2 NoiseOffset;   // static texture-space shift of both fields; desyncs layers sharing a scroll
float  WarpStrength;  // domain warp; the gas-vs-fluid dial
float  RidgeAmount;   // 0 = billowy fbm, 1 = sharp ridges/cracks
float  Threshold;     // how much of the field survives; higher = sparser
float  Softness;      // hardness of the surviving field's edges
float  FlatFloor;     // solid fill under the noise; 1 = flat colour, 0 = fully noise-driven

// ---- Sampling: distortion of what is already on screen ---------------------
// All offsets are in scene-texture uv and are pre-multiplied by SceneScale on the CPU, so nothing
// here has to know where the quad sits inside the scene texture.
float2 SceneOffset;      // uv of the quad's origin within the scene texture
float2 SceneScale;       // uv size of the quad within the scene texture
float2 SampleRadius;     // disk radius for the blur, aspect-corrected so it stays circular
float  SampleZoom;       // radial blur: how far along the centre ray the taps march
float2 SampleAberration; // chromatic: red/blue separation along the centre ray

// ---- Appearance ------------------------------------------------------------
float3 Tint;
float  Opacity;
float  Intensity;
float  PulseFreq;
float  PulseAmp;
struct VS_INPUT
{
    float4 Position : POSITION0;
    float3 Normal   : NORMAL0;
    float3 TexCoord : TEXCOORD0;
    float3 Hue      : TEXCOORD1;
};

struct PS_INPUT
{
    float4 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
};

PS_INPUT main_vertex(VS_INPUT IN)
{
    PS_INPUT OUT;
    OUT.Position = mul(IN.Position, MatrixTransform);
    OUT.TexCoord = IN.TexCoord.xy;
    return OUT;
}

// Center, drifted by a Lissajous wander when WobbleFreq/WobbleAmp ask for one. Uniform per draw
// call - both axes read the same Time every pixel - so this costs two sin evaluations for the
// whole layer, not per pixel. Different X/Y phase (the 1.7 offset) keeps the wander from
// collapsing onto a straight line when both frequencies happen to match.
float2 EffectiveCenter()
{
    float2 drift = float2(
        sin(Time * WobbleFreq.x * 6.28318530718),
        cos(Time * WobbleFreq.y * 6.28318530718 + 1.7)
    );

    return Center + drift * WobbleAmp;
}

// How deep into the shape a pixel sits: rises toward the screen edge in border mode, toward the
// shape's outer ring in radial mode. No texture fetches, so it is cheap enough to gate the
// early-out below.
float ShapeDistance(float2 uv)
{
    float2 offset = (uv - EffectiveCenter()) * AspectScale;
    float radial = length(offset) * 2.0;

    // Per-axis proximity to the nearest edge: 1 at the edge, 0 at the middle of that axis.
    float2 near = saturate(1.0 - min(uv, 1.0 - uv) * 2.0);

    // A p-norm rather than max(). max() is a rectangle - uniform thickness the whole way round and
    // sharp corners - while a lower exponent lets both axes contribute near a corner, so the trim
    // thickens there. Unlike mixing the radial term in to get the same effect, this is measured per
    // axis and so carries no aspect-ratio bias: on a widescreen display the radial distance to the
    // top edge is much shorter than to the left edge, which weights any radial blend toward the
    // left and right sides.
    float power = lerp(8.0, 2.0, CornerBias);
    float border = pow(pow(near.x, power) + pow(near.y, power), 1.0 / power);

    return lerp(radial, border, EdgeBlend);
}

// Returns 0 where the effect is absent, 1 where it is at full strength. Takes an already-displaced
// shape distance and an already-modulated falloff width, so the caller decides both how ragged the
// boundary is and how long the gradient behind it runs.
float ShapeMask(float2 uv, float shape, float feather)
{
    // Reach is expressed as "how far in the effect extends" so callers are not asked to think
    // backwards; the mask itself needs the distance at which it starts, which is the complement.
    float start = 1.0 - Reach;
    float mask = smoothstep(start, start + feather, shape);

    float2 dir = normalize((uv - EffectiveCenter()) * AspectScale + 0.00001);
    float lobe = saturate(dot(dir, FocusDir) * 0.5 + 0.5);
    return mask * lerp(1.0, pow(lobe, FocusPower), FocusAmount);
}

// Two scrolling samples of the tiling noise texture, the second one domain-warped
// by the first. This is what produces motion that never visibly repeats. Returned
// before thresholding: the shape boundary needs the smooth field, and only the
// alpha wants the hard-edged one.
float NoiseField(float2 uv)
{
    float b = dot(tex2D(NoiseSampler, uv * BaseScale + Time * BaseScroll + NoiseOffset), BaseChannel);

    float2 warped = uv * DetailScale + Time * DetailScroll + NoiseOffset + (b - 0.5) * WarpStrength;
    float d = dot(tex2D(NoiseSampler, warped), DetailChannel);

    float n = b * 0.55 + d * 0.45;

    float ridge = 1.0 - abs(n * 2.0 - 1.0);
    return lerp(n, ridge * ridge, RidgeAmount);
}

// ---- Sampling helpers ------------------------------------------------------

float2 SceneUv(float2 uv)
{
    return SceneOffset + uv * SceneScale;
}

// The ray from the shape centre out to this pixel, in scene-texture uv. Both the radial blur and
// the chromatic split march along it, which is what makes them read as lens artefacts rather than
// as a screen-space smear.
float2 CentreRay(float2 uv)
{
    return (uv - EffectiveCenter()) * SceneScale;
}

// Golden-angle spiral over a disk: sqrt(t) keeps the points area-uniform rather than bunched at the
// centre, and the irrational angle step means no tap count lands on a visible rosette. Weighted
// down toward the rim so the result reads as a soft blur instead of a hard-edged smear of copies.
//
// taps must be a literal at every call site. HLSL inlines these, so a literal makes the bound a
// compile-time constant and [unroll] turns the loop into straight-line instructions. Passing a
// uniform instead compiles to rep/break_lt, which does not survive translation to GL - the loop
// exits immediately, leaving only the centre sample, which is the untouched frame drawn over
// itself. That failure is completely invisible, which is why the tap count is baked per technique.
float3 SampleDisk(float2 uv, int taps)
{
    float2 base = SceneUv(uv);
    float3 sum = tex2D(SceneSampler, base).rgb;
    float total = 1.0;

    [unroll]
    for (int i = 1; i <= taps; i++)
    {
        float t = (float)i / (float)taps;
        float angle = (float)i * 2.39996323;
        float2 dir = float2(cos(angle), sin(angle)) * sqrt(t);
        float weight = 1.0 - t * 0.5;

        sum += tex2Dlod(SceneSampler, float4(base + dir * SampleRadius, 0, 0)).rgb * weight;
        total += weight;
    }

    return sum / total;
}

// Taps march back along the centre ray, so the streaks converge on Center. Zoom blur, and the one
// that reads as head-spin rather than as out-of-focus. taps is a literal for the same reason as in
// SampleDisk.
float3 SampleRadial(float2 uv, int taps)
{
    float2 base = SceneUv(uv);
    float2 ray = CentreRay(uv) * SampleZoom;
    float3 sum = tex2D(SceneSampler, base).rgb;

    [unroll]
    for (int i = 1; i <= taps; i++)
    {
        float t = (float)i / (float)taps;
        sum += tex2Dlod(SceneSampler, float4(base - ray * t, 0, 0)).rgb;
    }

    return sum / (float)(taps + 1);
}

// Red and blue pulled apart along the centre ray, green left where it is. Splitting radially rather
// than along a fixed axis is what makes it look like a lens: nothing separates at the centre and the
// fringing grows toward the corners.
float3 SampleChromatic(float2 uv)
{
    float2 base = SceneUv(uv);
    float2 split = CentreRay(uv) * SampleAberration;

    return float3(
        tex2D(SceneSampler, base + split).r,
        tex2D(SceneSampler, base).g,
        tex2D(SceneSampler, base - split).b
    );
}

// Everything the shape and noise machinery contributes, shared by every technique: 0 where the
// effect is absent, up to Opacity where it is fully present. Clips rather than returning 0 so a
// pixel outside the shape never reaches a texture fetch.
float OverlayAlpha(float2 uv)
{
    float shape = ShapeDistance(uv);

    // Conservative early-out ahead of the texture fetches: the displacement below is bounded by
    // +/- JitterReach * 0.5, so nothing past that margin can survive the real mask test.
    clip(shape - (1.0 - Reach) + JitterReach * 0.5 + 0.002);

    // Displacing the boundary is what stops the effect terminating along a straight line. The mask
    // depends only on distance to the nearest edge, so without this every column of a border-shaped
    // overlay ends at exactly the same depth and reads as a rectangle.
    //
    // This is a separate, deliberately lower-frequency field from the one that drives alpha below.
    // Reusing the detail noise makes the boundary buzz at the same rate as the texture; a slow field
    // instead makes some columns reach much deeper than their neighbours and hold it, which is what
    // a run of fluid actually does.
    float jitter = dot(tex2D(NoiseSampler, uv * JitterScale + Time * JitterScroll), JitterChannel);
    float flux = (jitter - 0.5) * 2.0;

    // The same field also stretches and compresses the falloff, so the effect does not merely reach
    // a varying distance behind a gradient of fixed length - the gradient itself is longer where it
    // reaches further. That correlation is what makes a deep run taper away and a shallow one end
    // bluntly, instead of every column sharing one profile at a different offset.
    float feather = max(Feather * (1.0 + flux * JitterFeather), 0.01);

    float mask = ShapeMask(uv, shape + flux * 0.5 * JitterReach, feather);
    clip(mask - 0.002);

    float n = NoiseField(uv);
    float shaped = smoothstep(Threshold - Softness, Threshold + Softness, n);
    float field = lerp(shaped, 1.0, FlatFloor);
    float pulse = 1.0 + PulseAmp * sin(Time * PulseFreq * 6.28318530718); // 2 pi

    return saturate(mask * field * Opacity * Intensity * pulse);
}

float4 main_fragment(PS_INPUT IN) : COLOR0
{
    return float4(Tint, OverlayAlpha(IN.TexCoord));
}

// The sampling techniques all return the distorted scene at the layer's own alpha. Straight-alpha
// blending then resolves to lerp(sharp, distorted, alpha) against the frame already on screen, so
// the shape mask doubles as the strength of the distortion at no extra cost.
//
// One technique per tap count, stamped out below. The count cannot be a uniform (see SampleDisk),
// so the caller picks the technique matching the quality it wants and pays exactly that many taps.
#define BLUR_FRAGMENT(name, taps)                                   \
    float4 name(PS_INPUT IN) : COLOR0                               \
    {                                                               \
        float alpha = OverlayAlpha(IN.TexCoord);                    \
        return float4(SampleDisk(IN.TexCoord, taps), alpha);        \
    }

#define RADIAL_FRAGMENT(name, taps)                                 \
    float4 name(PS_INPUT IN) : COLOR0                               \
    {                                                               \
        float alpha = OverlayAlpha(IN.TexCoord);                    \
        return float4(SampleRadial(IN.TexCoord, taps), alpha);      \
    }

BLUR_FRAGMENT(blur4_fragment, 4)
BLUR_FRAGMENT(blur8_fragment, 8)
BLUR_FRAGMENT(blur12_fragment, 12)
BLUR_FRAGMENT(blur16_fragment, 16)

RADIAL_FRAGMENT(radial4_fragment, 4)
RADIAL_FRAGMENT(radial8_fragment, 8)
RADIAL_FRAGMENT(radial12_fragment, 12)
RADIAL_FRAGMENT(radial16_fragment, 16)

float4 chromatic_fragment(PS_INPUT IN) : COLOR0
{
    float alpha = OverlayAlpha(IN.TexCoord);
    return float4(SampleChromatic(IN.TexCoord), alpha);
}

technique T0
{
    pass P0
    {
        VertexShader = compile vs_3_0 main_vertex();
        PixelShader = compile ps_3_0 main_fragment();
    }
}

#define SAMPLING_TECHNIQUE(name, fragment) \
    technique name                         \
    {                                      \
        pass P0                            \
        {                                  \
            VertexShader = compile vs_3_0 main_vertex(); \
            PixelShader = compile ps_3_0 fragment();     \
        }                                  \
    }

SAMPLING_TECHNIQUE(Blur4, blur4_fragment)
SAMPLING_TECHNIQUE(Blur8, blur8_fragment)
SAMPLING_TECHNIQUE(Blur12, blur12_fragment)
SAMPLING_TECHNIQUE(Blur16, blur16_fragment)

SAMPLING_TECHNIQUE(Radial4, radial4_fragment)
SAMPLING_TECHNIQUE(Radial8, radial8_fragment)
SAMPLING_TECHNIQUE(Radial12, radial12_fragment)
SAMPLING_TECHNIQUE(Radial16, radial16_fragment)

technique Chromatic
{
    pass P0
    {
        VertexShader = compile vs_3_0 main_vertex();
        PixelShader = compile ps_3_0 chromatic_fragment();
    }
}
