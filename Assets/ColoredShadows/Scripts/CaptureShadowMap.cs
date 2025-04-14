using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

// This pass creates an RTHandle and blits the camera color to it after rendering transparent objects.
// The RTHandle is then set as a global texture, which is available to shaders in the scene. The RTHandle is preserved in all frames while the renderer feature is running to create a recursive rendering effect.
public class CaptureShadowMap : ScriptableRenderPass
{
    private RTHandle shadowMap;
    private RTHandle shadowMapID;
    private Vector2Int depthDimensions, shadowIdDimensions;
    private const string shadowMapName = "_CustomShadowMap";
    private const string shadowMapIDName = "_CustomShadowMapID";
    private static readonly int shadowMapShaderID = Shader.PropertyToID(shadowMapName);
    private static readonly int shadowMapIDShaderID = Shader.PropertyToID(shadowMapIDName);

    public CaptureShadowMap(RenderPassEvent evt, Vector2Int depthDimensions, Vector2Int shadowIdDimensions)
    {
        renderPassEvent = evt;
        this.depthDimensions = depthDimensions;
        this.shadowIdDimensions = shadowIdDimensions;
    }

    // Unity calls the RecordRenderGraph method to add and configure one or more render passes in the render graph system.
    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
        UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

        if (cameraData.camera.cameraType != CameraType.Game)
            return;

        // Create the custom RTHandle
        RenderTextureDescriptor shadowMapDesc = cameraData.cameraTargetDescriptor;
        shadowMapDesc.colorFormat = RenderTextureFormat.RFloat;
        shadowMapDesc.width = depthDimensions.x;
        shadowMapDesc.height = depthDimensions.y;
        shadowMapDesc.depthBufferBits = 0;
        shadowMapDesc.msaaSamples = 1;
        RenderingUtils.ReAllocateHandleIfNeeded(ref shadowMap, shadowMapDesc, FilterMode.Trilinear, TextureWrapMode.Clamp, name: shadowMapName );
        
        RenderTextureDescriptor shadowMapIDDesc = cameraData.cameraTargetDescriptor;
        shadowMapIDDesc.colorFormat = RenderTextureFormat.RFloat;
        shadowMapIDDesc.width = shadowIdDimensions.x;
        shadowMapIDDesc.height = shadowIdDimensions.y;
        shadowMapIDDesc.depthBufferBits = 0;
        shadowMapIDDesc.msaaSamples = 1;
        RenderingUtils.ReAllocateHandleIfNeeded(ref shadowMapID, shadowMapIDDesc, FilterMode.Trilinear, TextureWrapMode.Clamp, name: shadowMapIDName );
        
        // Make the output texture available for the shaders in the scene.
        // In this sample the output texture is used recursively by the subsequent frames, so it must stay in memory while the renderer feature is running.
        // A TextureHandle object is discarded after each frame, that's why we cannot bind it as a global texture using the RenderGraph API (builder.SetGlobalTextureAfterPass).
        // Instead, we bind the RTHandle as a global texture using the shader API, because the RTHandle is not managed by the render graph system.
        Shader.SetGlobalTexture(shadowMapShaderID, shadowMap);
        Shader.SetGlobalTexture(shadowMapIDShaderID, shadowMapID);

        // Set camera color as a texture resource for this render graph instance
        var customData = frameData.Get<ColoredShadowsRenderFeature.CustomShadowData>();
        TextureHandle sourceDepth = customData.shadowMapColorFormatted;
        TextureHandle sourceColor = customData.shadowMapID;

        // Set RTHandle as a texture resource for this render graph instance
        TextureHandle destinationDepth = renderGraph.ImportTexture(shadowMap);
        TextureHandle destinationColor = renderGraph.ImportTexture(shadowMapID);
        
        if (!sourceColor.IsValid() || !destinationDepth.IsValid())
            return;
        
        RenderGraphUtils.BlitMaterialParameters para = new(sourceDepth, destinationDepth, Blitter.GetBlitMaterial(TextureDimension.Tex2D), 0);
        renderGraph.AddBlitPass(para, "CaptureShadowsDepth");
        
        RenderGraphUtils.BlitMaterialParameters para2 = new(sourceColor, destinationColor, Blitter.GetBlitMaterial(TextureDimension.Tex2D), 0);
        renderGraph.AddBlitPass(para2, "CaptureShadowsColor");
    }
    
    public void Dispose()
    {
        shadowMap?.Release();
        shadowMapID?.Release();
    }
}