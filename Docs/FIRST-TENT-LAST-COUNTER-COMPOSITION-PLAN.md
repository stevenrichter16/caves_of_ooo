# First Tent and Last Counter: hospitality and the limit of delivery

Status: complete and installed for fresh native generation. TC17 passes280/280 targeted checks. TC20 full suite: 14,042 passed,32 unchanged baseline failures,0 C# errors and no new failures. All280 added cases pass. TC21 reproduces all166 final asset/metadata files byte for byte; TC22 confirms installation and no task GUID collisions. CoO-original composition and art; no Qud-parity or live-FPS claim.

## Goal and scope

Develop two more complete native places. The First Tent is a gathering around a mortal choice: a generous open oath court, distinct guest thresholds, pilgrim rest and salt-service courts. The Last Counter is the occupied end of a retreating guarantee: a compact working supply court, public disclaimer frontage, and increasingly empty eastward approach. Seeded variation changes useful relationships and shapes; no manual fixes to demo scenes.

Replace fresh surface generation only at Overworld.5.17.0 / Village TentCampFirst and Overworld.18.18.0 / Village ConcordPost. Keep native population, services, loot, cave rolls, travel and late drama. Do not alter already generated graphs, neighboring abandoned counters, spawn, camera1.2x, full reveal, world tiers, saves or the original Unity session.

## Verification sweep / readiness

| Read source | Verified contract / correction | Readiness |
|---|---|---|
| CLAUDE.md; ADVERSARIAL_TESTING.md; Docs/PERF-FOUNDATION.md | Actual RED before production, counter-pairs, dedicated adversarial sweep, independent cold-eye and player-flow gates; generation-only allocations and existing render invalidation. | required |
| WorldMapAuthoring.Places, BiomeRows, TierRows, RoadRows | Both sites are currently Beating tier3. First Tent has mapped road; Last Counter has no mapped road. Neither has mapped river. Historical tier4 Last Counter wording does not change native balance. | green |
| Lore/10_Bible.md; Lore/Factions/07_TentRight.md; Lore/History/08_MaterialCulture.md | First Tent is a monument to a choice, not a god or temple. Threshold, guest-cloth, well and portable shelter are its visual vocabulary. Preserve the actual hospitality implementation rather than inventing new sanctuary rules. | green |
| Lore/Factions/04_SaccharineConcord.md; Lore/History/03_History.md | Last Counter guarantees no delivery beyond its line, withdrawn twice. Adjacent abandoned counters already express time-depth and remain unchanged. No supernatural barrier or delivery simulation is implied. | green |
| LandmarkBuilder.TentRightProfileCamp/FirstTentMonument | Preserve one camp host plus monument keeper, five GuestClothPole owners total, one SaltMaster and the profile well. Main village well remains separate. Native shelter/prop counts may expand through semantic layout. | green |
| LandmarkBuilder.LastCounterPost | Preserve SaccharineEnvoy, LastCounterSign, CampGoodsT1 chest and Campfire; architecture may be rearranged. Do not turn sign into a new menu or fabricate the Counter deity. | green |
| OverworldZoneManager.CreateVillagePipeline | POI profile gates named sites. Existing generic village river is unconditional; composed dry sites must bypass it. Actual cave placement must respect live open arrival footprint. | green |
| AreaCompositionScope; SpawnRing3DRecipes/Presenter | Own current map/profile plus actual native owner membership/visibility governs art. Stable IDs for moving owners. Removal never regenerates scenery. | green |

## Milestones

1. Verify source equivalence with WI14 baseline and record native three-seed census. Write native generation, imported-art and scoped rendering tests; capture actual RED.
2. Build logical semantic plans, atomic base/profile stages and safe arrival reservations. Keep clear route graph, restrained colonies, role-specific interiors and nonuniform spacing.
3. Generate reusable four-variant voxel families for each place's distinguishing native fixtures and actors. Reuse quiet established terrain/services. Each model uses at most two palette swatches and240 vertices, single-cell XZ bounds, combined mesh, shared material, no gameplay components/colliders/lights.
4. Wire managed scope, generation, owner-driven rendering and all required kit dependencies. Test native hospitality/salt delivery and envoy supply interactions with negative controls.
5. Render three seeds per area from the existing gameplay camera; refine generator rules as needed. Run focused/full regression, dedicated adversarial tests, independent review, byte-rebuild/GUID checks, install assets and scoped commit with living docs.

## Verification gates

Determinism/different seeds; positive and wrong address/profile/POI; foreign/restored map ownership; staged rejection without partial mutation; valid builder reuse after rejection; exact profile owners and real conversation IDs; all service approaches connected; live cave neighborhood and late population reservations; native destroyed/moved/hidden ownership; complete art coverage across three seeds; kit corruption and variant uniqueness.

Separate adversarial fixtures probe null, malformed content, lifecycle and map scope. Player-flow hypotheses include oath claim/reclaim/expiry or violence under its actual policy, salt turn-in vs wrong mineral, blocked service frontage, entering from stairs, removing tent wall/sign/chest, and moving actors between supported areas. Record true bugs separately from already-correct pins.

