using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;
using UnityEngine.Rendering.RenderGraphModule;

public class ReplacementShaderRenderFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public Shader replacementShader;
        public string replacementTag = "RenderType";
        public Color shaderColor = Color.white;
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    }

    public Settings settings = new Settings();
    private ReplacementShaderPass renderPass;
    public RTHandle renderTexture;

    public override void Create()
    {
        renderPass = new ReplacementShaderPass(settings.replacementShader, 
                                              settings.replacementTag, 
                                              settings.shaderColor);
        
        // Set the render pass event
        renderPass.renderPassEvent = (RenderPassEvent)settings.renderPassEvent;
        
        // Initialize the render texture
        RenderTextureDescriptor descriptor = new RenderTextureDescriptor(
            Screen.width, Screen.height, 
            RenderTextureFormat.ARGB32, 0);
        renderTexture = RTHandles.Alloc(descriptor, name: "ReplacementShaderRT");
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // Skip the main camera
        if (renderingData.cameraData.camera == Camera.main)
            return;
        
        // Set the camera's target texture
        if (renderingData.cameraData.targetTexture == null)
        {
            // renderPass.SetupRenderTexture(renderTexture);
        }
        
        renderer.EnqueuePass(renderPass);
    }

    protected override void Dispose(bool disposing)
    {
        // Release the render texture when the feature is disposed
        if (renderTexture != null)
        {
            RTHandles.Release(renderTexture);
            renderTexture = null;
        }
        
        renderPass?.Dispose();
        renderPass = null;
    }

    private class ReplacementShaderPass : ScriptableRenderPass
    {
        private readonly Shader replacementShader;
        private readonly string replacementTag;
        private readonly Color shaderColor;
        private RTHandle destination;
        private Material replacementMaterial;
        private List<ShaderTagId> shaderTagIdList = new List<ShaderTagId>();
        private FilteringSettings filteringSettings;
        private RenderStateBlock renderStateBlock;

        public ReplacementShaderPass(Shader shader, string tag, Color color)
        {
            replacementShader = shader;
            replacementTag = tag;
            shaderColor = color;
            
            // Initialize shader tag ids for compatible render pipelines
            shaderTagIdList.Add(new ShaderTagId("UniversalForward"));
            shaderTagIdList.Add(new ShaderTagId("SRPDefaultUnlit"));
            
            // Initialize filtering settings (render all opaque objects)
            filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
            
            // Initialize render state block
            renderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);
        }

        public void SetupRenderTexture(RTHandle renderTexture)
        {
            destination = renderTexture;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            // Configure the render target
            if (destination == null)
            {
                RenderTextureDescriptor descriptor = cameraTextureDescriptor;
                descriptor.enableRandomWrite = false;
                descriptor.depthBufferBits = 24;
                
                // Create a temporary render texture if destination is not set
                RTHandles.Alloc(descriptor, name: "TempReplacementRT");
                destination = RTHandles.Alloc(descriptor, name: "TempReplacementRT");
            }
            
            // Configure destination
            ConfigureTarget(destination);
            ConfigureClear(ClearFlag.All, Color.clear);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            base.RecordRenderGraph(renderGraph, frameData);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // Skip if replacement shader is null
            if (replacementShader == null)
                return;
                
            CommandBuffer cmd = CommandBufferPool.Get("ReplacementShaderPass");
            
            // Create material for replacement shader if needed
            if (replacementMaterial == null)
            {
                replacementMaterial = new Material(replacementShader);
                replacementMaterial.SetColor("_Color", shaderColor);
            }
            
            // Set up drawing settings
            DrawingSettings drawingSettings = CreateDrawingSettings(
                shaderTagIdList[0], ref renderingData, SortingCriteria.CommonOpaque);
            
            // Override shader pass with replacement material
            for (int i = 0; i < shaderTagIdList.Count; i++)
            {
                drawingSettings.overrideMaterial = replacementMaterial;
                drawingSettings.SetShaderPassName(i, shaderTagIdList[i]);
            }
            
            // Execute the render commands
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            
            // Draw renderers with the replacement shader
            context.DrawRenderers(
                renderingData.cullResults, 
                ref drawingSettings, 
                ref filteringSettings, 
                ref renderStateBlock);
            
            // Execute and release the command buffer
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Dispose()
        {
            // Clean up the material
            if (replacementMaterial != null)
            {
                Object.DestroyImmediate(replacementMaterial);
                replacementMaterial = null;
            }
        }
    }
}