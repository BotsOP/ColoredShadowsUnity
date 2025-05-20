using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

// This Renderer Feature sets up the BlitToRTHandlePass pass.
public class ColoredPointShadowsRenderFeature : ScriptableRendererFeature
{
    public RenderPassEvent injectionPoint = RenderPassEvent.AfterRenderingTransparents;
    public FilterSettings filterSettings;
    public Material shadowOverrideMaterial;
    

    private RenderShadowObjectsDirectional renderShadowObjectsDirectionalPass;
    private RenderShadowObjectsPoint renderShadowObjectsPassPoint;
    public override void Create()
    {
        CustomDirectionalLight customDirectionalLight = FindAnyObjectByType<CustomDirectionalLight>();
        CustomPointLight customPointLight = FindAnyObjectByType<CustomPointLight>();
        // copyDepthPass2 = new CopyDepthPass2(injectionPoint, Shader.Find("Hidden/Universal Render Pipeline/CopyDepth"), false, false, false, "Copy Shadow Depth");
        
        // renderShadowObjectsPass = new RenderShadowObjects("Render Custom Shadows depth", injectionPoint, filterSettings.PassNames,
            // filterSettings.RenderQueueType, filterSettings.LayerMask, filterSettings.LayerMaskID, customLight, shadowOverrideMaterial);
        renderShadowObjectsPassPoint = new RenderShadowObjectsPoint("Render Custom Point Shadows depth", injectionPoint, filterSettings.PassNames,
            filterSettings.RenderQueueType, filterSettings.LayerMask, filterSettings.LayerMaskID, customDirectionalLight, shadowOverrideMaterial);
    }
    
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType == CameraType.Preview
            || UniversalRenderer.IsOffscreenDepthTexture(ref renderingData.cameraData))
            return;
        
        if (renderingData.cameraData.cameraType != CameraType.Game)
            return;
        
        renderer.EnqueuePass(renderShadowObjectsPassPoint);
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

    
    
    [System.Serializable]
    public class FilterSettings
    {
        // TODO: expose opaque, transparent, all ranges as drop down

        /// <summary>
        /// The queue type for the objects to render.
        /// </summary>
        public RenderQueueType RenderQueueType;

        /// <summary>
        /// The layer mask to use.
        /// </summary>
        public LayerMask LayerMask;
        public LayerMask LayerMaskID;

        /// <summary>
        /// The passes to render.
        /// </summary>
        public string[] PassNames;

        /// <summary>
        /// The constructor for the filter settings.
        /// </summary>
        public FilterSettings()
        {
            RenderQueueType = RenderQueueType.Opaque;
            LayerMask = 0;
            LayerMaskID = 0;
        }
    }

    protected override void Dispose(bool disposing)
    {
        renderShadowObjectsPassPoint?.Dispose();
        renderShadowObjectsPassPoint = null;
    }
}