using System;
using System.Collections.Generic;
using System.Reflection;
using ColoredShadow.Core.Scripts;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;
using UnityEngine.VFX;

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
        [SerializeField] public int addToShadowID;
        [SerializeField] public int shadowTextureSize = 1024;
        [SerializeField] public Shader overrideShader;
        [SerializeField] public LayerMask shadowCastingMask = int.MaxValue;
        [SerializeField] public LayerMask shadowReceivingMask = int.MaxValue;
        [SerializeField] public List<float> customValues;
        [SerializeField] public bool enableVFXSupport;
        [SerializeField] public List<VisualEffect> visualEffects;
        [SerializeField] public int vfxSamplingSize = 256;
        [SerializeField] public float vfxUVSize = 1;
        [SerializeField] public bool relativeUVSize = true;

        public int shadowAtlasPosX;
        public int shadowAtlasPosY;
        public float nearPlane = 0.1f;
        public GraphicsBuffer vfxAppendBuffer;
        public int vfxAppendCount;
        
        public int TextureWidth => lightMode == LightMode.Point ? shadowTextureSize * 3 : shadowTextureSize;
        public int TextureHeight => lightMode == LightMode.Point ? shadowTextureSize * 2 : shadowTextureSize;
        public int TextureSurfaceArea => lightMode == LightMode.Point ? shadowTextureSize * 6 * shadowTextureSize : shadowTextureSize * shadowTextureSize;
        public Matrix4x4 ProjectionMatrix => lightMode == LightMode.Point ? Matrix4x4.Ortho(-size, size, -size, size, nearPlane, farPlane) : Matrix4x4.Perspective(fov, aspectRatio, nearPlane, farPlane);
        public Matrix4x4 ViewMatrix => GetViewMatrix();

        private int previousShadowTextureSize;
        
        private Mesh[] numberMeshes;
        private MeshRenderer[] meshRenderers;
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (SceneView.lastActiveSceneView == null || !ColShadowDebug.IsEnabled)
            {
                return;
            }
            Camera sceneCamera = SceneView.lastActiveSceneView.camera;
            if (sceneCamera == null)
            {
                return;
            }
            
            CustomLight[] customLights = FindObjectsByType<CustomLight>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (Time.frameCount % 60 == 0)
            {
                meshRenderers = FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            }
            if (meshRenderers == null)
            {
                return;
            }
            foreach (MeshRenderer meshRenderer in meshRenderers)
            {
                if(meshRenderer == null)
                        continue;
                if (!meshRenderer.sharedMaterial.HasProperty("_ShadowID"))
                    continue;

                Vector3 middlePos = meshRenderer.transform.position;
                Vector3 dir = sceneCamera.transform.position - middlePos;
                middlePos = GetRayBoxIntersection(middlePos, dir, middlePos, meshRenderer.bounds.size);
                float shadowID = meshRenderer.sharedMaterial.GetFloat("_ShadowID");
                Gizmos.color = ColShadowDebug.CasterNumberColor;
                Quaternion lookRotation = Quaternion.LookRotation(dir) * Quaternion.Euler(-90, -90, 90);
                StringToMeshNumber(shadowID.ToString("0.#"), middlePos, lookRotation, ColShadowDebug.CasterNumberSize);
            }
            
            foreach (CustomLight customLight in customLights)
            {
                Vector3 dir = sceneCamera.transform.position - customLight.transform.position;
                Gizmos.color = ColShadowDebug.CustomLightNumberColor;
                Quaternion lookRotation = Quaternion.LookRotation(dir) * Quaternion.Euler(-90, -90, 90);
                Vector3 middlePos = customLight.transform.position + dir.normalized * 0f;
                middlePos.y += 0.4f + ColShadowDebug.CustomLightNumberSize * 0.15f;
                StringToMeshNumber(customLight.addToShadowID.ToString("0.#"), middlePos, lookRotation, ColShadowDebug.CustomLightNumberSize);
            }
        }

        private void StringToMeshNumber(string str, Vector3 middlePos, Quaternion lookRotation, float charSize)
        {
            char[] characters = str.ToCharArray();
            float charSpace = 0.13f * charSize;
            float meshesWidth = charSpace * (characters.Length - 1);
            if(str.Contains('.'))
                meshesWidth -= charSpace * 0.9f;
            middlePos -= lookRotation * Vector3.left * meshesWidth * 0.5f;
            foreach (char character in characters)
            {
                switch (character)
                {
                    case '0':
                        Gizmos.DrawMesh(numberMeshes[0], middlePos, lookRotation, Vector3.one * charSize);
                        middlePos += lookRotation * Vector3.left * charSpace;
                        break;
                    case '1':
                        Gizmos.DrawMesh(numberMeshes[1], middlePos, lookRotation, Vector3.one * charSize);
                        middlePos += lookRotation * Vector3.left * charSpace;
                        break;
                    case '2':
                        Gizmos.DrawMesh(numberMeshes[2], middlePos, lookRotation, Vector3.one * charSize);
                        middlePos += lookRotation * Vector3.left * charSpace;
                        break;
                    case '3':
                        Gizmos.DrawMesh(numberMeshes[3], middlePos, lookRotation, Vector3.one * charSize);
                        middlePos += lookRotation * Vector3.left * charSpace;
                        break;
                    case '4':
                        Gizmos.DrawMesh(numberMeshes[4], middlePos, lookRotation, Vector3.one * charSize);
                        middlePos += lookRotation * Vector3.left * charSpace;
                        break;
                    case '5':
                        Gizmos.DrawMesh(numberMeshes[5], middlePos, lookRotation, Vector3.one * charSize);
                        middlePos += lookRotation * Vector3.left * charSpace;
                        break;
                    case '6':
                        Gizmos.DrawMesh(numberMeshes[6], middlePos, lookRotation, Vector3.one * charSize);
                        middlePos += lookRotation * Vector3.left * charSpace;
                        break;
                    case '7':
                        Gizmos.DrawMesh(numberMeshes[7], middlePos, lookRotation, Vector3.one * charSize);
                        middlePos += lookRotation * Vector3.left * charSpace;
                        break;
                    case '8':
                        Gizmos.DrawMesh(numberMeshes[8], middlePos, lookRotation, Vector3.one * charSize);
                        middlePos += lookRotation * Vector3.left * charSpace;
                        break;
                    case '9':
                        Gizmos.DrawMesh(numberMeshes[9], middlePos, lookRotation, Vector3.one * charSize);
                        middlePos += lookRotation * Vector3.left * charSpace;
                        break;
                    case '.':
                        middlePos -= lookRotation * Vector3.left * charSpace * 0.9f;
                        Gizmos.DrawMesh(numberMeshes[10], middlePos, lookRotation, Vector3.one * charSize);
                        middlePos += lookRotation * Vector3.left * charSpace;
                        break;
                    case '-':
                        Gizmos.DrawMesh(numberMeshes[11], middlePos, lookRotation, Vector3.one * charSize);
                        middlePos += lookRotation * Vector3.left * charSpace;
                        break;
                }
            }
        }
        
        public static Vector3 GetRayBoxIntersection(Vector3 rayOrigin, Vector3 rayDirection, Vector3 boxCenter, Vector3 boxSize)
        {
            Vector3 halfSize = boxSize * 0.5f;
            float tX = rayDirection.x > 0 ? halfSize.x / rayDirection.x : -halfSize.x / rayDirection.x;
            float tY = rayDirection.y > 0 ? halfSize.y / rayDirection.y : -halfSize.y / rayDirection.y;
            float tZ = rayDirection.z > 0 ? halfSize.z / rayDirection.z : -halfSize.z / rayDirection.z;
            float t = Mathf.Min(tX, Mathf.Min(tY, tZ));
            return rayOrigin + rayDirection * t;
        }

        private void SetNumberMeshes()
        {
            numberMeshes = new Mesh[12];
            for (int i = 0; i < 10; i++)
            {
                numberMeshes[i] = Resources.Load<Mesh>(i.ToString());
            }
            numberMeshes[10] = Resources.Load<Mesh>("dot");
            numberMeshes[11] = Resources.Load<Mesh>("minus");
        }

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

        private void OnValidate()
        {
            if (previousShadowTextureSize == shadowTextureSize)
                return;
            
            previousShadowTextureSize = shadowTextureSize;
            CustomLightManager.RefreshShadowAtlas();
        }