## Performance / honesty bounds

Generation-time plans, shared/batched voxel meshes, no new per-frame scanner, lights or allocation loop. Existing presenter fingerprints and dirty hooks own updates. Headless tests verify native gameplay and static camera previews verify composition. Live input feel, animations and sustained FPS require separate observation; no claim from screenshots or generation timings alone. No Qud parity claim.

## Implementation log

- TC00:1999 source/content/assembly-definition files compare byte-identically with the isolated validation project used for WI14:13,762 passes,32 pre-existing failures,0 compile errors. Reuse that completed full baseline with explicit source proof; a new final full run is mandatory. Original Unity untouched.
- Pre-existing working-tree changes were snapshotted before work; shared-file deltas will be recorded separately from unrelated edits.

## Scope divergences and review

Production comprises two exact named surface areas, three semantic formations per area, and40 new voxel models. Shared native services and assets remain authoritative. The initial three-seed preview set was expanded to four seeds per area so every selected formation receives a visual review; no hand-positioned preview repair is used.

- TC01: native census confirms all six original villages, including5poles/2hosts/2wells at First Tent and the envoy/sign/stock/fire at Last Counter. Both have350 unrelated WaterPuddle owners from the generic river despite no mapped river; composed generation removes that inherited channel. GuestClothPole and LastCounterSign have no DestructiblePart; native policy remains unchanged.
- TC02 records30 compile-error lines for the missing LastCounter native APIs. This is the actual native TDD RED. TC03 isolates the newly authored native fixtures only in the validation clone to capture independent rendering assertion RED; explicit synchronization restores them afterward.

- TC03:43 rendering tests run with0 compile errors,6pass/37expected assertion failures before support. Later production retains native membership and actor-reskin guards. Prior renderer negative-control addresses FirstTent/LastCounter now receive intentional coverage; move those four parameterized controls and one inline control to their still-unsupported depth1. No gate is removed or weakened; final test-name comparison will explicitly account for four readdressed cases.
- Proposed art budget40 variants: FirstTent ground/path/tent/corner/cloth/host; LastCounter ground/wall/sign/envoy. Reuse Wellmeet salt-master/wells/services, native chest/fire, dry vegetation and wreckage.

- TC04 is an intermediate compile attempt blocked by unresolved FirstTent production references; no green or art gate is inferred. TC05 restores shared before-bytes only in the isolated clone to expose72 actual missing-kit error lines. TC06 similarly exposes42 missing FirstTent native API errors in the new22-case fixture. All source/art/native fixtures will be restored by explicit synchronization before integrated validation.
- Native authority correction: Last Counter outdoors uses Floor (not Sand), with no RoadStone because it has no authored road. Its own quiet ground kit maps exact Floor. First Tent uses Sand and RoadStone. Guest hospitality follows the wearer and the salt-master pays reputation, not drams; these details are pinned with real placed NPCs.

- TC07 exports40 models/166 asset and metadata files,0 C# errors. TC08 passes79/79 imported-art checks. TC09 catches a test-only diagnostics method-name typo; no stale XML interpreted. Correct actual Diag.SetChannel, retaining native production.
- TC10 compiles cleanly and runs260 integrated cases:259pass/1fail. Broad34seed FirstTent generation exposes a real monument-pole approach colliding with the guest shelter wall. The generator now selects a live free cardinal frontage when its preferred left approach is occupied; no wall removal or seed-specific relocation. All other gameplay, rendering and art checks pass, including actual purchases/rest/hospitality/mineral exchange.
- TC11 renders all6 actual native graphs at64/1729/729490642, zero missing meshes and unmodeled owners. These are static gameplay-camera views. Visual review finds the high-contrast FirstTent roadgraph dominant and the LastCounter too bare/rectangular. Next rule-level refinement: quiet compatible ground colors, meaningful seeded role formations, contextual western logistics dressing and sparse desert shoulders, preserving the empty eastern travel approach and native owners. Tests precede new refinement production.
- Independent shared/native cross-review confirms own-map scope, actual cave delegate wiring, dependencies, actor lifetime and native contracts. The pole-frontage defect is the only confirmed additional native defect at this point. Player-flow hypothesis tests additionally exercise live wet cave-neighbor veto and tent-corner reorientation across10x5 render-patch boundaries.

## Refinement gate

TC11 independent art review samples all6 views. FirstTent sand32/path64 has excessive luminance separation; choose ground64/path106, reducing swatch luminance separation from.151 to.051. Only8 ground/path meshes change; other32 initial models remain identical. LastCounter walls stay unchanged. The sign's3 authored disclaimer bands face+Z while the gameplay camera is on negativeZ: add actual-camera orientation RED, then rotate its native recipe2quarters, keeping one-sided mesh and owner.

