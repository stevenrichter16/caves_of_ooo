# Drowned Ledger and Marrowstye: witness and intake

Status: complete and installed. Two native surface places,52 voxel model variants. WI13 passes all311 focused tests. WI14 full regression: 13,762 passes, 32 unchanged baseline failures, zero C# errors and no new failures. All 311 additional tests pass.

This completes the second pair of the four-area request, after Cinderhold and Sumphold (`d040c598`). Across the two commits:4 native areas,116 new voxel variants and593 additional passing checks. CoO-original composition and art. New layouts apply to fresh generation; current graphs, spawn, camera1.2× and full reveal remain intact.

[Six refined native camera previews](Verification/VoxelWorld/WI10-refined-preview/index.html) · [Final verification](Verification/VoxelWorld/WI16-closeout/README.md).

## Goal and scope

Make two places along an existing courier journey distinguishable through their layout and activity. Drowned Ledger is an excavation camp with protected witness-reading space and unequal wet excavation bays. Marrowstye is a pale, dry intake complex with wide hauling aisles, an offset clerk frontage, two coffer bays, supply quarters and a disused wing. Seed variation changes the plan within each place's functional relationships. Both remain native entity graphs, not decorative scenes.

Only fresh generation at the exact mapped Village/profile is replaced: `Overworld.17.5.0` / ExcavationCamp and `Overworld.12.12.0` / Intake. Do not migrate existing graphs, alter spawn, zoom, reveal, global biome generation, or the original Unity session. Every visible object resolves from a current native owner; destruction, movement and pickup retain native policy.

## Verification sweep and readiness

| Source checked | Verified contract / correction | Readiness |
|---|---|---|
| Lore/10_Bible.md; Lore/History/02_Geography.md; current WorldMapAuthoring | Ledger is currently Sodden tier2, without mapped road/river. Marrowstye is Spread tier1, with road/no river. Historical tier3/tier2 descriptions do not override current balance. | 🟢 |
| LandmarkBuilder.ExcavationCamp | Preserve 3 PreFellingBody, 3 SurveyStake, 1 ReadingTable, 1 RecensionScribe, 1 CurationSorter. The indoor body is adjacent to the table, not atop it; do not add a fourth body in its mesh. | 🟢 |
| LandmarkBuilder.CurationIntake | Preserve 2 StoneCoffer and 1 FilerClerk. Deliberately introduce 2 existing SaltCuredBody owners as intake cargo, with native policy. | 🟢 |
| Objects.json: PreFellingBody, ReadingTable, SurveyStake | Physical examination, no excavation or mind-reading verbs. No takeability/destruction promised. Lore readings stay attributed to the expedition; consciousness remains ambiguous. | 🟢 |
| FriendlyNPCs.json; BodyCourierTests.cs | Sorter grants a separate SealedBogTakenBody (weight30, Takeable, NoTrade). Clerk requires the active BogBodyCourier quest and the carried parcel; consumes it, sets fact, pays25 drams/+10 PaleCuration and completes once. Entering town alone does nothing. | 🟢 |
| Objects.json; DragSystem/DragRules/MovementSystem | StoneCoffer110 and SaltCuredBody90 are solid, haulable, noncarryable/nonthrowable, without Destructible. Coffers have no ContainerPart. Drag capacity Strength×8 gives strength14/12 thresholds; paired below-threshold controls required. | 🟢 |
| Zone.ProjectPool/UnprojectPool; native liquid movement | WaterPuddle is a native owner with projected permanent TileState while present. Dry boards get no underlying pool. Last-owner removal clears projection. | 🟢 |
| OverworldZoneManager; HouseDramaZoneBuilder | Keep native population, loot, services, cave-mouth authority and late drama. Opt into reserved-cell exclusion only for these named sites; stage changes before mutation and protect real arrival neighbors. | 🟢 |
| SpawnRing3DRecipes/Presenter; AreaCompositionScope | Current membership/visibility/profile and owned-map identity vetoes remain authoritative. Portable courier cargo retains its owner mapping in both sites. No synthetic floor fallback. | 🟢 |
| Objects.json: PaleCurator; Lore/Factions/03_PaleCuration.md | Do not surface the deeper curator's shop or lore access here. Existing clerk is not a trader. No curing, fees, refrigerated storage, Catcher procedure or new preservation mechanics. | ⚪ deliberate cut |

