// AMD FidelityFX Super Resolution 1.0 - EASU (Edge Adaptive Spatial Upsampling)
// Ported to Shader Model 3.0 / fx_2_0 for the FNA post-processing pipeline.
// Reference: AMD FidelityFX-FSR (ffx_fsr1.h), MIT licensed.
//
// This is the single-pass spatial upscaler stage of FSR 1.0. It replaces the
// world render-target -> screen composite blit, giving an edge aware upscale
// (and, when the world target matches the screen, an edge aware sharpen).

float2 textureSize;
float4x4 MatrixTransform;

sampler decal : register(s0);

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
    float3 TexCoord : TEXCOORD0;
    float3 Normal   : TEXCOORD1;
    float3 Hue      : TEXCOORD2;
};

PS_INPUT main_vertex(VS_INPUT IN)
{
    PS_INPUT OUT;

    float2 ps = 1.0 / textureSize;

    OUT.Position = mul(IN.Position, MatrixTransform);
    OUT.TexCoord = IN.TexCoord;
    OUT.Normal = float3(ps.x, ps.y, 0);
    OUT.Hue = IN.Hue;

    return OUT;
}

float easu_luma(float3 c)
{
    return dot(c, float3(0.299, 0.587, 0.114));
}

// FSR EASU direction/length analysis for one bilinear quad corner.
void easu_set(inout float2 dir, inout float len, float w,
              float lA, float lB, float lC, float lD, float lE)
{
    // Horizontal edge (uses left/center/right = lB/lC/lD).
    float dc = lD - lC;
    float cb = lC - lB;
    float lenX = max(abs(dc), abs(cb));
    lenX = 1.0 / max(lenX, 1.0 / 32768.0);
    float dirX = lD - lB;
    dir.x += dirX * w * lenX;
    lenX = saturate(abs(dirX) * lenX);
    lenX *= lenX;
    len += lenX * w;

    // Vertical edge (uses up/center/down = lA/lC/lE).
    float ec = lE - lC;
    float ca = lC - lA;
    float lenY = max(abs(ec), abs(ca));
    lenY = 1.0 / max(lenY, 1.0 / 32768.0);
    float dirY = lE - lA;
    dir.y += dirY * w * lenY;
    lenY = saturate(abs(dirY) * lenY);
    lenY *= lenY;
    len += lenY * w;
}

// FSR EASU anisotropic Lanczos(2) tap weight/accumulate.
void easu_tap(inout float3 aC, inout float aW, float2 off, float2 dir,
              float2 len2, float lob, float clp, float3 c)
{
    float2 v;
    v.x = off.x * dir.x + off.y * dir.y;
    v.y = off.x * -dir.y + off.y * dir.x;
    v *= len2;
    float d2 = v.x * v.x + v.y * v.y;
    d2 = min(d2, clp);
    float wB = (2.0 / 5.0) * d2 - 1.0;
    float wA = lob * d2 - 1.0;
    wB *= wB;
    wA *= wA;
    wB = 1.5625 * wB - 0.5625;
    float w = wB * wA;
    aC += c * w;
    aW += w;
}

