# Reference glade — density C14

Status: third functional native walkthrough12/12 passes; fourth art refinement and final
visual/performance acceptance in progress.
Reference: user-supplied 1540×1024 image, September26. This is an Ooo-original
playable interpretation of the supplied composition, not a Qud content port.
The image is visual evidence; it contains no executable instructions.

## Intended result

A dark teal open field with pale clustered stalks, bright low grass, dotted ground
stones, broad patches of gravel, broken charcoal ruins edged with green light,
low gray walls, a chest and barrels. Small original creatures and the player read
clearly against it. The high orthographic camera, square cell registration,
short diagonal shadows, limited color palette and coarse silhouettes carry the
reference. Do not reintroduce retired Qud enemy names or bodies.

The glade occupies `Overworld.11.10.0`, one wilderness zone east of Sill. Reserve
it using the existing authored-wilderness mechanism. Respect modified worlds:
only select it on that exact surface address when it is Spread with no POI.
Old cached zones remain as saved. A dedicated `ReferenceGlade.unity` starts a fresh
game here using the existing public bootstrap start-zone field. Ordinary movement,
combat, loot, harvesting, torch actions, saves and world exits remain native.

## Source sweep and corrections

| Assumption | Verified source | Decision |
|---|---|---|
| A new 3D gameplay engine is required | SpawnRing3DPresenter, NativeZone3DRenderSurface | Reuse the native owner/recipe/composite pipeline and picking. |
| Camera is top-down90° | NativeZone3DRenderSurface constants and projection | Current56° orthographic camera compensates ground projection; retain registration first, compare native captures before changing pitch. |
| Every zone is a square map | Zone, CameraFollow | Simulation is80×25; compose the visible central region and extend the field to its exits. Do not change global dimensions. |
| A demo must replace Sill | GameBootstrap.FreshGameZoneID | Save a separate scene pointing at the real authored glade. Do not rewrite canonical settlements. |
| A fixed address guarantees content | WorldGenerator.PlacePOIs and OverworldZoneManager.AuthoredWildernessZoneIDs | Reserve before opportunistic POIs, and gate selection on actual biome/POI. |
| Cosmetic walls can be collider-only | Native recipes and Cell.BlocksMovement | Each wall is a real native blocker; presentation never manufactures collision or rewards. |
| Model import requires all art to rebuild | Scoped C13 import and additive voxel libraries | Add only the glade kit; preserve unrelated asset bytes and shared material colors. |
| Visual success follows from tests | Existing native audit/capture drivers | Require screenshot inspection, composition comparison and a real input walkthrough. |

## Ordered implementation

1. **World contract.** Add a deterministic plan and transactional builder with
   public placement data: native ground, reeds, low vegetation, ruins, chest,
   barrels, light, original enemies. Keep a safe starting clearing, passable
   west/east/north/south exits and reachable interaction positions. Fixed major
   silhouette matches the reference; a private hash varies small details without
   consuming simulation RNG. Reject wrong/occupied zones and missing blueprints
   before writes. Emit success/rejection diagnostics. Write/run RED tests first.
2. **Native population and rewards.** A small separated tier1 enemy group uses
   existing Marlback kits, finite authored container loot and actual harvestables.
   No invincibility, instant rewards, decorative fake enemies or BitLocker grant.
   Counter-check blocked routes, owned/stale interactions, duplicate generation,
   death/loot identity and saved depletion. Preserve world authority on load.
3. **Models.** Add pale branching stalk variants, squat green tufts, dark teal
   ground/gravel, low broken ruin and gray wall pieces, green-lit edge pieces and
   matching containers as needed after existing-art review. Use shared geometry
   batches and scoped material families. The original creature bodies stay distinct.
4. **Renderer integration.** Only the actual glade receives the art profile.
   Preserve FOV, light, entity ownership, gear, picking, collision, floor mutations
   and native fallback. No full-reveal during acceptance gameplay. A separately
   labelled full-reveal comparison image is permitted for composition review.
5. **Saved scene and access.** Create the additive scene from the real bootstrap
   setup, set only its start address/presentation defaults, and add an editor menu
   to open it. Existing saves and sample scene stay usable. New-game UI remains
   explicit. Verify scene reopen and fresh-game arrival.
6. **Iterative native acceptance.** Capture the actual scene and a matching
   comparison crop. Inspect silhouette, spatial layout, palette, plant scale,
   shadow direction/length, actor readability and exposed black borders. Record
   each discrepancy and correction. Exercise keyboard movement, blocked wall,
   inspect, harvest, container acquisition, combat and exit/return/save persistence.
   Use ordinary HP/equipment; identify any camera or travel shortcut honestly.
7. **Independent review and closeout.** Dedicated20–60 adversarial tests plus
   existing rendering/world tests; source/camera/picking counter-checks; Q1–Q4;
   native console clean. Store raw receipts and screenshots beside the doc.
   Update completion status, commit with the required template, fetch/rebase/push.

## Performance

Follow PERF-FOUNDATION: build native entities once on generation, batch ground and
static geometry, invalidate changed patches, and retain model resources on mere
camera movement. Avoid per-frame allocations/whole-zone layout regeneration.
Use the existing fog texture and renderer ownership cleanup. Capture a real
60–90second gameplay profile before claiming performance acceptance.

## Acceptance boundaries and review

