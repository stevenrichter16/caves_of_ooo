# Reference glade — density C14

Status: the user approved the current visual direction and requested whole-biome
expansion (C15). The sixth native walkthrough passed 12/12 with a 60-second
performance capture; seventh grass geometry checks pass. Strict player-combat
acceptance and the subsequent biome rollout remain in progress.
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


## Fourth native route and fifth visual direction

Fourth assets pass140/140 native EditMode checks after measured native rig
proportions corrected the Marlback sizing contract and a one-microcell float
bound tolerance. Run `5145a7f823024f75b65b911c240d4f93` then passes12/12 live
ordinary-input checks, zero subscribed errors, in88.969 seconds. The movement
sample spans60.047 seconds/9,398 frames, mean6.389ms/p958.245ms in this editor.
Saved scene, start scene, seed, save override, last-game preference and background
input settings match before/after. The sampled settings receipt is adjacent.

Viewed arrival/composition frames show clearer figures, fuller grass and upright
plants, but floor luminance is too uniform and the pale stalks still form little
cones. The fourth visual pass is not accepted as the final reference match. The
fifth bounded plan changes only four reed recipes and adds static low-contrast
world-space ground variation to the privately owned glade material. It preserves
simulation, camera, placement, native visibility and ordinary materials. Native
GPU controls and a new screenshot comparison must follow test-first evidence.

### Stronger native checkpoint evidence

The next glade route strengthens the existing save assertion without changing
gameplay: F5 must emit its actual success message and change the isolated
checkpoint hash/metadata; D must actually move one cell while the file remains
unchanged; F6 must replace the player graph and restore exact player identity,
HP, tick, energy, position, file hash, chest depletion and harvested seam state.
This prevents ignored mutation/load keys from passing a stationary-state check.
The existing case count stays11 (or12 with the minute-long profile). This is
acceptance-harness hardening based on the lair audit review; the next native
run must exercise the strengthened path before it supplies new evidence.


## Dedicated native keyboard combat acceptance

The earlier visual route's `combat_uses_live_creatures` check observed aggregate creature attrition. The Warden or dog could satisfy it without a player swing. Historical receipts remain unchanged; future visual routes now call it `observed_live_hostile_attrition`, with the same limited predicate. This is not sufficient player-combat evidence.

A separate `LaunchCombat` mode now uses the same isolated native launcher and actual ordinary glade start. Before movement it verifies all three authored hostile identities and selects the original northern Scrabbler at22,5. It uses only keyboard equipment, movement and attacks, retaining actual AI and ordinary stats. No actor/terrain grants, travel shortcuts, damage calls, pose hooks or roll overrides are introduced. Each native key is bounded by a diagnostic marker; exact player/target HitRoll and positive DamageDealt must share their cause, and lethal attribution also requires matching DeathHandled. The selected enemy must attempt actual retaliation. The rig observer requires current Attack state, positive animation time and changed bound bones during a witnessed player-attack window, then captures that rendered frame.

The actual Scrabbler corpse chance is70%; finite death output therefore accepts either a corpse with exact SourceID/KillerID or one of the exact pre-fight owned gear IDs dropped at the lethal cell. This does not guarantee a corpse or gear observation in every successful branch. Actual removed-owner/view and live ordinary-player checks remain required. Twelve named native checks and four frames form acceptance; source compilation cannot substitute for that run.

Parser TDD recorded7 intended RED/19 controls before26/26GREEN; the current focused replay is26/26GREEN. Runtime/editor compiles against current Unity references have zero errors. Root and independent peer source reviews found no concrete blocker; attempted retaliation is not guaranteed damage, and image/animation quality still needs visual inspection. Exact preimage checks preceded publication. Plan, paired raw receipts, narrow existing-source patches, compiler logs and source hashes are in `Verification/DensityCompletion/ReferenceGlade/Combat/`. Actual Play remains pending.


### Combat audit startup identity correction

Actual combat run `fc5392c440204b53b957073c363609be` stopped before a combat key: ordinary40/40HP player at40,12, startup tick10, exact-source preflight failed. The original report/log/restoration remain preserved. Its owner descriptions were unfortunately emitted only after the failing Require, so that receipt cannot identify the exact failed per-owner predicate.

Source verification shows bootstrap eagerly records each brain's authored StartingCell, registers ordinary creatures, then calls ProcessUntilPlayerTurn before its after-bootstrap callback. Native NPCs can therefore already move before the player's first input. The audit now matches the exact three authored blueprint/start pairs using that captured starting identity, while retaining current live-zone/member/registration/alive checks and Warden/PetDog counts. It still selects the same northern authored Scrabbler and records its actual current location. Before any source refusal it records every current creature and all source-validation fields plus expected-match counts. No actor, world, scheduler or AI mutation is introduced, and combat attribution/retaliation/drop/pose gates remain unchanged. Runtime compilation against actual Unity references passes; actual combat and frame acceptance remain pending the next native run.

