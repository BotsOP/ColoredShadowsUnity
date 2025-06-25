using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace ColoredShadows.Scripts
{
    public class ColoredShadowsDebugRenderFeature : ScriptableRendererFeature
    {
        public Texture2D texture;
        public Material material;
        private RenderColoredShadowsDebug renderShadowObjectsPassDebug;
        public override void Create()
        {
            renderShadowObjectsPassDebug = new RenderColoredShadowsDebug("Render Scene view Custom Shadows", texture, material);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType == CameraType.SceneView)
            {
                renderShadowObjectsPassDebug.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
                renderer.EnqueuePass(renderShadowObjectsPassDebug);
            }
        }
    }
}