## Milestones

1. Record native three-seed census and baseline. Write native, art and rendering tests first; record actual RED before implementation.
2. Implement deterministic logical plans, validated staged base generation, exact late profile placement, and safe native arrivals. Keep unequal work spaces and clear service approaches.
3. Generate44 proposed models: Ledger24 (preserved bodies, stakes, bare table, scribe, sorter, sealed parcel), Marrowstye20 (ground, low walls, coffers, cured bodies, clerk). Four variants per family, no more than two palette swatches, <=240 vertices/model, one-cell XZ bounds. Reuse established terrain, reeds, water, furniture and service kits.
4. Integrate exact scope and owner-driven rendering. Exercise the actual placed sorter-to-clerk courier route and native hauling along the new intake aisles, with negative controls.
5. Render six native gameplay-camera previews; correct generator/art rules when composition is weak. Run targeted, adversarial and full regression, compare exact baseline failures, audit GUIDs and byte-identical asset rebuilding, then install and commit scoped work.

## Verification gates and cold-eye hypotheses

Positive/counter pairs cover deterministic seeds and different seeds; exact versus wrong address/profile/POI; foreign/restored graph authority; missing/malformed content rejection without partial mutation; successful plan reuse after rejected calls; clear versus blocked arrivals; actual native profile counts; reserved wet/service cells versus normal population; carried parcel versus wrong body/missing quest; completion versus repeat attempts; sufficient versus insufficient haul strength; successful hauling versus blocked atomicity; live moved/removed owners versus stale rendering; valid versus corrupt mesh bounds/palette/asset identity.

Separate adversarial fixtures target null/partial content, lifecycle reuse, mutation order, foreign map ownership and late pipeline placement. Cold-eye review compares both implementations, native actor/content contracts and documentation. Player-flow hypotheses include accepting a parcel with full inventory, dropping it under encumbrance, entering intake without delivery, substituting a cured body, retrying rewards, dragging around an obstacle, and removing a pool or cargo owner. Tests classify already-correct contracts separately from actual fixes.

## Performance and presentation

Generation-only plans and batched/shared mesh assets; no new Update loop, per-frame allocation, simulation scan or light emitter. Existing dirty hooks and presenter fingerprints remain in charge of refresh. Record native generation timing as observations, not sustained FPS. Static gameplay-camera previews verify composition and coverage; live input feel, motion and sustained performance remain explicit limitations of headless validation. Keep the user's quiet two-color, coarse voxel direction.

## Implementation log

- Preproduction: agent source sweeps confirm exact owners, current map settings, native courier and haul contracts. Correct historical tiers, the adjacent reading-table body, and one-shot courier semantics before implementation. User's standing authorization covers routine corrections without another approval round.

## Files changed

This plan first. Native plans/builders, dedicated tests, asset kits, scoped renderer/manager integration, reusable preview tool, model resources, living docs and compact receipts will be listed in the final manifest. Pre-existing mixed files are preserved and this phase's delta recorded separately.

- WI00 reuses the freshly completed WB14 baseline after1,864 source/input files compare byte-identically with the isolated project. WI01 captures both original native villages at three seeds before new generation.
- WI02 confirms missing-kit RED (78 compiler-error lines across the two new APIs). WI03 compiles cleanly:54 rendering cases,6pass/48expected failures before new routing/art. Task-owned art tests were temporarily omitted only from the isolated project for this assertion run and are restored by explicit synchronization.
- Cinderhold and Sumphold closed as `d040c598`. Their full regression has13,451passes/32 unchanged baseline failures. The second pair begins from that installed state.

