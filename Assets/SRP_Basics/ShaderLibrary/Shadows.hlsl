# ifndef CUSTOM_SHADOWS_INCLUDED
# define CUSTOM_SHADOWS_INCLUDED

# define MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT 4
# define MAX_CASCADE_COUNT 4

struct DirectionalShadowData
{
    float strength;
    int tileIndex;
};

TEXTURE2D_SHADOW(_DirectionalShadowAtlas);
# define SHADOW_SAMPLER sampler_linear_clamp_compare
SAMPLER_CMP(SHADOW_SAMPLER);

CBUFFER_START(_CustomShadows)
    float4x4 _DirectionalShadowMatrices[MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT * MAX_CASCADE_COUNT];
    float4 _CascadeCullingSpheres[MAX_CASCADE_COUNT];
    int _CascadeCount;
CBUFFER_END

struct ShadowData
{
    int cascadeIndex;
    float strength;
};

ShadowData GetShadowData (Surface surfaceWS)
{
    ShadowData data;
    data.strength = 1.0;
    // 通过计算点到CullSphere中心的距离, 判断当前点在级联阴影的哪一等级上
    int i;
    for (i = 0; i < _CascadeCount; i++)
    {
        float4 sphere = _CascadeCullingSpheres[i];
        float distanceSqr = DistanceSquared(surfaceWS.position, sphere.xyz);
        if (distanceSqr < sphere.w) break;
    }
    // 超出级联范围也就是最大范围之外的物体, 其收到的阴影强度设置为0
    if (i == _CascadeCount) data.strength = 0.0;
    data.cascadeIndex = i;
    return data;
}

float SampleDirectionalShadowAtlas (float3 positionSTS)
{
    return SAMPLE_TEXTURE2D_SHADOW(_DirectionalShadowAtlas, SHADOW_SAMPLER, positionSTS);
}

float GetDirectionalShadowAttenuation (DirectionalShadowData data, Surface surfaceWS)
{
    if (data.strength <= 0) return 1.0;
    float3 positionSTS = mul(_DirectionalShadowMatrices[data.tileIndex], float4(surfaceWS.position, 1)).xyz;
    float shadow = SampleDirectionalShadowAtlas(positionSTS);
    return lerp(1.0, shadow, data.strength);
}

#endif