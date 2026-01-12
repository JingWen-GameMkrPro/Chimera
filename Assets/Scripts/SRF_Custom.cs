using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RendererUtils;

public class SRF_Custom : ScriptableRendererFeature
{
    [System.Serializable]
    public class Setting
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        public Material material = null;
        public LayerMask layerMask = 0;
        // public ShaderTagId[] shaderTagIds = null;
        public string OutputTextureName = "UnknownTextureName";
        public string PassName = "UnknownPassName";
    }

    public Setting setting = new Setting();
    SRP_Custom pass = null;

    public override void Create()
    {
        pass = new SRP_Custom(setting);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (setting.material != null)
        {
            renderer.EnqueuePass(pass);
        }
    }

    public class SRP_Custom : ScriptableRenderPass
    {
        Setting setting = null;
        ShaderTagId[] shaderTags;

        public SRP_Custom(Setting setting)
        {
            this.setting = setting;
            this.renderPassEvent = setting.renderPassEvent;
            shaderTags = new ShaderTagId[]
            {
                new ShaderTagId("UniversalForward"),
            };
        }

        public class PassData
        {
            public RendererListHandle rendererListHandle;

        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            // ¸ê·½Án©ú
            var resourceData = frameData.Get<UniversalResourceData>();
            var renderingData = frameData.Get<UniversalRenderingData>();
            var cameraData = frameData.Get<UniversalCameraData>();

            TextureDesc textureDesc = new TextureDesc(cameraData.camera.pixelWidth, cameraData.camera.pixelHeight);
            textureDesc.colorFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_SRGB;
            textureDesc.name = setting.OutputTextureName;
            TextureHandle textureHandle = renderGraph.CreateTexture(textureDesc);

            RendererListDesc rendererListDesc = new(shaderTags, renderingData.cullResults, cameraData.camera);
            rendererListDesc.renderQueueRange = RenderQueueRange.opaque;
            rendererListDesc.layerMask = setting.layerMask;
            rendererListDesc.sortingCriteria = SortingCriteria.CommonOpaque;
            rendererListDesc.overrideMaterial = setting.material;
            rendererListDesc.overrideMaterialPassIndex = 0;
            RendererListHandle rendererListHandle = renderGraph.CreateRendererList(rendererListDesc);

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(setting.PassName, out var passData))
            {
                builder.AllowPassCulling(false);
                passData.rendererListHandle = rendererListHandle;
                builder.UseRendererList(passData.rendererListHandle);

                builder.SetRenderAttachment(textureHandle, 0, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Read);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    context.cmd.DrawRendererList(data.rendererListHandle);
                });
            }
        }
    }
}
