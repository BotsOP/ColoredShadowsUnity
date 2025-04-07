using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

// This pass creates an RTHandle and blits the camera color to it after rendering transparent objects.
// The RTHandle is then set as a global texture, which is available to shaders in the scene. The RTHandle is preserved in all frames while the renderer feature is running to create a recursive rendering effect.
public class BlitToRTHandlePass : ScriptableRenderPass
{
    private RTHandle m_InputHandle;
    private RTHandle m_OutputHandle;
    private Material shadowMaterial;
    private const string k_OutputName = "_CopyColorTexture";
    private static readonly int m_OutputId = Shader.PropertyToID(k_OutputName);
    private static readonly int LightSpaceMatrix = Shader.PropertyToID("_LightSpaceMatrix");

    public BlitToRTHandlePass(RenderPassEvent evt, Material shadowMaterial)
    {
        renderPassEvent = evt;
        this.shadowMaterial = shadowMaterial;
    }


    // Unity calls the RecordRenderGraph method to add and configure one or more render passes in the render graph system.
    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
        UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

        if (cameraData.camera.cameraType != CameraType.Game)
            return;

        // Create the custom RTHandle
        var desc = cameraData.cameraTargetDescriptor;
        desc.depthBufferBits = 0;
        desc.msaaSamples = 1;
        RenderingUtils.ReAllocateHandleIfNeeded(ref m_OutputHandle, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: k_OutputName );
        
        // Make the output texture available for the shaders in the scene.
        // In this sample the output texture is used recursively by the subsequent frames, so it must stay in memory while the renderer feature is running.
        // A TextureHandle object is discarded after each frame, that's why we cannot bind it as a global texture using the RenderGraph API (builder.SetGlobalTextureAfterPass).
        // Instead, we bind the RTHandle as a global texture using the shader API, because the RTHandle is not managed by the render graph system.
        Shader.SetGlobalTexture(m_OutputId, m_OutputHandle);

        // Set camera color as a texture resource for this render graph instance
        TextureHandle source = resourceData.cameraDepthTexture;

        // Set RTHandle as a texture resource for this render graph instance
        TextureHandle destination = renderGraph.ImportTexture(m_OutputHandle);
        
        if (!source.IsValid() || !destination.IsValid())
            return;
        
        float near_plane = 0.1f, far_plane = 7.5f;
        Matrix4x4 lightProjection = Matrix4x4.Ortho(-10.0f, 10.0f, -10.0f, 10.0f, near_plane, far_plane);
        Matrix4x4 lightView = LookAtLH(
            new Vector3(cameraData.camera.transform.position.x, cameraData.camera.transform.position.y, cameraData.camera.transform.position.z),  // Light position
            Vector3.zero,    // Look target (origin)
            Vector3.up     // Up vector
        );

        Matrix4x4 lightSpaceMatrix = lightProjection * lightView;
        // lightView = ShadowUtils.GetLightSpaceMatrix(cameraData.camera.transform.forward, cameraData.camera);
        cameraData.camera.transform.LookAt(Vector3.zero);
        // lightSpaceMatrix.m00 = -0.071f;
        // lightSpaceMatrix.m01 = 0f;
        // lightSpaceMatrix.m02 = -0.071f;
        // lightSpaceMatrix.m03 = 0f;
        //
        // lightSpaceMatrix.m10 = -0.067f;
        // lightSpaceMatrix.m11 = 0.033f;
        // lightSpaceMatrix.m12 = 0.067f;
        // lightSpaceMatrix.m13 = 0f;
        //
        // lightSpaceMatrix.m20 = -0.064f;
        // lightSpaceMatrix.m21 = -0.255f;
        // lightSpaceMatrix.m22 = 0.064f;
        // lightSpaceMatrix.m23 = 0.12f;
        //
        // lightSpaceMatrix.m30 = 0f;
        // lightSpaceMatrix.m31 = 0f;
        // lightSpaceMatrix.m32 = 0f;
        // lightSpaceMatrix.m33 = 1f;
        