New native gates require3 meaningful role formations per area, preserved service/court roles and broad-seed reachability. FirstTent removes paved spurs to monument poles. LastCounter gains asymmetrical open forecourt returns, clustered supply crates, proper rest mats and sparse western brush/rubble; eastern quiet space stays empty and traversable. These are generator rules, never hand-adjusted preview objects.

- TC12:276 integrated cases,259pass/17expected refinement failures,0 C# errors. All original gameplay/lifecycle checks now pass, including the broad FirstTent seed test after the cardinal-frontage fix. The failures confirm absent meaningful formation selection, excess monument paving, missing forecourt/colonies/furnishings, old ground palette and backward sign orientation. Implement these after the recorded RED.

- TC13: refined40-model export and six native camera captures complete with0 C# errors, missing meshes or unmodeled owners. Independent reviews accept the quieter FirstTent ground, role-dependent shelters, LastCounter asymmetric yard and camera-facing sign. Asset comparison confirms158/166 original files unchanged; only eight intended ground/path mesh UVs differ, with all GUIDs preserved.
- TC14:276 cases,275pass/1fail,0 C# errors. FirstTent native82/82 and imported-art81/81 pass. LastCounter's final service-access assertion fails for seed64 and is investigated before close-out.
- TC15: the failure is a generic native Campfire at(17,24), with open Floor/CampfireGroundMarker cells at(16,24) and(18,24). The formation flood helper deliberately excludes boundary cells, so its mask cannot alone establish this service is inaccessible. Native movement/transition/interaction must establish the correct test premise; no terrain is erased to satisfy the helper.
- TC16: final preview set expands to seeds64,1729,729490642,1 at each area. All eight actual graphs have zero missing meshes and zero unmodeled visible owners, and collectively show all six named formations. Gallery: [eight gameplay-camera previews](Verification/VoxelWorld/TC16-final-preview/index.html). These are static views with the existing camera, not a live playtest or FPS measurement.
- TC17:280/280 focused cases pass,0 C# errors. Full-zone cardinal reach and actual native movement/campfire use establish that the seed64 boundary fire was already accessible; blocking both standing cells supplies its negative control. No LastCounter production geometry change was needed.
- TC18: first complete run14,074 total,14,034 passed,40 failed,0 C# errors. All280 new cases pass and the32 baseline failures retain identical names/messages. Eight older assertions still use the newly converted sites as unconverted controls: three Cinderhold river cases, three Wellmeet river cases, one Sumphold drama case and one Sodden rendering case. Keep their assertions and replace only the obsolete scope fixtures, then rerun focused and full regression. The four additional test sources were proven unchanged against the initial source snapshot before recording their before-bytes; manifest now contains22 shared files.
- TC19:288/288 selected cases pass,0 C# errors: all280 new cases plus the eight corrected legacy controls. Cinderhold/Wellmeet/Sumphold use the same real map addresses with explicit ordinary-Village POIs, retaining their generic-river and non-reservation assertions; the new FirstTent reservation opt-in also has a positive control. Sodden's rendering exclusion uses the still-uncomposed LastCounter depth1. No case is removed and no production behavior changes. The2033-input source freeze is refreshed before the final TC20 full run.


## Final integration and verification

The pending gates in the chronological entries above are closed. The first full run TC18 exposed eight older controls which still treated these now-converted sites as unconverted; assertions were retained and their scope fixtures corrected. TC19 verifies all288 selected new and corrected legacy cases before the final full run. TC17 passes280/280 cases:82 FirstTent native,59 LastCounter native,81 imported-art and58 owner-driven rendering checks. This includes96 native and24 rendering dedicated adversarial cases. TC20 adds no regressions to the verified baseline: all280 new cases pass; the existing32 failures retain identical test names and failure messages. Four old negative controls were readdressed to their still-unsupported depth1 counterparts, with none discarded.

[Final eight-view gallery](Verification/VoxelWorld/TC16-final-preview/index.html) shows four world seeds per area, collectively covering all three formations each. Every actual native graph has zero missing meshes and zero unmodeled visible owners. Independent visual and shared-source reviews are complete. The boundary-fire assertion was corrected using actual native walking and campfire-rest execution; it was not a terrain bug. The seeded FirstTent frontage collision and backward LastCounter sign were confirmed and fixed during development.

TC21 reproduces all40 models /166 asset and metadata files byte-identically, then TC22 installs those exact resources and audits GUID uniqueness. The source freeze verifies2033 source/content/assembly/meta inputs in the original and isolated project, unchanged throughout the final full run and rebuild. [Close-out evidence](Verification/VoxelWorld/TC22-closeout/README.md) records the complete case comparison, artifact hashes, integration delta and source audit.

Fresh native chunks at Overworld.5.17.0 and Overworld.18.18.0 receive these rules through the real world manager. Existing graph instances are preserved. Current spawn,1.2x camera and full-reveal preferences are unchanged. Original Unity was not restarted. Static previews establish composition and coverage; live input feel, animation and sustained FPS are not inferred. Existing mixed source files remain installed; only this phase's exact delta is recorded in the scoped implementation patch, preserving unrelated work.
