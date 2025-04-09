using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

public class CaptureScreenRenderFeature : ScriptableRenderPass
{
    const string m_PassName = "CaptureScreenBeforeShadowMap";

    public CaptureScreenRenderFeature(RenderPassEvent evt)
    {
        renderPassEvent = evt;
    }

    // Function used to transfer the material from the renderer feature to the render pass.
    public void Setup()
    {
        //The pass will read the current color texture. That needs to be an intermediate texture. It's not supported to use the BackBuffer as input texture. 
        //By setting this property, URP will automatically create an intermediate texture. 
        //It's good practice to set it here and not from the RenderFeature. This way, the pass is selfcontaining and you can use it to directly enqueue the pass from a monobehaviour without a RenderFeature.
        requiresIntermediateTexture = true;
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        // UniversalResourceData contains all the texture handles used by the renderer, including the active color and depth textures
        // The active color and depth textures are the main color and depth buffers that the camera renders into
        var resourceData = frameData.Get<UniversalResourceData>();

        //This should never happen since we set m_Pass.requiresIntermediateTexture = true;
        //Unless you set the render event to AfterRendering, where we only have the BackBuffer. 
        if (resourceData.isActiveTargetBackBuffer)
        {
            Debug.LogError($"Skipping render pass. DitherEffectRendererFeature requires an intermediate ColorTexture, we can't use the BackBuffer as a texture input.");
            return;
        }

        // The destination texture is created here, 
        // the texture is created with the same dimensions as the active color texture
        var source = resourceData.activeColorTexture;

        var destinationDesc = renderGraph.GetTextureDesc(source);
        // destinationDesc.colorFormat = GraphicsFormat.R32G32B32A32_SFloat;
        // destinationDesc.clearColor = Color.white;
        // destinationDesc.filterMode = FilterMode.Bilinear;
        // destinationDesc.wrapMode = TextureWrapMode.Clamp;
        destinationDesc.name = $"BEFORE_SHADOW_CAPTURE";
        // destinationDesc.clearBuffer = true;

        TextureHandle destination = renderGraph.CreateTexture(destinationDesc);
        
        RenderGraphUtils.BlitMaterialParameters para = new(source, destination, Blitter.GetBlitMaterial(TextureDimension.Tex2D), 0);
        renderGraph.AddBlitPass(para, passName: m_PassName);

        var customData = frameData.Create<ColoredShadowsRenderFeature.MyCustomData>();
        customData.cameraColor = destination;
        resourceData.cameraColor = source;
    }
}
