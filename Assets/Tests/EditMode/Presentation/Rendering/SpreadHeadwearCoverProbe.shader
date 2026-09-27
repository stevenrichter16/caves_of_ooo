Shader "Hidden/CavesOfOoo/Tests/SpreadHeadwearCoverProbe"
{
    Properties
    {
        _BaseMap("Albedo",2D)="white"{}
        _FogLight("Fog",2D)="black"{}
        _BaseColor("Color",Color)=(1,1,1,1)
        _Transient("Transient",Float)=1
        _AmbientStrength("Ambient",Float)=1
        _SunStrength("Sun",Float)=0
        _Exposure("Exposure",Float)=.4
        _CoverHeadwear("Covered cosmetic",Float)=0
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        HLSLINCLUDE
        #include "Assets/Art3D/Village/Shaders/Village3DCommon.hlsl"
        half4 ProbeShadow(VillageVaryings input):SV_Target{VillageShadowFragment(input);return half4(1,1,1,1);}
        half4 ProbeDepth(VillageVaryings input):SV_Target{VillageDepthFragment(input);return half4(1,1,1,1);}
        ENDHLSL
        Pass
        {
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex VillageVertex
            #pragma fragment VillagePaletteFragment
            ENDHLSL
        }
        Pass
        {
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex VillageVertex
            #pragma fragment ProbeShadow
            ENDHLSL
        }
        Pass
        {
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex VillageVertex
            #pragma fragment ProbeDepth
            ENDHLSL
        }
    }
}
