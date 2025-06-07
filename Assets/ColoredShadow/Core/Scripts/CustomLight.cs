using System;
using UnityEngine;

namespace ColoredShadows.Scripts
{
    [SelectionBase]
    public class CustomLight : MonoBehaviour
    {
        public int lightIndex = -1;
        public CustomLightData lightData = new CustomLightData(CustomLightData.LightMode.Directional, 10, 0.1f, 50, 20, 20, 30, 1, 20, 0);
        public Vector2Int shadowTextureSize = new Vector2Int(1024, 1024);
        public Shader overrideShader;

        private void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Color farPlaneFillColor = new Color(1, 1, 1, 0.1f);
            Color farPlaneOutlineColor = new Color(1, 1, 1, 0.8f);
            
            Color fallOffFillColor = new Color(1, 0, 0, 0.05f);
            Color fallOffOutlineColor = new Color(1, 0, 0, 0.8f);

            switch (lightData.lightMode)
            {
                case CustomLightData.LightMode.Point :
                    Gizmos.matrix = Matrix4x4.Translate(transform.position); 
                    Gizmos.color = farPlaneFillColor;
                    Gizmos.DrawCube(Vector3.zero, Vector3.one * lightData.radius * 2);
                    Gizmos.color = farPlaneOutlineColor;
                    Gizmos.DrawWireCube(Vector3.zero, Vector3.one * lightData.radius * 2);
                    
                    Gizmos.color = fallOffFillColor;
                    Gizmos.DrawSphere(Vector3.zero, lightData.fallOffRange);
                    Gizmos.color = fallOffOutlineColor;
                    Gizmos.DrawWireSphere(Vector3.zero, lightData.fallOffRange);
                    break;
                case CustomLightData.LightMode.Directional :
                    Gizmos.color = farPlaneFillColor;
                    Gizmos.DrawCube(Vector3.forward * lightData.farPlane / 2, new Vector3(lightData.horizontalSize, lightData.verticalSize, lightData.farPlane));
                    Gizmos.color = farPlaneOutlineColor;
                    Gizmos.DrawWireCube(Vector3.forward * lightData.farPlane / 2, new Vector3(lightData.horizontalSize, lightData.verticalSize, lightData.farPlane));
                    
                    // Gizmos.color = fallOffFillColor;
                    // Gizmos.DrawCube(Vector3.forward * lightData.fallOffRange / 2, new Vector3(lightData.horizontalSize, lightData.verticalSize, lightData.fallOffRange));
                    // Gizmos.color = fallOffOutlineColor;
                    // Gizmos.DrawWireCube(Vector3.forward * lightData.fallOffRange / 2, new Vector3(lightData.horizontalSize, lightData.verticalSize, lightData.fallOffRange));
                    break;
                case CustomLightData.LightMode.Spot :
                    Gizmos.color = farPlaneFillColor;
                    Gizmos.DrawFrustum(Vector3.zero, lightData.fov, lightData.farPlane, lightData.nearPlane, lightData.aspectRatio);
                    Gizmos.color = fallOffOutlineColor;
                    Gizmos.DrawFrustum(Vector3.zero, lightData.fov, lightData.fallOffRange, lightData.nearPlane, lightData.aspectRatio);
                    break;
            }
        }

        private void OnValidate()
        {
            if (overrideShader == null)
            {
                overrideShader = Shader.Find("Shader Graphs/OverrideColShadow_UV_UVSize");
            }
            
            CustomLight[] lights = FindObjectsByType<CustomLight>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.InstanceID
            );
            if (lights.Length >= ColoredShadowsRenderFeature.MAX_AMOUNT_CUSTOM_LIGHTS)
            {
                Debug.LogError($"Cannot have more then {ColoredShadowsRenderFeature.MAX_AMOUNT_CUSTOM_LIGHTS} amount of custom lights");
                return;
            }
            Shader.SetGlobalInt("CurrentAmountCustomLights", lights.Length);
            for (int i = 0; i < lights.Length; i++)
            {
                lights[i].lightIndex = i;
            }
        }
    }

    [System.Serializable]
    public struct CustomLightData
    {
        public enum LightMode
        {
            Directional,
            Spot,
            Point,
        }

        public LightMode lightMode;
        public float radius;
        public float nearPlane, farPlane;
        public float horizontalSize, verticalSize;
        public float fov, aspectRatio;
        public float fallOffRange;
        public int addShadowID;

        public CustomLightData(LightMode lightMode, float radius, float nearPlane, float farPlane, float horizontalSize, float verticalSize, float fov, float aspectRatio, float fallOffRange, int addShadowID)
        {
            this.lightMode = lightMode;
            this.radius = radius;
            this.nearPlane = nearPlane;
            this.farPlane = farPlane;
            this.horizontalSize = horizontalSize;
            this.verticalSize = verticalSize;
            this.fov = fov;
            this.aspectRatio = aspectRatio;
            this.fallOffRange = fallOffRange;
            this.addShadowID = addShadowID;
        }
    }
}