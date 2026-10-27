# Slate depths 7–8 — next bounded material coverage slice

**Status, 2026-10-27: source-corrected preflight only.** Shared furniture source
`13178d5c0302bb655347a1daff20425d990b6c84` is verified on main by root; its fresh
packages are underway. Slate is the next queued slice after that delivery.
This updates the work-order context of [the earlier slate audit](GRAPHICS-SLATE-CAVES.md),
without rewriting its history. No Slate native art, runtime, tests or after
acceptance is created here. Root owns Unity, normal gameplay and Git.

The cave reference experiment remains parked/default-off. Keep **straight wall
silhouettes**, flat native16×24 and the normal lighting/grade. The retained
preference for shaded layered2D material relief and quieter flat floors does
not authorize another isolated parity detour. Remaining world families and the
later serious biome-by-biome/area-by-area polish pass stay open; none of the55
inventory or five Stump area polish statuses changes.

## Corrected source contract

All production references in this table were reread for this preflight.

| Potential false premise | Verified contract and consequence |
| --- | --- |
| Shale and Slate share a depth or structural profile | [SolidEarthBuilder:42](../Assets/Scripts/Gameplay/World/Generation/Builders/SolidEarthBuilder.cs:42) assigns SlateWall/SlateFloor only to7–8, between Shale5–6 and Quartzite9–10. [SlateWall:8584](../Assets/Resources/Content/Blueprints/Objects.json:8584) has HP/MaxHP38, hardness3 and real Rubble wreckage, not Shale28/2 or Limestone35/3. Surviving damage1–38 remains the same body. |
| A blue comma is enough to identify slate | [SlateFloor:8875](../Assets/Resources/Content/Blueprints/Objects.json:8875) is exact blueprint SlateFloor, comma, `&b`, GlyphVariants `,,,.'` followed by a backtick. [Terrain:3438](../Assets/Resources/Content/Blueprints/Objects.json:3438) supplies layer0/Terrain; no authored Material, Thermal or Destructible is added. Match actual owner/parts, not glyph alone. |
| Slate needs a new physics material | SlateWall inherits [Wall:3280](../Assets/Resources/Content/Blueprints/Objects.json:3280): solid, layer0, Stone/Mineral, combustibility0, Thermal flame1200/heat-capacity3. Slate changes render to `#`/`&b`/`^k` and its structural values. Preserve existing mechanics and real thermal/status cues. |
| The sixteen existing images are adjacency masks | [EnvironmentSpriteRenderer:527](../Assets/Scripts/Presentation/Rendering/EnvironmentSpriteRenderer.cs:527) describes spatial64×64 macro fields; [floor mapping:567](../Assets/Scripts/Presentation/Rendering/EnvironmentSpriteRenderer.cs:567) and [suffix loading:797](../Assets/Scripts/Presentation/Rendering/EnvironmentSpriteRenderer.cs:797) use SlateFloor and m00…m15 positions. These are opaque16×16 floor slices, not16×24 wall masks. |
| Depth proves original natural generation | [UndergroundLayoutPlan:37](../Assets/Scripts/Gameplay/World/Generation/UndergroundLayoutPlan.cs:37) permits ordinary roomed geometry for selected Spread/Sodden/Beating columns. It is cold generation input, not saved render provenance. An ordinary material adapter may cover valid owners in either layout; it must not claim to know a restored graph's original recipe. |
| Every open cell has SlateFloor | [StrataBuilder:41](../Assets/Scripts/Gameplay/World/Generation/Builders/StrataBuilder.cs:41) uses the mid-depth cave parameters, and [PlaceFloor:99](../Assets/Scripts/Gameplay/World/Generation/Builders/StrataBuilder.cs:99) rolls85% floor/10% Rubble/5% empty. These are probabilities, not an inspected census. Retain disclosed owner-free ambient backing without creating floor entities. |
| An ordinary column contains only six material owners | [CreateUndergroundPipeline:580](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs:580) also applies connectivity, stairs, optional route reservations, landmarks, hazards, population and containers. Generic Wall, VineWall, pillars, mineral nodes, hazards, actors and services are separate identities, not aliases for SlateWall/Floor. Actual presence remains to be censused. |
| Copy the current Shale companion wholesale | Shale now also contains the parked opt-in parity experiment. The simpler [Limestone companion](../Assets/Scripts/Presentation/Rendering/FlatPixelLimestoneCavesCatalog.cs:12) is the bounded policy template; change only verified Slate semantics, depth band and new art contract. Do not copy the study's alpha silhouettes, under-wall floor recesses, preview/light controls or72-frame contract. |

The proposed finite identity set is **two new material owners** plus four exact
borrowed controls: Rubble and Stalagmite retain centered native16×16 resources;
StairsDown uses opening-anchors frame4 and StairsUp opening-cellar frame0.
A provisional six-frame96×24 atlas (floor0–3, wall4–5) is sufficient to evaluate;
final palette/frames remain contingent on actual before views and part review.
No broader family is claimed covered because it appears in a Slate zone.

