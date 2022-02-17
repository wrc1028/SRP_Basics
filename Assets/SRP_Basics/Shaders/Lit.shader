Shader "Custom RP/Lit"
{
    Properties
    {
        [Enum(Off, 0, On, 1)] _ZWrite ("Z Write", float) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", float) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", float) = 0
        
        [Toggle(_PREMULTIPLY_ALPHA)] _PremulAlpha ("Premul Alpha", float) = 0

        [Toggle(_CLIPPING)] _Clipping ("Clipping", float) = 0
        _ClipValue ("Clip Value", Range(0, 1)) = 0

        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _MainColor ("Color", Color) = (1, 1, 1, 1)
        _Metallic ("Metallic Ctrl", Range(0, 1)) = 0
        _Smoothness ("Smoothness Ctrl", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Pass
        {
            ZWrite [_ZWrite]
            Blend [_SrcBlend] [_DstBlend]
            Cull [_Cull]
            // 着色器标签: 可以在渲染的时候指定特定标签(Tag)的Shader进行渲染
            Tags { "LightMode" = "CustomLit" }
            HLSLPROGRAM
            #pragma target 3.5
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling
            #pragma vertex LitVertex
            #pragma fragment LitFragment
            #pragma shader_feature _CLIPPING
            #pragma shader_feature _PREMULTIPLY_ALPHA
            #include "LitPass.hlsl"
            ENDHLSL
        }
    }
}