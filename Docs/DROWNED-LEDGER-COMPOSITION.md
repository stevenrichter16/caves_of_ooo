# Drowned Ledger: witness excavation

Status: complete and installed. All 105 owned native tests pass within WI13’s311/311 focused run. WI14 full regression: 13,762 passes, 32 unchanged baseline failures, zero C# errors and no new failures. All 311 additional tests pass.

CoO-original named-place composition. See [the aggregate plan](DROWNED-LEDGER-MARROWSTYE-COMPOSITION-PLAN.md) for source corrections, independent review, visual refinement and exact integration records.

## Scope and native authority

Only fresh generation at `Overworld.17.5.0`, with its current `Village` / `ExcavationCamp` profile, receives this grammar. Renaming the place preserves its function; copying its name or profile elsewhere does not expand coverage. A managed graph retains its own map authority. Existing saves are not migrated or reconstructed, and ordinary villages retain their original builders.

The generated worksite has three unequal wet excavation bays with staggered heads, a protected canvas reading room, a supply house, workers' accommodation, and dry routes to four borders, services and witnesses. The logical plan precedes realization; a seed changes room positions, bay margins and contextual vegetation. Three bounded snag/reed colonies describe the wet margins. Boards mark local work and the southern approach; the central commons stay plain. No object is manually moved to repair a preview.

The late profile contains exactly nine native owners: three `PreFellingBody`, three `SurveyStake`, one `ReadingTable`, one `RecensionScribe`, and one `CurationSorter`. One body lies beside the table under canvas; the other two stand on dry excavation frontages. The parcel offered by the sorter is a fourth, separate *kind* of body, created only by the existing quest action. It never consumes or duplicates a preserved witness.

## Verification sweep and corrections

| Evidence | Verified contract and consequence |
|---|---|
| `WorldMapAuthoring`, WI01 native census | Current Sodden tier2, no mapped road or river. Historical tier3 descriptions are not a rebalance instruction. The former350-cell bottom river came from generic village generation, not authored geography. Omit that builder only for this composed site. |
| `StampCatalog.ExcavationCamp`, WI01 | Nine defining owners survive all three census seeds. The old “on table” comment conflicts with adjacent native cells. Keep the table and body separate; no extra body in the mesh. |
| `Lore/History/03_History.md`; `PreFellingBody` | Readings are attributed, incomplete expedition accounts. The bodies say nothing. Native owners are walkable, weight80, non-takeable, with examination; no creature, container, destruction or body-reading simulation. |
| `TentWall`, `PeatBank`, `Duckboard`, `WaterPuddle` | Canvas and peat/wood fixtures preserve their real thermal/destruction policy. Water is an actual `LiquidPoolPart` owner. `Zone.ProjectPool` gives its cell a permanent coating *while present*; last-owner removal clears it. Boards sit on dry native ground, not hidden water. |
| `ReadingTable`, `SurveyStake` | Native examination fixtures, not new crafting/archaeology tools. Table is solid and non-takeable; stake is walkable and non-takeable. Neither gains destruction or a container through art. |
| `RecensionScribe`, `CurationSorter` | Existing factions, conversation IDs, brains, equipment and self-preservation remain. They are not traders. Retained village services provide the ordinary shop, water, lodging and inhabitants. |
| `FriendlyNPCs.json`, `BodyCourierTests` | Actual sorter accepts the one-shot `BogBodyCourier` quest and grants `SealedBogTakenBody`, weight30/Takeable/NoTrade. A full hard-cap pack drops the parcel at the player's feet; soft carrying capacity still affects movement. Refusing starts nothing. Clerk delivery and hauling are verified by the paired Marrowstye work. |
| `PhysicsPart.Initialize`:45–48, WI07 | A false Physics.Solid parameter alone does not make a tagged ReadingTable non-solid: initialization restores its native Solid tag. The malformed fixture now removes both authorities and asserts the actual created entity; a separate positive countercase pins normalization. No production change was warranted. |

## Implementation

`DrownedLedgerCompositionPlan.Create(zoneId, seed)` supplies read-only semantic `Rooms`, `Bays`, and `Profile`, plus bounded `GroundAt`, `ObjectAt`, `IsInterior`, `IsWet`, `IsApproach`, `IsReserved`, and `Signature` queries. An invalid address throws; out-of-range cell queries return null/false.

`DrownedLedgerCompositionBuilder` runs at1000. It validates every early and late native dependency, stages all owners, and only then commits the graph, interiors and reservations. The exact realized zone reference controls late work. A rejected second call keeps the first successful plan usable. A populated graph cannot be rebuilt; the generation framework may retry only after explicitly emptying the same instance.

`DrownedLedgerProfileBuilder` runs at3860. All nine actual destination cells and native owner contracts validate before publishing any late owner. Replays, foreign instances, blocked cells and malformed final dependencies fail without partial population. The successful flag prevents removed witnesses from being recreated.

