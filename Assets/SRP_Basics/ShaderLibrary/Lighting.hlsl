#ifndef CUSTOM_LIGHTING_INCLUDED
#define CUSTOM_LIGHTING_INCLUDED
// 光照计算
float3 GetLighting(Surface surface)
{
    return surface.normal.y;
}
#endif