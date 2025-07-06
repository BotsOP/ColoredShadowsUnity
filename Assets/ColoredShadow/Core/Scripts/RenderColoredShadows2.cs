using System.Collections.Generic;
using ColoredShadows.Scripts;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

public class RenderColoredShadows2 : ScriptableRenderPass
{
    private RTHandle shadowMapID;
    private RTHandle shadowMapID2;
    private FilteringSettings filteringSettings;
    private List<ShaderTagId> shaderTagIdList = new List<ShaderTagId>();
    private RenderStateBlock renderStateBlock;

    public RenderColoredShadows2()
    {
        profilingSampler = new ProfilingSampler("TEST_PROFILER");
        
        shaderTagIdList.Add(new ShaderTagId("SRPDefaultUnlit"));
        shaderTagIdList.Add(new ShaderTagId("UniversalForward"));
        shaderTagIdList.Add(new ShaderTagId("UniversalForwardOnly"));
        
        filteringSettings = new FilteringSettings(RenderQueueRange.transparent, int.MaxValue);
        
        renderStateBlock = new RenderStateBlock(RenderStateMask.Depth);
        renderStateBlock.depthState = new DepthState(true, CompareFunction.Less);
        
        // renderStateBlock = new RenderStateBlock(RenderStateMask.Depth);
        // renderStateBlock.depthState = new DepthState(true, CompareFunction.Less);
    }

    static ShaderTagId[] s_ShaderTagValues = new ShaderTagId[1];
    static RenderStateBlock[] s_RenderStateBlocks = new RenderStateBlock[1];
    private RendererListHandle InitRendererLists(UniversalRenderingData renderingData, UniversalLightData lightData, 
        RenderGraph renderGraph, CullingResults cullingResults, UniversalCameraData cameraData)
    {
        SortingCriteria sortingCriteria = SortingCriteria.CommonTransparent;
        DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(
            shaderTagIdList,
            renderingData,
            cameraData,
            lightData,
            sortingCriteria
        );
        drawingSettings.enableInstancing = true;
        drawingSettings.enableDynamicBatching = true;
        drawingSettings.overrideShader = CustomLightManager.customLights[0].overrideShader;

        s_ShaderTagValues[0] = ShaderTagId.none;
        s_RenderStateBlocks[0] = renderStateBlock;
        NativeArray<ShaderTagId> tagValues = new NativeArray<ShaderTagId>(s_ShaderTagValues, Allocator.Temp);
        NativeArray<RenderStateBlock> stateBlocks = new NativeArray<RenderStateBlock>(s_RenderStateBlocks, Allocator.Temp);
        RendererListParams param = new RendererListParams(cullingResults, drawingSettings, filteringSettings) 
        {
            stateBlocks = stateBlocks,
            tagValues = tagValues,
            isPassTagName = false,
        };
        return renderGraph.CreateRendererList(param);
    }

