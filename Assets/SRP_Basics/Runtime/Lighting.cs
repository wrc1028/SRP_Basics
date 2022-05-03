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
    private Shadows shadows = new Shadows();

    private static int dirLightCountId = Shader.PropertyToID("_DirectionalLightCount");
    private static int dirLightColorsId = Shader.PropertyToID("_DirectionalLightColors");
    private static int dirLightDirectionsId = Shader.PropertyToID("_DirectionalLightDirections");
    private static int dirLightShadowDataId = Shader.PropertyToID("_DirectionalLightShadowData");
    private static Vector4[] dirLightColors = new Vector4[maxDirLightCount];
    private static Vector4[] dirLightDirections = new Vector4[maxDirLightCount];
    private static Vector4[] dirLightShadowData = new Vector4[maxDirLightCount];
    public void Setup(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings shadowSettings)
    {
        this.cullingResults = cullingResults;
        buffer.BeginSample(bufferName);
        shadows.Setup(context, cullingResults, shadowSettings);
        SetupLights();
        shadows.Reder();
        buffer.EndSample(bufferName);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }
    
    // 通过cullingResults可以获取有哪几盏灯会影响到当前剔除的结果
    private void SetupLights()
    {
        NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;
        int dirLightCount = 0;
        for (int i = 0; i < visibleLights.Length; i++)
        {
            VisibleLight visibleLight = visibleLights[i];
            if (visibleLight.lightType != LightType.Directional) continue;
            SetupDirectionalLight(dirLightCount++, ref visibleLight);
            if (dirLightCount >= maxDirLightCount) break;
        }

        buffer.SetGlobalInt(dirLightCountId, visibleLights.Length >= 4 ? 4 : visibleLights.Length);
        buffer.SetGlobalVectorArray(dirLightColorsId, dirLightColors);
        buffer.SetGlobalVectorArray(dirLightDirectionsId, dirLightDirections);
        buffer.SetGlobalVectorArray(dirLightShadowDataId, dirLightShadowData);
    }
    /// <summary>
    /// 配置可见的平行光属性
    /// </summary>
    /// <param name="index">第几个会对场景产生影响的可见平行光</param>
    /// <param name="visibleLight">可见光(或称为有效光源)</param>
    private void SetupDirectionalLight(int index, ref VisibleLight visibleLight)
    {
        dirLightColors[index] = visibleLight.finalColor;
        dirLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2); // 也就是Y轴指向的方向, 1-x, 2-y, 3-z
        dirLightShadowData[index] = shadows.ReserveDirectionalShadows(visibleLight.light, index);
    }

    public void Cleanup()
    {
        shadows.Cleanup();
    }
}