## User acceptance and whole-biome expansion — 26 September

The user explicitly likes the current scene's closer-to-Qud appearance and asks
for that style throughout a complete biome, covering every chunk and all
objects/actors, with the player starting there. This is acceptance of the visual
direction, not evidence that biome coverage or all gameplay acceptance is done.
C15 in `DENSITY-COMPLETION-PLAN.md` implements the extension in the Spread.
The fixed clearing remains a distinct authored location; its layout must not
replace the surrounding biome. Further subjective C14 retuning is subordinate
to complete coverage and functional native checks.

Actual native contact/door/harvest gate: job `0cb6528f2138403f857713b7d1a9e1f3`,
245 total, 225 pass, 20 fail. Contact contributes 10 expected RED/6 controls;
ordinary-ring door registration contributes one expected RED; harvest timing
and stale ownership contribute three RED/three controls. Six older lair audit
cases ran from a stale loaded assembly despite corrected code passing 48/48
standalone; the new 50 modifier/isolation cases were absent. These are not
represented as current-source verification. The idle, clean editor was restarted
to load current assemblies before further acceptance. Raw XML is preserved in
`Verification/DensityCompletion/Integration/native-contact-door-harvest-red`.

The matched mottle-strength measurement passed its nonmutation controls:
0.24→0.65 changes normalized floor pixels by 3.69%, with little added variation.
The actual alternative image was inspected. Production retains the accepted
0.24 setting; the measurement does not justify changing the approved direction.

### Actual dagger-only death and bounded starting-kit correction

Native combat run `f86bde7dfd1f4462a6a6cab0311e1d22` passed exact source/start/live/registration preflight, reached the same northern Scrabbler in12 real moves and recorded5 actual attack keys with player damage, retaliation and a current Attack-state/bone frame. The ordinary player then died: HP40→25→25→15→15→0, target ending4HP. Both actual starter tonic units remained unused. Original report, raw log, captures and exact restoration remain unchanged; this is a valid failed dagger-only approach, not a completed battle or proof of a balance defect.

The reviewed audit now captures exact initial tonic object/ID ownership, uses at most those two units through the real inventory menu at HP≤two-thirds maximum, and verifies exact one-unit loss plus one fresh actor/target/item TonicApplied record. It may use at most one actual ready starting Rime Grip through hotbar/direction after this target has retaliated, requiring the same visible adjacent owner, actual cooldown and Frozen effect; targetHP≤4 skips the4-damage spell to retain the stronger existing melee lethal-attribution gate. No item grant, direct heal/damage, cooldown reset, AI changes, source/seed replacement, alternate target or retry-until-success is added. Support windows record actual before/after HP, gear, ticks, energy and raw observations even on nested failure.

Source correction: HealingTonic heals4d6+4. Existing native ApplyTonic consumes an item but is not an InventoryUI pendingEverydayTurn action, so the audit records this current item-only cost and never invents a scheduler charge. Rime uses the normal turn/cooldown. Event-channel ownership is restored alongside damage-channel ownership. The two policies are distinct: tonic follows HP threshold; Rime requires prior retaliation.

Receipt validation recorded43 cases:2 intended positive RED with41 controls, then43/43GREEN. Full actual Unity-reference runtime and fixture compilation have zero errors; independent reread found no concrete blocker. Narrow4-file publication followed exact preimage checks. Evidence/source hashes and the compact original failed timeline are in `Verification/DensityCompletion/ReferenceGlade/Combat/StarterTactics/`. Revised actual native strategy remains pending, and image/animation quality is not established by the observer alone.

### Actual starting-kit combat completion

Revised actual native combat `c9e07d91e9c944c1a4d2f901c73c227c` completed12/12 with0 unexpected errors and exact restoration. The same ordinary actor finished31/40HP after11 movement keys and5 attack keys, using one original ready Rime Grip and no healing tonic. Actual player-attributed melee death, corpse/original-owned-drop provenance, dead-view removal and current Attack-state/bone capture all passed. The earlier dagger-only death remains a separate valid failed attempt; this is one bounded starter-tactics encounter, not a balance claim or a retry-until-success source selection.

Root viewed actual03 attack and04 outcome frames: reeds near the upper edge partially occlude the actor. Numeric pose and keyboard checks do not establish animation quality or complete C15 model/biome coverage. Full report/log/captures/restoration are under `Verification/DensityCompletion/ReferenceGlade/NativeCombat/c9e07d91e9c944c1a4d2f901c73c227c/`.