- WI04 confirms actual missing native plan/builder RED (96 compiler-error lines). WI05 exports44 combined models successfully; WI06 passes85 imported-art checks. Full explicit synchronization restores the temporarily isolated native/art fixtures before the integrated gate.
- WI07 compiles cleanly and runs260 cases:253pass/7fail. It confirms a live pipeline cave-filter gap (adjacent blocked arrival), absent Bones art in the intake's disused wing (three seeds), two hauling-speed expectation mismatches, and one ReadingTable malformed-field test needing investigation. Root wires the already-tested per-area placement helpers and reuses registered Beating bones only for these additional named sites. Native agents investigate the remaining source-versus-test mismatches without changing gameplay balance.

## In-phase visual refinement

WI08's six native captures have zero missing meshes/unmodeled owners. Static review nevertheless finds composition weaknesses: the Ledger's bright straight board spine and three pond rectangles repeat the boatyard's silhouette; outdoor witnesses are obscured by the cut face; the indoor witness's dark clothing matches the floor; ordinary camp walls borrow cliff-height art. The intake has clear pale cargo silhouettes but an overly empty hall and four detached rectangles.

Before refinement, add tests for localized board branches, staggered/irregular bays, open cut-face sightlines, witness/floor contrast, low camp walls, real intake partitions with clear haul aisles, mapped-road apron, broken disused wing and bounded border plant colonies. Improve generator rules, never move objects by hand in a demonstration.

Art budget becomes52 models: add four quiet Ledger Duckboard and four Marrowstye RoadStone variants. Change the four preserved-body clothing swatches from floor-matching12 to13, keeping anatomy/two-color budget. Reuse the established low Sumphold stone-wall kit for the Ledger's ordinary shelters. All other initial model changes require an explicit audit explanation.

WI07 source corrections: the synthetic hauling fixture's Stat.Max default30 clipped its intended Speed100; give it the same999 maximum used by native drag fixtures, retaining44/36 drag penalties. Factory initialization normalizes a Physics.Solid=false table back to solid while the native Solid tag remains; the negative control must remove both authorities and assert the mutation actually happened. These are corrected fixture premises, not gameplay balance changes.

- WI09 compiles cleanly:309cases,257pass/52expected refinement failures. The corrected hauling fixture and normalized-Solid controls now pass; real Marrowstye cave-filter wiring passes. Failing new gates expose the Ledger's center-only helper, hidden witness sightlines, overlong boards and missing colonies; intake partitions/road/breached-quiet-wing/plant rules; and the planned contrast/new art families. All refinement production follows this recorded assertion RED.

- WI10 exports52 models and all six refined native views successfully, with zero missing meshes and zero unmodeled owners. Byte comparison against the initial44-model export changes only the four preserved-body mesh assets and two expanded libraries; all176 other original files, including every original prefab and GUID, are identical.
- WI11 passes308/309 focused checks with zero C# errors. Every native generation, courier, hauling, cave-arrival and imported-art assertion passes. The remaining root rendering assertion misspells the agreed family `boards` as `board`; correct its expected prefix to the actual consistent API, without renaming shipped assets or changing production. Rerun required.

