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
    return (1.0 - MinReflectivity) * (1.0 - metaiillc);
}

float Square(float value)
{
    return value * value;
}

BRDF GetBRDF(Surface surface)
{
    BRDF brdf;
    float oneMinusReflectivity = OneMinusReflectivity(surface.metallic);
    brdf.diffuse = surface.color * oneMinusReflectivity;
    // brdf.specular = surface.color - brdf.diffuse;
    brdf.specular = lerp(MinReflectivity, surface.color, surface.metallic);
    // 这样处理能够让调整光滑度值时, 效果变化更加直观
    float perceptualRoughness = 1.0 - surface.smoothness;
    brdf.roughness = perceptualRoughness * perceptualRoughness;
    return brdf;
}



#endif