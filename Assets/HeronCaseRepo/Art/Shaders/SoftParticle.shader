Shader "HeronCase/SoftParticle"
{
    // Round particle with a soft edge, colored by the particle color. Used for the splash foam and droplets;
    // _Droplet shades it as a small lit drop of water, and _Foam as a clump of bubbles that erodes away with the
    // particle alpha (the per-particle random comes in TEXCOORD0.z, see PourStreamView).
    // _Star turns it into a four-point twinkle, _Ring into a ring of light, _Rays into a sunburst, _Twinkle makes it
    // flicker at its own pace, and _DstBlend One makes it additive for the solved celebration. The sunburst and the
    // twinkle read the per-particle random from TEXCOORD0.z as well.
    Properties
    {
        _Softness ("Edge Softness", Range(0.01,1)) = 0.6
        _Core ("Bright Core", Range(0,1)) = 0.0
        _Star ("Star Rays", Range(0,1)) = 0.0
        _Ring ("Ring", Range(0,1)) = 0.0
        _RingWidth ("Ring Width", Range(0.01,0.5)) = 0.1
        _Rays ("Sunburst", Range(0,1)) = 0.0
        _RayCount ("Sunburst Ray Count", Float) = 28
        _Twinkle ("Twinkle", Range(0,1)) = 0.0
        _Droplet ("Water Drop Shading", Range(0,1)) = 0.0
        _Foam ("Eroding Foam", Range(0,1)) = 0.0
        _FoamScale ("Foam Bubbles Per Half Height", Float) = 1.3
        _FoamOpacity ("Foam Opacity", Range(0,1)) = 0.7
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 10
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
        }

        Blend SrcAlpha [_DstBlend]
        Cull Off
        ZWrite Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 uv         : TEXCOORD0;
            float4 color      : COLOR;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 uv         : TEXCOORD0;
            half4  color      : COLOR;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        CBUFFER_START(UnityPerMaterial)
            half _Softness;
            half _Core;
            half _Star;
            half _Ring;
            half _RingWidth;
            half _Rays;
            float _RayCount;
            half _Twinkle;
            half _Droplet;
            half _Foam;
            float _FoamScale;
            half _FoamOpacity;
            half _DstBlend;
        CBUFFER_END

        float2 Hash22(float2 p)
        {
            p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
            return frac(sin(p) * 43758.5453);
        }

        // distance to the nearest point of a jittered grid: low in round cells, like a clump of bubbles
        float Cells(float2 p)
        {
            float2 i = floor(p);
            float2 f = frac(p);
            float d = 1.0;
            for (int y = -1; y <= 1; y++)
            {
                for (int x = -1; x <= 1; x++)
                {
                    float2 o = float2(x, y);
                    float2 c = o + Hash22(i + o) * 0.8 + 0.1 - f;
                    d = min(d, dot(c, c));
                }
            }
            return sqrt(d);
        }

        Varyings vert(Attributes IN)
        {
            Varyings OUT;
            UNITY_SETUP_INSTANCE_ID(IN);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
            OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
            OUT.uv = IN.uv;
            OUT.color = IN.color;
            return OUT;
        }

        half4 frag(Varyings IN) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

            float2 p = IN.uv.xy * 2.0 - 1.0;
            float r = length(p);
            float aa = max(fwidth(r), 1e-4);
            half a = 1.0 - smoothstep(1.0 - _Softness - aa, 1.0, r);

            // thin rays along both axes that taper toward the tips, around a small glowing center
            float2 q = abs(p);
            float rays = saturate(1.0 - q.x * q.y * 60.0) * saturate(1.0 - r);
            float glow = exp(-r * r * 18.0);
            half star = saturate(rays * rays + glow);
            a = lerp(a, star, _Star);

            // a thin band just inside the edge, brighter and dimmer around the circle in a pattern set by the
            // particle's random, so it reads as a wave of light rather than a drawn circle; whole frequencies keep
            // it seamless where atan2 wraps
            float edge = 1.0 - _RingWidth;
            half ring = 1.0 - smoothstep(0.0, _RingWidth, abs(r - edge));
            float around = atan2(p.y, p.x);
            float wave = 0.5 + 0.3 * sin(around * 3.0 + IN.uv.z * 40.0) + 0.2 * sin(around * 7.0 + IN.uv.z * 90.0);
            a = lerp(a, ring * ring * lerp(0.15, 1.0, saturate(wave)), _Ring);

            // thin rays around a soft glow, each with its own angle jitter, width, length, brightness and shimmer
            // hashed from its index and the particle's random, so no two bursts or layers match. The ray count must
            // be whole so the rays meet where atan2 wraps; the neighbours are summed so a wide ray never clips.
            UNITY_BRANCH
            if (_Rays > 0.0)
            {
                float seed = IN.uv.z * 91.7;
                float t = (atan2(p.y, p.x) / 6.2831853 + 0.5) * _RayCount;
                float cell = floor(t);
                float sun = 0.0;
                for (int k = -1; k <= 1; k++)
                {
                    float c = cell + k;
                    float id = fmod(c + _RayCount, _RayCount);
                    float2 h1 = Hash22(float2(id, seed));
                    float2 h2 = Hash22(float2(id + 17.3, seed + 5.1));
                    float d = (t - c - 0.5 - (h1.x - 0.5) * 0.7) / lerp(0.04, 0.17, h1.y * h1.y);
                    float len = lerp(0.3, 1.0, h2.x);
                    float shimmer = 0.7 + 0.3 * sin(_Time.y * lerp(1.5, 4.0, h2.y) + h1.x * 40.0);
                    sun += exp(-d * d) * pow(saturate(1.0 - r / len), 1.5) * lerp(0.15, 1.0, h2.y * h2.y) * shimmer;
                }
                // thin rays and a tight glow keep the night dark between them instead of a grey haze
                sun *= smoothstep(0.03, 0.2, r);
                sun += exp(-r * r * 18.0) * 0.55;
                a = lerp(a, saturate(sun), _Rays);
            }

            half core = lerp(saturate(1.0 - r * 2.0), glow, _Star);
            half3 rgb = lerp(IN.color.rgb, half3(1, 1, 1), _Core * core);

            // a lit drop of water: deeper color toward the rim and a small highlight toward the upper left
            float2 hp = p - float2(-0.3, 0.35);
            half3 drop = saturate(IN.color.rgb * lerp(1.15, 0.65, r * r));
            drop = lerp(drop, half3(1, 1, 1), exp(-dot(hp, hp) * 20.0) * 0.85);
            rgb = lerp(rgb, drop, _Droplet);
            half alpha = a * IN.color.a;

            // foam: the falling alpha raises a cut-off through a bubbly field, so the clump breaks into smaller
            // bubbles and pops instead of fading out like smoke; the cells stay round on a stretched quad
            float aspect = fwidth(IN.uv.y) / max(fwidth(IN.uv.x), 1e-6);
            float cells = Cells(p * float2(aspect, 1.0) * _FoamScale + IN.uv.z * 61.0);
            float field = (1.0 - r) * 0.8 + (0.5 - cells) * 0.5;
            float erode = 1.0 - IN.color.a;
            float fw = max(fwidth(field), 1e-4);
            half foamA = smoothstep(erode - fw, erode + fw, field) * _FoamOpacity;
            // lighter inside, with the poured color showing toward the eroding edges
            half3 foamRgb = lerp(IN.color.rgb * 0.85, lerp(IN.color.rgb, half3(1, 1, 1), 0.3), smoothstep(erode, erode + 0.3, field));
            rgb = lerp(rgb, foamRgb, _Foam);
            alpha = lerp(alpha, foamA, _Foam);

            // each particle flickers at its own rate and phase
            half flicker = 0.5 + 0.5 * sin(_Time.y * lerp(9.0, 20.0, IN.uv.z) + IN.uv.z * 60.0);
            alpha *= lerp(1.0, 0.3 + 0.7 * flicker * flicker, _Twinkle);
            return half4(rgb, alpha);
        }
        ENDHLSL

        Pass
        {
            Name "Universal2D"
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            ENDHLSL
        }

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            ENDHLSL
        }
    }
}
