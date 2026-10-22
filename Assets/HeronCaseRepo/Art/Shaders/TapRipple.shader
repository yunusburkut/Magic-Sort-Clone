Shader "HeronCase/TapRipple"
{
    // The ring that answers a tap. The quad never changes size: the particle age in TEXCOORD0.z (0..1, see
    // TapRippleView) grows the ring inside it, thins it as it goes and fades it out, so a whole ripple is one particle.
    // A faint dot fills the ring for the first moment, which reads as the press itself.
    Properties
    {
        _StartRadius ("Start Radius", Range(0,1)) = 0.5
        _EndRadius ("End Radius", Range(0,1)) = 0.9
        _StartWidth ("Start Half Width", Range(0,0.5)) = 0.04
        _EndWidth ("End Half Width", Range(0,0.5)) = 0.02
        _Fill ("Press Dot", Range(0,1)) = 0.12
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
            half _StartRadius;
            half _EndRadius;
            half _StartWidth;
            half _EndWidth;
            half _Fill;
        CBUFFER_END

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
            float t = saturate(IN.uv.z);

            // the ring shoots out and slows down (out cubic) while it thins (out quad)
            float grow = 1.0 - (1.0 - t) * (1.0 - t) * (1.0 - t);
            float radius = lerp(_StartRadius, _EndRadius, grow);
            float width = lerp(_StartWidth, _EndWidth, 1.0 - (1.0 - t) * (1.0 - t));
            half ring = 1.0 - smoothstep(width - aa, width + aa, abs(r - radius));

            // the press dot fills the ring and is gone before the ring is
            half inside = 1.0 - smoothstep(radius - aa, radius + aa, r);
            half fill = inside * _Fill * saturate(1.0 - t * 2.5);

            half fade = (1.0 - t) * sqrt(1.0 - t);
            half alpha = saturate(ring * fade + fill) * IN.color.a;
            return half4(IN.color.rgb, alpha);
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
