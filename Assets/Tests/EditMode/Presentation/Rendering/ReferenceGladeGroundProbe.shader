Shader "Hidden/CavesOfOoo/Tests/ReferenceGladeGroundProbe"
{
    Properties
    {
        _BaseMap ("Albedo", 2D) = "white" {}
        _FogLight ("Fog", 2D) = "black" {}
        _BaseColor ("Color", Color) = (1,1,1,1)
        _Transient ("Transient", Float) = 0
        _AmbientStrength ("Ambient", Float) = 1
        _SunStrength ("Sun", Float) = 0
        _Exposure ("Exposure", Float) = .4
        _GroundMottleStrength ("Ground mottle", Float) = 0
        _ProbeHeight ("Height", Float) = 0
        _ProbeNormal ("Normal", Vector) = (0,1,0,0)
    }
    SubShader
    {
        Pass
        {
            Cull Off ZWrite Off ZTest Always
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ProbeVertex
            #pragma fragment VillagePaletteFragment
            // This GPU probe calls the actual production fragment, not a copy
            // of its color/noise/fog logic. Only its vertex projection is isolated.
            #include "Assets/Art3D/Village/Shaders/Village3DCommon.hlsl"
            float _ProbeHeight;
            float4 _ProbeNormal;
            struct ProbeAttributes { float4 position : POSITION; float2 uv : TEXCOORD0; };
            VillageVaryings ProbeVertex(ProbeAttributes input)
            {
                VillageVaryings output = (VillageVaryings)0;
                output.positionCS = float4(input.uv * 2 - 1, 0, 1);
                output.positionWS = float3(10 + input.uv.x * 16, _ProbeHeight, 5 + input.uv.y * 16);
                output.normalWS = _ProbeNormal.xyz;
                output.uv = float2(.5,.5);
                return output;
            }
            ENDHLSL
        }
    }
}
