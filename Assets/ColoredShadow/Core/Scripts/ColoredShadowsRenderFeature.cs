using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ColoredShadow.Core.Scripts
{
    public class ColoredShadowsRenderFeature : ScriptableRendererFeature
    {
        private RenderColoredShadows renderColoredShadows;
        private GraphicsBuffer lightInformationBuffer;
        private GraphicsBuffer counterBuffer;

        public override void Create()
        {
            Debug.Log(sizeof(bool));
            lightInformationBuffer = new GraphicsBuffer(
                GraphicsBuffer.Target.Structured,
                100,
                sizeof(int) * 2 +
                sizeof(float) * 16 +
                sizeof(float) * 16 +
                sizeof(float) * 4 +
                sizeof(float) * 2 +
                sizeof(float) * 3 +
                sizeof(int) * 7 +
                sizeof(float) * 12
            );
            counterBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 1, sizeof(int));
        
            renderColoredShadows = new RenderColoredShadows(lightInformationBuffer, counterBuffer);
            renderColoredShadows.renderPassEvent = RenderPassEvent.AfterRendering;
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
            counterBuffer?.Release();
            counterBuffer = null;
            lightInformationBuffer?.Release();
            lightInformationBuffer = null;
            renderColoredShadows?.Dispose();
            renderColoredShadows = null;
        }
    }
}
