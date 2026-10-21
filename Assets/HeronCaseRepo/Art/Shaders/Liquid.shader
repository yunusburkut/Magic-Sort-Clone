Shader "HeronCase/Liquid"
{
    // All liquid layers of one bottle, drawn on a single quad in tube-local space.
    // TubeLiquid feeds the per-renderer data through a MaterialPropertyBlock:
    //   _LayerColors[i]  layer color
    //   _LayerBounds[i]  top boundary of layer i: (height along world-up, chord centre along world-right,
    //                    chord half length, ellipse half height)
    //   _Frame           (local world-up x, y, surface slope, front/back tilt)
    //   _Shape           (body half width, neck half width, bottom y, lip y)
    //   _Shape2          (shoulder y, neck y, ellipse ratio at the lip, height of the rounded bottom)
    //   _Impact          (stream landing point along world-right, splash strength, slosh energy, bubble strength)
    Properties
    {
        [Header(Color)]
        _Saturation ("Saturation", Range(0,2)) = 1
        _Brightness ("Brightness", Range(0.5,2)) = 1.05
        _SideDarken ("Side Darken", Range(0,1)) = 0.3
        _BottomDarken ("Bottom Darken", Range(0,1)) = 0.16
        _ShineStrength ("Side Shine", Range(0,1)) = 0.14
        _ShinePos ("Side Shine Position (-1..1)", Range(-1,1)) = -0.62
        _ShineWidth ("Side Shine Width", Range(0.02,0.6)) = 0.09
        _LayerLine ("Layer Line Strength", Range(0,1)) = 0.08
        _LayerLineWidth ("Layer Line Width", Float) = 0.03

        [Header(Surface)]
        _FaceLighten ("Top Face Lighten", Range(0,1)) = 0.1
        _FaceBackDarken ("Top Face Far Side Darken", Range(0,1)) = 0.1
        _RimStrength ("Front Rim Highlight", Range(0,1)) = 0.35
        _RimWidth ("Front Rim Width", Float) = 0.035
        _WaveAmp ("Idle Wave Amplitude", Float) = 0.012
        _RippleAmp ("Impact Ripple Amplitude", Float) = 0.035

        [Header(Impact)]
        _ChurnStrength ("Churn Strength", Range(0,1)) = 0.32
        _ChurnWidth ("Churn Width", Float) = 0.15
        _FoamStrength ("Foam Strength", Range(0,1)) = 0.8
        _FoamWidth ("Foam Width", Float) = 0.24
        _BubbleDensity ("Bubble Density", Range(0,1)) = 0.55
        _BubbleSize ("Bubble Cell Size", Float) = 0.16
        _BubbleRise ("Bubble Rise Speed", Float) = 1.25
        _BubbleDepth ("Bubble Depth", Float) = 1.3
        _BubbleSpread ("Bubble Spread", Float) = 0.34
        _SplashWhite ("Bubbles & Foam Whiteness", Range(0,1)) = 1
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

        #define MAX_LAYERS 8
        // matches LiquidShape.BottomPower
        #define BOTTOM_POWER 2.1

        struct Attributes
        {
            float4 positionOS : POSITION;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 localPos   : TEXCOORD0;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        CBUFFER_START(UnityPerMaterial)
            half  _Saturation;
            half  _Brightness;
            half  _SideDarken;
            half  _BottomDarken;
            half  _ShineStrength;
            half  _ShinePos;
            half  _ShineWidth;
            half  _LayerLine;
            float _LayerLineWidth;
            half  _FaceLighten;
            half  _FaceBackDarken;
            half  _RimStrength;
            float _RimWidth;
            float _WaveAmp;
            float _RippleAmp;
            half  _ChurnStrength;
            float _ChurnWidth;
            half  _FoamStrength;
            float _FoamWidth;
            half  _BubbleDensity;
            float _BubbleSize;
            float _BubbleRise;
            float _BubbleDepth;
            float _BubbleSpread;
            half  _SplashWhite;
        CBUFFER_END

        // per renderer, set from TubeLiquid
        float4 _LayerColors[MAX_LAYERS];
        float4 _LayerBounds[MAX_LAYERS];
        float  _LayerCount;
        float4 _Frame;
        float4 _Shape;
        float4 _Shape2;
        float4 _Impact;

        float Hash21(float2 p)
        {
            p = frac(p * float2(123.34, 456.21));
            p += dot(p, p + 45.32);
            return frac(p.x * p.y);
        }

        float2 Hash22(float2 p)
        {
            float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
            p3 += dot(p3, p3.yzx + 33.33);
            return frac((p3.xx + p3.yz) * p3.zy);
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

        half3 Grade(half3 c)
        {
            half l = dot(c, half3(0.299, 0.587, 0.114));
            return saturate(lerp(l.xxx, c, _Saturation) * _Brightness);
        }

        // Inner radius of the bottle at a local height, with a rounded shoulder.
        float Radius(float y)
        {
            float t = saturate((y - _Shape2.x) / max(_Shape2.y - _Shape2.x, 1e-4));
            t = t * t * (3.0 - 2.0 * t);
            return lerp(_Shape.x, _Shape.y, t);
        }

        // One grid of rising bubbles; returns coverage 0..1. p is (world-right, world-up) relative to the tube.
        float Bubbles(float2 p, float cell, float seed, float px)
        {
            float2 id = floor(p / cell);
            float2 rnd = Hash22(id + seed);
            if (Hash21(id + seed * 1.7) > _BubbleDensity) return 0.0;

            float2 center = (id + 0.3 + 0.4 * rnd) * cell;
            center.x += sin(_Time.y * 7.0 + rnd.y * 6.2831) * cell * 0.08;
            float radius = cell * lerp(0.13, 0.27, rnd.x);

            float d = length(p - center);
            float ring = 1.0 - saturate(abs(d - radius * 0.8) / max(radius * 0.3, px));
            float fill = 1.0 - saturate(d / radius);
            // tiny specular dot toward the upper left
            float2 hp = p - center - float2(-0.35, 0.35) * radius;
            float spec = 1.0 - saturate(length(hp) / max(radius * 0.28, px));
            return saturate(ring * 0.85 + fill * 0.18 + spec);
        }

        Varyings vert(Attributes IN)
        {
            Varyings OUT;
            UNITY_SETUP_INSTANCE_ID(IN);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
            OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
            OUT.localPos = IN.positionOS.xy;
            return OUT;
        }

        half4 frag(Varyings IN) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

            int n = (int)(_LayerCount + 0.5);
            if (n <= 0) return half4(0, 0, 0, 0);

            float2 lp = IN.localPos;
            float px = max(length(fwidth(lp)) * 0.75, 1e-4);
            float t = _Time.y;

            // ---- bottle silhouette: walls, rounded bottom, top of the opening
            float r = Radius(lp.y);
            float rb = _Shape.x;
            float bx = max(saturate(abs(lp.x) / rb), 1e-5);
            float bottomRise = pow(max(1.0 - pow(bx, BOTTOM_POWER), 1e-5), 1.0 / BOTTOM_POWER);
            float bottomEdge = _Shape.z + _Shape2.w * (1.0 - bottomRise);
            float topEdge = _Shape.w + _Shape2.z * _Shape.y;
            float inside = min(min(r - abs(lp.x), lp.y - bottomEdge), topEdge - lp.y);
            float mask = saturate(inside / px + 0.5);
            if (mask <= 0.0) return half4(0, 0, 0, 0);

            // ---- level frame: h is height along world-up, w the position along world-right
            float2 up = _Frame.xy;
            float2 right = float2(up.y, -up.x);
            float h = dot(lp, up);
            float w = dot(lp, right);
            float slope = _Frame.z;
            float tilt = _Frame.w;

            float impW = _Impact.x;
            float impS = _Impact.y;
            float energy = _Impact.z;
            float bubbleE = _Impact.w;
            float dImp = w - impW;

            float waves = _WaveAmp * energy * (sin(w * 7.0 + t * 2.3) + 0.6 * sin(w * 12.0 - t * 3.7 + 1.3))
                        + _RippleAmp * impS * sin(abs(dImp) * 30.0 - t * 16.0) * exp(-abs(dImp) * 3.5);

            // ---- layers: walk the boundaries bottom to top, switching color above each front edge
            half3 col = Grade(_LayerColors[0].rgb);
            half3 topCol = col;
            float lineGlow = 0.0;
            float frontTop = 0.0, backTop = 0.0, surfTop = 0.0, halfFace = 1e-4;

            [unroll]
            for (int i = 0; i < MAX_LAYERS; i++)
            {
                if (i < n)
                {
                    float4 b = _LayerBounds[i];
                    float u = clamp((w - b.y) / max(b.z, 1e-4), -1.0, 1.0);
                    float s = sqrt(1.0 - u * u);
                    float e = max(b.w * (1.0 + tilt), 0.3 * b.w);
                    float surf = b.x + slope * (w - b.y);

                    if (i == n - 1)
                    {
                        surf += waves;
                        surfTop = surf;
                        halfFace = max(e * s, 1e-4);
                        frontTop = surf - e * s;
                        backTop = surf + e * s;
                        topCol = Grade(_LayerColors[i].rgb);
                    }
                    else
                    {
                        float front = surf - e * s;
                        float k = saturate((h - front) / px + 0.5);
                        col = lerp(col, Grade(_LayerColors[min(i + 1, MAX_LAYERS - 1)].rgb), k);
                        float dl = (h - front) / _LayerLineWidth;
                        lineGlow += exp(-dl * dl);
                    }
                }
            }

            // ---- side of the liquid column: a lit cylinder
            float nx = clamp(lp.x / max(r, 1e-4), -1.0, 1.0);
            float nz = sqrt(1.0 - nx * nx);
            half3 side = col * lerp(1.0 - _SideDarken, 1.0, nz);
            side *= lerp(1.0 - _BottomDarken, 1.0, saturate((lp.y - _Shape.z) / 1.5));
            float sh = (nx - _ShinePos) / _ShineWidth;
            side = lerp(side, half3(1, 1, 1), exp(-sh * sh) * _ShineStrength);
            side = lerp(side, half3(1, 1, 1), saturate(lineGlow) * _LayerLine);

            // ---- under the stream: a plunging, lighter column and rising bubbles, tinted with the poured (top) color
            // the churn, the bubbles and the foam all scale with the splash or the bubbles it left, so a still tube
            // skips them; the branch is the same for the whole draw
            bool stirred = impS > 0.0 || bubbleE > 0.0;
            half3 splash = lerp(topCol, half3(1, 1, 1), _SplashWhite);
            [branch]
            if (stirred)
            {
                float depth = surfTop - h;
                float colMask = exp(-(dImp * dImp) / (_ChurnWidth * _ChurnWidth));
                float churnN = Noise2(float2(dImp * 9.0, h * 5.0 + t * 7.0));
                float churn = colMask * saturate(1.0 - depth / 1.1) * impS * smoothstep(0.3, 0.8, churnN);
                side = lerp(side, splash, saturate(churn) * _ChurnStrength);

                float2 bp = float2(w, h - t * _BubbleRise);
                float bubbles = Bubbles(bp, _BubbleSize, 0.0, px);
                bubbles = max(bubbles, Bubbles(float2(w, h - t * _BubbleRise * 1.35) + 3.7, _BubbleSize * 0.62, 11.0, px) * 0.8);
                float spread = exp(-(dImp * dImp) / (_BubbleSpread * _BubbleSpread));
                float zone = smoothstep(_BubbleDepth, _BubbleDepth * 0.35, depth) * smoothstep(-0.01, 0.05, depth);
                side = lerp(side, splash, saturate(bubbles * spread * zone * bubbleE) * 0.85);
            }

            // ---- top surface seen from above
            float faceV = (h - surfTop) / halfFace;
            half3 face = lerp(topCol, half3(1, 1, 1), _FaceLighten);
            face *= lerp(1.0, 1.0 - _FaceBackDarken, saturate(faceV * 0.5 + 0.5));
            float rimD = (h - frontTop) / _RimWidth;
            face = lerp(face, half3(1, 1, 1), exp(-rimD * rimD) * _RimStrength);

            [branch]
            if (stirred)
            {
                float foamMask = exp(-(dImp * dImp) / (_FoamWidth * _FoamWidth)) * saturate(impS + bubbleE * 0.35);
                float fn = Noise2(float2(w * 16.0, h * 26.0 + t * 1.5)) * 0.6 + Noise2(float2(w * 31.0 - t, h * 47.0)) * 0.4;
                float foam = saturate(smoothstep(0.35, 0.62, fn) * foamMask * 1.5);
                face = lerp(face, splash, foam * _FoamStrength);
            }

            float kFace = saturate((h - frontTop) / px + 0.5);
            float kAbove = saturate((h - backTop) / px + 0.5);
            half3 rgb = lerp(side, face, kFace);
            half alpha = mask * (1.0 - kAbove);
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
