using EditorAttributes;
using UnityEngine;

namespace ColoredShadows.Scripts
{
    public class CustomLight : MonoBehaviour
    {
        [ReadOnly] public int lightIndex = -1;
        public CustomLightData lightData = new CustomLightData(CustomLightData.LightMode.Directional, 10, 0.1f, 50, 20, 20, 30, 1);
        public Vector2Int shadowTextureSize = new Vector2Int(1024, 1024);

        private void OnValidate()
        {
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

        public CustomLightData(LightMode lightMode, float radius, float nearPlane, float farPlane, float horizontalSize, float verticalSize, float fov, float aspectRatio)
        {
            this.lightMode = lightMode;
            this.radius = radius;
            this.nearPlane = nearPlane;
            this.farPlane = farPlane;
            this.horizontalSize = horizontalSize;
            this.verticalSize = verticalSize;
            this.fov = fov;
            this.aspectRatio = aspectRatio;
        }
    }
}