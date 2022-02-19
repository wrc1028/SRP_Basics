#ifndef CUSTOM_LIGHTING_INCLUDED
#define CUSTOM_LIGHTING_INCLUDED
// 光照计算
float3 CalculateLight(Surface surface, BRDF brdf, Light light)
{
    float NdotL = saturate(dot(surface.normal, light.direction));
    return NdotL * light.color * DirectBRDF(surface, brdf, light) * light.attenuation;
}

float3 GetLighting(Surface surfaceWS, BRDF brdf)
{
    ShadowData shadowData = GetShadowData(surfaceWS);
    float3 color = 0.0;
    // URP单一Pass处理多光源的方式
    for (int i = 0; i < GetDirectionalLightCount(); i++)
    {
        Light light = GetDirectionalLight(i, surfaceWS, shadowData);
        color += CalculateLight(surfaceWS, brdf, light) * surfaceWS.color;
    }
    return color;
}
#endif