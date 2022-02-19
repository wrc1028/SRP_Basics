using UnityEngine;
using UnityEngine.Rendering;

// 生成ShadowMap
public class Shadows
{
    // 产生阴影的平行光数量以及其索引
    private const int maxShadowedDirectionalLightCount = 4, maxCascades = 4;
    private struct ShadowedDirectionalLight
    {
        // 保留的时可见光源类型中符合要求的灯光索引
        public int visibleLightIndex;
    }
    private ShadowedDirectionalLight[] shadowedDirectionalLights = 
        new ShadowedDirectionalLight[maxShadowedDirectionalLightCount];

    private int shadowedDirectionalLightCount;
    
    private const string bufferName = "Shadows";
    private ScriptableRenderContext context;
    private CullingResults cullingResults;
    private ShadowSettings shadowSettings;
    private CommandBuffer shadowBuffer = new CommandBuffer
    {
        name = bufferName,
    };
    // 指定一个缓存id
    private static int 
        dirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas"), 
        dirShadowMatricesId = Shader.PropertyToID("_DirectionalShadowMatrices"), 
        cascadeCountId = Shader.PropertyToID("_CascadeCount"),
        cascadeCullingSpheresId = Shader.PropertyToID("_CascadeCullingSpheres");

    private static Matrix4x4[] dirShadowMatrices = new Matrix4x4[maxShadowedDirectionalLightCount * maxCascades];
    private static Vector4[] cascadeCullingSpheres = new Vector4[maxCascades];
    
    public void Setup(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings shadowSettings)
    {
        this.context = context;
        this.cullingResults = cullingResults;
        this.shadowSettings = shadowSettings;
        shadowedDirectionalLightCount = 0;
    }

