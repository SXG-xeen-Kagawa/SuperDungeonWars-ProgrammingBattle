Shader "MazeExplore/Maze Surface Atlas Lit"
{
    Properties
    {
        [MainTexture]
        _BaseMap("Surface Atlas (4 x 4)", 2D) = "white" {}

        [MainColor]
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)

        [Normal]
        _NormalMap("Normal Atlas (4 x 4)", 2D) = "bump" {}

        _NormalStrength("Normal Strength", Range(0, 2)) = 0.8

        _AmbientStrength("Ambient Strength", Range(0, 1)) = 0.55
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"

            Tags
            {
                "LightMode" = "UniversalForward"
            }

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _NormalStrength;
                half _AmbientStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                float2 surfaceType : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half4 tangentWS : TEXCOORD2;
                float2 faceUv : TEXCOORD3;
                float surfaceType : TEXCOORD4;
            };

            float2 GetAtlasCell(float surfaceType)
            {
                if (surfaceType < 0.5)
                {
                    return float2(0.0, 0.0);
                }

                if (surfaceType < 1.5)
                {
                    return float2(1.0, 0.0);
                }

                if (surfaceType < 2.5)
                {
                    return float2(2.0, 0.0);
                }

                if (surfaceType < 3.5)
                {
                    return float2(0.0, 1.0);
                }

                if (surfaceType < 4.5)
                {
                    return float2(1.0, 1.0);
                }

                return float2(3.0, 3.0);
            }

            float2 GetAtlasUv(
                float2 faceUv,
                float surfaceType)
            {
                const float2 atlasGrid = float2(4.0, 4.0);

                float2 atlasCell =
                    GetAtlasCell(surfaceType);

                float2 localUv =
                    saturate(faceUv);

                return (atlasCell + localUv)
                    / atlasGrid;
            }

            half3 GetNormalWSFromNormalMap(
                half3 baseNormalWS,
                half4 tangentWS,
                float2 atlasUv)
            {
                half3 normalTS =
                    UnpackNormalScale(
                        SAMPLE_TEXTURE2D(
                            _NormalMap,
                            sampler_NormalMap,
                            atlasUv),
                        _NormalStrength);

                half3 tangent =
                    normalize(tangentWS.xyz);

                half3 bitangent =
                    normalize(
                        cross(baseNormalWS, tangent)
                        * tangentWS.w);

                half3 normalWS =
                    tangent * normalTS.x
                    + bitangent * normalTS.y
                    + baseNormalWS * normalTS.z;

                return normalize(normalWS);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;

                VertexPositionInputs positionInputs =
                    GetVertexPositionInputs(
                        input.positionOS.xyz);

                output.positionCS =
                    positionInputs.positionCS;

                output.positionWS =
                    positionInputs.positionWS;

                output.normalWS =
                    TransformObjectToWorldNormal(
                        input.normalOS);

                float3 tangentWS =
                    TransformObjectToWorldDir(
                        input.tangentOS.xyz);

                output.tangentWS =
                    half4(
                        normalize(tangentWS),
                        input.tangentOS.w
                        * GetOddNegativeScale());

                output.faceUv =
                    TRANSFORM_TEX(
                        input.uv,
                        _BaseMap);

                output.surfaceType =
                    input.surfaceType.x;

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 atlasUv =
                    GetAtlasUv(
                        input.faceUv,
                        input.surfaceType);

                half4 surfaceColor =
                    SAMPLE_TEXTURE2D(
                        _BaseMap,
                        sampler_BaseMap,
                        atlasUv)
                    * _BaseColor;

                half3 baseNormalWS =
                    normalize(input.normalWS);

                half3 normalWS =
                    GetNormalWSFromNormalMap(
                        baseNormalWS,
                        input.tangentWS,
                        atlasUv);

                float4 shadowCoord =
                    TransformWorldToShadowCoord(
                        input.positionWS);

                Light mainLight =
                    GetMainLight(shadowCoord);

                half directLight =
                    saturate(
                        dot(
                            normalWS,
                            mainLight.direction))
                    * mainLight.shadowAttenuation;

                half3 ambientLight =
                    SampleSH(normalWS)
                    * _AmbientStrength;

                half3 lighting =
                    ambientLight
                    + mainLight.color
                    * directLight;

                return half4(
                    surfaceColor.rgb * lighting,
                    surfaceColor.a);
            }

            ENDHLSL
        }
    }
}