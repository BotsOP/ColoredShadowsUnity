using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

// This Renderer Feature sets up the BlitToRTHandlePass pass.
public class ColoredDirectionalShadowsRenderFeature : ScriptableRendererFeature
{
    public RenderPassEvent injectionPoint = RenderPassEvent.AfterRenderingTransparents;
    public FilterSettings filterSettings;
    public Material shadowOverrideMaterial;
    

    private RenderShadowObjectsDirectional renderShadowObjectsDirectionalPass;
    private RenderShadowObjectsPoint renderShadowObjectsPointPass;
    public override void Create()
    {
        CustomDirectionalLight customDirectionalLight = FindAnyObjectByType<CustomDirectionalLight>();
        CustomPointLight customPointLight = FindAnyObjectByType<CustomPointLight>();
        
        renderShadowObjectsDirectionalPass = new RenderShadowObjectsDirectional("Render Custom Shadows depth", injectionPoint, filterSettings.PassNames,
            filterSettings.RenderQueueType, filterSettings.LayerMask, filterSettings.LayerMaskID, customDirectionalLight, shadowOverrideMaterial);
        renderShadowObjectsPointPass = new RenderShadowObjectsPoint("Render Custom Shadows depth", injectionPoint, filterSettings.PassNames,
            filterSettings.RenderQueueType, filterSettings.LayerMask, filterSettings.LayerMaskID, customDirectionalLight, shadowOverrideMaterial);
    }
    
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType == CameraType.Preview
            || UniversalRenderer.IsOffscreenDepthTexture(ref renderingData.cameraData))
            return;
        
        if (renderingData.cameraData.cameraType != CameraType.Game)
            return;
        
        // renderer.EnqueuePass(renderShadowObjectsDirectionalPass);
        // renderer.EnqueuePass(renderShadowObjectsPointPass);
        
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
    public struct CustomLightData
    {
        public enum LightMode
        {
            Ortho,
            FOV,
        }

        public LightMode lightMode;
        public float nearPlane, farPlane;
        public float horizontalSize, verticalSize;
        public float fov, aspectRatio;

        public CustomLightData(LightMode lightMode, float nearPlane, float farPlane, float horizontalSize, float verticalSize, float fov, float aspectRatio)
        {
            this.lightMode = lightMode;
            this.nearPlane = nearPlane;
            this.farPlane = farPlane;
            this.horizontalSize = horizontalSize;
            this.verticalSize = verticalSize;
            this.fov = fov;
            this.aspectRatio = aspectRatio;
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
        renderShadowObjectsDirectionalPass?.Dispose();
        renderShadowObjectsDirectionalPass = null;
    }
}