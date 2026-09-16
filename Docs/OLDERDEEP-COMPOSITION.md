# Olderdeep — native composition

Status: complete and installed; all 85 owned native tests pass. The final exported art and complete owner coverage pass FC17/FC18/FC20b. FC22 full regression has 13,169 passing tests, exactly 32 unchanged baseline failures, zero C# errors and no new failures. All 344 additional tests pass.

The aggregate review and implementation record are in [OLDERDEEP-WELLMEET-COMPOSITION-PLAN.md](OLDERDEEP-WELLMEET-COMPOSITION-PLAN.md). Static native previews do not establish live input feel or sustained frame rate.

## Scope and design

Exactly `Overworld.4.6.0`, `.1`, and `.2`. The current world's sinkhole POI and
`FoundingVillage` profile remain authoritative, including a renamed profile.
Other catacomb villages, other profiles and depth 3+ retain their generators.
This is CoO-original composition and art, not a Qud implementation port.

The surface is a sheltered Grovelands approach whose vegetation frames the
real stone mouth. The descent has two broad turning and stopping
places, grouped low ledges, native rope anchors, lamp jars and one real supply
cache. This differs from the Cathedral's four long shelf bars. A ledge or
rope model does not add a climb verb or simulated fall.

The floor supports the existing founding chamber. Its unchanged native stamp
owns the Rooted, eleven plume owners, two residents, homes and plaques. The
body's eastern wall and empty embrace gap remain authored cultural space,
physically walkable where native geometry permits. No display-driven body
movement, extra divine audience or revealed mortal name is introduced.

## Verification sweep

| Source | Verified contract or correction |
|---|---|
| `Lore/10_Bible.md:131`; `Lore/Factions/06_CatacombVillagers.md:19–23` | A living body holds the embrace; bio-light is the Rooted's dream. The body is not a dead statue or invented boss. |
| `SinkholeSites.cs:16–23,40–48`; `WorldMapAuthoring.cs` | Olderdeep is the Grovelands mouth at4,6 with FoundingVillage profile. Geography's historical tier labels are not a reason to change actual encounter/loot balance. |
| `FoundingVillageBuilder.cs:14–106` | The stamp runs at3650 after stairs. It chooses a stair-safe body row, creates both chamber shells and a real east wall, places exactly eleven walkable plume owners, then residents and furniture. |
| `FoundingVillageBuilder.cs:56–82` | The shell disregards earlier route reservations. Only actual stairs receive corridors through it. New surrounding open floor therefore needs a full-pipeline pocket/arrival test; an open base alone does not prove circulation after the stamp. |
| `Objects.json`: TheRooted/FoundingPlume | The Rooted is an indestructible PhysicalObject fixture, not Creature. Plume is destructible HP40, walkable, native green light, underfoot interaction and HearthPatch war semantics. |
| `FoundingPlumePart.cs:33–62` | A living trusted player must share the actual plume cell during an input-ready session. Ordinary rest charges60 ticks, sets RootedMet once, and grants a two-week clock-based bloom. |
| `FoundingTrustService.cs:17–51` | One actual tepuibone unit can grant the one-time trust threshold. Current tender identity, reach, raw consent, inventory and narrative latch are checked on execution. Hospitality cannot manufacture consent. |
| `Objects.json`: BeetleJar | The current jar supplies light and examination; its description is not a shipped alarm, takeability or destruction mechanism. Art must preserve that distinction. |
| `OverworldZoneManager.cs:934–964,1019–1022` | Native descent/floor ambient is0.28/0.22. The founding chamber does not acquire Ginmere's daylight override. Underground IsInterior remains the existing whole-zone contract. |

## Native architecture

`OlderdeepCompositionPlan` is finite and deterministic, independent of the
caller's random stream. Its `Create`, `IsSupportedZone`, `GroundAt`, `ObjectAt`,
`IsApproach`, `Depth` and `Signature` describe physical native owners only.

