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
#if UNITY_EDITOR
            renderShadowObjectsPassDebug = new RenderColoredShadowsDebug("Render Scene view Custom Shadows");
#endif
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            #if UNITY_EDITOR
            if (renderingData.cameraData.cameraType != CameraType.SceneView || !ColShadowSettings.IsEnabled)
                return;
            
            renderShadowObjectsPassDebug.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
            renderer.EnqueuePass(renderShadowObjectsPassDebug);
            #endif
        }
    }
}