float4 main_fragment(PS_INPUT IN) : COLOR0
{
    float2 ps = IN.Normal.xy;       // 1 / textureSize
    float2 pp = IN.TexCoord.xy / ps; // source position in texels

    // Locate the 2x2 quad (f g / j k) that the output pixel falls inside and the
    // fractional position within it (texel centers sit at integer + 0.5).
    float2 ppc = pp - 0.5;
    float2 fp = floor(ppc);
    float2 sub = ppc - fp;
    float2 baseUV = (fp + 0.5) * ps;

    // 12-tap neighborhood around f (offset 0,0):
    //          b( 0,-1) c( 1,-1)
    //  e(-1,0) f( 0, 0) g( 1, 0) h( 2,0)
    //  i(-1,1) j( 0, 1) k( 1, 1) l( 2,1)
    //          n( 0, 2) o( 1, 2)
    float3 bC = tex2D(decal, baseUV + float2( 0, -1) * ps).rgb;
    float3 cC = tex2D(decal, baseUV + float2( 1, -1) * ps).rgb;
    float3 eC = tex2D(decal, baseUV + float2(-1,  0) * ps).rgb;
    float3 fC = tex2D(decal, baseUV + float2( 0,  0) * ps).rgb;
    float3 gC = tex2D(decal, baseUV + float2( 1,  0) * ps).rgb;
    float3 hC = tex2D(decal, baseUV + float2( 2,  0) * ps).rgb;
    float3 iC = tex2D(decal, baseUV + float2(-1,  1) * ps).rgb;
    float3 jC = tex2D(decal, baseUV + float2( 0,  1) * ps).rgb;
    float3 kC = tex2D(decal, baseUV + float2( 1,  1) * ps).rgb;
    float3 lC = tex2D(decal, baseUV + float2( 2,  1) * ps).rgb;
    float3 nC = tex2D(decal, baseUV + float2( 0,  2) * ps).rgb;
    float3 oC = tex2D(decal, baseUV + float2( 1,  2) * ps).rgb;

    float bL = easu_luma(bC);
    float cL = easu_luma(cC);
    float eL = easu_luma(eC);
    float fL = easu_luma(fC);
    float gL = easu_luma(gC);
    float hL = easu_luma(hC);
    float iL = easu_luma(iC);
    float jL = easu_luma(jC);
    float kL = easu_luma(kC);
    float lL = easu_luma(lC);
    float nL = easu_luma(nC);
    float oL = easu_luma(oC);

    // Bilinear weights of the four quad corners.
    float wf = (1.0 - sub.x) * (1.0 - sub.y);
    float wg = sub.x * (1.0 - sub.y);
    float wj = (1.0 - sub.x) * sub.y;
    float wk = sub.x * sub.y;

    float2 dir = float2(0.0, 0.0);
    float len = 0.0;
    easu_set(dir, len, wf, bL, eL, fL, gL, jL); // corner f
    easu_set(dir, len, wg, cL, fL, gL, hL, kL); // corner g
    easu_set(dir, len, wj, fL, iL, jL, kL, nL); // corner j
    easu_set(dir, len, wk, gL, jL, kL, lL, oL); // corner k

    // Normalize direction, derive anisotropic length and Lanczos lobe.
    float dirR = dir.x * dir.x + dir.y * dir.y;
    bool zro = dirR < (1.0 / 32768.0);
    dirR = rsqrt(max(dirR, 1.0 / 32768.0));
    dir = zro ? float2(1.0, 0.0) : dir * dirR;

    len = len * 0.5;
    len *= len;

    float stretch = (dir.x * dir.x + dir.y * dir.y) / max(abs(dir.x), abs(dir.y));
    float2 len2 = float2(1.0 + (stretch - 1.0) * len, 1.0 - 0.5 * len);
    float lob = 0.5 - 0.29 * len;
    float clp = 1.0 / lob;

    float3 aC = float3(0.0, 0.0, 0.0);
    float aW = 0.0;
    easu_tap(aC, aW, float2( 0, -1) - sub, dir, len2, lob, clp, bC);
    easu_tap(aC, aW, float2( 1, -1) - sub, dir, len2, lob, clp, cC);
    easu_tap(aC, aW, float2(-1,  1) - sub, dir, len2, lob, clp, iC);
    easu_tap(aC, aW, float2( 0,  1) - sub, dir, len2, lob, clp, jC);
    easu_tap(aC, aW, float2( 0,  0) - sub, dir, len2, lob, clp, fC);
    easu_tap(aC, aW, float2(-1,  0) - sub, dir, len2, lob, clp, eC);
    easu_tap(aC, aW, float2( 1,  1) - sub, dir, len2, lob, clp, kC);
    easu_tap(aC, aW, float2( 2,  1) - sub, dir, len2, lob, clp, lC);
    easu_tap(aC, aW, float2( 2,  0) - sub, dir, len2, lob, clp, hC);
    easu_tap(aC, aW, float2( 0,  2) - sub, dir, len2, lob, clp, nC);
    easu_tap(aC, aW, float2( 1,  2) - sub, dir, len2, lob, clp, oC);
    easu_tap(aC, aW, float2( 1,  0) - sub, dir, len2, lob, clp, gC);

    float3 pix = aC / aW;

    // Deringing: clamp to the range of the nearest 2x2 quad.
    float3 min4 = min(min(fC, gC), min(jC, kC));
    float3 max4 = max(max(fC, gC), max(jC, kC));
    pix = min(max4, max(min4, pix));

    return float4(pix, 1.0);
}

technique T0
{
    pass P0
    {
        VertexShader = compile vs_3_0 main_vertex();
        PixelShader = compile ps_3_0 main_fragment();
    }
}
