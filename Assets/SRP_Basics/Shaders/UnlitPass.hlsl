// HLSL并没有命名空间的概念, 除了代码块的局部作用域之外, 只有一个全局作用域
// 包含文件不同于命名空间, 它会将当前区于内的全部代码插入到使用它的Shader中
// ifndef 的作用是防止多次引用代码, 造成代码重复, 导致编译错误
#ifndef CUSTOM_UNLIT_PASS_INCLUDED // 如果当前关键字没有被包含
#define CUSTOM_UNLIT_PASS_INCLUDED

#include "../ShaderLibrary/Common.hlsl"

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);

// 将使用该材质物体对应的属性(以_BaseColor为例), 放入到一个恒定内存缓冲区(CBuffer)
UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
    UNITY_DEFINE_INSTANCED_PROP(float4, _MainTex_ST)
    UNITY_DEFINE_INSTANCED_PROP(float4, _MainColor)
    UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

// 模型数据
struct Attributes
{
    float3 positionOS : POSITION;
    float2 texcoord : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

// 转换后的数据
struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 uv : VAR_BASE_UV;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings UnlitVertex(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    output.positionCS = TransformObjectToHClip(input.positionOS);
    float4 tilingAndOffset = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _MainTex_ST);
    output.uv = input.texcoord * tilingAndOffset.xy + tilingAndOffset.zw;
    return output;
}
// SV_TARGET为当前返回值的语义, 用于指明当前输出的结果是一个什么值
float4 UnlitFragment(Varyings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    float4 mainTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
    float4 mainColor = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _MainColor);
    float4 finalColor = mainTex * mainColor;
    float alphaCutValue = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Cutoff);
    #ifdef _CLIPPING 
        clip(finalColor.r - alphaCutValue);
    #endif
    return finalColor;
}
#endif