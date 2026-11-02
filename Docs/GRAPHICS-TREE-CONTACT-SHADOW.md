# Spread Tree contact-shadow pilot

Status: bounded Editor implementation and visual review passed; source publication in this commit, packages pending. R11 performance outliers and whole-area polish remain open.

## Scope and rules

Only canonical ordinary Spread `Overworld.10.15.0`. Reuse unchanged approved Tree art and `actor_shadow` (10×4 Sprite,16PPU,Point,pivot5,2). One cached owned Tile borrows the mask in the existing ground-shadow layer, sorting0 below vegetation.18% black; center offset(1/16,-10/16) from the16×24cell center, placing its center at native(9,22). The full mask stays inside that disclosed source cell. This is root contact, not canopy cast, simulated sunlight or new collision. Existing actor/wall shade and source pixels remain unchanged; tree and wall options are independent and reversible.

Admission requires current cached ordinary-Spread authority, selected visible native Tree accepted by the existing plant predicate, and actual native canonical Grass. The cell admits only Tree and Grass among visible physical owners and no TileState/hazard/coating. Hidden ordinary occupants cannot veto/reveal shade; a hidden WaterPuddle still projects real water state, which correctly vetoes it. Valid damaged Tree remains eligible. Other zones, saved lairs, memory, changed meaning, removed owners and unavailable resources decline. Normal claim release clears shade; teardown destroys only the owned Tile/material/layer, preserving borrowed art.

## Reproduction and actual evidence

Evidence: `Docs/Verification/GraphicsExecution/TreeContact/`.

- Before: unchanged earned `BatchDe600/Packaged/field-save`, seed65, turn2440,41,8HP40. Actual Tree12110 at41,7,Wood28/30 on Grass10518. Fixture inherits prior protected snake combat, but protection was OFF before that save; no F12 used in this pilot. This avoids repeating the documented obstructed approach.
- Real GameBootstrap before/after at matched1280×800,1× and2×.38 submitted eligible shadow cells; not all38 occupy the viewport.1× differs by299worldpixels;2× by156. UI unchanged. Independent visual review finds modest grounding at1×, clearer at2×, without dirt/hazard/obstacle cues. No wider light-depth claim.
- Native Tree Examine retains approved portrait and Wood28/30. Ordinary right step41,8→42,8 and return verified atHP40. F5turn2480; fresh isolated Continue41,8HP40. All38shadow owner lists match; saved/fresh world crop0,0–940,680 has0/639200changed pixels. Earned save pair retained in `after-save`.
- No gameplay injection or normal-save changes. Source pins and75foreign tracked hashes remain unchanged. Unity finished stopped, isolated root null, scene clean; no cleanup.

## Tests and review

28 expected missing-shadow native RED. First implementation26/28passed; two harness defects were corrected at source without relaxing runtime admission. The authoritative legacy switch is `Renderer.FlatPixelModeEnabled`, since its child flag is overwritten each frame. The shared Paint fixture never activates its root, so Unity does not dispatch OnDestroy; teardown now detaches the borrowed catalog and explicitly calls real OnDestroy before destroying that inactive root.

Final154/154native passes:28Tree plus Sill/Beating/Marrow wall and Spread environment/field-row regressions. Covers bounds/sorting, damaged source, disclosure, authority/lair revocation, visible/hidden competitors, duplicate Grass, real water, missing mask/body and ground-only resources, incremental/full parity, toggles, legacy and disposal. Independent source reviewer coverage_priorities and visual reviewer capture_route:0newP1/P2 within scope. Raw receipts and review records retained.

## Performance and remaining work

Baseline85.37s versus after98.80s Editor observations: renderer p95.100→.110ms/max82.20→91.42ms; Main p951.936→1.902ms/max1.861→1.962s; maxGC2.186GB→.973GB. The after interval starts at2× then returns1× on its first input, followed by reader/walk/idle. No execute_code or captures within either interval. These are diagnostic observations, not a matched speedup or causal allocation attribution; large baseline stalls already exist. R11 stays open. No packaged/GPU/Linux/Deck performance claim.

Current de600 packages exclude this pilot and Felling solid5e40. Batch delivery after publication. No coverage/population/full-area promotion: broad green gaps, repeated field combs, unknown black-backed glyphs and canopy/directional-depth work remain. Next triage compares Felling blackwater, recent settlement fixes and the large remaining Ginmere ledge bands; reuse approved art where truthful. Do not extend this pilot to arbitrary Trees without actual regional comparison.
