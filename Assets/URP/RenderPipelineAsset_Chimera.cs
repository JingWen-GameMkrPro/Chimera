using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.Universal;

[CreateAssetMenu(menuName = "Chimera/RPA_Chimera")]
public class RenderPipelineAsset_Chimera : RenderPipelineAsset<RenderPipeline_Chimera>
{
    protected override RenderPipeline CreatePipeline()
    {
        Debug.Log("------RPA_Chimera CreatePipeline()------");
        return new RenderPipeline_Chimera();
    }
}

public class RenderPipeline_Chimera : RenderPipeline
{
    public const string TAG_PIPELINE_NAME = nameof(RenderPipeline_Chimera);

    public const string TAG_OPAQUE_NAME = nameof(RenderPipeline_Chimera) + "_Opaque";

    public const string TAG_TRANSPARENT_NAME = nameof(RenderPipeline_Chimera) + "_Transparent";

    public static ShaderTagId TagPipeline => new ShaderTagId(TAG_PIPELINE_NAME);
    public static ShaderTagId TagOpaque => new ShaderTagId(TAG_OPAQUE_NAME);
    public static ShaderTagId TagTransparent => new ShaderTagId(TAG_TRANSPARENT_NAME);


    // Get from CommandBufferPool to reuse
    CommandBuffer cmd;

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        Debug.Log("------RP_Chimera Render()------");
        foreach(var cam in cameras)
        {
            Debug.Log(cam.name);
            context.SetupCameraProperties(cam);

            clear(context, cam);

            drawOpaqueObject(context, cam);

            drawSkybox(context, cam);

            drawTransparentObject(context, cam);


            context.Submit();
        }
        return;
    }

    void clear(ScriptableRenderContext context, Camera cam)
    {
        cmd = CommandBufferPool.Get("Clear...");
        cmd.ClearRenderTarget(true, true, Color.black); // 清除深度與顏色
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    void drawSkybox(ScriptableRenderContext context, Camera cam)
    {
        cmd = CommandBufferPool.Get("Render Skybox...");
        cmd.DrawRendererList(context.CreateSkyboxRendererList(cam));
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    void drawOpaqueObject(ScriptableRenderContext context, Camera cam)
    {
        cam.TryGetCullingParameters(out var parameters);
        var cullResults = context.Cull(ref parameters);

        var desc = new RendererListDesc(RenderPipeline_Chimera.TagOpaque, cullResults, cam);
        desc.rendererConfiguration = PerObjectData.None;
        desc.renderQueueRange = RenderQueueRange.opaque;
        desc.sortingCriteria = SortingCriteria.CommonOpaque;

        cmd = CommandBufferPool.Get("Render Opaque Objects...");
        cmd.DrawRendererList(context.CreateRendererList(desc));
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    void drawTransparentObject(ScriptableRenderContext context, Camera cam)
    {
        cam.TryGetCullingParameters(out var parameters);
        var cullResults = context.Cull(ref parameters);

        var desc = new RendererListDesc(RenderPipeline_Chimera.TagTransparent, cullResults, cam);
        desc.rendererConfiguration = PerObjectData.None;
        desc.renderQueueRange = RenderQueueRange.transparent;
        desc.sortingCriteria = SortingCriteria.CommonTransparent;

        cmd = CommandBufferPool.Get("Render Transparent Objects...");
        cmd.DrawRendererList(context.CreateRendererList(desc));
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
}

//public class ScriptableRenderPass_Object : ScriptableRenderPass
