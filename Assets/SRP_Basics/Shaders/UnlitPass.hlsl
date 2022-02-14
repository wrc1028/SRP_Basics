// HLSL并没有命名空间的概念, 除了代码块的局部作用域之外, 只有一个全局作用域
// 包含文件不同于命名空间, 它会将当前区于内的全部代码插入到使用它的Shader中
// ifndef 的作用是防止多次引用代码, 造成代码重复, 导致编译错误
#ifndef CUSTOM_UNLIT_PASS_INCLUDED // 如果当前关键字没有被包含
#define CUSTOM_UNLIT_PASS_INCLUDED

#include "../ShaderLibrary/Common.hlsl"

// 将使用该材质物体对应的属性(以_BaseColor为例), 放入到一个恒定内存缓冲区(CBuffer)
CBUFFER_START(UnityPerMaterial)
    float4 _BaseColor;
CBUFFER_END

float4 UnlitVertex(float3 positionOS : POSITION) : SV_POSITION
{
    return TransformObjectToHClip(positionOS);
}
// SV_TARGET为当前返回值的语义, 用于指明当前输出的结果是一个什么值
float4 UnlitFragment() : SV_TARGET
{
    return _BaseColor;
}

#endif