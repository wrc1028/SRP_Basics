#ifndef CUSTOM_LIGHTING_INCLUDED
#define CUSTOM_LIGHTING_INCLUDED
// 光照计算
float3 CalculateLight(Surface surface, BRDF brdf, Light light)
{
    float NdotL = saturate(dot(surface.normal, light.direction));
    return NdotL * light.color * brdf.diffuse;
}

float3 GetLighting(Surface surface, BRDF brdf)
{
    float3 color = 0.0;
    // URP单一Pass处理多光源的方式
    for (int i = 0; i < GetDirectionalLightCount(); i++)
    {
        color += CalculateLight(surface, brdf, GetDirectionalLight(i)) * surface.color;
    }
    return color;
}
#endif