using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

public class SRF_BlendLayer : ScriptableRendererFeature
{
    [System.Serializable]
    public class Setting
    {
        public RenderPassEvent CaptureLayerTiming = RenderPassEvent.AfterRenderingPostProcessing;
        public RenderPassEvent PostProcessUiLayerTiming = RenderPassEvent.AfterRenderingPostProcessing + 49;
        public RenderPassEvent CombineLayerTiming = RenderPassEvent.AfterRenderingPostProcessing + 99;
        public Material PostProcessUiLayerMaterial;
        public Material CombineLayerMaterial;
        public Material PostProcessCombineLayerMaterial;
    }

    public class CacheLayer : ContextItem
    {
        public TextureHandle CaptureLayer;
        public TextureHandle PostProcessUiLayer;

        public override void Reset()
        {
            CaptureLayer = TextureHandle.nullHandle;
            PostProcessUiLayer = TextureHandle.nullHandle;
        }
    }

    public Setting InspectorSetting = new Setting();
    CaptureLayerRenderPass captureLayerRenderPass_;
    PostProcessUiLayerRenderPass postProcessUiLayerRenderPass_;
    CombineLayerRenderPass combineLayerRenderPass_;

    public override void Create()
    {
        captureLayerRenderPass_ = new CaptureLayerRenderPass();
        postProcessUiLayerRenderPass_ = new PostProcessUiLayerRenderPass(InspectorSetting);
        combineLayerRenderPass_ = new CombineLayerRenderPass(InspectorSetting);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        captureLayerRenderPass_.renderPassEvent = InspectorSetting.CaptureLayerTiming;
        renderer.EnqueuePass(captureLayerRenderPass_);

        postProcessUiLayerRenderPass_.renderPassEvent = InspectorSetting.PostProcessUiLayerTiming;
        renderer.EnqueuePass(postProcessUiLayerRenderPass_);

        combineLayerRenderPass_.renderPassEvent = InspectorSetting.CombineLayerTiming;
        renderer.EnqueuePass(combineLayerRenderPass_);
    }

    class CaptureLayerRenderPass : ScriptableRenderPass
    {
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourcesData = frameData.Get<UniversalResourceData>();
            var cacheLayer = frameData.GetOrCreate<CacheLayer>();

            cacheLayer.CaptureLayer = resourcesData.cameraColor;

            if (resourcesData.isActiveTargetBackBuffer)
            {
                Debug.LogError("Layer Feature doesn't work with direct to backbuffer rendering (yet)");
            }

            TextureDesc desc = resourcesData.cameraColor.GetDescriptor(renderGraph);

            if (!GraphicsFormatUtility.HasAlphaChannel(desc.format))
            {
                Debug.LogWarning("Layer does not have an alpha channel. Blending will overwrite the entire screen.");
            }

            desc.clearBuffer = true;
            desc.clearColor = new Color(1.0f, 1.0f, 1.0f, 0.0f);
            desc.name = "_CameraUiLayer";

            var newLayer = renderGraph.CreateTexture(desc);
            resourcesData.cameraColor = newLayer;
        }
    }

    class PostProcessUiLayerRenderPass : ScriptableRenderPass
    {
        Setting setting_;

        public PostProcessUiLayerRenderPass(Setting setting)
        {
            setting_ = setting;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (setting_.PostProcessUiLayerMaterial != null)
            {
                UniversalResourceData resourcesData = frameData.Get<UniversalResourceData>();

                TextureDesc desc = resourcesData.cameraColor.GetDescriptor(renderGraph);
                desc.clearBuffer = true;
                desc.clearColor = new Color(1.0f, 1.0f, 1.0f, 0.0f);
                desc.name = "_CameraPostProcessUiLayer";
                var newLayer = renderGraph.CreateTexture(desc);

                RenderGraphUtils.BlitMaterialParameters blitMaterialParameters = new(resourcesData.cameraColor, newLayer, setting_.PostProcessUiLayerMaterial, 0);
                renderGraph.AddBlitPass(blitMaterialParameters, passName);

                var cacheLayer = frameData.GetOrCreate<CacheLayer>();
                cacheLayer.PostProcessUiLayer = newLayer;
            }
        }
    }

    class CombineLayerRenderPass : ScriptableRenderPass
    {
        Setting setting_;

        private class PassData
        {
            internal TextureHandle captureTex;
            internal TextureHandle uiTex;
            internal Material combineMaterial;
        }

        public CombineLayerRenderPass(Setting setting)
        {
            setting_ = setting;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (setting_.CombineLayerMaterial == null) return;

            UniversalResourceData resourcesData = frameData.Get<UniversalResourceData>();
            var cacheLayer = frameData.GetOrCreate<CacheLayer>();

            TextureDesc desc = resourcesData.cameraColor.GetDescriptor(renderGraph);
            desc.clearBuffer = true;
            desc.clearColor = new Color(1.0f, 1.0f, 1.0f, 0.0f);
            desc.name = "_CameraCombineLayer";
            var combineLayer = renderGraph.CreateTexture(desc);

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName + "_Combine", out var passData))
            {
                passData.captureTex = cacheLayer.CaptureLayer;
                passData.uiTex = cacheLayer.PostProcessUiLayer;
                passData.combineMaterial = setting_.CombineLayerMaterial;

                builder.UseTexture(passData.captureTex);
                builder.UseTexture(passData.uiTex);

                builder.SetRenderAttachment(combineLayer, 0);

                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    context.cmd.SetGlobalTexture("_CaptureTex", data.captureTex);
                    Blitter.BlitTexture(context.cmd, data.uiTex, new Vector4(1, 1, 0, 0), data.combineMaterial, 0);
                });
            }

            if (setting_.PostProcessCombineLayerMaterial != null) 
            {
                desc.name = "_CameraPostProcessLayer";
                var finalLayer = renderGraph.CreateTexture(desc);

                RenderGraphUtils.BlitMaterialParameters blitMaterialParameters = new(combineLayer, finalLayer, setting_.PostProcessCombineLayerMaterial, 0);
                renderGraph.AddBlitPass(blitMaterialParameters, passName + "_FinalPostProcess");

                resourcesData.cameraColor = finalLayer;
            }
            else
            {
                resourcesData.cameraColor = combineLayer;
            }
        }
    }
}