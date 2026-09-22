Shader "UI/MazeFlowingFog"
{
    Properties
    {
        [PerRendererData]
        _MainTex ("Sprite Texture", 2D) = "white" {}

        _VisibilityMask ("Visibility Mask", 2D) = "black" {}

        _FogBaseColor ("Fog Base Color", Color) =
            (0.015, 0.045, 0.070, 1.0)

        _FogCloudColor ("Fog Cloud Color", Color) =
            (0.060, 0.220, 0.310, 1.0)

        _NoiseScale ("Noise Scale", Float) = 7.0
        _FlowDirection ("Flow Direction", Vector) =
            (0.70, 0.30, 0.0, 0.0)

        _FlowSpeed ("Flow Speed", Float) = 0.18
        _CloudContrast ("Cloud Contrast", Float) = 1.35

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            sampler2D _MainTex;
            sampler2D _VisibilityMask;

            fixed4 _FogBaseColor;
            fixed4 _FogCloudColor;

            float _NoiseScale;
            float4 _FlowDirection;
            float _FlowSpeed;
            float _CloudContrast;

            float4 _ClipRect;

            float Hash21(float2 value)
            {
                value = frac(value * float2(123.34, 456.21));
                value += dot(value, value + 45.32);

                return frac(value.x * value.y);
            }

            float ValueNoise(float2 uv)
            {
                float2 cell = floor(uv);
                float2 fraction = frac(uv);

                fraction = fraction * fraction
                    * (3.0 - 2.0 * fraction);

                float bottomLeft =
                    Hash21(cell + float2(0.0, 0.0));

                float bottomRight =
                    Hash21(cell + float2(1.0, 0.0));

                float topLeft =
                    Hash21(cell + float2(0.0, 1.0));

                float topRight =
                    Hash21(cell + float2(1.0, 1.0));

                float bottom = lerp(
                    bottomLeft,
                    bottomRight,
                    fraction.x);

                float top = lerp(
                    topLeft,
                    topRight,
                    fraction.x);

                return lerp(bottom, top, fraction.y);
            }

            float FractalNoise(float2 uv)
            {
                float value = 0.0;
                float amplitude = 0.5;

                for (int i = 0; i < 4; i++)
                {
                    value += ValueNoise(uv) * amplitude;
                    uv = uv * 2.03 + float2(17.13, 9.71);
                    amplitude *= 0.5;
                }

                return value;
            }

            v2f vert(appdata_t input)
            {
                v2f output;

                output.worldPosition = input.vertex;
                output.vertex =
                    UnityObjectToClipPos(output.worldPosition);

                output.uv = input.texcoord;
                output.color = input.color;

                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float visibility =
                    tex2D(
                        _VisibilityMask,
                        input.uv).r;

                // 既知セルは完全に描画しない。
                clip(0.5 - visibility);

                float time =
                    _Time.y * _FlowSpeed;

                float2 direction =
                    normalize(_FlowDirection.xy);

                float2 slowUv =
                    input.uv * _NoiseScale
                    + direction * time;

                float2 fastUv =
                    input.uv * (_NoiseScale * 1.65)
                    - direction * (time * 1.55);

                float slowCloud =
                    FractalNoise(slowUv);

                float fastCloud =
                    FractalNoise(fastUv);

                float cloud =
                    slowCloud * 0.72
                    + fastCloud * 0.28;

                cloud = saturate(
                    (cloud - 0.5) * _CloudContrast
                    + 0.5);

                cloud = smoothstep(
                    0.18,
                    0.88,
                    cloud);

                fixed4 color = lerp(
                    _FogBaseColor,
                    _FogCloudColor,
                    cloud);

                color *= input.color;
                color.a = 1.0;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(
                    input.worldPosition.xy,
                    _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }

            ENDCG
        }
    }
}