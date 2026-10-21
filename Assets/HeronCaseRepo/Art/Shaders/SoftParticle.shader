Shader "HeronCase/SoftParticle"
{
    // Round particle with a soft edge, colored by the particle color. Used for the splash mist and droplets;
    // _Droplet shades it as a small lit drop of water.
    // _Star turns it into a four-point twinkle, and _DstBlend One makes it additive for the solved sparks.
    Properties
    {
        _Softness ("Edge Softness", Range(0.01,1)) = 0.6
        _Core ("Bright Core", Range(0,1)) = 0.0
        _Star ("Star Rays", Range(0,1)) = 0.0
        _Droplet ("Water Drop Shading", Range(0,1)) = 0.0
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

        CBUFFER_START(UnityPerMaterial)
            half _Softness;
            half _Core;
            half _Star;
            half _Droplet;
            half _DstBlend;
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

            float2 p = IN.uv * 2.0 - 1.0;
            float r = length(p);
            float aa = max(fwidth(r), 1e-4);
            half a = 1.0 - smoothstep(1.0 - _Softness - aa, 1.0, r);

            // thin rays along both axes that taper toward the tips, around a small glowing center
            float2 q = abs(p);
            float rays = saturate(1.0 - q.x * q.y * 60.0) * saturate(1.0 - r);
            float glow = exp(-r * r * 18.0);
            half star = saturate(rays * rays + glow);
            a = lerp(a, star, _Star);

            half core = lerp(saturate(1.0 - r * 2.0), glow, _Star);
            half3 rgb = lerp(IN.color.rgb, half3(1, 1, 1), _Core * core);

            // a lit drop of water: deeper color toward the rim and a small highlight toward the upper left
            float2 hp = p - float2(-0.3, 0.35);
            half3 drop = saturate(IN.color.rgb * lerp(1.15, 0.65, r * r));
            drop = lerp(drop, half3(1, 1, 1), exp(-dot(hp, hp) * 20.0) * 0.85);
            rgb = lerp(rgb, drop, _Droplet);
            return half4(rgb, a * IN.color.a);
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
