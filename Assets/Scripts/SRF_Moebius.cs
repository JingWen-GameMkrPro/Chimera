using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RendererUtils;

public class SRF_Moebius : ScriptableRendererFeature
{
    [System.Serializable]
    public class Setting
    {
        [Header("Pass1: Draw Normal Texture")]
        public RenderPassEvent renderPassEventPass1 = RenderPassEvent.AfterRenderingOpaques;
        public Material materialPass1 = null;
        public LayerMask layerMaskPass1 = 0;
        public string textureNamePass1 = "UnknownTextureName";
        public string namePass1 = "UnknownPassName";
        [Header("Pass2: Sobel Filter Normal Texture")]
        public Material materialPass2 = null;
        public string namePass2 = "UnknownPassName";
    }

    public Setting setting = new Setting();
    SRP_Moebius pass = null;

    public override void Create()
    {
        pass = new SRP_Moebius(setting);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (setting.materialPass1 != null && setting.materialPass2 != null)
        {
            renderer.EnqueuePass(pass);
        }
    }

    public class SRP_Moebius : ScriptableRenderPass
    {
        Setting setting = null;
        ShaderTagId[] shaderTags;

        public SRP_Moebius(Setting setting)
        {
            this.setting = setting;
            this.renderPassEvent = setting.renderPassEventPass1;
            shaderTags = new ShaderTagId[]
            {
                new ShaderTagId("UniversalForward"),
            };
        }

        public class DataPass1
        {
            // 繪製物件名單
            public RendererListHandle rendererListHandle;
        }

        public class DataPass2
        {
            public TextureHandle textureHandle;
            public Material material;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            // 資源聲明
            var resourceData = frameData.Get<UniversalResourceData>();
            var renderingData = frameData.Get<UniversalRenderingData>();
            var cameraData = frameData.Get<UniversalCameraData>();

            TextureDesc textureDesc = new TextureDesc(cameraData.camera.pixelWidth, cameraData.camera.pixelHeight);
            // NOTE: UNorm (Unsigned Normalized), Linear Space [0 ~ 1], 適用於法線貼圖，避免SRGB格式導致Gamma校正問題
            textureDesc.colorFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_UNorm;
            textureDesc.name = setting.textureNamePass1;
            // NOTE: 表示這張貼圖將不會分配額外記憶體去寫入深度資訊
            textureDesc.depthBufferBits = 0;
            TextureHandle textureHandle = renderGraph.CreateTexture(textureDesc);

            RendererListDesc rendererListDesc = new(shaderTags, renderingData.cullResults, cameraData.camera);
            rendererListDesc.renderQueueRange = RenderQueueRange.opaque;
            rendererListDesc.layerMask = setting.layerMaskPass1;
            rendererListDesc.sortingCriteria = SortingCriteria.CommonOpaque;
            rendererListDesc.overrideMaterial = setting.materialPass1;
            rendererListDesc.overrideMaterialPassIndex = 0;
            RendererListHandle rendererListHandle = renderGraph.CreateRendererList(rendererListDesc);

            // NOTE: Pass1: Draw Normal Texture
            using (var builder = renderGraph.AddRasterRenderPass<DataPass1>(setting.namePass1, out var passData))
            {
                builder.AllowPassCulling(false);
                passData.rendererListHandle = rendererListHandle;
                builder.UseRendererList(passData.rendererListHandle);

                builder.SetRenderAttachment(textureHandle, 0, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Read);

                builder.SetRenderFunc((DataPass1 data, RasterGraphContext context) =>
                {
                    context.cmd.DrawRendererList(data.rendererListHandle);
                });
            }

            // NOTE: Pass2: Sobel Filter Normal Texture
            using (var builder = renderGraph.AddRasterRenderPass<DataPass2>(setting.namePass2, out var passData))
            {
                builder.AllowPassCulling(false);

                passData.textureHandle = textureHandle;
                passData.material = setting.materialPass2;
                builder.UseTexture(passData.textureHandle, AccessFlags.Read);
                builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);

                builder.SetRenderFunc((DataPass2 data, RasterGraphContext context) =>
                {
                    // 使用 Blitter API 進行全螢幕繪製
                    // 你的 Shader 需要有 _BlitTexture 屬性來接收輸入圖 (或是你在這裡手動 SetGlobalTexture)

                    // 1. 綁定輸入貼圖到 Shader 全域變數 (如果 Shader 使用特定名稱)
                    // context.cmd.SetGlobalTexture("_InputNormalMap", data.inputTexture);

                    // 2. 執行 Blit
                    // 注意：在 RenderGraph 中，Blitter.BlitTexture 取代了舊的 cmd.Blit
                    // _BlitTexture
                    Blitter.BlitTexture(context.cmd, data.textureHandle, new Vector4(1, 1, 0, 0), data.material, 0);
                });
            }
        }
    }
}
