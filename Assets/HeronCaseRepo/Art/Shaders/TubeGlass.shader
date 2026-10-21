Shader "HeronCase/TubeGlass"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Glass)]
        _GlassTint ("Glass Tint", Color) = (0.86,0.95,1,1)
        _AlphaBoost ("Alpha Boost", Range(0.5,3)) = 1.35
        _BodyAlpha ("Body Opacity", Range(0,1)) = 0.35
        _RimAlpha ("Rim Opacity", Range(0,1)) = 1
        _EdgeGlow ("Edge Glow", Range(0,1)) = 0.35

        [Header(Shine Sweep)]
        _SweepColor ("Sweep Color", Color) = (1,1,1,1)
        _SweepStrength ("Sweep Strength", Range(0,1)) = 0.55
        _SweepWidth ("Sweep Width", Range(0.01,0.5)) = 0.09
        _SweepPeriod ("Sweep Period (s)", Float) = 3.5
        _SweepStagger ("Stagger Per World Unit", Float) = 0.06
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
            float2 positionWS : TEXCOORD1;
            half4  color      : COLOR;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);

        CBUFFER_START(UnityPerMaterial)
            half4  _Color;
            half4  _GlassTint;
            half   _AlphaBoost;
            half   _BodyAlpha;
            half   _RimAlpha;
            half   _EdgeGlow;
            half4  _SweepColor;
            half   _SweepStrength;
            half   _SweepWidth;
            float  _SweepPeriod;
            float  _SweepStagger;
        CBUFFER_END

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

            // glass mask: everything the Blender render covered, including the near-transparent body
            float glass = smoothstep(0.02, 0.08, tex.a);

            half3 rgb = tex.rgb * _GlassTint.rgb;

            // the thin body in front of the liquid stays faint so its colours read clearly; the rim keeps a softer line
            float rim = smoothstep(0.2, 0.45, tex.a);
            float a = saturate(tex.a * _AlphaBoost * lerp(_BodyAlpha, _RimAlpha, rim));

            // brighter, cleaner silhouette: lift the strong (edge) pixels toward white
            float edge = smoothstep(0.35, 0.9, tex.a);
            rgb = lerp(rgb, half3(1, 1, 1), edge * _EdgeGlow);

            // diagonal shine sweep, staggered across the board by world x
            float phase = frac(_Time.y / _SweepPeriod + IN.positionWS.x * _SweepStagger);
            float s = IN.uv.y + IN.uv.x * 0.45;
            float center = lerp(-0.5, 2.2, phase);
            float sweep = (1.0 - smoothstep(0.0, _SweepWidth, abs(s - center))) * glass * _SweepStrength;
            rgb = lerp(rgb, _SweepColor.rgb, sweep);
            a = max(a, sweep * 0.8);

            return half4(rgb * IN.color.rgb, a * IN.color.a);
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
