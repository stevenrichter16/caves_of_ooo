# Sodden flood-water surface pilot

Status: bounded Editor material verification complete; full-area polish remains in progress. Scope: ordinary Sodden16.5 only. Current Mac/Linux fbfea packages do not include this pilot.

## Evidence and choice

Current earned SoddenSources/BeforeCheckpoint, seed65,16.5,turn2630,40,12HP38, reproduced through real GameBootstrap. Three broad teal pools dominate the normal-zoom frame. Runtime inspection at24–31,2 proves permanent ownerless water on real Grass (one DeadTree cover), `Live16x24 flood water` masks1/9, white tint, local native ground. Approved opening-ground frame0 contains384 identical RGBA(12,67,68,255) pixels. This is material polish, not missing sprite coverage or an overlay bug. The alternate Stump canopy grounding evidence is older; Spread sparse field layout requires a larger population slice. Finish this bounded visible material gap first; population/shadows remain separate pending work.

## Requirements and acceptance

Use the existing2D renderer, approved source bank masks and baseline palette. Add sparse short horizontal ripple rhythms with large quiet intervals. Preserve straight terrain boundaries, bank mask pixels, all land and other regions, actual liquid authority, navigation, fog/memory, hazards and saves. Eight deterministic coordinate variants; no animation, game RNG, new owners or mechanics. Reversible optional atlas: missing/malformed resource retains original water. Pilot only16.5 pending actual review; do not silently revise opening or all Sodden art.

Accept only after native RED/GREEN and resource/disclosure/coating countercases, matched1× actual gameplay comparisons plus2× readability review, bank movement/water identity/travel/save/freshreload, bounded profile and independent source/visual review. Record residual flatness, repetition, bank sharpness and any reload differences; no whole-area or performance acceptance from a narrow result.

## Production and saved source corrections

AI reference and exact builtin-tool prompt are in ArtSource/Live16x24/sodden-flood-water. It is a concept, not gameplay. Keep its quiet ripple grouping; reject smooth glow, raised-looking thick rims, brighter land and altered framing/UI. Original native semantic quads extend the approved editable Ground source; no AI raster sampling.128frames(8rhythms×16bank masks),2048×24,editable Blender source.49152 exact pixels verified;920interior pixels changed; every original bank/border pixel and four pinned sources unchanged. Blender reopening verifies49152quads. Original atlas is untouched.

Two native regression cases(seed64/65) failed before integration because the current surface was `opening-ground`, not the proposed resource; the other five FloodWater cases completed but their Passed/Inconclusive split is not recoverable from the job JSON. See red-job.json; this turn retains native tool job evidence rather than manufacturing XML. New countercases cover missing/malformed resource, disposal without borrowed destruction, hidden/unexplored state and mixed/passing/projected liquid. Adjacent Sodden regression runs in the same native job. Saved runtime correction is limited to optional atlas creation/selection/disposal in FlatPixelFloodWaterCatalog.

## Reproduction

Replay the existing SoddenSources/BeforeCheckpoint in a fresh isolated SaveRootOverride with GameID50e670c30b6d48feb82756df64bd1cbc; SetActiveGameID before native Continue. Never teleport or replace normal saves. Capture at40,12,1×; Prior/Prior/Next for2×. Build native source with Blender background Tools/Live16x24/build_sodden_flood_water.py -- --build, reopen editable.blend with -- --inspect, then python3 script --verify. The runtime PNG is an exact copy; importer remains point/clamp/no compression/no mipmaps at16PPU.

## Iteration and review corrections

Candidate1 actual1× showed ripples but still a uniform dash screen; independent visual review requested another iteration. It is preserved in candidate1. Candidate2 increases only ripple contrast and uses coarse quiet patches plus finer stable variation; no border/bank changes. Source review found address-only admission leaking into MerchantCamp sites and changed Spread authority. Two native failing counters reproduce it (authority-red.xml); selection now also requires current ordinary Sodden authority, retaining original water at sites. Initial148-case XML is145Passed+3Inconclusive, not148all-passed. Three known allocation-counter positive controls remain inconclusive; no allocation acceptance.

## Verified result — 2026-11-02T08:24:31.907371+00:00

Candidate2 changes1,979/639,200 matched world pixels; right UI is exact. Actual water reader at40,9, blocked tree and routed water step39,7 preserve identity/navigation. Ordinary17.5 travel retains the approved original water. Real snakes/bleeding reducedHP38→28; noF12 was used. F5turn2790 and fresh isolated Continue return40,12HP28 with **0/639,200 world pixels different**; log UI changes. Matched1× plus after-only2×, not matched2×.

Final native XML150cases =147Passed+3Inconclusive, zero failed/skipped,11.9049035s. Three known allocation positive controls remain inconclusive. Independent source/visual review found zero remaining P1/P2 after ordinary-site scope correction and second art iteration. Optional missing/malformed atlas keeps original water; borrowed assets survive disposal.

125.27s after-only Editor profile: rendererp95 .092875ms/max168.13ms, Mainmax2.276s, GCmax560,783,927bytes. Manual readers, encounter, travel and capture overhead included; no execute_code inside window. Spikes are unattributed; R11 and packaged/hardware performance remain open. Local ripple repetition, shallow water, rigid bank corners, repeated grass and ASCII portraits remain. Material-only acceptance does not close population, shadow, whole-area or delivery axes. Current fbfea packages exclude this slice.

Evidence: `Docs/Verification/GraphicsExecution/FloodSurface/verification.json`, actual before/after/readers/walk/travel/save/reload PNGs, separate native RED/initial/final receipts, profile and independent review. Unity stopped, SaveRootOverride null and scene clean after final capture. No cleanup;75foreign tracked files remain byte-preserved.
