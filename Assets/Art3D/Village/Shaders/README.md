# Village 3D shader draft contract

Drafts only. No Unity compile/import/runtime validation has been run by the author. Installed API verification: `/tmp/codex-v3d-urp-shader-api-review.md`. Its generic native-row upload example is superseded here by root's finalized **(24-y)*80+x** upload convention.

## Files and shader names

- `Village3DCommon.hlsl`: identical material layout, masking, lighting and pass implementations.
- `Village3DPalette.shader`: `CavesOfOoo/Village3D/Palette`.
- `Village3DWater.shader`: `CavesOfOoo/Village3D/Water`.
- `Village3DComposite.shader`: `CavesOfOoo/Village3D/Composite`.

## Runtime/builder bindings

| Binding | Required behavior |
|---|---|
| Per-presenter material `_FogLight` | Bind with Material.SetTexture on presenter-owned clones; never Shader.SetGlobalTexture. Actual Texture2D 80×25, RGBA data in linear space, no mipmaps, Point, Clamp. Upload `(24-nativeY)*80+x`. RGB is the native local light for visible cells or native stable remembered brightness for memory cells; alpha 0 unseen, approximately .5 remembered, 1 visible. Black/unbound/wrong-size resource clips all world fragments. |
| `_Transient` | Material or MPB float: 1 for owners that only render while currently visible, 0 for remembered static terrain. Shared color/depth predicates honor this. Shadow always requires alpha>.75. Whole-owner visibility filtering is still necessary for unseen actors whose mesh overlaps visible cells. |
| `_BaseMap`, `_BaseMap_ST` | Palette/albedo Texture2D and ordinary UV transform. Palette material binds imported `VillagePalette`; water may leave white and use `_BaseColor` dark teal. No alpha cutout contract in this first opaque wave. |
| `_BaseColor` | Palette tint white; water default dark teal `(0.045,0.19,0.18,1)`. Only RGB is used; output is opaque. |
| Remembered RGB | `albedo * fog.rgb` uses the native remembered/ambient brightness supplied by Village3DVisibility. No hardcoded memory tint and no additional live light, shadow or animated ripple is applied. |
| `_AmbientStrength`, `_SunStrength` | Default .7/.9; ambient SH and main directional light only. Need actual directional light and intended ambient environment. |
| `_WaveSpeed`, `_WaveStrength` | Water fragment-normal ripple speed/strength, default .8/.09. No mesh displacement and no environmental/reflection sampling. Palette ignores these but shares the identical CBUFFER layout. |
| Composite `_MainTex` | World camera's retained RenderTexture. Explicit `Universal2D` pass is recognized by current Renderer2D. XY MeshRenderer uses root's sorting-order/depth contract. |
| Composite `_FlipY` | Explicit 0 or 1 after actual native orientation check. No platform-only guessed flip and no fullscreen helper that flips a second time. |

The builder supplies an 80×25 zero-alpha default mask; the shader Properties black default also fails closed by its resource dimensions. No shared material or global fog mutation is permitted.

The actual texture-resource dimensions are queried with `_FogLight.GetDimensions`, an installed HLSL pattern used in Core `EntityLighting.hlsl`. Target 3.5 is declared for Metal/desktop use. The built-in white texture is 1×1 and fails closed; **an intentionally bound 80×25 all-visible texture would of course reveal all cells** and is the runtime's responsibility.

World color uses `UniversalForwardOnly`, plus custom mask-aware `ShadowCaster` and `DepthOnly` passes; it must render through the independent UniversalRenderer camera. Composite uses no world lighting, depth write or shadow pass. It is an opaque world rectangle at Transparent queue so Renderer2D sorting applies (`Blend One Zero`, ZTest Always); its geometry must be confined to the intended world viewport so it cannot paint over unrelated UI regions.

Soft-shadow variants match installed URP17.3. Current pipeline soft-shadow support is off, so the editor builder must explicitly enable it in the intended pipeline asset/configuration. Main light needs shadows enabled; a variant alone does not create a shadow map. `ApplyShadowBias` followed by installed `ApplyShadowClamping` matches this URP version. Fog lookup receives original unbiased world coordinates.

## Required import/native checks

- Compile every shader/pass and inspect ShaderUtil compiler messages; C# compile success alone is insufficient.
- Real renderer presence with unbound/wrong-size fog must yield no world geometry, including shadow/depth; bind correct all-visible texture as positive control.
- A multi-cell owner crosses unseen/remembered/visible boundaries per fragment. Transient owners never appear in memory; remembered static color remains unchanged when sun, live shadow caster or water time changes.
- Explicit shadow probe: alpha 0/.5 suppresses caster; alpha1 yields a real shadow under the same light and receiver geometry. Depth clip follows color, not shadow's stricter predicate.
- Native north/south/corner screenshot and projected input with `_FlipY=0/1` counter-control. Verify UI/sidebar/hotbar remains visible and the world camera never captures its own composite.
- Real water screenshot confirms opaque dark teal, readable ripples, no hidden-owner reflection and no transparency sorting defects.

Honesty bounds: minimal SH+main-light shading, not extra lights/APV/lightmaps/SSAO/decals, baked lighting or transparent-water parity. No 60-fps, SRP-batch count, soft-shadow appearance or visual-quality claim has been measured yet.
