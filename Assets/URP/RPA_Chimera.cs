using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;

[CreateAssetMenu(menuName = "Chimera/RPA_Chimera")]
public class RPA_Chimera : RenderPipelineAsset<RP_Chimera>
{
    protected override RenderPipeline CreatePipeline()
    {
        Debug.Log("------RPA_Chimera CreatePipeline()------");
        return new RP_Chimera();
    }
}

public class RP_Chimera : RenderPipeline
{
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

        var desc = new RendererListDesc(new ShaderTagId(nameof(RP_Chimera)), cullResults, cam);
        desc.rendererConfiguration = PerObjectData.None;
        desc.renderQueueRange = RenderQueueRange.opaque;
        desc.sortingCriteria = SortingCriteria.CommonOpaque;

        cmd = CommandBufferPool.Get("Render Opaque Objects...");
        cmd.DrawRendererList(context.CreateRendererList(desc));
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
}
