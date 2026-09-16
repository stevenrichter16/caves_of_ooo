# First Tent: the open promise

Status: complete and installed for fresh native generation. TC17 passes280/280 targeted checks. TC20 full suite: 14,042 passed,32 unchanged baseline failures,0 C# errors and no new failures. All280 added cases pass. TC21 reproduces all166 final asset/metadata files byte for byte; TC22 confirms installation and no task GUID collisions. CoO-original composition and art; no Qud-parity or live-FPS claim.

## Scope and identity

Only fresh generation at `Overworld.5.17.0`, while its actual map place is `Village` / `TentCampFirst`, receives the First Tent grammar. The current map supplies Beating tier3, a road and no river. A display-name change preserves the profile; the same name or profile at another address cannot reproduce the First Tent. Existing graph instances are not rebuilt.

The composition centres on an unroofed gathering court marked by four guest-cloth poles and an ordinary host. This is the memory of a mortal choice, not a temple, altar or new god. A second host and the fifth pole receive visitors beside a separate guest well. Four unequal shelters have recognizable purposes: receiving guests, shared pilgrim rest, salt service/storage and household life. Native beds, chairs and crates cluster around those uses. The native population's main well remains at40,12. Paths connect thresholds, the two water sources, the open court, the salt service and all four borders. Three sparse dry-vegetation colonies leave broad desert space between activities.

The profile publishes exactly nine native owners: two `TentRightHost`, five `GuestClothPole`, one `SaltMaster`, and one `Well`. The retained population adds a second well and the ordinary merchant, quartermaster, scribe, elder, innkeeper, inhabitants and services. The ordinary native Shrine may remain elsewhere; reservation gates keep it out of the first-oath court. It is not a fabricated monument interaction.

## Verification sweep and corrections

| Read source | Verified native contract and consequence |
|---|---|
| `CLAUDE.md`, `ADVERSARIAL_TESTING.md`, `Docs/PERF-FOUNDATION.md` | Actual RED precedes production, a dedicated adversarial fixture probes state and native player flows, independent review follows, and allocation-heavy planning stays outside hot paths. |
| `WorldMapAuthoring.Places/BiomeRows/TierRows/RoadRows`, TC01 census | Exact5,17 is Beating tier3 with road and no mapped river. Generic village generation previously supplied350 bottom water owners regardless of this geography; omit that builder only for the composed scope. |
| `Lore/Factions/07_TentRight.md`:116–126; `Lore/History/08_MaterialCulture.md`:157–163 | First Tent is a monument to a choice. Thresholds, guest cloth, wells and portable woven shelter give the site its identity. No borrowed deity or fixed stone temple is introduced. |
| `StampCatalog.TentRightProfileCamp/FirstTentMonument`, TC01 | One camp host plus one monument host, five total poles, one salt-master and a profile well. The monument keeper uses the same ordinary host blueprint and dialogue, not a new special role. |
| `TentWall`, `Bed`, `Chair`, `Crate`, `Well` | Canvas remains real solid, combustible cloth with8HP destruction; beds/chairs retain their own interactions, crates retain containers/destruction, wells retain native water service. Visual roof openness does not replace `Cell.IsInterior` as shade authority. |
| `GuestClothPole` | The native pole is walkable and non-takeable with examination. It has no destruction Part, conversation, aura or automatic guest-right. Art does not invent these. |
| `TentRightHost`, `FriendlyNPCs.json`, `UnderTheClothEffect` | The player explicitly claims or refuses hospitality. The effect travels with the guest, expires after three world-clock days, and a renewed claim refreshes it. Hostile people respect it; beasts do not. Attacking a person breaks it for-100 TentRight reputation. Native claim is not gated by a permanent oathbreaker ban; no new ban is inferred from lore prose. |
| `SaltMaster`, `MineralTradeService`, `SaltMaster_1` | One real PaleSalt is consumed for+5 TentRight standing. It is not a drams sale, repair menu or general merchant. Wrong minerals and empty repeated actions yield no reward. |
| `VillagePopulationBuilder`, `HouseDramaBuilder` | Retain native services, stock and settlement hooks. Main well remains a separate late owner. Scoped reservation policies protect court, profile frontages, doors and real stair arrivals from later residents/furniture. |
| `PhysicsPart.Initialize` | Solid tags are an independent native authority. Strict validation checks the actual created entity, not merely JSON strings. No normalized parameter override is claimed as a content bug. |

## Native implementation

`FirstTentCompositionPlan.Create(zoneId, seed)` supplies read-only semantic `Rooms` and `Profile`, `OathX/OathY`, bounded `GroundAt`, `ObjectAt`, `IsInterior`, `IsApproach`, `IsReserved`, `IsOathCourt` and `Signature`. Invalid addresses throw; out-of-range cell queries return null/false. A stable seed selects `FormationName` from three semantic arrangements: `NorthwardWelcome`, `SouthwardWelcome`, or `SaltRoadGathering`. The south welcome reflects all rooms, owners, flags and routes together around the native middle row. The salt-road arrangement exchanges salt service and household sides with a correctly oriented exterior service frontage. Shelter dimensions, court position and contextual dressing still vary within each arrangement. The receiving shelter opens five cells toward the commons. Logical layout precedes all entity creation.

