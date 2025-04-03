Shader "BlitWithMaterial"
{
   SubShader
   {
       Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
       ZWrite Off Cull Off
       Pass
       {
           Name "BlitWithMaterialPass"

           HLSLPROGRAM
           #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
           #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

           #pragma vertex Vert
           #pragma fragment Frag

           float4x4 _InverseVP3;
           float4x4 _CameraWorld3;
           float4x4 _LightSpaceMatrix;

           float3 GetWorldPosition(float2 uv)
            {
                // Sample depth
                float depth = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearRepeat, uv, _BlitMipLevel).r;
                // Convert depth to world space coordinates
                float4 clipPos = float4(uv * 2.0 - 1.0, depth, 1.0);
                float4 viewPos = mul(_InverseVP3, clipPos);
                viewPos /= viewPos.w;
                float4 worldPos = mul(_CameraWorld3, float4(viewPos.xyz, 1.0));
                
                return worldPos.xyz;
            }

           // Out frag function takes as input a struct that contains the screen space coordinate we are going to use to sample our texture. It also writes to SV_Target0, this has to match the index set in the UseTextureFragment(sourceTexture, 0, …) we defined in our render pass script.   
           float4 Frag(Varyings input) : SV_Target0
           {
               // this is needed so we account XR platform differences in how they handle texture arrays
               UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

               // sample the texture using the SAMPLE_TEXTURE2D_X_LOD
               float2 uv = input.texcoord.xy;
               float3 color = GetWorldPosition(uv);
               
               // Modify the sampled color
               // return mul(_LightSpaceMatrix, float4(color, 1));
               return _LightSpaceMatrix[0];
               // return float4(SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearRepeat, uv, _BlitMipLevel).r, 0, 0, 1);
               // return float4(_InverseVP3[0].z, 0, 0, 1);
           }

           ENDHLSL
       }
   }
}