`OlderdeepCompositionBuilder` is a fresh-empty-zone base at priority1000.
Required names and actual staged owner Parts must validate before the first
owner or reservation is committed. Failed retries preserve current owners
and clear any published successful plan. Supplies belong to the real cache.

The manager retains native mouth and stair construction, the founding stamp,
hazards, population and containers. A finite late helper protects actual
arrivals. The founding base keeps ordinary earth outside the unchanged main
chamber, with a broad western porch, a north/south entry at x40 and an eastern
passage along the northern edge. The late helper restores these bounded entry
corridors through ordinary stone added by the later shell. It stages missing
floor owners before removing stone, preserves all non-wall owners, and skips
the native body/plume/gap/root-wall region. It is called only by fresh generation;
the public helper does not have a standalone replay guard and must not be used
as a saved-world repair operation. FC12 verified completed four-edge circulation,
all open cells, actual lower-first stairs and unusual sacred-anchor rows.

FC07 exposed an additional ordering mismatch: the generic ConnectivityBuilder
always cuts four randomly positioned edge tunnels even when this base already
has four connected entries. The later native oval can sever those extra tails.
The dedicated founding-floor pipeline therefore omits that redundant pass;
the surface and descent retain it. The base, unchanged founding stamp and
bounded late helper own this one floor's entries. No general connectivity
repair runs after the sacred owners are placed.

Success and refusal emit `worldgen/OlderdeepCompositionPlanned` and
`OlderdeepCompositionRejected`. Arrival protection emits
`OlderdeepArrivalsReserved` or `OlderdeepArrivalsRejected`, including a refusal
reason. No rendering path regenerates a plan or repairs a missing owner.

## Current voxel identity

The final source kit defines 40 models: four variants for each of ten families.
Existing native kits supply ordinary trees, brush, descent cliffs, ledges and
portable supplies. Founding-specific models cover the living body, low lobed
plume, wall-facing niches, plaques, lamp jars, two resident roles and quiet
ground. A final dedicated muted stone-wall quartet supplies the founding
floor's low cutaway; the descent retains its taller cliff presentation.

The eleven native plume owners retain their fixed footprint and same-cell rest
interaction. Their art uses low overlapping lobes and broken corners instead
of a flat bright blanket. The Rooted remains one native cell with eastward arms;
the art does not fill the cultural gap. Niche openings face away from actual
backing walls, so southern niches expose their backs to this camera. Cutaway
walls improve visibility without claiming every bed faces the camera.

Source meshes stay within one horizontal cell, at most two palette swatches and
240 vertices. The final 116-model combined export, asset assertions and
three-seed native gallery now pass; rebuild preserves asset bytes and GUIDs.

## Verification and performance

Core and separate adversarial fixtures cover finite scope/profile controls,
seed/caller-RNG behavior, malformed/missing content atomicity, duplicate
owners, broad resting places, actual lower-first stairs, completed native
frontages and the untouched gap. Existing real offering, same-cell sleep,
war and underfoot-menu fixtures remain regression gates; new integrated
cases exercise those actions against the composed manager-generated floor.

Plans and staged collections allocate only during fresh generation. Existing
batched geometry and current-owner dirty reconciliation handle presentation.
No new per-turn scan, per-voxel GameObject, scheduler or migration is needed.

## Self-review and implementation log

⚪ Preserve deliberate native indestructibility and the absence of rope/climb
and jar-alarm verbs. The initial dream meeting is not the later god audience.

🧪 Headless native graphs and fixed-camera previews can verify observable
mechanics and composition. They cannot verify live input feel, animated
motion quality or sustained gameplay FPS.

- 2026-09-15: Read the shared Olderdeep/Wellmeet plan, current lore authority,
  founding blueprint/stamp/service/rest sources and predecessor composition
  patterns. Recorded the shell/earlier-reservation mismatch before production.
- FC00 reuses the completed predecessor's exact full-suite baseline:12,857
  total,12,825 passes,32 known failures, zero C# errors (parent receipt).
- FC01 unchanged native census completed with zero C# errors before tests were
  saved (parent receipt).
