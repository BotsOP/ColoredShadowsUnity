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
    // public RenderObjects.RenderObjectsSettings settings = new RenderObjects.RenderObjectsSettings();
    public Material overrideMaterial;
    public LayerMask m_LayerMask;

    private BlitToRTHandlePass blitToRTHandlePass;
    // private RenderObjectsPass renderObjectsPass;
    private RendererListRenderFeature.RendererListPass rendererListRender;
    private CaptureShadowMap captureShadows;
    private CaptureScreenRenderFeature captureScreen;
    private ResetScreenAfterShadowMapping resetScreenAfterShadowMapping;
    
    public RenderObjects2.RenderObjectsSettings settings = new RenderObjects2.RenderObjectsSettings();

    private RenderObjectsPass2 renderObjectsPass;
    private CopyDepthPass2 copyDepthPass2;
    public override void Create()
    {
        captureShadows = new CaptureShadowMap(injectionPoint);
        blitToRTHandlePass = new BlitToRTHandlePass(injectionPoint);
        captureScreen = new CaptureScreenRenderFeature(injectionPoint);
        resetScreenAfterShadowMapping = new ResetScreenAfterShadowMapping(injectionPoint);
        rendererListRender = new RendererListRenderFeature.RendererListPass(injectionPoint, m_LayerMask, overrideMaterial);

        Shader copyDephPS = Shader.Find("Hidden/Editor Gizmo");
        if (GraphicsSettings.TryGetRenderPipelineSettings<UniversalRendererResources>(
                out var universalRendererShaders))
        {
            copyDephPS = universalRendererShaders.copyDepthPS;
        }
        
        copyDepthPass2 = new CopyDepthPass2(injectionPoint, copyDephPS, false, false, false, "TestDepthCopy");
        
        
        RenderObjects2.FilterSettings filter = settings.filterSettings;
        
            // Render Objects pass doesn't support events before rendering prepasses.
            // The camera is not setup before this point and all rendering is monoscopic.
            // Events before BeforeRenderingPrepasses should be used for input texture passes (shadow map, LUT, etc) that doesn't depend on the camera.
            // These events are filtering in the UI, but we still should prevent users from changing it from code or
            // by changing the serialized data.
            if (settings.Event < RenderPassEvent.BeforeRenderingPrePasses)
                settings.Event = RenderPassEvent.BeforeRenderingPrePasses;
        
            renderObjectsPass = new RenderObjectsPass2(settings.passTag, injectionPoint, filter.PassNames,
                filter.RenderQueueType, filter.LayerMask, settings.cameraSettings);
        
            switch (settings.overrideMode)
            {
                case RenderObjects2.RenderObjectsSettings.OverrideMaterialMode.None:
                    renderObjectsPass.overrideMaterial = null;
                    renderObjectsPass.overrideShader = null;
                    break;
                case RenderObjects2.RenderObjectsSettings.OverrideMaterialMode.Material:
                    renderObjectsPass.overrideMaterial = settings.overrideMaterial;
                    renderObjectsPass.overrideMaterialPassIndex = settings.overrideMaterialPassIndex;
                    renderObjectsPass.overrideShader = null;
                    break;
                case RenderObjects2.RenderObjectsSettings.OverrideMaterialMode.Shader:
                    renderObjectsPass.overrideMaterial = null;
                    renderObjectsPass.overrideShader = settings.overrideShader;
                    renderObjectsPass.overrideShaderPassIndex = settings.overrideShaderPassIndex;
                    break;
            }
        
            if (settings.overrideDepthState)
                renderObjectsPass.SetDepthState(settings.enableWrite, settings.depthCompareFunction);
        
            if (settings.stencilSettings.overrideStencilState)
                renderObjectsPass.SetStencilState(settings.stencilSettings.stencilReference,
                    settings.stencilSettings.stencilCompareFunction, settings.stencilSettings.passOperation,
                    settings.stencilSettings.failOperation, settings.stencilSettings.zFailOperation);
    }
    
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType == CameraType.Preview
            || UniversalRenderer.IsOffscreenDepthTexture(ref renderingData.cameraData))
            return;
        
        if (renderingData.cameraData.cameraType != CameraType.Game)
            return;
        
        renderer.EnqueuePass(captureScreen);
        renderer.EnqueuePass(renderObjectsPass);
        renderer.EnqueuePass(copyDepthPass2);
        renderer.EnqueuePass(captureShadows);
        renderer.EnqueuePass(resetScreenAfterShadowMapping);
        // renderer.EnqueuePass(blitToRTHandlePass);
    }
    
    public class MyCustomData : ContextItem {
        public TextureHandle colorBeforeShadow;
        public TextureHandle shadowMapDepthFormatted;
        public TextureHandle shadowMapColorFormatted;
        public TextureHandle shadowMapID;
        public override void Reset()
        {
            colorBeforeShadow = TextureHandle.nullHandle;
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