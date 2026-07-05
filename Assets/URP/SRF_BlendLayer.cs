using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using System.Collections.Generic;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Experimental.Rendering;

// Render a specific camera in isolation and blend it with the previously captured camera color buffer
public class SRF_BlendLayer : ScriptableRendererFeature
{
    [System.Serializable]
    public class Setting
    {
        public RenderPassEvent CaptureTiming = RenderPassEvent.AfterRenderingPostProcessing;
        public RenderPassEvent RenderTiming = RenderPassEvent.AfterRenderingPostProcessing + 49;
        public Material RenderMaterial;
    }

    public class CaptureLayer : ContextItem
    {
        public TextureHandle Layer;

        public override void Reset()
        {
            Layer = TextureHandle.nullHandle;
        }
    }

    public Setting InspectorSetting = new Setting();

    public override void Create()
    {

    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {

    }
}
