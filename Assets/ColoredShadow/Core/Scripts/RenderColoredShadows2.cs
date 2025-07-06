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
    private FilteringSettings filteringSettings;
    private List<ShaderTagId> shaderTagIdList = new List<ShaderTagId>();
    // private RenderStateBlock renderStateBlock;

    public RenderColoredShadows2()
    {
        profilingSampler = new ProfilingSampler("TEST_PROFILER");
        
        shaderTagIdList.Add(new ShaderTagId("SRPDefaultUnlit"));
        shaderTagIdList.Add(new ShaderTagId("UniversalForward"));
        shaderTagIdList.Add(new ShaderTagId("UniversalForwardOnly"));
        
        filteringSettings = new FilteringSettings(RenderQueueRange.transparent, int.MaxValue);
        
        // renderStateBlock = new RenderStateBlock(RenderStateMask.Depth);
        // renderStateBlock.depthState = new DepthState(true, CompareFunction.Less);
    }

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

        RendererListParams param = new RendererListParams(cullingResults, drawingSettings, filteringSettings);
        return renderGraph.CreateRendererList(param);
    }

    private static void ExecutePass(PassData passData, RasterCommandBuffer cmd)
    {
        cmd.DisableScissorRect();
        cmd.SetViewport(new Rect(0, 0, 4096, 4096));
        cmd.SetViewProjectionMatrices(passData.viewMatrix, passData.projectionMatrix);
        cmd.DrawRendererList(passData.rendererListHdl1);
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
        
        using (var builder = renderGraph.AddRasterRenderPass<PassData>("TEST_CAPTURE", out var passData, profilingSampler))
        {
            builder.SetRenderAttachment(destinationColor, 0, AccessFlags.Write);
            builder.SetRenderAttachmentDepth(destinationDepth, AccessFlags.Write);
            
            if (cameraData.camera.TryGetCullingParameters(out ScriptableCullingParameters scriptableCullingParameters))
            {
                CustomLight customLight = CustomLightManager.customLights[0];
                filteringSettings.layerMask = customLight.shadowCastingMask;
                projectionMatrix = Matrix4x4.Ortho(
                    -customLight.size,
                    customLight.size,
                    -customLight.size,
                    customLight.size,
                    customLight.nearPlane,
                    customLight.farPlane
                );
                viewMatrix = GetViewMatrix(customLight.transform.position, customLight.transform.rotation);
                Matrix4x4 cullingMatrix = projectionMatrix * viewMatrix;
                scriptableCullingParameters.cullingMatrix = cullingMatrix;
                CullingResults cullingResults = cullContextData.Cull(ref scriptableCullingParameters);

                passData.rendererListHdl1 = InitRendererLists(renderingData, lightData, renderGraph, cullingResults, cameraData);
            }
            
            passData.viewMatrix = viewMatrix;
            passData.projectionMatrix = projectionMatrix;

            builder.AllowPassCulling(false);
            builder.AllowGlobalStateModification(true);
            builder.UseRendererList(passData.rendererListHdl1);
        
            builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) =>
            {
                ExecutePass(data, rgContext.cmd);
            });
        }

        RenderGraphUtils.BlitMaterialParameters para2 = new(destinationColor, destinationColorRT, Blitter.GetBlitMaterial(TextureDimension.Tex2D), 0);
        renderGraph.AddBlitPass(para2, "TEST_BLIT");

        Shader.SetGlobalTexture("_ColoredShadowMap" + 1, shadowMapID);
    }

    private class PassData
    {
        internal Matrix4x4 viewMatrix;
        internal Matrix4x4 projectionMatrix;
        internal RendererListHandle rendererListHdl1;
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
