using UnityEngine;
using UnityEngine.Rendering;
// The asset itself is just a handle and a place to store settings.
// 渲染管线资源: 一个存储渲染设置的资源, 其中就包含不同逻辑的渲染管线
[CreateAssetMenu(menuName = "Rendering/Custom Render Pipeline")]
public class CustomRenderPipelineAsset : RenderPipelineAsset
{
    public bool useSRPBatching, useDynamicBatching, useGPUInstancing;
    protected override RenderPipeline CreatePipeline()
    {
        return new CustomRenderPipeline(useSRPBatching, useDynamicBatching, useGPUInstancing);
    }
}
