using System.Collections.Generic;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine;

public class TraxURPRenderFeature : ScriptableRendererFeature
{
   public class TraxRenderObjectPass : ScriptableRenderPass
   {
      FilteringSettings m_FilteringSettings;
      List<ShaderTagId> m_ShaderTagIdList = new List<ShaderTagId>();

      public Material overrideMaterial { get; set; }

      RenderStateBlock m_RenderStateBlock;

      public TraxRenderObjectPass(Shader replacementShader)
      {
         m_RenderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);
         m_ShaderTagIdList.Add(new ShaderTagId("UniversalForward"));
         m_ShaderTagIdList.Add(new ShaderTagId("LightweightForward"));
         m_ShaderTagIdList.Add(new ShaderTagId("SRPDefaultUnlit"));
         m_ShaderTagIdList.Add(new ShaderTagId("Opaque"));
         renderPassEvent = RenderPassEvent.BeforeRendering;
         this.overrideMaterial = new Material(replacementShader);
      }

      int layerMask = 0;

      public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
      {
      }
      
      public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
      {
         SortingCriteria sortingCriteria = renderingData.cameraData.defaultOpaqueSortFlags;

         DrawingSettings drawingSettings = CreateDrawingSettings(m_ShaderTagIdList, ref renderingData, sortingCriteria);
         drawingSettings.overrideMaterial = overrideMaterial;
         drawingSettings.overrideMaterialPassIndex = 0;

         ref CameraData cameraData = ref renderingData.cameraData;
         Camera camera = cameraData.camera;

         CommandBuffer cmd = new CommandBuffer();

         // m_FilteringSettings.layerMask = layerMask;
   
         // context.ExecuteCommandBuffer(cmd);
         // cmd.Clear();


         Matrix4x4 projectionMatrix = Matrix4x4.Perspective(camera.fieldOfView, camera.aspect, camera.nearClipPlane, camera.farClipPlane);

         Matrix4x4 viewMatrix = camera.worldToCameraMatrix;

         cmd.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
         context.ExecuteCommandBuffer(cmd);

         context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref m_FilteringSettings, ref m_RenderStateBlock);

         context.ExecuteCommandBuffer(cmd);
         CommandBufferPool.Release(cmd);
      }
   }


   TraxRenderObjectPass pass;
   public Shader replacementShader;

   public override void Create()
   {
      pass = new TraxRenderObjectPass(replacementShader);
   }

   public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
   {
      renderer.EnqueuePass(pass);
   }
}
