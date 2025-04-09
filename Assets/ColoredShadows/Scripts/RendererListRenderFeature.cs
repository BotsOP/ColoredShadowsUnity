using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.Universal;

// This example clears the current active color texture, then renders the scene geometry associated to the m_LayerMask layer.
// Add scene geometry to your own custom layers and experiment switching the layer mask in the render feature UI.
// You can use the frame debugger to inspect the pass output.
public class RendererListRenderFeature : ScriptableRendererFeature
{
    public class RendererListPass : ScriptableRenderPass
    {
        // Layer mask used to filter objects to put in the renderer list
        private LayerMask m_LayerMask;
        private Material overrideMaterial;
        
        // List of shader tags used to build the renderer list
        private List<ShaderTagId> m_ShaderTagIdList = new List<ShaderTagId>();
        RenderStateBlock m_RenderStateBlock;
        FilteringSettings m_FilteringSettings;
        RenderObjects.CustomCameraSettings m_CameraSettings;

        public RendererListPass(RenderPassEvent evt, LayerMask layerMask, Material overrideMaterial)
        {
            renderPassEvent = evt;
            m_LayerMask = layerMask;
            this.overrideMaterial = overrideMaterial;
        }
        
        // This class stores the data needed by the pass, passed as parameter to the delegate function that executes the pass
        // private class PassData
        // {
        //     public RendererListHandle rendererListHandle;
        // }
        
        private class PassData
        {
            internal RenderObjects.CustomCameraSettings cameraSettings;
            internal RenderPassEvent renderPassEvent;

            internal TextureHandle color;
            internal RendererListHandle rendererListHdl;

            internal UniversalCameraData cameraData;

            // Required for code sharing purpose between RG and non-RG.
            internal RendererList rendererList;
        }

        // Sample utility method that showcases how to create a renderer list via the RenderGraph API
        // private void InitRendererLists(ContextContainer frameData, ref PassData passData, RenderGraph renderGraph)
        // {
        //     // Access the relevant frame data from the Universal Render Pipeline
        //     UniversalRenderingData universalRenderingData = frameData.Get<UniversalRenderingData>();
        //     UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
        //     UniversalLightData lightData = frameData.Get<UniversalLightData>();
        //     
        //     var sortFlags = cameraData.defaultOpaqueSortFlags;
        //     RenderQueueRange renderQueueRange = RenderQueueRange.opaque;
        //     FilteringSettings filterSettings = new FilteringSettings(renderQueueRange, m_LayerMask);
        //     
        //     ShaderTagId[] forwardOnlyShaderTagIds = new ShaderTagId[]
        //     {
        //         // new ShaderTagId("UniversalForwardOnly"),
        //         new ShaderTagId("UniversalForward"),
        //         // new ShaderTagId("SRPDefaultUnlit"), // Legacy shaders (do not have a gbuffer pass) are considered forward-only for backward compatibility
        //         // new ShaderTagId("LightweightForward") // Legacy shaders (do not have a gbuffer pass) are considered forward-only for backward compatibility
        //     };
        //     
        //     m_ShaderTagIdList.Clear();
        //     
        //     foreach (ShaderTagId sid in forwardOnlyShaderTagIds)
        //         m_ShaderTagIdList.Add(sid);
        //     
        //     DrawingSettings drawSettings = RenderingUtils.CreateDrawingSettings(m_ShaderTagIdList, universalRenderingData, cameraData, lightData, sortFlags);
        //     drawSettings.overrideMaterial = overrideMaterial;
        //
        //     var param = new RendererListParams(universalRenderingData.cullResults, drawSettings, filterSettings);
        //     passData.rendererListHandle = renderGraph.CreateRendererList(param);
        // }

        private void InitRendererLists(UniversalRenderingData renderingData, UniversalLightData lightData,
            ref PassData passData, ScriptableRenderContext context, RenderGraph renderGraph, bool useRenderGraph)
        {
            SortingCriteria sortingCriteria = passData.cameraData.defaultOpaqueSortFlags;
            DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(m_ShaderTagIdList, renderingData,
                passData.cameraData, lightData, sortingCriteria);
            drawingSettings.overrideMaterial = overrideMaterial;
            drawingSettings.overrideMaterialPassIndex = 0;

            CreateRendererListWithRenderStateBlock(renderGraph, ref renderingData.cullResults, drawingSettings,
                m_FilteringSettings, m_RenderStateBlock, ref passData.rendererListHdl);
        }
        
        private static void ExecutePass(PassData passData, RasterCommandBuffer cmd, RendererList rendererList, bool isYFlipped)
        {
            float cameraAspect = 1;
            
            float near_plane = 0.1f, far_plane = 10f, size = 5, size2 = 1;
            Matrix4x4 lightProjection = Matrix4x4.Ortho(-size, size, -size, size, near_plane, far_plane);
            lightProjection = GL.GetGPUProjectionMatrix(lightProjection, isYFlipped);

            Matrix4x4 viewMatrix = CaptureShadowMap.LookAtLH(
                new Vector3(1, 4, 1),  // Light position
                Vector3.zero,    // Look target (origin)
                Vector3.up     // Up vector
            );
            Vector4 cameraTranslation = viewMatrix.GetColumn(3);
            viewMatrix.SetColumn(3, cameraTranslation);

            SetViewAndProjectionMatrices(cmd, viewMatrix, lightProjection, false);

            cmd.DrawRendererList(rendererList);
        }
        
        // This static method is used to execute the pass and passed as the RenderFunc delegate to the RenderGraph render pass
        // static void ExecutePass(PassData data, RasterGraphContext context)
        // {
        //     context.cmd.ClearRenderTarget(RTClearFlags.Color, Color.white, 1,0);
        //     
        //     context.cmd.DrawRendererList(data.rendererListHandle);
        // }
        
        // This is where the renderGraph handle can be accessed.
        // Each ScriptableRenderPass can use the RenderGraph handle to add multiple render passes to the render graph
        
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData, profilingSampler))
            {
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

                InitPassData(cameraData, ref passData);

                passData.color = resourceData.activeColorTexture;
                builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Write);

                TextureHandle[] dBufferHandles = resourceData.dBuffer;
                for (int i = 0; i < dBufferHandles.Length; ++i)
                {
                    TextureHandle dBuffer = dBufferHandles[i];
                    if (dBuffer.IsValid())
                        builder.UseTexture(dBuffer, AccessFlags.Read);
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
        
        private void InitPassData(UniversalCameraData cameraData, ref PassData passData)
        {
            passData.cameraSettings = m_CameraSettings;
            passData.renderPassEvent = renderPassEvent;
            passData.cameraData = cameraData;
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
    }

    RendererListPass m_ScriptablePass;

    public LayerMask m_LayerMask;
    public Material material;

    /// <inheritdoc/>
    public override void Create()
    {
        m_ScriptablePass = new RendererListPass(RenderPassEvent.AfterRenderingTransparents, m_LayerMask, material);

        // Configures where the render pass should be injected.
        m_ScriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_ScriptablePass);
    }
}


