using System.Collections.Generic;
using System.Runtime.InteropServices;
using ColoredShadow.Core.Scripts;
using ColoredShadows.Scripts;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

public class RenderColoredShadows : ScriptableRenderPass
{
    private RTHandle shadowMapID;
    private RTHandle shadowMapID2;
    private FilteringSettings filteringSettings;
    private readonly List<ShaderTagId> shaderTagIdList = new List<ShaderTagId>();
    private readonly RenderStateBlock renderStateBlock;
    private GraphicsBuffer lightInformationBuffer;
    private Shader depthShader;
    private ComputeShader postProcessShadowMap;


    public RenderColoredShadows(GraphicsBuffer lightInformationBuffer)
    {
        profilingSampler = new ProfilingSampler("TEST_PROFILER");

        this.lightInformationBuffer = lightInformationBuffer;
        
        shaderTagIdList.Add(new ShaderTagId("SRPDefaultUnlit"));
        shaderTagIdList.Add(new ShaderTagId("UniversalForward"));
        shaderTagIdList.Add(new ShaderTagId("UniversalForwardOnly"));

        depthShader = Resources.Load<Shader>("DepthOverrideShader");
        postProcessShadowMap = Resources.Load<ComputeShader>("PostProcessShadowMap");
        
        filteringSettings = new FilteringSettings(RenderQueueRange.all, int.MaxValue);
        
        renderStateBlock = new RenderStateBlock(RenderStateMask.Depth) {
            depthState = new DepthState(true, CompareFunction.Less),
        };
    }

    public void Dispose()
    {
        shadowMapID?.Release();
        shadowMapID = null;
    }


    
    static ShaderTagId[] s_ShaderTagValues = new ShaderTagId[1];
    static RenderStateBlock[] s_RenderStateBlocks = new RenderStateBlock[1];
    private RendererListHandle InitRendererLists(UniversalRenderingData renderingData, UniversalLightData lightData, 
        RenderGraph renderGraph, CullingResults cullingResults, UniversalCameraData cameraData, Shader overrideShader)
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
        drawingSettings.overrideShader = overrideShader;

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

