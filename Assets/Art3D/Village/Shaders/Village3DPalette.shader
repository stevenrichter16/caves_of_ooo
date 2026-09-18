Shader "CavesOfOoo/Village3D/Palette"
{
    Properties
    {
        _BaseMap ("Palette / Albedo", 2D) = "white" {}
        [HideInInspector] [NoScaleOffset] _FogLight ("Native Visibility / Local Light", 2D) = "black" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _Transient ("Visible-Only Owner", Float) = 0
        _AmbientStrength ("Ambient Strength", Range(0,2)) = 0.7
        _SunStrength ("Sun Strength", Range(0,2)) = 0.9
        _Exposure ("Scene Exposure", Range(0,4)) = 1
        _WaveSpeed ("Water Ripple Speed", Float) = 0.8
        _WaveStrength ("Water Ripple Normal Strength", Range(0,0.4)) = 0.09
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Cull Back
        ZWrite On
        ZTest LEqual
        HLSLINCLUDE
        #include "Village3DCommon.hlsl"
        ENDHLSL
        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForwardOnly" }
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex VillageVertex
            #pragma fragment VillagePaletteFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ColorMask 0
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex VillageShadowVertex
            #pragma fragment VillageShadowFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ColorMask 0
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex VillageVertex
            #pragma fragment VillageDepthFragment
            #pragma multi_compile_instancing
            ENDHLSL
        }
    }
    Fallback Off
}
