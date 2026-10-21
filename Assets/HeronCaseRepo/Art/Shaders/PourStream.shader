Shader "HeronCase/PourStream"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Shape)]
        _Width ("Stream Width (of quad)", Range(0.1,1)) = 0.7
        _ThinWidth ("Width At Zero Alpha", Range(0,1)) = 0.2
        _Wobble ("Width Wobble", Range(0,0.5)) = 0.14
        _Sway ("Center Sway", Range(0,0.3)) = 0.06
        _WobbleFreq ("Wobble Frequency (per unit)", Float) = 2.5
        _FlowSpeed ("Flow Speed (units/s)", Float) = 6
        _EndFade ("Quad End Fade (uv)", Range(0,0.2)) = 0.03

        [Header(Casual Color)]
        _Saturation ("Saturation", Range(0,2)) = 1.15
        _Brightness ("Brightness", Range(0.5,2)) = 1.12
        _EdgeDarken ("Edge Darken", Range(0,1)) = 0.3

        [Header(Highlights)]
        _ShineColor ("Shine Color", Color) = (1,1,1,1)
        _ShinePos ("Shine Position (-1..1)", Range(-1,1)) = -0.38
        _ShineWidth ("Shine Width", Range(0.02,0.6)) = 0.16
        _ShineStrength ("Shine Strength", Range(0,1)) = 0.6
        _BackShineStrength ("Back Shine Strength", Range(0,1)) = 0.2
        _StreakStrength ("Flowing Streaks", Range(0,1)) = 0.35
        _StreakFreq ("Streak Frequency (per unit)", Float) = 7
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
            half   _Width;
            half   _ThinWidth;
            half   _Wobble;
            half   _Sway;
            float  _WobbleFreq;
            float  _FlowSpeed;
            half   _EndFade;
            half   _Saturation;
            half   _Brightness;
            half   _EdgeDarken;
            half4  _ShineColor;
            half   _ShinePos;
            half   _ShineWidth;
            half   _ShineStrength;
            half   _BackShineStrength;
            half   _StreakStrength;
            float  _StreakFreq;
        CBUFFER_END

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

        // Smooth 1D value noise in [0,1].
        float Noise1(float x)
        {
            float i = floor(x);
            float f = frac(x);
            f = f * f * (3.0 - 2.0 * f);
            return lerp(Hash11(i), Hash11(i + 1.0), f);
        }

        // Smooth 2D value noise in [0,1].
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

            // Pattern coordinate scrolls in world space so it is independent of the quad's stretch.
            // f(y + speed * t) makes every feature travel downward.
            float flowY = IN.positionWS.y + _Time.y * _FlowSpeed;

            // Vertex alpha drives thickness: the stream thins to a trickle while fading out.
            half fade = IN.color.a;
            half width = _Width * lerp(_ThinWidth, 1.0, fade);
            width *= 1.0 + _Wobble * (Noise1(flowY * _WobbleFreq) * 2.0 - 1.0);
            half center = _Sway * (Noise1(flowY * _WobbleFreq * 0.6 + 17.0) * 2.0 - 1.0);

            // xn: -1..1 across the stream body, |xn| > 1 is outside
            float x = IN.uv.x * 2.0 - 1.0;
            float xn = (x - center) / max(width, 1e-3);
            float d = abs(xn);
            float aa = max(fwidth(d), 1e-4);
            float body = 1.0 - smoothstep(1.0 - aa, 1.0 + aa, d);

            // round cross-section: darker toward the silhouette, like a cylinder of liquid
            float nz = sqrt(saturate(1.0 - d * d));
            half3 rgb = IN.color.rgb;
            half luma = dot(rgb, half3(0.299, 0.587, 0.114));
            rgb = lerp(luma.xxx, rgb, _Saturation) * _Brightness;
            rgb *= lerp(1.0 - _EdgeDarken, 1.0, nz);

            // glints stretched along the flow, sliding down with it
            float streak = Noise2(float2(xn * 2.5, flowY * _StreakFreq));
            streak = smoothstep(0.45, 0.85, streak);

            // squared by multiplication: pow() on a negative base is NaN in HLSL
            float s = (xn - _ShinePos) / _ShineWidth;
            float b = (xn + _ShinePos * 1.15) / (_ShineWidth * 0.7);
            float shineMask = exp(-s * s);
            float backMask = exp(-b * b);
            float shine = shineMask * _ShineStrength * lerp(0.75, 1.15, streak)
                        + backMask * _BackShineStrength
                        + streak * _StreakStrength * nz * 0.5;
            rgb = lerp(rgb, _ShineColor.rgb, saturate(shine));

            // soften the two quad ends so the cut never shows as a hard line
            float ends = smoothstep(0.0, _EndFade, IN.uv.y) * smoothstep(0.0, _EndFade, 1.0 - IN.uv.y);

            half alpha = body * ends * saturate(fade * 3.0);
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
