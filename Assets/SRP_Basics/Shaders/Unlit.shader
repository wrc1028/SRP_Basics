Shader "Custom RP/Unlit"
{
    Properties
    {

    }
    SubShader
    {
        Pass
        {
            HLSLPROGRAM
            #pragma vertex UnlitVertex
            #pragma fragment UnlitFragment
            #include "UnlitPass.hlsl"
            ENDHLSL
        }
    }
}