        Shader.SetGlobalMatrix(LightSpaceMatrix, lightSpaceMatrix);
        Debug.Log($"lightSpaceMatrix: {lightSpaceMatrix}");
        Debug.Log($"lightProjection: {lightProjection}");
        Debug.Log($"lightView: {lightView}");
        
        // Blit the input texture to the destination texture
        Shader.SetGlobalMatrix("_InverseVP3", cameraData.camera.projectionMatrix.inverse);
        Shader.SetGlobalMatrix("_CameraWorld3", cameraData.camera.cameraToWorldMatrix);
        Shader.SetGlobalVector("_ViewDirection3", cameraData.camera.transform.forward);
        Shader.SetGlobalVector("_ViewPos3", cameraData.camera.transform.position);
        
        RenderGraphUtils.BlitMaterialParameters para = new(source, destination, shadowMaterial, 0);
        renderGraph.AddBlitPass(para, "BlitToRTHandle_CopyColor");
        
        if(Input.GetKey(KeyCode.F))
        {
            Debug.Log($"F");
            resourceData.cameraColor = destination;
        }
    }
    
    public static Matrix4x4 LookAtLH(Vector3 eye, Vector3 center, Vector3 up)
    {
        Vector3 f = (center - eye).normalized;        // Forward (Z+)
        Vector3 s = Vector3.Cross(up, f).normalized;  // Right (X+)
        Vector3 u = Vector3.Cross(f, s);              // Up (Y+)

        Matrix4x4 result = Matrix4x4.identity;

        result[0, 0] = s.x;
        result[0, 1] = s.y;
        result[0, 2] = s.z;

        result[1, 0] = u.x;
        result[1, 1] = u.y;
        result[1, 2] = u.z;

        result[2, 0] = f.x;
        result[2, 1] = f.y;
        result[2, 2] = f.z;

        result[3, 0] = -Vector3.Dot(s, eye);
        result[3, 1] = -Vector3.Dot(u, eye);
        result[2, 3] = -Vector3.Dot(f, eye);

        return result;
    }
    
    public static Matrix4x4 LookAtRH(Vector3 eye, Vector3 center, Vector3 up)
    {
        Vector3 f = (center - eye).normalized;        // Forward (Z+)
        Vector3 s = Vector3.Cross(f, up).normalized;  // Right (X+)
        Vector3 u = Vector3.Cross(s, f);              // Up (Y+)

        Matrix4x4 result = Matrix4x4.identity;

        result[0, 0] = s.x;
        result[1, 0] = s.y;
        result[2, 0] = s.z;

        result[0, 1] = u.x;
        result[1, 1] = u.y;
        result[2, 1] = u.z;

        result[0, 2] = -f.x;
        result[1, 2] = -f.y;
        result[2, 2] = -f.z;

        result[0, 3] = -Vector3.Dot(s, eye);
        result[1, 3] = -Vector3.Dot(u, eye);
        result[2, 3] = Vector3.Dot(f, eye);

        return result;
    }
    
    public void Dispose()
    {
        m_InputHandle?.Release();
        m_OutputHandle?.Release();
    }
}

public static class ShadowUtils
{
    /// <summary>
    /// Creates a light space matrix based on a directional light for shadow mapping
    /// </summary>
    /// <param name="lightDirection">The direction of the light (from light to scene)</param>
    /// <param name="camera">The camera component that defines the view frustum</param>
    /// <param name="shadowDistance">Maximum distance for shadows to be rendered</param>
    /// <returns>A matrix that transforms from world space to light space</returns>
    public static Matrix4x4 GetLightSpaceMatrix(Vector3 lightDirection, Camera camera, float shadowDistance = 100f)
    {
        // Normalize the light direction
        lightDirection.Normalize();
        
        // Calculate camera view center - this is what we'll target
        Vector3 cameraViewCenter = camera.transform.position + camera.transform.forward * (shadowDistance * 0.5f);
        
        // Calculate light position that moves with the camera
        Vector3 lightPosition = cameraViewCenter - lightDirection * shadowDistance;
        
        // Find a suitable up vector (avoid issues when light is perfectly aligned with world up)
        Vector3 upVector = Vector3.up;
        if (Mathf.Abs(Vector3.Dot(lightDirection, upVector)) > 0.95f)
        {
            upVector = Vector3.forward;
        }
        
        // Create the light's view matrix (light looking at the center of camera's view)
        Matrix4x4 lightView = BlitToRTHandlePass.LookAtRH(
            lightPosition, 
            cameraViewCenter, 
            upVector
        );
        
        // Calculate bounds for the orthographic projection based on camera frustum
        CalculateLightSpaceBounds(camera, lightView, out float minX, out float maxX, 
            out float minY, out float maxY, out float minZ, out float maxZ, shadowDistance);
        
        // Create orthographic projection matrix for the light
        Matrix4x4 lightProjection = Matrix4x4.Ortho(minX, maxX, minY, maxY, minZ, maxZ);
        
        // The light space matrix combines both view and projection
        return lightProjection * lightView;
    }
    
