using UnityEngine;

public enum TextureSize { _256 = 256, _512 = 512, _1024 = 1024, _2048 = 2048, _4096 = 4096, _8192 = 8192, }

[System.Serializable]
public struct Directional
{
    public TextureSize atlasSize;
    [Range(1, 4)]
    public int cascadeCount;
    [Range(0.0f, 1.0f)]
    public float cascadeRatio1, cascadeRatio2, cascadeRatio3;
    public Vector3 cascadeRatios => new Vector3(cascadeRatio1, cascadeRatio2, cascadeRatio3);
    [Range(0.001f, 1.0f)]
    public float cascadeFade;
}

[System.Serializable]
public class ShadowSettings
{
    // 产生ShadowMap的相机距离主相机的最大距离
    [Min(0.0001f)]
    public float maxDistance = 100.0f;
    [Range(0.0001f, 1f)]
    public float distanceFade = 0.1f;
    public Directional directional = new Directional
    {
        atlasSize = TextureSize._1024,
        cascadeCount = 4,
        cascadeRatio1 = 0.1f,
        cascadeRatio2 = 0.25f,
        cascadeRatio3 = 0.5f,
        cascadeFade = 0.1f
    };
    
}
