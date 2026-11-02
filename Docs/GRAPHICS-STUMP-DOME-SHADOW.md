# Summit Scrub stone-dome contact shadow

Bounded Editor acceptance for ordinary `Overworld.3.2.0`, seed65. The actual baseline showed bright raised stone masses without convincing ground contact. Reusing approved art makes this a small reversible shading pass; it adds no new sprite family.

## Implementation and acceptance
The existing source-anchored wall-shadow layer borrows each selected opaque StoneDome tile,16% black, offset two native pixels south. All17 applicable source frames are fully opaque. Only the southern strip is visible beneath the original body. FlatWallShadowEnabled controls it; no new texture, shader, layer, collision, owner, RNG or save state.

Admission requires the exact current ordinary3.2 graph; disclosed, selected canonical native StoneDome; disclosed southern cell with exactly one genuine native TepuiStone and no other disclosed physical occupant. Source and receiver tile state must be empty. Hidden owners are ignored, preventing disclosure. Changed meaning, remembered cells, missing art, occupied ground and legacy presentation decline safely.

27 genuine missing-shadow RED cases preceded implementation. The combined dome, Beating-wall and Rim-tree batch passed60/60. Independent review found an incremental-test premise gap; an explicit !FullDirty assertion now proves receiver-only incremental execution. Final27/27 passed. The intervening method-filter job never initialized and is excluded. These batches are nonadditive.

## Actual play
Continue WorldMapRemaining/AfterCheckpoint, walk33,7→33,5, descend3.2. BeforeCheckpoint preserves F5turn4090 at40,12HP38Height. Matched1280×800 normal-zoom before/after changes2,357/639,200worldpixels and0right-UI pixels. Independent review accepts modest grounding, without heavy outlines or an apparent extra wall.

Walk to38,21; north remains blocked. Look38,20 identifies solid stone dome. Moving south to38,22 leaves the receiver; F5turn4210 and a fresh isolated Continue restore38,22HP38Dusk. A map ascent/descent returns to the same local position. NoF12 or injected movement. Saved/reloaded images differ by170worldpixels at(463,561)–(486,590), around the player/creature, outside the dome contact edge. Cause is unproven; exact image equality is not claimed.

The93.87s after-only Editor profile reports rendererp95 .093834ms/max99.933ms, Mainmax18,577.11ms and48batches. It includes diagnostic/tool calls and capture stalls; reported2321×1285 differs from fixed1280×800 captures. No speedup, packaged performance or R11 acceptance.

## Remaining work and delivery
Broad empty center, repetitive stone faces and remainingglyphs remain concrete defects. This is neither newASCII coverage nor full-area/population acceptance. Currentcc6418ff8Mac/Linux packages exclude this pilot; batch with the next accepted material correction. Linux/Deck execution remains untested.

Evidence: `Docs/Verification/GraphicsExecution/StumpDomeShadow/verification.json`, native receipts, actual captures and Before/AfterCheckpoint. Profile: `Profiles/manual-20261102-184316-165.json`. Root finished Playfalse, SaveRootOverride null and scene clean. Unrelated work and normal saves preserved; no cleanup.
