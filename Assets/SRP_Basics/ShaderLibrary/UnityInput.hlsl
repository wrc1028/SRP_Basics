#ifndef CUSTOM_UNLIT_INPUT_INCLUDED
#define CUSTOM_UNLIT_INPUT_INCLUDED

// 在渲染的时候, unity会在应用阶段将对应的变换矩阵输入到GPU的变量中
CBUFFER_START(UnityPerDraw)
    float4x4 unity_ObjectToWorld;
	float4 unity_LODFade;
    float4x4 unity_WorldToObject;
    real4 unity_WorldTransformParams;
CBUFFER_END

float3 _WorldSpaceCameraPos;
float4x4 unity_MatrixV;
float4x4 unity_MatrixVP;
float4x4 glstate_matrix_projection;

#endif