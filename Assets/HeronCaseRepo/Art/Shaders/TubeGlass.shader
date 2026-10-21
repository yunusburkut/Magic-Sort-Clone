Shader "HeronCase/TubeGlass"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Glass)]
        _GlassTint ("Glass Tint", Color) = (1,1,1,1)
        _Reflection ("Reflection", Range(0,2)) = 1
        _BodyAlpha ("Body Opacity", Range(0,2)) = 1
        _RimAlpha ("Rim Opacity", Range(0,2)) = 1

        [Header(Shine Sweep)]
        _SweepColor ("Sweep Color", Color) = (1,1,1,1)
        _SweepStrength ("Sweep Strength", Range(0,1)) = 0.55
        _SweepWidth ("Sweep Width", Range(0.01,0.5)) = 0.09
        _SweepPeriod ("Sweep Period (s)", Float) = 3.5
        _SweepDuration ("Sweep Travel Time (s)", Float) = 3.5
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

        // the sprite is premultiplied: its highlights add light even where the glass hides nothing
        Blend One OneMinusSrcAlpha
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
            half   _Reflection;
            half   _BodyAlpha;
            half   _RimAlpha;
            half4  _SweepColor;
            half   _SweepStrength;
            half   _SweepWidth;
            float  _SweepPeriod;
            float  _SweepDuration;
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

            // rgb is the light the glass gives off by itself (reflections), a how much of what is behind it hides
            half3 rgb = tex.rgb * _GlassTint.rgb * _Reflection;

            // the thin body in front of the liquid and the rim can be thinned or thickened apart
            float rim = smoothstep(0.2, 0.45, tex.a);
            float a = saturate(tex.a * lerp(_BodyAlpha, _RimAlpha, rim));

            // diagonal shine sweep, staggered across the board by world x; it crosses in _SweepDuration and then rests
            // off the tube for the rest of the period
            float phase = frac(_Time.y / _SweepPeriod + IN.positionWS.x * _SweepStagger);
            float s = IN.uv.y + IN.uv.x * 0.45;
            float center = lerp(-0.5, 2.2, saturate(phase * _SweepPeriod / max(_SweepDuration, 1e-3)));
            float sweep = (1.0 - smoothstep(0.0, _SweepWidth, abs(s - center))) * glass * _SweepStrength;
            rgb = lerp(rgb, _SweepColor.rgb, sweep);
            a = lerp(a, 1.0, sweep);

            return half4(rgb * IN.color.rgb, a) * IN.color.a;
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
