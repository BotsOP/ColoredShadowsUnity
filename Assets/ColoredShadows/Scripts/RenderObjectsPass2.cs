using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using UnityEngine.Scripting.APIUpdating;

    /// <summary>
    /// The scriptable render pass used with the render objects renderer feature.
    /// </summary>
    [MovedFrom(true, "UnityEngine.Experimental.Rendering.Universal")]
    public class RenderObjectsPass2 : ScriptableRenderPass
    {
        public Material overrideMaterial { get; set; }
        public int overrideMaterialPassIndex { get; set; }
        private bool renderDepth;
        private float shadowID;
        RenderQueueType renderQueueType;
        FilteringSettings m_FilteringSettings;
        List<ShaderTagId> m_ShaderTagIdList = new List<ShaderTagId>();
        RenderStateBlock m_RenderStateBlock;
        public void SetDepthState(bool writeEnabled, CompareFunction function = CompareFunction.Less)
        {
            m_RenderStateBlock.mask |= RenderStateMask.Depth;
            m_RenderStateBlock.depthState = new DepthState(writeEnabled, function);
        }


        /// <summary>
        /// The constructor for render objects pass.
        /// </summary>
        /// <param name="profilerTag">The profiler tag used with the pass.</param>
        /// <param name="renderPassEvent">Controls when the render pass executes.</param>
        /// <param name="shaderTags">List of shader tags to render with.</param>
        /// <param name="renderQueueType">The queue type for the objects to render.</param>
        /// <param name="layerMask">The layer mask to use for creating filtering settings that control what objects get rendered.</param>
        /// <param name="cameraSettings">The settings for custom cameras values.</param>
        public RenderObjectsPass2(string profilerTag, RenderPassEvent renderPassEvent, string[] shaderTags, RenderQueueType renderQueueType, int layerMask, 
            bool renderDepth, Material overrideMaterial, int overrideMaterialPassIndex = 0, float shadowID = 1)            
        {
            profilingSampler = new ProfilingSampler(profilerTag);
            this.renderDepth = renderDepth;
            this.overrideMaterial = overrideMaterial;
            this.overrideMaterialPassIndex = overrideMaterialPassIndex;
            this.shadowID = shadowID;
            Init(renderPassEvent, shaderTags, renderQueueType, layerMask);
        }

        internal void Init(RenderPassEvent renderPassEvent, string[] shaderTags, RenderQueueType renderQueueType, int layerMask)
        {
            this.renderPassEvent = renderPassEvent;
            this.renderQueueType = renderQueueType;
            RenderQueueRange renderQueueRange = (renderQueueType == RenderQueueType.Transparent)
                ? RenderQueueRange.transparent
                : RenderQueueRange.opaque;
            m_FilteringSettings = new FilteringSettings(renderQueueRange, layerMask);

            if (shaderTags != null && shaderTags.Length > 0)
            {
                foreach (var tag in shaderTags)
                    m_ShaderTagIdList.Add(new ShaderTagId(tag));
            }
            else
            {
                m_ShaderTagIdList.Add(new ShaderTagId("SRPDefaultUnlit"));
                m_ShaderTagIdList.Add(new ShaderTagId("UniversalForward"));
                m_ShaderTagIdList.Add(new ShaderTagId("UniversalForwardOnly"));
            }

            
            m_RenderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);
        }

        private static void ExecutePass(PassData passData, RasterCommandBuffer cmd, RendererList rendererList, bool isYFlipped)
        {
            // Camera camera = passData.cameraData.camera;
            // Rect pixelRect = passData.cameraData.pixelRect;
            // float cameraAspect = (float)pixelRect.width / (float)pixelRect.height;
            // float cameraAspect = (float)1;
            // Matrix4x4 projectionMatrix = Matrix4x4.Perspective(passData.cameraSettings.cameraFieldOfView, cameraAspect,
            //     camera.nearClipPlane, camera.farClipPlane);
            // projectionMatrix = GL.GetGPUProjectionMatrix(projectionMatrix, isYFlipped);
            //
            // Matrix4x4 viewMatrix = passData.cameraData.GetViewMatrix();
            // Vector4 cameraTranslation = viewMatrix.GetColumn(3);
            // viewMatrix.SetColumn(3, cameraTranslation + passData.cameraSettings.offset);
            //
            // SetViewAndProjectionMatrices(cmd, viewMatrix, projectionMatrix, false);
            // float near_plane = 0.1f, far_plane = 10f, size = 5, size2 = 1;
            // Matrix4x4 lightProjection = Matrix4x4.Ortho(-size, size, -size, size, near_plane, far_plane);
            // Matrix4x4 lightView = CaptureShadowMap.LookAtLH(
            //     new Vector3(1, 4, 1),  // Light position
            //     // new Vector3(cameraData.camera.transform.position.x, cameraData.camera.transform.position.y, cameraData.camera.transform.position.z),  // Light position
            //     Vector3.zero,    // Look target (origin)
            //     Vector3.up     // Up vector
            // );
            // SetViewAndProjectionMatrices(cmd, lightView, lightProjection, false);
            
            cmd.SetGlobalFloat("_ShadowID", passData.shadowID);
            cmd.DrawRendererList(rendererList);
        }

        private class PassData
        {
            internal RenderPassEvent renderPassEvent;

            internal float shadowID;
            internal TextureHandle color;
            internal RendererListHandle rendererListHdl;

            internal UniversalCameraData cameraData;

            // Required for code sharing purpose between RG and non-RG.
            internal RendererList rendererList;
        }

        private void InitPassData(UniversalCameraData cameraData, ref PassData passData)
        {
            passData.renderPassEvent = renderPassEvent;
            passData.cameraData = cameraData;
        }

        private void InitRendererLists(UniversalRenderingData renderingData, UniversalLightData lightData,
            ref PassData passData, ScriptableRenderContext context, RenderGraph renderGraph, bool useRenderGraph)
        {
            SortingCriteria sortingCriteria = (renderQueueType == RenderQueueType.Transparent)
                ? SortingCriteria.CommonTransparent
                : passData.cameraData.defaultOpaqueSortFlags;
            DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(m_ShaderTagIdList, renderingData,
                passData.cameraData, lightData, sortingCriteria);
            // drawingSettings.overrideMaterial = overrideMaterial;
            // drawingSettings.overrideMaterialPassIndex = overrideMaterialPassIndex;
            drawingSettings.enableInstancing = true;
            // drawingSettings.enableDynamicBatching = true;
            drawingSettings.overrideShader = overrideMaterial.shader;
            drawingSettings.overrideShaderPassIndex = overrideMaterialPassIndex;

            if (useRenderGraph)
            {
                CreateRendererListWithRenderStateBlock(renderGraph, ref renderingData.cullResults, drawingSettings,
                    m_FilteringSettings, m_RenderStateBlock, ref passData.rendererListHdl);
            }
            else
            {
                Debug.LogError($"Not using render graph");
            }
        }

        /// <inheritdoc />
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();
            
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            
            TextureHandle destinationColor;
            TextureHandle destination;
            if (renderDepth)
            {
                var customData = frameData.Create<ColoredShadowsRenderFeature.CustomShadowData>();

                var destinationDescColor = renderGraph.GetTextureDesc(resourceData.activeColorTexture);
                destinationDescColor.name = "SOURCE_COLOR";
                destinationColor = renderGraph.CreateTexture(destinationDescColor);
                customData.shadowMapID = destinationColor;
                
                var destinationDescDepth = renderGraph.GetTextureDesc(resourceData.activeDepthTexture);
                destinationDescDepth.name = "SOURCE_DEPTH";
                destination = renderGraph.CreateTexture(destinationDescDepth);
                customData.shadowMapDepthFormatted = destination;
                
                var destinationDescDepth2 = renderGraph.GetTextureDesc(resourceData.cameraDepthTexture);
                destinationDescDepth2.name = "DESTINATION_DEPTH";
                customData.shadowMapColorFormatted = renderGraph.CreateTexture(destinationDescDepth2);
            }
            else
            {
                var customData = frameData.Get<ColoredShadowsRenderFeature.CustomShadowData>();
                destinationColor = customData.shadowMapID;
                destination = customData.shadowMapDepthFormatted;
            }
            

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData, profilingSampler))
            {
                // UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

                cameraData.clearDepth = true;

                InitPassData(cameraData, ref passData);

                if (renderDepth)
                {
                    passData.color = destinationColor;
                    builder.SetRenderAttachmentDepth(destination, AccessFlags.Write);
                }
                else
                {
                    passData.shadowID = shadowID;
                    // overrideMaterial.SetFloat("_ShadowID", shadowID);
                    passData.color = destinationColor;
                    builder.SetRenderAttachment(destinationColor, 0, AccessFlags.Write);
                }

                InitRendererLists(renderingData, lightData, ref passData, default(ScriptableRenderContext), renderGraph, true);

                builder.UseRendererList(passData.rendererListHdl);

                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) =>
                {
                    var isYFlipped = data.cameraData.IsRenderTargetProjectionMatrixFlipped(data.color);
                    ExecutePass(data, rgContext.cmd, data.rendererListHdl, isYFlipped);
                });
            }
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
        
        public static readonly int viewMatrixID = Shader.PropertyToID("unity_MatrixV");
        public static readonly int projectionMatrixID = Shader.PropertyToID("glstate_matrix_projection");
        public static readonly int viewAndProjectionMatrixID = Shader.PropertyToID("unity_MatrixVP");

        static void SetViewAndProjectionMatrices(RasterCommandBuffer cmd, Matrix4x4 viewMatrix, Matrix4x4 projectionMatrix, bool setInverseMatrices)
        {
            Matrix4x4 viewAndProjectionMatrix = projectionMatrix * viewMatrix;
            cmd.SetGlobalMatrix(viewMatrixID, viewMatrix);
            cmd.SetGlobalMatrix(projectionMatrixID, projectionMatrix);
            cmd.SetGlobalMatrix(viewAndProjectionMatrixID, viewAndProjectionMatrix);

            // if (setInverseMatrices)
            // {
            //     Matrix4x4 inverseViewMatrix = Matrix4x4.Inverse(viewMatrix);
            //     Matrix4x4 inverseProjectionMatrix = Matrix4x4.Inverse(projectionMatrix);
            //     Matrix4x4 inverseViewProjection = inverseViewMatrix * inverseProjectionMatrix;
            //     cmd.SetGlobalMatrix(ShaderPropertyId.inverseViewMatrix, inverseViewMatrix);
            //     cmd.SetGlobalMatrix(ShaderPropertyId.inverseProjectionMatrix, inverseProjectionMatrix);
            //     cmd.SetGlobalMatrix(ShaderPropertyId.inverseViewAndProjectionMatrix, inverseViewProjection);
            // }
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
}
