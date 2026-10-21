Shader "HeronCase/BgCasual"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Aspect ("Sprite Aspect (w/h)", Float) = 1.3333

        [Header(Gradient)]
        _TopColor ("Top Color", Color) = (0.37,0.66,1,1)
        _BottomColor ("Bottom Color", Color) = (0.63,0.47,0.98,1)
        _GlowColor ("Center Glow Color", Color) = (1,0.96,0.85,1)
        _GlowStrength ("Center Glow Strength", Range(0,1)) = 0.22
        _VignetteStrength ("Vignette Strength", Range(0,1)) = 0.12

        [Header(Bokeh)]
        _BokehColor ("Bokeh Color", Color) = (1,1,1,1)
        _BokehStrength ("Bokeh Strength", Range(0,1)) = 0.14
        _BokehScale ("Bokeh Cells (vertical)", Float) = 6
        _BokehSpeed ("Bokeh Rise Speed", Float) = 0.02

        [Header(Stripes)]
        _StripeStrength ("Stripe Strength", Range(0,0.2)) = 0.035
        _StripeScale ("Stripe Count", Float) = 14
        _StripeSpeed ("Stripe Scroll Speed", Float) = 0.03
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
            half4  color      : COLOR;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);

        CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            half4  _Color;
            float  _Aspect;
            half4  _TopColor;
            half4  _BottomColor;
            half4  _GlowColor;
            half   _GlowStrength;
            half   _VignetteStrength;
            half4  _BokehColor;
            half   _BokehStrength;
            float  _BokehScale;
            float  _BokehSpeed;
            half   _StripeStrength;
            float  _StripeScale;
            float  _StripeSpeed;
        CBUFFER_END

        float Hash21(float2 p)
        {
            p = frac(p * float2(123.34, 456.21));
            p += dot(p, p + 45.32);
            return frac(p.x * p.y);
        }

        // One layer of soft circles, one candidate per grid cell, drifting upward with a gentle sway.
        float BokehLayer(float2 p, float scale, float speed, float seed)
        {
            p *= scale;
            p.y -= _Time.y * speed * scale;
            float2 cell = floor(p);
            float2 f = frac(p);
            float h = Hash21(cell + seed);
            float2 c = float2(0.25 + 0.5 * Hash21(cell + seed + 1.7), 0.25 + 0.5 * Hash21(cell + seed + 4.2));
            c.x += 0.12 * sin(_Time.y * 0.6 + h * 6.28);
            float r = lerp(0.12, 0.3, Hash21(cell + seed + 9.1));
            float d = length(f - c);
            float disc = smoothstep(r, r - 0.04, d);
            float twinkle = 0.75 + 0.25 * sin(_Time.y * 1.3 + h * 20.0);
            return disc * step(0.55, h) * twinkle;
        }

        Varyings vert(Attributes IN)
        {
            Varyings OUT;
            UNITY_SETUP_INSTANCE_ID(IN);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
            OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
            OUT.uv         = TRANSFORM_TEX(IN.uv, _MainTex);
            OUT.color      = IN.color * _Color;
            return OUT;
        }

        half4 frag(Varyings IN) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
            half alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv).a;
            float2 uv = IN.uv;
            float2 p = float2(uv.x * _Aspect, uv.y);   // square units for round shapes

            half3 rgb = lerp(_BottomColor.rgb, _TopColor.rgb, smoothstep(0.0, 1.0, uv.y));

            float2 g = (uv - float2(0.5, 0.55)) * float2(_Aspect, 1.0);
            rgb += _GlowColor.rgb * exp(-dot(g, g) * 3.0) * _GlowStrength;

            float stripes = step(0.5, frac((p.x + p.y) * _StripeScale + _Time.y * _StripeSpeed));
            rgb += stripes * _StripeStrength;

            float bokeh = BokehLayer(p, _BokehScale, _BokehSpeed, 0.0) * 0.6
                        + BokehLayer(p, _BokehScale * 0.55, _BokehSpeed * 0.6, 31.0);
            rgb = lerp(rgb, _BokehColor.rgb, saturate(bokeh) * _BokehStrength);

            float2 v = uv - 0.5;
            rgb *= 1.0 - _VignetteStrength * smoothstep(0.25, 0.75, length(v * float2(_Aspect, 1.0) * 0.8));

            return half4(rgb * IN.color.rgb, alpha * IN.color.a);
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