    /// <summary>
    /// Calculates the bounds for the light's orthographic projection based on the camera frustum
    /// </summary>
    private static void CalculateLightSpaceBounds(Camera camera, Matrix4x4 lightView, 
        out float minX, out float maxX, out float minY, out float maxY, 
        out float minZ, out float maxZ, float shadowDistance)
    {
        // Get frustum corners in world space
        Vector3[] frustumCorners = GetFrustumCorners(camera, shadowDistance);
        
        // Initialize bounds
        minX = float.MaxValue;
        maxX = float.MinValue;
        minY = float.MaxValue;
        maxY = float.MinValue;
        minZ = float.MaxValue;
        maxZ = float.MinValue;
        
        // Transform frustum corners to light space and find bounds
        for (int i = 0; i < frustumCorners.Length; i++)
        {
            // Convert corner to light space
            Vector3 corner = lightView.MultiplyPoint(frustumCorners[i]);
            
            // Update bounds
            minX = Mathf.Min(minX, corner.x);
            maxX = Mathf.Max(maxX, corner.x);
            minY = Mathf.Min(minY, corner.y);
            maxY = Mathf.Max(maxY, corner.y);
            minZ = Mathf.Min(minZ, corner.z);
            maxZ = Mathf.Max(maxZ, corner.z);
        }
        
        // Add padding to avoid edge artifacts
        float padding = 15f;
        minX -= padding;
        maxX += padding;
        minY -= padding;
        maxY += padding;
        
        // Ensure minimum depth is positive
        minZ = Mathf.Max(1f, minZ);
        
        // Optional: Stabilize the shadow by snapping to texel grid
        // This helps reduce shadow flickering when camera moves
        float texelSize = (maxX - minX) / 1024f; // Assuming 1024 shadow map resolution
        float offsetX = minX % texelSize;
        float offsetY = minY % texelSize;
        
        minX -= offsetX;
        maxX -= offsetX;
        minY -= offsetY;
        maxY -= offsetY;
    }
    
    /// <summary>
    /// Gets the 8 corners of the camera's view frustum
    /// </summary>
    private static Vector3[] GetFrustumCorners(Camera camera, float shadowDistance)
    {
        Vector3[] corners = new Vector3[8];
        
        // Use camera's view frustum to get corners directly
        float near = camera.nearClipPlane;
        float far = Mathf.Min(camera.farClipPlane, shadowDistance);
        
        // Get frustum corners in view space
        camera.CalculateFrustumCorners(
            new Rect(0, 0, 1, 1), 
            near,
            Camera.MonoOrStereoscopicEye.Mono, 
            corners
        );
        
        // Convert near corners to world space
        for (int i = 0; i < 4; i++)
        {
            corners[i] = camera.transform.TransformVector(corners[i]) + camera.transform.position;
        }
        
        // Get frustum corners in view space for far plane
        camera.CalculateFrustumCorners(
            new Rect(0, 0, 1, 1), 
            far,
            Camera.MonoOrStereoscopicEye.Mono, 
            corners
        );
        
        // Convert far corners to world space
        for (int i = 0; i < 4; i++)
        {
            corners[i + 4] = camera.transform.TransformVector(corners[i]) + camera.transform.position;
        }
        
        return corners;
    }
}
