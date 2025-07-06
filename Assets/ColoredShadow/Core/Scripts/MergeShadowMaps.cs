using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace ColoredShadow.Core.Scripts
{
    public class MergeShadowMaps : ScriptableRenderPass
    {
        static void ExecutePassCompute(PassData data, ComputeGraphContext cgContext)
        {
            int threadGroupSize = Mathf.CeilToInt(1024 / 32f);
            cgContext.cmd.SetComputeTextureParam(data.cs, data.cs.FindKernel("SampleShadowMap"), "_ShadowAtlas", data.copySourceTexture);
            cgContext.cmd.DispatchCompute(data.cs, data.cs.FindKernel("SampleShadowMap"), threadGroupSize, threadGroupSize, 1);
        }
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            // UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            //
            // var destinationDescColor = renderGraph.GetTextureDesc(resourceData.activeColorTexture);
            // destinationDescColor.format = GraphicsFormat.R32G32B32A32_SInt;
            // destinationDescColor.name = "SOURCE_COLOR";
            // destinationDescColor.width = 1024;
            // destinationDescColor.height = 1024;
            // destinationDescColor.enableRandomWrite = true;
            // TextureHandle destinationColor = renderGraph.CreateTexture(destinationDescColor);
            //
            // using (var builder = renderGraph.AddComputePass("MyComputePassTEST", out PassData data))
            // {
            //     data.copySourceTexture = destinationColor;
            //     data.cs = Resources.Load<ComputeShader>("MergeShadowMaps");
            //     builder.AllowPassCulling(false);
            //     // builder.UseGlobalTexture(Shader.PropertyToID("_TestTexture1"));
            //     builder.UseTexture(destinationColor);
            //     builder.SetRenderFunc((PassData data, ComputeGraphContext context) => ExecutePassCompute(data, context));
            // }
        }
        
        class PassData
        {
            internal TextureHandle copySourceTexture;
            internal ComputeShader cs;
        }
    }
}
