# Seventh glade visual experiment (private)

## Evidence and bounded intent

Sixth actual screenshot 4281297b663744e0bc6de61ec5d80201 improves heads/limbs. Keep all three forms, source rigs, gear sockets, native positions, camera pitch and simulation unchanged.

Native image remains sharper and flatter than reference. Pale reed caps are hard squares, green tufts roughly half the reference width at similar projected height, and ground has little broad/contact variation. Pixel census is in Analysis/sixth-reference-metrics.json: active ground colors vary by several levels, so it is incorrect to call mottle absent. Full color census is not proof of a specific cause because reference composition, compression and geometry differ.

Source sweep: glade's own MaterialFor clone has strength .24; SpawnRing3DGroundPatches uses that clone and actual world-space ground below .1. Uniform atlas albedo is exposed then lit. Existing mottle changes only near-ground upward nontransient faces before native visible/remembered branches. Native target is full viewport normally, .75 only low-detail; pipeline renderScale1. The owned camera and ARGB32 target have no MSAA. Bilinear composite filtering cannot antialias a same-resolution raw world target. Sun shadows are visible in the actual screenshot, so do not diagnose all shadows as disabled. No SSAO feature exists in native renderer. Uniform ambient SH supplies fill but cannot produce local contact occlusion.

## First experiment, no production changes

`ReferenceGladeRenderMeasurementTests` runs four actual owned-camera pairs on the same generated native fixture:

- Mottle0 versus current.24, shadows disabled for both.
- Current material with shadows off versus current soft shadows.
- Current1× render versus2× render bilinearly resolved to the same output size.
- Soft-shadow qualityMedium versusHigh on the owned Sun only.

Each writes baseline/alternative PNGs plus active profile, render dimensions, mean/spread/difference metrics and approximate masked-floor variation. It retains native entity positions/version/tile state and batched geometry count; restores owned material/light/target in finally and destroys only its temporary GPU/CPU assets. It never writes a shared material, pipeline/scene asset or global RenderSettings. Existing source camera and UI are unaffected. These are diagnostics, not an art acceptance or separately failing feature contract.

Offline compilation passes against actual Unity/Test references and copied exact fixture helper classes. Parent owns native execution and image comparison. Do not adopt sampling/light settings until those results show an improvement.

## Candidate changes after measurement

1. If 2× resolved edges materially improve plants, add a bounded per-surface sampling profile configured only on an authorized glade. Default1×, low-detail retains existing reduced target/shadows; rebuild on authority loss/regain; no global pipeline MSAA or renderer changes. Test exact owned target lifecycle, unchanged grid/picking/FOV/gear and actual resolved edge pixels. Native 60s performance rerun required.
2. Ground broad variation must be evaluated as normalized albedo response and penumbra width. Preserve zero-default outside glade and live/remembered/hidden controls. Do not blindly increase existing strength or claim noise is contact occlusion. Any contact-shading implementation needs separate design/evidence after these measurements.
3. Only four grass source variants may widen toward a30×24px reference footprint versus current15×24px, while preserving current height. Source RED before geometry changes; other36 recipes/palette exact. Hold reeds until edge sampling is measured so geometry is not repeatedly altered to compensate for aliasing.

## Open gates

Actual paired renders and source semantic tests; root's screenshot judgment; ordinary-zone/default and authority/FOV tests; projected visible height pins repaired independently from art tuning; finite gameplay/60-second native run with exact editor/save/input restoration. No seventh art acceptance is claimed.
