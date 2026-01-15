using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;

namespace ColoredShadow.Core.Scripts
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
        [SerializeField] private float farPlane = 50;
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
        [SerializeField] public bool blockPassthroughShadows = true;

        public int shadowAtlasPosX;
        public int shadowAtlasPosY;
        public float nearPlane = 0.1f;
        private GraphicsBuffer vfxAppendBuffer;
        public GraphicsBuffer VFXAppendBuffer
        {
            get
            {
                if (vfxAppendBuffer == null)
                {
                    vfxAppendBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Append, vfxSamplingSize * vfxSamplingSize, sizeof(float) * 9);
                }
                return vfxAppendBuffer;
            }
        }
        public GraphicsBuffer vfxAppendCountBuffer;
        public GraphicsBuffer VFXAppendCountBuffer
        {
            get
            {
                if (vfxAppendCountBuffer == null)
                {
                    vfxAppendCountBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, 1, sizeof(uint));
                }
                return vfxAppendCountBuffer;
            }
        }
        private uint VFXAppendCount
        {
            get
            {
                uint[] count = new uint[1];
                VFXAppendCountBuffer.GetData(count);
                return count[0];
            }
        }

        public float FarPlane => lightMode == LightMode.Point ? radius : farPlane;
        public int TextureWidth => lightMode == LightMode.Point ? shadowTextureSize * 3 : shadowTextureSize;
        public int TextureHeight => lightMode == LightMode.Point ? shadowTextureSize * 2 : shadowTextureSize;
        public int VFXSamplingSizeX => lightMode == LightMode.Point ? vfxSamplingSize * 3 : vfxSamplingSize;
        public int VFXSamplingSizeY => lightMode == LightMode.Point ? vfxSamplingSize * 2 : vfxSamplingSize;
        public int TextureSurfaceArea => lightMode == LightMode.Point ? shadowTextureSize * 6 * shadowTextureSize : shadowTextureSize * shadowTextureSize;
        public Matrix4x4 ProjectionMatrix
        {
            get
            {
                switch (lightMode)
                {
                    case LightMode.Directional:
                        return Matrix4x4.Ortho(-size, size, -size, size, nearPlane, farPlane);
                    case LightMode.Spot:
                        return Matrix4x4.Perspective(fov, aspectRatio, nearPlane, farPlane);
                    case LightMode.Point:
                        return Matrix4x4.Perspective(90, 1, nearPlane, radius);
                    default:
                        Debug.LogError($"Couldnt match lightmode. Did you add an extra lightmode?");
                        return Matrix4x4.zero;
                }
            }
        }
        
        public Matrix4x4 ViewMatrix
        {
            get
            {
                Matrix4x4 rotationMatrix = Matrix4x4.Rotate(Quaternion.Inverse(transform.rotation));
                Matrix4x4 translationMatrix = Matrix4x4.Translate(-transform.position);
                Matrix4x4 viewMatrix;
                if (lightMode == LightMode.Point)
                {
                    viewMatrix = rotationMatrix * translationMatrix;
                }
                else
                {
                    viewMatrix = rotationMatrix * translationMatrix;
                }
                viewMatrix.m20 *= -1;
                viewMatrix.m21 *= -1;
                viewMatrix.m22 *= -1;
                viewMatrix.m23 *= -1;
                return viewMatrix;
            }
        }

        private int previousShadowTextureSize;
        private LightMode cachedLightMode;
        private Material cachedDebugMat;
        
        private Mesh[] numberMeshes;
        private MeshRenderer[] meshRenderers;
        private List<(Matrix4x4, Matrix4x4)> cullingMatrices;
        
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (SceneView.lastActiveSceneView == null || !ColShadowSettings.IsEnabled)
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
                Gizmos.color = ColShadowSettings.CasterNumberColor;
                Quaternion lookRotation = Quaternion.LookRotation(dir) * Quaternion.Euler(-90, -90, 90);
                StringToMeshNumber(shadowID.ToString("0.#"), middlePos, lookRotation, ColShadowSettings.CasterNumberSize);
            }
            
            foreach (CustomLight customLight in customLights)
            {
                Vector3 dir = sceneCamera.transform.position - customLight.transform.position;
                Gizmos.color = ColShadowSettings.CustomLightNumberColor;
                Quaternion lookRotation = Quaternion.LookRotation(dir) * Quaternion.Euler(-90, -90, 90);
                Vector3 middlePos = customLight.transform.position + dir.normalized * 0f;
                middlePos.y += 0.4f + ColShadowSettings.CustomLightNumberSize * 0.15f;
                StringToMeshNumber(customLight.addToShadowID.ToString("0.#"), middlePos, lookRotation, ColShadowSettings.CustomLightNumberSize);
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
            Texture shadowMap = Shader.GetGlobalTexture("_ColoredShadowMap0");
            if(shadowMap == null || cachedDebugMat == null)
                return;
            
            Vector2Int shadowAtlasSize = CustomLightManager.GetShadowAtlasSize();
            cachedDebugMat.SetFloat("_ShadowAtlasUVX", (float)shadowAtlasPosX / shadowAtlasSize.x);
            cachedDebugMat.SetFloat("_ShadowAtlasUVY", (float)shadowAtlasPosY / shadowAtlasSize.y);
            cachedDebugMat.SetFloat("_ShadowMapSizeX", (float)TextureWidth / shadowAtlasSize.x);
            cachedDebugMat.SetFloat("_ShadowMapSizeY", (float)TextureHeight / shadowAtlasSize.y);
            
            if (lightMode != LightMode.Point)
            {
                Rect rect = new Rect(sceneView.cameraViewport.width - textureSize, sceneView.cameraViewport.height - textureSize, textureSize, textureSize);
                EditorGUI.DrawPreviewTexture(rect, shadowMap, cachedDebugMat);
            }
            else
            {
                Rect rect = new Rect(sceneView.cameraViewport.width - textureSize * 1.5f, sceneView.cameraViewport.height - textureSize, Mathf.Ceil(textureSize * 1.5f), Mathf.Ceil(textureSize));
                EditorGUI.DrawPreviewTexture(rect, shadowMap, cachedDebugMat);
            }

            Handles.EndGUI();
        }

        private void OnValidate()
        {
            if (previousShadowTextureSize == shadowTextureSize && cachedLightMode == lightMode)
                return;
            
            previousShadowTextureSize = shadowTextureSize;
            cachedLightMode = lightMode;
            CustomLightManager.RefreshShadowAtlas();

            if (enableVFXSupport)
            {
                Debug.Log("Created vfx append buffer");
                CreateBuffers();
            }
            else
            {
                Debug.Log("Released vfx append buffer");
                ReleaseBuffers();
            }
        }
