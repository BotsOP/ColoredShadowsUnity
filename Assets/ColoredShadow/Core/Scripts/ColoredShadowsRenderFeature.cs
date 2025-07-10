using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ColoredShadow.Core.Scripts
{
    public class ColoredShadowsRenderFeature : ScriptableRendererFeature
    {
        private RenderColoredShadows renderColoredShadows;
        private GraphicsBuffer lightInformationBuffer;

        public override void Create()
        {
            lightInformationBuffer = new GraphicsBuffer(
                GraphicsBuffer.Target.Structured,
                100,
                sizeof(int) * 2 +
                sizeof(float) * 16 +
                sizeof(float) * 16 +
                sizeof(float) * 3 +
                sizeof(float) * 2 +
                sizeof(float) * 3 +
                sizeof(int) * 5 +
                sizeof(float) * 12
            );
        
            renderColoredShadows = new RenderColoredShadows(lightInformationBuffer);
        }
    
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType == CameraType.Preview
                || UniversalRenderer.IsOffscreenDepthTexture(ref renderingData.cameraData))
                return;
        
            renderer.EnqueuePass(renderColoredShadows);
        }
        
        protected override void Dispose(bool disposing)
        {
            lightInformationBuffer?.Release();
            lightInformationBuffer = null;
            renderColoredShadows?.Dispose();
            renderColoredShadows = null;
        }
    }
}
