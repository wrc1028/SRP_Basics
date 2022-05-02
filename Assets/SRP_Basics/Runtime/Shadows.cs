using System.Security.Principal;
using UnityEngine;
using UnityEngine.Rendering;

public class Shadows 
{
    //===================buffer===================
    private const string bufferName = "Shadows";
    private CommandBuffer shadowBuffer = new CommandBuffer
    {
        name = bufferName
    };
    //===================render info===================
    private ScriptableRenderContext context;
    private CullingResults cullingResults;
    private ShadowSettings shadowSettings;
    //===================shadow setting===================
    private const int maxShadowedDirectionalLightCount = 1;
    private int shadowedDirectionalLightCount;
    private struct ShadowedDirectionalLight
    {
        public int visibleLightIndex;
    }
    private ShadowedDirectionalLight[] shadowedDirectionalLights = 
        new ShadowedDirectionalLight[maxShadowedDirectionalLightCount];
    //===================shader prop id===================
    private static int dirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas");

    public void Setup(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings shadowSettings)
    {
        this.context = context;
        this.cullingResults = cullingResults;
        this.shadowSettings = shadowSettings;
        // 用来记录当前产生阴影灯光的索引
        shadowedDirectionalLightCount = 0;
    }
    /// <summary>
    /// 渲染阴影
    /// </summary>
    public void Reder()
    {
        if (shadowedDirectionalLightCount > 0)
        {
            RenderDirectionalShadows();
        }
        else
        {
            shadowBuffer.GetTemporaryRT(dirShadowAtlasId, 1, 1, 32, FilterMode.Point, RenderTextureFormat.Shadowmap);
        }
    }
    private void RenderDirectionalShadows()
    {
        // 在缓存池中获取一个临时存储ShadowMap的RenderTexture
        int atlasSize = (int)shadowSettings.directional.atlasSize;
        shadowBuffer.GetTemporaryRT(dirShadowAtlasId, atlasSize, atlasSize, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        // 设置渲染的目标, 一旦设置的渲染目标, 在执行清理操作时, 将会清理当前所设置的渲染目标的缓存
        shadowBuffer.SetRenderTarget(dirShadowAtlasId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        shadowBuffer.ClearRenderTarget(true, false, Color.clear);
        shadowBuffer.BeginSample(bufferName);
        ExecuteBuffer();
        for (int i = 0; i < shadowedDirectionalLightCount; i++)
        {
            RenderDirectionalShadows(i, atlasSize);
        }
        shadowBuffer.EndSample(bufferName);
        ExecuteBuffer();
    }
    private void RenderDirectionalShadows(int index, int tileSize)
    {
        // 首先获得阴影的绘制方式(设置)
        ShadowedDirectionalLight light = shadowedDirectionalLights[index];
        ShadowDrawingSettings shadowDrawingSettings = new ShadowDrawingSettings(cullingResults, light.visibleLightIndex);
        cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives
        (
            light.visibleLightIndex, 0, 1, Vector3.zero, tileSize, 0.0f, 
            out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData splitData
        );
        // 将计算阴影的参数传入Buffer
        shadowDrawingSettings.splitData = splitData;
        shadowBuffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
        ExecuteBuffer();
        context.DrawShadows(ref shadowDrawingSettings);
    }
    /// <summary>
    /// 在渲染完毕后清楚相关缓存
    /// </summary>
    public void Cleanup()
    {
        shadowBuffer.ReleaseTemporaryRT(dirShadowAtlasId);
        ExecuteBuffer();
    }

    /// <summary>
    /// 保存ShadowMap信息
    /// </summary>
    /// <param name="light">当前产生阴影的光源</param>
    /// <param name="visibleLightIndex">当前阴影光源的索引</param>
    public void ReserveDirectionalShadows (Light light, int visibleLightIndex)
    {
        if (shadowedDirectionalLightCount < maxShadowedDirectionalLightCount &&
            light.shadows != LightShadows.None && light.shadowStrength > 0.0f && 
            cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds outBounds))
        {
            shadowedDirectionalLights[shadowedDirectionalLightCount++] =  new ShadowedDirectionalLight
            {
                visibleLightIndex = visibleLightIndex,
            };
        }
    }

    private void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(shadowBuffer);
        shadowBuffer.Clear();
    }

}