- FC03 captured actual absent Olderdeep plan, builder and arrival-helper types
  as RED, including the intended final native four-edge/pocket contract.
  Parent authorized implementation afterward. No shared native stamp was edited.
- FC05 first compile found a test-only missing `CavesOfOoo.Data` import for
  ConversationLoader. Corrected the import; no production change for that issue.
- Pre-run review corrected the traversal harness: the older formation flood
  intentionally excludes edges and starts from every western interior cell.
  The new gate now starts at one actual western edge cell, follows cardinal
  steps and checks every open cell, including all four transition seams.
- FC07:267 combined cases,251 passed,16 failed, zero C# errors. Olderdeep's only
  three failures were actual unreachable open floor cells at53,18 (seed64),
  26,23 (seed1729), and22,23 (seed729490642). Its other72 native cases, including
  real trust/offering/rest, passed. The extra edge-tunnel cause is described above.
- Added three independent base-without-generic-repair checks and four unusual
  stair/body-row controls before the follow-up run. At this stage the native
  test inventory was 27 core plus 55 adversarial =82 cases.
- FC08:294 combined cases,259 passed,35 failed, zero C# errors. All seven new
  native countercontrols passed. Olderdeep retained only the three known pocket
  failures and the explicit depth2 pipeline assertion. Parent then omitted the
  redundant generic pass only for this founding floor. Native stamp source,
  wall/gap geometry, stairs, population and loot contracts remain unchanged.
- FC06 first preview inspection confirmed the two broad used-descent courts.
  South-facing niche openings were occluded by borrowed high cliff art, and the
  eleven plume models read as a flat bright blanket. Parent/art-agent refinements
  are separately RED-gated: lower cutaway stone on this floor, and a quieter
  lobed plume silhouette. Their actual native owner counts/geometry stay fixed.
- FC12: all82 Olderdeep native cases passed, including completed four-edge
  circulation, exact sacred gap, unusual actual stair anchors, real mineral
  offering and same-cell rest. The XML also records all 55 Wellmeet native
  cases passing. Other shared rendering failures remained, so this was not a
  final suite completion claim.
- Final self-review found two silent arrival-helper refusal branches. Added
  exact reason/zero-mutation diagnostics tests plus a real-stair success counter
  before changing production. FC14 captured both missing-record REDs (313
  combined cases,306 passed,7 failed; the other five failures were separate
  Wellmeet coverage gates). The success control passed. A narrow shared Reject
  helper now emits the zone, seed and reason on all arrival refusal exits.
  FC15 subsequently passed all 58 Olderdeep adversarial cases, including those
  diagnostics. The 27 core cases were already green in FC12. The final combined
  verification still needs to run against the complete final source/art state.
- FC13 independent art review found the borrowed Cathedral cutaway palette too
  bright for native cave stone. FC15 captured a dedicated Olderdeep wall-family
  RED before that final source quartet was added. The final source kit is 40
  Olderdeep models plus 76 Wellmeet models, 116 combined; export and native
  gallery acceptance were pending at this historical handoff; FC17/FC18/FC21b
  subsequently completed those gates.
- A separate final shared-site review confirmed detached well/oven/lantern
  owners could emit auras into the player's active zone. FC16 captured three
  actual REDs. The native membership guards now pass FC18/FC20b and full FC22; no additional Olderdeep geometry or mechanic change is needed.

## Owned files

- `Assets/Scripts/Gameplay/World/Generation/OlderdeepCompositionPlan.cs`
- `Assets/Scripts/Gameplay/World/Generation/Builders/OlderdeepCompositionBuilder.cs`
- `Assets/Tests/EditMode/Gameplay/World/OlderdeepCompositionTests.cs`
- `Assets/Tests/EditMode/Gameplay/World/OlderdeepCompositionAdversarialTests.cs`
- Unity metadata for those files and this living document.

Shared routing, rendering, generated art and the reusable preview command are
installed with this phase; their exact deltas are listed in the close-out receipt.
