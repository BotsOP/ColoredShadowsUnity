using System.Collections.Generic;
using ColoredShadows.Scripts;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace ColoredShadow.Core.Scripts
{
    public class ColoredShadowsRenderFeature : ScriptableRendererFeature
    {
        public const int MAX_AMOUNT_CUSTOM_LIGHTS = 10;
        public ComputeShader cs;
    
        private RenderColoredShadows renderShadowObjectsPassPoint;
        private Dictionary<Camera, CustomLight> cameraLightPair;
        private RenderColoredShadows.LightInformation[] lightInformations;
        private GraphicsBuffer lightInformationBuffer;
        private MergeShadowMaps mergeShadowMaps;
        public override void Create()
        {
            CustomLight[] lights = FindObjectsByType<CustomLight>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.InstanceID
            );
        
            cameraLightPair = new Dictionary<Camera, CustomLight>();
            // foreach (CustomLight light in lights)
            // {
            //     cameraLightPair.Add(light.transform.GetComponent<Camera>(), light);
            // }
            lightInformations = new RenderColoredShadows.LightInformation[MAX_AMOUNT_CUSTOM_LIGHTS];
            lightInformationBuffer = new GraphicsBuffer(
                GraphicsBuffer.Target.Structured,
                MAX_AMOUNT_CUSTOM_LIGHTS,
                sizeof(int) * 2 +
                sizeof(float) * 16 +
                sizeof(float) * 3 +
                sizeof(float) * 2 +
                sizeof(float) * 3 +
                sizeof(int) * 3 +
                sizeof(float) * 12
            );
        
            renderShadowObjectsPassPoint = new RenderColoredShadows("Render Custom Point Shadows depth", cs, lightInformations, lightInformationBuffer);
            mergeShadowMaps = new MergeShadowMaps();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType == CameraType.Preview
                || UniversalRenderer.IsOffscreenDepthTexture(ref renderingData.cameraData))
                return;
        
            if (renderingData.cameraData.cameraType != CameraType.Game)
                return;

            Camera camera = renderingData.cameraData.camera;
            if (!cameraLightPair.ContainsKey(camera))
            {
                cameraLightPair.Add(camera, camera.transform.GetComponent<CustomLight>());
            }
            Shader.SetGlobalInt("CurrentAmountCustomLights", cameraLightPair.Count);
            if (cameraLightPair[camera] == null)
            {
                cameraLightPair[camera] = camera.transform.GetComponent<CustomLight>();
            }
            renderShadowObjectsPassPoint.customLight = cameraLightPair[camera];
        
            renderer.EnqueuePass(renderShadowObjectsPassPoint);

            if (cameraLightPair[camera].lightIndex == 0)
            {
                renderer.EnqueuePass(mergeShadowMaps);
            }
        }

        protected override void Dispose(bool disposing)
        {
            lightInformationBuffer?.Release();
            lightInformationBuffer = null;
            renderShadowObjectsPassPoint?.Dispose();
            renderShadowObjectsPassPoint = null;
        }
    }
    
    public class CustomData : ContextItem {
        public TextureHandle testTexture1;

        public override void Reset()
        {
            testTexture1 = TextureHandle.nullHandle;
        }
    }
}