    private static void ExecutePass(PassData passData, RasterCommandBuffer cmd)
    {
        cmd.DisableScissorRect();
        foreach (ShadowPass shadowPass in passData.shadowPasses)
        {
            cmd.SetViewport(new Rect(shadowPass.texturePosX, shadowPass.texturePosY, shadowPass.textureWidth, shadowPass.textureHeight));
            cmd.SetViewProjectionMatrices(shadowPass.viewMatrix, shadowPass.projectionMatrix);
            cmd.DrawRendererList(shadowPass.rendererList);
        }
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
        UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
        UniversalLightData lightData = frameData.Get<UniversalLightData>();
        UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
        CullContextData cullContextData = frameData.Get<CullContextData>();
        
        Matrix4x4 viewMatrix = Matrix4x4.zero;
        Matrix4x4 projectionMatrix = Matrix4x4.zero;
    
        var destinationDescColor = renderGraph.GetTextureDesc(resourceData.activeColorTexture);
        destinationDescColor.format = GraphicsFormat.R32G32B32A32_SInt;
        destinationDescColor.name = "SOURCE_COLOR";
        destinationDescColor.width = 4096;
        destinationDescColor.height = 4096;
        TextureHandle destinationColor = renderGraph.CreateTexture(destinationDescColor);
    
        var destinationDescDepth = renderGraph.GetTextureDesc(resourceData.activeDepthTexture);
        destinationDescDepth.name = "SOURCE_DEPTH";
        destinationDescDepth.width = 4096;
        destinationDescDepth.height = 4096;
        TextureHandle destinationDepth = renderGraph.CreateTexture(destinationDescDepth);
    
        RenderTextureDescriptor shadowMapIDDesc = cameraData.cameraTargetDescriptor;
        shadowMapIDDesc.width = 4096;
        shadowMapIDDesc.height = 4096;
        shadowMapIDDesc.depthBufferBits = 0;
        shadowMapIDDesc.msaaSamples = 1;
        RenderingUtils.ReAllocateHandleIfNeeded(ref shadowMapID, shadowMapIDDesc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "Test_RT");
        TextureHandle destinationColorRT = renderGraph.ImportTexture(shadowMapID);
        
        RenderTextureDescriptor shadowMapIDDesc2 = cameraData.cameraTargetDescriptor;
        shadowMapIDDesc2.width = 4096;
        shadowMapIDDesc2.height = 4096;
        shadowMapIDDesc2.depthBufferBits = 0;
        shadowMapIDDesc2.msaaSamples = 1;
        RenderingUtils.ReAllocateHandleIfNeeded(ref shadowMapID2, shadowMapIDDesc2, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "Test_RT2");
        TextureHandle destinationColorRT2 = renderGraph.ImportTexture(shadowMapID2);
        
        
        filteringSettings.layerMask = int.MaxValue;
        
        using (var builder = renderGraph.AddRasterRenderPass<PassData>("TEST_CAPTURE", out var passData, profilingSampler))
        {
            builder.SetRenderAttachment(destinationColor, 0, AccessFlags.Write);
            builder.SetRenderAttachmentDepth(destinationDepth, AccessFlags.Write);

            List<ShadowPass> shadowPasses = new List<ShadowPass>();
            for (int i = 0; i < CustomLightManager.customLights.Count; i++)
            {
                CustomLight light = CustomLightManager.customLights[i];
                projectionMatrix = Matrix4x4.Ortho(
                    -light.size,
                    light.size,
                    -light.size,
                    light.size,
                    light.nearPlane,
                    light.farPlane
                );
                viewMatrix = GetViewMatrix(light.transform.position, light.transform.rotation);
                Matrix4x4 cullingMatrix = projectionMatrix * viewMatrix;

                cameraData.camera.cullingMatrix = cullingMatrix;
                if (cameraData.camera.TryGetCullingParameters(false, out ScriptableCullingParameters scriptableCullingParameters))
                {
                    CullingResults cullingResults = cullContextData.Cull(ref scriptableCullingParameters);
                    RendererListHandle rendererList = InitRendererLists(renderingData, lightData, renderGraph, cullingResults, cameraData);
                    builder.UseRendererList(rendererList);
                    ShadowPass newShadowPass = new ShadowPass(
                        light.shadowTextureSize,
                        light.shadowTextureSize,
                        1024 * i,
                        0,
                        rendererList,
                        viewMatrix,
                        projectionMatrix
                    );
                    shadowPasses.Add(newShadowPass);
                }
            }
            passData.shadowPasses = shadowPasses;

            builder.AllowPassCulling(false);
            builder.AllowGlobalStateModification(true);
        
            builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) =>
            {
                ExecutePass(data, rgContext.cmd);
            });
        }

        RenderGraphUtils.BlitMaterialParameters para2 = new(destinationColor, destinationColorRT, Blitter.GetBlitMaterial(TextureDimension.Tex2D), 0);
        renderGraph.AddBlitPass(para2, "TEST_BLIT");
        Shader.SetGlobalTexture("_ColoredShadowMap" + 0, shadowMapID);
    }

    private class PassData
    {
        internal List<ShadowPass> shadowPasses;
    }

    private struct ShadowPass
    {
        public int textureWidth;
        public int textureHeight;
        public int texturePosX;
        public int texturePosY;
        public RendererListHandle rendererList;
        public Matrix4x4 viewMatrix;
        public Matrix4x4 projectionMatrix;

        public ShadowPass(int textureWidth, int textureHeight, int texturePosX, int texturePosY, RendererListHandle rendererList, Matrix4x4 viewMatrix, Matrix4x4 projectionMatrix)
        {
            this.textureWidth = textureWidth;
            this.textureHeight = textureHeight;
            this.texturePosX = texturePosX;
            this.texturePosY = texturePosY;
            this.rendererList = rendererList;
            this.viewMatrix = viewMatrix;
            this.projectionMatrix = projectionMatrix;
        }
    }
    
    private static Matrix4x4 GetViewMatrix(Vector3 cameraPosition, Quaternion cameraRotation)
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
}