#endif
        private void CreateBuffers()
        {
            vfxAppendBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Append, vfxSamplingSize * vfxSamplingSize, sizeof(float) * 9);
            vfxAppendCountBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, 1, sizeof(int));
        }

        private void ReleaseBuffers()
        {
            vfxAppendBuffer?.Release();
            vfxAppendBuffer = null;
            vfxAppendCountBuffer?.Release();
            vfxAppendCountBuffer = null;
        }

        public Matrix4x4 GetViewMatrixWithRot(Vector3 rot)
        {
            Matrix4x4 rotationMatrix = Matrix4x4.Rotate(Quaternion.Inverse(Quaternion.Euler(rot)));
            Matrix4x4 translationMatrix = Matrix4x4.Translate(-transform.position);
            Matrix4x4 viewMatrix = rotationMatrix * translationMatrix;
            viewMatrix.m20 *= -1;
            viewMatrix.m21 *= -1;
            viewMatrix.m22 *= -1;
            viewMatrix.m23 *= -1;
            return viewMatrix;
        }

        private void Update()
        {
            if (enableVFXSupport)
            {
                foreach (VisualEffect visualEffect in visualEffects)
                {
                    // Debug.Log($"{!visualEffect.HasGraphicsBuffer("_ShadowPositions")} {!visualEffect.HasInt("_ShadowPositionsCount")}");
                    if (!visualEffect.HasGraphicsBuffer("ShadowData") || !visualEffect.HasInt("AmountShadowData") || vfxAppendBuffer == null)
                    {
                        Debug.LogWarning($"{visualEffect.gameObject.name} doesn't have a ShadowData graphics buffer and/or doesn't have an AmountShadowData int in the VFX graph");
                        continue;
                    } 
                    
                    visualEffect.SetGraphicsBuffer("ShadowData", vfxAppendBuffer);
                    uint vfxAppendCount = VFXAppendCount;
                    visualEffect.SetInt("AmountShadowData", (int)vfxAppendCount);
                    // Debug.Log(vfxAppendCount);
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
            cullingMatrices = new List<(Matrix4x4, Matrix4x4)>(6);
            if (overrideShader == null)
            {
                overrideShader = Shader.Find("ColoredShadow/OverrideColShadow_UV_UVSize");
            }
            CustomLightManager.AddCustomLight(this);

            
#if UNITY_EDITOR
            cachedDebugMat = Resources.Load<Material>("Custom_test");
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
            vfxAppendBuffer?.Release();
            vfxAppendBuffer = null;
        }
        
        private static void UpdateLightIndices()
        {
            CustomLight[] lights = FindObjectsByType<CustomLight>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.InstanceID
            );
            Shader.SetGlobalInt("CurrentAmountCustomLights", lights.Length);
            for (int i = 0; i < lights.Length; i++)
            {
                lights[i].lightIndex = i;
            }
        }

        public Vector2Int GetLocalShadowAtlasPos(int index = 0)
        {
            switch (lightMode)
            {
                case LightMode.Directional:
                    return new Vector2Int(shadowAtlasPosX, shadowAtlasPosY);
                case LightMode.Spot:
                    return new Vector2Int(shadowAtlasPosX, shadowAtlasPosY);
                case LightMode.Point:
                    return new Vector2Int(shadowAtlasPosX + shadowTextureSize * (index % 3), shadowAtlasPosY + shadowTextureSize * (index / 3));
                default:
                    Debug.LogError($"Couldnt match lightmode. Did you add an extra lightmode?");
                    return Vector2Int.one * -1;
            }
        }

        private Matrix4x4 cachedMatrix;
        public List<(Matrix4x4, Matrix4x4)> GetCullingMatrices()
        {
            Matrix4x4 viewMatrix = ViewMatrix;
            
            Matrix4x4 projectionMatrix = ProjectionMatrix;
            cullingMatrices.Clear();
            
            switch (lightMode)
            {
                case LightMode.Point:
                    Matrix4x4 newMatrix = Matrix4x4.identity;
                    newMatrix.m00 = 1;
                    newMatrix.m11 = 1;
                    newMatrix.m22 = -1;
                    newMatrix.m33 = 1;
                    newMatrix.m03 = -transform.position.x;
                    newMatrix.m13 = -transform.position.y;
                    newMatrix.m23 = transform.position.z;
                    
                    cullingMatrices.Add((projectionMatrix, newMatrix));
                    cullingMatrices.Add((projectionMatrix, Matrix4x4.Rotate(Quaternion.Euler(0, 90, 0)) * newMatrix));
                    cullingMatrices.Add((projectionMatrix, Matrix4x4.Rotate(Quaternion.Euler(0, 180, 0)) * newMatrix));
                    cullingMatrices.Add((projectionMatrix, Matrix4x4.Rotate(Quaternion.Euler(0, 270, 0)) * newMatrix));
                    cullingMatrices.Add((projectionMatrix, Matrix4x4.Rotate(Quaternion.Euler(90, 0, 0)) * newMatrix));
                    cullingMatrices.Add((projectionMatrix, Matrix4x4.Rotate(Quaternion.Euler(270, 0, 0)) * newMatrix));
                    break;
                case LightMode.Spot:
                    cullingMatrices.Add((projectionMatrix, viewMatrix));
                    break;
                case LightMode.Directional:
                    cullingMatrices.Add((projectionMatrix, viewMatrix));
                    break;
                default:
                    Debug.LogError($"Couldnt match lightmode. Did you add an extra lightmode?");
                    break;
            }
            return cullingMatrices;
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