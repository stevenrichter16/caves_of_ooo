Shader "CavesOfOoo/MorrowfastScene"
{
    Properties
    {
        [PerRendererData] _MainTex ("Source component", 2D) = "white" {}
        _SceneFog ("Native cell visibility", 2D) = "black" {}
        _OwnerFog ("Use owner, visibility", Vector) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "CanUseSpriteAtlas"="True" }
        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_SceneFog); SAMPLER(sampler_SceneFog);
            float4 _OwnerFog;
            struct Attributes { float3 positionOS:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float2 fogUV:TEXCOORD1; float4 color:COLOR; };
            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 world=TransformObjectToWorld(input.positionOS);
                output.positionCS=TransformWorldToHClip(world);
                output.uv=input.uv;
                output.fogUV=float2(world.x/80.0,world.y/25.0);
                output.color=input.color;
                return output;
            }
            half4 frag(Varyings input):SV_Target
            {
                half4 color=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,input.uv)*input.color;
                half fog=_OwnerFog.x>.5?_OwnerFog.y:SAMPLE_TEXTURE2D(_SceneFog,sampler_SceneFog,input.fogUV).r;
                clip(color.a-.001);clip(fog-.001);color.rgb*=fog;return color;
            }
            ENDHLSL
        }
    }
}
