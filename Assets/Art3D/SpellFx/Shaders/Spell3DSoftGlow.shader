Shader "CavesOfOoo/Spell3D/SoftGlow"
{
    Properties
    {
        _BaseMap ("Unused Albedo", 2D) = "white" {}
        [HideInInspector][NoScaleOffset] _FogLight ("Native Visibility", 2D) = "black" {}
        _BaseColor ("Linear Authored Glow Color", Color) = (1,1,1,1)
        _Emission ("Local Glow Strength", Range(0,1)) = 0
        _Transient ("Visible Only", Float) = 1
        _AmbientStrength ("Unused Ambient", Float) = 0
        _SunStrength ("Unused Sun", Float) = 0
        _Exposure ("Unused Exposure", Float) = 1
        _WaveSpeed ("Unused Wave Speed", Float) = 0
        _WaveStrength ("Unused Wave Strength", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Cull Off
        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha One, Zero One
        Pass
        {
            Name "LocalGlow"
            Tags { "LightMode"="UniversalForwardOnly" }
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex SpellGlowVertex
            #pragma fragment SpellGlowFragment
            #pragma multi_compile_instancing
            #include "../../Village/Shaders/Village3DCommon.hlsl"
            float _Emission;
            struct GlowAttributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct GlowVaryings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half alpha : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
            GlowVaryings SpellGlowVertex(GlowAttributes input)
            {
                GlowVaryings output = (GlowVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input,output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = position.positionCS;
                output.positionWS = position.positionWS;
                output.alpha = input.color.a;
                return output;
            }
            half4 SpellGlowFragment(GlowVaryings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                VillageVisibleFog(input.positionWS);
                return half4(_BaseColor.rgb,saturate(input.alpha)*saturate(_Emission)*saturate(_BaseColor.a));
            }
            ENDHLSL
        }
    }
    Fallback Off
}
