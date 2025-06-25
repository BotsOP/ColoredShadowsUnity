using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

namespace ColoredShadows.Scripts
{
    public class RenderColoredShadowsDebug : ScriptableRenderPass
    {
        private RTHandle shadowMapID;
        private RTHandle shadowMapDepth;
        private const string shadowMapIDName = "_CustomSceneShadowMapID";
    
        private RenderQueueType renderQueueType;
        private FilteringSettings filteringSettings;
        private List<ShaderTagId> shaderTagIdList = new List<ShaderTagId>();
        private RenderStateBlock renderStateBlock;

        private ComputeShader cs;

        public RenderColoredShadowsDebug(string profilerTag)            
        {
            profilingSampler = new ProfilingSampler(profilerTag);
            
            cs = Resources.Load<ComputeShader>("DebugColShadowView");
            
            Init(renderPassEvent);
        }

        internal void Init(RenderPassEvent renderPassEvent)
        {
            this.renderPassEvent = renderPassEvent;
            RenderQueueRange renderQueueRange = RenderQueueRange.transparent;
            filteringSettings = new FilteringSettings(renderQueueRange, 0);

            shaderTagIdList.Add(new ShaderTagId("SRPDefaultUnlit"));
            shaderTagIdList.Add(new ShaderTagId("UniversalForward"));
            shaderTagIdList.Add(new ShaderTagId("UniversalForwardOnly"));
        
            renderStateBlock = new RenderStateBlock(RenderStateMask.Depth);
            renderStateBlock.depthState = new DepthState(true, CompareFunction.Less);
        }

        private static void ExecutePass(PassData passData, RasterCommandBuffer cmd, bool isYFlipped)
        {
            // Matrix4x4 projectionMatrix = passData.projectionMatrix;
            // cmd.DisableScissorRect();
            // float resolutionSizeX = passData.textureSize.x;
            // float resolutionSizeY = passData.textureSize.y;
        
            // cmd.SetViewport(new Rect(0, 0, resolutionSizeX, resolutionSizeY));
            cmd.SetViewProjectionMatrices(passData.viewMatrix, passData.projectionMatrix);
            cmd.DrawRendererList(passData.rendererListHdl1);
        }

        private void InitRendererLists(UniversalRenderingData renderingData, UniversalLightData lightData,
            ref PassData passData, RenderGraph renderGraph, FilteringSettings filteringSettings)
        {
            SortingCriteria sortingCriteria = passData.cameraData.defaultOpaqueSortFlags;
            DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(shaderTagIdList, renderingData,
                passData.cameraData, lightData, sortingCriteria);
            // drawingSettings.enableInstancing = true;
            // drawingSettings.enableDynamicBatching = true;

            // drawingSettings.overrideMaterial = overrideMat;
            drawingSettings.overrideShader = (Shader.Find("ColoredShadow/OverrideColShadow_Debug"));
            // drawingSettings.overrideMaterial.SetTexture("_UV_Image", texture);
            drawingSettings.overrideShaderPassIndex = 0;
            // drawingSettings.overrideMaterialPassIndex = 0;

            CreateRendererListWithRenderStateBlock(renderGraph, ref renderingData.cullResults, drawingSettings, filteringSettings, renderStateBlock, ref passData.rendererListHdl1);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalLightData universalLightData = frameData.Get<UniversalLightData>();
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

            Matrix4x4 viewMatrix = cameraData.GetViewMatrix();
            Matrix4x4 projectionMatrix = Matrix4x4.Perspective(
                cameraData.camera.fieldOfView,
                cameraData.camera.aspect,
                cameraData.camera.nearClipPlane,
                cameraData.camera.farClipPlane
            );
            
            filteringSettings.layerMask = Int32.MaxValue;

            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null)
            {
                return;
            }
            
            int textureSizeX = cameraData.cameraTargetDescriptor.width;
            int textureSizeY = cameraData.cameraTargetDescriptor.height + 1;
        
            var destinationDescColor = renderGraph.GetTextureDesc(resourceData.cameraColor);
            destinationDescColor.format = GraphicsFormat.R32G32B32A32_SFloat;
            destinationDescColor.name = "SOURCE_COLOR";
            destinationDescColor.width = textureSizeX;
            destinationDescColor.height = textureSizeY;
            destinationDescColor.enableRandomWrite = true;
            TextureHandle destinationColor = renderGraph.CreateTexture(destinationDescColor);
        