Read-only decoding reconfirmed the16 old Slate macro images:4096 opaque pixels,
RGB78/82/96 (3243),86/90/104 (550),68/72/86 (303). Preserve all original PNGs,
metadata and editable sources. Their blue-gray material direction can inform
original native16×24 production; stretching those squares or repurposing their
macro indices is not the proposed work. Root must judge collision contrast and
repetition under the actual normal Cave grade, not compensate from a concept.

## Earned baseline and least additional travel

The retained [actual package receipt](Verification/Live16x24/slate-caves-before-play.json)
and directly inspected [entry image](Verification/Live16x24/slate-caves-before-01-packaged-entry.jpg)
show a narrow, very dark glyph corridor with player40,12 HP33 on the up stair.
Root originally Continued97 at depth5, used `>` to6, moved Up to40,12 and used
`>` to7; F5 saved [fixture98](Verification/Live16x24/Fixtures/slate-caves-entry98/Quick.json)
at `Overworld.16.13.7`, turn12620, HP33. Normal `<` returned to6 without damage.
This is an entry/return observation, not depth8, a full chunk or a material reader.

Read-only fixture hashes still match the receipt:

- Quick.json: `d18714d583a8210a3c023950bc9f3a09a63647d0f125a5e21435c26fa5617bc1`
- Quick.sav.gz: `948d90f6114983c8f229215a1394e29d87434cc8067c71c5cb695c03a3363d19`

The least additional travel is unchanged earned98 in root's usual isolated
save session, ordinary Continue, inspect the up stair/real floor/wall readers,
then explore in short normal movement batches for the actual down stair.
Depth8 and its stair position have **not** been observed; do not infer a safe
route from hidden saved entities or generate a prettier graph. Both depths
use population tier3 by [PopulationTable:1102](../Assets/Scripts/Data/Tables/PopulationTable.cs:1102),
so preserve actual combat, damage, refusal and save outcomes. The earlier
undamaged return is not a promise that the next route is safe.

## Admission, backing and essential counterchecks

Prepare only canonical7/8 addresses and preserve the established special-column
exclusions (sinkholes, RootSite, MultiCellPilot, authored wilderness). Require
[WorldLocationContext.For](../Assets/Scripts/Gameplay/World/WorldLocationContext.cs:5),
exact current CachedZones reference, correctly sized live map/POI arrays, null
current POI and the read-only [saved-lair exclusion](../Assets/Scripts/Gameplay/World/Generation/LairStacks.cs:203).
No generating/ledger-adopting query or global regional admission bypass. Source
eligibility is current authority, not proof of historical generation.

Keep actual floor-owned LocalGround separate from owner-free AmbientBackdrop.
The [existing backing policy](../Assets/Scripts/Presentation/Rendering/FlatPixelLimestoneCavesCatalog.cs:124)
refuses nonempty tile state, competing visible meanings/authored backgrounds and
missing/rejected real floor art. Empty-cell/Rubble backing must not disclose
hidden, foreign or custodied occupants or invent passability/material ownership.
Ground beneath an actor never suppresses that actor. Selected-body and authorized
reader identity, memory/FOV, coatings, heat/charge and dirty clearing remain real.

Required genuine RED pairs, before any production adapter:

- Exact7/8 six-owner positives versus6/9, stale graph, special/POI/saved-lair and
  malformed map refusals; genuine visible native preconditions before mutation.
- Slate38/3 structural semantics, surviving damage and real destruction→Rubble;
  exact render/variant/owned parts, custody/footprint/reskin/foreign-role refusals.
- Actual floor versus Rubble/empty backing; actor covering/uncovering, hidden-owner
  invariance, missing independent resources/disposal and real reader authorization.
- Submitted full/dirty paint, removal and water/heat/charge/cultivation fallback;
  no view-only test substituted for actual submitted layer checks.

Use cached Sprite/Tile views and existing finite renderer fixtures. No per-query
resource loads, scene scans, parsing, sprite creation, gameplay events or RNG;
no borrowed texture destruction. Profile actual work and preserve unsupported
allocation probes as inconclusive rather than inferring performance from code.

## Finite gates and remaining world scope

1. Root captures fresh ordinary7/8 entries/returns, wider disclosed routes,
   owners/readers and state census before authoring; retain the actual room,
   landmark, service and actor gaps encountered. Do not seed props for fullness.
2. Actual capture→built-in imagegen study→owner/part extraction→original editable
   native production. Reject changed fog, geometry, UI, scale and generated pixel
   resampling. Keep straight boundaries and accepted source art stable.
3. Genuine RED, minimal companion/hooks, native GREEN and related regressions;
   then actual normal-zoom material/backing/route review. Fix named defects only.
4. Ordinary collision/destruction/Rubble traversal,6↔7↔8, reader and fresh Continue
   checks; bounded profile, independent review, same-commit living records, exact
   source push and fresh packages. No Linux/Deck execution claim without running.

Slate native materials do not finish the underground. Quartzite9–10, Obsidian11+,
special/authored columns, uncovered actors, mineral/hazard/service owners, and
remaining surface biome/settlement/interior families stay in the broader queue.
Already shared storage/furniture can retain their own valid global body contracts;
this slice must not reimplement them. Whole-chunk fullness and later serious
area polish require separate actual evidence, not atlas or owner counts.
