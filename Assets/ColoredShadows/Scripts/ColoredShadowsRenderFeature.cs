using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

// This Renderer Feature sets up the BlitToRTHandlePass pass.
public class ColoredShadowsRenderFeature : ScriptableRendererFeature
{
    public RenderPassEvent injectionPoint = RenderPassEvent.AfterRenderingTransparents;
    [Header("test")]

    public RenderObjects2.FilterSettings filterSettings;
    public RenderObjects2.FilterSettings filterSettingsID;
    public float shadowID;
    public RenderObjects2.FilterSettings filterSettingsID2;
    public float shadowID2;
    public RenderObjects2.FilterSettings filterSettingsID3;
    public float shadowID3;

    public Material shadowOverrideMaterial;

    private CaptureShadowMap captureShadows;
    private RenderObjectsPass2 renderObjectsPass;
    private RenderObjectsPass2 renderObjectsPassID;
    private RenderObjectsPass2 renderObjectsPassID2;
    private RenderObjectsPass2 renderObjectsPassID3;
    private CopyDepthPass2 copyDepthPass2;
    public override void Create()
    {
        Transform lightTransform = FindAnyObjectByType<Light>().transform;
        captureShadows = new CaptureShadowMap(injectionPoint, lightTransform);

        copyDepthPass2 = new CopyDepthPass2(injectionPoint, Shader.Find("Hidden/Universal Render Pipeline/CopyDepth"), false, false, false, "Copy Shadow Depth");
        
        renderObjectsPass = new RenderObjectsPass2("Render Custom Shadows depth", injectionPoint, filterSettings.PassNames,
            filterSettings.RenderQueueType, filterSettings.LayerMask, true, shadowOverrideMaterial);
        renderObjectsPassID = new RenderObjectsPass2("Render Custom Shadows ID", injectionPoint, filterSettingsID.PassNames,
            filterSettingsID.RenderQueueType, filterSettingsID.LayerMask, false, shadowOverrideMaterial, 0, shadowID);
        renderObjectsPassID2 = new RenderObjectsPass2("Render Custom Shadows ID2", injectionPoint, filterSettingsID2.PassNames,
            filterSettingsID2.RenderQueueType, filterSettingsID2.LayerMask, false, shadowOverrideMaterial, 0, shadowID2);
        renderObjectsPassID3 = new RenderObjectsPass2("Render Custom Shadows ID3", injectionPoint, filterSettingsID3.PassNames,
            filterSettingsID3.RenderQueueType, filterSettingsID3.LayerMask, false, shadowOverrideMaterial, 0, shadowID3);
    }
    
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType == CameraType.Preview
            || UniversalRenderer.IsOffscreenDepthTexture(ref renderingData.cameraData))
            return;
        
        if (renderingData.cameraData.cameraType != CameraType.Game)
            return;
        
        renderer.EnqueuePass(renderObjectsPass);
        renderer.EnqueuePass(renderObjectsPassID);
        renderer.EnqueuePass(renderObjectsPassID2);
        renderer.EnqueuePass(renderObjectsPassID3);
        renderer.EnqueuePass(copyDepthPass2);
        renderer.EnqueuePass(captureShadows);
    }
    
    public class CustomShadowData : ContextItem {
        public TextureHandle shadowMapDepthFormatted;
        public TextureHandle shadowMapColorFormatted;
        public TextureHandle shadowMapID;
        public override void Reset()
        {
            shadowMapDepthFormatted = TextureHandle.nullHandle;
            shadowMapColorFormatted = TextureHandle.nullHandle;
            shadowMapID = TextureHandle.nullHandle;
        }
    }

    protected override void Dispose(bool disposing)
    {
        captureShadows?.Dispose();
        captureShadows = null;
    }
}