`FirstTentCompositionBuilder` runs at1000. It validates all early and late native dependencies against actual inherited Parts, fields, glyphs, visibility and physical roles before adding any owner. It stages every terrain/fixture placement, then commits owners, interior flags and reservations. Rejected calls leave a previous successful plan usable. A foreign instance with the same address has no authority to consume that plan. A populated graph cannot be reconstructed; the native generation framework may retry the same instance only after explicitly emptying it.

`FirstTentProfileBuilder` runs at3860. All nine real destination cells and owner contracts validate before any profile owner is published. It records the native settlement ID, prevents replay, and does not restore removed hosts or props. A malformed final dependency or blocked destination rejects the complete profile without publishing earlier owners.

`FirstTentArrivalReservationBuilder` runs at3870. It reserves actual stair owners and their open standing neighbors before native population, without carving geometry or recreating objects. The base's `CanPlaceCaveEntrance(zone, cell)` predicate checks the full3×3 current footprint: exterior, dry, clear and unreserved. It reads actual pool/blocker owners and rejects foreign graph/cell references. The First Tent contains no planned water tiles; its two wells remain native solid fixtures.

World-generation diagnostics pair success and failure: `FirstTentCompositionPlanned/Rejected`, `FirstTentProfilePlaced/Rejected`, and `FirstTentArrivalsReserved/Rejected`.

## Presentation and performance

Shared owner-driven rendering supplies the First Tent's six new families: quiet sand, packed path, coarse tent wall, topological corner, broad dark guest cloth and host. Four variants per family share the limited palette and one-cell bounds; ordinary floor, salt-master, furniture and village services reuse existing voxel kits. Corner selection describes current native walls rather than remembered authored geometry. The art document owns exact asset counts and audits.

BFS routes, semantic arrays, validation and staged owners exist only during fresh generation. No new per-frame loop, per-turn scanner, light simulation, collision layer or oath listener is added. Existing owner membership, visibility and dirty hooks continue to govern presentation. Static camera images do not establish live input feel or sustained frame performance.

## In-phase review

- 🟡 TC10 confirmed a seeded frontage collision: the western upper monument pole can sit beside the guest shelter's east wall. Assuming every owner could be approached from its left made the plan throw, even though another cardinal frontage was clear. The generator now chooses an available exterior standing neighbor; it does not delete the wall or move the demonstration seed. TC12 confirms the34-seed native base flood now passes. No geometry was carved to force that frontage.
- 🟡 TC11 static camera review: all three original layouts repeated the same four-corner arrangement, while pole-by-pole paths turned the gathering into a large road diagram. TC12 captured all four new composition cases RED. The generator now offers three different role relationships, broadens guest reception, leaves the entire reserved court as unshaded Sand and skips path generation for walkable poles. TC13 camera review confirms the intended changes; TC14 passes all82 native cases.
- 🔵 The initial diagnostic test used a nonexistent method name; corrected to the shipped `Diag.SetChannel` before the first compiled integration run. No production behavior changed.
- 🧪 Existing native mechanics pinned as correct in TC10: placed-host refusal/claim/refresh/clock expiry, people-versus-beast protection and attack consequences, real salt consumption and wrong-mineral refusal, actual climate shade, replay/foreign-instance gates, malformed-content atomicity, and native cave/late population frontages. These are regression pins, not newly invented gameplay fixes.
- ⚪ No permanent oathbreaker ban, proximity sanctuary, automatic protection from Urqu/Catchers, indirect-harm correction, new god, mining loop or repair economy is introduced. These remain outside this composition's native contract.
- 🧪 Final native/visual review: all three TC13 First Tent images were inspected. The broad public entrance, reflected guest/rest relationship and salt-household exchange read distinctly; both wells and five poles remain visible, and the open court is clear of paved pole spurs. No remaining material First Tent issue was found within this bounded static review. It cannot establish live movement feel or sustained FPS.
- 🧪 Independent Last Counter source review checked open forecourt returns, owner routes, native DryBrush/Rubble semantics and staged supply ownership. No source-only defect was identified. TC14 produced a service-access assertion failure there; TC15–TC17 established that the old flood helper excluded usable boundary cells. Actual movement and campfire use, paired with blocked-standing-cell controls, confirm the native service is reachable. This was a false test premise, not a gameplay bug; no production geometry was changed for it.
- 🧪 Final full regression and final imported-asset audit remain pending. Current camera1.2x and full reveal are unchanged.

## Implementation log