    public void Render()
    {
        if (shadowedDirectionalLightCount > 0)
        {
            RenderDirectionalShadows();
        }
        else
        {
            // 始终保存ShadowMap在Buffer内有实例
            shadowBuffer.GetTemporaryRT(dirShadowAtlasId, 1, 1, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        }
    }
    private void RenderDirectionalShadows()
    {
        // 在CommandBuffer内用指定的缓存id创建一个临时的RenderTexture
        int atlasSize = (int)shadowSettings.directional.atlasSize;
        shadowBuffer.GetTemporaryRT(dirShadowAtlasId, atlasSize, atlasSize, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        // 设置渲染目标, 指示GPU在当前时刻, 将结果渲染到这个缓存内
        shadowBuffer.SetRenderTarget(dirShadowAtlasId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        // 清除上一帧ShadowMap的渲染结果
        shadowBuffer.ClearRenderTarget(true, false, Color.clear);
        // ----------进行ShaderMap渲染----------
        shadowBuffer.BeginSample(bufferName);
        ExecuteBuffer();
        // 对需要能产生阴影的光源进行遍历, 生成这些光源的ShadowMap
        int tiles = shadowedDirectionalLightCount * shadowSettings.directional.cascadeCount;
        int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
        int tileSize = atlasSize / split;
        for (int i = 0; i < shadowedDirectionalLightCount; i++)
        {
            RenderDirectionalShadows(i, split, tileSize);
        }
        // 将能产生阴影的光源的矩阵传送到Buffer里面
        shadowBuffer.SetGlobalInt(cascadeCountId, shadowSettings.directional.cascadeCount);
        shadowBuffer.SetGlobalVectorArray(cascadeCullingSpheresId, cascadeCullingSpheres);
        shadowBuffer.SetGlobalMatrixArray(dirShadowMatricesId, dirShadowMatrices);
        shadowBuffer.EndSample(bufferName);
        ExecuteBuffer();
    }
    // 遍历 能产生阴影的光源, 生成各自的ShadowMap
    // 将多个光源的ShadowMap合并
    // 返回一个二维结果:x保存当前灯光的强度, y保存光源索引
    private void RenderDirectionalShadows(int index, int split, int tileSize)
    {
        ShadowedDirectionalLight light = shadowedDirectionalLights[index];
        ShadowDrawingSettings shadowDrawingSettings = new ShadowDrawingSettings(cullingResults, light.visibleLightIndex);
        int cascadeCount = shadowSettings.directional.cascadeCount;
        int tileOffset = index * cascadeCount;
        Vector3 ratios = shadowSettings.directional.cascadeRatios;
        // 将数据传入到下面这个方法内进行计算, 也就是在光源的反方向上, 距离主相机范围最大距离处放入一个正交相机
        // 以材质球中ShadowCaster为光照模型进行渲染, 渲染出ShadowMap, out就是返回的结果
        for (int i = 0; i < cascadeCount; i++)
        {
            cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(light.visibleLightIndex, i, cascadeCount, ratios, 
                tileSize, 0.0f, out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData splitData);
            shadowDrawingSettings.splitData = splitData;
            if (index == 0)
            {
                Vector4 cullingSphere = splitData.cullingSphere;
                cullingSphere.w *= cullingSphere.w;
                cascadeCullingSpheres[i] = cullingSphere;
            }
            int tileIndex = tileOffset + i;
            dirShadowMatrices[tileIndex] = ConvertToAtlasMatrix(projectionMatrix * viewMatrix, SetTileViewport(tileIndex, split, tileSize), split);
            shadowBuffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
            ExecuteBuffer();
            context.DrawShadows(ref shadowDrawingSettings);
        }
    }
    // 对多光源产生的ShadowMap进行偏移, 从而合并成一张 4合1
    private Vector2 SetTileViewport(int index, int split, float tileSize)
    {
        Vector2 offset = new Vector2(index % split, index / split);
        shadowBuffer.SetViewport(new Rect(offset.x * tileSize, offset.y * tileSize, tileSize, tileSize));
        return offset;
    }
    // ??? 
    Matrix4x4 ConvertToAtlasMatrix (Matrix4x4 m, Vector2 offset, int split)
    {
        if (SystemInfo.usesReversedZBuffer) 
        {
			m.m20 = -m.m20;
			m.m21 = -m.m21;
			m.m22 = -m.m22;
			m.m23 = -m.m23;
		}
        float scale = 1f / split;
		m.m00 = (0.5f * (m.m00 + m.m30) + offset.x * m.m30) * scale;
		m.m01 = (0.5f * (m.m01 + m.m31) + offset.x * m.m31) * scale;
		m.m02 = (0.5f * (m.m02 + m.m32) + offset.x * m.m32) * scale;
		m.m03 = (0.5f * (m.m03 + m.m33) + offset.x * m.m33) * scale;
		m.m10 = (0.5f * (m.m10 + m.m30) + offset.y * m.m30) * scale;
		m.m11 = (0.5f * (m.m11 + m.m31) + offset.y * m.m31) * scale;
		m.m12 = (0.5f * (m.m12 + m.m32) + offset.y * m.m32) * scale;
		m.m13 = (0.5f * (m.m13 + m.m33) + offset.y * m.m33) * scale;
		m.m20 = 0.5f * (m.m20 + m.m30);
		m.m21 = 0.5f * (m.m21 + m.m31);
		m.m22 = 0.5f * (m.m22 + m.m32);
		m.m23 = 0.5f * (m.m23 + m.m33);
        return m;
    }
    // 清空指定缓存id内的信息。这里指清除ShadowMap的缓存
    public void CleanupShadowMap()
    {
        shadowBuffer.ReleaseTemporaryRT(dirShadowAtlasId);
        ExecuteBuffer();
    }
    // 执行当前缓存区内(CommandBuffer)的命令
    public void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(shadowBuffer);
        shadowBuffer.Clear();
    }
    // 储存用于渲染阴影的渲染信息: 阴影强度、
    public Vector2 ReserveDirectionalShadows(Light light, int visibleLightIndex)
    {
        // 对灯光进行筛选. 比如选出最重要的一个光源排在第一个位置上
        if (shadowedDirectionalLightCount < maxShadowedDirectionalLightCount && 
            light.shadows != LightShadows.None && light.shadowStrength > 0.0f &&
            cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b))
        {
            // 设置可见光源信息, 循环在Lighting类中
            shadowedDirectionalLights[shadowedDirectionalLightCount] = 
                new ShadowedDirectionalLight{ visibleLightIndex = visibleLightIndex };
            return new Vector2(light.shadowStrength, shadowSettings.directional.cascadeCount * shadowedDirectionalLightCount++);
        }
        return Vector2.zero;
    }
}
