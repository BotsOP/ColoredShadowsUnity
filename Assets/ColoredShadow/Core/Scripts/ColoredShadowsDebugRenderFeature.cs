using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace ColoredShadows.Scripts
{
    public class ColoredShadowsDebugRenderFeature : ScriptableRendererFeature
    {
        private RenderColoredShadowsDebug renderShadowObjectsPassDebug;
        public override void Create()
        {
            renderShadowObjectsPassDebug = new RenderColoredShadowsDebug("Render Scene view Custom Shadows");
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType != CameraType.SceneView || !ColShadowDebug.IsEnabled)
                return;
            
            renderShadowObjectsPassDebug.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
            renderer.EnqueuePass(renderShadowObjectsPassDebug);
        }
    }
}