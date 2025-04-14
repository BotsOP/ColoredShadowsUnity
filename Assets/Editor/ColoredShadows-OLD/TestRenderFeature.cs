using System.Collections.Generic;
using UnityEditor.Rendering.Universal;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

public class TestRenderFeature : ScriptableRendererFeature
{
    class CustomRenderPass : ScriptableRenderPass
    {
        public Material material;

        public void Setup(Material material)
        {
            this.material = material;
        }
        // This class stores the data needed by the RenderGraph pass.
        // It is passed as a parameter to the delegate function that executes the RenderGraph pass.
        private class PassData
        {
            internal TextureHandle copySourceTexture;
        }

        // This static method is passed as the RenderFunc delegate to the RenderGraph render pass.
        // It is used to execute draw commands.
        static void ExecutePass(PassData data, RasterGraphContext context)
        {
            // Records a rendering command to copy, or blit, the contents of the source texture
            // to the color render target of the render pass.
            Blitter.BlitTexture(context.cmd, data.copySourceTexture,
                new Vector4(1, 1, 0, 0), 0, false);
        }

        // RecordRenderGraph is where the RenderGraph handle can be accessed, through which render passes can be added to the graph.
        // FrameData is a context container through which URP resources can be accessed and managed.
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            string passName = "Copy To Debug Texture";
            
            if(frameData.Get<UniversalCameraData>().camera.gameObject.name == "MainCamera1")
                return;

            // Add a raster render pass to the render graph. The PassData type parameter determines
            // the type of the passData output variable.
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName,
                out var passData))
            {
                // UniversalResourceData contains all the texture references used by URP,
                // including the active color and depth textures of the camera.
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

                // Populate passData with the data needed by the rendering function
                // of the render pass.
                // Use the camera's active color texture
                // as the source texture for the copy operation.
                passData.copySourceTexture = resourceData.cameraColor;
                
                // Create a destination texture for the copy operation based on the settings,
                // such as dimensions, of the textures that the camera uses.
                // Set msaaSamples to 1 to get a non-multisampled destination texture.
                // Set depthBufferBits to 0 to ensure that the CreateRenderGraphTexture method
                // creates a color texture and not a depth texture.
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
                desc.msaaSamples = 1;
                desc.depthBufferBits = 0;

                // For demonstrative purposes, this sample creates a temporary destination texture.
                // UniversalRenderer.CreateRenderGraphTexture is a helper method
                // that calls the RenderGraph.CreateTexture method.
                // Using a RenderTextureDescriptor instance instead of a TextureDesc instance
                // simplifies your code.

                TextureHandle destination =
                    UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc,
                        "_InputTexture", false);
                // Declare that this render pass uses the source texture as a read-only input.
                builder.UseTexture(passData.copySourceTexture);
                

                // Declare that this render pass uses the temporary destination texture
                // as its color render target.
                // This is similar to cmd.SetRenderTarget prior to the RenderGraph API.

                builder.SetRenderAttachment(destination, 0);

                // RenderGraph automatically determines that it can remove this render pass
                // because its results, which are stored in the temporary destination texture,
                // are not used by other passes.
                // For demonstrative purposes, this sample turns off this behavior to make sure
                // that render graph executes the render pass. 

                builder.AllowPassCulling(false);

                // Set the ExecutePass method as the rendering function that render graph calls
                // for the render pass. 
                // This sample uses a lambda expression to avoid memory allocations.

                builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context));
                builder.SetGlobalTextureAfterPass(destination, Shader.PropertyToID("_InputTexture"));
            }
        }
    }

    public RenderPassEvent injectionPoint = RenderPassEvent.AfterRenderingTransparents;
    public Material material;

    CustomRenderPass m_ScriptablePass;

    /// <inheritdoc/>
    public override void Create()
    {
        m_ScriptablePass = new CustomRenderPass();
        // Configures where the render pass should be injected.
        m_ScriptablePass.renderPassEvent = injectionPoint;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if(material == null)
            return;

        m_ScriptablePass.Setup(material);
        renderer.EnqueuePass(m_ScriptablePass);
    }
}