- TC00:1999 source/content/assembly-definition files match the previous validated WI14 baseline byte-for-byte:13,762 passes,32 unchanged baseline failures, zero C# errors. Original Unity remains untouched.
- TC01: native census confirms exact two hosts, five poles, one salt-master and two wells for all three recorded seeds.
- TC06: actual missing-type RED captured before production,42 C# error lines for missing First Tent plan/builders. Parent authorized implementation only after this receipt.
- TC10: integrated260 total,259 passed,1 failed, zero C# errors. Owned native cases are22 core +56 dedicated adversarial =78;77 passed. The sole failure is the broad-seed frontage collision described above. Shared art/rendering and all other native flows passed.
- Post-TC10: narrow cardinal-frontage fix applied. Broad base-generation test now reports the failing seed if any future exception occurs. Actual attack tests additionally position the player on a reachable adjacent standing cell before invoking melee. TC12 confirms this earlier fix and all pre-existing feature cases pass.
- TC11: all three initial native images inspected. Root reports six pair images with zero uncovered owners. The arrangement/road-readability findings above led to generator-rule tests rather than manual scene edits.
- TC12: actual assertion RED276 total,259 passed,17 failed, zero C# errors. All four First Tent formation/court cases fail as expected; the original34-seed endpoint case now passes. Parent authorized the three formations/open Sand court/wider guest entrance only after this receipt.
- Post-TC12: refined semantic rules implemented.
- TC13: all six pair native previews complete with zero C# errors and zero missing/unmodeled owners. First Tent seeds64/1729/729490642 render NorthwardWelcome/SouthwardWelcome/SaltRoadGathering respectively. All three independently inspected as described above.
- TC14:276 combined cases,275 passed,1 failed, zero C# errors. All82 First Tent native cases pass. The sole failure is the Last Counter service-access assertion at seed64; no First Tent failure remains. Subsequent native verification resolves this as a boundary-mask test premise, recorded below.
- TC15–TC17: Last Counter service-access investigation confirms the generic boundary campfire is reachable and usable. The fixture now floods all native cells and pairs actual movement/interaction with blocked controls. TC17 final targeted280/280 pass, zero C# errors; all82 First Tent native cases remain GREEN. No production fix is attributed to the false premise.
- Final shared cold-eye: all18 captured baseline files match their manifest hashes. Exact current deltas preserve map/profile authority through restore, finite generation ordering, live owner visibility and removal, stable moving model variants, current-neighbor tent corners and resource fallback cleanup. No material new shared issue found in this bounded pass. Full regression, byte-rebuild and installation audits remain pending.
- Seed honesty: the native base flood sweeps0–31 and both signed integer extremes; the formation relationship case samples0–63; final-pipeline/visual cases use64,1729,729490642; actual cave rolls use1–5 plus those three seeds. This is bounded regression coverage, not exhaustive seed proof.

## Owned files

- `Assets/Scripts/Gameplay/World/Generation/FirstTentCompositionPlan.cs` and copied metadata.
- `Assets/Scripts/Gameplay/World/Generation/Builders/FirstTentCompositionBuilder.cs` and copied metadata.
- `Assets/Tests/EditMode/Gameplay/World/FirstTentCompositionTests.cs` and copied metadata.
- `Assets/Tests/EditMode/Gameplay/World/FirstTentCompositionAdversarialTests.cs` and copied metadata.
- This document. Shared routing/rendering and art are listed separately in the aggregate implementation manifest.


## Final integration and verification

The pending gates in the chronological entries above are closed. The first full run TC18 exposed eight older controls which still treated these now-converted sites as unconverted; assertions were retained and their scope fixtures corrected. TC19 verifies all288 selected new and corrected legacy cases before the final full run. TC17 passes280/280 cases:82 FirstTent native,59 LastCounter native,81 imported-art and58 owner-driven rendering checks. This includes96 native and24 rendering dedicated adversarial cases. TC20 adds no regressions to the verified baseline: all280 new cases pass; the existing32 failures retain identical test names and failure messages. Four old negative controls were readdressed to their still-unsupported depth1 counterparts, with none discarded.

[Final eight-view gallery](Verification/VoxelWorld/TC16-final-preview/index.html) shows four world seeds per area, collectively covering all three formations each. Every actual native graph has zero missing meshes and zero unmodeled visible owners. Independent visual and shared-source reviews are complete. The boundary-fire assertion was corrected using actual native walking and campfire-rest execution; it was not a terrain bug. The seeded FirstTent frontage collision and backward LastCounter sign were confirmed and fixed during development.

TC21 reproduces all40 models /166 asset and metadata files byte-identically, then TC22 installs those exact resources and audits GUID uniqueness. The source freeze verifies2033 source/content/assembly/meta inputs in the original and isolated project, unchanged throughout the final full run and rebuild. [Close-out evidence](Verification/VoxelWorld/TC22-closeout/README.md) records the complete case comparison, artifact hashes, integration delta and source audit.

Fresh native chunks at Overworld.5.17.0 and Overworld.18.18.0 receive these rules through the real world manager. Existing graph instances are preserved. Current spawn,1.2x camera and full-reveal preferences are unchanged. Original Unity was not restarted. Static previews establish composition and coverage; live input feel, animation and sustained FPS are not inferred. Existing mixed source files remain installed; only this phase's exact delta is recorded in the scoped implementation patch, preserving unrelated work.
