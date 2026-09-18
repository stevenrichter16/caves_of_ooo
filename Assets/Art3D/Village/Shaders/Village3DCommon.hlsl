#ifndef CAVES_OF_OOO_VILLAGE3D_COMMON_INCLUDED
#define CAVES_OF_OOO_VILLAGE3D_COMMON_INCLUDED
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);
// Bound on each presenter-owned material clone; never a shared global mask.
TEXTURE2D(_FogLight);
SAMPLER(sampler_FogLight);
CBUFFER_START(UnityPerMaterial)
float4 _BaseMap_ST;
half4 _BaseColor;
float _Transient;
float _AmbientStrength;
float _SunStrength;
float _Exposure;
float _WaveSpeed;
float _WaveStrength;
CBUFFER_END

float3 _LightDirection;
float3 _LightPosition;
struct VillageAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float2 uv : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};
struct VillageVaryings
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : TEXCOORD0;
    half3 normalWS : TEXCOORD1;
    float2 uv : TEXCOORD2;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};
VillageVaryings VillageVertex(VillageAttributes input)
{
    VillageVaryings output = (VillageVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
    output.positionCS = position.positionCS;
    output.positionWS = position.positionWS;
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
    return output;
}
half4 VillageFog(float3 positionWS)
{
    // Query the actual bound resource. A missing/default/wrong-size texture must
    // not silently reveal all owners; a caller-provided size uniform is insufficient.
    uint width, height;
    _FogLight.GetDimensions(width, height);
    clip(width == 80u && height == 25u ? 1.0 : -1.0);
    float2 cell = floor(positionWS.xz);
    clip(cell);
    clip(float2(79.0, 24.0) - cell);
    // Runtime upload index is (24-nativeY)*80+x, so world Z matches texture row.
    float2 uv = (cell + 0.5) / float2(80.0, 25.0);
    return SAMPLE_TEXTURE2D(_FogLight, sampler_FogLight, uv);
}
half4 VillageVisibleFog(float3 positionWS)
{
    half4 fog = VillageFog(positionWS);
    clip(fog.a - (_Transient > 0.5 ? 0.75h : 0.01h));
    return fog;
}
half3 VillageAlbedo(float2 uv)
{
    return SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).rgb * _BaseColor.rgb * max(0,_Exposure);
}
half3 VillageLitColor(half3 albedo, float3 positionWS, half3 normalWS, half3 localLight)
{
    half3 normal = normalize(normalWS);
    Light sun = GetMainLight(TransformWorldToShadowCoord(positionWS), positionWS, half4(1,1,1,1));
    half3 ambient = SampleSH(normal) * _AmbientStrength;
    half3 direct = sun.color * (saturate(dot(normal, sun.direction)) * _SunStrength
        * sun.distanceAttenuation * sun.shadowAttenuation);
    return albedo * (ambient + direct) * saturate(localLight);
}
half4 VillagePaletteFragment(VillageVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    half4 fog = VillageVisibleFog(input.positionWS);
    half3 albedo = VillageAlbedo(input.uv);
    // Native remembered RGB already contains the saved-cell ambient brightness.
    // Do not apply live directional lights, shadows or animation again.
    if (fog.a <= 0.75h) return half4(albedo * fog.rgb, 1);
    return half4(VillageLitColor(albedo, input.positionWS, input.normalWS, fog.rgb), 1);
}
half4 VillageWaterFragment(VillageVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    half4 fog = VillageVisibleFog(input.positionWS);
    half3 albedo = VillageAlbedo(input.uv);
    if (fog.a <= 0.75h) return half4(albedo * fog.rgb, 1);
    // Opaque color/normal ripples only: no scene-color/reflection/depth sampling
    // and no vertex displacement that could diverge from depth/shadow geometry.
    float phase = _Time.y * _WaveSpeed;
    float rippleX = sin(input.positionWS.x * 3.1 + input.positionWS.z * 1.7 + phase);
    float rippleZ = cos(input.positionWS.z * 2.7 - input.positionWS.x * 1.3 + phase * 0.73);
    half3 normal = normalize(input.normalWS + half3(rippleX, 0, rippleZ) * _WaveStrength);
    half3 color = VillageLitColor(albedo, input.positionWS, normal, fog.rgb);
    return half4(color, 1);
}
VillageVaryings VillageShadowVertex(VillageAttributes input)
{
    VillageVaryings output = VillageVertex(input);
#if _CASTING_PUNCTUAL_LIGHT_SHADOW
    float3 lightDirectionWS = normalize(_LightPosition - output.positionWS);
#else
    float3 lightDirectionWS = _LightDirection;
#endif
    output.positionCS = TransformWorldToHClip(ApplyShadowBias(output.positionWS,
        normalize(output.normalWS), lightDirectionWS));
    output.positionCS = ApplyShadowClamping(output.positionCS);
    // positionWS stays unbiased for the authoritative native cell lookup.
    return output;
}
half4 VillageShadowFragment(VillageVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    half4 fog = VillageFog(input.positionWS);
    clip(fog.a - 0.75h); // remembered owners cannot cast information-leaking shadows
    return 0;
}
half4 VillageDepthFragment(VillageVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    VillageVisibleFog(input.positionWS); // identical visibility to the color pass
    return 0;
}
#endif
