using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ColoredShadowsRenderFeature2 : ScriptableRendererFeature
{
    private RenderColoredShadows2 renderColoredShadows2;

    public override void Create()
    {
        renderColoredShadows2 = new RenderColoredShadows2();
    }
    
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType == CameraType.Preview
            || UniversalRenderer.IsOffscreenDepthTexture(ref renderingData.cameraData))
            return;
        renderer.EnqueuePass(renderColoredShadows2);
    }
    
    
}