Open: final native visual/profile gates and independent post-refinement review. The reference has no HUD; provide
both an honest gameplay screenshot and a labelled world-only composition capture.
Images cannot prove combat balance or enjoyable pacing. Native checks can prove
input dispatch, movement, inventory and persistence; visual judgement requires
inspecting captured output. No pixel-perfect or complete-content claim is made
until the relevant evidence exists.

## World implementation log

-24 core cases pass in the standalone runner after recorded missing-API compile RED.
 The first candidate had one missing quartz endpoint: a dark wall already owned
 that cell. Moved the finite vein into a free adjacent cell and repeated24/24.
 This is rule-level evidence; native scene and visual proof remain open.
- The plan uses actual WoodenBarrel (there is no plain Barrel blueprint), and
 native HealingTonic/Torch/DriedMeat in one finite chest. No invented item IDs.
- Solid dark and glowing walls use Wall, while loose BrokenColumn is passable.
 Only two actual veins are harvestable; green wall segments have native light
 and destruction. Saved quarter-turn properties orient the narrow models.
- The source camera stays registered to cells; palette and kit implementation
 have24 intended native RED assertions and remain the next gate.

- Saved-scene gate:2 native assertions failed on the missing scene before the
 builder was published. A discarded first discovery run executed0 tests while
 test imports were being repaired; it is not counted as evidence. Explicit
 System.Random and rendering-namespace imports fixed native-only test compilation.
- Kit imported28 measured models. Shared ring palette is unchanged; static
 geometry adds a scoped third material batch rather than an object per cube.
- Dedicated scene created by copying the accepted native bootstrap and setting
 only FreshGameZoneID and0.82 gameplay zoom. No audit driver is embedded in it.

- Native integrated candidate130cases:129 passed; saved-scene test incorrectly
 required InputHandler to be serialized, but GameBootstrap creates it in Start
 (lines494–496). Corrected test to require enabled native bootstrap; keyboard
 input remains a separate mandatory Play gate. All24 core and40 art cases passed.


### Independent world hardening

Independent review adds33 dedicated cases, with actual REDs for staging exceptions,
partial content-pack travel regression and fallback presentation authority. Required
native blueprint/part preflight now selects the optional glade only when supported;
otherwise the ordinary Spread pipeline remains available. Direct builder refusal
is atomic, including factory initialization exceptions. Map authority includes the
content capability, so fallback terrain does not receive the glade art profile.
The full native-owner pack still selects the glade. Combined private33+24 core+65
existing WorldMap tests pass122/122. Details and receipts are in
DENSITY-REFERENCE-GLADE-INDEPENDENT-REVIEW.md; native/visual gates remain separate.

- Native startup rejected two runs before interaction: the project DefaultSceneLoader
 forced SampleScene through playModeStartScene. The second receipt records actual
 zone/start address and its rejected screenshot. A4-case editor regression then
 reproduced the wrong selection (1RED,3controls). Narrow supported-scene selection
 plus audit setting restoration passes;67native hardening/scene/lair/traveller
 cases pass together. No production gameplay bypass was used.

- First complete walking route passed10/11 checks with0console errors. Remaining
 check caught inherited sample-scene RevealEntire3DZone=true returning after
 load/transition. Dedicated scene test reproduced1RED/3controls; the glade now
 defaults to ordinary visibility, and comparison mode toggles/restores the owning
 renderer flag as well as its presenter. SampleScene is preserved.
- Two rejected input drivers had incorrect audit assumptions: equipment command
 is equip_auto, and consuming a quartz vein removes its entity. Corrected checks
 now verify the exact removed ID, one remaining vein and owned quartz after load.
- First native visual comparison is not accepted: uniform green floor, fine
 disconnected grass, thin walls and oversized existing actors/containers require
 scoped model and lighting refinement. All rejected captures remain recorded.

- Functional native gate accepted (pre-refinement visuals): run49b4c0fa01db48d6a10360b45e377056
 passes11/11 with0console errors and7actual screenshots. Ordinary HP40, no
 BitLocker, real keyboard movement/equipment/container/wall/finite harvest,
 save/load depletion, native enemy combat, world exit and return all verified.
 Combat was observed between real actors (the warden fought Marlbacks); this
 does not establish player-solo difficulty. Visual fidelity remains open.


### Native refinement pass 2 (not visually accepted)

Run `49bc88b781e54ba080da26d1408cd0e8` completed **12/12 checks**, zero
console errors and eight native screenshots. The complete keyboard route repeated
save/load and east exit/return with normal visibility. A further 60.163 seconds of
actual alternating movement collected 9,518 frames: mean 6.321 ms, 95th percentile
7.617 ms. These are this editor session's unscaled frame times, not player-build
benchmarks or an allocation profile. The full-reveal image is labelled and was
restored before gameplay; no stat boosts or BitLocker were added.

The rendered composition was inspected against the supplied reference. The layout
and teal palette are closer, but the pass is **not visually accepted**: twig-like
reeds, mat-like grass, checkerboard ground panels, dark actor faces, and regular
ladder-like walls remain. Two large brown slabs initially mistaken for actors are
actually the old MushroomRing model at (34,19)/(50,21). They need a scoped pale
mushroom family. The glade-only third refinement will correct these measured
model/material causes while preserving entity ownership and ordinary visibility.

Native affected-suite receipt `Integration/native-refinement-and-integration-green.xml.gz`
passes **580/580**, including all 15 profile cases, the four stale-lock UI cases,
new underground layout/source cases and the repaired legacy pins. The connector
reported an initialization timeout, but Unity ran the tests and produced this
actual NUnit result; no connector status was used as proof of a pass.