    private static void ExecutePass(PassData passData, RasterCommandBuffer cmd, bool clearDepth)
    {
        if(clearDepth)
            cmd.ClearRenderTarget(true, false, Color.blueViolet);
        
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
        
        Vector2Int shadowAtlasSize = CustomLightManager.GetShadowAtlasSize();
        int shadowAtlasWidth = Mathf.Max(1, shadowAtlasSize.x);
        int shadowAtlasHeight = Mathf.Max(1, shadowAtlasSize.y);
        Shader.SetGlobalInt("_CustomShadowAtlasWidth", shadowAtlasWidth);
        Shader.SetGlobalInt("_CustomShadowAtlasHeight", shadowAtlasHeight);
        Shader.SetGlobalInt("_CurrentAmountCustomLights", CustomLightManager.CustomLightCount);
    
        TextureDesc destinationDescColor = renderGraph.GetTextureDesc(resourceData.activeColorTexture);
        // destinationDescColor.format = GraphicsFormat.R16G16B16A16_SFloat;
        // destinationDescColor.format = GraphicsFormat.R32G32B32A32_SInt;
        destinationDescColor.format = GraphicsFormat.R32G32B32A32_SFloat;
        destinationDescColor.name = "SOURCE_COLOR";
        destinationDescColor.width = shadowAtlasWidth;
        destinationDescColor.height = shadowAtlasHeight;
        destinationDescColor.enableRandomWrite = true;
        destinationDescColor.clearBuffer = true;
        TextureHandle destinationColor = renderGraph.CreateTexture(destinationDescColor);
    
        TextureDesc destinationDescDepth = renderGraph.GetTextureDesc(resourceData.activeDepthTexture);
        destinationDescDepth.name = "SOURCE_DEPTH";
        destinationDescDepth.width = shadowAtlasWidth;
        destinationDescDepth.height = shadowAtlasHeight;
        TextureHandle destinationDepth = renderGraph.CreateTexture(destinationDescDepth);
        
        RenderTextureDescriptor shadowMapDepthDesc = cameraData.cameraTargetDescriptor;
        shadowMapDepthDesc.depthStencilFormat = GraphicsFormat.D32_SFloat;
        shadowMapDepthDesc.colorFormat = RenderTextureFormat.RFloat;
        shadowMapDepthDesc.width = shadowAtlasWidth;
        shadowMapDepthDesc.height = shadowAtlasHeight;
        shadowMapDepthDesc.depthBufferBits = 0;
        shadowMapDepthDesc.msaaSamples = 1;
        RenderingUtils.ReAllocateHandleIfNeeded(ref shadowMapID, shadowMapDepthDesc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "RT_COLSHADOW_DEPTH" );
        TextureHandle destinationDepthRT = renderGraph.ImportTexture(shadowMapID);
        
        List<ShadowPass> shadowPasses = new List<ShadowPass>();
        List<ShadowPass> shadowPassesReceivingDepth = new List<ShadowPass>();
        bool anyVFXPass = false;
        bool anyDepthMergePass = false;
        LightInformation[] lightInformations = new LightInformation[CustomLightManager.CustomLightCount];
        
        for (int i = 0; i < CustomLightManager.CustomLightCount; i++)
        {
            CustomLight light = CustomLightManager.GetCustomLight(i);
            if (GetShadowPass(light, out List<ShadowPass> tempShadowPasses, out List<ShadowPass> tempShadowPassesReceivingDepth))
            {
                shadowPasses.AddRange(tempShadowPasses);
                
                if(light.blockPassthroughShadows)
                {
                    anyDepthMergePass = true;
                    shadowPassesReceivingDepth.AddRange(tempShadowPassesReceivingDepth);
                }
            }

            if (light.enableVFXSupport)
                anyVFXPass = true;

            lightInformations[i] = GetLightInformation(light);
        }
        
        using (var builder = renderGraph.AddRasterRenderPass<PassData>("CAPTURE_COL_SHADOWS", out var passData, profilingSampler))
        {
            builder.SetRenderAttachment(destinationColor, 0, AccessFlags.Write);
            builder.SetRenderAttachmentDepth(destinationDepth, AccessFlags.Write);

            foreach (ShadowPass shadowPass in shadowPasses)
            { 
                builder.UseRendererList(shadowPass.rendererList);
            }

            passData.shadowPasses = shadowPasses;
            passData.color = destinationColor;

            builder.AllowPassCulling(false);
            builder.AllowGlobalStateModification(true);
        
            builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) =>
            {
                ExecutePass(data, rgContext.cmd, false);
            });
        }

        if (anyDepthMergePass)
        {
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("GET_DEPTH_OF_SHADOW_RECEIVERS", out var passData, profilingSampler))
            {
                builder.SetRenderAttachmentDepth(destinationDepth, AccessFlags.Write);

                foreach (ShadowPass shadowPass in shadowPassesReceivingDepth)
                {
                    builder.UseRendererList(shadowPass.rendererList);
                }

                passData.shadowPasses = shadowPassesReceivingDepth;

                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) =>
                {
                    ExecutePass(data, rgContext.cmd, true);
                });
            }
        }

        if (anyVFXPass)
        {
            using (var builder = renderGraph.AddComputePass("PP_SHADOWMAP", out PassDataCompute passData))
            {
                passData.cs = postProcessShadowMap;
                passData.shadowMap = destinationColor;
                passData.depthMap = destinationDepth;
                passData.amountBlurEdges = ColShadowSettings.AmountShadowBlur;
                
                builder.UseTexture(destinationColor);
                builder.UseTexture(destinationDepth);
                
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassDataCompute data, ComputeGraphContext cgContext) =>
                {
                    int threadGroupX = Mathf.CeilToInt(shadowAtlasWidth / 32.0f);
                    int threadGroupY = Mathf.CeilToInt(shadowAtlasHeight / 32.0f);

                    if (data.amountBlurEdges > 0)
                    {
                        cgContext.cmd.SetComputeIntParam(data.cs, "sampleSize", data.amountBlurEdges);
                            
                        int blur1 = data.cs.FindKernel("Blur1");
                        cgContext.cmd.SetComputeTextureParam(data.cs, blur1, "_ShadowAtlas", data.shadowMap);
                        cgContext.cmd.SetComputeTextureParam(data.cs, blur1, "_DepthMap", data.depthMap);
                        cgContext.cmd.DispatchCompute(data.cs, blur1, threadGroupX, threadGroupY, 1);
                    
                        int blur2 = data.cs.FindKernel("Blur2");
                        cgContext.cmd.SetComputeTextureParam(data.cs, blur2, "_ShadowAtlas", data.shadowMap);
                        cgContext.cmd.SetComputeTextureParam(data.cs, blur2, "_DepthMap", data.depthMap);
                        cgContext.cmd.DispatchCompute(data.cs, blur2, threadGroupX, threadGroupY, 1);
                    }

                    if (anyDepthMergePass)
                    {
                        int depthMerge = data.cs.FindKernel("DepthMerge");
                        cgContext.cmd.SetComputeTextureParam(data.cs, depthMerge, "_ShadowAtlas", data.shadowMap);
                        cgContext.cmd.SetComputeTextureParam(data.cs, depthMerge, "_DepthMap", data.depthMap);
                        cgContext.cmd.DispatchCompute(data.cs, depthMerge, threadGroupX, threadGroupY, 1);
                    }
                });
            }
        }
        
        RenderGraphUtils.BlitMaterialParameters para2 = new(destinationDepth, destinationDepthRT, Blitter.GetBlitMaterial(TextureDimension.Tex2D), 0);
        renderGraph.AddBlitPass(para2, "CaptureShadowsColor");
        
        lightInformationBuffer.SetData(lightInformations);
        Shader.SetGlobalBuffer("_ColoredShadowLightInformation", lightInformationBuffer);
        cameraData.camera.ResetCullingMatrix();
        
        using (var builder = renderGraph.AddRasterRenderPass<PassData>("SET_GLOBAL_TEX", out var pd2)) {
            pd2.color = destinationColor;
            builder.AllowPassCulling(false);
            builder.AllowGlobalStateModification(true);
            builder.UseTexture(destinationColor);
            builder.SetRenderFunc<PassData>((data, ctx) => {
                ctx.cmd.SetGlobalTexture(Shader.PropertyToID("_ColoredShadowMap0"), data.color);
            });
        }
        return;

        bool GetShadowPass(CustomLight light, out List<ShadowPass> shadowPases, out List<ShadowPass> shadowPasesReceivingDepth)
        {
            shadowPases = new List<ShadowPass>();
            shadowPasesReceivingDepth = new List<ShadowPass>();
            List<(Matrix4x4, Matrix4x4)> projViewMatrices = light.GetCullingMatrices();

            for (int i = 0; i < projViewMatrices.Count; i++)
            {
                (Matrix4x4, Matrix4x4) projViewMatrix = projViewMatrices[i];
                cameraData.camera.cullingMatrix = projViewMatrix.Item1 * projViewMatrix.Item2;
                if (!cameraData.camera.TryGetCullingParameters(false, out ScriptableCullingParameters scriptableCullingParameters))
                {
                    Debug.LogError($"Couldnt get ScriptableCullingParameters from {light.gameObject.name}");
                    return false;
                }

                CullingResults cullingResults = cullContextData.Cull(ref scriptableCullingParameters);
                filteringSettings.layerMask = light.shadowCastingMask;
                RendererListHandle rendererList = InitRendererLists(renderingData, lightData, renderGraph, cullingResults, cameraData, light.overrideShader);
                shadowPases.Add(new ShadowPass(light.GetLocalShadowAtlasPos(i), light.shadowTextureSize, light.shadowTextureSize, rendererList, projViewMatrix.Item2, projViewMatrix.Item1));
                
                if(!light.enableVFXSupport)
                    continue;
                
                rendererList = InitRendererLists(renderingData, lightData, renderGraph, cullingResults, cameraData, depthShader);
                shadowPasesReceivingDepth.Add(new ShadowPass(light.GetLocalShadowAtlasPos(i), light.shadowTextureSize, light.shadowTextureSize, rendererList, projViewMatrix.Item2, projViewMatrix.Item1));
            }

            return true;
        }
    }
    private static LightInformation GetLightInformation(CustomLight light)
    {
        List<float> customValuesCopy = new List<float>(light.customValues);

        for (int j = 0; customValuesCopy.Count < 12; j++)
        {
            customValuesCopy.Add(0);
            if (j > 12)
            {
                Debug.LogError($"Cannot fill Custom Values list above 12 entries");
                break;
            }
        }
                
        return new LightInformation(
            light.lightIndex,
            (int)light.lightMode,
            GL.GetGPUProjectionMatrix(light.ProjectionMatrix, false) * light.ViewMatrix,
            Matrix4x4.Inverse(light.ProjectionMatrix * light.ViewMatrix),
            light.transform.position,
            light.lightMode == LightMode.Directional ? float.MaxValue : light.fallOffRange,
            light.farPlane,
            light.transform.position,
            light.TextureWidth,
            light.TextureHeight,
            light.addToShadowID,
            light.shadowAtlasPosX,
            light.shadowAtlasPosY,
            light.blockPassthroughShadows,
            ColShadowSettings.AmountShadowBlur > 0,
            customValuesCopy
        );
    }

    private class PassData
    {
        internal TextureHandle color;
        internal List<ShadowPass> shadowPasses;
    }

    private class PassDataCompute
    {
        internal ComputeShader cs;
        internal TextureHandle shadowMap;
        internal TextureHandle depthMap;
        internal int textureWidth;
        internal int textureHeight;
        internal int amountBlurEdges;
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

        public ShadowPass(Vector2Int atlasPos, int textureWidth, int textureHeight, RendererListHandle rendererList, Matrix4x4 viewMatrix, Matrix4x4 projectionMatrix)
        {
            texturePosX = atlasPos.x;
            texturePosY = atlasPos.y;
            this.textureWidth = textureWidth;
            this.textureHeight = textureHeight;
            this.rendererList = rendererList;
            this.viewMatrix = viewMatrix;
            this.projectionMatrix = projectionMatrix;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct LightInformation
    {
        public int index; //up to 1024 - 10 bit
        public int lightMode; // up to 8 - 4 bit
        public Matrix4x4 lightMatrix;
        public Matrix4x4 invLightMatrix;
        public Vector3 lightPos;
        public float fallOffRange;
        public float farPlane;
        public Vector3 cameraPos;
        public int textureSizeX; // up to 16.384 - 14 bit
        public int textureSizeY; // up to 16.384 - 14 bit
        public int lightIDMultiplier;
        public int shadowAtlasPosX; // up to 16.384 - 14 bit
        public int shadowAtlasPosY; // up to 16.384 - 14 bit
        public int passthroughShadows; // 1 bit
        public int blurredEdges; // 1 bit
        public float customValue0;
        public float customValue1;
        public float customValue2;
        public float customValue3;
        public float customValue4;
        public float customValue5;
        public float customValue6;
        public float customValue7;
        public float customValue8;
        public float customValue9;
        public float customValue10;
        public float customValue11;
        public LightInformation(int index, int lightMode, Matrix4x4 lightMatrix, Matrix4x4 invLightMatrix, Vector3 lightPos, float fallOffRange, float farPlane, Vector3 cameraPos, int textureSizeX, int textureSizeY, int lightIDMultiplier, int shadowAtlasPosX, int shadowAtlasPosY, bool passthroughShadows, bool blurredEdges, List<float> customValues) : this()
        {
            this.index = index;
            this.lightMode = lightMode;
            this.lightMatrix = lightMatrix;
            this.invLightMatrix = invLightMatrix;
            this.lightPos = lightPos;
            this.fallOffRange = fallOffRange;
            this.farPlane = farPlane;
            this.cameraPos = cameraPos;
            this.textureSizeX = textureSizeX;
            this.textureSizeY = textureSizeY;
            this.lightIDMultiplier = lightIDMultiplier;
            this.shadowAtlasPosX = shadowAtlasPosX;
            this.shadowAtlasPosY = shadowAtlasPosY;
            this.passthroughShadows = passthroughShadows ? 1 : 0;
            this.blurredEdges = blurredEdges ? 1 : 0;
            customValue0 = customValues[0];
            customValue1 = customValues[1];
            customValue2 = customValues[2];
            customValue3 = customValues[3];
            customValue4 = customValues[4];
            customValue5 = customValues[5];
            customValue6 = customValues[6];
            customValue7 = customValues[7];
            customValue8 = customValues[8];
            customValue9 = customValues[9];
            customValue10 = customValues[10];
            customValue11 = customValues[11];
        }
    }
}
