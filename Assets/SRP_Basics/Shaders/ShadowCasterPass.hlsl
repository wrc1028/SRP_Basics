#ifndef CUSTOM_SHADOW_CASTER_INCLUDED
#define CUSTOM_SHADOW_CASTER_INCLUDED

#include "../ShaderLibrary/Common.hlsl"

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);

UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
    UNITY_DEFINE_INSTANCED_PROP(float4, _MainTex_ST)
    UNITY_DEFINE_INSTANCED_PROP(float4, _MainColor)
    UNITY_DEFINE_INSTANCED_PROP(float, _ClipValue)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

struct Attributes
{
    float3 positionOS : POSITION;
    float2 texcoord : TEXCOORD0;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Veryings
{
    float4 positionCS : SV_POSITION;
    float2 uv : VAR_BASE_UV;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

Veryings ShadowCasterVertex(Attributes input)
{
    Veryings output;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);

    output.positionCS = TransformObjectToHClip(input.positionOS);
    float4 tilingAndOffset = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _MainTex_ST);
    output.uv = input.texcoord * tilingAndOffset.xy + tilingAndOffset.zw;
    return output;
}

void ShadowCasterFragment(Veryings input)
{
    UNITY_SETUP_INSTANCE_ID(input);

    float4 mainTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
    float4 mainColor = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _MainColor);
    float4 baseColor = mainTex * mainColor;

    #ifdef _CLIPPING
        clip(baseColor.a - UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _ClipValue));
    #endif

}

#endif