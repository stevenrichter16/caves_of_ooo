Shader "Hidden/CavesOfOoo/FlatPixelPortrait"
{
    Properties
    {
        _MainTex ("Borrowed sprite atlas", 2D) = "white" {}
        _Tint ("Linear palette tint", Vector) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Overlay" }
        Pass
        {
            ZTest Always
            ZWrite Off
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _Tint;
            struct Input { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct Output { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };
            Output vert(Input value)
            {
                Output output;
                output.vertex = UnityObjectToClipPos(value.vertex);
                output.uv = value.uv;
                return output;
            }
            float4 frag(Output value) : SV_Target
            {
                // Sampling handles the atlas's declared sRGB format. The
                // framebuffer performs output encoding; never encode twice.
                return tex2D(_MainTex, value.uv) * _Tint;
            }
            ENDCG
        }
    }
    Fallback Off
}
