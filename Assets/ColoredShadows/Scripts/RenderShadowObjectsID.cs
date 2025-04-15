using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class RenderShadowObjectsID : ScriptableRenderPass
{
    private Material overrideMaterial;
    private int overrideMaterialPassIndex;
    private float shadowID;
    private Transform lightTransform;
    private ColoredShadowsRenderFeature.CustomLightData lightData;
    
    private RenderQueueType renderQueueType;
    private FilteringSettings filteringSettings;
    private List<ShaderTagId> shaderTagIdList = new List<ShaderTagId>();
    private RenderStateBlock renderStateBlock;
    
    private static readonly int LightSpaceMatrix = Shader.PropertyToID("_LightSpaceMatrix");
    
    public void SetDepthState(bool writeEnabled, CompareFunction function = CompareFunction.Less)
    {
        renderStateBlock.mask |= RenderStateMask.Depth;
        renderStateBlock.depthState = new DepthState(writeEnabled, function);
    }

    public RenderShadowObjectsID(string profilerTag, RenderPassEvent renderPassEvent, string[] shaderTags, RenderQueueType renderQueueType, int layerMask, 
        Transform lightTransform, ColoredShadowsRenderFeature.CustomLightData lightData, Vector2Int depthDimensions, Vector2Int shadowIdDimensions, Material overrideMaterial, int overrideMaterialPassIndex = 0)            
    {
        profilingSampler = new ProfilingSampler(profilerTag);
        this.overrideMaterial = overrideMaterial;
        this.overrideMaterialPassIndex = overrideMaterialPassIndex;
        this.lightTransform = lightTransform;
        this.lightData = lightData;
        Init(renderPassEvent, shaderTags, renderQueueType, layerMask);
    }

    internal void Init(RenderPassEvent renderPassEvent, string[] shaderTags, RenderQueueType renderQueueType, int layerMask)
    {
        this.renderPassEvent = renderPassEvent;
        this.renderQueueType = renderQueueType;
        RenderQueueRange renderQueueRange = (renderQueueType == RenderQueueType.Transparent)
            ? RenderQueueRange.transparent
            : RenderQueueRange.opaque;
        filteringSettings = new FilteringSettings(renderQueueRange, layerMask);

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

        
        renderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);
    }

    private static void ExecutePass(PassData passData, RasterCommandBuffer cmd, RendererList rendererList, bool isYFlipped)
    {
        Matrix4x4 projectionMatrix = passData.projectionMatrix;
        projectionMatrix = GL.GetGPUProjectionMatrix(projectionMatrix, isYFlipped);
        Matrix4x4 viewMatrix = passData.viewMatrix;
        Rect viewport = new Rect(0, 0, 4096, 4096);
        
        // passData.camera.allowMSAA = false;
        // cmd.SetupCameraProperties(passData.camera);
        SetViewAndProjectionMatrices(cmd, viewMatrix, projectionMatrix);
        cmd.SetGlobalMatrix(LightSpaceMatrix, projectionMatrix * passData.viewMatrix2);

        cmd.SetViewport(viewport);
        cmd.DrawRendererList(rendererList);
        cmd.SetViewport(new Rect(0, 0, 1024, 1024));
    }
    

    private void InitPassData(UniversalCameraData cameraData, ref PassData passData)
    {
        passData.renderPassEvent = renderPassEvent;
        passData.cameraData = cameraData;
    }

    private void InitRendererLists(UniversalRenderingData renderingData, UniversalLightData lightData,
        ref PassData passData, ScriptableRenderContext context, RenderGraph renderGraph, bool useRenderGraph)
    {
        // context.SetupCameraProperties(passData.camera);
        SortingCriteria sortingCriteria = (renderQueueType == RenderQueueType.Transparent)
            ? SortingCriteria.CommonTransparent
            : passData.cameraData.defaultOpaqueSortFlags;
        DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(shaderTagIdList, renderingData,
            passData.cameraData, lightData, sortingCriteria);
        drawingSettings.enableInstancing = true;
        drawingSettings.enableDynamicBatching = true;
        drawingSettings.overrideShader = overrideMaterial.shader;
        drawingSettings.overrideShaderPassIndex = overrideMaterialPassIndex;

        if (useRenderGraph)
        {
            CreateRendererListWithRenderStateBlock(renderGraph, ref renderingData.cullResults, drawingSettings,
                filteringSettings, renderStateBlock, ref passData.rendererListHdl);
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
        UniversalLightData universalLightData = frameData.Get<UniversalLightData>();
        
        TextureHandle destinationColor;
        var customData = frameData.Get<ColoredShadowsRenderFeature.CustomShadowData>();
        destinationColor = customData.shadowMapID;
        

        using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData, profilingSampler))
        {
            InitPassData(cameraData, ref passData);

            passData.shadowID = shadowID;
            passData.color = destinationColor;
            builder.SetRenderAttachment(destinationColor, 0, AccessFlags.Write);

            if (lightData.lightMode == ColoredShadowsRenderFeature.CustomLightData.LightMode.Ortho)
            {
                passData.projectionMatrix = Matrix4x4.Ortho(
                    -lightData.horizontalSize,
                    lightData.horizontalSize,
                    -lightData.verticalSize,
                    lightData.verticalSize,
                    lightData.nearPlane,
                    lightData.farPlane
                );
            }
            else
            {
                passData.projectionMatrix = Matrix4x4.Perspective(
                    lightData.fov,
                    lightData.aspectRatio,
                    lightData.nearPlane,
                    lightData.farPlane
                );
            }
            Matrix4x4 passDataViewMatrix = GetViewMatrix(lightTransform.position, lightTransform.rotation);
            
            passData.camera = cameraData.camera;
            passData.viewMatrix = passDataViewMatrix;
            passData.viewMatrix2 = LookAtLH(new Vector3(-lightTransform.position.x, lightTransform.position.y, -lightTransform.position.z), Vector3.zero, Vector3.up);
            // Debug.Log($"new viewMatrix {passData.viewMatrix}");
            // Debug.Log($"old viewMatrix {cameraData.GetViewMatrix()}");
            //
            // Debug.Log($"new projMatrix {passData.projectionMatrix}");
            // Debug.Log($"old projMatrix {cameraData.GetProjectionMatrix()}");


            InitRendererLists(renderingData, universalLightData, ref passData, default(ScriptableRenderContext), renderGraph, true);

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

    static void SetViewAndProjectionMatrices(RasterCommandBuffer cmd, Matrix4x4 viewMatrix, Matrix4x4 projectionMatrix)
    {
        Matrix4x4 viewAndProjectionMatrix = projectionMatrix * viewMatrix;
        cmd.SetGlobalMatrix(viewMatrixID, viewMatrix);
        cmd.SetGlobalMatrix(projectionMatrixID, projectionMatrix);
        cmd.SetGlobalMatrix(viewAndProjectionMatrixID, viewAndProjectionMatrix);
    }
    
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

    private class PassData
    {
        internal RenderPassEvent renderPassEvent;

        internal Matrix4x4 viewMatrix;
        internal Matrix4x4 viewMatrix2;
        internal Matrix4x4 projectionMatrix;

        internal float shadowID;
        internal TextureHandle color;
        internal RendererListHandle rendererListHdl;

        internal UniversalCameraData cameraData;
        internal Camera camera;

        // Required for code sharing purpose between RG and non-RG.
        internal RendererList rendererList;
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
