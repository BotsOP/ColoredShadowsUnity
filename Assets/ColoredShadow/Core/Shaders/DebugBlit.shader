Shader "Custom/test"
{
    Properties
    {
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Resources/CustomShadowsLibrary.hlsl"
            #include "Resources/PackCustomShadowValues.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float _ShadowAtlasUVX;
            float _ShadowAtlasUVY;
            float _ShadowMapSizeX;
            float _ShadowMapSizeY;

            TEXTURE2D(_ColoredShadowMap0);
            TEXTURE2D(_DepthShadowMap);
            SAMPLER(sampler_ColoredShadowMap0);

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                IN.uv.x = remap(IN.uv.x, 0, 1, _ShadowAtlasUVX, _ShadowAtlasUVX + _ShadowMapSizeX);
                IN.uv.y = remap(IN.uv.y, 0, 1, _ShadowAtlasUVY, _ShadowAtlasUVY + _ShadowMapSizeY);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 input = SAMPLE_TEXTURE2D(_ColoredShadowMap0, sampler_ColoredShadowMap0, IN.uv);
                float depth = SAMPLE_TEXTURE2D(_DepthShadowMap, sampler_ColoredShadowMap0, IN.uv);
                uint shadowID = GetShadowID(input);
                return half4(shadowID, depth * 20, 0, 1);
            }
            ENDHLSL
        }
    }
}
