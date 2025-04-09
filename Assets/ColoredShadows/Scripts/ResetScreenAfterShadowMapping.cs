using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

public class ResetScreenAfterShadowMapping : ScriptableRenderPass
{
    const string m_PassName = "ResetScreen";

    public ResetScreenAfterShadowMapping(RenderPassEvent evt)
    {
        renderPassEvent = evt;
    }
    

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        var resourceData = frameData.Get<UniversalResourceData>();
        
        if (resourceData.isActiveTargetBackBuffer)
        {
            Debug.LogError($"Skipping render pass. DitherEffectRendererFeature requires an intermediate ColorTexture, we can't use the BackBuffer as a texture input.");
            return;
        }
        
        var customData = frameData.Get<ColoredShadowsRenderFeature.MyCustomData>();
        // resourceData.cameraColor = customData.cameraColor;
        
        RenderGraphUtils.BlitMaterialParameters para = new(customData.cameraColor, resourceData.cameraColor, Blitter.GetBlitMaterial(TextureDimension.Tex2D), 0);
        renderGraph.AddBlitPass(para, passName: m_PassName);
    }
}
