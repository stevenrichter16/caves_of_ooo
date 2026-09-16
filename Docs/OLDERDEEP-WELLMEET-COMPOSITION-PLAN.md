# Olderdeep and Wellmeet — native composition and voxel identity

Status: complete and installed. Four native chunks, 116 voxel model variants. FC18 passed 652 focused checks; FC20b passed 451 scope/art/native regression checks. FC22 completed the full suite: 13,169 passing tests, exactly 32 unchanged baseline failures, zero C# errors and no new failures. All 344 additional tests pass.

This is the second pair requested on 2026-09-15, following Cathedral/Stillleaf in `3f0f1ad8`. CoO-original composition and art, not a Qud implementation port. The running user editor, camera, 1.2× view, full reveal, spawn and saved graphs were not changed by this phase. See [the verification receipt](Verification/VoxelWorld/FC23-closeout/README.md) and [twelve native previews](Verification/VoxelWorld/FC17-final-preview/index.html).

## Scope and design

Four native chunks, with distinct functions and generation rules:

| Area | Native addresses | Composition |
|---|---|---|
| Olderdeep | `Overworld.4.6.0`, `.1`, `.2` | A sheltered Grovelands mouth, a used descent with spacious stopping places, then the inhabited founding chamber with its quiet eastern embrace gap. |
| Wellmeet | `Overworld.8.16.0` | An open Tent-Right settlement arranged around real well service, guest reception, salt exchange, shaded resting tents and working yards. |

Olderdeep retains the native FoundingVillage floor stamp after stairs. Its
Rooted body, eleven living plume cells, listener, tender, plaques and homes
stay mechanically native. Shape the approach and surrounding chamber, never
move the body merely to improve a screenshot or pave the untouched embrace gap.

Wellmeet receives a dedicated semantic base, not a random village plus one
decorative tent. Variable tent clusters and purposeful paths connect the well
commons, receiving yard, salt court, residential shade and services. The main
repairable well remains at `(40,12)`. Preserve native VillagePopulation,
SettlementManager services/NPC linkage, trade, containers, cave links and
house-drama hooks. The TentCamp profile and its real host/salt-master must land
exactly once with reachable frontages.

The First Tent (`5.17.0`), other settlements, other holes and depth 3+ are
countercontrols. Managed worlds must retain their own current POI/profile
authority, including renamed profiles and restored zone graphs.

## Verified sources and corrections before production

| Source inspected | Actual contract / correction |
|---|---|
| `Lore/10_Bible.md:131`; `Objects.json:31395` | The Rooted is a contented living, fungal-plumed body embracing the root wall. The native owner is an indestructible fixture, not a creature or new boss. Art must not disclose additional divine-name or ending truths. |
| `SinkholeSites.cs`; `FoundingVillageBuilder.cs` | Olderdeep is Grovelands at `(4,6)`, profile FoundingVillage. The floor stamp runs at 3650 after stairs, chooses a stair-safe body row, keeps the eastern gap clear and owns its two chamber outlines. Preserve exactly eleven plume owners. |
| `FoundingPlumePart.cs` | Sleep needs a living player on the actual plume cell, liked CatacombFolk reputation and a live input-ready turn/session. It uses ordinary rest, a one-time meeting fact and a two-week saved-clock bloom. No new dream scheduler or audience. |
| `FoundingTrustService.cs` | Tepuibone offering is a guarded one-time real mineral exchange. Tender identity/Parts, reach, consent, session, inventory and prior fact are revalidated. Guest safety is not consent. |
| `Objects.json`: FoundingPlume/BeetleJar | Plume is walkable and destructible, emits native green light and owns the rest action. A lamp-like jar must not gain a new alarm or destruction mechanic just through its mesh. |
| `Lore/Factions/07_TentRight.md:113–123`; `WorldMapAuthoring.cs:220` | Wellmeet is the Tent-Right water/salt hub, Beating at `(8,16)`, profile TentCamp. The First Tent is a separate place and stays unchanged. |
| `UnderTheClothEffect.cs` | The three-day oath protects from hostile people, not beasts; actual clock expiry and oathbreaking remain native. Do not describe absolute protection from every danger. |
| `StampCatalog.TentRightProfileCamp` in `LandmarkBuilder.cs:745` | The native camp supplies two tents, one well, a cloth pole, a host and a salt-master. FirstTent only adds a four-pole monument/keeper. Keep actual profile services, not invented caravan/boat systems. |
| `VillageBuilder.cs`; `VillagePopulationBuilder.cs` | VillageBuilder's injected SettlementManager is unused. Durable service/NPC linkage is in VillagePopulation. Do not invent building-record dependencies or bypass that population stage. |
| `VillagePopulationBuilder.cs:538` | The repairable main well is always placed at `(40,12)`, independent of reservations; leave that cell and cardinal markers clear. |
| `VillagePopulationBuilder.cs:1166–1220` | Outdoor placement respects reserved cells; at sweep time, interior placement checked exact StoneFloor and ignored reservations. Shade uses the separate IsInterior flag. A scoped reservation fix requires actual RED plus unchanged-village countercontrols. |
| `OverworldZoneManager.cs:846`; `WorldMapAuthoring.RiverRows` | Every old village gets a bottom river even when no river is authored; Wellmeet's map row has no river. New Wellmeet generation will omit that generic river explicitly and rely on real wells, while other village pipelines retain existing behavior. Never paint native water dry. |
| `VillagePopulationBuilder.cs:100` | Ordinary population also provides a functional Shrine/Sanctuary. Preserve it; the First Tent monument's no-god canon does not justify silently deleting native village services. |
| Current voxel libraries, recipes and area scope | Retain one-cell owners, shape-stable portable models, missing-source fallback and weak zone-local map authority. The older 3.75-cell decorative village well is unsuitable for a native one-cell owner. |

