using UnityEngine;
using UnityEngine.Rendering;
// Our camera renderer is roughly equivalent to the scriptable renderers of the Universal RP.
// 可编程渲染器, 可以按照自己想法去自定义渲染逻辑
partial class CameraRenderer
{
    /// <summary>
    /// 渲染上下文, 可以通俗的理解为: 当前渲染所需要的全部缓存(buffers)
    /// 比如顶点数据、材质、纹理等等
    /// </summary>
    private ScriptableRenderContext context;
    /// <summary>
    /// 渲染相机, 以当前这个相机的参数对场景中的物体进行渲染
    /// </summary>
    private Camera camera;
    // 自定义的渲染Buffer
    private const string bufferName = "Render Camera !";
    private CommandBuffer buffer = new CommandBuffer()
    {
        name = bufferName,
    };
    // 剔除结果
    private CullingResults cullingResults;
    // 无光Shader的ID
    private static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");

    // --------------------------------------------------------------------------------------- 

    // 绘制相机所能看见的所有几何图形
    public void Render(ScriptableRenderContext context, Camera camera)
    {
        this.context = context;
        this.camera = camera;

        PrepareBuffer();
        PrepareForSceneWindow();
        if (!Cull()) return;

        Step();
        DrawVisibleGeometry();
        DrawUnsupportedShaders();
        DrawGizmos();
        Submit();
    }
    // 剔除
    private bool Cull()
    {
        ScriptableCullingParameters parameters;
        // 尝试获得摄像机的剔除所需要的参数，比如相机的位置、近裁面和远裁面、可见层设置等等
        if (camera.TryGetCullingParameters(out parameters))
        {
            cullingResults = context.Cull(ref parameters);
            return true;
        }
        return false;
    }
    // 设置渲染参数
    private void Step()
    {
        context.SetupCameraProperties(camera);
        
        // 清理标志: 渲染前清除缓存的方式
        CameraClearFlags flags = camera.clearFlags;
        buffer.ClearRenderTarget(
            flags <= CameraClearFlags.Depth, 
            flags == CameraClearFlags.Color, 
            Color.clear
        );
        buffer.BeginSample(SampleName);
        ExecuteBuffer();
    }
    // 
    private void DrawVisibleGeometry()
    {
        // --------------------绘制不透明物体--------------------
        // ?排序设置: 用于对场景中物体渲染顺序进行排序(使用距离排序还是 正交排序?)
        SortingSettings sortingSettings = new SortingSettings(camera)
        {
            // 渲染排序规则: 当前是按照不透明物体的渲染顺序进行渲染(从前往后)?
            criteria = SortingCriteria.CommonOpaque,
        };
        // ?绘制参数设置: 如何对物体进行绘制, 是否开启合批处理
        DrawingSettings drawingSettings = new DrawingSettings(unlitShaderTagId, sortingSettings);
        // ?渲染物体筛选: 通过筛选(着色器设置?)对渲染物体进行选择性渲染
        FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
        // 最后将需要渲染的物体及其参数传给当前的渲染Buffer
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
        
        // --------------------绘制天空盒--------------------
        context.DrawSkybox(camera);

        // --------------------绘制透明物体--------------------
        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        drawingSettings.sortingSettings = sortingSettings;
        filteringSettings.renderQueueRange = RenderQueueRange.transparent;
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
    }
    // ?将推入渲染队列的渲染命令(commands)和渲染所需的数据提交, 最终渲染出画面
    private void Submit()
    {
        buffer.EndSample(SampleName);
        ExecuteBuffer();

        context.Submit();
    }
    // 
    private void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }
}
