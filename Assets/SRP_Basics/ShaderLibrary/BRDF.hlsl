#ifndef CUSTOM_BRDF_INCLUDED
#define CUSTOM_BRDF_INCLUDED

#define MinReflectivity 0.04

struct BRDF
{
    float3 diffuse;     // 漫反射
    float3 specular;    // 高光反射
    float roughness;    // 粗糙度
};

// 根据金属度的值获得当前表面的反射率
float OneMinusReflectivity(float metaiillc)
{
    // 反射率越高, 材质更倾向于镜面反射; 反之漫反射
    // 当前返回的系数(0.0 ~ 0.96)为漫反射颜色在辐照度颜色中的占比
    // 当金属都为0时(也就是当前材质为非金属), 其结果为0.96
    // 这是因为任何物体都或多或少有反射率, 如果返回1, 那么在计算光照时就会不符合能量守恒定律
    float range = 1.0 - MinReflectivity;
    return range - metaiillc * range;
}
// 五次方
float Pow5(float value)
{
    return value * value * value * value * value;
}
float3 DisneyDiffuse(Surface surface, BRDF brdf, Light light)
{
    float NdotV = saturate(dot(surface.normal, surface.viewDirection));
    float NdotL = saturate(dot(surface.normal, light.direction));
    float3 halfDir = normalize(surface.viewDirection + light.direction);
    float LdotH = saturate(dot(light.direction, halfDir));
    float FD90 = 0.5 + 2 * LdotH * LdotH * brdf.roughness;
    float FdV = 1 + (FD90 - 1) * Pow5(clamp(1 - NdotV, 0, 1));
    float FdL = 1 + (FD90 - 1) * Pow5(clamp(1 - NdotL, 0, 1));
    return brdf.diffuse * FdV * FdL;
    return 0;
}
// 平方
float Square(float value)
{
    return value * value;
}

// (r^2) / (d^2 * max(0.1, (L · H) ^2) * n)
// d = (N · H) ^ 2 * (r ^ 2 - 1) + 1.00001
// n = 4 * r + 2
float SpecularStrength(Surface surface, BRDF brdf, Light light)
{
    float3 halfDir = SafeNormalize(surface.viewDirection + light.direction);
    float NdotH2 = Square(saturate(dot(surface.normal, halfDir)));
    float LdotH2 = Square(saturate(dot(light.direction, halfDir)));
    float r2 = Square(brdf.roughness);
    float d2 = Square(NdotH2 * (r2 - 1) + 1.00001);
    float n = 4 * brdf.roughness + 2.0;
    return r2 / (d2 * max(0.1, LdotH2) * n);
}

float3 DirectBRDF(Surface surface, BRDF brdf, Light light)
{
    return DisneyDiffuse(surface, brdf, light) + SpecularStrength(surface, brdf, light) * brdf.specular;
}

BRDF GetBRDF(Surface surface, bool applyAlphaToDiffuse = false)
{
    BRDF brdf;
    float oneMinusReflectivity = OneMinusReflectivity(surface.metallic);
    brdf.diffuse = surface.color * oneMinusReflectivity;
    if (applyAlphaToDiffuse) brdf.diffuse *= surface.alpha;
    // brdf.specular = surface.color - brdf.diffuse;
    brdf.specular = lerp(MinReflectivity, surface.color, surface.metallic);
    // 这样处理能够让调整光滑度值时, 效果变化更加直观(经验模型)
    // float perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surface.smoothness);
	// brdf.roughness = PerceptualRoughnessToRoughness(perceptualRoughness);
    float perceptualRoughness = 1.0 - surface.smoothness;
    brdf.roughness = perceptualRoughness * perceptualRoughness;
    return brdf;
}



#endif