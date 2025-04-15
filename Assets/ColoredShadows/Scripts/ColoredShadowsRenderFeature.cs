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

    public Material shadowOverrideMaterial;

    public CustomLightData lightData;
    public Vector2Int depthDimensions = new Vector2Int(1024, 1024);
    public Vector2Int shadowIDimension = new Vector2Int(1024, 1024);

    private CaptureShadowMap captureShadows;
    private RenderShadowObjects renderShadowObjectsPass;
    private RenderShadowObjectsID renderShadowObjectsPassID;
    private CopyDepthPass2 copyDepthPass2;
    public override void Create()
    {
        Debug.Log($"Create");
        Transform lightTransform = FindAnyObjectByType<Light>().transform;
        captureShadows = new CaptureShadowMap(injectionPoint, depthDimensions, shadowIDimension);

        copyDepthPass2 = new CopyDepthPass2(injectionPoint, Shader.Find("Hidden/Universal Render Pipeline/CopyDepth"), false, false, false, "Copy Shadow Depth");
        
        renderShadowObjectsPass = new RenderShadowObjects("Render Custom Shadows depth", injectionPoint, filterSettings.PassNames,
            filterSettings.RenderQueueType, filterSettings.LayerMask, lightTransform, lightData, depthDimensions, shadowIDimension, shadowOverrideMaterial);
        renderShadowObjectsPassID = new RenderShadowObjectsID("Render Custom Shadows ID", injectionPoint, filterSettingsID.PassNames,
            filterSettingsID.RenderQueueType, filterSettingsID.LayerMask, lightTransform, lightData, depthDimensions, shadowIDimension, shadowOverrideMaterial, 0);
    }
    
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType == CameraType.Preview
            || UniversalRenderer.IsOffscreenDepthTexture(ref renderingData.cameraData))
            return;
        
        if (renderingData.cameraData.cameraType != CameraType.Game)
            return;
        
        renderer.EnqueuePass(renderShadowObjectsPass);
        renderer.EnqueuePass(renderShadowObjectsPassID);
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

    protected override void Dispose(bool disposing)
    {
        captureShadows?.Dispose();
        captureShadows = null;
    }
}