`DrownedLedgerArrivalReservationBuilder` runs at3870, after actual cave placement and before population. It protects actual stair owners and eight open standing neighbors without carving or spawning. The base exposes `CanPlaceCaveEntrance(zone, cell)` for scoped native placement. Its complete3×3 arrival area must be dry, exterior, unreserved and clear. It checks live pool/blocker owners as well as the original plan and rejects a foreign cell or graph reference.

Both success and rejection paths emit world-generation diagnostics: `DrownedLedgerCompositionPlanned/Rejected`, `DrownedLedgerProfilePlaced/Rejected`, and `DrownedLedgerArrivalsReserved/Rejected`.

## Performance and presentation

Plans, BFS routes, native validation and staging run only during generation. There is no new per-turn listener, Update loop, physics layer, liquid simulation or hot-path allocation. Shared batched voxel meshes are selected from current owners; existing dirty hooks handle movement/removal. Floor absence remains real and never invokes plan-based scenery reconstruction.

The new art kit uses four variants per defining family and at most two palette swatches. Ground, native cloth, work materials, reeds and service art reuse existing kits. Current refinement proposes a quieter local Duckboard family and a clothing swatch that separates the indoor witness from the floor. The art document and final manifest own exact delivered asset counts.

## In-phase review

- 🟡 Fixed, WI11 GREEN: long bright boards dominated the commons and the excavation heads resembled Sumphold. WI09 confirmed15 new layout cases RED. The generator now staggers the heads by four rows, restricts boards to local work/southern access, adds three bounded snag/reed colonies, and places three actual supply Crates in the smaller reading room.
- 🟡 WI08 camera review: a tall bank face stood immediately in front of both outdoor witnesses. After confirmed RED, the generator leaves three low, dry, reserved foreground cells before each body. The native bodies and camera remain unchanged.
- 🟡 Fixed, WI11 GREEN: actual-pipeline tests confirmed that checking only the stair center accepted a wet or blocked arrival neighbor. The scoped predicate now checks the full arrival area and live owners; clear→blocked→removed and foreign-cell cases pass.
- 🔵 WI07 ReadingTable case was a normalized test input, corrected as described above. Do not describe it as a gameplay bug.
- 🧪 Independent native camera check: all three WI10 Ledger images show separated intact pools, visible foreground witnesses, localized quiet boards, three reed/snags colonies and uncluttered reading activity. No additional material visual issue identified at this static camera. This does not measure live traversal feel.
- ⚪ No excavation procedure, mind-reading, preservation process, freezer, fee, boat movement or repeatable courier contract is introduced. Native refusal and one-shot semantics remain.
- 🧪 Static camera images can establish owner coverage, arrangement and occlusion. They cannot establish live keyboard feel, movement cadence, animation quality or sustained frame performance. Final all-green status is not yet claimed.

## Implementation log

- Preproduction: source survey and native census verified the exact profile, courier departure and historical corrections. `CLAUDE.md` and the adversarial methodology were read before implementation.
- WI04: actual missing-type RED captured by root before native production. Initial core25 and dedicated adversarial59 cases followed the native plan/builders contract.
- WI07: focused run260 total,253 passed,7 failed, zero C# errors. Ledger83/84 passed. Its sole failed case was the normalized ReadingTable mutation; the other six failures belonged to the paired integration. Corrected the fixture and added its normalization countercase.
- WI08: native preview review identified bank occlusion, overlong bright boards and repetitive wet layout. Added15 layout refinement cases, two actual arrival-neighbor cases, and three native DeadTree dependency cases before the corresponding production changes.
- WI09: actual focused RED309 total,257 passed,52 failed, zero C# errors. Ledger20 failures are exactly15 visual-rule cases, three new DeadTree preflight cases and two adjacent arrival-hazard cases. ReadingTable correction/countercase passed. Root authorized the bounded generator and arrival-footprint fixes only after this receipt.
- WI10: rebuilt52 pair models and six native previews. Ledger's three seed images independently inspected after the changes; zero uncovered native owners reported by the root preview receipt.
- WI11: combined targeted309 total,308 passed,1 failed, zero C# errors. All105 Ledger native cases pass (25 core +80 adversarial). The remaining shared fixture expected `board-` instead of the agreed shipped `boards-` family, corrected separately by root. Full-suite results remain pending.
- Seed honesty: the native adversarial fixture sweeps0–31 plus `int.MinValue`/`int.MaxValue` for all dry base cells and intact basins; named final-pipeline/visual cases use64,1729,729490642. No separate external pure-plan receipt or exhaustive seed claim is made.

## Owned files

- `Assets/Scripts/Gameplay/World/Generation/DrownedLedgerCompositionPlan.cs` and metadata.
- `Assets/Scripts/Gameplay/World/Generation/Builders/DrownedLedgerCompositionBuilder.cs` and metadata.
- `Assets/Tests/EditMode/Gameplay/World/DrownedLedgerCompositionTests.cs` and metadata.
- `Assets/Tests/EditMode/Gameplay/World/DrownedLedgerCompositionAdversarialTests.cs` and metadata.
- This document. Shared routing, rendering, art and aggregate verification are owned by the pair's integration pass and listed in its final manifest.
