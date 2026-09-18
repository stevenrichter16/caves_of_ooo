Shader "CavesOfOoo/Spell3D/Luminous"
{
    Properties
    {
        _BaseMap ("Albedo", 2D) = "white" {}
        [HideInInspector][NoScaleOffset] _FogLight ("Native Visibility", 2D) = "black" {}
        _BaseColor ("Linear Authored Color", Color) = (1,1,1,1)
        _Emission ("Self Light Floor", Range(0,1)) = 0
        _Transient ("Visible Only", Float) = 1
        _AmbientStrength ("Ambient Strength", Float) = .7
        _SunStrength ("Sun Strength", Float) = .9
        _Exposure ("Exposure", Float) = 1
        _WaveSpeed ("Unused Wave Speed", Float) = 0
        _WaveStrength ("Unused Wave Strength", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Cull Back
        ZWrite On
        ZTest LEqual
        HLSLINCLUDE
        // Borrow the exact physical-cell visibility contract, not scene materials.
        #include "../../Village/Shaders/Village3DCommon.hlsl"
        float _Emission;
        half4 SpellLuminousFragment(VillageVaryings input) : SV_Target
        {
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            half4 fog = VillageVisibleFog(input.positionWS);
            half3 albedo = VillageAlbedo(input.uv);
            half3 lit = VillageLitColor(albedo,input.positionWS,input.normalWS,fog.rgb);
            // Emission remains local to the authored surface, with a slight facet
            // response. It never changes native light/FOV or post processing.
            half facet = .8h + .2h * saturate(dot(normalize(input.normalWS),normalize(half3(-.4h,.85h,-.3h))));
            return half4(max(lit,albedo*saturate(_Emission)*facet),1);
        }
        ENDHLSL
        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForwardOnly" }
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex VillageVertex
            #pragma fragment SpellLuminousFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
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
