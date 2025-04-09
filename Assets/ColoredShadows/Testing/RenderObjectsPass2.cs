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
        RenderQueueType renderQueueType;
        FilteringSettings m_FilteringSettings;
        RenderObjects2.CustomCameraSettings m_CameraSettings;


        /// <summary>
        /// The override material to use.
        /// </summary>
        public Material overrideMaterial { get; set; }

        /// <summary>
        /// The pass index to use with the override material.
        /// </summary>
        public int overrideMaterialPassIndex { get; set; }

        /// <summary>
        /// The override shader to use.
        /// </summary>
        public Shader overrideShader { get; set; }

        /// <summary>
        /// The pass index to use with the override shader.
        /// </summary>
        public int overrideShaderPassIndex { get; set; }

        List<ShaderTagId> m_ShaderTagIdList = new List<ShaderTagId>();

        /// <summary>
        /// Sets the write and comparison function for depth.
        /// </summary>
        /// <param name="writeEnabled">Sets whether it should write to depth or not.</param>
        /// <param name="function">The depth comparison function to use.</param>
        public void SetDepthState(bool writeEnabled, CompareFunction function = CompareFunction.Less)
        {
            m_RenderStateBlock.mask |= RenderStateMask.Depth;
            m_RenderStateBlock.depthState = new DepthState(writeEnabled, function);
        }

        /// <summary>
        /// Sets up the stencil settings for the pass.
        /// </summary>
        /// <param name="reference">The stencil reference value.</param>
        /// <param name="compareFunction">The comparison function to use.</param>
        /// <param name="passOp">The stencil operation to use when the stencil test passes.</param>
        /// <param name="failOp">The stencil operation to use when the stencil test fails.</param>
        /// <param name="zFailOp">The stencil operation to use when the stencil test fails because of depth.</param>
        public void SetStencilState(int reference, CompareFunction compareFunction, StencilOp passOp, StencilOp failOp, StencilOp zFailOp)
        {
            StencilState stencilState = StencilState.defaultValue;
            stencilState.enabled = true;
            stencilState.SetCompareFunction(compareFunction);
            stencilState.SetPassOperation(passOp);
            stencilState.SetFailOperation(failOp);
            stencilState.SetZFailOperation(zFailOp);

            m_RenderStateBlock.mask |= RenderStateMask.Stencil;
            m_RenderStateBlock.stencilReference = reference;
            m_RenderStateBlock.stencilState = stencilState;
        }

        RenderStateBlock m_RenderStateBlock;

        /// <summary>
        /// The constructor for render objects pass.
        /// </summary>
        /// <param name="profilerTag">The profiler tag used with the pass.</param>
        /// <param name="renderPassEvent">Controls when the render pass executes.</param>
        /// <param name="shaderTags">List of shader tags to render with.</param>
        /// <param name="renderQueueType">The queue type for the objects to render.</param>
        /// <param name="layerMask">The layer mask to use for creating filtering settings that control what objects get rendered.</param>
        /// <param name="cameraSettings">The settings for custom cameras values.</param>
        public RenderObjectsPass2(string profilerTag, RenderPassEvent renderPassEvent, string[] shaderTags, RenderQueueType renderQueueType, int layerMask, RenderObjects2.CustomCameraSettings cameraSettings)            
        {
            profilingSampler = new ProfilingSampler(profilerTag);
            Init(renderPassEvent, shaderTags, renderQueueType, layerMask, cameraSettings);
        }

        internal RenderObjectsPass2(URPProfileId profileId, RenderPassEvent renderPassEvent, string[] shaderTags, RenderQueueType renderQueueType, int layerMask, RenderObjects2.CustomCameraSettings cameraSettings)
        {
            profilingSampler = ProfilingSampler.Get(profileId);
            Init(renderPassEvent, shaderTags, renderQueueType, layerMask, cameraSettings);
        }

        internal void Init(RenderPassEvent renderPassEvent, string[] shaderTags, RenderQueueType renderQueueType, int layerMask, RenderObjects2.CustomCameraSettings cameraSettings)
        {
            this.renderPassEvent = renderPassEvent;
            this.renderQueueType = renderQueueType;
            this.overrideMaterial = null;
            this.overrideMaterialPassIndex = 0;
            this.overrideShader = null;
            this.overrideShaderPassIndex = 0;
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
            m_CameraSettings = cameraSettings;
        }

        private static void ExecutePass(PassData passData, RasterCommandBuffer cmd, RendererList rendererList, bool isYFlipped)
        {
            Camera camera = passData.cameraData.camera;

            // In case of camera stacking we need to take the viewport rect from base camera
            // Rect pixelRect = passData.cameraData.pixelRect;
            // float cameraAspect = (float)pixelRect.width / (float)pixelRect.height;
            float cameraAspect = (float)1;
           
            if (passData.cameraSettings.overrideCamera)
            {
                if (passData.cameraData.xr.enabled)
                {
                    Debug.LogWarning("RenderObjects pass is configured to override camera matrices. While rendering in stereo camera matrices cannot be overridden.");
                }
                else
                {
                    Matrix4x4 projectionMatrix = Matrix4x4.Perspective(passData.cameraSettings.cameraFieldOfView, cameraAspect,
                        camera.nearClipPlane, camera.farClipPlane);
                    projectionMatrix = GL.GetGPUProjectionMatrix(projectionMatrix, isYFlipped);

                    Matrix4x4 viewMatrix = passData.cameraData.GetViewMatrix();
                    Vector4 cameraTranslation = viewMatrix.GetColumn(3);
                    viewMatrix.SetColumn(3, cameraTranslation + passData.cameraSettings.offset);

                    SetViewAndProjectionMatrices(cmd, viewMatrix, projectionMatrix, false);
                }
            }

            float near_plane = 0.1f, far_plane = 10f, size = 5, size2 = 1;
            Matrix4x4 lightProjection = Matrix4x4.Ortho(-size, size, -size, size, near_plane, far_plane);
            Matrix4x4 lightView = CaptureShadowMap.LookAtLH(
                new Vector3(1, 4, 1),  // Light position
                // new Vector3(cameraData.camera.transform.position.x, cameraData.camera.transform.position.y, cameraData.camera.transform.position.z),  // Light position
                Vector3.zero,    // Look target (origin)
                Vector3.up     // Up vector
            );
            // SetViewAndProjectionMatrices(cmd, lightView, lightProjection, false);
            
            cmd.DrawRendererList(rendererList);

            if (passData.cameraSettings.overrideCamera && passData.cameraSettings.restoreCamera && !passData.cameraData.xr.enabled)
            {
                SetViewAndProjectionMatrices(cmd, passData.cameraData.GetViewMatrix(), GL.GetGPUProjectionMatrix(passData.cameraData.GetProjectionMatrix(0), isYFlipped), false);
            }            
        }

        private class PassData
        {
            internal RenderObjects2.CustomCameraSettings cameraSettings;
            internal RenderPassEvent renderPassEvent;

            internal TextureHandle color;
            internal RendererListHandle rendererListHdl;

            internal UniversalCameraData cameraData;

            // Required for code sharing purpose between RG and non-RG.
            internal RendererList rendererList;
        }

        private void InitPassData(UniversalCameraData cameraData, ref PassData passData)
        {
            passData.cameraSettings = m_CameraSettings;
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
            drawingSettings.overrideMaterial = overrideMaterial;
            drawingSettings.overrideMaterialPassIndex = overrideMaterialPassIndex;
            drawingSettings.overrideShader = overrideShader;
            drawingSettings.overrideShaderPassIndex = overrideShaderPassIndex;

            if (useRenderGraph)
            {
                CreateRendererListWithRenderStateBlock(renderGraph, ref renderingData.cullResults, drawingSettings,
                    m_FilteringSettings, m_RenderStateBlock, ref passData.rendererListHdl);
            }
            else
            {
                Debug.Log($"no");
            }
        }

        /// <inheritdoc />
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData, profilingSampler))
            {
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

                cameraData.clearDepth = true;

                InitPassData(cameraData, ref passData);

                passData.color = resourceData.activeColorTexture;
                // builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);
                
                var sourceDepth = resourceData.activeDepthTexture;
                var destinationDescDepth = renderGraph.GetTextureDesc(sourceDepth);
                destinationDescDepth.name = "SOURCE_DEPTH";
                TextureHandle destination = renderGraph.CreateTexture(destinationDescDepth);
                var customData = frameData.Get<ColoredShadowsRenderFeature.MyCustomData>();
                customData.cameraColor2 = destination;
                
                var destinationDescDepth2 = renderGraph.GetTextureDesc(resourceData.cameraDepthTexture);
                destinationDescDepth2.name = "DESTINATION_DEPTH";
                TextureHandle destination2 = renderGraph.CreateTexture(destinationDescDepth2);
                customData.cameraColor3 = destination2;
                
                builder.SetRenderAttachmentDepth(destination, AccessFlags.Write);

                TextureHandle mainShadowsTexture = resourceData.mainShadowsTexture;
                TextureHandle additionalShadowsTexture = resourceData.additionalShadowsTexture;

                if (mainShadowsTexture.IsValid())
                    builder.UseTexture(mainShadowsTexture, AccessFlags.Read);

                if (additionalShadowsTexture.IsValid())
                    builder.UseTexture(additionalShadowsTexture, AccessFlags.Read);

                TextureHandle[] dBufferHandles = resourceData.dBuffer;
                for (int i = 0; i < dBufferHandles.Length; ++i)
                {
                    TextureHandle dBuffer = dBufferHandles[i];
                    if (dBuffer.IsValid())
                        builder.UseTexture(dBuffer, AccessFlags.Read);
                }

                TextureHandle ssaoTexture = resourceData.ssaoTexture;
                if (ssaoTexture.IsValid())
                    builder.UseTexture(ssaoTexture, AccessFlags.Read);

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
        
        [System.Serializable]
        public class CustomCameraSettings
        {
            /// <summary>
            /// Used to mark whether camera values should be changed or not.
            /// </summary>
            public bool overrideCamera = false;

            /// <summary>
            /// Should the values be reverted after rendering the objects?
            /// </summary>
            public bool restoreCamera = true;

            /// <summary>
            /// Changes the camera offset.
            /// </summary>
            public Vector4 offset;

            /// <summary>
            /// Changes the camera field of view.
            /// </summary>
            public float cameraFieldOfView = 60.0f;
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
