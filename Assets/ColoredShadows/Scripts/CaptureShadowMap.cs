using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

// This pass creates an RTHandle and blits the camera color to it after rendering transparent objects.
// The RTHandle is then set as a global texture, which is available to shaders in the scene. The RTHandle is preserved in all frames while the renderer feature is running to create a recursive rendering effect.
public class CaptureShadowMap : ScriptableRenderPass
{
    private RTHandle shadowMap;
    private RTHandle shadowMapID;
    private Transform lightTransform;
    private const string shadowMapName = "_CustomShadowMap";
    private const string shadowMapIDName = "_CustomShadowMapID";
    private static readonly int shadowMapShaderID = Shader.PropertyToID(shadowMapName);
    private static readonly int shadowMapIDShaderID = Shader.PropertyToID(shadowMapIDName);
    private static readonly int LightSpaceMatrix = Shader.PropertyToID("_LightSpaceMatrix");
    private static readonly int LightSpaceMatrix2 = Shader.PropertyToID("_LightSpaceMatrix2");

    public CaptureShadowMap(RenderPassEvent evt, Transform lightTransform)
    {
        renderPassEvent = evt;
        this.lightTransform = lightTransform;
    }
    
    private class PassData
    {
        internal TextureHandle copySourceTexture;
    }

    // Unity calls the RecordRenderGraph method to add and configure one or more render passes in the render graph system.
    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
        UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

        if (cameraData.camera.cameraType != CameraType.Game)
            return;

        // Create the custom RTHandle
        RenderTextureDescriptor shadowMapDesc = cameraData.cameraTargetDescriptor;
        shadowMapDesc.colorFormat = RenderTextureFormat.RFloat;
        shadowMapDesc.depthBufferBits = 0;
        shadowMapDesc.msaaSamples = 1;
        RenderingUtils.ReAllocateHandleIfNeeded(ref shadowMap, shadowMapDesc, FilterMode.Trilinear, TextureWrapMode.Clamp, name: shadowMapName );
        
        RenderTextureDescriptor shadowMapIDDesc = cameraData.cameraTargetDescriptor;
        shadowMapIDDesc.colorFormat = RenderTextureFormat.ARGBFloat;
        shadowMapIDDesc.depthBufferBits = 0;
        shadowMapIDDesc.msaaSamples = 1;
        RenderingUtils.ReAllocateHandleIfNeeded(ref shadowMapID, shadowMapIDDesc, FilterMode.Trilinear, TextureWrapMode.Clamp, name: shadowMapIDName );
        
        // Make the output texture available for the shaders in the scene.
        // In this sample the output texture is used recursively by the subsequent frames, so it must stay in memory while the renderer feature is running.
        // A TextureHandle object is discarded after each frame, that's why we cannot bind it as a global texture using the RenderGraph API (builder.SetGlobalTextureAfterPass).
        // Instead, we bind the RTHandle as a global texture using the shader API, because the RTHandle is not managed by the render graph system.
        Shader.SetGlobalTexture(shadowMapShaderID, shadowMap);
        Shader.SetGlobalTexture(shadowMapIDShaderID, shadowMapID);

        // Set camera color as a texture resource for this render graph instance
        var customData = frameData.Get<ColoredShadowsRenderFeature.CustomShadowData>();
        TextureHandle sourceDepth = customData.shadowMapColorFormatted;
        TextureHandle sourceColor = customData.shadowMapID;
        // TextureHandle source = resourceData.activeColorTexture;

        // Set RTHandle as a texture resource for this render graph instance
        TextureHandle destinationDepth = renderGraph.ImportTexture(shadowMap);
        TextureHandle destinationColor = renderGraph.ImportTexture(shadowMapID);
        
        if (!sourceColor.IsValid() || !destinationDepth.IsValid())
            return;
        
        float near_plane = 0.1f, far_plane = 10f, size = 5, size2 = 1;
        Matrix4x4 lightProjection = Matrix4x4.Ortho(-size, size, -size, size, near_plane, far_plane);
        Matrix4x4 lightProjection2 = Matrix4x4.Ortho(-size2, size2, -size2, size2, near_plane, far_plane / 5);
        Matrix4x4 lightView = LookAtLH(
            lightTransform.position,  // Light position
            Vector3.zero,    // Look target (origin)
            Vector3.up     // Up vector
        );

        cameraData.clearDepth = true;

        Matrix4x4 lightSpaceMatrix = lightProjection * lightView;
        Matrix4x4 lightSpaceMatrix2 = lightProjection2 * lightView;
        cameraData.camera.transform.LookAt(Vector3.zero);
        
        Shader.SetGlobalMatrix(LightSpaceMatrix, lightSpaceMatrix);
        Shader.SetGlobalMatrix(LightSpaceMatrix2, lightSpaceMatrix2);
        // Debug.Log($"lightSpaceMatrix: {lightSpaceMatrix}");
        // Debug.Log($"lightProjection: {lightProjection}");
        // Debug.Log($"lightView: {lightView}");
        
        Shader.SetGlobalVector("_ViewPos3", lightTransform.position);

        RenderGraphUtils.BlitMaterialParameters para = new(sourceDepth, destinationDepth, Blitter.GetBlitMaterial(TextureDimension.Tex2D), 0);
        renderGraph.AddBlitPass(para, "CaptureShadowsDepth");
        
        RenderGraphUtils.BlitMaterialParameters para2 = new(sourceColor, destinationColor, Blitter.GetBlitMaterial(TextureDimension.Tex2D), 0);
        renderGraph.AddBlitPass(para2, "CaptureShadowsColor");
    }
    
    public static Matrix4x4 GetViewMatrix(Vector3 cameraPosition, Quaternion cameraRotation)
    {
        // Create the rotation matrix from the camera's rotation
        Matrix4x4 rotationMatrix = Matrix4x4.Rotate(Quaternion.Inverse(cameraRotation));
    
        // Create the translation matrix from the camera's position
        Matrix4x4 translationMatrix = Matrix4x4.Translate(-cameraPosition);
    
        // The view matrix is rotation * translation
        // This order is important - we first translate then rotate
        return rotationMatrix * translationMatrix;
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
    
    public void Dispose()
    {
        shadowMap?.Release();
        shadowMapID?.Release();
    }
}