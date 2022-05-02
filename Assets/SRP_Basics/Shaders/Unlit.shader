Shader "Custom RP/Unlit"
{
    Properties
    {
        _MainTex ("Mian Tex", 2D) = "white" {}
        _MainColor ("Main Color", Color) = (1, 1, 1, 1)

        [Header(BlendMode)]
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", float) = 0
        [Enum(Off, 0, On, 1)] _ZWrite ("Z Write", float) = 1

        [Header(Clip)]
        [Enum(UnityEngine.Rendering.CullMode)] _CullMode ("Cull Mode", float) = 0
        [Toggle(_CLIPPING)] _Clipping ("Alpha Clipping", float) = 0
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0
    }
    SubShader
    {
        Pass
        {
            
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull [_CullMode]

            HLSLPROGRAM
            #pragma target 3.5
            #pragma multi_compile_instancing
            #pragma vertex UnlitVertex
            #pragma fragment UnlitFragment
            // It will generate one or both variants, depending on how we configured our materials.
            #pragma shader_feature _CLIPPING

            #include "UnlitPass.hlsl"
            ENDHLSL
        }
    }
}
