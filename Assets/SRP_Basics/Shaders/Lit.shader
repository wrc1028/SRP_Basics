Shader "Custom RP/Lit"
{
    Properties
    {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _MainColor ("Color", Color) = (1, 1, 1, 1)
        _Metallic ("Metallic Ctrl", Range(0, 1)) = 0
        _Smoothness ("Smoothness Ctrl", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Pass
        {
            // 着色器标签: 可以在渲染的时候指定特定标签(Tag)的Shader进行渲染
            Tags { "LightMode" = "CustomLit" }
            HLSLPROGRAM
            #pragma target 3.5
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling
            #pragma vertex LitVertex
            #pragma fragment LitFragment
            #include "LitPass.hlsl"
            ENDHLSL
        }
    }
}