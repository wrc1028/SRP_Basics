using UnityEngine;

/// <summary>
/// 平行光的ShadowMap图集的尺寸
/// </summary>
public enum TextureSize
{
    _256 = 256, _512 = 512, _1024 = 1024, 
    _2048 = 2048, _4096 = 4096, _8192 = 8192, 
}

[System.Serializable]
/// <summary>
/// 平行光ShadowMap的设置
/// </summary>
public struct Directional
{
    public TextureSize atlasSize;
    [Range(1, 4)]
    public int cascadeCount;
    [Range(0.0f, 1.0f)]
    public float cascadeRatio1, cascadeRatio2, cascadeRatio3; 
    [HideInInspector]
    public Vector3 CascadeRatios => new Vector3(cascadeRatio1, cascadeRatio2, cascadeRatio3);
}

[System.Serializable]
/// <summary>
/// 阴影设置
/// </summary>
public class ShadowSettings
{
    [Min(0.0f)]
    /// <summary>
    /// 产生联机阴影最大的距(CSM级联阴影)
    /// 链接:https://zhuanlan.zhihu.com/p/460945398
    /// </summary>
    public float maxDistance = 100.0f;
    public Directional directional = new Directional
    {
        atlasSize = TextureSize._1024, 
        cascadeCount = 4, 
        cascadeRatio1 = 0.1f,
        cascadeRatio2 = 0.25f, 
        cascadeRatio3 = 0.5f,
    };
}
