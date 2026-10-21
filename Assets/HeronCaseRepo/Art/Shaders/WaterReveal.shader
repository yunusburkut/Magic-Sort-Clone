Shader "HeronCase/WaterReveal"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _RevealAmount ("Reveal Amount", Range(0,1)) = 1
        _ConcaveDepth ("Concave Depth", Range(0,0.5)) = 0.08

        [Header(Casual Color)]
        _Saturation ("Saturation", Range(0,2)) = 1.15
        _Brightness ("Brightness", Range(0.5,2)) = 1.12
        _BottomShade ("Bottom Shade", Range(0,1)) = 0.82
        _RimDarken ("Side Darken", Range(0,1)) = 0.18

        [Header(Shine Stripe)]
        _ShineColor ("Shine Color", Color) = (1,1,1,1)
        _ShinePos ("Shine Position (uv.x)", Range(0,1)) = 0.2
        _ShineWidth ("Shine Width", Range(0.005,0.3)) = 0.055
        _ShineStrength ("Shine Strength", Range(0,1)) = 0.45

        [Header(Surface on top segment)]
        [PerRendererData] _Surface ("Is Surface", Float) = 0
        _SurfaceGlow ("Surface Glow", Range(0,1)) = 0.28
        _MeniscusStrength ("Meniscus Strength", Range(0,1)) = 0.6
        _RippleSpeed ("Ripple Speed", Float) = 2.2
        _RippleFreq ("Ripple Frequency", Float) = 18

        [Header(Bubbles)]
        _BubbleStrength ("Bubble Strength", Range(0,1)) = 0.35
        _BubbleScale ("Bubble Cells Per Unit", Float) = 4
        _BubbleSpeed ("Bubble Rise Speed", Float) = 0.35
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
            "CanUseSpriteAtlas" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        #define PI 3.14159265
        #define EDGE_SOFTNESS 0.02

        // Top face ellipse of WaterSmall.png in uv space (rendered by Tools/AssetGen/make_assets.py).
        #define TOP_CENTER float2(0.5, 0.828)
        #define TOP_RADIUS float2(0.484, 0.117)

        struct Attributes
        {
            float4 positionOS : POSITION;
            float2 uv         : TEXCOORD0;
            float4 color      : COLOR;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 uv         : TEXCOORD0;
            float2 positionWS : TEXCOORD1;
            half4  color      : COLOR;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);

        CBUFFER_START(UnityPerMaterial)
            half4  _Color;
            float  _RevealAmount;
            float  _ConcaveDepth;
            half   _Saturation;
            half   _Brightness;
            half   _BottomShade;
            half   _RimDarken;
            half4  _ShineColor;
            half   _ShinePos;
            half   _ShineWidth;
            half   _ShineStrength;
            float  _Surface;
            half   _SurfaceGlow;
            half   _MeniscusStrength;
            float  _RippleSpeed;
            float  _RippleFreq;
            half   _BubbleStrength;
            float  _BubbleScale;
            float  _BubbleSpeed;
        CBUFFER_END

        float Hash21(float2 p)
        {
            p = frac(p * float2(123.34, 456.21));
            p += dot(p, p + 45.32);
            return frac(p.x * p.y);
        }

        // Sparse soft dots rising in world space; sprites may be batched so object space is not reliable.
        float Bubbles(float2 posWS)
        {
            float2 p = posWS * _BubbleScale;
            p.y -= _Time.y * _BubbleSpeed * _BubbleScale;
            float2 cell = floor(p);
            float2 f = frac(p);
            float h = Hash21(cell);
            float2 c = float2(0.3 + 0.4 * Hash21(cell + 7.1) + 0.08 * sin(_Time.y * 2.0 + h * 6.28), 0.5);
            float r = lerp(0.05, 0.12, Hash21(cell + 3.3));
            float d = length(f - c);
            float disc = smoothstep(r, r * 0.55, d);
            float ring = smoothstep(r * 0.55, r * 0.8, d) * disc;   // brighter rim reads as a bubble
            return (disc * 0.5 + ring) * step(0.62, h);
        }

        Varyings vert(Attributes IN)
        {
            Varyings OUT;
            UNITY_SETUP_INSTANCE_ID(IN);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
            OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
            OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz).xy;
            OUT.uv         = IN.uv;
            OUT.color      = IN.color * _Color;
            return OUT;
        }

        half4 frag(Varyings IN) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
            half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
            half3 rgb = tex.rgb * IN.color.rgb;

            // candy look: push saturation and brightness, darken toward the bottom and the sides
            half luma = dot(rgb, half3(0.299, 0.587, 0.114));
            rgb = lerp(luma.xxx, rgb, _Saturation) * _Brightness;
            rgb *= lerp(_BottomShade, 1.0, saturate(IN.uv.y * 1.4));
            float side = abs(IN.uv.x * 2.0 - 1.0);
            rgb *= 1.0 - _RimDarken * smoothstep(0.65, 1.0, side);

            float2 e = (IN.uv - TOP_CENTER) / TOP_RADIUS;
            float topDist = length(e);
            float topFace = 1.0 - smoothstep(0.9, 1.0, topDist);

            float shine = 1.0 - smoothstep(0.0, _ShineWidth, abs(IN.uv.x - _ShinePos));
            shine *= smoothstep(0.08, 0.25, IN.uv.y) * (1.0 - topFace);
            rgb = lerp(rgb, _ShineColor.rgb, shine * _ShineStrength);

            float bubbles = Bubbles(IN.positionWS) * (1.0 - topFace) * smoothstep(0.1, 0.3, IN.uv.y);
            rgb = lerp(rgb, half3(1, 1, 1), bubbles * _BubbleStrength);

            // live surface: soft glow, concentric ripples and a bright meniscus ring
            float ripple = 0.5 + 0.5 * sin(topDist * _RippleFreq - _Time.y * _RippleSpeed);
            float meniscus = smoothstep(0.7, 0.93, topDist) * (1.0 - smoothstep(0.93, 1.0, topDist));
            half3 surfaceAdd = topFace * _SurfaceGlow * (0.65 + 0.35 * ripple) + meniscus * _MeniscusStrength;
            rgb += surfaceAdd * _Surface;

            half4 col = half4(rgb, tex.a * IN.color.a);

            float concave = sin(IN.uv.x * PI) * _ConcaveDepth * (1.0 - _RevealAmount);
            float threshold = _RevealAmount - concave;
            float revealAlpha = 1.0 - smoothstep(threshold - EDGE_SOFTNESS, threshold, IN.uv.y);
            col.a *= revealAlpha;
            return col;
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
