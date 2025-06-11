using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ColoredShadows.Scripts
{
    public enum LightMode
    {
        Directional,
        Spot,
        Point,
    }
    
    [SelectionBase, ExecuteInEditMode]
    public class CustomLight : MonoBehaviour
    {
        [SerializeField] public int lightIndex = 0;
        [SerializeField] public LightMode lightMode;
        [SerializeField] public float radius = 10;
        [SerializeField] public float farPlane = 50;
        [SerializeField] public float size = 10;
        [SerializeField] public float fov = 60;
        [SerializeField] public float aspectRatio = 1;
        [SerializeField] public float fallOffRange = 50;
        [SerializeField] public int addShadowID;
        [SerializeField] public int shadowTextureSize = 1024;
        [SerializeField] public Shader overrideShader;
        [SerializeField] public LayerMask shadowCastingMask = int.MaxValue;
        [SerializeField] public LayerMask shadowReceivingMask = int.MaxValue;
        [SerializeField] public List<float> customValues;
        

        public float nearPlane = 1;
        public int amountPixelsToSkipPerSample = 8;

        public GraphicsBuffer vfxAppendBuffer;
        public int vfxAppendCount;

        private void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Color farPlaneFillColor = new Color(1, 1, 1, 0.1f);
            Color farPlaneOutlineColor = new Color(1, 1, 1, 0.8f);
            
            Color fallOffFillColor = new Color(1, 0, 0, 0.05f);
            Color fallOffOutlineColor = new Color(1, 0, 0, 0.8f);

            switch (lightMode)
            {
                case LightMode.Point :
                    Gizmos.matrix = Matrix4x4.Translate(transform.position); 
                    Gizmos.color = farPlaneFillColor;
                    Gizmos.DrawCube(Vector3.zero, Vector3.one * radius * 2);
                    Gizmos.color = farPlaneOutlineColor;
                    Gizmos.DrawWireCube(Vector3.zero, Vector3.one * radius * 2);
                    
                    Gizmos.color = fallOffFillColor;
                    Gizmos.DrawSphere(Vector3.zero, fallOffRange);
                    Gizmos.color = fallOffOutlineColor;
                    Gizmos.DrawWireSphere(Vector3.zero, fallOffRange);
                    break;
                case LightMode.Directional :
                    Gizmos.color = farPlaneFillColor;
                    Gizmos.DrawCube(Vector3.forward * farPlane / 2, new Vector3(size * 2, size * 2, farPlane));
                    Gizmos.color = farPlaneOutlineColor;
                    Gizmos.DrawWireCube(Vector3.forward * farPlane / 2, new Vector3(size * 2, size * 2, farPlane));
                    break;
                case LightMode.Spot :
                    Gizmos.color = farPlaneOutlineColor;
                    Gizmos.DrawFrustum(Vector3.zero, fov, farPlane, 0.1f, aspectRatio);
                    Gizmos.color = fallOffOutlineColor;
                    Gizmos.DrawFrustum(Vector3.zero, fov, fallOffRange, 0.1f, aspectRatio);
                    break;
            }
        }

        private void SceneViewGUI(SceneView sceneView)
        {
            if(!Selection.Contains(gameObject))
                return;
            
            Handles.BeginGUI();

            // Define position for the RenderTexture (bottom-left corner)
            float crossSection = Vector2.Distance(Vector2.zero, new Vector2(sceneView.cameraViewport.width, sceneView.cameraViewport.height));
            int textureSize = (int)(crossSection / 10.0f);
            Texture shadowMap = Shader.GetGlobalTexture("_ColoredShadowMap" + lightIndex);
            if(shadowMap == null)
                return;
            
            if (lightMode != LightMode.Point)
            {
                Rect rect = new Rect(sceneView.cameraViewport.width - textureSize, sceneView.cameraViewport.height - textureSize, textureSize, textureSize);
                EditorGUI.DrawRect(rect, Color.black);
                GUI.DrawTexture(rect, shadowMap);
            }
            else
            {
                Rect rect = new Rect(sceneView.cameraViewport.width - textureSize * 1.5f, sceneView.cameraViewport.height - textureSize, Mathf.Ceil(textureSize * 1.5f), Mathf.Ceil(textureSize / 2.0f));
                Rect rect2 = new Rect(sceneView.cameraViewport.width - textureSize * 1.5f, sceneView.cameraViewport.height - textureSize / 2, Mathf.Ceil(textureSize * 1.5f), Mathf.Ceil(textureSize / 2.0f));
                GUI.DrawTextureWithTexCoords(rect2, shadowMap, new Rect(0, 0, 0.5f, 1), false);
                GUI.DrawTextureWithTexCoords(rect, shadowMap, new Rect(0.5f, 0, 0.5f, 1), false);
            }

            Handles.EndGUI();
        }

        private void Reset()
        {
            overrideShader = Shader.Find("ColoredShadow/OverrideColShadow_UV_UVSize");
        }

        private void OnEnable()
        {
            if (overrideShader == null)
            {
                overrideShader = Shader.Find("ColoredShadow/OverrideColShadow_UV_UVSize");
            }

            SceneView.duringSceneGui += SceneViewGUI;
            
            UpdateLightIndices();
        }
        private void OnDisable()
        {
            SceneView.duringSceneGui -= SceneViewGUI;
            UpdateLightIndices();
        }
        
        private static void UpdateLightIndices()
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