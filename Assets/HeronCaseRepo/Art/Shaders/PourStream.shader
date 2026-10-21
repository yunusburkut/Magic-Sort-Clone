Shader "HeronCase/PourStream"
{
    // Ribbon mesh built by PourStreamView along the falling stream.
    //   uv0.x  position across the ribbon, the liquid edge is at |x| = 1
    //   uv1    (distance from the lip, distance to the head) in world units
    //   uv2    (half width, distance from the tail) in world units
    //   color  liquid color, alpha = overall opacity
    // _Caps (per renderer): (round tail, round head, fade head into the target liquid)
    Properties
    {
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Shape)]
        _Wobble ("Edge Wobble", Range(0,0.5)) = 0.12
        _Sway ("Center Sway", Range(0,0.3)) = 0.05
        _WobbleFreq ("Wobble Frequency (per unit)", Float) = 2.2
        _FlowSpeed ("Flow Speed (units/s)", Float) = 7
        _SettleLength ("Smooth Length At Lip", Float) = 0.6
        _HeadFadeLength ("Head Fade Length", Float) = 0.18

        [Header(Casual Color)]
        _Saturation ("Saturation", Range(0,2)) = 1
        _Brightness ("Brightness", Range(0.5,2)) = 1.05
        _EdgeDarken ("Edge Darken", Range(0,1)) = 0.18
        _Lighten ("Lighten (matches liquid seen through the glass, linear)", Range(0,1)) = 0.13

        [Header(Highlights)]
        _ShineColor ("Shine Color", Color) = (1,1,1,1)
        _ShinePos ("Shine Position (-1..1)", Range(-1,1)) = -0.4
        _ShineWidth ("Shine Width", Range(0.02,0.6)) = 0.15
        _ShineStrength ("Shine Strength", Range(0,1)) = 0.3
        _BackShineStrength ("Back Shine Strength", Range(0,1)) = 0.08
        _StreakStrength ("Flowing Streaks", Range(0,1)) = 0.2
        _StreakFreq ("Streak Frequency (per unit)", Float) = 6
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

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        struct Attributes
        {
            float4 positionOS : POSITION;
            float2 uv0        : TEXCOORD0;
            float2 uv1        : TEXCOORD1;
            float2 uv2        : TEXCOORD2;
            float4 color      : COLOR;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 uv0        : TEXCOORD0;
            float2 uv1        : TEXCOORD1;
            float2 uv2        : TEXCOORD2;
            half4  color      : COLOR;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        CBUFFER_START(UnityPerMaterial)
            half4  _Color;
            half   _Wobble;
            half   _Sway;
            float  _WobbleFreq;
            float  _FlowSpeed;
            float  _SettleLength;
            float  _HeadFadeLength;
            half   _Saturation;
            half   _Brightness;
            half   _EdgeDarken;
            half   _Lighten;
            half4  _ShineColor;
            half   _ShinePos;
            half   _ShineWidth;
            half   _ShineStrength;
            half   _BackShineStrength;
            half   _StreakStrength;
            float  _StreakFreq;
        CBUFFER_END

        // per renderer, set from PourStreamView
        float4 _Caps;

        float Hash11(float p)
        {
            p = frac(p * 0.1031);
            p *= p + 33.33;
            p *= p + p;
            return frac(p);
        }

        float Hash21(float2 p)
        {
            p = frac(p * float2(123.34, 456.21));
            p += dot(p, p + 45.32);
            return frac(p.x * p.y);
        }

        float Noise1(float x)
        {
            float i = floor(x);
            float f = frac(x);
            f = f * f * (3.0 - 2.0 * f);
            return lerp(Hash11(i), Hash11(i + 1.0), f);
        }

        float Noise2(float2 p)
        {
            float2 i = floor(p);
            float2 f = frac(p);
            f = f * f * (3.0 - 2.0 * f);
            float a = Hash21(i);
            float b = Hash21(i + float2(1, 0));
            float c = Hash21(i + float2(0, 1));
            float d = Hash21(i + float2(1, 1));
            return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
        }

        // Half width left at a distance d from a rounded end of radius r (1 = full width).
        float CapWidth(float d, float r)
        {
            float k = 1.0 - saturate(d / max(r, 1e-4));
            return sqrt(saturate(1.0 - k * k));
        }

        Varyings vert(Attributes IN)
        {
            Varyings OUT;
            UNITY_SETUP_INSTANCE_ID(IN);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
            OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
            OUT.uv0 = IN.uv0;
            OUT.uv1 = IN.uv1;
            OUT.uv2 = IN.uv2;
            OUT.color = IN.color * _Color;
            return OUT;
        }

        half4 frag(Varyings IN) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

            float x = IN.uv0.x;
            float dist = IN.uv1.x;
            float toHead = IN.uv1.y;
            float hw = IN.uv2.x;
            float fromTail = IN.uv2.y;

            // f(dist - speed * t) makes every feature travel down the stream
            float flowCoord = dist - _Time.y * _FlowSpeed;

            // the stream leaves the lip as a smooth sheet and gets livelier as it falls
            float settle = saturate(dist / max(_SettleLength, 1e-3));
            float edge = 1.0 + _Wobble * settle * (Noise1(flowCoord * _WobbleFreq) * 2.0 - 1.0);
            float center = _Sway * settle * (Noise1(flowCoord * _WobbleFreq * 0.6 + 17.0) * 2.0 - 1.0);

            edge *= lerp(1.0, CapWidth(toHead, hw), _Caps.y);
            edge *= lerp(1.0, CapWidth(fromTail, hw), _Caps.x);

            float xn = (x - center) / max(edge, 1e-3);
            float d = abs(xn);
            float aa = max(fwidth(d), 1e-4);
            float body = 1.0 - smoothstep(1.0 - aa, 1.0 + aa, d);

            // round cross-section: darker toward the silhouette, like a cylinder of liquid
            float nz = sqrt(saturate(1.0 - d * d));
            half3 rgb = IN.color.rgb;
            half luma = dot(rgb, half3(0.299, 0.587, 0.114));
            rgb = saturate(lerp(luma.xxx, rgb, _Saturation) * _Brightness);
            // the bottled liquid is always seen through the whitish glass, the falling stream is not
            rgb = lerp(rgb, half3(1, 1, 1), _Lighten);
            rgb *= lerp(1.0 - _EdgeDarken, 1.0, nz);

            // glints stretched along the flow, sliding down with it
            float streak = Noise2(float2(xn * 2.5, flowCoord * _StreakFreq));
            streak = smoothstep(0.45, 0.85, streak);

            // squared by multiplication: pow() on a negative base is NaN in HLSL
            float s = (xn - _ShinePos) / _ShineWidth;
            float b = (xn + _ShinePos * 1.15) / (_ShineWidth * 0.7);
            float shine = exp(-s * s) * _ShineStrength * lerp(0.75, 1.15, streak)
                        + exp(-b * b) * _BackShineStrength
                        + streak * _StreakStrength * nz * 0.5 * settle;
            rgb = lerp(rgb, _ShineColor.rgb, saturate(shine));

            // where the head is already under the target surface it melts into the liquid
            float headFade = lerp(1.0, saturate(toHead / max(_HeadFadeLength, 1e-3)), _Caps.z);

            half alpha = body * headFade * IN.color.a;
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
