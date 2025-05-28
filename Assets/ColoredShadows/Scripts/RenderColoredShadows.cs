using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace ColoredShadows.Scripts
{
    public class RenderColoredShadows : ScriptableRenderPass
    {
        private Material overrideMaterial;
        private int overrideMaterialPassIndex;
        public CustomLight customLight;
    
        private RTHandle shadowMapID;
        private RTHandle shadowMapDepth;
        private const string shadowMapIDName = "_CustomShadowMapID";
        private const string shadowMapDepthName = "_CustomShadowMapDepth";
    
        private RenderQueueType renderQueueType;
        private FilteringSettings filteringSettings;
        private FilteringSettings filteringSettingsID;
        private List<ShaderTagId> shaderTagIdList = new List<ShaderTagId>();
        private RenderStateBlock renderStateBlock;
        private LightInformation[] lightInformations;
        private GraphicsBuffer lightInformationBuffer;
    
        public RenderColoredShadows(string profilerTag, RenderPassEvent renderPassEvent, string[] shaderTags, RenderQueueType renderQueueType, 
            int layerMask, int layerMaskID, Material overrideMaterial, LightInformation[] lightInformations, GraphicsBuffer lightInformationBuffer, int overrideMaterialPassIndex = 0)            
        {
            profilingSampler = new ProfilingSampler(profilerTag);
            this.overrideMaterial = overrideMaterial;
            this.overrideMaterialPassIndex = overrideMaterialPassIndex;
            Init(renderPassEvent, shaderTags, renderQueueType, layerMask, layerMaskID);
        
            Debug.Log(ColoredShadowsRenderFeature.MAX_AMOUNT_CUSTOM_LIGHTS);
            this.lightInformations = lightInformations;
            this.lightInformationBuffer = lightInformationBuffer;
        }

        internal void Init(RenderPassEvent renderPassEvent, string[] shaderTags, RenderQueueType renderQueueType, int layerMask, int layerMaskID)
        {
            this.renderPassEvent = renderPassEvent;
            this.renderQueueType = renderQueueType;
            RenderQueueRange renderQueueRange = (renderQueueType == RenderQueueType.Transparent)
                ? RenderQueueRange.transparent
                : RenderQueueRange.opaque;
            filteringSettings = new FilteringSettings(renderQueueRange, layerMask);
            filteringSettingsID = new FilteringSettings(renderQueueRange, layerMask);

            if (shaderTags != null && shaderTags.Length > 0)
            {
                foreach (var tag in shaderTags)
                    shaderTagIdList.Add(new ShaderTagId(tag));
            }
            else
            {
                shaderTagIdList.Add(new ShaderTagId("SRPDefaultUnlit"));
                shaderTagIdList.Add(new ShaderTagId("UniversalForward"));
                shaderTagIdList.Add(new ShaderTagId("UniversalForwardOnly"));
            }
        
            renderStateBlock = new RenderStateBlock(RenderStateMask.Depth);
            renderStateBlock.depthState = new DepthState(true, CompareFunction.Less);
        }

        private static void ExecutePass(PassData passData, RasterCommandBuffer cmd, bool isYFlipped)
        {
            Matrix4x4 projectionMatrix = passData.projectionMatrix;
            // cmd.SetViewProjectionMatrices(passData.viewMatrix, projectionMatrix);
            // projectionMatrix = GL.GetGPUProjectionMatrix(projectionMatrix, !isYFlipped);
            cmd.DisableScissorRect();
            float resolutionSizeX = passData.textureSize.x;
            float resolutionSizeY = passData.textureSize.y;
        
            cmd.SetViewport(new Rect(0, 0, resolutionSizeX, resolutionSizeY));
            cmd.SetViewProjectionMatrices(passData.viewMatrix, projectionMatrix);
            cmd.DrawRendererList(passData.rendererListHdl1);
            
            if (passData.lightMode == CustomLightData.LightMode.Point)
            {
                cmd.SetGlobalVector("_ColoredLightPos", passData.lightPos);
                cmd.SetGlobalFloat("_ColoredLightFarPlane", passData.farPlane);
                
                cmd.SetViewport(new Rect(resolutionSizeX * 1, 0, resolutionSizeX, resolutionSizeY));
                cmd.SetViewProjectionMatrices(Matrix4x4.Rotate(Quaternion.Euler(0, 90, 0)) * passData.viewMatrix, projectionMatrix);
                cmd.DrawRendererList(passData.rendererListHdl2);

                cmd.SetViewport(new Rect(resolutionSizeX * 2, 0, resolutionSizeX, resolutionSizeY));
                cmd.SetViewProjectionMatrices(Matrix4x4.Rotate(Quaternion.Euler(0, 180, 0)) * passData.viewMatrix, projectionMatrix);
                cmd.DrawRendererList(passData.rendererListHdl3);

                cmd.SetViewport(new Rect(resolutionSizeX * 3, 0, resolutionSizeX, resolutionSizeY));
                cmd.SetViewProjectionMatrices(Matrix4x4.Rotate(Quaternion.Euler(0, 270, 0)) * passData.viewMatrix, projectionMatrix);
                cmd.DrawRendererList(passData.rendererListHdl4);

                cmd.SetViewport(new Rect(resolutionSizeX * 4, 0, resolutionSizeX, resolutionSizeY));
                cmd.SetViewProjectionMatrices(Matrix4x4.Rotate(Quaternion.Euler(90, 0, 0)) * passData.viewMatrix, projectionMatrix);
                cmd.DrawRendererList(passData.rendererListHdl5);

                cmd.SetViewport(new Rect(resolutionSizeX * 5, 0, resolutionSizeX, resolutionSizeY));
                cmd.SetViewProjectionMatrices(Matrix4x4.Rotate(Quaternion.Euler(270, 0, 0)) * passData.viewMatrix, projectionMatrix);
                cmd.DrawRendererList(passData.rendererListHdl6);
            }
        }

        private void InitPassData(UniversalCameraData cameraData, ref PassData passData)
        {
            passData.cameraData = cameraData;
        }

        private void InitRendererLists(UniversalRenderingData renderingData, UniversalLightData lightData,
            ref PassData passData, RenderGraph renderGraph, FilteringSettings filteringSettings)
        {
            SortingCriteria sortingCriteria = (renderQueueType == RenderQueueType.Transparent)
                ? SortingCriteria.CommonTransparent
                : passData.cameraData.defaultOpaqueSortFlags;
            DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(shaderTagIdList, renderingData,
                passData.cameraData, lightData, sortingCriteria);
            drawingSettings.enableInstancing = true;
            drawingSettings.enableDynamicBatching = true;
            drawingSettings.overrideShader = overrideMaterial.shader;
            drawingSettings.overrideShaderPassIndex = overrideMaterialPassIndex;

            CreateRendererListWithRenderStateBlock(renderGraph, ref renderingData.cullResults, drawingSettings, filteringSettings, renderStateBlock, ref passData.rendererListHdl1);
            if (customLight.lightData.lightMode == CustomLightData.LightMode.Point)
            {
                CreateRendererListWithRenderStateBlock(renderGraph, ref renderingData.cullResults, drawingSettings, filteringSettings, renderStateBlock, ref passData.rendererListHdl2);
                CreateRendererListWithRenderStateBlock(renderGraph, ref renderingData.cullResults, drawingSettings, filteringSettings, renderStateBlock, ref passData.rendererListHdl3);
                CreateRendererListWithRenderStateBlock(renderGraph, ref renderingData.cullResults, drawingSettings, filteringSettings, renderStateBlock, ref passData.rendererListHdl4);
                CreateRendererListWithRenderStateBlock(renderGraph, ref renderingData.cullResults, drawingSettings, filteringSettings, renderStateBlock, ref passData.rendererListHdl5);
                CreateRendererListWithRenderStateBlock(renderGraph, ref renderingData.cullResults, drawingSettings, filteringSettings, renderStateBlock, ref passData.rendererListHdl6);
            }
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalLightData universalLightData = frameData.Get<UniversalLightData>();
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

            Matrix4x4 viewMatrix = Matrix4x4.zero;
            Matrix4x4 projectionMatrix = Matrix4x4.zero;
            TextureHandle destinationColor;
            TextureHandle destinationDepth;
            TextureHandle destinationColorRT;
            int textureXMultiplier = 1;
        
            CustomLightData lightData = customLight.lightData;
            switch (lightData.lightMode)
            {
                case CustomLightData.LightMode.Point:
                    float lightRadius = lightData.radius;
                    Matrix4x4 viewCullingMatrix = GetViewMatrix(customLight.transform.position - Vector3.forward * lightRadius, Quaternion.identity);
                    Matrix4x4 projectionCullingMatrix = Matrix4x4.Ortho(-lightRadius, lightRadius, -lightRadius, lightRadius, 0.1f, lightRadius * 2);
                    cameraData.camera.cullingMatrix = projectionCullingMatrix * viewCullingMatrix;
                    viewMatrix = GetViewMatrix(customLight.transform.position, Quaternion.identity);
                    projectionMatrix = Matrix4x4.Perspective(90, 1, 0.1f, 9999f);
                    textureXMultiplier = 6;
                    break;
                case CustomLightData.LightMode.Directional:
                    viewMatrix = GetViewMatrix(customLight.transform.position, customLight.transform.rotation);
                    projectionMatrix = Matrix4x4.Ortho(
                        -customLight.lightData.horizontalSize,
                        customLight.lightData.horizontalSize,
                        -customLight.lightData.verticalSize,
                        customLight.lightData.verticalSize,
                        customLight.lightData.nearPlane,
                        customLight.lightData.farPlane
                    );
                    cameraData.camera.cullingMatrix = projectionMatrix * viewMatrix;
                    break;
                case CustomLightData.LightMode.Spot:
                    viewMatrix = GetViewMatrix(customLight.transform.position, customLight.transform.rotation);
                    projectionMatrix = Matrix4x4.Perspective(
                        customLight.lightData.fov,
                        customLight.lightData.aspectRatio,
                        customLight.lightData.nearPlane,
                        customLight.lightData.farPlane
                    );
                    cameraData.camera.cullingMatrix = projectionMatrix * viewMatrix;
                    break;
            }
        
            var destinationDescColor = renderGraph.GetTextureDesc(resourceData.activeColorTexture);
            // destinationDescColor.colorFormat = GraphicsFormat.R32G32B32A32_SInt;
            destinationDescColor.format = GraphicsFormat.R32G32B32A32_SInt;
            // destinationDescColor.colorFormat = GraphicsFormat.R32G32B32A32_SFloat;
            destinationDescColor.name = "SOURCE_COLOR";
            // destinationDescColor.filterMode = FilterMode.Point;
            destinationDescColor.width = customLight.shadowTextureSize.x * textureXMultiplier;
            destinationDescColor.height = customLight.shadowTextureSize.y;
            destinationColor = renderGraph.CreateTexture(destinationDescColor);
        
            var destinationDescDepth = renderGraph.GetTextureDesc(resourceData.activeDepthTexture);
            destinationDescDepth.name = "SOURCE_DEPTH";
            // destinationDescDepth.filterMode = FilterMode.Point;
            destinationDescDepth.width = customLight.shadowTextureSize.x * textureXMultiplier;
            destinationDescDepth.height = customLight.shadowTextureSize.y;
            destinationDescDepth.isShadowMap = true;
            destinationDepth = renderGraph.CreateTexture(destinationDescDepth);
        
            RenderTextureDescriptor shadowMapIDDesc = cameraData.cameraTargetDescriptor;
            shadowMapIDDesc.colorFormat = RenderTextureFormat.ARGBInt;
            shadowMapIDDesc.colorFormat = RenderTextureFormat.ARGBFloat;
            shadowMapIDDesc.width = customLight.shadowTextureSize.x * textureXMultiplier;
            shadowMapIDDesc.height = customLight.shadowTextureSize.y;
            shadowMapIDDesc.depthBufferBits = 0;
            shadowMapIDDesc.msaaSamples = 1;
            RenderingUtils.ReAllocateHandleIfNeeded(ref shadowMapID, shadowMapIDDesc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: shadowMapIDName + customLight.lightIndex );
            destinationColorRT = renderGraph.ImportTexture(shadowMapID);
        
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData, profilingSampler))
            {
                InitPassData(cameraData, ref passData);

                passData.color = destinationColor;
                builder.SetRenderAttachment(destinationColor, 0, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(destinationDepth, AccessFlags.Write);

                passData.lightMode = customLight.lightData.lightMode;
                passData.textureSize = customLight.shadowTextureSize;
                passData.projectionMatrix = projectionMatrix;
                passData.viewMatrix = viewMatrix;
                passData.lightPos = customLight.transform.position;
                passData.farPlane = customLight.lightData.farPlane;
            
                InitRendererLists(renderingData, universalLightData, ref passData, renderGraph, filteringSettingsID);
            
                builder.UseRendererList(passData.rendererListHdl1);
                if (customLight.lightData.lightMode == CustomLightData.LightMode.Point)
                {
                    builder.UseRendererList(passData.rendererListHdl2);
                    builder.UseRendererList(passData.rendererListHdl3);
                    builder.UseRendererList(passData.rendererListHdl4);
                    builder.UseRendererList(passData.rendererListHdl5);
                    builder.UseRendererList(passData.rendererListHdl6);
                }

                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);
            
                builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) =>
                {
                    var isYFlipped = data.cameraData.IsRenderTargetProjectionMatrixFlipped(data.color);
                    ExecutePass(data, rgContext.cmd, isYFlipped);
                });
            }
        
            RenderGraphUtils.BlitMaterialParameters para2 = new(destinationColor, destinationColorRT, Blitter.GetBlitMaterial(TextureDimension.Tex2D), 0);
            renderGraph.AddBlitPass(para2, "CaptureShadowsColor");

            Shader.SetGlobalTexture("_ColoredShadowMap" + customLight.lightIndex, shadowMapID);
            Shader.SetGlobalVector("_ColoredLightPos", customLight.transform.position);

            lightInformations[customLight.lightIndex] = new LightInformation(
                customLight.lightIndex,
                (int)customLight.lightData.lightMode,
                GL.GetGPUProjectionMatrix(projectionMatrix, false) * viewMatrix,
                customLight.transform.position,
                customLight.lightData.fallOffRange,
                customLight.lightData.farPlane,
                cameraData.camera.transform.position,
                customLight.shadowTextureSize.x,
                customLight.shadowTextureSize.y
            );
            if (customLight.lightIndex == 0)
            {
                lightInformationBuffer.SetData(lightInformations);
            }
            Shader.SetGlobalBuffer("ColoredShadowLightInformation", lightInformationBuffer);
        }
    
        static ShaderTagId[] s_ShaderTagValues = new ShaderTagId[1];
        static RenderStateBlock[] s_RenderStateBlocks = new RenderStateBlock[1];
        static void CreateRendererListWithRenderStateBlock(RenderGraph renderGraph, ref CullingResults cullResults, DrawingSettings ds, FilteringSettings fs, RenderStateBlock rsb, ref RendererListHandle rl)
        {
            s_ShaderTagValues[0] = ShaderTagId.none;
            s_RenderStateBlocks[0] = rsb;
            NativeArray<ShaderTagId> tagValues = new NativeArray<ShaderTagId>(s_ShaderTagValues, Allocator.Temp);
            NativeArray<RenderStateBlock> stateBlocks = new NativeArray<RenderStateBlock>(s_RenderStateBlocks, Allocator.Temp);
            var param = new RendererListParams(cullResults, ds, fs)
            {
                tagValues = tagValues,
                stateBlocks = stateBlocks,
                isPassTagName = false
            };
            rl = renderGraph.CreateRendererList(param);
        }
    
        private class PassData
        {
            internal TextureHandle color;
            internal Matrix4x4 viewMatrix;
            internal Matrix4x4 projectionMatrix;
            internal CustomLightData.LightMode lightMode;
            internal Vector3 lightPos;
            internal float farPlane;

            internal RendererListHandle rendererListHdl1;
            internal RendererListHandle rendererListHdl2;
            internal RendererListHandle rendererListHdl3;
            internal RendererListHandle rendererListHdl4;
            internal RendererListHandle rendererListHdl5;
            internal RendererListHandle rendererListHdl6;

            internal UniversalCameraData cameraData;
            internal Vector2Int textureSize;

            // Required for code sharing purpose between RG and non-RG.
            internal RendererList rendererList;
            internal int mainLightIndex;
        }

        public struct LightInformation
        {
            public int index;
            public int lightMode;
            public Matrix4x4 lightMatrix;
            public Vector3 lightPos;
            public float fallOffRange;
            public float farPlane;
            public Vector3 cameraPos;
            public int textureSizeX;
            public int textureSizeY;
            public LightInformation(int index, int lightMode, Matrix4x4 lightMatrix, Vector3 lightPos, float fallOffRange, float farPlane, Vector3 cameraPos, int textureSizeX, int textureSizeY)
            {
                this.index = index;
                this.lightMode = lightMode;
                this.lightMatrix = lightMatrix;
                this.lightPos = lightPos;
                this.fallOffRange = fallOffRange;
                this.farPlane = farPlane;
                this.cameraPos = cameraPos;
                this.textureSizeX = textureSizeX;
                this.textureSizeY = textureSizeY;
            }
        }

        public void Dispose()
        {
            shadowMapID?.Release();
            shadowMapID = null;
        }

        #region BoilerPlate

        // public static readonly int viewMatrixID = Shader.PropertyToID("unity_MatrixV");
        // public static readonly int projectionMatrixID = Shader.PropertyToID("glstate_matrix_projection");
        // public static readonly int viewAndProjectionMatrixID = Shader.PropertyToID("unity_MatrixVP");
        //
        // static void SetViewAndProjectionMatrices(RasterCommandBuffer cmd, Matrix4x4 viewMatrix, Matrix4x4 projectionMatrix)
        // {
        //     Matrix4x4 viewAndProjectionMatrix = projectionMatrix * viewMatrix;
        //     cmd.SetGlobalMatrix(viewMatrixID, viewMatrix);
        //     cmd.SetGlobalMatrix(projectionMatrixID, projectionMatrix);
        //     cmd.SetGlobalMatrix(viewAndProjectionMatrixID, viewAndProjectionMatrix);
        // }
    
        public static Matrix4x4 GetViewMatrix(Vector3 cameraPosition, Quaternion cameraRotation)
        {
            Matrix4x4 rotationMatrix = Matrix4x4.Rotate(Quaternion.Inverse(cameraRotation));
            Matrix4x4 translationMatrix = Matrix4x4.Translate(-cameraPosition);
            Matrix4x4 viewMatrix = rotationMatrix * translationMatrix;
            viewMatrix.m20 *= -1;
            viewMatrix.m21 *= -1;
            viewMatrix.m22 *= -1;
            viewMatrix.m23 *= -1;
        
            return viewMatrix;
        }
    
        public static Matrix4x4 LookAtLH(Vector3 eye, Vector3 center, Vector3 up)
        {
            Vector3 f = (center - eye).normalized;        // Forward (Z+)
            Vector3 s = Vector3.Cross(up, f).normalized;  // Right (X+)
            Vector3 u = Vector3.Cross(f, s);              // Up (Y+)

            Matrix4x4 result = Matrix4x4.identity;

            result[0, 0] = -s.x;
            result[0, 1] = -s.y;
            result[0, 2] = -s.z;

            result[1, 0] = u.x;
            result[1, 1] = u.y;
            result[1, 2] = u.z;

            result[2, 0] = f.x;
            result[2, 1] = f.y;
            result[2, 2] = f.z;

            result[3, 0] = -Vector3.Dot(s, eye);
            result[3, 1] = -Vector3.Dot(u, eye);
            result[2, 3] = -Vector3.Dot(f, eye);

            return result;
        }

    
    
        internal enum URPProfileId
        {
            // CPU
            UniversalRenderTotal,
            UpdateVolumeFramework,
            RenderCameraStack,

            // GPU
            AdditionalLightsShadow,
            ColorGradingLUT,
            CopyColor,
            CopyDepth,
            DrawDepthNormalPrepass,
            DepthPrepass,
            UpdateReflectionProbeAtlas,

            // DrawObjectsPass
            DrawOpaqueObjects,
            DrawTransparentObjects,
            DrawScreenSpaceUI,

            //Full Record Render Graph
            RecordRenderGraph,

            // RenderObjectsPass
            //RenderObjects,

            LightCookies,

            MainLightShadow,
            ResolveShadows,
            SSAO,

            // PostProcessPass
            StopNaNs,
            SMAA,
            GaussianDepthOfField,
            BokehDepthOfField,
            TemporalAA,
            MotionBlur,
            PaniniProjection,
            UberPostProcess,
            Bloom,
            LensFlareDataDrivenComputeOcclusion,
            LensFlareDataDriven,
            LensFlareScreenSpace,
            DrawMotionVectors,
            DrawFullscreen,

            // PostProcessPass RenderGraph
            [HideInDebugUI] RG_SetupPostFX,
            [HideInDebugUI] RG_StopNaNs,
            [HideInDebugUI] RG_SMAAMaterialSetup,
            [HideInDebugUI] RG_SMAAEdgeDetection,
            [HideInDebugUI] RG_SMAABlendWeight,
            [HideInDebugUI] RG_SMAANeighborhoodBlend,
            [HideInDebugUI] RG_SetupDoF,
            [HideInDebugUI] RG_DOFComputeCOC,
            [HideInDebugUI] RG_DOFDownscalePrefilter,
            [HideInDebugUI] RG_DOFBlurH,
            [HideInDebugUI] RG_DOFBlurV,
            [HideInDebugUI] RG_DOFBlurBokeh,
            [HideInDebugUI] RG_DOFPostFilter,
            [HideInDebugUI] RG_DOFComposite,
            [HideInDebugUI] RG_TAA,
            [HideInDebugUI] RG_TAACopyHistory,
            [HideInDebugUI] RG_MotionBlur,
            [HideInDebugUI] RG_BloomSetup,
            [HideInDebugUI] RG_BloomPrefilter,
            [HideInDebugUI] RG_BloomDownsample,
            [HideInDebugUI] RG_BloomUpsample,
            [HideInDebugUI] RG_UberPostSetupBloomPass,
            [HideInDebugUI] RG_UberPost,
            [HideInDebugUI] RG_FinalSetup,
            [HideInDebugUI] RG_FinalFSRScale,
            [HideInDebugUI] RG_FinalBlit,

            BlitFinalToBackBuffer,
            DrawSkybox
        }

        #endregion
    }
}
