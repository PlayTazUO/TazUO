float4x4 MatrixTransform;

sampler NoiseSampler : register(s0);

// ---- Shape: where on screen the effect lives -------------------------------
float2 Center;        // vignette centre in screen uv; (0.5, 0.5) is the middle
float2 AspectScale;   // (1, height/width); keeps the radial falloff circular
float  Radius;        // normalised distance at which the effect begins
float  Feather;       // width of the falloff band
float  EdgeBlend;     // 0 = radial vignette, 1 = rectangular border trim
float2 FocusDir;      // unit vector biasing the effect toward one side or corner
float  FocusPower;    // higher = tighter lobe
float  FocusAmount;   // 0 = uniform ring, 1 = fully directional

// ---- Noise: how it moves ---------------------------------------------------
float  Time;          // seconds; wrapped by the CPU, see note below
float2 Scale0;
float2 Scale1;
float2 Scroll0;
float2 Scroll1;
float4 Channel0;      // selects which packed noise channel layer 0 reads
float4 Channel1;
float  WarpStrength;  // domain warp; the gas-vs-fluid dial
float  RidgeAmount;   // 0 = billowy fbm, 1 = sharp ridges/cracks
float  Threshold;
float  Softness;
float  NoiseAmount;   // 0 = flat colour (tunnel vision), 1 = fully noise-driven

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

// Returns 0 where the effect is absent, 1 where it is at full strength.
float ShapeMask(float2 uv)
{
    float2 offset = (uv - Center) * AspectScale;
    float radial = length(offset) * 2.0;

    float2 edge = min(uv, 1.0 - uv);
    float border = 1.0 - min(edge.x, edge.y) * 2.0;

    float shape = lerp(radial, border, EdgeBlend);
    float mask = smoothstep(Radius, Radius + Feather, shape);

    float2 dir = normalize(offset + 0.00001);
    float lobe = saturate(dot(dir, FocusDir) * 0.5 + 0.5);
    return mask * lerp(1.0, pow(lobe, FocusPower), FocusAmount);
}

// Two scrolling samples of the tiling noise texture, the second one domain-warped
// by the first. This is what produces motion that never visibly repeats.
float NoiseField(float2 uv)
{
    float n0 = dot(tex2D(NoiseSampler, uv * Scale0 + Time * Scroll0), Channel0);

    float2 warped = uv * Scale1 + Time * Scroll1 + (n0 - 0.5) * WarpStrength;
    float n1 = dot(tex2D(NoiseSampler, warped), Channel1);

    float n = n0 * 0.55 + n1 * 0.45;

    float ridge = 1.0 - abs(n * 2.0 - 1.0);
    n = lerp(n, ridge * ridge, RidgeAmount);

    return smoothstep(Threshold - Softness, Threshold + Softness, n);
}

float4 main_fragment(PS_INPUT IN) : COLOR0
{
    float mask = ShapeMask(IN.TexCoord);
    clip(mask - 0.002);

    float field = lerp(1.0, NoiseField(IN.TexCoord), NoiseAmount);
    float pulse = 1.0 + PulseAmp * sin(Time * PulseFreq * 6.28318530718);

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