- Independent native cross-review finds one additional malformed-content mismatch: intake preflight accepts a clerk glyph that the renderer rejects. WI12 records actual RED for both early and late batches (25cases,23pass/2fail,zero C# errors). The shared native validator now requires the clerk's canonical `@`, before any mutation; canonical restoration is the positive control. No default-content or hauling balance change.
- Independent visual acceptance inspects both areas at64/1729; a second reviewer inspects all three Ledger views and another all three intake views. The resulting native camera coverage is complete for all six images. Readability limits remain at one-cell faces/tools under full-chunk framing. Independent source reviews cover identity, actual content policy, arrival protection, paths, model registration and gameplay courier/haul flows.

## Final verification and cold-eye review

- WI13:311/311 focused passes, zero C# errors. This includes105 Ledger native,
  48 intake native,99 imported-art and59 shared rendering/courier checks.
  The real sorter-to-clerk handoff works with the same parcel; dropping it at
  the window cannot complete delivery. The original three witnesses, two coffers
  and two ordinary cured-body owners remain distinct. Actual drag thresholds,
  speed penalties, blocked-move atomicity and release remain native behavior.
- WI14 full suite: 13,762 passes, 32 unchanged baseline failures, zero C# errors and no new failures. All 311 additional tests pass.
  Exact failure names AND messages match WB14, with no removed baseline cases.
- WI15 rebuild succeeds and all214 final asset/metadata files are byte-identical.
  Install52 models in build-visible Resources. No task GUID collision among
  6,263 scanned metadata files. Final source hashes match the
  isolated validation project and the production freeze used by the full run.
- The first44 models retain176/182 original file bytes. Only four preserved
  mesh assets changed their intended clothing UV and two library arrays expanded.
  Every original prefab and metadata GUID is unchanged. The eight added models
  provide quiet local boards and the intake's established paths.
- Independent native cross-reviews check both implementations against real
  blueprint, conversation, drag, projection and population behavior. Independent
  art review checks both sites at64/1729, with subsequent native reviewers
  inspecting each area's third seed too. The final clerk delta preserves atomic
  preflight in both base and late batches. No unresolved material finding remains.

| Severity | Finding and resolution |
|---|---|
| 🟡 Cave placement | Generic center-only filtering ignored adjacent hazards and left native helpers unused. Both new sites call their realized-plan guards; the Ledger additionally validates/reserves its full3×3 arrival footprint against live blockers and pools. |
| 🟡 Content coverage | Native Bones in the disused wing had no scoped recipe. Reuse its already-registered Beating mesh without adding synthetic debris. |
| 🟡 Witness visibility | Floor-matching clothing and high peat heads obscured bodies. Adjust four model swatches and open three native foreground cells per outside witness; lower ordinary camp walls. |
| 🟡 Composition | Long yellow boards, undifferentiated intake interior and occupied disused wing undermined the places' roles. Local branches, staggered bays, real partitions/paths, clustered native vegetation and reserved quiet rooms fix the generation rules. |
| 🟡 Preflight symmetry | A wrong clerk glyph passed native validation but failed 3D routing. Canonical glyph validation now rejects early and late batches before mutation; restoration controls pass. |
| 🔵 Fixture premises | Native Solid-tag normalization and Stat.Max30 invalidated two synthetic assumptions. Correct fixtures; keep native physics and44/36 hauling penalties. The boards-prefix typo was also confined to a root assertion. |
| 🧪 Visual/performance bounds | Static native camera views and headless gameplay contracts are verified. One-cell faces/tools remain small; no live input feel, animation timing or sustained FPS claim. |
| ⚪ Scope cuts | No new reading/curing procedure, coffer storage, fees, PaleCurator shop, Catcher resolution, global biome replacement or save migration. Body and fixture policies remain native. |

## Installed files and reproducibility

Two logical plans and three builder stages per site; four core/adversarial native
fixtures; two voxel libraries/builders and imported-art fixtures; shared
scope/manager/catalog/renderer integration; native courier and rendering fixtures;
a reusable preview menu and gallery;52 models and copied metadata; living docs
and compact receipts. Exact owned paths, hashes and preserved mixed-file deltas
are in `Docs/Verification/VoxelWorld/WI16-closeout/integration.json` and `implementation.patch`.
The installed patch passes its reverse check. Initially clean shared files and
new owned files are committed directly; unrelated work remains unstaged.

Reusable preview: Caves of Ooo → Composition → Render Drowned Ledger and
Marrowstye previews. Gallery: `python3 Tools/witness_intake_gallery.py <run-dir>`.
The original Unity session was not restarted; validation used an isolated
project. Raw Unity logs/XML remain local and compact receipts are committed.
