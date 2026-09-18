Shader "CavesOfOoo/Village3D/Composite"
{
    Properties
    {
        _MainTex ("Village World RenderTexture", 2D) = "black" {}
        _FlipY ("Explicit RenderTexture Y Flip", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Transparent" }
        Cull Off
        ZWrite Off
        ZTest Always
        Blend One Zero
        Pass
        {
            Name "VillageComposite"
            // Renderer2D does not select UniversalForward passes.
            Tags { "LightMode"="Universal2D" }
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex CompositeVertex
            #pragma fragment CompositeFragment
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float _FlipY;
            CBUFFER_END
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            Varyings CompositeVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                // Ordinary mesh UVs. Neither platform macro nor fullscreen-blit
                // helper is allowed to add a second implicit flip here.
                output.uv.y = lerp(output.uv.y, 1.0 - output.uv.y, step(0.5, _FlipY));
                return output;
            }
            half4 CompositeFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return half4(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).rgb, 1);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
