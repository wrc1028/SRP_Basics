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
    // Shader Tag 对应的ShaderID
    private static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");
    private static ShaderTagId litShaderTagId = new ShaderTagId("CustomLit");
    // 光源信息
    private Lighting lighting = new Lighting();

    // --------------------------------------------------------------------------------------- 

    // 绘制相机所能看见的所有几何图形
    public void Render(ScriptableRenderContext context, Camera camera, bool useDynamicBatching, bool useGPUInstancing, ShadowSettings shadowSettings)
    {
        this.context = context;
        this.camera = camera;

        PrepareBuffer();
        PrepareForSceneWindow();
        if (!Cull(shadowSettings.maxDistance)) return;
        // Step(); 渲染阴影时因为更改了渲染目标, 因此先将ShadowMap的渲染提到设置相机参数之前(context.SetupCameraProperties(camera))
        buffer.BeginSample(SampleName);
        ExecuteBuffer();
        lighting.Setup(context, cullingResults, shadowSettings);
        buffer.EndSample(SampleName);
        ExecuteBuffer();
        
        Step();
        DrawVisibleGeometry(useDynamicBatching, useGPUInstancing);
        DrawUnsupportedShaders();
        DrawGizmos();
        lighting.Cleanup();
        Submit();
    }
    // 剔除
    private bool Cull(float maxShadowDistance)
    {
        ScriptableCullingParameters parameters;
        // 尝试获得摄像机的剔除所需要的参数，比如相机的位置、近裁面和远裁面、可见层设置等等
        if (camera.TryGetCullingParameters(out parameters))
        {
            // 生成两份结果, 一份用于相机计算, 一份用于计算光照贴图????
            // 设置相机能看见Shadow的最大距离, 得到用于生成光照贴图的剔除结果
            parameters.shadowDistance = Mathf.Min(camera.farClipPlane, maxShadowDistance);
            cullingResults = context.Cull(ref parameters);
            return true;
        }
        return false;
    }
    // 设置渲染参数
    private void Step()
    {
        // 设置相机的参数: 相机矩阵、位置信息等等, (设置渲染目标??)
        context.SetupCameraProperties(camera);
        
        // 清理上一帧渲染结果(Buffer)的方式, 当前参数设置的不太准确
        CameraClearFlags flags = camera.clearFlags;
        buffer.ClearRenderTarget(
            flags <= CameraClearFlags.Depth, 
            flags == CameraClearFlags.Color, 
            Color.clear
        );
        buffer.BeginSample(SampleName);
        ExecuteBuffer(); // 第一次执行CommandBuffer中的命令: 以指定方式清理上一帧的渲染结果、开始记录渲染过程
    }
    // 
    private void DrawVisibleGeometry(bool useDynamicBatching, bool useGPUInstancing)
    {
        // --------------------绘制不透明物体--------------------
        // ?排序设置: 用于对场景中物体渲染顺序进行排序(使用距离排序还是 正交排序?)
        SortingSettings sortingSettings = new SortingSettings(camera)
        {
            // 渲染排序规则: 当前是按照不透明物体的渲染顺序进行渲染(从前往后)?
            criteria = SortingCriteria.CommonOpaque,
        };
        // ?绘制参数设置: 如何对物体进行绘制, 是否开启合批处理
        // SetShaderPassName 指将带有该标签的Shader参与渲染
        DrawingSettings drawingSettings = new DrawingSettings(unlitShaderTagId, sortingSettings)
        {
            enableDynamicBatching = useDynamicBatching,
            enableInstancing = useGPUInstancing,
        };
        drawingSettings.SetShaderPassName(1, litShaderTagId);
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
        ExecuteBuffer(); // 第二次执行CommandBuffer中的命令: 结束记录渲染过程
        // 提交渲染
        context.Submit();
    }
    // 执行CommandBuffer中的渲染命令
    private void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }
}
