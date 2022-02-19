using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class Lighting
{
    private const string bufferName = "Lighting";
    private const int maxDirLightCount = 4;
    // TODO: CommandBuffer 运行逻辑
    // CommandBuffer 携带一系列的渲染命令, 依赖相机, 用来扩展渲染管线的渲染结果
    // 可以指定在相机渲染的某个渲染阶段执行
    private CommandBuffer buffer = new CommandBuffer()
    {
        name = bufferName,
    };
    private CullingResults cullingResults;
    // 阴影信息
    private Shadows shadows = new Shadows();
    // 平行光属性ID
    private static int dirLightCountId = Shader.PropertyToID("_DirectionalLightCount");
    private static int dirLightColorsId = Shader.PropertyToID("_DirectionalLightColors");
    private static int dirLightDirectionsId = Shader.PropertyToID("_DirectionalLightDirections");
    private static int dirLightShowDataId = Shader.PropertyToID("_DirectionalLightShadowdatas");
    private static Vector4[] dirLightColors = new Vector4[maxDirLightCount];
    private static Vector4[] dirLightDirections = new Vector4[maxDirLightCount];
    private static Vector4[] dirLightShadowDatas = new Vector4[maxDirLightCount]; 
    public void Setup(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings shadowSettings)
    {
        this.cullingResults = cullingResults;

        buffer.BeginSample(bufferName);
        // 配置阴影信息
        shadows.Setup(context, cullingResults, shadowSettings);
        // 从剔除结果中获取可见光源信息, 并将信息设置到渲染缓存中指定的位置
        SetupLights();
        // 渲染阴影
        shadows.Render();
        buffer.EndSample(bufferName);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }
    
    // 通过cullingResults可以获取有哪几盏灯会影响到当前剔除结果内的模型
    private void SetupLights()
    {
        // 获得可见光源
        NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;
        int dirLightCount = 0;
        for (int i = 0; i < visibleLights.Length; i++)
        {
            // 对光源进行筛选及配置
            VisibleLight visibleLight = visibleLights[i];
            if (visibleLight.lightType != LightType.Directional) continue;
            SetupDirectionalLight(dirLightCount++, ref visibleLight);
            if (dirLightCount >= maxDirLightCount) break;
        }
        // 将光源信息发到指定id的buffer内
        buffer.SetGlobalInt(dirLightCountId, visibleLights.Length >= 4 ? 4 : visibleLights.Length);
        buffer.SetGlobalVectorArray(dirLightColorsId, dirLightColors);
        buffer.SetGlobalVectorArray(dirLightDirectionsId, dirLightDirections);
        buffer.SetGlobalVectorArray(dirLightShowDataId, dirLightShadowDatas);
    }

    // 在相机内可见的灯光
    private void SetupDirectionalLight(int index, ref VisibleLight visibleLight)
    {
        dirLightColors[index] = visibleLight.finalColor;
        dirLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2); // 也就是Y轴指向的方向, 1-x, 2-y, 3-z
        dirLightShadowDatas[index] = shadows.ReserveDirectionalShadows(visibleLight.light, index);
    }
    
    public void Cleanup()
    {
        shadows.CleanupShadowMap();
    }
}