            var destinationDescDepth = renderGraph.GetTextureDesc(resourceData.activeDepthTexture);
            destinationDescDepth.name = "SOURCE_DEPTH";
            destinationDescDepth.width = textureSizeX;
            destinationDescDepth.height = textureSizeY;
            TextureHandle destinationDepth = renderGraph.CreateTexture(destinationDescDepth);
        
            RenderTextureDescriptor shadowMapIDDesc = cameraData.cameraTargetDescriptor;
            // shadowMapIDDesc.colorFormat = RenderTextureFormat.ARGBInt;
            // shadowMapIDDesc.colorFormat = RenderTextureFormat.ARGBFloat;
            shadowMapIDDesc.width = textureSizeX;
            shadowMapIDDesc.height = textureSizeY;
            shadowMapIDDesc.depthBufferBits = 0;
            shadowMapIDDesc.msaaSamples = 1;
            RenderingUtils.ReAllocateHandleIfNeeded(ref shadowMapID, shadowMapIDDesc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: shadowMapIDName);
            TextureHandle destinationColorRT = renderGraph.ImportTexture(shadowMapID);
            
            Shader.SetGlobalFloat("_NumberSize", ColShadowDebug.ShadowNumberSize * 4);

            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Capture Scene Custom Shadow Data", out var passData, profilingSampler))
            {
                passData.color = destinationColor;
                builder.SetRenderAttachment(destinationColor, 0, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(destinationDepth, AccessFlags.Write);
                
                passData.cameraData = cameraData;
            
                passData.textureSize = new Vector2Int(textureSizeX, textureSizeY);
                passData.projectionMatrix = projectionMatrix;
                passData.viewMatrix = viewMatrix;
            
                InitRendererLists(renderingData, universalLightData, ref passData, renderGraph, filteringSettings);
                
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);
            
                builder.UseRendererList(passData.rendererListHdl1);
                
                builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) =>
                {
                    var isYFlipped = data.cameraData.IsRenderTargetProjectionMatrixFlipped(data.color);
                    ExecutePass(data, rgContext.cmd, isYFlipped);
                });
            }

            using (var builder = renderGraph.AddComputePass<PassDataCompute>("Capture Scene Custom Shadow Data Compute", out var passData, profilingSampler))
            {
                builder.UseTexture(destinationColor, AccessFlags.ReadWrite);
                builder.UseTexture(resourceData.cameraColor, AccessFlags.Read);

                passData.cs = cs;
                passData.shadowMap = destinationColor;
                passData.cameraColor = resourceData.cameraColor;
                passData.textureSizeX = textureSizeX;
                passData.textureSizeY = textureSizeY;
                
                builder.AllowPassCulling(false);
                builder.SetRenderFunc((PassDataCompute data, ComputeGraphContext context) =>
                {
                    int kernel = data.cs.FindKernel("CSMain");
                    context.cmd.SetComputeTextureParam(passData.cs, kernel, "_ShadowMap", passData.shadowMap);
                    context.cmd.SetComputeTextureParam(passData.cs, kernel, "_CameraColor", passData.cameraColor);
                    context.cmd.DispatchCompute(passData.cs, kernel, Mathf.CeilToInt(passData.textureSizeX / 32f), Mathf.CeilToInt(passData.textureSizeY / 32f), 1);
                });
            }
            
            RenderGraphUtils.BlitMaterialParameters para2 = new(destinationColor, resourceData.activeColorTexture, Blitter.GetBlitMaterial(TextureDimension.Tex2D), 0);
            renderGraph.AddBlitPass(para2, "CaptureSceneShadowsColor");
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
            internal LightMode lightMode;

            internal RendererListHandle rendererListHdl1;
            internal UniversalCameraData cameraData;
            internal Vector2Int textureSize;
        }
        
        private class PassDataCompute
        {
            internal ComputeShader cs;
            internal TextureHandle shadowMap;
            internal TextureHandle cameraColor;
            internal TextureHandle cameraActiveColor;
            internal int textureSizeX;
            internal int textureSizeY;
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
            public int lightIDMultiplier;
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
            public LightInformation(int index, int lightMode, Matrix4x4 lightMatrix, Vector3 lightPos, float fallOffRange, float farPlane, Vector3 cameraPos, int textureSizeX, int textureSizeY, int lightIDMultiplier, List<float> customValues) : this()
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
                this.lightIDMultiplier = lightIDMultiplier;
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
