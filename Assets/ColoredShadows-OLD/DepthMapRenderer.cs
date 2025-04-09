using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

public class DepthMapRenderer : MonoBehaviour
{
    [Header("Camera Setup")]
    [SerializeField] private Camera depthCamera;
    [SerializeField] private RenderTexture depthRenderTexture;
    [SerializeField] private RenderTexture colorRenderTexture;
    [SerializeField] private Material depthVisualizationMaterial;
    [SerializeField] private bool showDebugTexture = true;
    [SerializeField] private Material shadowMaterial;
    [SerializeField, Range(0.1f, 100f)] private float farClipPlane = 50f;

    [Header("Render Settings")]
    public Color shaderColor = Color.blue;
    [SerializeField] private int textureResolution = 1024;
    [SerializeField] private FilterMode filterMode = FilterMode.Bilinear;
    [SerializeField] private LayerMask depthLayers = -1; // Default to everything

    private Shader depthShader;
    private Matrix4x4 lightSpaceMatrix;
    public Shader replacementShader;

    private void Awake()
    {
        // If camera not assigned, create one
        if (depthCamera == null)
        {
            GameObject cameraObj = new GameObject("DepthCamera");
            cameraObj.transform.SetParent(transform);
            cameraObj.transform.localPosition = Vector3.zero;
            cameraObj.transform.localRotation = Quaternion.identity;
            depthCamera = cameraObj.AddComponent<Camera>();
        }

        // Find the depth shader if not assigned
        if (depthShader == null)
        {
            depthShader = Shader.Find("Hidden/DepthOnly");
        }

        // Initialize the depth camera
        SetupDepthCamera();

        // Create render textures
        CreateRenderTextures();

        // Create visualization material if needed
        if (depthVisualizationMaterial == null && showDebugTexture)
        {
            depthVisualizationMaterial = new Material(Shader.Find("Hidden/DepthToGreyscale"));
        }
        
        if (replacementShader != null)
        {
            // Set the replacement shader
            depthCamera.SetReplacementShader(replacementShader, "RenderType");
            
            // Pass color to the shader
            Shader.SetGlobalColor("_Color", shaderColor);
        }
    }

    private void SetupDepthCamera()
    {
        depthCamera.clearFlags = CameraClearFlags.SolidColor;
        depthCamera.backgroundColor = Color.black;
        depthCamera.cullingMask = depthLayers;
        depthCamera.farClipPlane = farClipPlane;
        depthCamera.depth = -1; // Make sure it renders before main camera
        depthCamera.allowHDR = false;
        depthCamera.allowMSAA = false;
        depthCamera.enabled = false; // We'll render manually
    }

    private void CreateRenderTextures()
    {
        // Create depth render texture
        if (depthRenderTexture == null || depthRenderTexture.width != textureResolution)
        {
            if (depthRenderTexture != null)
                depthRenderTexture.Release();

            depthRenderTexture = new RenderTexture(textureResolution, textureResolution, 24, RenderTextureFormat.Depth);
            depthRenderTexture.filterMode = filterMode;
            depthRenderTexture.wrapMode = TextureWrapMode.Clamp;
            depthRenderTexture.Create();
        }

        // Create color render texture (for debugging or additional data)
        if (colorRenderTexture == null || colorRenderTexture.width != textureResolution)
        {
            if (colorRenderTexture != null)
                colorRenderTexture.Release();

            colorRenderTexture = new RenderTexture(textureResolution, textureResolution, 0, RenderTextureFormat.ARGB32);
            colorRenderTexture.filterMode = filterMode;
            colorRenderTexture.wrapMode = TextureWrapMode.Clamp;
            colorRenderTexture.depthStencilFormat = GraphicsFormat.D32_SFloat;
            colorRenderTexture.Create();
        }
    }

    private void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += RenderDepthMap;
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= RenderDepthMap;
        depthCamera.ResetReplacementShader();
    }

    private void RenderDepthMap(ScriptableRenderContext context, Camera camera)
    {
        // Only render before main camera renders
        if (camera != Camera.main)
            return;
        
        lightSpaceMatrix = camera.projectionMatrix * LookAtUnity(camera.transform.position, camera.transform.position + camera.transform.forward, Vector3.up);
        shadowMaterial.SetMatrix("_LightSpaceMatrix", lightSpaceMatrix);

        // Make sure camera is set up properly
        depthCamera.targetTexture = depthRenderTexture;
        shadowMaterial.SetTexture("_ShadowMap", depthRenderTexture);
        depthCamera.RenderWithShader(replacementShader, "Depth");

        // Optional: also render a color version
        depthCamera.targetTexture = colorRenderTexture;
        depthCamera.clearFlags = CameraClearFlags.SolidColor;
        depthCamera.backgroundColor = Color.black;
        depthCamera.RenderWithShader(replacementShader, "RenderType");

        // Reset
        depthCamera.targetTexture = null;
    }
    
    public static Matrix4x4 LookAtUnity(Vector3 eye, Vector3 center, Vector3 up)
    {
        // This directly creates a matrix that will work with Unity's coordinate system
        Vector3 forward = (center - eye).normalized;
        Vector3 right = Vector3.Cross(up, forward).normalized;
        Vector3 upDirection = Vector3.Cross(forward, right);

        Matrix4x4 worldToCamera = new Matrix4x4();
        
        worldToCamera.m00 = right.x;
        worldToCamera.m01 = upDirection.x;
        worldToCamera.m02 = forward.x;
        worldToCamera.m03 = eye.x;
        
        worldToCamera.m10 = right.y;
        worldToCamera.m11 = upDirection.y;
        worldToCamera.m12 = forward.y;
        worldToCamera.m13 = eye.y;
        
        worldToCamera.m20 = right.z;
        worldToCamera.m21 = upDirection.z;
        worldToCamera.m22 = forward.z;
        worldToCamera.m23 = eye.z;
        
        worldToCamera.m30 = 0;
        worldToCamera.m31 = 0;
        worldToCamera.m32 = 0;
        worldToCamera.m33 = 1;

        return worldToCamera.inverse;
    }

    // For visualization in the inspector
    private void OnGUI()
    {
        if (showDebugTexture && depthRenderTexture != null)
        {
            GUI.DrawTexture(new Rect(10, 10, 256, 256), depthRenderTexture, ScaleMode.ScaleToFit, false);
            
            if (colorRenderTexture != null)
            {
                GUI.DrawTexture(new Rect(276, 10, 256, 256), colorRenderTexture, ScaleMode.ScaleToFit, false);
            }
        }
    }

    // Public accessors
    public RenderTexture DepthTexture => depthRenderTexture;
    public RenderTexture ColorTexture => colorRenderTexture;
    public Camera DepthCamera => depthCamera;

    // Update camera position and rotation
    public void UpdateCameraTransform(Vector3 position, Quaternion rotation)
    {
        depthCamera.transform.position = position;
        depthCamera.transform.rotation = rotation;
    }
}