Shader "Custom RP/Unlit"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Pass
        {
            HLSLPROGRAM
            #pragma multi_compile_instancing
            #pragma vertex UnlitVertex
            #pragma fragment UnlitFragment
            #include "UnlitPass.hlsl"
            ENDHLSL
        }
    }
}
