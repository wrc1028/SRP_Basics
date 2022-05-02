using UnityEngine;
using UnityEngine.Rendering;

// 一个的渲染管线实例
public class CustomRenderPipeline : RenderPipeline
{
    private CameraRenderer renderer;
    private bool useDynamicBatching, useGPUInstancing;
    private ShadowSettings shadowSettings;
    public CustomRenderPipeline(bool useSRPBatching, bool useDynamicBatching, bool useGPUInstancing, ShadowSettings shadowSettings)
    {
        // SRP 并没有减少Draw Call, 而是优化渲染序列
        // 优先级 SRP Batch > GPUInstancing

        GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatching;
        GraphicsSettings.lightsUseLinearIntensity = true;
        this.useDynamicBatching = useDynamicBatching;
        this.useGPUInstancing = useGPUInstancing;

        this.shadowSettings = shadowSettings;
    }
    /// <summary>
    /// 渲染方法, Unity在每一帧调用调用这个方法进行渲染
    /// </summary>
    /// <param name="context"> 一个用于渲染场景物体的结构体(Buffer) </param>
    /// <param name="cameras"> 场景中的摄像机 </param>
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        renderer = new CameraRenderer();
        foreach (Camera camera in cameras)
        {
            renderer.Render(context, camera, useDynamicBatching, useGPUInstancing, shadowSettings);      
        }
    }
}