## Implementation sequence

1. Preserve exact shared-file starting contents and reuse the immediately
   preceding full SC17 run as FC00 baseline: 12,857 total, 12,825 passes, 32
   unchanged known failures, zero C# errors. No production changes intervened.
2. Capture a native census of both current sites before geometry/recipes change.
   Use it to budget actual model families, including late village services,
   role NPCs, loose supplies and runtime visual variants.
3. Author and run actual REDs for the finite scopes, deterministic semantic
   plans, native realization, protected arrivals/frontages and required Parts.
   Builders stage/preflight before mutation; diagnostics explain rejections.
4. Implement native plans/builders and narrow manager routing. Keep Olderdeep's
   sacred stamp last relative to stairs; preserve Wellmeet's service pipeline.
   Test shade/interior placement independently, and Wellmeet's real dry ground
   against the unchanged First Tent river.
5. Build reusable voxel kits with four variants per repeated family, at most
   two palette swatches, broad readable silhouettes and one-cell horizontal
   bounds. Instances borrow combined meshes; no per-voxel GameObjects/colliders.
6. Exercise native owner removal, path and stair reachability, lower-first
   access, repairable-site authority, stock, guest oath, real mineral exchange
   and plume sleep. Keep positive/negative pairs and separate adversarial files.
7. Render actual manager-generated chunks at three seeds. Review all forms;
   fix generation/asset rules instead of manually arranging demonstrations.
8. Cold-eye cross-system and player-flow review; actual RED before fixes.
   Finish with complete model coverage, metadata/rebuild audits and exact
   full-suite baseline comparison. Update living docs and scoped commit.

## Performance and honesty bounds

Plans run at fresh generation only. Rendering uses the existing per-cell dirty
reconciliation and static batching. New model/pose decisions should be cached
or cheap and allocate no collections in frame/turn hot paths. Native interaction
flows remain cold, event-driven operations. Do not build a new per-turn room scan
or imply measured FPS from static pictures.

Native headless previews can establish composition, model coverage and observable
gameplay contracts. They cannot establish live input/animation feel or sustained
frame rate. No save migration is requested; saved/current owner graphs are not
regenerated. Explicit native indestructible fixtures remain exceptions.

## Implementation log

- 2026-09-15: Cathedral/Stillleaf closed in `3f0f1ad8`. Three independent read-only
  surveys converge on Olderdeep plus Wellmeet; First Tent retained as a control.
  Verified correction table above precedes production. Shared-file snapshots
  are in `/Users/steven/.cache/caves-of-ooo-validation/founding-camp/before`.
  The imported isolated execution project is reused from the preceding phase;
  the running user editor is not restarted.

