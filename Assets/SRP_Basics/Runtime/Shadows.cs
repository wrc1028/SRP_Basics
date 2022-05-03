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
    private const int maxShadowedDirectionalLightCount = 4, maxCascades = 4;  
    private int shadowedDirectionalLightCount;
    private struct ShadowedDirectionalLight
    {
        public int visibleLightIndex;
    }
    private ShadowedDirectionalLight[] shadowedDirectionalLights = 
        new ShadowedDirectionalLight[maxShadowedDirectionalLightCount];
    //===================shader prop id===================
    private static int dirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas");
    private static int dirShadowMatricesId = Shader.PropertyToID("_DirectionalShadowMatrices");
    private static int cascadeCountId = Shader.PropertyToID("_CascadeCount");
    private static int cascadeCullingSpheresId = Shader.PropertyToID("_CascadeCullingSpheres");
    private static Matrix4x4[] dirShadowMatrices = new Matrix4x4[maxShadowedDirectionalLightCount * maxCascades];
    private static Vector4[] cascadeCullingSpheres = new Vector4[maxCascades];

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

        int tiles = shadowedDirectionalLightCount * shadowSettings.directional.cascadeCount;
        int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
        int tileSize = atlasSize / split;
        for (int i = 0; i < shadowedDirectionalLightCount; i++)
        {
            RenderDirectionalShadows(i, split, tileSize);
        }
        shadowBuffer.SetGlobalInt(cascadeCountId, shadowSettings.directional.cascadeCount);
        shadowBuffer.SetGlobalVectorArray(cascadeCullingSpheresId, cascadeCullingSpheres);
        shadowBuffer.SetGlobalMatrixArray(dirShadowMatricesId, dirShadowMatrices);
        shadowBuffer.EndSample(bufferName);
        ExecuteBuffer();
    }
    /// <summary>
    /// 渲染单个支持产生阴影灯光的ShadowMap
    /// </summary>
    /// <param name="index">支持阴影的灯光索引</param>
    /// <param name="split">将ShadowMap分成几份</param>
    /// <param name="tileSize">每个灯光ShadowMap的尺寸</param>
    private void RenderDirectionalShadows(int index, int split, int tileSize)
    {
        // 首先获得阴影的绘制方式(设置)
        ShadowedDirectionalLight light = shadowedDirectionalLights[index];
        ShadowDrawingSettings shadowDrawingSettings = new ShadowDrawingSettings(cullingResults, light.visibleLightIndex);
        int cascadeCount = shadowSettings.directional.cascadeCount;
        int tileStart = index * cascadeCount;
        Vector3 cascadeRatios = shadowSettings.directional.CascadeRatios;
        for (int i = 0; i < cascadeCount; i++)
        {
            cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives
            (
                light.visibleLightIndex, i, cascadeCount, cascadeRatios, tileSize, 0.0f, 
                out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData splitData
            );
            // 将计算阴影的参数传入Buffer
            shadowDrawingSettings.splitData = splitData;
            if (index == 0)
            {
                Vector4 cullingSphere = splitData.cullingSphere;
                cullingSphere.w *= cullingSphere.w;
                cascadeCullingSpheres[i] = cullingSphere;
            }
            int tileIndex = tileStart + i;
            dirShadowMatrices[tileIndex] = ConvertToAtlasMatrix(projectionMatrix * viewMatrix, SetTileViewport(tileIndex, split, tileSize), split);
            shadowBuffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
            ExecuteBuffer();
            context.DrawShadows(ref shadowDrawingSettings);
        }
    }
    /// <summary>
    /// 将各光源的ShadowMap进行位移排序, 集成到一张ShadowMap中
    /// </summary>
    /// <param name="index">ShadowMap索引</param>
    /// <param name="split">拆分数量</param>
    private Vector2 SetTileViewport(int index, int split, float tileSize)
    {
        Vector2 offset = new Vector2(index % split, index / split);
        shadowBuffer.SetViewport(new Rect(offset.x * tileSize, offset.y * tileSize, tileSize, tileSize));
        return offset;
    }
    /// <summary>
    /// 将世界空间的视角投影矩阵转换到Tile Space, 也就是计算Shadow是所用到的将世界空间坐标转化到ShadowMap相机空间的矩阵?
    /// </summary>
    /// <param name="m">视角投影矩阵转</param>
    /// <param name="offset">ShadowMap偏移</param>
    /// <param name="split">拆分数量</param>
    /// <returns></returns>
    private Matrix4x4 ConvertToAtlasMatrix(Matrix4x4 m, Vector2 offset, int split)
    {
        // 因为深度缓存器的精度收到限制, 并且所储存的深度信息是非线性的, 所以通过反转深度值可以更好的利用位信息???
        if (SystemInfo.usesReversedZBuffer)
        {
            m.m20 = -m.m20;
            m.m21 = -m.m21;
            m.m22 = -m.m22;
            m.m23 = -m.m23;
        }
        // 因为在绘制ShadowMap的时候, 是在视锥体定义的某一个Cube区域内, 他的一个区域范围是-1到1, 但是存储ShadowMap的RT的坐标及深度是0到1的, 
        // 因此需要将矩阵进行限制, 加上多光源的情况, 每个光源产生的ShadowMap是通过传进来的offset进行偏移, 因此也需要考虑这一因素
        float scale = 1.0f / split;
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
    /// <returns>x:当前光源阴影的强度; y:能产生阴影灯光的索引</returns>
    public Vector2 ReserveDirectionalShadows(Light light, int visibleLightIndex)
    {
        if (shadowedDirectionalLightCount < maxShadowedDirectionalLightCount &&
            light.shadows != LightShadows.None && light.shadowStrength > 0.0f && 
            cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds outBounds))
        {
            shadowedDirectionalLights[shadowedDirectionalLightCount] =  new ShadowedDirectionalLight
            {
                visibleLightIndex = visibleLightIndex,
            };
            return new Vector2(light.shadowStrength, shadowSettings.directional.cascadeCount * shadowedDirectionalLightCount++);
        }
        return Vector2.zero;
    }

    private void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(shadowBuffer);
        shadowBuffer.Clear();
    }

}