float4x4 MatrixTransform;

sampler NoiseSampler : register(s0);

// ---- Shape: where on screen the effect lives -------------------------------
float2 Center;        // vignette centre in screen uv; (0.5, 0.5) is the middle
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
float  WarpStrength;  // domain warp; the gas-vs-fluid dial
float  RidgeAmount;   // 0 = billowy fbm, 1 = sharp ridges/cracks
float  Threshold;     // how much of the field survives; higher = sparser
float  Softness;      // hardness of the surviving field's edges
float  FlatFloor;     // solid fill under the noise; 1 = flat colour, 0 = fully noise-driven

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

// How deep into the shape a pixel sits: rises toward the screen edge in border mode, toward the
// shape's outer ring in radial mode. No texture fetches, so it is cheap enough to gate the
// early-out below.
float ShapeDistance(float2 uv)
{
    float2 offset = (uv - Center) * AspectScale;
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

    float2 dir = normalize((uv - Center) * AspectScale + 0.00001);
    float lobe = saturate(dot(dir, FocusDir) * 0.5 + 0.5);
    return mask * lerp(1.0, pow(lobe, FocusPower), FocusAmount);
}

// Two scrolling samples of the tiling noise texture, the second one domain-warped
// by the first. This is what produces motion that never visibly repeats. Returned
// before thresholding: the shape boundary needs the smooth field, and only the
// alpha wants the hard-edged one.
float NoiseField(float2 uv)
{
    float b = dot(tex2D(NoiseSampler, uv * BaseScale + Time * BaseScroll), BaseChannel);

    float2 warped = uv * DetailScale + Time * DetailScroll + (b - 0.5) * WarpStrength;
    float d = dot(tex2D(NoiseSampler, warped), DetailChannel);

    float n = b * 0.55 + d * 0.45;

    float ridge = 1.0 - abs(n * 2.0 - 1.0);
    return lerp(n, ridge * ridge, RidgeAmount);
}

float4 main_fragment(PS_INPUT IN) : COLOR0
{
    float shape = ShapeDistance(IN.TexCoord);

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
    float jitter = dot(tex2D(NoiseSampler, IN.TexCoord * JitterScale + Time * JitterScroll), JitterChannel);
    float flux = (jitter - 0.5) * 2.0;

    // The same field also stretches and compresses the falloff, so the effect does not merely reach
    // a varying distance behind a gradient of fixed length - the gradient itself is longer where it
    // reaches further. That correlation is what makes a deep run taper away and a shallow one end
    // bluntly, instead of every column sharing one profile at a different offset.
    float feather = max(Feather * (1.0 + flux * JitterFeather), 0.01);

    float mask = ShapeMask(IN.TexCoord, shape + flux * 0.5 * JitterReach, feather);
    clip(mask - 0.002);

    float n = NoiseField(IN.TexCoord);
    float shaped = smoothstep(Threshold - Softness, Threshold + Softness, n);
    float field = lerp(shaped, 1.0, FlatFloor);
    float pulse = 1.0 + PulseAmp * sin(Time * PulseFreq * 6.28318530718); // 2 pi

    float alpha = saturate(mask * field * Opacity * Intensity * pulse);
    return float4(Tint, alpha);
}

technique T0
{
    pass P0
    {
        VertexShader = compile vs_3_0 main_vertex();
        PixelShader = compile ps_3_0 main_fragment();
    }
}