#endif

        private void Update()
        {
            if (enableVFXSupport)
            {
                foreach (VisualEffect visualEffect in visualEffects)
                {
                    // Debug.Log($"{!visualEffect.HasGraphicsBuffer("_ShadowPositions")} {!visualEffect.HasInt("_ShadowPositionsCount")}");
                    if (!visualEffect.HasGraphicsBuffer("_ShadowPositions") || !visualEffect.HasInt("_ShadowPositionsCount"))
                        continue;
                    
                    visualEffect.SetGraphicsBuffer("_ShadowPositions", vfxAppendBuffer);
                    visualEffect.SetInt("_ShadowPositionsCount", vfxAppendCount);
                }
            }
        }

        private void Reset()
        {
            Debug.Log($"Reset");
            overrideShader = Shader.Find("ColoredShadow/OverrideColShadow_UV_UVSize");
        }

        private void OnEnable()
        {
            Debug.Log($"Enabled");
            if (overrideShader == null)
            {
                overrideShader = Shader.Find("ColoredShadow/OverrideColShadow_UV_UVSize");
            }
            CustomLightManager.AddCustomLight(this);

            
#if UNITY_EDITOR
            SetNumberMeshes();
            SceneView.duringSceneGui += SceneViewGUI;
#endif
            UpdateLightIndices();
        }
        private void OnDisable()
        {
            Debug.Log($"Disabled");
            CustomLightManager.RemoveCustomLight(this);
#if UNITY_EDITOR
            SceneView.duringSceneGui -= SceneViewGUI;
#endif
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
        
        public Matrix4x4 GetViewMatrix()
        {
            Matrix4x4 rotationMatrix = Matrix4x4.Rotate(Quaternion.Inverse(transform.rotation));
            Matrix4x4 translationMatrix = Matrix4x4.Translate(-transform.position);
            Matrix4x4 viewMatrix = rotationMatrix * translationMatrix;
            viewMatrix.m20 *= -1;
            viewMatrix.m21 *= -1;
            viewMatrix.m22 *= -1;
            viewMatrix.m23 *= -1;
            return viewMatrix;
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