using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal.Internal
{
    /// <summary>
    /// Copy the given depth buffer into the given destination depth buffer.
    ///
    /// You can use this pass to copy a depth buffer to a destination,
    /// so you can use it later in rendering. If the source texture has MSAA
    /// enabled, the pass uses a custom MSAA resolve. If the source texture
    /// does not have MSAA enabled, the pass uses a Blit or a Copy Texture
    /// operation, depending on what the current platform supports.
    /// </summary>
    public class CopyDepthPass2 : ScriptableRenderPass
    {
        private RTHandle source { get; set; }
        private RTHandle destination { get; set; }
        internal int MssaSamples { get; set; }
        // In some cases (Scene view, XR and etc.) we actually want to output to depth buffer
        // So this variable needs to be set to true to enable the correct copy shader semantic
        internal bool CopyToDepth { get; set; }
        // In XR CopyDepth, we need a special workaround to handle dummy color issue in RenderGraph.
        internal bool CopyToDepthXR { get; set; }
        // We need to know if we're copying to the backbuffer in order to handle y-flip correctly
        internal bool CopyToBackbuffer { get; set; }
        Material m_CopyDepthMaterial;

        internal bool m_CopyResolvedDepth;
        internal bool m_ShouldClear;
        private PassData m_PassData;

        /// <summary>
        /// Shader resource ids used to communicate with the shader implementation
        /// </summary>
        static class ShaderConstants
        {
            public static readonly int _CameraDepthAttachment = Shader.PropertyToID("_CameraDepthAttachment");
            public static readonly int _CameraDepthTexture = Shader.PropertyToID("_CameraDepthTexture");
            public static readonly int _ZWriteShaderHandle = Shader.PropertyToID("_ZWrite");
        }

        /// <summary>
        /// Creates a new <c>CopyDepthPass</c> instance.
        /// </summary>
        /// <param name="evt">The <c>RenderPassEvent</c> to use.</param>
        /// <param name="copyDepthShader">The <c>Shader</c> to use for copying the depth.</param>
        /// <param name="shouldClear">Controls whether it should do a clear before copying the depth.</param>
        /// <param name="copyToDepth">Controls whether it should do a copy to a depth format target.</param>
        /// <param name="copyResolvedDepth">Set to true if the source depth is MSAA resolved.</param>
        /// <param name="customPassName">An optional custom profiling name to disambiguate multiple copy passes.</param>
        /// <seealso cref="RenderPassEvent"/>
        public CopyDepthPass2(RenderPassEvent evt, Shader copyDepthShader, bool shouldClear = false, bool copyToDepth = false, bool copyResolvedDepth = false, string customPassName = null)
        {
            profilingSampler = new ProfilingSampler(customPassName);
            m_PassData = new PassData();
            CopyToDepth = copyToDepth;
            m_CopyDepthMaterial = copyDepthShader != null ? CoreUtils.CreateEngineMaterial(copyDepthShader) : null;
            renderPassEvent = evt;
            m_CopyResolvedDepth = copyResolvedDepth;
            m_ShouldClear = shouldClear;
            CopyToDepthXR = false;
            CopyToBackbuffer = false;
        }

        /// <summary>
        /// Configure the pass with the source and destination to execute on.
        /// </summary>
        /// <param name="source">Source Render Target</param>
        /// <param name="destination">Destination Render Target</param>
        public void Setup(RTHandle source, RTHandle destination)
        {
            this.source = source;
            this.destination = destination;
            this.MssaSamples = -1;
        }

        /// <summary>
        /// Cleans up resources used by the pass.
        /// </summary>
        public void Dispose()
        {
            CoreUtils.Destroy(m_CopyDepthMaterial);
        }

        private class PassData
        {
            internal TextureHandle source;
            internal UniversalCameraData cameraData;
            internal Material copyDepthMaterial;
            internal int msaaSamples;
            internal bool copyResolvedDepth;
            internal bool copyToDepth;
            internal bool isDstBackbuffer;
        }

        private static void ExecutePass(RasterCommandBuffer cmd, PassData passData, RTHandle source)
        {
            var copyDepthMaterial = passData.copyDepthMaterial;
            var msaaSamples = passData.msaaSamples;
            var copyResolvedDepth = passData.copyResolvedDepth;
            var copyToDepth = passData.copyToDepth;

            if (copyDepthMaterial == null)
            {
                Debug.LogErrorFormat("Missing {0}. Copy Depth render pass will not execute. Check for missing reference in the renderer resources.", copyDepthMaterial);
                return;
            }
            using (new ProfilingScope(cmd, ProfilingSampler.Get(URPProfileId.CopyDepth)))
            {
                int cameraSamples = 0;
                if (msaaSamples == -1)
                {
                    RTHandle sourceTex = source;
                    cameraSamples = sourceTex.rt.antiAliasing;
                }
                else
                    cameraSamples = msaaSamples;

                // When depth resolve is supported or multisampled texture is not supported, set camera samples to 1
                if (SystemInfo.supportsMultisampledTextures == 0 || copyResolvedDepth)
                    cameraSamples = 1;
                
                GlobalKeyword DepthMsaa2 = GlobalKeyword.Create(ShaderKeywordStrings.DepthMsaa2);
                GlobalKeyword DepthMsaa4 = GlobalKeyword.Create(ShaderKeywordStrings.DepthMsaa4);
                GlobalKeyword DepthMsaa8 = GlobalKeyword.Create(ShaderKeywordStrings.DepthMsaa8);

                switch (cameraSamples)
                {
                    case 8:
                        cmd.SetKeyword(DepthMsaa2, false);
                        cmd.SetKeyword(DepthMsaa4, false);
                        cmd.SetKeyword(DepthMsaa8, true);
                        break;

                    case 4:
                        cmd.SetKeyword(DepthMsaa2, false);
                        cmd.SetKeyword(DepthMsaa4, true);
                        cmd.SetKeyword(DepthMsaa8, false);
                        break;

                    case 2:
                        cmd.SetKeyword(DepthMsaa2, true);
                        cmd.SetKeyword(DepthMsaa4, false);
                        cmd.SetKeyword(DepthMsaa8, false);
                        break;

                    // MSAA disabled, auto resolve supported or ms textures not supported
                    default:
                        cmd.SetKeyword(DepthMsaa2, false);
                        cmd.SetKeyword(DepthMsaa4, false);
                        cmd.SetKeyword(DepthMsaa8, false);
                        break;
                }

                GlobalKeyword _OUTPUT_DEPTH = GlobalKeyword.Create(ShaderKeywordStrings._OUTPUT_DEPTH);
                cmd.SetKeyword(_OUTPUT_DEPTH, copyToDepth);

                // We must perform a yflip if we're rendering into the backbuffer and we have a flipped source texture.
                bool yflip = passData.cameraData.IsHandleYFlipped(source) && passData.isDstBackbuffer;

                Vector2 viewportScale = source.useScaling ? new Vector2(source.rtHandleProperties.rtHandleScale.x, source.rtHandleProperties.rtHandleScale.y) : Vector2.one;
                Vector4 scaleBias = yflip ? new Vector4(viewportScale.x, -viewportScale.y, 0, viewportScale.y) : new Vector4(viewportScale.x, viewportScale.y, 0, 0);

                // When we render to the backbuffer, we update the viewport to cover the entire screen just in case it hasn't been updated already.
                // if (passData.isDstBackbuffer)
                    // cmd.SetViewport(passData.cameraData.pixelRect);

                copyDepthMaterial.SetTexture(ShaderConstants._CameraDepthAttachment, source);
                copyDepthMaterial.SetFloat(ShaderConstants._ZWriteShaderHandle, copyToDepth ? 1.0f : 0.0f);
                Blitter.BlitTexture(cmd, source, scaleBias, copyDepthMaterial, 0);
            }
        }

        /// <inheritdoc/>
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            if (cmd == null)
                throw new ArgumentNullException("cmd");

            // Disable obsolete warning for internal usage
            #pragma warning disable CS0618
            destination = k_CameraTarget;
            #pragma warning restore CS0618
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

            var customData = frameData.Get<ColoredShadowsRenderFeature.MyCustomData>();
            
            // Render(renderGraph, frameData, resourceData.cameraDepthTexture, resourceData.activeDepthTexture, true);
            Render(renderGraph, frameData, customData.cameraColor3, customData.cameraColor2, true);
        }

        /// <summary>
        /// Sets up the Copy Depth pass for RenderGraph execution
        /// </summary>
        /// <param name="renderGraph">The current RenderGraph used for recording and execution of a frame.</param>
        /// <param name="frameData">The renderer settings containing rendering data of the current frame.</param>
        /// <param name="destination"><c>TextureHandle</c> of the destination it will copy to.</param>
        /// <param name="source"><c>TextureHandle</c> of the source it will copy from.</param>
        /// <param name="bindAsCameraDepth">If this is true, the destination texture is bound as _CameraDepthTexture after the copy pass</param>
        /// <param name="passName">The pass name used for debug and identifying the pass.</param>
        public void Render(RenderGraph renderGraph, ContextContainer frameData, TextureHandle destination, TextureHandle source, bool bindAsCameraDepth = false, string passName = "Copy Depth TEST")
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            Render(renderGraph, destination, source, resourceData, cameraData, bindAsCameraDepth, passName);
        }

        /// <summary>
        /// Sets up the Copy Depth pass for RenderGraph execution
        /// </summary>
        /// <param name="renderGraph">The current RenderGraph used for recording and execution of a frame.</param>
        /// <param name="destination"><c>TextureHandle</c> of the destination it will copy to.</param>
        /// <param name="source"><c>TextureHandle</c> of the source it will copy from.</param>
        /// <param name="resourceData">URP texture handles for the current frame.</param>
        /// <param name="cameraData">Camera settings for the current frame.</param>
        /// <param name="bindAsCameraDepth">If this is true, the destination texture is bound as _CameraDepthTexture after the copy pass</param>
        /// <param name="passName">The pass name used for debug and identifying the pass.</param>
        public void Render(RenderGraph renderGraph, TextureHandle destination, TextureHandle source, UniversalResourceData resourceData, UniversalCameraData cameraData, bool bindAsCameraDepth = false, string passName = "Copy Depth")
        {
            // TODO RENDERGRAPH: should call the equivalent of Setup() to initialise everything correctly
            MssaSamples = -1;

            //Having a different pass name than profilingSampler.name is bad practice but this method was public before we cleaned up this naming 
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData, profilingSampler))
            {
                passData.copyDepthMaterial = m_CopyDepthMaterial;
                passData.msaaSamples = MssaSamples;
                passData.cameraData = cameraData;
                passData.copyResolvedDepth = m_CopyResolvedDepth;
                passData.copyToDepth = CopyToDepth || CopyToDepthXR;
                passData.isDstBackbuffer = CopyToBackbuffer || CopyToDepthXR;
                
                // Writes depth using custom depth output
                // builder.SetRenderAttachmentDepth(destination, AccessFlags.WriteAll);
                builder.SetRenderAttachment(destination, 0);
                // builder.SetRenderAttachment(destination, 0, AccessFlags.WriteAll);

                passData.source = source;
                builder.UseTexture(source, AccessFlags.Read);

                // if (bindAsCameraDepth && destination.IsValid())
                //     builder.SetGlobalTextureAfterPass(destination, ShaderConstants._CameraDepthTexture);

                // TODO RENDERGRAPH: culling? force culling off for testing
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    ExecutePass(context.cmd, data, data.source);
                });
            }
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
}