- FC01 captured the untouched native four-address census at three seeds.
  FC02 captured missing Wellmeet/kit contracts; FC03 added Olderdeep's missing
  contracts. FC04 ran rendering assertions separately from those missing-type
  fixtures: 45 cases, 7 passing controls and 38 expected failures, zero C# errors.
  Production followed those captures. The niche-facing fixture's north/south
  expectations were corrected against `Village3DProjection` (north is +Z).
- Additional verified contract: `SettlementSiteVisuals.ApplyToEntity` already
  calls each native site Part's `OnStageChanged`, which stores its last applied
  stage. A read-only `VisualStage` accessor lets rendering choose the actual
  owner-local damaged/patched/repaired/improved form without consulting another
  world's active SettlementManager. No new saved field or migration.
- Wellmeet replaces the old random eight-by-six camp stamp with a semantic late
  profile assembler using the same four real profile owners. Its five tents
  include the profile's two receiving tents plus three household/service spaces.
  VillagePopulation and all later service, stock, quest and drama stages remain.

- FC06: first twelve native previews built with zero C# errors. All Olderdeep
  owners had models; Wellmeet coverage exposed alchemy shelving, campfire and
  quest markers, and the legitimate pilgrim reskin. Visual review found rigid
  dark camp roads, misoriented tent runs and a flat joining plume carpet.
- FC07: 267 targeted cases, 251 pass, 16 failures, zero C# errors. The important
  gameplay failures expose generic connectivity tails sealed by the later
  founding shell, unreserved village stairs, and a builder retry guard.
  Harness corrections (property reflection and internal inventory API) are
  recorded separately from production fixes.
- **Verified scope correction:** `SettlementSiteDefinitions.IsTrackedVillage`
  only enables repair sites in the original starting village. Wellmeet's prior
  wells were ordinary wells, so the earlier assertion that we were merely
  retaining repairability was incorrect. This phase deliberately enables the
  existing three repair-site systems for the exact Wellmeet/TentCamp profile;
  the First Tent and unrelated villages remain controls. The native site
  mechanics, costs, consequences and save representation stay unchanged.
- FC08/FC09 captured refinement REDs before changing plume geometry, tent
  corners, packed-earth paths, shelf coverage, reservations and profile retry
  behavior. Olderdeep's generic final-floor connectivity pass was removed:
  its random edge cuts became sealed pockets after the native founding shell.
  The finite base already supplies connected entries. Independent four-way
  flood checks, lower-first access and forced stair positions cover the repair.
- FC11 exported the intermediate 108 models. FC12 ran 310 targeted cases:
  all native composition and kit cases passed, with eleven remaining rendering
  or harness failures. The real portable frog/gecko failures were fixed by
  deriving actor identity independently of area art selection. Reflection on
  a nonexistent preview factory and an incorrect borrowed wall family were
  distinguished from native gameplay failures.
- FC13 rendered all twelve refined native views. FC14 then verified actual
  paid repairs, owner removal and native gameplay while exposing five remaining
  model-coverage cases and two arrival diagnostic failures. Rejected Olderdeep
  arrival calls now reset their diagnostic before checking dependencies.
- FC15 captured final art/recipe RED: dedicated muted Olderdeep cutaway stone,
  very low Wellmeet activity markers, and exact Farmer/WellKeeper aliases.
  The final libraries contain 40 Olderdeep and 76 Wellmeet variants. No new
  blueprint, palette, native interaction or invisible collision was added.
- Cold-eye review found that disposable preview managers could publish another
  world's settlement registry. Both area-preview commands now use the explicit
  detached manager factory, with a live-manager countercontrol. A second
  isolation defect remained in native repair Parts: detached owners could emit
  stage auras into `SettlementRuntime.ActiveZone`. FC16 captured all three
  failures before adding actual zone-membership guards in the well, oven and
  lantern Parts. The same owners, once attached, still emit their native aura.
