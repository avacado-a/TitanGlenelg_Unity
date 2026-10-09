Shader "TitanGlenelg/TeamMannequinHDRP"
{
    Properties
    {
        _PrimaryTeamColor ("Primary Team Color", Color) = (0.85, 0.1, 0.1, 1.0) // Glenelg Scarlet Red
        _SecondaryTeamColor ("Secondary Team Color", Color) = (0.05, 0.05, 0.05, 1.0) // Matte Black
        _NumberColor ("Jersey Number Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _Metallic ("Metallic", Range(0, 1)) = 0.2
        _Smoothness ("Smoothness", Range(0, 1)) = 0.6
    }
    SubShader
    {
        Tags { "RenderPipeline"="HDRenderPipeline" "RenderType"="Opaque" }
        LOD 300

        Pass
        {
            Name "ForwardOnly"
            Tags { "LightMode"="ForwardOnly" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _PrimaryTeamColor;
                float4 _SecondaryTeamColor;
                float4 _NumberColor;
                float _Metallic;
                float _Smoothness;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformWorldToHClip(TransformObjectToWorld(input.positionOS));
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                // Dynamic two-tone stylized uniform stripe mapping
                float stripeMask = step(0.48, frac(input.uv.y * 4.0));
                float3 baseCol = lerp(_PrimaryTeamColor.rgb, _SecondaryTeamColor.rgb, stripeMask);
                
                // Simple directional lighting term
                float3 lightDir = normalize(float3(0.5, 1.0, 0.3));
                float NdotL = saturate(dot(normalize(input.normalWS), lightDir));
                float3 litColor = baseCol * (0.3 + 0.7 * NdotL);

                return float4(litColor, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/HDRP/MaterialError"
}
