Shader "TitanGlenelg/FieldLineProjector"
{
    Properties
    {
        _LineColor ("Marker Color", Color) = (0.2, 0.6, 1.0, 0.85) // Blue LOS or Yellow 1st Down
        _LineWidth ("Line Width", Float) = 0.45
        _GlowIntensity ("Glow Intensity", Float) = 1.5
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="HDRenderPipeline" }
        LOD 200
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _LineColor;
                float _LineWidth;
                float _GlowIntensity;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformWorldToHClip(TransformObjectToWorld(input.positionOS));
                output.uv = input.uv;
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                // Soft edge falloff
                float distFromCenter = abs(input.uv.y - 0.5) * 2.0;
                float alpha = saturate(1.0 - pow(distFromCenter, 4.0)) * _LineColor.a;
                float3 col = _LineColor.rgb * _GlowIntensity;
                return float4(col, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