- FC17 rebuilt the final 116 assets and rendered all four chunks at three seeds
  with zero C# errors. Every visible native owner has a model, with zero missing
  meshes in all twelve captures. Reviewed the muted founding floor, low plumes,
  connected cloth corners and low service markers; native layouts were not
  manually repositioned. Final targeted/full regression and reproducibility
  audits follow this capture.
- FC18 passed all 652 focused cases with zero C# errors, including all 341 new
  phase cases. FC19 ran the complete suite: 13,199 total, 13,163 passing, the
  same 32 baseline failures plus four obsolete scope assertions. Two named
  Olderdeep's newly supported floor as a non-voxel control; a chest test named
  its newly supported mouth; the older stack test required the generic descent
  builder at Olderdeep. These assertions were updated against the verified
  intended scope, retaining depth-3 refusals, both Stump/Olderdeep chest owner
  and lock controls, and a real Lampwell generic-descent countercontrol. This
  is test-scope maintenance, not four newly fixed gameplay defects.

## Final verification, review, and installation

- FC20 initially exposed a test-edit compile error: a repeated declaration was
  changed outside the parameterized stack fixture. The unrelated declaration
  was restored; this was a harness correction, not a gameplay fix. FC20b then
  passed all 451 selected native/art/scope checks with zero C# errors.
- FC21 completed export but crashed in Unity Burst during shutdown. It is not
  recorded as a successful run. FC21b completed with exit0 and zero C# errors;
  all 470 generated asset and metadata files reproduce byte for byte.
- FC22 complete regression: 13,169 passing tests, exactly 32 unchanged baseline failures, zero C# errors and no new failures. All 344 additional tests pass.
- All twelve FC17 native captures have zero missing meshes and zero unmodeled
  visible owners. The final independent material review found no blocking
  defect. The reusable menu is Caves of Ooo → Composition → Render Olderdeep
  and Wellmeet previews; `Tools/founding_camp_gallery.py` builds its viewer.
- All 116 new models are installed in the actual game Resources directories.
  No task GUID collides among 5,988 scanned metadata files.
  Source/input hashes match the isolated project used for final verification.
- The integration manifest distinguishes initially clean shared files from
  files already containing other work. Clean deltas are committed directly;
  mixed-file deltas are installed and recorded in `implementation.patch`, whose
  reverse-check succeeds. Pre-existing unrelated work is excluded from staging.

| Review | Finding and resolution |
|---|---|
| 🟡 Circulation | Olderdeep's late shell sealed generic edge cuts; the redundant floor connectivity pass was removed. Four-way reachability, unusual stairs and lower-first access pass. |
| 🟡 Native ownership | Camp stairs and service frontages needed reservations; the scoped interior-selection flag and late arrival helper protect them without changing other villages. |
| 🟡 World isolation | Detached previews published a settlement registry and detached repair owners emitted auras into another zone. Detached manager construction and actual owner-membership guards now pass positive/negative controls. |
| 🟡 Presentation | Portable cave actors became static in camp; tent corners, plaque/niche facing, bright cave rock and tall ground marks were misleading. Native identity/topology and the final quiet geometry resolve these defects. |
| 🔵 Verified API corrections | Repairability at Wellmeet is deliberately newly enabled; colony/quest glyphs, actual repair recipe names, internal inventory APIs and diagnostic refusal behavior were checked against shipped code. |
| 🧪 Honest limit | Native headless generation, full-suite gameplay contracts, model coverage and static composition are verified. Live input/animation feel and sustained FPS are not measured. |
| ⚪ Intentional native exceptions | Rooted/other explicit indestructible owners and story-gated content retain existing semantics. Art adds no jar alarm, niche sleep, absolute guest protection, extra divine audience or save migration. |

Files changed: new Olderdeep/Wellmeet plans and builders; core/adversarial native
tests; two voxel libraries, two offline builders, two art fixtures; shared
recipes/catalog/presenter/scope and world routing; three repair Parts and the
tracking predicate; scoped village population and regression controls; the
detached preview factory and both native preview commands; 116 generated
models and metadata; gallery helper, these living docs and verification receipts.
Exact paths and before/after hashes are in the close-out integration manifest.
