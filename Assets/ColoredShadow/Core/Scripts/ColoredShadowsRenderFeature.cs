using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace ColoredShadows.Scripts
{
    public class ColoredShadowsRenderFeature : ScriptableRendererFeature
    {
        public const int MAX_AMOUNT_CUSTOM_LIGHTS = 100;
        public RenderPassEvent injectionPoint = RenderPassEvent.AfterRenderingTransparents;
        public FilterSettings filterSettings;
        public Material shadowOverrideMaterial;
    
        private RenderColoredShadows renderShadowObjectsPassPoint;
        private Dictionary<Camera, CustomLight> cameraLightPair;
        private RenderColoredShadows.LightInformation[] lightInformations;
        private GraphicsBuffer lightInformationBuffer;
        public override void Create()
        {
            CustomLight[] lights = FindObjectsByType<CustomLight>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.InstanceID
            );
        
            cameraLightPair = new Dictionary<Camera, CustomLight>();
            foreach (CustomLight light in lights)
            {
                cameraLightPair.Add(light.transform.GetComponent<Camera>(), light);
            }
            lightInformations = new RenderColoredShadows.LightInformation[MAX_AMOUNT_CUSTOM_LIGHTS];
            lightInformationBuffer = new GraphicsBuffer(
                GraphicsBuffer.Target.Structured,
                MAX_AMOUNT_CUSTOM_LIGHTS,
                sizeof(int) * 2 +
                sizeof(float) * 16 +
                sizeof(float) * 3 +
                sizeof(float) * 2 +
                sizeof(float) * 3 +
                sizeof(int) * 3
            );
            Debug.Log($"Create");
        
            renderShadowObjectsPassPoint = new RenderColoredShadows("Render Custom Point Shadows depth", injectionPoint, filterSettings.PassNames,
                filterSettings.RenderQueueType, filterSettings.LayerMask, shadowOverrideMaterial, lightInformations, lightInformationBuffer);
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
            renderShadowObjectsPassPoint.customLight = cameraLightPair[camera];
            // Debug.Log($"Add render pass");
        
            renderer.EnqueuePass(renderShadowObjectsPassPoint);
        }
    
        public class CustomShadowData : ContextItem 
        {
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
        public class FilterSettings
        {
            public RenderQueueType RenderQueueType;
            public LayerMask LayerMask;
            public string[] PassNames;

            public FilterSettings()
            {
                RenderQueueType = RenderQueueType.Opaque;
                LayerMask = 0;
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
}