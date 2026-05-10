// Pass 5 — Animated Environment shader.
// Custom URP 2D Unlit shader that takes the existing CP437 glyph
// sprite + vertex color (already lighting-multiplied by ZoneRenderer's
// LightMap), and applies one or more time-driven animations:
//
//   - SWAY: vertex displacement based on sin(time * frequency +
//     worldPos.x * variancePerCell). Top of sprite moves more than
//     base (height-weighted) so grass blades sway from a fixed root.
//
//   - SCROLL: UV offset over time. Useful for water glyphs where
//     the texture appears to drift in a direction.
//
//   - FLICKER: brightness multiplier oscillates. Useful for fire
//     glyphs (in addition to the already-shipped Light2DFlicker
//     which modulates the Light2D source's intensity).
//
// All three can be toggled per-material via shader keywords:
//   _ENABLE_SWAY, _ENABLE_SCROLL, _ENABLE_FLICKER
//
// Material parameters tune the magnitudes:
//   _SwayAmount, _SwayFrequency, _ScrollSpeedX, _ScrollSpeedY,
//   _FlickerAmount, _FlickerFrequency
//
// The shader does NOT receive Light2D contributions itself — the
// vertex color (from Tilemap.SetColor) is already pre-multiplied
// by ZoneRenderer's software lightmap, and that gives identical
// brightness to the rest of the tilemap's foreground.
//
// See Docs/GRAPHICS-PASS5.md §5A.1 for design rationale.
Shader "CavesOfOoo/AnimatedEnvironment"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)

        [Toggle(_ENABLE_SWAY)] _EnableSway ("Enable Sway", Float) = 0
        _SwayAmount ("Sway Amount (world units)", Range(0, 0.3)) = 0.06
        _SwayFrequency ("Sway Frequency (Hz)", Range(0.1, 8)) = 1.5
        _SwayPhasePerCell ("Sway Phase Variance Per Cell", Range(0, 5)) = 0.7

        [Toggle(_ENABLE_SCROLL)] _EnableScroll ("Enable UV Scroll", Float) = 0
        _ScrollSpeedX ("Scroll Speed X (UV / sec)", Range(-2, 2)) = 0.3
        _ScrollSpeedY ("Scroll Speed Y (UV / sec)", Range(-2, 2)) = 0.05

        [Toggle(_ENABLE_FLICKER)] _EnableFlicker ("Enable Flicker", Float) = 0
        _FlickerAmount ("Flicker Amount (0=off 1=full)", Range(0, 1)) = 0.2
        _FlickerFrequency ("Flicker Frequency (Hz)", Range(1, 20)) = 8

        // Sprite renderer needs alpha clip + standard sprite setup
        _AlphaClip ("Alpha Clip Threshold", Range(0, 1)) = 0.1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _ENABLE_SWAY
            #pragma shader_feature_local _ENABLE_SCROLL
            #pragma shader_feature_local _ENABLE_FLICKER

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float _SwayAmount;
                float _SwayFrequency;
                float _SwayPhasePerCell;
                float _ScrollSpeedX;
                float _ScrollSpeedY;
                float _FlickerAmount;
                float _FlickerFrequency;
                float _AlphaClip;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float spriteV : TEXCOORD1; // for height-weighted sway
                float worldX : TEXCOORD2;  // for per-cell phase
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 posOS = IN.positionOS;

                // Convert to world space for per-cell phase + sway.
                float3 worldPos = TransformObjectToWorld(posOS);

                #ifdef _ENABLE_SWAY
                {
                    // Height weight: lower vertices (V near 0) don't sway,
                    // upper vertices (V near 1) sway full amount. So grass
                    // blades pivot from their base rather than translating.
                    float heightWeight = saturate(IN.uv.y);
                    // Per-cell phase desync via worldPos.x — neighboring
                    // cells sway out-of-phase so the grass field doesn't
                    // shimmy as one solid block.
                    float phase = worldPos.x * _SwayPhasePerCell;
                    float swayOffset = sin(_Time.y * _SwayFrequency + phase) * _SwayAmount * heightWeight;
                    posOS.x += swayOffset;
                }
                #endif

                OUT.positionHCS = TransformObjectToHClip(posOS);
                OUT.color = IN.color * _Color;
                // IMPORTANT: pass the RAW sprite-local UV (in [0, 1]
                // range), NOT the post-TRANSFORM_TEX atlas UV.
                // The fragment shader applies scroll in sprite-local
                // space (with frac() wrapping inside the glyph), then
                // maps to the atlas sub-rect via TRANSFORM_TEX. If we
                // applied scroll AFTER TRANSFORM_TEX, frac() would
                // wrap across the entire CP437 atlas — scrolling
                // through every glyph instead of within one.
                OUT.uv = IN.uv;
                OUT.spriteV = IN.uv.y;
                OUT.worldX = worldPos.x;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;

                #ifdef _ENABLE_SCROLL
                {
                    // Constant-velocity UV scroll IN SPRITE-LOCAL SPACE.
                    // frac() wraps within [0, 1] of THIS glyph. Then
                    // TRANSFORM_TEX maps to the atlas sub-rect.
                    uv.x = frac(uv.x + _Time.y * _ScrollSpeedX);
                    uv.y = frac(uv.y + _Time.y * _ScrollSpeedY);
                }
                #endif

                // NOW apply atlas mapping: convert sprite-local [0, 1]
                // to atlas UV via TRANSFORM_TEX equivalent (scale + offset).
                float2 atlasUV = uv * _MainTex_ST.xy + _MainTex_ST.zw;
                half4 sampled = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, atlasUV);
                half4 color = sampled * IN.color;

                #ifdef _ENABLE_FLICKER
                {
                    // Per-cell phase via worldX so adjacent fires don't
                    // pulse identically. Brightness wobbles between
                    // (1 - amount) and 1.
                    float phase = IN.worldX * 1.7;
                    float wobble = sin(_Time.y * _FlickerFrequency + phase) * 0.5 + 0.5;
                    float brightness = 1.0 - _FlickerAmount * (1.0 - wobble);
                    color.rgb *= brightness;
                }
                #endif

                clip(color.a - _AlphaClip);
                return color;
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
